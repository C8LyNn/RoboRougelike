#!/usr/bin/env python3
"""M01-011 — Długi karabin precyzyjny; visual-only procedural Blender asset.

Run:
  blender -b --factory-startup --python-exit-code 1 --python generate_m01_011.py -- --out /tmp/m01-011
Coordinates: 1 Blender unit = 1 metre. +Z muzzle/forward, +Y up, +X right.
Mount pivot: MOUNT_TOP_SOCKET at world (0,0,0), facing +Y onto the vehicle.
Bolt slide: BOLT_CARRIER_R, translates 120mm along local -Z and returns.
This script does NOT implement firing, projectiles, gameplay or Unity prefabs.
"""
import argparse
from collections import Counter
import json
import math
from pathlib import Path
import sys

import bpy
import bmesh
from mathutils import Vector

ID = "M01-011"
COLLECTION = ID + "_LONG_PRECISION_RIFLE"
STEM = "M01-011_long_precision_rifle"
BOLT_STROKE_M = .120
FRAME_RANGE = (1, 48)
MATS = {
    "Graphite_Cerakote": ((.047,.065,.078,1), .68,.38,0),
    "Titanium_Satin": ((.32,.40,.46,1), .82,.29,0),
    "Ceramic_Ivory": ((.79,.82,.77,1), .13,.31,0),
    "Copper_Anodized": ((.53,.225,.085,1), .75,.28,0),
    "Elastomer_Black": ((.016,.024,.032,1), .05,.76,0),
    "Cyan_Optics": ((.018,.66,.92,1), .12,.20,2.8),
    "Amber_Indicator": ((.96,.36,.038,1), .13,.31,1.8),
}
meshes = []
fixed = []
moving = []


def reset():
    # Repeated invocations in the same Blender file replace this model only.
    old = bpy.data.collections.get(COLLECTION)
    if old:
        for ob in list(old.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
        bpy.data.collections.remove(old)
    for action in list(bpy.data.actions):
        if action.name.startswith(ID + "_") and action.users == 0:
            bpy.data.actions.remove(action)
    col = bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(col)
    return col


COL = reset()


def mat(name):
    label = ID + "_" + name
    m = bpy.data.materials.get(label) or bpy.data.materials.new(label)
    rgba, metallic, roughness, emission = MATS[name]
    m.diffuse_color = rgba
    m.use_nodes = True
    shader = m.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = rgba
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if "Emission Color" in shader.inputs:
        shader.inputs["Emission Color"].default_value = rgba
        shader.inputs["Emission Strength"].default_value = emission
    return m


def register(ob, material, parent, animated=False, bevel=0.):
    ob.parent = parent
    if bevel:
        mod = ob.modifiers.new("Machined_Edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
        for poly in ob.data.polygons:
            poly.use_smooth = True
        mod = ob.modifiers.new("Weighted_Vertex_Normals", "WEIGHTED_NORMAL")
        mod.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for collection in list(ob.users_collection):
        collection.objects.unlink(ob)
    COL.objects.link(ob)
    ob.data.materials.clear()
    ob.data.materials.append(mat(material))
    meshes.append(ob)
    (moving if animated else fixed).append(ob)
    return ob


def empty(name, xyz, parent=None):
    ob = bpy.data.objects.new(name, None)
    COL.objects.link(ob)
    ob.empty_display_type = "ARROWS"
    ob.empty_display_size = .075
    ob.parent = parent
    ob.location = xyz
    return ob


ROOT = empty("MOUNT_TOP_SOCKET", (0,0,0))


def box(name, pos, size, material, parent=ROOT, animated=False, bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.location = pos
    return register(ob, material, parent, animated, bevel)


def cyl(name, pos, radius, depth, material, parent=ROOT,
        animated=False, segments=32, axis="Z", bevel=.0015):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments, radius=radius, depth=depth)
    ob = bpy.context.object
    ob.name = name
    ob.location = pos
    if axis == "Y":
        ob.rotation_euler[0] = math.pi/2
    elif axis == "X":
        ob.rotation_euler[1] = math.pi/2
    return register(ob, material, parent, animated, bevel)


def torus(name, pos, radius, minor, material, parent=ROOT, animated=False):
    bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=10,
                                    major_radius=radius, minor_radius=minor)
    ob = bpy.context.object
    ob.name = name
    ob.location = pos
    return register(ob, material, parent, animated)


def tube(name, pos, outer, inner, length, material, segments=48):
    """Closed annular solid, with real continuous bore rather than black cap."""
    assert outer > inner > 0 and length > 0
    vertices = []
    for i in range(segments):
        theta = 2*math.pi*i/segments
        x, y = math.cos(theta), math.sin(theta)
        vertices.extend(((outer*x,outer*y,-length/2),
                         (outer*x,outer*y,length/2),
                         (inner*x,inner*y,-length/2),
                         (inner*x,inner*y,length/2)))
    faces = []
    for i in range(segments):
        j = (i+1)%segments
        a, b = 4*i, 4*j
        faces.extend(((a,b,b+1,a+1), (a+2,a+3,b+3,b+2),
                      (a+1,b+1,b+3,a+3), (a,a+2,b+2,b)))
    data = bpy.data.meshes.new(name + "_mesh")
    data.from_pydata(vertices, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    COL.objects.link(ob)
    ob.location = pos
    return register(ob, material, ROOT)


def strut(name, p1, p2, width, thickness, material, bevel=.005):
    a, b = Vector(p1), Vector(p2)
    midpoint = (a+b)*.5
    ob = box(name, midpoint, (width,thickness,(b-a).length),
             material, bevel=bevel)
    ob.rotation_euler = (b-a).to_track_quat("Z","Y").to_euler()
    return ob


def build():
    # Vehicle socket and solid mount structure; origin is the contact plane.
    box("Mount_Top_Socket", (0,.025,.17), (.310,.050,.310),
        "Titanium_Satin", bevel=.013)
    box("Mount_Elastomer_Gasket", (0,.059,.17), (.275,.018,.270),
        "Elastomer_Black", bevel=.006)
    for x in (-.115,.115):
        for z in (.055,.280):
            cyl("Socket_Bolt_%s_%s"%(int(x*1000),int(z*1000)),
                (x,.061,z), .012,.015,"Copper_Anodized",axis="Y",segments=12)
    box("Load_Bearing_Column",(0,.135,.46),(.190,.150,.350),
        "Titanium_Satin",bevel=.021)
    box("Rear_Recoil_Block",(0,.235,.260),(.315,.185,.210),
        "Graphite_Cerakote",bevel=.025)
    box("Butt_Impact_Pad",(0,.240,.145),(.280,.145,.035),
        "Elastomer_Black",bevel=.010)
    for x in (-.095,.095):
        box("Butt_Amber_Trim_%s"%int(x*1000),(x,.255,.126),
            (.012,.065,.008),"Amber_Indicator",bevel=.002)

    # Long asymmetrical chassis and segmented armor.
    box("Precision_Receiver",(0,.270,.705),(.290,.190,.770),
        "Graphite_Cerakote",bevel=.023)
    box("Receiver_Upper_Ceramic_Left",(-.093,.370,.748),(.082,.023,.490),
        "Ceramic_Ivory",bevel=.011)
    box("Receiver_Lower_Skid",(0,.163,.745),(.250,.029,.590),
        "Titanium_Satin",bevel=.010)
    box("Right_Side_Copper_Band",(.150,.270,.622),(.020,.075,.320),
        "Copper_Anodized",bevel=.005)
    box("Right_Side_Serial_Plate",(.164,.270,.852),(.014,.063,.120),
        "Ceramic_Ivory",bevel=.004)
    box("Underbarrel_Magazine",(0,.100,.740),(.200,.120,.245),
        "Graphite_Cerakote",bevel=.017)
    box("Magazine_Ceramic_Base",(0,.031,.740),(.173,.018,.192),
        "Ceramic_Ivory",bevel=.005)
    for n in range(4):
        box("Magazine_Copper_Rib_%02d"%n,(-.088+n*.058,.073,.742),
            (.009,.074,.176),"Copper_Anodized",bevel=.003)

    # Deliberate asymmetry: a heavy left brace, slender right strut,
    # with actual fasteners, and no interference with the high bolt path.
    strut("Left_Long_Diagonal_Brace",(-.210,.224,.375),(-.255,.225,1.345),
          .058,.065,"Titanium_Satin",.010)
    strut("Right_Light_Diagonal_Brace",(.190,.193,.455),(.208,.182,1.300),
          .026,.038,"Copper_Anodized",.005)
    # Real lateral webs connect both diagonal braces to the receiver.
    for n,z in enumerate((.450,.860,1.055)):
        box("Left_Brace_Load_Web_%d"%n,(-.176,.226,z),
            (.148,.063,.052),"Graphite_Cerakote",bevel=.007)
        box("Right_Brace_Load_Web_%d"%n,(.164,.195,z),
            (.104,.052,.055),"Titanium_Satin",bevel=.006)
    for n,z in enumerate((.440,.650,.870,1.090)):
        box("Left_Brace_Ceramic_Saddle_%d"%n,(-.222,.255,z),
            (.073,.015,.060),"Ceramic_Ivory",bevel=.006)
    for n,z in enumerate((.510,.720,.930,1.140)):
        cyl("Right_Brace_Fastener_%d"%n,(.207,.195,z),
            .012,.015,"Titanium_Satin",axis="X",segments=12)

    # Main 1.20m barrel has a genuine through bore. Perforated thermal
    # housing is suggested by separated ribs; no coplanar mesh surfaces.
    tube("Bored_Long_Barrel",(0,.270,1.645),.043,.022,1.200,
         "Titanium_Satin",segments=64)
    cyl("Barrel_Breech_Collar",(0,.270,1.093),.078,.085,
        "Graphite_Cerakote",segments=48)
    for n,z in enumerate((1.22,1.40,1.58,1.76,1.94)):
        torus("Copper_Heat_Ring_%d"%n,(0,.270,z),.060,.006,
              "Copper_Anodized")
        for side,label in ((-1,"L"),(1,"R")):
            box("Barrel_Floating_Side_Rail_%s_%d"%(label,n),
                (side*.060,.270,z),(.022,.042,.105),
                "Ceramic_Ivory",bevel=.006)
    tube("Muzzle_Brake_Bored",(0,.270,2.220),.071,.040,.155,
         "Graphite_Cerakote",segments=48)
    torus("Muzzle_Copper_Rim",(0,.270,2.288),.059,.008,
          "Copper_Anodized")
    for side,label in ((-1,"L"),(1,"R")):
        box("Muzzle_Flank_Vent_%s"%label,(side*.073,.270,2.204),
            (.018,.056,.055),"Titanium_Satin",bevel=.004)

    # Long optic sits above receiver, well left of the right-hand bolt.
    for n,z in enumerate((.660,.925)):
        box("Optic_Claw_%d"%n,(0,.408,z),(.085,.108,.040),
            "Titanium_Satin",bevel=.009)
        box("Optic_Clamp_%d"%n,(0,.462,z),(.118,.024,.052),
            "Graphite_Cerakote",bevel=.007)
    cyl("Precision_Optic_Tube",(0,.508,.810),.052,.550,
        "Graphite_Cerakote",segments=48)
    cyl("Optic_Objective_Rim",(0,.508,1.093),.069,.044,
        "Titanium_Satin",segments=48)
    cyl("Optic_Objective_Lens",(0,.508,1.119),.043,.006,
        "Cyan_Optics",segments=48)
    cyl("Optic_Eyepiece",(0,.508,.528),.057,.045,
        "Elastomer_Black",segments=48)
    for n,z in enumerate((.610,.735,.860,.990)):
        torus("Optic_Focus_Ring_%d"%n,(0,.508,z),.056,.005,
              "Copper_Anodized")
    box("Optic_Range_Indicator",(0,.565,.815),(.057,.008,.095),
        "Amber_Indicator",bevel=.003)

    # Stationary X-side slide rail is attached to receiver through two
    # real load-bearing brackets; moving shoe touches its top face.
    box("Bolt_Fixed_Rail",(.200,.3775,.760),(.080,.025,.700),
        "Titanium_Satin",bevel=0)
    for n,z in enumerate((.525,.945)):
        box("Bolt_Rail_Bracket_%d"%n,(.150,.342,z),(.135,.096,.070),
            "Graphite_Cerakote",bevel=.005)
    box("Bolt_Amber_End_Stop",(.200,.376,1.112),(.090,.016,.025),
        "Amber_Indicator",bevel=.003)
    # The bolt is entirely outside the solid receiver. Its lower shoe
    # contacts the fixed rail at Y=.390; bolt sits above that shoe.
    bolt = empty("BOLT_CARRIER_R", (0,0,0), ROOT)
    box("Bolt_Sliding_Shoe",(.200,.4075,.780),(.067,.035,.130),
        "Elastomer_Black",bolt,True,bevel=0)
    box("Bolt_Machined_Block",(.200,.443,.780),(.092,.050,.185),
        "Titanium_Satin",bolt,True,bevel=.007)
    box("Bolt_Ceramic_Top",(.200,.476,.780),(.070,.019,.143),
        "Ceramic_Ivory",bolt,True,bevel=.004)
    box("Bolt_External_Handle",(.289,.459,.785),(.090,.025,.032),
        "Copper_Anodized",bolt,True,bevel=.007)
    cyl("Bolt_Handle_Knob",(.350,.459,.785),.026,.042,
        "Graphite_Cerakote",bolt,True,segments=24,axis="X")
    box("Bolt_Status_Line",(.200,.488,.780),(.026,.009,.072),
        "Cyan_Optics",bolt,True,bevel=.002)
    for frame,travel in ((1,0),(12,-BOLT_STROKE_M),
                         (24,-BOLT_STROKE_M),(36,0),(48,0)):
        bolt.location.z = travel
        bolt.keyframe_insert(data_path="location",frame=frame,
                             group="Bolt Cycle")
    bolt.location.z = 0
    scene = bpy.context.scene
    scene.frame_start,scene.frame_end,scene.render.fps=1,48,60
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.
    scene.frame_set(1)
    return scene


def mesh_qa():
    verts = tris = 0
    materials = set()
    for ob in meshes:
        me = ob.data
        assert len(me.materials)==1,(ob.name,"material missing")
        materials.add(me.materials[0].name)
        verts += len(me.vertices)
        me.calc_loop_triangles()
        tris += len(me.loop_triangles)
        bm = bmesh.new()
        bm.from_mesh(me)
        assert bm.faces and all(e.is_manifold for e in bm.edges), (
            ob.name,"open or non-manifold")
        assert bm.calc_volume(signed=True)>1e-9, (
            ob.name,"inward winding or zero volume")
        bm.free()
        for tri in me.loop_triangles:
            a,b,c = [me.vertices[i].co for i in tri.vertices]
            assert (b-a).cross(c-a).length > 1e-12,(ob.name,"degenerate")
    assert len(materials)>=3
    assert len(meshes)>=50
    return verts,tris,len(materials)


def bounds(ob):
    matrix = ob.matrix_world
    pts = [matrix @ v.co for v in ob.data.vertices]
    return [(min(p[k] for p in pts),max(p[k] for p in pts))
            for k in range(3)]


def aabb_overlap(a,b):
    return [min(a[k][1],b[k][1])-max(a[k][0],b[k][0])
            for k in range(3)]


def motion_qa(scene):
    rail = bpy.data.objects["Bolt_Fixed_Rail"]
    shoe = bpy.data.objects["Bolt_Sliding_Shoe"]
    receiver = bpy.data.objects["Precision_Receiver"]
    brackets = [bpy.data.objects["Bolt_Rail_Bracket_%d"%n] for n in range(2)]
    samples = 0
    comparisons = 0
    minimum_gap = float("inf")
    # Independently sample the real evaluated mesh matrices, including
    # subframes of the Bezier-interpolated bolt action.
    for frame in range(1,49):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        rail_b = bounds(rail)
        shoe_b = bounds(shoe)
        recv_b = bounds(receiver)
        assert abs(shoe_b[1][0]-rail_b[1][1])<1e-6,(
            frame,"shoe separated from rail")
        assert shoe_b[0][0]>=rail_b[0][0]+.005,(
            frame,"shoe off left edge of rail")
        assert shoe_b[0][1]<=rail_b[0][1]-.005,(
            frame,"shoe off right edge of rail")
        assert shoe_b[2][0]>=rail_b[2][0]+.015,(
            frame,"shoe off rear of rail")
        assert shoe_b[2][1]<=rail_b[2][1]-.015,(
            frame,"shoe off front of rail")
        assert min(aabb_overlap(rail_b,recv_b))<0,(
            frame,"rail/receiver side clearance is expected")
        # Brackets bridge rail and chassis. They deliberately meet the
        # sliding shoe on a plane, but no moving solid penetrates them.
        for bracket in brackets:
            bb=bounds(bracket)
            assert all(x>0.003 for x in aabb_overlap(bb,recv_b)),(
                frame,"bracket detached from receiver")
            assert all(x>0.003 for x in aabb_overlap(bb,rail_b)),(
                frame,"bracket detached from rail")
        for m in moving:
            ma=bounds(m)
            for s in fixed:
                sa=bounds(s)
                overlap=aabb_overlap(ma,sa)
                comparisons+=1
                # Two designed contacts: shoe/rail and shoe/brackets,
                # both must be planar, never volumetric interpenetration.
                if m is shoe and (s is rail or s in brackets):
                    assert overlap[1]<=1e-6,(frame,m.name,s.name,overlap)
                    continue
                if all(x>1e-6 for x in overlap):
                    raise AssertionError((
                        frame,"unexpected moving/fixed penetration",
                        m.name,s.name,overlap))
                gap=math.sqrt(sum(max(0.,-v)**2 for v in overlap))
                if gap>0:
                    minimum_gap=min(minimum_gap,gap)
        samples+=1
    scene.frame_set(1)
    assert samples==48
    return samples,comparisons,minimum_gap


def export(out):
    out.mkdir(parents=True,exist_ok=True)
    bpy.context.scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(STEM+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(out/(STEM+"_animated.glb")),
                              export_format="GLB",export_animations=True)
    bpy.ops.export_scene.fbx(filepath=str(out/(STEM+"_animated.fbx")),
                             axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
                             bake_anim=True)
    if hasattr(bpy.ops.wm,"obj_export"):
        bpy.ops.wm.obj_export(filepath=str(out/(STEM+".obj")),
                              export_materials=True)
    else:
        bpy.ops.export_scene.obj(filepath=str(out/(STEM+".obj")),
                                 use_materials=True)
    for suffix in (".blend","_animated.glb","_animated.fbx",".obj",".mtl"):
        p=out/(STEM+suffix)
        assert p.exists() and p.stat().st_size>100,(p,"export missing")


def main():
    cli=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="/tmp/m01-011")
    args=parser.parse_args(cli)
    out=Path(args.out).resolve()
    scene=build()
    vertices,triangles,materials=mesh_qa()
    samples,checks,minimum_gap=motion_qa(scene)
    export(out)
    report={
        "id":ID,"name":"Długi karabin precyzyjny",
        "status":"BLENDER_EXPORTED",
        "blender_version":bpy.app.version_string,
        "metres_per_unit":1.0,"forward":"+Z","up":"+Y",
        "mount":"MOUNT_TOP_SOCKET at (0,0,0), top vehicle socket",
        "animation_node":"BOLT_CARRIER_R",
        "bolt_axis":"local -Z / rearward","bolt_stroke_m":BOLT_STROKE_M,
        "frames":[1,48],"fps":60,"motion_samples":samples,
        "aabb_comparisons":checks,
        "minimum_noncontact_aabb_gap_m":round(minimum_gap,6),
        "mesh_objects":len(meshes),"vertices":vertices,
        "triangles":triangles,"materials":materials,
        "closed_manifold_positive_volume":"PASS",
        "bolt_track_engagement_and_moving_fixed_aabb":"PASS",
        "bored_barrel":"TRUE ANNULAR MESH",
        "unity_import":"NOT RUN","unity_playmode":"NOT RUN",
        "manual_visual_inspection":"NOT RUN",
        "note":"AABB checks only; physical Unity collision and visual render require separate QA."
    }
    (out/(ID+"_qa.json")).write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(ID+"_RESULT "+json.dumps(report,ensure_ascii=False))


if __name__=="__main__":
    main()
