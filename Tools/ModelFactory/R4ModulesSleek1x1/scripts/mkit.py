"""RoboRougelike - R4 Sleek module library 1x1x1: composition kit.
Builds module meshes from the SAME generators as the approved-style A_Sleek building blocks (#1232 pilot r4panel, #1235 r4sleek / r4convex,
imported unchanged): every large volume is a 'plate on a constant-width bevelled frame' solid, so panels, bevel width and frame piping match
the blocks. Small technical parts are simple lathed / chamfered solids painted into one zone.
A module = a list of CLOSED shells (they may interpenetrate, they never share vertices), all inside the 0.52 m block envelope (+-0.26 m).
Blender axes: X right, Y back (-Y forward), Z up. Units: metres."""
import math, os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); PKG = os.path.dirname(HERE); MF = os.path.dirname(PKG)
sys.path.insert(0, os.path.join(MF, "R4BlocksSleek1235", "scripts"))
import paths25                                   # noqa: adds the #1232 pilot scripts to sys.path
import r4panel as RP, r4convex as RC, r4sleek as S
from styles import STYLES, R41

G, H = 0.55, 0.26
X = np.array([1.0, 0, 0]); Y = np.array([0, 1.0, 0]); Z = np.array([0, 0, 1.0]); O3 = np.zeros(3)
L = S.P_line
DIRS = {"right": X, "left": -X, "up": Z, "down": -Z, "forward": -Y, "back": Y}
# paint zones (material slots). Base / Secondary / Trim are the three zones of the A_Sleek blocks; Metal and Glow are PROPOSALS for modules.
ZONE = {"plate": "R4P_Base", "plate_wall": "R4P_Trim", "frame": "R4P_Secondary", "bevel": "R4P_Secondary", "corner": "R4P_Secondary",
        "base": "R4P_Base", "sec": "R4P_Secondary", "dark": "R4P_Trim", "metal": "R4P_Metal", "glow": "R4P_Glow"}
ZONE_ORDER = ["R4P_Base", "R4P_Secondary", "R4P_Trim", "R4P_Metal", "R4P_Glow"]
ZONE_DEFAULT = {"R4P_Base": "Armor_Plate_Light", "R4P_Secondary": "Armor_Gunmetal", "R4P_Trim": "Armor_Dark", "R4P_Metal": "Steel_Light"}

LOD = False          # set by build(): LOD1 = no plates / single chamfers / halved segment counts


def style(s=1.0, **kw):
    """A_Sleek style scaled for a part whose smallest dimension is ~0.16*s m (s = 1: exactly the block style)"""
    A = dict(STYLES["A"]); s = max(0.2, min(1.0, s))
    A.update(w=0.020*s, rho=0.040*s, margin=max(0.006*s, 0.003), chamfer=max(0.004*s, 0.002), g=0.004 if s >= 0.6 else 0.003,
             seg=4 if s >= 0.8 else (3 if s >= 0.5 else 2), cseg=4 if s >= 0.8 else (3 if s >= 0.5 else 2), seam_h=0.006, min_plate=max(0.05*s, 0.03))
    A.update(kw); return A


def auto_s(*dims): return max(0.2, min(1.0, min(dims)/0.16))


# ------------------------------------------------------------------------------------------------ mesh utilities
def xf(m, R=None, t=None):
    R = np.eye(3) if R is None else np.asarray(R, float); t = np.zeros(3) if t is None else np.asarray(t, float)
    m["V"] = m["V"] @ R.T + t
    for f in m["faces"]: f["n"] = [np.asarray(n) @ R.T for n in f["n"]]
    return m


def mirror(m, axis=0):
    m["V"] = m["V"].copy(); m["V"][:, axis] *= -1
    for f in m["faces"]:
        nn = [np.array(n, float) for n in f["n"]]
        for n in nn: n[axis] *= -1
        f["v"] = list(f["v"])[::-1]; f["n"] = nn[::-1]; f["uv"] = list(f["uv"])[::-1]
    return m


def copy(m): return dict(V=m["V"].copy(), faces=[dict(v=list(f["v"]), n=[np.array(n) for n in f["n"]], uv=[np.array(u) for u in f["uv"]], group=f["group"]) for f in m["faces"]])


def paint(m, how):
    """how: group name (every face) or {old group: new group}"""
    for f in m["faces"]:
        if isinstance(how, str): f["group"] = how
        elif f["group"] in how: f["group"] = how[f["group"]]
    return m


def rot(axis, deg):
    a = np.asarray(axis, float); a = a/np.linalg.norm(a); c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg)); K = np.array([[0, -a[2], a[1]], [a[2], 0, -a[0]], [-a[1], a[0], 0]])
    return np.eye(3) + s*K + (1 - c)*(K @ K)


def R_to(axis):
    """rotation taking +Z to axis"""
    n = np.asarray(axis, float); n = n/np.linalg.norm(n)
    if abs(n[2]) > 0.999999: return np.eye(3) if n[2] > 0 else np.diag([1.0, -1.0, -1.0])
    u = np.cross(Z, n); u /= np.linalg.norm(u); v = np.cross(n, u)
    return np.stack([u, v, n], axis=1)


def _clean(m):
    out = dict(V=np.asarray(m["V"], float), faces=[])
    for f in m["faces"]:
        uv = f.get("uv"); uv = [np.zeros(2)]*len(f["v"]) if uv is None else uv
        out["faces"].append(dict(v=list(f["v"]), n=[np.asarray(n, float) for n in f["n"]], uv=[np.asarray(u, float) for u in uv], group=f["group"]))
    return out


# ------------------------------------------------------------------------------------------------ module under construction
class Module:
    def __init__(self): self.parts = []; self.ports = []; self.empties = []; self.nodes = {}; self.node = "Body"
    def mesh(self):
        V = []; F = []; off = 0
        for p in self.parts:
            V.append(p["V"]); F += [dict(v=[i + off for i in f["v"]], n=f["n"], uv=f["uv"], group=f["group"]) for f in p["faces"]]; off += len(p["V"])
        return dict(V=np.concatenate(V), faces=F)
    def node_mesh(self, name):
        V = []; F = []; off = 0
        for p in self.parts:
            if p["node"] != name: continue
            V.append(p["V"]); F += [dict(v=[i + off for i in f["v"]], n=f["n"], uv=f["uv"], group=f["group"]) for f in p["faces"]]; off += len(p["V"])
        return dict(V=np.concatenate(V), faces=F) if V else None


CUR = None


def begin():
    global CUR
    CUR = Module(); return CUR


def add(m, grp=None, R=None, at=None):
    m = _clean(m)
    if grp is not None: paint(m, grp)
    if R is not None or at is not None: xf(m, R, at)
    m["node"] = CUR.node; CUR.parts.append(m); return m


def node(name="Body", pivot=(0, 0, 0), anim=None):
    """following parts belong to a separate child object (movable part) with its own pivot. anim (PROPOSAL, preview clip only):
    dict(type='spin'|'swing'|'slide', axis=(x, y, z), amount=degrees | metres, frames=48)"""
    CUR.node = name
    if name != "Body": CUR.nodes[name] = dict(pivot=[float(x) for x in pivot], anim=anim)


def frame(n):
    """face frame of the approved pipeline (build_blocks.frame): u, v, n"""
    n = np.asarray(n, float); u = np.cross(Z, n) if abs(n[2]) < 0.9 else np.cross(X, n); u = u/np.linalg.norm(u); return u, np.cross(n, u), n


class group:
    """with group(R, s, at, lod): every part added inside is scaled, rotated and moved together (optionally built at LOD1 detail)"""
    def __init__(self, R=None, s=1.0, at=(0, 0, 0), lod=False): self.R = np.eye(3) if R is None else np.asarray(R, float); self.s = s; self.at = np.asarray(at, float); self.lod = lod
    def __enter__(self):
        global LOD
        self.i0 = len(CUR.parts); self.was = LOD; LOD = LOD or self.lod; return self
    def __exit__(self, *a):
        global LOD
        LOD = self.was
        for p in CUR.parts[self.i0:]:
            p["V"] = (p["V"]*self.s) @ self.R.T + self.at
            for f in p["faces"]: f["n"] = [n @ self.R.T for n in f["n"]]; f["uv"] = [u*self.s for u in f["uv"]]


def port(d, kind="full", half=None, c=(0.0, 0.0, 0.0)):
    """declared contact face of the module (cell 0,0,0). half = half size of the tested patch in grid units (approved ruler: full = 0.47);
    c = any 3D point whose projection is the patch centre"""
    u, v, n = frame(DIRS[d]); c = np.asarray(c, float)
    CUR.ports.append(dict(dir=d, cell=[0, 0, 0], kind=kind, half=[half or (0.47 if kind == "full" else 0.24)]*2, offset=[round(float(c @ u)/G, 5), round(float(c @ v)/G, 5)]))


def empty(name, pos, d=(0, 0, 1)): CUR.empties.append(dict(name=name, pos=[float(x) for x in pos], dir=[float(x) for x in d]))


# ------------------------------------------------------------------------------------------------ sleek (plated) solids
AXES = {"z": (X, Y, Z), "x": (Y, Z, X), "y": (X, Z, -Y)}       # profile axes (a, b) + extrusion t, right-handed


def rect(a0, a1, b0, b1): return [L((a0, b0)), L((a1, b0)), L((a1, b1)), L((a0, b1))]


def prof(pts):
    out = []
    for p in pts:
        if isinstance(p, list): out += p
        elif isinstance(p, dict): out.append(p)
        else: out.append(L(p))
    return out


def prism(pts, t0, t1, axis="z", s=None, org=(0, 0, 0), plates=None, caps=("auto", "auto"), grp=None, R=None, at=None, **kw):
    """sleek extruded profile. pts: CCW 2D points / S.P_arc lists in the (a, b) plane of the axis. plates: None = every run, or set of edge indices."""
    P = prof(pts); q = np.array([v["p"] for v in P]); ext = q.max(0) - q.min(0)
    st = style(auto_s(ext[0], ext[1], t1 - t0) if s is None else s, **kw)
    ax = AXES[axis] if isinstance(axis, str) else axis
    ep = None if plates is None else (lambda i: i in plates)
    m = S.prism(P, t0, t1, (ax[0], ax[1], ax[2], np.asarray(org, float)), st, edge_plate=ep, caps=caps, lod=LOD)
    return add(m, grp, R, at)


def box(x0, x1, y0, y1, z0, z1, s=None, faces=None, grp=None, R=None, at=None, **kw):
    """sleek box. faces: None = plates everywhere, or string of plated faces out of 'x X y Y z Z' (lower = negative side)"""
    pl = None; caps = ("auto", "auto")
    if faces is not None:
        pl = {i for i, c in enumerate("yXYx") if c in faces}; caps = ("auto" if "z" in faces else "none", "auto" if "Z" in faces else "none")
    return prism([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], z0, z1, "z", s if s is not None else auto_s(x1 - x0, y1 - y0, z1 - z0), plates=pl, caps=caps, grp=grp, R=R, at=at, **kw)


def srev(pts, N=24, s=1.0, plate_ok=None, grp=None, R=None, at=None, **kw):
    """sleek solid of revolution about +Z: OPEN CCW (r, z) polyline, bottom axis point -> top axis point"""
    m = S.revolve(prof(pts), max(6, N//2) if LOD else N, style(s, **kw), lod=LOD, plate_ok=plate_ok)
    return add(m, grp, R, at)


def hull(points, s=1.0, plates=True, grp=None, R=None, at=None, **kw):
    st = style(s, **kw); pts = [tuple(float(c) for c in p) for p in points]
    if LOD: m = RC.lod_build(pts, st)
    else: m = RC.build(pts, st, plate_faces=None if plates is True else ((lambda n: False) if plates is False else plates))
    for f in m["faces"]:
        P = m["V"][f["v"]]; f["uv"] = [(p[0] + 0.5*p[2], p[1] + 0.5*p[2]) for p in P]
    return add(m, grp, R, at)


def sbox(x0, x1, y0, y1, z0, z1, grp="dark", b=None, R=None, at=None):
    """plain single-zone box with one small chamfer (technical inserts, lenses, light bars)"""
    b = b if b is not None else min(0.006, 0.2*min(x1 - x0, y1 - y0, z1 - z0))
    st = style(1.0, g=0.0, w=b, seg=1, cseg=1)
    m = RC.build([(x, y, z) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)], st, plate_faces=lambda n: False)
    for f in m["faces"]:
        P = m["V"][f["v"]]; f["uv"] = [(p[0] + 0.5*p[2], p[1] + 0.5*p[2]) for p in P]
    return add(m, grp, R, at)


# ------------------------------------------------------------------------------------------------ simple lathed solids (one zone per segment)
def arc2(c, r, a0, a1, n):
    return [(c[0] + r*math.cos(math.radians(a0 + (a1 - a0)*i/n)), c[1] + r*math.sin(math.radians(a0 + (a1 - a0)*i/n))) for i in range(n + 1)]


def lathe(pts, grp="sec", N=20, closed=False, crease=32.0, R=None, at=None, phase=0.0):
    """revolve an (r, z) polyline about +Z. CCW: bottom axis point -> outside -> top axis point (or a closed CCW loop off the axis).
    grp: one zone or a list (one per segment). Vertex normals are smoothed where neighbouring segments meet under the crease angle."""
    P = [np.asarray(p, float) for p in pts]; n = len(P); segs = list(range(n if closed else n - 1))
    def sn(i):
        d = P[(i + 1) % n] - P[i]; return S.unit(np.array([d[1], -d[0]]))
    nrm = {i: sn(i) for i in segs}; cc = math.cos(math.radians(crease)); sec = []
    for k, i in enumerate(segs):
        na = nb = nrm[i]; ip = (i - 1) % n if closed else i - 1; inx = (i + 1) % n if closed else i + 1
        if ip in nrm and nrm[ip] @ nrm[i] > cc: na = S.unit(nrm[ip] + nrm[i])
        if inx in nrm and inx != i and nrm[inx] @ nrm[i] > cc: nb = S.unit(nrm[inx] + nrm[i])
        sec.append(((P[i], na), (P[(i + 1) % n], nb), grp[k] if isinstance(grp, (list, tuple)) else grp))
    mesh = S.Mesh(); S.revolve_section(mesh, sec, (max(6, N//2) if N > 8 else N) if LOD else N, phase)
    return add(dict(V=np.array(mesh.V), faces=mesh.F), None, R, at)


def cyl(r, z0, z1, grp="sec", b=0.004, N=20, R=None, at=None):
    b = min(b, r*0.5, (z1 - z0)*0.4)
    return lathe([(0, z0), (r - b, z0), (r, z0 + b), (r, z1 - b), (r - b, z1), (0, z1)], grp, N, R=R, at=at)


def rod(p0, p1, r, grp="metal", b=0.003, N=10):
    p0 = np.asarray(p0, float); p1 = np.asarray(p1, float); d = p1 - p0; ln = float(np.linalg.norm(d))
    return cyl(r, 0.0, ln, grp, b, N, R=R_to(d), at=p0)


def tube(r0, r1, z0, z1, grp="sec", b=0.004, N=20, R=None, at=None):
    """hollow ring / pipe section (closed loop off the axis)"""
    b = min(b, (r1 - r0)*0.4, (z1 - z0)*0.4)
    return lathe([(r0, z0 + b), (r0 + b, z0), (r1 - b, z0), (r1, z0 + b), (r1, z1 - b), (r1 - b, z1), (r0 + b, z1), (r0, z1 - b)], grp, N, closed=True, R=R, at=at)


def torus(Rm, r, grp="sec", N=24, n=8, R=None, at=None):
    n = max(4, n//2) if LOD else n
    return lathe([(Rm + r*math.cos(2*math.pi*i/n), r*math.sin(2*math.pi*i/n)) for i in range(n)], grp, N, closed=True, crease=80.0, R=R, at=at)


def ball(r, grp="glow", N=16, n=8, R=None, at=None, squash=1.0):
    n = max(4, n//2) if LOD else n
    return lathe([(r*math.cos(a), squash*r*math.sin(a)) for a in np.linspace(-math.pi/2, math.pi/2, n + 1)], grp, N, crease=80.0, R=R, at=at)


def cone(r0, r1, z0, z1, grp="sec", N=20, b=0.003, R=None, at=None):
    pts = [(0, z0)] + ([(r0 - b, z0), (r0, z0 + b)] if r0 > 2*b else [(max(r0, 1e-4), z0)]) + ([(r1, z1 - b), (r1 - b, z1)] if r1 > 2*b else [(max(r1, 1e-4), z1)]) + [(0, z1)]
    return lathe(pts, grp, N, R=R, at=at)


def slab(pts, t0, t1, axis="z", grp="glow", org=(0, 0, 0), R=None, at=None):
    """plain extruded polygon (CCW, may be concave), one zone, hard edges: glyphs, fins, blades"""
    import geo2d as G2
    ax = AXES[axis] if isinstance(axis, str) else axis; ea, eb, et = [np.asarray(a, float) for a in ax]; org = np.asarray(org, float)
    P = [np.asarray(p, float) for p in pts]; n = len(P); mesh = S.Mesh()
    if G2.area2(P) < 0: P = P[::-1]
    def to3(p, t): return org + p[0]*ea + p[1]*eb + t*et
    P2, tris = G2.triangulate(np.array(P))
    for t in tris:
        mesh.face([to3(P2[i], t1) for i in t], [et]*3, grp, [tuple(P2[i]) for i in t], et)
        mesh.face([to3(P2[i], t0) for i in t], [-et]*3, grp, [tuple(P2[i]) for i in t], -et)
    u = 0.0
    for i in range(n):
        a, b = P[i], P[(i + 1) % n]; d = b - a; ln = float(np.linalg.norm(d)); nn = (d[1]*ea - d[0]*eb)/ln
        mesh.face([to3(a, t0), to3(b, t0), to3(b, t1), to3(a, t1)], [nn]*4, grp, [(u, t0), (u + ln, t0), (u + ln, t1), (u, t1)], nn); u += ln
    return add(dict(V=np.array(mesh.V), faces=mesh.F), None, R, at)


def ngon(n, r, phase=0.0): return [(r*math.cos(math.radians(phase) + 2*math.pi*i/n), r*math.sin(math.radians(phase) + 2*math.pi*i/n)) for i in range(n)]


# ------------------------------------------------------------------------------------------------ module build + statistics
def build(fn, lod=False):
    global LOD
    LOD = lod; M = begin()
    try: fn()
    finally: LOD = False
    m = M.mesh(); m["ports"] = M.ports; m["empties"] = M.empties; m["shells"] = len(M.parts); m["nodes"] = M.nodes; m["by_node"] = {n: M.node_mesh(n) for n in ["Body"] + list(M.nodes)}; m["part_stats"] = [RP.mesh_stats(p) for p in M.parts]
    return m


def stats(m):
    st = RP.mesh_stats(dict(V=m["V"], faces=m["faces"])); bad = [i for i, p in enumerate(m["part_stats"]) if not p["ok"]]
    st["bad_shells"] = bad; st["ok"] = st["ok"] and not bad; st["shells"] = m["shells"]
    st["bounds"] = [[round(float(x), 5) for x in m["V"].min(0)], [round(float(x), 5) for x in m["V"].max(0)]]
    return st
