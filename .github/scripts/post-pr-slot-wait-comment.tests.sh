#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT="$ROOT/.github/scripts/post-pr-slot-wait-comment.sh"

fail() { echo "FAIL: $*" >&2; exit 1; }
pass() { echo "PASS: $*"; }

set +e
"$SCRIPT" 2>/dev/null
rc=$?
set -e
[[ "$rc" -eq 2 ]] || fail "expected usage exit 2, got $rc"
pass "usage exit 2"

set +e
env -u GH_TOKEN -u GITHUB_TOKEN "$SCRIPT" --repo o/r --pr 1 2>/dev/null
rc=$?
set -e
[[ "$rc" -eq 1 ]] || fail "expected exit 1 without token, got $rc"
pass "missing token exit 1"

echo "All post-pr-slot-wait-comment tests passed."
