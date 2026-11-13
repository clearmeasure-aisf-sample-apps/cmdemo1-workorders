#!/usr/bin/env bash
# Checks for pr-publish-ids.sh (no network).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IDS="$SCRIPT_DIR/pr-publish-ids.sh"
failed=0

assert_ids() {
  local label="$1"
  local expected_version="$2"
  local expected_tag="$3"
  shift 3
  local out version tag
  out="$("$IDS" "$@")"
  version="$(awk -F= '/^version=/{print $2; exit}' <<<"$out")"
  tag="$(awk -F= '/^image_tag=/{print $2; exit}' <<<"$out")"
  if [[ "$version" != "$expected_version" || "$tag" != "$expected_tag" ]]; then
    echo "FAIL: $label (got version=$version image_tag=$tag)" >&2
    failed=1
  else
    echo "PASS: $label"
  fi
}

assert_fail() {
  local label="$1"
  shift
  if "$IDS" "$@" >/dev/null 2>&1; then
    echo "FAIL: $label (expected non-zero exit)" >&2
    failed=1
  else
    echo "PASS: $label (rejected)"
  fi
}

assert_ids "full sha" "2.4.17-pr.1660" "pr-1660-abcdef0123456789abcdef0123456789abcdef01" \
  --major 2 --minor 4 --run 17 --pr 1660 --sha abcdef0123456789abcdef0123456789abcdef01

assert_ids "short sha lowercased" "2.4.1-pr.9" "pr-9-deadbee" \
  --major 2 --minor 4 --run 1 --pr 9 --sha DeadBee

assert_fail "missing pr" --major 2 --minor 4 --run 1 --sha abcdef0
assert_fail "non-numeric run" --major 2 --minor 4 --run x --pr 1 --sha abcdef0
assert_fail "bad sha" --major 2 --minor 4 --run 1 --pr 1 --sha zz

if [[ "$failed" -ne 0 ]]; then
  echo "pr-publish-ids tests failed" >&2
  exit 1
fi
echo "All pr-publish-ids tests passed."
