#!/usr/bin/env python3
"""M03-021 — Pałąk przeciwkapotażowy / dual horseshoe roll-over hoop.
Run: blender -b --python generate_m03_021.py -- --output /tmp/M03-021
Unity convention: 1 Blender unit = 1 metre; Z up; root pivot (0,0,0)
at the chassis mounting plane. Visual asset only; not a certified roll cage.
"""
import bpy
import bmesh
import math
import sys
import argparse
from pathlib import Path
from mathutils import Vector

NAME = "M03-021"
COLLECTION = "MODEL_FACTORY_M03_021"
R = 0.62
Y_OFF = 0.19
BOTTOM = 0.078
SHOULDER = 0.92
TUBE_R = 0.044
TUBE_BORE = 0.031
SNAPS = [(-R, -Y_OFF, 0), (-R, Y_OFF, 0),
         (R, -Y_OFF, 0), (R, Y_OFF, 0)]


def cleanup():
    coll = bpy.data.collections.get(COLLECTION)
    if coll is not None:
        for obj in list(coll.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(coll)
    # Remove only our orphan datablocks, never foreign scene objects.
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(NAME + "_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(NAME + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)


def material(label, rgb, metallic, roughness, emission=None):
    mat = bpy.data.materials.new(NAME + "_" + label)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission is not None:
        # Blender 4.x and 5.x input names.
        key = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
        bsdf.inputs[key].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 1.2
    return mat


def link(obj, coll, root):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)
    obj.parent = root
    return obj


def cube(label, loc, dims, mat, coll, root, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = NAME + "_" + label
    ob.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new("Controlled edge radius", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod2 = ob.modifiers.new("Weighted face normals", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=mod2.name)
    ob.data.materials.append(mat)
    return link(ob, coll, root)


def swept_tube(label, points, outer, inner, mat, coll, root, sides=16):
    """Closed, watertight hollow pipe with annular end caps; XZ plane path."""
    assert len(points) >= 2 and 0 < inner < outer
    pts = [Vector(p) for p in points]
    verts = []
    faces = []
    for i, p in enumerate(pts):
        tangent = ((pts[min(i+1, len(pts)-1)] -
                    pts[max(i-1, 0)])).normalized()
        normal = Vector((-tangent.z, 0.0, tangent.x)).normalized()
        binormal = Vector((0.0, 1.0, 0.0))
        for rad in (outer, inner):
            for j in range(sides):
                angle = 2.0 * math.pi * j / sides
                v = p + rad * (normal * math.cos(angle) +
                               binormal * math.sin(angle))
                verts.append(tuple(v))
    stride = sides * 2
    for i in range(len(pts) - 1):
        a = i * stride
        b = (i + 1) * stride
        for j in range(sides):
            k = (j+1) % sides
            # outer winding, then inner with opposite winding
            faces.append((a+j, b+j, b+k, a+k))
            faces.append((a+sides+j, a+sides+k,
                          b+sides+k, b+sides+j))
    end = (len(pts)-1)*stride
    for j in range(sides):
        k = (j+1) % sides
        faces.append((j, k, sides+k, sides+j))
        faces.append((end+j, end+sides+j, end+sides+k, end+k))
    mesh = bpy.data.meshes.new(NAME + "_" + label + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(NAME + "_" + label, mesh)
    coll.objects.link(obj)
    obj.parent = root
    obj.data.materials.append(mat)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # Mesh is a closed orientable shell. Ensure outward orientation.
    if bm.calc_volume(signed=True) < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    for face in mesh.polygons:
        face.use_smooth = True
    return obj


def segment(label, p0, p1, outer, inner, mat, coll, root):
    # General straight cylinder along any direction: rotate a local XZ path.
    a, b = Vector(p0), Vector(p1)
    axis = (b-a).normalized()
    ref = Vector((0, 1, 0)) if abs(axis.y) < 0.9 else Vector((1, 0, 0))
    u = axis.cross(ref).normalized()
    v = axis.cross(u).normalized()
    n = 16
    verts, faces = [], []
    for p in (a, b):
        for radius in (outer, inner):
            for j in range(n):
                th = 2*math.pi*j/n
                verts.append(tuple(p + radius*(u*math.cos(th)+v*math.sin(th))))
    for j in range(n):
        k=(j+1)%n
        faces += [(j, 2*n+j, 2*n+k, k),
                  (n+j, n+k, 3*n+k, 3*n+j),
                  (j,k,n+k,n+j),
                  (2*n+j,3*n+j,3*n+k,2*n+k)]
    mesh=bpy.data.meshes.new(NAME+"_"+label+"_Mesh")
    mesh.from_pydata(verts,[],faces); mesh.update()
    ob=bpy.data.objects.new(NAME+"_"+label,mesh)
    coll.objects.link(ob); ob.parent=root; mesh.materials.append(mat)
    bm=bmesh.new(); bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    if bm.calc_volume(signed=True)<0:
        bmesh.ops.reverse_faces(bm,faces=bm.faces)
    bm.to_mesh(mesh); bm.free()
    for poly in mesh.polygons: poly.use_smooth=True
    return ob


def horseshoe(y):
    pts = [(-R, y, BOTTOM), (-R, y, 0.35), (-R, y, 0.65),
           (-R, y, SHOULDER)]
    # semicircle from left to right, 48 samples
    for i in range(1,49):
        theta=math.pi*(1-i/48)
        pts.append((R*math.cos(theta),y,
                    SHOULDER+R*math.sin(theta)))
    pts += [(R,y,0.65),(R,y,0.35),(R,y,BOTTOM)]
    return pts


def build():
    cleanup()
    coll=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    root=bpy.data.objects.new(NAME+"_ROOT_PIVOT",None)
    coll.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root.empty_display_size=0.13
    root["unit_scale"]="1 unit = 1 metre"
    root["mount_plane"]="Z=0; chassis upper mounting plane"
    root["visual_only"]=True
    mats={
        "graphite":material("graphite",(0.075,0.092,0.105),0.75,0.32),
        "titanium":material("titanium",(0.40,0.49,0.54),0.85,0.27),
        "ceramic":material("ceramic",(0.79,0.81,0.72),0.10,0.42),
        "rubber":material("rubber",(0.026,0.030,0.033),0.05,0.75),
        "copper":material("copper",(0.57,0.25,0.085),0.83,0.31),
        "amber":material("amber",(0.72,0.28,0.02),0.28,0.31,
                         emission=(0.95,0.32,0.02))
    }
    # Two independent horseshoe-shaped hollow tubes, intentionally joined
    # by three welded transverse pipes and four bracing struts.
    for tag,y in (("FRONT",-Y_OFF),("REAR",Y_OFF)):
        swept_tube("Hoop_"+tag,horseshoe(y),TUBE_R,TUBE_BORE,
                   mats["titanium"],coll,root)
        # Ceramic impact shield follows the top crown as an annular sleeve.
        shield=[]
        for i in range(17,32):
            theta=math.pi*(1-i/48)
            shield.append((R*math.cos(theta),y,
                           SHOULDER+R*math.sin(theta)))
        swept_tube("CrownSleeve_"+tag,shield,0.054,0.0445,
                   mats["ceramic"],coll,root)
    for tag,x,z in (("Left",-R,0.73),("Right",R,0.73),
                    ("Crown",0.0,SHOULDER+R)):
        segment("Crossbar_"+tag,(x,-Y_OFF,z),(x,Y_OFF,z),
                0.027,0.019,mats["graphite"],coll,root)
    for side,x in (("L",-R),("R",R)):
        for face,y in (("F",-Y_OFF),("B",Y_OFF)):
            # Outboard diagonals terminate in the upright; these are
            # intentional structural weld contacts, not accidental overlaps.
            x2=x+(0.15 if x<0 else -0.15)
            segment("Brace_"+side+face,(x2,y,0.115),
                    (x,y,0.57),0.022,0.014,mats["graphite"],coll,root)
            cube("FootPlate_"+side+face,(x,y,0.034),
                 (0.22,0.18,0.068),mats["graphite"],coll,root,0.012)
            cube("FootRubber_"+side+face,(x,y,0.0765),
                 (0.16,0.13,0.017),mats["rubber"],coll,root,0.004)
            swept_tube("Clamp_"+side+face,
                       [(x,y,0.108),(x,y,0.151)],
                       0.053,0.0445,mats["copper"],coll,root)
            cube("StatusMarker_"+side+face,
                 (x+(-0.078 if x<0 else 0.078),y,0.071),
                 (0.031,0.074,0.009),mats["amber"],coll,root,0.003)
    for i,(x,y,z) in enumerate(SNAPS):
        snap=bpy.data.objects.new(NAME+"_SNAP_%02d"%(i+1),None)
        coll.objects.link(snap); snap.parent=root
        snap.location=(x,y,z)
        snap.empty_display_type="ARROWS"
        snap.empty_display_size=0.10
        snap["mount_normal"]="+Z"
        snap["grid_m"]=1.0
    return coll


def export(coll, out):
    out=Path(out); out.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in coll.objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects[NAME+"_ROOT_PIVOT"]
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(NAME+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(out/(NAME+".glb")),
                              export_format="GLB",use_selection=True,
                              export_animations=False,export_yup=True)
    bpy.ops.export_scene.fbx(filepath=str(out/(NAME+".fbx")),
                             use_selection=True,apply_unit_scale=True,
                             add_leaf_bones=False)
    for ext in (".blend",".glb",".fbx"):
        p=out/(NAME+ext)
        if not p.is_file() or p.stat().st_size<1000:
            raise RuntimeError("Missing/empty exported asset: "+str(p))
    print("M03-021 EXPORT PASS:",[(p.name,p.stat().st_size)
                                for p in out.iterdir() if p.is_file()])


if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",default="/tmp/M03-021")
    args=parser.parse_args(sys.argv[sys.argv.index("--")+1:]
                           if "--" in sys.argv else [])
    collection=build()
    export(collection,args.output)
