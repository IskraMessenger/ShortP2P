#!/usr/bin/env bash
# ARM64 / Raspberry Pi wrapper for run.Development.sh.
# Forces Docker platform linux/arm64 (native on Pi; buildx/QEMU elsewhere).
#
# Usage:
#   ./scripts/server/docker/run-arm64.Development.sh
#   ./scripts/server/docker/run-arm64.Development.sh 8080 --memory 1024 --cpus 2
#
# Swagger: https://localhost:<HOST_PORT>/swagger
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=_run-common.sh
. "$script_dir/_run-common.sh"

require_arm64_capable_host
export_docker_platform linux/arm64

echo "Docker platform: linux/arm64"
exec "$script_dir/run.Development.sh" "$@"
