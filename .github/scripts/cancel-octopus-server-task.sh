#!/usr/bin/env bash
# Cancel one Octopus Deploy server task by id (superseded PR environment deploy).
#
# Usage:
#   cancel-octopus-server-task.sh --task-id ServerTasks-123
#
# Env:
#   OCTOPUS_URL, OCTOPUS_ACCESS_TOKEN (from OctopusDeploy/login), OCTOPUS_SPACE (name)
# Does not print tokens. Empty --task-id is a no-op success (nothing to cancel).

set -euo pipefail

usage() {
  echo "usage: $0 --task-id ServerTasks-N" >&2
  exit 2
}

task_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --task-id) task_id="${2:-}"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -n "${1:-}" ]] && usage
# Allow empty task id (caller had no deploy yet).
if [[ -z "$task_id" || "$task_id" == "null" ]]; then
  echo "No Octopus server task id; nothing to cancel."
  exit 0
fi

: "${OCTOPUS_URL:?OCTOPUS_URL is required}"
: "${OCTOPUS_ACCESS_TOKEN:?OCTOPUS_ACCESS_TOKEN is required}"
: "${OCTOPUS_SPACE:?OCTOPUS_SPACE is required}"

base="${OCTOPUS_URL%/}"
auth_header="Authorization: Bearer ${OCTOPUS_ACCESS_TOKEN}"

space_id="$(
  curl -fsS --header "$auth_header" \
    "${base}/api/spaces?partialName=$(jq -rn --arg s "$OCTOPUS_SPACE" '$s|@uri')&take=100" \
    | jq -r --arg s "$OCTOPUS_SPACE" '[.Items[] | select(.Name == $s)][0].Id // empty'
)"
[[ -n "$space_id" ]] || { echo "Octopus space '$OCTOPUS_SPACE' not found" >&2; exit 1; }

# POST .../tasks/{id}/cancel — idempotent enough for already-completed tasks (non-2xx logged, not fatal when already done).
http_code="$(
  curl -sS -o /tmp/octopus-cancel-task.json -w '%{http_code}' \
    --header "$auth_header" \
    --header 'Content-Type: application/json' \
    -X POST \
    "${base}/api/${space_id}/tasks/${task_id}/cancel" \
    -d '{}' || true
)"

if [[ "$http_code" =~ ^2 ]]; then
  echo "Cancelled Octopus task ${task_id}"
  exit 0
fi

# 404 / 405 / conflict when already finished: treat as released.
echo "Octopus cancel task ${task_id} returned HTTP ${http_code} (treating as already finished if not 5xx)"
if [[ "$http_code" =~ ^5 ]]; then
  exit 1
fi
exit 0
