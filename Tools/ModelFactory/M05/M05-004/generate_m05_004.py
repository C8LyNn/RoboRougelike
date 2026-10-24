#!/usr/bin/env python3
"""M05-004 dome lidar: deterministic OBJ/MTL + Blender animated GLB source.
All dimensions are metres. Blender Z-up, glTF export Y-up. Pure-Python
--self-test does not import bpy; it produces an actual static OBJ asset.
"""
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

ID = "M05-004"
N = 48
PIVOT_Z = 0.43
DOME_CENTER_Z = 0.34
DOME_OUTER_R = 0.430
DOME_INNER_R = 0.418
ROTOR_MAX_RADIUS = 0.302
ROTOR_MAX_Z = 0.600
ROTOR_MIN_Z = 0.385
ROTATION_FRAMES = (1, 31, 61, 91, 121)
PALETTE = {
    "graphite": {"rgb": (0.060, 0.085, 0.112), "metallic": .65, "roughness": .38},
    "titanium": {"rgb": (.36, .45, .51), "metallic": .78, "roughness": .29},
    "ceramic": {"rgb": (.77, .81, .80), "metallic": .15, "roughness": .32},
    "amber": {"rgb": (.97, .47, .045), "metallic": .27, "roughness": .37},
    "scan_cyan": {"rgb": (.025, .57, .93), "metallic": .20, "roughness": .21, "emission": 1.9},
    "smoke_glass": {"rgb": (.26, .49, .61), "metallic": .02, "roughness": .12, "alpha": .23},
}
PARTS = []

def add(name, kind, material, group="fixed", **params):
    assert name not in {p["name"] for p in PARTS}
    PARTS.append(dict(name=name, kind=kind, material=material, group=group, params=params))

# Root mount at (0, 0, 0), top-socket contact; body occupies Z >= 0.
add("Mount_foot", "cylinder", "graphite", r=.260, z0=0, z1=.070)
add("Mount_contact_ring", "annulus", "titanium", ro=.315, ri=.265, z0=.052, z1=.090)
add("Lower_housing", "cylinder", "graphite", r=.345, z0=.082, z1=.253)
add("Shoulder_collar", "annulus", "ceramic", ro=.395, ri=.352, z0=.245, z1=.291)
add("Dome_guard", "annulus", "titanium", ro=.490, ri=.455, z0=.285, z1=.332)
add("Sealed_transparent_dome", "dome", "smoke_glass", ro=DOME_OUTER_R,
    ri=DOME_INNER_R, cz=DOME_CENTER_Z, rings=14)
for i in range(8):
    angle = 2 * math.pi * i / 8
    add("Housing_stripe_%02d" % i, "box", "amber" if i % 2 == 0 else "titanium",
        x=.347*math.cos(angle), y=.347*math.sin(angle), z=.173,
        sx=.070, sy=.035, sz=.100, yaw=angle)
for i in range(4):
    a = 2*math.pi*i/4
    add("Dome_clamp_%02d" % i, "box", "graphite",
        x=.448*math.cos(a), y=.448*math.sin(a), z=.291,
        sx=.045, sy=.072, sz=.070, yaw=a)

# The entire optical head rotates as one assembly around (0,0,0.43), Z axis.
add("Rotor_shaft", "cylinder", "titanium", "spin", r=.080, z0=.385, z1=.493)
add("Rotor_carrier", "cylinder", "graphite", "spin", r=.255, z0=.445, z1=.491)
add("Rotor_guide_ring", "annulus", "ceramic", "spin", ro=.285, ri=.256, z0=.443, z1=.465)
add("Rotor_top_deck", "cylinder", "titanium", "spin", r=.208, z0=.488, z1=.515)
for i in range(4):
    a = 2*math.pi*i/4
    x,y=.166*math.cos(a),.166*math.sin(a)
    add("Emitter_shroud_%02d" % i, "box", "graphite", "spin",
        x=x,y=y,z=.540,sx=.122,sy=.110,sz=.052,yaw=a)
    add("Optic_bezel_%02d" % i, "annulus", "ceramic", "spin",
        ro=.060,ri=.043,z0=.563,z1=.576,x=x,y=y)
    add("Optic_lens_%02d" % i, "cylinder", "scan_cyan", "spin",
        r=.039,z0=.563,z1=.579,x=x,y=y)
for i in range(8):
    a=2*math.pi*i/8
    add("Rotor_tick_%02d" % i, "box", "amber" if i%2==0 else "scan_cyan", "spin",
        x=.244*math.cos(a),y=.244*math.sin(a),z=.504,
        sx=.034,sy=.021,sz=.018,yaw=a)

def cylinder(r,z0,z1,x=0.0,y=0.0):
    v=[]; f=[]
    for z in (z0,z1):
        for j in range(N):
            a=2*math.pi*j/N
            v.append((x+r*math.cos(a),y+r*math.sin(a),z))
    low=len(v);v.append((x,y,z0))
    high=len(v);v.append((x,y,z1))
    for j in range(N):
        k=(j+1)%N
        f.append((j,k,N+k,N+j))
        f.append((high,N+j,N+k))
        f.append((low,k,j))
    return v,f

def annulus(ro,ri,z0,z1,x=0.0,y=0.0):
    v=[];f=[]
    for radius,z in ((ro,z0),(ro,z1),(ri,z0),(ri,z1)):
        for j in range(N):
            a=2*math.pi*j/N
            v.append((x+radius*math.cos(a),y+radius*math.sin(a),z))
    for j in range(N):
        k=(j+1)%N
        f.extend((
            (j,k,N+k,N+j),
            (N+j,N+k,3*N+k,3*N+j),
            (2*N+j,3*N+j,3*N+k,2*N+k),
            (j,2*N+j,2*N+k,k),
        ))
    return v,f

def box(x,y,z,sx,sy,sz,yaw=0.0):
    c,s=math.cos(yaw),math.sin(yaw)
    v=[]
    for dx,dy,dz in ((-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
                     (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)):
        dx*=sx/2;dy*=sy/2
        v.append((x+c*dx-s*dy,y+s*dx+c*dy,z+dz*sz/2))
    return v,[(3,2,1,0),(4,5,6,7),(0,1,5,4),
              (3,7,6,2),(0,4,7,3),(1,2,6,5)]

def dome(ro,ri,cz,rings):
    # Closed 12 mm shell, open visually at its lower rim. Both spherical
    # surfaces and the lower annular lip are manifold; no zero-area pole faces.
    v=[];f=[]
    for radius in (ro,ri):
        v.append((0,0,cz+radius))
        for i in range(1,rings+1):
            theta=(math.pi/2)*i/rings
            for j in range(N):
                a=2*math.pi*j/N
                v.append((radius*math.sin(theta)*math.cos(a),
                          radius*math.sin(theta)*math.sin(a),
                          cz+radius*math.cos(theta)))
    inner_offset=1+rings*N
    for surface in (0,1):
        offset=0 if surface==0 else inner_offset
        for j in range(N):
            k=(j+1)%N
            face=(offset,offset+1+j,offset+1+k)
            f.append(face if surface==0 else tuple(reversed(face)))
        for ring in range(rings-1):
            upper=offset+1+ring*N
            lower=upper+N
            for j in range(N):
                k=(j+1)%N
                face=(upper+j,lower+j,lower+k,upper+k)
                f.append(face if surface==0 else tuple(reversed(face)))
    outer_bottom=1+(rings-1)*N
    inner_bottom=inner_offset+1+(rings-1)*N
    for j in range(N):
        k=(j+1)%N
        f.append((outer_bottom+j,inner_bottom+j,inner_bottom+k,outer_bottom+k))
    return v,f

GENERATORS={"cylinder":cylinder,"annulus":annulus,"box":box,"dome":dome}

def build_meshes():
    result=[]
    for p in PARTS:
        vertices,faces=GENERATORS[p["kind"]](**p["params"])
        result.append(dict(**p,vertices=vertices,faces=faces))
    return result

def verify(meshes):
    assert len(meshes)==len(PARTS) and len(meshes)>=30
    assert len(set(p["material"] for p in meshes))>=5
    assert all(p["material"] in PALETTE for p in meshes)
    assert all(p["group"] in ("fixed","spin") for p in meshes)
    minv=[float("inf")]*3;maxv=[-float("inf")]*3
    tris=0;edges_checked=0
    for p in meshes:
        vertices,faces=p["vertices"],p["faces"]
        assert len(vertices)>0 and len(faces)>0
        edges=Counter()
        for face in faces:
            assert len(face)>=3 and len(set(face))==len(face)
            assert all(0<=i<len(vertices) for i in face)
            tris+=len(face)-2
            for i,a in enumerate(face):
                b=face[(i+1)%len(face)]
                edges[tuple(sorted((a,b)))]+=1
        # Every primitive is closed manifold with exactly two incident faces
        # per undirected edge. This catches torn rims and dangling polygons.
        assert all(count==2 for count in edges.values()),p["name"]
        edges_checked+=len(edges)
        for vertex in vertices:
            assert all(math.isfinite(v) for v in vertex)
            for i in range(3):
                minv[i]=min(minv[i],vertex[i]);maxv[i]=max(maxv[i],vertex[i])
    assert all(-.50000001<=v<=.50000001 for v in (minv[0],maxv[0],minv[1],maxv[1]))
    assert minv[2]>=-1e-9 and maxv[2]<=.77000001
    assert abs(DOME_OUTER_R-DOME_INNER_R-.012)<1e-8
    # Rotor never reaches the fixed dome at its most restrictive radial edge.
    dome_ceiling_at_rotor_edge=DOME_CENTER_Z+math.sqrt(DOME_INNER_R**2-ROTOR_MAX_RADIUS**2)
    assert ROTOR_MAX_Z < dome_ceiling_at_rotor_edge-.020
    # Rotor lower extent is above the fixed lower housing, guard and clamps.
    assert ROTOR_MIN_Z>.332+.040
    assert len(ROTATION_FRAMES)==5 and ROTATION_FRAMES[0]==1
    return {
        "id":ID,"status":"STATIC_QA_PASS","parts":len(meshes),
        "fixed_parts":sum(p["group"]=="fixed" for p in meshes),
        "rotating_parts":sum(p["group"]=="spin" for p in meshes),
        "materials_used":sorted(set(p["material"] for p in meshes)),
        "vertices":sum(len(p["vertices"]) for p in meshes),
        "polygons":sum(len(p["faces"]) for p in meshes),
        "triangles":tris,"manifold_edges_checked":edges_checked,
        "bounds_m":{"min":[round(v,6) for v in minv],
                    "max":[round(v,6) for v in maxv]},
        "mount_m":[0,0,0],"rotor_pivot_m":[0,0,PIVOT_Z],
        "rotor_axis":"Blender +Z; glTF/Unity +Y after export_yup",
        "animation_keyframes":list(ROTATION_FRAMES),
        "rotation_degrees":[0,90,180,270,360],
        "rotor_to_dome_ceiling_clearance_m":round(dome_ceiling_at_rotor_edge-ROTOR_MAX_Z,6),
        "rotor_to_lower_guard_clearance_m":round(ROTOR_MIN_Z-.332,6),
        "collision_method":"analytic radial/vertical envelopes + closed-manifold mesh; NOT Unity physics",
        "blender":"NOT RUN","glb":"NOT RUN","unity":"NOT RUN","visual":"NOT RUN",
    }

def export_obj(out,meshes):
    out.mkdir(parents=True,exist_ok=True)
    mtl=out/(ID+".mtl")
    with mtl.open("w",encoding="utf-8",newline="\n") as file:
        file.write("# M05-004 reproducible material palette\n")
        for name,properties in PALETTE.items():
            r,g,b=properties["rgb"]
            file.write("newmtl %s\nKd %.6f %.6f %.6f\n" % (name,r,g,b))
            file.write("Ka 0.03 0.03 0.03\nKs 0.35 0.35 0.35\nNs 120\n")
            if "alpha" in properties:
                file.write("d %.6f\n" % properties["alpha"])
            if properties.get("emission"):
                file.write("Ke %.6f %.6f %.6f\n" % (r,g,b))
            file.write("\n")
    obj=out/(ID+".obj")
    with obj.open("w",encoding="utf-8",newline="\n") as file:
        file.write("# M05-004 dome lidar static frame 1; Blender Z-up, 1 unit = 1 m\n")
        file.write("mtllib "+mtl.name+"\n")
        offset=1
        for p in meshes:
            file.write("o "+p["name"]+"\nusemtl "+p["material"]+"\n")
            for v in p["vertices"]:
                file.write("v %.6f %.6f %.6f\n"%v)
            for face in p["faces"]:
                file.write("f "+" ".join(str(offset+i) for i in face)+"\n")
            offset+=len(p["vertices"])
    return obj,mtl

def build_blender(out,meshes):
    import bpy
    from mathutils import Quaternion,Vector
    # Idempotent cleanup restricted to this model's own collection.
    old=bpy.data.collections.get(ID+"_ASSET")
    if old:
        for ob in tuple(old.objects):
            bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(ID+"_ASSET")
    bpy.context.scene.collection.children.link(col)
    mats={}
    for name,properties in PALETTE.items():
        mat=bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(ID+"_"+name)
        mat.diffuse_color=(*properties["rgb"],properties.get("alpha",1.0))
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            for key,value in (("Base Color",(*properties["rgb"],1.0)),
                              ("Metallic",properties["metallic"]),
                              ("Roughness",properties["roughness"])):
                if bsdf.inputs.get(key):
                    bsdf.inputs[key].default_value=value
            if properties.get("emission"):
                if bsdf.inputs.get("Emission Color"):
                    bsdf.inputs["Emission Color"].default_value=(*properties["rgb"],1)
                if bsdf.inputs.get("Emission Strength"):
                    bsdf.inputs["Emission Strength"].default_value=properties["emission"]
            if "alpha" in properties:
                if bsdf.inputs.get("Alpha"):
                    bsdf.inputs["Alpha"].default_value=properties["alpha"]
                if bsdf.inputs.get("Transmission Weight"):
                    bsdf.inputs["Transmission Weight"].default_value=.24
        if "alpha" in properties:
            if hasattr(mat,"surface_render_method"):
                mat.surface_render_method="DITHERED"
            elif hasattr(mat,"blend_method"):
                mat.blend_method="BLEND"
        mats[name]=mat
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None)
        col.objects.link(ob)
        ob.empty_display_type="PLAIN_AXES"
        ob.empty_display_size=.09
        if parent: ob.parent=parent
        return ob
    root=empty(ID+"_MOUNT_TOP_SOCKET")
    root["port"]="TOP_SOCKET"
    root["mount_local_m"]=[0.0,0.0,0.0]
    root["scale"]="1 unit = 1 metre"
    rotor=empty(ID+"_LIDAR_ROTOR",root)
    rotor.location=(0,0,PIVOT_Z)
    rotor.rotation_mode="QUATERNION"
    rotor["pivot_local_m"]=[0.0,0.0,PIVOT_Z]
    rotor["axis"]="Blender +Z, glTF Y-up"
    rotor["max_rotation_deg"]=360
    for p in meshes:
        shift=PIVOT_Z if p["group"]=="spin" else 0.0
        vertices=[(v[0],v[1],v[2]-shift) for v in p["vertices"]]
        mesh=bpy.data.meshes.new(ID+"_"+p["name"]+"_mesh")
        mesh.from_pydata(vertices,[],p["faces"])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new(ID+"_"+p["name"],mesh)
        col.objects.link(obj)
        obj.parent=rotor if p["group"]=="spin" else root
        obj.data.materials.append(mats[p["material"]])
        for face in mesh.polygons:
            face.use_smooth=p["kind"] in ("cylinder","annulus","dome")
        obj["asset_id"]=ID
        obj["motion_group"]=p["group"]
    # Quaternion 0->360 needs intermediate keys: otherwise endpoints are
    # equivalent and Blender/glTF interpolation may produce zero rotation.
    for frame,degrees in zip(ROTATION_FRAMES,(0,90,180,270,360)):
        angle=math.radians(degrees)
        rotor.rotation_quaternion=Quaternion((0,0,1),angle)
        rotor.keyframe_insert(data_path="rotation_quaternion",frame=frame)
    action=rotor.animation_data.action if rotor.animation_data else None
    if action:
        try:
            for curve in action.fcurves:
                for key in curve.keyframe_points:
                    key.interpolation="LINEAR"
        except AttributeError:
            pass  # Blender 4.x/5.x action APIs vary; keys remain valid.
    scene=bpy.context.scene
    scene.frame_start=1;scene.frame_end=121;scene.frame_set(1)
    # Export ONLY this asset, never other collections in the artist's scene.
    previous_selection=tuple(bpy.context.selected_objects)
    previous_active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in col.objects: obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(
            filepath=str(out/(ID+"_animated.glb")),
            export_format="GLB",use_selection=True,
            export_animations=True,export_yup=True,
            export_force_sampling=True,
        )
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in previous_selection:
            if obj.name in bpy.data.objects: obj.select_set(True)
        if previous_active and previous_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=previous_active
    # Saving .blend stores the entire open scene. Use --factory-startup for
    # a self-contained .blend when executing this generator.
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ID+".blend")))
    return [ID+".blend",ID+"_animated.glb"]

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-dir",default=str(Path(__file__).resolve().parent))
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    opt=parser.parse_args(args)
    out=Path(opt.output_dir).resolve()
    meshes=build_meshes()
    report=verify(meshes)
    obj,mtl=export_obj(out,meshes)
    report["obj"]=obj.name;report["mtl"]=mtl.name
    if not opt.self_test:
        try: import bpy  # noqa: F401
        except ImportError:
            if opt.require_blender:
                raise RuntimeError("bpy unavailable; GLB and .blend NOT RUN")
        else:
            report["blender_outputs"]=build_blender(out,meshes)
            report["blender"]="EXECUTED"
            report["glb"]="EXPORTED_NOT_UNITY_TESTED"
    (out/(ID+".static-qa.json")).write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))

if __name__=="__main__":
    main()
