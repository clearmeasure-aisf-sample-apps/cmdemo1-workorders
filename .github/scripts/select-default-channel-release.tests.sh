#!/usr/bin/env bash
# Checks for select-default-channel-release.sh (no network). Proves Pull requests
# channel releases are ignored (cmdemo1-system#655).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SEL="$SCRIPT_DIR/select-default-channel-release.sh"
failed=0
tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

write_json() {
  local path="$1"
  shift
  printf '%s\n' "$@" >"$path"
}

assert_eq() {
  local label="$1" actual="$2" expected="$3"
  if [[ "$actual" != "$expected" ]]; then
    echo "FAIL: $label (got '$actual', expected '$expected')" >&2
    failed=1
  else
    echo "PASS: $label"
  fi
}

channels_paged="$tmpdir/channels.json"
releases_mixed="$tmpdir/releases.json"
write_json "$channels_paged" '{
  "Items": [
    { "Id": "Channels-PR", "Name": "Pull requests" },
    { "Id": "Channels-Default", "Name": "Default" }
  ]
}'
write_json "$releases_mixed" '{
  "Items": [
    { "Id": "Releases-pr", "Version": "2.4.999-pr.12", "ChannelId": "Channels-PR", "ReleaseNotes": "Build of aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa," },
    { "Id": "Releases-def", "Version": "2.4.100", "ChannelId": "Channels-Default", "ReleaseNotes": "Build of bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb," }
  ]
}'

out="$("$SEL" --channels-json "$channels_paged" --releases-json "$releases_mixed")"
channel_id="$(awk -F= '/^channel_id=/{print substr($0,12); exit}' <<<"$out")"
release_json="$(awk -F= '/^release_json=/{print substr($0,14); exit}' <<<"$out")"
assert_eq "Default channel id" "$channel_id" "Channels-Default"
version="$(jq -r '.Version' <<<"$release_json")"
assert_eq "Default channel release wins over newer PR release" "$version" "2.4.100"

releases_pr_only="$tmpdir/releases-pr-only.json"
write_json "$releases_pr_only" '{
  "Items": [
    { "Id": "Releases-pr", "Version": "2.4.999-pr.12", "ChannelId": "Channels-PR" }
  ]
}'
out="$("$SEL" --channels-json "$channels_paged" --releases-json "$releases_pr_only")"
release_json="$(awk -F= '/^release_json=/{print substr($0,14); exit}' <<<"$out")"
assert_eq "no Default release yields empty release_json" "$release_json" ""

channels_none="$tmpdir/channels-none.json"
write_json "$channels_none" '{ "Items": [ { "Id": "Channels-PR", "Name": "Pull requests" } ] }'
out="$("$SEL" --channels-json "$channels_none" --releases-json "$releases_mixed")"
channel_id="$(awk -F= '/^channel_id=/{print substr($0,12); exit}' <<<"$out")"
assert_eq "missing Default channel yields empty channel_id" "$channel_id" ""

# Bare arrays (channels/all style)
write_json "$tmpdir/channels-array.json" '[
  { "Id": "Channels-Default", "Name": "Default" },
  { "Id": "Channels-PR", "Name": "Pull requests" }
]'
write_json "$tmpdir/releases-array.json" '[
  { "Id": "Releases-pr", "Version": "2.4.50-pr.3", "ChannelId": "Channels-PR" },
  { "Id": "Releases-def", "Version": "2.4.40", "ChannelId": "Channels-Default" }
]'
out="$("$SEL" --channels-json "$tmpdir/channels-array.json" --releases-json "$tmpdir/releases-array.json")"
version="$(awk -F= '/^release_json=/{print substr($0,14); exit}' <<<"$out" | jq -r '.Version')"
assert_eq "bare arrays still ignore Pull requests channel" "$version" "2.4.40"

if [[ "$failed" -ne 0 ]]; then
  echo "select-default-channel-release tests failed" >&2
  exit 1
fi
echo "All select-default-channel-release tests passed."
