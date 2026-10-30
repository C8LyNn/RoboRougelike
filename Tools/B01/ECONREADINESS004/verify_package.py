#!/usr/bin/env python3
"""W22 verify exact additive B01-22 source, independent GUIDs and frozen V3."""
from pathlib import Path
import json,re,subprocess
ROOT=Path(__file__).resolve().parents[3]
M=json.loads((Path(__file__).with_name('manifest.json')).read_text())
assert M['base_exact']=='b97b03cac74bef78ab86a8f3a2c98635f30c61c6'
assert M['native_unity']=='NOT_RUN' and M['g2']=='HOLD'
def git(*arg):
    return subprocess.check_output(['git',*arg],cwd=ROOT,text=True).strip()
assert git('rev-parse','HEAD:Assets/B01Packages/ECONREBUILD003/Runtime/RebuildOwnerAuthorityV3.cs')==M['frozen_v3_blob']
assert git('rev-parse','HEAD:Assets/B01Packages/ECONREBUILD002/Runtime/RebuildOwnerAuthorityV2.cs')==M['frozen_v2_blob']
assert git('rev-parse','HEAD:Assets/B01Packages/ECONREBUILD001/Runtime/RebuildQueueService.cs')==M['frozen_v1_blob']
for path,expected in M['blobs'].items():
    assert git('rev-parse','HEAD:'+path)==expected,(path,expected)
base=M['base_exact']
exists=subprocess.run(['git','cat-file','-e',base+'^{commit}'],cwd=ROOT,
    stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0
if not exists: subprocess.run(['git','fetch','--no-tags','--depth=1','origin',base],cwd=ROOT,
    check=True,stdout=subprocess.DEVNULL)
changed=set(git('diff','--name-only',base,'HEAD').splitlines())
assert changed==set(M['paths']), (sorted(changed),sorted(M['paths']))
assert len(changed)==12 and all(
    p.startswith('Assets/B01Packages/ECONREADINESS004') or
    p.startswith('Tools/B01/ECONREADINESS004/') or
    p=='.github/workflows/b01-22-p17w22-econ-readiness.yml'
    for p in changed)
def guids(path):
    return re.findall(r'^guid:\s*([0-9a-f]{32})$',path.read_text(),re.M)
newmeta=list((ROOT/'Assets/B01Packages/ECONREADINESS004').rglob('*.meta'))
newmeta.append(ROOT/'Assets/B01Packages/ECONREADINESS004.meta')
new=[x for p in newmeta for x in guids(p)]
assert len(newmeta)==4 and len(new)==4 and len(set(new))==4
oldmeta=list((ROOT/'Assets/B01Packages/ECONREBUILD003').rglob('*.meta'))
oldmeta.append(ROOT/'Assets/B01Packages/ECONREBUILD003.meta')
old=set(x for p in oldmeta for x in guids(p))
assert not old.intersection(new) and len(old)==4
asm=json.loads((ROOT/'Assets/B01Packages/ECONREADINESS004/Runtime/B01.EconReadinessV4.asmdef').read_text())
assert asm['name']=='B01.EconReadinessV4' and asm['noEngineReferences'] is True
assert asm['references']==['B01.EconRebuildV3']
print('MANIFEST_PASS 12 owned additive paths, 11 exact source blobs, 4 distinct meta GUIDs, V1/V2/V3 frozen source; INDEPENDENT_G0_PENDING')
