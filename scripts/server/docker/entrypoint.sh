#!/usr/bin/env bash
# Container entrypoint for ShortP2P.MessengerServer.Api.
# Ensures data dirs, TLS cert, signing key, and port/env wiring before start.
set -euo pipefail

DATA_DIR="${SHORTP2P_DATA_DIR:-/var/lib/shortp2p/data}"
CERT_DIR="${SHORTP2P_CERT_DIR:-/etc/shortp2p/certs}"
INTERNAL_PORT="${INTERNAL_PORT:-51111}"
HOST_PORT="${HOST_PORT:-$INTERNAL_PORT}"
CERT_PASSWORD="${SHORTP2P_CERT_PASSWORD:-changeit}"
PFX_PATH="${SHORTP2P_CERT_PATH:-$CERT_DIR/server.pfx}"

mkdir -p "$DATA_DIR" "$CERT_DIR"

# Persist a random Auth:SigningKey across restarts unless provided via env.
if [ -z "${Auth__SigningKey:-}" ]; then
  key_file="$DATA_DIR/.signing-key"
  if [ -f "$key_file" ]; then
    Auth__SigningKey="$(tr -d '[:space:]' < "$key_file")"
  else
    Auth__SigningKey="$(openssl rand -base64 48 | tr -d '\n')"
    printf '%s\n' "$Auth__SigningKey" > "$key_file"
    chmod 600 "$key_file"
    echo "Generated Auth:SigningKey and saved to $key_file"
  fi
  export Auth__SigningKey
fi

# Generate a self-signed PFX for quick start if the admin has not mounted one.
if [ ! -f "$PFX_PATH" ]; then
  echo "No TLS certificate at $PFX_PATH — generating a self-signed cert (dev/quick-start only)."
  tmp_dir="$(mktemp -d)"
  openssl req -x509 -newkey rsa:2048 \
    -keyout "$tmp_dir/key.pem" \
    -out "$tmp_dir/cert.pem" \
    -days 825 \
    -nodes \
    -subj "/CN=${Trust__SelfHost:-localhost}" \
    >/dev/null 2>&1
  openssl pkcs12 -export \
    -out "$PFX_PATH" \
    -inkey "$tmp_dir/key.pem" \
    -in "$tmp_dir/cert.pem" \
    -passout "pass:$CERT_PASSWORD" \
    >/dev/null 2>&1
  rm -rf "$tmp_dir"
  chmod 600 "$PFX_PATH"
fi

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
echo "  ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}"
export ASPNETCORE_URLS="https://0.0.0.0:${INTERNAL_PORT}"
export Kestrel__Endpoints__Https__Url="https://0.0.0.0:${INTERNAL_PORT}"
export Kestrel__Endpoints__Https__Certificate__Path="$PFX_PATH"
export Kestrel__Endpoints__Https__Certificate__Password="$CERT_PASSWORD"

# Clients connect to the published (host) port, not the container-internal one.
export Trust__SelfPort="${Trust__SelfPort:-$HOST_PORT}"

export Auth__LiteDb__ConnectionString="${Auth__LiteDb__ConnectionString:-Filename=${DATA_DIR}/messenger-auth.litedb;Connection=shared}"
export HostPowers__LiteDb__ConnectionString="${HostPowers__LiteDb__ConnectionString:-Filename=${DATA_DIR}/messenger-host-powers.litedb;Connection=shared}"
export Trust__LiteDb__ConnectionString="${Trust__LiteDb__ConnectionString:-Filename=${DATA_DIR}/messenger-trust.litedb;Connection=shared}"

# Persistent stack: override appsettings Persistence:* via ASP.NET env vars.
# Credentials come only from the shared secrets volume (never from appsettings).
# Compose private network: Host=postgres (service DNS), Port=POSTGRES_PORT.
PG_SECRETS="${SHORTP2P_PG_SECRETS_DIR:-}"
if [ -n "$PG_SECRETS" ]; then
  cred_file="$PG_SECRETS/credentials.env"
  # Safety wait: normally depends_on healthy already ordered postgres first.
  wait_secs="${SHORTP2P_PG_SECRETS_WAIT_SECS:-60}"
  waited=0
  while [ ! -f "$cred_file" ]; do
    if [ "$waited" -ge "$wait_secs" ]; then
      echo "ERROR: Postgres credentials not found at $cred_file after ${wait_secs}s." >&2
      echo "ERROR: Persistence cannot use appsettings defaults (localhost/postgres)." >&2
      exit 1
    fi
    if [ "$waited" -eq 0 ]; then
      echo "Waiting for Postgres credentials at $cred_file ..."
    fi
    sleep 1
    waited=$((waited + 1))
  done

  # shellcheck disable=SC1090
  set -a
  # Clear any empty compose placeholders so sourced values always win.
  unset POSTGRES_USER POSTGRES_PASSWORD 2>/dev/null || true
  . "$cred_file"
  set +a

  if [ -z "${POSTGRES_USER:-}" ] || [ -z "${POSTGRES_PASSWORD:-}" ]; then
    echo "ERROR: $cred_file must define non-empty POSTGRES_USER and POSTGRES_PASSWORD." >&2
    exit 1
  fi

  # Compose DNS default: postgres. Override POSTGRES_HOST only if needed.
  pg_host="${POSTGRES_HOST:-postgres}"
  pg_port="${POSTGRES_PORT:-5432}"
  pg_db="${POSTGRES_DB:-shortp2p_messenger}"

  # Wait until Postgres accepts TCP on the configured port.
  # Prefer 127.0.0.1 for /dev/tcp when host is localhost (avoid IPv6-only resolve).
  tcp_host="$pg_host"
  if [ "$tcp_host" = "localhost" ]; then
    tcp_host="127.0.0.1"
  fi
  ready_secs="${SHORTP2P_PG_READY_WAIT_SECS:-90}"
  ready_waited=0
  while ! (echo >/dev/tcp/"${tcp_host}"/"${pg_port}") 2>/dev/null; do
    if [ "$ready_waited" -ge "$ready_secs" ]; then
      echo "ERROR: Postgres not reachable at ${pg_host}:${pg_port} after ${ready_secs}s." >&2
      exit 1
    fi
    if [ "$ready_waited" -eq 0 ]; then
      echo "Waiting for Postgres at ${pg_host}:${pg_port} ..."
    fi
    sleep 1
    ready_waited=$((ready_waited + 1))
  done

  export Persistence__Enabled="${Persistence__Enabled:-true}"
  export Persistence__ApplyMigrationsOnStartup="${Persistence__ApplyMigrationsOnStartup:-true}"
  # Always overwrite — do not fall back to appsettings localhost/postgres/12345678.
  export Persistence__ConnectionString="Host=${pg_host};Port=${pg_port};Database=${pg_db};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
  echo "  Persistence: Enabled=${Persistence__Enabled} Host=${pg_host} Port=${pg_port} Database=${pg_db} Username=${POSTGRES_USER}"
fi

echo "ShortP2P Messenger Server starting"
echo "  listen (container): https://0.0.0.0:${INTERNAL_PORT}"
echo "  publish (host map): ${HOST_PORT} -> ${INTERNAL_PORT}"
echo "  Trust:SelfHost=${Trust__SelfHost:-127.0.0.1} Trust:SelfPort=${Trust__SelfPort}"

# No extra args: ASP.NET Core host takes configuration from env / appsettings only.
exec dotnet ShortP2P.MessengerServer.Api.dll
