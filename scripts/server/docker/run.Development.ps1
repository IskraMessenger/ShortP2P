# Start ShortP2P Messenger Server (Development) in Docker Compose.
#
# Usage:
#   .\scripts\server\docker\run.Development.ps1
#   .\scripts\server\docker\run.Development.ps1 8080 -MemoryMb 1024 -Cpus 2
#   .\scripts\server\docker\run.Development.ps1 8080:51111 -MemoryMb 512 -Cpus 1
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

function Show-Usage {
    @"
Usage: run.Development.ps1 [[-]PortMapping] HOST[:INTERNAL] [options]

Start ShortP2P Messenger Server (Development) via Docker Compose.
Mapping is HOST:INTERNAL. INTERNAL defaults to 51111.

  -PortMapping HOST[:INTERNAL]  e.g. 8080 or 8080:51111
  -HostPort N                   Host (external) port
  -InternalPort N               Container listen port (default 51111)
  -MemoryMb N                   Memory limit in MB (min 512, default 512)
  -Cpus N                       CPU cores (min 1, default 1)
  -CertsDir PATH                Host TLS certs folder
                                (default: `$env:APPUSER\ShortP2P\MessengerServer\certs
                                 or LOCALAPPDATA if APPUSER unset)
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
. (Join-Path $scriptDir '_run-common.ps1')
$composeFile = Join-Path $scriptDir 'docker-compose.Development.yml'
$envFile = Join-Path $scriptDir '.env.Development'
$envExample = Join-Path $scriptDir '.env.Development.example'

$ResolvedHostPort = 51111
$ResolvedInternalPort = 51111
$ResolvedMemoryMb = 512
$ResolvedCpus = 1
if ($env:HOST_PORT -match '^\d+$') { $ResolvedHostPort = [int]$env:HOST_PORT }
if ($env:INTERNAL_PORT -match '^\d+$') { $ResolvedInternalPort = [int]$env:INTERNAL_PORT }
if ($env:MEMORY_MB -match '^\d+$') { $ResolvedMemoryMb = [int]$env:MEMORY_MB }
if ($env:CPUS -match '^\d+$') { $ResolvedCpus = [int]$env:CPUS }

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
if ($MemoryMb -ne 0) {
    if ($MemoryMb -lt 512) { throw "Memory must be at least 512 MB (got $MemoryMb)" }
    $ResolvedMemoryMb = $MemoryMb
}
elseif ($ResolvedMemoryMb -lt 512) {
    throw "MEMORY_MB must be at least 512 (got $ResolvedMemoryMb)"
}
if ($Cpus -ne 0) {
    if ($Cpus -lt 1) { throw "CPUs must be at least 1 (got $Cpus)" }
    $ResolvedCpus = $Cpus
}
elseif ($ResolvedCpus -lt 1) {
    throw "CPUS must be at least 1 (got $ResolvedCpus)"
}

if (-not (Test-Path -LiteralPath $envFile) -and (Test-Path -LiteralPath $envExample)) {
    Copy-Item -LiteralPath $envExample -Destination $envFile
    Write-Host "Created $envFile from .env.Development.example"
}

$ResolvedCertsDir = if ($CertsDir) {
    $CertsDir
}
elseif ($env:CERTS_DIR) {
    $env:CERTS_DIR
}
else {
    Get-DefaultCertsDir
}
New-Item -ItemType Directory -Path $ResolvedCertsDir -Force | Out-Null
$ResolvedCertsDir = (Resolve-Path -LiteralPath $ResolvedCertsDir).Path

$env:HOST_PORT = "$ResolvedHostPort"
$env:INTERNAL_PORT = "$ResolvedInternalPort"
$env:TRUST_SELF_HOST = $ResolvedSelfHost
$env:MEMORY_MB = "$ResolvedMemoryMb"
$env:CPUS = "$ResolvedCpus"
$env:CERTS_DIR = $ResolvedCertsDir

Write-Host "Environment:   Development"
Write-Host "Port mapping:  ${ResolvedHostPort}:${ResolvedInternalPort} (host:internal)"
Write-Host "Resources:     ${ResolvedMemoryMb} MB RAM, ${ResolvedCpus} CPU"
Write-Host "TLS certs:     $ResolvedCertsDir → /etc/shortp2p/certs"
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

Write-Host "Server:  https://localhost:${ResolvedHostPort}"
Write-Host "Swagger: https://localhost:${ResolvedHostPort}/swagger"
