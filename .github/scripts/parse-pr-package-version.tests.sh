#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SCRIPT="$ROOT/.github/scripts/parse-pr-package-version.sh"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

fail() { echo "FAIL: $*" >&2; exit 1; }
pass() { echo "PASS: $*"; }

mkdir -p "$TMP/pkgs"
touch "$TMP/pkgs/ChurchBulletin.Database.2.4.99-pr.7.nupkg"
touch "$TMP/pkgs/ChurchBulletin.UI.2.4.99-pr.7.nupkg"

out="$("$SCRIPT" --packages-dir "$TMP/pkgs")"
[[ "$out" == "version=2.4.99-pr.7" ]] || fail "got '$out'"
pass "parses Database package version"

rm -f "$TMP/pkgs/ChurchBulletin.Database.2.4.99-pr.7.nupkg"
out="$("$SCRIPT" --packages-dir "$TMP/pkgs")"
[[ "$out" == "version=2.4.99-pr.7" ]] || fail "UI fallback got '$out'"
pass "falls back to UI package"

set +e
"$SCRIPT" --packages-dir "$TMP/empty" >/dev/null 2>&1
rc=$?
set -e
[[ "$rc" -ne 0 ]] || fail "expected failure for missing dir"
pass "missing dir fails"

echo "All parse-pr-package-version tests passed."
