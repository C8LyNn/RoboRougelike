#!/usr/bin/env python3
"""Independent static QA for M03-014: OBJ topology, fit, mounts and repeatability."""
import collections
import json
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parent
GEN=ROOT/'generate_m03_014.py'

def read_obj(path):
    objects={}; vertices=[]; current=None
    for line in path.read_text().splitlines():
        parts=line.split()
        if not parts: continue
        if parts[0]=='o':
            current=parts[1];objects[current]={'faces':[],'material':None,'indices':set()}
        elif parts[0]=='usemtl':
            objects[current]['material']=parts[1]
        elif parts[0]=='v': vertices.append(tuple(float(v) for v in parts[1:4]))
        elif parts[0]=='f':
            face=tuple(int(x.split('/')[0])-1 for x in parts[1:]);objects[current]['faces'].append(face)
            objects[current]['indices'].update(face)
    return vertices,objects

def volume(v,faces):
    total=0.0
    for face in faces:
        a=v[face[0]]
        for i in range(1,len(face)-1):
            b,c=v[face[i]],v[face[i+1]]
            total+=sum(a[k]*(b[(k+1)%3]*c[(k+2)%3]-b[(k+2)%3]*c[(k+1)%3]) for k in range(3))/6
    return total

def verify(out):
    v,objects=read_obj(out/'M03-014_skid_chassis_runner.obj')
    manifest=json.loads((out/'M03-014_manifest.json').read_text())
    assert len(objects)==18==manifest['mesh_count']
    assert len(set(o['material'] for o in objects.values()))==5
    assert len(manifest['mounts'])==4
    assert len({tuple(m['position']) for m in manifest['mounts']})==4
    assert all(m['axis']==[0,-1,0] and m['position'][1]==0 for m in manifest['mounts'])
    assert manifest['pivot']==[0,0,0]
    assert manifest['animation']=='not required (static)'
    boxes={}
    for name,o in objects.items():
        faces=o['faces'];indices=o['indices']
        edges=collections.Counter(tuple(sorted((f[i],f[(i+1)%len(f)]))) for f in faces for i in range(len(f)))
        assert edges and all(n==2 for n in edges.values()),f'nonmanifold: {name}'
        assert volume(v,faces)>1e-8,f'wrong winding/volume: {name}'
        pts=[v[i] for i in indices]
        boxes[name]=[(min(p[k] for p in pts),max(p[k] for p in pts)) for k in range(3)]
    for i,(a,ba) in enumerate(boxes.items()):
        for b,bb in list(boxes.items())[i+1:]:
            positive=[min(ba[k][1],bb[k][1])-max(ba[k][0],bb[k][0]) for k in range(3)]
            assert not all(x>1e-6 for x in positive),f'penetration AABB: {a} / {b}: {positive}'
    belly=boxes['Ceramic_Belly_Ramped']
    assert abs(belly[2][0]+1)<1e-7 and abs(belly[2][1]-1)<1e-7
    assert belly[1][0]<-.24 and abs(belly[1][1]+.15)<1e-7
    assert len([x for x in objects if x.startswith('Copper_Socket_')])==4
    assert len([x for x in objects if x.startswith('Amber_Marker_')])==4
    print(f'PASS M03-014: {len(objects)} closed oriented meshes, 5 materials, 4 mounts, no AABB volume overlap')

def main():
    with tempfile.TemporaryDirectory() as d:
        a,b=Path(d)/'a',Path(d)/'b'
        for out in (a,b):
            subprocess.run([sys.executable,str(GEN),'--out',str(out)],check=True,capture_output=True,text=True)
            verify(out)
        for name in ['M03-014_skid_chassis_runner.obj','M03-014_skid_chassis_runner.mtl','M03-014_manifest.json']:
            assert (a/name).read_bytes()==(b/name).read_bytes(),f'non-deterministic {name}'
        print('PASS deterministic byte-identical OBJ/MTL/manifest in two fresh processes')

if __name__=='__main__': main()
