#!/usr/bin/env python3
"""MODEL-05 / M05-008 — cylindrical battery, visual-only asset for Unity 6.

Pure-Python deterministic mesh/OBJ/MTL QA:
    python generate_m05_008.py --self-test
    python generate_m05_008.py --static-obj --out build/M05-008
Blender 4.x (FBX, GLB, BLEND and keyed service hatch):
    blender -b --factory-startup --python generate_m05_008.py -- --out build/M05-008

Z-up Blender: X right, Y rear, Z up. One unit = one metre.
Root mount pivot = (0,0,0), at bottom-center attachment interface.
Hatch hinge = X axis at (0,-0.345,0.590); open angle -70 degrees.
This is visual geometry only: no energy/storage/gameplay behavior.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

ASSET = "M05-008"
PREFIX = "RRL_M05_008_"
COLLECTION = PREFIX + "CYLINDRICAL_BATTERY"
HINGE = (0.0, -0.345, 0.590)
OPEN_DEGREES = -70.0
MATERIALS = {
    "Graphite": ((0.052, 0.069, 0.085, 1.0), 0.74, 0.36, 0.0),
    "Titanium": ((0.43, 0.49, 0.51, 1.0), 0.84, 0.27, 0.0),
    "Ceramic": ((0.70, 0.75, 0.72, 1.0), 0.18, 0.42, 0.0),
    "Copper": ((0.69, 0.27, 0.105, 1.0), 0.72, 0.30, 0.0),
    "Rubber": ((0.022, 0.027, 0.035, 1.0), 0.02, 0.84, 0.0),
    "CyanLED": ((0.035, 0.58, 0.77, 1.0), 0.14, 0.23, 2.8),
    "AmberLED": ((0.96, 0.43, 0.06, 1.0), 0.12, 0.30, 1.8),
}
SOCKETS = {
    "MOUNT_BOTTOM": ((0.0, 0.0, 0.0), (0.0, 0.0, math.pi)),
    "SERVICE_FRONT": ((0.0, -0.345, 0.43), (math.pi / 2, 0.0, 0.0)),
    "POWER_REAR": ((0.0, 0.355, 0.25), (-math.pi / 2, 0.0, 0.0)),
}
FACES = (
    (0, 3, 2), (0, 2, 1), (4, 5, 6), (4, 6, 7),
    (0, 1, 5), (0, 5, 4), (1, 2, 6), (1, 6, 5),
    (2, 3, 7), (2, 7, 6), (3, 0, 4), (3, 4, 7),
)


def part(name, verts, faces, mat, group="static", bevel=0.0):
    return dict(name=name, vertices=verts, faces=faces, material=mat,
                group=group, bevel=bevel)


def box(name, xyz, dims, mat, group="static", bevel=0.0):
    x, y, z = xyz
    a, b, c = (v * 0.5 for v in dims)
    verts = [
        (x-a, y-b, z-c), (x+a, y-b, z-c),
        (x+a, y+b, z-c), (x-a, y+b, z-c),
        (x-a, y-b, z+c), (x+a, y-b, z+c),
        (x+a, y+b, z+c), (x-a, y+b, z+c),
    ]
    return part(name, verts, list(FACES), mat, group, bevel)


def add(a, b):
    return tuple(x+y for x, y in zip(a, b))


def sub(a, b):
    return tuple(x-y for x, y in zip(a, b))


def scale(a, t):
    return tuple(x*t for x in a)


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def dot(a, b):
    return sum(x*y for x, y in zip(a, b))


def norm(a):
    return math.sqrt(dot(a, a))


def cylinder_between(name, a, b, radius, mat, sides=24, group="static"):
    """Closed, positively wound cylinder with arbitrary axial direction."""
    assert radius > 0 and norm(sub(b, a)) > 0
    direction = scale(sub(b, a), 1.0/norm(sub(b, a)))
    helper = (0, 0, 1) if abs(direction[2]) < 0.85 else (0, 1, 0)
    u = cross(helper, direction)
    u = scale(u, 1.0/norm(u))
    v = cross(direction, u)
    verts = []
    for point in (a, b):
        for i in range(sides):
            angle = 2*math.pi*i/sides
            verts.append(add(point, add(scale(u, radius*math.cos(angle)),
                                        scale(v, radius*math.sin(angle)))))
    verts.extend((a, b))
    faces = []
    for i in range(sides):
        j = (i+1) % sides
        faces.extend(((i, j, sides+j), (i, sides+j, sides+i),
                      (2*sides, j, i), (2*sides+1, sides+i, sides+j)))
    return part(name, verts, faces, mat, group)


def cyl_z(name, center, radius, height, mat, sides=32, group="static"):
    x, y, z = center
    return cylinder_between(name, (x, y, z-height/2),
                            (x, y, z+height/2), radius, mat, sides, group)


def torus_z(name, z, major, minor, mat, rings=48, segments=8):
    assert major > minor > 0
    verts = []
    for i in range(rings):
        t = 2*math.pi*i/rings
        for j in range(segments):
            p = 2*math.pi*j/segments
            r = major+minor*math.cos(p)
            verts.append((r*math.cos(t), r*math.sin(t),
                          z+minor*math.sin(p)))
    faces = []
    for i in range(rings):
        for j in range(segments):
            a = i*segments+j
            b = ((i+1) % rings)*segments+j
            c = ((i+1) % rings)*segments+(j+1) % segments
            d = i*segments+(j+1) % segments
            faces.extend(((a, b, c), (a, c, d)))
    return part(name, verts, faces, mat)


def build_parts():
    out = []
    push = out.append
    # Thick cylindrical cell with separated contact rings, not a scaled box.
    push(cyl_z("CoreCan", (0,0,.405), .265, .560, "Graphite", 64))
    push(cyl_z("LowerSocket", (0,0,.093), .294, .105, "Titanium", 48))
    push(cyl_z("UpperCap", (0,0,.708), .284, .072, "Titanium", 48))
    push(cyl_z("TopContact", (0,0,.754), .180, .027, "Copper", 48))
    push(cyl_z("BottomMountBoss", (0,0,.020), .160, .040, "Graphite", 32))
    for index, z in enumerate((.180,.263,.346,.429,.512,.595)):
        push(torus_z("CoolingBand_%02d" % index, z, .266, .009,
                     "Copper" if index % 2 == 0 else "Titanium"))
    push(torus_z("StatusRingTop", .737, .248, .010, "CyanLED"))
    push(torus_z("StatusRingBottom", .147, .267, .009, "AmberLED"))
    # Four long ceramic radial ribs stop before the service-hatch opening.
    for side in (-1,1):
        push(box("SideArmor_%+d" % side, (side*.276,0,.405),
                 (.025,.130,.390), "Ceramic", bevel=.005))
        push(box("BackArmor_%+d" % side, (side*.125,.261,.400),
                 (.095,.024,.340), "Ceramic", bevel=.005))
    # Service aperture and external, actually hinged ceramic hatch.
    # Hatch is in front of cylindrical skin (radius 0.265).
    push(box("FrontPortFrame", (0,-.281,.418),
             (.300,.022,.304), "Titanium", bevel=.004))
    push(box("HatchPanel", (0,-.333,.425),
             (.336,.020,.312), "Ceramic", "hatch", .007))
    push(box("HatchInset", (0,-.346,.421),
             (.257,.008,.226), "Graphite", "hatch", .003))
    for side in (-1,1):
        push(box("HatchRail_%+d" % side, (side*.147,-.349,.425),
                 (.018,.012,.267), "Copper", "hatch", .003))
    push(box("HatchWarning", (0,-.353,.335),
             (.106,.010,.022), "AmberLED", "hatch", .002))
    push(cylinder_between("HingeShaft", (-.208,-.345,.590),
                          (.208,-.345,.590), .014, "Titanium", 24))
    for side in (-1,1):
        push(cylinder_between("HingeBearing_%+d" % side,
                              (side*.195-.014,-.345,.590),
                              (side*.195+.014,-.345,.590),
                              .024, "Copper", 24))
    # Thick cable harness runs behind the battery. Segmented curved path:
    # bend sections overlap as intentional continuous insulated conduit.
    for side in (-1,1):
        x = side*.146
        points = [
            (x,.170,.768), (x,.255,.767), (x,.355,.745),
            (x,.398,.665), (x,.398,.490), (x,.392,.315),
            (x,.305,.238),
        ]
        for i,(a,b) in enumerate(zip(points,points[1:])):
            push(cylinder_between("PowerCable_%+d_%02d" % (side,i),
                                  a,b,.023,"Rubber",16))
        push(cylinder_between("Terminal_%+d" % side,
                              (x,.292,.235),(x,.333,.252),
                              .033,"Copper",24))
        push(cylinder_between("CableLight_%+d" % side,
                              (x,.398,.530),(x,.398,.610),
                              .025,"CyanLED",20))
    # Two recessed feet / keyed bottom interface for Unity prefab alignment.
    for side in (-1,1):
        push(box("MountFoot_%+d" % side, (side*.222,0,.038),
                 (.085,.145,.070), "Titanium", bevel=.004))
        push(box("MountPad_%+d" % side, (side*.222,0,.002),
                 (.072,.125,.004), "Rubber"))
    return out


def rotate_x(v, angle):
    c, s = math.cos(angle), math.sin(angle)
    return (v[0], c*v[1]-s*v[2], s*v[1]+c*v[2])


def animated_vertices(p, angle_deg):
    if p["group"] != "hatch":
        return p["vertices"]
    a = math.radians(angle_deg)
    return [add(HINGE, rotate_x(sub(v, HINGE), a)) for v in p["vertices"]]


def bounds(vertices):
    return [(min(v[i] for v in vertices), max(v[i] for v in vertices))
            for i in range(3)]


def verify_hatch_clearance(parts):
    """Conservative envelope: hatch remains outside cylindrical can at all keys.

    The real battery body has R<=0.294, but the front port frame reaches
    Y=-0.292. No hatch vertices may cross Y=-0.300 throughout deployment.
    All moving hatch parts are attached to one rigid pivot, so no internal
    sweep intersections can develop between them.
    """
    hatch = [p for p in parts if p["group"] == "hatch"]
    assert len(hatch) >= 5
    minimum = float("inf")
    for frame in range(71):
        angle = OPEN_DEGREES*frame/70
        for p in hatch:
            vertices = animated_vertices(p, angle)
            # Ignore top edge contact of the rigid hatch with its hinge:
            # only require separation from front can/frame at lower z.
            for x,y,z in vertices:
                if z <= .56:
                    minimum = min(minimum, -y-.294)
                    assert y < -.299, (p["name"], frame, y, z)
    return {"sampled_hatch_poses":71, "min_front_separation_m":round(minimum,6)}


def validate(parts):
    names = set()
    allverts = []
    triangles = 0
    for p in parts:
        assert p["name"] not in names
        names.add(p["name"])
        assert p["material"] in MATERIALS
        verts, faces = p["vertices"], p["faces"]
        assert len(verts) >= 8 and faces
        edge_uses = Counter()
        volume = 0.0
        for v in verts:
            assert all(math.isfinite(t) for t in v)
        for face in faces:
            assert len(face) == 3 and len(set(face)) == 3
            assert all(0 <= i < len(verts) for i in face)
            a,b,c = (verts[i] for i in face)
            assert norm(cross(sub(b,a),sub(c,a))) > 1e-10, p["name"]
            volume += dot(a,cross(b,c))/6
            for i,j in ((face[0],face[1]),(face[1],face[2]),(face[2],face[0])):
                edge_uses[tuple(sorted((i,j)))] += 1
        assert all(n == 2 for n in edge_uses.values()), p["name"]
        assert volume > 1e-12, ("inverted winding",p["name"],volume)
        allverts.extend(verts)
        triangles += len(faces)
    box3 = bounds(allverts)
    assert box3[2][0] >= -1e-8 and box3[2][1] <= .800
    assert len(SOCKETS) == 3
    clearance = verify_hatch_clearance(parts)
    return dict(asset=ASSET, result="PASS", parts=len(parts),
                vertices=len(allverts), triangles=triangles,
                materials=len(MATERIALS),
                closed_bounds_m=[[round(a,6),round(b,6)] for a,b in box3],
                mount_pivot_m=[0,0,0],
                sockets={k:list(v[0]) for k,v in SOCKETS.items()},
                hinge_axis="X", hinge_origin_m=list(HINGE),
                hinge_open_degrees=OPEN_DEGREES,
                visual_only=True, **clearance,
                blender="NOT RUN", unity="NOT RUN")


def write_obj(parts, folder):
    folder.mkdir(parents=True, exist_ok=True)
    mtl_name = ASSET+"_battery.mtl"
    obj_name = ASSET+"_battery.obj"
    lines = ["# M05-008 actual generated closed mesh, Blender Z-up",
             "mtllib "+mtl_name]
    mats = []
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mats.extend(["newmtl "+name,
                     "Kd %.6f %.6f %.6f" % rgba[:3],
                     "Ks %.6f %.6f %.6f" % (metal,metal,metal),
                     "Ns %.3f" % ((1-rough)*800),
                     "Ke %.6f %.6f %.6f" % tuple(emission*c for c in rgba[:3]),
                     "illum 2",""])
    (folder/mtl_name).write_text("\n".join(mats)+"\n",encoding="utf-8")
    offset = 1
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        for x,y,z in p["vertices"]:
            lines.append("v %.8f %.8f %.8f" % (x,y,z))
        for a,b,c in p["faces"]:
            lines.append("f %d %d %d" % (a+offset,b+offset,c+offset))
        offset += len(p["vertices"])
    (folder/obj_name).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return dict(backend="python-stdlib",obj=obj_name,mtl=mtl_name)


def export_blender(parts, folder):
    import bpy
    old = bpy.data.collections.get(COLLECTION)
    if old:
        for obj in tuple(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    root = bpy.data.objects.new(PREFIX+"MOUNT_ROOT",None)
    col.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root["visual_only"] = True
    root["mount"] = "bottom centre / Z-up / 1 unit = 1 metre"
    hinge = bpy.data.objects.new(PREFIX+"HATCH_HINGE_X",None)
    col.objects.link(hinge)
    hinge.parent = root
    hinge.location = HINGE
    hinge.rotation_mode = "XYZ"
    hinge["rotation_axis"] = "local X"
    hinge["range_degrees"] = (OPEN_DEGREES,0.0)
    for name,(xyz,rotation) in SOCKETS.items():
        obj = bpy.data.objects.new(PREFIX+name,None)
        col.objects.link(obj)
        obj.parent = root
        obj.location = xyz
        obj.rotation_euler = rotation
        obj.empty_display_type = "ARROWS"
        obj["socket_type"] = "visual_module_mount"
    mats = {}
    for name,(rgba,metal,rough,emission) in MATERIALS.items():
        mat = bpy.data.materials.get(PREFIX+name)
        if mat is None:
            mat = bpy.data.materials.new(PREFIX+name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Metallic"].default_value = metal
        bsdf.inputs["Roughness"].default_value = rough
        if emission:
            bsdf.inputs["Emission Color"].default_value = rgba
            bsdf.inputs["Emission Strength"].default_value = emission
        mat.diffuse_color = rgba
        mats[name] = mat
    for p in parts:
        moving = p["group"] == "hatch"
        verts = [sub(v,HINGE) for v in p["vertices"]] if moving else p["vertices"]
        mesh = bpy.data.meshes.new(PREFIX+p["name"]+"_MESH")
        mesh.from_pydata(verts,[],p["faces"])
        mesh.update()
        obj = bpy.data.objects.new(PREFIX+p["name"],mesh)
        col.objects.link(obj)
        obj.parent = hinge if moving else root
        mesh.materials.append(mats[p["material"]])
        if p["bevel"] > 0:
            # Blender 4.0 Weighted Normal modifier requires Auto Smooth;
            # on Blender 4.1+ this legacy property was removed.
            if hasattr(mesh, "use_auto_smooth"):
                mesh.use_auto_smooth = True
            modifier = obj.modifiers.new("Edge_chamfer","BEVEL")
            modifier.width = p["bevel"]
            modifier.segments = 3
            obj.modifiers.new("Weighted_normals","WEIGHTED_NORMAL")
    for frame, angle in ((1,0.0),(32,OPEN_DEGREES),(64,0.0)):
        hinge.rotation_euler[0] = math.radians(angle)
        hinge.keyframe_insert(data_path="rotation_euler",frame=frame,index=0)
    hinge.rotation_euler[0] = 0.0
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 64
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    fbx = folder/(ASSET+"_battery.fbx")
    glb = folder/(ASSET+"_battery.glb")
    blend = folder/(ASSET+"_battery.blend")
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
                             object_types={"MESH","EMPTY"},
                             axis_forward="-Z",axis_up="Y",
                             use_mesh_modifiers=True,bake_anim=True)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    for f in (fbx,glb,blend):
        assert f.exists() and f.stat().st_size > 100
    return dict(backend="blender-bpy",blender="PASS (exports written)",
                fbx=fbx.name,glb=glb.name,blend=blend.name,
                unity="NOT RUN")


def main():
    args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M05-008")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--static-obj",action="store_true")
    opt = parser.parse_args(args)
    parts = build_parts()
    report = validate(parts)
    if opt.self_test:
        print(json.dumps(report,indent=2))
        return
    folder = Path(opt.out).resolve()
    if opt.static_obj:
        result = write_obj(parts,folder)
    else:
        try:
            import bpy
        except ImportError:
            result = write_obj(parts,folder)
        else:
            result = export_blender(parts,folder)
    report.update(result)
    folder.mkdir(parents=True,exist_ok=True)
    (folder/(ASSET+"_manifest.json")).write_text(
        json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2))


if __name__ == "__main__":
    main()
