#!/usr/bin/env bash
# Generate a PostgreSQL admin password: 8–64 chars, [a-zA-Z0-9], with upper, lower, and digit.
set -euo pipefail

generate_postgres_password() {
  local length="${1:-16}"
  if [ "$length" -lt 8 ]; then length=8; fi
  if [ "$length" -gt 64 ]; then length=64; fi

  local lower upper digit all char password="" i pos
  lower='abcdefghijklmnopqrstuvwxyz'
  upper='ABCDEFGHIJKLMNOPQRSTUVWXYZ'
  digit='0123456789'
  all="${lower}${upper}${digit}"

  rand_byte() {
    if command -v openssl >/dev/null 2>&1; then
      openssl rand -hex 1 | head -c 2
    else
      od -An -N1 -tu1 /dev/urandom | tr -d ' '
    fi
  }

  pick_from() {
    local set="$1"
    local n="${#set}"
    local b
    b="$(rand_byte)"
    b=$((16#${b:-0} % n))
    printf '%s' "${set:$b:1}"
  }

  password="$(pick_from "$lower")$(pick_from "$upper")$(pick_from "$digit")"
  while [ "${#password}" -lt "$length" ]; do
    password+="$(pick_from "$all")"
  done

  if ! [[ "$password" =~ [a-z] ]] || ! [[ "$password" =~ [A-Z] ]] || ! [[ "$password" =~ [0-9] ]]; then
    echo "Password generation failed validation" >&2
    return 1
  fi
  if [ "${#password}" -lt 8 ] || [ "${#password}" -gt 64 ]; then
    echo "Password length out of range" >&2
    return 1
  fi

  printf '%s' "$password"
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  generate_postgres_password "${1:-16}"
fi
