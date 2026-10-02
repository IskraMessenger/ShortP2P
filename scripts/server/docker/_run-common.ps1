# Shared helpers for run*.ps1 scripts.
# Dot-sourced only - not executed directly.

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
