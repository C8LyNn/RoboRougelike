"""2D helpers for the #1235 A_Sleek rollout generators (pure numpy; scipy/skimage only for the raster 'opening' of non-convex cap plates)."""
import math
import numpy as np


def area2(P):
    P = np.asarray(P, float); x, y = P[:, 0], P[:, 1]
    return 0.5*float(np.dot(x, np.roll(y, -1)) - np.dot(y, np.roll(x, -1)))


def dedupe(P, tol=1e-7):
    out = []
    for p in P:
        if not out or np.linalg.norm(np.asarray(p) - out[-1]) > tol: out.append(np.asarray(p, float))
    while len(out) > 1 and np.linalg.norm(out[0] - out[-1]) <= tol: out.pop()
    return np.array(out)


def drop_collinear(P, tol=1e-9):
    P = dedupe(P); n = len(P); keep = []
    for i in range(n):
        a, b, c = P[i - 1], P[i], P[(i + 1) % n]
        if abs((b[0] - a[0])*(c[1] - b[1]) - (b[1] - a[1])*(c[0] - b[0])) > tol: keep.append(b)
    return np.array(keep)


def is_convex(P, tol=1e-9):
    P = drop_collinear(P); n = len(P)
    if n < 3: return False
    for i in range(n):
        a, b, c = P[i - 1], P[i], P[(i + 1) % n]
        if (b[0] - a[0])*(c[1] - b[1]) - (b[1] - a[1])*(c[0] - b[0]) < -tol: return False
    return True


def clip_halfplane(P, nrm, d):
    """keep the part of polygon P with nrm.x <= d (Sutherland-Hodgman; fine for the mildly non-convex cap outlines used here)"""
    P = [np.asarray(p, float) for p in P]; out = []; nrm = np.asarray(nrm, float)
    for i in range(len(P)):
        p0, p1 = P[i], P[(i + 1) % len(P)]; s0 = float(p0 @ nrm) - d; s1 = float(p1 @ nrm) - d
        if s0 <= 1e-10: out.append(p0)
        if (s0 < -1e-10 and s1 > 1e-10) or (s0 > 1e-10 and s1 < -1e-10): out.append(p0 + (p1 - p0)*(s0/(s0 - s1)))
    if len(out) < 3: return None
    out = dedupe(out)
    return out if len(out) >= 3 and abs(area2(out)) > 1e-9 else None


def miter_inset(P, c):
    """per-vertex inward offset of a CCW polygon by c (miter); keeps the vertex count (used for plate top = plate foot inset by the chamfer)"""
    P = np.asarray(P, float); n = len(P); out = []
    for i in range(n):
        e0 = P[i] - P[i - 1]; e1 = P[(i + 1) % n] - P[i]; e0 = e0/np.linalg.norm(e0); e1 = e1/np.linalg.norm(e1)
        n0 = np.array([-e0[1], e0[0]]); n1 = np.array([-e1[1], e1[0]]); den = 1.0 + float(n0 @ n1)
        m = (n0 + n1)/den if den > 0.2 else (n0 + n1)/0.2
        out.append(P[i] + c*m)
    return np.array(out)


def vertex_normals(P):
    """outward unit normals at the vertices of a CCW polygon (bisector)"""
    P = np.asarray(P, float); n = len(P); out = []
    for i in range(n):
        e0 = P[i] - P[i - 1]; e1 = P[(i + 1) % n] - P[i]; e0 = e0/np.linalg.norm(e0); e1 = e1/np.linalg.norm(e1)
        v = np.array([e0[1], -e0[0]]) + np.array([e1[1], -e1[0]]); l = np.linalg.norm(v)
        out.append(v/l if l > 1e-9 else np.array([e0[1], -e0[0]]))
    return np.array(out)


# ---------------------------------------------------------------- triangulation (polygon with holes) + T-junction repair
def _seg_x(a, b, c, d):
    def o(p, q, r): return (q[0] - p[0])*(r[1] - p[1]) - (q[1] - p[1])*(r[0] - p[0])
    e = 1e-12
    return (o(a, b, c)*o(a, b, d) < -e) and (o(c, d, a)*o(c, d, b) < -e)


def _bridge(outer, hole, all_loops):
    """merge a CW hole into the CCW outer loop with the shortest non-crossing bridge; returns the merged index loop (indices into pts handled by caller)"""
    best = None
    for i, po in enumerate(outer):
        for j, ph in enumerate(hole):
            d = (po[1][0] - ph[1][0])**2 + (po[1][1] - ph[1][1])**2
            if best is not None and d >= best[0]: continue
            ok = True
            for L in all_loops:
                m = len(L)
                for k in range(m):
                    if _seg_x(po[1], ph[1], L[k][1], L[(k + 1) % m][1]): ok = False; break
                if not ok: break
            if ok: best = (d, i, j)
    assert best is not None, "no bridge"
    _, i, j = best
    return outer[:i + 1] + hole[j:] + hole[:j + 1] + outer[i:]


def triangulate(outer, holes=()):
    """ear clipping. outer: CCW Nx2, holes: list of Mx2 (any orientation). returns (pts Kx2, tris list of index triples) with T-junctions repaired"""
    pts = []; key = {}
    def pid(p):
        k = (round(float(p[0])/2e-7), round(float(p[1])/2e-7))
        if k not in key: key[k] = len(pts); pts.append(np.asarray(p, float))
        return key[k]
    O = dedupe(outer)
    if area2(O) < 0: O = O[::-1]
    loop = [(pid(p), p) for p in O]; Hs = []
    for h in holes:
        h = dedupe(h)
        if area2(h) > 0: h = h[::-1]
        Hs.append([(pid(p), p) for p in h])
    Hs.sort(key=lambda L: -max(p[1][0] for p in L))
    loops = [loop] + Hs
    for h in Hs: loop = _bridge(loop, h, loops)
    idx = [i for i, _ in loop]; P = np.array(pts); tris = []
    def cross(a, b, c): return (P[b][0] - P[a][0])*(P[c][1] - P[a][1]) - (P[b][1] - P[a][1])*(P[c][0] - P[a][0])
    guard = 0
    while len(idx) > 3 and guard < 20000:
        guard += 1; n = len(idx); best = None
        for i in range(n):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % n]
            if a == c: best = (1e9, i, True); break                       # spike from a bridge: drop
            cr = cross(a, b, c)
            if cr <= 1e-13: continue
            ok = True
            for q in idx:
                if q in (a, b, c): continue
                if cross(a, b, q) >= -1e-13 and cross(b, c, q) >= -1e-13 and cross(c, a, q) >= -1e-13:
                    # q inside or on the border of the candidate ear
                    ok = False; break
            if not ok: continue
            # quality: prefer fat ears
            la = np.linalg.norm(P[b] - P[a]); lb = np.linalg.norm(P[c] - P[b]); lc = np.linalg.norm(P[a] - P[c]); qy = cr/(la*la + lb*lb + lc*lc)
            if best is None or qy > best[0]: best = (qy, i, False)
        if best is None:
            # relaxed pass: allow points on the border (collinear chains)
            for i in range(n):
                a, b, c = idx[i - 1], idx[i], idx[(i + 1) % n]
                cr = cross(a, b, c)
                if cr <= 1e-13: continue
                ok = True
                for q in idx:
                    if q in (a, b, c): continue
                    if cross(a, b, q) > 1e-11 and cross(b, c, q) > 1e-11 and cross(c, a, q) > 1e-11: ok = False; break
                if ok: best = (0, i, False); break
        if best is None:
            # only zero-area corners left: remove one collinear vertex (repaired below by the T-junction pass)
            rm = None
            for i in range(n):
                if abs(cross(idx[i - 1], idx[i], idx[(i + 1) % n])) <= 1e-13: rm = i; break
            assert rm is not None, "ear clipping stuck"
            idx.pop(rm); continue
        _, i, spike = best
        if not spike: tris.append((idx[i - 1], idx[i], idx[(i + 1) % n]))
        idx.pop(i)
    if len(idx) == 3 and cross(*idx) > 1e-13: tris.append(tuple(idx))
    tris = fix_tjunctions(P, tris)
    return P, tris


def fix_tjunctions(P, tris, tol=1e-9):
    """split triangles whose edge passes through another vertex of the same triangulation"""
    P = np.asarray(P); used = sorted({i for t in tris for i in t}); changed = True; tris = list(tris); guard = 0
    while changed and guard < 200:
        changed = False; guard += 1; out = []
        for t in tris:
            done = False
            for e in range(3):
                a, b, c = t[e], t[(e + 1) % 3], t[(e + 2) % 3]; ab = P[b] - P[a]; L2 = float(ab @ ab)
                if L2 < 1e-18: continue
                for q in used:
                    if q in t: continue
                    aq = P[q] - P[a]; s = float(aq @ ab)/L2
                    if s <= 1e-6 or s >= 1 - 1e-6: continue
                    if abs(ab[0]*aq[1] - ab[1]*aq[0])/math.sqrt(L2) > 3e-7: continue
                    out.append((a, q, c)); out.append((q, b, c)); done = True; changed = True; break
                if done: break
            if not done: out.append(t)
        tris = out
    return [t for t in tris if abs((P[t[1]][0] - P[t[0]][0])*(P[t[2]][1] - P[t[0]][1]) - (P[t[1]][1] - P[t[0]][1])*(P[t[2]][0] - P[t[0]][0])) > 1e-13]


def split_by_lines(P, polys, axis, values):
    """split convex polygons (lists of 2D points) by the lines coord[axis] = v; returns list of convex polygons"""
    out = [np.asarray(p, float) for p in polys]
    for v in sorted(values):
        nxt = []
        for p in out:
            lo, hi = p[:, axis].min(), p[:, axis].max()
            if v <= lo + 1e-9 or v >= hi - 1e-9: nxt.append(p); continue
            n = np.zeros(2); n[axis] = 1.0
            a = clip_halfplane(p, n, v); b = clip_halfplane(p, -n, -v)
            if a is not None: nxt.append(a)
            if b is not None: nxt.append(b)
        out = nxt
    return out


# ---------------------------------------------------------------- raster morphology for non-convex cap plates
def raster_open(region, holes=(), erode=0.0, rho=0.04, px=0.0005, boxes=None, simplify=0.0004):
    """plates for a non-convex cap: (region eroded by `erode`) [intersected with optional boxes] then opened with a disc of radius rho.
    region: CCW polygon Nx2. returns list of CCW polygons (outer loops only), vertices simplified to `simplify` metres."""
    from scipy import ndimage
    from skimage import measure, draw
    R = np.asarray(region, float); lo = R.min(0) - 4*px; hi = R.max(0) + 4*px
    W = int(math.ceil((hi[0] - lo[0])/px)) + 1; Hh = int(math.ceil((hi[1] - lo[1])/px)) + 1
    def mask_of(poly):
        rr, cc = draw.polygon((np.asarray(poly)[:, 1] - lo[1])/px, (np.asarray(poly)[:, 0] - lo[0])/px, (Hh, W)); m = np.zeros((Hh, W), bool); m[rr, cc] = True; return m
    M = mask_of(R)
    for h in holes: M &= ~mask_of(h)
    d_in = ndimage.distance_transform_edt(M)*px
    out = []
    for bx in (boxes or [None]):
        A = d_in >= erode
        if bx is not None:
            yy, xx = np.mgrid[0:Hh, 0:W]; X = lo[0] + xx*px; Y = lo[1] + yy*px
            A &= (X >= bx[0]) & (X <= bx[2]) & (Y >= bx[1]) & (Y <= bx[3])
        S = ndimage.distance_transform_edt(A)*px >= rho
        if not S.any(): continue
        dS = ndimage.distance_transform_edt(~S)*px
        for c in measure.find_contours(rho - dS, 0.0):
            poly = np.stack([lo[0] + c[:, 1]*px, lo[1] + c[:, 0]*px], axis=1)[:-1]
            if len(poly) < 8: continue
            poly = measure.approximate_polygon(np.vstack([poly, poly[:1]]), simplify)[:-1]
            if area2(poly) < 0: poly = poly[::-1]
            if area2(poly) > 1e-4: out.append(poly)
    return out
