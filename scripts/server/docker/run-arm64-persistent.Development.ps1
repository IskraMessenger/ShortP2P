# ARM64 / Raspberry Pi wrapper for run-persistent.Development.ps1.
# Forces Docker platform linux/arm64 (useful for buildx cross-build from Windows).
#
# Usage:
#   .\scripts\server\docker\run-arm64-persistent.Development.ps1
#   .\scripts\server\docker\run-arm64-persistent.Development.ps1 8080 -MemoryMb 1024 -Cpus 2
#
# Swagger: https://localhost:<HostPort>/swagger
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $PortMapping,

    [int] $HostPort = 0,
    [int] $InternalPort = 0,
    [string] $SelfHost = '',
    [int] $MemoryMb = 0,
    [int] $Cpus = 0,
    [string] $PersistenceDir = '',
    [string] $CertsDir = '',
    [int] $PostgresPort = 0,
    [switch] $Help
)

$ErrorActionPreference = 'Stop'

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
. (Join-Path $scriptDir '_run-common.ps1')
Initialize-DockerArm64Platform
Write-Host 'Docker platform: linux/arm64'
try {
    & (Join-Path $scriptDir 'run-persistent.Development.ps1') @PSBoundParameters
}
finally {
    # Do not leave the arm64 marker sticky for a later non-arm64 run in this shell.
    Remove-Item Env:SHORTP2P_DOCKER_ARM64 -ErrorAction SilentlyContinue
}
