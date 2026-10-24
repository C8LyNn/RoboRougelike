#!/usr/bin/env python3
"""M03-003 — modular bridge frame, 2 x 1 x 1 m, RoboRougelike.

Procedural Blender 4.x / 5.x generator. No gameplay changes.
    blender -b --factory-startup --python generate_m03_003.py -- --out ./build/M03-003
Pure Python static fallback / geometry self-test:
    python generate_m03_003.py --self-test
    python generate_m03_003.py --out ./build/M03-003 --static-obj

Blender coordinates: X=longitudinal (2 m), Y=width (1 m), Z=height (1 m).
Root pivot = geometric grid origin (0,0,0); six socket empties lie at
the centres of the six module faces. FBX is exported Y-up for Unity.
This is a VISUAL structural asset, not a load-bearing physics simulation.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = "M03-003"
PREFIX = "RRL_M03_003_"
COLLECTION = "RRL_M03_003_BRIDGE_FRAME"
MATERIALS = {
    "Gunmetal": ((0.067, 0.084, 0.108, 1), 0.79, 0.31),
    "Steel": ((0.32, 0.39, 0.43, 1), 0.77, 0.29),
    "Ceramic": ((0.64, 0.67, 0.62, 1), 0.22, 0.42),
    "Amber": ((0.95, 0.43, 0.055, 1), 0.36, 0.30),
    "Rubber": ((0.027, 0.031, 0.037, 1), 0.04, 0.84),
}
# Outward-facing triangles for box corners in the order created below.
BOX_FACES = (
    (0, 3, 2), (0, 2, 1), (4, 5, 6), (4, 6, 7),
    (0, 1, 5), (0, 5, 4), (1, 2, 6), (1, 6, 5),
    (2, 3, 7), (2, 7, 6), (3, 0, 4), (3, 4, 7),
)
SOCKETS = {
    "SNAP_X_POS": ((1.0, 0.0, 0.0), (0, math.pi / 2, 0)),
    "SNAP_X_NEG": ((-1.0, 0.0, 0.0), (0, -math.pi / 2, 0)),
    "SNAP_Y_POS": ((0.0, 0.5, 0.0), (-math.pi / 2, 0, 0)),
    "SNAP_Y_NEG": ((0.0, -0.5, 0.0), (math.pi / 2, 0, 0)),
    "SNAP_Z_POS": ((0.0, 0.0, 0.5), (0, 0, 0)),
    "SNAP_Z_NEG": ((0.0, 0.0, -0.5), (math.pi, 0, 0)),
}


def box(name, center, size, material, bevel=0.0, role="structural"):
    assert all(s > 0 for s in size), name
    x, y, z = center
    a, b, c = (s / 2 for s in size)
    vertices = [
        (x-a, y-b, z-c), (x+a, y-b, z-c),
        (x+a, y+b, z-c), (x-a, y+b, z-c),
        (x-a, y-b, z+c), (x+a, y-b, z+c),
        (x+a, y+b, z+c), (x-a, y+b, z+c),
    ]
    return dict(name=name, vertices=vertices, faces=list(BOX_FACES),
                material=material, bevel=bevel, role=role)


def cylinder(name, center, radius, depth, axis, material, sides=12):
    """Closed cylindrical screw head, with outward faces and cap normals."""
    assert axis in ("X", "Y", "Z") and radius > 0 and depth > 0
    assert sides >= 8
    def position(u, v, w):
        x, y, z = center
        if axis == "X":
            return (x+w, y+u, z+v)
        if axis == "Y":
            return (x+v, y+w, z+u)
        return (x+u, y+v, z+w)
    vertices = []
    for w in (-depth/2, depth/2):
        for i in range(sides):
            angle = 2*math.pi*i/sides
            vertices.append(position(radius*math.cos(angle),
                                     radius*math.sin(angle), w))
    vertices.extend((position(0, 0, -depth/2), position(0, 0, depth/2)))
    faces = []
    for i in range(sides):
        j = (i+1) % sides
        faces.extend(((i,j,sides+j),(i,sides+j,sides+i),
                      (2*sides,j,i),(2*sides+1,sides+i,sides+j)))
    return dict(name=name, vertices=vertices, faces=faces,
                material=material, bevel=0.0, role="fastener")


def build_parts():
    """Closed independent solids; contact/overlap at engineered welds is intentional."""
    parts = []
    add = parts.append

    # TWO DISTINCT MASSIVE I-GIRDERS: 1.88 m long, full-height steel webs.
    # Each web enters its upper/lower flanges by 25 mm: intentional weld stock.
    for side in (-1, 1):
        label = "L" if side < 0 else "R"
        y = side * 0.34
        add(box(f"MainGirder_{label}_Web", (0,y,0),
                (1.88,.080,.52), "Gunmetal", .012))
        for sign, end in ((-1,"Bottom"), (1,"Top")):
            add(box(f"MainGirder_{label}_{end}_Flange", (0,y,sign*.280),
                    (1.88,.240,.090), "Steel", .012))
        # Recessed outer service stripe; not coplanar with the web.
        add(box(f"OuterStripe_{label}", (0,side*.383,0),
                (1.34,.010,.045), "Amber", .003, "accent"))
        for x in (-.72, 0, .72):
            add(box(f"WebRib_{label}_{x:+.2f}", (x,side*.389,0),
                    (.055,.032,.390), "Steel", .006))
    # Transverse beams: structural connection between the two girder webs.
    for x in (-.78, 0, .78):
        for z, face in ((-.270,"Lower"),(.270,"Upper")):
            add(box(f"CrossMember_{face}_{x:+.2f}", (x,0,z),
                    (.105,.720,.100), "Gunmetal", .008))
    # Four corner posts set exact physical envelope: 2 x 1 x 1 m.
    # Posts touch and overlap girder flanges at four engineered end joints.
    for xsign in (-1,1):
        for ysign in (-1,1):
            add(box(f"CornerPost_{xsign:+d}_{ysign:+d}",
                    (xsign*.960,ysign*.460,0),
                    (.080,.080,1.000), "Steel", .010))
            # Rubber collision/paint marker on the inside of each post.
            add(box(f"CornerInsert_{xsign:+d}_{ysign:+d}",
                    (xsign*.915,ysign*.455,0),
                    (.012,.055,.150), "Rubber", .003, "accent"))
    # End-face socket carriers are anchored to full-width end crossbars.
    for xsign in (-1,1):
        add(box(f"EndTie_{xsign:+d}", (xsign*.930,0,0),
                (.100,.920,.120), "Gunmetal", .009))
        add(box(f"EndSocketPad_{xsign:+d}", (xsign*.975,0,0),
                (.050,.260,.240), "Ceramic", .008))
        add(box(f"EndSocketMark_{xsign:+d}", (xsign*.990,0,0),
                (.016,.110,.110), "Amber", .004, "accent"))
    # Mid-side sockets with web-to-pad spacers (not floating plates).
    for ysign in (-1,1):
        add(box(f"SideSocketSpacer_{ysign:+d}", (0,ysign*.425,0),
                (.220,.110,.180), "Gunmetal", .006))
        add(box(f"SideSocketPad_{ysign:+d}", (0,ysign*.475,0),
                (.260,.050,.240), "Ceramic", .008))
        add(box(f"SideSocketMark_{ysign:+d}", (0,ysign*.495,0),
                (.090,.009,.080), "Amber", .003, "accent"))
    # Vertical socket stubs connect to the centre transverse members.
    for zsign in (-1,1):
        add(box(f"VerticalSocketStub_{zsign:+d}", (0,0,zsign*.385),
                (.200,.200,.160), "Gunmetal", .008))
        add(box(f"VerticalSocketPad_{zsign:+d}", (0,0,zsign*.470),
                (.260,.260,.060), "Ceramic", .008))
        add(box(f"VerticalSocketMark_{zsign:+d}", (0,0,zsign*.494),
                (.110,.110,.009), "Amber", .003, "accent"))
    # Exposed fasteners on both I-girder top flanges, within module envelope.
    for side in (-1,1):
        for x in (-.78,-.39,.39,.78):
            add(cylinder(f"FlangeBolt_{side:+d}_{x:+.2f}",
                         (x,side*.34,.328), .024,.018,"Z","Ceramic",12))
    return parts


def _sub(a,b):
    return (a[0]-b[0],a[1]-b[1],a[2]-b[2])


def _cross(a,b):
    return (a[1]*b[2]-a[2]*b[1],
            a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])


def _dot(a,b):
    return sum(x*y for x,y in zip(a,b))


def validate(parts):
    assert len(MATERIALS) >= 3
    names = set()
    mins = [float("inf")]*3
    maxs = [-float("inf")]*3
    triangles = 0
    for part in parts:
        assert part["name"] not in names, "duplicate object"
        names.add(part["name"])
        assert part["material"] in MATERIALS
        verts, faces = part["vertices"], part["faces"]
        assert verts and faces
        for vertex in verts:
            assert all(math.isfinite(n) for n in vertex), part["name"]
            for axis in range(3):
                mins[axis] = min(mins[axis], vertex[axis])
                maxs[axis] = max(maxs[axis], vertex[axis])
        edge_use = {}
        signed_volume = 0.0
        for tri in faces:
            assert len(tri) == 3 and len(set(tri)) == 3
            assert all(0 <= k < len(verts) for k in tri)
            a,b,c = (verts[k] for k in tri)
            normal = _cross(_sub(b,a), _sub(c,a))
            assert _dot(normal,normal) > 1e-16, part["name"]
            signed_volume += _dot(a,_cross(b,c))/6.0
            for u,v in ((tri[0],tri[1]),(tri[1],tri[2]),(tri[2],tri[0])):
                key = (min(u,v),max(u,v))
                edge_use[key] = edge_use.get(key,0)+1
            triangles += 1
        assert all(n == 2 for n in edge_use.values()), (
            "open/non-manifold mesh: " + part["name"])
        assert signed_volume > 1e-12, "reversed normals: " + part["name"]
    assert len([n for n in names if n.startswith("MainGirder_")]) == 6
    assert len([n for n in names if n.startswith("CornerPost_")]) == 4
    assert len(SOCKETS) == 6
    for axis, extent in enumerate((1.0,.5,.5)):
        assert abs(mins[axis]+extent) < 1e-8
        assert abs(maxs[axis]-extent) < 1e-8
    return {
        "asset": ASSET_ID, "result": "PASS", "parts": len(parts),
        "triangles": triangles, "materials": len(MATERIALS),
        "size_m": [2.0,1.0,1.0], "pivot_m": [0,0,0],
        "snap_points": {key:list(value[0]) for key,value in SOCKETS.items()},
        "topology": "watertight components, positive winding, nondegenerate",
        "overlaps": "intentional girder welds, carrier-to-pad joints and fasteners",
        "animation": "NONE — rigid frame",
        "blender_execution": "NOT RUN unless bpy backend used",
        "unity_execution": "NOT RUN"
    }


def static_obj(parts, outdir):
    """Real OBJ+MTL preview without Blender; six socket positions in manifest."""
    outdir.mkdir(parents=True, exist_ok=True)
    mtl_name = ASSET_ID + "_bridge_frame.mtl"
    obj_name = ASSET_ID + "_bridge_frame.obj"
    mtl = []
    for name,(rgba,metallic,roughness) in MATERIALS.items():
        mtl.extend([f"newmtl {name}",
                    "Kd {:.6f} {:.6f} {:.6f}".format(*rgba[:3]),
                    "Ks {:.6f} {:.6f} {:.6f}".format(*([metallic]*3)),
                    f"Ns {max(1.0,(1-roughness)*800):.2f}",
                    "illum 2", ""])
    (outdir/mtl_name).write_text("\n".join(mtl),encoding="utf-8")
    lines = ["# M03-003 procedural closed-mesh static preview",
             "# Blender Z-up, six logical sockets listed in manifest",
             f"mtllib {mtl_name}"]
    offset = 1
    for part in parts:
        lines.extend((f"o {part['name']}",f"usemtl {part['material']}"))
        for x,y,z in part["vertices"]:
            lines.append(f"v {x:.8f} {y:.8f} {z:.8f}")
        for a,b,c in part["faces"]:
            lines.append(f"f {a+offset} {b+offset} {c+offset}")
        offset += len(part["vertices"])
    (outdir/obj_name).write_text("\n".join(lines)+"\n",encoding="utf-8")
    return {"backend":"python-stdlib", "obj":obj_name,"mtl":mtl_name,
            "fbx":"NOT RUN", "blender":"NOT RUN"}


def blender_export(parts, outdir):
    import bpy
    # Only touch this asset's collection. Do not delete the scene or other work.
    previous = bpy.data.collections.get(COLLECTION)
    if previous:
        for obj in list(previous.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(previous)
    # Orphan cleanup prevents mesh datablock growth on repeated runs.
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    collection = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    root = bpy.data.objects.new(PREFIX+"GRID_ROOT",None)
    collection.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = .12
    root["grid_dimensions_m"] = (2.0,1.0,1.0)
    root["visual_only"] = True
    root["mount_origin"] = "grid centre"
    for name,(position,rotation) in SOCKETS.items():
        socket = bpy.data.objects.new(PREFIX+name,None)
        collection.objects.link(socket)
        socket.parent = root
        socket.location = position
        socket.rotation_euler = rotation
        socket.empty_display_type = "ARROWS"
        socket.empty_display_size = .09
        socket["socket_type"] = "frame_grid_port"
        socket["local_outward_axis"] = "+Z"
    materials = {}
    for name,(rgba,metallic,roughness) in MATERIALS.items():
        mat = bpy.data.materials.get(PREFIX+name)
        if mat is None:
            mat = bpy.data.materials.new(PREFIX+name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        mat.diffuse_color = rgba
        materials[name] = mat
    for part in parts:
        mesh = bpy.data.meshes.new(PREFIX+part["name"]+"_Mesh")
        mesh.from_pydata(part["vertices"], [], part["faces"])
        mesh.update()
        obj = bpy.data.objects.new(PREFIX+part["name"],mesh)
        collection.objects.link(obj)
        obj.parent = root
        mesh.materials.append(materials[part["material"]])
        # Flat structural surfaces, small physically plausible edge chamfers.
        if part["bevel"]:
            bevel = obj.modifiers.new("Machined_edge", "BEVEL")
            bevel.width = part["bevel"]
            bevel.segments = 3
            obj.modifiers.new("Weighted_normals", "WEIGHTED_NORMAL")
        else:
            for polygon in mesh.polygons:
                polygon.use_smooth = part["role"] == "fastener"
    bpy.context.view_layer.update()
    outdir.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    fbx = outdir/(ASSET_ID+"_bridge_frame.fbx")
    glb = outdir/(ASSET_ID+"_bridge_frame.glb")
    bpy.ops.export_scene.fbx(
        filepath=str(fbx), use_selection=True, object_types={"MESH","EMPTY"},
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True,
        use_mesh_modifiers=True, bake_anim=False, add_leaf_bones=False)
    bpy.ops.export_scene.gltf(
        filepath=str(glb), export_format="GLB", use_selection=True,
        export_animations=False, export_apply=True)
    assert fbx.exists() and fbx.stat().st_size > 0
    assert glb.exists() and glb.stat().st_size > 0
    return {"backend":"blender-bpy", "fbx":fbx.name,"glb":glb.name,
            "blender":"EXPORTED (Unity import NOT RUN)"}


def main():
    args_in = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--out",default="./build/M03-003")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--static-obj",action="store_true")
    args = parser.parse_args(args_in)
    parts = build_parts()
    report = validate(parts)
    if args.self_test:
        print(json.dumps(report,ensure_ascii=False,indent=2))
        return
    outdir = Path(args.out).resolve()
    if args.static_obj:
        output = static_obj(parts,outdir)
    else:
        try:
            import bpy  # noqa: F401
        except ImportError:
            output = static_obj(parts,outdir)
        else:
            output = blender_export(parts,outdir)
    report.update(output)
    outdir.mkdir(parents=True,exist_ok=True)
    (outdir/(ASSET_ID+"_manifest.json")).write_text(
        json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,ensure_ascii=False,indent=2))


if __name__ == "__main__":
    main()
