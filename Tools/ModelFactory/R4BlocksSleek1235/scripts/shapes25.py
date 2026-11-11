"""The 25 remaining R4 building blocks in A_Sleek (#1235). Every solid uses the dimensions of the approved
R4Blocks/scripts/build_blocks.py (PR #963 @03af520): grid 0.55 m, block 0.52 m, contact faces at +-0.26 m, half height top at -0.015 m.
build(kind, lod) -> mesh dict (V, faces[v, n, uv, group]). Blender axes: X right, Y back, Z up."""
import math
import numpy as np
import paths25
import r4panel as RP, r4convex as RC, r4sleek as S, roundcorner
from styles import STYLES

A = dict(STYLES["A"]); A.update(seam_h=0.006, min_plate=0.05)
G, H, HT = 0.55, 0.26, -0.015
X = np.array([1.0, 0, 0]); Y = np.array([0, 1.0, 0]); Z = np.array([0, 0, 1.0]); O3 = np.zeros(3)
AX_Z = (X, Y, Z, O3)          # profile (x, y), extruded along z
AX_X = (Y, Z, X, O3)          # profile (y, z), extruded along x
AX_Y = (X, Z, -Y, O3)         # profile (x, z), extruded along -y (right-handed)
L = S.P_line

def corners(x0, x1, y0, y1, z0, z1): return [(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
def rect(a0, a1, b0, b1): return [L((a0, b0)), L((a1, b0)), L((a1, b1)), L((a0, b1))]
def ngon(n, r, rot=0.0): return [(r*math.cos(rot + 2*math.pi*i/n), r*math.sin(rot + 2*math.pi*i/n)) for i in range(n)]

def convex(points, g_face=None, plate_faces=None):
    def f(lod): return RC.lod_build(points, A) if lod else RC.build(points, A, g_face=g_face, plate_faces=plate_faces)
    f.points = points
    return f

def box(x0, x1, y0, y1, z0, z1, sx=(), sy=(), sz=()):
    def f(lod): return S.prism(rect(x0, x1, y0, y1), z0, z1, AX_Z, A, seams_ab=[(0, v) for v in sx] + [(1, v) for v in sy], seams_t=list(sz), lod=lod)
    return f

# ------------------------------------------------------------------------------------------------ wave 1: convex / straight
INNER = [p for p in corners(-H, H, -H, H, -H, H) if p != (H, -H, H)]
_oc = 0.060
OCT = [(H, -H + _oc), (H, H - _oc), (H - _oc, H), (-H + _oc, H), (-H, H - _oc), (-H, -H + _oc), (-H + _oc, -H), (H - _oc, -H)]
def long_wedge(lod): return S.prism([L((-G - H, -H)), L((H, -H)), L((H, H))], -H, H, AX_X, A, seams_ab=[(0, -G/2)], lod=lod)
def wide_wedge(lod): return S.prism([L((-H, -H)), L((H, -H)), L((H, H))], -H, G + H, AX_X, A, seams_t=[G/2], lod=lod)

WAVE1 = {
 "InnerCorner": convex(INNER, g_face=lambda n, g: g/math.sqrt(3) if abs(abs(n[0]) - abs(n[1])) < 1e-6 and abs(abs(n[0]) - abs(n[2])) < 1e-6 else g),
 "HalfWedge": convex(corners(-H, H, -H, H, -H, -H) + [(x, H, HT) for x in (-H, H)]),
 "LongWedge": long_wedge,
 "WideWedge": wide_wedge,
 "Pyramid": convex(corners(-H, H, -H, H, -H, -H) + [(0, 0, H)]),
 "HexColumn": convex([(x, y, z) for (x, y) in ngon(6, H) for z in (-H, H)]),
 "OctColumn": convex([(x, y, z) for (x, y) in OCT for z in (-H, H)], plate_faces=lambda n: abs(n[2]) > 0.5 or abs(abs(n[0]) - abs(n[1])) > 0.5),      # 85 mm chamfer faces stay plain frame
 "Plate1x2": box(-H, G + H, -H, H, -H, HT, sx=[G/2]),
 "Plate2x2": box(-H, G + H, -G - H, H, -H, HT, sx=[G/2], sy=[-G/2]),
 "Beam2": box(-H, G + H, -H, H, -H, H, sx=[G/2]),
 "Beam3": box(-H, 2*G + H, -H, H, -H, H, sx=[G/2, 3*G/2]),
 "Slab2x2": box(-H, G + H, -G - H, H, -H, H, sx=[G/2], sy=[-G/2]),
 "BigCube2": box(-H, G + H, -G - H, H, -H, G + H, sx=[G/2], sy=[-G/2], sz=[G/2]),
}
# ------------------------------------------------------------------------------------------------ wave 2: curved
R2 = 2*H                                                                # RoundEdge / RoundCorner radius of the approved blocks
# The approved curved solids are POLYGONAL (arc(16), 9 x 9 sphere grid, 20-segment half circle, 48-gon cylinder) and so are their colliders.
# To stay inside the approved collider hull the A_Sleek arcs use vertices that are a SUBSET of the approved polygon vertices (their chords
# then lie inside the approved polygon); the sphere patch, which is tessellated differently, uses the inscribed radius of the approved grid.
RC_R = R2*math.cos(math.pi/32)**2
def round_edge(lod):
    n = 4 if lod else 8                                                  # approved arc: 16 segments
    return S.prism(S.P_arc((H, -H), R2, 90, 180, n) + [L((H, -H))], -H, H, AX_X, A, lod=lod)
def round_top(lod):
    n = 5 if lod else 10                                                 # approved half circle: 20 segments (subset)
    return S.prism([L((-H, -H)), L((H, -H))] + S.P_arc((0, 0), H, 0, 180, n), -H, H, AX_X, A, lod=lod)
def cylinder(lod): return S.revolve([L((0, -H)), L((H, -H)), L((H, H)), L((0, H))], 12 if lod else 24, A, lod=lod)     # approved: 48-gon
CONE_RIM = 0.050; CONE_TIP = 0.020
def cone(lod):
    al = math.atan2(H, 2*H - CONE_RIM); zc = H - CONE_TIP/math.sin(al)
    return S.revolve([L((0, -H)), L((H, -H)), L((H, -H + CONE_RIM))] + S.P_arc((0, zc), CONE_TIP, math.degrees(al), 90, 1 if lod else 3), 12 if lod else 24, A, lod=lod)

DOME_Z0 = -H + 0.070; DOME_A = 0.228; DOME_TOP = 0.246
def dome(lod):
    """low base plate (prism, top cap pierced by a round hole) + elliptic dome revolved into the hole: one shell"""
    g = 0.0 if lod else A["g"]; N = 12 if lod else 24; zc = DOME_Z0 - g; a, b = DOME_A, DOME_TOP - zc; af, bf = a - g, b - g
    hole = [np.array([af*math.cos(2*math.pi*i/N), af*math.sin(2*math.pi*i/N)]) for i in range(N)]
    base = S.prism(rect(-H, H, -H, H), -H, DOME_Z0, AX_Z, A, lod=lod, caps=("auto", "none"), cap_holes=(None, [hole]))
    def E(th, aa, bb): return np.array([aa*math.cos(th), zc + bb*math.sin(th)]), S.unit(np.array([math.cos(th)/aa, math.sin(th)/bb]))
    sec = []
    if lod:
        ths = np.linspace(0, math.pi/2, 5); pts = [E(t, a, b) for t in ths]
        sec = [(pts[i], pts[i + 1], "plate") for i in range(4)]
    else:
        t1 = math.asin(0.024/bf); t2 = math.asin((0.024 + A["chamfer"])/b)
        c0, c1, c2 = E(0.0, af, bf), E(t1/2, af, bf), E(t1, af, bf); w1 = E(t2, a, b)
        sec = [(c0, c1, "frame"), (c1, c2, "frame")]
        d = w1[0] - c2[0]; wn = S.unit(np.array([d[1], -d[0]])); wn = wn if wn[0] > 0 else -wn
        sec.append(((c2[0], wn), (w1[0], wn), "plate_wall"))
        ths = np.linspace(t2, math.pi/2, 9); pts = [E(t, a, b) for t in ths]
        sec += [(pts[i], pts[i + 1], "plate") for i in range(8)]
    mesh = S.Mesh(); S.merge(mesh, base); S.revolve_section(mesh, sec, N)
    return mesh.result(base["style"])

def round_corner(lod): return roundcorner.build(A, RC_R, (-H, H, -H), (1, 0, 0), (0, -1, 0), (0, 0, 1), lod=lod)

WAVE2 = {"RoundEdge": round_edge, "RoundTop": round_top, "Cylinder": cylinder, "Cone": cone, "Dome": dome, "RoundCorner": round_corner}

# ------------------------------------------------------------------------------------------------ wave 3: concave (one continuous shell)
def step(lod): return S.prism([L((-H, -H)), L((H, -H)), L((H, H)), L((0, H)), L((0, HT)), L((-H, HT))], -H, H, AX_X, A, lod=lod)
def corner_l3(lod):
    return S.prism([L((-H, -G - H)), L((H, -G - H)), L((H, -H)), L((G + H, -H)), L((G + H, H)), L((-H, H))], -H, H, AX_Z, A, seams_ab=[(0, G/2), (1, -G/2)], lod=lod)
def tee4(lod):
    return S.prism([L((-H, -G - H)), L((H, -G - H)), L((H, -H)), L((G + H, -H)), L((G + H, H)), L((-G - H, H)), L((-G - H, -H)), L((-H, -H))], -H, H, AX_Z, A,
                   seams_ab=[(0, -G/2), (0, G/2), (1, -G/2)], lod=lod)
WAVE3 = {"Step": step, "CornerL3": corner_l3, "Tee4": tee4}

# ------------------------------------------------------------------------------------------------ wave 4: tunnel, concave curve, open lattice
ARCH_R = 0.170; COVE_R = 0.400
def arch(lod):
    n = 5 if lod else 10
    return S.prism([L((-H, -H))] + S.P_arc((0, -H), ARCH_R, 180, 0, n, convex=False) + [L((H, -H)), L((H, H)), L((-H, H))], -H, H, AX_Y, A, lod=lod)
def cove_edge(lod):
    n = 4 if lod else 8
    return S.prism([L((-H, -H)), L((H, -H)), L((H, H))] + S.P_arc((-H, H), COVE_R, 0, -90, n, convex=False), -H, H, AX_X, A, lod=lod)
TRUSS_T = 0.055; HUB = 0.165; ARM = 0.050; ARM_REACH = 0.310
def truss_cube(lod):
    """open lattice: one genus-5 frame shell (rounded outside, square windows, beam depth TRUSS_T) + six X-braced hub shells.
    Each hub carries the plate of the approved partial port (flush on the contact plane); the arm ends are buried in the frame beams."""
    st = dict(A)
    if lod: st.update(w=A["g"] + A["w"], g=0.0, seg=1)
    g = st["g"]; win = H - TRUSS_T; mesh = S.Mesh()
    outer = RC.build(corners(-H, H, -H, H, -H, H), st, plate_faces=lambda n: False)
    for f in outer["faces"]:
        P = outer["V"][f["v"]]
        if f["group"] != "frame": mesh.face(list(P), f["n"], f["group"], [(p[0] + p[2], p[1] + p[2]) for p in P]); continue
        n = outer["planes"][f["plane"]][0]; u, v, _ = RP.face_frame(n); lvl = H - g
        loop = sorted(P, key=lambda p: math.atan2(p @ v, p @ u)); W = [lvl*n + a*win*u + b*win*v for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        loop = loop[-1:] + loop[:-1] if abs(math.atan2(loop[0] @ v, loop[0] @ u) - math.atan2(-1, -1)) > 0.3 else loop
        k0 = int(np.argmin([np.linalg.norm(p - W[0]) for p in loop])); loop = loop[k0:] + loop[:k0]
        for k in range(4):                                             # ring of four trapezoids between the frame quad and the window
            k1 = (k + 1) % 4; q = [loop[k], loop[k1], W[k1], W[k]]; mesh.face(q, [n]*4, "frame", [(p @ u, p @ v) for p in q], n)
            wn = -S.unit((W[k] + W[k1])/2 - lvl*n)                      # window wall faces the opening
            a_, b_ = W[k], W[k1]; q = [a_, b_, b_ - (TRUSS_T - g)*n, a_ - (TRUSS_T - g)*n]
            mesh.face(q, [wn]*4, "frame", [(p @ u, p @ n) for p in q], wn)
    hs = dict(A); hs.update(g=A["g"] + 0.002, w=0.006 if lod else 0.008, seg=1 if lod else 2, cseg=1 if lod else A["cseg"], min_plate=0.2)
    a_ = ARM/math.sqrt(2); d = np.array([1, 1])/math.sqrt(2); p = np.array([-1, 1])/math.sqrt(2)
    quad = [np.array([HUB, HUB - a_]), d*ARM_REACH - p*ARM/2, d*ARM_REACH + p*ARM/2, np.array([HUB - a_, HUB])]
    prof = []
    for k in range(4):
        c, s_ = math.cos(k*math.pi/2), math.sin(k*math.pi/2)
        prof += [L((c*q[0] - s_*q[1], s_*q[0] + c*q[1])) for q in quad]
    hp = HUB - hs["g"] - hs["w"] - 0.003; rho = 0.030
    def hub_plate(piece): return [(np.array([(-hp + rho, -hp + rho), (hp - rho, -hp + rho), (hp - rho, hp - rho), (-hp + rho, hp - rho)]), rho)]
    for n in (X, -X, Y, -Y, Z, -Z):
        u, v, _ = RP.face_frame(n); n = np.asarray(n, float)
        if not lod:
            S.merge(mesh, S.prism(prof, H - 0.036, H, (u, v, n, O3), hs, caps=("none", "auto"), cap_custom=(None, hub_plate))); continue
        # LOD1: chamfered hub box + two plain diagonal bars (closed boxes, top 6 mm below the contact plane, ends buried in the frame)
        hl = dict(A); hl.update(g=0.002, w=0.008); S.merge(mesh, S.prism(rect(-HUB, HUB, -HUB, HUB), H - 0.036, H, (u, v, n, O3), hl, lod=True))
        for dd, pp in ((d, p), (p, -d)):
            c8 = [(sa*ARM_REACH*dd[0] + sb*ARM/2*pp[0])*u + (sa*ARM_REACH*dd[1] + sb*ARM/2*pp[1])*v + z_*n for sa in (-1, 1) for sb in (-1, 1) for z_ in (H - 0.030, H - 0.006)]
            for q in ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)):
                P_ = [c8[i] for i in q]; fn = S.unit(np.cross(P_[1] - P_[0], P_[2] - P_[0])); ctr = sum(P_)/4 - sum(c8)/8
                fn = fn if fn @ ctr > 0 else -fn; mesh.face(P_, [fn]*4, "frame", [(x @ u, x @ v) for x in P_], fn)
    return mesh.result(st)

WAVE4 = {"Arch": arch, "CoveEdge": cove_edge, "TrussCube": truss_cube}

# approved solid of every block as a union of convex parts (optionally minus cylinders) - used by the containment tests
def _bx(x0, x1, y0, y1, z0, z1): return {"hull": corners(x0, x1, y0, y1, z0, z1)}
SOLID = {
 "InnerCorner": [{"hull": INNER}], "HalfWedge": [{"hull": WAVE1["HalfWedge"].points}], "LongWedge": [{"hull": corners(-H, H, -G - H, H, -H, -H) + [(x, H, H) for x in (-H, H)]}],
 "WideWedge": [{"hull": corners(-H, G + H, -H, H, -H, -H) + [(x, H, H) for x in (-H, G + H)]}], "Pyramid": [{"hull": WAVE1["Pyramid"].points}],
 "HexColumn": [{"hull": WAVE1["HexColumn"].points}], "OctColumn": [{"hull": WAVE1["OctColumn"].points}],
 "Plate1x2": [_bx(-H, G + H, -H, H, -H, HT)], "Plate2x2": [_bx(-H, G + H, -G - H, H, -H, HT)], "Beam2": [_bx(-H, G + H, -H, H, -H, H)], "Beam3": [_bx(-H, 2*G + H, -H, H, -H, H)],
 "Slab2x2": [_bx(-H, G + H, -G - H, H, -H, H)], "BigCube2": [_bx(-H, G + H, -G - H, H, -H, G + H)],
 "RoundEdge": [{"hull": [(x, H - R2*math.cos(math.radians(90*i/16)), -H + R2*math.sin(math.radians(90*i/16))) for x in (-H, H) for i in range(17)] + [(x, H, -H) for x in (-H, H)]}],
 "RoundCorner": [{"hull": [(-H, H, -H)] + [(-H + R2*math.sin(math.radians(90*i/8))*math.cos(math.radians(90*j/8)), H - R2*math.sin(math.radians(90*i/8))*math.sin(math.radians(90*j/8)), -H + R2*math.cos(math.radians(90*i/8)))
                           for i in range(9) for j in range(9)]}],
 "RoundTop": [{"hull": [(x, y, -H) for x in (-H, H) for y in (-H, H)] + [(x, H*math.cos(math.radians(180*i/20)), H*math.sin(math.radians(180*i/20))) for x in (-H, H) for i in range(21)]}],
 "Cylinder": [{"hull": [(H*math.cos(2*math.pi*i/48), H*math.sin(2*math.pi*i/48), z) for i in range(48) for z in (-H, H)]}],
 "Dome": [_bx(-H, H, -H, H, -H, DOME_Z0), {"hull": [((H - 0.012)*math.cos(t)*math.cos(p), (H - 0.012)*math.cos(t)*math.sin(p), DOME_Z0 - 0.010 + (H - 0.012 - DOME_Z0 + 0.010)*math.sin(t))
                                               for t in np.linspace(0, math.pi/2, 17) for p in np.linspace(0, 2*math.pi, 48, endpoint=False)]}],
 "Cone": [{"hull": [(H*math.cos(2*math.pi*i/48), H*math.sin(2*math.pi*i/48), z) for i in range(48) for z in (-H, -H + CONE_RIM)] + [(0, 0, H)]}],
 "Arch": [{"hull": corners(-H, H, -H, H, -H, H), "minus": [{"axis": [0, 1, 0], "point": [0, 0, -H], "r": ARCH_R}]}],
 "CoveEdge": [{"hull": corners(-H, H, -H, H, -H, H), "minus": [{"axis": [1, 0, 0], "point": [0, -H, H], "r": COVE_R}]}],
 "TrussCube": [_bx(-H, H, -H, H, -H, H)],
 "Step": [_bx(-H, H, -H, H, -H, HT), _bx(-H, H, 0.0, H, -H, H)],
 "CornerL3": [_bx(-H, G + H, -H, H, -H, H), _bx(-H, H, -G - H, H, -H, H)], "Tee4": [_bx(-G - H, G + H, -H, H, -H, H), _bx(-H, H, -G - H, H, -H, H)],
}
WAVES = [WAVE1, WAVE2, WAVE3, WAVE4]
WAVE_OF = {k: i + 1 for i, w in enumerate(WAVES) for k in w}
ALL = {k: v for w in WAVES for k, v in w.items()}
def build(kind, lod=False): return ALL[kind](lod)
