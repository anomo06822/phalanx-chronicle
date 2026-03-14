#!/usr/bin/env bash

set -euo pipefail

if [ "$#" -ne 2 ]; then
  echo "usage: $0 <path-to-app> <output-zip>" >&2
  exit 1
fi

app_path="$1"
output_zip="$2"

if [ ! -d "$app_path" ]; then
  echo "error: app bundle not found: $app_path" >&2
  exit 1
fi

mkdir -p "$(dirname "$output_zip")"
rm -f "$output_zip"

if command -v ditto >/dev/null 2>&1; then
  ditto -c -k --sequesterRsrc --keepParent "$app_path" "$output_zip"
  exit 0
fi

app_parent="$(dirname "$app_path")"
app_name="$(basename "$app_path")"

(
  cd "$app_parent"
  zip -r -y "$output_zip" "$app_name"
)
