#!/usr/bin/env python3
"""B01-22 W23 actual Git SHA manifest, Unity GUID closure and frozen ancestry."""
from pathlib import Path
import subprocess, re, json
ROOT=Path(__file__).resolve().parents[3]
m=json.loads((Path(__file__).with_name('manifest.json')).read_text())
assert m['parent_sha']=='1135ca4f5d685f6188b7c221c7237a449e21f226'
assert m['native_unity']=='NOT_RUN' and m['release']=='HOLD'
def git(*args):
    return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
for path,pinned in m['pinned_blobs'].items():
    actual=git('rev-parse','HEAD:'+path)
    assert actual==pinned,(path,actual,pinned)
for key,path in m['frozen_files'].items():
    assert git('rev-parse','HEAD:'+path)==m['frozen_sha1'][key],key
base=m['parent_sha']
if subprocess.run(['git','cat-file','-e',base+'^{commit}'],cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
    subprocess.run(['git','fetch','--no-tags','--depth=1','origin',base],
                   cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
changed=set(git('diff','--name-only',base,'HEAD').splitlines())
assert changed==set(m['owned_paths']),(sorted(changed),sorted(m['owned_paths']))
assert len(changed)==12
assert all(x.startswith(('Assets/B01Packages/ECONCOREGUARD005',
                         'Tools/B01/ECONCOREGUARD005/')) or
           x=='.github/workflows/b01-22-p17w23-econ-core-guard.yml' for x in changed)
def guids(path):
    return re.findall(r'^guid:\s*([0-9a-f]{32})$',path.read_text(),re.M)
newmetas=list((ROOT/'Assets/B01Packages/ECONCOREGUARD005').rglob('*.meta'))
newmetas.append(ROOT/'Assets/B01Packages/ECONCOREGUARD005.meta')
newids=[x for path in newmetas for x in guids(path)]
assert len(newmetas)==4 and len(newids)==4 and len(set(newids))==4
oldmetas=list((ROOT/'Assets/B01Packages/ECONREADINESS004').rglob('*.meta'))
oldmetas.append(ROOT/'Assets/B01Packages/ECONREADINESS004.meta')
oldids={x for path in oldmetas for x in guids(path)}
assert len(oldids)==4 and not(oldids&set(newids))
asm=json.loads((ROOT/'Assets/B01Packages/ECONCOREGUARD005/Runtime/B01.EconCoreGuardV5.asmdef').read_text())
assert asm['name']=='B01.EconCoreGuardV5' and asm['noEngineReferences']
assert asm['references']==['B01.EconRebuildV3','B01.EconReadinessV4']
print('W23_MANIFEST_PASS 12 owned paths 11 nonself pinned Git SHA1 4 unique GUID; V3/V4 frozen unchanged; SOURCE_G0_INDEPENDENT_PENDING')
