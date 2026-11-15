#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
scratch="$(mktemp -d)"
trap 'rm -rf -- "$scratch"' EXIT
"${DOTNET:-dotnet}" publish src/Forma.Web/Forma.Web.csproj -c Release -o "$scratch"
test -s "$scratch/wwwroot/index.html"
mkdir -p dist
# Copy only public files. Compression sidecars are optional; the original files still serve.
python3 - "$scratch/wwwroot" dist <<'PY'
from pathlib import Path
from shutil import copy2, rmtree
import sys
source, dest = map(Path, sys.argv[1:])
for item in dest.iterdir():
    if item.is_dir():
        rmtree(item)
    else:
        item.unlink()
for item in source.rglob('*'):
    if item.is_file() and item.suffix not in ('.br', '.gz'):
        target = dest / item.relative_to(source)
        target.parent.mkdir(parents=True, exist_ok=True)
        copy2(item, target)
PY
