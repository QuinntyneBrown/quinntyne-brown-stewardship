param([int]$Port = 7340, [switch]$SkipBuild, [switch]$RehearseOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Set-Location $workspace
$runDirectory = Join-Path $workspace ('.local/participant-demo-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runDirectory | Out-Null
$savedEnvironment = @{}
foreach ($name in @('PATH','DOTNET_ROOT','DEMO_OUTPUT','FFMPEG_PATH')) { $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name) }
function Invoke-DemoStep([string]$Name, [string]$Executable, [string[]]$Arguments, [int]$Timeout = 180) {
    $child = $null
    try {
        $child = Start-Process -FilePath $Executable -ArgumentList $Arguments -WorkingDirectory $workspace -WindowStyle Hidden -PassThru -RedirectStandardOutput "$runDirectory/$Name.stdout.log" -RedirectStandardError "$runDirectory/$Name.stderr.log"
        if (-not $child.WaitForExit($Timeout * 1000)) { throw "Demo step timed out: $Name" }
        if ($child.ExitCode -ne 0) { throw "Demo step failed: $Name. See $runDirectory/$Name.stderr.log" }
        Write-Output "PASS $Name"
    } finally {
        if ($child -and -not $child.HasExited) { & taskkill.exe /PID $child.Id /T /F | Out-Null }
    }
}
try {
    if (Test-Path -LiteralPath "$workspace/.local/dotnet/dotnet.exe") {
        $env:DOTNET_ROOT = "$workspace/.local/dotnet"
        $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
    }
    $env:DEMO_OUTPUT = $runDirectory
    if (-not $env:FFMPEG_PATH) {
        $encoder = Get-ChildItem -LiteralPath "$workspace/.local/live-demo/ffmpeg" -Filter ffmpeg.exe -Recurse | Select-Object -First 1
        if (-not $encoder) { throw 'Set FFMPEG_PATH to a full FFmpeg distribution.' }
        $env:FFMPEG_PATH = $encoder.FullName
    }
    if (-not $SkipBuild) {
        Invoke-DemoStep 'curriculum' 'node' @('scripts/build-curriculum.mjs')
        Invoke-DemoStep 'build' 'node' @('scripts/build.mjs') 600
    }
    Invoke-DemoStep 'narration' 'node' @('e2e/demo/participant-render.mjs','--narrate')
    Invoke-DemoStep 'prepare' 'pwsh' @('-NoProfile','-File','scripts/prepare-live-demo.ps1','-Port',"$Port",'-OutputDirectory',('"' + $runDirectory + '"'),'-ParticipantDemo') 600
    Invoke-DemoStep 'rehearsal' 'node' @('e2e/demo/record.mjs','--participant','--rehearse') 300
    if ($RehearseOnly) { Write-Output "Rehearsal passed; no video was recorded. Evidence: $runDirectory" }
    else {
        Invoke-DemoStep 'record' 'node' @('e2e/demo/record.mjs','--participant') 720
        Invoke-DemoStep 'render' 'node' @('e2e/demo/participant-render.mjs') 960
        Invoke-DemoStep 'verify' 'node' @('e2e/demo/participant-verify.mjs') 240
        Write-Output "Ready for playback review and promotion: $runDirectory"
    }
} finally {
    $cleanupErrors = @()
    $cliPath = $env:PLAYWRIGHT_CLI_PATH
    if (-not $cliPath) { $cliPath = Join-Path $env:APPDATA 'npm/node_modules/@playwright/cli/playwright-cli.js' }
    if (Test-Path -LiteralPath $cliPath) {
        try { Invoke-DemoStep 'browser-cleanup' 'node' @(('"' + $cliPath + '"'),('-s=' + (Split-Path $runDirectory -Leaf)),'close') 30 }
        catch { $cleanupErrors += $_.Exception.Message }
    }
    $runPath = Join-Path $runDirectory 'run.json'
    if (Test-Path -LiteralPath $runPath) {
        $run = Get-Content -LiteralPath $runPath -Raw | ConvertFrom-Json
        try {
            if ($run.processId) {
                $owned = Get-Process -Id $run.processId -ErrorAction SilentlyContinue
                if ($owned) {
                    if ($owned.StartTime.ToUniversalTime().Ticks -ne ([DateTimeOffset]$run.processStartedAt).UtcTicks) { throw 'Refusing to stop a reused process ID.' }
                    Stop-Process -Id $owned.Id -Force
                    $owned.WaitForExit(10000) | Out-Null
                }
            }
        } catch { $cleanupErrors += $_.Exception.Message }
        try {
            $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($run.connectionString)
            if ($run.database -notmatch '^StewardshipDemo_[a-f0-9]{32}$' -or $builder.InitialCatalog -ne $run.database -or -not $builder.DataSource.StartsWith('np:')) { throw 'Refusing cleanup of an unverified demo database.' }
            $builder['Initial Catalog'] = 'master'
            $connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
            try {
                $connection.Open()
                $command = $connection.CreateCommand()
                $command.CommandTimeout = 30
                $command.CommandText = "IF DB_ID(N'$($run.database)') IS NOT NULL BEGIN ALTER DATABASE [$($run.database)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$($run.database)]; END"
                [void]$command.ExecuteNonQuery()
            } finally { $connection.Dispose() }
        } catch { $cleanupErrors += $_.Exception.Message }
    }
    # The dedicated recorder also closes its own CLI session when a scene fails.
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name]) }
    @{completedAt=[DateTimeOffset]::Now.ToString('o'); errors=@($cleanupErrors); runDirectory=$runDirectory} | ConvertTo-Json | Set-Content -LiteralPath "$runDirectory/cleanup.json"
    if ($cleanupErrors.Count) { throw "Cleanup incomplete for ${runDirectory}: $($cleanupErrors -join '; ')" }
    Write-Output "Demo services and disposable database cleaned up. Evidence: $runDirectory"
}
