#!/usr/bin/env bash
# ARM64 / Raspberry Pi wrapper for run-persistent.Release.sh.
# Forces Docker platform linux/arm64 (native on Pi; buildx/QEMU elsewhere).
#
# Usage:
#   ./scripts/server/docker/run-arm64-persistent.Release.sh
#   ./scripts/server/docker/run-arm64-persistent.Release.sh 8080 --persistence-dir /data/shortp2p/pg
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=_run-common.sh
. "$script_dir/_run-common.sh"

export_docker_arm64_platform

echo "Docker platform: linux/arm64"
exec "$script_dir/run-persistent.Release.sh" "$@"
