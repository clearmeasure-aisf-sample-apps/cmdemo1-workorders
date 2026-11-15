#!/usr/bin/env bash
# Post a one-time PR comment when waiting for a pool slot exceeds the notify threshold.
#
# Usage:
#   post-pr-slot-wait-comment.sh --repo OWNER/NAME --pr N
#
# Idempotent: skips when an open comment from this marker already exists.
# Env: GH_TOKEN. Does not print tokens.

set -euo pipefail

usage() {
  echo "usage: $0 --repo OWNER/NAME --pr N" >&2
  exit 2
}

repo="" pr=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo) repo="${2:-}"; shift 2 ;;
    --pr) pr="${2:-}"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -n "$repo" && -n "$pr" ]] || usage

MARKER='<!-- pr-environment-slot-wait -->'
body="${MARKER}
Waiting for a pool slot for more than 5 minutes. The **PR environment** check is queued until a warm slot is free (2 warm slots in shadow mode)."

if [[ -z "${GH_TOKEN:-${GITHUB_TOKEN:-}}" ]]; then
  echo "GH_TOKEN or GITHUB_TOKEN is required" >&2
  exit 1
fi

existing="$(
  gh api "repos/${repo}/issues/${pr}/comments" --paginate \
    --jq --arg m "$MARKER" '[.[] | select(.body | contains($m))] | length'
)"
if [[ "$existing" != "0" ]]; then
  echo "Slot-wait comment already present on PR ${pr}."
  exit 0
fi

gh api "repos/${repo}/issues/${pr}/comments" -f body="$body" >/dev/null
echo "Posted slot-wait comment on PR ${pr}."
