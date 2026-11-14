#!/usr/bin/env bash
# Pick the newest Default-channel release from Octopus JSON (issue cmdemo1-system#655).
#
# Usage:
#   select-default-channel-release.sh --channels-json PATH --releases-json PATH
#
# Reads:
#   channels JSON: either a paged { Items: [...] } or a bare array of channels
#   releases JSON: either a paged { Items: [...] } or a bare array of releases
#
# Writes to stdout (and GITHUB_OUTPUT when set):
#   channel_id=...     id of the channel named Default (empty when none)
#   release_json=...   compact JSON of the first Default-channel release, or empty
#
# Pull requests channel releases are never selected, even when they appear first in
# the releases list. Callers should prefer requesting releases?channel=<Default id>.

set -euo pipefail

usage() {
  echo "usage: $0 --channels-json PATH --releases-json PATH" >&2
  exit 2
}

channels_json="" releases_json=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --channels-json) channels_json="${2:-}"; shift 2 ;;
    --releases-json) releases_json="${2:-}"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -n "$channels_json" && -n "$releases_json" ]] || usage
[[ -f "$channels_json" && -f "$releases_json" ]] || {
  echo "channels-json and releases-json must be readable files" >&2
  exit 1
}

channel_id="$(jq -r '
  (if type == "array" then . else (.Items // []) end)
  | map(select(.Name == "Default") | .Id)
  | .[0] // empty
' "$channels_json")"

release_json=""
if [[ -n "$channel_id" ]]; then
  release_json="$(jq -c --arg cid "$channel_id" '
    (if type == "array" then . else (.Items // []) end)
    | map(select(.ChannelId == $cid))
    | .[0] // empty
  ' "$releases_json")"
  if [[ "$release_json" == "null" ]]; then
    release_json=""
  fi
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    printf 'channel_id=%s\n' "$channel_id"
    printf 'release_json=%s\n' "$release_json"
  } >> "$GITHUB_OUTPUT"
fi

printf 'channel_id=%s\n' "$channel_id"
printf 'release_json=%s\n' "$release_json"
