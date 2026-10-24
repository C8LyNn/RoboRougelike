#!/usr/bin/env python3
"""Independent OBJ topology, deterministic export and GLB animation QA."""
from __future__ import annotations
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
SRC=HERE/'generate_m04_011.py'
STEM='M04-011_deployable_aero_fin'

def parse_obj(path):
    verts=[];objects={};materials=set();current=None
    for line in path.read_text(encoding='utf8').splitlines():
        if line.startswith('o '):
            current=line[2:]
            assert current not in objects
            objects[current]={'v':[],'f':[],'mat':None}
        elif line.startswith('v '):
            v=tuple(map(float,line.split()[1:]));assert len(v)==3
            assert all(math.isfinite(x) for x in v)
            verts.append(v);objects[current]['v'].append(len(verts)-1)
        elif line.startswith('f '):
            f=tuple(int(v)-1 for v in line.split()[1:]);assert len(f)==3
            objects[current]['f'].append(f)
        elif line.startswith('usemtl '):
            objects[current]['mat']=line[7:];materials.add(line[7:])
    return verts,objects,materials

def topology(v,objects):
    count=0
    for name,obj in objects.items():
        own=set(obj['v']);edges=Counter();directions=Counter();volume=0
        for a,b,c in obj['f']:
            assert a in own and b in own and c in own and len({a,b,c})==3,name
            A,B,C=v[a],v[b],v[c]
            cross=((B[1]-A[1])*(C[2]-A[2])-(B[2]-A[2])*(C[1]-A[1]),
                   (B[2]-A[2])*(C[0]-A[0])-(B[0]-A[0])*(C[2]-A[2]),
                   (B[0]-A[0])*(C[1]-A[1])-(B[1]-A[1])*(C[0]-A[0]))
            assert sum(x*x for x in cross)>1e-18,name
            volume+=(A[0]*(B[1]*C[2]-B[2]*C[1])+
                     A[1]*(B[2]*C[0]-B[0]*C[2])+
                     A[2]*(B[0]*C[1]-B[1]*C[0]))/6
            for i,j in ((a,b),(b,c),(c,a)):
                edges[tuple(sorted((i,j)))]+=1;directions[(i,j)]+=1
        assert edges and all(n==2 for n in edges.values()),('manifold',name)
        assert all(directions[(j,i)]==n for (i,j),n in directions.items()),('winding',name)
        assert volume>1e-11,('positive volume',name,volume)
        count+=len(obj['f'])
    return count

def check_static():
    spec=importlib.util.spec_from_file_location('model',SRC)
    model=importlib.util.module_from_spec(spec);spec.loader.exec_module(model)
    result=model.validate(model.build())
    with tempfile.TemporaryDirectory(prefix='m04-011-qa-') as tmp:
        out=Path(tmp)
        subprocess.run([sys.executable,str(SRC),'--static-obj','--out',str(out)],
                       check=True,capture_output=True,text=True)
        v,objects,mats=parse_obj(out/(STEM+'.obj'))
        assert len(objects)==result['parts']==51
        assert len(v)==result['vertices']==1642
        assert topology(v,objects)==result['triangles']==3104
        assert len(mats)==result['materials']==7
        assert min(x for x,y,z in v)==0
        assert max(x for x,y,z in v)<.75
        # Byte-identical regeneration in two independent subprocesses.
        with tempfile.TemporaryDirectory(prefix='m04-011-rebuild-') as second:
            subprocess.run([sys.executable,str(SRC),'--static-obj','--out',second],
                           check=True,capture_output=True,text=True)
            for filename in (STEM+'.obj',STEM+'.mtl','M04-011_static_qa.json'):
                expected=(out/filename).read_bytes()
                actual=(Path(second)/filename).read_bytes()
                assert hashlib.sha256(expected).digest()==hashlib.sha256(actual).digest(),filename
        report=json.loads((out/'M04-011_static_qa.json').read_text())
        for key in ('parts','vertices','triangles','materials','sample_count',
                    'fixed_pair_checks','min_conservative_aabb_clearance_m'):
            assert report[key]==result[key],key
        # Actual ring bore is 19mm; actual rotating axle is 14mm radius.
        for tag,yc in model.CENTRES.items():
            shaft=objects[tag+'_AXLE']
            axis=[v[i] for i in shaft['v']]
            shaft_radius=max(math.hypot(x-model.PIVOT_X,z-model.PIVOT_Z)
                             for x,y,z in axis)
            for side in ('P','N'):
                ring=objects[tag+'_BEARING_'+side]
                rv=[v[i] for i in ring['v']]
                bore=min(math.hypot(x-model.PIVOT_X,z-model.PIVOT_Z)
                         for x,y,z in rv)
                assert bore-shaft_radius>=.00499
        print(json.dumps({'id':model.ID,'status':'PASS','objects':len(objects),
            'vertices':len(v),'triangles':result['triangles'],
            'materials':len(mats),'pose_samples':result['sample_count'],
            'collision_pair_checks':result['fixed_pair_checks'],
            'min_clearance_m':result['min_conservative_aabb_clearance_m'],
            'regenerated_obj_mtl_parity':'BYTE_IDENTICAL','blender':'NOT RUN',
            'unity':'NOT RUN'},indent=2))

def check_glb(path):
    with path.open('rb') as f:
        head=f.read(12)
        magic,version,total=struct.unpack('<4sII',head)
        assert magic==b'glTF' and version==2 and total==path.stat().st_size
        length,kind=struct.unpack('<I4s',f.read(8))
        assert kind==b'JSON'
        doc=json.loads(f.read(length).decode('utf8'))
    assert len(doc['meshes'])==51,('meshes',len(doc['meshes']))
    assert len(doc['materials'])>=7
    nodes=doc['nodes'];animations=doc.get('animations',[])
    paths=[]
    for anim in animations:
        for channel in anim['channels']:
            target=channel['target']
            paths.append((nodes[target['node']]['name'],target['path']))
    for tag in ('FORE','MID','AFT'):
        assert any(tag+'_HINGE_Y' in name and prop=='rotation'
                   for name,prop in paths),(tag,paths)
    print(json.dumps({'id':'M04-011','glb':'PASS','meshes':len(doc['meshes']),
        'materials':len(doc['materials']),'animations':len(animations),
        'rotation_channels':len([x for x in paths if x[1]=='rotation']),
        'three_hinges':'PASS'},indent=2))

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--check-glb',type=Path)
    args=ap.parse_args()
    if args.check_glb:check_glb(args.check_glb)
    else:check_static()
if __name__=='__main__':main()
