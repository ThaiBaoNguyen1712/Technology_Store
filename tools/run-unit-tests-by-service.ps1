[CmdletBinding()]
param(
    [ValidateSet('All', 'Auth', 'Cart', 'Payment', 'Products')]
    [string]$Service = 'All',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot '..\tests\Tech_Store.UnitTests\Tech_Store.UnitTests.csproj'
$resolvedProjectPath = (Resolve-Path $projectPath).Path

$serviceFilters = @{
    Auth = 'Tech_Store.UnitTests.Auth'
    Cart = 'Tech_Store.UnitTests.Cart'
    Payment = 'Tech_Store.UnitTests.Payment'
    Products = 'Tech_Store.UnitTests.Products'
}

$appProcess = Get-Process -Name 'Tech_Store' -ErrorAction SilentlyContinue
$shouldSkipBuild = $NoBuild.IsPresent -or $null -ne $appProcess

$arguments = @(
    'test'
    $resolvedProjectPath
    '--logger'
    'console;verbosity=normal'
)

if ($shouldSkipBuild) {
    $arguments += '--no-build'
} else {
    $arguments += '--no-restore'
}

if ($Service -ne 'All') {
    $arguments += @('--filter', "FullyQualifiedName~$($serviceFilters[$Service])")
}

Write-Host ''
Write-Host '========================================' -ForegroundColor Cyan
Write-Host " Running unit tests for: $Service" -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ''
if ($null -ne $appProcess -and -not $NoBuild.IsPresent) {
    Write-Host 'Detected running Tech_Store process, switching to --no-build to avoid file locks.' -ForegroundColor Yellow
    Write-Host ''
}
Write-Host "dotnet $($arguments -join ' ')" -ForegroundColor DarkGray
Write-Host ''

& dotnet @arguments
exit $LASTEXITCODE
