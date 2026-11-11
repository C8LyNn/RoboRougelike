"""#1235 R4B_RoundCorner in A_Sleek: one octant of a sphere (radius 0.52 m, centre on the block corner) built as a single shell:
three flat quarter-disc faces with plates, a wrapped spherical plate, constant-width round bevels on all six edges, four corner patches."""
import math
import numpy as np
import r4panel as RP, geo2d as G2
from r4sleek import Mesh, unit

E = np.eye(3)


def arc_between(ci, ni, cj, nj, s):
    den = float(np.linalg.norm(nj - ni)); rad = float(np.linalg.norm(cj - ci))/den
    corr = cj - (ci - rad*ni + rad*nj); nn = RP.slerp(ni, nj, s)
    return ci - rad*ni + rad*nn + s*corr, nn


def build(style, R, corner, ex, ey, ez, lod=False):
    st = dict(RP.DEFAULT); st.update(style)
    g, w, seg, margin, cham, rho, cseg = st["g"], st["w"], st["seg"], st["margin"], st["chamfer"], st["rho"], st["cseg"]
    K = 36
    if lod: w = g + w; g = 0.0; seg = 1; K = 12
    smooth = seg > 1; Rf = R - g; B = np.array([ex, ey, ez], float).T; corner = np.asarray(corner, float)
    mesh = Mesh()
    def W(p): return corner + B @ np.asarray(p, float)
    def WN(n): return B @ np.asarray(n, float)
    def face(pts, nrm, grp, ref=None):
        nr = [WN(n) for n in nrm]; mesh.face([W(p) for p in pts], nr, grp, [(p[0] + p[2]*0.3, p[1] + p[2]*0.3) for p in pts], WN(ref) if ref is not None else unit(sum(nr)))
    # ------------------------------------------------------------ sphere: boundary rings by azimuth around the octant centre
    d0 = unit(np.ones(3)); t1 = unit(E[0] - (E[0] @ d0)*d0); t2 = np.cross(d0, t1)
    def U(r, psi): return math.cos(r)*d0 + math.sin(r)*(math.cos(psi)*t1 + math.sin(psi)*t2)
    def dU(r, psi): return -math.sin(r)*d0 + math.cos(r)*(math.cos(psi)*t1 + math.sin(psi)*t2)
    d_w = math.asin(g/Rf) + w/Rf; d_S = d_w + (margin + rho)/Rf; sS = math.sin(d_S)
    SV = [unit(np.roll(np.array([math.sqrt(max(1 - 2*sS*sS, 0)), sS, sS]), i)) for i in range(3)]
    def dist_S(u):
        viol = [i for i in range(3) if u[i] < sS]
        if not viol: return 0.0
        if len(viol) == 1:
            i = viol[0]; q = u - u[i]*E[i]; p = sS*E[i] + math.cos(d_S)*unit(q)
            if all(p[j] >= sS - 1e-12 for j in range(3)): return d_S - math.asin(u[i])
        return min(math.acos(max(-1, min(1, float(u @ v)))) for v in SV)
    def radial(inside, psi):
        lo, hi = 0.0, 1.3
        for _ in range(60):
            mid = (lo + hi)/2
            if inside(U(mid, psi)): lo = mid
            else: hi = mid
        return lo
    psis = [2*math.pi*k/K for k in range(K)]
    rF = [radial(lambda u: all(u[i] >= math.sin(d_w) for i in range(3)), p) for p in psis]
    F = [Rf*U(r, p) for r, p in zip(rF, psis)]; Fn = [U(r, p) for r, p in zip(rF, psis)]
    if not lod:
        rQ = [radial(lambda u: dist_S(u) <= rho/Rf, p) for p in psis]; rT = [radial(lambda u: dist_S(u) <= (rho - cham)/Rf, p) for p in psis]
        Q = [Rf*U(r, p) for r, p in zip(rQ, psis)]; T = [R*U(r, p) for r, p in zip(rT, psis)]
        for k in range(K):
            k1 = (k + 1) % K
            face([F[k], F[k1], Q[k1], Q[k]], [Fn[k], Fn[k1], U(rQ[k1], psis[k1]), U(rQ[k], psis[k])], "frame")
            wn = [unit(g*dU(rQ[j], psis[j]) + cham*U(rQ[j], psis[j])) for j in (k, k1)]
            face([Q[k], Q[k1], T[k1], T[k]], [wn[0], wn[1], wn[1], wn[0]], "plate_wall")
        rings = [[R*U(f*r, p) for r, p in zip(rT, psis)] for f in (1/3, 2/3, 1.0)]; rn = [[U(f*r, p) for r, p in zip(rT, psis)] for f in (1/3, 2/3, 1.0)]
        grp = "plate"; top = R*d0
    else:
        rings = [[Rf*U(f*r, p) for r, p in zip(rF, psis)] for f in (0.5, 1.0)]; rn = [[U(f*r, p) for r, p in zip(rF, psis)] for f in (0.5, 1.0)]
        grp = "plate"; top = Rf*d0
    for k in range(K):
        k1 = (k + 1) % K
        face([top, rings[0][k], rings[0][k1]], [d0, rn[0][k], rn[0][k1]], grp)
        for a in range(len(rings) - 1):
            face([rings[a][k], rings[a + 1][k], rings[a + 1][k1], rings[a][k1]], [rn[a][k], rn[a + 1][k], rn[a + 1][k1], rn[a][k1]], grp)
    # ------------------------------------------------------------ flat faces, bevels, corners (three-fold symmetry)
    rr = math.sqrt(Rf*Rf - g*g) - w; gw = g + w; xs = math.sqrt(rr*rr - gw*gw); n3 = K//3
    def L(f, x, y, n):                               # face coordinates -> local (a, b, c): face 0 = c (x=a, y=b), 1 = a (x=b, y=c), 2 = b (x=c, y=a)
        p = np.zeros(3); p[(f + 0) % 3 if False else [0, 1, 2][f]] = 0
        ax = [(0, 1, 2), (1, 2, 0), (2, 0, 1)][f]; p[ax[0]] = x; p[ax[1]] = y; p[ax[2]] = n; return p
    flat_arc = {}
    for f in range(3):
        ax = [(0, 1, 2), (1, 2, 0), (2, 0, 1)][f]; nf = -E[ax[2]]; k0 = f*n3
        arc2 = [np.array([xs, gw])]
        for j in range(1, n3):
            u = Fn[(k0 + j) % K]; ph = math.atan2(u[ax[1]], u[ax[0]]); arc2.append(rr*np.array([math.cos(ph), math.sin(ph)]))
        arc2.append(np.array([gw, xs])); flat_arc[f] = arc2
        poly = np.array([np.array([gw, gw])] + arc2)
        plates = []
        if not lod:
            core = G2.drop_collinear(poly); r_ = rho
            while r_ >= cham + 0.004:
                S_ = RP.inset_convex(core, margin + r_)
                if S_ is not None: plates.append((RP.round_polygon(S_, r_, cseg), RP.round_polygon(S_, r_ - cham, cseg))); break
                r_ *= 0.75
        if lod: face([L(f, p[0], p[1], g) for p in poly], [nf]*len(poly), "plate", nf)
        else:
            P2, tris = G2.triangulate(poly, [q for q, _ in plates])
            for t in tris: face([L(f, P2[i][0], P2[i][1], g) for i in t], [nf]*3, "frame", nf)
            for Qp, Tp in plates:
                m = len(Qp); nq = G2.vertex_normals(Qp)
                for k in range(m):
                    k1 = (k + 1) % m
                    wn = [unit(g*L(f, nq[j][0], nq[j][1], 0.0) + cham*nf) for j in (k, k1)]
                    face([L(f, Qp[k][0], Qp[k][1], g), L(f, Qp[k1][0], Qp[k1][1], g), L(f, Tp[k1][0], Tp[k1][1], 0.0), L(f, Tp[k][0], Tp[k][1], 0.0)], [wn[0], wn[1], wn[1], wn[0]], "plate_wall")
                face([L(f, p[0], p[1], 0.0) for p in Tp], [nf]*m, "plate", nf)
    def row(ci, ni, cj, nj): return [arc_between(ci, ni, cj, nj, s/seg) for s in range(seg + 1)]
    def strip(r0, r1):
        for s in range(seg):
            pts = [r0[s][0], r0[s + 1][0], r1[s + 1][0], r1[s][0]]; nr = [r0[s][1], r0[s + 1][1], r1[s + 1][1], r1[s][1]]
            fn = unit(sum(nr)); face(pts, nr if smooth else [fn]*4, "bevel", fn)
    def patch(ring, cn):
        pts = [p for p, _ in ring]; nr = [n for _, n in ring]; mean = np.mean(pts, axis=0)
        if not smooth and len(pts) == 3: face(pts, [cn]*3, "corner", cn); return
        ctr = mean + (0.0 if not smooth else 0.30*w)*cn
        for q in range(len(pts)):
            q1 = (q + 1) % len(pts); fn = unit(np.cross(pts[q] - ctr, pts[q1] - ctr)); fn = fn if fn @ cn > 0 else -fn
            face([ctr, pts[q], pts[q1]], [cn, nr[q], nr[q1]] if smooth else [fn]*3, "corner", cn)
    sph_rows = {}; edge_rows = {}
    for f in range(3):
        ax = [(0, 1, 2), (1, 2, 0), (2, 0, 1)][f]; nf = -E[ax[2]]; ny = -E[ax[1]]; k0 = f*n3
        rows = [row(F[(k0 + j) % K], Fn[(k0 + j) % K], L(f, flat_arc[f][j][0], flat_arc[f][j][1], g), nf) for j in range(n3 + 1)]
        for j in range(n3): strip(rows[j], rows[j + 1])
        sph_rows[f] = rows
        e0 = row(L(f, gw, gw, g), nf, L(f, gw, g, gw), ny); e1 = row(L(f, xs, gw, g), nf, L(f, xs, g, gw), ny); strip(e0, e1); edge_rows[f] = (e0, e1)
    # origin corner (three flat faces)
    ring = []
    for f in (0, 2, 1): ring += edge_rows[f][0][:-1]
    patch(ring, -d0)
    # outer corners: axis-edge bevel of face f meets the sphere near +e_x of face f; the other face there is (f + 2) % 3
    for f in range(3):
        o = (f + 2) % 3; e1 = edge_rows[f][1]; r_o = sph_rows[o][-1]; r_f = sph_rows[f][0]
        ring = e1[:-1] + [x for x in reversed(r_o)][:-1] + r_f[:-1]
        patch(ring, unit(-E[[(0, 1, 2), (1, 2, 0), (2, 0, 1)][f][2]] - E[[(0, 1, 2), (1, 2, 0), (2, 0, 1)][f][1]] + Fn[(f*n3) % K]))
    return mesh.result(st)
