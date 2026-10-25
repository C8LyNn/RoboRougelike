#!/usr/bin/env python3
"""M03-023 — Six-sided 2x2 modular mounting block.
Blender 4.x: blender -b --python-exit-code 1 --python generate_m03_023.py -- --output /tmp/M03-023
Unity scale: 1 unit = 1 metre. Bounds: X/Y [-1,1], Z [0,2].
ROOT pivot (0,0,0) is the bottom socket centre. Six SNAP empties
orient their local +Z to the outward normal. Static model; no animation.
"""
import argparse
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ID = "M03-023"
COLLECTION = "MODEL_FACTORY_M03_023"
FACE_SOCKETS = [
    ("PX",(1,0,1),(1,0,0)), ("NX",(-1,0,1),(-1,0,0)),
    ("PY",(0,1,1),(0,1,0)), ("NY",(0,-1,1),(0,-1,0)),
    ("PZ",(0,0,2),(0,0,1)), ("NZ",(0,0,0),(0,0,-1)),
]


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


def face_basis(normal):
    n=Vector(normal).normalized()
    ref=Vector((0,0,1)) if abs(n.z)<0.9 else Vector((0,1,0))
    u=ref.cross(n).normalized()
    v=n.cross(u).normalized()
    return u,v,n


def annular_socket(label,center,normal,outer,inner,depth,offset,mat,coll,root,sides=8):
    """Real octagonal annular prism with unobstructed through-bore."""
    assert outer>inner>0 and depth>0
    u,v,n=face_basis(normal)
    c=Vector(center)+n*offset
    verts=[]
    for h in (0.0,depth):
        for radius in (outer,inner):
            for i in range(sides):
                angle=2*math.pi*i/sides
                verts.append(tuple(c+n*h+radius*(u*math.cos(angle)+v*math.sin(angle))))
    faces=[]
    for j in range(sides):
        k=(j+1)%sides
        faces.extend([(j,2*sides+j,2*sides+k,k),
                      (sides+j,sides+k,3*sides+k,3*sides+j),
                      (j,k,sides+k,sides+j),
                      (2*sides+j,3*sides+j,3*sides+k,2*sides+k)])
    return solid_mesh(label,verts,faces,mat,coll,root)


def build():
    cleanup()
    coll=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    root=bpy.data.objects.new(ID+"_ROOT_PIVOT",None)
    coll.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=0.15
    root["mount_pivot"]="bottom socket centre (0,0,0)"
    root["unity_scale"]="1 unit = 1 metre"
    root["module_grid"]="2x2 metre XY, six face sockets"
    root["animation"]="none required"
    mats={
        "graphite":material("graphite",(0.055,0.073,0.09),0.75,0.33),
        "titanium":material("titanium",(0.49,0.55,0.59),0.82,0.27),
        "copper":material("copper",(0.60,0.29,0.10),0.79,0.29),
        "ceramic":material("ceramic",(0.83,0.80,0.67),0.12,0.46),
        "rubber":material("rubber",(0.020,0.029,0.036),0.02,0.84),
        "amber":material("amber",(0.82,0.32,0.018),0.25,0.30,
                         glow=(0.72,0.20,0.01)),
    }
    # Twelve open-bore frame rails; eight welded corner hubs.
    edges=[]
    for x in (-1,1):
        for y in (-1,1):
            edges.append(((x,y,0),(x,y,2)))
    for z in (0,2):
        for x in (-1,1):
            edges.append(((x,-1,z),(x,1,z)))
        for y in (-1,1):
            edges.append(((-1,y,z),(1,y,z)))
    assert len(edges)==12
    for i,(a,b) in enumerate(edges):
        hollow_beam("EdgeBeam_%02d"%(i+1),a,b,0.075,0.050,
                    mats["graphite"],coll,root)
    for x in (-1,1):
        for y in (-1,1):
            for z in (0,2):
                label=("P" if x>0 else "N")+("P" if y>0 else "N")+("P" if z>0 else "N")
                box("CornerHub_"+label,(x,y,z),(0.19,0.19,0.19),
                    mats["titanium"],coll,root)
    # Every face has a genuine central bore, ceramic liner and rubber seal,
    # four diagonals meeting the corner hubs, and four visible indicators.
    for tag,center,normal in FACE_SOCKETS:
        c=Vector(center)
        u,v,n=face_basis(normal)
        annular_socket("SocketCollar_"+tag,c,n,0.265,0.168,0.105,0.018,
                       mats["copper"],coll,root)
        annular_socket("CeramicLiner_"+tag,c,n,0.202,0.170,0.025,0.125,
                       mats["ceramic"],coll,root)
        annular_socket("RubberSeal_"+tag,c,n,0.225,0.205,0.012,0.151,
                       mats["rubber"],coll,root)
        for s in (-1,1):
            for t in (-1,1):
                start=c+u*(s*0.20)+v*(t*0.20)
                end=c+u*(s*0.97)+v*(t*0.97)
                label=("P" if s>0 else "N")+("P" if t>0 else "N")
                hollow_beam("FaceBrace_"+tag+"_"+label,start,end,
                            0.037,0.022,mats["titanium"],coll,root)
        for i in range(4):
            angle=i*math.pi/2
            p=c+u*(0.33*math.cos(angle))+v*(0.33*math.sin(angle))+n*0.013
            hollow_beam("Indicator_"+tag+"_%02d"%(i+1),
                        p-n*0.035,p+n*0.035,0.030,0.016,
                        mats["amber"],coll,root,sides=8)
        snap=bpy.data.objects.new(ID+"_SNAP_"+tag,None)
        coll.objects.link(snap)
        snap.parent=root
        snap.location=c
        snap.rotation_mode="QUATERNION"
        snap.rotation_quaternion=Vector((0,0,1)).rotation_difference(n)
        snap.empty_display_type="ARROWS"
        snap.empty_display_size=0.14
        snap["face"]=tag
        snap["outward_normal"]=tuple(float(a) for a in n)
        snap["grid_m"]=1.0
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
    parser.add_argument("--output",default="/tmp/M03-023")
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    args=parser.parse_args(argv)
    export(build(),args.output)
