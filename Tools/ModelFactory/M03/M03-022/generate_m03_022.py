#!/usr/bin/env python3
"""M03-022 — Zewnętrzna kratownica boczna / external triangular side truss.

Run: blender --background --python-exit-code 1 --python generate_m03_022.py -- --output /tmp/M03-022
Unity scale: 1 unit = 1 m. X is chassis longitudinal, Y points inboard,
Z is up. ROOT pivot (0,0,0) lies on the chassis side attachment plane Y=0.
Four SNAP sockets have normals +Y (towards chassis). No gameplay changes.
"""
import argparse
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ID = "M03-022"
COLLECTION = "MODEL_FACTORY_M03_022"
SPAN = 1.76
HEIGHT = 0.64
OUTBOARD_Y = -0.165
X_NODES = [-0.80, -0.60, -0.40, -0.20, 0.0, 0.20, 0.40, 0.60, 0.80]
Z_NODES = [(-0.265 if i % 2 == 0 else 0.265) for i in range(9)]
SNAPS = [(-0.73, 0.0, -0.25), (-0.73, 0.0, 0.25),
         (0.73, 0.0, -0.25), (0.73, 0.0, 0.25)]


def cleanup():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is not None:
        for obj in list(coll.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(coll)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(ID + "_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(ID + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)


def material(label, color, metal, rough, glow=None):
    m = bpy.data.materials.new(ID + "_" + label)
    m.use_nodes = True
    m.diffuse_color = (*color, 1.0)
    shader = m.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metal
    shader.inputs["Roughness"].default_value = rough
    if glow:
        key = "Emission Color" if "Emission Color" in shader.inputs else "Emission"
        shader.inputs[key].default_value = (*glow, 1.0)
        shader.inputs["Emission Strength"].default_value = 0.65
    return m


def solid_mesh(label, vertices, faces, mat, coll, root, smooth=False):
    mesh = bpy.data.meshes.new(ID + "_" + label + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(ID + "_" + label, mesh)
    coll.objects.link(ob)
    ob.parent = root
    mesh.materials.append(mat)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if bm.calc_volume(signed=True) < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    if smooth:
        for polygon in mesh.polygons:
            polygon.use_smooth = True
    return ob


def hollow_beam(label, a, b, outer, inner, mat, coll, root, sides=12):
    """Watertight annular tube; end caps remain open in the middle (true bore)."""
    a, b = Vector(a), Vector(b)
    axis = (b - a).normalized()
    assert (b - a).length > 0.03 and 0 < inner < outer
    ref = Vector((0, 1, 0)) if abs(axis.y) < 0.85 else Vector((1, 0, 0))
    u = axis.cross(ref).normalized()
    v = axis.cross(u).normalized()
    verts = []
    for p in (a, b):
        for radius in (outer, inner):
            for j in range(sides):
                t = 2 * math.pi * j / sides
                verts.append(tuple(p + radius * (u * math.cos(t) + v * math.sin(t))))
    faces = []
    for j in range(sides):
        k = (j + 1) % sides
        faces.extend([(j, 2*sides+j, 2*sides+k, k),
                      (sides+j, sides+k, 3*sides+k, 3*sides+j),
                      (j, k, sides+k, sides+j),
                      (2*sides+j, 3*sides+j, 3*sides+k, 2*sides+k)])
    return solid_mesh(label, verts, faces, mat, coll, root, smooth=True)


def box(label, center, size, mat, coll, root):
    x, y, z = center
    a, b, c = (v / 2 for v in size)
    verts = [(x+dx*a, y+dy*b, z+dz*c)
             for dz in (-1,1) for dy in (-1,1) for dx in (-1,1)]
    faces = [(0,2,3,1), (4,5,7,6), (0,1,5,4),
             (2,6,7,3), (0,4,6,2), (1,3,7,5)]
    return solid_mesh(label, verts, faces, mat, coll, root)


def build():
    cleanup()
    coll = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    root = bpy.data.objects.new(ID + "_ROOT_PIVOT", None)
    coll.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.12
    root["mount_plane"] = "Y=0, chassis side"
    root["unit_scale"] = "1 unit = 1 metre"
    root["animation"] = "none (static triangular truss)"
    mats = {
        "graphite": material("graphite", (0.055,0.072,0.085), 0.72, 0.32),
        "titanium": material("titanium", (0.43,0.51,0.55), 0.82, 0.26),
        "copper": material("copper", (0.58,0.26,0.095), 0.79, 0.31),
        "ceramic": material("ceramic", (0.82,0.79,0.67), 0.10, 0.48),
        "rubber": material("rubber", (0.022,0.029,0.033), 0.03, 0.82),
        "amber": material("amber", (0.72,0.27,0.018), 0.25, 0.32,
                          glow=(0.80,0.25,0.018)),
    }
    # Two longitudinal rails and two end uprights: all are hollow metal tubes.
    for tag,z in (("LOWER",-0.265), ("UPPER",0.265)):
        hollow_beam("Rail_"+tag, (-0.88,OUTBOARD_Y,z),
                    (0.88,OUTBOARD_Y,z), 0.038,0.025,
                    mats["graphite"],coll,root)
    for tag,x in (("LEFT",-0.80), ("RIGHT",0.80)):
        hollow_beam("EndPost_"+tag, (x,OUTBOARD_Y,-0.265),
                    (x,OUTBOARD_Y,0.265), 0.035,0.022,
                    mats["titanium"],coll,root)
    # Zigzag has no crossing diagonals; eight true triangular bays.
    # Touching/welded node ends are deliberate structural contacts.
    for i in range(8):
        hollow_beam("TriangleDiagonal_%02d"%(i+1),
                    (X_NODES[i],OUTBOARD_Y,Z_NODES[i]),
                    (X_NODES[i+1],OUTBOARD_Y,Z_NODES[i+1]),
                    0.029,0.017,mats["titanium"],coll,root)
    # Copper nodal sleeves (not free-floating geometry).
    for i,(x,z) in enumerate(zip(X_NODES,Z_NODES)):
        hollow_beam("NodeCollar_%02d"%(i+1),
                    (x,OUTBOARD_Y-0.025,z),
                    (x,OUTBOARD_Y+0.025,z),
                    0.047,0.030,mats["copper"],coll,root)
    # Four mounts bridge the outboard truss to the chassis side.
    for i,(x,y,z) in enumerate(SNAPS):
        tag = "%02d"%(i+1)
        hollow_beam("MountStandoff_"+tag,
                    (x,-0.145,z),(x,-0.028,z),
                    0.041,0.026,mats["titanium"],coll,root)
        box("MountPad_"+tag,(x,-0.018,z),(0.18,0.036,0.16),
            mats["graphite"],coll,root)
        box("Isolator_"+tag,(x,-0.039,z),(0.135,0.008,0.12),
            mats["rubber"],coll,root)
        box("CeramicImpactTile_"+tag,(x,-0.213,z),
            (0.13,0.022,0.105),mats["ceramic"],coll,root)
        snap=bpy.data.objects.new(ID+"_SNAP_"+tag,None)
        coll.objects.link(snap)
        snap.parent=root
        snap.location=(x,y,z)
        snap.empty_display_type="ARROWS"
        snap.empty_display_size=0.09
        snap["mount_normal"]="+Y"
        snap["grid_m"]=1.0
    # High-contrast markers are mechanically attached to the upper rail.
    for i,x in enumerate((-0.56,-0.18,0.18,0.56)):
        box("AmberMarker_%02d"%(i+1),
            (x,OUTBOARD_Y-0.041,0.265),
            (0.085,0.009,0.024),mats["amber"],coll,root)
    return coll


def export(coll, folder):
    folder=Path(folder)
    folder.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in coll.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects[ID+"_ROOT_PIVOT"]
    bpy.ops.wm.save_as_mainfile(filepath=str(folder/(ID+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(folder/(ID+".glb")),
                              export_format="GLB",use_selection=True,
                              export_animations=False,export_yup=True)
    bpy.ops.export_scene.fbx(filepath=str(folder/(ID+".fbx")),
                             use_selection=True,apply_unit_scale=True,
                             add_leaf_bones=False)
    for ext in (".blend",".glb",".fbx"):
        p=folder/(ID+ext)
        if not p.is_file() or p.stat().st_size < 1000:
            raise RuntimeError("Missing or empty export: "+str(p))
    print(ID+" EXPORT PASS",[(p.name,p.stat().st_size)
                             for p in folder.iterdir() if p.is_file()])


if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",default="/tmp/M03-022")
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    args=parser.parse_args(argv)
    export(build(),args.output)
