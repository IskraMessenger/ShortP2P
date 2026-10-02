# Shared helpers for run*.ps1 scripts.
# Dot-sourced only - not executed directly.

# Fail clearly on 32-bit ARM when targeting linux/arm64 natively.
# On Windows/amd64 this is a no-op (buildx/QEMU cross-build is expected).
function Test-Arm64CapableHost {
    $arch = $null
    try {
        $uname = Get-Command uname -ErrorAction SilentlyContinue
        if ($uname) {
            $arch = (& uname -m 2>$null | Out-String).Trim()
        }
    }
    catch { }

    if (-not $arch) {
        try {
            $osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
            if ($osArch -eq 'Arm' -or $osArch -eq 'Armv6' -or $osArch -eq 'Armv7') {
                $arch = $osArch
            }
        }
        catch { }
    }

    if ($arch -match '^(armv6l|armv7l|armhf|Arm|Armv6|Armv7)$') {
        throw @"
ARM64 Docker images require a 64-bit host (aarch64 / arm64).
This host reports '$arch' (32-bit ARM).
Use Raspberry Pi OS 64-bit (or another aarch64 OS), or build/push from an aarch64 machine / Docker buildx with QEMU emulation.
"@
    }
}

# Set DOCKER_PLATFORM / DOCKER_DEFAULT_PLATFORM for compose build & run.
function Set-DockerPlatform {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Platform
    )
    $env:DOCKER_PLATFORM = $Platform
    $env:DOCKER_DEFAULT_PLATFORM = $Platform
}

function Clear-DockerPlatform {
    Remove-Item Env:DOCKER_PLATFORM -ErrorAction SilentlyContinue
    Remove-Item Env:DOCKER_DEFAULT_PLATFORM -ErrorAction SilentlyContinue
}

# Host OS -> typical Docker linux platform (amd64 / arm64), or $null if unknown.
function Get-HostLinuxDockerPlatform {
    try {
        $osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
        switch ($osArch) {
            'X64' { return 'linux/amd64' }
            'Arm64' { return 'linux/arm64' }
        }
    }
    catch { }

    try {
        $uname = Get-Command uname -ErrorAction SilentlyContinue
        if ($uname) {
            $arch = (& uname -m 2>$null | Out-String).Trim()
            switch -Regex ($arch) {
                '^(x86_64|amd64)$' { return 'linux/amd64' }
                '^(aarch64|arm64)$' { return 'linux/arm64' }
            }
        }
    }
    catch { }

    return $null
}

# Label for script output (effective compose / build platform).
function Get-EffectiveDockerPlatformLabel {
    if ($env:DOCKER_PLATFORM) {
        return $env:DOCKER_PLATFORM
    }
    if ($env:DOCKER_DEFAULT_PLATFORM) {
        return $env:DOCKER_DEFAULT_PLATFORM
    }
    $native = Get-HostLinuxDockerPlatform
    if ($native) {
        return "$native (host default)"
    }
    return 'host default'
}

# Called by non-arm64 run*.ps1 after dot-sourcing this file.
# Clears sticky DOCKER_* from a prior run-arm64-* in the same shell, then forces
# linux/amd64 on amd64 hosts. When invoked from a run-arm64*.ps1 wrapper, keeps arm64.
function Initialize-DockerNativePlatform {
    $fromArm64Wrapper = $false
    foreach ($frame in Get-PSCallStack) {
        if ($frame.ScriptName -and ($frame.ScriptName -match '[\\/]run-arm64[^\\/]*\.ps1$')) {
            $fromArm64Wrapper = $true
            break
        }
    }

    if ($fromArm64Wrapper) {
        Set-DockerPlatform -Platform 'linux/arm64'
        return
    }

    Remove-Item Env:SHORTP2P_DOCKER_ARM64 -ErrorAction SilentlyContinue
    Clear-DockerPlatform

    $native = Get-HostLinuxDockerPlatform
    if ($native -eq 'linux/amd64') {
        # Avoid qemu-user arm64 builds when DOCKER_DEFAULT_PLATFORM or Desktop defaults are sticky.
        Set-DockerPlatform -Platform 'linux/amd64'
    }
}

# Initialize linux/arm64 for Raspberry Pi / cross-build via buildx.
function Initialize-DockerArm64Platform {
    Test-Arm64CapableHost
    $env:SHORTP2P_DOCKER_ARM64 = '1'
    Set-DockerPlatform -Platform 'linux/arm64'
}

# Path to platform compose overlay when DOCKER_PLATFORM is set; else $null.
function Get-DockerPlatformComposeFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ScriptDir
    )
    if ($env:DOCKER_PLATFORM -eq 'linux/arm64') {
        $file = Join-Path $ScriptDir 'docker-compose.arm64.yml'
        if (Test-Path -LiteralPath $file) {
            return $file
        }
    }
    return $null
}

# Append -f <platform-overlay> to compose args when DOCKER_PLATFORM is set.
function Add-DockerPlatformComposeArgs {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [string[]] $ComposeArgs,

        [Parameter(Mandatory = $true)]
        [string] $ScriptDir
    )
    $platformFile = Get-DockerPlatformComposeFile -ScriptDir $ScriptDir
    if ($platformFile) {
        return @($ComposeArgs + @('-f', $platformFile))
    }
    return $ComposeArgs
}

function Get-AppUserRoot {
    if ($env:APPUSER) {
        return $env:APPUSER
    }
    if ($env:LOCALAPPDATA) {
        return $env:LOCALAPPDATA
    }
    return (Join-Path $HOME '.local/share')
}

function Get-DefaultPersistenceDir([string] $Suffix) {
    return (Join-Path (Get-AppUserRoot) "ShortP2P\MessengerServer\$Suffix")
}

# Development default: persistence-development. If only legacy persistence-debug exists, keep using it.
# Ignores sticky $env:PERSISTENCE_DIR that still points at the old default path after the rename.
function Resolve-DevelopmentPersistenceDir {
    param(
        [string] $Explicit = '',
        [string] $FromEnv = ''
    )

    if ($Explicit) {
        return $Explicit
    }

    $preferred = Get-DefaultPersistenceDir -Suffix 'persistence-development'
    $legacy = Get-DefaultPersistenceDir -Suffix 'persistence-debug'

    if ($FromEnv) {
        $fromNorm = $FromEnv.TrimEnd('\', '/')
        $legacyNorm = $legacy.TrimEnd('\', '/')
        $isStickyOldDefault = [string]::Equals($fromNorm, $legacyNorm, [StringComparison]::OrdinalIgnoreCase)
        if (-not $isStickyOldDefault) {
            return $FromEnv
        }
        if ((Test-Path -LiteralPath $legacy) -and -not (Test-Path -LiteralPath $preferred)) {
            return $legacy
        }
        return $preferred
    }

    if ((Test-Path -LiteralPath $legacy) -and -not (Test-Path -LiteralPath $preferred)) {
        return $legacy
    }
    return $preferred
}

# Default: $APPUSER/ShortP2P/MessengerServer/certs (independent of persistence).
function Get-DefaultCertsDir {
    return (Join-Path (Get-AppUserRoot) 'ShortP2P\MessengerServer\certs')
}

function Test-PostgresAlreadyInitialized([string] $PersistenceDir) {
    $pgdata = Join-Path $PersistenceDir 'pgdata'
    $pgVersion = Join-Path $PersistenceDir 'PG_VERSION'
    return (Test-Path -LiteralPath $pgdata) -or (Test-Path -LiteralPath $pgVersion)
}

function Test-PostgresPassword([string] $Password) {
    if ($Password.Length -lt 8 -or $Password.Length -gt 64) {
        Write-Host 'Password must be 8-64 characters.'
        return $false
    }
    if ($Password -notmatch '^[A-Za-z0-9]+$') {
        Write-Host 'Password must use only Latin letters and digits (a-z, A-Z, 0-9).'
        return $false
    }
    if ($Password -notmatch '[a-z]' -or $Password -notmatch '[A-Z]' -or $Password -notmatch '[0-9]') {
        Write-Host 'Password must include uppercase, lowercase, and a digit.'
        return $false
    }
    return $true
}

# Interactive Postgres admin credentials for first persistent start.
# Empty login -> shortp2p. Empty password -> generated inside the container (not saved on host).
function Prompt-PostgresAdminCredentials {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PersistenceDir
    )

    if (Test-PostgresAlreadyInitialized -PersistenceDir $PersistenceDir) {
        if (-not $env:POSTGRES_USER) { $env:POSTGRES_USER = 'shortp2p' }
        Write-Host "Postgres already initialized under $PersistenceDir - skipping credential prompt."
        Write-Host ("Admin login: {0} (password is stored in the container secrets volume only)." -f $env:POSTGRES_USER)
        return
    }

    if ($env:POSTGRES_USER -or $env:POSTGRES_PASSWORD) {
        if (-not $env:POSTGRES_USER) { $env:POSTGRES_USER = 'shortp2p' }
        if ($env:POSTGRES_PASSWORD -and -not (Test-PostgresPassword $env:POSTGRES_PASSWORD)) {
            throw 'Invalid POSTGRES_PASSWORD from environment.'
        }
        Write-Host "Using Postgres admin from environment: user=$($env:POSTGRES_USER)"
        if (-not $env:POSTGRES_PASSWORD) {
            Write-Host 'Password empty - will be generated inside the container on first start.'
        }
        return
    }

    if (-not [Environment]::UserInteractive -or [Console]::IsInputRedirected) {
        $env:POSTGRES_USER = 'shortp2p'
        Remove-Item Env:POSTGRES_PASSWORD -ErrorAction SilentlyContinue
        Write-Host 'Non-interactive session: Postgres admin user=shortp2p, password will be auto-generated in container.'
        return
    }

    Write-Host ''
    Write-Host 'PostgreSQL admin (first start)'
    Write-Host '  Login default: shortp2p'
    Write-Host '  Password: leave empty to auto-generate (8-64 Latin letters+digits; stored only in container).'

    $inputUser = Read-Host 'Postgres login [shortp2p]'
    if ([string]::IsNullOrWhiteSpace($inputUser)) {
        $inputUser = 'shortp2p'
    }

    while ($true) {
        $secure1 = Read-Host 'Postgres password (empty = auto-generate)' -AsSecureString
        $bstr1 = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure1)
        try {
            $inputPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr1)
        }
        finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr1)
        }

        if ([string]::IsNullOrEmpty($inputPassword)) {
            break
        }
        if (-not (Test-PostgresPassword $inputPassword)) {
            continue
        }

        $secure2 = Read-Host 'Confirm password' -AsSecureString
        $bstr2 = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure2)
        try {
            $confirm = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr2)
        }
        finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr2)
        }

        if ($inputPassword -eq $confirm) {
            break
        }
        Write-Host 'Passwords do not match.'
    }

    $env:POSTGRES_USER = $inputUser
    if ([string]::IsNullOrEmpty($inputPassword)) {
        Remove-Item Env:POSTGRES_PASSWORD -ErrorAction SilentlyContinue
        Write-Host ("Postgres admin: {0} (password will be auto-generated inside the container)." -f $inputUser)
    }
    else {
        $env:POSTGRES_PASSWORD = $inputPassword
        Write-Host ("Postgres admin: {0} (password will be stored in the container secrets volume only)." -f $inputUser)
    }
}
