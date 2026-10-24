#!/usr/bin/env python3
"""M01-003 / Podwójny emiter łuku — visual-only Unity 6 asset.

One unit = one metre. Local axes: X = left/right, Y = up, +Z = forward.
Pivot MOUNT_TOP_SOCKET at (0,0,0), flush with the underside of the mount.
Animated protective shields translate OUTWARD on local X; no gameplay logic.
Blender 4.2+ exports animated FBX and GLB. Plain Python creates OBJ/MTL.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = "M01-003"
SEGMENTS = 24
OPEN_DISTANCE = 0.115
SHIELD_CENTRE_X = 0.420
SHIELD_HALF_WIDTH = 0.035
FIXED_MAX_X = 0.365
# Fixed slide track spans each shield's entire 115mm X travel. Its inner
# 15mm is mechanically seated in the chassis. A moving shoe (seated in
# the shield) rests on the track with zero negative clearance, throughout
# travel, instead of floating ~41mm above a Z-facing cosmetic strip.
GUIDE_CENTRE_X = .465
GUIDE_SPAN_X = .230
GUIDE_CENTRE_Y = .263
GUIDE_HEIGHT = .035
SHOE_CENTRE_Y = .300
SHOE_HEIGHT = .039
SHOE_WIDTH = .045
SHOE_LENGTH_Z = .350
CORE_CENTRES = (-0.220, 0.220)
CORE_RING_RADIUS = 0.116
FPS = 30
KEYS = ((1, 0.0), (6, 0.0), (9, 1.0), (16, 1.0),
        (20, 0.0), (31, 0.0))
# (linear RGBA, metallic, roughness, emission RGB, emission strength)
MATERIALS = {
    "Graphite_Armor": ((.075,.095,.125,1),.72,.36,(0,0,0),0),
    "Ceramic_Ivory": ((.76,.79,.78,1),.22,.31,(0,0,0),0),
    "Copper_Coils": ((.56,.23,.09,1),.81,.28,(0,0,0),0),
    "Gasket_Black": ((.025,.032,.045,1),.05,.85,(0,0,0),0),
    "Arc_Cyan": ((.10,.68,.96,1),.15,.24,(.04,.64,1),3.0),
    "Arc_Violet": ((.54,.23,.96,1),.15,.24,(.54,.14,1),3.0),
    "Signal_Amber": ((.98,.42,.04,1),.35,.33,(.95,.17,.02),.35),
}
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])
def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))
def dot(a,b):
    return sum(x*y for x,y in zip(a,b))

def make(name,material,vertices,faces,moving=None,smooth=False):
    return dict(name=name,material=material,vertices=vertices,
                faces=faces,moving=moving,smooth=smooth)

def chamfer_box(name, material, x, y, z, sx, sy, sz, cut=.012, moving=None):
    """Watertight 8-corner bevel profile, extruded along Z."""
    assert min(sx,sy,sz)>0 and 0<cut<min(sx,sy)/2
    a,b=sx/2,sy/2
    xy=[(-a+cut,-b),(a-cut,-b),(a,-b+cut),(a,b-cut),
        (a-cut,b),(-a+cut,b),(-a,b-cut),(-a,-b+cut)]
    v=[(x+u,y+w,z+q) for q in (-sz/2,sz/2) for u,w in xy]
    v.extend(((x,y,z-sz/2),(x,y,z+sz/2)))
    f=[]
    for i in range(8):
        j=(i+1)%8
        f.extend(((i,j,8+j),(i,8+j,8+i)))
        f.append((16,j,i))
        f.append((17,8+i,8+j))
    return make(name,material,v,f,moving)

def cylinder_z(name, material, x, y, z0, z1, r, segments=SEGMENTS,
               moving=None):
    """Closed capped cylinder, +Z aligned, with face-level hard edges."""
    assert z1>z0 and r>0 and segments>=8
    v=[(x+r*math.cos(2*math.pi*j/segments),
        y+r*math.sin(2*math.pi*j/segments),z)
       for z in (z0,z1) for j in range(segments)]
    v.extend(((x,y,z0),(x,y,z1)))
    f=[]
    for j in range(segments):
        k=(j+1)%segments
        f.extend(((j,k,segments+k),(j,segments+k,segments+j)))
        f.extend(((2*segments,k,j),
                  (2*segments+1,segments+j,segments+k)))
    return make(name,material,v,f,moving,True)

def annulus_z(name,material,x,y,z0,z1,inner,outer,segments=SEGMENTS):
    """Closed manifold annular coil ring; NO solid disc through core."""
    assert 0<inner<outer and z0<z1
    profile=((z0,inner),(z0,outer),(z1,outer),(z1,inner))
    v=[(x+r*math.cos(2*math.pi*j/segments),
        y+r*math.sin(2*math.pi*j/segments),z)
       for z,r in profile for j in range(segments)]
    f=[]
    for i in range(4):
        nxt=(i+1)%4
        for j in range(segments):
            k=(j+1)%segments
            a=i*segments+j
            b=i*segments+k
            c=nxt*segments+k
            d=nxt*segments+j
            f.extend(((a,b,c),(a,c,d)))
    return make(name,material,v,f,None,False)

def build():
    p=[]
    add=p.append
    # Top-socket mount, bottom face exactly at Y=0.
    add(chamfer_box("Top_Socket_Plate","Graphite_Armor",
                    0,.042,0,.49,.084,.37,.024))
    add(chamfer_box("Mount_Riser","Copper_Coils",
                    0,.153,.06,.18,.22,.17,.015))
    add(chamfer_box("Mount_Collar","Gasket_Black",
                    0,.236,.11,.28,.035,.23,.012))
    for side in (-1,1):
        add(chamfer_box("Socket_Key_"+str(side),"Ceramic_Ivory",
                        side*.18,.084,-.06,.046,.020,.16,.008))
    # Fixed central armor is below the independently guarded emitter tubes.
    add(chamfer_box("Emitter_Chassis","Graphite_Armor",
                    0,.285,.36,.73,.16,.54,.035))
    add(chamfer_box("Top_Spine","Ceramic_Ivory",
                    0,.378,.32,.18,.026,.39,.009))
    add(chamfer_box("Rear_Bridge","Copper_Coils",
                    0,.388,.495,.49,.050,.062,.010))
    add(chamfer_box("Nose_Bridge","Ceramic_Ivory",
                    0,.375,.910,.31,.035,.060,.010))
    for side in (-1,1):
        x=side*.220
        core="Arc_Cyan" if side<0 else "Arc_Violet"
        label="L" if side<0 else "R"
        add(cylinder_z("Core_"+label,core,x,.415,.515,.975,.060,32))
        add(cylinder_z("Core_Gasket_"+label,"Gasket_Black",
                       x,.415,.494,.526,.082,24))
        add(cylinder_z("Muzzle_Lip_"+label,"Ceramic_Ivory",
                       x,.415,.970,.990,.085,24))
        for i,z in enumerate((.550,.704,.855),1):
            add(annulus_z("Coil_"+label+"_"+str(i),"Copper_Coils",
                          x,.415,z,z+.031,.079,CORE_RING_RADIUS))
        for i,z in enumerate((.600,.795),1):
            add(annulus_z("Insulator_"+label+"_"+str(i),
                          "Gasket_Black",x,.415,z,z+.016,.074,.094))
        # X-direction track anchored to the chassis and spanning the
        # whole outward shield stroke. Rail ends at +/- .580 m.
        add(chamfer_box("Shield_Guide_"+label,"Copper_Coils",
                        side*GUIDE_CENTRE_X,GUIDE_CENTRE_Y,.704,
                        GUIDE_SPAN_X,GUIDE_HEIGHT,.475,.008))
        # Exposed fixed metal undercarriage with distinct white highlight.
        add(chamfer_box("Lower_Fin_"+label,"Ceramic_Ivory",
                        side*.278,.204,.49,.094,.038,.37,.010))
        # A two-part side cover moves strictly OUTWARD; parts share a parent.
        moving=label
        add(chamfer_box("Shield_"+label,"Graphite_Armor",
                        side*SHIELD_CENTRE_X,.414,.709,.070,.195,.386,
                        .015,moving))
        # Moving foot touches the top of the copper X-guide without
        # penetrating the rail; its upper 3mm is welded inside shield.
        add(chamfer_box("Shield_Slider_Shoe_"+label,"Gasket_Black",
                        side*SHIELD_CENTRE_X,SHOE_CENTRE_Y,.704,
                        SHOE_WIDTH,SHOE_HEIGHT,SHOE_LENGTH_Z,.006,moving))
        add(chamfer_box("Shield_Trim_"+label,"Ceramic_Ivory",
                        side*(SHIELD_CENTRE_X+.001),.508,.709,
                        .048,.014,.280,.006,moving))
        add(chamfer_box("Shield_Light_"+label,"Signal_Amber",
                        side*(SHIELD_CENTRE_X+.001),.515,.789,
                        .025,.008,.083,.003,moving))
    return p

def validate(parts):
    names=set()
    triangles=0
    verts=0
    groups={"L":0,"R":0}
    for p in parts:
        assert p["name"] not in names, "duplicate name"
        names.add(p["name"])
        assert p["material"] in MATERIALS
        assert p["vertices"] and p["faces"]
        verts+=len(p["vertices"])
        edges={}
        for vertex in p["vertices"]:
            assert all(math.isfinite(v) for v in vertex)
        for a,b,c in p["faces"]:
            assert len({a,b,c})==3
            assert min(a,b,c)>=0 and max(a,b,c)<len(p["vertices"])
            A,B,C=(p["vertices"][i] for i in (a,b,c))
            n=cross(sub(B,A),sub(C,A))
            assert dot(n,n)>1e-15, "degenerate face: "+p["name"]
            for i,j in ((a,b),(b,c),(c,a)):
                e=tuple(sorted((i,j)))
                edges[e]=edges.get(e,0)+1
            triangles+=1
        assert all(count==2 for count in edges.values()), (
            "open or nonmanifold part: "+p["name"])
        if p["moving"]:
            groups[p["moving"]]+=1
    assert groups=={"L":4,"R":4}, groups
    assert len(MATERIALS)>=3
    assert len(parts)==37
    # Conservative AABB clearance: even closed shields must not touch
    # the fixed chassis; outward-only travel monotonically increases gap.
    min_shield_x=SHIELD_CENTRE_X-SHIELD_HALF_WIDTH
    assert min_shield_x>FIXED_MAX_X+.015
    assert OPEN_DISTANCE>0
    assert max(abs(x)+CORE_RING_RADIUS for x in CORE_CENTRES)<min_shield_x
    for frame,extent in KEYS:
        assert 0<=extent<=1
        for side in (-1,1):
            moving_inner=SHIELD_CENTRE_X+extent*OPEN_DISTANCE-SHIELD_HALF_WIDTH
            assert moving_inner>FIXED_MAX_X+.015
            assert moving_inner>abs(CORE_CENTRES[0])+CORE_RING_RADIUS+.015
    # Validate actual procedurally generated mesh vertices, not just
    # constants: rails attach to the stationary chassis and moving shoes
    # stay seated for 21 continuous pose samples per side.
    by_name={p["name"]:p for p in parts}
    def bounds(p):
        return [(min(v[k] for v in p["vertices"]),
                 max(v[k] for v in p["vertices"])) for k in range(3)]
    chassis=bounds(by_name["Emitter_Chassis"])
    sampled_track_poses=0
    for label,side in (("L",-1),("R",1)):
        guide=by_name["Shield_Guide_"+label]
        shoe=by_name["Shield_Slider_Shoe_"+label]
        shield=by_name["Shield_"+label]
        assert guide["moving"] is None
        assert shoe["moving"]==shield["moving"]==label
        r,b,h=(bounds(p) for p in (guide,shoe,shield))
        # The rail has a load path into the rigid frame, not a floating
        # narrow strip outside the body shell.
        attachment=[min(r[k][1],chassis[k][1]) -
                    max(r[k][0],chassis[k][0]) for k in range(3)]
        assert min(attachment) >= .014-1e-8, (label,attachment)
        assert abs(r[1][1]-b[1][0])<1e-8,(label,"rail/shoe y gap")
        assert h[1][0] < b[1][1]-.0029,(label,"shoe not in shield")
        assert b[2][0]>=h[2][0] and b[2][1]<=h[2][1]
        for k in range(21):
            delta=side*OPEN_DISTANCE*k/20
            bx0,bx1=(b[0][0]+delta,b[0][1]+delta)
            assert bx0 >= r[0][0]+.001 and bx1 <= r[0][1]-.001, (
                label,k,"shoe escapes fixed track",bx0,bx1,r[0])
            sampled_track_poses+=1
    assert sampled_track_poses==42
    assert KEYS[0][1]==0 and KEYS[-1][1]==0
    assert any(k[1]==1 for k in KEYS)
    return {
        "asset_id":ASSET_ID,"parts":len(parts),"vertices":verts,
        "triangles":triangles,"materials":len(MATERIALS),
        "mount":"MOUNT_TOP_SOCKET at (0,0,0), Y up, +Z forward",
        "moving_groups":groups,"shield_axis":"local X, outward per side",
        "track_attached_to_chassis":2,
        "moving_rail_shoes":2,
        "shield_track_poses_verified":sampled_track_poses,
        "nominal_shoe_rail_vertical_gap_m":0.,
        "guide_into_chassis_min_x_m":.015,
        "geometry_revision":"M01-003_V2_CONNECTED_SHIELD_SLIDES",
        "shield_travel_m":OPEN_DISTANCE,
        "shield_fixed_chassis_clearance_m":round(
            min_shield_x-FIXED_MAX_X,4),
        "keys":[[f,t] for f,t in KEYS],
        "watertight_per_part":"PASS",
        "extreme_frame_clearance":"PASS (conservative AABB)",
        "visual_qa":"NOT RUN","unity_import":"NOT RUN",
        "blender_export":"NOT RUN"}

def export_obj(parts,outdir):
    """Dependency-free actual static OBJ+MTL for immediate Unity import."""
    stem="M01-003_dual_arc_emitter"
    obj=outdir/(stem+".obj")
    mtl=outdir/(stem+".mtl")
    lines=["# M01-003 static OBJ: Y-up +Z-forward; mount origin 0 0 0",
           "mtllib "+mtl.name]
    offset=0
    for part in parts:
        lines.extend(("o "+part["name"],"usemtl "+part["material"]))
        for x,y,z in part["vertices"]:
            lines.append("v %.8f %.8f %.8f"%(x,y,z))
        for a,b,c in part["faces"]:
            lines.append("f %d %d %d"%(offset+a+1,offset+b+1,offset+c+1))
        offset+=len(part["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    materials=["# M01-003 static MTL; Blender FBX/GLB preserve PBR emission."]
    for name,(rgba,metal,rough,emission,strength) in MATERIALS.items():
        materials.extend(("newmtl "+name,
                          "Kd %.5f %.5f %.5f"%rgba[:3],
                          "Ks %.5f %.5f %.5f"%(metal,metal,metal),
                          "Ns %.3f"%((1-rough)*1000),
                          "Ke %.5f %.5f %.5f"%tuple(
                              min(1,v*strength) for v in emission),
                          "d %.4f"%rgba[3],"illum 2",""))
    mtl.write_text("\n".join(materials)+"\n",encoding="utf-8")
    return [str(obj),str(mtl)]

def blender_export(parts,outdir):
    import bpy
    collection=bpy.data.collections.get("MODEL_M01_003")
    if collection:
        for obj in list(collection.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(collection)
    collection=bpy.data.collections.new("MODEL_M01_003")
    bpy.context.scene.collection.children.link(collection)
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None)
        collection.objects.link(ob)
        ob.parent=parent
        ob.empty_display_size=.055
        return ob
    mount=empty("MOUNT_TOP_SOCKET")
    left=empty("SHIELD_SLIDER_L",mount)
    right=empty("SHIELD_SLIDER_R",mount)
    mats={}
    for name,(rgba,metal,rough,emission,strength) in MATERIALS.items():
        key="M01_003_"+name
        mat=bpy.data.materials.get(key)
        if mat is None:
            mat=bpy.data.materials.new(key)
        mat.use_nodes=True
        shader=mat.node_tree.nodes.get("Principled BSDF")
        if shader:
            shader.inputs["Base Color"].default_value=rgba
            shader.inputs["Metallic"].default_value=metal
            shader.inputs["Roughness"].default_value=rough
            if "Emission Color" in shader.inputs:
                shader.inputs["Emission Color"].default_value=(*emission,1)
            if "Emission Strength" in shader.inputs:
                shader.inputs["Emission Strength"].default_value=strength
        mat.diffuse_color=rgba
        mats[name]=mat
    for part in parts:
        mesh=bpy.data.meshes.new("M01_003_"+part["name"])
        mesh.from_pydata(part["vertices"],[],part["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new("M01_003_"+part["name"],mesh)
        collection.objects.link(obj)
        obj.parent=left if part["moving"]=="L" else (
            right if part["moving"]=="R" else mount)
        mesh.materials.append(mats[part["material"]])
        # Flat panels and annular collars keep hard normal breaks.
        for face in mesh.polygons:
            face.use_smooth=False
    for frame,extent in KEYS:
        left.location.x=-OPEN_DISTANCE*extent
        right.location.x=OPEN_DISTANCE*extent
        left.keyframe_insert(data_path="location",index=0,frame=frame)
        right.keyframe_insert(data_path="location",index=0,frame=frame)
    # Constant hold, abrupt mechanical jump during opening/closing.
    for slider in (left,right):
        if slider.animation_data and slider.animation_data.action:
            action=slider.animation_data.action
            try:
                for curve in action.fcurves:
                    for key in curve.keyframe_points:
                        key.interpolation="LINEAR"
            except AttributeError:
                pass # Blender 4.4+ layered Actions: keys still export.
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=31
    scene.render.fps=FPS
    scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=mount
    fbx=outdir/"M01-003_dual_arc_emitter.fbx"
    glb=outdir/"M01-003_dual_arc_emitter.glb"
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    return {"backend":"bpy","fbx":str(fbx),"glb":str(glb),
            "animation":"shield slide 0..0.115m on local X, 31 frames at 30fps"}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M01-003")
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args(argv)
    parts=build()
    report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    outdir=Path(args.out)
    outdir.mkdir(parents=True,exist_ok=True)
    report["static_files"]=export_obj(parts,outdir)
    try:
        import bpy
    except ImportError:
        report["backend"]="plain-python-static-obj"
    else:
        report.update(blender_export(parts,outdir))
        report["blender_export"]="PASS (files generated; visual QA pending)"
    manifest=outdir/"M01-003_manifest.json"
    manifest.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
