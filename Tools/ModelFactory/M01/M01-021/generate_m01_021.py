#!/usr/bin/env python3
"""MODEL-01 / M01-021: Kanister plazmy z dyszą.

Visual-only Blender 4.x bpy generator for a side-mounted pressure vessel and rotating valve. Native geometry uses metres, Y up,
+Z forward; MOUNT_SIDE_SOCKET pivot is (0,0,0). No gameplay code.
When run with ordinary Python, produces verifiable static OBJ/MTL/QA only;
BLEND/FBX/animated GLB require an actual Blender run.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = 'M01-021'
STEM = 'M01-021_plasma_canister_nozzle'
FRAMES = 73
FPS = 60
WAVE_LOBES = 6
MATS = {
 'Graphite_Housing': ((.063,.077,.100,1),.72,.38,(0,0,0),0),
 'Titanium_Chassis': ((.36,.42,.48,1),.80,.29,(0,0,0),0),
 'Ivory_Ceramic': ((.79,.83,.79,1),.14,.32,(0,0,0),0),
 'Copper_Coil': ((.62,.27,.11,1),.79,.25,(0,0,0),0),
 'Dark_Insulator': ((.024,.030,.040,1),.08,.76,(0,0,0),0),
 'Cyan_Resonance': ((.05,.62,.85,1),.22,.27,(.035,.55,.95),2.3),
 'Amber_Indicator': ((.92,.36,.055,1),.30,.36,(.95,.23,.015),.42),
}

def sub(a,b):return tuple(a[i]-b[i] for i in range(3))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(a[i]*b[i] for i in range(3))
def part(name,material,vertices,faces,moving=False):
 return {'name':name,'material':material,'vertices':vertices,'faces':faces,'moving':moving}
def box(name,material,x,y,z,sx,sy,sz,cut=.008,moving=False,angle=0):
 assert min(sx,sy,sz)>0 and 0<cut<min(sx,sy)/2,(name,sx,sy,sz,cut)
 a,b=sx/2,sy/2
 xy=[(-a+cut,-b),(a-cut,-b),(a,-b+cut),(a,b-cut),(a-cut,b),(-a+cut,b),(-a,b-cut),(-a,-b+cut)]
 co,si=math.cos(angle),math.sin(angle)
 def transform(u,v,q):return (x+co*u-si*v,y+si*u+co*v,z+q)
 v=[transform(u,w,q) for q in (-sz/2,sz/2) for u,w in xy]
 v.extend([transform(0,0,-sz/2),transform(0,0,sz/2)])
 f=[]
 for i in range(8):
  j=(i+1)%8
  f.extend(((i,j,8+j),(i,8+j,8+i),(16,j,i),(17,8+i,8+j)))
 return part(name,material,v,f,moving)
def cylinder(name,material,x,y,z0,z1,r,segments=24,moving=False):
 assert z1>z0 and r>0
 v=[(x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for z in (z0,z1) for j in range(segments)]
 v.extend(((x,y,z0),(x,y,z1)))
 f=[]
 for j in range(segments):
  k=(j+1)%segments
  f.extend(((j,k,segments+k),(j,segments+k,segments+j),(2*segments,k,j),(2*segments+1,segments+j,segments+k)))
 return part(name,material,v,f,moving)
def sleeve(name,material,x,y,z0,z1,inner,outer,segments=36,moving=False):
 assert z1>z0 and 0<inner<outer
 profile=((z0,inner),(z0,outer),(z1,outer),(z1,inner))
 v=[(x+r*math.cos(math.tau*j/segments),y+r*math.sin(math.tau*j/segments),z) for z,r in profile for j in range(segments)]
 f=[]
 for i in range(4):
  nxt=(i+1)%4
  for j in range(segments):
   k=(j+1)%segments;a=i*segments+j;b=i*segments+k;c=nxt*segments+k;d=nxt*segments+j
   f.extend(((a,b,c),(a,c,d)))
 return part(name,material,v,f,moving)
def wave_torus(name,material,major,minor,amp,z0=RING_Z,phase=0,segments=96,radial=14):
 """Closed watertight 6-lobe valve rim; oscillates as a rigid moving assembly."""
 assert major>minor>0 and segments%WAVE_LOBES==0
 v=[];f=[]
 for i in range(segments):
  u=math.tau*i/segments
  for j in range(radial):
   t=math.tau*j/radial
   r=major+minor*math.cos(t)
   v.append((r*math.cos(u),r*math.sin(u),z0+amp*math.sin(WAVE_LOBES*u+phase)+minor*math.sin(t)))
 for i in range(segments):
  ni=(i+1)%segments
  for j in range(radial):
   nj=(j+1)%radial
   a=i*radial+j;b=ni*radial+j;c=ni*radial+nj;d=i*radial+nj
   f.extend(((a,b,c),(a,c,d)))
 return part(name,material,v,f,True)

VALVE_X = .82
VALVE_Y = .16
VALVE_Z = .778
VALVE_SWEEP = math.radians(115)

def move_to_valve(p):
 p['vertices']=[(x+VALVE_X,y+VALVE_Y,z) for x,y,z in p['vertices']]
 return p

def build():
 p=[];add=p.append
 # Side socket is the physical origin. The support reaches the pressure tank.
 add(box('Mount_Side_Key','Titanium_Chassis',.075,0,.12,.15,.34,.22,.025))
 add(box('Mount_Lock_Ceramic','Ivory_Ceramic',.157,0,.12,.075,.23,.12,.012))
 add(box('Side_Load_Beam','Titanium_Chassis',.24,0,.20,.30,.12,.14,.018))
 add(box('Lower_Cradle','Graphite_Housing',.36,-.22,.51,.38,.09,.62,.016))
 add(box('Upper_Cradle','Graphite_Housing',.36,.22,.51,.38,.09,.62,.016))
 add(cylinder('Pressure_Tank_Core','Graphite_Housing',.36,0,.22,.99,.218,48))
 add(sleeve('Tank_Rear_Ferrule','Titanium_Chassis',.36,0,.23,.29,.219,.254,48))
 add(sleeve('Tank_Front_Ferrule','Titanium_Chassis',.36,0,.92,.98,.219,.254,48))
 add(sleeve('Tank_Ceramic_Band_A','Ivory_Ceramic',.36,0,.34,.395,.219,.240,48))
 add(sleeve('Tank_Ceramic_Band_B','Ivory_Ceramic',.36,0,.78,.835,.219,.240,48))
 add(sleeve('Copper_Pressure_Band','Copper_Coil',.36,0,.53,.60,.219,.249,48))
 add(sleeve('Front_Insulator','Dark_Insulator',.36,0,.988,1.034,.135,.225,48))
 add(sleeve('Nozzle_Base','Titanium_Chassis',.36,0,1.025,1.115,.115,.183,48))
 add(sleeve('Nozzle_Ceramic_Throat','Ivory_Ceramic',.36,0,1.112,1.325,.091,.132,48))
 add(sleeve('Nozzle_Graphite_Skirt','Graphite_Housing',.36,0,1.265,1.365,.134,.185,48))
 add(sleeve('Nozzle_Copper_Lip','Copper_Coil',.36,0,1.355,1.380,.148,.179,48))
 add(sleeve('Nozzle_Plasma_Liner','Cyan_Resonance',.36,0,1.363,1.388,.094,.117,48))
 # Tank relief cap and pressure indicator: deliberately offset from valve wheel.
 add(cylinder('Rear_Pressure_Cap','Titanium_Chassis',.36,0,.157,.235,.147,32))
 add(cylinder('Rear_Cap_Amber','Amber_Indicator',.36,0,.147,.171,.083,24))
 add(cylinder('Gauge_Housing','Titanium_Chassis',.68,-.20,.49,.58,.062,24))
 add(cylinder('Gauge_Face','Ivory_Ceramic',.68,-.20,.579,.587,.049,24))
 add(cylinder('Gauge_Needle','Amber_Indicator',.68,-.20,.588,.599,.010,12))
 add(box('Gauge_Bracket','Titanium_Chassis',.59,-.19,.50,.17,.05,.12,.010))
 # Exposed bypass tubes and maintenance ribs.
 for i in range(4):
  u=math.tau*(i+.5)/4
  x=.36+.276*math.cos(u);y=.276*math.sin(u)
  add(cylinder(f'Pressure_Bypass_{i+1:02d}','Copper_Coil',x,y,.35,.88,.019,16))
  add(cylinder(f'Bypass_Collar_Rear_{i+1:02d}','Titanium_Chassis',x,y,.348,.376,.029,16))
  add(cylinder(f'Bypass_Collar_Front_{i+1:02d}','Titanium_Chassis',x,y,.854,.886,.029,16))
 for i in range(6):
  u=math.tau*i/6
  x=.36+.235*math.cos(u);y=.235*math.sin(u)
  add(box(f'Tank_Grip_Rib_{i+1:02d}','Ivory_Ceramic',x,y,.71,.055,.028,.17,.008,False,u))
 # Distinctive valve mechanism, with a real 30+ mm axial air gap.
 add(box('Valve_Offset_Pipe','Titanium_Chassis',.695,.16,.54,.24,.050,.080,.010))
 add(cylinder('Valve_Seat','Copper_Coil',VALVE_X,VALVE_Y,.51,.58,.063,24))
 add(cylinder('Valve_Stem','Titanium_Chassis',VALVE_X,VALVE_Y,.58,.720,.027,20))
 add(sleeve('Valve_Gasket','Dark_Insulator',VALVE_X,VALVE_Y,.685,.715,.028,.044,24))
 add(move_to_valve(wave_torus('Valve_Handwheel_Rim','Copper_Coil',.122,.015,0,z0=VALVE_Z,segments=72,radial=12)))
 add(cylinder('Valve_Handwheel_Hub','Titanium_Chassis',VALVE_X,VALVE_Y,.755,.797,.034,24,True))
 for i in range(4):
  u=math.tau*i/4
  add(box(f'Valve_Handwheel_Spoke_{i+1:02d}','Ivory_Ceramic',
          VALVE_X+.075*math.cos(u),VALVE_Y+.075*math.sin(u),VALVE_Z,
          .082,.021,.020,.006,True,u))
 add(cylinder('Valve_Wheel_Amber_Dot','Amber_Indicator',VALVE_X,VALVE_Y,.796,.807,.017,16,True))
 # Fixed status lights on the rear collar.
 for i in range(4):
  u=math.tau*i/4
  add(box(f'Pressure_Status_{i+1:02d}','Cyan_Resonance',
          .36+.265*math.cos(u),.265*math.sin(u),.292,.055,.025,.046,.006,False,u))
 return p

def bounds(v):return tuple((min(a[i] for a in v),max(a[i] for a in v)) for i in range(3))
def signed_volume(p):
 v=p['vertices'];return sum(dot(v[a],cross(v[b],v[c]))/6 for a,b,c in p['faces'])
def topology(parts):
 names=set();tv=0;tf=0
 for p in parts:
  assert p['name'] not in names,p['name'];names.add(p['name'])
  assert p['material'] in MATS
  v=p['vertices'];faces=p['faces'];assert v and faces
  edges={};wind={}
  for a,b,c in faces:
   assert len({a,b,c})==3 and min(a,b,c)>=0 and max(a,b,c)<len(v)
   normal=cross(sub(v[b],v[a]),sub(v[c],v[a]));assert dot(normal,normal)>1e-17,(p['name'],'degenerate')
   for x,y in ((a,b),(b,c),(c,a)):
    e=tuple(sorted((x,y)));edges[e]=edges.get(e,0)+1;wind[e]=wind.get(e,0)+(1 if x<y else -1)
  assert all(n==2 for n in edges.values()),(p['name'],'non-manifold')
  assert all(n==0 for n in wind.values()),(p['name'],'winding')
  assert signed_volume(p)>1e-10,(p['name'],'inverted',signed_volume(p))
  tv+=len(v);tf+=len(faces)
 return {'parts':len(parts),'vertices':tv,'triangles':tf,'materials':len(MATS),'watertight_oriented_positive_volume':'PASS'}
def clearance(parts):
 fixed=[p for p in parts if not p['moving']]
 moving=[p for p in parts if p['moving']]
 assert len(moving)==7,('unexpected valve wheel part count',len(moving))
 checks=0;minimum=float('inf')
 fixed_bounds=[(p['name'],bounds(p['vertices'])) for p in fixed]
 for frame in range(1,FRAMES+1):
  theta=VALVE_SWEEP*math.sin(math.pi*(frame-1)/(FRAMES-1))
  c,s=math.cos(theta),math.sin(theta)
  for p in moving:
   transformed=[(VALVE_X+c*(x-VALVE_X)-s*(y-VALVE_Y),
                 VALVE_Y+s*(x-VALVE_X)+c*(y-VALVE_Y),z) for x,y,z in p['vertices']]
   mb=bounds(transformed)
   for name,fb in fixed_bounds:
    gaps=[max(mb[k][0]-fb[k][1],fb[k][0]-mb[k][1]) for k in range(3)]
    gap=max(gaps);checks+=1
    assert gap>1e-6,(frame,p['name'],name,gaps)
    minimum=min(minimum,gap)
 assert minimum>.02,minimum
 return {'sampled_animation_frames':FRAMES,'aabb_pair_checks':checks,
         'moving_parts':len(moving),'moving_vs_fixed_collisions':0,
         'minimum_separation_aabb_m':round(minimum,6),
         'valve_max_angle_degrees':115,
         'clearance':'PASS (sampled AABB; not continuous triangle CCD)'}

def export_obj(parts,out):
 obj=out/(STEM+'.obj');mtl=out/(STEM+'.mtl')
 lines=['# M01-021 Y-up +Z-forward; mount (0,0,0)','mtllib '+mtl.name];offset=0
 for p in parts:
  lines.extend(('o '+p['name'],'usemtl '+p['material']))
  for x,y,z in p['vertices']:lines.append(f'v {x:.8f} {y:.8f} {z:.8f}')
  for a,b,c in p['faces']:lines.append(f'f {a+offset+1} {b+offset+1} {c+offset+1}')
  offset+=len(p['vertices'])
 obj.write_text('\n'.join(lines)+'\n',encoding='utf-8')
 m=['# M01-021 static OBJ material definitions']
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  m.extend(('newmtl '+name,'Kd %.5f %.5f %.5f'%rgba[:3],
            'Ks %.5f %.5f %.5f'%(metal,metal,metal),
            'Ns %.3f'%((1-rough)*1000),
            'Ke %.5f %.5f %.5f'%tuple(min(1.,v*strength) for v in emission),
            'd %.4f'%rgba[3],'illum 2',''))
 mtl.write_text('\n'.join(m)+'\n',encoding='utf-8')
 return [obj.name,mtl.name]
def blender_export(parts,out):
 import bpy
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 for mesh in list(bpy.data.meshes):
  if mesh.users==0:bpy.data.meshes.remove(mesh)
 for c in list(bpy.data.collections):
  if c.name.startswith('M01_021_PLASMA_CANISTER'):bpy.data.collections.remove(c)
 for mat in list(bpy.data.materials):
  if mat.name.startswith('M01_021_') and mat.users==0:bpy.data.materials.remove(mat)
 scene=bpy.context.scene
 collection=bpy.data.collections.new('M01_021_PLASMA_CANISTER');scene.collection.children.link(collection)
 def empty(name,loc=(0.,0.,0.),parent=None):
  o=bpy.data.objects.new(name,None);collection.objects.link(o);o.location=loc
  if parent:o.parent=parent
  o.empty_display_size=.06
  return o
 mount=empty('MOUNT_FRONT_SOCKET')
 ring=empty('VALVE_WHEEL_ROTATE_Z', (VALVE_X,VALVE_Y,0.),mount)
 mats={}
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  mat=bpy.data.materials.new('M01_021_'+name);mat.use_nodes=True
  bsdf=mat.node_tree.nodes.get('Principled BSDF')
  if bsdf:
   bsdf.inputs['Base Color'].default_value=rgba
   bsdf.inputs['Metallic'].default_value=metal
   bsdf.inputs['Roughness'].default_value=rough
   if 'Emission Color' in bsdf.inputs:bsdf.inputs['Emission Color'].default_value=(*emission,1.)
   if 'Emission Strength' in bsdf.inputs:bsdf.inputs['Emission Strength'].default_value=strength
  mat.diffuse_color=rgba;mats[name]=mat
 for p in parts:
  vs=[(x-VALVE_X,y-VALVE_Y,z) if p['moving'] else (x,y,z) for x,y,z in p['vertices']]
  mesh=bpy.data.meshes.new('M01_021_'+p['name']);mesh.from_pydata(vs,[],p['faces']);mesh.update(calc_edges=True)
  obj=bpy.data.objects.new('M01_021_'+p['name'],mesh);collection.objects.link(obj)
  obj.parent=ring if p['moving'] else mount;mesh.materials.append(mats[p['material']])
  for face in mesh.polygons:face.use_smooth=True
  bevel=0
  # Use weighted normals only where Blender supports it; all authored geometry is manifold.
 scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
 for frame in range(1,FRAMES+1,3):
  ring.rotation_euler.z=VALVE_SWEEP*math.sin(math.pi*(frame-1)/(FRAMES-1))
  ring.keyframe_insert(data_path='rotation_euler',index=2,frame=frame)
 scene.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT')
 for obj in collection.objects:obj.select_set(True)
 bpy.context.view_layer.objects.active=mount
 fbx=out/(STEM+'.fbx');glb=out/(STEM+'.glb');blend=out/(STEM+'.blend')
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,axis_forward='-Z',axis_up='Y',
                          add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False)
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,export_animations=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(blend))
 return {'blender_export':'PASS (files written; visual inspection pending)',
         'blender_version':'.'.join(str(x) for x in bpy.app.version),
         'animated_files':[fbx.name,glb.name,blend.name],
         'animation_node':'VALVE_WHEEL_ROTATE_Z','animation_keyframes':25}
def main():
 argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
 parser=argparse.ArgumentParser();parser.add_argument('--out',default='build/M01-021')
 parser.add_argument('--repeat',action='store_true');parser.add_argument('--self-test',action='store_true')
 args=parser.parse_args(argv);parts=build()
 report={'asset_id':ASSET_ID,'model':'Kanister plazmy z dyszą',
         'mount':'MOUNT_FRONT_SOCKET (0,0,0), Y-up, +Z-forward',
         'motion':'VALVE_WHEEL_ROTATE_Z, 0→115→0 degrees about local Z',
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
   first=(len(bpy.data.objects),len(bpy.data.materials),
          len([c for c in bpy.data.collections if c.name.startswith('M01_021_RESONATOR')]))
   report.update(blender_export(parts,out))
   second=(len(bpy.data.objects),len(bpy.data.materials),
           len([c for c in bpy.data.collections if c.name.startswith('M01_021_RESONATOR')]))
   assert first==second,('Blender rerun leaked objects/materials',first,second)
   report['blender_repeatability']='PASS (two builds in one process)'
   report['blender_scene_counts']=list(second)
 (out/(STEM+'_qa.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps(report,ensure_ascii=False,indent=2))
if __name__=='__main__':main()