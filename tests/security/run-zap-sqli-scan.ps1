[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$TargetUrl,

    [string]$EvidenceRoot,

    [string]$DockerImage = $(if ($env:ZAP_DOCKER_IMAGE) { $env:ZAP_DOCKER_IMAGE } else { 'ghcr.io/zaproxy/zaproxy:stable' }),

    [int]$MaxMinutes = $(if ($env:ZAP_MAX_MINUTES) { [int]$env:ZAP_MAX_MINUTES } else { 5 })
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($EvidenceRoot)) {
    $EvidenceRoot = Join-Path $PSScriptRoot '..\..\artifacts\security\zap\manual'
}

$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) {
    throw 'Docker CLI was not found. Install Docker Desktop or add docker to PATH before running the ZAP scan.'
}

New-Item -ItemType Directory -Force -Path $EvidenceRoot | Out-Null

$reportJson = 'zap-report.json'
$reportHtml = 'zap-report.html'
$reportMd = 'zap-report.md'
$summaryJson = Join-Path $EvidenceRoot 'sql-injection-summary.json'

Write-Host "Running OWASP ZAP full scan against $TargetUrl"
docker run --rm `
    -v "${EvidenceRoot}:/zap/wrk/:rw" `
    $DockerImage `
    zap-full-scan.py `
    -t $TargetUrl `
    -J $reportJson `
    -r $reportHtml `
    -w $reportMd `
    -m $MaxMinutes `
    -I `
    -d | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "OWASP ZAP scan command failed with exit code $LASTEXITCODE"
}

$jsonPath = Join-Path $EvidenceRoot $reportJson
if (-not (Test-Path $jsonPath)) {
    throw "Expected ZAP JSON report at $jsonPath"
}

$report = Get-Content $jsonPath -Raw | ConvertFrom-Json
$alerts = @($report.site | ForEach-Object { $_.alerts } | Where-Object { $_ })
$sqlInjectionAlerts = @(
    $alerts | Where-Object {
        $_.name -match 'SQL Injection' -or $_.alert -match 'SQL Injection'
    }
)

$summary = [PSCustomObject]@{
    targetUrl = $TargetUrl
    scanTimestampUtc = [DateTime]::UtcNow.ToString('O')
    sqlInjectionAlertCount = $sqlInjectionAlerts.Count
    sqlInjectionAlerts = $sqlInjectionAlerts
}

$summary | ConvertTo-Json -Depth 8 | Set-Content -Path $summaryJson -Encoding UTF8

if ($sqlInjectionAlerts.Count -gt 0) {
    Write-Error "Detected $($sqlInjectionAlerts.Count) SQL injection alert(s). Review $summaryJson and $jsonPath."
    exit 1
}

Write-Host "No SQL injection alerts found. Evidence saved to $EvidenceRoot"
