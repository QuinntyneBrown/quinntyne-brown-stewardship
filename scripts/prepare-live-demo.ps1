param([int]$Port = 7240)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Set-Location $workspace
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) { throw "Port $Port is already in use. Choose another demo port." }
. "$PSScriptRoot/use-local-sql.ps1"
$demoDatabase = 'StewardshipDemo_' + (Get-Date -Format 'yyyyMMdd_HHmmss')
$env:ConnectionStrings__Stewardship = $env:ConnectionStrings__Stewardship.Replace('Database=Stewardship;', "Database=$demoDatabase;")
$output = Join-Path $workspace '.local/live-demo'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$cli = Join-Path $workspace 'backend/src/QuinntyneBrownStewardship.Cli/bin/Release/net10.0/QuinntyneBrownStewardship.Cli.dll'
$demoPassword = 'Demo-' + [Guid]::NewGuid().ToString('N') + '!'
function Invoke-DemoCli([string[]]$Arguments, [switch]$Password) {
    if ($Password) { "$demoPassword`n$demoPassword" | & dotnet $cli @Arguments }
    else { & dotnet $cli @Arguments }
    if ($LASTEXITCODE -ne 0) { throw "Demo setup failed: $($Arguments[0])" }
}
function Invoke-DemoSql([string]$Sql) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($env:ConnectionStrings__Stewardship)
    $connection.Open()
    try { $command = $connection.CreateCommand(); $command.CommandText = $Sql; [void]$command.ExecuteNonQuery() } finally { $connection.Dispose() }
}
Invoke-DemoCli @('migrate')
$curriculumPath = 'backend/src/QuinntyneBrownStewardship.Cli/Content/starter-curriculum.json'
# The demo states the programme's size from the document it imports, never from a literal.
$moduleCount = @((Get-Content -LiteralPath (Join-Path $workspace $curriculumPath) -Raw | ConvertFrom-Json).modules).Count
Invoke-DemoCli @('import-curriculum', $curriculumPath)
# The bundled curriculum arrives in draft. The demo publishes it as an administrator would before any cohort follows it;
# the readiness rule itself is proven by the API acceptance suite.
Invoke-DemoSql "UPDATE Curricula SET State = N'Published', PublishedAt = SYSDATETIMEOFFSET() WHERE [Key] = 'starter'; UPDATE m SET m.State = N'Published' FROM Modules m JOIN Curricula c ON c.Id = m.CurriculumId WHERE c.[Key] = 'starter';"
Invoke-DemoCli @('provision-mentor', 'mentor@demo.invalid', 'Quinntyne Brown') -Password
foreach ($account in @('participant', 'rehearsal', 'awaiting', 'cutoff', 'graduate', 'reserved')) {
    Invoke-DemoCli @('provision', "$account@demo.invalid") -Password
}
$cohort = [Guid]::NewGuid(); $endedCohort = [Guid]::NewGuid()
# Two weeks per stage of the cycle the bundled curriculum teaches, and a conversation after each.
$durationWeeks = 10; $sessionCadenceWeeks = 2
$sessionAllowance = [math]::Floor($durationWeeks / $sessionCadenceWeeks)
foreach ($definition in @(
    @{id=$cohort; startDate=(Get-Date).AddDays(-28).ToString('yyyy-MM-dd'); mentorEmail='mentor@demo.invalid'; curriculumKey='starter'; durationWeeks=$durationWeeks; sessionCadenceWeeks=$sessionCadenceWeeks},
    @{id=$endedCohort; startDate=(Get-Date).AddDays(-100).ToString('yyyy-MM-dd'); mentorEmail='mentor@demo.invalid'; curriculumKey='starter'; durationWeeks=$durationWeeks; sessionCadenceWeeks=$sessionCadenceWeeks}
)) {
    $definition | ConvertTo-Json | Set-Content -LiteralPath "$output/cohort.json"
    Invoke-DemoCli @('create-cohort', "$output/cohort.json")
}
foreach ($account in @('participant', 'rehearsal', 'cutoff', 'reserved')) { Invoke-DemoCli @('enroll', "$account@demo.invalid", $cohort.ToString()) }
Invoke-DemoCli @('enroll', 'graduate@demo.invalid', $endedCohort.ToString())
$firstDay = (Get-Date).Date.AddDays(3)
$slots = @()
foreach ($day in @(3,4,5,10,11)) {
    foreach ($hour in @(10,14,16)) { $slots += @{id=[Guid]::NewGuid(); startsAt=([DateTimeOffset](Get-Date).Date.AddDays($day).AddHours($hour)).ToString('o'); durationMinutes=45} }
}
$cutoffSlot = @{id=[Guid]::NewGuid(); startsAt=[DateTimeOffset]::Now.AddHours(12).ToString('o'); durationMinutes=45}
$slots += $cutoffSlot
@{mentorEmail='mentor@demo.invalid';slots=$slots} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$output/availability.json"
Invoke-DemoCli @('publish-availability', "$output/availability.json")

# Historical records are demo fixtures. The running app uses its real clock,
# production services, persistence and authorization for every recorded action.
$connection = [System.Data.SqlClient.SqlConnection]::new($env:ConnectionStrings__Stewardship)
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = @'
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @mentor uniqueidentifier = (SELECT Id FROM Participants WHERE EmailAddress = 'mentor@demo.invalid');
DECLARE @module uniqueidentifier = (SELECT m.Id FROM Modules m JOIN Curricula c ON c.Id = m.CurriculumId WHERE c.[Key] = 'starter' AND m.Ordinal = 1);
DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
UPDATE Sections SET CreatedAt = DATEADD(day,-120,@now);
INSERT INTO Completions (Id,EnrollmentId,SectionId,CompletedAt)
SELECT NEWID(),e.Id,s.Id,DATEADD(day,-22,@now) FROM Enrollments e
JOIN Participants p ON p.Id=e.ParticipantId CROSS JOIN Sections s
WHERE (p.EmailAddress IN ('participant@demo.invalid','rehearsal@demo.invalid') AND s.ModuleId=@module AND s.Ordinal<=2)
OR p.EmailAddress='graduate@demo.invalid';
DECLARE @enrollment uniqueidentifier, @email nvarchar(254), @i int, @slot uniqueidentifier, @booking uniqueidentifier;
DECLARE accounts CURSOR LOCAL FAST_FORWARD FOR
SELECT e.Id,p.EmailAddress FROM Enrollments e JOIN Participants p ON p.Id=e.ParticipantId
WHERE p.EmailAddress IN ('participant@demo.invalid','rehearsal@demo.invalid','graduate@demo.invalid');
OPEN accounts; FETCH NEXT FROM accounts INTO @enrollment,@email;
WHILE @@FETCH_STATUS=0 BEGIN
 SET @i=0;
 WHILE @i < CASE WHEN @email='graduate@demo.invalid' THEN @allowance ELSE 2 END BEGIN
  SET @slot=NEWID(); SET @booking=NEWID();
  INSERT INTO Availability (Id,MentorId,StartsAt,DurationMinutes) VALUES (@slot,@mentor,DATEADD(hour,CASE WHEN @email='rehearsal@demo.invalid' THEN 3 ELSE 0 END,DATEADD(day,CASE WHEN @email='graduate@demo.invalid' THEN -90+@i*12 ELSE -21+@i*14 END,@now)),45);
  INSERT INTO Bookings (Id,EnrollmentId,SlotId,CreatedAt,CancelledAt) VALUES (@booking,@enrollment,@slot,DATEADD(day,-100,@now),NULL);
  INSERT INTO BookingAudits (Id,BookingId,ActorId,Action,PreviousSlotId,SlotId,At,CorrelationId)
  SELECT NEWID(),@booking,ParticipantId,'Booked',NULL,@slot,DATEADD(day,-100,@now),'demo-fixture' FROM Enrollments WHERE Id=@enrollment;
  INSERT INTO Notes (Id,EnrollmentId,ModuleId,SessionId,PromptId,Body,CreatedAt,RevisedAt,Revision)
  VALUES (NEWID(),@enrollment,NULL,@booking,NULL,N'Conversation reflection: listen to the people carrying the cost before choosing a technical solution.',DATEADD(day,-7,@now),DATEADD(day,-7,@now),NEWID());
  SET @i=@i+1;
 END;
 IF @email<>'graduate@demo.invalid' BEGIN
  SET @i=1;
  WHILE @i<=21 BEGIN
   INSERT INTO Notes (Id,EnrollmentId,ModuleId,SessionId,PromptId,Body,CreatedAt,RevisedAt,Revision)
   VALUES (NEWID(),@enrollment,@module,NULL,NULL,CONCAT(N'Practice journal ',@i,N': record the people affected, the decision made, and what to revisit.'),DATEADD(day,-6,@now),DATEADD(minute,-@i,DATEADD(day,-6,@now)),NEWID());
   SET @i=@i+1;
  END;
 END;
 FETCH NEXT FROM accounts INTO @enrollment,@email;
END;
CLOSE accounts; DEALLOCATE accounts;
INSERT INTO Bookings (Id,EnrollmentId,SlotId,CreatedAt,CancelledAt)
SELECT NEWID(),e.Id,CASE WHEN p.EmailAddress='cutoff@demo.invalid' THEN @cutoffSlot ELSE @reservedSlot END,@now,NULL
FROM Enrollments e JOIN Participants p ON p.Id=e.ParticipantId WHERE p.EmailAddress IN ('cutoff@demo.invalid','reserved@demo.invalid');
COMMIT TRANSACTION;
'@
    # The graduate has spent the whole allowance, whatever the cohort's duration and cadence make it.
    [void]$command.Parameters.AddWithValue('@allowance', [int]$sessionAllowance)
    [void]$command.Parameters.AddWithValue('@cutoffSlot', [Guid]$cutoffSlot.id)
    [void]$command.Parameters.AddWithValue('@reservedSlot', [Guid]$slots[0].id)
    [void]$command.ExecuteNonQuery()
} finally { $connection.Dispose() }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "https://localhost:$Port;http://localhost:$($Port-2000)"
$env:HttpsPort = $Port.ToString()
$api = Join-Path $workspace 'backend/src/QuinntyneBrownStewardship.Api/bin/Release/net10.0/QuinntyneBrownStewardship.Api.dll'
$process = Start-Process -FilePath dotnet -ArgumentList @($api) -WorkingDirectory (Join-Path $workspace 'backend/src/QuinntyneBrownStewardship.Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput "$output/api.stdout.log" -RedirectStandardError "$output/api.stderr.log"
@{baseUrl="https://localhost:$Port"; database=$demoDatabase; processId=$process.Id; password=$demoPassword; firstDay=$firstDay.ToString('yyyy-MM-dd'); moduleCount=$moduleCount; sessionAllowance=$sessionAllowance; preparedAt=[DateTimeOffset]::Now.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath "$output/run.json"
$ready = $false
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    if ($process.HasExited) { throw "The demo API exited. See $output/api.stderr.log." }
    try {
        $health = Invoke-RestMethod "https://localhost:$Port/health" -SkipCertificateCheck -TimeoutSec 2
        if ($health.status -eq 'Healthy') { $ready = $true; break }
    } catch { Start-Sleep -Milliseconds 500 }
}
if (-not $ready) { throw "The demo API did not become healthy. See $output/api.stderr.log." }
Write-Output "Demo prepared at https://localhost:$Port with database $demoDatabase and API process $($process.Id)."
