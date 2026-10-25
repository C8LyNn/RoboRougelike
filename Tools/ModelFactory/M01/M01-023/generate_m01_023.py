#!/usr/bin/env python3
"""MODEL-01 / M01-023 — Wieżyczka o profilu klinowym.

Visual-only procedural Blender 4.x model. Y up, +Z forward, 1 unit = 1 m.
Mount pivot MOUNT_TOP_SOCKET is (0,0,0); turret yaw pivot (0,0.44,0).
Without bpy, only real static OBJ/MTL/QA files are written (not fake GLB).
Run: blender -b --python generate_m01_023.py -- --out generated --repeat
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID = 'M01-023'
STEM = 'M01-023_wedge_profile_turret'
COLLECTION = 'M01_023_WEDGE_TURRET'
PIVOT_Y = .44
FRAMES = 73
FPS = 60
MATS = {
 'Graphite_Armor': ((.052,.064,.085,1),.65,.43,(0,0,0),0),
 'Titanium_Frame': ((.39,.45,.50,1),.83,.28,(0,0,0),0),
 'Ivory_Ceramic': ((.80,.83,.76,1),.12,.32,(0,0,0),0),
 'Copper_Cooling': ((.62,.28,.11,1),.77,.30,(0,0,0),0),
 'Rubber_Seal': ((.024,.031,.039,1),.08,.72,(0,0,0),0),
 'Cyan_Emitter': ((.05,.54,.78,1),.18,.26,(.03,.47,.90),2.0),
 'Amber_Warning': ((.91,.35,.05,1),.30,.35,(.95,.24,.02),.6),
}

def sub(a,b):return tuple(a[i]-b[i] for i in range(3))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(a[i]*b[i] for i in range(3))
def part(name,material,v,f,moving=False):return {'name':name,'material':material,'vertices':v,'faces':f,'moving':moving}
def signed_volume(p):
 v=p['vertices'];return sum(dot(v[a],cross(v[b],v[c]))/6 for a,b,c in p['faces'])
def oriented(p):
 if signed_volume(p)<0:p['faces']=[(a,c,b) for a,b,c in p['faces']]
 return p

def box(name,material,x,y,z,sx,sy,sz,cut=.006,moving=False):
 assert min(sx,sy,sz)>0 and 0<cut<min(sx,sz)/2
 a,b=sx/2,sz/2
 xz=[(-a+cut,-b),(a-cut,-b),(a,-b+cut),(a,b-cut),(a-cut,b),(-a+cut,b),(-a,b-cut),(-a,-b+cut)]
 v=[(x+u,q,z+w) for q in (y-sy/2,y+sy/2) for u,w in xz]
 v.extend(((x,y-sy/2,z),(x,y+sy/2,z)))
 f=[]
 for i in range(8):
  j=(i+1)%8
  f.extend(((i,j,8+j),(i,8+j,8+i),(16,j,i),(17,8+i,8+j)))
 return oriented(part(name,material,v,f,moving))

def cylinder_y(name,material,x,z,y0,y1,r,segments=32,moving=False):
 assert y1>y0 and r>0
 v=[(x+r*math.cos(math.tau*j/segments),y,z+r*math.sin(math.tau*j/segments)) for y in (y0,y1) for j in range(segments)]
 v.extend(((x,y0,z),(x,y1,z)))
 f=[]
 for j in range(segments):
  k=(j+1)%segments
  f.extend(((j,k,segments+k),(j,segments+k,segments+j),(2*segments,k,j),(2*segments+1,segments+j,segments+k)))
 return oriented(part(name,material,v,f,moving))

def ring_y(name,material,x,z,y0,y1,inner,outer,segments=40,moving=False):
 assert y1>y0 and 0<inner<outer
 v=[];f=[]
 for y,r in ((y0,inner),(y0,outer),(y1,outer),(y1,inner)):
  v.extend((x+r*math.cos(math.tau*j/segments),y,z+r*math.sin(math.tau*j/segments)) for j in range(segments))
 for i in range(4):
  nxt=(i+1)%4
  for j in range(segments):
   k=(j+1)%segments;a=i*segments+j;b=i*segments+k;c=nxt*segments+k;d=nxt*segments+j
   f.extend(((a,b,c),(a,c,d)))
 return oriented(part(name,material,v,f,moving))

def sleeve_z(name,material,x,y,z0,z1,inner,outer,segments=36,moving=False):
 assert z1>z0 and 0<inner<outer
 v=[];f=[]
 for z,r in ((z0,inner),(z0,outer),(z1,outer),(z1,inner)):
  v.extend((x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for j in range(segments))
 for i in range(4):
  nxt=(i+1)%4
  for j in range(segments):
   k=(j+1)%segments;a=i*segments+j;b=i*segments+k;c=nxt*segments+k;d=nxt*segments+j
   f.extend(((a,b,c),(a,c,d)))
 return oriented(part(name,material,v,f,moving))

def wedge(name,material,outline,bottom,roof_rear,roof_front,moving=True):
 """Convex XZ footprint, variable roof height, closed 3D wedge."""
 assert len(outline)>=3
 bottom_rear,bottom_front=bottom if isinstance(bottom,tuple) else (bottom,bottom)
 assert min(roof_rear-bottom_rear,roof_front-bottom_front)>0
 n=len(outline)
 # Rear is -Z, front is +Z; top slopes toward the lower armored nose.
 v=[(x,bottom_rear+(bottom_front-bottom_rear)*(z+.48)/1.03,z) for x,z in outline]
 v.extend((x,roof_rear+(roof_front-roof_rear)*(z+.48)/1.03,z) for x,z in outline)
 f=[]
 for i in range(1,n-1):f.extend(((0,i+1,i),(n,n+i,n+i+1)))
 for i in range(n):
  j=(i+1)%n
  f.extend(((i,j,n+j),(i,n+j,n+i)))
 return oriented(part(name,material,v,f,moving))

def yaw(frame):
 return math.tau*(frame-1)/(FRAMES-1)
def rotate(v,angle):
 c,s=math.cos(angle),math.sin(angle)
 return [(c*x+s*z,y,-s*x+c*z) for x,y,z in v]

def build():
 p=[];add=p.append
 # Mount and fixed rotary pedestal: maximum fixed Y = 0.36 m.
 add(box('Mount_Top_Key','Titanium_Frame',0,.070,0,.44,.14,.42,.027))
 add(box('Mount_Elastomer_Gasket','Rubber_Seal',0,.157,0,.39,.036,.37,.017))
 add(box('Pedestal_Armored_Shoe','Graphite_Armor',0,.225,0,.72,.110,.70,.040))
 add(box('Pedestal_Ceramic_Front','Ivory_Ceramic',0,.244,.354,.53,.065,.030,.010))
 add(ring_y('Stationary_Bearing_Race','Titanium_Frame',0,0,.278,.335,.204,.322,48))
 add(ring_y('Bearing_Copper_Track','Copper_Cooling',0,0,.337,.356,.228,.282,48))
 add(cylinder_y('Stationary_Shaft','Graphite_Armor',0,0,.271,.349,.144,32))
 for i in range(8):
  a=math.tau*i/8
  add(cylinder_y(f'Pedestal_Bolt_{i+1:02d}','Amber_Warning',.295*math.cos(a),.295*math.sin(a),.326,.350,.017,12))
 # Rotating assembly starts at Y=0.455: >95 mm clear of pedestal.
 add(ring_y('Turret_Rotating_Slew_Ring','Titanium_Frame',0,0,.455,.505,.208,.316,48,True))
 add(ring_y('Turret_Lower_Rubber_Collar','Rubber_Seal',0,0,.508,.527,.213,.285,48,True))
 add(box('Turret_Internal_Spine','Titanium_Frame',0,.573,-.04,.65,.095,.76,.028,True))
 add(box('Turret_Central_Receiver','Graphite_Armor',0,.643,-.045,.47,.095,.65,.025,True))
 # Two separate, asymmetric-in-plan wedge armor shells (not a single solid cap).
 left=[(-.53,-.48),(-.085,-.48),(-.075,.31),(-.145,.55),(-.32,.55),(-.53,.25)]
 right=[(-x,z) for x,z in reversed(left)]
 add(wedge('Wedge_Armor_Left','Graphite_Armor',left,.627,.858,.710))
 add(wedge('Wedge_Armor_Right','Graphite_Armor',right,.627,.858,.710))
 # Distinct inset ceramic wedge plates above the armored shells.
 inner_l=[(-.465,-.385),(-.155,-.385),(-.145,.275),(-.205,.405),(-.34,.405),(-.465,.185)]
 inner_r=[(-x,z) for x,z in reversed(inner_l)]
 add(wedge('Wedge_Ceramic_Left','Ivory_Ceramic',inner_l,(.850,.713),.877,.740))
 add(wedge('Wedge_Ceramic_Right','Ivory_Ceramic',inner_r,(.850,.713),.877,.740))
 # Twin slanted accent strips and cooling vents create a legible wedge profile.
 for side,x in (('L',-.487),('R',.487)):
  add(box(f'Armor_Outer_Rail_{side}','Titanium_Frame',x,.724,-.11,.035,.070,.65,.012,True))
  add(box(f'Rear_Warning_Stripe_{side}','Amber_Warning',x*.83,.863,-.335,.040,.010,.150,.005,True))
  for j in range(4):
   add(box(f'Copper_Vent_{side}_{j+1:02d}','Copper_Cooling',x*.81,.865,-.24+j*.12,.064,.018,.025,.006,True))
 # Central narrow barrel, physical hollow muzzle and layered ceramic shroud.
 add(sleeve_z('Barrel_Inner_Tube','Titanium_Frame',0,.660,.22,1.03,.055,.084,40,True))
 add(sleeve_z('Barrel_Ceramic_Shroud','Ivory_Ceramic',0,.660,.40,.80,.089,.122,40,True))
 add(sleeve_z('Muzzle_Brake_Outer','Graphite_Armor',0,.660,.99,1.14,.084,.142,40,True))
 add(sleeve_z('Muzzle_Emitter_Rim','Cyan_Emitter',0,.660,1.126,1.15,.088,.120,40,True))
 add(sleeve_z('Barrel_Copper_Heat_Band','Copper_Cooling',0,.660,.785,.832,.085,.139,40,True))
 add(box('Rear_Ammo_Cassette','Graphite_Armor',0,.655,-.505,.315,.205,.195,.026,True))
 add(box('Rear_Cassette_Grip','Titanium_Frame',0,.773,-.525,.19,.042,.11,.012,True))
 for x,side in ((-.20,'L'),(.20,'R')):
  add(box(f'Optic_Pod_{side}','Titanium_Frame',x,.798,.365,.085,.060,.20,.010,True))
  add(box(f'Optic_Cyan_Lens_{side}','Cyan_Emitter',x,.797,.482,.053,.035,.018,.005,True))
 return p

def bounds(vertices):return tuple((min(v[i] for v in vertices),max(v[i] for v in vertices)) for i in range(3))
def topology(parts):
 names=set();nv=nf=0
 for p in parts:
  assert p['name'] not in names,p['name'];names.add(p['name'])
  assert p['material'] in MATS
  verts=p['vertices'];faces=p['faces'];assert verts and faces
  edges={};wind={}
  for a,b,c in faces:
   assert len({a,b,c})==3 and min(a,b,c)>=0 and max(a,b,c)<len(verts)
   n=cross(sub(verts[b],verts[a]),sub(verts[c],verts[a]));assert dot(n,n)>1e-17,(p['name'],'degenerate')
   for x,y in ((a,b),(b,c),(c,a)):
    e=tuple(sorted((x,y)));edges[e]=edges.get(e,0)+1;wind[e]=wind.get(e,0)+(1 if x<y else -1)
  assert all(n==2 for n in edges.values()),(p['name'],'non-manifold')
  assert all(n==0 for n in wind.values()),(p['name'],'inconsistent winding')
  assert signed_volume(p)>1e-10,(p['name'],'inverted volume',signed_volume(p))
  nv+=len(verts);nf+=len(faces)
 return {'parts':len(parts),'vertices':nv,'triangles':nf,'materials':len(MATS),
         'watertight_oriented_positive_volume':'PASS'}

def clearance(parts):
 fixed=[p for p in parts if not p['moving']];moving=[p for p in parts if p['moving']]
 fb=[(p['name'],bounds(p['vertices'])) for p in fixed]
 checks=0;minimum=1000.
 for frame in range(1,FRAMES+1):
  a=yaw(frame)
  for m in moving:
   mb=bounds(rotate(m['vertices'],a))
   for name,b in fb:
    gaps=[max(mb[i][0]-b[i][1],b[i][0]-mb[i][1]) for i in range(3)]
    gap=max(gaps);checks+=1
    assert gap>1e-6,('turret-pedestal collision',frame,m['name'],name,gaps)
    minimum=min(minimum,gap)
 assert minimum>=.09,minimum
 return {'sampled_animation_frames':FRAMES,'aabb_pair_checks':checks,
         'moving_parts':len(moving),'moving_vs_fixed_collisions':0,
         'minimum_separation_aabb_m':round(minimum,6),
         'turret_yaw_degrees':360,'clearance':'PASS (sampled conservative AABB; not continuous triangle CCD)'}

def export_obj(parts,out):
 obj=out/(STEM+'.obj');mtl=out/(STEM+'.mtl')
 lines=['# M01-023 Y-up +Z-forward; MOUNT_TOP_SOCKET (0,0,0)','mtllib '+mtl.name];offset=0
 for p in parts:
  lines.extend(('o '+p['name'],'usemtl '+p['material']))
  for x,y,z in p['vertices']:lines.append(f'v {x:.8f} {y:.8f} {z:.8f}')
  for a,b,c in p['faces']:lines.append(f'f {a+offset+1} {b+offset+1} {c+offset+1}')
  offset+=len(p['vertices'])
 obj.write_text('\n'.join(lines)+'\n',encoding='utf-8')
 m=['# M01-023 PBR-compatible OBJ material palette']
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  m.extend(('newmtl '+name,'Kd %.5f %.5f %.5f'%rgba[:3],
   'Ks %.5f %.5f %.5f'%(metal,metal,metal),'Ns %.3f'%((1-rough)*1000),
   'Ke %.5f %.5f %.5f'%tuple(min(1.,v*strength) for v in emission),
   'd %.4f'%rgba[3],'illum 2',''))
 mtl.write_text('\n'.join(m)+'\n',encoding='utf-8')
 return [obj.name,mtl.name]

def blender_export(parts,out):
 import bpy
 scene=bpy.context.scene
 # Delete only this model's objects, meshes, materials; repeat-safe.
 for col in list(bpy.data.collections):
  if col.name==COLLECTION:
   for obj in list(col.objects):bpy.data.objects.remove(obj,do_unlink=True)
   bpy.data.collections.remove(col)
 for mesh in list(bpy.data.meshes):
  if mesh.name.startswith('M01_023_') and mesh.users==0:bpy.data.meshes.remove(mesh)
 for mat in list(bpy.data.materials):
  if mat.name.startswith('M01_023_') and mat.users==0:bpy.data.materials.remove(mat)
 col=bpy.data.collections.new(COLLECTION);scene.collection.children.link(col)
 def empty(name,loc,parent=None):
  obj=bpy.data.objects.new(name,None);col.objects.link(obj);obj.location=loc
  if parent:obj.parent=parent
  obj.empty_display_size=.045
  return obj
 mount=empty('MOUNT_TOP_SOCKET',(0,0,0))
 turret=empty('WEDGE_TURRET_YAW',(0,PIVOT_Y,0),mount)
 mats={}
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  mat=bpy.data.materials.new('M01_023_'+name);mat.use_nodes=True
  bsdf=mat.node_tree.nodes.get('Principled BSDF')
  if bsdf:
   bsdf.inputs['Base Color'].default_value=rgba
   bsdf.inputs['Metallic'].default_value=metal
   bsdf.inputs['Roughness'].default_value=rough
   if 'Emission Color' in bsdf.inputs:bsdf.inputs['Emission Color'].default_value=(*emission,1.)
   if 'Emission Strength' in bsdf.inputs:bsdf.inputs['Emission Strength'].default_value=strength
  mat.diffuse_color=rgba;mats[name]=mat
 for p in parts:
  vertices=[(x,y-PIVOT_Y,z) for x,y,z in p['vertices']] if p['moving'] else p['vertices']
  mesh=bpy.data.meshes.new('M01_023_'+p['name']);mesh.from_pydata(vertices,[],p['faces']);mesh.update(calc_edges=True)
  obj=bpy.data.objects.new('M01_023_'+p['name'],mesh);col.objects.link(obj)
  obj.parent=turret if p['moving'] else mount;mesh.materials.append(mats[p['material']])
  # Faceted hard-surface armour; no indiscriminate smooth normals across bevels.
  for poly in mesh.polygons:poly.use_smooth=False
 scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
 for frame in range(1,FRAMES+1,3):
  turret.rotation_euler.y=yaw(frame)
  turret.keyframe_insert(data_path='rotation_euler',index=1,frame=frame)
 if turret.animation_data and turret.animation_data.action:
  action=turret.animation_data.action
  try:
   for fc in action.fcurves:
    for key in fc.keyframe_points:key.interpolation='LINEAR'
  except AttributeError:pass  # Blender 4.4+ layered actions differ.
 scene.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT')
 for obj in col.objects:obj.select_set(True)
 bpy.context.view_layer.objects.active=mount
 fbx=out/(STEM+'.fbx');glb=out/(STEM+'.glb');blend=out/(STEM+'.blend')
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',
                          add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False)
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_animations=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(blend))
 return {'blender_export':'PASS (files written; Unity visual inspection NOT RUN)',
         'blender_version':'.'.join(map(str,bpy.app.version)),
         'animated_files':[fbx.name,glb.name,blend.name],
         'animation_node':'WEDGE_TURRET_YAW','animation_keyframes':25}

def main():
 argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
 ap=argparse.ArgumentParser();ap.add_argument('--out',default='build/M01-023')
 ap.add_argument('--repeat',action='store_true');ap.add_argument('--self-test',action='store_true')
 args=ap.parse_args(argv)
 parts=build()
 report={'asset_id':ID,'model':'Wieżyczka o profilu klinowym',
         'mount':'MOUNT_TOP_SOCKET (0,0,0), Y-up, +Z-forward, 1 unit = 1 m',
         'motion':'WEDGE_TURRET_YAW at (0,0.44,0); full 360 degrees around local Y',
         'gameplay':'NOT IMPLEMENTED',**topology(parts),**clearance(parts),
         'blender_export':'NOT RUN','unity_import':'NOT RUN',
         'unity_editmode':'NOT RUN','unity_playmode':'NOT RUN','visual_inspection':'NOT RUN'}
 if args.self_test:print(json.dumps(report,ensure_ascii=False,indent=2));return
 out=Path(args.out).resolve();out.mkdir(parents=True,exist_ok=True)
 report['static_files']=export_obj(parts,out)
 try:import bpy
 except ImportError:report['backend']='plain-python-static-obj'
 else:
  report['backend']='blender-bpy';report.update(blender_export(parts,out))
  if args.repeat:
   first=(len(bpy.data.objects),len(bpy.data.materials),len([c for c in bpy.data.collections if c.name==COLLECTION]))
   report.update(blender_export(parts,out))
   second=(len(bpy.data.objects),len(bpy.data.materials),len([c for c in bpy.data.collections if c.name==COLLECTION]))
   assert first==second,('scene resource leak',first,second)
   report['blender_repeatability']='PASS (two builds in one Blender process)'
   report['blender_scene_counts']=list(second)
 (out/(STEM+'_qa.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps(report,ensure_ascii=False,indent=2))
if __name__=='__main__':main()