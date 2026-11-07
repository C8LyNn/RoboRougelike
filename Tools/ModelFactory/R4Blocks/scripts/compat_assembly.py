# Compatibility test: a small vehicle assembled on the 0.55 m grid from the R4 blocks + the REAL exported R4 weapon and movement GLBs.
# Checks every port pairing (faces 0.03 m apart = two 0.52 blocks on a 0.55 grid, centres aligned) and that no two parts overlap.
import sys, os, math, json
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
OUT = sys.argv[sys.argv.index("--") + 1]
R = os.path.dirname(OUT.rstrip("\\/"))
G = 0.55
BLK = lambda n: os.path.join(OUT, f"R4B_{n}", f"R4B_{n}.glb")
WPN = lambda n: os.path.join(R, "R4_Weapons_938_port", f"R4W_{n}", f"R4W_{n}.glb")
MOV = lambda n: os.path.join(R, "R4_Movement_940_port", f"R4M_{n}", f"R4M_{n}.glb")
# (label, glb, cell (x,y,z) Blender grid, quarter turns about Z)
LAYOUT = [("wedge_L", BLK("Wedge"), (-1, -2, 0), 0), ("wedge_C", BLK("Wedge"), (0, -2, 0), 0), ("wedge_R", BLK("Wedge"), (1, -2, 0), 0),
          ("cube_L1", BLK("Cube"), (-1, -1, 0), 0), ("cube_C1", BLK("Cube"), (0, -1, 0), 0), ("cube_R1", BLK("Cube"), (1, -1, 0), 0),
          ("truss_L0", BLK("TrussCube"), (-1, 0, 0), 0), ("half_C0", BLK("Cube"), (0, 0, 0), 0), ("truss_R0", BLK("TrussCube"), (1, 0, 0), 0),
          ("beam_rear", BLK("Beam3"), (-1, 1, 0), 0),
          ("round_L", BLK("RoundEdge"), (-1, 1, 1), 2), ("cyl_C", BLK("Cylinder"), (0, 1, 1), 0), ("round_R", BLK("RoundEdge"), (1, 1, 1), 2),
          ("half_L", BLK("HalfBlock"), (-1, 0, 1), 0), ("halfw_C", BLK("HalfWedge"), (0, 0, 1), 0), ("half_R", BLK("HalfBlock"), (1, 0, 1), 0),
          ("rc_L", BLK("RoundCorner"), (-1, -1, 1), 0), ("rc_R", BLK("RoundCorner"), (1, -1, 1), 3),
          ("gun", WPN("Basic"), (0, -1, 1), 0),
          ("wheel_FL", MOV("Wheel"), (-1, -1, -1), 2), ("wheel_FR", MOV("Wheel"), (1, -1, -1), 0),
          ("wheel_RL", MOV("Wheel"), (-1, 1, -1), 2), ("wheel_RR", MOV("Wheel"), (1, 1, -1), 0),
          ("booster", MOV("Booster"), (0, 1, -1), 0)]
bpy.ops.wm.read_factory_settings(use_empty=True); scn = bpy.context.scene
parts = {}
for (lab, glb, cell, q) in LAYOUT:
    before = set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=glb); new = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in new if o.parent is None]
    holder = bpy.data.objects.new("PART_" + lab, None); scn.collection.objects.link(holder)
    for r_ in roots: r_.parent = holder
    holder.location = Vector(cell)*G; holder.rotation_euler = (0, 0, q*math.pi/2)
    parts[lab] = {"holder": holder, "objs": new, "glb": os.path.relpath(glb, R), "cell": cell, "turns": q}
scn.frame_set(1); bpy.context.view_layer.update()
def wm(o): return o.matrix_world
ports = []
for lab, p in parts.items():
    for o in p["objs"]:
        if o.type == 'EMPTY' and (o.name.split(".")[0].startswith("Port_") or o.name.split(".")[0].startswith("Socket_Mount")):
            n = (wm(o).to_3x3() @ Vector((0, 0, 1))).normalized()
            ports.append((lab, o.name.split(".")[0], wm(o).translation.copy(), n))
# block empties: +Z of the empty = outward normal; R4 weapon/movement sockets: single-arrow empties, outward = away from the cell centre
cent = {lab: Vector(p["cell"])*G for lab, p in parts.items()}
fixed = []
for (lab, nm, pos, n) in ports:
    rel = (pos - cent[lab])/G; own = cent[lab] + Vector([round(c) for c in rel])*G   # the port's own cell (multi-cell blocks)
    d = pos - own; ax = max(range(3), key=lambda i: abs(d[i])); nn = Vector((0, 0, 0)); nn[ax] = 1 if d[ax] > 0 else -1
    fixed.append((lab, nm, pos, nn))
pairs = []
for i, (la, na, pa, nA) in enumerate(fixed):
    for (lb, nb, pb, nB) in fixed[i + 1:]:
        if la == lb or nA.dot(nB) > -0.99: continue
        d = pb - pa; along = d.dot(nA); tang = (d - nA*along).length
        if abs(along - 0.03) < 0.01 and tang < 0.05:
            pairs.append({"a": f"{la}:{na}", "b": f"{lb}:{nb}", "gap_mm": round(along*1000, 3), "tangential_mm": round(tang*1000, 3),
                          "PASS": abs(along - 0.03) <= 0.0005 and tang <= 0.0005})
# every attached R4 weapon / movement module must sit on a block port
mods = [lab for lab in parts if lab.startswith(("gun", "wheel", "booster"))]
mod_ok = {m: any((pr["a"].startswith(m + ":") or pr["b"].startswith(m + ":")) and pr["PASS"] for pr in pairs) for m in mods}
# overlaps between different parts (rest pose)
def tree(objs):
    bm = bmesh.new()
    for o in objs:
        if o.type != 'MESH' or max(o.matrix_world.to_scale()) < 0.01: continue
        t = bmesh.new(); t.from_mesh(o.data); t.transform(o.matrix_world); me = bpy.data.meshes.new("t"); t.to_mesh(me); t.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    return BVHTree.FromBMesh(bm) if len(bm.verts) else None
trees = {lab: tree(p["objs"]) for lab, p in parts.items()}
labs = list(parts); overlaps = []
for i in range(len(labs)):
    for j in range(i + 1, len(labs)):
        a, b = trees[labs[i]], trees[labs[j]]
        if a and b and a.overlap(b): overlaps.append((labs[i], labs[j]))
res = {"layout": [{"part": lab, "glb": p["glb"], "cell": p["cell"], "quarter_turns_z": p["turns"]} for lab, p in parts.items()],
       "port_pairs": pairs, "pairs_pass": all(p["PASS"] for p in pairs), "n_pairs": len(pairs), "modules_on_ports": mod_ok, "overlaps": overlaps}
res["PASS"] = res["pairs_pass"] and all(mod_ok.values()) and not overlaps and len(pairs) > 0
json.dump(res, open(os.path.join(OUT, "compat_assembly.json"), "w"), indent=1)
# ---- hero render
scn.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
scn.render.resolution_x, scn.render.resolution_y = 1600, 1000
w = bpy.data.worlds.new("W"); scn.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.19, 0.2, 0.22, 1)
bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, -G - 0.65)); gp = bpy.context.object
m = bpy.data.materials.new("Ground"); m.use_nodes = True; m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.11, 0.115, 0.12, 1); gp.data.materials.append(m)
for (nm, typ, en, loc, rot) in (("Key", 'SUN', 3.5, (3, -3, 4), (math.radians(48), 0, math.radians(38))), ("Fill", 'AREA', 600.0, (-5, -2, 2.5), (math.radians(70), 0, math.radians(-65))),
                                ("Rim", 'AREA', 700.0, (1, 6, 3.5), (math.radians(-60), 0, math.radians(180)))):
    ld = bpy.data.lights.new(nm, typ); ld.energy = en
    if typ == 'AREA': ld.size = 4
    lo = bpy.data.objects.new(nm, ld); scn.collection.objects.link(lo); lo.location = loc; lo.rotation_euler = rot
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam); scn.camera = cam; cam.data.lens = 50
tgt = Vector((0, -0.1, -0.15))
for tag, d, dist in (("34front", (1.0, -1.3, 0.75), 5.6), ("side", (1, 0, 0.12), 5.6), ("34rear", (-1.0, 1.3, 0.8), 5.6)):
    cam.location = tgt + Vector(d).normalized()*dist; cam.rotation_euler = (tgt - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scn.render.filepath = os.path.join(OUT, "compat", f"compat_assembly_{tag}.png"); bpy.ops.render.render(write_still=True)
gp.hide_render = False
bpy.ops.object.select_all(action="DESELECT")
for lab, p in parts.items():
    for o in p["objs"]: o.select_set(True)
    p["holder"].select_set(True)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, "compat", "R4_Compat_Vehicle.glb"), export_format="GLB", use_selection=True, export_yup=True, export_animations=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "compat", "compat_assembly.blend"))
print("COMPAT_DONE", res["PASS"], len(pairs), overlaps)
