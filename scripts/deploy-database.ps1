<#
.SYNOPSIS
    Deploys the SecurityRule SQL Server database schema.

.DESCRIPTION
    Runs setup-database.sql against the target SQL Server instance.
    Requires either sqlcmd.exe (installed with SQL Server tools) or
    the SqlServer PowerShell module.

.PARAMETER ServerInstance
    SQL Server instance name or address. Default: localhost.

.PARAMETER UseWindowsAuth
    Use Windows Authentication (default). When set to $false, provide
    -SqlLogin and -SqlPassword for SQL Server Authentication.

.PARAMETER SqlLogin
    SQL Server login name (used only when -UseWindowsAuth is $false).

.PARAMETER SqlPassword
    SQL Server login password (used only when -UseWindowsAuth is $false).

.EXAMPLE
    .\deploy-database.ps1

.EXAMPLE
    .\deploy-database.ps1 -ServerInstance "myserver\SQLEXPRESS"

.EXAMPLE
    .\deploy-database.ps1 -ServerInstance "myserver" -UseWindowsAuth $false `
        -SqlLogin "sa" -SqlPassword "P@ssw0rd"
#>

[CmdletBinding()]
param(
    [string] $ServerInstance  = "localhost",
    [bool]   $UseWindowsAuth  = $true,
    [string] $SqlLogin        = "",
    [string] $SqlPassword     = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir  = $PSScriptRoot
$sqlScript  = Join-Path $scriptDir "setup-database.sql"

if (-not (Test-Path $sqlScript)) {
    Write-Error "SQL script not found: $sqlScript"
    exit 1
}

Write-Host "Deploying SecurityRule database to [$ServerInstance]..." -ForegroundColor Cyan

# ------------------------------------------------------------------
# Try sqlcmd.exe first (available when SQL Server tools are installed)
# ------------------------------------------------------------------
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue

if ($sqlcmd) {
    Write-Host "Using sqlcmd.exe" -ForegroundColor Gray

    if ($UseWindowsAuth) {
        & sqlcmd -S $ServerInstance -E -i $sqlScript
    }
    else {
        & sqlcmd -S $ServerInstance -U $SqlLogin -P $SqlPassword -i $sqlScript
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Error "sqlcmd exited with code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}
else {
    # ------------------------------------------------------------------
    # Fall back to the SqlServer PowerShell module
    # ------------------------------------------------------------------
    Write-Host "sqlcmd.exe not found. Trying SqlServer PowerShell module..." -ForegroundColor Gray

    if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        Write-Host "Installing SqlServer module (requires internet access and admin rights)..." -ForegroundColor Yellow
        Install-Module -Name SqlServer -Scope CurrentUser -Force -AllowClobber
    }

    Import-Module SqlServer -ErrorAction Stop

    $sqlText = Get-Content $sqlScript -Raw

    # Split on GO batch separator (case-insensitive, whole line)
    $batches = $sqlText -split '(?im)^\s*GO\s*$' |
               Where-Object { $_.Trim() -ne "" }

    if ($UseWindowsAuth) {
        foreach ($batch in $batches) {
            Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $batch -TrustServerCertificate
        }
    }
    else {
        $securePassword = ConvertTo-SecureString $SqlPassword -AsPlainText -Force
        $credential     = New-Object System.Management.Automation.PSCredential($SqlLogin, $securePassword)

        foreach ($batch in $batches) {
            Invoke-Sqlcmd -ServerInstance $ServerInstance -Credential $credential `
                          -Query $batch -TrustServerCertificate
        }
    }
}

Write-Host "Database deployment complete." -ForegroundColor Green
