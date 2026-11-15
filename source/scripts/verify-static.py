#!/usr/bin/env python3
"""Verify the release contains and links its public payload."""
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlsplit

class Links(HTMLParser):
    files = []
    def handle_starttag(self, tag, attributes):
        attrs = dict(attributes)
        if tag == 'script' and attrs.get('src'):
            self.files.append(attrs['src'])
        if tag == 'link' and attrs.get('href'):
            self.files.append(attrs['href'])

dist = Path(__file__).resolve().parent.parent / "dist"
parser = Links()
parser.feed((dist / "index.html").read_text(encoding="utf-8"))
for asset in parser.files:
    uri = urlsplit(asset)
    if not uri.scheme and not uri.netloc:
        assert (dist / uri.path.lstrip("/")).is_file(), f"Missing referenced asset: {asset}"
wasm = list((dist / "_framework").glob("*.wasm"))
assert wasm, "No WebAssembly runtime/assets"
assert any(path.read_bytes()[:4] == bytes([0, 97, 115, 109]) for path in wasm), "No usable WASM magic bytes"
print(f"Static release has {len(parser.files)} linked assets and {len(wasm)} WASM files.")
