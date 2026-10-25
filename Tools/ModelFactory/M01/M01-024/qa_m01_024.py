#!/usr/bin/env python3
"""Independent M01-024 numerical, static OBJ and Blender-export validator."""
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
GEN=HERE/'generate_m01_024.py'
STEM='M01-024_mechanical_anchor_harpoon'

def parse_obj(path):
 objects={};current=None;verts=[];faces=[];used=set()
 for line in path.read_text(encoding='utf-8').splitlines():
  if line.startswith('o '):
   current=line[2:];assert current not in objects
   objects[current]={'vertices':[],'faces':[]}
  elif line.startswith('v '):
   assert current is not None
   v=tuple(map(float,line.split()[1:]));assert len(v)==3 and all(map(math.isfinite,v))
   objects[current]['vertices'].append(v);verts.append(v)
  elif line.startswith('f '):
   assert current is not None
   f=tuple(map(int,line.split()[1:]));assert len(f)==3 and all(1<=i<=len(verts) for i in f)
   objects[current]['faces'].append(f);faces.append(f)
  elif line.startswith('usemtl '):used.add(line.split()[1])
 return objects,verts,faces,used

def check_glb(path):
 blob=path.read_bytes();assert len(blob)>100
 magic,version,length=struct.unpack_from('<4sII',blob)
 assert magic==b'glTF' and version==2 and length==len(blob)
 chunk_len,chunk_type=struct.unpack_from('<II',blob,12)
 assert chunk_type==0x4e4f534a
 gltf=json.loads(blob[20:20+chunk_len].decode('utf-8'))
 nodes=gltf.get('nodes',[])
 found=[j for j,n in enumerate(nodes) if 'HARPOON_TIP_EXTEND_Z' in n.get('name','')]
 assert len(found)==1,found
 channels=[ch.get('target',{}) for anim in gltf.get('animations',[]) for ch in anim.get('channels',[])]
 assert any(ch.get('node')==found[0] and ch.get('path')=='translation' for ch in channels)
 assert len(gltf.get('meshes',[]))>=40
 assert len(gltf.get('materials',[]))>=6
 return {'glb_bytes':len(blob),'glb_meshes':len(gltf['meshes']),
         'glb_materials':len(gltf['materials']),
         'glb_animations':len(gltf['animations']),
         'harpoon_translation_channel':'PASS'}

def verify(out,require_blender=False):
 report=json.loads((out/(STEM+'_qa.json')).read_text(encoding='utf-8'))
 assert report['asset_id']=='M01-024'
 assert report['watertight_oriented_positive_volume']=='PASS'
 assert report['materials']>=3
 assert report['sampled_animation_frames']==73
 assert report['moving_vs_fixed_collisions']==0
 assert report['minimum_separation_aabb_m']>=.05
 assert report['mount'].startswith('MOUNT_FRONT_SOCKET (0,0,0)')
 objects,vs,faces,used=parse_obj(out/(STEM+'.obj'))
 mtl=(out/(STEM+'.mtl')).read_text(encoding='utf-8')
 names={line.split()[1] for line in mtl.splitlines() if line.startswith('newmtl ')}
 assert used==names and len(names)==7
 assert len(objects)==report['parts'] and len(objects)>=45
 assert len(vs)==report['vertices'] and len(faces)==report['triangles']
 for required in ('Drum_Cable_Coil_01','Drum_Rear_Flange','Drum_Front_Flange','Harpoon_Extending_Shaft','Harpoon_Piercing_Point','Anchor_Hook_01','Anchor_Hook_02','Anchor_Hook_03'):
  assert required in objects,required
 spec=importlib.util.spec_from_file_location('m01_024_generator',GEN)
 module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
 expected=module.build();cursor=0
 for p in expected:
  o=objects[p['name']]
  assert len(o['vertices'])==len(p['vertices'])
  assert len(o['faces'])==len(p['faces'])
  for a,b in zip(o['vertices'],p['vertices']):
   assert max(abs(a[i]-b[i]) for i in range(3))<5.1e-9
  for f,e in zip(o['faces'],p['faces']):assert f==tuple(x+cursor+1 for x in e)
  cursor+=len(p['vertices'])
 assert cursor==len(vs)
 result={'result':'PASS','parts':len(objects),'vertices':len(vs),'triangles':len(faces),
         'materials':len(names),'sampled_frames':73,'aabb_pair_checks':report['aabb_pair_checks'],
         'minimum_clearance_m':report['minimum_separation_aabb_m'],
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
 ap=argparse.ArgumentParser();ap.add_argument('--out',type=Path)
 ap.add_argument('--require-blender',action='store_true');args=ap.parse_args()
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