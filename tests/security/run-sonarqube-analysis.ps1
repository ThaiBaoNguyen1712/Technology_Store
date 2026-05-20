[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SonarHostUrl,

    [Parameter(Mandatory = $true)]
    [string]$SonarToken,

    [string]$SonarProjectKey = $(if ($env:SONAR_PROJECT_KEY) { $env:SONAR_PROJECT_KEY } else { 'tech-store' }),

    [string]$SonarProjectName = $(if ($env:SONAR_PROJECT_NAME) { $env:SONAR_PROJECT_NAME } else { 'Tech_Store' }),

    [string]$Configuration = 'Release',

    [string]$EvidenceRoot = $(Join-Path $PSScriptRoot '..\..\artifacts\security\sonarqube\manual'),

    [string]$ScannerVersion = $(if ($env:SONAR_SCANNER_VERSION) { $env:SONAR_SCANNER_VERSION } else { '9.*' })
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$solutionPath = Join-Path $repoRoot 'Tech_Store.sln'
$unitTestsProject = Join-Path $repoRoot 'tests\Tech_Store.UnitTests\Tech_Store.UnitTests.csproj'
$resultsDir = Join-Path $EvidenceRoot 'test-results'
$coverageDir = Join-Path $EvidenceRoot 'coverage'
$scannerDir = Join-Path $EvidenceRoot '.tools\dotnet-sonarscanner'

New-Item -ItemType Directory -Force -Path $EvidenceRoot, $resultsDir, $coverageDir, $scannerDir | Out-Null

$javaCommand = Get-Command java -ErrorAction SilentlyContinue
if (-not $javaCommand) {
    throw 'Java runtime was not found. SonarScanner for .NET requires java in PATH.'
}

Write-Host "Installing or updating dotnet-sonarscanner $ScannerVersion into $scannerDir"
dotnet tool update dotnet-sonarscanner --tool-path $scannerDir --version $ScannerVersion | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "dotnet tool update failed with exit code $LASTEXITCODE"
}

$scannerPath = Join-Path $scannerDir 'dotnet-sonarscanner.exe'
if (-not (Test-Path $scannerPath)) {
    throw "dotnet-sonarscanner was not found at $scannerPath"
}

$trxPattern = Join-Path $resultsDir '*.trx'
$coveragePattern = Join-Path $coverageDir '*.opencover.xml'
$coverageOutput = Join-Path $coverageDir 'coverage'

$analysisStarted = $false

try {
    Write-Host 'Starting SonarQube analysis'
    & $scannerPath begin `
        "/k:$SonarProjectKey" `
        "/n:$SonarProjectName" `
        "/d:sonar.host.url=$SonarHostUrl" `
        "/d:sonar.token=$SonarToken" `
        "/d:sonar.cs.vstest.reportsPaths=$trxPattern" `
        "/d:sonar.cs.opencover.reportsPaths=$coveragePattern" `
        "/d:sonar.working.directory=$EvidenceRoot\.sonarqube"

    if ($LASTEXITCODE -ne 0) {
        throw "SonarScanner begin failed with exit code $LASTEXITCODE"
    }

    $analysisStarted = $true

    Write-Host 'Restoring solution for SonarQube'
    dotnet restore $solutionPath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE"
    }

    Write-Host 'Building solution for SonarQube'
    dotnet build $solutionPath -c $Configuration --no-restore | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }

    Write-Host 'Running unit tests with coverage for SonarQube'
    dotnet test $unitTestsProject `
        -c $Configuration `
        --no-build `
        --logger 'trx;LogFileName=unit-tests.trx' `
        --results-directory $resultsDir `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=opencover `
        "/p:CoverletOutput=$coverageOutput" | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}
finally {
    if ($analysisStarted) {
        Write-Host 'Finalizing SonarQube analysis'
        & $scannerPath end "/d:sonar.token=$SonarToken"
        if ($LASTEXITCODE -ne 0) {
            throw "SonarScanner end failed with exit code $LASTEXITCODE"
        }
    }
}

Write-Host "SonarQube evidence saved to $EvidenceRoot"
