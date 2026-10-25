#!/usr/bin/env python3
"""M03-016: inverted trapezoid chassis bracket. Blender bpy + stdlib OBJ/MTL.
Unity coordinates: X across, Y fore/aft, Z up; 1 unit = 1 metre.
Pivot (0,0,0), four +Z mounting sockets at z=0.044.
Only MODEL_M03_016 collection is replaced on repeated Blender execution.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ID = "M03-016"
COLLECTION = "MODEL_M03_016"
MATERIALS = {
    "GraphiteFrame": ((0.105, 0.135, 0.165, 1), 0.78, 0.34),
    "TitaniumSlottedRails": ((0.43, 0.50, 0.53, 1), 0.80, 0.30),
    "CeramicWear": ((0.83, 0.83, 0.73, 1), 0.12, 0.54),
    "CopperFasteners": ((0.65, 0.34, 0.16, 1), 0.90, 0.25),
    "AmberMarkings": ((0.94, 0.49, 0.045, 1), 0.24, 0.41),
}
SNAPS = [(x, y, 0.044) for y in (-0.27, 0.27) for x in (-0.58, 0.58)]


def triangulate(face):
    for i in range(1, len(face) - 1):
        yield (face[0], face[i], face[i + 1])


def volume(vertices, faces):
    v = 0.0
    for face in faces:
        for i, j, k in triangulate(face):
            a, b, c = vertices[i], vertices[j], vertices[k]
            v += (a[0] * (b[1]*c[2]-b[2]*c[1])
                  + a[1] * (b[2]*c[0]-b[0]*c[2])
                  + a[2] * (b[0]*c[1]-b[1]*c[0])) / 6.0
    return v


def solid(name, vertices, faces, material):
    if material not in MATERIALS:
        raise ValueError("Unknown material: " + material)
    if volume(vertices, faces) < 0:
        faces = [tuple(reversed(f)) for f in faces]
    if volume(vertices, faces) <= 1e-9:
        raise ValueError("Degenerate solid: " + name)
    return dict(name=name, vertices=vertices, faces=faces, material=material)


def box(name, center, size, material):
    x, y, z = center
    sx, sy, sz = [v / 2 for v in size]
    vertices = [(x+dx*sx, y+dy*sy, z+dz*sz)
                for dz in (-1, 1) for dy in (-1, 1) for dx in (-1, 1)]
    faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1),
             (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    return solid(name, vertices, faces, material)


def slotted_rail(name, outer_xz, y, depth=0.09):
    """Closed extruded quadrilateral with a real through-slot (not a decal).
    Outer and inner rings have matching CCW winding in XZ; the hole is 56%
    of the original outline, leaving continuous structural material.
    """
    cx = sum(p[0] for p in outer_xz) / 4
    cz = sum(p[1] for p in outer_xz) / 4
    outer = sorted(outer_xz, key=lambda p: math.atan2(p[1]-cz, p[0]-cx))
    inner = [(cx + (x-cx)*0.56, cz + (z-cz)*0.56) for x,z in outer]
    front, back = y-depth/2, y+depth/2
    vertices = [(x,front,z) for x,z in outer+inner]
    vertices += [(x,back,z) for x,z in outer+inner]
    faces = []
    for i in range(4):
        j = (i+1) % 4
        faces.extend([
            (i, j, 4+j, 4+i),          # front annulus
            (8+j, 8+i, 12+i, 12+j),   # back annulus
            (i, 8+i, 8+j, j),         # outside wall
            (4+j, 12+j, 12+i, 4+i),   # inner slot wall
        ])
    return solid(name, vertices, faces, "TitaniumSlottedRails")


def cylinder(name, x, y, z0, z1, radius, material, segments=12):
    if z1 <= z0:
        raise ValueError("Bad cylinder height")
    vertices = []
    for z in (z0, z1):
        vertices += [(x+radius*math.cos(2*math.pi*i/segments),
                      y+radius*math.sin(2*math.pi*i/segments), z)
                     for i in range(segments)]
    faces = [tuple(range(segments-1,-1,-1)),
             tuple(range(segments,2*segments))]
    for i in range(segments):
        j = (i+1)%segments
        faces.append((i,j,segments+j,segments+i))
    return solid(name, vertices, faces, material)


def build():
    meshes = []
    for sign in (-1,1):
        y = sign*0.27
        tag = "FRONT" if sign < 0 else "REAR"
        meshes += [
            box(tag+"_TOP_BEAM", (0,y,-0.035), (1.50,0.09,0.07), "GraphiteFrame"),
            box(tag+"_BOTTOM_BEAM", (0,y,-0.72), (0.96,0.09,0.08), "GraphiteFrame"),
        ]
        left = [(-0.73,-0.07),(-0.61,-0.07),(-0.37,-0.68),(-0.49,-0.68)]
        for side in (-1,1):
            shape = left if side < 0 else [(-x,z) for x,z in left]
            meshes.append(slotted_rail(tag+("_LEFT" if side<0 else "_RIGHT")+"_THROUGH_SLOT",shape,y))
        for x in (-0.58,0.58):
            label = "L" if x < 0 else "R"
            meshes.append(box(tag+"_"+label+"_MOUNT_PAD",(x,y,0.022),
                              (0.19,0.16,0.044),"TitaniumSlottedRails"))
            meshes.append(cylinder(tag+"_"+label+"_COPPER_BOLT",x,y,0.044,0.065,
                                   0.024,"CopperFasteners"))
        meshes.append(box(tag+"_CERAMIC_LOWER_SHOE",(0,y,-0.775),
                          (0.52,0.07,0.03),"CeramicWear"))
        meshes.append(box(tag+"_AMBER_EDGE",(0,sign*0.322,-0.028),
                          (0.22,0.014,0.018),"AmberMarkings"))
    for z, x, tag in [(-0.035,-0.55,"TOP_L"),(-0.035,0.55,"TOP_R"),
                       (-0.72,-0.30,"BOTTOM_L"),(-0.72,0.30,"BOTTOM_R")]:
        meshes.append(box("CROSSBAR_"+tag,(x,0,z),(0.11,0.45,0.06),
                          "GraphiteFrame"))
    return meshes


def write_obj(out, meshes):
    out.mkdir(parents=True,exist_ok=True)
    obj = ["# M03-016 deterministic triangulated OBJ, metres", "mtllib M03-016.mtl"]
    index = 1
    for mesh in meshes:
        obj += ["o "+mesh["name"],"usemtl "+mesh["material"]]
        for x,y,z in mesh["vertices"]:
            obj.append("v %.7f %.7f %.7f"%(x,y,z))
        for face in mesh["faces"]:
            for tri in triangulate(face):
                obj.append("f "+" ".join(str(index+i) for i in tri))
        index += len(mesh["vertices"])
    (out/"M03-016.obj").write_text("\n".join(obj)+"\n",encoding="utf-8")
    mtl = ["# M03-016 material palette"]
    for name,(rgba,metal,rough) in MATERIALS.items():
        mtl += ["newmtl "+name,"Kd %.6f %.6f %.6f"%rgba[:3],
                "Ks %.6f %.6f %.6f"%(metal,metal,metal),
                "Ns %.3f"%((1-rough)*500),"illum 2"]
    (out/"M03-016.mtl").write_text("\n".join(mtl)+"\n",encoding="utf-8")
    manifest = dict(id=ID, scale_m=1, pivot=[0,0,0], snap_axis="+Z",
                    snap_positions=SNAPS, mesh_count=len(meshes),
                    materials=list(MATERIALS), animation="none",
                    through_slots=4,
                    vertex_count=sum(len(m["vertices"]) for m in meshes),
                    triangle_count=sum(sum(len(f)-2 for f in m["faces"]) for m in meshes))
    (out/"M03-016.manifest.json").write_text(
        json.dumps(manifest,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    return manifest


def blender_export(out, meshes):
    import bpy
    old = bpy.data.collections.get(COLLECTION)
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    collection = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    palette = {}
    for name,(rgba,metal,rough) in MATERIALS.items():
        mat = bpy.data.materials.get("M03_016_"+name)
        if mat is None:
            mat = bpy.data.materials.new("M03_016_"+name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = rgba
            bsdf.inputs["Metallic"].default_value = metal
            bsdf.inputs["Roughness"].default_value = rough
        mat.diffuse_color = rgba
        palette[name] = mat
    root = bpy.data.objects.new("M03_016_ROOT_PIVOT",None)
    collection.objects.link(root)
    for spec in meshes:
        mesh = bpy.data.meshes.new(spec["name"]+"_Mesh")
        mesh.from_pydata(spec["vertices"],[],spec["faces"])
        mesh.update(calc_edges=True)
        obj = bpy.data.objects.new(spec["name"],mesh)
        collection.objects.link(obj)
        obj.parent = root
        mesh.materials.append(palette[spec["material"]])
    for i,location in enumerate(SNAPS):
        empty = bpy.data.objects.new("SNAP_%02d_PLUS_Z"%(i+1),None)
        empty.empty_display_type = "ARROWS"
        empty.location = location
        empty["mount_normal"] = "+Z"
        collection.objects.link(empty)
        empty.parent = root
    bpy.ops.object.select_all(action="DESELECT")
    for obj in collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=str(out/"M03-016.glb"),
                              export_format="GLB",use_selection=True)
    bpy.ops.export_scene.fbx(filepath=str(out/"M03-016.fbx"),
                             use_selection=True,apply_unit_scale=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/"M03-016.blend"))
    return len(collection.objects)


def main():
    args = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--output",default="output/M03-016")
    parser.add_argument("--static-only",action="store_true")
    opt = parser.parse_args(args)
    out = Path(opt.output).resolve()
    meshes = build()
    manifest = write_obj(out,meshes)
    print("STATIC_EXPORT",json.dumps(manifest,sort_keys=True))
    if not opt.static_only:
        try:
            import bpy
        except ImportError:
            print("BLENDER_EXPORT_NOT_RUN: bpy unavailable")
        else:
            print("BLENDER_EXPORT",blender_export(out,meshes))


if __name__ == "__main__":
    main()
