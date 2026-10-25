#!/usr/bin/env python3
"""M04-021 — Ster ogonowy przegubowy / articulated tail rudder.
Blender 4.x/5.x; Z up, 1 unit = 1 metre.
Rear chassis mount: Y=0, rearward = -Y; origin is mounting pivot (0,0,0).
Fixed swept fin + hinged aft rudder. Rudder yaws around its own local Z
at (0,-0.79,0), within +/-24 degrees; no gameplay implementation.
Run: blender -b --factory-startup --python-exit-code 1 --python generate_m04_021.py -- --out build/M04-021
"""
import bpy
import bmesh
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

MODEL_ID = "M04-021"
MAX_YAW = 24.0
HINGE_Y = -0.79
END_FRAME = 121

def material(name, rgb, metallic=0.0, roughness=0.4, glow=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    shader = m.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if glow:
        if "Emission Color" in shader.inputs:
            shader.inputs["Emission Color"].default_value = (*rgb, 1.0)
        elif "Emission" in shader.inputs:
            shader.inputs["Emission"].default_value = (*rgb, 1.0)
        if "Emission Strength" in shader.inputs:
            shader.inputs["Emission Strength"].default_value = glow
    return m

def attach(obj, role, mat, parent):
    obj["model_id"] = MODEL_ID
    obj["role"] = role
    if mat is not None:
        obj.data.materials.append(mat)
    obj.parent = parent
    return obj

def empty(name, location, parent=None):
    ob = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    if parent is not None:
        ob.parent = parent
    ob.empty_display_type = "ARROWS"
    ob.empty_display_size = 0.09
    return ob

def apply_bevel(ob, radius):
    if radius <= 0:
        return
    mod = ob.modifiers.new("Machined edge radii", "BEVEL")
    mod.width = radius
    mod.segments = 3
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)

def box(name, loc, dims, mat, parent, role="detail", bevel=0.008):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    apply_bevel(ob, min(bevel, min(dims)*0.20))
    return attach(ob, role, mat, parent)

def cylinder(name, loc, radius, depth, mat, parent, role="detail", sides=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius,
                                        depth=depth, location=loc)
    ob = bpy.context.object
    ob.name = name
    for p in ob.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    return attach(ob, role, mat, parent)

def torus(name, loc, radius, tube, mat, parent, role="joint"):
    bpy.ops.mesh.primitive_torus_add(major_segments=48, minor_segments=10,
                                    location=loc, major_radius=radius,
                                    minor_radius=tube)
    ob = bpy.context.object
    ob.name = name
    for p in ob.data.polygons:
        p.use_smooth = True
    return attach(ob, role, mat, parent)

def fin(name, yz, thickness, mat, parent, role, x_offset=0.0, bevel=0.008):
    """Closed extruded YZ silhouette, bevelled; mesh vertices local to parent."""
    poly = list(yz)
    signed = sum(poly[i][0]*poly[(i+1)%len(poly)][1] -
                 poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly)))
    if signed < 0:
        poly.reverse()
    n = len(poly)
    verts = [(x_offset-thickness/2,y,z) for y,z in poly]
    verts += [(x_offset+thickness/2,y,z) for y,z in poly]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    for i in range(n):
        j = (i+1)%n
        faces.append((n+i,i,j,n+j))
    mesh = bpy.data.meshes.new(name+"_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(ob)
    attach(ob,role,mat,parent)
    apply_bevel(ob,min(bevel,thickness*0.18))
    return ob

def build():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials):
        if m.users == 0:
            bpy.data.materials.remove(m)
    mats = {
        "graphite": material("M04-021 / graphite shell", (0.047,0.060,0.080), 0.65,0.36),
        "titanium": material("M04-021 / titanium structure", (0.41,0.48,0.55), 0.85,0.27),
        "copper": material("M04-021 / brushed copper", (0.63,0.29,0.11), 0.78,0.31),
        "ceramic": material("M04-021 / ivory ceramic", (0.76,0.83,0.81), 0.18,0.30),
        "rubber": material("M04-021 / black damping", (0.018,0.024,0.029), 0.0,0.83),
        "cyan": material("M04-021 / cyan position lamp", (0.02,0.68,0.94), 0.1,0.19,2.8),
        "amber": material("M04-021 / amber hazard lamp", (0.97,0.32,0.035), 0.1,0.25,2.2),
    }
    root = empty("M04-021_ROOT_REAR_MOUNT_Y0", (0,0,0))
    root["mount"] = "rear chassis plane Y=0; Z up; rear -Y"
    root["units"] = "1 Blender unit = 1 Unity metre"
    root["motion"] = "hinged aft rudder +/-24 deg about local Z"
    hinge = empty("RUDDER_YAW_PIVOT_Z", (0,HINGE_Y,0), root)
    hinge["axis"] = "local Z"
    hinge["range_degrees"] = "-24..+24"

    # Base: four captive fasteners, resilient gasket, reinforced load path.
    box("Rear mounting plate", (0,-0.018,0.19), (0.56,0.055,0.38),
        mats["graphite"],root,"mount",0.025)
    box("Mount elastomer isolation pad", (0,-0.061,0.19), (0.48,0.019,0.31),
        mats["rubber"],root,"mount",0.007)
    for x in (-0.22,0.22):
        for z in (0.055,0.32):
            bolt = cylinder("Captive mount bolt", (x,-0.063,z),0.026,0.021,
                            mats["titanium"],root,"mount",16)
            bolt.rotation_euler.x = math.pi/2
    for x in (-0.18,0.18):
        box("Rear titanium tail boom", (x,-0.30,0.19),
            (0.055,0.46,0.075),mats["titanium"],root,"mount",0.013)
        box("Copper boom stripe", (x,-0.34,0.236),
            (0.027,0.30,0.012),mats["copper"],root,"mount",0.004)
    box("Central keel", (0,-0.30,0.13),(0.22,0.48,0.11),
        mats["graphite"],root,"mount",0.022)

    # Fixed front fin: its swept rear edge ends at Y=-0.70.
    fixed_outline = [(-0.15,0.16),(-0.43,0.14),(-0.67,0.30),
                     (-0.70,0.83),(-0.49,1.02),(-0.21,0.86)]
    fin("Forward swept fixed fin",fixed_outline,0.092,
        mats["graphite"],root,"fixed_fin",bevel=0.014)
    fin("Forward titanium ridge",
        [(-0.23,0.28),(-0.40,0.27),(-0.60,0.40),
         (-0.62,0.77),(-0.47,0.90),(-0.28,0.80)],
        0.020,mats["titanium"],root,"fixed_fin",x_offset=0.052,bevel=0.004)
    fin("Fixed white swept edge",
        [(-0.49,0.92),(-0.64,0.79),(-0.66,0.42),(-0.69,0.43),
         (-0.69,0.83),(-0.49,1.01)],
        0.017,mats["ceramic"],root,"fixed_fin",x_offset=-0.055,bevel=0.003)
    for z in (0.34,0.53,0.72):
        box("Fixed copper rib", (0.062,-0.39,z),
            (0.014,0.22,0.023),mats["copper"],root,"fixed_fin",0.004)
    box("Fixed upper beacon", (0,-0.48,1.04),
        (0.065,0.090,0.025),mats["amber"],root,"fixed_fin",0.006)

    # Vertical hinge shaft; bearing races deliberately touch their axle.
    cylinder("Exposed vertical hinge axle",(0,HINGE_Y,0.55),
             0.022,0.72,mats["titanium"],root,"joint",32)
    for z in (0.29,0.82):
        cylinder("Fixed bearing shell",(0,HINGE_Y,z),
                 0.050,0.055,mats["graphite"],root,"joint",32)
        torus("Bearing copper rim",(0,HINGE_Y,z),0.050,0.009,
              mats["copper"],root,"joint")
    for z in (0.42,0.68):
        torus("Moving hinge thrust race",(0,0,z),0.042,0.009,
              mats["titanium"],hinge,"joint")
        box("Hinge-to-rudder lug",(0,-0.045,z),
            (0.064,0.11,0.040),mats["copper"],hinge,"joint",0.008)

    # Rear section is built in hinge-local coordinates (no hidden world transforms).
    # The foremost swept-panel point is -0.06m behind the hinge; fixed fin ends -0.70m,
    # leaving >=0.10m of geometric clearance at full steering deflection.
    aft_outline = [(-0.060,0.25),(-0.35,0.20),(-0.59,0.42),
                   (-0.57,0.68),(-0.32,0.86),(-0.060,0.81)]
    fin("Articulated rear rudder blade",aft_outline,0.072,
        mats["graphite"],hinge,"swept_panel",bevel=0.013)
    fin("Rudder raised titanium chevron",
        [(-0.13,0.31),(-0.34,0.29),(-0.51,0.44),
         (-0.50,0.62),(-0.31,0.76),(-0.14,0.74)],
        0.018,mats["titanium"],hinge,"swept_panel",
        x_offset=0.043,bevel=0.003)
    fin("Ceramic aft cutting edge",
        [(-0.47,0.31),(-0.59,0.42),(-0.57,0.68),
         (-0.45,0.78),(-0.48,0.68),(-0.50,0.43)],
        0.017,mats["ceramic"],hinge,"swept_panel",
        x_offset=-0.045,bevel=0.003)
    for z in (0.38,0.52,0.66):
        box("Copper rudder cross rib",(0.054,-0.28,z),
            (0.016,0.29,0.018),mats["copper"],hinge,"swept_panel",0.003)
    for z in (0.39,0.65):
        box("Cyan steering position indicator",(0.057,-0.46,z),
            (0.015,0.095,0.014),mats["cyan"],hinge,"swept_panel",0.003)
    box("Rudder trailing amber lamp",(0,-0.565,0.55),
        (0.085,0.032,0.055),mats["amber"],hinge,"swept_panel",0.006)
    for frame,degrees in ((1,0),(22,-MAX_YAW),(55,0),
                          (88,MAX_YAW),(121,0)):
        hinge.rotation_euler = (0,0,math.radians(degrees))
        hinge.keyframe_insert(data_path="rotation_euler",frame=frame,
                              group="M04-021 steering yaw")
    if hinge.animation_data and hinge.animation_data.action:
        hinge.animation_data.action.name = "M04-021_RUDDER_YAW"
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.0
    scene.frame_start=1
    scene.frame_end=END_FRAME
    scene.render.fps=30
    scene.frame_set(1)
    bpy.context.view_layer.update()
    return root,hinge

def world_bounds(objects):
    # Transform all actual mesh vertices; not merely object origins.
    pts=(o.matrix_world @ v.co for o in objects for v in o.data.vertices)
    coords=list(pts)
    return ([min(p[i] for p in coords) for i in range(3)],
            [max(p[i] for p in coords) for i in range(3)])

def validate(hinge):
    scene=bpy.context.scene
    meshes=[o for o in scene.objects if o.type=="MESH" and
            o.get("model_id")==MODEL_ID]
    materials={m.name for o in meshes for m in o.data.materials}
    assert len(meshes)>=28, ("missing detail",len(meshes))
    assert len(materials)>=7
    assert scene.unit_settings.scale_length==1.0
    assert all(abs(a-b)<1e-6 for a,b in zip(hinge.location,(0,HINGE_Y,0))), tuple(hinge.location)
    edges=0
    for o in meshes:
        assert len(o.data.polygons)>0 and len(o.data.vertices)>0,o.name
        bm=bmesh.new()
        try:
            bm.from_mesh(o.data)
            assert all(e.is_manifold for e in bm.edges),("non-manifold",o.name)
            assert all(f.calc_area()>1e-12 for f in bm.faces),("degenerate",o.name)
            assert all(math.isfinite(c) for v in bm.verts for c in v.co),o.name
            edges+=len(bm.edges)
        finally:
            bm.free()
    fixed=[o for o in meshes if o.get("role")=="fixed_fin"]
    swept=[o for o in meshes if o.get("role")=="swept_panel"]
    assert len(fixed)>=4 and len(swept)>=6
    minimum_gap=100.0
    vertex_samples=0
    # 0.5 degree increments, including both mechanical end stops.
    for step in range(-48,49):
        hinge.rotation_euler.z=math.radians(step*0.5)
        bpy.context.view_layer.update()
        stationary=world_bounds(fixed)
        mobile=world_bounds(swept)
        gap=stationary[0][1]-mobile[1][1]
        minimum_gap=min(minimum_gap,gap)
        vertex_samples+=sum(len(o.data.vertices) for o in swept)
        assert gap>0.045,("rudder/fixed fin swept overlap",step*0.5,gap)
    scene.frame_set(1)
    bpy.context.view_layer.update()
    report={
        "id":MODEL_ID,"mesh_count":len(meshes),
        "material_count":len(materials),"materials":sorted(materials),
        "manifold_edges_checked":edges,"yaw_range_deg":[-MAX_YAW,MAX_YAW],
        "yaw_axis":"hinge local Z","hinge_pivot_m":[0,HINGE_Y,0],
        "mount":"rear chassis Y=0, Z up, origin=(0,0,0)",
        "sampled_yaw_poses":97,"swept_vertex_checks":vertex_samples,
        "minimum_fixed_to_rudder_y_gap_m":round(minimum_gap,6),
        "collision_scope":"swept panel vs fixed fin, 0.5-degree samples; bearing/hinge contact excluded; NOT full continuous triangle CCD",
        "blender_qa":"PASS","unity":"NOT RUN"
    }
    print("M04-021 GEOMETRY QA "+json.dumps(report,ensure_ascii=False))
    return report

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    out=Path(argv[argv.index("--out")+1]) if "--out" in argv else Path("build/M04-021")
    out.mkdir(parents=True,exist_ok=True)
    root,hinge=build()
    report=validate(hinge)
    bpy.ops.wm.save_as_mainfile(filepath=str((out/"M04-021.blend").resolve()))
    bpy.ops.export_scene.gltf(filepath=str((out/"M04-021.glb").resolve()),
                              export_format="GLB",export_animations=True)
    report["blend_bytes"]=(out/"M04-021.blend").stat().st_size
    report["glb_bytes"]=(out/"M04-021.glb").stat().st_size
    (out/"M04-021_QA.json").write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print("M04-021 EXPORT COMPLETE",str(out.resolve()))

if __name__=="__main__":
    main()
