"""R4 base-block redesign (#1232): pure-numpy generator for 'plates on a chamfered frame' convex blocks.

A block is described ONLY by the convex cell solid that the approved R4Blocks source already uses (same corner points,
same +-0.26 m contact planes). The generator produces one watertight manifold shell:
  frame  = the solid moved in by g, every edge bevelled with a constant in-face width w (round or flat)
  plates = one raised plate per face, top exactly ON the original contact plane (flush R4 contact patch)
Blender axes (same as r4lib): X right, Y back (-Y = forward), Z up.  File/Unity: (x, z, -y).
"""
import math
import numpy as np
from scipy.spatial import ConvexHull, HalfspaceIntersection
from scipy.optimize import linprog


def unit(v):
    v = np.asarray(v, float); return v / np.linalg.norm(v)


def planes_from_points(pts):
    pts = np.asarray(pts, float); h = ConvexHull(pts); out = []
    for eq in h.equations:
        n = eq[:3]; d = -eq[3]
        if not any(np.allclose(n, p[0], atol=1e-7) and abs(d - p[1]) < 1e-7 for p in out): out.append((n.copy(), float(d)))
    return out


def face_frame(n):
    n = unit(n)
    u = np.cross([0, 0, 1], n) if abs(n[2]) < 0.9 else np.cross([1, 0, 0], n)
    u = unit(u); v = np.cross(n, u)
    return u, v, n


def clip_polytope(planes):
    """vertices + CCW (seen from outside) polygon per plane index for {x: n.x <= d}"""
    A = np.array([p[0] for p in planes]); b = np.array([p[1] for p in planes])
    res = linprog([0, 0, 0, -1], A_ub=np.hstack([A, np.ones((len(A), 1))]), b_ub=b, bounds=[(None, None)]*3 + [(0, None)])
    if not res.success or res.x[3] < 1e-6: return None
    hs = HalfspaceIntersection(np.hstack([A, -b[:, None]]), res.x[:3])
    V = hs.intersections; V = V[np.all(np.isfinite(V), axis=1)]
    key = np.round(V / 1e-7).astype(np.int64); _, ui = np.unique(key, axis=0, return_index=True); V = V[np.sort(ui)]
    faces = {}
    for i, (n, d) in enumerate(planes):
        on = np.where(np.abs(V @ n - d) < 1e-6)[0]
        if len(on) < 3: continue
        u, v, _ = face_frame(n); c = V[on].mean(0)
        ang = np.arctan2((V[on] - c) @ v, (V[on] - c) @ u)
        faces[i] = [int(x) for x in on[np.argsort(ang)]]
    return V, faces


def slerp(a, b, t):
    a = unit(a); b = unit(b); w = math.acos(max(-1.0, min(1.0, float(a @ b))))
    if w < 1e-9: return a
    return (math.sin((1 - t)*w)*a + math.sin(t*w)*b) / math.sin(w)


# ---------------------------------------------------------------- 2D convex polygon helpers
def inset_convex(P, d):
    """offset every edge of CCW convex polygon P (Nx2) inward by d (d may be per-edge array); returns CCW polygon or None"""
    P = np.asarray(P, float); n = len(P); d = np.full(n, d) if np.isscalar(d) else np.asarray(d, float)
    poly = [p for p in P]
    for k in range(n):
        a, b = P[k], P[(k + 1) % n]; e = b - a; e = e / np.linalg.norm(e); nin = np.array([-e[1], e[0]])
        q = a + nin*d[k]; out = []
        for i in range(len(poly)):
            p0, p1 = poly[i], poly[(i + 1) % len(poly)]; s0 = (p0 - q) @ nin; s1 = (p1 - q) @ nin
            if s0 >= -1e-12: out.append(p0)
            if (s0 > 1e-12 and s1 < -1e-12) or (s0 < -1e-12 and s1 > 1e-12): out.append(p0 + (p1 - p0)*(s0/(s0 - s1)))
        poly = out
        if len(poly) < 3: return None
    Q = [poly[0]]
    for p in poly[1:]:
        if np.linalg.norm(p - Q[-1]) > 1e-7: Q.append(p)
    if np.linalg.norm(Q[0] - Q[-1]) < 1e-7: Q.pop()
    if len(Q) < 3: return None
    Q = np.array(Q); x, y = Q[:, 0], Q[:, 1]
    if 0.5*abs(np.dot(x, np.roll(y, -1)) - np.dot(y, np.roll(x, -1))) < 1e-6: return None
    return Q


def round_polygon(S, rho, nseg):
    """Minkowski sum of CCW convex polygon S with a disc of radius rho; nseg arc segments per corner (nseg=1 -> clipped corner)"""
    S = np.asarray(S, float); n = len(S); out = []
    for k in range(n):
        p = S[k]; e0 = p - S[k - 1]; e1 = S[(k + 1) % n] - p
        a0 = math.atan2(e0[1], e0[0]) - math.pi/2; a1 = math.atan2(e1[1], e1[0]) - math.pi/2
        while a1 < a0 - 1e-12: a1 += 2*math.pi
        for s in range(nseg + 1):
            a = a0 + (a1 - a0)*s/nseg; out.append(p + rho*np.array([math.cos(a), math.sin(a)]))
    return np.array(out)


class MeshBuilder:
    def __init__(self):
        self.V = []; self._key = {}; self.F = []          # F: dict(v=[idx], n=[loop normals], group, plane, zone)
    def vid(self, p):
        k = tuple(np.round(np.asarray(p)/2e-7).astype(np.int64))
        if k not in self._key: self._key[k] = len(self.V); self.V.append(np.asarray(p, float))
        return self._key[k]
    def face(self, pts, normals, group, plane, ref=None):
        ids = [self.vid(p) for p in pts]
        keep = [i for i in range(len(ids)) if ids[i] != ids[i - 1]]
        ids = [ids[i] for i in keep]; normals = [unit(normals[i]) for i in keep]
        if len(set(ids)) < 3: return
        P = np.array([self.V[i] for i in ids]); nn = np.zeros(3)
        for i in range(len(P)): nn += np.cross(P[i], P[(i + 1) % len(P)])
        if np.linalg.norm(nn) < 1e-12: return
        ref = np.mean(normals, axis=0) if ref is None else ref
        if nn @ ref < 0: ids = ids[::-1]; normals = normals[::-1]
        self.F.append(dict(v=ids, n=normals, group=group, plane=plane))


def annulus(mb, outer, inner, n, group, plane):
    """triangulate the planar ring between two nested CCW convex loops (3D points): greedy shortest diagonal, validated"""
    outer = np.asarray(outer); inner = np.asarray(inner); no, ni = len(outer), len(inner)
    j0 = int(np.argmin(np.linalg.norm(inner - outer[0], axis=1)))
    O = lambda k: outer[k % no]; I = lambda k: inner[(j0 + k) % ni]
    def area(a, b, c): return float(np.cross(b - a, c - a) @ n)
    i = j = 0
    while i < no or j < ni:
        ao = area(O(i), O(i + 1), I(j)) if i < no else -1.0          # advance outer
        ai = area(O(i), I(j + 1), I(j)) if j < ni else -1.0          # advance inner
        if ao > 1e-12 and ai > 1e-12:
            adv_o = np.linalg.norm(O(i + 1) - I(j)) <= np.linalg.norm(O(i) - I(j + 1))
        elif ao > 1e-12: adv_o = True
        elif ai > 1e-12: adv_o = False
        else: raise RuntimeError("annulus triangulation failed")
        if adv_o: mb.face([O(i), O(i + 1), I(j)], [n]*3, group, plane, n); i += 1
        else: mb.face([O(i), I(j + 1), I(j)], [n]*3, group, plane, n); j += 1


DEFAULT = dict(g=0.004, w=0.016, seg=1, margin=0.008, chamfer=0.004, rho=0.030, cseg=1, min_plate=0.05, bulge=0.55)


def _line_x(p0, d0, p1, d1):
    """intersection of 2D lines p0 + t d0 and p1 + s d1"""
    A = np.array([d0, -np.asarray(d1)]).T; t = np.linalg.solve(A, np.asarray(p1) - np.asarray(p0)); return np.asarray(p0) + t[0]*np.asarray(d0)


def build(points, style, plate_faces=None):
    """Constant-width bevel construction (like a Blender OFFSET bevel, so acute edges keep a full contact face):
      P0     = solid with every plane moved in by g           (frame level, g below the contact plane)
      F_i    = each P0 face inset by w IN ITS OWN PLANE         (flat frame face)
      edge   = fillet tangent to both faces at distance w from the edge (radius w*tan(dihedral/2)), seg segments
      plate  = F_i inset by margin, corners rounded/clipped, raised by g -> its top lies exactly ON the contact plane
    returns dict(V, faces=[dict(v, n, group, plane)], planes, K, style); groups: plate | plate_wall | frame | bevel | corner"""
    st = dict(DEFAULT); st.update(style)
    g, w, seg = st["g"], st["w"], st["seg"]; smooth = seg > 1
    planes = planes_from_points(points)
    K = clip_polytope([(n, d - g) for (n, d) in planes])
    assert K is not None, "solid too thin for this style"
    KV, KF = K; mb = MeshBuilder(); corner = {}
    # ---- flat frame faces + plates
    for i, loop in KF.items():
        n = planes[i][0]; u, v, _ = face_frame(n); P3 = np.array([KV[k] for k in loop]); o = P3.mean(0)
        P2 = np.stack([(P3 - o) @ u, (P3 - o) @ v], axis=1); m_ = len(loop); F2 = []
        for k in range(m_):
            pa, pb, pc = P2[k - 1], P2[k], P2[(k + 1) % m_]
            e0 = (pb - pa)/np.linalg.norm(pb - pa); e1 = (pc - pb)/np.linalg.norm(pc - pb)
            n0 = np.array([-e0[1], e0[0]]); n1 = np.array([-e1[1], e1[0]])
            F2.append(_line_x(pa + w*n0, e0, pb + w*n1, e1))
        F2 = np.array(F2)
        for k in range(m_):
            assert (F2[(k + 1) % m_] - F2[k]) @ (P2[(k + 1) % m_] - P2[k]) > 1e-9, "bevel width collapses a face edge"
        F3 = np.array([o + q[0]*u + q[1]*v for q in F2])
        for k, vid in enumerate(loop): corner[(i, vid)] = F3[k]
        plate = None
        if plate_faces is None or plate_faces(n):
            rho = st["rho"]
            while rho >= st["chamfer"] + 0.002:
                S = inset_convex(F2, st["margin"] + rho)
                if S is not None:
                    x, y = S[:, 0], S[:, 1]; area = 0.5*abs(np.dot(x, np.roll(y, -1)) - np.dot(y, np.roll(x, -1)))
                    if area > st["min_plate"]**2 * 0.25 or rho < 0.012: plate = (S, rho); break
                rho *= 0.7
        if plate is None:
            mb.face(list(F3), [n]*len(F3), "frame", i, n); continue
        S, rho = plate
        Q2 = round_polygon(S, rho, st["cseg"]); T2 = round_polygon(S, rho - st["chamfer"], st["cseg"])
        Q3 = np.array([o + q[0]*u + q[1]*v for q in Q2]); T3 = np.array([o + t[0]*u + t[1]*v + g*n for t in T2])
        annulus(mb, F3, Q3, n, "frame", i)
        m = len(Q3)
        def wall_n(j):
            a = np.cross(Q3[(j + 1) % m] - Q3[j], T3[j] - Q3[j]); return a/max(np.linalg.norm(a), 1e-12)
        for k in range(m):
            k1 = (k + 1) % m; wn = wall_n(k)
            if st.get("wall_smooth"):
                def vn(j):
                    a = wall_n(j); b = wall_n(j - 1)
                    return unit(a + b) if a @ b > math.cos(math.radians(50)) else None
                n0 = vn(k); n1 = vn(k1); nk = wn if n0 is None else n0; nk1 = wn if n1 is None else n1
                mb.face([Q3[k], Q3[k1], T3[k1], T3[k]], [nk, nk1, nk1, nk], "plate_wall", i, wn)
            else:
                mb.face([Q3[k], Q3[k1], T3[k1], T3[k]], [wn]*4, "plate_wall", i, wn)
        mb.face(list(T3), [n]*m, "plate", i, n)
    # ---- edge fillets
    edge_faces = {}
    for i, loop in KF.items():
        for k in range(len(loop)):
            a, b = loop[k], loop[(k + 1) % len(loop)]; edge_faces.setdefault((min(a, b), max(a, b)), []).append(i)
    def row(a, b, fi, fj, s):
        """fillet point + normal at the END a of edge (a,b), going from face fi (s=0) to face fj (s=1)"""
        ni, nj = planes[fi][0], planes[fj][0]; A = KV[a]; t = unit(KV[b] - KV[a])
        ci, cj = corner[(fi, a)], corner[(fj, a)]
        qi = ci - A; ti = qi @ t; qi = qi - ti*t; qj = cj - A; tj = qj @ t; qj = qj - tj*t          # in-face offsets (length w) and along-edge slides
        den = np.linalg.norm(nj - ni); rad = np.linalg.norm(qj - qi)/den if den > 1e-9 else 0.0
        O = qi - rad*ni; nn = slerp(ni, nj, s)
        return A + (ti + (tj - ti)*s)*t + O + rad*nn, nn
    for (a, b), fs in edge_faces.items():
        if len(fs) != 2: raise RuntimeError("non-manifold core")
        fi, fj = fs
        for s in range(seg):
            pa0, n0 = row(a, b, fi, fj, s/seg); pa1, n1 = row(a, b, fi, fj, (s + 1)/seg)
            pb0, _ = row(b, a, fi, fj, s/seg); pb1, _ = row(b, a, fi, fj, (s + 1)/seg)
            fn = unit(n0 + n1)
            mb.face([pa0, pa1, pb1, pb0], [n0, n1, n1, n0] if smooth else [fn]*4, "bevel", fi, fn)
    # ---- vertex patches
    vert_faces = {}
    for i, loop in KF.items():
        for k in loop: vert_faces.setdefault(k, []).append(i)
    nbr = {}
    for (a, b), fs in edge_faces.items():
        nbr.setdefault(a, {})[frozenset(fs)] = b; nbr.setdefault(b, {})[frozenset(fs)] = a
    for a, fs in vert_faces.items():
        ns = [planes[i][0] for i in fs]; c = unit(np.sum(ns, axis=0)); u, v, _ = face_frame(c)
        order = np.argsort([math.atan2(n @ v, n @ u) for n in ns]); fs = [fs[k] for k in order]; k_ = len(fs)
        ring = []
        for t in range(k_):
            fi, fj = fs[t], fs[(t + 1) % k_]; b = nbr[a][frozenset((fi, fj))]
            for s in range(seg): ring.append(row(a, b, fi, fj, s/seg))
        pts = [p for p, _ in ring]; nrm = [n for _, n in ring]
        if not smooth and k_ == 3:
            mb.face(pts, [c]*len(pts), "corner", fs[0], c)
        else:
            mean = np.mean(pts, axis=0); ctr = mean + (st["bulge"] if smooth else 0.0)*(KV[a] - mean)*0.5
            for q in range(len(pts)):
                p0, p1 = pts[q], pts[(q + 1) % len(pts)]
                fn = unit(np.cross(p0 - ctr, p1 - ctr)); fn = fn if fn @ c > 0 else -fn
                mb.face([ctr, p0, p1], [c, nrm[q], nrm[(q + 1) % len(pts)]] if smooth else [fn]*3, "corner", fs[0], c)
    return dict(V=np.array(mb.V), faces=mb.F, planes=planes, K=K, style=st)


def lod_build(points, style):
    """LOD1: same silhouette, no plates: faces ON the contact planes, one flat chamfer of width g + w"""
    st = dict(DEFAULT); st.update(style); st.update(w=st["g"] + st["w"], g=0.0, seg=1)
    m = build(points, st, plate_faces=lambda n: False)
    for f in m["faces"]:
        if f["group"] == "frame": f["group"] = "plate"
    return m


def mesh_stats(m):
    """topology report equivalent to build_blocks.mesh_report (manifold / boundary / degenerate / orientation)"""
    edges = {}; tris = 0; zero = 0
    for f in m["faces"]:
        v = f["v"]; tris += len(v) - 2
        P = m["V"][v]; nn = np.zeros(3)
        for i in range(len(P)): nn += np.cross(P[i], P[(i + 1) % len(P)])
        if np.linalg.norm(nn)/2 < 1e-10: zero += 1
        for i in range(len(v)):
            a, b = v[i], v[(i + 1) % len(v)]; edges.setdefault((min(a, b), max(a, b)), []).append(a < b)
    nonman = sum(1 for e in edges.values() if len(e) != 2); bnd = sum(1 for e in edges.values() if len(e) == 1)
    incons = sum(1 for e in edges.values() if len(e) == 2 and e[0] == e[1])
    # signed volume (outward orientation)
    vol = 0.0
    for f in m["faces"]:
        P = m["V"][f["v"]]
        for i in range(1, len(P) - 1): vol += P[0] @ np.cross(P[i], P[i + 1])/6
    return dict(vertices=len(m["V"]), faces=len(m["faces"]), triangles=tris, non_manifold_edges=nonman, boundary_edges=bnd, zero_area_faces=zero,
                inconsistent_normals=incons, volume_m3=round(vol, 6), ok=(nonman == 0 and bnd == 0 and zero == 0 and incons == 0 and vol > 0))


def triangulate(m):
    """per-group flat arrays for GLB: returns {group: (pos Nx3, nrm Nx3, uv Nx2, idx Mx3)} in Blender axes"""
    out = {}
    for f in m["faces"]:
        g = f["group"]; d = out.setdefault(g, dict(pos=[], nrm=[], uv=[], idx=[]))
        n_pl = m["planes"][f["plane"]][0]; u, v, _ = face_frame(n_pl); base = len(d["pos"])
        for vi, nn in zip(f["v"], f["n"]):
            p = m["V"][vi]; d["pos"].append(p); d["nrm"].append(nn); d["uv"].append([p @ u + 0.5, p @ v + 0.5])
        for i in range(1, len(f["v"]) - 1): d["idx"].append([base, base + i, base + i + 1])
    return {g: (np.array(d["pos"]), np.array(d["nrm"]), np.array(d["uv"]), np.array(d["idx"])) for g, d in out.items()}
