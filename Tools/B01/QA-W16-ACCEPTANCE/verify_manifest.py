#!/usr/bin/env python3
"""Verify QA-only source manifest; never Unity G1 acceptance."""
import hashlib
import json
from pathlib import Path
p=Path(__file__).resolve().parent
m=json.loads((p/'manifest.json').read_text(encoding='utf-8'))
assert m['schema']=='b01-60-w16-qa-sources-v1'
for name,item in m['files'].items():
    raw=(p/name).read_bytes()
    assert len(raw)==item['bytes'],name
    assert hashlib.sha256(raw).hexdigest()==item['sha256'],name
assert len(m['files'])==4,'unexpected source count'
print('QA_SOURCE_SHA256_MANIFEST_PASS',len(m['files']))
