#!/usr/bin/env python3
"""M04-018 | Deployable stabilizer anchor for RoboRougelike (visual asset only).

Unity coordinates: +Y up, +Z forward, metres. Origin (0,0,0) is the
underside chassis mounting plane. The anchor extends in negative Y.
The carriage slides 0.22 m down; four claw pivots open 52 degrees
about their local Z axes. All claw axes are side-oriented on the carriage,
not attached to the upper mounting face. No gameplay code is generated.

Run: blender -b --factory-startup --python-exit-code 1 --python
     generate_m04_018.py -- --out build/M04-018
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ID = "M04-018"
STEM = "M04-018_stabilizer_anchor"
MATS = {}
MESHES = []
CLAWS = []
OPEN_DEG = 52
STROKE = 0.22


def mat(name, rgb, metallic=0.0, roughness=0.45, glow=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = (*rgb, 1.0)
    p.inputs["Metallic"].default_value = metallic
    p.inputs["Roughness"].default_value = roughness
    if glow is not None:
        if "Emission Color" in p.inputs:
            p.inputs["Emission Color"].default_value = (*glow, 1)
            p.inputs["Emission Strength"].default_value = 1.8
        elif "Emission" in p.inputs:
            p.inputs["Emission"].default_value = (*glow, 1)
    MATS[name] = m


def finish(obj, material, parent=None, bevel=0.0):
    obj.name = obj.name.replace(".", "_")
    obj.data.materials.append(MATS[material])
    if parent is not None:
        obj.parent = parent
    if bevel:
        mod = obj.modifiers.new("Soft machined edges", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    MESHES.append(obj)
    return obj


def box(name, loc, dims, material, parent=None, bevel=0.004):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.object
    o.name = name
    o.location = loc
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(o, material, parent, bevel)


def cyl(name, loc, radius, depth, material, parent=None, vertices=32, axis="Y"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth)
    o = bpy.context.object
    o.name = name
    o.location = loc
    if axis == "Y":
        o.rotation_euler[0] = math.pi / 2
    elif axis == "X":
        o.rotation_euler[1] = math.pi / 2
    return finish(o, material, parent, 0.002)


def torus(name, loc, major, minor, material, parent=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=8,
                                    location=loc, major_radius=major,
                                    minor_radius=minor)
    o = bpy.context.object
    o.name = name
    o.rotation_euler[0] = math.pi / 2
    return finish(o, material, parent)


def prism(name, outline, half_width, material, parent=None):
    """Closed watertight XY profile extruded symmetrically in local Z."""
    area = sum(outline[i][0]*outline[(i+1)%len(outline)][1]
               -outline[(i+1)%len(outline)][0]*outline[i][1]
               for i in range(len(outline)))
    if area < 0:
        outline = list(reversed(outline))
    n = len(outline)
    verts = [(x,y,-half_width) for x,y in outline]
    verts += [(x,y,half_width) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name+"_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material, parent, 0.003)


def empty(name, loc, parent=None):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    o.empty_display_type = "PLAIN_AXES"
    o.empty_display_size = 0.07
    o.location = loc
    if parent is not None:
        o.parent = parent
    return o


def rod(name, a, b, radius, material, parent=None):
    mid = (Vector(a)+Vector(b))*0.5
    vec = Vector(b)-Vector(a)
    o = cyl(name, tuple(mid), radius, vec.length, material, parent, 16, "Z")
    o.rotation_euler = vec.to_track_quat("Z", "Y").to_euler()
    return o


def setup():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials):
        if m.users == 0:
            bpy.data.materials.remove(m)
    mat("01_Graphite_Armor", (.045,.057,.070), .62, .38)
    mat("02_Brushed_Titanium", (.40,.48,.52), .78, .29)
    mat("03_Burnished_Copper", (.49,.24,.10), .78, .33)
    mat("04_White_Ceramic", (.79,.81,.75), .13, .26)
    mat("05_Rubber_Grip", (.075,.085,.077), .0, .83)
    mat("06_Cyan_Indicator", (.016,.32,.40), .22, .27, (.01,.7,1))
    mat("07_Amber_Warning", (.75,.32,.035), .25, .33, (1,.25,.01))
    mat("08_Dark_Steel", (.13,.15,.17), .82, .43)


def animate(obj, data_path, keyframes, group):
    for frame, value in keyframes:
        if data_path == "location":
            obj.location.y = value
        else:
            obj.rotation_euler.z = value
        obj.keyframe_insert(data_path=data_path, frame=frame, group=group)


def build():
    setup()
    mount = empty("MOUNT_UNDERSIDE_ORIGIN", (0,0,0))
    cyl("Mount_octagonal_plate", (0,-.045,0), .285,.090,
        "02_Brushed_Titanium",mount,8)
    cyl("Mount_gasket", (0,-.095,0), .225,.018,"05_Rubber_Grip",mount)
    cyl("Housing_pressure_shell", (0,-.242,0), .166,.270,
        "01_Graphite_Armor",mount)
    cyl("Housing_lower_ceramic_seal", (0,-.381,0), .172,.026,
        "04_White_Ceramic",mount)
    torus("Housing_copper_service_ring", (0,-.135,0), .173,.012,
          "03_Burnished_Copper",mount)
    for i in range(8):
        a = i*math.tau/8
        x,z = .247*math.cos(a),.247*math.sin(a)
        cyl("Mount_bolt_%02d"%i,(x,-.097,z),.020,.019,
            "08_Dark_Steel",mount,12)
    for i in range(4):
        a = (i+.5)*math.pi/2
        x,z=.17*math.cos(a),.17*math.sin(a)
        box("Housing_heat_sink_%02d"%i,(x,-.245,z),
            (.025,.175,.045),"03_Burnished_Copper",mount,.003)
    carriage = empty("CARRIAGE_PIVOT_Y_TELESCOPE", (0,-.315,0),mount)
    # Sliding piston deliberately telescopes INSIDE the housing sleeve.
    cyl("Piston_polished_shaft", (0,-.105,0),.102,.345,
        "02_Brushed_Titanium",carriage)
    cyl("Piston_ceramic_bearing", (0,-.278,0),.127,.060,
        "04_White_Ceramic",carriage)
    cyl("Carriage_lower_flange", (0,-.324,0),.157,.050,
        "08_Dark_Steel",carriage,16)
    torus("Carriage_amber_status", (0,-.342,0),.136,.010,
          "07_Amber_Warning",carriage)
    for i in range(4):
        a=i*math.pi/2
        yaw=empty("CLAW_AZIMUTH_%02d"%i,(0,-.225,0),carriage)
        yaw.rotation_euler.y=-a
        # Bearing and claw pivot are located on the SIDE of the carriage.
        cyl("Claw_hinge_bearing_%02d"%i,(.207,0,0),.068,.105,
            "08_Dark_Steel",yaw,24,"Z")
        cyl("Claw_hinge_copper_cap_%02d"%i,(.207,0,.060),.048,.018,
            "03_Burnished_Copper",yaw,20,"Z")
        hinge=empty("CLAW_DEPLOY_PIVOT_Z_%02d"%i,(.207,0,0),yaw)
        CLAWS.append(hinge)
        prism("Claw_forged_taper_%02d"%i,
              [(.032,-.018),(.090,-.015),(.137,-.210),
               (.106,-.355),(.052,-.330)],
              .042,"02_Brushed_Titanium",hinge)
        prism("Claw_graphite_guard_%02d"%i,
              [(.055,-.065),(.095,-.070),(.118,-.225),
               (.086,-.250),(.062,-.230)],
              .050,"01_Graphite_Armor",hinge)
        rod("Claw_external_copper_rib_%02d"%i,
            (.105,-.040,.053),(.127,-.250,.053),.012,
            "03_Burnished_Copper",hinge)
        box("Claw_emissive_safety_mark_%02d"%i,
            (.109,-.115,.058),(.018,.050,.011),
            "06_Cyan_Indicator",hinge,.002)
        prism("Claw_angled_ground_shoe_%02d"%i,
              [(.058,-.320),(.166,-.338),(.204,-.379),
               (.145,-.420),(.077,-.384)],
              .060,"05_Rubber_Grip",hinge)
        prism("Claw_ceramic_ground_spike_%02d"%i,
              [(.161,-.374),(.222,-.390),(.251,-.441),
               (.189,-.426)],
              .037,"04_White_Ceramic",hinge)
        box("Claw_warning_plate_%02d"%i,
            (.118,-.305,-.058),(.040,.022,.013),
            "07_Amber_Warning",hinge,.002)
    animate(carriage,"location",
            [(1,-.315),(25,-.535),(90,-.535),(118,-.315),(140,-.315)],
            "AnchorTelescoping")
    for hinge in CLAWS:
        animate(hinge,"rotation_euler",
                [(1,0),(25,0),(54,math.radians(OPEN_DEG)),
                 (86,math.radians(OPEN_DEG)),(116,0),(140,0)],
                "AnchorClawDeployment")
    scene=bpy.context.scene
    scene.frame_start,scene.frame_end=1,140
    scene.render.fps=30
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.0
    # Geometry QA: closed meshes, no degenerate faces, consistent local normals.
    for obj in MESHES:
        assert obj.type=="MESH" and len(obj.data.vertices)>0,obj.name
        assert all(p.area>1e-10 for p in obj.data.polygons),obj.name
        edges={}
        for p in obj.data.polygons:
            for edge in p.edge_keys:
                edge=tuple(sorted(edge))
                edges[edge]=edges.get(edge,0)+1
        assert edges and all(v==2 for v in edges.values()),"nonmanifold "+obj.name
    # Swept clearance for the 4 articulated claws, using ACTUAL transformed
    # vertices in 29 sampled animation poses. Excludes intentional shaft/sleeve
    # telescopic contact and hinge bearings, which are designed mating parts.
    claw_meshes=[o for o in MESHES if o.parent in CLAWS]
    assert len(claw_meshes)==4*7
    min_radial=999.0
    min_angular=999.0
    sample_frames=sorted(set([1,25,54,86,116,140]+list(range(1,141,5))))
    checks=0
    for frame in sample_frames:
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for i in range(4):
            group=claw_meshes[i*7:(i+1)*7]
            for o in group:
                for v in o.data.vertices:
                    p=o.matrix_world @ v.co
                    radius=math.hypot(p.x,p.z)
                    min_radial=min(min_radial,radius)
                    checks+=1
                    # Each claw must remain within its own 90-degree sector.
                    az=math.atan2(p.z,p.x)
                    centre=i*math.pi/2
                    delta=abs(math.atan2(math.sin(az-centre),math.cos(az-centre)))
                    min_angular=min(min_angular,math.pi/4-delta)
    assert min_radial>.198,("claw enters housing radial envelope",min_radial)
    assert min_angular>math.radians(12),("claw enters neighbouring sector",min_angular)
    scene.frame_set(1)
    return {"id":ID,"model":"Kotwica stabilizująca","meshes":len(MESHES),
            "materials":len(MATS),"claw_pivots":len(CLAWS),
            "stroke_m":STROKE,"claw_deploy_deg":OPEN_DEG,
            "animation_frames":140,"qa_geometry":"PASS",
            "sampled_animation_frames":len(sample_frames),
            "sampled_vertex_checks":checks,
            "min_claw_radius_m":round(min_radial,5),
            "housing_radius_m":.172,
            "min_radial_clearance_m":round(min_radial-.172,5),
            "min_sector_angular_clearance_deg":round(math.degrees(min_angular),3),
            "blender_version":bpy.app.version_string,
            "unity_editmode":"NOT RUN","unity_playmode":"NOT RUN",
            "swept_triangle_collision":"NOT RUN (sampled vertex/sector envelopes only)"}


def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M04-018")
    out=Path(parser.parse_args(argv).out)
    out.mkdir(parents=True,exist_ok=True)
    report=build()
    bpy.ops.wm.save_as_mainfile(filepath=str((out/(STEM+".blend")).resolve()))
    bpy.ops.export_scene.gltf(filepath=str((out/(STEM+".glb")).resolve()),
                              export_format="GLB",export_animations=True)
    path=str((out/(STEM+".obj")).resolve())
    if hasattr(bpy.ops.wm,"obj_export"):
        bpy.ops.wm.obj_export(filepath=path)
        report["obj_export"]="PASS"
    elif hasattr(bpy.ops.export_scene,"obj"):
        bpy.ops.export_scene.obj(filepath=path)
        report["obj_export"]="PASS"
    else:
        report["obj_export"]="NOT RUN (OBJ exporter unavailable)"
    (out/(STEM+"_manifest.json")).write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print("M04-018 GENERATOR_COMPLETE",json.dumps(report))


if __name__=="__main__":
    main()
