#!/usr/bin/env bash
# Wake cm-ai-ops-bot after Build finishes on a bot pull request by toggling
# the bot assignee on each linked issue (fires the existing issue-assigned
# listener). No comments, no secrets, GITHUB_TOKEN only.
#
# Env (workflow_run):
#   GH_TOKEN, REPO (owner/name), DEFAULT_BRANCH
#   WR_EVENT, WR_CONCLUSION, WR_HEAD_BRANCH, WR_HEAD_SHA
# Optional: BOT_LOGIN (default cm-ai-ops-bot), DEDUPE_MINUTES (default 10)
#
# Pure helpers below are sourced by factory-wake.tests.sh.

set -euo pipefail

BOT_LOGIN="${BOT_LOGIN:-cm-ai-ops-bot}"
DEDUPE_MINUTES="${DEDUPE_MINUTES:-10}"

log() {
  printf '%s\n' "$*"
}

is_eligible_conclusion() {
  case "${1:-}" in
    success|failure) return 0 ;;
    *) return 1 ;;
  esac
}

is_default_branch() {
  local head="${1:-}"
  local default="${2:-}"
  [[ -n "$head" && -n "$default" && "$head" == "$default" ]]
}

# Build in this repo runs on push (not pull_request). Accept push or
# pull_request so a finished Build on a PR branch can wake the bot.
is_eligible_trigger_event() {
  case "${1:-}" in
    pull_request|push) return 0 ;;
    *) return 1 ;;
  esac
}

# Print unique issue numbers referenced by Closes/Close/Closed/Fixes/Fix/Fixed/Resolves/Resolve/Resolved #N.
parse_closes_from_body() {
  local body="${1:-}"
  local matches
  matches="$(printf '%s' "$body" | grep -oiE '(closes|close|closed|fixes|fix|fixed|resolves|resolve|resolved)[[:space:]]+#[[:digit:]]+' || true)"
  if [[ -z "$matches" ]]; then
    return 0
  fi
  printf '%s\n' "$matches" | grep -oE '[[:digit:]]+' | sort -n | uniq
}

comment_is_review_verdict() {
  local body="${1:-}"
  case "$body" in
    "Review → Merged"*|"Review → Done"*|"Review → In Progress"*) return 0 ;;
    *) return 1 ;;
  esac
}

# ISO-8601 timestamps; skip when assigned_at is within the last DEDUPE_MINUTES of now_epoch.
assigned_within_dedupe_window() {
  local assigned_at="${1:-}"
  local now_epoch="${2:-}"
  local minutes="${3:-$DEDUPE_MINUTES}"
  [[ -z "$assigned_at" || -z "$now_epoch" ]] && return 1
  local assigned_epoch
  assigned_epoch="$(date -u -d "$assigned_at" +%s 2>/dev/null || true)"
  [[ -z "$assigned_epoch" ]] && return 1
  local window_seconds=$((minutes * 60))
  local age=$((now_epoch - assigned_epoch))
  (( age >= 0 && age < window_seconds ))
}

# comments_json: JSON array of {body, created_at}; head_commit_date: ISO-8601.
has_review_verdict_after() {
  local comments_json="${1:-[]}"
  local head_commit_date="${2:-}"
  [[ -z "$head_commit_date" ]] && return 1
  local head_epoch
  head_epoch="$(date -u -d "$head_commit_date" +%s 2>/dev/null || true)"
  [[ -z "$head_epoch" ]] && return 1

  local n i body created created_epoch
  n="$(printf '%s' "$comments_json" | jq 'length')"
  for ((i = 0; i < n; i++)); do
    body="$(printf '%s' "$comments_json" | jq -r ".[$i].body // \"\"")"
    created="$(printf '%s' "$comments_json" | jq -r ".[$i].created_at // \"\"")"
    if ! comment_is_review_verdict "$body"; then
      continue
    fi
    created_epoch="$(date -u -d "$created" +%s 2>/dev/null || true)"
    [[ -z "$created_epoch" ]] && continue
    if (( created_epoch > head_epoch )); then
      return 0
    fi
  done
  return 1
}

# --- live GitHub helpers (not used by unit tests) ---

gh_api() {
  gh api "$@"
}

repo_owner() {
  printf '%s' "${REPO%%/*}"
}

repo_name() {
  printf '%s' "${REPO#*/}"
}

find_open_bot_prs() {
  local head_sha="$1"
  local head_branch="$2"
  local prs
  prs="$(gh_api "repos/${REPO}/commits/${head_sha}/pulls" --jq \
    "[.[] | select(.state==\"open\" and .user.login==\"${BOT_LOGIN}\") | {number, body, html_url}]" 2>/dev/null || echo '[]')"
  if [[ "$(printf '%s' "$prs" | jq 'length')" -gt 0 ]]; then
    printf '%s' "$prs"
    return 0
  fi
  # Fallback: open PRs with this head branch in the same repo.
  gh_api "repos/${REPO}/pulls?state=open&head=$(repo_owner):${head_branch}&per_page=20" --jq \
    "[.[] | select(.user.login==\"${BOT_LOGIN}\") | {number, body, html_url}]" 2>/dev/null || echo '[]'
}

closing_issue_numbers() {
  local pr_number="$1"
  local body="$2"
  local from_graphql
  from_graphql="$(gh_api graphql -f query="
    query(\$owner:String!, \$name:String!, \$number:Int!) {
      repository(owner:\$owner, name:\$name) {
        pullRequest(number:\$number) {
          closingIssuesReferences(first: 50) { nodes { number } }
        }
      }
    }" -f owner="$(repo_owner)" -f name="$(repo_name)" -F number="$pr_number" \
    --jq '.data.repository.pullRequest.closingIssuesReferences.nodes[].number' 2>/dev/null || true)"

  local combined
  combined="$(printf '%s\n%s\n' "$from_graphql" "$(parse_closes_from_body "$body")" | grep -E '^[0-9]+$' | sort -n | uniq || true)"
  printf '%s\n' "$combined"
}

head_commit_date() {
  local sha="$1"
  gh_api "repos/${REPO}/commits/${sha}" --jq '.commit.committer.date // .commit.author.date'
}

issue_recently_assigned_to_bot() {
  local issue="$1"
  local now_epoch="$2"
  local timeline
  timeline="$(gh_api "repos/${REPO}/issues/${issue}/timeline?per_page=100" -H "Accept: application/vnd.github+json" \
    --jq "[.[] | select(.event==\"assigned\" and .assignee.login==\"${BOT_LOGIN}\") | .created_at]" 2>/dev/null || echo '[]')"
  local n i at
  n="$(printf '%s' "$timeline" | jq 'length')"
  for ((i = 0; i < n; i++)); do
    at="$(printf '%s' "$timeline" | jq -r ".[$i]")"
    if assigned_within_dedupe_window "$at" "$now_epoch" "$DEDUPE_MINUTES"; then
      log "issue #${issue}: skip — ${BOT_LOGIN} assigned within last ${DEDUPE_MINUTES}m (${at})"
      return 0
    fi
  done
  return 1
}

issue_has_post_commit_verdict() {
  local issue="$1"
  local head_date="$2"
  local comments
  comments="$(gh_api "repos/${REPO}/issues/${issue}/comments?per_page=100" \
    --jq '[.[] | {body, created_at}]' 2>/dev/null || echo '[]')"
  if has_review_verdict_after "$comments" "$head_date"; then
    log "issue #${issue}: skip — Review verdict comment after head commit (${head_date})"
    return 0
  fi
  return 1
}

reassign_bot() {
  local issue="$1"
  local payload
  payload="$(jq -n --arg u "$BOT_LOGIN" '{assignees:[$u]}')"
  log "issue #${issue}: remove ${BOT_LOGIN} assignee"
  # Not assigned yet is fine; still add to fire issue-assigned.
  gh_api -X DELETE "repos/${REPO}/issues/${issue}/assignees" --input - <<<"$payload" >/dev/null 2>&1 \
    || log "issue #${issue}: remove returned non-zero (may not have been assigned)"
  log "issue #${issue}: add ${BOT_LOGIN} assignee"
  gh_api -X POST "repos/${REPO}/issues/${issue}/assignees" --input - <<<"$payload" >/dev/null
  log "issue #${issue}: wake done"
}

wake_issue() {
  local issue="$1"
  local head_date="$2"
  local now_epoch="$3"
  if issue_recently_assigned_to_bot "$issue" "$now_epoch"; then
    return 0
  fi
  if issue_has_post_commit_verdict "$issue" "$head_date"; then
    return 0
  fi
  reassign_bot "$issue"
}

process_pr() {
  local pr_number="$1"
  local body="$2"
  local head_sha="$3"
  local now_epoch="$4"
  local head_date
  head_date="$(head_commit_date "$head_sha")"
  log "PR #${pr_number}: head ${head_sha} committed ${head_date}"

  local issues issue
  issues="$(closing_issue_numbers "$pr_number" "$body")"
  if [[ -z "${issues//[[:space:]]/}" ]]; then
    log "PR #${pr_number}: no closing issues — nothing to wake"
    return 0
  fi
  while IFS= read -r issue; do
    [[ -z "$issue" ]] && continue
    log "PR #${pr_number}: linked issue #${issue}"
    wake_issue "$issue" "$head_date" "$now_epoch"
  done <<<"$issues"
}

main() {
  if [[ -z "${REPO:-}" ]]; then
    log "REPO is required"
    exit 1
  fi
  if [[ -z "${GH_TOKEN:-}${GITHUB_TOKEN:-}" ]]; then
    log "GH_TOKEN (or GITHUB_TOKEN) is required"
    exit 1
  fi
  export GH_TOKEN="${GH_TOKEN:-$GITHUB_TOKEN}"

  # workflow_dispatch with no run context: no-op success for smoke.
  if [[ "${EVENT_NAME:-}" == "workflow_dispatch" && -z "${WR_CONCLUSION:-}" ]]; then
    log "workflow_dispatch with no workflow_run context — exit without touching issues"
    exit 0
  fi

  local conclusion="${WR_CONCLUSION:-}"
  local event="${WR_EVENT:-}"
  local head_branch="${WR_HEAD_BRANCH:-}"
  local head_sha="${WR_HEAD_SHA:-}"
  local default_branch="${DEFAULT_BRANCH:-}"

  log "factory-wake: event=${event} conclusion=${conclusion} head=${head_branch} sha=${head_sha} default=${default_branch}"

  if ! is_eligible_conclusion "$conclusion"; then
    log "skip: conclusion '${conclusion}' is not success or failure"
    exit 0
  fi
  if ! is_eligible_trigger_event "$event"; then
    log "skip: trigger event '${event}' is not pull_request or push"
    exit 0
  fi
  if is_default_branch "$head_branch" "$default_branch"; then
    log "skip: head branch '${head_branch}' is the default branch"
    exit 0
  fi

  local prs
  prs="$(find_open_bot_prs "$head_sha" "$head_branch")"
  local pr_count
  pr_count="$(printf '%s' "$prs" | jq 'length')"
  if [[ "$pr_count" -eq 0 ]]; then
    log "skip: no open ${BOT_LOGIN} pull request for this head"
    exit 0
  fi

  local now_epoch
  now_epoch="$(date -u +%s)"
  local i number body
  for ((i = 0; i < pr_count; i++)); do
    number="$(printf '%s' "$prs" | jq -r ".[$i].number")"
    body="$(printf '%s' "$prs" | jq -r ".[$i].body // \"\"")"
    log "eligible open bot PR #${number}"
    process_pr "$number" "$body" "$head_sha" "$now_epoch"
  done
}

# When sourced by tests, skip main.
if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
  main "$@"
fi
