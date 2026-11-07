# Showcase renders for the wheel variants: the real R4M_Wheel* GLBs mounted on R4 block ports (copy of R4Blocks/scripts/compat_showcase.py) with the same placement
# math as compat_matrix.py (module socket 30 mm off the block port, socket direction against the port normal).
# usage: blender -b --factory-startup --python wheels_showcase.py -- <wynik dir> <out dir>
import bpy, os, sys, math, json
from mathutils import Vector, Matrix

G, H, GAP = 0.55, 0.26, 0.030
DIRS = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 0, 1), "down": (0, 0, -1), "forward": (0, -1, 0), "back": (0, 1, 0)}
a = sys.argv[sys.argv.index("--") + 1:]; ROOT, OUT = a[0], a[1]
WD, MD, BD = os.path.join(ROOT, "R4_Weapons_938_port"), os.path.join(ROOT, "R4_Movement_940_port"), os.path.join(ROOT, "R4_Blocks")
ROWS = {
    "wheels": [("R4M_WheelS_Fork_Road", "Cube", "down", (0, 0, 0)), ("R4M_WheelS_Caster_Slick", "Cube", "down", (0, 0, 0)),
               ("R4M_WheelM_Steer_Road", "Cube", "down", (0, 0, 0)), ("R4M_WheelM_SwingArm_Offroad", "Cube", "down", (0, 0, 0)),
               ("R4M_WheelM_Twin_Road", "Cube", "down", (0, 0, 0)), ("R4M_WheelM_Fork_Spiked", "Cube", "down", (0, 0, 0)),
               ("R4M_WheelL_Fork_Balloon", "Cube", "down", (0, 0, 0)), ("R4M_WheelL_Steer_Offroad", "Cube", "down", (0, 0, 0)),
               ("R4M_WheelM_Side_Slick", "Cube", "right", (0, 0, 0)), ("R4M_WheelM_Side_Offroad", "Cube", "right", (0, 0, 0)),
               ("R4M_WheelL_Side_Offroad", "Cube", "right", (0, 0, 0)), ("R4M_WheelXL_Side_Monster", "Cube", "right", (0, 0, 0))],
}


def load(path):
    before = set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=path)
    return [o for o in bpy.data.objects if o not in before]


def place(objs, X):
    bpy.context.view_layer.update()
    for o in objs:
        if o.parent is None: o.matrix_world = X @ o.matrix_world


def setup(scn):
    eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    scn.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in eng else 'BLENDER_EEVEE_NEXT'
    scn.render.resolution_x, scn.render.resolution_y = 1800, 760
    w = scn.world or bpy.data.worlds.new("W"); scn.world = w; w.use_nodes = True
    bg = w.node_tree.nodes.get("Background"); bg.inputs[0].default_value = (0.20, 0.21, 0.23, 1); bg.inputs[1].default_value = 0.9
    vt = [i.identifier for i in scn.view_settings.bl_rna.properties['view_transform'].enum_items]; scn.view_settings.view_transform = 'AgX' if 'AgX' in vt else 'Filmic'
    for (nm, typ, en, loc, rot) in (("Key", 'SUN', 3.2, (2, -2, 3), (math.radians(50), 0, math.radians(35))), ("Fill", 'SUN', 1.0, (-3, -1.5, 1.5), (math.radians(70), 0, math.radians(-65))),
                                    ("Rim", 'SUN', 1.2, (0.5, 3.5, 2.2), (math.radians(-60), 0, math.radians(180)))):
        ld = bpy.data.lights.new(nm, typ); ld.energy = en; lo = bpy.data.objects.new(nm, ld); scn.collection.objects.link(lo); lo.location = loc; lo.rotation_euler = rot
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam); scn.camera = cam
    cam.data.type = 'ORTHO'; return cam


report = {}
for row, items in ROWS.items():
    bpy.ops.wm.read_factory_settings(use_empty=True); scn = bpy.context.scene; cam = setup(scn)
    for i, (m, b, d, cell) in enumerate(items):
        O = Vector((i*2.6, 0, 0))
        bo = load(os.path.join(BD, "R4B_" + b, "R4B_" + b + ".glb")); place(bo, Matrix.Translation(O))
        n = Vector(DIRS[d]); P = Vector(cell)*G + n*H
        Finv = (Matrix.Translation(-P).inverted() @ n.rotation_difference(Vector((0, 0, 1))).to_matrix().to_4x4().inverted())   # canonical -> block
        mp = os.path.join(ROOT, "R4_Wheels", m, m + ".glb"); mo = load(mp)
        s = [o for o in mo if o.name.startswith("Socket_Mount")][0]; bpy.context.view_layer.update()
        sl = s.matrix_world.translation.copy(); dd = sl.normalized()
        C = Matrix.Translation((0, 0, GAP)) @ dd.rotation_difference(Vector((0, 0, -1))).to_matrix().to_4x4() @ Matrix.Translation(-sl)
        best = None
        for k in range(4):   # roll that keeps the module's forward (-Y) closest to the world forward
            X = Matrix.Translation(O) @ Finv @ Matrix.Rotation(k*math.pi/2, 4, 'Z') @ C
            f = (X.to_3x3() @ Vector((0, -1, 0))).dot(Vector((0, -1, 0))) + 0.5*(X.to_3x3() @ Vector((0, 0, 1))).dot(Vector((0, 0, 1)))
            if best is None or f > best[0]: best = (f, X, k)
        place(mo, best[1]); bpy.context.view_layer.update()
        ws = s.matrix_world.translation; wp = Matrix.Translation(O) @ P
        report[m] = {"block": "R4B_" + b, "port": f"{d}_{''.join(str(c) for c in cell)}", "roll_deg": 90*best[2],
                     "socket_to_port_gap_mm": round((ws - wp).dot(n)*1000, 3),
                     "tangential_mm": round(((ws - wp) - n*(ws - wp).dot(n)).length*1000, 3)}
    for o in bpy.data.objects:
        if o.type == 'MESH' and not o.name.startswith(("R4W_", "R4M_", "R4B_")): o.hide_render = True   # FX meshes off
    L = (len(items) - 1)*2.6; ctr = Vector((L/2 + 0.3, -0.3, 0.0))
    cam.data.ortho_scale = L*0.97 + 1.6
    for tag, dv in (("34front", (0.28, -1.0, 0.42)), ("side", (0, -1, 0.12))):
        cam.location = ctr + Vector(dv).normalized()*20; cam.rotation_euler = (ctr - cam.location).to_track_quat('-Z', 'Y').to_euler()
        scn.render.filepath = os.path.join(OUT, f"wheels_on_blocks_{tag}.png"); bpy.ops.render.render(write_still=True)
json.dump(report, open(os.path.join(OUT, "wheels_showcase.json"), "w"), indent=1)
print("SHOWCASE_DONE")
