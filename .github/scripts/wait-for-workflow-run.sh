#!/usr/bin/env bash
# Wait for a workflow run on a given commit to finish successfully.
#
# Usage:
#   wait-for-workflow-run.sh --repo OWNER/NAME --workflow FILE.yml --sha SHA [--timeout-seconds N]
#
# Writes:
#   run_id=<id>
#   conclusion=<conclusion>
# to stdout and to GITHUB_OUTPUT when set.
# Does not print tokens. Requires `gh`.

set -euo pipefail

usage() {
  echo "usage: $0 --repo OWNER/NAME --workflow FILE.yml --sha SHA [--timeout-seconds N] [--poll-seconds N]" >&2
  exit 2
}

repo="" workflow="" sha="" timeout_seconds=900 poll_seconds=15
while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo) repo="${2:-}"; shift 2 ;;
    --workflow) workflow="${2:-}"; shift 2 ;;
    --sha) sha="${2:-}"; shift 2 ;;
    --timeout-seconds) timeout_seconds="${2:-}"; shift 2 ;;
    --poll-seconds) poll_seconds="${2:-}"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -n "$repo" && -n "$workflow" && -n "$sha" ]] || usage

if [[ -z "${GH_TOKEN:-${GITHUB_TOKEN:-}}" ]]; then
  echo "GH_TOKEN or GITHUB_TOKEN is required" >&2
  exit 1
fi

sha_lower="$(printf '%s' "$sha" | tr '[:upper:]' '[:lower:]')"
deadline=$((SECONDS + timeout_seconds))
run_id=""
conclusion=""

write_outputs() {
  if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
    {
      printf 'run_id=%s\n' "$run_id"
      printf 'conclusion=%s\n' "$conclusion"
    } >> "$GITHUB_OUTPUT"
  fi
  printf 'run_id=%s\n' "$run_id"
  printf 'conclusion=%s\n' "$conclusion"
}

find_run() {
  # Newest run for this workflow + head SHA (push and workflow_dispatch share the commit).
  gh api "repos/${repo}/actions/workflows/${workflow}/runs?head_sha=${sha_lower}&per_page=20" \
    --jq '[.workflow_runs[] | select(.event == "push" or .event == "workflow_dispatch")] | sort_by(.created_at) | reverse | .[0] // empty | {id, status, conclusion}'
}

while (( SECONDS < deadline )); do
  json="$(find_run || true)"
  if [[ -n "$json" && "$json" != "null" ]]; then
    run_id="$(printf '%s' "$json" | jq -r '.id // empty')"
    status="$(printf '%s' "$json" | jq -r '.status // empty')"
    conclusion="$(printf '%s' "$json" | jq -r '.conclusion // empty')"
    if [[ -n "$run_id" && "$status" == "completed" ]]; then
      write_outputs
      if [[ "$conclusion" != "success" ]]; then
        echo "Workflow ${workflow} for ${sha_lower} concluded ${conclusion} (run ${run_id})" >&2
        exit 1
      fi
      exit 0
    fi
    echo "Waiting for ${workflow} run ${run_id:-?} status=${status:-pending}..."
  else
    echo "Waiting for ${workflow} run for ${sha_lower} to appear..."
  fi
  sleep "$poll_seconds"
done

echo "Timed out after ${timeout_seconds}s waiting for ${workflow} on ${sha_lower}" >&2
exit 1
