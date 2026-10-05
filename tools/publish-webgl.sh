#!/usr/bin/env bash
# Publish Build/WebGL to the gh-pages branch (GitHub Pages).
# Build first from Unity: Tools > Shark Hunter > Build WebGL.
# gh-pages is a deploy-only branch: each publish replaces it with a single fresh commit (force push)
# so the repo doesn't accumulate a history of ~12 MB binary builds.
set -euo pipefail

cd "$(dirname "$0")/.."
BUILD=Build/WebGL
[ -f "$BUILD/index.html" ] || { echo "No $BUILD/index.html - build WebGL from Unity first." >&2; exit 1; }

REMOTE=${REMOTE:-origin}
SRC_SHA=$(git rev-parse --short HEAD)
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

cp -R "$BUILD"/. "$TMP"/
touch "$TMP/.nojekyll"

cd "$TMP"
git init -q -b gh-pages
git add -A
git -c user.name="$(git -C "$OLDPWD" config user.name)" -c user.email="$(git -C "$OLDPWD" config user.email)" \
    commit -q -m "Publish WebGL build of $SRC_SHA"
git push --force "$(git -C "$OLDPWD" remote get-url "$REMOTE")" gh-pages:gh-pages
echo "Published. Enable Pages (Settings > Pages > Deploy from branch: gh-pages, / root) if not already."
