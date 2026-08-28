#!/bin/sh
set -eu

output_dir="${1:?Usage: prepare-pages.sh <published-wwwroot>}"

test -f "$output_dir/index.html"
touch "$output_dir/.nojekyll"
cp "$output_dir/index.html" "$output_dir/404.html"
