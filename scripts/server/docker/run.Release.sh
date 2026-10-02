#!/usr/bin/env bash
# Start ShortP2P Messenger Server (Release/Production) in Docker Compose.
# For Development use run.Development.sh.
#
# Usage:
#   ./scripts/server/docker/run.Release.sh
#   ./scripts/server/docker/run.Release.sh 8080 --memory 1024 --cpus 2
#   ./scripts/server/docker/run.Release.sh 8080:51111 -m 512 -c 1
#
# Mapping is HOST:INTERNAL. INTERNAL defaults to 51111.
# Memory min 512 MB, CPUs min 1.
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=_run-common.sh
. "$script_dir/_run-common.sh"
ensure_native_docker_platform

HOST_PORT="${HOST_PORT:-51111}"
INTERNAL_PORT="${INTERNAL_PORT:-51111}"
TRUST_SELF_HOST="${TRUST_SELF_HOST:-127.0.0.1}"
MEMORY_MB="${MEMORY_MB:-512}"
CPUS="${CPUS:-1}"
CERTS_DIR="${CERTS_DIR:-}"

usage() {
  cat <<'EOF'
Usage: run.Release.sh [HOST_PORT | HOST:INTERNAL] [options]

Start ShortP2P Messenger Server (Release/Production) via Docker Compose.

  HOST_PORT              Publish this host port (internal stays 51111)
  HOST:INTERNAL          Explicit port mapping

  -p, --host-port N      Host (external) port
  -i, --internal-port N  Container listen port (default 51111)
  -m, --memory N         Memory limit in MB (min 512, default 512)
  -c, --cpus N           CPU cores (min 1, default 1)
  --certs-dir PATH       Host TLS certs folder
                         (default: $APPUSER/ShortP2P/MessengerServer/certs)
  --self-host ADDR       Trust:SelfHost (default 127.0.0.1)
  -h, --help             Show this help
EOF
}

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

parse_run_args "$@"

if [ -z "$CERTS_DIR" ]; then
  CERTS_DIR="$(default_certs_dir)"
fi
mkdir -p "$CERTS_DIR"
CERTS_DIR="$(cd "$CERTS_DIR" && pwd)"

if [ ! -f "$env_file" ] && [ -f "$script_dir/.env.example" ]; then
  cp "$script_dir/.env.example" "$env_file"
  echo "Created $env_file from .env.example"
fi

export HOST_PORT INTERNAL_PORT TRUST_SELF_HOST MEMORY_MB CPUS CERTS_DIR

echo "Environment:   Production"
echo "Docker platform: $(effective_docker_platform_label)"
echo "Port mapping:  ${HOST_PORT}:${INTERNAL_PORT} (host:internal)"
echo "Resources:     ${MEMORY_MB} MB RAM, ${CPUS} CPU"
echo "TLS certs:     $CERTS_DIR -> /etc/shortp2p/certs"
echo "Trust:SelfHost $TRUST_SELF_HOST  Trust:SelfPort $HOST_PORT"

compose_args=( -f "$compose_file" )
platform_file="$(docker_platform_compose_file "$script_dir")"
if [ -n "$platform_file" ]; then
  compose_args+=( -f "$platform_file" )
fi
if [ -f "$env_file" ]; then
  compose_args+=( --env-file "$env_file" )
fi

(
  cd "$script_dir"
  HOST_PORT="$HOST_PORT" \
  INTERNAL_PORT="$INTERNAL_PORT" \
  TRUST_SELF_HOST="$TRUST_SELF_HOST" \
  MEMORY_MB="$MEMORY_MB" \
  CPUS="$CPUS" \
  CERTS_DIR="$CERTS_DIR" \
  docker compose "${compose_args[@]}" up --build -d
)

echo "Server: https://localhost:${HOST_PORT}"
