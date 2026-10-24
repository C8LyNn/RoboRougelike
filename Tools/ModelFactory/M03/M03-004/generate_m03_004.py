#!/usr/bin/env python3
"""M03-004 reinforced X frame, 1m modular unit. Python stdlib OBJ + Blender bpy GLB.

Blender: Z up, Y forward, X right. The root pivot is the centre of the REAR
SNAP face (0, 0, 0). Envelope: X/Z -0.5..+0.5, Y 0..1 metres.
No animation: this catalogue entry is a rigid structural module.
"""
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M03-004"
FACES = ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
         (3, 7, 6, 2), (0, 4, 7, 3), (1, 2, 6, 5))
PALETTE = {
    "frame_graphite": ((0.075, 0.090, 0.115), 0.78, 0.35),
    "x_rib_titanium": ((0.39, 0.47, 0.53), 0.70, 0.28),
    "port_ceramic": ((0.76, 0.79, 0.75), 0.16, 0.30),
    "joint_orange": ((0.98, 0.42, 0.065), 0.42, 0.34),
}
PORTS = {
    "REAR": ((0, 0, 0), (0, -1, 0)),
    "FRONT": ((0, 1, 0), (0, 1, 0)),
    "LEFT": ((-0.5, 0.5, 0), (-1, 0, 0)),
    "RIGHT": ((0.5, 0.5, 0), (1, 0, 0)),
    "BOTTOM": ((0, 0.5, -0.5), (0, 0, -1)),
    "TOP": ((0, 0.5, 0.5), (0, 0, 1)),
}


def cube_vertices(center, size):
    x, y, z = center
    dx, dy, dz = (s / 2 for s in size)
    return [(x-dx, y-dy, z-dz), (x+dx, y-dy, z-dz),
            (x+dx, y+dy, z-dz), (x-dx, y+dy, z-dz),
            (x-dx, y-dy, z+dz), (x+dx, y-dy, z+dz),
            (x+dx, y+dy, z+dz), (x-dx, y+dy, z+dz)]


def brace_vertices(start, end, thickness_x=0.052, width_yz=0.068):
    """Right-handed oriented cuboid; local W along YZ diagonal, U along X."""
    dy, dz = end[1] - start[1], end[2] - start[2]
    length = math.hypot(dy, dz)
    assert length > 0.10
    vy, vz = dz / length, -dy / length  # V = W cross U
    out = []
    for ux, vv, ww in ((-1,-1,0),(1,-1,0),(1,1,0),(-1,1,0),
                       (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)):
        out.append((start[0] + ux * thickness_x/2,
                    start[1] + vv * vy * width_yz/2 + ww * dy,
                    start[2] + vv * vz * width_yz/2 + ww * dz))
    return out


def build_parts():
    parts = []
    def box(name, center, size, material):
        parts.append(dict(name=name, material=material, vertices=cube_vertices(center, size)))
    for sx in (-1, 1):
        for sz in (-1, 1):
            box(f"Longitudinal_{'L' if sx < 0 else 'R'}_{'B' if sz < 0 else 'T'}",
                (sx*0.43, 0.5, sz*0.43), (.080, .84, .080), "frame_graphite")
    for side, y in (("Rear", .08), ("Front", .92)):
        for sz in (-1, 1):
            box(f"{side}_Crossbar_{'Bottom' if sz < 0 else 'Top'}",
                (0, y, sz*.43), (.86, .08, .08), "frame_graphite")
        for sx in (-1, 1):
            box(f"{side}_Upright_{'Left' if sx < 0 else 'Right'}",
                (sx*.43, y, 0), (.08, .08, .86), "frame_graphite")
    # Rear/front centre-port load paths: the ceramic SNAP faces are supported
    # by actual transverse beams, not free-floating visual connectors.
    for side, y in (("Rear", .08), ("Front", .92)):
        box(f"{side}_SNAP_LoadBridge", (0, y, 0), (.86, .08, .075), "frame_graphite")
    for sz in (-1, 1):
        box("Center_Spine_" + ("Bottom" if sz < 0 else "Top"),
            (0, .5, sz*.43), (.085, .76, .08), "frame_graphite")
    for sx in (-1, 1):
        side = "Left" if sx < 0 else "Right"
        x = sx * .43
        for sy in (-1, 1):
            for sz in (-1, 1):
                y, z = (.18 if sy < 0 else .82), sz * .405
                dy, dz = y - .5, z
                distance = math.hypot(dy, dz)
                # Rib ends are mechanically captured under the central hub;
                # outer ends are welded into the perimeter longitudinal rail.
                t = .057 / distance
                inner = (x, .5 + t * dy, t * dz)
                outer = (x, y, z)
                parts.append(dict(name=f"X_Rib_{side}_{'Rear' if sy<0 else 'Front'}_{'B' if sz<0 else 'T'}",
                                  material="x_rib_titanium",
                                  vertices=brace_vertices(inner, outer)))
        box(f"X_Joint_{side}", (x, .5, 0), (.078, .150, .150), "joint_orange")
    # Six real SNAP contact faces; rear port at pivot, not a fictitious empty.
    box("SNAP_Rear_Plate", (0, .025, 0), (.25, .05, .25), "port_ceramic")
    box("SNAP_Front_Plate", (0, .975, 0), (.25, .05, .25), "port_ceramic")
    for sx in (-1, 1):
        box("SNAP_" + ("Left" if sx<0 else "Right") + "_Plate",
            (sx*.48, .5, 0), (.04, .25, .25), "port_ceramic")
    for sz in (-1, 1):
        box("SNAP_" + ("Bottom" if sz<0 else "Top") + "_Plate",
            (0, .5, sz*.48), (.25, .25, .04), "port_ceramic")
    return parts


def vec_sub(a, b):
    return tuple(x-y for x,y in zip(a,b))


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def dot(a, b):
    return sum(x*y for x,y in zip(a,b))


def validate(parts):
    names = [p["name"] for p in parts]
    assert len(names) == len(set(names)), "Duplicate mesh names"
    assert len(parts) == 32, len(parts)
    assert len({p["material"] for p in parts}) >= 3
    bounds = [[min(v[i] for p in parts for v in p["vertices"]),
               max(v[i] for p in parts for v in p["vertices"])] for i in range(3)]
    for got, wanted in zip(bounds, ((-.5,.5),(0,1),(-.5,.5))):
        assert abs(got[0]-wanted[0]) < 1e-8 and abs(got[1]-wanted[1]) < 1e-8, (got,wanted)
    min_normal_dot = float('inf')
    signed_volume = 0.0
    for part in parts:
        vertices = part["vertices"]
        assert len(vertices) == 8
        centroid = tuple(sum(v[i] for v in vertices)/8 for i in range(3))
        for face in FACES:
            a,b,c = (vertices[i] for i in face[:3])
            n = cross(vec_sub(b,a), vec_sub(c,a))
            fcenter = tuple(sum(vertices[j][i] for j in face)/4 for i in range(3))
            outward = dot(n, vec_sub(fcenter, centroid))
            assert outward > 1e-9, (part["name"], face, outward)
            min_normal_dot = min(min_normal_dot, outward)
            # Quad split (a,b,c), (a,c,d), oriented surface volume.
            d = vertices[face[3]]
            for t in ((a,b,c),(a,c,d)):
                signed_volume += dot(t[0], cross(t[1],t[2])) / 6
    assert signed_volume > .01
    for key, (point, normal) in PORTS.items():
        assert len(point)==3 and abs(dot(normal,normal)-1)<1e-9, key
    # Non-contact structural ribs occupy separate side planes, no L/R crossing.
    for p in parts:
        if p["name"].startswith("X_Rib_Left"):
            assert max(v[0] for v in p["vertices"]) < -.39
        if p["name"].startswith("X_Rib_Right"):
            assert min(v[0] for v in p["vertices"]) > .39
    return dict(id=ID, result="STATIC_GEOMETRY_PASS", parts=len(parts),
                vertices=8*len(parts), quads=6*len(parts), triangles=12*len(parts),
                materials_used=sorted({p["material"] for p in parts}),
                bounds_xyz_m=bounds, pivot_rear_m=[0,0,0],
                snap_ports={k:{"position_m":pos,"outward_normal":normal}
                            for k,(pos,normal) in PORTS.items()},
                side_x_brace_count=8, diagonal_crossings="two bolted central hubs",
                mesh_winding="PASS: all 192 quad face normals point outwards",
                min_face_normal_outward_dot=round(min_normal_dot, 9),
                closed_meshes="32 individual oriented closed hexahedra",
                intended_joints="overlaps at welded frame intersections and central hub capture are intentional",
                animation="NONE_REQUIRED", blender="NOT RUN", unity="NOT RUN",
                visual_qa="NOT RUN")


def export_obj(parts, folder):
    folder.mkdir(parents=True, exist_ok=True)
    mtl = folder / (ID + ".mtl")
    with mtl.open("w", encoding="utf-8", newline="\n") as f:
        f.write("# M03-004 deterministic PBR approximation palette\n")
        for name,(rgb,metallic,roughness) in PALETTE.items():
            f.write("newmtl " + name + "\n")
            f.write("Kd " + " ".join(f"{v:.6f}" for v in rgb) + "\n")
            f.write(f"Pm {metallic:.6f}\nPr {roughness:.6f}\n\n")
    obj = folder / (ID + ".obj")
    with obj.open("w", encoding="utf-8", newline="\n") as f:
        f.write("# M03-004 reinforced X frame, 1 unit = 1 metre\nmtllib M03-004.mtl\n")
        offset = 1
        for part in parts:
            f.write(f"o {part['name']}\nusemtl {part['material']}\n")
            for v in part["vertices"]:
                f.write("v " + " ".join(f"{n:.6f}" for n in v) + "\n")
            for face in FACES:
                f.write("f " + " ".join(str(offset+i) for i in face) + "\n")
            offset += 8
    return obj, mtl


def export_blender(parts, folder):
    import bpy
    collection_name = ID + "_ASSET"
    old = bpy.data.collections.get(collection_name)
    if old:
        # Removing objects alone leaves orphaned bpy Mesh datablocks after
        # repeated regeneration; clear those owned datablocks as well.
        previous_meshes = [ob.data for ob in old.objects if ob.type == "MESH"]
        for ob in list(old.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.collections.remove(old)
        for mesh in previous_meshes:
            if mesh.users == 0:
                bpy.data.meshes.remove(mesh)
    col = bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(col)
    mats = {}
    for name,(rgb,metallic,roughness) in PALETTE.items():
        mat = bpy.data.materials.get(ID+"_"+name) or bpy.data.materials.new(ID+"_"+name)
        mat.diffuse_color = (*rgb,1)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (*rgb,1)
            bsdf.inputs["Metallic"].default_value = metallic
            bsdf.inputs["Roughness"].default_value = roughness
        mats[name] = mat
    root = bpy.data.objects.new(ID+"_MOUNT_REAR", None)
    col.objects.link(root)
    root.empty_display_type = 'PLAIN_AXES'
    root["port"] = "REAR"
    root["axis"] = "+Y forward; +Z up; +X right; 1 unit = 1 metre"
    for name,(position,normal) in PORTS.items():
        if name == "REAR":
            continue
        port = bpy.data.objects.new(ID+"_SNAP_"+name, None)
        col.objects.link(port)
        port.parent = root
        port.location = position
        port["outward_normal_xyz"] = list(normal)
        port["port_type"] = "FRAME_SNAP"
        port.empty_display_size = .065
    for part in parts:
        mesh = bpy.data.meshes.new(ID+"_"+part["name"]+"_mesh")
        mesh.from_pydata(part["vertices"], [], FACES)
        mesh.update(calc_edges=True)
        ob = bpy.data.objects.new(ID+"_"+part["name"], mesh)
        col.objects.link(ob)
        ob.parent = root
        ob.data.materials.append(mats[part["material"]])
        ob["model_id"] = ID
        ob["role"] = "STRUCTURAL_VISUAL"
        bevel = ob.modifiers.new("Machined_edges_6mm", "BEVEL")
        bevel.width = .006
        bevel.segments = 3
        bevel.limit_method = "ANGLE"
        bevel.angle_limit = math.radians(28)
        normals = ob.modifiers.new("Weighted_face_normals", "WEIGHTED_NORMAL")
        normals.keep_sharp = True
    folder.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(folder / (ID + ".blend")))
    previous = tuple(bpy.context.selected_objects)
    previous_active = bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action='DESELECT')
        for ob in col.objects:
            ob.select_set(True)
        bpy.context.view_layer.objects.active = root
        glb_path = folder / (ID + ".glb")
        outcome = bpy.ops.export_scene.gltf(filepath=str(glb_path),
                                            export_format="GLB", use_selection=True,
                                            export_yup=True, export_apply=True)
        if "FINISHED" not in outcome or not glb_path.exists() or glb_path.stat().st_size < 4096:
            raise RuntimeError("Blender glTF export failed: missing or invalid GLB")
    finally:
        bpy.ops.object.select_all(action='DESELECT')
        for ob in previous:
            if ob.name in bpy.data.objects:
                ob.select_set(True)
        if previous_active and previous_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active = previous_active
    return [str(folder/(ID+".blend")),str(folder/(ID+".glb"))]


def main():
    argv = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", default=str(Path(__file__).resolve().parent))
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--require-blender", action="store_true")
    args = parser.parse_args(argv)
    out = Path(args.output_dir).resolve()
    parts = build_parts()
    qa = validate(parts)
    obj,mtl = export_obj(parts,out)
    qa["obj"] = obj.name
    qa["mtl"] = mtl.name
    if not args.self_test:
        try:
            import bpy
        except ImportError:
            if args.require_blender:
                raise RuntimeError("bpy unavailable: animated GLB and .blend NOT RUN")
        else:
            qa["blender_outputs"] = export_blender(parts,out)
            qa["blender"] = "EXECUTED"
    (out/(ID+".static-qa.json")).write_text(json.dumps(qa,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(qa,indent=2,ensure_ascii=False))

if __name__ == "__main__":
    main()
