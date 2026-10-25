#!/usr/bin/env python3
"""M03-015 generator protection cage: deterministic static OBJ and animated bpy GLB/FBX/BLEND.
Blender native Z-up; Unity scale 1m. No game mechanics or shared scene changes.
"""
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M03-015"
STEM = "M03-015_generator_protection_cage"
COLLECTION = "MODEL_M03_015"
MATS = {
    "GraphiteFrame": ((0.095, 0.135, 0.175, 1.0), 0.76, 0.34),
    "BrushedSteelBars": ((0.47, 0.54, 0.58, 1.0), 0.68, 0.28),
    "CeramicPads": ((0.80, 0.80, 0.70, 1.0), 0.10, 0.40),
    "CopperHinges": ((0.57, 0.28, 0.10, 1.0), 0.75, 0.29),
    "RubberLatches": ((0.030, 0.045, 0.055, 1.0), 0.02, 0.77),
    "AmberSafety": ((0.99, 0.47, 0.035, 1.0), 0.10, 0.26),
}
PARTS = []
HINGE = (-0.50, 0.475, 0.0)
DOOR_OPEN_DEG = 100.0
LATCH_RELEASE_DEG = 65.0

def volume(vs, fs):
    total = 0.0
    for f in fs:
        a = vs[f[0]]
        for j in range(1, len(f)-1):
            b, c = vs[f[j]], vs[f[j+1]]
            total += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                      a[1]*(b[2]*c[0]-b[0]*c[2]) +
                      a[2]*(b[0]*c[1]-b[1]*c[0])) / 6.0
    return total

def add(name, vs, fs, material, parent="STATIC"):
    assert material in MATS
    assert all(len(f) >= 3 for f in fs)
    assert len({tuple(round(c, 9) for c in v) for v in vs}) == len(vs), name
    if volume(vs, fs) < 0:
        fs = [tuple(reversed(f)) for f in fs]
    assert volume(vs, fs) > 1e-10, name
    PARTS.append((name, vs, fs, material, parent))

def box(name, center, size, material, parent="STATIC"):
    x,y,z=center
    a,b,c=(s/2 for s in size)
    vs=[(x+u*a,y+v*b,z+w*c) for u,v,w in
        [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
         (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),
        (3,7,6,2),(0,4,7,3),(1,2,6,5)]
    add(name,vs,fs,material,parent)

def tube(name, center, z0, z1, outer, inner, material, parent="STATIC", sides=24):
    assert 0 < inner < outer and z1 > z0
    cx,cy=center
    vs=[]
    for z,r in [(z0,outer),(z1,outer),(z0,inner),(z1,inner)]:
        vs += [(cx+r*math.cos(2*math.pi*i/sides),
                cy+r*math.sin(2*math.pi*i/sides),z) for i in range(sides)]
    fs=[]
    for i in range(sides):
        j=(i+1)%sides
        fs.extend([(i,j,sides+j,sides+i),
                   (2*sides+i,3*sides+i,3*sides+j,2*sides+j),
                   (sides+i,sides+j,3*sides+j,3*sides+i),
                   (i,2*sides+i,2*sides+j,j)])
    add(name,vs,fs,material,parent)

def build():
    PARTS.clear()
    # Cage dimensions: 1.12 wide, 0.86 deep, 1.04 tall.
    # Welded frame joints are intentional; there is no accidental self-intersection within a mesh.
    for ztag,z in [("Base",.08),("Roof",.99)]:
        for ytag,y in [("Rear",-.40),("Front",.40)]:
            box("Frame_%s_%s_X"%(ztag,ytag),(0,y,z),(1.12,.060,.060),"GraphiteFrame")
        for xtag,x in [("L",-.53),("R",.53)]:
            box("Frame_%s_%s_Y"%(ztag,xtag),(x,0,z),(.060,.86,.060),"GraphiteFrame")
    for xtag,x in [("L",.53),("R",-.53)]:
        for ytag,y in [("Rear",-.40),("Front",.40)]:
            box("Corner_%s_%s"%(xtag,ytag),(x,y,.535),(.064,.064,.91),"GraphiteFrame")
    # Open sides and rear: individual rods, with consistent clearance from the generator bay.
    for side,x in [("Left",-.53),("Right",.53)]:
        for idx,y in enumerate((-.25,-.10,.05,.20)):
            box("SideBar_%s_%02d"%(side,idx),(x,y,.535),(.023,.023,.82),"BrushedSteelBars")
    for idx,x in enumerate((-.38,-.19,0,.19,.38)):
        box("RearBar_%02d"%idx,(x,-.40,.535),(.024,.024,.82),"BrushedSteelBars")
    for idx,y in enumerate((-.24,0,.24)):
        box("RoofBar_%02d"%idx,(0,y,.99),(1.01,.020,.020),"BrushedSteelBars")
    for idx,x in enumerate((-.36,-.12,.12,.36)):
        box("FloorRail_%02d"%idx,(x,0,.075),(.030,.71,.030),"GraphiteFrame")
    for idx,(x,y) in enumerate(((-.39,-.26),(.39,-.26),(-.39,.26),(.39,.26))):
        box("MountCeramic_%02d"%idx,(x,y,.015),(.14,.14,.030),"CeramicPads")
        tube("MountBushing_%02d"%idx,(x,y),.030,.068,.044,.022,"CopperHinges")
    # Fixed hinge barrels occupy distinct Z intervals from door barrels.
    for idx,(z0,z1) in enumerate(((.18,.28),(.73,.83))):
        tube("HingeFixed_%02d"%idx,HINGE[:2],z0,z1,.030,.014,"CopperHinges")
    # Door parts are authored in hinge-local coordinates. Opening rotates about world +Z,
    # outward from +Y wall, with no sweep into the generator bay.
    for tag,x in [("Hinge",.035),("Latch",1.015)]:
        box("DoorRail_%s"%tag,(x,0,.53),(.035,.040,.78),"GraphiteFrame","DOOR")
    for tag,z in [("Bottom",.15),("Top",.91)]:
        box("DoorRail_%s"%tag,(.525,0,z),(1.02,.040,.035),"GraphiteFrame","DOOR")
    for idx,x in enumerate((.18,.34,.50,.66,.82)):
        box("DoorBar_%02d"%idx,(x,0,.53),(.021,.022,.70),"BrushedSteelBars","DOOR")
    for idx,(z0,z1) in enumerate(((.34,.43),(.58,.67))):
        tube("HingeDoor_%02d"%idx,(0,0),z0,z1,.030,.014,"CopperHinges","DOOR")
    # Latch pivots and lever geometry are kept on outer (+Y) face, not inside cage.
    for idx,z in enumerate((.32,.73)):
        box("LatchHousing_%02d"%idx,(.975,.050,z),(.085,.045,.095),"CeramicPads","DOOR")
        box("LatchHandle_%02d"%idx,(.015,.020,.040),(.025,.025,.085),
            "RubberLatches","LATCH_%02d"%idx)
        box("LatchTip_%02d"%idx,(.015,.023,.086),(.035,.032,.022),
            "CopperHinges","LATCH_%02d"%idx)
        box("LatchAmber_%02d"%idx,(.94,.087,z),(.036,.009,.034),"AmberSafety","DOOR")
    # Static keeper brackets sit clear of the door in its fully closed pose.
    for idx,z in enumerate((.32,.73)):
        box("Keeper_%02d"%idx,(.53,.405,z),(.048,.045,.10),"CopperHinges")
    for idx,x in enumerate((-.38,.38)):
        box("RoofAmber_%02d"%idx,(x,.401,1.025),(.12,.012,.014),"AmberSafety")
    return PARTS

def manifest(backend):
    return {
        "id":ID,"model":"Klatka ochronna generatora","backend":backend,
        "unit":"metre","axes":"Blender +Z up; glTF exporter converts to +Y up",
        "pivot":[0,0,0],"mount_axis":"+Z (inside chassis)",
        "snap":[{"name":"SNAP_INNER_%02d"%i,"position":[x,y,0],"axis":[0,0,-1]}
                for i,(x,y) in enumerate(((-.39,-.26),(.39,-.26),(-.39,.26),(.39,.26)))],
        "hinge_world":list(HINGE),"door_rotation_axis":"+Z",
        "door_closed_degrees":0,"door_open_degrees":DOOR_OPEN_DEG,
        "latch_release_degrees":LATCH_RELEASE_DEG,
        "keyframes":{"closed":1,"unlatch":12,"door_open":48,"hold":60,"door_closed":96,"latched":108},
        "mesh_count":len(PARTS),"materials":list(MATS),
        "intentional_contacts":"welded frame intersections; hinge and pad mounting surfaces",
        "unity_import":"NOT RUN"
    }

def write_obj(out):
    out.mkdir(parents=True,exist_ok=True)
    lines=["mtllib "+STEM+".mtl"]; index=1
    for name,vs,fs,mat,parent in PARTS:
        lines.extend(("o "+name,"usemtl "+mat))
        if parent == "DOOR":
            transformed=[(x+HINGE[0],y+HINGE[1],z+HINGE[2]) for x,y,z in vs]
        elif parent.startswith("LATCH_"):
            n=int(parent.split("_")[1])
            z=(.32,.73)[n]
            transformed=[(x+.975+HINGE[0],y+.095+HINGE[1],z0+z) for x,y,z0 in vs]
        else: transformed=vs
        lines.extend("v %.7f %.7f %.7f"%v for v in transformed)
        lines.extend("f "+" ".join(str(index+i) for i in face) for face in fs)
        index+=len(vs)
    (out/(STEM+".obj")).write_text("\n".join(lines)+"\n",encoding="utf-8")
    lines=[]
    for name,(color,metal,rough) in MATS.items():
        lines.extend(("newmtl "+name,"Kd %.6f %.6f %.6f"%color[:3],
                      "Ns %.4f"%(max(1,100*(1-rough))),"illum 2",""))
    (out/(STEM+".mtl")).write_text("\n".join(lines),encoding="utf-8")

def write_blender(out):
    import bpy
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for obj in list(old.objects):
            mesh=obj.data if obj.type=="MESH" else None
            bpy.data.objects.remove(obj,do_unlink=True)
            if mesh and mesh.users==0: bpy.data.meshes.remove(mesh)
        bpy.data.collections.remove(old)
    coll=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    root=bpy.data.objects.new("MOUNT_INNER_FRAME",None);coll.objects.link(root)
    door=bpy.data.objects.new("HINGE_DOOR_PIVOT_Z",None)
    coll.objects.link(door);door.parent=root;door.location=HINGE
    latches={}
    for i,z in enumerate((.32,.73)):
        name="LATCH_%02d"%i
        obj=bpy.data.objects.new(name+"_PIVOT_X",None)
        coll.objects.link(obj);obj.parent=door;obj.location=(.975,.095,z)
        latches[name]=obj
    materials={}
    for name,(color,metal,rough) in MATS.items():
        mat=bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(ID+"_"+name)
        mat.diffuse_color=color;mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=color
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
        materials[name]=mat
    for name,vs,fs,mat,parent in PARTS:
        mesh=bpy.data.meshes.new(ID+"_"+name)
        mesh.from_pydata(vs,[],fs);mesh.update()
        mesh.materials.append(materials[mat])
        obj=bpy.data.objects.new(name,mesh);coll.objects.link(obj)
        obj.parent=(door if parent=="DOOR" else latches[parent] if parent.startswith("LATCH_") else root)
        # Face-weighted normals not required for box edges; flat shading preserves hard edges.
        for polygon in mesh.polygons: polygon.use_smooth=False
    for item in manifest("bpy")["snap"]:
        obj=bpy.data.objects.new(item["name"],None)
        obj.empty_display_type="ARROWS";obj.empty_display_size=.065
        coll.objects.link(obj);obj.parent=root;obj.location=item["position"]
    for frame,deg in ((1,0),(12,0),(48,DOOR_OPEN_DEG),(60,DOOR_OPEN_DEG),(96,0),(108,0)):
        door.rotation_euler[2]=math.radians(deg)
        door.keyframe_insert(data_path="rotation_euler",index=2,frame=frame)
    for latch in latches.values():
        for frame,deg in ((1,0),(12,LATCH_RELEASE_DEG),(48,LATCH_RELEASE_DEG),
                          (60,LATCH_RELEASE_DEG),(96,LATCH_RELEASE_DEG),(108,0)):
            latch.rotation_euler[0]=math.radians(deg)
            latch.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=108
    bpy.context.scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in coll.objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.gltf(filepath=str(out/(STEM+".glb")),
        export_format="GLB",use_selection=True,export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(out/(STEM+".fbx")),
        use_selection=True,bake_anim=True,axis_forward="-Z",axis_up="Y")
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(STEM+".blend")))
    return len([o for o in coll.objects if o.type=="MESH"])

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M03-015")
    ns=parser.parse_args(argv);out=Path(ns.out).resolve()
    build();write_obj(out)
    try: import bpy
    except ImportError: backend="static"
    else:
        assert write_blender(out)==len(PARTS)
        backend="bpy"
    (out/(ID+"_manifest.json")).write_text(json.dumps(manifest(backend),indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(ID,backend,len(PARTS),"meshes",len(MATS),"materials")

if __name__=="__main__": main()
