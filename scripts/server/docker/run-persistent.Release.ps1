# Start ShortP2P Messenger Server (Release) with PostgreSQL 11 persistence.
#
# Usage:
#   .\scripts\server\docker\run-persistent.Release.ps1
#   .\scripts\server\docker\run-persistent.Release.ps1 8080 -MemoryMb 1024 -Cpus 2
#   .\scripts\server\docker\run-persistent.Release.ps1 -PersistenceDir 'D:\data\shortp2p\pg'
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

function Show-Usage {
    @"
Usage: run-persistent.Release.ps1 [[-]PortMapping] HOST[:INTERNAL] [options]

Start Release/Production Messenger Server with Persistence (PostgreSQL 11).

  -PortMapping HOST[:INTERNAL]  e.g. 8080 or 8080:51111
  -HostPort N                   Host (external) port
  -InternalPort N               Container listen port (default 51111)
  -MemoryMb N                   Memory limit in MB (min 512, default 512)
  -Cpus N                       CPU cores (min 1, default 1)
  -PersistenceDir PATH          Host folder for Postgres data
                                (default: `$env:APPUSER\ShortP2P\MessengerServer\persistence
                                 or LOCALAPPDATA if APPUSER unset)
  -CertsDir PATH                Host TLS certs folder
                                (default: `$env:APPUSER\ShortP2P\MessengerServer\certs
                                 or LOCALAPPDATA if APPUSER unset)
  -PostgresPort N               Postgres listen/connect port (default 5432; Compose network only)
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
Initialize-DockerNativePlatform
$baseCompose = Join-Path $scriptDir 'docker-compose.yml'
$persistCompose = Join-Path $scriptDir 'docker-compose.persistent.yml'
$envFile = Join-Path $scriptDir '.env.persistent.Release'
$envExample = Join-Path $scriptDir '.env.persistent.Release.example'

$ResolvedHostPort = 51111
$ResolvedInternalPort = 51111
$ResolvedMemoryMb = 512
$ResolvedCpus = 1
$ResolvedPostgresPort = 5432
if ($env:HOST_PORT -match '^\d+$') { $ResolvedHostPort = [int]$env:HOST_PORT }
if ($env:INTERNAL_PORT -match '^\d+$') { $ResolvedInternalPort = [int]$env:INTERNAL_PORT }
if ($env:MEMORY_MB -match '^\d+$') { $ResolvedMemoryMb = [int]$env:MEMORY_MB }
if ($env:CPUS -match '^\d+$') { $ResolvedCpus = [int]$env:CPUS }
if ($env:POSTGRES_PORT -match '^\d+$') { $ResolvedPostgresPort = [int]$env:POSTGRES_PORT }

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

if ($PostgresPort -ne 0) {
    if (-not (Test-Port $PostgresPort)) { throw "Invalid Postgres port: $PostgresPort" }
    $ResolvedPostgresPort = $PostgresPort
}
elseif (-not (Test-Port $ResolvedPostgresPort)) {
    throw "Invalid POSTGRES_PORT: $ResolvedPostgresPort"
}

$ResolvedPersistenceDir = if ($PersistenceDir) {
    $PersistenceDir
}
elseif ($env:PERSISTENCE_DIR) {
    $env:PERSISTENCE_DIR
}
else {
    Get-DefaultPersistenceDir -Suffix 'persistence'
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

New-Item -ItemType Directory -Path $ResolvedPersistenceDir -Force | Out-Null
New-Item -ItemType Directory -Path $ResolvedCertsDir -Force | Out-Null
$ResolvedPersistenceDir = (Resolve-Path -LiteralPath $ResolvedPersistenceDir).Path
$ResolvedCertsDir = (Resolve-Path -LiteralPath $ResolvedCertsDir).Path

if (-not (Test-Path -LiteralPath $envFile) -and (Test-Path -LiteralPath $envExample)) {
    Copy-Item -LiteralPath $envExample -Destination $envFile
    Write-Host "Created $envFile from .env.persistent.Release.example"
}

Prompt-PostgresAdminCredentials -PersistenceDir $ResolvedPersistenceDir

$env:HOST_PORT = "$ResolvedHostPort"
$env:INTERNAL_PORT = "$ResolvedInternalPort"
$env:TRUST_SELF_HOST = $ResolvedSelfHost
$env:MEMORY_MB = "$ResolvedMemoryMb"
$env:CPUS = "$ResolvedCpus"
$env:PERSISTENCE_DIR = $ResolvedPersistenceDir
$env:CERTS_DIR = $ResolvedCertsDir
$env:POSTGRES_PORT = "$ResolvedPostgresPort"
if (-not $env:POSTGRES_USER) { $env:POSTGRES_USER = 'shortp2p' }

Write-Host "Environment:   Production (persistent)"
Write-Host "Docker platform: $(Get-EffectiveDockerPlatformLabel)"
Write-Host "Port mapping:  ${ResolvedHostPort}:${ResolvedInternalPort} (host:internal)"
Write-Host "Resources:     ${ResolvedMemoryMb} MB RAM, ${ResolvedCpus} CPU"
Write-Host "Postgres data: $ResolvedPersistenceDir"
Write-Host "TLS certs:     $ResolvedCertsDir -> /etc/shortp2p/certs"
Write-Host "Postgres port: ${ResolvedPostgresPort} (Compose network Host=postgres; not published to host)"
Write-Host "Postgres admin: $($env:POSTGRES_USER)"
Write-Host "Trust:SelfHost $ResolvedSelfHost  Trust:SelfPort $ResolvedHostPort"

$composeArgs = @('-f', $baseCompose, '-f', $persistCompose)
$composeArgs = @(Add-DockerPlatformComposeArgs -ComposeArgs $composeArgs -ScriptDir $scriptDir)
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
Write-Host "Persistence: PostgreSQL 11 (Persistence:Enabled=true)"
