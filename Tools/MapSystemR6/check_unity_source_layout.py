"""Static structural check; not a C# compile or Unity import test."""
from pathlib import Path
import re,json,sys
root=Path(__file__).resolve().parents[1]
asset_root=root/'Assets/RoboRougelike/MapSystemR4'
errors=[]
files=[p for p in asset_root.rglob('*') if p.is_file() and p.suffix!='.meta']
for p in files:
    meta=p.with_name(p.name+'.meta')
    if not meta.exists(): errors.append(f'Missing meta: {p}')
for p in [a for a in asset_root.rglob('*') if a.is_dir()]:
    if not p.with_name(p.name+'.meta').exists(): errors.append(f'Missing folder meta: {p}')
guids=[]
for p in asset_root.rglob('*.meta'):
    found=re.search(r'^guid:\s*([0-9a-f]{32})$',p.read_text(encoding='utf-8'),flags=re.M)
    if not found: errors.append(f'Invalid GUID meta: {p}')
    else: guids.append(found.group(1))
if len(guids)!=len(set(guids)):errors.append('Duplicate Unity asset GUID(s)')
for p in asset_root.rglob('*.asmdef'):
    try:
        obj=json.loads(p.read_text(encoding='utf-8'))
        if 'name' not in obj:errors.append(f'Assembly without name {p}')
    except json.JSONDecodeError as exc:errors.append(str(exc))
for p in (asset_root/'Runtime').rglob('*.cs'):
    s=p.read_text(encoding='utf-8')
    if re.search(r'\busing\s+UnityEditor\s*;',s):errors.append(f'Runtime imports UnityEditor: {p}')
    if s.count('{') != s.count('}') :errors.append(f'Unbalanced text braces (heuristic): {p}')
if errors:
    for e in errors: print('ERROR:',e)
    sys.exit(1)
print(f'PASS static Unity source layout: {len(files)} source/assets, {len(guids)} unique .meta GUIDs, {len(list(asset_root.rglob("*.asmdef")))} assembly definitions')
