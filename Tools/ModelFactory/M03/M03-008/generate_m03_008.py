#!/usr/bin/env python3
"""M03-008: three-layer wedge armor plate, for RoboRougelike / Unity 6.

Coordinates in Blender (metres): X = horizontal, Y = front/back, Z = up.
The mounting face is Y=0, its outward mount normal is +Y. Front points -Y.
All 4 bolt bores are real through-holes (not painted dots), radius 26 mm.
No gameplay, no shared scenes or Unity project settings are changed.

  python generate_m03_008.py --self-test
  python generate_m03_008.py --static-obj --out build/M03-008
  blender -b --factory-startup --python-exit-code 1 \
    --python generate_m03_008.py -- --out build/M03-008
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

ID = "M03-008"
PREFIX = "RRL_M03_008_"
COLLECTION = PREFIX + "WEDGE_ARMOR"
STEM = "M03-008_wedge_armor"
HOLE_RADIUS = .026
COLLAR_RADIUS = .040
MOUNT_X = .410
MOUNT_Z = .290
MATERIALS = {
    "StructuralSteel": ((.29, .35, .40, 1), .79, .30),
    "Gunmetal": ((.075, .090, .115, 1), .70, .34),
    "Ceramic": ((.66, .70, .67, 1), .24, .37),
    "Elastomer": ((.028, .035, .040, 1), .02, .84),
    "Amber": ((.98, .44, .048, 1), .34, .27),
}
BOX_FACES = (
    (0, 3, 2), (0, 2, 1), (4, 5, 6), (4, 6, 7),
    (0, 1, 5), (0, 5, 4), (1, 2, 6), (1, 6, 5),
    (2, 3, 7), (2, 7, 6), (3, 0, 4), (3, 4, 7),
)


def box(name, centre, size, material, bevel=0.0, role="structural"):
    x, y, z = centre
    a, b, c = (s / 2 for s in size)
    assert min(size) > 0
    v = [
        (x-a, y-b, z-c), (x+a, y-b, z-c),
        (x+a, y+b, z-c), (x-a, y+b, z-c),
        (x-a, y-b, z+c), (x+a, y-b, z+c),
        (x+a, y+b, z+c), (x-a, y+b, z+c),
    ]
    return dict(name=name, vertices=v, faces=list(BOX_FACES),
                material=material, bevel=bevel, role=role)


def prism_xy(name, profile, zmin, zmax, material, bevel=0.0, role="armor"):
    """Watertight extrusion of a convex XY polygon along the Z axis."""
    p = list(profile)
    signed_area = sum(p[i][0]*p[(i+1) % len(p)][1] -
                      p[(i+1) % len(p)][0]*p[i][1]
                      for i in range(len(p)))
    assert abs(signed_area) > 1e-10
    if signed_area < 0:
        p.reverse()
    n = len(p)
    vertices = [(x, y, zmin) for x, y in p] + [
        (x, y, zmax) for x, y in p]
    faces = []
    for i in range(1, n-1):
        faces.append((0, i+1, i))            # bottom (-Z)
        faces.append((n, n+i, n+i+1))        # top (+Z)
    for i in range(n):
        j = (i+1) % n
        faces.extend(((i, j, n+j), (i, n+j, n+i)))
    return dict(name=name, vertices=vertices, faces=faces,
                material=material, bevel=bevel, role=role)


def ring(name, cx, cz, ymin, ymax, outer_radius, material,
         square_half=None, sides=24):
    """Closed through-bored annulus or square tile; no boolean dependencies.

    Rings are drawn in the XZ plane. Each of four vertex rings has the same
    winding; front/back annuli and the inner/outer walls have outward normals.
    For square tiles, radial intersections hit all four exact square corners.
    """
    assert sides >= 16 and sides % 8 == 0 and 0 < HOLE_RADIUS < outer_radius
    rings = [[], [], [], []]  # outer-front, inner-front, outer-back, inner-back
    for i in range(sides):
        theta = 2*math.pi*i/sides
        c, s = math.cos(theta), math.sin(theta)
        if square_half is None:
            ro = outer_radius
        else:
            hx, hz = square_half
            ro = min(hx/abs(c) if abs(c)>1e-10 else float("inf"),
                     hz/abs(s) if abs(s)>1e-10 else float("inf"))
        for idx, radius in ((0, ro), (1, HOLE_RADIUS),
                            (2, ro), (3, HOLE_RADIUS)):
            y = ymin if idx < 2 else ymax
            rings[idx].append((cx+radius*c, y, cz+radius*s))
    vertices = [v for sub in rings for v in sub]
    faces = []
    for i in range(sides):
        j = (i+1) % sides
        of, inf, ob, inb = i, sides+i, 2*sides+i, 3*sides+i
        ofj, infj, obj, inbj = j, sides+j, 2*sides+j, 3*sides+j
        faces.extend((
            (of, ofj, infj), (of, infj, inf),       # -Y front annulus
            (ob, inbj, obj), (ob, inb, inbj),      # +Y back annulus
            (of, ob, obj), (of, obj, ofj),         # outside wall
            (inf, infj, inbj), (inf, inbj, inb),   # hole wall
        ))
    return dict(name=name, vertices=vertices, faces=faces,
                material=material, bevel=0.0, role="bored_mount")


def wedge_front_y(x):
    ax = abs(x)
    if ax <= .040:
        return -.170
    if ax <= .345:
        return -.170 + (ax-.040)*(.066/.305)
    return -.104 + (ax-.345)*(.010/.035)


def build_parts():
    parts = []
    add = parts.append

    # Structural rear plate: 3x3 tiles; four corners have actual bored holes.
    # All tile seams meet exactly, without overlapping. Rear plane is Y=0.
    xs = (-.480, -.340, .340, .480)
    zs = (-.360, -.220, .220, .360)
    for xi in range(3):
        for zi in range(3):
            x0, x1 = xs[xi:xi+2]
            z0, z1 = zs[zi:zi+2]
            if xi in (0, 2) and zi in (0, 2):
                cx, cz = (x0+x1)/2, (z0+z1)/2
                add(ring(f"BaseBoredTile_{xi}_{zi}", cx, cz, -.032, 0,
                         .07, "StructuralSteel", square_half=(.07, .07)))
            else:
                add(box(f"BasePlateTile_{xi}_{zi}",
                        ((x0+x1)/2, -.016, (z0+z1)/2),
                        (x1-x0, .032, z1-z0), "StructuralSteel", .0))

    # Mount collars sit ON the plate's front surface, leaving bores open.
    for sx in (-1, 1):
        for sz in (-1, 1):
            add(ring(f"MountCollar_{sx:+d}_{sz:+d}",
                     sx*MOUNT_X, sz*MOUNT_Z, -.045, -.032,
                     COLLAR_RADIUS, "Gunmetal"))

    # Distinct elastomer shock-isolating middle layer, not a decal.
    add(box("ShockAbsorberMiddle", (0, -.042, 0),
            (.740, .020, .480), "Elastomer", .002))

    # Convex outer wedge, with wide centre crest and two chamfered side facets.
    # Its rear face touches the shock layer at Y=-0.052, without penetrating it.
    profile = [
        (-.380, -.052), (.380, -.052),
        (.380, -.094), (.345, -.104),
        (.040, -.170), (-.040, -.170),
        (-.345, -.104), (-.380, -.094),
    ]
    add(prism_xy("ThreeLayerWedgeOuter", profile, -.250, .250,
                 "Ceramic", .0035))

    # Machined protective ribs follow the true angled ceramic face.
    # Only 2mm is embedded (engineered contact); 12mm projects outward.
    for side in (-1, 1):
        x0, x1 = ((.255, .310) if side > 0 else (-.310, -.255))
        add(prism_xy(f"AngledRib_{side:+d}", [
            (x0, wedge_front_y(x0)+.002),
            (x1, wedge_front_y(x1)+.002),
            (x1, wedge_front_y(x1)-.012),
            (x0, wedge_front_y(x0)-.012),
        ], -.185, .185, "Gunmetal", .002, "rib"))

    # Visible safety marks on the flat centre crest. They never intersect ribs.
    for sign in (-1, 1):
        add(box(f"CrestWarning_{sign:+d}", (0, -.176, sign*.158),
                (.066, .016, .028), "Amber", .002, "accent"))
    return parts


def subtract(a, b):
    return tuple(x-y for x, y in zip(a, b))


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def dot(a, b):
    return sum(x*y for x, y in zip(a, b))


def bounds(part):
    return [(min(v[a] for v in part["vertices"]),
             max(v[a] for v in part["vertices"])) for a in range(3)]


def validate(parts):
    names = set()
    used = set()
    minv = [float("inf")]*3
    maxv = [-float("inf")]*3
    triangles = vertices = 0
    for part in parts:
        name = part["name"]
        assert name not in names, ("duplicate", name)
        names.add(name)
        assert part["material"] in MATERIALS
        used.add(part["material"])
        verts, faces = part["vertices"], part["faces"]
        assert verts and faces
        vertices += len(verts)
        triangles += len(faces)
        for v in verts:
            assert len(v) == 3 and all(math.isfinite(x) for x in v)
            for a in range(3):
                minv[a] = min(minv[a], v[a])
                maxv[a] = max(maxv[a], v[a])
        edges = {}
        volume = 0.
        for tri in faces:
            assert len(tri) == 3 and len(set(tri)) == 3, name
            assert all(0 <= k < len(verts) for k in tri), name
            a, b, c = (verts[k] for k in tri)
            n = cross(subtract(b, a), subtract(c, a))
            assert dot(n, n) > 1e-17, ("degenerate", name)
            volume += dot(a, cross(b, c))/6
            for i, j in ((tri[0], tri[1]), (tri[1], tri[2]), (tri[2], tri[0])):
                key = (min(i, j), max(i, j))
                edges.setdefault(key, []).append((i, j))
        assert all(len(e) == 2 and e[0] == e[1][::-1]
                   for e in edges.values()), ("nonmanifold/winding", name)
        assert volume > 1e-12, ("inward winding", name, volume)

    assert len(used) == 5 and len(MATERIALS) == 5
    assert len([x for x in names if x.startswith("BaseBoredTile_")]) == 4
    assert len([x for x in names if x.startswith("BasePlateTile_")]) == 5
    assert len([x for x in names if x.startswith("MountCollar_")]) == 4
    assert len([x for x in names if x.startswith("AngledRib_")]) == 2
    assert len([x for x in names if x.startswith("CrestWarning_")]) == 2
    assert len(parts) == 19
    assert all(abs(v-t) < 1e-8 for v,t in zip(minv, (-.48,-.184,-.36)))
    assert all(abs(v-t) < 1e-8 for v,t in zip(maxv, (.48,0,.36)))

    # Explicit 3-layer contacts, with zero accidental interpenetration.
    by = {p["name"]: p for p in parts}
    base_y = bounds(by["BasePlateTile_1_1"])[1]
    shock_y = bounds(by["ShockAbsorberMiddle"])[1]
    wedge_y = bounds(by["ThreeLayerWedgeOuter"])[1]
    assert abs(base_y[0]+.032)<1e-9 and abs(base_y[1])<1e-9
    assert abs(shock_y[0]+.052)<1e-9 and abs(shock_y[1]+.032)<1e-9
    assert abs(wedge_y[1]+.052)<1e-9  # rear plane of outer armor
    assert abs(wedge_front_y(0)+.170)<1e-9
    assert abs(wedge_front_y(.38)+.094)<1e-9
    for sx in (-1, 1):
        for sz in (-1, 1):
            collar = by[f"MountCollar_{sx:+d}_{sz:+d}"]
            cy = bounds(collar)[1]
            assert abs(cy[0]+.045)<1e-9 and abs(cy[1]+.032)<1e-9
            # Circle is physically clear of both the rubber and wedge corners.
            assert math.hypot(MOUNT_X-.380,MOUNT_Z-.250) > COLLAR_RADIUS+.005
            assert math.hypot(MOUNT_X-.370,MOUNT_Z-.240) > COLLAR_RADIUS+.005
    for side in (-1, 1):
        rib = by[f"AngledRib_{side:+d}"]
        for x,y,z in rib["vertices"]:
            dy = y-wedge_front_y(x)
            assert abs(dy-.002)<1e-9 or abs(dy+.012)<1e-9
            assert abs(z) == .185
    for sign in (-1, 1):
        ylo, yhi = bounds(by[f"CrestWarning_{sign:+d}"])[1]
        assert abs(ylo+.184)<1e-9 and abs(yhi+.168)<1e-9

    return {
        "id": ID, "agent": "M03", "result": "PASS",
        "geometry_revision": "M03-008_V1_THREE_LAYER_REAL_BORES",
        "parts": len(parts), "vertices": vertices, "triangles": triangles,
        "materials": len(used),
        "layer_order_back_to_front": [
            "StructuralSteel -0.032..0m",
            "Elastomer -0.052..-0.032m",
            "Ceramic wedge -0.170..-0.052m",
        ],
        "mount_bores": 4, "bore_radius_m": HOLE_RADIUS,
        "collar_outer_radius_m": COLLAR_RADIUS,
        "mount_positions_m": [
            [sx*MOUNT_X,0,sz*MOUNT_Z]
            for sx in (-1,1) for sz in (-1,1)],
        "pivot_m": [0,0,0], "mount_normal": [0,1,0],
        "front_normal": [0,-1,0],
        "size_m": [.960,.184,.720], "animation": "NOT_REQUIRED",
        "engineered_overlap": "2mm on armor face ribs and safety markers only",
        "mesh_qa": "watertight directed edges, positive signed volume, no degenerates",
        "blender": "NOT RUN", "unity": "NOT RUN",
    }


def static_obj(parts, out):
    out.mkdir(parents=True, exist_ok=True)
    objfile = STEM+".obj"
    mtlfile = STEM+".mtl"
    mtl = []
    for name,(rgba,metallic,roughness) in MATERIALS.items():
        mtl += [f"newmtl {name}",
                "Kd {:.6f} {:.6f} {:.6f}".format(*rgba[:3]),
                "Ks {:.6f} {:.6f} {:.6f}".format(*([metallic]*3)),
                f"Ns {(1-roughness)*800:.2f}", "illum 2", ""]
    (out/mtlfile).write_text("\n".join(mtl), encoding="utf-8")
    lines = ["# Real M03-008 three-layer wedge armor, 4 through-bores",
             "# Blender Z-up; Unity sockets in JSON manifest",
             f"mtllib {mtlfile}"]
    offset = 1
    for p in parts:
        lines.extend((f"o {p['name']}", f"usemtl {p['material']}"))
        for v in p["vertices"]:
            lines.append("v {:.9f} {:.9f} {:.9f}".format(*v))
        for tri in p["faces"]:
            lines.append("f {} {} {}".format(*(offset+i for i in tri)))
        offset += len(p["vertices"])
    (out/objfile).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return {"backend":"python-stdlib", "obj":objfile, "mtl":mtlfile,
            "blender":"NOT RUN", "fbx":"NOT RUN", "glb":"NOT RUN"}


def blender_export(parts, out):
    import bpy
    previous = bpy.data.collections.get(COLLECTION)
    if previous:
        for obj in tuple(previous.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(previous)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    collection = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    root = bpy.data.objects.new(PREFIX+"ROOT", None)
    collection.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = .09
    root["mount_plane"] = "Y=0; normal +Y; armor projects toward -Y"
    root["units"] = "metres; 1 Blender unit = 1 Unity metre"
    root["visual_only"] = True
    sockets = {"SNAP_FRAME_FRONT": (0.,0.,0.)}
    for sx in (-1,1):
        for sz in (-1,1):
            sockets[f"BOLT_{sx:+d}_{sz:+d}"] = (sx*MOUNT_X,0.,sz*MOUNT_Z)
    for name,position in sockets.items():
        socket = bpy.data.objects.new(PREFIX+name, None)
        collection.objects.link(socket)
        socket.parent = root
        socket.location = position
        socket.rotation_euler = (-math.pi/2,0,0)  # local +Z -> +Y
        socket.empty_display_type = "ARROWS"
        socket.empty_display_size = .045
        socket["mount_axis_local"] = "+Z"
        socket["mount_normal_blender"] = "+Y"
    materials = {}
    for name,(rgba,metallic,roughness) in MATERIALS.items():
        mat = bpy.data.materials.get(PREFIX+name)
        if mat is None:
            mat = bpy.data.materials.new(PREFIX+name)
        mat.use_nodes = True
        mat.diffuse_color = rgba
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        assert bsdf is not None
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if name == "Amber":
            em = bsdf.inputs.get("Emission Color")
            strength = bsdf.inputs.get("Emission Strength")
            if em: em.default_value = (.18,.065,.002,1)
            if strength: strength.default_value = .25
        materials[name] = mat
    for p in parts:
        mesh = bpy.data.meshes.new(PREFIX+p["name"]+"_MESH")
        mesh.from_pydata(p["vertices"],[],p["faces"])
        mesh.update()
        obj = bpy.data.objects.new(PREFIX+p["name"],mesh)
        collection.objects.link(obj)
        obj.parent = root
        mesh.materials.append(materials[p["material"]])
        if p["bevel"]:
            bevel = obj.modifiers.new("Machined_chamfer","BEVEL")
            bevel.width = p["bevel"]
            bevel.segments = 3
            obj.modifiers.new("Weighted_normals","WEIGHTED_NORMAL")
    bpy.context.view_layer.update()
    out.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    glb, fbx, blend = (out/(STEM+ext) for ext in (".glb",".fbx",".blend"))
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
                              use_selection=True,export_animations=False,
                              export_apply=True)
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
                             object_types={"MESH","EMPTY"},
                             axis_forward="-Z",axis_up="Y",
                             apply_unit_scale=True,use_mesh_modifiers=True,
                             bake_anim=False,add_leaf_bones=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    for path in (glb,fbx,blend):
        assert path.exists() and path.stat().st_size>1024, path
    return {"backend":"blender-bpy", "blender":"EXPORTED",
            "glb":glb.name, "fbx":fbx.name, "blend":blend.name,
            "unity":"NOT RUN"}


def main():
    args_in = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="./build/M03-008")
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--static-obj", action="store_true")
    args = parser.parse_args(args_in)
    parts = build_parts()
    report = validate(parts)
    if args.self_test:
        print(json.dumps(report, indent=2,ensure_ascii=False))
        return
    out = Path(args.out).resolve()
    if args.static_obj:
        result = static_obj(parts,out)
    else:
        try:
            import bpy  # noqa: F401
        except ImportError:
            result = static_obj(parts,out)
        else:
            result = blender_export(parts,out)
    report.update(result)
    out.mkdir(parents=True,exist_ok=True)
    (out/(STEM+"_manifest.json")).write_text(
        json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))


if __name__ == "__main__":
    main()
