#!/usr/bin/env bash
# Wait for a workflow run on a given commit to finish successfully.
#
# Usage:
#   wait-for-workflow-run.sh --repo OWNER/NAME --workflow FILE.yml --sha SHA
#     [--timeout-seconds N] [--poll-seconds N]
#     [--require-artifact NAME] [--ref BRANCH] [--dispatch-if-missing]
#
# When --require-artifact is set, a successful run without that artifact is ignored
# (Publish image may conclude success after skipping because no open PR existed yet).
# With --dispatch-if-missing and --ref, dispatches the workflow once on that ref.
#
# Writes run_id= and conclusion= to stdout and GITHUB_OUTPUT when set.
# Does not print tokens. Requires `gh`.

set -euo pipefail

usage() {
  echo "usage: $0 --repo OWNER/NAME --workflow FILE.yml --sha SHA [--timeout-seconds N] [--poll-seconds N] [--require-artifact NAME] [--ref BRANCH] [--dispatch-if-missing]" >&2
  exit 2
}

repo="" workflow="" sha="" timeout_seconds=900 poll_seconds=15
require_artifact="" ref="" dispatch_if_missing=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --repo) repo="${2:-}"; shift 2 ;;
    --workflow) workflow="${2:-}"; shift 2 ;;
    --sha) sha="${2:-}"; shift 2 ;;
    --timeout-seconds) timeout_seconds="${2:-}"; shift 2 ;;
    --poll-seconds) poll_seconds="${2:-}"; shift 2 ;;
    --require-artifact) require_artifact="${2:-}"; shift 2 ;;
    --ref) ref="${2:-}"; shift 2 ;;
    --dispatch-if-missing) dispatch_if_missing=true; shift ;;
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
dispatched=false
seen_skip=false

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

find_runs() {
  gh api "repos/${repo}/actions/workflows/${workflow}/runs?head_sha=${sha_lower}&per_page=20" \
    --jq '[.workflow_runs[] | select(.event == "push" or .event == "workflow_dispatch")] | sort_by(.created_at) | reverse'
}

artifact_count() {
  local id="$1"
  local name="$2"
  gh api "repos/${repo}/actions/runs/${id}/artifacts?per_page=100" \
    --jq --arg n "$name" '[.artifacts[] | select(.name == $n)] | length'
}

maybe_dispatch() {
  if [[ "$dispatch_if_missing" != "true" || "$dispatched" == "true" ]]; then
    return 0
  fi
  if [[ -z "$ref" ]]; then
    echo "--dispatch-if-missing requires --ref" >&2
    return 1
  fi
  echo "Dispatching ${workflow} on ref ${ref} (no publish artifact for ${sha_lower} yet)..."
  gh workflow run "$workflow" --repo "$repo" --ref "$ref"
  dispatched=true
}

while (( SECONDS < deadline )); do
  runs_json="$(find_runs || echo '[]')"
  if [[ -z "$runs_json" || "$runs_json" == "null" ]]; then
    runs_json='[]'
  fi

  count="$(printf '%s' "$runs_json" | jq 'length')"
  if [[ "$count" -eq 0 ]]; then
    echo "Waiting for ${workflow} run for ${sha_lower} to appear..."
    maybe_dispatch || true
    sleep "$poll_seconds"
    continue
  fi

  # Prefer the newest completed success that has the required artifact (when set).
  accepted=""
  while IFS= read -r row; do
    [[ -n "$row" ]] || continue
    rid="$(printf '%s' "$row" | jq -r '.id')"
    status="$(printf '%s' "$row" | jq -r '.status')"
    concl="$(printf '%s' "$row" | jq -r '.conclusion // empty')"
    if [[ "$status" != "completed" ]]; then
      echo "Waiting for ${workflow} run ${rid} status=${status}..."
      accepted="pending"
      break
    fi
    if [[ "$concl" != "success" ]]; then
      echo "Workflow ${workflow} run ${rid} concluded ${concl}" >&2
      run_id="$rid"
      conclusion="$concl"
      write_outputs
      exit 1
    fi
    if [[ -n "$require_artifact" ]]; then
      ac="$(artifact_count "$rid" "$require_artifact")"
      if [[ "$ac" -lt 1 ]]; then
        echo "Run ${rid} succeeded but artifact '${require_artifact}' is missing (publish likely skipped)."
        seen_skip=true
        continue
      fi
    fi
    run_id="$rid"
    conclusion="$concl"
    write_outputs
    exit 0
  done < <(printf '%s' "$runs_json" | jq -c '.[]')

  if [[ "$accepted" == "pending" ]]; then
    sleep "$poll_seconds"
    continue
  fi

  if [[ "$seen_skip" == "true" ]]; then
    maybe_dispatch || true
  fi
  echo "Waiting for a ${workflow} run of ${sha_lower} that published artifacts..."
  sleep "$poll_seconds"
done

echo "Timed out after ${timeout_seconds}s waiting for ${workflow} on ${sha_lower}" >&2
exit 1
