#!/usr/bin/env python3
"""M04-004: rear-frame vectoring jet nozzle, Blender 4.x/5.x procedural asset.

Pure Python: --self-test generates deterministic OBJ/MTL and conservative sweep QA.
Blender: blender --background --factory-startup --python generate_m04_004.py -- --output-dir ./out
Units: metres. Blender +Y = exhaust/aft, +Z = up. Rear mount pivot (0,0,0).
Yaw around Blender +Z, pitch around local +X. No Unity gameplay code.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M04-004"
PIVOT_Y = 0.46
YAW_MAX_DEG = 12.0
PITCH_MAX_DEG = 9.0
SEGMENTS = 32
PALETTE = {
    "gunmetal": {"rgb": (0.105, 0.135, 0.165), "metallic": 0.78, "roughness": 0.31},
    "titanium": {"rgb": (0.44, 0.51, 0.55), "metallic": 0.82, "roughness": 0.27},
    "ceramic": {"rgb": (0.73, 0.76, 0.70), "metallic": 0.08, "roughness": 0.46},
    "safety_yellow": {"rgb": (0.98, 0.57, 0.055), "metallic": 0.32, "roughness": 0.38},
    "ion_cyan": {"rgb": (0.03, 0.62, 0.91), "metallic": 0.2, "roughness": 0.22, "emission": 1.7},
}
PARTS = []


def add(name, group, material, verts, faces, smooth=False):
    assert group in ("fixed", "moving")
    assert material in PALETTE
    assert len(verts) >= 4 and faces
    assert all(0 <= i < len(verts) for f in faces for i in f)
    PARTS.append(dict(name=name, group=group, material=material,
                      vertices=[tuple(map(float, v)) for v in verts],
                      faces=[tuple(f) for f in faces], smooth=smooth))


def box(name, group, material, center, size):
    cx, cy, cz = center
    sx, sy, sz = (x / 2.0 for x in size)
    v = [(cx-sx,cy-sy,cz-sz), (cx+sx,cy-sy,cz-sz),
         (cx+sx,cy+sy,cz-sz), (cx-sx,cy+sy,cz-sz),
         (cx-sx,cy-sy,cz+sz), (cx+sx,cy-sy,cz+sz),
         (cx+sx,cy+sy,cz+sz), (cx-sx,cy+sy,cz+sz)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),
         (3,7,6,2),(0,4,7,3),(1,2,6,5)]
    add(name,group,material,v,f)


def lathe(name, group, material, profile, sides=SEGMENTS):
    """Solid hollow radial profile [(y,inner_radius,outer_radius), ...]."""
    assert len(profile) >= 2
    assert all(0 < a < b for _, a, b in profile)
    v = []
    for y, inner, outer in profile:
        for radius in (outer, inner):
            for i in range(sides):
                a = 2 * math.pi * i / sides
                v.append((radius * math.cos(a), y, radius * math.sin(a)))
    f = []
    for p in range(len(profile)-1):
        a, b = p*2*sides, (p+1)*2*sides
        for j in range(sides):
            k=(j+1)%sides
            f.append((a+j,b+j,b+k,a+k))
            f.append((a+sides+k,b+sides+k,b+sides+j,a+sides+j))
    last=(len(profile)-1)*2*sides
    for j in range(sides):
        k=(j+1)%sides
        f.append((sides+j,j,k,sides+k))              # rear cap, -Y
        f.append((last+j,last+sides+j,last+sides+k,last+k))  # front cap, +Y
    add(name,group,material,v,f,smooth=True)


def cylinder_y(name, group, material, x, y, z, radius, length, sides=12):
    v=[]
    for yy in (y-length/2,y+length/2):
        for i in range(sides):
            a=2*math.pi*i/sides
            v.append((x+radius*math.cos(a),yy,z+radius*math.sin(a)))
    faces=[tuple(reversed(tuple(range(sides)))),tuple(range(sides,2*sides))]
    for i in range(sides):
        j=(i+1)%sides
        faces.append((i,sides+i,sides+j,j))
    add(name,group,material,v,faces,smooth=True)


def radial_fin(name, angle):
    """Closed 3D swept fin. Tangential thickness 26 mm; attached to outer shell."""
    profile=[(.29,.265),(.35,.345),(.55,.425),(.64,.390),(.58,.330),(.40,.295)]
    t=.013
    ca,sa=math.cos(angle),math.sin(angle)
    verts=[]
    for side in (-1,1):
        for y,r in profile:
            verts.append((r*ca-side*t*sa,y,r*sa+side*t*ca))
    n=len(profile)
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    for i in range(n):
        j=(i+1)%n
        faces.append((i,j,n+j,n+i))
    add(name,"moving","safety_yellow",verts,faces)


def build_spec():
    if PARTS:
        return PARTS
    # All fixed geometry ends at Y<=0.49; moving geometry begins at Y>=0.56
    # before rotation. The gap is verified over the full yaw/pitch envelope.
    box("Rear_frame_mount_plate","fixed","gunmetal",(0,.06,0),(.88,.12,.78))
    box("Rear_mount_center_boss","fixed","titanium",(0,.145,0),(.31,.05,.27))
    for xside, tag in ((-.44,"L"),(.44,"R")):
        box("Side_gimbal_yoke_"+tag,"fixed","titanium",(xside,.31,0),(.09,.32,.15))
        box("Side_servo_pivot_"+tag,"fixed","gunmetal",(xside,.435,0),(.115,.09,.19))
        for z, label in ((-.26,"D"),(.26,"U")):
            box("Mount_stiffener_"+tag+label,"fixed","gunmetal",
                (xside,.23,z),(.075,.18,.075))
    for x, sx in ((-.33,"L"),(.33,"R")):
        for z, sz in ((-.29,"D"),(.29,"U")):
            cylinder_y("Mount_bolt_"+sx+sz,"fixed","safety_yellow",
                       x,.136,z,.024,.026,12)
    box("Upper_bridge","fixed","titanium",(0,.25,.355),(.70,.18,.06))
    box("Lower_bridge","fixed","titanium",(0,.25,-.355),(.70,.18,.06))

    # Moving group local coordinates are relative to yaw/pitch gimbal at Y=.46.
    lathe("Variable_section_nozzle_shell","moving","gunmetal",[
        (.10,.165,.225),(.18,.180,.240),(.36,.212,.280),
        (.55,.246,.327),(.72,.272,.365),(.75,.275,.373)])
    lathe("Ceramic_exit_throat","moving","ceramic",[
        (.752,.274,.372),(.790,.283,.385),(.818,.292,.394)])
    lathe("Intake_titanium_coll ar".replace(" ",""),"moving","titanium",[
        (.105,.163,.233),(.148,.168,.244)])
    lathe("Rear_cooling_band","moving","titanium",[
        (.265,.191,.259),(.294,.194,.268)])
    lathe("Forward_cooling_band","moving","titanium",[
        (.593,.256,.348),(.619,.259,.358)])
    lathe("Inner_ion_emitter","moving","ion_cyan",[
        (.805,.271,.289),(.812,.272,.290)])
    lathe("Exit_hazard_band","moving","safety_yellow",[
        (.795,.291,.397),(.809,.294,.401)])
    for i in range(6):
        radial_fin("External_vector_fin_%02d"%i, 2*math.pi*i/6)
    for i in range(8):
        a=2*math.pi*i/8
        r=.359
        cylinder_y("Exit_lock_bolt_%02d"%i,"moving","titanium",
                   r*math.cos(a),.748,r*math.sin(a),.014,.022,10)
    for i in range(6):
        a=2*math.pi*i/6+math.pi/6
        r=.283
        cylinder_y("Cooling_rivet_%02d"%i,"moving","ceramic",
                   r*math.cos(a),.366,r*math.sin(a),.014,.019,10)
    return PARTS


def transformed(p, yaw=0.0, pitch=0.0):
    """Moving local +Y translated to gimbal and rotated pitch-X then yaw-Z."""
    cy,sy=math.cos(yaw),math.sin(yaw)
    cp,sp=math.cos(pitch),math.sin(pitch)
    out=[]
    for x,y,z in p["vertices"]:
        if p["group"] == "moving":
            y,z = y*cp-z*sp, y*sp+z*cp
            x,y = x*cy-y*sy, x*sy+y*cy
            y += PIVOT_Y
        out.append((x,y,z))
    return out


def bounds(verts):
    return tuple((min(v[i] for v in verts),max(v[i] for v in verts)) for i in range(3))


def overlap(a,b):
    return math.prod(max(0.0,min(a[i][1],b[i][1])-max(a[i][0],b[i][0]))
                     for i in range(3))


def validate():
    build_spec()
    assert len(PARTS)==len({p["name"] for p in PARTS})
    assert len({p["material"] for p in PARTS})>=3
    assert sum(p["group"]=="moving" for p in PARTS)>0
    tri_count=0
    for p in PARTS:
        for f in p["faces"]:
            assert len(f)>=3 and len(set(f))==len(f),(p["name"],f)
            v=p["vertices"]
            a,b,c=(v[f[i]] for i in range(3))
            cross=((b[1]-a[1])*(c[2]-a[2])-(b[2]-a[2])*(c[1]-a[1]),
                   (b[2]-a[2])*(c[0]-a[0])-(b[0]-a[0])*(c[2]-a[2]),
                   (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]))
            assert sum(x*x for x in cross)>1e-14,(p["name"],f)
            tri_count+=len(f)-2
    fixed=[p for p in PARTS if p["group"]=="fixed"]
    moving=[p for p in PARTS if p["group"]=="moving"]
    static_bounds=[bounds(transformed(p)) for p in fixed]
    checks=0
    minimum_gap=float("inf")
    # 7x7 combinations include every angular extreme; 61 animation frames
    # separately checked. AABB is conservative, NOT an engine physics test.
    poses=[(math.radians(YAW_MAX_DEG)*(i-3)/3,
            math.radians(PITCH_MAX_DEG)*(j-3)/3)
           for i in range(7) for j in range(7)]
    for frame in range(61):
        t=frame/60
        poses.append((math.radians(YAW_MAX_DEG)*math.sin(2*math.pi*t),
                      math.radians(PITCH_MAX_DEG)*math.sin(2*math.pi*t+math.pi/2)))
    for yaw,pitch in poses:
        for m in moving:
            bb=bounds(transformed(m,yaw,pitch))
            for fb in static_bounds:
                checks+=1
                assert overlap(bb,fb)<1e-12,(
                    "MOVING_FIXED_INTERSECTION",m["name"],yaw,pitch,bb,fb)
                minimum_gap=min(minimum_gap,bb[1][0]-fb[1][1])
    return {
        "id":ID,"status":"STATIC_QA_PASS","parts":len(PARTS),
        "fixed_parts":len(fixed),"moving_parts":len(moving),
        "materials":sorted({p["material"] for p in PARTS}),
        "vertices":sum(len(p["vertices"]) for p in PARTS),
        "triangles":tri_count,
        "mount":"rear frame: origin (0,0,0), exhaust +Y, Blender +Z up",
        "gimbal_pivot_m":[0,PIVOT_Y,0],
        "yaw_range_deg":[-YAW_MAX_DEG,YAW_MAX_DEG],
        "pitch_range_deg":[-PITCH_MAX_DEG,PITCH_MAX_DEG],
        "pose_samples":len(poses),"moving_fixed_aabb_checks":checks,
        "minimum_y_separation_m":round(minimum_gap,5),
        "aabb_collision":"PASS (conservative broad-phase, not Unity physics)",
        "blender":"NOT RUN","animated_glb":"NOT RUN",
        "unity_import":"NOT RUN","visual_inspection":"NOT RUN"
    }


def export_obj(outdir):
    obj=outdir/(ID+".obj")
    mtl=outdir/(ID+".mtl")
    with mtl.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M04-004 reproducible palette\n")
        for name,m in PALETTE.items():
            r,g,b=m["rgb"]
            f.write("newmtl "+name+"\n")
            f.write("Kd %.6f %.6f %.6f\n"%(r,g,b))
            f.write("Ka 0.04 0.04 0.04\nKs 0.32 0.32 0.32\nNs 96\n")
            if m.get("emission"):
                f.write("Ke %.6f %.6f %.6f\n"%(r,g,b))
            f.write("\n")
    with obj.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M04-004 static neutral pose; mount origin 0 0 0\nmtllib M04-004.mtl\n")
        index=1
        for p in PARTS:
            f.write("o "+p["name"]+"\nusemtl "+p["material"]+"\n")
            for x,y,z in transformed(p):
                f.write("v %.6f %.6f %.6f\n"%(x,y,z))
            for face in p["faces"]:
                f.write("f "+" ".join(str(index+i) for i in face)+"\n")
            index+=len(p["vertices"])
    return obj,mtl


def blender_build(outdir):
    import bpy
    # Safe on repeated invocations in the same .blend: replace own collection.
    old=bpy.data.collections.get(ID+"_ASSET")
    if old is not None:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(ID+"_ASSET")
    bpy.context.scene.collection.children.link(col)
    mats={}
    for name,p in PALETTE.items():
        mat=bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(ID+"_"+name)
        mat.diffuse_color=(*p["rgb"],1)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=(*p["rgb"],1)
            bsdf.inputs["Metallic"].default_value=p["metallic"]
            bsdf.inputs["Roughness"].default_value=p["roughness"]
            if p.get("emission"):
                for key,val in (("Emission Color",(*p["rgb"],1)),
                                ("Emission Strength",p["emission"])):
                    if key in bsdf.inputs:
                        bsdf.inputs[key].default_value=val
        mats[name]=mat
    def empty(name,parent=None,location=(0,0,0)):
        o=bpy.data.objects.new(ID+"_"+name,None)
        col.objects.link(o)
        o.empty_display_type="PLAIN_AXES"
        o.empty_display_size=.10
        if parent:
            o.parent=parent
        o.location=location
        return o
    mount=empty("MOUNT_REAR")
    mount["port"]="REAR_FRAME_FACE_Y0"
    mount["units"]="1 Blender unit = 1 metre"
    mount["exhaust_axis"]="+Y, Blender +Z up"
    yaw=empty("YAW_Z",mount,(0,PIVOT_Y,0))
    pitch=empty("PITCH_X",yaw)
    yaw["range_deg"]=[-YAW_MAX_DEG,YAW_MAX_DEG]
    pitch["range_deg"]=[-PITCH_MAX_DEG,PITCH_MAX_DEG]
    for p in PARTS:
        mesh=bpy.data.meshes.new(ID+"_"+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new(ID+"_"+p["name"],mesh)
        col.objects.link(ob)
        ob.parent=pitch if p["group"]=="moving" else mount
        ob.data.materials.append(mats[p["material"]])
        for poly in mesh.polygons:
            poly.use_smooth=p["smooth"]
        ob["role"]=p["group"]
        ob["model_id"]=ID
    for frame in range(1,62):
        t=(frame-1)/60
        yaw.rotation_euler[2]=math.radians(YAW_MAX_DEG)*math.sin(2*math.pi*t)
        pitch.rotation_euler[0]=math.radians(PITCH_MAX_DEG)*math.cos(2*math.pi*t)
        yaw.keyframe_insert(data_path="rotation_euler",index=2,frame=frame)
        pitch.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=61
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(outdir/(ID+".blend")))
    prior_selection=list(bpy.context.selected_objects)
    prior_active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in col.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active=mount
        bpy.ops.export_scene.gltf(
            filepath=str(outdir/(ID+"_animated.glb")),
            export_format="GLB",
            export_animations=True,
            use_selection=True,
            export_yup=True)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in prior_selection:
            if ob.name in bpy.data.objects:
                ob.select_set(True)
        if prior_active and prior_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=prior_active
    return {"blend":str(outdir/(ID+".blend")),
            "glb":str(outdir/(ID+"_animated.glb"))}


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir",default=str(Path(__file__).resolve().parent))
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    args=parser.parse_args(argv)
    outdir=Path(args.output_dir).resolve()
    outdir.mkdir(parents=True,exist_ok=True)
    qa=validate()
    obj,mtl=export_obj(outdir)
    qa["obj"]=obj.name
    qa["mtl"]=mtl.name
    if not args.self_test:
        try:
            import bpy
        except ImportError:
            if args.require_blender:
                raise RuntimeError("Blender bpy unavailable; GLB/.blend NOT RUN")
        else:
            qa["blender_artifacts"]=blender_build(outdir)
            qa["blender"]="EXECUTED"
            qa["animated_glb"]="EXPORTED_UNVALIDATED"
    (outdir/(ID+".static-qa.json")).write_text(
        json.dumps(qa,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(qa,indent=2,ensure_ascii=False))


if __name__=="__main__":
    main()
