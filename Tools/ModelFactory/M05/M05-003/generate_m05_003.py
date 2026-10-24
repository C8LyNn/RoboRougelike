#!/usr/bin/env python3
"""M05-003 dual optical scanner — visual-only asset for Unity 6 RoboRougelike.

Python 3.11+ stdlib static OBJ/MTL exporter and self-test.
Blender 4.2+ bpy: deterministic mesh/material authoring and animated FBX/GLB.
Unity coordinates: 1 unit = 1 metre, Y up, lenses face local +Z.
Origin / MOUNT_FRONT: centre of rear/front-facing vehicle mount, (0,0,0).
Only optical iris ornaments rotate; this is NOT a scanning gameplay system.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

ID = "M05-003"
COLLECTION = "MODEL_M05_003"
EYE_X = 0.215
EYE_Y = 0.020
ROTOR_Z = 0.635
ROTOR_RADIUS_LIMIT = 0.075
BARREL_BORE_RADIUS = 0.091
LENS_REAR_Z = 0.617
LENS_FRONT_Z = 0.624
BEZEL_REAR_Z = 0.645
FPS = 60
FRAMES = (1, 16, 31, 46, 61)

# RGBA, metallic, roughness, emission RGB, emission strength.
MATERIALS = {
    "Graphite_Armor": ((0.065,0.086,0.107,1),0.42,0.48,(0,0,0),0),
    "Ceramic_Ivory": ((0.57,0.64,0.66,1),0.22,0.36,(0,0,0),0),
    "Titanium_Bezel": ((0.27,0.34,0.39,1),0.78,0.25,(0,0,0),0),
    "Rubber_Gasket": ((0.020,0.029,0.036,1),0.02,0.91,(0,0,0),0),
    "Ruby_Lens": ((0.35,0.013,0.021,1),0.24,0.17,(0.95,0.014,0.018),0.72),
    "Crimson_Emitter": ((0.66,0.025,0.020,1),0.17,0.28,(1.0,0.012,0.012),1.7),
    "Iris_Gunmetal": ((0.12,0.16,0.20,1),0.74,0.32,(0,0,0),0),
    "Amber_Status": ((0.92,0.38,0.035,1),0.27,0.26,(1.0,0.22,0.015),0.9),
    "Fastener_Steel": ((0.49,0.55,0.58,1),0.86,0.25,(0,0,0),0),
}

def mesh(name, material, vertices, faces, parent="MOUNT_FRONT", smooth=False):
    """Consistently orient each closed mesh using its signed volume."""
    assert material in MATERIALS
    volume = 0.0
    for a,b,c in faces:
        x,y,z = (vertices[a],vertices[b],vertices[c])
        volume += dot(x, cross(y,z))/6
    assert abs(volume) > 1e-12, (name, "zero enclosed volume")
    if volume < 0:
        faces = [(a,c,b) for a,b,c in faces]
    return {"name":name,"material":material,"vertices":vertices,
            "faces":faces,"parent":parent,"smooth":smooth}

def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])

def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))

def dot(a,b):
    return sum(x*y for x,y in zip(a,b))

def chamfer_box(name, cx,cy,z0,z1,sx,sy,chamfer,material):
    """Closed 8-sided XY profile extruded along +Z; explicit bevels, no modifiers."""
    assert z1>z0 and sx>2*chamfer and sy>2*chamfer and chamfer>0
    a,b = sx/2,sy/2
    outline=[(-a+chamfer,-b),(a-chamfer,-b),(a,-b+chamfer),
             (a,b-chamfer),(a-chamfer,b),(-a+chamfer,b),
             (-a,b-chamfer),(-a,-b+chamfer)]
    verts=[(cx+x,cy+y,z) for z in (z0,z1) for x,y in outline]
    verts.extend(((cx,cy,z0),(cx,cy,z1)))
    faces=[]
    for j in range(8):
        k=(j+1)%8
        faces.extend(((j,k,8+k),(j,8+k,8+j)))
        faces.extend(((16,k,j),(17,8+j,8+k)))
    return mesh(name,material,verts,faces)

def cylinder(name,r,z0,z1,cx,cy,material,segments=32,parent="MOUNT_FRONT"):
    assert r>0 and z1>z0 and segments>=8
    verts=[(cx+r*math.cos(2*math.pi*i/segments),
            cy+r*math.sin(2*math.pi*i/segments),z)
           for z in (z0,z1) for i in range(segments)]
    verts.extend(((cx,cy,z0),(cx,cy,z1)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend(((i,j,segments+j),(i,segments+j,segments+i)))
        faces.extend(((2*segments,j,i),
                      (2*segments+1,segments+i,segments+j)))
    return mesh(name,material,verts,faces,parent=parent,smooth=True)

def annulus(name,r_inner,r_outer,z0,z1,cx,cy,material,segments=48):
    """Closed radial sleeve; real opening, not a solid cap."""
    assert 0<r_inner<r_outer and z0<z1
    verts=[]
    for z,r in ((z0,r_inner),(z0,r_outer),(z1,r_outer),(z1,r_inner)):
        verts.extend((cx+r*math.cos(2*math.pi*i/segments),
                      cy+r*math.sin(2*math.pi*i/segments),z)
                     for i in range(segments))
    faces=[]
    for layer in range(4):
        nxt=(layer+1)%4
        for i in range(segments):
            j=(i+1)%segments
            a=layer*segments+i
            b=layer*segments+j
            c=nxt*segments+j
            d=nxt*segments+i
            faces.extend(((a,b,c),(a,c,d)))
    return mesh(name,material,verts,faces,smooth=True)

def sector(name,r0,r1,angle0,angle1,z0,z1,material,parent):
    """Solid radial iris segment with individually oriented closed faces."""
    assert 0<r0<r1 and angle1>angle0 and z1>z0
    verts=[(r*math.cos(a),r*math.sin(a),z)
           for z in (z0,z1) for r,a in
           ((r0,angle0),(r1,angle0),(r1,angle1),(r0,angle1))]
    quads=((0,3,2,1),(4,5,6,7),(0,1,5,4),
           (1,2,6,5),(2,3,7,6),(3,0,4,7))
    faces=[]
    for a,b,c,d in quads:
        faces.extend(((a,b,c),(a,c,d)))
    return mesh(name,material,verts,faces,parent=parent)

def build():
    p=[]
    add=p.append
    # Mounting datum: rear face at z=0; a shallow rear connector is visible.
    add(chamfer_box("Rear_Mount_Plate",0,0,0.008,0.082,.79,.35,.046,
                    "Titanium_Bezel"))
    add(annulus("Rear_Docking_Collar",.043,.078,-.020,.011,0,0,
                "Fastener_Steel"))
    for x in (-.339,.339):
        for y in (-.128,.128):
            add(cylinder("Mount_Bolt_%s_%s"%("L" if x<0 else "R",
                         "D" if y<0 else "U"),.013,.082,.099,x,y,
                         "Fastener_Steel",16))
    # Low-poly chamfered armoured scanner housing with inset white cover.
    add(chamfer_box("Scanner_Core",0,0,.087,.431,.745,.325,.065,
                    "Graphite_Armor"))
    add(chamfer_box("Upper_Ceramic_Spine",0,.158,.139,.394,.31,.037,.009,
                    "Ceramic_Ivory"))
    add(chamfer_box("Lower_Ceramic_Spine",0,-.156,.161,.400,.43,.025,.007,
                    "Ceramic_Ivory"))
    for sign,side in ((-1,"L"),(1,"R")):
        x=sign*.335
        add(chamfer_box("Side_Armor_"+side,x,0,.136,.395,.078,.235,.017,
                        "Ceramic_Ivory"))
        add(chamfer_box("Side_Amber_Indicator_"+side,x,0.111,.292,.352,
                        .048,.014,.004,"Amber_Status"))
        add(chamfer_box("Top_Service_Fin_"+side,sign*.284,.159,.265,.441,
                        .064,.044,.012,"Titanium_Bezel"))
    # Two physical optical housings; no solid cap across the clear aperture.
    for sign,side in ((-1,"L"),(1,"R")):
        x=sign*EYE_X
        add(annulus("Barrel_Gasket_"+side,.094,.142,.391,.417,
                    x,EYE_Y,"Rubber_Gasket"))
        add(annulus("Optical_Barrel_"+side,BARREL_BORE_RADIUS,.137,
                    .414,.647,x,EYE_Y,"Titanium_Bezel"))
        add(annulus("Front_Ceramic_Bezel_"+side,.090,.147,
                    BEZEL_REAR_Z,.669,x,EYE_Y,"Ceramic_Ivory"))
        add(annulus("Red_Inner_Trim_"+side,.083,.089,
                    .614,.628,x,EYE_Y,"Crimson_Emitter"))
        # Ruby disk sits BEHIND the rotating decorative iris (no collision).
        add(cylinder("Ruby_Optical_Lens_"+side,.082,LENS_REAR_Z,
                     LENS_FRONT_Z,x,EYE_Y,"Ruby_Lens",48))
        # Five rigid iris petals rotate as a group in the free aperture.
        parent="LENS_ROTOR_"+side
        add(cylinder("Iris_Center_Hub_"+side,.018,-.004,.004,0,0,
                     "Fastener_Steel",24,parent=parent))
        for i in range(5):
            a=2*math.pi*i/5
            add(sector("Iris_Petal_%s_%02d"%(side,i+1),.015,.069,
                       a+.10,a+.72,-.003,.003,"Iris_Gunmetal",parent))
        add(sector("Iris_Index_Mark_"+side,.067,.073,.10,.27,
                   -.002,.002,"Amber_Status",parent))
    # Centre status display and ventilation, separated from the two barrels.
    add(chamfer_box("Centre_Status_Bridge",0,-.122,.434,.462,
                    .104,.048,.011,"Graphite_Armor"))
    for i in range(3):
        add(chamfer_box("Vent_Slit_%02d"%(i+1),0,-.135+i*.012,
                        .462,.468,.058,.008,.002,"Rubber_Gasket"))
    add(chamfer_box("Central_Status_LED",0,.139,.434,.445,
                    .071,.017,.004,"Crimson_Emitter"))
    return p

def world_vertex(part, v):
    if part["parent"]=="LENS_ROTOR_L":
        return (v[0]-EYE_X,v[1]+EYE_Y,v[2]+ROTOR_Z)
    if part["parent"]=="LENS_ROTOR_R":
        return (v[0]+EYE_X,v[1]+EYE_Y,v[2]+ROTOR_Z)
    return v

def validate(parts):
    assert parts
    names=set()
    tri_count=0
    vertices_total=0
    materials=set()
    bounds=[[float("inf")]*3,[-float("inf")]*3]
    for part in parts:
        assert part["name"] not in names,part["name"]
        names.add(part["name"])
        assert part["material"] in MATERIALS
        materials.add(part["material"])
        verts=part["vertices"]
        assert verts and part["faces"]
        vertices_total+=len(verts)
        for v in verts:
            assert all(math.isfinite(x) for x in v)
            w=world_vertex(part,v)
            for k in range(3):
                bounds[0][k]=min(bounds[0][k],w[k])
                bounds[1][k]=max(bounds[1][k],w[k])
        edges={}
        signed_volume=0.
        for a,b,c in part["faces"]:
            assert len({a,b,c})==3
            assert min(a,b,c)>=0 and max(a,b,c)<len(verts)
            x,y,z=verts[a],verts[b],verts[c]
            n=cross(sub(y,x),sub(z,x))
            assert dot(n,n)>1e-14,part["name"]
            signed_volume+=dot(x,cross(y,z))/6
            for u,v in ((a,b),(b,c),(c,a)):
                e=(min(u,v),max(u,v))
                edges[e]=edges.get(e,0)+1
            tri_count+=1
        assert signed_volume>1e-11,(part["name"],signed_volume)
        assert all(count==2 for count in edges.values()),part["name"]
    assert len(materials)>=3
    assert len([x for x in names if x.startswith("Iris_Petal_")])==10
    for side in ("L","R"):
        rotor=[p for p in parts if p["parent"]=="LENS_ROTOR_"+side]
        assert len(rotor)==7, (side,len(rotor))
        # Mechanical visual continuity. V1 had a full 6 mm radial gap
        # between central rotor hub (18 mm radius) and all 5 petals
        # (24 mm minimum radius). Index tags touched only a line at
        # the petal tip (69 mm). Both were floating details.
        hub=next(p for p in rotor if p["name"]=="Iris_Center_Hub_"+side)
        petals=[p for p in rotor if p["name"].startswith("Iris_Petal_")]
        index=next(p for p in rotor if p["name"]=="Iris_Index_Mark_"+side)
        assert len(petals)==5
        def radial_limits(part):
            r=[math.hypot(v[0],v[1]) for v in part["vertices"]]
            return (min(r),max(r))
        hub_min,hub_max=radial_limits(hub)
        assert abs(hub_max-.018)<1e-9
        for petal in petals:
            pmin,pmax=radial_limits(petal)
            assert abs(pmin-.015)<1e-9 and abs(pmax-.069)<1e-9
            assert hub_max-pmin >= .003-1e-9, (petal["name"],"floating hub")
        index_min,index_max=radial_limits(index)
        assert abs(index_min-.067)<1e-9 and abs(index_max-.073)<1e-9
        assert radial_limits(petals[0])[1]-index_min >= .002-1e-9
        # The index mark's angular sector (.10 .. .27 rad) overlaps the
        # first petal's (.10 .. .72 rad); radial overlap is therefore
        # a true volumetric seat, not an accidental concentric tangent.
        assert index["faces"] and index["parent"]=="LENS_ROTOR_"+side
        maxr=max(math.hypot(v[0],v[1]) for p in rotor for v in p["vertices"])
        minz=min(v[2]+ROTOR_Z for p in rotor for v in p["vertices"])
        maxz=max(v[2]+ROTOR_Z for p in rotor for v in p["vertices"])
        assert maxr<ROTOR_RADIUS_LIMIT
        assert BARREL_BORE_RADIUS-maxr>0.015
        assert minz-LENS_FRONT_Z>0.004
        assert BEZEL_REAR_Z-maxz>0.004
        # Rotor geometry is invariant under 360-degree rigid Z rotation.
        for angle in (0,math.pi/2,math.pi,3*math.pi/2,2*math.pi):
            assert all(math.hypot(v[0]*math.cos(angle)-v[1]*math.sin(angle),
                                      v[0]*math.sin(angle)+v[1]*math.cos(angle))
                       <BARREL_BORE_RADIUS-.015
                       for p in rotor for v in p["vertices"])
    assert bounds[0][2]<-.019 and .668<bounds[1][2]<.672
    assert max(abs(bounds[0][0]),abs(bounds[1][0]))<.42
    return {"id":ID,"qa_geometry":"PASS","closed_manifold_parts":len(parts),
            "parts":len(parts),"vertices":vertices_total,"triangles":tri_count,
            "materials":len(materials),"iris_petals":10,
            "lens_count":2,"rotor_clearance_qa":"PASS_5_ANGLES_EACH",
            "iris_petals_seated_in_hubs":10,
            "iris_index_markers_attached":2,
            "iris_hub_overlap_m":.003,
            "iris_index_overlap_m":.002,
            "geometry_revision":"M05-003_V2_CONNECTED_IRIS",
            "rotor_housing_radial_clearance_min_m":round(
                BARREL_BORE_RADIUS-ROTOR_RADIUS_LIMIT,3),
            "rotor_lens_axial_clearance_min_m":round(
                ROTOR_Z-.004-LENS_FRONT_Z,3),
            "rotor_bezel_axial_clearance_min_m":round(
                BEZEL_REAR_Z-(ROTOR_Z+.004),3),
            "bounds_m":bounds,"mount_pivot":[0,0,0],
            "optical_axis":"+Z","rotation_axis":"+Z",
            "animation":"two counter-rotating 360deg irises, 61 frames at 60fps"}

def write_obj(parts,outdir):
    """Always write a usable static OBJ and matching MTL (no Blender needed)."""
    outdir.mkdir(parents=True,exist_ok=True)
    mtl_name=ID+"_dual_optical_scanner.mtl"
    obj_name=ID+"_dual_optical_scanner.obj"
    mtl=["# M05-003 material palette, Blender-like diffuse/emissive values"]
    for name,(rgba,metal,rough,emission,strength) in MATERIALS.items():
        mtl.extend(("newmtl "+name,
                    "Kd %.6f %.6f %.6f"%rgba[:3],
                    "Ks %.6f %.6f %.6f"%(metal,metal,metal),
                    "Ns %.3f"%((1-rough)*300),
                    "Ke %.6f %.6f %.6f"%tuple(c*strength for c in emission),
                    "d 1.0","illum 2",""))
    (outdir/mtl_name).write_text("\n".join(mtl)+"\n",encoding="utf-8")
    lines=["# M05-003 static model; 1 unit = 1 m, Y up, optical +Z",
           "mtllib "+mtl_name]
    offset=0
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"],"s off"))
        for v in p["vertices"]:
            x,y,z=world_vertex(p,v)
            lines.append("v %.7f %.7f %.7f"%(x,y,z))
        for a,b,c in p["faces"]:
            lines.append("f %d %d %d"%(a+1+offset,b+1+offset,c+1+offset))
        offset+=len(p["vertices"])
    (outdir/obj_name).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return {"obj":str(outdir/obj_name),"mtl":str(outdir/mtl_name)}

def blender_export(parts,outdir):
    import bpy
    old=bpy.data.collections.get(COLLECTION)
    if old:
        for ob in list(old.objects):
            data=ob.data
            bpy.data.objects.remove(ob,do_unlink=True)
            if data is not None and data.users==0 and isinstance(data,bpy.types.Mesh):
                bpy.data.meshes.remove(data)
        bpy.data.collections.remove(old)
    for action in list(bpy.data.actions):
        if action.name.startswith("M05_003_") and action.users==0:
            bpy.data.actions.remove(action)
    coll=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    def empty(name,parent=None,location=(0,0,0)):
        ob=bpy.data.objects.new(name,None)
        coll.objects.link(ob)
        ob.empty_display_size=.065
        ob.location=location
        ob.parent=parent
        return ob
    mount=empty("MOUNT_FRONT")
    rotors={}
    for side,x in (("L",-EYE_X),("R",EYE_X)):
        rotors["LENS_ROTOR_"+side]=empty("LENS_ROTOR_"+side,mount,
                                       (x,EYE_Y,ROTOR_Z))
    mats={}
    for name,(rgba,metal,rough,emission,strength) in MATERIALS.items():
        mname="M05_003_"+name
        mat=bpy.data.materials.get(mname)
        if mat is None:
            mat=bpy.data.materials.new(mname)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=rgba
            bsdf.inputs["Metallic"].default_value=metal
            bsdf.inputs["Roughness"].default_value=rough
            emission_input=bsdf.inputs.get("Emission Color")
            if emission_input is None:
                emission_input=bsdf.inputs.get("Emission")
            if emission_input is not None:
                emission_input.default_value=(*emission,1)
            strength_input=bsdf.inputs.get("Emission Strength")
            if strength_input is not None:
                strength_input.default_value=strength
        mat.diffuse_color=rgba
        mats[name]=mat
    for p in parts:
        data=bpy.data.meshes.new("M05_003_"+p["name"])
        data.from_pydata(p["vertices"],[],p["faces"])
        data.update(calc_edges=True)
        ob=bpy.data.objects.new("M05_003_"+p["name"],data)
        coll.objects.link(ob)
        ob.parent=mount if p["parent"]=="MOUNT_FRONT" else rotors[p["parent"]]
        ob.data.materials.append(mats[p["material"]])
        for face in data.polygons:
            face.use_smooth=p["smooth"]
    for side,sign in (("L",1),("R",-1)):
        rotor=rotors["LENS_ROTOR_"+side]
        rotor.rotation_mode="XYZ"
        for frame,quarter in zip(FRAMES,range(5)):
            rotor.rotation_euler[2]=sign*quarter*math.pi/2
            rotor.keyframe_insert(data_path="rotation_euler",index=2,frame=frame)
        action=rotor.animation_data.action
        if action:
            action.name="M05_003_Iris_"+side
            try:
                for curve in action.fcurves:
                    for key in curve.keyframe_points:
                        key.interpolation="LINEAR"
            except AttributeError:
                # Blender 4.4+ layered actions use a different curve API.
                pass
    scene=bpy.context.scene
    scene.frame_start=FRAMES[0]
    scene.frame_end=FRAMES[-1]
    scene.render.fps=FPS
    scene.frame_set(FRAMES[0])
    bpy.ops.object.select_all(action="DESELECT")
    for ob in coll.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=mount
    fbx=outdir/(ID+"_dual_optical_scanner.fbx")
    glb=outdir/(ID+"_dual_optical_scanner.glb")
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    return {"backend":"Blender bpy","fbx":str(fbx),"glb":str(glb),
            "blender_export":"RUN","unity_import":"NOT RUN"}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M05-003")
    ap.add_argument("--self-test",action="store_true")
    ap.add_argument("--require-blender",action="store_true")
    args=ap.parse_args(argv)
    parts=build()
    report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    outdir=Path(args.out)
    report.update(write_obj(parts,outdir))
    try:
        import bpy
    except ImportError:
        if args.require_blender:
            raise RuntimeError("Blender bpy unavailable; animated export NOT RUN")
        report.update({"backend":"Python stdlib static OBJ/MTL",
                       "blender_export":"NOT RUN","fbx":"NOT RUN",
                       "glb":"NOT RUN","unity_import":"NOT RUN"})
    else:
        report.update(blender_export(parts,outdir))
    (outdir/(ID+"_manifest.json")).write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
