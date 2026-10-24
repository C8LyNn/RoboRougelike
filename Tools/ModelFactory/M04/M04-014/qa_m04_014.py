#!/usr/bin/env python3
"""Independent M04-014 static mesh and deterministic OBJ export QA.
Optional GLB verification is structural, not a Unity runtime test.
"""
import argparse
import hashlib
import importlib.util
import json
import math
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

HERE=Path(__file__).resolve().parent
GEN=HERE/'generate_m04_014.py'
STEM='M04-014_mud_screw_drive'

def read_obj(path):
 groups={};verts=[];faces=[];used={};current=None
 for line in path.read_text(encoding='utf-8').splitlines():
  if line.startswith('o '):
   current=line[2:];assert current not in groups;groups[current]=[]
  elif line.startswith('usemtl '):
   assert current;used[current]=line.split(maxsplit=1)[1]
  elif line.startswith('v '):
   v=tuple(map(float,line.split()[1:]));assert len(v)==3
   assert all(math.isfinite(q) for q in v)
   verts.append(v);groups[current].append(v)
  elif line.startswith('f '):
   face=tuple(map(int,line.split()[1:]));assert len(face)==3
   assert all(1<=q<=len(verts) for q in face);faces.append(face)
 return groups,verts,faces,used

def check_glb(path):
 raw=path.read_bytes()
 assert len(raw)>1000
 magic,version,length=struct.unpack_from('<III',raw)
 assert (magic,version,length)==(0x46546C67,2,len(raw))
 n,typ=struct.unpack_from('<II',raw,12);assert typ==0x4E4F534A
 data=json.loads(raw[20:20+n].decode('utf-8'))
 meshes=data.get('meshes',[]);materials=data.get('materials',[])
 nodes=data.get('nodes',[]);animations=data.get('animations',[])
 assert len(meshes)>=36 and len(materials)>=8
 assert any('ROTOR_Z_AXIS' in x.get('name','') for x in nodes)
 rotation_targets=[]
 for anim in animations:
  for ch in anim.get('channels',[]):
   if ch['target']['path']=='rotation':
    rotation_targets.append(nodes[ch['target']['node']]['name'])
 assert any('ROTOR_Z_AXIS' in name for name in rotation_targets)
 return {'glb':'PASS','meshes':len(meshes),'materials':len(materials),
         'animations':len(animations),'rotation_targets':rotation_targets}

def run():
 parser=argparse.ArgumentParser();parser.add_argument('--glb',type=Path)
 args=parser.parse_args()
 subprocess.run([sys.executable,'-m','py_compile',str(GEN)],check=True)
 spec=importlib.util.spec_from_file_location('m04_014_generator',GEN)
 mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
 parts=mod.build();report=mod.validate(parts)
 assert report['id']=='M04-014'
 assert report['materials']>=8 and report['moving_meshes']==5
 assert report['guard_radial_clearance_m']>=.028
 assert report['guard_axial_clearance_m']>=.028
 assert report['motion_vertex_checks']>=70000
 assert report['pivot']==[0,-.36,.52]
 assert report['unity']=='NOT RUN'
 with tempfile.TemporaryDirectory() as temp:
  a=Path(temp)/'a';b=Path(temp)/'b'
  for p in (a,b):
   subprocess.run([sys.executable,str(GEN),'--out',str(p)],check=True,capture_output=True,text=True)
  for file in (STEM+'.obj',STEM+'.mtl','M04-014_manifest.json'):
   assert hashlib.sha256((a/file).read_bytes()).digest()==hashlib.sha256((b/file).read_bytes()).digest(),file
  groups,verts,faces,used=read_obj(a/(STEM+'.obj'))
  mats={line.split()[1] for line in (a/(STEM+'.mtl')).read_text().splitlines() if line.startswith('newmtl ')}
  assert len(groups)==report['parts']
  assert len(verts)==report['vertices']
  assert len(faces)==report['triangles']
  assert len(mats)==report['materials']
  assert set(used.values())==mats
  for part in parts:
   actual=groups[part['name']];expected=part['vertices']
   assert len(actual)==len(expected)
   for v,w in zip(actual,expected):
    assert max(abs(v[i]-w[i]) for i in range(3))<5.1e-9
  flight=groups['ROTOR_ThreeTurn_Spiral_Flight']
  max_r=max(math.hypot(x,y-mod.CY) for x,y,z in flight)
  assert .296-max_r>=.028
  assert min(z for x,y,z in flight)-.093>=.028
  assert .977-max(z for x,y,z in flight)>=.028
  assert max(y for x,y,z in verts)<=1e-9
  result={'result':'PASS','id':'M04-014','parts':len(groups),
          'vertices':len(verts),'triangles':len(faces),'materials':len(mats),
          'spiral_turns':3,'motion_vertex_checks':report['motion_vertex_checks'],
          'guard_radial_clearance_m':report['guard_radial_clearance_m'],
          'guard_axial_clearance_m':report['guard_axial_clearance_m'],
          'deterministic_OBJ_MTL_manifest':True,
          'blender':'NOT RUN (in static QA process)','unity':'NOT RUN'}
 if args.glb:result.update(check_glb(args.glb))
 print(json.dumps(result,indent=2))
if __name__=='__main__':run()
