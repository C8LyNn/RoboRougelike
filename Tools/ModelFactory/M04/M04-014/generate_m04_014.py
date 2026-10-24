#!/usr/bin/env python3
"""M04-014 mud screw drive — isolated visual asset for Unity 6.

Unity metres; +Y up, +Z vehicle forward, +X right. The underside mount
plane is Y=0; every mesh is at/below it. Auger axis is +Z, at
(0,-0.36,0.52); animation rotates +Z about that local pivot.
No gameplay/physics code or scene edits. Reentrant Blender factory reset.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID='M04-014'
STEM='M04-014_mud_screw_drive'
CY=-0.36
PIVOT=(0.0,CY,0.52)
N=24
FPS=30
KEYS=((1,0.0),(31,90.0),(61,180.0),(91,270.0),(121,360.0))
MATS={
 'Graphite_Housing':((.055,.071,.084,1),.65,.37,(0,0,0),0),
 'Titanium_Satin':((.40,.48,.54,1),.80,.28,(0,0,0),0),
 'Copper_Worn':((.48,.22,.09,1),.70,.38,(0,0,0),0),
 'Ceramic_Ivory':((.78,.80,.74,1),.12,.35,(0,0,0),0),
 'Rubber_Seals':((.023,.030,.035,1),.03,.82,(0,0,0),0),
 'Cyan_Status':((.05,.58,.84,1),.10,.29,(.03,.42,.9),1.2),
 'Amber_Warning':((.94,.37,.035,1),.20,.39,(.85,.19,.01),.5),
 'Steel_Auger':((.32,.37,.40,1),.83,.32,(0,0,0),0),
}

def sub(a,b):return tuple(a[i]-b[i] for i in range(3))
def dot(a,b):return sum(a[i]*b[i] for i in range(3))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def unit(a):
 d=math.sqrt(dot(a,a));assert d>1e-12
 return tuple(v/d for v in a)

def mesh(name,material,vertices,faces,moving=False):
 assert material in MATS
 # Orient closed shell outward, preserving local manifold orientation.
 volume=sum(dot(vertices[a],cross(vertices[b],vertices[c]))/6 for a,b,c in faces)
 if volume<0:faces=[(a,c,b) for a,b,c in faces]
 return {'name':name,'material':material,'vertices':vertices,'faces':faces,'moving':moving}

def cyl(name,mat,x,y,z0,z1,r,n=N,moving=False):
 assert z1>z0 and r>0
 v=[(x+r*math.cos(2*math.pi*j/n),y+r*math.sin(2*math.pi*j/n),z)
    for z in (z0,z1) for j in range(n)]
 v.extend(((x,y,z0),(x,y,z1)))
 f=[]
 for j in range(n):
  k=(j+1)%n
  f.extend(((j,k,n+k),(j,n+k,n+j),(2*n,k,j),(2*n+1,n+j,n+k)))
 return mesh(name,mat,v,f,moving)

def ring(name,mat,x,y,z0,z1,ri,ro,n=N):
 assert z1>z0 and 0<ri<ro
 loops=((z0,ri),(z0,ro),(z1,ro),(z1,ri))
 v=[(x+r*math.cos(2*math.pi*j/n),y+r*math.sin(2*math.pi*j/n),z)
    for z,r in loops for j in range(n)]
 f=[]
 for k in range(4):
  for j in range(n):
   a=k*n+j;b=k*n+(j+1)%n;c=((k+1)%4)*n+(j+1)%n;d=((k+1)%4)*n+j
   f.extend(((a,b,c),(a,c,d)))
 return mesh(name,mat,v,f)

def box(name,mat,x0,x1,y0,y1,z0,z1):
 assert x0<x1 and y0<y1 and z0<z1
 v=[(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),
    (x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)]
 quads=((0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7))
 f=[tri for a,b,c,d in quads for tri in ((a,b,c),(a,c,d))]
 return mesh(name,mat,v,f)

def strut(name,mat,a,b,r=.014,n=12):
 axis=unit(sub(b,a));ref=(0,1,0) if abs(axis[1])<.85 else (1,0,0)
 u=unit(cross(ref,axis));w=cross(axis,u)
 v=[tuple(p[k]+r*(math.cos(2*math.pi*j/n)*u[k]+math.sin(2*math.pi*j/n)*w[k]) for k in range(3))
    for p in (a,b) for j in range(n)]
 v.extend((a,b));f=[]
 for j in range(n):
  k=(j+1)%n
  f.extend(((j,k,n+k),(j,n+k,n+j),(2*n,k,j),(2*n+1,n+j,n+k)))
 return mesh(name,mat,v,f)

def helix():
 """Watertight helical auger flight: 3 turns, swept thick radial ribbon."""
 turns=3
 samples=144
 z0=.135
 z1=.935
 ri=.115
 ro=.263
 thick=.019
 v=[]
 for i in range(samples+1):
  t=i/samples
  a=2*math.pi*turns*t
  z=z0+(z1-z0)*t
  co,si=math.cos(a),math.sin(a)
  for r,dz in ((ri,-thick/2),(ro,-thick/2),(ro,thick/2),(ri,thick/2)):
   v.append((r*co,CY+r*si,z+dz))
 f=[]
 for i in range(samples):
  for j in range(4):
   a=4*i+j;b=4*(i+1)+j;c=4*(i+1)+(j+1)%4;d=4*i+(j+1)%4
   f.extend(((a,b,c),(a,c,d)))
 f.extend(((0,1,2),(0,2,3)))
 q=4*samples
 f.extend(((q,q+2,q+1),(q,q+3,q+2)))
 return mesh('ROTOR_ThreeTurn_Spiral_Flight','Steel_Auger',v,f,True)

def build():
 p=[];add=p.append
 # Plate lies flush against the UNDERSIDE, not above the vehicle.
 add(box('MOUNT_Underside_Saddle','Graphite_Housing',-.33,.33,-.054,0,-.04,1.08))
 for side in (-1,1):
  x=side*.288
  add(box('Mount_Clamp_%s'%('L' if side<0 else 'R'),'Titanium_Satin',x-.031,x+.031,-.084,-.053,.055,1.005))
  for j,z in enumerate((.13,.52,.92)):
   add(cyl('Mount_Fastener_%s_%d'%('L' if side<0 else 'R',j),'Amber_Warning',x,-.05,z-.012,z+.012,.012,12))
 # Hollow guards at both ends of the screw; rotor never touches their faces.
 for i,(z0,z1) in enumerate(((.064,.093),(.977,1.006)),1):
  add(ring('Guard_Annulus_%d'%i,'Graphite_Housing',0,CY,z0,z1,.296,.346,40))
  add(ring('Guard_Ceramic_Rim_%d'%i,'Ceramic_Ivory',0,CY,z0+.002,z0+.010,.299,.343,40))
  add(ring('Guard_Copper_Trace_%d'%i,'Copper_Worn',0,CY,z1-.008,z1-.002,.325,.345,40))
 # Structural braces avoid the auger sweep (r<.263) by design.
 for side in (-1,1):
  x=side*.329
  for i,z in enumerate((.105,.965),1):
   add(strut('Outer_Brace_%s_%d'%('L' if side<0 else 'R',i),'Titanium_Satin',
       (side*.286,-.067,z),(x,CY,z),.014))
  add(strut('Cage_Longitudinal_Rail_%s'%('L' if side<0 else 'R'),
       'Titanium_Satin',(x,CY,.09),(x,CY,.98),.011))
 # Bearings are hollow, with 7mm radial gap to the animated 48mm shaft.
 for i,(z0,z1) in enumerate(((.025,.064),(1.006,1.048)),1):
  add(ring('Stationary_Shaft_Bearing_%d'%i,'Copper_Worn',0,CY,z0,z1,.055,.092,32))
  add(ring('Bearing_Rubber_Lip_%d'%i,'Rubber_Seals',0,CY,z0+.003,z0+.011,.055,.083,32))
  for side in (-1,1):
   add(strut('Bearing_Bridge_%d_%s'%(i,'L' if side<0 else 'R'),
       'Graphite_Housing',(side*.095,CY,(z0+z1)/2),
       (side*.300,CY,(z0+z1)/2),.013))
 # Entire rotating assembly is parented under a SINGLE axis pivot.
 add(cyl('ROTOR_Drive_Shaft','Titanium_Satin',0,CY,.015,1.055,.048,32,True))
 add(cyl('ROTOR_Auger_Core','Graphite_Housing',0,CY,.110,.961,.112,40,True))
 add(helix())
 for i,(z0,z1) in enumerate(((.110,.128),(.945,.961)),1):
  add(cyl('ROTOR_End_Hub_%d'%i,'Copper_Worn',0,CY,z0,z1,.114,32,True))
 # Status indicators are recessed in top saddle, not on rotating flight.
 for side in (-1,1):
  x=side*.245
  add(box('Saddle_Status_%s'%('L' if side<0 else 'R'),'Cyan_Status',
      x-.011,x+.011,-.055,-.053,.34,.66))
 return p

def validate(parts):
 names=set();vertices=0;triangles=0;moving=[]
 for p in parts:
  name=p['name'];assert name not in names;names.add(name)
  assert p['material'] in MATS
  v=p['vertices'];f=p['faces'];assert v and f
  assert all(all(math.isfinite(q) for q in vert) for vert in v)
  edges={};oriented={};vol=0
  for a,b,c in f:
   assert len({a,b,c})==3 and 0<=min(a,b,c) and max(a,b,c)<len(v)
   A,B,C=(v[k] for k in (a,b,c))
   assert dot(cross(sub(B,A),sub(C,A)),cross(sub(B,A),sub(C,A)))>1e-18,(name,'degenerate')
   vol+=dot(A,cross(B,C))/6
   for u,w in ((a,b),(b,c),(c,a)):
    edge=tuple(sorted((u,w)));edges[edge]=edges.get(edge,0)+1
    oriented[(u,w)]=oriented.get((u,w),0)+1
  assert all(k==2 for k in edges.values()),(name,'nonmanifold')
  assert all(oriented.get((u,w),0)==1 and oriented.get((w,u),0)==1 for u,w in edges),(name,'winding')
  assert vol>1e-12,(name,'volume',vol)
  vertices+=len(v);triangles+=len(f)
  if p['moving']:moving.append(p)
  assert max(q[1] for q in v)<=.0000001,(name,'above mount plane')
 assert len(MATS)>=3
 assert len(moving)==5
 assert len(parts)>=35
 assert {p['name'] for p in moving}=={p['name'] for p in parts if p['name'].startswith('ROTOR_')}
 assert KEYS[0]==(1,0) and KEYS[-1]==(121,360)
 flight=next(p for p in moving if 'Spiral_Flight' in p['name'])
 max_r=max(math.hypot(x,y-CY) for x,y,z in flight['vertices'])
 flight_min_z=min(v[2] for v in flight['vertices'])
 flight_max_z=max(v[2] for v in flight['vertices'])
 radial_gap=.296-max_r
 axial_gap=min(flight_min_z-.093,.977-flight_max_z)
 assert radial_gap>.028,(radial_gap,'guard radial gap')
 assert axial_gap>.028,(axial_gap,'guard axial gap')
 # Check each sampled animation frame for the actual spiral vertices.
 checks=0
 for frame in range(1,122):
  a=2*math.pi*(frame-1)/120;c=math.cos(a);s=math.sin(a)
  for x,y,z in flight['vertices']:
   xx=c*x-s*(y-CY);yy=s*x+c*(y-CY)
   assert math.hypot(xx,yy)<.296-.028
   assert .093+.028<z<.977-.028
   checks+=1
 return {'id':ID,'result':'PASS','parts':len(parts),'vertices':vertices,
  'triangles':triangles,'materials':len(MATS),'moving_meshes':len(moving),
  'spiral_turns':3,'motion_vertex_checks':checks,
  'guard_radial_clearance_m':round(radial_gap,6),
  'guard_axial_clearance_m':round(axial_gap,6),
  'mount':'UNDERSIDE Y=0; entire asset Y<=0; +Y up; +Z forward',
  'pivot':list(PIVOT),'axis':'+Z','animation_frames':121,'fps':FPS,
  'manifold':'PASS','winding':'PASS','blender':'NOT RUN','unity':'NOT RUN'}

def static_export(parts,out,report):
 out.mkdir(parents=True,exist_ok=True)
 obj=out/(STEM+'.obj');mtl=out/(STEM+'.mtl')
 with obj.open('w',encoding='utf-8',newline='\n') as f:
  f.write('# M04-014 Blender/Unity metres, Y up, Z forward\nmtllib '+STEM+'.mtl\n')
  offset=1
  for p in parts:
   f.write('o '+p['name']+'\nusemtl '+p['material']+'\n')
   for v in p['vertices']:f.write('v %.9f %.9f %.9f\n'%tuple(v))
   for a,b,c in p['faces']:f.write('f %d %d %d\n'%(a+offset,b+offset,c+offset))
   offset+=len(p['vertices'])
 with mtl.open('w',encoding='utf-8',newline='\n') as f:
  for name,(rgba,metal,rough,emission,power) in MATS.items():
   f.write('newmtl '+name+'\nKd %.5f %.5f %.5f\nKs %.4f %.4f %.4f\nNs %.3f\nd 1\n\n'%(
    rgba[0],rgba[1],rgba[2],metal,metal,metal,100*(1-rough)))
 (out/(ID+'_manifest.json')).write_text(json.dumps(report,indent=2,sort_keys=True)+'\n',encoding='utf-8')

def blender_export(parts,out,report):
 import bpy
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for material,(rgba,metal,rough,emission,power) in MATS.items():
  m=bpy.data.materials.new(material);m.diffuse_color=rgba;m.use_nodes=True
  bs=m.node_tree.nodes.get('Principled BSDF')
  bs.inputs['Base Color'].default_value=rgba
  bs.inputs['Metallic'].default_value=metal
  bs.inputs['Roughness'].default_value=rough
  if power:
   if 'Emission Color' in bs.inputs:
    bs.inputs['Emission Color'].default_value=(*emission,1)
    bs.inputs['Emission Strength'].default_value=power
   elif 'Emission' in bs.inputs:bs.inputs['Emission'].default_value=(*emission,1)
 mount=bpy.data.objects.new('MOUNT_UNDERSIDE_Y0',None);bpy.context.collection.objects.link(mount)
 mount.location=(0,0,0)
 pivot=bpy.data.objects.new('ROTOR_Z_AXIS',None);bpy.context.collection.objects.link(pivot)
 pivot.location=PIVOT;pivot.parent=mount
 for p in parts:
  moving=p['moving']
  vertices=[sub(v,PIVOT) for v in p['vertices']] if moving else p['vertices']
  m=bpy.data.meshes.new(p['name']+'_mesh');m.from_pydata(vertices,[],p['faces']);m.update()
  o=bpy.data.objects.new(p['name'],m);bpy.context.collection.objects.link(o)
  o.parent=pivot if moving else mount
  m.materials.append(bpy.data.materials[p['material']])
  for poly in m.polygons:poly.use_smooth=not p['name'].startswith(('MOUNT_','Mount_','Saddle_'))
  if hasattr(m,'use_auto_smooth'):
   m.use_auto_smooth=True
   m.auto_smooth_angle=math.radians(40)
 for frame,deg in KEYS:
  pivot.rotation_euler=(0,0,math.radians(deg))
  pivot.keyframe_insert(data_path='rotation_euler',frame=frame,group='M04-014 auger')
 if pivot.animation_data and pivot.animation_data.action:
  for curve in pivot.animation_data.action.fcurves:
   for point in curve.keyframe_points:point.interpolation='LINEAR'
 scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=121;scene.render.fps=FPS
 scene.frame_set(1)
 blend=out/(STEM+'.blend');glb=out/(STEM+'.glb');fbx=out/(STEM+'.fbx')
 bpy.ops.wm.save_as_mainfile(filepath=str(blend))
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',export_animations=True)
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=False,add_leaf_bones=False)
 report['blender']='PASS (bpy save + GLB + FBX exports)'
 report['blender_files']=[p.name for p in (blend,glb,fbx)]
 return report

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--out',type=Path,default=Path('build/M04-014'))
 parser.add_argument('--self-test',action='store_true')
 args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:])
 parts=build();report=validate(parts)
 if args.self_test:
  print(json.dumps(report,indent=2));return
 try:import bpy
 except ImportError:bpy=None
 if bpy is not None:
  args.out.mkdir(parents=True,exist_ok=True)
  report=blender_export(parts,args.out,report)
 static_export(parts,args.out,report)
 print(json.dumps(report,indent=2))
if __name__=='__main__':main()
