# ARM64 / Raspberry Pi wrapper for run.Development.ps1.
# Forces Docker platform linux/arm64 (useful for buildx cross-build from Windows).
#
# Usage:
#   .\scripts\server\docker\run-arm64.Development.ps1
#   .\scripts\server\docker\run-arm64.Development.ps1 8080 -MemoryMb 1024 -Cpus 2
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
    [string] $CertsDir = '',
    [switch] $Help
)

$ErrorActionPreference = 'Stop'

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
. (Join-Path $scriptDir '_run-common.ps1')
Initialize-DockerArm64Platform
Write-Host 'Docker platform: linux/arm64'
try {
    & (Join-Path $scriptDir 'run.Development.ps1') @PSBoundParameters
}
finally {
    Remove-Item Env:SHORTP2P_DOCKER_ARM64 -ErrorAction SilentlyContinue
}
