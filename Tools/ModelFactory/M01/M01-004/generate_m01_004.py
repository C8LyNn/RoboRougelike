#!/usr/bin/env python3
"""M01-004 — Cassette mortar (VISUAL ONLY). Blender Z up, -Y forward, metres.

Standalone CPython: deterministic static OBJ/MTL + 21-pose AABB QA.
Blender bpy: adds isolated collection, 0..18 degree elevation animation,
reproducible .blend and animated GLB with export limited to this asset.
No project gameplay, prefabs, scenes, or Unity settings are touched.
"""
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M01-004"
PIVOT_Z = 0.70
MAX_ELEVATION_DEG = 18.0
SAMPLES = 21
PALETTE = {
    "graphite": {"rgb": (.072, .091, .115), "metallic": .73, "roughness": .40},
    "gunmetal": {"rgb": (.27, .34, .39), "metallic": .86, "roughness": .28},
    "copper": {"rgb": (.74, .35, .13), "metallic": .83, "roughness": .27},
    "ceramic": {"rgb": (.78, .82, .78), "metallic": .14, "roughness": .42},
    "rubber": {"rgb": (.025, .030, .039), "metallic": .02, "roughness": .75},
    "signal_teal": {"rgb": (.035, .55, .72), "metallic": .35, "roughness": .24, "emission": 1.4},
}
PARTS = []


def add(name, material, group, vertices, faces, smooth=None, kind="mesh"):
    assert group in ("fixed", "tilt")
    assert material in PALETTE
    assert name not in {p["name"] for p in PARTS}, name
    PARTS.append({"name": name, "material": material, "group": group,
                  "vertices": vertices, "faces": faces,
                  "smooth": smooth or [False] * len(faces), "kind": kind})


def box(name, material, group, center, size):
    x, y, z = center
    sx, sy, sz = [v / 2.0 for v in size]
    v = [(x-sx,y-sy,z-sz),(x+sx,y-sy,z-sz),
         (x+sx,y+sy,z-sz),(x-sx,y+sy,z-sz),
         (x-sx,y-sy,z+sz),(x+sx,y-sy,z+sz),
         (x+sx,y+sy,z+sz),(x-sx,y+sy,z+sz)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),
         (3,7,6,2),(0,4,7,3),(1,2,6,5)]
    add(name, material, group, v, f, kind="box")


def tube_y(name, material, group, x, z, front_y, rear_y,
           outer_r, inner_r=0.0, segments=24):
    """Watertight ring (or capped cylinder), axis Y, face normals outward."""
    assert front_y < rear_y and outer_r > inner_r >= 0 and segments >= 12
    v = []
    for y, r in [(front_y,outer_r),(rear_y,outer_r)] + (
        [(front_y,inner_r),(rear_y,inner_r)] if inner_r else []):
        v.extend((x+r*math.cos(i*2*math.pi/segments), y,
                  z+r*math.sin(i*2*math.pi/segments)) for i in range(segments))
    faces, smooth = [], []
    n = segments
    for i in range(n):
        j = (i+1)%n
        faces.append((i,n+i,n+j,j))
        smooth.append(True)
        if inner_r:
            faces.append((2*n+i,2*n+j,3*n+j,3*n+i))
            smooth.append(True)
            faces.append((i,j,2*n+j,2*n+i))
            smooth.append(False)
            faces.append((n+i,3*n+i,3*n+j,n+j))
            smooth.append(False)
    if not inner_r:
        faces.extend([tuple(range(n)),tuple(reversed(range(n,2*n)))])
        smooth.extend([False,False])
    add(name,material,group,v,faces,smooth,kind="tube")


def build_parts():
    if PARTS:
        return PARTS
    # The true top SNAP pivot is at the rear/base plate centre (0,0,0).
    # Fixed pedestal ends below Z=0.47; all moving meshes clear it.
    box("Mount_TOP_SNAPPad", "gunmetal", "fixed", (0,0,.055), (.76,.55,.10))
    box("Mount_RubberIsolator", "rubber", "fixed", (0,0,.123), (.58,.42,.035))
    box("Pedestal_Spine", "graphite", "fixed", (0,.04,.255), (.29,.26,.25))
    for side in (-1,1):
        tag = "L" if side < 0 else "R"
        box("Pedestal_Cheek_"+tag, "gunmetal", "fixed",
            (side*.285,.075,.285), (.085,.31,.32))
        box("Pedestal_CopperRib_"+tag, "copper", "fixed",
            (side*.30,-.065,.275), (.025,.045,.22))
        for y in (-.17,.18):
            tube_y("Mount_Bolt_%s_%s"%(tag,"F" if y<0 else "B"),
                   "ceramic","fixed",side*.295,.073,y-.013,y+.013,.026)
    tube_y("Pedestal_CentralBearing","gunmetal","fixed",
           0,.375,-.08,.08,.088,.040,24)

    # Everything below is in coordinates LOCAL to the elevating pivot.
    # The animated parent is located at world Z=0.70.
    box("Breech_Block","graphite","tilt",(0,.16,0),(.31,.25,.21))
    box("Breech_TopPlate","ceramic","tilt",(0,.16,.124),(.27,.21,.025))
    box("Barrel_ReinforcedCollar","gunmetal","tilt",(0,-.18,0),(.33,.17,.25))
    tube_y("Barrel_Hollow","gunmetal","tilt",
           0,0,-.745,.125,.115,.074,32)
    tube_y("Barrel_MuzzleBrake","copper","tilt",
           0,0,-.815,-.735,.169,.120,32)
    tube_y("Barrel_FrontWearRing","ceramic","tilt",
           0,0,-.690,-.652,.146,.118,32)
    tube_y("Barrel_HeatRing","copper","tilt",
           0,0,-.445,-.417,.140,.119,32)
    tube_y("Barrel_RearLockRing","ceramic","tilt",
           0,0,-.012,.025,.140,.119,32)

    # Distinct boxy 2x3 ammunition cassette; no ammo gameplay created.
    box("Cassette_Housing","graphite","tilt",(.354,-.095,0),(.228,.41,.288))
    box("Cassette_Lid","ceramic","tilt",(.354,-.095,.153),(.224,.40,.020))
    box("Cassette_CopperSpine","copper","tilt",(.468,-.095,0),(.018,.37,.20))
    box("Cassette_LowerSkid","rubber","tilt",(.354,-.095,-.151),(.21,.37,.017))
    for col,x in enumerate((.298,.407)):
        for row,z in enumerate((-.088,0,.088)):
            tube_y("Cassette_Cell_%d_%d"%(col+1,row+1),
                   "gunmetal","tilt",x,z,-.331,-.304,.036,.018,16)
            tube_y("Cassette_Indicator_%d_%d"%(col+1,row+1),
                   "signal_teal","tilt",x,z,-.342,-.332,.016,0,16)

    # Left-side asymmetrical exposed guide and mechanical trim.
    box("Left_GuideRail","gunmetal","tilt",(-.248,-.235,-.025),(.065,.61,.064))
    box("Left_GuideCopperInlay","copper","tilt",(-.286,-.24,.017),(.014,.49,.018))
    box("Left_RearLock","ceramic","tilt",(-.248,.050,-.035),(.093,.065,.09))
    for i,y in enumerate((-.48,-.30,-.12)):
        box("Left_HeatFin_%02d"%i,"graphite","tilt",
            (-.191,y,.095),(.065,.035,.080))
    box("Breech_StatusRail","signal_teal","tilt",(-.05,.25,.116),(.17,.018,.013))
    return PARTS


def rotated_vertex(v, group, degrees):
    if group != "tilt":
        return v
    x,y,z = v
    a=math.radians(degrees)
    return (x,y*math.cos(a)-z*math.sin(a),
            PIVOT_Z+y*math.sin(a)+z*math.cos(a))


def aabb(part, degrees):
    points=[rotated_vertex(v,part["group"],degrees) for v in part["vertices"]]
    return tuple(tuple(fn(v[i] for v in points) for i in range(3))
                 for fn in (min,max))


def overlap_volume(a,b):
    return math.prod(max(0.0,min(a[1][i],b[1][i])-max(a[0][i],b[0][i]))
                     for i in range(3))


def face_area(vertices,face):
    # Triangulate polygon fan and sum triangle magnitudes.
    v0=vertices[face[0]]
    area=0.
    for i in range(1,len(face)-1):
        a=[vertices[face[i]][k]-v0[k] for k in range(3)]
        b=[vertices[face[i+1]][k]-v0[k] for k in range(3)]
        c=(a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
        area+=.5*math.sqrt(sum(t*t for t in c))
    return area


def validate():
    parts=build_parts()
    assert len(parts)==len({p["name"] for p in parts})
    assert len({p["material"] for p in parts})>=3
    faces=vertices=0
    for p in parts:
        v=p["vertices"]
        assert all(all(math.isfinite(q) for q in xyz) for xyz in v)
        for face in p["faces"]:
            assert len(face)>=3 and len(set(face))==len(face)
            assert all(0<=i<len(v) for i in face)
            assert face_area(v,face)>1e-10,(p["name"],face)
        vertices+=len(v)
        faces+=len(p["faces"])
    fixed=[p for p in parts if p["group"]=="fixed"]
    moving=[p for p in parts if p["group"]=="tilt"]
    checks=0
    min_vertical_clearance=float("inf")
    for i in range(SAMPLES):
        degrees=-MAX_ELEVATION_DEG*i/(SAMPLES-1)
        for m in moving:
            mb=aabb(m,degrees)
            for f in fixed:
                fb=aabb(f,degrees)
                assert overlap_volume(mb,fb)<1e-10,(
                    "moving/fixed penetration",m["name"],f["name"],degrees)
                checks+=1
            min_vertical_clearance=min(
                min_vertical_clearance,mb[0][2]-max(aabb(f,0)[1][2] for f in fixed))
    # All animated components remain strictly above all fixed components.
    assert min_vertical_clearance>=.015,min_vertical_clearance
    return {
        "id":ID,"status":"STATIC_QA_PASS","visual_only":True,
        "parts":len(parts),"vertices":vertices,"faces":faces,
        "materials_used":sorted({p["material"] for p in parts}),
        "pivot_blender_m":[0,0,0],"pivot_tilt_blender_m":[0,0,PIVOT_Z],
        "axis_blender":"Z up, -Y muzzle forward, +X right",
        "elevation_degrees":[0,-MAX_ELEVATION_DEG],
        "samples":SAMPLES,"moving_fixed_aabb_checks":checks,
        "min_vertical_clearance_m":round(min_vertical_clearance,6),
        "collision_test":"PASS (conservative AABB, not Unity physics)",
        "blender":"NOT RUN","unity":"NOT RUN","visual_review":"NOT RUN"
    }


def write_obj(out):
    parts=build_parts()
    obj=out/(ID+".obj")
    mtl=out/(ID+".mtl")
    with mtl.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M01-004 procedural material palette\n")
        for name,p in PALETTE.items():
            r,g,b=p["rgb"]
            f.write("newmtl %s\nKd %.6f %.6f %.6f\n"%(name,r,g,b))
            f.write("Ka 0.025 0.025 0.025\nKs 0.3 0.3 0.3\nNs 100\n")
            if p.get("emission"):
                f.write("Ke %.6f %.6f %.6f\n"%(r,g,b))
            f.write("\n")
    with obj.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M01-004 static 0 degree pose, Blender Z up\n")
        f.write("mtllib %s\n"%mtl.name)
        offset=1
        for p in parts:
            f.write("o %s\nusemtl %s\n"%(p["name"],p["material"]))
            for vertex in p["vertices"]:
                x,y,z=rotated_vertex(vertex,p["group"],0)
                f.write("v %.6f %.6f %.6f\n"%(x,y,z))
            for face in p["faces"]:
                f.write("f "+" ".join(str(offset+i) for i in face)+"\n")
            offset+=len(p["vertices"])
    return obj,mtl


def build_blender(out):
    import bpy
    from mathutils import Matrix
    scene=bpy.context.scene
    name=ID+"_ASSET"
    old=bpy.data.collections.get(name)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    # Remove orphaned meshes from prior runs of this generator only.
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(ID+"_") and mesh.users==0:
            bpy.data.meshes.remove(mesh)
    col=bpy.data.collections.new(name)
    scene.collection.children.link(col)
    mats={}
    for matname,p in PALETTE.items():
        key=ID+"_"+matname
        m=bpy.data.materials.get(key) or bpy.data.materials.new(key)
        m.diffuse_color=(*p["rgb"],1)
        m.use_nodes=True
        bsdf=m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=(*p["rgb"],1)
            bsdf.inputs["Metallic"].default_value=p["metallic"]
            bsdf.inputs["Roughness"].default_value=p["roughness"]
            if p.get("emission"):
                bsdf.inputs["Emission Color"].default_value=(*p["rgb"],1)
                bsdf.inputs["Emission Strength"].default_value=p["emission"]
        mats[matname]=m

    def empty(label,parent=None,location=(0,0,0)):
        obj=bpy.data.objects.new(ID+"_"+label,None)
        col.objects.link(obj)
        obj.empty_display_type="PLAIN_AXES"
        obj.empty_display_size=.11
        obj.parent=parent
        obj.location=location
        return obj

    mount=empty("MOUNT_TOP")
    mount["port"]="TOP_SOCKET"
    mount["units"]="metres"
    mount["forward_blender"]="-Y"
    mount["up_blender"]="+Z"
    pivot=empty("TILT_PIVOT",mount,(0,0,PIVOT_Z))
    pivot["axis"]="local X"
    pivot["pitch_range_degrees"]="0 to -18"
    muzzle=empty("MUZZLE_SOCKET",pivot,(0,-.815,0))
    muzzle["forward"]="-Y"
    cassette=empty("CASSETTE_SERVICE",pivot,(.354,-.095,.153))
    cassette["cells"]="2x3 decorative; no ammo mechanic"

    for p in build_parts():
        mesh=bpy.data.meshes.new(ID+"_"+p["name"]+"_mesh")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new(ID+"_"+p["name"],mesh)
        col.objects.link(obj)
        obj.parent=pivot if p["group"]=="tilt" else mount
        mesh.materials.append(mats[p["material"]])
        for poly,sm in zip(mesh.polygons,p["smooth"]):
            poly.use_smooth=sm
        if p["kind"]=="box":
            bevel=obj.modifiers.new("Machined_edges","BEVEL")
            bevel.width=.004
            bevel.segments=2
            bevel.affect="EDGES" if hasattr(bevel,"affect") else None
            normal=obj.modifiers.new("Weighted_face_normals","WEIGHTED_NORMAL")
            normal.keep_sharp=True
        obj["module_id"]=ID
        obj["animation_group"]=p["group"]

    for frame,angle in ((1,0),(20,-MAX_ELEVATION_DEG),(40,0)):
        pivot.rotation_euler.x=math.radians(angle)
        pivot.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    scene.frame_start=1
    scene.frame_end=40
    scene.render.fps=30
    scene.frame_set(1)
    # Preserve other assets: glTF exporter sees ONLY our collection.
    prev_selected=tuple(bpy.context.selected_objects)
    prev_active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in col.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=mount
        bpy.ops.export_scene.gltf(
            filepath=str(out/(ID+"_animated.glb")),
            export_format="GLB",use_selection=True,
            export_animations=True,export_yup=True)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in prev_selected:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if prev_active and prev_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=prev_active
    # Save the source .blend. For isolation run Blender --factory-startup.
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ID+".blend")))
    return {"glb":ID+"_animated.glb","blend":ID+".blend"}


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--output-dir",default=str(Path(__file__).resolve().parent))
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--require-blender",action="store_true")
    args=ap.parse_args(argv)
    out=Path(args.output_dir).resolve()
    out.mkdir(parents=True,exist_ok=True)
    qa=validate()
    write_obj(out)
    if not args.self_test:
        try:
            import bpy
        except ImportError:
            if args.require_blender:
                raise RuntimeError("Blender bpy missing: animated GLB and .blend NOT RUN")
        else:
            qa["blender_outputs"]=build_blender(out)
            qa["blender"]="EXECUTED"
    (out/(ID+".static-qa.json")).write_text(
        json.dumps(qa,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(qa,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
