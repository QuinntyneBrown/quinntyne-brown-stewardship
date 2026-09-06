# Dot-source this file before CLI, API, and integration-test commands on Windows.
# The named pipe also works when the .NET and LocalDB runtime architectures differ.
$instanceInfo = & SqlLocalDB info MSSQLLocalDB
if ($LASTEXITCODE -ne 0) { throw 'SQL Server LocalDB is not installed.' }
if (-not ($instanceInfo -match 'State:\s+Running')) {
    & SqlLocalDB start MSSQLLocalDB
    if ($LASTEXITCODE -ne 0) { throw 'Could not start SQL Server LocalDB.' }
    $instanceInfo = & SqlLocalDB info MSSQLLocalDB
}
$pipeLine = $instanceInfo | Where-Object { $_ -match '^Instance pipe name:' }
$sqlPipe = ($pipeLine -replace '^Instance pipe name:\s*', '').Trim()
if (-not $sqlPipe.StartsWith('np:')) { throw 'LocalDB did not report a named pipe.' }
$env:ConnectionStrings__Stewardship = "Server=$sqlPipe;Database=Stewardship;Integrated Security=True;TrustServerCertificate=True"
$env:STEWARDSHIP_TEST_SQL = "Server=$sqlPipe;Integrated Security=True;TrustServerCertificate=True"
