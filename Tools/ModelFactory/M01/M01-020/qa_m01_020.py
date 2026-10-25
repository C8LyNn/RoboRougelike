#!/usr/bin/env python3
"""Independent validation for M01-020 static and Blender binary exports."""
from __future__ import annotations
import argparse
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
GEN=HERE/'generate_m01_020.py'
STEM='M01-020_short_wave_resonator'

def parse_obj(path):
 objects={};current=None;vertices=[];faces=[];used=set()
 for line in path.read_text(encoding='utf-8').splitlines():
  if line.startswith('o '):
   current=line[2:];assert current not in objects
   objects[current]={'vertices':[],'faces':[]}
  elif line.startswith('v '):
   assert current is not None
   v=tuple(map(float,line.split()[1:]));assert len(v)==3 and all(math.isfinite(x) for x in v)
   objects[current]['vertices'].append(v);vertices.append(v)
  elif line.startswith('f '):
   assert current is not None
   f=tuple(map(int,line.split()[1:]));assert len(f)==3 and all(1<=x<=len(vertices) for x in f)
   objects[current]['faces'].append(f);faces.append(f)
  elif line.startswith('usemtl '):used.add(line.split()[1])
 return objects,vertices,faces,used

def check_glb(path):
 blob=path.read_bytes();assert len(blob)>100 and blob[:4]==b'glTF'
 magic,version,total=struct.unpack_from('<4sII',blob)
 assert magic==b'glTF' and version==2 and total==len(blob)
 length,typ=struct.unpack_from('<II',blob,12);assert typ==0x4e4f534a
 data=json.loads(blob[20:20+length].decode('utf-8'))
 nodes=data.get('nodes',[])
 ring=[i for i,n in enumerate(nodes) if 'WAVE_RING_VIBRATE_Z' in n.get('name','')]
 assert len(ring)==1,('missing animated ring',ring)
 channels=[ch.get('target',{}) for anim in data.get('animations',[]) for ch in anim.get('channels',[])]
 moving=[t for t in channels if t.get('node')==ring[0] and t.get('path')=='translation']
 assert moving,('missing animated translation',channels)
 assert len(data.get('meshes',[]))>=49
 assert len(data.get('materials',[]))>=6
 return {'glb_bytes':len(blob),'glb_meshes':len(data['meshes']),
         'glb_materials':len(data['materials']),
         'glb_animations':len(data['animations']),'ring_translation_channels':len(moving)}

def verify(out,require_blender=False):
 report=json.loads((out/(STEM+'_qa.json')).read_text(encoding='utf-8'))
 assert report['asset_id']=='M01-020'
 assert report['watertight_oriented_positive_volume']=='PASS'
 assert report['materials']==7
 assert report['sampled_animation_frames']==73
 assert report['moving_vs_fixed_collisions']==0
 assert report['minimum_separation_aabb_m']>=.07
 assert report['mount'].startswith('MOUNT_FRONT_SOCKET (0,0,0)')
 objs,vs,faces,used=parse_obj(out/(STEM+'.obj'))
 mtl=(out/(STEM+'.mtl')).read_text(encoding='utf-8')
 materials={l.split()[1] for l in mtl.splitlines() if l.startswith('newmtl ')}
 assert used==materials and len(materials)==7
 assert len(objs)==report['parts']==49
 assert len(vs)==report['vertices']==7006
 assert len(faces)==report['triangles']==13872
 assert {'WAVE_RING_MAIN','WAVE_RING_CYAN','WAVE_RING_INNER','Mount_Front_Key','Front_Emitter_Aperture'}.issubset(objs)
 spec=importlib.util.spec_from_file_location('m01_020_generator',GEN)
 module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
 expected=module.build();cursor=0
 for p in expected:
  obj=objs[p['name']]
  assert len(obj['vertices'])==len(p['vertices'])
  assert len(obj['faces'])==len(p['faces'])
  for a,b in zip(obj['vertices'],p['vertices']):
   assert max(abs(a[i]-b[i]) for i in range(3))<5.1e-9
  for f,e in zip(obj['faces'],p['faces']):
   assert f==tuple(x+cursor+1 for x in e)
  cursor+=len(p['vertices'])
 assert cursor==len(vs)
 result={'result':'PASS','parts':len(objs),'vertices':len(vs),
         'triangles':len(faces),'materials':len(materials),
         'sampled_frames':73,'aabb_pair_checks':report['aabb_pair_checks'],
         'min_clearance_m':report['minimum_separation_aabb_m'],
         'blender':'NOT RUN','unity':'NOT RUN'}
 if require_blender:
  assert report['backend']=='blender-bpy'
  assert report['blender_repeatability'].startswith('PASS')
  assert report['blender_export'].startswith('PASS')
  assert (out/(STEM+'.blend')).read_bytes()[:7]==b'BLENDER'
  assert (out/(STEM+'.fbx')).read_bytes()[:18]==b'Kaydara FBX Binary'
  result.update(check_glb(out/(STEM+'.glb')))
  result['blender']='PASS'
 return result

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--out',type=Path)
 parser.add_argument('--require-blender',action='store_true')
 args=parser.parse_args()
 subprocess.run([sys.executable,'-m','py_compile',str(GEN)],check=True)
 if args.out:result=verify(args.out,args.require_blender)
 else:
  with tempfile.TemporaryDirectory() as temp:
   out=Path(temp)
   subprocess.run([sys.executable,str(GEN),'--out',str(out)],check=True,stdout=subprocess.DEVNULL)
   result=verify(out)
   digest=hashlib.sha256((out/(STEM+'.obj')).read_bytes()).hexdigest()
   subprocess.run([sys.executable,str(GEN),'--out',str(out)],check=True,stdout=subprocess.DEVNULL)
   assert hashlib.sha256((out/(STEM+'.obj')).read_bytes()).hexdigest()==digest
   result['deterministic_obj_sha256']=digest
 print(json.dumps(result,ensure_ascii=False,indent=2))
if __name__=='__main__':main()