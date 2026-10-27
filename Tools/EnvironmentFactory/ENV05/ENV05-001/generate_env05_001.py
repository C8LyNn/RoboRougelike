#!/usr/bin/env python3
"""ENV05-001 original low-poly warning bollard. Blender Z up, -Y front, 1 unit=1 m.
Run: blender -b -t 2 --python-exit-code 1 --python generate_env05_001.py -- --out /tmp/env05-001 --self-test
CC0-1.0; no external meshes or textures; static visual prop only.
"""
import argparse, hashlib, json, sys
from pathlib import Path
import bpy, bmesh
from mathutils import Vector

PREFIX="ENV05_001_"
COLLECTION=PREFIX+"LIGHT_BOLLARD"
MATERIALS={
 "Graphite":((.070,.092,.110,1),.68,.32,0),
 "GalvanizedSteel":((.48,.54,.57,1),.72,.38,0),
 "AmberBeacon":((1,.33,.018,1),.08,.28,1.8)
}
def clear_own():
 col=bpy.data.collections.get(COLLECTION)
 if col:
  for obj in list(col.all_objects): bpy.data.objects.remove(obj,do_unlink=True)
  bpy.data.collections.remove(col)
 for mat in list(bpy.data.materials):
  if mat.name.startswith(PREFIX) and mat.users==0: bpy.data.materials.remove(mat)
 for mesh in list(bpy.data.meshes):
  if mesh.name.startswith(PREFIX) and mesh.users==0: bpy.data.meshes.remove(mesh)

def make_material(name,params):
 rgba,metal,rough,emission=params
 m=bpy.data.materials.new(PREFIX+name)
 m.diffuse_color=rgba; m.use_nodes=True
 p=m.node_tree.nodes.get("Principled BSDF")
 p.inputs["Base Color"].default_value=rgba
 p.inputs["Metallic"].default_value=metal
 p.inputs["Roughness"].default_value=rough
 if emission:
  p.inputs["Emission Color"].default_value=rgba
  p.inputs["Emission Strength"].default_value=emission
 return m

def attach(obj,name,col,root,mat):
 obj.name=PREFIX+name
 if obj.type=="MESH":
  obj.data.name=PREFIX+name+"_mesh"
  obj.data.materials.clear(); obj.data.materials.append(mat)
 for c in list(obj.users_collection): c.objects.unlink(obj)
 col.objects.link(obj); obj.parent=root
 return obj

def box(name,pos,dims,mat,col,root,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
 obj=bpy.context.object; obj.dimensions=dims
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=obj.modifiers.new("machined_chamfer","BEVEL")
  mod.width=bevel; mod.segments=1
  bpy.ops.object.modifier_apply(modifier=mod.name)
 return attach(obj,name,col,root,mat)

def bolt(name,pos,mat,col,root):
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.014,depth=.014,location=pos)
 return attach(bpy.context.object,name,col,root,mat)

def taper(col,root,mat):
 b,t=.112,.083; z0,z1=.125,.661
 verts=[(-b,-b,z0),(b,-b,z0),(b,b,z0),(-b,b,z0),
        (-t,-t,z1),(t,-t,z1),(t,t,z1),(-t,t,z1)]
 faces=[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
 mesh=bpy.data.meshes.new(PREFIX+"TaperedShell_mesh")
 mesh.from_pydata(verts,[],faces); mesh.update(); mesh.materials.append(mat)
 obj=bpy.data.objects.new(PREFIX+"TaperedShell",mesh)
 col.objects.link(obj); obj.parent=root

def build():
 clear_own()
 col=bpy.data.collections.new(COLLECTION)
 bpy.context.scene.collection.children.link(col)
 root=bpy.data.objects.new(PREFIX+"ROOT_PIVOT_GROUND",None)
 col.objects.link(root); root.location=(0,0,0); root.empty_display_size=.13
 mats={k:make_material(k,v) for k,v in MATERIALS.items()}
 dark,steel,amber=(mats[k] for k in ("Graphite","GalvanizedSteel","AmberBeacon"))
 box("BaseFoot",(0,0,.041),(.32,.32,.082),dark,col,root,.012)
 box("BaseReinforcement",(0,0,.105),(.265,.265,.048),steel,col,root,.006)
 for sx in (-1,1):
  for sy in (-1,1):
   bolt("BaseBolt_%s_%s"%(sx,sy),(sx*.117,sy*.117,.088),steel,col,root)
 taper(col,root,dark)
 box("FrontServicePanel",(0,-.116,.395),(.118,.010,.173),steel,col,root,.003)
 box("FrontServiceInset",(0,-.123,.395),(.075,.006,.112),dark,col,root,.0015)
 for label,x in (("L",-.101),("R",.101)):
  box("CornerRib_"+label,(x,-.076,.430),(.018,.022,.492),steel,col,root,.004)
 box("BeaconLowerGasket",(0,0,.674),(.195,.195,.038),dark,col,root,.008)
 box("SingleAmberBeaconLens",(0,0,.760),(.175,.175,.140),amber,col,root,.012)
 box("BeaconTopVisor",(0,0,.851),(.213,.213,.046),steel,col,root,.009)
 for label,x in (("L",-.109),("R",.109)):
  box("BeaconFrontGuard_"+label,(x,-.106,.762),(.016,.017,.147),dark,col,root,.003)
 return col

def inspect(col):
 meshes=[o for o in col.all_objects if o.type=="MESH"]
 tri=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
 used={m.name for o in meshes for m in o.data.materials if m}
 assert 200<=tri<=1500,("triangle_budget",tri)
 assert len(used)==3,("material_budget",used)
 assert sum("SingleAmberBeaconLens" in o.name for o in meshes)==1
 assert all(not(o.animation_data and o.animation_data.action) for o in col.all_objects)
 verts=[]
 for obj in meshes:
  assert not obj.modifiers,obj.name
  bm=bmesh.new(); bm.from_mesh(obj.data)
  assert all(e.is_manifold for e in bm.edges),obj.name
  assert all(f.calc_area()>1e-9 for f in bm.faces),obj.name
  bm.free()
  verts.extend(obj.matrix_world@v.co for v in obj.data.vertices)
 lo=[min(v[i] for v in verts) for i in range(3)]
 hi=[max(v[i] for v in verts) for i in range(3)]
 assert -.005<=lo[2]<=.005 and hi[2]<1.0,(lo,hi)
 assert max(abs(lo[0]),abs(hi[0]),abs(lo[1]),abs(hi[1]))<.25
 return {"mesh_count":len(meshes),"triangles":tri,"materials":sorted(used),
         "aabb_min_m":lo,"aabb_max_m":hi,"pivot_m":[0,0,0],
         "units":"1 unit = 1 m","axes":"Blender +Z up, -Y front; glTF Y up",
         "suggested_collider":"box 0.32 x 0.32 x 0.88 m, center 0,0,0.44",
         "animations":"NOT_APPLICABLE","unity":"NOT_RUN","windows":"NOT_RUN"}

def preview_setup(col):
 scene=bpy.context.scene
 scene.render.engine="CYCLES"; scene.cycles.samples=16
 scene.render.resolution_x=512; scene.render.resolution_y=512
 scene.render.resolution_percentage=100
 scene.render.image_settings.file_format="PNG"; scene.render.film_transparent=True
 scene.world.use_nodes=True
 scene.world.node_tree.nodes["Background"].inputs["Color"].default_value=(.42,.47,.55,1)
 scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value=.7
 for name,loc,energy in (("Key",(1.5,-2,2.7),450),("Fill",(-1.4,1,2),260)):
  data=bpy.data.lights.new(PREFIX+name,"AREA"); data.energy=energy; data.size=2.5
  obj=bpy.data.objects.new(PREFIX+name,data); col.objects.link(obj)
  obj.location=loc
  obj.rotation_euler=(Vector((0,0,.47))-obj.location).to_track_quat("-Z","Y").to_euler()
 data=bpy.data.cameras.new(PREFIX+"PreviewCamera")
 camera=bpy.data.objects.new(PREFIX+"PreviewCamera",data)
 col.objects.link(camera); data.type="ORTHO"; data.ortho_scale=1.25
 scene.camera=camera
 return camera

def sha(path):
 h=hashlib.sha256()
 with open(path,"rb") as f:
  for block in iter(lambda:f.read(1024*1024),b""): h.update(block)
 return h.hexdigest()

def main():
 argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
 p=argparse.ArgumentParser(); p.add_argument("--out",required=True)
 p.add_argument("--self-test",action="store_true")
 args=p.parse_args(argv); out=Path(args.out).resolve(); out.mkdir(parents=True,exist_ok=True)
 if args.self_test:
  sentinel=bpy.data.objects.new("ENV05_FOREIGN_QA_SENTINEL",None)
  bpy.context.scene.collection.objects.link(sentinel)
  first=inspect(build()); col=build(); metrics=inspect(col)
  assert first==metrics, ("non-deterministic regeneration",first,metrics)
  assert bpy.data.objects.get(sentinel.name) is not None, "foreign object deleted"
  bpy.data.objects.remove(sentinel,do_unlink=True)
 else:
  col=build(); metrics=inspect(col)
 camera=preview_setup(col)
 blend=out/"ENV05-001_light_bollard.blend"
 bpy.ops.wm.save_as_mainfile(filepath=str(blend))
 for name,loc in (("front_three_quarter",(1.7,-2,1.42)),
                  ("rear_three_quarter",(-1.7,1.85,1.4))):
  camera.location=loc
  camera.rotation_euler=(Vector((0,0,.45))-camera.location).to_track_quat("-Z","Y").to_euler()
  bpy.context.scene.render.filepath=str(out/("ENV05-001_"+name+".png"))
  bpy.ops.render.render(write_still=True)
 bpy.ops.object.select_all(action="DESELECT")
 for obj in col.all_objects:
  if obj.type in ("MESH","EMPTY"): obj.select_set(True)
 glb=out/"ENV05-001_light_bollard.glb"
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",use_selection=True,
                           export_apply=True,export_yup=True,export_animations=False,
                           export_cameras=False,export_lights=False)
 outputs=[blend,glb,out/"ENV05-001_front_three_quarter.png",
          out/"ENV05-001_rear_three_quarter.png"]
 assert all(p.is_file() and p.stat().st_size>1000 for p in outputs)
 report={"id":"ENV05-001","status":"BLENDER_QA_PASS",
         "blender_version":bpy.app.version_string,"self_test":args.self_test,
         "source_script_sha256":sha(Path(__file__)),"metrics":metrics,
         "outputs":{p.name:{"bytes":p.stat().st_size,"sha256":sha(p)} for p in outputs},
         "license":"CC0-1.0; entirely original, no external assets",
         "review":"MANUAL_ART_REVIEW_PENDING"}
 (out/"ENV05-001_qa.json").write_text(json.dumps(report,indent=2)+"\n")
 print("ENV05-001 BLENDER_QA_PASS",json.dumps(metrics))
if __name__=="__main__": main()
