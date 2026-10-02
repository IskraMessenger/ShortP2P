#!/usr/bin/env bash
# ARM64 / Raspberry Pi wrapper for run-persistent.Development.sh.
# Forces Docker platform linux/arm64 (native on Pi; buildx/QEMU elsewhere).
#
# Usage:
#   ./scripts/server/docker/run-arm64-persistent.Development.sh
#   ./scripts/server/docker/run-arm64-persistent.Development.sh 8080 --memory 1024 --cpus 2
#
# Swagger: https://localhost:<HOST_PORT>/swagger
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=_run-common.sh
. "$script_dir/_run-common.sh"

export_docker_arm64_platform

echo "Docker platform: linux/arm64"
exec "$script_dir/run-persistent.Development.sh" "$@"
