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
export ASPNETCORE_URLS="https://0.0.0.0:${INTERNAL_PORT}"
export Kestrel__Endpoints__Https__Url="https://0.0.0.0:${INTERNAL_PORT}"
export Kestrel__Endpoints__Https__Certificate__Path="$PFX_PATH"
export Kestrel__Endpoints__Https__Certificate__Password="$CERT_PASSWORD"

# Clients connect to the published (host) port, not the container-internal one.
export Trust__SelfPort="${Trust__SelfPort:-$HOST_PORT}"

export Auth__LiteDb__ConnectionString="${Auth__LiteDb__ConnectionString:-Filename=${DATA_DIR}/messenger-auth.litedb;Connection=shared}"
export HostPowers__LiteDb__ConnectionString="${HostPowers__LiteDb__ConnectionString:-Filename=${DATA_DIR}/messenger-host-powers.litedb;Connection=shared}"
export Trust__LiteDb__ConnectionString="${Trust__LiteDb__ConnectionString:-Filename=${DATA_DIR}/messenger-trust.litedb;Connection=shared}"

echo "ShortP2P Messenger Server starting"
echo "  listen (container): https://0.0.0.0:${INTERNAL_PORT}"
echo "  publish (host map): ${HOST_PORT} -> ${INTERNAL_PORT}"
echo "  Trust:SelfHost=${Trust__SelfHost:-127.0.0.1} Trust:SelfPort=${Trust__SelfPort}"

exec dotnet ShortP2P.MessengerServer.Api.dll "$@"
