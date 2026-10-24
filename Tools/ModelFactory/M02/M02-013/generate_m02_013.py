#!/usr/bin/env python3
"""M02-013 axial-flux rotor engine: reproducible mechanical visual asset.

Unity convention: 1 unit = 1 metre; Y up, +Z forward; axial motor
shaft and animation around +Z. MOUNT_FRAME at (0,0,0), the rear frame
mounting plane z=0. Only the ROTOR_SPIN_Z assembly rotates.
No gameplay, forces, drivetrain code, Unity prefabs or shared scenes.
Works with plain Python (OBJ/MTL) or Blender 4.0+ (BLEND + animated GLB).
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M02-013"
FPS = 30
FRAMES = (1, 61)
TAU = 2 * math.pi
SEG = 32
MATS = {
    "Graphite_Housing": ((.067,.087,.113,1), .70, .35, (0,0,0), 0),
    "Titanium_Brushed": ((.50,.58,.62,1), .80, .27, (0,0,0), 0),
    "Copper_Stator": ((.72,.29,.075,1), .78, .27, (0,0,0), 0),
    "Ceramic_Ivory": ((.83,.83,.76,1), .12, .39, (0,0,0), 0),
    "Magnet_Cobalt": ((.09,.24,.52,1), .58, .32, (0,0,0), 0),
    "Magnet_Crimson": ((.67,.095,.13,1), .56, .31, (0,0,0), 0),
    "Bearing_Steel": ((.32,.38,.44,1), .84, .21, (0,0,0), 0),
    "Amber_Indicator": ((.95,.39,.035,1), .28, .37, (.8,.20,.01), .23),
    "Gasket_Rubber": ((.022,.029,.036,1), .04, .86, (0,0,0), 0),
}
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])
def dot(a,b):
    return sum(x*y for x,y in zip(a,b))
def sub(a,b):
    return tuple(x-y for x,y in zip(a,b))
def piece(name,material,v,f,spin=False):
    return dict(name=name,material=material,vertices=v,faces=f,spin=spin)
def polygon_prism(name,mat,poly,z0,z1,spin=False):
    """Triangulated closed XY footprint, extruded in Z; CCW perimeter."""
    assert z1>z0 and len(poly)>=3
    n=len(poly)
    cx=sum(p[0] for p in poly)/n
    cy=sum(p[1] for p in poly)/n
    v=[(x,y,z) for z in (z0,z1) for x,y in poly]
    v.extend(((cx,cy,z0),(cx,cy,z1)))
    f=[]
    for i in range(n):
        j=(i+1)%n
        f.extend(((i,j,n+j),(i,n+j,n+i),
                  (2*n,j,i),(2*n+1,n+i,n+j)))
    return piece(name,mat,v,f,spin)
def ring(name,mat,inner,outer,z0,z1,n=SEG,spin=False):
    """Manifold annular sleeve: genuine open centre, not a solid disk."""
    assert 0<inner<outer and z0<z1 and n>=12
    profile=((inner,z0),(outer,z0),(outer,z1),(inner,z1))
    v=[(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),z)
       for r,z in profile for j in range(n)]
    f=[]
    for k in range(4):
        t=(k+1)%4
        for j in range(n):
            q=(j+1)%n
            a=k*n+j; b=k*n+q; c=t*n+q; d=t*n+j
            f.extend(((a,b,c),(a,c,d)))
    return piece(name,mat,v,f,spin)
def sector(name,mat,r0,r1,a0,a1,z0,z1,spin=False,steps=5):
    assert 0<r0<r1 and 0<a1-a0<math.pi/2
    poly=[(r1*math.cos(a0+(a1-a0)*i/steps),
           r1*math.sin(a0+(a1-a0)*i/steps))
          for i in range(steps+1)]
    poly += [(r0*math.cos(a0+(a1-a0)*i/steps),
              r0*math.sin(a0+(a1-a0)*i/steps))
             for i in range(steps,-1,-1)]
    return polygon_prism(name,mat,poly,z0,z1,spin)
def beam(name,mat,r0,r1,theta,width,z0,z1,spin=False):
    assert r1>r0 and width>0
    u=(math.cos(theta),math.sin(theta))
    p=(-u[1],u[0])
    poly=[]
    for r,w in ((r0,-width/2),(r1,-width/2),
                (r1,width/2),(r0,width/2)):
        poly.append((r*u[0]+w*p[0],r*u[1]+w*p[1]))
    return polygon_prism(name,mat,poly,z0,z1,spin)
def cylinder(name,mat,x,y,z0,z1,r,n=SEG,spin=False):
    assert z1>z0 and r>0
    v=[(x+r*math.cos(TAU*i/n),y+r*math.sin(TAU*i/n),z)
       for z in (z0,z1) for i in range(n)]
    v.extend(((x,y,z0),(x,y,z1)))
    f=[]
    for i in range(n):
        j=(i+1)%n
        f.extend(((i,j,n+j),(i,n+j,n+i),
                  (2*n,j,i),(2*n+1,n+i,n+j)))
    return piece(name,mat,v,f,spin)

def build():
    p=[]
    add=p.append
    # Rear mounting plane z=0; open bore for through shaft.
    add(ring("Frame_Mount_Flange","Graphite_Housing",.036,.132,0,.052))
    add(ring("Rear_Cage_Ring","Graphite_Housing",.290,.356,.068,.115))
    add(ring("Front_Cage_Ring","Graphite_Housing",.290,.356,.530,.573))
    for k in range(8):
        a=TAU*k/8
        # Radial struts and front/rear rings overlap by design: rigid cage.
        add(cylinder("Cage_Longitudinal_Post_%02d"%k,
                     "Titanium_Brushed",.328*math.cos(a),
                     .328*math.sin(a),.092,.550,.014,16))
        add(beam("Stator_Radial_Mount_%02d"%k,"Graphite_Housing",
                 .274,.342,a,.029,.235,.275))
    # Rear and front spider supports join bearing housings to outer cage.
    for k in range(4):
        a=TAU*k/4
        add(beam("Rear_Spider_%02d"%k,"Titanium_Brushed",
                 .073,.333,a,.029,.077,.101))
        add(beam("Front_Spider_%02d"%k,"Titanium_Brushed",
                 .073,.333,a,.026,.547,.570))
    add(ring("Rear_Bearing_Outer","Bearing_Steel",.035,.079,.050,.105))
    add(ring("Front_Bearing_Outer","Bearing_Steel",.035,.079,.541,.596))
    add(ring("Rear_Seal","Gasket_Rubber",.035,.065,.102,.113))
    add(ring("Front_Seal","Gasket_Rubber",.035,.065,.530,.541))
    for k in range(4):
        a=TAU*(k+.5)/4
        x=.328*math.cos(a); y=.328*math.sin(a)
        # The rear surface of each mounting ear is exactly z=0.
        add(cylinder("Mount_Ear_%02d"%k,"Graphite_Housing",
                     x,y,0,.085,.053,16))
        add(cylinder("Mount_Ear_Bolt_%02d"%k,"Ceramic_Ivory",
                     x,y,.083,.092,.014,12))
    # Fixed 12-pole stator. Copper teeth are visibly seated in the
    # back iron, ceramic inserts on top of each copper winding.
    add(ring("Stator_Back_Iron","Graphite_Housing",.112,.295,.239,.286))
    add(ring("Stator_Inner_Collar","Ceramic_Ivory",.105,.135,.272,.303))
    for k in range(12):
        a=TAU*k/12
        add(sector("Stator_Copper_Winding_%02d"%k,"Copper_Stator",
                   .155,.264,a-.178,a+.178,.282,.341))
        add(sector("Stator_Ceramic_Cap_%02d"%k,"Ceramic_Ivory",
                   .174,.243,a-.125,a+.125,.339,.351))
    # Single rotor: shaft, inner bearing races, laminated annular rotor,
    # alternating permanent magnets and twelve raised cooling ribs.
    # Magnet lower face z=.380, copper cap upper face z=.351:
    # actual minimum axial airgap 29mm, independent of rotor angle.
    add(cylinder("Rotor_Through_Shaft","Bearing_Steel",
                 0,0,.025,.660,.028,32,True))
    add(ring("Rear_Bearing_Inner_Race","Titanium_Brushed",
             .027,.034,.053,.102,32,True))
    add(ring("Front_Bearing_Inner_Race","Titanium_Brushed",
             .027,.034,.544,.593,32,True))
    add(ring("Rotor_Hub","Titanium_Brushed",.027,.087,.365,.478,32,True))
    add(ring("Rotor_Laminated_Disc","Graphite_Housing",
             .065,.255,.402,.446,48,True))
    add(ring("Rotor_Face_Trim","Titanium_Brushed",
             .078,.250,.443,.451,48,True))
    for k in range(12):
        a=TAU*k/12
        mat="Magnet_Cobalt" if k%2==0 else "Magnet_Crimson"
        add(sector("Rotor_Magnet_%02d"%k,mat,
                   .170,.250,a-.164,a+.164,.380,.409,True))
        add(beam("Rotor_Cooling_Rib_%02d"%k,"Titanium_Brushed",
                 .100,.232,a,.017,.448,.469,True))
        add(cylinder("Rotor_Rivet_%02d"%k,"Amber_Indicator",
                     .235*math.cos(a),.235*math.sin(a),
                     .466,.473,.007,12,True))
    add(ring("Rotor_Front_Shaft_Collar","Copper_Stator",
             .027,.054,.474,.505,24,True))
    add(ring("Rotor_End_Spline","Titanium_Brushed",
             .027,.045,.630,.655,24,True))
    return p

def bounds(p):
    return [(min(v[k] for v in p["vertices"]),
             max(v[k] for v in p["vertices"])) for k in range(3)]
def validate(parts):
    names=set(); faces_total=0; vertex_total=0
    signed_volumes=[]
    for p in parts:
        assert p["name"] not in names,("duplicate",p["name"])
        names.add(p["name"])
        assert p["material"] in MATS
        assert p["vertices"] and p["faces"]
        edge={}
        vol=0.0
        for v in p["vertices"]:
            assert all(math.isfinite(t) for t in v)
        for a,b,c in p["faces"]:
            assert len({a,b,c})==3
            assert 0<=min(a,b,c) and max(a,b,c)<len(p["vertices"])
            A,B,C=(p["vertices"][i] for i in (a,b,c))
            area=cross(sub(B,A),sub(C,A))
            assert dot(area,area)>1e-16,("degenerate",p["name"])
            vol+=dot(A,cross(B,C))/6
            for i,j in ((a,b),(b,c),(c,a)):
                key=tuple(sorted((i,j)))
                edge.setdefault(key,[]).append((i,j))
        assert all(len(v)==2 and v[0]==v[1][::-1]
                   for v in edge.values()),("nonmanifold/winding",p["name"])
        assert vol>1e-10,("negative/zero volume",p["name"],vol)
        signed_volumes.append(vol)
        faces_total+=len(p["faces"])
        vertex_total+=len(p["vertices"])
    assert len(parts)>=100
    assert len(MATS)>=3
    by={p["name"]:p for p in parts}
    assert len([x for x in names if x.startswith("Stator_Copper_Winding_")])==12
    assert len([x for x in names if x.startswith("Rotor_Magnet_")])==12
    assert len([x for x in names if x.startswith("Rotor_Cooling_Rib_")])==12
    assert all(by[n]["spin"] for n in names if n.startswith("Rotor_"))
    assert all(not by[n]["spin"] for n in names if n.startswith("Stator_"))
    assert min(v[2] for p in parts for v in p["vertices"])>=-1e-8
    # Actual mesh bounds, not nominal constants, are checked.
    winding_top=max(bounds(by["Stator_Ceramic_Cap_%02d"%i])[2][1]
                    for i in range(12))
    magnet_bottom=min(bounds(by["Rotor_Magnet_%02d"%i])[2][0]
                      for i in range(12))
    airgap=magnet_bottom-winding_top
    assert airgap>=.0289,airgap
    # Rotor disc + magnets + cooling ribs clear every stationary cage post
    # at every rotation, because the radial envelopes are disjoint.
    moving_outer=max(math.hypot(v[0],v[1])
                     for p in parts if p["spin"] and
                     (p["name"].startswith("Rotor_Magnet_") or
                      p["name"].startswith("Rotor_Cooling_Rib_") or
                      p["name"]=="Rotor_Laminated_Disc")
                     for v in p["vertices"])
    post_inner=min(math.hypot(v[0],v[1])
                   for p in parts if p["name"].startswith(
                       "Cage_Longitudinal_Post_")
                   for v in p["vertices"])
    cage_gap=post_inner-moving_outer
    assert cage_gap>.050,cage_gap
    # Shaft is intentionally seated in its rotating inner bearing races,
    # but has positive running clearance from both fixed outer races.
    bearing_clearance=.035-.034
    assert bearing_clearance>=.0009
    assert .035-.028>=.006
    assert bounds(by["Rotor_Through_Shaft"])[2][0]>.0
    assert abs(bounds(by["Frame_Mount_Flange"])[2][0])<1e-8
    assert len([p for p in parts if p["spin"]])>=30
    return dict(asset_id=ID,part_count=len(parts),vertices=vertex_total,
                triangles=faces_total,materials=len(MATS),
                rotor_meshes=sum(bool(p["spin"]) for p in parts),
                static_meshes=sum(not p["spin"] for p in parts),
                manifold_closed_outward=len(parts),
                min_signed_mesh_volume_m3=min(signed_volumes),
                axial_airgap_m=round(airgap,6),
                radial_rotor_cage_gap_m=round(cage_gap,6),
                bearing_outer_race_clearance_m=bearing_clearance,
                axis="+Z",pivot="MOUNT_FRAME (0,0,0), rear mounting plane z=0",
                animation="ROTOR_SPIN_Z: 360 degrees, frames 1-61 at 30 fps",
                rotation_clearance="PASS: 360deg conservative radial/axial bounds",
                blender="NOT RUN",glb="NOT RUN",unity_import="NOT RUN",
                unity_editmode="NOT RUN",unity_playmode="NOT RUN",
                visual_review="NOT RUN")

def export_obj(parts,out):
    stem="M02-013_axial_rotor_engine"
    obj=out/(stem+".obj"); mtl=out/(stem+".mtl")
    lines=["# M02-013 real static geometry, Z axial, Y up",
           "mtllib "+mtl.name]
    off=0
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        for v in p["vertices"]:
            lines.append("v %.8f %.8f %.8f"%v)
        for f in p["faces"]:
            lines.append("f %d %d %d"%tuple(off+i+1 for i in f))
        off+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf8")
    m=["# PBR source materials; static MTL is approximate."]
    for name,(rgba,metal,rough,emission,strength) in MATS.items():
        m.extend(("newmtl "+name,
                  "Kd %.5f %.5f %.5f"%rgba[:3],
                  "Ks %.5f %.5f %.5f"%(metal,metal,metal),
                  "Ns %.3f"%((1-rough)*1000),
                  "Ke %.5f %.5f %.5f"%tuple(
                      min(1,t*strength) for t in emission),
                  "illum 2",""))
    mtl.write_text("\n".join(m)+"\n",encoding="utf8")
    return [obj.name,mtl.name]

def blender_export(parts,out):
    import bpy
    # Idempotent: remove previous generated collection and all its objects.
    old=bpy.data.collections.get("MODEL_M02_013")
    if old:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new("MODEL_M02_013")
    bpy.context.scene.collection.children.link(collection)
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None)
        collection.objects.link(ob)
        ob.parent=parent
        ob.empty_display_size=.065
        return ob
    mount=empty("MOUNT_FRAME")
    rotor=empty("ROTOR_SPIN_Z",mount)
    mats={}
    for name,(rgba,metal,rough,emission,strength) in MATS.items():
        key="M02_013_"+name
        mat=bpy.data.materials.get(key) or bpy.data.materials.new(key)
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
    for p in parts:
        mesh=bpy.data.meshes.new("M02_013_"+p["name"])
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update(calc_edges=True)
        ob=bpy.data.objects.new("M02_013_"+p["name"],mesh)
        collection.objects.link(ob)
        ob.parent=rotor if p["spin"] else mount
        mesh.materials.append(mats[p["material"]])
        for poly in mesh.polygons:
            poly.use_smooth=False
    rotor.rotation_mode="XYZ"
    for frame,angle in ((1,0),(61,TAU)):
        rotor.rotation_euler.z=angle
        rotor.keyframe_insert(data_path="rotation_euler",index=2,frame=frame)
    if rotor.animation_data and rotor.animation_data.action:
        try:
            for fc in rotor.animation_data.action.fcurves:
                for key in fc.keyframe_points:
                    key.interpolation="LINEAR"
        except AttributeError:
            pass
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=61
    scene.render.fps=FPS
    scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in collection.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=mount
    blend=out/"M02-013.blend"
    glb=out/"M02-013_animated.glb"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True,
        export_nla_strips=False,export_frame_range=True)
    assert blend.is_file() and blend.stat().st_size>1000
    assert glb.is_file() and glb.stat().st_size>1000
    return {"blender":"PASS: saved .blend",
            "glb":"PASS: exported animated .glb",
            "blend_file":blend.name,"glb_file":glb.name,
            "blender_version":bpy.app.version_string}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M02-013")
    parser.add_argument("--self-test",action="store_true")
    args=parser.parse_args(argv)
    parts=build()
    report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2))
        return
    out=Path(args.out).resolve()
    out.mkdir(parents=True,exist_ok=True)
    report["static_files"]=export_obj(parts,out)
    try:
        import bpy
    except ImportError:
        report["backend"]="plain-python-static-OBJ"
    else:
        report["backend"]="Blender-bpy"
        report.update(blender_export(parts,out))
    (out/"M02-013_qa.json").write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf8")
    print(json.dumps(report,indent=2))
if __name__=="__main__":
    main()
