#!/usr/bin/env bash
# Checks for resolve-open-pr.sh (no network / no gh).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESOLVE="$SCRIPT_DIR/resolve-open-pr.sh"
failed=0

assert_pr() {
  local expected="$1"
  local label="$2"
  local json="$3"
  local actual
  actual="$("$RESOLVE" --from-json - <<<"$json" | awk -F= '/^pr_number=/{print $2; exit}')"
  if [[ "$actual" != "$expected" ]]; then
    echo "FAIL: $label (expected pr_number=$expected, got pr_number=$actual)" >&2
    failed=1
  else
    echo "PASS: $label (pr_number=$expected)"
  fi
}

assert_pr "12" "single open PR" '[{"number":12}]'
assert_pr "7" "lowest of several" '[{"number":42},{"number":7},{"number":99}]'
assert_pr "" "empty list" '[]'
assert_pr "" "null payload" 'null'
assert_pr "" "non-array object" '{"number":3}'
assert_pr "3" "ignores non-numeric number" '[{"number":"x"},{"number":3}]'

if [[ "$failed" -ne 0 ]]; then
  echo "resolve-open-pr tests failed" >&2
  exit 1
fi
echo "All resolve-open-pr tests passed."
