#!/usr/bin/env python3
"""MODEL-01 / M01-024 — Mechaniczny harpun kotwiczący.

Visual-only procedural Blender 4.x model. Y up, +Z forward, 1 unit = 1 m.
Mount pivot MOUNT_FRONT_SOCKET is (0,0,0); turret yaw pivot (0,0.44,0).
Without bpy, only real static OBJ/MTL/QA files are written (not fake GLB).
Run: blender -b --python generate_m01_023.py -- --out generated --repeat
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID = 'M01-024'
STEM = 'M01-024_mechanical_anchor_harpoon'
COLLECTION = 'M01_024_ANCHOR_HARPOON'
EXTENSION = .24
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

def cylinder_z(name,material,x,y,z0,z1,r,segments=32,moving=False):
 assert z1>z0 and r>0
 v=[(x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for z in (z0,z1) for j in range(segments)]
 v.extend(((x,y,z0),(x,y,z1)))
 f=[]
 for j in range(segments):
  k=(j+1)%segments
  f.extend(((j,k,segments+k),(j,segments+k,segments+j),(2*segments,k,j),(2*segments+1,segments+j,segments+k)))
 return oriented(part(name,material,v,f,moving))

def ring_z(name,material,x,y,z0,z1,inner,outer,segments=40,moving=False):
 assert z1>z0 and 0<inner<outer
 v=[];f=[]
 for z,r in ((z0,inner),(z0,outer),(z1,outer),(z1,inner)):
  v.extend((x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for j in range(segments))
 for i in range(4):
  for j in range(segments):
   k=(j+1)%segments;a=i*segments+j;b=i*segments+k;c=((i+1)%4)*segments+k;d=((i+1)%4)*segments+j
   f.extend(((a,b,c),(a,c,d)))
 return oriented(part(name,material,v,f,moving))

def ring_x(name,material,y,z,x0,x1,inner,outer,segments=40):
 assert x1>x0 and 0<inner<outer
 v=[];f=[]
 for x,r in ((x0,inner),(x0,outer),(x1,outer),(x1,inner)):
  v.extend((x,y+r*math.cos(math.tau*j/segments),z+r*math.sin(math.tau*j/segments)) for j in range(segments))
 for i in range(4):
  for j in range(segments):
   k=(j+1)%segments;a=i*segments+j;b=i*segments+k;c=((i+1)%4)*segments+k;d=((i+1)%4)*segments+j
   f.extend(((a,b,c),(a,c,d)))
 return oriented(part(name,material,v,f))

def cone_z(name,material,x,y,z0,z1,r0,r1,segments=32,moving=False):
 assert z1>z0 and r0>r1>0
 v=[(x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for z,r in ((z0,r0),(z1,r1)) for j in range(segments)]
 v.extend(((x,y,z0),(x,y,z1)))
 f=[]
 for j in range(segments):
  k=(j+1)%segments
  f.extend(((j,k,segments+k),(j,segments+k,segments+j),(2*segments,k,j),(2*segments+1,segments+j,segments+k)))
 return oriented(part(name,material,v,f,moving))

def anchor_claw(name,material,angle):
 # Closed backward-raked anchor hook plate; radial/Z outline, 46mm tangential thickness.
 outline=[(.071,1.44),(.12,1.32),(.245,1.135),(.300,1.18),(.232,1.395),(.14,1.49)]
 c,s=math.cos(angle),math.sin(angle);thickness=.023
 v=[]
 for t in (-thickness,thickness):
  for r,z in outline:v.append((r*c-t*s,.58+r*s+t*c,z))
 n=len(outline);f=[]
 for j in range(1,n-1):f.extend(((0,j+1,j),(n,n+j,n+j+1)))
 for j in range(n):
  k=(j+1)%n;f.extend(((j,k,n+k),(j,n+k,n+j)))
 return oriented(part(name,material,v,f,True))

def extension(frame):
 # 0 to 240mm extension, hold, retract; all segments linear.
 keys=((1,0.),(19,EXTENSION),(43,EXTENSION),(61,0.),(73,0.))
 for (a,v),(b,w) in zip(keys,keys[1:]):
  if a<=frame<=b:return v+(w-v)*(frame-a)/(b-a)
 return 0.

def build():
 p=[];add=p.append
 # Front mounting socket at origin, 1 unit = 1 metre, Y-up and +Z-forward.
 add(box('Mount_Front_Key','Titanium_Frame',0,.43,.095,.49,.28,.18,.025))
 add(box('Mount_Rubber_Gasket','Rubber_Seal',0,.43,.203,.43,.25,.036,.015))
 add(box('Receiver_Graphite_Core','Graphite_Armor',0,.54,.42,.52,.34,.51,.040))
 add(box('Receiver_Titanium_Bottom','Titanium_Frame',0,.332,.45,.57,.050,.49,.017))
 add(box('Receiver_Ceramic_Top','Ivory_Ceramic',0,.744,.46,.44,.046,.35,.016))
 for side,x in (('L',-.282),('R',.282)):
  add(box(f'Side_Armor_{side}','Graphite_Armor',x,.54,.41,.047,.27,.44,.012))
  add(box(f'Copper_Vent_{side}','Copper_Cooling',x*1.125,.64,.54,.020,.095,.145,.006))
  for j in range(3):
   add(box(f'Warning_Rivet_{side}_{j}','Amber_Warning',x*1.15,.42+j*.09,.66,.016,.016,.010,.003))
 # Hollow guide sleeve; its stationary geometry ends behind the translating spear.
 add(ring_z('Guide_Titanium_Sleeve','Titanium_Frame',0,.58,.61,.842,.079,.143,40))
 add(ring_z('Guide_Ceramic_Muzzle','Ivory_Ceramic',0,.58,.807,.872,.085,.169,40))
 add(ring_z('Guide_Copper_Lockring','Copper_Cooling',0,.58,.743,.776,.145,.175,40))
 add(ring_z('Guide_Cyan_Alignment','Cyan_Emitter',0,.58,.861,.887,.093,.151,40))
 # Exposed side cable drum, wound copper coils and retaining flanges.
 add(box('Drum_Bracket','Titanium_Frame',.337,.505,.35,.17,.13,.27,.012))
 add(ring_x('Drum_Rear_Flange','Graphite_Armor',.57,.345,.385,.415,.038,.204,48))
 add(ring_x('Drum_Front_Flange','Graphite_Armor',.57,.345,.530,.560,.038,.204,48))
 add(ring_x('Drum_Central_Axle','Titanium_Frame',.57,.345,.385,.560,.020,.065,32))
 for j in range(7):
  xx=.422+j*.015
  add(ring_x(f'Drum_Cable_Coil_{j+1:02d}','Copper_Cooling',.57,.345,xx,xx+.009,.117,.131,36))
 for j in range(8):
  a=math.tau*j/8
  add(box(f'Drum_Flange_Rivet_{j+1:02d}','Titanium_Frame',.568,.57+.174*math.cos(a),.345+.174*math.sin(a),.018,.018,.018,.004))
 add(box('Cable_Lead_Upper','Copper_Cooling',.32,.738,.61,.032,.026,.35,.007))
 add(box('Cable_Lead_Clamp','Titanium_Frame',.31,.738,.80,.09,.052,.045,.008))
 # Translate all spear components together along +Z; no gameplay implementation.
 add(cylinder_z('Harpoon_Extending_Shaft','Titanium_Frame',0,.58,.952,1.555,.056,36,True))
 add(ring_z('Harpoon_Grip_Band','Rubber_Seal',0,.58,1.075,1.155,.058,.079,36,True))
 add(ring_z('Harpoon_Copper_Retention','Copper_Cooling',0,.58,1.305,1.350,.060,.092,36,True))
 add(cone_z('Harpoon_Piercing_Point','Ivory_Ceramic',0,.58,1.52,1.775,.104,.004,36,True))
 add(ring_z('Harpoon_Cyan_Tracer','Cyan_Emitter',0,.58,1.490,1.525,.062,.110,36,True))
 for j in range(3):
  a=math.tau*j/3
  add(anchor_claw(f'Anchor_Hook_{j+1:02d}','Graphite_Armor',a))
  add(cylinder_z(f'Anchor_Hinge_Pin_{j+1:02d}','Copper_Cooling',.102*math.cos(a),.58+.102*math.sin(a),1.397,1.430,.021,12,True))
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
  dz=extension(frame)
  for m in moving:
   mb=bounds([(x,y,z+dz) for x,y,z in m['vertices']])
   for name,b in fb:
    gaps=[max(mb[i][0]-b[i][1],b[i][0]-mb[i][1]) for i in range(3)]
    gap=max(gaps);checks+=1
    assert gap>1e-6,('harpoon housing collision',frame,m['name'],name,gaps)
    minimum=min(minimum,gap)
 assert minimum>=.05,minimum
 return {'sampled_animation_frames':FRAMES,'aabb_pair_checks':checks,
         'moving_parts':len(moving),'moving_vs_fixed_collisions':0,
         'minimum_separation_aabb_m':round(minimum,6),
         'harpoon_extension_m':EXTENSION,'clearance':'PASS (sampled conservative AABB; not continuous triangle CCD)'}

def export_obj(parts,out):
 obj=out/(STEM+'.obj');mtl=out/(STEM+'.mtl')
 lines=['# M01-024 Y-up +Z-forward; MOUNT_FRONT_SOCKET (0,0,0)','mtllib '+mtl.name];offset=0
 for p in parts:
  lines.extend(('o '+p['name'],'usemtl '+p['material']))
  for x,y,z in p['vertices']:lines.append(f'v {x:.8f} {y:.8f} {z:.8f}')
  for a,b,c in p['faces']:lines.append(f'f {a+offset+1} {b+offset+1} {c+offset+1}')
  offset+=len(p['vertices'])
 obj.write_text('\n'.join(lines)+'\n',encoding='utf-8')
 m=['# M01-024 PBR-compatible OBJ material palette']
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
  if mesh.name.startswith('M01_024_') and mesh.users==0:bpy.data.meshes.remove(mesh)
 for mat in list(bpy.data.materials):
  if mat.name.startswith('M01_024_') and mat.users==0:bpy.data.materials.remove(mat)
 col=bpy.data.collections.new(COLLECTION);scene.collection.children.link(col)
 def empty(name,loc,parent=None):
  obj=bpy.data.objects.new(name,None);col.objects.link(obj);obj.location=loc
  if parent:obj.parent=parent
  obj.empty_display_size=.045
  return obj
 mount=empty('MOUNT_FRONT_SOCKET',(0,0,0))
 turret=empty('HARPOON_TIP_EXTEND_Z',(0,0,0),mount)
 mats={}
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  mat=bpy.data.materials.new('M01_024_'+name);mat.use_nodes=True
  bsdf=mat.node_tree.nodes.get('Principled BSDF')
  if bsdf:
   bsdf.inputs['Base Color'].default_value=rgba
   bsdf.inputs['Metallic'].default_value=metal
   bsdf.inputs['Roughness'].default_value=rough
   if 'Emission Color' in bsdf.inputs:bsdf.inputs['Emission Color'].default_value=(*emission,1.)
   if 'Emission Strength' in bsdf.inputs:bsdf.inputs['Emission Strength'].default_value=strength
  mat.diffuse_color=rgba;mats[name]=mat
 for p in parts:
  vertices=p['vertices']
  mesh=bpy.data.meshes.new('M01_024_'+p['name']);mesh.from_pydata(vertices,[],p['faces']);mesh.update(calc_edges=True)
  obj=bpy.data.objects.new('M01_024_'+p['name'],mesh);col.objects.link(obj)
  obj.parent=turret if p['moving'] else mount;mesh.materials.append(mats[p['material']])
  # Faceted hard-surface armour; no indiscriminate smooth normals across bevels.
  for poly in mesh.polygons:poly.use_smooth=False
 scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
 for frame in (1,19,43,61,73):
  turret.location.z=extension(frame)
  turret.keyframe_insert(data_path='location',index=2,frame=frame)
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
         'animation_node':'HARPOON_TIP_EXTEND_Z','animation_keyframes':5}

def main():
 argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
 ap=argparse.ArgumentParser();ap.add_argument('--out',default='build/M01-024')
 ap.add_argument('--repeat',action='store_true');ap.add_argument('--self-test',action='store_true')
 args=ap.parse_args(argv)
 parts=build()
 report={'asset_id':ID,'model':'Mechaniczny harpun kotwiczący',
         'mount':'MOUNT_FRONT_SOCKET (0,0,0), Y-up, +Z-forward, 1 unit = 1 m',
         'motion':'HARPOON_TIP_EXTEND_Z at (0,0,0); linear +Z stroke 0.24m',
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