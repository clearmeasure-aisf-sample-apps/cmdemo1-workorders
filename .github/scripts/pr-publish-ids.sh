#!/usr/bin/env bash
# Format the publish-only PR image tag and package version.
#
# Usage:
#   pr-publish-ids.sh --major M --minor N --run R --pr P --sha SHA
#
# Writes:
#   version=M.N.R-pr.P
#   image_tag=pr-P-SHA
# to stdout and to GITHUB_OUTPUT when that file is set.
# SHA must be 7–40 lowercase hex characters (short or full git object name).

set -euo pipefail

usage() {
  echo "usage: $0 --major M --minor N --run R --pr P --sha SHA" >&2
  exit 2
}

major="" minor="" run="" pr="" sha=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --major) major="${2:-}"; shift 2 ;;
    --minor) minor="${2:-}"; shift 2 ;;
    --run) run="${2:-}"; shift 2 ;;
    --pr) pr="${2:-}"; shift 2 ;;
    --sha) sha="${2:-}"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -n "$major" && -n "$minor" && -n "$run" && -n "$pr" && -n "$sha" ]] || usage

if [[ ! "$major" =~ ^[0-9]+$ || ! "$minor" =~ ^[0-9]+$ || ! "$run" =~ ^[0-9]+$ || ! "$pr" =~ ^[0-9]+$ ]]; then
  echo "major, minor, run, and pr must be non-negative integers" >&2
  exit 1
fi

sha_lower="$(printf '%s' "$sha" | tr '[:upper:]' '[:lower:]')"
if [[ ! "$sha_lower" =~ ^[0-9a-f]{7,40}$ ]]; then
  echo "sha must be 7–40 hexadecimal characters" >&2
  exit 1
fi

version="${major}.${minor}.${run}-pr.${pr}"
image_tag="pr-${pr}-${sha_lower}"

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  {
    printf 'version=%s\n' "$version"
    printf 'image_tag=%s\n' "$image_tag"
  } >> "$GITHUB_OUTPUT"
fi

printf 'version=%s\n' "$version"
printf 'image_tag=%s\n' "$image_tag"
