# RoboRougelike R4 building blocks - the basic construction set on the game grid (0.55 m cells, 0.52 m blocks).
# Shape set = the standard voxel-builder family (cube, wedge, tetra, inner corner, corner wedge, rounded edge/corner, half
# blocks, cylinder, beams, long wedge, plate, truss) in the R4 / R41 industrial style. Every full cell face is an R4 port
# (flush square contact face with recessed coupling, the same port as the R4 weapons and movement parts); triangular and
# half faces carry a smaller declared contact patch. Usage: blender -b --factory-startup --python build_blocks.py -- OUTDIR [names]
import sys, os, math, json, time, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
import r4lib as L
from r4lib import M, box, cyl, cone, revolve, tube, hull, Part, new_col

G, H = 0.55, 0.26          # grid pitch, block half-size (BlockSize 0.52)
HT = -0.015                # top of half-height blocks (half cell minus half gap)
BEV = 0.010
DIRS = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 0, 1), "down": (0, 0, -1), "forward": (0, -1, 0), "back": (0, 1, 0)}   # Blender axes
UNITY_BIT = {"right": 0, "left": 1, "up": 2, "down": 3, "forward": 4, "back": 5}     # R3MountingGrid.FaceDirections order (Unity right,left,up,down,forward,back)
OUT = sys.argv[sys.argv.index("--") + 1]
ONLY = sys.argv[sys.argv.index("--") + 2].split(",") if len(sys.argv) > sys.argv.index("--") + 2 else None


def frame(n):
    n = Vector(n).normalized()
    u = Vector((0, 0, 1)).cross(n) if abs(n.z) < 0.9 else Vector((1, 0, 0)).cross(n)
    u.normalize(); v = n.cross(u)
    return u, v, n


def fmat(c, n, u=None):
    if u is None: u, v, n = frame(n)
    else:
        n = Vector(n).normalized(); u = Vector(u).normalized(); v = n.cross(u)
    m = Matrix((u, v, n)).transposed().to_4x4(); m.translation = Vector(c)
    return m


def to_face(objs, m):
    for o in objs: o.data.transform(m)


def port_pattern(pt, c, n, s=1.0, u=None):
    """R4 port contact pattern: recessed coupling ring + centre socket + 4 locating holes, all below the face"""
    m = fmat(c, n, u); made = []
    made.append(tube("pp_groove", 0.092*s, 0.072*s, (0, 0, -0.004), (0, 0, 0.010), M["Armor_Dark"], pt.D, seg=24))
    made.append(cyl("pp_socket", 0.034*s, (0, 0, -0.006), (0, 0, 0.010), M["Armor_Dark"], pt.D, seg=20))
    for (x, y) in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        made.append(cyl("pp_hole", 0.013*max(s, 0.7), (0.150*s*x, 0.150*s*y, -0.007), (0.150*s*x, 0.150*s*y, 0.010), M["Armor_Dark"], pt.D, seg=12))
    made.append(tube("pp_ring", 0.0905*s, 0.0735*s, (0, 0, -0.0042), (0, 0, -0.0022), M["Steel_Light"], pt.Pc, seg=24))
    made.append(cyl("pp_core", 0.0325*s, (0, 0, -0.0062), (0, 0, -0.0040), M["Steel_Light"], pt.Pc, seg=20))
    to_face(made, m)


def seam_frame(pt, c, n, hw, hh, inset=0.034, u=None):
    """thin panel seam groove inset from the face edges (rectangular face, half sizes hw x hh)"""
    m = fmat(c, n, u); a, b = hw - inset, hh - inset; w = 0.0016; made = []
    for (x0, x1, y0, y1) in ((-a, a, b - w, b + w), (-a, a, -b - w, -b + w), (a - w, a + w, -b, b), (-a - w, -a + w, -b, b)):
        made.append(box("seam", x0, x1, y0, y1, -0.0015, 0.010, M["Armor_Dark"], pt.D))
    to_face(made, m)


def panel(pt, poly, n, inset=0.040):
    """recessed armour panel (same paintable gunmetal, framed by a dark seam) on a planar convex face"""
    n = Vector(n).normalized(); P = [Vector(p) for p in poly]; c = sum(P, Vector()) / len(P)
    def shrink(d): return [c + (p - c)*max(0.05, 1.0 - d/max((p - c).length, 1e-6)) for p in P]
    cut = shrink(inset); fill = shrink(inset + 0.004)
    hull("panel_cut", [tuple(p + n*t) for p in cut for t in (-0.0026, 0.010)], M["Armor_Dark"], pt.D)
    hull("panel", [tuple(p + n*t) for p in fill for t in (-0.0026, -0.0009)], M["Armor_Gunmetal"], pt.Pc)


def corners(x0, x1, y0, y1, z0, z1): return [(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]


def full_face_ports(pt, x0, x1, y0, y1, z0, z1, dirs):
    """ports + seams on the given full rectangular faces of an axis-aligned box spanning whole cells"""
    for d in dirs:
        n = Vector(DIRS[d]); ax = [i for i in range(3) if n[i] != 0][0]
        lo, hi = (x0, y0, z0), (x1, y1, z1)
        # one port per cell on that face
        rng = [[lo[i] + H + G*k for k in range(int(round((hi[i] - lo[i] - 2*H)/G)) + 1)] for i in range(3)]
        rng[ax] = [hi[ax] if n[ax] > 0 else lo[ax]]
        for x in rng[0]:
            for y in rng[1]:
                for z in rng[2]:
                    port_pattern(pt, (x, y, z), n)
        c = [(lo[i] + hi[i])/2 for i in range(3)]; c[ax] = hi[ax] if n[ax] > 0 else lo[ax]
        t = [i for i in range(3) if i != ax]; u, v, _ = frame(n)
        hw = abs(sum((hi[i] - lo[i])/2*abs(u[i]) for i in range(3))); hh = abs(sum((hi[i] - lo[i])/2*abs(v[i]) for i in range(3)))
        seam_frame(pt, c, n, hw, hh)


# ------------------------------------------------------------------------------------------------ shape builders
# each returns (mesh_object, collider_points or None (box), meta) ; meta: cells, ports [(cell, dir, kind, patch)], faces
def P_full(cell, d): return {"cell": cell, "dir": d, "kind": "full", "offset": [0, 0], "half": [0.47, 0.47]}
def P_part(cell, d, center, half): return {"cell": cell, "dir": d, "kind": "partial", "center": [round(float(x), 5) for x in center], "half": half}


def b_cube(work, nm):
    ob = hull(nm, corners(-H, H, -H, H, -H, H), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "c")
    full_face_ports(pt, -H, H, -H, H, -H, H, DIRS.keys()); pt.build(ob)
    return ob, None, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], d) for d in DIRS]}


def b_wedge(work, nm):
    pts = corners(-H, H, -H, H, -H, -H) + [(x, H, H) for x in (-H, H)]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "w")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down", "back"])
    s = 1/math.sqrt(2); panel(pt, [(-H, -H, -H), (H, -H, -H), (H, H, H), (-H, H, H)], (0, -s, s))
    for sx in (-1, 1):
        panel(pt, [(sx*H, -H, -H), (sx*H, H, -H), (sx*H, H, H)], (sx, 0, 0))
        port_pattern(pt, (sx*H, H*0.5, -H*0.5), (sx, 0, 0), 0.5)
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down"), P_full([0, 0, 0], "back")] +
                     [P_part([0, 0, 0], d, (s_*H, H*0.5, -H*0.5), [0.2, 0.2]) for (d, s_) in (("left", -1), ("right", 1))]}


TETRA = [(-H, H, -H), (H, H, -H), (-H, -H, -H), (-H, H, H)]


def b_tetra(work, nm):
    ob = hull(nm, TETRA, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "t")
    s = 1/math.sqrt(3); panel(pt, [TETRA[1], TETRA[2], TETRA[3]], (s, -s, s), inset=0.06)
    for (c, n) in (((-H*0.5, H*0.5, -H), (0, 0, -1)), ((-H*0.5, H, -H*0.5), (0, 1, 0)), ((-H, H*0.5, -H*0.5), (-1, 0, 0))):
        port_pattern(pt, c, n, 0.5)
    pt.build(ob)
    return ob, TETRA, {"cells": [[0, 0, 0]], "ports": [P_part([0, 0, 0], d, c_, [0.2, 0.2]) for (d, c_) in (("down", (-H*0.5, H*0.5, -H)), ("back", (-H*0.5, H, -H*0.5)), ("left", (-H, H*0.5, -H*0.5)))]}


INNER = [p for p in corners(-H, H, -H, H, -H, H) if p != (H, -H, H)]


def b_inner(work, nm):
    ob = hull(nm, INNER, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "i")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down", "back", "left"])
    s = 1/math.sqrt(3); panel(pt, [(-H, -H, H), (H, H, H), (H, -H, -H)], (s, -s, s), inset=0.06)
    for (c, n) in (((-H*0.5, H*0.5, H), (0, 0, 1)), ((-H*0.5, -H, -H*0.5), (0, -1, 0)), ((H, H*0.5, -H*0.5), (1, 0, 0))):
        port_pattern(pt, c, n, 0.5)
    pt.build(ob)
    return ob, INNER, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], d) for d in ("down", "back", "left")] +
                       [P_part([0, 0, 0], d, c_, [0.2, 0.2]) for (d, c_) in (("up", (-H*0.5, H*0.5, H)), ("forward", (-H*0.5, -H, -H*0.5)), ("right", (H, H*0.5, -H*0.5)))]}


CWEDGE = corners(-H, H, -H, H, -H, -H) + [(-H, H, H)]


def b_cornerwedge(work, nm):
    ob = hull(nm, CWEDGE, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "cw")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down"])
    s = 1/math.sqrt(2)
    panel(pt, [(-H, -H, -H), (H, -H, -H), (-H, H, H)], Vector((0, -1, 1)).normalized(), inset=0.05)
    panel(pt, [(H, -H, -H), (H, H, -H), (-H, H, H)], Vector((1, 0, 1)).normalized(), inset=0.05)
    port_pattern(pt, (-H*0.5, H, -H*0.5), (0, 1, 0), 0.5); port_pattern(pt, (-H, H*0.5, -H*0.5), (-1, 0, 0), 0.5)
    pt.build(ob)
    return ob, CWEDGE, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down")] + [P_part([0, 0, 0], d, c_, [0.2, 0.2]) for (d, c_) in (("back", (-H*0.5, H, -H*0.5)), ("left", (-H, H*0.5, -H*0.5)))]}


def arc(n=12):
    return [(H - 2*H*math.cos(math.radians(90*i/n)), -H + 2*H*math.sin(math.radians(90*i/n))) for i in range(n + 1)]


def b_roundedge(work, nm):
    pts = [(x, y, z) for x in (-H, H) for (y, z) in arc(16)] + [(x, H, -H) for x in (-H, H)]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "re")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down", "back"])
    for sx in (-1, 1):
        port_pattern(pt, (sx*H, H*0.45, -H*0.45), (sx, 0, 0), 0.5)
    for k in range(1, 4):   # seam rings along the curved face
        a = math.radians(22.5*k); y = H - 2*H*math.cos(a); z = -H + 2*H*math.sin(a); nn = Vector((0, -math.cos(a), math.sin(a)))
        box("arc_seam", -H + 0.03, H - 0.03, -0.0016, 0.0016, -0.0015, 0.010, M["Armor_Dark"], pt.D).data.transform(fmat((0, y, z), nn, (1, 0, 0)))
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down"), P_full([0, 0, 0], "back")] + [P_part([0, 0, 0], d, (s_*H, H*0.45, -H*0.45), [0.2, 0.2]) for (d, s_) in (("left", -1), ("right", 1))]}


def b_roundcorner(work, nm):
    pts = []
    for i in range(9):
        for j in range(9):
            th, ph = math.radians(90*i/8), math.radians(90*j/8)
            pts.append((-H + 2*H*math.sin(th)*math.cos(ph), H - 2*H*math.sin(th)*math.sin(ph), -H + 2*H*math.cos(th)))
    pts.append((-H, H, -H))
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "rc")
    for (c, n) in (((-H*0.55, H*0.55, -H), (0, 0, -1)), ((-H*0.55, H, -H*0.55), (0, 1, 0)), ((-H, H*0.55, -H*0.55), (-1, 0, 0))):
        port_pattern(pt, c, n, 0.5)
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_part([0, 0, 0], d, c_, [0.2, 0.2]) for (d, c_) in (("down", (-H*0.55, H*0.55, -H)), ("back", (-H*0.55, H, -H*0.55)), ("left", (-H, H*0.55, -H*0.55)))]}


def b_half(work, nm):
    ob = hull(nm, corners(-H, H, -H, H, -H, HT), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "h")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down"])
    port_pattern(pt, (0, 0, HT), (0, 0, 1)); seam_frame(pt, (0, 0, HT), (0, 0, 1), H, H)
    for d in ("right", "left", "forward", "back"):
        n = Vector(DIRS[d]); port_pattern(pt, tuple(n*H + Vector((0, 0, (-H + HT)/2))), n, 0.5)
    pt.build(ob)
    return ob, None, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down")] + [P_part([0, 0, 0], d, tuple(Vector(DIRS[d])*H + Vector((0, 0, (-H + HT)/2))), [0.45, 0.22]) for d in ("right", "left", "forward", "back")],
                      "note": "top face at -0.015 m (half cell); stack-on-top is a half-cell offset, not a grid port"}


def b_halfwedge(work, nm):
    pts = corners(-H, H, -H, H, -H, -H) + [(x, H, HT) for x in (-H, H)]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "hw")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down"])
    nn = Vector((0, -(HT + H), 2*H)).normalized(); panel(pt, [(-H, -H, -H), (H, -H, -H), (H, H, HT), (-H, H, HT)], nn)
    port_pattern(pt, (0, H, (-H + HT)/2), (0, 1, 0), 0.5)
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down"), P_part([0, 0, 0], "back", (0, H, (-H + HT)/2), [0.45, 0.22])]}


def b_cylinder(work, nm):
    ob = cyl(nm, H, (0, 0, -H), (0, 0, H), M["Armor_Gunmetal"], work, seg=48, bevel=BEV); pt = Part(work, "cy")
    for s in (-1, 1): port_pattern(pt, (0, 0, s*H), (0, 0, s))
    for z in (-0.13, 0.0, 0.13):
        tube("cyl_seam", H + 0.01, H - 0.0016, (0, 0, z - 0.0016), (0, 0, z + 0.0016), M["Armor_Dark"], pt.D, seg=48)
    for k in range(4):
        a = math.radians(45 + 90*k); c = (H*math.cos(a), H*math.sin(a), 0.065)
        panel(pt, [(c[0] - 0.05*math.sin(a), c[1] + 0.05*math.cos(a), 0.015), (c[0] + 0.05*math.sin(a), c[1] - 0.05*math.cos(a), 0.015),
                   (c[0] + 0.05*math.sin(a), c[1] - 0.05*math.cos(a), 0.115), (c[0] - 0.05*math.sin(a), c[1] + 0.05*math.cos(a), 0.115)], (math.cos(a), math.sin(a), 0), inset=0.008)
    pt.build(ob)
    return ob, "cyl", {"cells": [[0, 0, 0]], "ports": [P_part([0, 0, 0], d, tuple(Vector(DIRS[d])*H), [0.3, 0.3]) for d in ("up", "down")],
                       "note": "round end faces: contact patch limited to the inscribed square (half 0.30 cell)"}


def b_beam(n):
    def f(work, nm):
        x1 = (n - 1)*G + H
        ob = hull(nm, corners(-H, x1, -H, H, -H, H), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "b")
        full_face_ports(pt, -H, x1, -H, H, -H, H, DIRS.keys())
        for k in range(1, n):   # visual joint between cells (one rigid piece, bonded cells)
            for (d, n_) in ((-H, (0, -1, 0)), (H, (0, 1, 0))):
                box("cell_joint", k*G - H + 0.0, k*G - H + 0.0, 0, 0, 0, 0, M["Armor_Dark"], pt.D) if False else None
            for (a, b_, c_, d_, e, g) in ((k*G - G/2 - 0.0016, k*G - G/2 + 0.0016, -H - 0.01, H + 0.01, -H - 0.01, -H + 0.0025),
                                          (k*G - G/2 - 0.0016, k*G - G/2 + 0.0016, -H - 0.01, H + 0.01, H - 0.0025, H + 0.01)):
                box("joint_seam", a, b_, c_, d_, e, g, M["Armor_Dark"], pt.D)
        pt.build(ob)
        cells = [[k, 0, 0] for k in range(n)]
        ports = []
        for k in range(n):
            for d in DIRS:
                if (d == "left" and k > 0) or (d == "right" and k < n - 1): continue
                ports.append(P_full([k, 0, 0], d))
        return ob, None, {"cells": cells, "ports": ports, "bonds": [[[k, 0, 0], [k + 1, 0, 0]] for k in range(n - 1)]}
    return f


def b_longwedge(work, nm):
    y0 = -G - H
    pts = corners(-H, H, y0, H, -H, -H) + [(x, H, H) for x in (-H, H)]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "lw")
    full_face_ports(pt, -H, H, y0, H, -H, H, ["down", "back"])
    nn = Vector((0, -2*H, G + 2*H)).normalized(); panel(pt, [(-H, y0, -H), (H, y0, -H), (H, H, H), (-H, H, H)], nn)
    for sx in (-1, 1):
        panel(pt, [(sx*H, y0, -H), (sx*H, H, -H), (sx*H, H, H)], (sx, 0, 0))
        port_pattern(pt, (sx*H, H - 0.18, -H + 0.16), (sx, 0, 0), 0.5)
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0], [0, -1, 0]], "ports": [P_full([0, 0, 0], "down"), P_full([0, -1, 0], "down"), P_full([0, 0, 0], "back")] +
                     [P_part([0, 0, 0], d, (s_*H, H - 0.18, -H + 0.16), [0.2, 0.2]) for (d, s_) in (("left", -1), ("right", 1))], "bonds": [[[0, 0, 0], [0, -1, 0]]]}


def b_plate(work, nm):
    x1, y0 = G + H, -G - H
    ob = hull(nm, corners(-H, x1, y0, H, -H, HT), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "pl")
    full_face_ports(pt, -H, x1, y0, H, -H, H, ["down"])
    for cx in (0, G):
        for cy in (0, -G):
            port_pattern(pt, (cx, cy, HT), (0, 0, 1))
    seam_frame(pt, (G/2, -G/2, HT), (0, 0, 1), G/2 + H, G/2 + H)
    pt.build(ob)
    cells = [[0, 0, 0], [1, 0, 0], [0, -1, 0], [1, -1, 0]]
    return ob, None, {"cells": cells, "ports": [P_full(c, "down") for c in cells], "bonds": [[cells[0], cells[1]], [cells[0], cells[2]], [cells[1], cells[3]]],
                      "note": "half-height 2x2 plate; top face at -0.015 m"}


def b_truss(work, nm):
    t = 0.050
    ob = box(nm, -H, -H + t, -H, -H + t, -H, H, M["Armor_Gunmetal"], work, bevel=0.004); pt = Part(work, "tr")
    for sx in (-1, 1):
        for sy in (-1, 1):
            if (sx, sy) != (-1, -1): box("edge_z", sx*H - (t if sx > 0 else 0) if sx > 0 else -H, sx*H if sx > 0 else -H + t, sy*H - t if sy > 0 else -H, sy*H if sy > 0 else -H + t, -H, H, M["Armor_Gunmetal"], pt.U, bevel=0.004)
            box("edge_x", -H, H, (H - t) if sy > 0 else -H, H if sy > 0 else -H + t, (H - t) if sx > 0 else -H, H if sx > 0 else -H + t, M["Armor_Gunmetal"], pt.U, bevel=0.004)
            box("edge_y", (H - t) if sx > 0 else -H, H if sx > 0 else -H + t, -H, H, (H - t) if sy > 0 else -H, H if sy > 0 else -H + t, M["Armor_Gunmetal"], pt.U, bevel=0.004)
    for d, n in DIRS.items():
        n = Vector(n); u, v, _ = frame(n); m = fmat(tuple(n*H), n)
        made = [box("face_hub", -0.170, 0.170, -0.170, 0.170, -0.034, 0.0, M["Armor_Dark"], pt.U, bevel=0.003)]
        for a in (45, 135):
            ca, sa = math.cos(math.radians(a)), math.sin(math.radians(a)); Lh = H*math.sqrt(2) - 0.04
            made.append(hull("brace", [(ca*l - sa*w, sa*l + ca*w, z) for l in (-Lh, Lh) for w in (-0.016, 0.016) for z in (-0.030, 0.0)], M["Armor_Plate_Light"], pt.U))
        to_face(made, m); port_pattern(pt, tuple(n*H), n)
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                box("node", sx*H - 0.035*sx - 0.035, sx*H - 0.035*sx + 0.035, sy*H - 0.035*sy - 0.035, sy*H - 0.035*sy + 0.035, sz*H - 0.035*sz - 0.035, sz*H - 0.035*sz + 0.035, M["Armor_Dark"], pt.U, bevel=0.004)
    pt.build(ob)
    return ob, None, {"cells": [[0, 0, 0]], "ports": [P_part([0, 0, 0], d, tuple(Vector(DIRS[d])*H), [0.30, 0.30]) for d in DIRS], "note": "open lattice block (light, cheap): each face contacts through a 0.34 m square hub carrying the standard coupling; collider = full cube"}


BLOCKS = [("Cube", b_cube, "1x1x1 cube - all six faces are ports"), ("Wedge", b_wedge, "1x1x1 wedge (prism): slope from front-bottom to back-top"),
          ("Tetra", b_tetra, "1x1x1 tetra corner: three right-triangle faces + slanted face"), ("InnerCorner", b_inner, "1x1x1 inner corner: cube with one corner cut off"),
          ("CornerWedge", b_cornerwedge, "1x1x1 corner wedge (pyramid corner): square base, apex at back-left-top"),
          ("RoundEdge", b_roundedge, "1x1x1 rounded edge (quarter cylinder)"), ("RoundCorner", b_roundcorner, "1x1x1 rounded corner (sphere octant)"),
          ("HalfBlock", b_half, "1x1x1/2 half-height block"), ("HalfWedge", b_halfwedge, "1x1x1/2 low wedge"), ("Cylinder", b_cylinder, "1x1x1 round column (axis up)"),
          ("Beam2", b_beam(2), "2x1x1 beam - one rigid piece over two bonded cells"), ("Beam3", b_beam(3), "3x1x1 beam - one rigid piece over three bonded cells"),
          ("LongWedge", b_longwedge, "1x2x1 long wedge (gentle slope over two cells)"), ("Plate2x2", b_plate, "2x2x1/2 plate"),
          ("TrussCube", b_truss, "1x1x1 open lattice block")]

# ------------------------------------------------------------------------------------------------ set 2 (more shapes)
def tri_normal(a, b, c, outward_from=(0, 0, 0)):
    n = (Vector(b) - Vector(a)).cross(Vector(c) - Vector(a)).normalized()
    if n.dot((Vector(a) + Vector(b) + Vector(c))/3 - Vector(outward_from)) < 0: n = -n
    return n


def b_pyramid(work, nm):
    apex = (0, 0, H); base = [(-H, -H, -H), (H, -H, -H), (H, H, -H), (-H, H, -H)]
    pts = corners(-H, H, -H, H, -H, -H) + [apex]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "py")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down"])
    for i in range(4):
        a, b = base[i], base[(i + 1) % 4]; panel(pt, [a, b, apex], tri_normal(a, b, apex, (0, 0, -0.1)), inset=0.06)
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down")], "note": "square pyramid, apex at the top-face centre"}


def b_dome(work, nm):
    z0 = -H + 0.070
    ob = hull(nm, corners(-H, H, -H, H, -H, z0), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "dm")
    prof = [(0, z0 - 0.010)] + [((H - 0.012)*math.cos(math.radians(90*i/16)), z0 + (H - 0.012 - z0)*math.sin(math.radians(90*i/16))) for i in range(16)] + [(0, H - 0.012)]
    revolve("dome", prof, M["Armor_Gunmetal"], pt.U, 48, local=True)
    for zr in (0.45, 0.80):   # two seam rings on the shell
        t = math.asin(zr); r = (H - 0.012)*math.cos(t); z = z0 + (H - 0.012 - z0)*zr
        tube("dome_seam", r + 0.012, r - 0.0035, (0, 0, z - 0.0016), (0, 0, z + 0.0016), M["Armor_Dark"], pt.D, seg=48)
    cyl("dome_cap", 0.040, (0, 0, H - 0.020), (0, 0, H - 0.006), M["Steel_Light"], pt.Pc, seg=24)
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down"])
    pt.build(ob)
    return ob, [(x, y, z) for x in (-H, H) for y in (-H, H) for z in (-H, z0)] + [(0, 0, H - 0.006)] + [(H*math.cos(a), H*math.sin(a), 0.05) for a in [k*math.pi/4 for k in range(8)]], \
        {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down")], "note": "low base plate + elliptic dome (sensor / cockpit cap)"}


def b_cone(work, nm):
    ob = cyl(nm, H, (0, 0, -H), (0, 0, -H + 0.050), M["Armor_Gunmetal"], work, seg=48, bevel=0.006); pt = Part(work, "cn")
    cone("cone", H - 0.012, 0.032, (0, 0, -H + 0.049), (0, 0, H - 0.012), M["Armor_Gunmetal"], pt.U, seg=48)
    cyl("cone_tip", 0.034, (0, 0, H - 0.030), (0, 0, H), M["Steel_Light"], pt.U, seg=24, bevel=0.004)
    tube("cone_seam", H + 0.01, H - 0.0035, (0, 0, -H + 0.049 - 0.0016), (0, 0, -H + 0.049 + 0.0016), M["Armor_Dark"], pt.D, seg=48)
    port_pattern(pt, (0, 0, -H), (0, 0, -1))
    pt.build(ob)
    return ob, [(H*math.cos(a), H*math.sin(a), z) for a in [k*math.pi/8 for k in range(16)] for z in (-H, -H + 0.05)] + [(0, 0, H)], \
        {"cells": [[0, 0, 0]], "ports": [P_part([0, 0, 0], "down", (0, 0, -H), [0.3, 0.3])], "note": "nose cone; round base: contact patch limited to the inscribed square (half 0.30 cell)"}


def ngon(n, r, rot=0.0): return [(r*math.cos(rot + 2*math.pi*i/n), r*math.sin(rot + 2*math.pi*i/n)) for i in range(n)]


def b_hex(work, nm):
    P = ngon(6, H); pts = [(x, y, z) for (x, y) in P for z in (-H, H)]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "hx")
    for sz in (-1, 1): port_pattern(pt, (0, 0, sz*H), (0, 0, sz), 0.8)
    for i in range(6):
        a, b = P[i], P[(i + 1) % 6]; n = Vector(((a[0] + b[0])/2, (a[1] + b[1])/2, 0)).normalized()
        panel(pt, [(a[0], a[1], -H), (b[0], b[1], -H), (b[0], b[1], H), (a[0], a[1], H)], n, inset=0.035)
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_part([0, 0, 0], d, tuple(Vector(DIRS[d])*H), [0.3, 0.3]) for d in ("up", "down")],
                     "note": "hexagonal column (vertex on +-X, flats 0.225 m from the axis); ports on the end faces only"}


def b_oct(work, nm):
    c = 0.060; P = [(H, -H + c), (H, H - c), (H - c, H), (-H + c, H), (-H, H - c), (-H, -H + c), (-H + c, -H), (H - c, -H)]
    pts = [(x, y, z) for (x, y) in P for z in (-H, H)]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "oc")
    for sz in (-1, 1): port_pattern(pt, (0, 0, sz*H), (0, 0, sz)); seam_frame(pt, (0, 0, sz*H), (0, 0, sz), H - c*0.6, H - c*0.6)
    for d in ("right", "left", "forward", "back"):
        n = Vector(DIRS[d]); port_pattern(pt, tuple(n*H), n, 0.7)
    for i in (1, 3, 5, 7):
        a, b = P[i], P[(i + 1) % 8]; n = Vector(((a[0] + b[0])/2, (a[1] + b[1])/2, 0)).normalized()
        box("chamfer_seam", -H + 0.04, H - 0.04, -0.0016, 0.0016, -0.0015, 0.010, M["Armor_Dark"], pt.D).data.transform(fmat(((a[0] + b[0])/2, (a[1] + b[1])/2, 0), n, (0, 0, 1)))
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], d) for d in ("up", "down")] + [P_part([0, 0, 0], d, tuple(Vector(DIRS[d])*H), [0.33, 0.45]) for d in ("right", "left", "forward", "back")],
                     "note": "octagonal column (0.06 m chamfered vertical edges); side faces 0.40 m wide carry a reduced patch"}


def b_roundtop(work, nm):
    prof = [(-H, -H), (H, -H)] + [(H*math.cos(math.radians(180*i/20)), H*math.sin(math.radians(180*i/20))) for i in range(21)]
    pts = [(x, y, z) for x in (-H, H) for (y, z) in prof]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "rt")
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down"])
    for sx in (-1, 1): port_pattern(pt, (sx*H, 0, -0.05), (sx, 0, 0), 0.8)
    for sy in (-1, 1): port_pattern(pt, (0, sy*H, -0.13), (0, sy, 0), 0.45)
    for k in (1, 2, 3, 4, 5):
        a = math.radians(30*k); nn = Vector((0, math.cos(a), math.sin(a)))
        box("arc_seam", -H + 0.03, H - 0.03, -0.0016, 0.0016, -0.0015, 0.010, M["Armor_Dark"], pt.D).data.transform(fmat((0, H*math.cos(a), H*math.sin(a)), nn, (1, 0, 0)))
    pt.build(ob)
    return ob, pts, {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down")] + [P_part([0, 0, 0], d, (s_*H, 0, -0.05), [0.3, 0.3]) for (d, s_) in (("left", -1), ("right", 1))] +
                     [P_part([0, 0, 0], d, (0, s_*H, -0.13), [0.45, 0.22]) for (d, s_) in (("forward", -1), ("back", 1))], "note": "half block + half cylinder top (axis X)"}


def b_arch(work, nm):
    R = 0.170
    ob = hull(nm, corners(-H, H, -H, H, -H, H), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "ar")
    cyl("arch_cut", R, (0, -H - 0.03, -H), (0, H + 0.03, -H), M["Armor_Gunmetal"], pt.D, seg=48)
    box("arch_cut_lo", -R, R, -H - 0.03, H + 0.03, -H - 0.05, -H, M["Armor_Gunmetal"], pt.D)
    full_face_ports(pt, -H, H, -H, H, -H, H, ["up", "left", "right"])
    for sy in (-1, 1): port_pattern(pt, (0, sy*H, 0.085), (0, sy, 0), 0.45)
    tube("arch_rim", R + 0.022, R + 0.0185, (0, -H - 0.01, -H), (0, -H + 0.0025, -H), M["Armor_Dark"], pt.D, seg=48)
    tube("arch_rim", R + 0.022, R + 0.0185, (0, H - 0.0025, -H), (0, H + 0.01, -H), M["Armor_Dark"], pt.D, seg=48)
    pt.build(ob)
    return ob, ("boxes", [(-H, -R, -H, H, -H, H), (R, H, -H, H, -H, H), (-R, R, -H, H, -H + R, H)]), \
        {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], d) for d in ("up", "left", "right")] + [P_part([0, 0, 0], d, (0, s_*H, 0.085), [0.45, 0.15]) for (d, s_) in (("forward", -1), ("back", 1))],
         "socket": ((0, 0, H), (0, 0, 0)), "note": "arch: semicircular tunnel r 0.17 m along Y under the block; mounts from the top (Socket_Mount on the up face)"}


def b_step(work, nm):
    ob = hull(nm, corners(-H, H, -H, H, -H, HT), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "st")
    box("step_upper", -H, H, 0.0, H, HT - 0.010, H, M["Armor_Gunmetal"], pt.U, bevel=BEV)
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down", "back"])
    for d in ("right", "left", "forward"):
        n = Vector(DIRS[d]); c = (n.x*H, n.y*H if d == "forward" else -0.13 + 0.0, (-H + HT)/2)
        if d != "forward": c = (n.x*H, -0.0, (-H + HT)/2)
        port_pattern(pt, c, n, 0.45)
    port_pattern(pt, (0, H/2, H), (0, 0, 1), 0.45)
    seam_frame(pt, (0, -H/2, HT), (0, 0, 1), H/2, H)
    pt.build(ob)
    return ob, ("boxes", [(-H, H, -H, H, -H, HT), (-H, H, 0.0, H, HT, H)]), \
        {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down"), P_full([0, 0, 0], "back")] +
         [P_part([0, 0, 0], d, (Vector(DIRS[d]).x*H, -H if d == "forward" else 0.0, (-H + HT)/2), [0.45, 0.22]) for d in ("right", "left", "forward")] +
         [P_part([0, 0, 0], "up", (0, H/2, H), [0.2, 0.45])], "note": "stair step: full-width lower half + upper half at the back; the tread at -0.015 m is not a grid port"}


def b_cove(work, nm):
    R = 0.400
    ob = hull(nm, corners(-H, H, -H, H, -H, H), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "cv")
    cyl("cove_cut", R, (-H - 0.03, -H, H), (H + 0.03, -H, H), M["Armor_Gunmetal"], pt.D, seg=64)
    full_face_ports(pt, -H, H, -H, H, -H, H, ["down", "back"])
    for sx in (-1, 1): port_pattern(pt, (sx*H, 0.15, -0.15), (sx, 0, 0), 0.5)
    for k in (1, 2, 3):
        a = math.radians(22.5*k); y = -H + R*math.sin(a); z = H - R*math.cos(a); nn = Vector((0, -math.sin(a), math.cos(a)))
        box("cove_seam", -H + 0.03, H - 0.03, -0.0016, 0.0016, -0.0015, 0.010, M["Armor_Dark"], pt.D).data.transform(fmat((0, y, z), nn, (1, 0, 0)))
    pt.build(ob)
    return ob, ("boxes", [(-H, H, -H + R*0.72, H, -H, H), (-H, H, -H, H, -H, H - R*0.72)]), \
        {"cells": [[0, 0, 0]], "ports": [P_full([0, 0, 0], "down"), P_full([0, 0, 0], "back")] + [P_part([0, 0, 0], d, (s_*H, 0.15, -0.15), [0.2, 0.2]) for (d, s_) in (("left", -1), ("right", 1))],
         "note": "inverted round edge (concave fillet r 0.40 m along X) - fills the inside of a rounded corner"}


def b_poly(cells, boxes, desc_note):
    """polycube: union of full-height boxes given in cell index ranges (i0, i1, j0, j1, k0, k1); a full R4 port on every exposed cell face"""
    def f(work, nm):
        def co(b): return (b[0]*G - H, b[1]*G + H, b[2]*G - H, b[3]*G + H, b[4]*G - H, b[5]*G + H)
        b0 = co(boxes[0]); ob = hull(nm, corners(*b0), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "pc")
        for b in boxes[1:]: box("poly_box", *co(b), M["Armor_Gunmetal"], pt.U, bevel=BEV)
        S = {tuple(c) for c in cells}; ports = []
        for c in cells:
            for d, n in DIRS.items():
                nb = (c[0] + n[0], c[1] + n[1], c[2] + n[2])
                if nb in S: continue
                ctr = tuple(Vector(c)*G + Vector(n)*H); port_pattern(pt, ctr, n); seam_frame(pt, ctr, n, H, H)
                ports.append(P_full(list(c), d))
        pt.build(ob)
        bonds = [[list(a), list(b)] for a in sorted(S) for b in sorted(S) if a < b and sum(abs(a[i] - b[i]) for i in range(3)) == 1]
        return ob, ("boxes", [co(b) for b in boxes]), {"cells": [list(c) for c in cells], "ports": ports, "bonds": bonds, "note": desc_note}
    return f


def b_plate12(work, nm):
    x1 = G + H
    ob = hull(nm, corners(-H, x1, -H, H, -H, HT), M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "p2")
    full_face_ports(pt, -H, x1, -H, H, -H, H, ["down"])
    for cx in (0, G): port_pattern(pt, (cx, 0, HT), (0, 0, 1))
    seam_frame(pt, (G/2, 0, HT), (0, 0, 1), G/2 + H, H)
    for sx, x in ((-1, -H), (1, x1)): port_pattern(pt, (x, 0, (-H + HT)/2), (sx, 0, 0), 0.45)
    pt.build(ob)
    cells = [[0, 0, 0], [1, 0, 0]]
    return ob, None, {"cells": cells, "ports": [P_full(c, "down") for c in cells] + [P_part([0, 0, 0], "left", (-H, 0, (-H + HT)/2), [0.45, 0.22]), P_part([1, 0, 0], "right", (G + H, 0, (-H + HT)/2), [0.45, 0.22])],
                      "bonds": [[cells[0], cells[1]]], "note": "half-height 1x2 plate; top face at -0.015 m"}


def b_widewedge(work, nm):
    x1 = G + H
    pts = corners(-H, x1, -H, H, -H, -H) + [(x, H, H) for x in (-H, x1)]
    ob = hull(nm, pts, M["Armor_Gunmetal"], work, bevel=BEV); pt = Part(work, "ww")
    full_face_ports(pt, -H, x1, -H, H, -H, H, ["down", "back"])
    s = 1/math.sqrt(2)
    for cx in (0, G): panel(pt, [(cx - H, -H, -H), (cx + H, -H, -H), (cx + H, H, H), (cx - H, H, H)], (0, -s, s))
    for sx, x in ((-1, -H), (1, x1)):
        panel(pt, [(x, -H, -H), (x, H, -H), (x, H, H)], (sx, 0, 0)); port_pattern(pt, (x, H*0.5, -H*0.5), (sx, 0, 0), 0.5)
    pt.build(ob)
    cells = [[0, 0, 0], [1, 0, 0]]
    return ob, pts, {"cells": cells, "ports": [P_full(c, d) for c in cells for d in ("down", "back")] + [P_part([0, 0, 0], "left", (-H, H*0.5, -H*0.5), [0.2, 0.2]), P_part([1, 0, 0], "right", (x1, H*0.5, -H*0.5), [0.2, 0.2])],
                     "bonds": [[cells[0], cells[1]]], "note": "2-wide wedge (one rigid piece over two bonded cells)"}


BLOCKS2 = [("Pyramid", b_pyramid, "1x1x1 square pyramid"), ("Dome", b_dome, "1x1x1 dome on a base plate"), ("Cone", b_cone, "1x1x1 nose cone (round base)"),
           ("HexColumn", b_hex, "1x1x1 hexagonal column (axis up)"), ("OctColumn", b_oct, "1x1x1 octagonal column (chamfered vertical edges)"),
           ("RoundTop", b_roundtop, "1x1x1 half block with a half-cylinder top"), ("Arch", b_arch, "1x1x1 arch (tunnel under the block)"),
           ("Step", b_step, "1x1x1 stair step"), ("CoveEdge", b_cove, "1x1x1 inverted round edge (concave fillet)"),
           ("CornerL3", b_poly([(0, 0, 0), (1, 0, 0), (0, -1, 0)], [(0, 1, 0, 0, 0, 0), (0, 0, -1, 0, 0, 0)], "L corner piece over three bonded cells"), "L-shaped corner 3 cells"),
           ("Tee4", b_poly([(-1, 0, 0), (0, 0, 0), (1, 0, 0), (0, -1, 0)], [(-1, 1, 0, 0, 0, 0), (0, 0, -1, 0, 0, 0)], "T junction over four bonded cells"), "T junction 4 cells"),
           ("Slab2x2", b_poly([(0, 0, 0), (1, 0, 0), (0, -1, 0), (1, -1, 0)], [(0, 1, -1, 0, 0, 0)], "2x2x1 full-height slab, one rigid piece"), "2x2x1 slab"),
           ("BigCube2", b_poly([(i, j, k) for i in (0, 1) for j in (0, -1) for k in (0, 1)], [(0, 1, -1, 0, 0, 1)], "2x2x2 large cube, one rigid piece over eight bonded cells"), "2x2x2 large cube"),
           ("Plate1x2", b_plate12, "1x2x1/2 plate"), ("WideWedge", b_widewedge, "2x1x1 wide wedge")]
BLOCKS = BLOCKS + BLOCKS2


# ------------------------------------------------------------------------------------------------ QA helpers
def mesh_report(o):
    bm = bmesh.new(); bm.from_mesh(o.data)
    nm_ = sum(1 for e in bm.edges if not e.is_manifold); bd = sum(1 for e in bm.edges if e.is_boundary)
    za = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    inc = sum(1 for e in bm.edges if e.is_manifold and len(e.link_faces) == 2 and not e.is_contiguous)
    tris = sum(len(f.verts) - 2 for f in bm.faces); bm.free()
    uv = len(o.data.uv_layers) > 0
    return {"object": o.name, "non_manifold_edges": nm_, "boundary_edges": bd, "zero_area_faces": za, "inconsistent_normals": inc, "triangles": tris, "uv": uv,
            "ok": nm_ == 0 and bd == 0 and za == 0 and inc == 0 and uv}


def verts(o): return [o.matrix_world @ v.co for v in o.data.vertices]


def patch_test(tree, cell, d, off, half, N=9):
    n = Vector(DIRS[d]); u, v, _ = frame(n)
    c = Vector(cell)*G + n*H; flush = prot = miss = rec = 0
    for i in range(N):
        for j in range(N):
            a = (off[0] - half[0]*0.85 + 2*half[0]*0.85*i/(N - 1))*G; b = (off[1] - half[1]*0.85 + 2*half[1]*0.85*j/(N - 1))*G
            p = c + u*a + v*b + n*0.2; hit = tree.ray_cast(p, -n, 0.5)
            if hit[0] is None: miss += 1; continue
            k = (hit[0] - c).dot(n)
            if k > 0.0005: prot += 1
            elif k > -0.0010: flush += 1
            else: rec += 1
    return {"flush_pct": round(100*flush/(N*N), 1), "recessed_pct": round(100*rec/(N*N), 1), "protruding": prot, "missing": miss}


def contact_mask(ports):
    """R3 ContactMask (local, Unity face order) for the anchor cell + full list per cell"""
    m = 0
    for p in ports:
        if p["cell"] == [0, 0, 0]: m |= 1 << UNITY_BIT[p["dir"]]
    return m


def setup_render(scn):
    scn.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
    scn.render.resolution_x, scn.render.resolution_y = 1000, 760; scn.render.film_transparent = False
    w = scn.world or bpy.data.worlds.new("W"); scn.world = w; w.use_nodes = True
    bg = w.node_tree.nodes.get("Background"); bg.inputs[0].default_value = (0.20, 0.21, 0.23, 1); bg.inputs[1].default_value = 0.9
    scn.view_settings.view_transform = 'AgX' if 'AgX' in [i.identifier for i in scn.view_settings.bl_rna.properties['view_transform'].enum_items] else 'Filmic'
    for (nm, typ, en, loc, rot) in (("Key", 'SUN', 3.2, (2, -2, 3), (math.radians(50), 0, math.radians(35))), ("Fill", 'AREA', 220.0, (-3, -1.5, 1.5), (math.radians(70), 0, math.radians(-65))),
                                    ("Rim", 'AREA', 260.0, (0.5, 3.5, 2.2), (math.radians(-60), 0, math.radians(180)))):
        ld = bpy.data.lights.new(nm, typ); ld.energy = en
        if typ == 'AREA': ld.size = 2.5
        lo = bpy.data.objects.new(nm, ld); scn.collection.objects.link(lo); lo.location = loc; lo.rotation_euler = rot
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam); scn.camera = cam; cam.data.lens = 60
    return cam


def shoot(scn, cam, target, dist, d, path):
    cam.location = Vector(target) + Vector(d).normalized()*dist
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scn.render.filepath = path; bpy.ops.render.render(write_still=True)


def export(root, objs, base):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=base + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=False, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE', add_leaf_bones=False, use_mesh_modifiers=True)
    bpy.ops.export_scene.gltf(filepath=base + ".glb", export_format='GLB', use_selection=True, export_yup=True, export_apply=True)


def reimport_check(base, ref):
    res = {}
    for ext, op in ((".fbx", lambda p: bpy.ops.import_scene.fbx(filepath=p)), (".glb", lambda p: bpy.ops.import_scene.gltf(filepath=p))):
        before = set(bpy.data.objects); op(base + ext); new = [o for o in bpy.data.objects if o not in before]
        ms = [o for o in new if o.type == 'MESH']
        if not ms: res[ext] = {"PASS": False, "why": "no mesh"}; continue
        bpy.context.view_layer.update()
        v = verts(ms[0]); lo = [min(p[i] for p in v) for i in range(3)]; hi = [max(p[i] for p in v) for i in range(3)]
        dmm = max(max(abs(lo[i] - ref[0][i]), abs(hi[i] - ref[1][i])) for i in range(3))*1000
        res[ext] = {"PASS": dmm < 0.05 and len(ms[0].data.vertices) > 0, "bounds_dev_mm": round(dmm, 4), "verts": len(ms[0].data.vertices),
                    "empties": sorted(o.name for o in new if o.type == 'EMPTY')}
        for o in new: bpy.data.objects.remove(o, do_unlink=True)
    return res


# ------------------------------------------------------------------------------------------------ main
summary = {}
for (name, fn, desc) in BLOCKS:
    if ONLY and name not in ONLY: continue
    t0 = time.time(); ID = "R4B_" + name; od = os.path.join(OUT, ID); os.makedirs(os.path.join(od, "renders"), exist_ok=True)
    L.reset_scene(); L.init_palette(); scn = bpy.context.scene
    work = new_col("_build_" + name)
    try:
        ob, cpts, meta = fn(work, ID)
    except Exception as ex:
        import traceback; traceback.print_exc(); summary[ID] = ("ERROR", [repr(ex)], 0); print("BLOCK", ID, "ERROR", repr(ex), flush=True); continue
    for p in meta["ports"]:
        if "center" in p:
            n_ = Vector(DIRS[p["dir"]]); u_, v_, _ = frame(n_); dc = Vector(p["center"]) - n_*H
            p["offset"] = [round(dc.dot(u_)/G, 4), round(dc.dot(v_)/G, 4)]
    L.shade_and_uv(ob)
    col = new_col(ID)
    for c in list(ob.users_collection): c.objects.unlink(ob)
    col.objects.link(ob)
    root = bpy.data.objects.new(ID + "_ROOT", None); col.objects.link(root); root.empty_display_size = 0.1
    ob.parent = root
    empties = [root]
    for p in meta["ports"]:
        n = Vector(DIRS[p["dir"]]); u, v, _ = frame(n)
        c = Vector(p["cell"])*G + n*H + (u*p["offset"][0] + v*p["offset"][1])*G
        e = bpy.data.objects.new(f"Port_{p['dir']}_{p['cell'][0]}{p['cell'][1]}{p['cell'][2]}" + ("" if p["kind"] == "full" else "_part"), None)
        col.objects.link(e); e.parent = root; e.location = c; e.empty_display_type = 'SINGLE_ARROW'; e.empty_display_size = 0.06
        e.rotation_euler = n.to_track_quat('Z', 'Y').to_euler(); e["kind"] = p["kind"]; e["patch_offset_cells"] = p["offset"]; e["patch_half_cells"] = p["half"]
        empties.append(e)
    sock = [p for p in meta["ports"] if p["dir"] == "down" and p["cell"] == [0, 0, 0]]
    s = bpy.data.objects.new("Socket_Mount", None); col.objects.link(s); s.parent = root; s.empty_display_type = 'SINGLE_ARROW'; s.empty_display_size = 0.08
    s.location = (0, 0, -H); s.rotation_euler = (math.pi, 0, 0); empties.append(s)
    if meta.get("socket"): s.location, s.rotation_euler = meta["socket"]
    cmask = contact_mask(meta["ports"])
    for k, v in {"block_kind": name, "description": desc, "grid_m": G, "block_m": 2*H, "cells": json.dumps(meta["cells"]), "contact_mask_local": cmask,
                 "unity_up": "+Y", "unity_forward": "+Z", "style": "R4 / R41 industrial", "port": "R4 Robocraft-style port"}.items(): root[k] = v
    # collider (convex): hull of the base shape or box over the cells
    cc = new_col(ID + "_Colliders")
    if cpts is None:
        xs = [c[0] for c in meta["cells"]]; ys = [c[1] for c in meta["cells"]]; zs = [c[2] for c in meta["cells"]]
        z1 = HT if name in ("HalfBlock", "Plate2x2", "Plate1x2") else max(zs)*G + H
        coll = hull("UCX_" + ID, corners(min(xs)*G - H, max(xs)*G + H, min(ys)*G - H, max(ys)*G + H, min(zs)*G - H, z1), M["Armor_Dark"], cc)
    elif isinstance(cpts, tuple) and cpts[0] == "boxes":
        colls = [hull(f"UCX_{ID}_{i:02d}", corners(*b), M["Armor_Dark"], cc) for i, b in enumerate(cpts[1])]; coll = colls[0]
    elif cpts == "cyl":
        coll = cyl("UCX_" + ID, H, (0, 0, -H), (0, 0, H), M["Armor_Dark"], cc, seg=16)
    else:
        coll = hull("UCX_" + ID, cpts, M["Armor_Dark"], cc)
    # ---- QA
    bpy.context.view_layer.update()
    mr = mesh_report(ob); V = verts(ob)
    lo = [min(p[i] for p in V) for i in range(3)]; hi = [max(p[i] for p in V) for i in range(3)]
    cx = [c[0] for c in meta["cells"]]; cy = [c[1] for c in meta["cells"]]; cz = [c[2] for c in meta["cells"]]
    lim_lo = [min(cx)*G - H, min(cy)*G - H, min(cz)*G - H]; lim_hi = [max(cx)*G + H, max(cy)*G + H, max(cz)*G + H]
    in_cells = all(lo[i] >= lim_lo[i] - 0.0005 and hi[i] <= lim_hi[i] + 0.0005 for i in range(3))
    bm = bmesh.new(); bm.from_mesh(ob.data); bm.transform(ob.matrix_world); tree = BVHTree.FromBMesh(bm); bm.free()
    ptest = []
    for p in meta["ports"]:
        r = patch_test(tree, p["cell"], p["dir"], p["offset"], p["half"])
        r.update({"cell": p["cell"], "dir": p["dir"], "kind": p["kind"]})
        r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65)
        ptest.append(r)
    mats = sorted({s_.material.name for s_ in ob.material_slots if s_.material})
    allowed = {"Armor_Dark", "Armor_Gunmetal", "Armor_Plate_Light", "Steel_Light", "TeamColor_Red", "Hazard_Yellow", "Energy_Hot"}
    base = os.path.join(od, ID)
    export(root, [root, ob] + empties[1:], base)
    bpy.ops.export_scene.gltf(filepath=base + "_colliders.glb", export_format='GLB', use_selection=False, export_yup=True) if False else None
    bpy.ops.object.select_all(action='DESELECT')
    for o_ in cc.objects: o_.select_set(True)
    bpy.context.view_layer.objects.active = coll
    bpy.ops.export_scene.gltf(filepath=base + "_colliders.glb", export_format='GLB', use_selection=True, export_yup=True)
    re_ = reimport_check(base, (lo, hi))
    failures = []
    if not mr["ok"]: failures.append("topology")
    if not in_cells: failures.append("geometry outside its grid cells")
    if not all(p["PASS"] for p in ptest): failures.append("port contact patch")
    if not set(mats) <= allowed: failures.append("palette")
    if not all(v["PASS"] for v in re_.values()): failures.append("export reimport")
    # renders
    for o in cc.objects: o.hide_render = True
    cam = setup_render(scn)
    ctr = Vector([(lo[i] + hi[i])/2 for i in range(3)]); ext = max(hi[i] - lo[i] for i in range(3))
    renders = []
    for tag, dvec in (("34front", (1.0, -1.25, 0.85)), ("34rear", (-1.0, 1.25, 0.85)), ("under", (0.8, -0.9, -0.9))):
        p_ = os.path.join(od, "renders", f"{ID}_{tag}.png"); shoot(scn, cam, ctr, ext*3.1 + 0.6, dvec, p_); renders.append(os.path.basename(p_))
    bpy.ops.wm.save_as_mainfile(filepath=base + ".blend")
    files = {}
    for f in sorted(os.listdir(od)):
        fp = os.path.join(od, f)
        if os.path.isfile(fp) and not f.endswith(".json"): files[f] = {"sha256": hashlib.sha256(open(fp, "rb").read()).hexdigest(), "bytes": os.path.getsize(fp)}
    qa = {"id": ID, "block_kind": name, "description": desc, "status": "BLENDER_PASS" if not failures else "HOLD", "failures": failures, "unity": "UNITY_NOT_RUN",
          "cells": meta["cells"], "bonds": meta.get("bonds", []), "note": meta.get("note"), "contact_mask_local_anchor": cmask,
          "contact_mask_bits": "R3MountingGrid.FaceDirections order: right, left, up, down, forward, back (Unity)",
          "ports": meta["ports"], "port_contact_test": ptest, "mesh": mr, "bounds_m": [[round(x, 4) for x in lo], [round(x, 4) for x in hi]],
          "inside_cells": in_cells, "materials": mats, "reimport": re_, "renders": renders, "files": files, "seconds": round(time.time() - t0, 1),
          "blender": bpy.app.version_string}
    json.dump(qa, open(base + "_QA.json", "w", encoding="utf-8"), indent=1)
    summary[ID] = (qa["status"], failures, mr["triangles"])
    print("BLOCK", ID, qa["status"], failures, mr["triangles"], flush=True)
json.dump(summary, open(os.path.join(OUT, "_summary.json" if not ONLY else "_summary_partial.json"), "w"), indent=1)
print("BLOCKS_DONE")
