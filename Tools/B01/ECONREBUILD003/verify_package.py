#!/usr/bin/env python3
"""W21 B01-22 original additive Git source SHA + .meta GUID source-only acceptance."""
import json
import re
import subprocess
from pathlib import Path

BASE = 'be9e985a4d6bf8caa854f91cc19987ba79329f47'
ROOT = Path(__file__).resolve().parents[3]
manifest_path = Path(__file__).with_name('manifest.json')
m = json.loads(manifest_path.read_text(encoding='utf-8'))
assert m['base_exact'] == BASE
assert m['original_v2_source_blob'] == '4ca04a77e118207a1d3e2df5eb098ded56d48392'
assert m['frozen_v1_source_blob'] == 'cac95b28647528a7a46cb818c6d78037dc3e80a2'
assert m['native_unity'] == 'NOT_RUN' and m['g2'] == 'HOLD'
def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT, text=True).strip()
for path, sha in m['blobs'].items():
    actual = git('rev-parse','HEAD:'+path)
    assert actual == sha, (path,actual,sha)
assert git('rev-parse','HEAD:Assets/B01Packages/ECONREBUILD002/Runtime/RebuildOwnerAuthorityV2.cs') == m['original_v2_source_blob']
assert git('rev-parse','HEAD:Assets/B01Packages/ECONREBUILD001/Runtime/RebuildQueueService.cs') == m['frozen_v1_source_blob']
if not subprocess.run(['git','cat-file','-e',BASE+'^{commit}'],cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode == 0:
    subprocess.run(['git','fetch','--no-tags','--depth=1','origin',BASE],cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
changed=set(git('diff','--name-only',BASE,'HEAD').splitlines())
assert changed==set(m['owned_paths']), (sorted(changed),sorted(m['owned_paths']))
assert all(x.startswith(('Assets/B01Packages/ECONREBUILD003',
                        'Tools/B01/ECONREBUILD003/',
                        '.github/workflows/b01-22-p17w21-econ-v3.yml')) for x in changed)
new_guids = []
for meta in sorted(Path('Assets/B01Packages/ECONREBUILD003').rglob('*.meta')):
    text = meta.read_text()
    ms=re.findall(r'^guid:\\s*([0-9a-f]{32})$',text,re.M)
    assert len(ms)==1,meta
    new_guids.extend(ms)
# Also package root .meta is outside directory.
rootmeta=Path('Assets/B01Packages/ECONREBUILD003.meta')
rootmatch=re.findall(r'^guid:\\s*([0-9a-f]{32})$',rootmeta.read_text(),re.M)
assert len(rootmatch)==1
new_guids.extend(rootmatch)
assert len(new_guids)==4 and len(set(new_guids))==4
prior = set()
for meta in Path('Assets/B01Packages/ECONREBUILD002').rglob('*.meta'):
    prior.update(re.findall(r'^guid:\\s*([0-9a-f]{32})$',meta.read_text(),re.M))
prior.update(re.findall(r'^guid:\\s*([0-9a-f]{32})$',Path('Assets/B01Packages/ECONREBUILD002.meta').read_text(),re.M))
assert len(prior)==4 and not (prior & set(new_guids))
asm=json.loads(Path('Assets/B01Packages/ECONREBUILD003/Runtime/B01.EconRebuildV3.asmdef').read_text())
assert asm['noEngineReferences'] is True and asm['name']=='B01.EconRebuildV3'
print('PASS EXACT GIT W21 12 additive paths, 10 pinned original blobs, 4 unique new GUIDs, 0 old GUID collisions; SOURCE_G0_NOT_INDEPENDENT')
