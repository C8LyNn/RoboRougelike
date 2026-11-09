# #1182 assembly test: example builds that combine the REAL earlier models (R4 blocks #963, weapons #939, movement #941,
# wheel variants #1018 - their exported GLBs) with the new R4C connectors, placed on the 0.55 m grid with Unity rotations.
# For every scene:
#  - placement: grid parts by cell + integer Unity rotation (incl. mirror), modules on a host port face via their own Socket_Mount;
#  - logic: port faces of all parts in world cells -> mated pairs (opposite faces, kind rule: modules only on full ports),
#           cell conflicts, face-adjacent cells WITHOUT a mated port pair = "adjacent, not connected" (triforcing);
#  - geometry: BVH overlap of every pair of parts (real meshes), flush contact of every mated pair (30 mm gap, both faces);
#  - graph: connectivity to the anchor part, bridges (single-point links) and critical parts (single points of failure);
#  - markers: green = mated port pair, amber = free connector / module port, red = adjacent but NOT connected;
#  - renders (3/4 front, side, top) + scene GLB (for the HTML viewer) + .blend + <scene>_assembly.json.
# usage: blender -b --factory-startup --python assemble.py -- <wynik dir> <out dir> [scene,scene]
import bpy, json, os, sys, math, time, hashlib
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

G, H, GAP = 0.55, 0.26, 0.030
a_ = sys.argv[sys.argv.index("--") + 1:]; ROOT, OUT = a_[0], a_[1]; ONLY = a_[2].split(",") if len(a_) > 2 else None
CD = os.path.join(ROOT, "R4_Connectors1182")
DIRS_U = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 1, 0), "down": (0, -1, 0), "forward": (0, 0, 1), "back": (0, 0, -1)}
NAME_OF = {v: k for k, v in DIRS_U.items()}
SEARCH = ["R4_Blocks", "R4_Connectors1182", "R4_Weapons_938_port", "R4_Movement_940_port", "R4_Wheels"]


def Bv(u): return Vector((u[0], -u[2], u[1]))
A = Matrix(((1, 0, 0), (0, 0, -1), (0, 1, 0)))                        # Unity -> Blender (Bv)
def RB(Ru): return A @ Ru @ A.inverted()
def vadd(a, b): return tuple(int(a[i] + b[i]) for i in range(3))
def mul(R, v): return tuple(int(round(sum(R[i][j]*v[j] for j in range(3)))) for i in range(3))
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()


def rot(x_to, z_to, mirror_x=False):
    """integer Unity rotation from the images of +X and +Z (y = z x x keeps det +1); mirror_x prepends scale(-1,1,1) in world."""
    x, z = Vector(x_to), Vector(z_to); y = z.cross(x)
    R = Matrix((x, y, z)).transposed()
    assert abs(R.determinant() - 1) < 1e-6, (x_to, z_to)
    if mirror_x: R = Matrix(((-1, 0, 0), (0, 1, 0), (0, 0, 1))) @ R
    return R
I3 = Matrix.Identity(3)
UP = rot((1, 0, 0), (0, 1, 0))                                       # native +Z (forward) -> +Y (up): vertical masts

# R4 block sockets (from Tools/ModelFactory/R4Blocks/integration/R4_BLOCKS_UNITY.json, PR #963) - only blocks used here
BLOCKS = {
    "R4B_Cube": ([(0, 0, 0)], [((0, 0, 0), d, "full") for d in DIRS_U]),
    "R4B_Wedge": ([(0, 0, 0)], [((0, 0, 0), "down", "full"), ((0, 0, 0), "back", "full"), ((0, 0, 0), "left", "partial"), ((0, 0, 0), "right", "partial")]),
    "R4B_Beam3": ([(0, 0, 0), (1, 0, 0), (2, 0, 0)], [((0, 0, 0), "left", "full"), ((2, 0, 0), "right", "full")] +
                  [((i, 0, 0), d, "full") for i in range(3) for d in ("up", "down", "forward", "back")]),
}


def compatible(a, b):
    if "module" in (a, b): return ({a, b} - {"module"}) <= {"full", "r4_full"} and a != b     # modules need a FULL port
    return True                                                                                   # structural links: any port pair


# ------------------------------------------------------------------------------------------------ scenes (Unity cells)
def chassis(ox, parts, mods, tag):
    for i in (-1, 0, 1):
        for k in (-1, 0, 1): parts.append((f"{tag}_cube_{i}_{k}", "R4B_Cube", (ox + i, 0, k), I3))
    for i in (-1, 1):
        for k in (-1, 1): mods.append((f"{tag}_wheel_{i}_{k}", "R4M_Wheel", (ox + i, 0, k), "down", 0))
    mods.append((f"{tag}_gun", "R4W_Basic", (ox, 0, 0), "up", 0))


def sc_two_robots():
    P, Mo = [], []
    chassis(-3, P, Mo, "A")                                            # robot A: plain armour bolted to the chassis
    for i in (-1, 0, 1): P.append((f"A_wedge_{i}", "R4B_Wedge", (-3 + i, 0, 2), I3))
    chassis(3, P, Mo, "B")                                             # robot B: spaced armour on two ROD_SHORT
    for i in (-1, 1): P.append((f"B_rod_{i}", "R4C_ROD_SHORT", (3 + i, 0, 2), I3))
    for i in (-1, 0, 1): P.append((f"B_armour_{i}", "R4B_Cube", (3 + i, 0, 4), I3)); P.append((f"B_wedge_{i}", "R4B_Wedge", (3 + i, 0, 5), I3))
    P.append(("B_filler_cube", "R4B_Cube", (3, 0, 2), I3))            # sits BETWEEN the rods: touches them, must not connect
    return {"parts": P, "modules": Mo, "anchors": {"robot_A": "A_cube_0_0", "robot_B": "B_cube_0_0"},
            "note": "A: wedge armour directly on the chassis. B: armour wall 2 cells out on two rods (redundant); the filler cube is adjacent to both rod nodes but rods have no side ports."}


def sc_long_mast():
    P = [("base", "R4B_Beam3", (-1, 0, 0), I3), ("mast", "R4C_STRUT_LONG", (0, 1, 0), UP), ("nose_wedge", "R4B_Wedge", (0, 1, 1), I3)]
    Mo = [("rail", "R4W_Railgun", (0, 5, 0), "up", 0)]
    return {"parts": P, "modules": Mo, "anchors": {"mast": "base"}, "note": "STRUT_LONG weapon mast on a single mount (bridge), railgun on the top node, wedge armour on the bottom node."}


def sc_braced_mast():
    brace = rot((-1, 0, 0), (0, 1, 0))                                 # native diag (+x+z) -> (-x+y): A (3,1,0) .. B (1,3,0)
    P = [("base", "R4B_Beam3", (0, 0, 0), I3), ("foot", "R4B_Cube", (3, 0, 0), I3), ("mast", "R4C_STRUT_STANDARD", (0, 1, 0), UP),
         ("brace", "R4C_ROD_DIAG_2D_LONG", (3, 1, 0), brace), ("wedge", "R4B_Wedge", (0, 1, 1), I3)]
    Mo = [("gun", "R4W_Shotgun", (0, 3, 0), "up", 0)]
    return {"parts": P, "modules": Mo, "anchors": {"frame": "base"}, "note": "STRUT_STANDARD mast braced by ROD_DIAG_2D_LONG (closed loop = redundant); brace passes next to the beam and mast without connecting."}


def sc_arc_outrigger():
    R = rot((0, -1, 0), (1, 0, 0))                                     # native A tangent +z -> +x, B below the chassis side
    P = [(f"chassis_{k}", "R4B_Cube", (0, 0, k), I3) for k in (-1, 0, 1)]
    P += [("arc_R", "R4C_STRUT_ARC", (1, 0, 0), R), ("arc_L_mirror", "R4C_STRUT_ARC", (-1, 0, 0), rot((0, -1, 0), (1, 0, 0), mirror_x=True))]
    Mo = [("side_wheel_R", "R4M_WheelM_Side_Offroad", (3, -2, 0), "right", 0), ("steer_wheel_L", "R4M_WheelM_Steer_Road", (-3, -2, 0), "down", 0),
          ("gun", "R4W_RocketLauncher", (0, 0, 0), "up", 0)]
    return {"parts": P, "modules": Mo, "anchors": {"chassis": "chassis_0"}, "note": "STRUT_ARC outriggers: right one native, left one MIRRORED (scale X -1); side-hub wheel and steering wheel on the arc end nodes."}


def sc_cross_deck():
    P = [("beam_back", "R4B_Beam3", (0, 0, 0), I3), ("beam_front", "R4B_Beam3", (0, 0, 2), I3), ("deck", "R4C_STRUT_CROSS", (0, 1, 0), I3),
         ("bumper", "R4C_ROD_FRAME", (0, 0, 3), I3)]
    Mo = [("gun_L", "R4W_BeamCutter", (0, 1, 2), "up", 0), ("gun_R", "R4W_BeamCutter", (2, 1, 2), "up", 0)]
    return {"parts": P, "modules": Mo, "anchors": {"frame": "beam_back"}, "note": "STRUT_CROSS deck ties two beams (4 redundant links); ROD_FRAME bumper cage on two front links; weapons on strut nodes."}


SCENES = {"two_robots": sc_two_robots, "long_mast": sc_long_mast, "braced_mast": sc_braced_mast, "arc_outrigger": sc_arc_outrigger, "cross_deck": sc_cross_deck}


# ------------------------------------------------------------------------------------------------ helpers
def glb(model):
    for d in SEARCH:
        p = os.path.join(ROOT, d, model, model + ".glb")
        if os.path.exists(p): return p
    raise FileNotFoundError(model)


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def load_under(model, uid):
    before = set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=glb(model))
    new = [o for o in bpy.data.objects if o not in before]
    E = bpy.data.objects.new("P_" + uid, None); bpy.context.scene.collection.objects.link(E)
    for o in new:
        if o.parent is None: o.parent = E
    return E, new


def solid(n): return n.startswith(("R4W_", "R4M_", "R4B_", "R4C_"))


def bvh_of(objs):
    dg = bpy.context.evaluated_depsgraph_get(); V, P = [], []
    for o in objs:
        if o.type != 'MESH' or not solid(o.name): continue
        e = o.evaluated_get(dg); me = e.to_mesh(); mw = e.matrix_world
        b = len(V); V.extend(mw @ v.co for v in me.vertices); P.extend([b + i for i in p.vertices] for p in me.polygons); e.to_mesh_clear()
    return BVHTree.FromPolygons(V, P) if P else None, len(P)


def marker(name, loc, col, mats, shape="sphere"):
    if col not in mats:
        m = bpy.data.materials.new("MK_" + col); m.use_nodes = True; b = m.node_tree.nodes["Principled BSDF"]
        rgb = {"green": (0.0, 0.55, 0.06, 1), "amber": (0.95, 0.38, 0.0, 1), "red": (0.85, 0.0, 0.0, 1)}[col]
        b.inputs["Base Color"].default_value = rgb; b.inputs["Emission Color"].default_value = rgb; b.inputs["Emission Strength"].default_value = 0.6
        mats[col] = m
    if shape == "sphere": bpy.ops.mesh.primitive_uv_sphere_add(radius=0.045, segments=16, ring_count=8, location=loc)
    else: bpy.ops.mesh.primitive_cube_add(size=0.07, location=loc)
    o = bpy.context.active_object; o.name = name; o.data.materials.append(mats[col]); return o


def setup_render(scn):
    scn.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
    scn.render.resolution_x, scn.render.resolution_y = 1400, 1000
    w = bpy.data.worlds.new("W"); scn.world = w; w.use_nodes = True
    bg = w.node_tree.nodes.get("Background"); bg.inputs[0].default_value = (0.20, 0.21, 0.23, 1); bg.inputs[1].default_value = 0.9
    scn.view_settings.view_transform = 'AgX' if 'AgX' in [i.identifier for i in scn.view_settings.bl_rna.properties['view_transform'].enum_items] else 'Filmic'
    for (nm, typ, en, loc, r) in (("Key", 'SUN', 3.2, (2, -2, 3), (math.radians(50), 0, math.radians(35))), ("Fill", 'SUN', 1.2, (-3, -1.5, 1.5), (math.radians(70), 0, math.radians(-65))),
                                  ("Rim", 'SUN', 1.4, (0.5, 3.5, 2.2), (math.radians(-60), 0, math.radians(180)))):
        ld = bpy.data.lights.new(nm, typ); ld.energy = en; lo = bpy.data.objects.new(nm, ld); scn.collection.objects.link(lo); lo.location = loc; lo.rotation_euler = r
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam); scn.camera = cam; cam.data.lens = 50
    return cam


def shoot(scn, cam, target, dist, d, path):
    cam.location = Vector(target) + Vector(d).normalized()*dist
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scn.render.filepath = path; bpy.ops.render.render(write_still=True)


# ------------------------------------------------------------------------------------------------ one scene
def run(name, spec):
    t0 = time.time(); reset(); scn = bpy.context.scene; scn.frame_set(1)
    parts, faces, cells = {}, {}, {}           # uid -> info ; (cell, dir) -> [(uid, kind, port_id)] ; cell -> [uid]
    errors = []

    def claim(uid, c, role):
        cells.setdefault(c, []).append((uid, role))

    for uid, model, at, Ru in spec["parts"]:
        E, objs = load_under(model, uid); RBm = RB(Ru)
        E.matrix_world = Matrix.Translation(Bv(at)*G) @ RBm.to_4x4()
        info = {"model": model, "type": "connector" if model.startswith("R4C_") else "block", "at": list(at),
                "rot_unity": [[int(round(Ru[i][j])) for j in range(3)] for i in range(3)], "mirrored": Ru.determinant() < 0,
                "glb_sha256": sha(glb(model)), "objs": objs, "ports": []}
        if model.startswith("R4C_"):
            q = json.load(open(os.path.join(CD, model, model + "_QA.json"), encoding="utf-8"))
            for c in q["occupied_cells_unity"]: claim(uid, vadd(at, mul(Ru, c["cell"])), c["role"])
            plist = [(tuple(p["cell"]), p["dir"], p["kind"], p["id"]) for p in q["ports"]]
        else:
            bc, bp = BLOCKS[model]
            for c in bc: claim(uid, vadd(at, mul(Ru, c)), "block")
            plist = [(c, d, k, f"{d}@{c[0]},{c[1]},{c[2]}") for (c, d, k) in bp]
        for (c, d, k, pid) in plist:
            wc = vadd(at, mul(Ru, c)); wd = NAME_OF[mul(Ru, DIRS_U[d])]
            faces.setdefault((wc, wd), []).append((uid, k, pid)); info["ports"].append({"id": pid, "cell": list(wc), "dir": wd, "kind": k})
        parts[uid] = info
    bpy.context.view_layer.update()
    for uid, model, host_cell, hdir, roll in spec["modules"]:
        E, objs = load_under(model, uid); bpy.context.view_layer.update()
        s = [o for o in objs if o.name.startswith("Socket_Mount")][0]; sl = s.matrix_world.translation.copy()
        n = Bv(DIRS_U[hdir]); P = Bv(host_cell)*G + n*H
        Ra = sl.normalized().rotation_difference(-n).to_matrix().to_4x4(); Rr = Matrix.Rotation(roll*math.pi/2, 4, n)
        E.matrix_world = Matrix.Translation(P + n*GAP) @ Rr @ Ra @ Matrix.Translation(-sl)
        mc = vadd(host_cell, DIRS_U[hdir]); od = NAME_OF[tuple(-x for x in DIRS_U[hdir])]
        claim(uid, mc, "module")
        faces.setdefault((mc, od), []).append((uid, "module", "Socket_Mount"))
        parts[uid] = {"model": model, "type": "module", "on": {"cell": list(host_cell), "dir": hdir}, "roll_deg": roll*90,
                      "glb_sha256": sha(glb(model)), "objs": objs, "ports": [{"id": "Socket_Mount", "cell": list(mc), "dir": od, "kind": "module"}]}
    bpy.context.view_layer.update()

    # ---- logic: cells, pairs, adjacency
    conflicts = [{"cell": list(c), "parts": [u for u, _ in v]} for c, v in cells.items() if len({u for u, _ in v}) > 1]
    owner = {c: v[0][0] for c, v in cells.items()}
    pairs, rejected, seen = [], [], set()
    for (c, d), lst in faces.items():
        oc = vadd(c, DIRS_U[d]); od = NAME_OF[tuple(-x for x in DIRS_U[d])]
        for (ua, ka, pa) in lst:
            for (ub, kb, pb) in faces.get((oc, od), []):
                if ua == ub: continue
                key = tuple(sorted([(ua, pa), (ub, pb)]))
                if key in seen: continue
                seen.add(key)
                rec = {"a": ua, "a_port": pa, "a_kind": ka, "b": ub, "b_port": pb, "b_kind": kb, "face_cell": list(c), "face_dir": d}
                (pairs if compatible(ka, kb) else rejected).append(rec)
    mated_faces = set()
    for p in pairs:
        c = tuple(p["face_cell"]); mated_faces.add((c, p["face_dir"])); mated_faces.add((vadd(c, DIRS_U[p["face_dir"]]), NAME_OF[tuple(-x for x in DIRS_U[p["face_dir"]])]))
    adjacent = []
    for c, u in owner.items():
        for d, du in DIRS_U.items():
            m = vadd(c, du)
            if m in owner and owner[m] != u and (c, d) not in mated_faces and (c < m):
                adjacent.append({"cell": list(c), "dir": d, "parts": [u, owner[m]], "roles": [cells[c][0][1], cells[m][0][1]]})
    # ---- geometry: pairwise overlaps (real meshes), contact of mated pairs
    trees = {}
    for uid, info in parts.items(): trees[uid] = bvh_of(info["objs"])[0]
    overlaps = []
    U = list(parts)
    for i in range(len(U)):
        for j in range(i + 1, len(U)):
            ta, tb = trees[U[i]], trees[U[j]]
            if ta and tb and ta.overlap(tb): overlaps.append([U[i], U[j], len(ta.overlap(tb))])
    for p in pairs:
        n = Bv(DIRS_U[p["face_dir"]]); c0 = Bv(p["face_cell"])*G + n*H
        u = Vector((1, 0, 0)) if abs(n.x) < 0.9 else Vector((0, 1, 0)); u = (u - n*u.dot(n)).normalized(); w = n.cross(u)
        partial = "partial" in (p["a_kind"], p["b_kind"])          # wedge side triangles: off-centre patch -> scan the whole face
        flushA = flushB = both = prot = miss = 0; N, hm = (11, 0.24) if partial else (7, 0.09)
        for i in range(N):
            for j in range(N):
                o = c0 + n*(GAP/2) + u*(-hm + 2*hm*i/(N - 1)) + w*(-hm + 2*hm*j/(N - 1))
                ha = trees[p["a"]].ray_cast(o, -n, 0.3); hb = trees[p["b"]].ray_cast(o, n, 0.3)
                if ha[0] is not None and ha[3] < GAP/2 - 0.0006: prot += 1
                if hb[0] is not None and hb[3] < GAP/2 - 0.0006: prot += 1
                if ha[0] is None or hb[0] is None: miss += 1; continue
                fa, fb = ha[3] < GAP/2 + 0.0006, hb[3] < GAP/2 + 0.0006; flushA += fa; flushB += fb; both += fa and fb
        ok = prot == 0 and (both >= 4 if partial else (miss == 0 and flushA >= 0.5*N*N and flushB >= 0.5*N*N))
        p["contact"] = {"samples": N*N, "patch_half_m": hm, "flush_a_pct": round(100*flushA/N/N, 1), "flush_b_pct": round(100*flushB/N/N, 1),
                        "flush_both": both, "protruding": prot, "missing": miss, "gap_m": GAP, "method": "partial: whole face, >=4 flush-both, no protrusion" if partial else "patch 0.18 m, no miss, >=50% flush each side", "PASS": ok}
    # ---- graph
    adj = {u: [] for u in parts}
    for k, p in enumerate(pairs): adj[p["a"]].append((p["b"], k)); adj[p["b"]].append((p["a"], k))
    def reach(src, skip_part=None, skip_link=None):
        seen_, st = {src}, [src]
        while st:
            x = st.pop()
            for y, k in adj[x]:
                if y == skip_part or y in seen_: continue
                if skip_link and frozenset((x, y)) == skip_link: continue
                seen_.add(y); st.append(y)
        return seen_
    graph = {}
    for gname, anc in spec["anchors"].items():
        comp = reach(anc)
        links = {frozenset((pairs[k]["a"], pairs[k]["b"])) for x in comp for _, k in adj[x]}
        bridges = []
        for L in links:
            lost = comp - reach(anc, skip_link=L)
            if lost: bridges.append({"link": sorted(L), "ports": sum(1 for p in pairs if frozenset((p["a"], p["b"])) == L), "parts_lost": sorted(lost)})
        crit = {}
        for x in comp - {anc}:
            lost = comp - {x} - reach(anc, skip_part=x)
            if lost: crit[x] = sorted(lost)
        graph[gname] = {"anchor": anc, "parts": len(comp), "members": sorted(comp), "single_point_links": bridges, "critical_parts": crit}
    connected = set().union(*[set(g["members"]) for g in graph.values()])
    floating = sorted(set(parts) - connected)
    unmated_modules = [u for u, i in parts.items() if i["type"] == "module" and not any(u in (p["a"], p["b"]) for p in pairs)]
    # ---- markers
    mats, mk = {}, []
    for k, p in enumerate(pairs):
        n = Bv(DIRS_U[p["face_dir"]]); mk.append(marker(f"MK_mated_{k:02d}", Bv(p["face_cell"])*G + n*(H + GAP/2), "green", mats))
    for uid, info in parts.items():
        if info["type"] == "block": continue
        for pt in info["ports"]:
            if (tuple(pt["cell"]), pt["dir"]) in mated_faces: continue
            n = Bv(DIRS_U[pt["dir"]]); mk.append(marker(f"MK_free_{uid}_{pt['dir']}_{'_'.join(map(str, pt['cell']))}", Bv(pt["cell"])*G + n*(H + 0.05), "amber", mats))
    for k, a in enumerate(adjacent):
        n = Bv(DIRS_U[a["dir"]]); mk.append(marker(f"MK_adjacent_{k:02d}", Bv(a["cell"])*G + n*(G/2), "red", mats, "cube"))
    # ---- outputs
    od = os.path.join(OUT, name); os.makedirs(od, exist_ok=True)
    allm = [o for i in parts.values() for o in i["objs"] if o.type == 'MESH' and solid(o.name)]
    pts = [o.matrix_world @ Vector(c) for o in allm for c in o.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)]); ctr = (lo + hi)/2; ext = max(hi - lo)
    cam = setup_render(scn); renders = []
    for tag, d in (("34front", (1.0, -1.3, 0.9)), ("side", (1.0, 0.0, 0.15)), ("top", (0.02, -0.25, 1.0)), ("34back", (-1.0, 1.2, 0.8))):
        p_ = os.path.join(od, f"ASM_{name}_{tag}.png"); shoot(scn, cam, ctr, ext*2.1 + 1.6, d, p_); renders.append(os.path.basename(p_))
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.data.objects:
        if o.type in ('MESH', 'EMPTY') and o.name not in ("Cam",): o.select_set(True)
    gp = os.path.join(od, f"ASM_{name}.glb")
    bpy.ops.export_scene.gltf(filepath=gp, export_format='GLB', use_selection=True, export_yup=True, export_apply=True, export_animations=False)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(od, f"ASM_{name}.blend"))
    contact_fail = [p for p in pairs if not p["contact"]["PASS"]]
    ok = not conflicts and not overlaps and not rejected and not contact_fail and not floating and not unmated_modules
    rep = {"scene": name, "note": spec["note"], "grid_m": G, "face_m": H, "gap_m": GAP, "unity": "UNITY_NOT_RUN",
           "parts": {u: {k: v for k, v in i.items() if k != "objs"} for u, i in parts.items()},
           "cells": {",".join(map(str, c)): v for c, v in sorted(cells.items())},
           "mated_pairs": pairs, "rejected_pairs": rejected, "adjacent_not_connected": adjacent, "cell_conflicts": conflicts,
           "mesh_overlaps": overlaps, "floating_parts": floating, "unmated_modules": unmated_modules, "graph": graph,
           "summary": {"parts": len(parts), "mated_pairs": len(pairs), "contact_fail": len(contact_fail), "rejected": len(rejected),
                       "adjacent_not_connected": len(adjacent), "overlaps": len(overlaps), "cell_conflicts": len(conflicts),
                       "floating": len(floating), "single_point_links": sum(len(g["single_point_links"]) for g in graph.values())},
           "renders": renders, "glb": os.path.basename(gp), "glb_sha256": sha(gp), "PASS": ok, "sec": round(time.time() - t0, 1)}
    json.dump(rep, open(os.path.join(od, f"ASM_{name}_assembly.json"), "w", encoding="utf-8"), indent=1)
    print("ASM", name, "PASS" if ok else "FAIL", rep["summary"], rep["sec"], flush=True)
    return rep


# ------------------------------------------------------------------------------------------------ rule checks (logic only)
def rule_checks():
    """negative tests of the port rule, no geometry: a module offered to a rod_mini port must be rejected; structural pairs accepted."""
    cases = [("module", "rod_mini", False), ("module", "r4_full", True), ("module", "full", True), ("module", "partial", False), ("module", "module", False),
             ("rod_mini", "full", True), ("rod_mini", "r4_full", True), ("rod_mini", "rod_mini", True), ("r4_full", "partial", True)]
    return [{"a": a, "b": b, "expected": e, "got": compatible(a, b), "PASS": compatible(a, b) == e} for a, b, e in cases]


os.makedirs(OUT, exist_ok=True)
allrep = {"rule_checks": rule_checks(), "scenes": {}}
for nm, fn in SCENES.items():
    if ONLY and nm not in ONLY: continue
    try: allrep["scenes"][nm] = {k: v for k, v in run(nm, fn()).items() if k in ("summary", "PASS", "graph", "note")}
    except Exception as ex:
        import traceback; traceback.print_exc(); allrep["scenes"][nm] = {"PASS": False, "error": repr(ex)}; print("ASM", nm, "ERROR", repr(ex), flush=True)
allrep["PASS"] = all(s["PASS"] for s in allrep["scenes"].values()) and all(r["PASS"] for r in allrep["rule_checks"])
json.dump(allrep, open(os.path.join(OUT, "assemblies_summary.json" if not ONLY else f"assemblies_summary_{'_'.join(ONLY)}.json"), "w", encoding="utf-8"), indent=1)
print("ASM_DONE", allrep["PASS"], flush=True)
