#!/usr/bin/env bash
# Cancel in-progress Octopus deployments for prior PR releases of this pull request.
# Called at the start of a new "PR environment" run so a newer push supersedes the old deploy.
#
# Usage:
#   cancel-superseded-octopus-tasks.sh --project SLUG-ui --pr N [--keep-release VERSION]
#
# Env: OCTOPUS_URL, OCTOPUS_ACCESS_TOKEN, OCTOPUS_SPACE
# Release numbers match 2.4.<run>-pr.<n>. Does not print tokens.

set -euo pipefail

usage() {
  echo "usage: $0 --project NAME --pr N [--keep-release VERSION]" >&2
  exit 2
}

project="" pr="" keep_release=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --project) project="${2:-}"; shift 2 ;;
    --pr) pr="${2:-}"; shift 2 ;;
    --keep-release) keep_release="${2:-}"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -n "$project" && -n "$pr" ]] || usage
[[ "$pr" =~ ^[0-9]+$ ]] || { echo "pr must be a non-negative integer" >&2; exit 1; }

: "${OCTOPUS_URL:?OCTOPUS_URL is required}"
: "${OCTOPUS_ACCESS_TOKEN:?OCTOPUS_ACCESS_TOKEN is required}"
: "${OCTOPUS_SPACE:?OCTOPUS_SPACE is required}"

base="${OCTOPUS_URL%/}"
auth_header="Authorization: Bearer ${OCTOPUS_ACCESS_TOKEN}"
suffix="-pr.${pr}"

space_id="$(
  curl -fsS --header "$auth_header" \
    "${base}/api/spaces?partialName=$(jq -rn --arg s "$OCTOPUS_SPACE" '$s|@uri')&take=100" \
    | jq -r --arg s "$OCTOPUS_SPACE" '[.Items[] | select(.Name == $s)][0].Id // empty'
)" || space_id=""
if [[ -z "$space_id" ]]; then
  echo "Octopus space '$OCTOPUS_SPACE' not found; skip supersede cancel."
  exit 0
fi

project_id="$(
  curl -fsS --header "$auth_header" \
    "${base}/api/${space_id}/projects?partialName=$(jq -rn --arg s "$project" '$s|@uri')&take=100" \
    | jq -r --arg s "$project" '[.Items[] | select(.Name == $s)][0].Id // empty'
)" || project_id=""
if [[ -z "$project_id" ]]; then
  echo "Octopus project '$project' not found; skip supersede cancel."
  exit 0
fi

tasks_json="$(
  curl -fsS --header "$auth_header" \
    "${base}/api/${space_id}/tasks?project=${project_id}&states=Executing&states=Queued&states=Cancelling&take=100" \
    || echo '{"Items":[]}'
)"

mapfile -t task_ids < <(
  printf '%s' "$tasks_json" | jq -r --arg suffix "$suffix" --arg keep "$keep_release" '
    (.Items // [])
    | map(select(
        ((.Description // "") | contains($suffix))
        or ((.Name // "") | contains($suffix))
      ))
    | .[]
    | select(
        ($keep == "")
        or (
          ((.Description // "") | contains($keep) | not)
          and ((.Name // "") | contains($keep) | not)
        )
      )
    | .Id
  '
)

if [[ ${#task_ids[@]} -eq 0 ]]; then
  echo "No in-progress Octopus tasks for PR ${pr}."
  exit 0
fi

script_dir="$(cd "$(dirname "$0")" && pwd)"
for id in "${task_ids[@]}"; do
  [[ -n "$id" ]] || continue
  echo "Cancelling superseded Octopus task ${id} for PR ${pr}"
  OCTOPUS_URL="$OCTOPUS_URL" OCTOPUS_ACCESS_TOKEN="$OCTOPUS_ACCESS_TOKEN" OCTOPUS_SPACE="$OCTOPUS_SPACE" \
    bash "${script_dir}/cancel-octopus-server-task.sh" --task-id "$id" || true
done
