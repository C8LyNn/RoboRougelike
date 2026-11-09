# RoboRougelike #1182 - R4 connector set: light RODS and heavy STRUTS (spaced armour, rod/triforcing, weapon masts, frames).
# Built on the existing R4 standard, not a new one: 0.55 m grid, 0.52 m block (face 0.26 m from the cell centre), the R4
# port contact pattern of the R4 blocks / weapons / movement parts (same port_pattern / seam code as R4Blocks/build_blocks.py),
# R41 locked palette. A connector = NODES (cells with explicit ports) joined by MEMBERS (rod tubes / strut beams).
#  - ROD node  : small hub + stub + reduced R4 port plate (0.23 m, "rod_mini", block-to-block partial-patch class) only on
#                end faces opposite a member (a rod is a pure link: no side ports, junction hubs carry no ports);
#                structural only - weapons / movement (0.509 m plate) need a full port and are rejected there.
#  - STRUT node: block-size node (0.52 m) whose port faces are full R4 ports ("r4_full") - blocks AND modules mount there.
#  - Members never carry ports: a rod passing next to armour does not connect to it (triforcing / spaced armour).
# Authored in Unity cells (x right, y up, z forward); Blender = (x, -z, y).
# usage: blender -b --factory-startup --python build_connectors.py -- OUTDIR [ID,ID,...]
import sys, os, math, json, time, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
import r4lib as L
from r4lib import M, box, cyl, tube, hull, hexbolt, Part, new_col

G, H = 0.55, 0.26
VERSION = "1182-v1"
DIRS_U = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 1, 0), "down": (0, -1, 0), "forward": (0, 0, 1), "back": (0, 0, -1)}
UNITY_BIT = {"right": 0, "left": 1, "up": 2, "down": 3, "forward": 4, "back": 5}


def Bv(u): return Vector((u[0], -u[2], u[1]))                 # Unity -> Blender
def Uv(b): return (b[0], b[2], -b[1])                          # Blender -> Unity


DIRS = {k: tuple(Bv(v)) for k, v in DIRS_U.items()}            # Blender normals (forward = -Y, up = +Z)
a_ = sys.argv[sys.argv.index("--") + 1:]
OUT = a_[0]; ONLY = a_[1].split(",") if len(a_) > 1 else None

ROD_R, STRUT_W, STRUT_H, STRUT_CH = 0.030, 0.180, 0.130, 0.026
MINI_HALF = 0.115            # rod port plate half size (m)
MINI_HALF_CELLS = round(MINI_HALF/G, 4)

# ------------------------------------------------------------------------------------------------ catalogue (Unity cells)
def ln(a, b): return {"a": a, "b": b, "kind": "line"}
def ar(a, b, c): return {"a": a, "b": b, "kind": "arc", "c": c}
STRAIGHT = lambda n: ({"A": (0, 0, 0), "B": (0, 0, n - 1)}, [ln("A", "B")])
CROSS = ({"C": (1, 0, 1), "N00": (0, 0, 0), "N20": (2, 0, 0), "N02": (0, 0, 2), "N22": (2, 0, 2)}, [ln("C", k) for k in ("N00", "N20", "N02", "N22")])
CAT = {
    "ROD_SHORT": ("rod", *STRAIGHT(2), "straight rod 1x2"),
    "ROD_STANDARD": ("rod", *STRAIGHT(3), "straight rod 1x3"),
    "ROD_LONG": ("rod", *STRAIGHT(5), "straight rod 1x5 (spaced-armour standoff)"),
    "ROD_ARC_SHORT": ("rod", {"A": (0, 0, 0), "B": (1, 0, 1)}, [ar("A", "B", (1, 0, 0))], "quarter-arc rod, 2x2 (radius 1 cell)"),
    "ROD_ARC_LONG": ("rod", {"A": (0, 0, 0), "B": (2, 0, 2)}, [ar("A", "B", (2, 0, 0))], "quarter-arc rod, 3x3 (radius 2 cells)"),
    "ROD_DIAG_2D_SHORT": ("rod", {"A": (0, 0, 0), "B": (1, 0, 1)}, [ln("A", "B")], "flat diagonal rod, 2x2"),
    "ROD_DIAG_2D_LONG": ("rod", {"A": (0, 0, 0), "B": (2, 0, 2)}, [ln("A", "B")], "flat diagonal rod, 3x3"),
    "ROD_DIAG_3D": ("rod", {"A": (0, 0, 0), "B": (2, 2, 2)}, [ln("A", "B")], "space-diagonal rod, 3x3x3"),
    "ROD_CROSS": ("rod", *CROSS, "X rod cross 3x3 with a centre hub"),
    "ROD_PLUS": ("rod", {"C": (1, 0, 1), "W": (0, 0, 1), "E": (2, 0, 1), "S": (1, 0, 0), "N": (1, 0, 2)}, [ln("C", k) for k in ("W", "E", "S", "N")], "plus rod (4 arms) 3x3"),
    "ROD_FRAME": ("rod", {"N00": (0, 0, 0), "N20": (2, 0, 0), "N22": (2, 0, 2), "N02": (0, 0, 2)},
                  [ln("N00", "N20"), ln("N20", "N22"), ln("N22", "N02"), ln("N02", "N00")], "open square rod frame 3x3"),
    "ROD_T": ("rod", {"J": (1, 0, 2), "L": (0, 0, 2), "R": (2, 0, 2), "S": (1, 0, 0)}, [ln("J", "L"), ln("J", "R"), ln("J", "S")], "T rod 3x3"),
    "STRUT_SHORT": ("strut", *STRAIGHT(2), "straight strut 1x2"),
    "STRUT_STANDARD": ("strut", *STRAIGHT(3), "straight strut 1x3"),
    "STRUT_LONG": ("strut", *STRAIGHT(5), "straight strut 1x5 (weapon mast / long brace)"),
    "STRUT_ARC": ("strut", {"A": (0, 0, 0), "B": (2, 0, 2)}, [ar("A", "B", (2, 0, 0))], "quarter-arc strut, 3x3 (radius 2 cells)"),
    "STRUT_DIAG_2D": ("strut", {"A": (0, 0, 0), "B": (2, 0, 2)}, [ln("A", "B")], "flat diagonal strut, 3x3"),
    "STRUT_DIAG_3D": ("strut", {"A": (0, 0, 0), "B": (2, 2, 2)}, [ln("A", "B")], "space-diagonal strut, 3x3x3"),
    "STRUT_CROSS": ("strut", *CROSS, "X strut cross 3x3 with a heavy centre node"),
    "STRUT_RAMP": ("strut", {"A": (0, 0, 0), "B": (0, 0, 2), "C": (0, 2, 0)}, [ln("A", "B"), ln("A", "C"), ln("C", "B")], "triangulated knee bracket / ramp (3 nodes)"),
}
ORDER = list(CAT.keys())


# ------------------------------------------------------------------------------------------------ geometry helpers (same as R4Blocks)
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
    """R4 port contact pattern (identical to R4Blocks): recessed coupling ring + centre socket + 4 locating holes, all below the face"""
    m = fmat(c, n, u); made = []
    # full-size ports keep the R4Blocks segment counts; the 0.45-scale rod_mini pattern (r 41 mm) uses fewer segments (same shape)
    sR, sC, sH = (24, 20, 12) if s >= 0.9 else (16, 12, 8)
    made.append(tube("pp_groove", 0.092*s, 0.072*s, (0, 0, -0.004), (0, 0, 0.010), M["Armor_Dark"], pt.D, seg=sR))
    made.append(cyl("pp_socket", 0.034*s, (0, 0, -0.006), (0, 0, 0.010), M["Armor_Dark"], pt.D, seg=sC))
    for (x, y) in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
        made.append(cyl("pp_hole", 0.013*max(s, 0.7), (0.150*s*x, 0.150*s*y, -0.007), (0.150*s*x, 0.150*s*y, 0.010), M["Armor_Dark"], pt.D, seg=sH))
    made.append(tube("pp_ring", 0.0905*s, 0.0735*s, (0, 0, -0.0042), (0, 0, -0.0022), M["Steel_Light"], pt.Pc, seg=sR))
    made.append(cyl("pp_core", 0.0325*s, (0, 0, -0.0062), (0, 0, -0.0040), M["Steel_Light"], pt.Pc, seg=sC))
    to_face(made, m)


def seam_frame(pt, c, n, hw, hh, inset=0.034, u=None):
    m = fmat(c, n, u); a, b = hw - inset, hh - inset; w = 0.0016; made = []
    for (x0, x1, y0, y1) in ((-a, a, b - w, b + w), (-a, a, -b - w, -b + w), (a - w, a + w, -b, b), (-a - w, -a + w, -b, b)):
        made.append(box("seam", x0, x1, y0, y1, -0.0015, 0.010, M["Armor_Dark"], pt.D))
    to_face(made, m)


def corners(x0, x1, y0, y1, z0, z1): return [(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
def csq(hs, c, z): return [(x, y, z) for (x, y) in ((hs, hs - c), (hs - c, hs), (-hs + c, hs), (-hs, hs - c), (-hs, -hs + c), (-hs + c, -hs), (hs - c, -hs), (hs, -hs + c))]


def sweep(name, P, prof, up, mat, col):
    """closed tube: 2D profile [(u, v)] swept along polyline P (Blender), frames from a fixed up hint"""
    Q = [P[0]]
    for p in P[1:]:
        if (p - Q[-1]).length > 1e-4: Q.append(p)
    P = Q
    bm = bmesh.new(); rings = []
    for i, p in enumerate(P):
        t = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
        u = Vector(up).cross(t)
        if u.length < 1e-4: u = Vector((1, 0, 0)).cross(t)
        u.normalize(); v = t.cross(u)
        rings.append([bm.verts.new(p + u*a + v*b) for (a, b) in prof])
    n = len(prof)
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(n): bm.faces.new((r0[k], r0[(k + 1) % n], r1[(k + 1) % n], r1[k]))
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); col.objects.link(ob); ob.data.materials.append(mat)
    return ob


def circle(r, n=12): return [(r*math.cos(2*math.pi*k/n), r*math.sin(2*math.pi*k/n)) for k in range(n)]
def octprof(w, h, c): return [(w/2, h/2 - c), (w/2 - c, h/2), (-w/2 + c, h/2), (-w/2, h/2 - c), (-w/2, -h/2 + c), (-w/2 + c, -h/2), (w/2 - c, -h/2), (w/2, -h/2 + c)]


# ------------------------------------------------------------------------------------------------ connector logic
class Conn:
    def __init__(self, pid):
        self.id = pid; self.fam, self.nodes, self.members, self.desc = CAT[pid]
        self.paths = []
        for m in self.members:
            pa = Bv(self.nodes[m["a"]])*G; pb = Bv(self.nodes[m["b"]])*G
            if m["kind"] == "line":
                n = max(2, int((pb - pa).length/0.02))
                P = [pa.lerp(pb, i/(n - 1)) for i in range(n)]; up = self.plane_up(pb - pa)
            else:
                c = Bv(m["c"])*G; ra, rb = pa - c, pb - c; nrm = ra.cross(rb).normalized(); R = ra.length
                ang = ra.angle(rb); n = max(12, int(R*ang/0.02))
                P = [c + Matrix.Rotation(ang*i/(n - 1), 3, nrm) @ ra for i in range(n)]; up = nrm
            self.paths.append((P, up))
        self.r_eff = (ROD_R + 0.012) if self.fam == "rod" else 0.145
        self.occ = self.analytic_cells()
        self.ports = self.make_ports()

    @staticmethod
    def plane_up(t):
        t = t.normalized()
        return Vector((0, 0, 1)) if abs(t.z) < 0.95 else Vector((0, 1, 0))

    def tangents(self, node):
        out = []
        for (m, (P, up)) in zip(self.members, self.paths):
            if m["a"] == node: out.append((P[1] - P[0]).normalized())
            if m["b"] == node: out.append((P[-2] - P[-1]).normalized())
        return out

    def analytic_cells(self):
        role = {}
        for k, c in self.nodes.items(): role[tuple(c)] = "node"
        for P, _ in self.paths:
            for p in P:
                u = Uv(p); cu = [x/G for x in u]
                for cx in range(math.floor(cu[0] - 1), math.ceil(cu[0] + 1) + 1):
                    for cy in range(math.floor(cu[1] - 1), math.ceil(cu[1] + 1) + 1):
                        for cz in range(math.floor(cu[2] - 1), math.ceil(cu[2] + 1) + 1):
                            q = [cx*G, cy*G, cz*G]
                            d2 = sum(max(0.0, abs(u[i] - q[i]) - (H - 0.002))**2 for i in range(3))   # sphere vs block box (+-0.258)
                            if d2 < self.r_eff**2 and (cx, cy, cz) not in role:
                                inside = all(abs(u[i] - q[i]) <= G/2 for i in range(3))
                                role[(cx, cy, cz)] = "span" if inside else "crossing"
        # a cell first marked crossing but later containing the member axis is a span
        for P, _ in self.paths:
            for p in P:
                u = Uv(p); c = tuple(int(round(x/G)) for x in u)
                if role.get(c) == "crossing": role[c] = "span"
        return dict(sorted(role.items()))

    def make_ports(self):
        ports = []
        for nk, c in self.nodes.items():
            T = self.tangents(nk)
            for d, n in DIRS_U.items():
                nb = (c[0] + n[0], c[1] + n[1], c[2] + n[2])
                if nb in self.occ: continue
                if any(Vector(Bv(n)).dot(t) > 0.3 for t in T): continue
                # rods: only end-faces opposite a member (pure links, no side ports); struts: every free face (mounting hub)
                if self.fam == "rod" and not any(Vector(Bv(n)).dot(t) < -0.3 for t in T): continue
                ports.append({"id": f"P_{nk}_{d}", "node": nk, "cell": list(c), "dir": d, "kind": "rod_mini" if self.fam == "rod" else "r4_full",
                              "half": [MINI_HALF_CELLS, MINI_HALF_CELLS] if self.fam == "rod" else [0.47, 0.47]})
        return ports


def rod_node(pt, c, ports, tangs):
    cB = Bv(c)*G; a, b = 0.060, 0.044
    hub = [cB + Vector(p) for p in [(sx*a, sy*a, sz*b) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)] +
           [(sx*a, sy*b, sz*a) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)] + [(sx*b, sy*a, sz*a) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]]
    hull("rod_hub", [tuple(p) for p in hub], M["Armor_Gunmetal"], pt.U, bevel=0.004)
    for p in ports:
        n = Vector(DIRS[p["dir"]]); m = fmat(cB + n*H, n); made = []
        made.append(box("stub", -0.030, 0.030, -0.030, 0.030, -(H - 0.050), -0.012, M["Armor_Dark"], pt.U, bevel=0.003))
        made.append(hull("mini_plate", csq(MINI_HALF, 0.018, -0.016) + csq(MINI_HALF, 0.018, 0.0), M["Armor_Dark"], pt.U, bevel=0.0015))
        for (x, y) in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
            made.append(hexbolt("mini_bolt", (0.084*x, 0.084*y, -0.0155), (0.084*x, 0.084*y, -0.0230), M["Steel_Light"], pt.Pc, r=0.0055))
        for sx in (-1, 1):
            made.append(box("mini_haz", sx*(MINI_HALF - 0.0005), sx*(MINI_HALF + 0.0016), 0.030, 0.075, -0.013, -0.003, M["Hazard_Yellow"], pt.Pc))
        made.append(box("mini_idx", -0.018, 0.018, MINI_HALF - 0.0005, MINI_HALF + 0.0016, -0.013, -0.003, M["TeamColor_Red"], pt.Pc))
        to_face(made, m)
        port_pattern(pt, cB + n*H, n, s=0.45)
    for t in tangs:            # steel collar where the member leaves the hub
        P = [cB + t*0.085, cB + t*0.125]
        sweep("rod_collar", P, circle(ROD_R + 0.010, 16), Conn.plane_up(t), M["Steel_Light"], pt.U)


def strut_node(pt, c, ports, tangs):
    cB = Bv(c)*G
    hull("strut_node", [tuple(cB + Vector(p)) for p in corners(-H, H, -H, H, -H, H)], M["Armor_Gunmetal"], pt.U, bevel=0.022)
    pdirs = {p["dir"] for p in ports}
    for ax in range(3):        # dark armoured edges: a strut node never reads as a plain R4B_Cube (outside the port patch)
        i, j = [k for k in range(3) if k != ax]
        for si in (-1, 1):
            for sj in (-1, 1):
                pts = []
                for t in (-H + 0.030, H - 0.030):
                    for (a_, b_) in ((H + 0.0008, H - 0.034), (H - 0.034, H + 0.0008), (H - 0.045, H - 0.045)):
                        q = [0.0, 0.0, 0.0]; q[ax] = t; q[i] = si*a_; q[j] = sj*b_; pts.append(tuple(cB + Vector(q)))
                hull("node_edge", pts, M["Armor_Dark"], pt.Pc)
    for d, n in DIRS.items():
        n = Vector(n)
        if d in pdirs:
            port_pattern(pt, cB + n*H, n); seam_frame(pt, cB + n*H, n, H, H)
        else:
            seam_frame(pt, cB + n*H, n, H, H, inset=0.020)
    for t in tangs:            # load collar where the beam leaves the node
        P = [cB + t*0.22, cB + t*0.30]
        sweep("strut_collar", P, octprof(STRUT_W + 0.040, STRUT_H + 0.040, STRUT_CH + 0.010), Conn.plane_up(t), M["Armor_Dark"], pt.U)


def rod_member(pt, P, up):
    sweep("rod", P, circle(ROD_R, 12), up, M["Armor_Gunmetal"], pt.U)
    Lm = sum((P[i + 1] - P[i]).length for i in range(len(P) - 1))
    def at(s):
        acc = 0.0
        for i in range(len(P) - 1):
            l = (P[i + 1] - P[i]).length
            if acc + l >= s: return i, (s - acc)/l
            acc += l
        return len(P) - 2, 1.0
    def seg(s0, s1, r, mat):
        i0, f0 = at(s0); i1, f1 = at(s1)
        Q = [P[i0].lerp(P[i0 + 1], f0)] + P[i0 + 1:i1 + 1] + [P[i1].lerp(P[i1 + 1], f1)]
        sweep("rod_band", Q, circle(r, 12), up, mat, pt.U)
    seg(Lm/2 - 0.020, Lm/2 + 0.020, ROD_R + 0.0045, M["TeamColor_Red"])
    k = 1
    while k*G < Lm - 0.30:                      # dark sleeve at every cell pitch (reads the grid along the rod)
        if abs(k*G - Lm/2) > 0.06: seg(k*G - 0.012, k*G + 0.012, ROD_R + 0.006, M["Armor_Dark"])
        k += 1
    for s in (0.17, Lm - 0.17):
        if Lm > 0.60: seg(s - 0.008, s + 0.008, ROD_R + 0.004, M["Hazard_Yellow"])


def strut_member(pt, P, up):
    sweep("strut", P, octprof(STRUT_W, STRUT_H, STRUT_CH), up, M["Armor_Gunmetal"], pt.U)
    cum = [0.0]
    for i in range(len(P) - 1): cum.append(cum[-1] + (P[i + 1] - P[i]).length)
    Lm = cum[-1]
    def frame_at(s):
        i = max(0, min(len(P) - 2, next((j for j in range(len(P) - 1) if cum[j + 1] >= s), len(P) - 2)))
        f = (s - cum[i])/max(1e-9, cum[i + 1] - cum[i]); p = P[i].lerp(P[i + 1], f); t = (P[i + 1] - P[i]).normalized()
        u = Vector(up).cross(t); u = u if u.length > 1e-4 else Vector((1, 0, 0)).cross(t); u.normalize(); v = t.cross(u)
        return p, t, u, v
    def piece(s0, s1, prof, mat, col):
        Q = [frame_at(s0 + (s1 - s0)*k/4)[0] for k in range(5)]
        sweep("strut_piece", Q, prof, up, mat, col)
    st = 0.33
    while st < Lm - 0.32:                                   # stiffening ribs every half cell
        piece(st - 0.012, st + 0.012, octprof(STRUT_W + 0.022, STRUT_H + 0.022, STRUT_CH + 0.004), M["Armor_Dark"], pt.U)
        st += G/2
    st = 0.33 + G/4
    while st < Lm - 0.32:                                   # lightening windows through the web + team stripe on top
        p, t, u, v = frame_at(st)
        m = Matrix((t, v, u)).transposed().to_4x4(); m.translation = p
        o = cyl("window", 0.034, (0, 0, -0.2), (0, 0, 0.2), M["Armor_Dark"], pt.D, seg=16); o.data.transform(m)
        q = Matrix((t, u, v)).transposed().to_4x4(); q.translation = p
        o2 = box("team_stripe", -0.050, 0.050, -0.028, 0.028, STRUT_H/2 - 0.0012, STRUT_H/2 + 0.0016, M["TeamColor_Red"], pt.Pc); o2.data.transform(q)
        st += G/2
    for s in (0.31, Lm - 0.31):                              # hazard ticks at both ends (on top face)
        p, t, u, v = frame_at(s)
        q = Matrix((t, u, v)).transposed().to_4x4(); q.translation = p
        for k in (-1, 1):
            o = box("haz", -0.008, 0.008, k*0.050 - 0.012, k*0.050 + 0.012, STRUT_H/2 - 0.0012, STRUT_H/2 + 0.0016, M["Hazard_Yellow"], pt.Pc); o.data.transform(q)


def build(work, C):
    first = list(C.nodes)[0]; c0 = Bv(C.nodes[first])*G
    ob = hull(C.id + "_core", [tuple(c0 + Vector(p)) for p in corners(-0.05, 0.05, -0.05, 0.05, -0.05, 0.05)], M["Armor_Gunmetal"], work)
    pt = Part(work, "c")
    for (P, up) in C.paths: (rod_member if C.fam == "rod" else strut_member)(pt, P, up)
    for nk, c in C.nodes.items():
        (rod_node if C.fam == "rod" else strut_node)(pt, c, [p for p in C.ports if p["node"] == nk], C.tangents(nk))
    pt.build(ob); ob.name = C.id
    return ob


# ------------------------------------------------------------------------------------------------ QA helpers (as R4Blocks)
def mesh_report(o):
    bm = bmesh.new(); bm.from_mesh(o.data)
    nm_ = sum(1 for e in bm.edges if not e.is_manifold); bd = sum(1 for e in bm.edges if e.is_boundary)
    za = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    inc = sum(1 for e in bm.edges if e.is_manifold and len(e.link_faces) == 2 and not e.is_contiguous)
    tris = sum(len(f.verts) - 2 for f in bm.faces)
    shells = 0; seen = set()
    for v in bm.verts:
        if v.index in seen: continue
        shells += 1; stack = [v]
        while stack:
            x = stack.pop()
            if x.index in seen: continue
            seen.add(x.index); stack.extend(e.other_vert(x) for e in x.link_edges)
    bm.free(); uv = len(o.data.uv_layers) > 0
    return {"object": o.name, "non_manifold_edges": nm_, "boundary_edges": bd, "zero_area_faces": za, "inconsistent_normals": inc, "triangles": tris,
            "shells": shells, "uv": uv, "ok": nm_ == 0 and bd == 0 and za == 0 and inc == 0 and uv}


def verts(o): return [o.matrix_world @ v.co for v in o.data.vertices]


def patch_test(tree, cellB, d, half, N=9):
    n = Vector(DIRS[d]); u, v, _ = frame(n)
    c = Vector(cellB)*G + n*H; flush = prot = miss = rec = 0
    for i in range(N):
        for j in range(N):
            a = (-half[0]*0.85 + 2*half[0]*0.85*i/(N - 1))*G; b = (-half[1]*0.85 + 2*half[1]*0.85*j/(N - 1))*G
            p = c + u*a + v*b + n*0.2; hit = tree.ray_cast(p, -n, 0.5)
            if hit[0] is None: miss += 1; continue
            k = (hit[0] - c).dot(n)
            if k > 0.0005: prot += 1
            elif k > -0.0010: flush += 1
            else: rec += 1
    return {"flush_pct": round(100*flush/(N*N), 1), "recessed_pct": round(100*rec/(N*N), 1), "protruding": prot, "missing": miss}


def geometric_cells(ob, tree, V):
    lo = [min(p[i] for p in V) for i in range(3)]; hi = [max(p[i] for p in V) for i in range(3)]
    cells = set(); h = H - 0.002
    loU, hiU = Uv(lo), Uv(hi)
    rngs = [range(int(math.floor(min(loU[i], hiU[i])/G + 0.5)) - 1, int(math.floor(max(loU[i], hiU[i])/G + 0.5)) + 2) for i in range(3)]
    for cx in rngs[0]:
        for cy in rngs[1]:
            for cz in rngs[2]:
                c = Bv((cx, cy, cz))*G
                bv = [c + Vector(p) for p in corners(-h, h, -h, h, -h, h)]
                bt = BVHTree.FromPolygons(bv, [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)])
                if tree.overlap(bt) or any(all(abs(p[i] - c[i]) < h for i in range(3)) for p in V[::7]):
                    cells.add((cx, cy, cz))
    return cells


def setup_render(scn, ortho=None):
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


def shoot(scn, cam, target, dist, d, path, ortho=None):
    cam.data.type = 'ORTHO' if ortho else 'PERSP'
    if ortho: cam.data.ortho_scale = ortho
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


def reimport_check(base, ref, n_empties):
    res = {}
    for ext, op in ((".fbx", lambda p: bpy.ops.import_scene.fbx(filepath=p)), (".glb", lambda p: bpy.ops.import_scene.gltf(filepath=p))):
        before = set(bpy.data.objects); op(base + ext); new = [o for o in bpy.data.objects if o not in before]
        ms = [o for o in new if o.type == 'MESH']
        if not ms: res[ext] = {"PASS": False, "why": "no mesh"}; continue
        bpy.context.view_layer.update()
        v = verts(ms[0]); lo = [min(p[i] for p in v) for i in range(3)]; hi = [max(p[i] for p in v) for i in range(3)]
        dmm = max(max(abs(lo[i] - ref[0][i]), abs(hi[i] - ref[1][i])) for i in range(3))*1000
        ne = len([o for o in new if o.type == 'EMPTY'])
        res[ext] = {"PASS": dmm < 0.05 and ne >= n_empties, "bounds_dev_mm": round(dmm, 4), "verts": len(ms[0].data.vertices), "empties": ne}
        for o in new: bpy.data.objects.remove(o, do_unlink=True)
    return res


def r3(v): return [round(float(x), 4) for x in v]


# ------------------------------------------------------------------------------------------------ main
summary = {}
for pid in ORDER:
    if ONLY and pid not in ONLY: continue
    t0 = time.time(); od = os.path.join(OUT, "R4C_" + pid); os.makedirs(os.path.join(od, "renders"), exist_ok=True); ID = "R4C_" + pid
    L.reset_scene(); L.init_palette(); scn = bpy.context.scene
    work = new_col("_build")
    try:
        C = Conn(pid); ob = build(work, C); ob.name = ID
    except Exception as ex:
        import traceback; traceback.print_exc(); summary[ID] = ("ERROR", [repr(ex)], 0); print("CONN", ID, "ERROR", repr(ex), flush=True); continue
    L.shade_and_uv(ob)
    col = new_col(ID)
    for c in list(ob.users_collection): c.objects.unlink(ob)
    col.objects.link(ob)
    root = bpy.data.objects.new(ID + "_ROOT", None); col.objects.link(root); root.empty_display_size = 0.1; ob.parent = root
    empties = []
    def mk(name, loc, n=None, size=0.06, kind=None):
        e = bpy.data.objects.new(name, None); col.objects.link(e); e.parent = root; e.location = loc
        e.empty_display_type = 'SINGLE_ARROW' if n is not None else 'PLAIN_AXES'; e.empty_display_size = size
        if n is not None: e.rotation_euler = Vector(n).to_track_quat('Z', 'Y').to_euler()
        if kind: e["kind"] = kind
        empties.append(e); return e
    for p in C.ports:
        n = Vector(DIRS[p["dir"]]); pos = Bv(p["cell"])*G + n*H
        e = mk(f"Port_{p['dir']}_{p['cell'][0]}_{p['cell'][1]}_{p['cell'][2]}", pos, n, kind=p["kind"]); e["port_id"] = p["id"]; e["patch_half_cells"] = p["half"]
        p["position_m"] = r3(Uv(pos)); p["normal"] = list(DIRS_U[p["dir"]])
    for nk, c in C.nodes.items(): mk(f"Node_{nk}", Bv(c)*G, size=0.05)
    # Socket_Start / Socket_End: the port of the first / farthest node most opposite to its member
    keys = [k for k in C.nodes if any(p["node"] == k for p in C.ports)]; a0 = keys[0]     # junction hubs carry no ports
    far = max(keys, key=lambda k: (Vector(C.nodes[k]) - Vector(C.nodes[a0])).length)
    def axial(nk):
        T = C.tangents(nk); cand = [p for p in C.ports if p["node"] == nk]
        return min(cand, key=lambda p: (sum(Vector(DIRS[p["dir"]]).dot(t) for t in T), list(DIRS_U).index(p["dir"])))
    sockets = {}
    for sn, nk in (("Socket_Start", a0), ("Socket_End", far)):
        p = axial(nk); n = Vector(DIRS[p["dir"]]); e = mk(sn, Bv(p["cell"])*G + n*H, n, size=0.09, kind=p["kind"]); e["port_id"] = p["id"]; sockets[sn] = p["id"]
    root["part_id"] = pid; root["family"] = C.fam; root["version"] = VERSION; root["grid_m"] = G; root["block_m"] = 2*H
    root["unity_up"] = "+Y"; root["unity_forward"] = "+Z"; root["occupied_cells_unity"] = json.dumps([list(k) for k in C.occ])
    # colliders (convex pieces): node boxes + member hulls, as a proposal for Unity
    cc = new_col(ID + "_Colliders"); ci = 0
    for nk, c in C.nodes.items():
        cB = Bv(c)*G; h = H if C.fam == "strut" else 0.120
        hull(f"UCX_{ID}_{ci:02d}", [tuple(cB + Vector(p)) for p in corners(-h, h, -h, h, -h, h)], M["Armor_Dark"], cc); ci += 1
        if C.fam == "rod":
            for p in [q for q in C.ports if q["node"] == nk]:
                n = Vector(DIRS[p["dir"]]); u, v, _ = frame(n); f = cB + n*H
                hull(f"UCX_{ID}_{ci:02d}", [tuple(f + u*a + v*b + n*t) for a in (-MINI_HALF, MINI_HALF) for b in (-MINI_HALF, MINI_HALF) for t in (-0.016, 0.0)], M["Armor_Dark"], cc); ci += 1
    rr = ROD_R if C.fam == "rod" else math.hypot(STRUT_W, STRUT_H)/2
    for (P, up) in C.paths:
        step = max(1, len(P)//6)
        for i in range(0, len(P) - 1, step):
            Q = P[i:min(len(P), i + step + 1)]
            pts = []
            for q in (Q[0], Q[-1]):
                t = (Q[-1] - Q[0]).normalized(); u = Vector(up).cross(t); u = u if u.length > 1e-4 else Vector((1, 0, 0)).cross(t); u.normalize(); v = t.cross(u)
                pts += [tuple(q + u*a + v*b) for a in (-rr, rr) for b in (-rr*(1 if C.fam == "rod" else STRUT_H/STRUT_W), rr*(1 if C.fam == "rod" else STRUT_H/STRUT_W))]
            hull(f"UCX_{ID}_{ci:02d}", pts, M["Armor_Dark"], cc); ci += 1
    # ---- QA
    bpy.context.view_layer.update()
    mr = mesh_report(ob); V = verts(ob)
    lo = [min(p[i] for p in V) for i in range(3)]; hi = [max(p[i] for p in V) for i in range(3)]
    bm = bmesh.new(); bm.from_mesh(ob.data); bm.transform(ob.matrix_world); tree = BVHTree.FromBMesh(bm); bm.free()
    geo = geometric_cells(ob, tree, V); ana = set(C.occ)
    occ_ok = geo <= ana and set(tuple(c) for c in C.nodes.values()) <= geo
    ptest = []
    for p in C.ports:
        r = patch_test(tree, Bv(p["cell"]), p["dir"], p["half"])
        r.update({"port": p["id"], "kind": p["kind"]})
        r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "r4_full" else 65)
        ptest.append(r)
    mats = sorted({s_.material.name for s_ in ob.material_slots if s_.material})
    allowed = {"Armor_Dark", "Armor_Gunmetal", "Armor_Plate_Light", "Steel_Light", "TeamColor_Red", "Hazard_Yellow", "Energy_Hot"}
    budget = 14000 if C.fam == "rod" else 22000
    base = os.path.join(od, ID)
    export(root, [root, ob] + empties, base)
    bpy.ops.object.select_all(action='DESELECT')
    for o_ in cc.objects: o_.select_set(True)
    bpy.context.view_layer.objects.active = list(cc.objects)[0]
    bpy.ops.export_scene.gltf(filepath=base + "_colliders.glb", export_format='GLB', use_selection=True, export_yup=True)
    re_ = reimport_check(base, (lo, hi), len(empties) + 1)
    failures = []
    if not mr["ok"]: failures.append("topology")
    if mr["shells"] != 1: failures.append(f"floating shells ({mr['shells']})")
    if not occ_ok: failures.append("geometry enters undeclared cells")
    if not all(p["PASS"] for p in ptest): failures.append("port contact patch")
    if not C.ports: failures.append("no ports")
    if not set(mats) <= allowed: failures.append("palette")
    if mr["triangles"] > budget: failures.append("triangle budget")
    if not all(v["PASS"] for v in re_.values()): failures.append("export reimport")
    # ---- renders: front, side, 3/4, wireframe 3/4, fixed-scale board view
    for o in cc.objects: o.hide_render = True
    for o in empties + [root]: o.hide_render = True
    cam = setup_render(scn)
    ctr = Vector([(lo[i] + hi[i])/2 for i in range(3)]); ext = max(hi[i] - lo[i] for i in range(3))
    renders = []
    for tag, dvec in (("front", (0.0, -1.0, 0.0)), ("side", (1.0, 0.0, 0.0)), ("34front", (1.0, -1.25, 0.85))):
        p_ = os.path.join(od, "renders", f"{ID}_{tag}.png"); shoot(scn, cam, ctr, ext*2.6 + 0.8, dvec, p_); renders.append(os.path.basename(p_))
    wf = ob.copy(); wf.data = ob.data.copy(); col.objects.link(wf); mod = wf.modifiers.new("wf", 'WIREFRAME'); mod.thickness = 0.0022; ob.hide_render = True
    p_ = os.path.join(od, "renders", f"{ID}_34front_wireframe.png"); shoot(scn, cam, ctr, ext*2.6 + 0.8, (1.0, -1.25, 0.85), p_); renders.append(os.path.basename(p_))
    bpy.data.objects.remove(wf, do_unlink=True); ob.hide_render = False
    scn.render.resolution_x, scn.render.resolution_y = 700, 700
    board_c = Vector((0.50, -1.00, 0.45))            # same camera for every part (fixed scale 4.4 m)
    p_ = os.path.join(od, "renders", f"{ID}_board.png"); shoot(scn, cam, board_c, 20, (1.0, -1.25, 0.85), p_, ortho=4.4); renders.append(os.path.basename(p_))
    bpy.ops.wm.save_as_mainfile(filepath=base + ".blend")
    files = {}
    for f in sorted(os.listdir(od)):
        fp = os.path.join(od, f)
        if os.path.isfile(fp) and not f.endswith(".json") and not f.endswith(".blend1"): files[f] = {"sha256": hashlib.sha256(open(fp, "rb").read()).hexdigest(), "bytes": os.path.getsize(fp)}
    loU, hiU = Uv(lo), Uv(hi); bbU = [[round(min(loU[i], hiU[i]), 4) for i in range(3)], [round(max(loU[i], hiU[i]), 4) for i in range(3)]]
    members = []
    for k, (m, (P, up)) in enumerate(zip(C.members, C.paths)):
        members.append({"id": f"M{k}", "from_node": m["a"], "to_node": m["b"], "kind": m["kind"], "length_m": round(sum((P[i + 1] - P[i]).length for i in range(len(P) - 1)), 4),
                        "carries_ports": False})
    qa = {"id": ID, "part_id": pid, "family": C.fam, "description": C.desc, "status": "BLENDER_PASS" if not failures else "HOLD", "failures": failures,
          "unity": "UNITY_NOT_RUN", "occupied_cells_unity": [{"cell": list(k), "role": v} for k, v in C.occ.items()],
          "occupied_geometric": sorted([list(c) for c in geo]), "occupancy_consistent": occ_ok, "nodes": {k: list(v) for k, v in C.nodes.items()},
          "members": members, "ports": C.ports, "sockets": sockets, "port_contact_test": ptest, "mesh": mr, "triangle_budget": budget,
          "bounds_unity_m": bbU, "materials": mats, "reimport": re_, "renders": renders, "files": files,
          "colliders": {"file": ID + "_colliders.glb", "pieces": len(cc.objects)}, "seconds": round(time.time() - t0, 1), "blender": bpy.app.version_string}
    json.dump(qa, open(base + "_QA.json", "w", encoding="utf-8"), indent=1)
    summary[ID] = (qa["status"], failures, mr["triangles"])
    print("CONN", ID, qa["status"], failures, mr["triangles"], round(time.time() - t0, 1), flush=True)
json.dump(summary, open(os.path.join(OUT, "_summary_" + ("all" if not ONLY else "part") + ".json"), "w"), indent=1)
print("CONN_DONE")
