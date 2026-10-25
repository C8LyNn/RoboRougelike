#!/usr/bin/env python3
"""M03-024 — telescoping gap-bridging module, Blender 4.x.
Unity: 1 BU = 1 m. Root pivot at near mounting plane (0,0,0).
X axis bridges gap; movable carriage translates +X by 0.42 m.
Two independent hollow rectangular telescopic beams, no gameplay changes.
Run: blender -b --python-exit-code 1 --python generate_m03_024.py -- --output /tmp/M03-024
"""
import argparse
import math
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

ID = "M03-024"
COLLECTION = "MODEL_FACTORY_M03_024"
TRAVEL = 0.42
Y_RAILS = (-0.32, 0.32)


def cleanup():
    coll = bpy.data.collections.get(COLLECTION)
    if coll:
        for ob in list(coll.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.collections.remove(coll)
    for action in list(bpy.data.actions):
        if action.name.startswith(ID + "_") and action.users == 0:
            bpy.data.actions.remove(action)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(ID + "_") and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for mat in list(bpy.data.materials):
        if mat.name.startswith(ID + "_") and mat.users == 0:
            bpy.data.materials.remove(mat)


def material(name, rgb, metallic, roughness, emissive=False):
    m = bpy.data.materials.new(ID + "_" + name)
    m.use_nodes = True
    m.diffuse_color = (*rgb, 1.0)
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emissive:
        key = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
        bsdf.inputs[key].default_value = (*rgb, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 0.3
    return m


def solid(name, vertices, faces, mat, coll, parent):
    mesh = bpy.data.meshes.new(ID + "_" + name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(ID + "_" + name, mesh)
    coll.objects.link(ob)
    ob.parent = parent
    mesh.materials.append(mat)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if bm.calc_volume(signed=True) < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return ob


def box(name, center, size, mat, coll, parent):
    x,y,z=center
    a,b,c=[s/2 for s in size]
    v=[(x+dx*a,y+dy*b,z+dz*c) for dz in (-1,1) for dy in (-1,1) for dx in (-1,1)]
    f=[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)]
    return solid(name,v,f,mat,coll,parent)


def rectangular_tube_x(name, x0, x1, y, z, outer_y, outer_z, wall, mat, coll, parent):
    """True open-bore rectangular prism, 16 verts, watertight walls/end annuli."""
    assert x1>x0 and 0<2*wall<min(outer_y,outer_z)
    vertices=[]
    for x in (x0,x1):
        for width,height in ((outer_y,outer_z),(outer_y-2*wall,outer_z-2*wall)):
            vertices.extend([(x,y-width/2,z-height/2),(x,y+width/2,z-height/2),
                             (x,y+width/2,z+height/2),(x,y-width/2,z+height/2)])
    faces=[]
    for j in range(4):
        k=(j+1)%4
        faces.extend([(j,8+j,8+k,k), (4+j,4+k,12+k,12+j),
                      (j,k,4+k,4+j), (8+j,12+j,12+k,8+k)])
    return solid(name,vertices,faces,mat,coll,parent)


def snap(name, pos, normal, coll, parent):
    ob=bpy.data.objects.new(ID+"_SNAP_"+name,None)
    coll.objects.link(ob)
    ob.parent=parent
    ob.location=pos
    ob.rotation_mode="QUATERNION"
    ob.rotation_quaternion=Vector((0,0,1)).rotation_difference(Vector(normal))
    ob.empty_display_type="ARROWS"
    ob.empty_display_size=0.10
    ob["normal_local"]=tuple(float(x) for x in normal)
    ob["grid_m"]=1.0
    return ob


def build():
    cleanup()
    coll=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(coll)
    root=bpy.data.objects.new(ID+"_ROOT_PIVOT",None)
    coll.objects.link(root)
    root.empty_display_type="PLAIN_AXES"
    root["mount_pivot"]="near mounting plane (0,0,0)"
    root["unity_scale"]="1 unit = 1 metre"
    root["mount_type"]="frame connections, two near and two movable far"
    root["extension_range_m"]=[0.0,TRAVEL]
    carriage=bpy.data.objects.new(ID+"_SLIDING_CARRIAGE",None)
    coll.objects.link(carriage)
    carriage.parent=root
    carriage["motion_axis"]="+X"
    carriage["motion_range_m"]=[0.0,TRAVEL]
    mats={
      "graphite":material("graphite",(0.055,0.067,0.079),0.78,0.32),
      "titanium":material("titanium",(0.50,0.56,0.61),0.83,0.26),
      "copper":material("copper",(0.64,0.32,0.11),0.76,0.32),
      "ceramic":material("ceramic",(0.79,0.78,0.70),0.08,0.46),
      "rubber":material("rubber",(0.025,0.033,0.040),0.03,0.84),
      "amber":material("amber",(0.85,0.38,0.018),0.24,0.36,True),
    }
    for side,y in enumerate(Y_RAILS):
        # Outer shell inner opening: 0.138 x 0.178 m; sliding core
        # outer dimensions: 0.124 x 0.160 m (7/9 mm per-side clearance).
        rectangular_tube_x("FixedShell_%d"%side,0.07,1.20,y,0.22,
                           0.174,0.214,0.018,mats["graphite"],coll,root)
        rectangular_tube_x("SlidingCore_%d"%side,0.55,1.74,y,0.22,
                           0.124,0.160,0.016,mats["titanium"],coll,carriage)
        # Mounting collars are structural pad blocks; sockets are metadata empties.
        box("NearMountPad_%d"%side,(0.035,y,0.22),(0.070,0.20,0.26),
            mats["copper"],coll,root)
        box("FarMountPad_%d"%side,(1.755,y,0.22),(0.075,0.16,0.20),
            mats["copper"],coll,carriage)
        snap("NEAR_%d"%side,(0.0,y,0.22),(-1,0,0),coll,root)
        snap("FAR_%d"%side,(1.79,y,0.22),(1,0,0),coll,carriage)
        # Side abrasion strips are mounted outside the fixed shell.
        box("GuideStrip_%d"%side,(0.67,y+(-1 if y<0 else 1)*0.094,0.22),
            (0.82,0.010,0.08),mats["rubber"],coll,root)
        # Warning stripe on visible outer wall.
        box("WarningStripe_%d"%side,(0.42,y+(-1 if y<0 else 1)*0.102,0.265),
            (0.20,0.008,0.022),mats["amber"],coll,root)
        # Travel-stop indicator: visual only, not a new physics component.
        box("EndStop_%d"%side,(1.215,y,0.22),(0.024,0.20,0.22),
            mats["copper"],coll,root)
    # Welded crossbars: joins are intentional, no arbitrary interpenetration.
    for i,x in enumerate((0.18,0.94)):
        box("FixedCrossbar_%d"%i,(x,0,0.22),(0.070,0.62,0.080),
            mats["graphite"],coll,root)
    for i,x in enumerate((1.43,1.66)):
        box("MovingCrossbar_%d"%i,(x,0,0.22),(0.065,0.62,0.065),
            mats["titanium"],coll,carriage)
    # Deck segments stay separated throughout the whole extension range.
    box("FixedDeck",(0.59,0,0.355),(0.82,0.47,0.035),
        mats["ceramic"],coll,root)
    box("MovingDeck",(1.53,0,0.305),(0.32,0.46,0.020),
        mats["ceramic"],coll,carriage)
    for i,x in enumerate((0.31,0.83)):
        box("DeckGrip_%d"%i,(x,0,0.377),(0.11,0.39,0.010),
            mats["rubber"],coll,root)
    for i,x in enumerate((1.42,1.62)):
        box("MovingGrip_%d"%i,(x,0,0.321),(0.07,0.39,0.010),
            mats["rubber"],coll,carriage)
    # Timeline: closed, extend, hold, retract. Carriage and far SNAPs share pivot.
    for frame,x in ((1,0.0),(15,0.0),(55,TRAVEL),(70,TRAVEL),(110,0.0)):
        carriage.location=(x,0,0)
        carriage.keyframe_insert(data_path="location",frame=frame,group="Extension")
    for fc in carriage.animation_data.action.fcurves:
        for key in fc.keyframe_points:
            key.interpolation="LINEAR"
    carriage.animation_data.action.name=ID+"_Extend_Hold_Retract"
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=110
    bpy.context.scene.frame_set(1)
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
                              export_animations=True,export_yup=True)
    bpy.ops.export_scene.fbx(filepath=str(folder/(ID+".fbx")),
                             use_selection=True,apply_unit_scale=True,
                             bake_anim=True,add_leaf_bones=False)
    for ext in (".blend",".glb",".fbx"):
        p=folder/(ID+ext)
        if not p.is_file() or p.stat().st_size<1000:
            raise RuntimeError("Missing/empty export: "+str(p))
    print(ID+" EXPORT PASS",[(ext,(folder/(ID+ext)).stat().st_size)
                            for ext in (".blend",".glb",".fbx")])


if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",default="/tmp/M03-024")
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    args=parser.parse_args(argv)
    export(build(),args.output)
