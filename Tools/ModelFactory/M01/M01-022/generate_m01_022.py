#!/usr/bin/env python3
"""M01-022 / Samonaprowadzający zasobnik dronów.

Blender 4.x bpy visual-only generator. Six 2x3 launch cells and six separately
hinged armored doors, with stowed visual drones. Unity metres, Y up, +Z front;
MOUNT_TOP_SOCKET is the world-space origin. No gameplay/drone AI is implemented.
Run: blender -b --python generate_m01_022.py -- --repeat --out generated
Without Blender: deterministic OBJ/MTL and numerical QA, no fake GLB/FBX.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID='M01-022'
STEM='M01-022_homing_drone_pod'
FRAMES=73
FPS=60
OPEN_DEG=68
COLS=(-.36,0.,.36)
ROWS=(.42,.81)
MATS={
 'Carbon_Graphite':((.052,.065,.081,1),.70,.40,(0,0,0),0),
 'Brushed_Titanium':((.34,.41,.49,1),.82,.29,(0,0,0),0),
 'Ceramic_Ivory':((.79,.83,.79,1),.12,.33,(0,0,0),0),
 'Oxidized_Copper':((.59,.24,.095,1),.72,.32,(0,0,0),0),
 'Rubber_Black':((.018,.025,.035,1),.05,.78,(0,0,0),0),
 'Signal_Cyan':((.035,.50,.76,1),.21,.31,(.035,.45,.9),1.8),
 'Warning_Amber':((.92,.38,.04,1),.25,.40,(.9,.23,.015),.5),
}

def sub(a,b):return tuple(a[i]-b[i] for i in range(3))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(a[i]*b[i] for i in range(3))
def part(name,material,vertices,faces,hinge=None):
 return {'name':name,'material':material,'vertices':vertices,'faces':faces,'hinge':hinge}
def box(name,material,x,y,z,sx,sy,sz,cut=.006,hinge=None):
 assert min(sx,sy,sz)>0 and 0<cut<min(sx,sy)/2,(name,sx,sy,sz,cut)
 a,b=sx/2,sy/2
 xy=[(-a+cut,-b),(a-cut,-b),(a,-b+cut),(a,b-cut),(a-cut,b),(-a+cut,b),(-a,b-cut),(-a,-b+cut)]
 v=[(x+u,y+w,z+q) for q in (-sz/2,sz/2) for u,w in xy]
 v.extend(((x,y,z-sz/2),(x,y,z+sz/2)))
 f=[]
 for i in range(8):
  j=(i+1)%8
  f.extend(((i,j,8+j),(i,8+j,8+i),(16,j,i),(17,8+i,8+j)))
 return part(name,material,v,f,hinge)
def cylinder(name,material,x,y,z0,z1,r,segments=24,hinge=None):
 assert z1>z0 and r>0
 v=[(x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for z in (z0,z1) for j in range(segments)]
 v.extend(((x,y,z0),(x,y,z1)))
 f=[]
 for j in range(segments):
  k=(j+1)%segments
  f.extend(((j,k,segments+k),(j,segments+k,segments+j),(2*segments,k,j),(2*segments+1,segments+j,segments+k)))
 return part(name,material,v,f,hinge)
def sleeve(name,material,x,y,z0,z1,inner,outer,segments=24,hinge=None):
 assert z1>z0 and 0<inner<outer
 profile=((z0,inner),(z0,outer),(z1,outer),(z1,inner))
 v=[(x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for z,r in profile for j in range(segments)]
 f=[]
 for i in range(4):
  nxt=(i+1)%4
  for j in range(segments):
   k=(j+1)%segments;a=i*segments+j;b=i*segments+k;c=nxt*segments+k;d=nxt*segments+j
   f.extend(((a,b,c),(a,c,d)))
 return part(name,material,v,f,hinge)
def hinge_position(idx):
 row,col=divmod(idx,3)
 return (COLS[col],ROWS[row]+.148,.900)
def pose(frame):
 # Doors swing out, with a brief hold open; easing guarantees closed endpoints.
 t=(frame-1)/(FRAMES-1)
 if t<.16: p=0
 elif t<.48: p=(t-.16)/.32
 elif t<.68: p=1
 else: p=1-(t-.68)/.32
 p=max(0.,min(1.,p));p=p*p*(3-2*p)
 return -math.radians(OPEN_DEG)*p

def build():
 p=[];add=p.append
 # Mount is exactly the root origin; structural load path to rear chassis.
 add(box('Mount_Top_Key','Brushed_Titanium',0,.070,.20,.48,.14,.34,.025))
 add(box('Mount_Rubber_Gasket','Rubber_Black',0,.155,.20,.37,.038,.27,.010))
 add(box('Mount_Cross_Beam','Brushed_Titanium',0,.205,.35,.91,.09,.13,.017))
 add(box('Rear_Backbone','Carbon_Graphite',0,.618,.405,1.20,.84,.36,.040))
 add(box('Rear_Heat_Sink','Brushed_Titanium',0,.62,.205,.91,.55,.055,.014))
 for x,side in ((-.595,'L'),(.595,'R')):
  add(box(f'Side_Armor_{side}','Ceramic_Ivory',x,.62,.47,.082,.89,.51,.015))
  add(box(f'Side_Conduit_{side}','Oxidized_Copper',x,.62,.747,.043,.70,.060,.009))
 for y,row in ((.220,'Lower'),(1.010,'Upper')):
  add(box(f'Cross_Rail_{row}','Brushed_Titanium',0,y,.610,1.06,.052,.19,.010))
  add(box(f'Cross_Rail_Accent_{row}','Warning_Amber',0,y,.718,.87,.015,.014,.004))
 # Six physical cell mouths, six visible stowed drones, six separate doors.
 for idx in range(6):
  row,col=divmod(idx,3);x=COLS[col];y=ROWS[row];tag=f'{idx+1:02d}'
  add(sleeve(f'Cell_Hex_Rim_{tag}','Brushed_Titanium',x,y,.615,.714,.112,.150,6))
  add(sleeve(f'Cell_Gasket_{tag}','Rubber_Black',x,y,.715,.756,.108,.133,6))
  add(sleeve(f'Cell_Cyan_Inner_{tag}','Signal_Cyan',x,y,.755,.775,.111,.124,6))
  add(cylinder(f'Drone_Hull_{tag}','Ceramic_Ivory',x,y,.738,.805,.074,20))
  add(cylinder(f'Drone_Sensor_{tag}','Signal_Cyan',x,y,.805,.823,.032,20))
  for s,dx in (('L',-.075),('R',.075)):
   add(box(f'Drone_Fin_{s}_{tag}','Carbon_Graphite',x+dx,y,.765,.065,.018,.057,.004))
  # Fixed hinge boss remains behind the moving door with an air gap.
  add(cylinder(f'Hinge_Bearing_{tag}','Oxidized_Copper',x,y+.149,.758,.817,.029,16))
  # Door surface begins at z=.870, outside all fixed geometry (max z=.823).
  add(box(f'Armored_Door_{tag}','Carbon_Graphite',x,y,.904,.300,.322,.058,.020,idx))
  add(box(f'Door_Ceramic_Face_{tag}','Ceramic_Ivory',x,y-.007,.944,.254,.260,.018,.013,idx))
  add(box(f'Door_Cyan_Status_{tag}','Signal_Cyan',x,y-.100,.958,.116,.021,.012,.004,idx))
  add(box(f'Door_Amber_Identifier_{tag}','Warning_Amber',x+.095,y+.086,.958,.026,.064,.012,.004,idx))
 # Rear antenna-like direction finder, offset outside door sweep.
 add(box('Telemetry_Ridge','Brushed_Titanium',0,1.075,.375,.58,.075,.15,.015))
 add(box('Telemetry_Cyan','Signal_Cyan',0,1.126,.397,.30,.018,.062,.005))
 return p

def bounds(v):return tuple((min(a[i] for a in v),max(a[i] for a in v)) for i in range(3))
def signed_volume(p):
 v=p['vertices'];return sum(dot(v[a],cross(v[b],v[c]))/6 for a,b,c in p['faces'])
def topology(parts):
 names=set();nv=nf=0
 for p in parts:
  assert p['name'] not in names,p['name'];names.add(p['name'])
  assert p['material'] in MATS
  v=p['vertices'];f=p['faces'];assert v and f
  edges={};winding={}
  for a,b,c in f:
   assert len({a,b,c})==3 and min(a,b,c)>=0 and max(a,b,c)<len(v)
   n=cross(sub(v[b],v[a]),sub(v[c],v[a]));assert dot(n,n)>1e-17,(p['name'],'degenerate')
   for i,j in ((a,b),(b,c),(c,a)):
    e=tuple(sorted((i,j)));edges[e]=edges.get(e,0)+1;winding[e]=winding.get(e,0)+(1 if i<j else -1)
  assert all(k==2 for k in edges.values()),(p['name'],'nonmanifold')
  assert all(k==0 for k in winding.values()),(p['name'],'inconsistent winding')
  assert signed_volume(p)>1e-10,(p['name'],'negative volume',signed_volume(p))
  nv+=len(v);nf+=len(f)
 return {'parts':len(parts),'vertices':nv,'triangles':nf,'materials':len(MATS),
         'watertight_oriented_positive_volume':'PASS'}
def transform(p,frame):
 if p['hinge'] is None:return p['vertices']
 px,py,pz=hinge_position(p['hinge']);a=pose(frame);c,s=math.cos(a),math.sin(a)
 return [(x,py+c*(y-py)-s*(z-pz),pz+s*(y-py)+c*(z-pz)) for x,y,z in p['vertices']]
def clearance(parts):
 fixed=[p for p in parts if p['hinge'] is None]
 moving=[p for p in parts if p['hinge'] is not None]
 assert len(moving)==24
 fb=[(p['name'],bounds(p['vertices'])) for p in fixed]
 checks=0;minimum=1000.
 for frame in range(1,FRAMES+1):
  for m in moving:
   mb=bounds(transform(m,frame))
   for name,b in fb:
    gaps=[max(mb[i][0]-b[i][1],b[i][0]-mb[i][1]) for i in range(3)]
    gap=max(gaps);checks+=1
    assert gap>1e-6,('moving-fixed collision',frame,m['name'],name,gaps)
    minimum=min(minimum,gap)
 # Doors belong to separate 2x3 cells; test door groups, not intentional
 # overlapping faceplate and decals belonging to the same door.
 for frame in range(1,FRAMES+1):
  mb=[(p['hinge'],p['name'],bounds(transform(p,frame))) for p in moving]
  for i in range(len(mb)):
   for j in range(i+1,len(mb)):
    if mb[i][0]==mb[j][0]:continue
    gap=max(max(mb[i][2][k][0]-mb[j][2][k][1],mb[j][2][k][0]-mb[i][2][k][1]) for k in range(3))
    checks+=1
    assert gap>1e-6,('door-door collision',frame,mb[i][1],mb[j][1])
    minimum=min(minimum,gap)
 assert minimum>.008,minimum
 return {'sampled_animation_frames':FRAMES,'aabb_pair_checks':checks,'moving_parts':len(moving),
         'moving_vs_fixed_collisions':0,'moving_vs_other_door_collisions':0,
         'minimum_separation_aabb_m':round(minimum,6),'door_open_degrees':OPEN_DEG,
         'clearance':'PASS (sampled conservative AABB, not continuous triangle CCD)'}
def export_obj(parts,out):
 obj=out/(STEM+'.obj');mtl=out/(STEM+'.mtl')
 lines=['# M01-022 Y-up +Z-forward; mount (0,0,0)','mtllib '+mtl.name];offset=0
 for p in parts:
  lines.extend(('o '+p['name'],'usemtl '+p['material']))
  for x,y,z in p['vertices']:lines.append(f'v {x:.8f} {y:.8f} {z:.8f}')
  for a,b,c in p['faces']:lines.append(f'f {a+offset+1} {b+offset+1} {c+offset+1}')
  offset+=len(p['vertices'])
 obj.write_text('\n'.join(lines)+'\n',encoding='utf-8')
 lines=['# M01-022 material palette']
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  lines.extend(('newmtl '+name,'Kd %.5f %.5f %.5f'%rgba[:3],
   'Ks %.5f %.5f %.5f'%(metal,metal,metal),'Ns %.3f'%((1-rough)*1000),
   'Ke %.5f %.5f %.5f'%tuple(min(1.,v*strength) for v in emission),
   'd %.4f'%rgba[3],'illum 2',''))
 mtl.write_text('\n'.join(lines)+'\n',encoding='utf-8')
 return [obj.name,mtl.name]
def blender_export(parts,out):
 import bpy
 # Delete only our own collection and owned datablocks; preserve foreign scene.
 scene=bpy.context.scene
 for col in list(bpy.data.collections):
  if col.name=='M01_022_DRONE_POD':
   for obj in list(col.objects):bpy.data.objects.remove(obj,do_unlink=True)
   bpy.data.collections.remove(col)
 for mat in list(bpy.data.materials):
  if mat.name.startswith('M01_022_') and mat.users==0:bpy.data.materials.remove(mat)
 col=bpy.data.collections.new('M01_022_DRONE_POD');scene.collection.children.link(col)
 def empty(name,loc=(0,0,0),parent=None):
  obj=bpy.data.objects.new(name,None);col.objects.link(obj);obj.location=loc
  if parent:obj.parent=parent
  obj.empty_display_size=.045
  return obj
 root=empty('MOUNT_TOP_SOCKET')
 hinges=[empty(f'DOOR_{i+1:02d}_HINGE_X',hinge_position(i),root) for i in range(6)]
 mats={}
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  mat=bpy.data.materials.new('M01_022_'+name);mat.use_nodes=True
  bsdf=mat.node_tree.nodes.get('Principled BSDF')
  if bsdf:
   bsdf.inputs['Base Color'].default_value=rgba
   bsdf.inputs['Metallic'].default_value=metal
   bsdf.inputs['Roughness'].default_value=rough
   if 'Emission Color' in bsdf.inputs:bsdf.inputs['Emission Color'].default_value=(*emission,1.)
   if 'Emission Strength' in bsdf.inputs:bsdf.inputs['Emission Strength'].default_value=strength
  mat.diffuse_color=rgba;mats[name]=mat
 for p in parts:
  verts=p['vertices']
  if p['hinge'] is not None:
   px,py,pz=hinge_position(p['hinge'])
   verts=[(x-px,y-py,z-pz) for x,y,z in verts]
  mesh=bpy.data.meshes.new('M01_022_'+p['name'])
  mesh.from_pydata(verts,[],p['faces']);mesh.update(calc_edges=True)
  obj=bpy.data.objects.new('M01_022_'+p['name'],mesh);col.objects.link(obj)
  obj.parent=hinges[p['hinge']] if p['hinge'] is not None else root
  mesh.materials.append(mats[p['material']])
  for polygon in mesh.polygons:polygon.use_smooth=True
 scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
 for frame in range(1,FRAMES+1,3):
  for hinge in hinges:
   hinge.rotation_euler.x=pose(frame)
   hinge.keyframe_insert(data_path='rotation_euler',index=0,frame=frame)
 scene.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT')
 for obj in col.objects:obj.select_set(True)
 bpy.context.view_layer.objects.active=root
 fbx=out/(STEM+'.fbx');glb=out/(STEM+'.glb');blend=out/(STEM+'.blend')
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',
                          add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False)
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_animations=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(blend))
 return {'blender_export':'PASS (files written; visual review pending)',
         'blender_version':'.'.join(map(str,bpy.app.version)),
         'animated_files':[fbx.name,glb.name,blend.name],
         'animation_nodes':[o.name for o in hinges],'animation_keyframes_per_hinge':25}
def main():
 argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
 ap=argparse.ArgumentParser();ap.add_argument('--out',default='build/M01-022')
 ap.add_argument('--repeat',action='store_true');ap.add_argument('--self-test',action='store_true')
 args=ap.parse_args(argv);parts=build()
 report={'asset_id':ASSET_ID,'model':'Samonaprowadzający zasobnik dronów',
         'mount':'MOUNT_TOP_SOCKET (0,0,0), Y-up, +Z-forward',
         'motion':'six DOOR_NN_HINGE_X; 0 to -68 to 0 degrees around local X',
         'visual_drone_cells':'2x3','gameplay_ai':'NOT IMPLEMENTED',
         **topology(parts),**clearance(parts),
         'blender_export':'NOT RUN','unity_import':'NOT RUN',
         'unity_editmode':'NOT RUN','unity_playmode':'NOT RUN',
         'visual_inspection':'NOT RUN'}
 if args.self_test:print(json.dumps(report,ensure_ascii=False,indent=2));return
 out=Path(args.out).resolve();out.mkdir(parents=True,exist_ok=True)
 report['static_files']=export_obj(parts,out)
 try:import bpy
 except ImportError:report['backend']='plain-python-static-obj'
 else:
  report['backend']='blender-bpy';report.update(blender_export(parts,out))
  if args.repeat:
   first=(len(bpy.data.objects),len(bpy.data.materials),len([c for c in bpy.data.collections if c.name=='M01_022_DRONE_POD']))
   report.update(blender_export(parts,out))
   second=(len(bpy.data.objects),len(bpy.data.materials),len([c for c in bpy.data.collections if c.name=='M01_022_DRONE_POD']))
   assert first==second,('repeat build leaked scene resources',first,second)
   report['blender_repeatability']='PASS (two builds in one Blender process)'
   report['blender_scene_counts']=list(second)
 (out/(STEM+'_qa.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps(report,ensure_ascii=False,indent=2))
if __name__=='__main__':main()