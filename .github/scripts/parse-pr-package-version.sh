#!/usr/bin/env bash
# Read the PR package version from a directory of ChurchBulletin.*.nupkg files.
#
# Usage:
#   parse-pr-package-version.sh --packages-dir DIR
#
# Writes version=M.N.R-pr.P to stdout and GITHUB_OUTPUT when set.
# Prefers ChurchBulletin.Database.*.nupkg, then ChurchBulletin.UI.*.nupkg.

set -euo pipefail

usage() {
  echo "usage: $0 --packages-dir DIR" >&2
  exit 2
}

packages_dir=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --packages-dir) packages_dir="${2:-}"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -n "$packages_dir" ]] || usage
[[ -d "$packages_dir" ]] || { echo "packages dir not found: $packages_dir" >&2; exit 1; }

pick=""
for pattern in 'ChurchBulletin.Database.*.nupkg' 'ChurchBulletin.UI.*.nupkg' 'ChurchBulletin.*.nupkg'; do
  # shellcheck disable=SC2086
  for f in "$packages_dir"/$pattern; do
    [[ -f "$f" ]] || continue
    pick="$f"
    break 2
  done
done

[[ -n "$pick" ]] || { echo "No ChurchBulletin*.nupkg in $packages_dir" >&2; exit 1; }

base="$(basename "$pick")"
# ChurchBulletin.Database.2.4.12-pr.99.nupkg → 2.4.12-pr.99
if [[ "$base" =~ ^ChurchBulletin\.[A-Za-z0-9_.]+\.([0-9]+\.[0-9]+\.[0-9]+-pr\.[0-9]+)\.nupkg$ ]]; then
  version="${BASH_REMATCH[1]}"
elif [[ "$base" =~ ^ChurchBulletin\.([0-9]+\.[0-9]+\.[0-9]+-pr\.[0-9]+)\.nupkg$ ]]; then
  version="${BASH_REMATCH[1]}"
else
  echo "Could not parse PR package version from $base" >&2
  exit 1
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'version=%s\n' "$version" >> "$GITHUB_OUTPUT"
fi
printf 'version=%s\n' "$version"
