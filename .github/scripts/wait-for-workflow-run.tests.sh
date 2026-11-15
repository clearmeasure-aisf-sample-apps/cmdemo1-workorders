#!/usr/bin/env bash
# Unit tests for wait-for-workflow-run.sh argument parsing (no live gh calls).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT="$ROOT/.github/scripts/wait-for-workflow-run.sh"

fail() { echo "FAIL: $*" >&2; exit 1; }
pass() { echo "PASS: $*"; }

set +e
out="$("$SCRIPT" 2>&1)"
rc=$?
set -e
[[ "$rc" -eq 2 ]] || fail "expected exit 2 for missing args, got $rc"
pass "missing args exit 2"

set +e
env -u GH_TOKEN -u GITHUB_TOKEN "$SCRIPT" \
  --repo o/r --workflow publish-image.yml --sha abcdef0 2>/dev/null
rc=$?
set -e
[[ "$rc" -eq 1 ]] || fail "expected exit 1 without token, got $rc"
pass "missing token exit 1"

# Script documents require-artifact / dispatch-if-missing (regression for skipped publish).
grep -q 'require-artifact' "$SCRIPT" || fail "missing --require-artifact"
grep -q 'dispatch-if-missing' "$SCRIPT" || fail "missing --dispatch-if-missing"
grep -q 'artifact_count' "$SCRIPT" || fail "missing artifact_count helper"
pass "require-artifact and dispatch-if-missing present"

echo "All wait-for-workflow-run tests passed."
