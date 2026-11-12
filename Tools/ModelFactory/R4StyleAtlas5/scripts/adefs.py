"""#1239 asset registry of the atlas: every canonical asset as ONE style-neutral definition that the kit interprets in five styles."""
import os, sys, json, importlib.util
import numpy as np
import akit as K
sys.modules["mkit"] = K                         # the #1238 module definitions are executed against the style-aware kit (source file unchanged)
_spec = importlib.util.spec_from_file_location("modules_1238", os.path.join(K.MF, "R4ModulesSleek1x1", "scripts", "modules.py")); MD = importlib.util.module_from_spec(_spec); _spec.loader.exec_module(MD)
H = K.H
ASSETS = {}
def reg(cid, fn, family, env=None, glow=(0.2, 0.8, 1.0), **kw): ASSETS[cid] = dict(id=cid, fn=fn, family=family, env=env or [[-H]*3, [H]*3], glow=list(glow), **kw)
for _n, _d in MD.ALL.items():
    _cid = ("R4D_" if _d["group"] == "T" else "R4X_") + _n
    _fam = "world_trap" if _d["group"] == "T" else ("deployer" if _d["group"] == "F" or _n in ("DeployableShieldGenerator", "DeployableAmplifier") else "module")
    reg(_cid, _d["fn"], _fam, env=_d.get("env"), glow=_d["glow"], source="#1238", interface="PROPOSAL")

# =================================================================================================== building blocks (interface: approved PR #963)
import paths, shapes as PSH, shapes25 as SH25
INV = {i["CanonicalPartId"]: i for i in json.load(open(os.path.join(K.PKG, "inventory", "canonical_inventory.json")))["items"]}
from akit import box, sbox, prism, srev, hull, lathe, cyl, rod, tube, torus, ball, cone, slab, node, rot, R_to, X, Y, Z


def _block_fn(kind):
    def fn():
        def make():
            if K.BASE.LOD: K._SAFE = 2                                 # LOD1 of every style keeps the approved-ruler chamfer on the contact faces
            st = K.style(1.0); K._SAFE = False if K.BASE.LOD else K._SAFE; SH25.A.clear(); SH25.A.update(st); SH25.A.update(seam_h=0.006, min_plate=0.05)
            if kind in PSH.SOLIDS: return K.BASE.add(K.RP.lod_build(PSH.SOLIDS[kind], st) if K.BASE.LOD else K.RP.build(PSH.SOLIDS[kind], st))
            return K.BASE.add(SH25.build(kind, K.BASE.LOD or K.STYLE == "C"))          # C: seamed multi-cell solids use the plain rolled-edge build
        n0 = len(K.BASE.CUR.parts); m = K._solid(make)
        if not K.BASE.LOD and K.STYLE != "A":                       # the style bevel must not cost the approved contact: otherwise approved-ruler widths
            import raycast
            T = np.array([[m["V"][f["v"][0]], m["V"][f["v"][i]], m["V"][f["v"][i + 1]]] for f in m["faces"] for i in range(1, len(f["v"]) - 1)]); bad = False
            for p in INV["R4B_" + kind]["ports"]:
                r = raycast.patch_test(T, p["cell"], p["dir"], p["offset"], p["half"])
                if not (r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65)): bad = True; break
            if bad:
                del K.BASE.CUR.parts[n0:]; K._SAFE = 2
                try: m = make(); K._post_solid(m); K.FALLBACK.append((kind, K.STYLE, "safe-contact"))
                finally: K._SAFE = False
        for f in m["faces"]:
            if not np.any(f["uv"]): P = m["V"][f["v"]]; f["uv"] = [np.array((p[0] + 0.5*p[2], p[1] + 0.5*p[2])) for p in P]
    return fn


def _cells_env(cells):
    c = np.array(cells, float)*K.G; return [(c.min(0) - H).tolist(), (c.max(0) + H).tolist()]


for _cid, _it in INV.items():
    if _it["family"] == "block":
        _k = _cid[4:]; reg(_cid, _block_fn(_k), "block", env=_cells_env(_it["occupied_cells"]), ports=_it["ports"], planes=[(K.DIRS[p["dir"]].tolist(), float(np.dot(np.array(p["cell"])*K.G, K.DIRS[p["dir"]]) + H)) for p in _it["ports"]], body=_cid, source="#963", interface="APPROVED", kind=_k, ref=("blocks", _cid))

# =================================================================================================== assets with an approved rig: geometry is re-authored per style on the SOURCE node names / pivots;
# sockets and the functional animation are taken from the approved GLB in Blender (see abuild.py)
def _b(v): return [v[0], -v[2], v[1]]                 # file axes -> Blender axes
def _env_of(cid, pad=0.002):
    lo, hi = INV[cid]["bounds_file_m"]; a, b = _b(lo), _b(hi); return [[min(a[i], b[i]) - pad for i in range(3)], [max(a[i], b[i]) + pad for i in range(3)]]


def r4w_basic():
    box(-0.255, 0.255, -0.255, 0.255, -H, -0.175); K.port("down")
    srev([(0, -0.175), (0.21, -0.175), (0.19, 0.014), (0, 0.014)], 24, 0.6)
    for a in (45, 135, 225, 315): sbox(0.17, 0.235, -0.03, 0.03, -0.175, -0.06, "dark", R=rot(Z, a))
    node("Yaw", (0, 0, 0.135), parent="GunPivot")
    cyl(0.186, 0.02, 0.055, "sec", 0.008, 24)
    for sx in (-1, 1): box(sx*0.165 - 0.024, sx*0.165 + 0.024, -0.11, 0.13, 0.05, 0.218, s=0.3)
    box(-0.12, 0.12, 0.13, 0.254, 0.05, 0.17, s=0.4)
    sbox(-0.08, 0.08, 0.246, 0.255, 0.08, 0.14, "glow", b=0.002)
    node("Pitch", (0, 0, 0.135), parent="Yaw")
    box(-0.125, 0.125, -0.20, 0.075, 0.088, 0.205, s=0.45)
    cyl(0.048, 0.0, 0.20, "sec", 0.008, 16, R=R_to(-Y), at=(0, -0.20, 0.135))
    cyl(0.05, -0.138, 0.138, "dark", 0.006, 14, R=R_to(X), at=(0, 0, 0.135))
    sbox(-0.05, 0.05, -0.16, 0.04, 0.205, 0.234, "dark")
    cyl(0.022, 0.0, 0.012, "glow", 0.002, 10, R=R_to(-Y), at=(0.09, -0.205, 0.175))
    node("Barrel", (0, 0, 0.135), parent="Pitch")
    cyl(0.028, 0.0, 0.50, "metal", 0.004, 12, R=R_to(-Y), at=(0, -0.105, 0.135))
    cyl(0.037, 0.0, 0.06, "dark", 0.005, 12, R=R_to(-Y), at=(0, -0.558, 0.135))
    node("MuzzleFlash", (0, -0.624, 0.135), parent="Pitch")
    lathe([(0, 0.0), (0.045, 0.03), (0.02, 0.06), (0, 0.13)], "glow", 8, crease=10, R=R_to(-Y), at=(0, -0.624, 0.135))
    node()


def r4m_wheel_m_side_offroad():
    box(-H, -0.205, -0.25, 0.25, -0.25, 0.25); K.port("left")
    srev([(0, 0.0), (0.16, 0.0), (0.11, 0.14), (0, 0.14)], 20, 0.5, R=R_to(X), at=(-0.205, 0, 0))
    cyl(0.05, 0.0, 0.12, "metal", 0.006, 12, R=R_to(X), at=(-0.065, 0, 0))
    for a in (0, 90, 180, 270): sbox(-0.2, -0.08, -0.02, 0.02, 0.10, 0.18, "dark", R=rot(X, a + 45))
    node("Rotor", (0.022, 0, 0), parent=None)
    w0, w1 = -0.148 - 0.022, 0.192 - 0.022
    with K.group(R=R_to(X), at=(0.022, 0, 0)):
        lathe([(0.20, w0 + 0.03), (0.33, w0), (0.365, w0 + 0.02), (0.365, w1 - 0.02), (0.33, w1), (0.20, w1 - 0.03)], "dark", 28, closed=True, crease=40)
        lathe([(0, w0 + 0.05), (0.21, w0 + 0.05), (0.23, w0 + 0.035), (0.23, w1 - 0.035), (0.21, w1 - 0.05), (0, w1 - 0.02)], ["base", "sec", "sec", "sec", "base"], 20)
        cyl(0.07, w1 - 0.03, w1 - 0.002, "metal", 0.008, 12)
        for i in range(5): cyl(0.016, w1 - 0.022, w1 - 0.008, "glow" if i == 0 else "metal", 0.003, 6, at=rot(Z, 72*i) @ np.array([0.14, 0, 0]))
        for i in range(14): sbox(0.355, 0.388, -0.035, 0.035, w0 + 0.03, w1 - 0.03, "dark", b=0.006, R=rot(Z, 360/14*i))
    node()


def r4c_rod_standard():
    for (y0, y1) in ((0.07, H), (-1.36, -1.17)): box(-0.113, 0.113, y0, y1, -0.111, 0.113, s=0.3)
    cyl(0.058, 0.0, 1.26, "sec", 0.006, 14, R=R_to(-Y), at=(0, 0.08, 0))
    for y in (0.0, -1.1): cyl(0.098, -0.07, 0.07, "base", 0.012, 16, R=R_to(Y), at=(0, y, 0)); torus(0.098, 0.012, "dark", 16, 6, R=R_to(Y), at=(0, y, 0))
    for y in (-0.35, -0.75): cyl(0.072, -0.03, 0.03, "dark", 0.006, 12, R=R_to(Y), at=(0, y, 0))
    sbox(-0.02, 0.02, -0.72, -0.38, 0.05, 0.066, "glow", b=0.003)


def r4m_hover_engine():
    box(-H, H, -H, H, 0.205, H); K.port("up")
    lathe([(0.20, -0.30), (0.215, -0.317), (0.245, -0.31), (0.26, -0.26), (0.26, -0.04), (0.235, 0.0), (0.205, -0.04), (0.21, -0.26)], ["glow", "sec", "sec", "base", "sec", "dark", "dark", "dark"], 28, closed=True)
    for a in (45, 135, 225, 315): rod(rot(Z, a) @ np.array([0.235, 0, -0.02]), rot(Z, a) @ np.array([0.10, 0, 0.21]), 0.022, "metal", 0.004, 8)
    cyl(0.075, -0.19, 0.21, "sec", 0.01, 16)
    node("Rotor", (0, 0, -0.212), parent=None)
    cyl(0.06, -0.246, -0.197, "dark", 0.006, 14)
    for i in range(7): sbox(0.05, 0.196, -0.03, 0.03, -0.007, 0.007, "metal", R=rot(Z, 360/7*i) @ rot(X, 20), at=(0, 0, -0.221))
    for i, (y, hx) in enumerate(((-0.164, 0.10), (-0.082, 0.185), (0.0, 0.214), (0.082, 0.185), (0.164, 0.10))):
        node("Vane_%d" % (i + 1), (0, y, -0.293), parent=None)
        sbox(-hx, hx, y - 0.032, y + 0.032, -0.2975, -0.2885, "base", b=0.003)
    node("PulseRing", (0, 0, -0.337), parent=None); torus(0.20, 0.012, "glow", 24, 6, at=(0, 0, -0.337 + 0.014))
    node("ThrustGlow", (0, 0, -0.324), parent=None); cyl(0.19, -0.322, -0.316, "glow", 0.002, 20)
    node()


def _iface(cid, fn, lib, family, names, body, fx=()):
    it = INV[cid]; reg(cid, fn, family, env=_env_of(cid), planes=[(_b(p["normal"]), float(np.dot(_b(p["position_m"]), _b(p["normal"])))) for p in it["ports"]] if family == "connector" else None, body=body, names=names, fx=list(fx), ref=(lib, cid), source="#%d" % it["source"]["pr"], interface="APPROVED rig / sockets (from source GLB)",
                       source_clips=it["source_clips"], hierarchy=it.get("hierarchy"), glow=(1.0, 0.45, 0.10))
_iface("R4W_Basic", r4w_basic, "weapons", "weapon", {"Yaw": "R4W_Basic_Yaw", "Pitch": "R4W_Basic_Pitch", "Barrel": "R4W_Basic_Barrel", "MuzzleFlash": "MuzzleFlash"}, "R4W_Basic_Base", fx=("MuzzleFlash",))
_iface("R4M_WheelM_Side_Offroad", r4m_wheel_m_side_offroad, "wheels", "wheel", {"Rotor": "R4M_WheelM_Side_Offroad_Rotor"}, "R4M_WheelM_Side_Offroad_Mount")
_iface("R4C_ROD_STANDARD", r4c_rod_standard, "connectors", "connector", {}, "R4C_ROD_STANDARD")
_iface("R4M_HoverEngine", r4m_hover_engine, "movement", "movement", dict({"Rotor": "R4M_HoverEngine_Rotor", "PulseRing": "Hover_PulseRing", "ThrustGlow": "Hover_ThrustGlow"}, **{"Vane_%d" % i: "R4M_HoverEngine_Vane_%d" % i for i in range(1, 6)}),
       "R4M_HoverEngine_Housing", fx=("PulseRing", "ThrustGlow"))
PILOT = ["R4B_Cube", "R4B_RoundEdge", "R4C_ROD_STANDARD", "R4W_Basic", "R4M_WheelM_Side_Offroad", "R4X_Gyroscope", "R4M_HoverEngine", "R4X_Battery", "R4D_ExplosiveTrap", "R4X_ExplosiveTrapDeployer", "R4X_FlyingDroneDeployer", "R4X_OffensiveTurretDeployer"]

# =================================================================================================== 20 connectors (#1186): generic node / member builder driven by the SOURCE connector manifest
def _conn_fn(cid):
    it = INV[cid]; strut = "STRUT" in cid; man = json.load(open(paths.ref_file("connectors", cid, cid + "_connector_manifest.json")))
    nodes = {k: np.array(_b(v), float)*K.G for k, v in it["nodes"].items()}; cell_of = {tuple(v): k for k, v in it["nodes"].items()}
    def fn():
        for name, c in nodes.items():
            if strut: box(c[0] - H, c[0] + H, c[1] - H, c[1] + H, c[2] - H, c[2] + H); continue
            h = 0.113; lo = c - h; hi = c + h
            for p in it["ports"]:
                if cell_of.get(tuple(p["cell"])) != name: continue
                n = np.array(_b(p["normal"]), float); ax = int(np.argmax(np.abs(n)))
                if n[ax] > 0: hi[ax] = c[ax] + H
                else: lo[ax] = c[ax] - H
            box(lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], s=0.3)
        r = 0.095 if strut else 0.055
        for mb in man["members"]:
            a, b = nodes[mb["from_node"]], nodes[mb["to_node"]]
            if mb["kind"] == "arc":
                c = np.array([b[0], a[1], a[2]]); R0 = abs(b[0] - a[0]); pts = [c + R0*np.array([math.cos(t), math.sin(t), 0.0]) for t in np.linspace(math.pi, 1.5*math.pi, 9)]
                if np.linalg.norm(pts[0] - a) > 1e-6 or np.linalg.norm(pts[-1] - b) > 1e-6: pts = [a + (b - a)*t for t in np.linspace(0, 1, 9)]
                for p0, p1 in zip(pts[:-1], pts[1:]): rod(p0, p1, r, "sec", 0.004, 10)
                for p0 in pts[1:-1]: ball(r*1.02, "sec", 10, 6, at=p0)
            else:
                rod(a, b, r, "sec", 0.006, 12); d = (b - a)/np.linalg.norm(b - a); L = float(np.linalg.norm(b - a))
                for t in ((0.5,) if L < 0.9 else (0.33, 0.67)): cyl(r*1.3, -0.03, 0.03, "dark", 0.006, 12, R=R_to(d), at=a + d*L*t)
    return fn
import math
for _cid, _it in INV.items():
    if _it["family"] == "connector" and _cid != "R4C_ROD_STANDARD":
        reg(_cid, _conn_fn(_cid), "connector", env=_env_of(_cid, 0.004), planes=[(_b(p["normal"]), float(np.dot(_b(p["position_m"]), _b(p["normal"])))) for p in _it["ports"]], body=_cid, names={}, ref=("connectors", _cid), source="#1186", interface="source PR #1186 (connector contract = proposal there)", glow=(1.0, 0.45, 0.10))
PHASE = {"pilot": PILOT, "blocks": [c for c, a in ASSETS.items() if a["family"] == "block"], "connectors": [c for c, a in ASSETS.items() if a["family"] == "connector"],
         "modules": [c for c, a in ASSETS.items() if a["family"] in ("module", "deployer")], "traps": [c for c, a in ASSETS.items() if a["family"] == "world_trap"]}
