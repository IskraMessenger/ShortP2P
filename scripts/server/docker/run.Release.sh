#!/usr/bin/env bash
# Start ShortP2P Messenger Server (Release/Production) in Docker Compose.
# For Development use run.Development.sh.
#
# Usage:
#   ./scripts/server/docker/run.Release.sh
#   ./scripts/server/docker/run.Release.sh 8080
#   ./scripts/server/docker/run.Release.sh 8080:51111
#   ./scripts/server/docker/run.Release.sh --host-port 8080 --internal-port 51111
#
# Mapping is HOST:INTERNAL. INTERNAL defaults to 51111.
set -euo pipefail

HOST_PORT="${HOST_PORT:-51111}"
INTERNAL_PORT="${INTERNAL_PORT:-51111}"
TRUST_SELF_HOST="${TRUST_SELF_HOST:-127.0.0.1}"

usage() {
  cat <<'EOF'
Usage: run.Release.sh [HOST_PORT | HOST:INTERNAL] [options]

Start ShortP2P Messenger Server (Release/Production) via Docker Compose.

  HOST_PORT              Publish this host port (internal stays 51111)
  HOST:INTERNAL          Explicit port mapping

  -p, --host-port N      Host (external) port
  -i, --internal-port N  Container listen port (default 51111)
  --self-host ADDR       Trust:SelfHost (default 127.0.0.1)
  -h, --help             Show this help
EOF
}

is_port() {
  [[ "$1" =~ ^[0-9]+$ ]] && [ "$1" -ge 1 ] && [ "$1" -le 65535 ]
}

parse_mapping() {
  local value="$1"
  if [[ "$value" == *:* ]]; then
    local host="${value%%:*}"
    local internal="${value#*:}"
    if ! is_port "$host" || ! is_port "$internal"; then
      echo "Invalid port mapping: $value (expected HOST:INTERNAL)" >&2
      exit 1
    fi
    HOST_PORT="$host"
    INTERNAL_PORT="$internal"
  else
    if ! is_port "$value"; then
      echo "Invalid host port: $value" >&2
      exit 1
    fi
    HOST_PORT="$value"
  fi
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

script_dir="$(cd "$(dirname "$0")" && pwd)"
repo_root="$(find_repo_root "$script_dir" || true)"
if [ -z "${repo_root}" ]; then
  repo_root="$(find_repo_root "$(pwd)" || true)"
fi
if [ -z "${repo_root}" ]; then
  echo "Cannot find the ShortP2P repo root." >&2
  exit 1
fi

compose_file="$script_dir/docker-compose.yml"
env_file="$script_dir/.env"

while [ $# -gt 0 ]; do
  case "$1" in
    -h|--help) usage; exit 0 ;;
    -p|--host-port)
      shift
      [ $# -gt 0 ] || { echo "--host-port requires a value" >&2; exit 1; }
      parse_mapping "$1"
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
    -*)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
    *)
      parse_mapping "$1"
      ;;
  esac
  shift
done

if [ ! -f "$env_file" ] && [ -f "$script_dir/.env.example" ]; then
  cp "$script_dir/.env.example" "$env_file"
  echo "Created $env_file from .env.example"
fi

export HOST_PORT INTERNAL_PORT TRUST_SELF_HOST

echo "Environment:   Production"
echo "Port mapping:  ${HOST_PORT}:${INTERNAL_PORT} (host:internal)"
echo "Trust:SelfHost $TRUST_SELF_HOST  Trust:SelfPort $HOST_PORT"

compose_args=( -f "$compose_file" )
if [ -f "$env_file" ]; then
  compose_args+=( --env-file "$env_file" )
fi

(
  cd "$script_dir"
  HOST_PORT="$HOST_PORT" \
  INTERNAL_PORT="$INTERNAL_PORT" \
  TRUST_SELF_HOST="$TRUST_SELF_HOST" \
  docker compose "${compose_args[@]}" up --build -d
)

echo "Server: https://localhost:${HOST_PORT}"
