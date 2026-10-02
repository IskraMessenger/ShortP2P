# ARM64 / Raspberry Pi wrapper for run-persistent.Release.ps1.
# Forces Docker platform linux/arm64 (useful for buildx cross-build from Windows).
#
# Usage:
#   .\scripts\server\docker\run-arm64-persistent.Release.ps1
#   .\scripts\server\docker\run-arm64-persistent.Release.ps1 8080 -PersistenceDir 'D:\data\shortp2p\pg'
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
& (Join-Path $scriptDir 'run-persistent.Release.ps1') @PSBoundParameters
