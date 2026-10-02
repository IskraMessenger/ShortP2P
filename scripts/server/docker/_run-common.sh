#!/usr/bin/env bash
# Shared helpers for run*.sh scripts.
# Sourced only — not executed directly.

# Fail clearly on 32-bit ARM when targeting linux/arm64 natively.
require_arm64_capable_host() {
  local arch
  arch="$(uname -m 2>/dev/null || true)"
  case "$arch" in
    armv6l|armv7l|armhf)
      echo "ARM64 Docker images require a 64-bit host (aarch64 / arm64)." >&2
      echo "This host reports '$arch' (32-bit ARM)." >&2
      echo "Use Raspberry Pi OS 64-bit (or another aarch64 OS), or build/push from" >&2
      echo "an aarch64 machine / Docker buildx with QEMU emulation." >&2
      return 1
      ;;
  esac
  return 0
}

# Export DOCKER_PLATFORM / DOCKER_DEFAULT_PLATFORM for compose build & run.
# Usage: export_docker_platform linux/arm64
export_docker_platform() {
  local platform="${1:-}"
  if [ -z "$platform" ]; then
    echo "export_docker_platform requires a platform (e.g. linux/arm64)" >&2
    return 1
  fi
  export DOCKER_PLATFORM="$platform"
  export DOCKER_DEFAULT_PLATFORM="$platform"
}

# Path to platform compose overlay when DOCKER_PLATFORM is set; else empty.
# Callers: platform_file="$(docker_platform_compose_file "$script_dir")"
#          [ -n "$platform_file" ] && compose_args+=( -f "$platform_file" )
docker_platform_compose_file() {
  local dir="${1:-.}"
  case "${DOCKER_PLATFORM:-}" in
    linux/arm64)
      if [ -f "$dir/docker-compose.arm64.yml" ]; then
        printf '%s\n' "$dir/docker-compose.arm64.yml"
      fi
      ;;
  esac
}

is_port() {
  [[ "$1" =~ ^[0-9]+$ ]] && [ "$1" -ge 1 ] && [ "$1" -le 65535 ]
}

is_positive_int() {
  [[ "$1" =~ ^[0-9]+$ ]] && [ "$1" -ge 1 ]
}

parse_mapping() {
  local value="$1"
  if [[ "$value" == *:* ]]; then
    local host="${value%%:*}"
    local internal="${value#*:}"
    if ! is_port "$host" || ! is_port "$internal"; then
      echo "Invalid port mapping: $value (expected HOST:INTERNAL)" >&2
      return 1
    fi
    HOST_PORT="$host"
    INTERNAL_PORT="$internal"
  else
    if ! is_port "$value"; then
      echo "Invalid host port: $value" >&2
      return 1
    fi
    HOST_PORT="$value"
  fi
}

parse_memory_mb() {
  local value="$1"
  if ! is_positive_int "$value"; then
    echo "Invalid memory (MB): $value (expected integer >= 512)" >&2
    return 1
  fi
  if [ "$value" -lt 512 ]; then
    echo "Memory must be at least 512 MB (got $value)" >&2
    return 1
  fi
  MEMORY_MB="$value"
}

parse_cpus() {
  local value="$1"
  if ! is_positive_int "$value"; then
    echo "Invalid CPUs: $value (expected integer >= 1)" >&2
    return 1
  fi
  CPUS="$value"
}

# APPUSER falls back to LOCALAPPDATA (Windows) or XDG_DATA_HOME / ~/.local/share (Unix).
appuser_root() {
  if [ -n "${APPUSER:-}" ]; then
    printf '%s\n' "$APPUSER"
  elif [ -n "${LOCALAPPDATA:-}" ]; then
    printf '%s\n' "$LOCALAPPDATA"
  else
    printf '%s\n' "${XDG_DATA_HOME:-$HOME/.local/share}"
  fi
}

# Default: $APPUSER/ShortP2P/MessengerServer/<suffix>
default_persistence_dir() {
  local suffix="${1:-persistence}"
  printf '%s\n' "$(appuser_root)/ShortP2P/MessengerServer/$suffix"
}

# Default: $APPUSER/ShortP2P/MessengerServer/certs (independent of persistence).
default_certs_dir() {
  printf '%s\n' "$(appuser_root)/ShortP2P/MessengerServer/certs"
}

find_repo_root() {
  local dir="$1"
  while [ -n "$dir" ] && [ "$dir" != "/" ]; do
    if [ -f "$dir/src/Server/ShortP2P.MessengerServer.Api/ShortP2P.MessengerServer.Api.csproj" ]; then
      printf '%s\n' "$dir"
      return 0
    fi
    dir="$(dirname "$dir")"
  done
  return 1
}

# True when Postgres data dir already initialized (skip credential prompts).
postgres_already_initialized() {
  local persistence_dir="$1"
  [ -d "$persistence_dir/pgdata" ] || [ -f "$persistence_dir/PG_VERSION" ]
}

validate_postgres_password() {
  local password="$1"
  local len="${#password}"
  if [ "$len" -lt 8 ] || [ "$len" -gt 64 ]; then
    echo "Password must be 8–64 characters." >&2
    return 1
  fi
  if ! [[ "$password" =~ ^[A-Za-z0-9]+$ ]]; then
    echo "Password must use only Latin letters and digits (a-z, A-Z, 0-9)." >&2
    return 1
  fi
  if ! [[ "$password" =~ [a-z] ]] || ! [[ "$password" =~ [A-Z] ]] || ! [[ "$password" =~ [0-9] ]]; then
    echo "Password must include uppercase, lowercase, and a digit." >&2
    return 1
  fi
  return 0
}

# Interactive Postgres admin credentials for first persistent start.
# Empty login → shortp2p. Empty password → generated inside the container (not saved on host).
# Sets POSTGRES_USER / POSTGRES_PASSWORD (password may stay empty for auto-gen).
prompt_postgres_admin_credentials() {
  local persistence_dir="$1"
  local input_user="" input_password="" confirm=""

  if postgres_already_initialized "$persistence_dir"; then
    export POSTGRES_USER="${POSTGRES_USER:-shortp2p}"
    echo "Postgres already initialized under $persistence_dir — skipping credential prompt."
    echo "Admin login: $POSTGRES_USER (password is stored in the container secrets volume only)."
    return 0
  fi

  # Non-interactive overrides (CI / already exported).
  if [ -n "${POSTGRES_USER:-}" ] || [ -n "${POSTGRES_PASSWORD:-}" ]; then
    export POSTGRES_USER="${POSTGRES_USER:-shortp2p}"
    if [ -n "${POSTGRES_PASSWORD:-}" ]; then
      validate_postgres_password "$POSTGRES_PASSWORD" || return 1
    fi
    echo "Using Postgres admin from environment: user=$POSTGRES_USER"
    if [ -z "${POSTGRES_PASSWORD:-}" ]; then
      echo "Password empty — will be generated inside the container on first start."
    fi
    return 0
  fi

  if [ ! -t 0 ]; then
    export POSTGRES_USER=shortp2p
    unset POSTGRES_PASSWORD || true
    echo "Non-interactive stdin: Postgres admin user=shortp2p, password will be auto-generated in container."
    return 0
  fi

  echo
  echo "PostgreSQL admin (first start)"
  echo "  Login default: shortp2p"
  echo "  Password: leave empty to auto-generate (8–64 Latin letters+digits; stored only in container)."
  printf "Postgres login [%s]: " "shortp2p"
  read -r input_user || true
  input_user="$(printf '%s' "$input_user" | tr -d '\r')"
  if [ -z "$input_user" ]; then
    input_user="shortp2p"
  fi

  while true; do
    printf "Postgres password (empty = auto-generate): "
    # -s when available (bash); fall back to visible if not a TTY quirk
    if read -r -s input_password; then
      echo
    else
      input_password=""
      echo
    fi
    input_password="$(printf '%s' "$input_password" | tr -d '\r')"
    if [ -z "$input_password" ]; then
      break
    fi
    if ! validate_postgres_password "$input_password"; then
      continue
    fi
    printf "Confirm password: "
    if read -r -s confirm; then
      echo
    else
      confirm=""
      echo
    fi
    confirm="$(printf '%s' "$confirm" | tr -d '\r')"
    if [ "$input_password" = "$confirm" ]; then
      break
    fi
    echo "Passwords do not match." >&2
  done

  export POSTGRES_USER="$input_user"
  if [ -n "$input_password" ]; then
    export POSTGRES_PASSWORD="$input_password"
    echo "Postgres admin: $POSTGRES_USER (password will be stored in the container secrets volume only)."
  else
    unset POSTGRES_PASSWORD || true
    echo "Postgres admin: $POSTGRES_USER (password will be auto-generated inside the container)."
  fi
}

parse_run_args() {
  while [ $# -gt 0 ]; do
    case "$1" in
      -h|--help)
        usage
        exit 0
        ;;
      -p|--host-port)
        shift
        [ $# -gt 0 ] || { echo "--host-port requires a value" >&2; exit 1; }
        parse_mapping "$1" || exit 1
        ;;
      -i|--internal-port)
        shift
        [ $# -gt 0 ] || { echo "--internal-port requires a value" >&2; exit 1; }
        if ! is_port "$1"; then
          echo "Invalid internal port: $1" >&2
          exit 1
        fi
        INTERNAL_PORT="$1"
        ;;
      --self-host)
        shift
        [ $# -gt 0 ] || { echo "--self-host requires a value" >&2; exit 1; }
        TRUST_SELF_HOST="$1"
        ;;
      -m|--memory|--memory-mb)
        shift
        [ $# -gt 0 ] || { echo "--memory requires a value (MB, min 512)" >&2; exit 1; }
        parse_memory_mb "$1" || exit 1
        ;;
      -c|--cpus|--cpu)
        shift
        [ $# -gt 0 ] || { echo "--cpus requires a value (min 1)" >&2; exit 1; }
        parse_cpus "$1" || exit 1
        ;;
      --persistence-dir|--persist-dir)
        shift
        [ $# -gt 0 ] || { echo "--persistence-dir requires a path" >&2; exit 1; }
        PERSISTENCE_DIR="$1"
        ;;
      --certs-dir)
        shift
        [ $# -gt 0 ] || { echo "--certs-dir requires a path" >&2; exit 1; }
        CERTS_DIR="$1"
        ;;
      --postgres-port)
        shift
        [ $# -gt 0 ] || { echo "--postgres-port requires a value" >&2; exit 1; }
        if ! is_port "$1"; then
          echo "Invalid Postgres port: $1" >&2
          exit 1
        fi
        POSTGRES_PORT="$1"
        ;;
      -*)
        echo "Unknown option: $1" >&2
        usage >&2
        exit 1
        ;;
      *)
        parse_mapping "$1" || exit 1
        ;;
    esac
    shift
  done
}
