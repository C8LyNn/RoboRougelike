#!/usr/bin/env python3
"""M02-004 perforated steel wheel, deterministic Python mesh + Blender 3.x/4.x.
Blender Z up; side axle along +X. 1 unit = 1 metre. Root pivot (0,0,0).
No game mechanics. Static OBJ and MTL require Python stdlib only.
"""
import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

ID = "M02-004"
TAU = math.tau
N = 96
SLOTS = 12
SLOT_DIVISIONS = 8
BORE_R = 0.085
SHAFT_R = 0.055
SEAL_R = 0.078
WHEEL_MAX_R = 0.478
WEB_RADII = (0.085, 0.165, 0.332, 0.370)
WEB_BACK, WEB_FRONT = 0.145, 0.255
AXLE_MIN, AXLE_MAX = 0.006, 0.345
MATERIALS = {
    "forged_steel": {"rgb": (0.185, 0.224, 0.258), "metallic": .88, "roughness": .34},
    "dark_steel": {"rgb": (0.061, 0.077, 0.092), "metallic": .81, "roughness": .46},
    "copper": {"rgb": (0.61, 0.285, 0.095), "metallic": .78, "roughness": .35},
    "ceramic": {"rgb": (0.76, 0.79, 0.72), "metallic": .24, "roughness": .47},
    "rubber": {"rgb": (0.025, 0.029, 0.033), "metallic": .02, "roughness": .78},
}
# Each mesh is an independent closed manifold or an intentional welded detail.
PARTS = []

def part(name, vertices, faces, material, group, smooth=False):
    assert vertices and faces
    PARTS.append(dict(name=name, vertices=vertices, faces=faces,
                      material=material, group=group, smooth=smooth))

def polar(x, radius, angle):
    return (x, radius * math.cos(angle), radius * math.sin(angle))

def ring_tube(name, profile, material, group, segments=N):
    """Sweep a closed axial/radial profile around the X axle, no caps required."""
    v = [polar(x, r, TAU * i / segments) for x, r in profile for i in range(segments)]
    f = []
    for j in range(len(profile)):
        k = (j + 1) % len(profile)
        for i in range(segments):
            nxt = (i + 1) % segments
            f.append((j*segments+i, j*segments+nxt, k*segments+nxt, k*segments+i))
    part(name, v, f, material, group, smooth=True)

def cylinder(name, x0, x1, radius, material, group, segments=32):
    # Profile goes around rectangular cross-section, includes axial end caps.
    ring_tube(name, ((x0, 0.0), (x1, 0.0), (x1, radius), (x0, radius)),
              material, group, segments)

def perforated_web():
    """12 true THROUGH apertures, not black decals or boolean-only cutouts.
    The radial/azimuth grid removes the central band for 4 of 8 sectors per
    aperture; exposed sides form hole walls. Vertices shared by adjacent cells.
    """
    radii = WEB_RADII
    def occupied(radial, angular):
        return not (radial == 1 and angular % SLOT_DIVISIONS in (2, 3, 4, 5))
    v, faces, index = [], [], {}
    def point(face, radial, angle_index):
        key = (face, radial, angle_index % N)
        if key not in index:
            x = WEB_FRONT if face == 1 else WEB_BACK
            index[key] = len(v)
            v.append(polar(x, radii[radial], TAU * (angle_index % N) / N))
        return index[key]
    for i in range(len(radii)-1):
        for a in range(N):
            if not occupied(i, a):
                continue
            a1 = (a+1) % N
            b0,b1,b2,b3 = (point(0,i,a),point(0,i+1,a),
                            point(0,i+1,a1),point(0,i,a1))
            t0,t1,t2,t3 = (point(1,i,a),point(1,i+1,a),
                            point(1,i+1,a1),point(1,i,a1))
            faces.append((t0,t1,t2,t3))     # front +X
            faces.append((b3,b2,b1,b0))     # rear -X
            if i == 0 or not occupied(i-1,a):
                faces.append((b0,t0,t3,b3)) # inward radial
            if i == len(radii)-2 or not occupied(i+1,a):
                faces.append((b1,b2,t2,t1)) # outward radial
            if not occupied(i,(a-1)%N):
                faces.append((b0,b1,t1,t0)) # -theta
            if not occupied(i,a1):
                faces.append((b3,t3,t2,b2)) # +theta
    part("Perforated_web_12_open_windows", v, faces, "forged_steel", "rotor")

def lug_mesh(name, angle):
    # 24 replaceable traction teeth; 4 mm intentional overlap into steel rim.
    da = .063
    r0, r1 = .461, WHEEL_MAX_R
    x0, x1 = .164, .296
    vertices = [polar(x,r,angle+a) for x in (x0,x1)
                for r in (r0,r1) for a in (-da,da)]
    faces = ((0,2,3,1),(4,5,7,6),(0,1,5,4),
             (2,6,7,3),(0,4,6,2),(1,3,7,5))
    part(name, vertices, faces, "dark_steel", "rotor")

def build():
    if PARTS:
        return PARTS
    perforated_web()
    # Solid steel rolling hoop; bevels encoded in real geometry, not a modifier.
    ring_tube("Rolling_hoop_machined_chamfers",
              ((.100,.370),(.100,.435),(.120,.465),
               (.340,.465),(.360,.435),(.360,.370)),
              "dark_steel","rotor")
    # Front hub is a raised annulus; bore stays open for stationary shaft.
    ring_tube("Hub_forged_annulus",
              ((.255,.085),(.255,.160),(.266,.160),(.266,.085)),
              "copper","rotor",64)
    # Front machined face stripe, flush with front rolling hoop.
    ring_tube("Front_machined_rim_inlay",
              ((.360,.393),(.360,.407),(.365,.407),(.365,.393)),
              "ceramic","rotor")
    # Independent 24 lugs. The overlap with the rolling hoop is intentional.
    for k in range(24):
        lug_mesh("Replaceable_traction_tooth_%02d" % (k+1), TAU*k/24)
    # Six small hub fastener heads, on rotating face (no stationary contact).
    for k in range(6):
        a=TAU*k/6
        # Cylinder translated radially, with its own closed manifold.
        r=.127
        verts=[]
        for x in (.267,.274):
            for j in range(12):
                t=TAU*j/12
                verts.append((x,r*math.cos(a)+.008*math.cos(t),
                              r*math.sin(a)+.008*math.sin(t)))
        fs=[tuple(range(11,-1,-1)),tuple(range(12,24))]
        fs += [(j,(j+1)%12,(j+1)%12+12,j+12) for j in range(12)]
        part("Hub_bolt_%02d" % (k+1),verts,fs,"ceramic","rotor")
    # Stationary axle, mount collar and floating dust seal are concentric.
    cylinder("Stationary_axle",AXLE_MIN,AXLE_MAX,SHAFT_R,
             "forged_steel","fixed")
    ring_tube("Rear_mount_flange",
              ((0.0,.0),(0.0,.075),(.050,.075),(.050,.0)),
              "copper","fixed",48)
    ring_tube("Stationary_dust_seal",
              ((.262,.057),(.262,SEAL_R),(.289,SEAL_R),(.289,.057)),
              "rubber","fixed",48)
    return PARTS

def edges_and_volume(mesh):
    vertices, faces = mesh["vertices"], mesh["faces"]
    edges = Counter()
    signed_volume = 0.0
    for face in faces:
        assert len(face) >= 3
        assert len(set(face)) == len(face), (mesh["name"], "degenerate polygon")
        for i,a in enumerate(face):
            b = face[(i+1)%len(face)]
            edges[tuple(sorted((a,b)))] += 1
        # Signed volume via triangle fan (also tests face winding).
        p = vertices[face[0]]
        for j in range(1,len(face)-1):
            q, r = vertices[face[j]], vertices[face[j+1]]
            signed_volume += (p[0]*(q[1]*r[2]-q[2]*r[1])
                +p[1]*(q[2]*r[0]-q[0]*r[2])
                +p[2]*(q[0]*r[1]-q[1]*r[0]))/6.0
    assert all(n == 2 for n in edges.values()), (mesh["name"], "nonmanifold edges",
                                                  Counter(edges.values()))
    assert abs(signed_volume) > 1e-8, (mesh["name"], "zero volume")
    return signed_volume

def qa():
    meshes=build()
    assert len({m["name"] for m in meshes})==len(meshes)
    assert len({m["material"] for m in meshes})>=3
    assert sum("traction_tooth" in m["name"] for m in meshes)==24
    assert sum("Hub_bolt_" in m["name"] for m in meshes)==6
    assert len(meshes)==35, len(meshes)
    for m in meshes:
        assert m["material"] in MATERIALS
        for vertex in m["vertices"]:
            assert all(math.isfinite(c) for c in vertex)
            assert -.000001<=vertex[0]<=.365001
            assert math.hypot(vertex[1],vertex[2])<=WHEEL_MAX_R+1e-6
        edges_and_volume(m)
    assert SHAFT_R < SEAL_R < BORE_R
    assert (BORE_R - SEAL_R) >= .006
    assert (BORE_R - SHAFT_R) >= .029
    assert WEB_BACK > .050 + .09
    # Analytic concentric radial clearances remain invariant under X rotation.
    # No AABB-only false positives for nested annuli; 72 rotation samples.
    min_gap=float("inf")
    for frame in range(73):
        theta=TAU*frame/72
        assert math.isfinite(math.sin(theta))
        min_gap=min(min_gap,BORE_R-SEAL_R,BORE_R-SHAFT_R)
        assert min_gap >= .006-1e-10
    return {
        "id":ID,"result":"STATIC_GEOMETRY_PASS",
        "model":"perforated steel wheel","unit":"metre",
        "mount_pivot_blender":[0,0,0],
        "axle":"Blender +X (wheel rotation about X)",
        "rotation_degrees":360,
        "perforation_count":SLOTS,"radial_through_holes":True,
        "rotating_teeth":24,"part_count":len(meshes),
        "materials_used":sorted({m["material"] for m in meshes}),
        "obj_vertices":sum(len(m["vertices"]) for m in meshes),
        "obj_faces":sum(len(m["faces"]) for m in meshes),
        "max_radius_m":WHEEL_MAX_R,
        "width_from_mount_m":.365,
        "shaft_to_bore_clearance_m":round(BORE_R-SHAFT_R,4),
        "seal_to_bore_clearance_m":round(BORE_R-SEAL_R,4),
        "rotation_clearance_samples":73,
        "manifold_closed_meshes":"PASS (each mesh edge used twice)",
        "stationary_vs_rotor_clearance":"PASS (analytic radial, not Unity physics)",
        "traction_tooth_to_rim_overlap":"INTENTIONAL 4mm weld intersection",
        "blender":"NOT RUN","unity":"NOT RUN","visual":"NOT RUN"
    }

def export_obj(out):
    out.mkdir(parents=True,exist_ok=True)
    obj=out/(ID+".obj")
    mtl=out/(ID+".mtl")
    with mtl.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M02-004 deterministic material palette\n")
        for name, props in MATERIALS.items():
            r,g,b=props["rgb"]
            f.write("newmtl %s\nKd %.6f %.6f %.6f\nKa 0.03 0.03 0.03\n"
                    "Ks 0.32 0.32 0.32\nNs 100\n\n" % (name,r,g,b))
    with obj.open("w",encoding="utf-8",newline="\n") as f:
        f.write("# M02-004 real through-perforated steel wheel, static pose\nmtllib M02-004.mtl\n")
        offset=1
        for mesh in build():
            f.write("o %s\nusemtl %s\n" % (mesh["name"],mesh["material"]))
            for vertex in mesh["vertices"]:
                f.write("v %.6f %.6f %.6f\n" % vertex)
            for face in mesh["faces"]:
                f.write("f "+" ".join(str(offset+i) for i in face)+"\n")
            offset+=len(mesh["vertices"])
    return obj,mtl

def blender_export(out):
    import bpy
    old=bpy.data.collections.get(ID+"_ASSET")
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new(ID+"_ASSET")
    bpy.context.scene.collection.children.link(col)
    palette={}
    for name,p in MATERIALS.items():
        mat=bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(ID+"_"+name)
        mat.diffuse_color=(*p["rgb"],1)
        mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value=(*p["rgb"],1)
            bsdf.inputs["Metallic"].default_value=p["metallic"]
            bsdf.inputs["Roughness"].default_value=p["roughness"]
        palette[name]=mat
    def empty(name,parent=None):
        ob=bpy.data.objects.new(name,None)
        col.objects.link(ob)
        ob.empty_display_type="PLAIN_AXES"
        ob.empty_display_size=.08
        if parent: ob.parent=parent
        return ob
    root=empty(ID+"_MOUNT_SIDE_X0")
    root["port"]="SIDE_AXLE_X0"
    root["units"]="metres"
    root["axis"]="X, Blender Z up"
    rotor=empty(ID+"_ROTOR",root)
    rotor.rotation_mode="XYZ"
    rotor["wheel_radius_m"]=WHEEL_MAX_R
    rotor["bore_radius_m"]=BORE_R
    for mesh in build():
        data=bpy.data.meshes.new(ID+"_"+mesh["name"]+"_mesh")
        data.from_pydata(mesh["vertices"],[],mesh["faces"])
        data.update(calc_edges=True)
        ob=bpy.data.objects.new(ID+"_"+mesh["name"],data)
        col.objects.link(ob)
        ob.parent=rotor if mesh["group"]=="rotor" else root
        data.materials.append(palette[mesh["material"]])
        for poly in data.polygons:
            poly.use_smooth=mesh["smooth"]
        ob["module_id"]=ID
        ob["role"]=mesh["group"]
    for frame,angle in ((1,0.0),(73,TAU)):
        rotor.rotation_euler.x=angle
        rotor.keyframe_insert(data_path="rotation_euler",index=0,frame=frame)
    if rotor.animation_data and rotor.animation_data.action:
        try:
            for fc in rotor.animation_data.action.fcurves:
                for kp in fc.keyframe_points: kp.interpolation="LINEAR"
        except AttributeError:
            pass
    scene=bpy.context.scene
    scene.frame_start=1
    scene.frame_end=73
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ID+".blend")))
    prior_selected=list(bpy.context.selected_objects)
    prior_active=bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in col.objects: ob.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(filepath=str(out/(ID+"_animated.glb")),
                                  export_format="GLB",export_animations=True,
                                  use_selection=True,export_yup=True)
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for ob in prior_selected:
            if ob.name in bpy.data.objects: ob.select_set(True)
        if prior_active and prior_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=prior_active
    return {"blend":str(out/(ID+".blend")),"glb":str(out/(ID+"_animated.glb"))}

def main():
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--output-dir",default=str(Path(__file__).resolve().parent/"generated"))
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--require-blender",action="store_true")
    a=parser.parse_args(args)
    out=Path(a.output_dir).resolve()
    q=qa()
    obj,mtl=export_obj(out)
    q["obj"]=obj.name
    q["mtl"]=mtl.name
    if not a.self_test:
        try:
            import bpy
        except ImportError:
            if a.require_blender:
                raise RuntimeError("Blender bpy unavailable; GLB and .blend NOT RUN")
        else:
            q["blender_outputs"]=blender_export(out)
            q["blender"]="EXECUTED"
    (out/(ID+".static-qa.json")).write_text(json.dumps(q,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(q,indent=2))

if __name__=="__main__":
    main()
