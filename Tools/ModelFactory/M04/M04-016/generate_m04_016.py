#!/usr/bin/env python3
"""M04-016: corner-mounted four-nozzle microthruster cluster (visual only).

Unity scale 1 Blender unit = 1 metre. +Y up, +Z vehicle forward, +X right.
The mounting pivot (0,0,0) is the intersection of two perpendicular chassis
corner faces X=0 and Z=0. The thruster assembly projects into +X,+Z and -Y.
Four NOZZLE_PIVOT empties independently tilt their nozzles by +/-12 degrees.
This file intentionally does not implement propulsion/gameplay.
Run: blender -b --factory-startup --python-exit-code 1 --python generate_m04_016.py -- --out build/M04-016
"""
import argparse
import json
import math
import sys
from pathlib import Path

try:
    import bpy
except ImportError as exc:
    raise SystemExit("Requires Blender bpy (not ordinary Python)") from exc

ID = "M04-016"
OUTNAME = "M04-016_corner_microthrusters"
CENTRES = ((0.28, 0.28), (0.28, 0.67), (0.67, 0.28), (0.67, 0.67))
TILT_DEG = 12.0
FRAMES = (1, 21, 41, 61, 81)
MATS = {}
MOVING = {}
STATIC = []
PIVOTS = []

def mat(name, rgb, metal=0.0, rough=0.38, glow=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    bs = m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value = (*rgb, 1.0)
    bs.inputs["Metallic"].default_value = metal
    bs.inputs["Roughness"].default_value = rough
    if glow:
        if "Emission Color" in bs.inputs:
            bs.inputs["Emission Color"].default_value = (*glow, 1.0)
            bs.inputs["Emission Strength"].default_value = 1.8
        elif "Emission" in bs.inputs:
            bs.inputs["Emission"].default_value = (*glow, 1.0)
    MATS[name] = m

def finish(o, material, group=None):
    o.data.materials.append(MATS[material])
    for f in o.data.polygons:
        f.use_smooth = False  # avoid smoothing planar caps across sharp edges
    if group is None:
        STATIC.append(o)
    else:
        MOVING[group].append(o)
    return o

def cube(name, pos, dims, material, group=None, bevel=0.004):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    o = bpy.context.object
    o.name = name
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new("Manufactured edge radius", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, material, group)

def cyl(name, pos, radius, length, material, group=None, vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius,
        depth=length, location=pos, rotation=(math.pi / 2, 0, 0))
    o = bpy.context.object
    o.name = name
    o = finish(o, material, group)
    for face in o.data.polygons:
        face.use_smooth = len(face.vertices) == 4  # cylindrical wall only
    return o

def ring(name, pos, ro, ri, depth, material, group=None, sides=48):
    """Closed four-wall annular tube; genuine open centre, manifold mesh."""
    verts = []
    for r, y in ((ro, -depth/2), (ro, depth/2),
                 (ri, -depth/2), (ri, depth/2)):
        verts += [(r*math.cos(2*math.pi*k/sides),
                   r*math.sin(2*math.pi*k/sides), y)
                  for k in range(sides)]
    faces = []
    n = sides
    for i in range(n):
        j = (i+1) % n
        faces += [(i,j,n+j,n+i), (n+i,n+j,3*n+j,3*n+i),
                  (2*n+j,2*n+i,3*n+i,3*n+j),
                  (i,2*n+i,2*n+j,j)]
    mesh = bpy.data.meshes.new(name+"_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    o.location = pos
    o.rotation_euler = (math.pi/2, 0, 0)
    o = finish(o, material, group)
    for face_idx, face in enumerate(o.data.polygons):
        face.use_smooth = face_idx % 4 in (0, 2)  # curved walls, not annular end caps
    return o

def empty(name, location, parent=None):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    o.empty_display_type = "SPHERE"
    o.empty_display_size = 0.045
    if parent:
        o.parent = parent
    o.location = location
    return o

def build():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for m in list(bpy.data.materials):
        bpy.data.materials.remove(m)
    MATS.clear()
    MOVING.clear()
    STATIC.clear()
    PIVOTS.clear()
    mat("01_Carbon_Graphite", (0.055,0.075,0.105), .63, .34)
    mat("02_Satin_Titanium", (0.48,0.54,0.58), .82, .28)
    mat("03_Anodized_Copper", (0.57,0.235,0.105), .76, .29)
    mat("04_Ivory_Ceramic", (0.75,0.75,0.67), .10, .42)
    mat("05_Rotor_Dark_Steel", (0.15,0.18,0.20), .91, .29)
    mat("06_Cyan_Ion_Emission", (0.012,0.26,0.38), .25, .25, (0.0,.66,1.0))
    mat("07_Amber_Safety_Emission", (.60,.27,.04), .28, .32, (1.0,.31,.018))
    mount = empty("MOUNT_CORNER_X0_Z0", (0,0,0))
    # Two perpendicular physical mounting surfaces, no top mounting plate.
    cube("Socket_Face_X0", (.025,-.085,.45), (.05,.17,.88), "02_Satin_Titanium")
    cube("Socket_Face_Z0", (.45,-.085,.025), (.88,.17,.05), "02_Satin_Titanium")
    cube("Diagonal_Armor_Bridge", (.47,-.16,.47), (.82,.07,.82), "01_Carbon_Graphite", bevel=.025)
    # Braced exposed edges make the corner silhouette read as a rugged module.
    for x in (.09,.86):
        cube("Longitudinal_Brace_%s" % x, (x,-.18,.47),
             (.032,.09,.82), "02_Satin_Titanium")
    for z in (.09,.86):
        cube("Cross_Brace_%s" % z, (.47,-.18,z),
             (.82,.09,.032), "03_Anodized_Copper")
    for i,(x,z) in enumerate(CENTRES):
        # Stationary pedestal ends at Y=-0.285; animated ring begins below
        # Y=-0.36 even at extreme tilt, so the two do not interpenetrate.
        cube("Pedestal_%02d" % i, (x,-.252,z),
             (.115,.065,.115), "02_Satin_Titanium", bevel=.012)
        cyl("Bearing_Disc_%02d" % i, (x,-.294,z), .064, .017,
            "03_Anodized_Copper", vertices=24)
        p = empty("NOZZLE_PIVOT_%02d" % i, (x,-.455,z), mount)
        PIVOTS.append(p)
        MOVING[i] = []
        # All movable geometry is built in nozzle-local coordinates.
        ring("Duct_%02d" % i, (0,-.085,0), .124,.098,.178,
             "01_Carbon_Graphite",i)
        ring("Mouth_Ceramic_%02d" % i, (0,-.178,0), .126,.098,.018,
             "04_Ivory_Ceramic",i)
        ring("Cyan_Lip_%02d" % i, (0,-.192,0), .116,.102,.009,
             "06_Cyan_Ion_Emission",i)
        ring("Rear_Heat_Shield_%02d" % i, (0,.002,0), .128,.097,.014,
             "02_Satin_Titanium",i)
        cyl("Turbine_Hub_%02d" % i, (0,-.091,0), .026,.052,
            "05_Rotor_Dark_Steel",i,vertices=24)
        for blade in range(4):
            a = math.pi*blade/2 + math.pi/4
            r = .061
            b = cube("Blade_%02d_%d" % (i,blade),
                     (r*math.cos(a),-.092,r*math.sin(a)),
                     (.085,.012,.023), "05_Rotor_Dark_Steel",i,bevel=.003)
            b.rotation_euler[1] = -a
        for j in range(6):
            a = 2*math.pi*j/6
            rad = .139
            fin = cube("Copper_Radiator_%02d_%d" % (i,j),
                       (rad*math.cos(a),-.082,rad*math.sin(a)),
                       (.023,.105,.017), "03_Anodized_Copper",i,bevel=.002)
            fin.rotation_euler[1] = -a
        for j in range(2):
            a = math.pi*j
            rad = .145
            cube("Amber_Status_%02d_%d" % (i,j),
                 (rad*math.cos(a),-.16,rad*math.sin(a)),
                 (.024,.021,.024), "07_Amber_Safety_Emission",i,bevel=.002)
        for obj in MOVING[i]:
            obj.parent = p
        # Distinct gimbal axes: two rotate about X and two about Z.
        axis = 0 if i in (0,3) else 2
        phase = -1 if i in (1,2) else 1
        for frame,deg in zip(FRAMES,(0,12,0,-12,0)):
            p.rotation_euler[axis] = math.radians(deg*phase)
            p.keyframe_insert(data_path="rotation_euler",index=axis,frame=frame)
        p.rotation_euler[axis] = 0.0
    for obj in STATIC:
        obj.parent = mount
    bpy.context.scene.frame_start = 1
    bpy.context.scene.frame_end = 81
    bpy.context.scene.render.fps = 30
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()

def validate():
    assert len(PIVOTS)==4 and len(MATS)>=3
    assert all(len(MOVING[i])>=12 for i in range(4))
    assert tuple(PIVOTS[0].parent.location)==(0.0,0.0,0.0)
    assert all(p.animation_data and p.animation_data.action for p in PIVOTS)
    meshes = [o for o in bpy.data.objects if o.type=="MESH"]
    assert len(meshes)>=65
    assert all(len(o.data.polygons)>0 for o in meshes)
    assert all(f.area>1e-10 for o in meshes for f in o.data.polygons)
    # Swept envelopes: each movable nozzle fits in a sphere of radius .30m
    # centred on its pivot, across all +/-12 degree tilts. The 0.39m centre
    # spacing is too small for these broad spheres, so use tighter horizontal
    # bound: radius .145 + .196*sin(12deg) = .1858m; 2*.1858 < .39m.
    horizontal_radius=.145+.196*math.sin(math.radians(TILT_DEG))
    nearest=min(math.hypot(a[0]-b[0],a[1]-b[1])
                for i,a in enumerate(CENTRES) for b in CENTRES[i+1:])
    nozzle_gap=nearest-2*horizontal_radius
    assert nozzle_gap>.015, nozzle_gap
    # Static pedestals stop at Y=-.285; moving duct top at local +.009
    # plus tilt rise max .145*sin(12deg), pivot Y=-.455.
    highest_moving=-.455+.009+.145*math.sin(math.radians(TILT_DEG))
    static_lowest=-.294-.017/2
    vertical_gap=static_lowest-highest_moving
    assert vertical_gap>.09, vertical_gap
    return dict(id=ID, meshes=len(meshes),
                vertices=sum(len(o.data.vertices) for o in meshes),
                triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),
                materials=len(MATS), nozzles=4, animated_pivots=len(PIVOTS),
                mount=[0,0,0], mount_planes=["X=0","Z=0"],
                gimbal_axes=["X","Z","Z","X"],
                tilt_range_degrees=[-12,12], animation_frames=list(FRAMES),
                min_inter_nozzle_analytical_gap_m=round(nozzle_gap,5),
                min_pedestal_to_nozzle_analytical_gap_m=round(vertical_gap,5),
                unit_scale_m=1, unity="NOT RUN",
                limitations="Conservative geometric envelopes; not full swept triangle collision or Unity physics.")

def main():
    argv = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--out",default="build/M04-016")
    out = Path(ap.parse_args(argv).out).resolve()
    out.mkdir(parents=True,exist_ok=True)
    build()
    report=validate()
    (out/(OUTNAME+"_manifest.json")).write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(OUTNAME+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(out/(OUTNAME+".glb")),
                              export_format="GLB",export_animations=True)
    if hasattr(bpy.ops.wm,"obj_export"):
        bpy.ops.wm.obj_export(filepath=str(out/(OUTNAME+".obj")),
                              export_materials=True)
    else:
        bpy.ops.export_scene.obj(filepath=str(out/(OUTNAME+".obj")),
                                 use_materials=True)
    print("M04-016 BLENDER_GENERATION_PASS",json.dumps(report,ensure_ascii=False))

if __name__=="__main__":
    main()
