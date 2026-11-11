"""#1235: the pilot's convex 'plates on a constant-width bevelled frame' generator (scripts/r4panel.build of #1232, copied unchanged)
with ONE addition: g_face(normal, g) lets a face use its own frame depth. It is needed where four planes meet in one vertex
(InnerCorner): the cut face is offset by g/sqrt(3) so the offset planes stay concurrent and no sliver edge appears; its plate is raised
by the same amount, so the plate top still lies exactly on the approved plane."""
import math
import numpy as np
from r4panel import (unit, planes_from_points, face_frame, clip_polytope, slerp, inset_convex, round_polygon, MeshBuilder, annulus, DEFAULT, _line_x)


def build(points, style, plate_faces=None, g_face=None):
    """Constant-width bevel construction (like a Blender OFFSET bevel, so acute edges keep a full contact face):
      P0     = solid with every plane moved in by g           (frame level, g below the contact plane)
      F_i    = each P0 face inset by w IN ITS OWN PLANE         (flat frame face)
      edge   = fillet tangent to both faces at distance w from the edge (radius w*tan(dihedral/2)), seg segments
      plate  = F_i inset by margin, corners rounded/clipped, raised by g -> its top lies exactly ON the contact plane
    returns dict(V, faces=[dict(v, n, group, plane)], planes, K, style); groups: plate | plate_wall | frame | bevel | corner"""
    st = dict(DEFAULT); st.update(style)
    g, w, seg = st["g"], st["w"], st["seg"]; smooth = seg > 1
    planes = planes_from_points(points)
    gf = [g if g_face is None else g_face(n, g) for (n, d) in planes]
    K = clip_polytope([(n, d - gi) for (n, d), gi in zip(planes, gf)])
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
        Q3 = np.array([o + q[0]*u + q[1]*v for q in Q2]); T3 = np.array([o + t[0]*u + t[1]*v + gf[i]*n for t in T2])
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


def lod_build(points, style, g_face=None):
    st = dict(DEFAULT); st.update(style); st.update(w=st["g"] + st["w"], g=0.0, seg=1)
    m = build(points, st, plate_faces=lambda n: False)
    for f in m["faces"]:
        if f["group"] == "frame": f["group"] = "plate"
    return m
