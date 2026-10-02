#!/usr/bin/env bash
# Wraps the official postgres image entrypoint: admin user (default shortp2p) and password
# are created on first start; credentials live only in the container secrets volume.
# Listens on POSTGRES_PORT (default 5432) — not published to the host.
set -euo pipefail

SECRETS_DIR="${SHORTP2P_PG_SECRETS_DIR:-/var/lib/shortp2p-secrets}"
CRED_FILE="$SECRETS_DIR/credentials.env"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PG_PORT="${POSTGRES_PORT:-5432}"

mkdir -p "$SECRETS_DIR"
chmod 700 "$SECRETS_DIR" 2>/dev/null || true

if [ -f "$CRED_FILE" ]; then
  set -a
  # shellcheck disable=SC1090
  . "$CRED_FILE"
  set +a
else
  export POSTGRES_USER="${POSTGRES_USER:-shortp2p}"
  if [ -z "${POSTGRES_PASSWORD:-}" ]; then
    # shellcheck source=postgres-generate-password.sh
    . "$SCRIPT_DIR/postgres-generate-password.sh"
    POSTGRES_PASSWORD="$(generate_postgres_password 16)"
    export POSTGRES_PASSWORD
  fi
  umask 077
  cat > "$CRED_FILE" <<EOF
# PostgreSQL admin — created on first container start. Do not copy to the host.
POSTGRES_USER=${POSTGRES_USER}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
EOF
  chmod 600 "$CRED_FILE" 2>/dev/null || true
  echo "PostgreSQL admin user: ${POSTGRES_USER} (password stored in container volume only)"
fi

export POSTGRES_USER="${POSTGRES_USER:-shortp2p}"
export POSTGRES_PASSWORD
export POSTGRES_PORT="$PG_PORT"
export PGPORT="$PG_PORT"

echo "PostgreSQL listen port: ${PG_PORT} (Compose network only)"

# Official image already uses listen_addresses='*'; override port for non-default values.
exec /usr/local/bin/docker-entrypoint.sh postgres -c "port=${PG_PORT}" "$@"
