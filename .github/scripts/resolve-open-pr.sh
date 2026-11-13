#!/usr/bin/env bash
# Resolve the open pull request number for a branch head.
#
# Usage:
#   resolve-open-pr.sh --repo OWNER/NAME --head BRANCH
#   resolve-open-pr.sh --from-json FILE   # test helper; FILE is gh pr list JSON
#
# Writes pr_number=<n> (or empty) to stdout and to GITHUB_OUTPUT when set.
# Does not print tokens. Requires `gh` when using --repo/--head.

set -euo pipefail

write_pr_output() {
  local pr_number="$1"
  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    printf 'pr_number=%s\n' "$pr_number" >> "$GITHUB_OUTPUT"
  fi
  printf 'pr_number=%s\n' "$pr_number"
}

# Pick the lowest open PR number for the head branch (stable when duplicates exist).
pick_pr_number_from_json() {
  local json="$1"
  if [[ -z "$json" || "$json" == "null" ]]; then
    printf ''
    return 0
  fi
  printf '%s' "$json" | jq -r '
    if type != "array" then
      empty
    else
      [ .[] | select((.number | type) == "number") | .number ] | sort | .[0] // empty
    end
  '
}

usage() {
  echo "usage: $0 --repo OWNER/NAME --head BRANCH | --from-json FILE" >&2
  exit 2
}

mode="${1:-}"
case "$mode" in
  --from-json)
    file="${2:-}"
    [[ -n "$file" ]] || usage
    if [[ "$file" == "-" ]]; then
      json="$(cat)"
    else
      json="$(cat "$file")"
    fi
    write_pr_output "$(pick_pr_number_from_json "$json")"
    ;;
  --repo)
    repo="${2:-}"
    head_flag="${3:-}"
    head="${4:-}"
    [[ -n "$repo" && "$head_flag" == "--head" && -n "$head" ]] || usage
    if [[ -z "${GH_TOKEN:-${GITHUB_TOKEN:-}}" ]]; then
      echo "GH_TOKEN or GITHUB_TOKEN is required for --repo/--head" >&2
      exit 1
    fi
    # gh pr list --head matches the branch name on the head repo (same-repo PRs included).
    json="$(
      gh pr list --repo "$repo" --head "$head" --state open --json number
    )"
    write_pr_output "$(pick_pr_number_from_json "$json")"
    ;;
  *)
    usage
    ;;
esac
