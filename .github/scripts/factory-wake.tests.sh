#!/usr/bin/env bash
# Unit checks for factory-wake.sh pure helpers (no GitHub API).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=factory-wake.sh
source "$SCRIPT_DIR/factory-wake.sh"

failed=0

pass() {
  echo "PASS: $*"
}

fail() {
  echo "FAIL: $*" >&2
  failed=1
}

assert_eq() {
  local expected="$1"
  local actual="$2"
  local label="$3"
  if [[ "$expected" == "$actual" ]]; then
    pass "$label"
  else
    fail "$label (expected '$expected', got '$actual')"
  fi
}

assert_true() {
  local label="$1"
  shift
  if "$@"; then
    pass "$label"
  else
    fail "$label (expected true)"
  fi
}

assert_false() {
  local label="$1"
  shift
  if "$@"; then
    fail "$label (expected false)"
  else
    pass "$label"
  fi
}

assert_true "conclusion success" is_eligible_conclusion success
assert_true "conclusion failure" is_eligible_conclusion failure
assert_false "conclusion cancelled" is_eligible_conclusion cancelled
assert_false "conclusion empty" is_eligible_conclusion ""

assert_true "event pull_request" is_eligible_trigger_event pull_request
assert_true "event push" is_eligible_trigger_event push
assert_false "event schedule" is_eligible_trigger_event schedule
assert_false "event workflow_dispatch" is_eligible_trigger_event workflow_dispatch

assert_true "default branch match" is_default_branch master master
assert_false "default branch mismatch" is_default_branch feature/x master
assert_false "default branch empty head" is_default_branch "" master

assert_eq "42" "$(parse_closes_from_body 'Closes #42' | tr -d '\n')" "Closes #N"
assert_eq "7"$'\n'"99" "$(parse_closes_from_body $'Fixes #99\n\nCloses #7')" "multiple closing keywords sorted"
assert_eq "" "$(parse_closes_from_body 'Part of clearmeasure-aisf-sample-apps/cmdemo1-system#585' | tr -d '\n')" "Part of is not Closes"
assert_eq "12" "$(parse_closes_from_body 'This closes #12 eventually' | tr -d '\n')" "closes mid-sentence"

assert_true "verdict Merged" comment_is_review_verdict "Review → Merged"
assert_true "verdict Done with suffix" comment_is_review_verdict "Review → Done"$'\n'"details"
assert_true "verdict In Progress" comment_is_review_verdict "Review → In Progress: continuing"
assert_false "verdict prefix only" comment_is_review_verdict "Review"
assert_false "unrelated comment" comment_is_review_verdict "Todo → In Progress"

now="$(date -u +%s)"
recent="$(date -u -d "@$((now - 60))" --iso-8601=seconds)"
old="$(date -u -d "@$((now - 700))" --iso-8601=seconds)"
assert_true "assigned 1m ago within 10m" assigned_within_dedupe_window "$recent" "$now" 10
assert_false "assigned 700s ago outside 10m" assigned_within_dedupe_window "$old" "$now" 10

head_date="$(date -u -d "@$((now - 3600))" --iso-8601=seconds)"
after="$(date -u -d "@$((now - 1800))" --iso-8601=seconds)"
before="$(date -u -d "@$((now - 7200))" --iso-8601=seconds)"

comments_after="$(jq -n --arg b "Review → Done" --arg t "$after" '[{body:$b, created_at:$t}]')"
comments_before="$(jq -n --arg b "Review → Merged" --arg t "$before" '[{body:$b, created_at:$t}]')"
comments_other="$(jq -n --arg b "status update" --arg t "$after" '[{body:$b, created_at:$t}]')"

assert_true "verdict after head" has_review_verdict_after "$comments_after" "$head_date"
assert_false "verdict before head" has_review_verdict_after "$comments_before" "$head_date"
assert_false "non-verdict after head" has_review_verdict_after "$comments_other" "$head_date"

if [[ "$failed" -ne 0 ]]; then
  echo "factory-wake tests failed" >&2
  exit 1
fi
echo "All factory-wake tests passed."
