#!/usr/bin/env python3
"""Independent QA for M04-008 actual OBJ and optional Blender-generated GLB.

No third-party packages. Does not claim Unity runtime or render validation.
"""
import argparse
import json
import math
import struct
import subprocess
import sys
import tempfile
from collections import Counter
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/'generate_m04_008.py'

def sub(a,b):return [x-y for x,y in zip(a,b)]
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(x*y for x,y in zip(a,b))

def check_obj(directory):
    report=json.loads((directory/'M04-008_manifest.json').read_text())
    assert report['status']=='PASS'
    assert report['asset']=='M04-008'
    assert report['side_mount_normal']=='+X'
    assert report['side_mount_origin_m']==[0,0,0]
    assert report['sampled_poses']==65
    assert report['min_shroud_bore_clearance_m']>.010
    assert report['moving_meshes']==22
    assert set(report['gimbals'])=={'FORE','AFT'}
    obj=(directory/report['obj']).read_text()
    mtl=(directory/report['mtl']).read_text()
    declared={line.split(maxsplit=1)[1] for line in mtl.splitlines() if line.startswith('newmtl ')}
    assert len(declared)==7
    verts=[];triangles=[]; objects={};materials={}; current=None
    for line in obj.splitlines():
        if line.startswith('o '):
            current=line[2:];assert current not in objects
            objects[current]={'faces':[],'vertex_ids':set()}
        elif line.startswith('usemtl '):
            assert current is not None
            materials[current]=line[7:]
        elif line.startswith('v '):
            v=tuple(float(x) for x in line.split()[1:]);assert len(v)==3
            assert all(math.isfinite(x) for x in v)
            verts.append(v)
        elif line.startswith('f '):
            f=tuple(int(x)-1 for x in line.split()[1:]);assert len(f)==3
            assert current is not None
            objects[current]['faces'].append(f)
            objects[current]['vertex_ids'].update(f)
            triangles.append(f)
    assert len(objects)==report['parts']==65
    assert len(triangles)==report['triangles']==7420
    assert set(materials.values())==declared
    assert all(0<=i<len(verts) for tri in triangles for i in tri)
    for name,part in objects.items():
        edge=Counter();oriented=Counter();volume=0
        for a,b,c in part['faces']:
            assert len({a,b,c})==3
            va,vb,vc=verts[a],verts[b],verts[c]
            assert dot(cross(sub(vb,va),sub(vc,va)),cross(sub(vb,va),sub(vc,va)))>1e-16
            volume+=dot(va,cross(vb,vc))/6
            for i,j in ((a,b),(b,c),(c,a)):
                edge[tuple(sorted((i,j)))]+=1;oriented[(i,j)]+=1
        assert all(x==2 for x in edge.values()),name
        assert all(oriented[(b,a)]==n for (a,b),n in oriented.items()),name
        assert volume>1e-12,(name,volume)
    bounds=[(min(v[i] for v in verts),max(v[i] for v in verts)) for i in range(3)]
    assert abs(bounds[0][0])<1e-7
    assert abs(bounds[0][1]-.400)<1e-7
    assert abs(bounds[1][0]+.534)<1e-7
    assert abs(bounds[1][1]-.534)<1e-7
    assert abs(bounds[2][0]+.244)<1e-7
    assert abs(bounds[2][1]-.244)<1e-7
    assert len([name for name in objects if name.endswith('_SHROUD')])==2
    assert len([name for name in objects if '_IRIS_' in name and '_IRIS_CARRIER' not in name])==8
    # Independent radial bore check on the actual exported resting-pose vertices.
    rest_clearance=1e9
    for side,cy in (('FORE',-.29),('AFT',.29)):
        for name,entry in objects.items():
            if not name.startswith(side+'_'):continue
            if not any(s in name for s in ('_TILT_','_LENS_','_CORE','_IRIS_')):continue
            for i in entry['vertex_ids']:
                _,y,z=verts[i]
                rest_clearance=min(rest_clearance,.160-math.hypot(y-cy,z))
    assert rest_clearance>.020,rest_clearance
    print(json.dumps({'asset':'M04-008','qa':'PASS','parts':len(objects),
          'triangles':len(triangles),'materials':len(declared),
          'bounds_m':bounds,'rest_bore_clearance_m':round(rest_clearance,6),
          'sampled_sweep_bore_clearance_m':report['min_shroud_bore_clearance_m'],
          'blender':'NOT RUN by this check','unity':'NOT RUN'},indent=2))

def check_glb(glb):
    data=glb.read_bytes()
    assert data[:4]==b'glTF'
    version,length=struct.unpack_from('<II',data,4)
    assert version==2 and length==len(data)
    n,chunk_type=struct.unpack_from('<II',data,12)
    assert chunk_type==0x4E4F534A
    document=json.loads(data[20:20+n].decode('utf-8'))
    names=[n.get('name','') for n in document.get('nodes',[])]
    assert any('FORE_Y_GIMBAL' in n for n in names),names
    assert any('AFT_Y_GIMBAL' in n for n in names),names
    assert len(document.get('materials',[]))>=7
    animations=document.get('animations',[])
    assert animations,'Blender GLB contains no animation'
    channels=[c for a in animations for c in a['channels']]
    animated_nodes={names[c['target']['node']] for c in channels}
    assert any('FORE_Y_GIMBAL' in n for n in animated_nodes),animated_nodes
    assert any('AFT_Y_GIMBAL' in n for n in animated_nodes),animated_nodes
    assert len([c for c in channels if c['target']['path']=='rotation'])>=2
    assert len(document.get('meshes',[]))>=65
    print(json.dumps({'glb':'PASS','meshes':len(document['meshes']),
          'materials':len(document['materials']),'animations':len(animations),
          'rotation_channels':len([c for c in channels if c['target']['path']=='rotation']),
          'animated_gimbals':sorted(animated_nodes)},indent=2))

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--check-glb',type=Path)
    args=parser.parse_args()
    subprocess.run([sys.executable,'-m','py_compile',str(GEN)],check=True)
    subprocess.run([sys.executable,str(GEN),'--self-test'],check=True,capture_output=True)
    with tempfile.TemporaryDirectory() as td:
        directory=Path(td)
        subprocess.run([sys.executable,str(GEN),'--static-obj','--out',str(directory)],check=True,capture_output=True)
        check_obj(directory)
    if args.check_glb:check_glb(args.check_glb)

if __name__=='__main__':main()
