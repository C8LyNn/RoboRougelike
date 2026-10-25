#!/usr/bin/env python3
"""MODEL-01 / M01-020: Krótki emitujący falę rezonator.

Visual-only Blender 4.x bpy generator. Native geometry uses metres, Y up,
+Z forward; MOUNT_FRONT_SOCKET pivot is (0,0,0). No gameplay code.
When run with ordinary Python, produces verifiable static OBJ/MTL/QA only;
BLEND/FBX/animated GLB require an actual Blender run.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = 'M01-020'
STEM = 'M01-020_short_wave_resonator'
FRAMES = 73
FPS = 60
RING_Z = 1.065
VIBRATION = .024
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
 """Closed watertight 6-lobe wavefront; oscillates as a rigid moving assembly."""
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

def build():
 p=[];add=p.append
 # Fixed load path: front mount at z=0 -> housing -> magnetic throat.
 add(box('Mount_Front_Key','Titanium_Chassis',0,0,.038,.43,.36,.076,.026))
 add(box('Mount_Center_Key','Dark_Insulator',0,0,.085,.21,.18,.09,.018))
 add(cylinder('Rear_Power_Block','Graphite_Housing',0,0,.10,.31,.175,32))
 add(sleeve('Rear_Bolt_Flange','Titanium_Chassis',0,0,.112,.150,.155,.207,48))
 add(cylinder('Main_Resonator_Body','Graphite_Housing',0,0,.29,.66,.185,48))
 add(sleeve('Main_Body_Ceramic_Rim','Ivory_Ceramic',0,0,.32,.36,.184,.211,48))
 add(sleeve('Rear_Induction_Coil','Copper_Coil',0,0,.386,.434,.185,.224,48))
 add(sleeve('Middle_Induction_Coil','Copper_Coil',0,0,.465,.513,.185,.224,48))
 add(sleeve('Forward_Induction_Coil','Copper_Coil',0,0,.544,.592,.185,.224,48))
 add(sleeve('Front_Throat_Gasket','Dark_Insulator',0,0,.640,.672,.136,.207,48))
 add(sleeve('Front_Throat_Ceramic','Ivory_Ceramic',0,0,.671,.728,.139,.204,48))
 add(sleeve('Front_Emitter_Aperture','Titanium_Chassis',0,0,.727,.844,.100,.142,48))
 add(sleeve('Aperture_Cyan_Liner','Cyan_Resonance',0,0,.830,.851,.104,.121,48))
 add(cylinder('Inner_Field_Emitter','Cyan_Resonance',0,0,.735,.798,.071,36))
 add(sleeve('Outer_Nose_Band','Graphite_Housing',0,0,.745,.809,.201,.255,48))
 add(sleeve('Nose_Amber_Rim','Amber_Indicator',0,0,.799,.812,.217,.241,48))
 # Six distinctive longitudinal scalloped waveguide horns and exposed bolts.
 for i in range(6):
  u=math.tau*i/6;co,si=math.cos(u),math.sin(u);tag=f'{i+1:02d}'
  add(box('Waveguide_Rib_'+tag,'Titanium_Chassis',.253*co,.253*si,.665,.172,.052,.36,.013,False,u))
  add(box('Ceramic_Wave_Plate_'+tag,'Ivory_Ceramic',.309*co,.309*si,.707,.137,.024,.263,.007,False,u))
  add(box('Copper_Conductor_'+tag,'Copper_Coil',.330*co,.330*si,.691,.105,.016,.190,.004,False,u))
  add(cylinder('Service_Bolt_'+tag,'Amber_Indicator',.175*co,.175*si,.153,.180,.019,12))
  add(box('Rear_Signal_'+tag,'Cyan_Resonance',.188*co,.188*si,.257,.058,.028,.044,.006,False,u))
 # Detached moving field ring is magnetically suspended ahead of the muzzle.
 # Its swept Z interval [0.966, 1.164] is separated from all fixed pieces
 # whose maximal Z is 0.887. Rigid oscillation has no penetration.
 add(wave_torus('WAVE_RING_MAIN','Titanium_Chassis',.348,.028,.042))
 add(wave_torus('WAVE_RING_CYAN','Cyan_Resonance',.399,.013,.042,phase=0))
 add(wave_torus('WAVE_RING_INNER','Ivory_Ceramic',.300,.013,.042,phase=0))
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
 fixed=[p for p in parts if not p['moving']];moving=[p for p in parts if p['moving']]
 assert len(moving)==3
 # Conservative AABB swept clearance, not triangle-triangle CCD.
 comparisons=0;mingap=float('inf')
 for frame in range(1,FRAMES+1):
  dz=VIBRATION*math.sin(math.tau*4*(frame-1)/(FRAMES-1))
  for m in moving:
   mb=bounds([(x,y,z+dz) for x,y,z in m['vertices']])
   for f in fixed:
    fb=bounds(f['vertices'])
    gaps=[max(mb[k][0]-fb[k][1],fb[k][0]-mb[k][1]) for k in range(3)]
    comparisons+=1
    assert max(gaps)>1e-6,(frame,m['name'],f['name'],gaps)
    mingap=min(mingap,max(gaps))
 assert mingap>.07,mingap
 return {'sampled_animation_frames':FRAMES,'aabb_pair_checks':comparisons,
         'moving_parts':len(moving),'moving_vs_fixed_collisions':0,
         'minimum_separation_aabb_m':round(mingap,6),'ring_vibration_amplitude_m':VIBRATION,
         'ring_vibration_cycles':4,'clearance':'PASS (sampled conservative AABB, not CCD)'}
def export_obj(parts,out):
 obj=out/(STEM+'.obj');mtl=out/(STEM+'.mtl')
 lines=['# M01-020 Y-up +Z-forward; mount (0,0,0)','mtllib '+mtl.name];offset=0
 for p in parts:
  lines.extend(('o '+p['name'],'usemtl '+p['material']))
  for x,y,z in p['vertices']:lines.append(f'v {x:.8f} {y:.8f} {z:.8f}')
  for a,b,c in p['faces']:lines.append(f'f {a+offset+1} {b+offset+1} {c+offset+1}')
  offset+=len(p['vertices'])
 obj.write_text('\n'.join(lines)+'\n',encoding='utf-8')
 m=['# M01-020 static OBJ material definitions']
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
  if c.name.startswith('M01_020_RESONATOR'):bpy.data.collections.remove(c)
 for mat in list(bpy.data.materials):
  if mat.name.startswith('M01_020_') and mat.users==0:bpy.data.materials.remove(mat)
 scene=bpy.context.scene
 collection=bpy.data.collections.new('M01_020_RESONATOR');scene.collection.children.link(collection)
 def empty(name,loc=(0.,0.,0.),parent=None):
  o=bpy.data.objects.new(name,None);collection.objects.link(o);o.location=loc
  if parent:o.parent=parent
  o.empty_display_size=.06
  return o
 mount=empty('MOUNT_FRONT_SOCKET')
 ring=empty('WAVE_RING_VIBRATE_Z', (0.,0.,RING_Z),mount)
 mats={}
 for name,(rgba,metal,rough,emission,strength) in MATS.items():
  mat=bpy.data.materials.new('M01_020_'+name);mat.use_nodes=True
  bsdf=mat.node_tree.nodes.get('Principled BSDF')
  if bsdf:
   bsdf.inputs['Base Color'].default_value=rgba
   bsdf.inputs['Metallic'].default_value=metal
   bsdf.inputs['Roughness'].default_value=rough
   if 'Emission Color' in bsdf.inputs:bsdf.inputs['Emission Color'].default_value=(*emission,1.)
   if 'Emission Strength' in bsdf.inputs:bsdf.inputs['Emission Strength'].default_value=strength
  mat.diffuse_color=rgba;mats[name]=mat
 for p in parts:
  vs=[(x,y,z-RING_Z) if p['moving'] else (x,y,z) for x,y,z in p['vertices']]
  mesh=bpy.data.meshes.new('M01_020_'+p['name']);mesh.from_pydata(vs,[],p['faces']);mesh.update(calc_edges=True)
  obj=bpy.data.objects.new('M01_020_'+p['name'],mesh);collection.objects.link(obj)
  obj.parent=ring if p['moving'] else mount;mesh.materials.append(mats[p['material']])
  for face in mesh.polygons:face.use_smooth=True
  bevel=0
  # Use weighted normals only where Blender supports it; all authored geometry is manifold.
 scene.frame_start=1;scene.frame_end=FRAMES;scene.render.fps=FPS
 for frame in range(1,FRAMES+1,3):
  ring.location.z=RING_Z+VIBRATION*math.sin(math.tau*4*(frame-1)/(FRAMES-1))
  ring.keyframe_insert(data_path='location',index=2,frame=frame)
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
         'animation_node':'WAVE_RING_VIBRATE_Z','animation_keyframes':25}
def main():
 argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
 parser=argparse.ArgumentParser();parser.add_argument('--out',default='build/M01-020')
 parser.add_argument('--repeat',action='store_true');parser.add_argument('--self-test',action='store_true')
 args=parser.parse_args(argv);parts=build()
 report={'asset_id':ASSET_ID,'model':'Krótki emitujący falę rezonator',
         'mount':'MOUNT_FRONT_SOCKET (0,0,0), Y-up, +Z-forward',
         'motion':'WAVE_RING_VIBRATE_Z, 4 cycles, ±24 mm along local Z',
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
          len([c for c in bpy.data.collections if c.name.startswith('M01_020_RESONATOR')]))
   report.update(blender_export(parts,out))
   second=(len(bpy.data.objects),len(bpy.data.materials),
           len([c for c in bpy.data.collections if c.name.startswith('M01_020_RESONATOR')]))
   assert first==second,('Blender rerun leaked objects/materials',first,second)
   report['blender_repeatability']='PASS (two builds in one process)'
   report['blender_scene_counts']=list(second)
 (out/(STEM+'_qa.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print(json.dumps(report,ensure_ascii=False,indent=2))
if __name__=='__main__':main()