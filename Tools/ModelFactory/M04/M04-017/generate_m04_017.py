#!/usr/bin/env python3
"""M04-017 — Pierścień teleportacyjny wizualny (no teleport gameplay).

Blender/Unity coordinates: +Y up, +Z forward; 1 Blender unit = 1 metre.
MOUNT_TOP origin (0,0,0) is the top-facing chassis attachment plane Y=0.
Ring centre: (0,1.22,0) m; ring rotates about local Z through 360 degrees.
Eight sector pivots open radially by 0.075 m without meeting neighbouring sectors.
Static supports are BEHIND the ring (positive Z); no moving mesh enters them.
Run: blender -b --factory-startup --python-exit-code 1 --python generate_m04_017.py -- --out build/M04-017
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy

ID = "M04-017"
STEM = "M04-017_teleport_visual_ring"
SEGMENTS = 8
CENTER_Y = 1.22
GAP_DEGREES = 5.0
OPEN_M = 0.075
MATS = {}
PARTS = []
SECTOR_PIVOTS = []


def material(name, rgb, metallic=0.0, roughness=0.4, emission=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    shader = m.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        if "Emission Color" in shader.inputs:
            shader.inputs["Emission Color"].default_value = (*emission, 1.0)
            shader.inputs["Emission Strength"].default_value = 2.0
        elif "Emission" in shader.inputs:
            shader.inputs["Emission"].default_value = (*emission, 1.0)
    MATS[name] = m


def finish(obj, mat_name, parent=None):
    obj.data.materials.append(MATS[mat_name])
    if parent is not None:
        obj.parent = parent
    PARTS.append(obj)
    return obj


def cube(name, center, dimensions, mat_name, parent=None, bevel=0.003):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("Machined rounded edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat_name, parent)


def cylinder(name, center, radius, depth, mat_name, parent=None, vertices=48, axis="Y"):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth,
        location=center, rotation=((math.pi / 2, 0, 0) if axis == "Y" else (0,0,0)))
    obj = bpy.context.object
    obj.name = name
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return finish(obj, mat_name, parent)


def empty(name, position, parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.empty_display_type = "SPHERE"
    obj.empty_display_size = 0.035
    obj.location = position
    if parent is not None:
        obj.parent = parent
    return obj


def arc_sector(name, a0, a1, r_inner, r_outer, thickness, mat_name, parent, steps=22):
    """Watertight annular sector: outward normals and real radial/end walls."""
    vertices = []
    for radius, z in (
        (r_outer, -thickness / 2),
        (r_outer, thickness / 2),
        (r_inner, -thickness / 2),
        (r_inner, thickness / 2),
    ):
        for i in range(steps + 1):
            a = a0 + (a1 - a0) * i / steps
            vertices.append((radius * math.cos(a), radius * math.sin(a), z))
    n = steps + 1
    faces = []
    for i in range(steps):
        j = i + 1
        faces.extend([
            (i, j, n + j, n + i),                      # outer wall
            (2*n+i, 3*n+i, 3*n+j, 2*n+j),            # inner wall
            (2*n+i, 2*n+j, j, i),                    # front face
            (n+i, n+j, 3*n+j, 3*n+i),                # back face
        ])
    faces.append((0, n, 3*n, 2*n))                   # start end-cap
    faces.append((steps, 2*n+steps, 3*n+steps, n+steps))  # end end-cap
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for i, polygon in enumerate(mesh.polygons):
        polygon.use_smooth = i < 4 * steps and i % 4 in (0, 1)
    return finish(obj, mat_name, parent)


def animate_sector(pivot, angle, index):
    direction = (math.cos(angle), math.sin(angle))
    for frame, amount in ((1,0.0),(21,0.025),(61,OPEN_M),(101,0.025),(121,0.0)):
        pivot.location = (direction[0]*amount, direction[1]*amount, 0.0)
        pivot.keyframe_insert(data_path="location", frame=frame, group="SectorDeploy")
    if pivot.animation_data and pivot.animation_data.action:
        pivot.animation_data.action.name = "M04-017_Sector_%02d_Deploy" % index


def build():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for old in list(bpy.data.materials):
        bpy.data.materials.remove(old)
    MATS.clear()
    PARTS.clear()
    SECTOR_PIVOTS.clear()
    material("01_Graphite_Ceramic_Armor", (.050,.073,.100), .55, .37)
    material("02_Satin_Titanium", (.50,.56,.61), .82, .28)
    material("03_Burnished_Copper", (.55,.24,.105), .75, .29)
    material("04_Ivory_Insulator", (.80,.78,.67), .12, .36)
    material("05_Cyan_Arc_Emission", (.012,.29,.45), .25, .25, (.03,.70,1.0))
    material("06_Amber_Status_Emission", (.56,.25,.03), .23, .31, (1.0,.34,.015))
    material("07_Dark_Steel", (.13,.17,.20), .88, .33)
    mount = empty("MOUNT_TOP_Y0", (0,0,0))
    # A low octagonal-footprint mounting block; nothing goes below the chassis plane.
    cube("Base_mounting_plate", (0,.060,.16), (.60,.12,.40), "07_Dark_Steel", mount, .016)
    cube("Base_titanium_spine", (0,.175,.16), (.42,.12,.32), "02_Satin_Titanium", mount, .014)
    cube("Base_copper_crossbar", (0,.245,.16), (.48,.028,.18), "03_Burnished_Copper", mount)
    for side in (-1,1):
        x = side * .22
        cylinder("Base_bolt_%s" % side, (x,.145,.01), .030,.025, "04_Ivory_Insulator", mount, 24)
        # Support rails sit behind the entire rotating assembly: Z>=.21.
        strut = cube("Rear_support_%s" % side,
                     (side*.49,.63,.30), (.095,.79,.09),
                     "02_Satin_Titanium", mount, .012)
        strut.rotation_euler[2] = side * .20
        cube("Support_copper_trim_%s" % side,
             (side*.49,.63,.365), (.032,.66,.023),
             "03_Burnished_Copper", mount, .003)
        cube("Upper_rear_brace_%s" % side,
             (side*.37,1.03,.30), (.12,.18,.09),
             "07_Dark_Steel", mount, .01)
    # Fixed optical projector is recessed behind ring, safely inside the aperture.
    cylinder("Rear_projector_core", (0,CENTER_Y,.23), .22,.085, "07_Dark_Steel", mount, axis="Z")
    cylinder("Projector_cyan_lens", (0,CENTER_Y,.165), .15,.025, "05_Cyan_Arc_Emission", mount, axis="Z")
    cylinder("Projector_ceramic_retainer", (0,CENTER_Y,.185), .175,.016, "04_Ivory_Insulator", mount, axis="Z")
    for k in range(8):
        a = k * 2*math.pi/8
        cylinder("Projector_copper_pin_%02d" % k,
                 (.27*math.cos(a), CENTER_Y+.27*math.sin(a), .23),
                 .020,.04,"03_Burnished_Copper",mount,16,"Z")
    spinner = empty("SPIN_PIVOT_Z_RING_CENTER", (0,CENTER_Y,0), mount)
    for i in range(SEGMENTS):
        midpoint = (i + .5) * 2*math.pi/SEGMENTS
        start = i * 2*math.pi/SEGMENTS + math.radians(GAP_DEGREES)/2
        end = (i+1) * 2*math.pi/SEGMENTS - math.radians(GAP_DEGREES)/2
        sector = empty("SEGMENT_PIVOT_%02d" % (i+1), (0,0,0), spinner)
        SECTOR_PIVOTS.append(sector)
        arc_sector("Armor_sector_%02d" % i, start,end,.697,.821,.115,
                   "01_Graphite_Ceramic_Armor",sector)
        arc_sector("Titanium_outer_rim_%02d" % i,start,end,.827,.866,.125,
                   "02_Satin_Titanium",sector)
        arc_sector("Cyan_inner_channel_%02d" % i,start,end,.660,.691,.082,
                   "05_Cyan_Arc_Emission",sector)
        # These details are in front of the armor: no z overlap.
        x,y=.765*math.cos(midpoint),.765*math.sin(midpoint)
        detail = cube("Copper_index_%02d" % i,(x,y,-.092),
                      (.064,.025,.024),"03_Burnished_Copper",sector,.003)
        detail.rotation_euler[2] = midpoint
        x,y=.77*math.cos(midpoint+.13),.77*math.sin(midpoint+.13)
        led = cube("Amber_index_%02d" % i,(x,y,-.094),
                   (.035,.017,.018),"06_Amber_Status_Emission",sector,.002)
        led.rotation_euler[2] = midpoint+.13
        animate_sector(sector, midpoint, i+1)
    # Avoid the 0 and 360 degree quaternion alias by keying quarter-turns.
    for frame, angle in ((1,0),(31,math.pi/2),(61,math.pi),
                         (91,3*math.pi/2),(121,2*math.pi)):
        spinner.rotation_euler = (0,0,angle)
        spinner.keyframe_insert(data_path="rotation_euler",frame=frame,group="RingSpin")
    if spinner.animation_data and spinner.animation_data.action:
        spinner.animation_data.action.name="M04-017_Ring_One_Revolution"
    scene = bpy.context.scene
    scene.frame_start, scene.frame_end = 1,121
    scene.render.fps=30
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.0
    scene.frame_set(1)
    # Basic geometry QA on original closed mesh surfaces.
    assert len(MATS)>=3 and len(SECTOR_PIVOTS)==8
    assert len(PARTS)>=50
    for obj in PARTS:
        assert obj.type=="MESH" and len(obj.data.vertices)>0
        assert all(len(p.vertices)>=3 and p.area>1e-10 for p in obj.data.polygons),obj.name
        counts={}
        for polygon in obj.data.polygons:
            for edge_key in polygon.edge_keys:
                key=tuple(sorted(edge_key))
                counts[key]=counts.get(key,0)+1
        assert all(n==2 for n in counts.values()),"nonmanifold mesh: "+obj.name
    # Conservative guaranteed distances based on separated radial/axial envelopes.
    # Sector neighbours have >=5 deg angular gap and move only radially.
    gap_at_inner = 2*.660*math.sin(math.radians(GAP_DEGREES)/2)
    radial_layer_gap = min(.697-.691,.827-.821)
    # Static support Z>=.21; spinning sector Z<=+.0625.
    support_axial_gap = .21-.0625
    assert gap_at_inner>.05 and radial_layer_gap>.005 and support_axial_gap>.14
    return {"id":ID,"meshes":len(PARTS),"materials":len(MATS),
            "sector_pivots":len(SECTOR_PIVOTS),
            "ring_rotation_degrees":360,
            "sector_open_m":OPEN_M,
            "min_adjacent_sector_angular_gap_m":round(gap_at_inner,5),
            "min_radial_layer_gap_m":round(radial_layer_gap,5),
            "min_support_axial_gap_m":round(support_axial_gap,5),
            "animation_frames":121,
            "blender_version":bpy.app.version_string,
            "qa_geometry":"PASS",
            "unity_import":"NOT RUN"}


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M04-017")
    out=Path(parser.parse_args(argv).out)
    out.mkdir(parents=True,exist_ok=True)
    report=build()
    bpy.ops.wm.save_as_mainfile(filepath=str((out/(STEM+".blend")).resolve()))
    bpy.ops.export_scene.gltf(filepath=str((out/(STEM+".glb")).resolve()),
                              export_format="GLB",export_animations=True)
    obj_path=str((out/(STEM+".obj")).resolve())
    if hasattr(bpy.ops.wm,"obj_export"):
        bpy.ops.wm.obj_export(filepath=obj_path)
    elif hasattr(bpy.ops.export_scene,"obj"):
        bpy.ops.export_scene.obj(filepath=obj_path)
    else:
        report["obj_export"]="NOT RUN (no OBJ exporter)"
    (out/(STEM+"_manifest.json")).write_text(json.dumps(report,indent=2)+"\n",
                                               encoding="utf-8")
    print("M04-017 BLENDER_GENERATION_COMPLETE",json.dumps(report))


if __name__=="__main__":
    main()
