# Start ShortP2P Messenger Server (Release/Production) in Docker Compose.
# For Development use run.Development.ps1.
#
# Usage:
#   .\scripts\server\docker\run.Release.ps1
#   .\scripts\server\docker\run.Release.ps1 8080
#   .\scripts\server\docker\run.Release.ps1 8080:51111
#   .\scripts\server\docker\run.Release.ps1 -HostPort 8080 -InternalPort 51111
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $PortMapping,

    [int] $HostPort = 0,
    [int] $InternalPort = 0,
    [string] $SelfHost = '',
    [switch] $Help
)

$ErrorActionPreference = 'Stop'

function Show-Usage {
    @"
Usage: run.Release.ps1 [[-]PortMapping] HOST[:INTERNAL] [options]

Start ShortP2P Messenger Server (Release/Production) via Docker Compose.
Mapping is HOST:INTERNAL. INTERNAL defaults to 51111.

  -PortMapping HOST[:INTERNAL]  e.g. 8080 or 8080:51111
  -HostPort N                   Host (external) port
  -InternalPort N               Container listen port (default 51111)
  -SelfHost ADDR                Trust:SelfHost (default 127.0.0.1)
  -Help                         Show this help
"@
}

if ($Help) {
    Show-Usage
    return
}

function Find-RepoRoot {
    $start = @()
    if ($PSScriptRoot) { $start += $PSScriptRoot }
    $start += (Get-Location).Path

    foreach ($origin in $start) {
        $dir = $origin
        while ($dir) {
            $probe = Join-Path $dir 'src\Server\ShortP2P.MessengerServer.Api\ShortP2P.MessengerServer.Api.csproj'
            if (Test-Path -LiteralPath $probe) {
                return (Resolve-Path -LiteralPath $dir).Path
            }
            $parent = Split-Path -Parent $dir
            if (-not $parent -or $parent -eq $dir) { break }
            $dir = $parent
        }
    }

    throw "Cannot find the ShortP2P repo root."
}

function Test-Port([int] $Port) {
    return ($Port -ge 1 -and $Port -le 65535)
}

function Set-PortMapping([string] $Value) {
    if ($Value -match '^(\d+):(\d+)$') {
        $script:ResolvedHostPort = [int]$Matches[1]
        $script:ResolvedInternalPort = [int]$Matches[2]
    }
    elseif ($Value -match '^\d+$') {
        $script:ResolvedHostPort = [int]$Value
    }
    else {
        throw "Invalid port mapping: $Value (expected HOST or HOST:INTERNAL)"
    }

    if (-not (Test-Port $script:ResolvedHostPort)) {
        throw "Invalid host port: $($script:ResolvedHostPort)"
    }
    if (-not (Test-Port $script:ResolvedInternalPort)) {
        throw "Invalid internal port: $($script:ResolvedInternalPort)"
    }
}

$repoRoot = Find-RepoRoot
$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Join-Path $repoRoot 'scripts\server\docker' }
$composeFile = Join-Path $scriptDir 'docker-compose.yml'
$envFile = Join-Path $scriptDir '.env'
$envExample = Join-Path $scriptDir '.env.example'

$ResolvedHostPort = 51111
$ResolvedInternalPort = 51111
if ($env:HOST_PORT -match '^\d+$') { $ResolvedHostPort = [int]$env:HOST_PORT }
if ($env:INTERNAL_PORT -match '^\d+$') { $ResolvedInternalPort = [int]$env:INTERNAL_PORT }

$ResolvedSelfHost = if ($SelfHost) { $SelfHost } elseif ($env:TRUST_SELF_HOST) { $env:TRUST_SELF_HOST } else { '127.0.0.1' }

if ($PortMapping) { Set-PortMapping $PortMapping }
if ($HostPort -ne 0) {
    if (-not (Test-Port $HostPort)) { throw "Invalid host port: $HostPort" }
    $ResolvedHostPort = $HostPort
}
if ($InternalPort -ne 0) {
    if (-not (Test-Port $InternalPort)) { throw "Invalid internal port: $InternalPort" }
    $ResolvedInternalPort = $InternalPort
}

if (-not (Test-Path -LiteralPath $envFile) -and (Test-Path -LiteralPath $envExample)) {
    Copy-Item -LiteralPath $envExample -Destination $envFile
    Write-Host "Created $envFile from .env.example"
}

$env:HOST_PORT = "$ResolvedHostPort"
$env:INTERNAL_PORT = "$ResolvedInternalPort"
$env:TRUST_SELF_HOST = $ResolvedSelfHost

Write-Host "Environment:   Production"
Write-Host "Port mapping:  ${ResolvedHostPort}:${ResolvedInternalPort} (host:internal)"
Write-Host "Trust:SelfHost $ResolvedSelfHost  Trust:SelfPort $ResolvedHostPort"

$composeArgs = @('-f', $composeFile)
if (Test-Path -LiteralPath $envFile) {
    $composeArgs += @('--env-file', $envFile)
}

Push-Location $scriptDir
try {
    & docker compose @composeArgs up --build -d
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose failed (exit $LASTEXITCODE)."
    }
}
finally {
    Pop-Location
}

Write-Host "Server: https://localhost:${ResolvedHostPort}"
