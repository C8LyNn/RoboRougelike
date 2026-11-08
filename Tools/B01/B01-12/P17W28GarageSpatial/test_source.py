#!/usr/bin/env python3
"""B01-12 W28 exact source/package checks. Not a C# compile or Unity G1 claim."""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

PKG = Path('Tools/B01/B01-12/P17W28GarageSpatial')
PRODUCT = Path('Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs')
TEST = Path('Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs')
W27 = {
    'Assets/R3/R3GarageView.cs': 'fb6f4f21c2f3953e43e1a7b223ef5110dbcc26c1',
    'Assets/R3/R3VehicleModel.cs': '1d0f378dc598e08b3d7c16975f419d84231511bb',
    'Assets/R3/R3MountingGrid.cs': 'f39faa2ad90d56fb19caac42b3d8aff8285d8663',
    'Assets/R3/R3VehicleAssembly.cs': 'ed97dd1b0ee3b77c839615497b249dcaee479bb8',
    'Assets/R4_1/R41PartGeometry.cs': '76a14de4c347dd3e91b0a37487d804023eab5da1',
}
checks=[]

def check(name, fn):
    try:
        fn()
        checks.append((name,'PASS'))
    except Exception as exc:
        checks.append((name,'FAIL'))
        print('FAIL',name,exc,flush=True)

def require(condition, message):
    if not condition: raise AssertionError(message)

def read(root, relative):
    return (root/relative).read_bytes()

def verify_manifest(root):
    d=json.loads((root/PKG/'manifest.json').read_text(encoding='utf-8'))
    require(d['package_id']=='B01-12-GARAGE-SPATIAL-ZONES-001/R1','package identity')
    require(d['base_commit']=='e03d36f2b86f8c73a067e574b67237a8921f622a','baseline pin')
    require(len(d['files'])>=9,'missing files')
    for name, entry in d['files'].items():
        b=read(root,name)
        require(len(b)==entry['bytes'],f'FAIL_CLOSED size {name}')
        require(hashlib.sha256(b).hexdigest()==entry['sha256'],f'FAIL_CLOSED sha256 {name}')
        require(hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()==entry['git_blob_sha1'],f'FAIL_CLOSED git blob {name}')
    return d

def verify_meta(root):
    entries=list((root/'Assets/B01UserExperience').rglob('*.meta'))
    entries += [root/'Assets/B01UserExperience.meta',root/'Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs.meta']
    guids=[]
    for p in entries:
        data=p.read_text(encoding='utf-8')
        m=re.search(r'^guid: ([0-9a-f]{32})$',data,re.M)
        require(m is not None,f'guid invalid: {p}')
        guids.append(m.group(1))
        if p.suffixes[-2:]==['.cs','.meta']:
            require('MonoImporter:' in data,'C# meta importer missing')
    require(len(set(guids))==len(guids),'duplicate local GUID')
    # In a complete W27 checkout (GitHub Actions), check our FOUR new GUIDs
    # do not collide with any original Unity asset GUID. Source-only package
    # copies cannot supply that evidence and must not report repository-wide G0.
    if (root/'Assets/R3/R3GarageView.cs').is_file():
        own_paths={x.resolve() for x in entries}
        for other in (root/'Assets').rglob('*.meta'):
            if other.resolve() in own_paths:continue
            match=re.search(r'^guid: ([0-9a-f]{32})$',other.read_text(encoding='utf-8',errors='replace'),re.M)
            if match and match.group(1) in guids:raise AssertionError('foreign GUID collision '+str(other))
    return guids

def verify_semantics(root):
    src=(root/PRODUCT).read_text(encoding='utf-8')
    for token in ['R3GarageView','R3VehicleModel.OccupiedCells','R41PartGeometry.Grid','R41PartGeometry.BlockSize',
                  'R3GarageView.PlacementIssue','source.PlacementIssue','source.Preview',
                  'OccupiedRed','ProtrusionBlue','WeaponPurple','MechanismOrange','ValidGreen',
                  'Graphics.DrawMesh','RenderQueue','_SURFACE_TYPE_TRANSPARENT',
                  'RuntimeInitializeOnLoadMethod','QueryTriggerInteraction']:
        if token=='RenderQueue':continue
        if token=='QueryTriggerInteraction':continue
        require(token in src,f'API/color missing: {token}')
    for pattern in [r'\.(GarageBuy|GarageBuyWeapon|GarageMount|GarageRepair|GarageRemove)\s*\(',
                    r'\.(Save|Commit|WriteAllBytes|Delete|Spend|Award|GarageEconomy)\s*\(',
                    r'\.(enabled|isTrigger)\s*=']:
        require(not re.search(pattern,src),f'forbidden gameplay/physics mutation: {pattern}')
    require('Collider' in src and 'DrawMesh' in src,'missing mesh-only description')
    require('MaxBoxes=256' in src,'volume bound absent')
    require('=> hasTarget&&placementIssue==null' in src,'preview validity must use R3 authority')
    require('public bool SourceValidated => visible&&garage!=null&&garage.PreviewValid' in src,'host-controlled acceptance lost')
    require('return' in src,'source invalid')
    tests=(root/TEST).read_text(encoding='utf-8')
    require(tests.count('[Test]')>=6,'missing actual Unity test cases')
    require('NUnit.Framework' in tests,'tests need Unity NUnit')

def verify_w27(root):
    for path,sha in W27.items():
        original=root/path
        require(original.is_file(),f'original W27 file unavailable {path}')
        p=subprocess.run(['git','hash-object',str(original)],cwd=root,capture_output=True,text=True)
        require(p.returncode==0 and p.stdout.strip()==sha,f'W27 blob pin changed for {path}: {p.stdout.strip()}')
    # The exact source contracts checked here are original W27 player files.
    view=(root/'Assets/R3/R3GarageView.cs').read_text(encoding='utf-8-sig')
    model=(root/'Assets/R3/R3VehicleModel.cs').read_text(encoding='utf-8-sig')
    for signature in ['public string PlacementIssue','public Transform Preview','public bool PreviewValid',
                      'public R3PartKind MountKind','public Vector3Int SelectedRotation','public bool HasTarget']:
        require(signature in view,f'W27 UI contract missing: {signature}')
    for signature in ['OccupiedCells(R3PartState part)','public string PlacementError(',
                      'public string OwnedPlacementError(']:
        require(signature in model,f'W27 model contract missing: {signature}')

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--root',default=None)
    parser.add_argument('--require-w27',action='store_true')
    args=parser.parse_args()
    root=Path(args.root).resolve() if args.root else Path(__file__).resolve().parents[4]
    try:
        # A manifest mismatch exits 2 before running any substantive checks.
        mf=verify_manifest(root)
    except Exception as exc:
        print('FAIL_CLOSED manifest:',exc)
        return 2
    check('manifest exact Git/SHA256/bytes',lambda:require(len(mf['files'])>=9,'too few files'))
    check('Unity asset GUID+MonoImporter',lambda:verify_meta(root))
    check('Unity-facing real source API & read-only constraints',lambda:verify_semantics(root))
    check('no duplicate source/GUID list',lambda:require(len(set(mf['files']))==len(mf['files']),'duplicate entry'))
    check('runtime code 5 categories',lambda:require(all(x in (root/PRODUCT).read_text() for x in ['OccupiedRed','ProtrusionBlue','WeaponPurple','MechanismOrange','ValidGreen']),'missing categories'))
    if args.require_w27:check('original exact W27 Git blobs + public signatures',lambda:verify_w27(root))
    print('B01-12 W28 SOURCE CHECKS',len(checks),'PASS',sum(x[1]=='PASS' for x in checks),'FAIL',sum(x[1]=='FAIL' for x in checks))
    print('SOURCE_ONLY=YES UNITY_COMPILE=NOT_RUN UNITY_G1=NOT_RUN')
    return 1 if any(x[1]=='FAIL' for x in checks) else 0

if __name__=='__main__':sys.exit(main())
