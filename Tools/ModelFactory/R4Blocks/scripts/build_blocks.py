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
from r4lib import M, box, cyl, tube, hull, Part, new_col

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
    ob, cpts, meta = fn(work, ID)
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
    cmask = contact_mask(meta["ports"])
    for k, v in {"block_kind": name, "description": desc, "grid_m": G, "block_m": 2*H, "cells": json.dumps(meta["cells"]), "contact_mask_local": cmask,
                 "unity_up": "+Y", "unity_forward": "+Z", "style": "R4 / R41 industrial", "port": "R4 Robocraft-style port"}.items(): root[k] = v
    # collider (convex): hull of the base shape or box over the cells
    cc = new_col(ID + "_Colliders")
    if cpts is None:
        xs = [c[0] for c in meta["cells"]]; ys = [c[1] for c in meta["cells"]]; zs = [c[2] for c in meta["cells"]]
        z1 = HT if name in ("HalfBlock", "Plate2x2") else max(zs)*G + H
        coll = hull("UCX_" + ID, corners(min(xs)*G - H, max(xs)*G + H, min(ys)*G - H, max(ys)*G + H, min(zs)*G - H, z1), M["Armor_Dark"], cc)
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
    bpy.ops.object.select_all(action='DESELECT'); coll.select_set(True); bpy.context.view_layer.objects.active = coll
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
