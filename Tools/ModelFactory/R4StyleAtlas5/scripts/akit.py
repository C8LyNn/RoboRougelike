"""#1239 style-aware composition kit. Same authoring API as R4ModulesSleek1x1/scripts/mkit.py (imported unchanged as BASE), but every
primitive is interpreted through the active style: bevel language, panelling, segment counts, part thickness, fasteners, asymmetry.
One asset definition -> five different meshes. Contact plates stay ON the contact planes in every style (generator guarantee)."""
import math, os, sys, zlib, random, importlib.util
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); PKG = os.path.dirname(HERE); MF = os.path.dirname(PKG)
_spec = importlib.util.spec_from_file_location("mkit_base", os.path.join(MF, "R4ModulesSleek1x1", "scripts", "mkit.py")); BASE = importlib.util.module_from_spec(_spec); sys.modules["mkit_base"] = BASE; _spec.loader.exec_module(BASE)
import astyle as ST
H, G, X, Y, Z = BASE.H, BASE.G, BASE.X, BASE.Y, BASE.Z
STYLE = "A"; ASSET = ""; NOHW = False; RIG = False; PLANES = None; _GRP = 0; _SAFE = False; FALLBACK = []; ENV = (np.array([-H]*3), np.array([H]*3)); _wfac = 1.0
rot, R_to, arc2, ngon, frame, port, empty, DIRS, auto_s, RP, S, RC, paths25, rect, prof = BASE.rot, BASE.R_to, BASE.arc2, BASE.ngon, BASE.frame, BASE.port, BASE.empty, BASE.DIRS, BASE.auto_s, BASE.RP, BASE.S, BASE.RC, BASE.paths25, BASE.rect, BASE.prof
def __getattr__(name): return getattr(BASE, name)


def _rng(tag=""): return random.Random(zlib.crc32(f"{ASSET}|{len(BASE.CUR.parts)}|{tag}".encode()))


def style(s=1.0, **kw):
    g = dict(ST.GEO[STYLE]); s = max(0.2, min(1.0, s)); A = dict(BASE.STYLES["A"])
    if _SAFE: ga = ST.GEO["A"]; g.update(w=ga["w"], g=ga["g"], margin=ga["margin"], chamfer=ga["chamfer"], rho=ga["rho"]/0.45, cseg=ga["cseg"], seg=ga["seg"] if _SAFE == 2 else g["seg"])     # small contact pads keep the plate size of the approved ruler
    A.update(w=g["w"]*s*_wfac, rho=g["rho"]*s*(1.0 if (s >= 1.0 or STYLE == "A") else 0.45), margin=max(g["margin"]*s, 0.003 if g["margin"] else 0.0), chamfer=max(g["chamfer"]*s, 0.0015 if g["chamfer"] else 0.0), g=(g["g"] if s >= 0.6 else g["g"]*0.75),
             seg=g["seg"] if s >= 0.8 else max(1, round(g["seg"]*0.7)) if s >= 0.5 else max(1, round(g["seg"]*0.5)), cseg=g["cseg"] if s >= 0.8 else max(1, round(g["cseg"]*0.7)) if s >= 0.5 else max(1, round(g["cseg"]*0.5)),
             wall_smooth=g["wall_smooth"], seam_h=0.006, min_plate=max(0.05*s, 0.03))
    A["rho"] = max(A["rho"], A["chamfer"] + 0.0045) if g["rho"] else 0.0
    A.update(kw); return A


BASE.style = style                       # the BASE solids look their style up at call time


def _q(x): return round(float(x), 4) + 0.00002          # keeps chamfer vertices off the welding grid ties


def _inside(m, pad=0.0):
    lo, hi = m["V"].min(0), m["V"].max(0); return bool((lo >= ENV[0] - 1e-7 + pad).all() and (hi <= ENV[1] + 1e-7 - pad).all())


def _touches(m):
    lo, hi = m["V"].min(0), m["V"].max(0)
    if PLANES is not None and any(float((m["V"] @ pn).max()) > pd - 0.006 for pn, pd in PLANES): return True
    return bool((lo <= ENV[0] + 0.0065).any() or (hi >= ENV[1] - 0.0065).any())


def _is_contact(n, top):
    if PLANES is not None: return any(float(np.dot(n, pn)) > 0.99 and abs(top - pd) < 0.004 for pn, pd in PLANES)     # explicit contact planes (multi-cell blocks, connectors)
    ax = int(np.argmax(np.abs(n))); lim = ENV[1][ax] if n[ax] > 0 else -ENV[0][ax]; return bool(abs(n[ax]) > 0.99 and abs(top - lim) < 0.0065)


def _islands(m, groups=("plate",)):
    idx = [i for i, f in enumerate(m["faces"]) if f["group"] in groups]; par = {}
    def find(a):
        while par.setdefault(a, a) != a: par[a] = par[par[a]]; a = par[a]
        return a
    for i in idx:
        v = m["faces"][i]["v"]
        for k in v[1:]: par[find(("v", k))] = find(("v", v[0]))
    out = {}
    for i in idx: out.setdefault(find(("v", m["faces"][i]["v"][0])), []).append(i)
    return list(out.values())


def _a_slits(m):
    """A: one hairline light slit along the long side of every free panel"""
    for faces in _islands(m):
        P = np.concatenate([m["V"][m["faces"][i]["v"]] for i in faces]); n = np.mean([np.mean(m["faces"][i]["n"], axis=0) for i in faces], axis=0); nn = np.linalg.norm(n)
        if nn < 0.98: continue
        n = n/nn; u, v, _ = frame(n); c = P.mean(0); a = (P - c) @ u; b = (P - c) @ v; ea, eb = (a.max() - a.min())/2, (b.max() - b.min())/2
        c = c + u*(a.max() + a.min())/2 + v*(b.max() + b.min())/2; top = float((P @ n).max()); c = c + n*(top - c @ n)
        if _is_contact(n, top) or min(ea, eb) < 0.05: continue
        lu, lv, el, es = (u, v, ea, eb) if ea >= eb else (v, u, eb, ea); Rm = np.stack([lu, lv, n], axis=1)
        if np.linalg.det(Rm) < 0: Rm = np.stack([lu, -lv, n], axis=1)
        BASE.sbox(_q(-el*0.7), _q(el*0.7), -0.0022, 0.0022, -0.002, 0.0012, "glow", 0.0006, Rm, c + lv*es*0.62)
        if not (BASE.RP.mesh_stats(BASE.CUR.parts[-1])["ok"] and _inside(BASE.CUR.parts[-1])): BASE.CUR.parts.pop()


def _post_solid(m, s_hint=1.0):
    """style finish of a plated solid: scrap patchwork, fasteners in the frame recess (never above the contact plane)"""
    if STYLE == "A" and not BASE.LOD and m is not None and BASE.CUR.node != "Payload" and not NOHW: _a_slits(m)
    if STYLE not in ("B", "D") or BASE.LOD or m is None or BASE.CUR.node == "Payload" or NOHW: return
    r = _rng("solid"); isl = _islands(m); bolts = []; over = []
    for faces in isl:
        P = np.concatenate([m["V"][m["faces"][i]["v"]] for i in faces]); n = np.mean([np.mean(m["faces"][i]["n"], axis=0) for i in faces], axis=0); nn = np.linalg.norm(n)
        if STYLE == "D":
            pick = r.random(); grp = "plate" if pick < 0.55 else ("patch" if pick < 0.82 else "rust")
            for i in faces: m["faces"][i]["group"] = grp
        if nn < 0.98: continue                                                    # curved plate: no flat hardware
        n = n/nn; u, v, _ = frame(n); c = P.mean(0); a = (P - c) @ u; b = (P - c) @ v; ea, eb = (a.max() - a.min())/2, (b.max() - b.min())/2
        c = c + u*(a.max() + a.min())/2 + v*(b.max() + b.min())/2; top = float((P @ n).max()); c = c + n*(top - c @ n)
        on_contact = _is_contact(n, top)
        if min(ea, eb) >= 0.045 and STYLE in ("B", "D"):
            gdep = 0.008 if STYLE == "B" else 0.005; rad = 0.0085 if STYLE == "B" else 0.0045
            for sa in (-1, 1):
                for sb in (-1, 1):
                    if STYLE == "D" and r.random() < 0.45: continue
                    bolts.append((c + u*sa*(ea - 0.004) + v*sb*(eb - 0.004) - n*gdep, n, rad, gdep))
        if on_contact or RIG or min(ea, eb) < 0.05: continue
        lu, lv, el, es = (u, v, ea, eb) if ea >= eb else (v, u, eb, ea)
        if STYLE == "B":
            for k in (-0.42, 0.42): over.append((c + lv*es*k, lu, lv, n, el - 0.012, 0.008, 0.007, "dark", 0.0))
        elif STYLE == "D" and r.random() < 0.75:
            over.append((c + lu*el*r.uniform(-0.25, 0.25) + lv*es*r.uniform(-0.25, 0.25), lu, lv, n, el*r.uniform(0.35, 0.55), es*r.uniform(0.35, 0.6), 0.003, "rust" if r.random() < 0.5 else "patch", r.uniform(-7, 7)))
    for (p, n, rad, gdep) in bolts:
        hgt = gdep - 0.002
        if STYLE == "B": BASE.lathe([(0, 0), (rad, 0), (rad, hgt*0.8), (rad*0.7, hgt), (0, hgt)], "bolt", 6, crease=1.0, R=R_to(n), at=p)
        else: BASE.lathe([(0, 0), (rad, 0), (rad*0.7, hgt*0.7), (0, hgt)], "bolt", 6, crease=60.0, R=R_to(n), at=p)
        if not _inside(BASE.CUR.parts[-1]): BASE.CUR.parts.pop()
    for (p, lu, lv, n, hl, hw, hh, grp, ang) in over:
        Rm = np.stack([lu, lv, n], axis=1)
        if np.linalg.det(Rm) < 0: Rm = np.stack([lv, lu, n], axis=1); hl, hw = hw, hl
        if min(hl, hw) < 0.006: continue
        hl, hw = _q(hl), _q(hw)
        BASE.sbox(-hl, hl, -hw, hw, -0.002, hh, grp, _q(min(0.003, hh*0.4, hw*0.4, hl*0.4)) - 0.00002, Rm @ rot(Z, ang), p)
        if not BASE.RP.mesh_stats(BASE.CUR.parts[-1])["ok"]: BASE.CUR.parts.pop(); continue
        if not _inside(BASE.CUR.parts[-1]): BASE.CUR.parts.pop()


SKIP_BELOW = {"A": 0.0, "B": 0.0, "C": 0.055, "D": 0.0, "E": 0.045}       # C / E: few, large shapes - small secondary parts are not built
GROW = {"A": 1.0, "B": 1.0, "C": 1.10, "D": 1.0, "E": 1.18}               # C / E: free-standing parts are enlarged (inside the envelope)


def _node_count(): return sum(1 for p in BASE.CUR.parts if p["node"] == BASE.CUR.node)


def _finish(m, grp=None):
    """per-style treatment of a free-standing simple part: drop (C / E, small), enlarge (C / E), scrap asymmetry (D)"""
    if m is None: return None
    ext = float((m["V"].max(0) - m["V"].min(0)).max()); payload = BASE.CUR.node == "Payload"
    if SKIP_BELOW[STYLE] and ext < SKIP_BELOW[STYLE] and not payload and not (BASE.CUR.node != "Body" and _node_count() <= 1) and not _touches(m):
        if BASE.CUR.parts and BASE.CUR.parts[-1] is m: BASE.CUR.parts.pop(); return None
    if _GRP or RIG: return m                                    # rigged source nodes keep their approved bounds; parts of a transformed group are finished in local space: left as authored
    slide = BASE.CUR.node != "Body" and (BASE.CUR.nodes[BASE.CUR.node].get("anim") or {}).get("type") == "slide"      # sliding parts keep their authored travel room
    if GROW[STYLE] != 1.0 and not _touches(m) and not payload and not slide:
        c = (m["V"].max(0) + m["V"].min(0))/2; old = m["V"].copy(); piv = np.array(BASE.CUR.nodes[BASE.CUR.node]["pivot"]) if BASE.CUR.node != "Body" else c
        m["V"] = (m["V"] - piv)*GROW[STYLE] + piv if BASE.CUR.node != "Body" else (m["V"] - c)*GROW[STYLE] + c
        if not _inside(m, 0.001): m["V"] = old
    _jitter(m); return m


def _addons_base(m):
    """mount base (a plated box lying on a contact plane): style hardware on its free face"""
    if BASE.LOD or m is None or NOHW or RIG or BASE.CUR.node != "Body" or STYLE not in ("A", "B", "D"): return
    lo, hi = m["V"].min(0), m["V"].max(0); sz = hi - lo
    if not (abs(lo[2] - ENV[0][2]) < 0.002 and sz[0] > 0.45 and sz[1] > 0.45 and sz[2] < 0.12): return
    z = hi[2]; r = _rng("base"); n0 = len(BASE.CUR.parts)
    if STYLE == "B":                                                         # cast corner clamps with through bolts
        for sx in (-1, 1):
            for sy in (-1, 1):
                BASE.sbox(_q(sx*0.25 - 0.045*(sx > 0) - 0.0), _q(sx*0.25 + 0.045*(sx < 0)), _q(sy*0.25 - 0.045*(sy > 0)), _q(sy*0.25 + 0.045*(sy < 0)), _q(z - 0.004), _q(z + 0.03), "dark", 0.005)
                BASE.lathe([(0, 0), (0.014, 0), (0.014, 0.008), (0.009, 0.012), (0, 0.012)], "bolt", 6, crease=1.0, at=(sx*0.225, sy*0.225, z + 0.03))
    elif STYLE == "D":                                                       # welded angle iron on two sides, a pipe and a tie-down ring
        for k in range(2):
            a = r.choice((0, 90, 180, 270)); R = rot(Z, a)
            BASE.sbox(-0.2, 0.2, 0.215, 0.245, _q(z - 0.003), _q(z + 0.022), "rust", 0.003, R)
            BASE.sbox(-0.2, 0.2, 0.238, 0.25, _q(z - 0.003), _q(z + 0.05), "rust", 0.003, R)
        a = r.choice((0, 90, 180, 270)); BASE.lathe([(0, 0), (0.016, 0), (0.016, 0.36), (0, 0.36)], "metal", 7, crease=0.5, R=rot(Z, a) @ R_to(X), at=rot(Z, a) @ np.array([-0.18, -0.225, z + 0.016]))
    else:                                                                    # A: light seam around the deck
        BASE.lathe([(0.226, z - 0.002), (0.234, z - 0.002), (0.234, z + 0.0025), (0.226, z + 0.0025)], "glow", 4, closed=True, crease=1.0, phase=math.pi/4)
        BASE.CUR.parts[-1]["V"] *= np.array([1/math.cos(math.pi/4)]*2 + [1.0])
    for p in BASE.CUR.parts[n0:]:
        if not _inside(p): p["drop"] = True
    BASE.CUR.parts[n0:] = [p for p in BASE.CUR.parts[n0:] if not p.get("drop")]


def _jitter(m):
    """D: controlled asymmetry of parts that do not touch a contact plane / the envelope"""
    if STYLE != "D" or m is None or _GRP or RIG or _touches(m) or BASE.CUR.node == "Payload": return
    r = _rng("jit"); c = m["V"].mean(0); Rz = rot(Z, r.uniform(-3.0, 3.0)); t = np.array([r.uniform(-0.004, 0.004), r.uniform(-0.004, 0.004), 0.0]); old = (m["V"].copy(), [list(f["n"]) for f in m["faces"]])
    m["V"] = (m["V"] - c) @ Rz.T + c + t
    for f in m["faces"]: f["n"] = [np.asarray(x) @ Rz.T for x in f["n"]]
    if not _inside(m, 0.001):
        m["V"] = old[0]
        for f, nn in zip(m["faces"], old[1]): f["n"] = nn


def _solid(fn, *a, **kw):
    """build a plated solid in the active style; if the style bevel does not fit this part (assert or open shell) retry with a narrower one"""
    global _wfac
    was = BASE.LOD; base_f = _rng("w").uniform(0.45, 1.55) if STYLE == "D" else 1.0; m = None
    if STYLE == "C" and not BASE.LOD:                      # arcade: one soft roll, no frame step and no plates
        if fn is BASE.prism: kw["plates"] = set(); kw["caps"] = ("none", "none")
        elif fn is BASE.hull: kw["plates"] = False
        elif fn is BASE.srev: kw["plate_ok"] = (lambda i: False)
    try:
        for f in (1.0, 0.6, 0.35, 0.2, 0.1):
            _wfac = base_f*f; n0 = len(BASE.CUR.parts)
            try: m = fn(*a, **kw)
            except (AssertionError, RuntimeError, ValueError, IndexError): del BASE.CUR.parts[n0:]; m = None; continue
            if BASE.RP.mesh_stats(m)["ok"]: break
            del BASE.CUR.parts[n0:]; m = None
        if m is None:                                             # the style bevel cannot be built on this solid: approved-ruler widths, then the plain build
            global _SAFE
            for mode in ("safe", "plain"):
                n0 = len(BASE.CUR.parts); _SAFE = 2; BASE.LOD = was or mode == "plain"; _wfac = 1.0
                try:
                    m = fn(*a, **kw)
                    if BASE.RP.mesh_stats(m)["ok"]: FALLBACK.append((ASSET, STYLE, mode)); break
                    del BASE.CUR.parts[n0:]; m = None
                except (AssertionError, RuntimeError, ValueError, IndexError): del BASE.CUR.parts[n0:]; m = None
                finally: _SAFE = False
            BASE.LOD = was
        assert m is not None, "solid does not fit any bevel width"
        if STYLE in ("B", "D", "E") and not BASE.LOD and not _GRP and _touches(m) and float((m["V"].max(0) - m["V"].min(0)).min()) < 0.3 and float((m["V"].max(0) - m["V"].min(0)).max()) < 0.5:
            n0 = next(i for i, p in enumerate(BASE.CUR.parts) if p is m); _SAFE = True; _wfac = 1.0
            try:
                m2 = fn(*a, **kw)
                if BASE.RP.mesh_stats(m2)["ok"]: del BASE.CUR.parts[n0]; m = m2
                else: BASE.CUR.parts.pop()
            except AssertionError: del BASE.CUR.parts[n0 + 1:]
            finally: _SAFE = False
    finally: BASE.LOD = was; _wfac = 1.0
    _post_solid(m); _addons_base(m)
    if not _touches(m): _finish(m)
    return m


def prism(*a, **kw): return _solid(BASE.prism, *a, **kw)
def srev(pts, N=24, s=1.0, **kw): return _solid(BASE.srev, pts, _n(N), s, **kw)
def hull(*a, **kw): return _solid(BASE.hull, *a, **kw)
def box(x0, x1, y0, y1, z0, z1, s=None, faces=None, grp=None, R=None, at=None, **kw):
    pl = None; caps = ("auto", "auto")
    if faces is not None:
        pl = {i for i, c in enumerate("yXYx") if c in faces}; caps = ("auto" if "z" in faces else "none", "auto" if "Z" in faces else "none")
    return prism([(x0, y0), (x1, y0), (x1, y1), (x0, y1)], z0, z1, "z", s if s is not None else auto_s(x1 - x0, y1 - y0, z1 - z0), plates=pl, caps=caps, grp=grp, R=R, at=at, **kw)


def _n(N):
    if N <= 8: return N                                   # authored polygons (square hopper, hex tank) keep their side count
    return max(ST.LATHE_MIN[STYLE], int(round(N*ST.LATHE_N[STYLE])))


def lathe(pts, grp="sec", N=20, closed=False, crease=32.0, R=None, at=None, phase=0.0):
    if ST.LATHE_FLAT[STYLE] and N > 8: crease = 0.5         # hand-cut facets
    if STYLE == "D" and N > 8: phase = phase + _rng("ph").uniform(0, 1.0)
    m = BASE.lathe(pts, grp, _n(N), closed, crease, R, at, phase); return _finish(m)


def cyl(r, z0, z1, grp="sec", b=0.004, N=20, R=None, at=None):
    L = z1 - z0; big = r >= 0.035 and L >= 0.07 and BASE.CUR.node != "Payload" and not BASE.LOD and not RIG
    if STYLE in ("C", "E"): b = min(r*0.45, L*0.4, 0.03 if STYLE == "E" else 0.02)      # soft ends
    b = min(b, r*0.5, L*0.4)
    if STYLE == "E" and big:                                    # barrel: bulging belly
        k = 1.10; pts = [(0, z0), (r - b, z0), (r, z0 + b), (r*k, z0 + L*0.5), (r, z1 - b), (r - b, z1), (0, z1)]
        m = BASE.lathe(pts, grp, _n(N), False, 60.0, R, at)
        if _inside(m): return _finish(m)
        BASE.CUR.parts.pop()
    m = lathe([(0, z0), (r - b, z0), (r, z0 + b), (r, z1 - b), (r - b, z1), (0, z1)], grp, N, R=R, at=at)
    if m is None or not big: return m
    n0 = len(BASE.CUR.parts)
    if STYLE == "B":                                            # flanged ends + bolt circle
        for zc in (z0 + 0.018, z1 - 0.018): BASE.lathe([(r - 0.002, zc - 0.008), (r + 0.010, zc - 0.008), (r + 0.010, zc + 0.008), (r - 0.002, zc + 0.008)], "dark", _n(N), closed=True, crease=1.0, R=R, at=at)
    elif STYLE == "D":                                          # two rusty straps of unequal height
        rr = _rng("strap")
        for zc in (z0 + L*rr.uniform(0.2, 0.35), z0 + L*rr.uniform(0.6, 0.8)): BASE.lathe([(r - 0.001, zc - 0.007), (r + 0.004, zc - 0.007), (r + 0.004, zc + 0.007), (r - 0.001, zc + 0.007)], "rust", _n(N), closed=True, crease=0.5, R=R, at=at)
    elif STYLE == "A" and grp != "glow":                        # hairline light ring
        zc = z0 + L*0.72; BASE.lathe([(r - 0.001, zc - 0.002), (r + 0.0015, zc - 0.002), (r + 0.0015, zc + 0.002), (r - 0.001, zc + 0.002)], "glow", _n(N), closed=True, crease=1.0, R=R, at=at)
    BASE.CUR.parts[n0:] = [p for p in BASE.CUR.parts[n0:] if _inside(p)]
    return m


def rod(p0, p1, r, grp="metal", b=0.003, N=10):
    p0 = np.asarray(p0, float); p1 = np.asarray(p1, float); d = p1 - p0; ln = float(np.linalg.norm(d)); rr = r*(1.0 if RIG else ST.FAT[STYLE])
    lo = np.minimum(p0, p1) - rr; hi = np.maximum(p0, p1) + rr
    if not ((lo >= ENV[0]).all() and (hi <= ENV[1]).all()): rr = r
    Rm = R_to(d)
    if STYLE == "B" and ln > 0.08 and not BASE.LOD:              # hydraulic: cylinder body + polished rod + gland
        m = lathe([(0, 0), (rr*1.25, 0), (rr*1.25, ln*0.55), (rr*1.5, ln*0.55), (rr*1.5, ln*0.62), (rr*0.7, ln*0.62), (rr*0.7, ln), (0, ln)], [grp, "dark", "dark", "dark", "dark", "metal", "metal"], N, R=Rm, at=p0, crease=20)
        if m is not None and _inside(m): return m
        if m is not None: BASE.CUR.parts.pop()
    if STYLE == "E" and ln > 0.08:                                # tapered
        n0 = len(BASE.CUR.parts); m = lathe([(0, 0), (rr*1.2, 0), (rr*0.75, ln), (0, ln)], grp, N, R=Rm, at=p0, crease=50)
        if m is None or _inside(m): return m
        del BASE.CUR.parts[n0:]
    if STYLE == "D" and ln > 0.1 and not BASE.LOD:               # pipe with a welded sleeve
        m = lathe([(0, 0), (rr, 0), (rr, ln), (0, ln)], grp, N, R=Rm, at=p0); k = _rng("slv").uniform(0.3, 0.7)
        BASE.lathe([(rr - 0.001, ln*k - 0.012), (rr*1.45, ln*k - 0.012), (rr*1.45, ln*k + 0.012), (rr - 0.001, ln*k + 0.012)], "rust", 7, closed=True, crease=0.5, R=Rm, at=p0)
        if not _inside(BASE.CUR.parts[-1]): BASE.CUR.parts.pop()
        return m
    return lathe([(0, 0), (max(rr - b, rr*0.5), 0), (rr, min(b, ln*0.3)), (rr, ln - min(b, ln*0.3)), (max(rr - b, rr*0.5), ln), (0, ln)], grp, N, R=Rm, at=p0)


def tube(r0, r1, z0, z1, grp="sec", b=0.004, N=20, R=None, at=None):
    b = min(b, (r1 - r0)*0.4, (z1 - z0)*0.4)
    return lathe([(r0, z0 + b), (r0 + b, z0), (r1 - b, z0), (r1, z0 + b), (r1, z1 - b), (r1 - b, z1), (r0 + b, z1), (r0, z1 - b)], grp, N, closed=True, R=R, at=at)


def torus(Rm, r, grp="sec", N=24, n=8, R=None, at=None):
    rr = r*(1.0 if RIG else ST.FAT[STYLE]); Rm2 = Rm - (rr - r)               # the outer radius is kept, the tube grows inwards
    if Rm2 < rr*1.15: rr, Rm2 = r, Rm
    n2 = {"A": n, "B": 4, "C": max(6, n - 2), "D": max(5, n - 2), "E": n + 2}[STYLE]; n2 = max(4, n2//2) if BASE.LOD else n2     # B: square-section rings
    return lathe([(Rm2 + rr*math.cos(2*math.pi*i/n2 + (math.pi/4 if n2 == 4 else 0)), rr*math.sin(2*math.pi*i/n2 + (math.pi/4 if n2 == 4 else 0))) for i in range(n2)], grp, N, closed=True, crease=80.0 if STYLE != "B" else 30.0, R=R, at=at)


def ball(r, grp="glow", N=16, n=8, R=None, at=None, squash=1.0):
    n2 = {"A": n, "B": max(4, n - 2), "C": max(5, n - 2), "D": max(4, n - 2), "E": n + 2}[STYLE]; n2 = max(3, n2//2) if BASE.LOD else n2
    return lathe([(r*math.cos(a), squash*r*math.sin(a)) for a in np.linspace(-math.pi/2, math.pi/2, n2 + 1)], grp, N, crease=80.0, R=R, at=at)


def cone(r0, r1, z0, z1, grp="sec", N=20, b=0.003, R=None, at=None):
    pts = [(0, z0)] + ([(r0 - b, z0), (r0, z0 + b)] if r0 > 2*b else [(max(r0, 1e-4), z0)]) + ([(r1, z1 - b), (r1 - b, z1)] if r1 > 2*b else [(max(r1, 1e-4), z1)]) + [(0, z1)]
    return lathe(pts, grp, N, R=R, at=at)


def sbox(x0, x1, y0, y1, z0, z1, grp="dark", b=None, R=None, at=None):
    d = min(x1 - x0, y1 - y0, z1 - z0)
    if b is None: b = min(0.006, 0.2*d)
    if STYLE == "C": b = min(0.32*d, 0.016)
    if STYLE == "E": b = min(0.38*d, 0.02)
    if STYLE == "D": b = min(0.3*d, b*_rng("sb").uniform(0.5, 2.2))
    m = BASE.sbox(x0, x1, y0, y1, z0, z1, grp, b, R, at)
    if not BASE.RP.mesh_stats(m)["ok"]: BASE.CUR.parts.pop(); m = BASE.sbox(_q(x0), _q(x1), _q(y0), _q(y1), _q(z0), _q(z1), grp, _q(b*0.8), R, at)
    return _finish(m)


def slab(*a, **kw): m = BASE.slab(*a, **kw); return _finish(m)


def node(name="Body", pivot=(0, 0, 0), anim=None, parent=None):
    BASE.node(name, pivot, anim)
    if name != "Body": BASE.CUR.nodes[name]["parent"] = parent


class group(BASE.group):
    """payload miniatures (lod=True groups of the trap deployers) become the movable node 'Payload' so that Deploy can animate them"""
    def __enter__(self):
        self.payload = self.lod and BASE.CUR.node == "Body"
        if self.payload: BASE.node("Payload", tuple(float(x) for x in self.at), dict(type="payload")); BASE.CUR.nodes["Payload"]["parent"] = None
        global _GRP
        _GRP += 1; return super().__enter__()
    def __exit__(self, *a):
        global _GRP
        _GRP -= 1; super().__exit__(*a)
        if self.payload: BASE.node()


def build(fn, style_key, lod=False, env=None, asset="", rig=False, planes=None):
    global STYLE, ENV, ASSET, RIG, PLANES
    RIG = rig; PLANES = [(np.asarray(n, float), float(d)) for n, d in planes] if planes else None
    STYLE = style_key; ASSET = asset; ENV = (np.array(env[0], float), np.array(env[1], float)) if env is not None else (np.array([-H]*3), np.array([H]*3))
    BASE.style = style; del FALLBACK[:]
    m = BASE.build(fn, lod); m["style"] = style_key; m["fallbacks"] = sorted({f[2] for f in FALLBACK})
    return m


stats = BASE.stats
