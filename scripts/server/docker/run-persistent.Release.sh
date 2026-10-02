#!/usr/bin/env bash
# Start ShortP2P Messenger Server (Release) with PostgreSQL 11 persistence.
#
# Usage:
#   ./scripts/server/docker/run-persistent.Release.sh
#   ./scripts/server/docker/run-persistent.Release.sh 8080 --memory 1024 --cpus 2
#   ./scripts/server/docker/run-persistent.Release.sh --persistence-dir /data/shortp2p/pg
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=_run-common.sh
. "$script_dir/_run-common.sh"

HOST_PORT="${HOST_PORT:-51111}"
INTERNAL_PORT="${INTERNAL_PORT:-51111}"
TRUST_SELF_HOST="${TRUST_SELF_HOST:-127.0.0.1}"
MEMORY_MB="${MEMORY_MB:-512}"
CPUS="${CPUS:-1}"
PERSISTENCE_DIR="${PERSISTENCE_DIR:-}"
CERTS_DIR="${CERTS_DIR:-}"
POSTGRES_PORT="${POSTGRES_PORT:-5432}"

usage() {
  cat <<'EOF'
Usage: run-persistent.Release.sh [HOST_PORT | HOST:INTERNAL] [options]

Start Release/Production Messenger Server with Persistence (PostgreSQL 11).

  HOST_PORT / HOST:INTERNAL   Port mapping (internal default 51111)
  -p, --host-port N           Host port
  -i, --internal-port N       Container listen port
  -m, --memory N              Memory limit MB (min 512)
  -c, --cpus N                CPU cores (min 1)
  --persistence-dir PATH      Host folder for Postgres data
                              (default: $APPUSER/ShortP2P/MessengerServer/persistence)
  --certs-dir PATH            Host TLS certs folder
                              (default: $APPUSER/ShortP2P/MessengerServer/certs)
  --postgres-port N           Postgres listen/connect port (default 5432; Compose network only)
  --self-host ADDR            Trust:SelfHost
  -h, --help
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

base_compose="$script_dir/docker-compose.yml"
persist_compose="$script_dir/docker-compose.persistent.yml"
env_file="$script_dir/.env.persistent.Release"

parse_run_args "$@"

if [ -z "$PERSISTENCE_DIR" ]; then
  PERSISTENCE_DIR="$(default_persistence_dir persistence)"
fi
if [ -z "$CERTS_DIR" ]; then
  CERTS_DIR="$(default_certs_dir)"
fi

# Resolve to absolute paths for Docker bind mounts.
mkdir -p "$PERSISTENCE_DIR" "$CERTS_DIR"
PERSISTENCE_DIR="$(cd "$PERSISTENCE_DIR" && pwd)"
CERTS_DIR="$(cd "$CERTS_DIR" && pwd)"

if [ ! -f "$env_file" ] && [ -f "$script_dir/.env.persistent.Release.example" ]; then
  cp "$script_dir/.env.persistent.Release.example" "$env_file"
  echo "Created $env_file from .env.persistent.Release.example"
fi

prompt_postgres_admin_credentials "$PERSISTENCE_DIR"

export HOST_PORT INTERNAL_PORT TRUST_SELF_HOST MEMORY_MB CPUS PERSISTENCE_DIR CERTS_DIR POSTGRES_PORT
export POSTGRES_USER
# POSTGRES_PASSWORD may be unset (auto-generate in container)
if [ -n "${POSTGRES_PASSWORD:-}" ]; then
  export POSTGRES_PASSWORD
fi

echo "Environment:   Production (persistent)"
echo "Port mapping:  ${HOST_PORT}:${INTERNAL_PORT} (host:internal)"
echo "Resources:     ${MEMORY_MB} MB RAM, ${CPUS} CPU"
echo "Postgres data: $PERSISTENCE_DIR"
echo "TLS certs:     $CERTS_DIR → /etc/shortp2p/certs"
echo "Postgres port: ${POSTGRES_PORT} (Compose network Host=postgres; not published to host)"
echo "Postgres admin: ${POSTGRES_USER:-shortp2p}"
echo "Trust:SelfHost $TRUST_SELF_HOST  Trust:SelfPort $HOST_PORT"

compose_args=( -f "$base_compose" -f "$persist_compose" )
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
  PERSISTENCE_DIR="$PERSISTENCE_DIR" \
  CERTS_DIR="$CERTS_DIR" \
  POSTGRES_PORT="$POSTGRES_PORT" \
  POSTGRES_USER="${POSTGRES_USER:-shortp2p}" \
  POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-}" \
  docker compose "${compose_args[@]}" up --build -d
)

echo "Server: https://localhost:${HOST_PORT}"
echo "Persistence: PostgreSQL 11 (Persistence:Enabled=true)"
