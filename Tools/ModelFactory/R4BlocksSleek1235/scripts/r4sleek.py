"""#1235 A_Sleek rollout - generators for the 25 remaining R4 building blocks (pure numpy; continues scripts/r4panel.py of the #1232 pilot).

  prism()    extruded 2D profile (convex or concave, straight or arc edges, optional cell seams)  -> one watertight shell
  revolve()  solid of revolution from a hand-authored (r, z) section
The visual language is the pilot's: frame g below the contact surface, constant-width (w) round bevels, one raised plate per face /
per logical cell whose top lies exactly ON the approved contact plane, rounded plate corners (rho), chamfered plate shoulder.
Blender axes: X right, Y back (-Y forward), Z up."""
import math, os, sys
import numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "R4BlocksRedesign1232", "scripts"))
import r4panel as RP
import geo2d as G2

unit = RP.unit


def rot2(v): return np.array([v[1], -v[0]])            # tangent (CCW) -> outward normal


def slerp2(a, b, t):
    a = np.asarray(a, float); b = np.asarray(b, float); w = math.acos(max(-1.0, min(1.0, float(a @ b))))
    if w < 1e-9: return a
    return (math.sin((1 - t)*w)*a + math.sin(t*w)*b)/math.sin(w)


def miter(n0, n1):
    d = 1.0 + float(np.dot(n0, n1)); return (np.asarray(n0) + np.asarray(n1))/max(d, 1e-3)


class Mesh:
    def __init__(self): self.V = []; self._key = {}; self.F = []
    def vid(self, p):
        k = tuple(np.round(np.asarray(p, float)/2e-7).astype(np.int64))
        if k not in self._key: self._key[k] = len(self.V); self.V.append(np.asarray(p, float))
        return self._key[k]
    def face(self, pts, normals, group, uv=None, ref=None):
        ids = [self.vid(p) for p in pts]; n = len(ids)
        keep = [i for i in range(n) if ids[i] != ids[i - 1]]
        if len(keep) < 3: return False
        ids = [ids[i] for i in keep]; normals = [unit(normals[i]) for i in keep]; uv = [np.asarray(uv[i], float) for i in keep] if uv is not None else [np.zeros(2)]*len(keep)
        if len(set(ids)) < len(ids):                                  # pinched polygon (collapsed ring): keep first occurrences
            seen = set(); k2 = []
            for i, v in enumerate(ids):
                if v not in seen: seen.add(v); k2.append(i)
            ids = [ids[i] for i in k2]; normals = [normals[i] for i in k2]; uv = [uv[i] for i in k2]
            if len(ids) < 3: return False
        P = np.array([self.V[i] for i in ids]); nn = np.zeros(3)
        for i in range(len(P)): nn += np.cross(P[i], P[(i + 1) % len(P)])
        if np.linalg.norm(nn) < 2e-11: return False
        ref = np.mean(normals, axis=0) if ref is None else ref
        if nn @ ref < 0: ids = ids[::-1]; normals = normals[::-1]; uv = uv[::-1]
        self.F.append(dict(v=ids, n=normals, uv=uv, group=group)); return True
    def result(self, style, **extra):
        d = dict(V=np.array(self.V), faces=self.F, style=style); d.update(extra); return d


# =============================================================================================== profile description
def P_line(p): return dict(p=np.asarray(p, float), arc=None)


def P_arc(center, r, a0, a1, n, convex=True):
    """arc vertices from angle a0 to a1 (degrees). convex: material inside the circle (outward normal = radial), inscribed polygon with n chords.
    concave: material outside (outward normal = -radial); the polygon is CIRCUMSCRIBED (tangent polygon: end points on the circle, n vertices
    between them at r / cos(half step)), so no facet ever enters the void."""
    out = []; c = np.asarray(center, float)
    if convex:
        for i in range(n + 1):
            a = math.radians(a0 + (a1 - a0)*i/n); d = np.array([math.cos(a), math.sin(a)]); out.append(dict(p=c + r*d, arc=d))
        return out
    h = math.radians(abs(a1 - a0)/n)/2; angs = [a0] + [a0 + (a1 - a0)*(i + 0.5)/n for i in range(n)] + [a1]
    for i, ad in enumerate(angs):
        a = math.radians(ad); d = np.array([math.cos(a), math.sin(a)])
        out.append(dict(p=c + (r if i in (0, len(angs) - 1) else r/math.cos(h))*d, arc=-d))
    return out


def prism(profile, t0, t1, axes, style, seams_ab=(), seams_t=(), edge_plate=None, caps=("auto", "auto"), cap_seams=None, lod=False, cap_holes=(None, None),
          cap_custom=(None, None), tangent_seams=True, open_profile=False, outline_only=False):
    """profile: CCW list of dict(p, arc) (arc = analytic outward normal for vertices on an arc, None for corners of straight edges).
    axes = (e_a, e_b, e_t, origin): 3D directions of the two profile axes and of the extrusion (right-handed) and the 3D origin.
    seams_ab: [(axis 0|1, value)] cell boundaries in profile coordinates; seams_t: cell boundaries along the extrusion.
    edge_plate(i, p0, p1) -> False to suppress the plate of the run that contains original edge i. caps: per end 'auto' | 'none'.
    returns mesh dict; mesh['cap_hole_loops'] = 3D loops of requested cap holes"""
    st = dict(RP.DEFAULT); st.update(style)
    g, w, seg, margin, cham, rho0, cseg = st["g"], st["w"], st["seg"], st["margin"], st["chamfer"], st["rho"], st["cseg"]
    seam_h = st.get("seam_h", 0.006); min_plate = st.get("min_plate", 0.05); smooth = seg > 1
    if lod: w = g + w; g = 0.0; seg = 1; smooth = False
    e_a, e_b, e_t, org = [np.asarray(x, float) for x in axes]
    cap_seams = seams_ab if cap_seams is None else cap_seams
    # ---------------------------------------------------------------- 1. vertices with in/out normals, seam nodes
    V = [dict(p=np.asarray(v["p"], float), arc=v.get("arc")) for v in profile]; n0 = len(V)
    assert G2.area2([v["p"] for v in V]) > 0, "profile must be CCW"
    # open_profile (revolve sections): the closing edge last -> first is virtual, the two end vertices keep one normal and get no bevel
    edges = []
    for i in range(n0):
        a, b = V[i], V[(i + 1) % n0]; is_arc = a["arc"] is not None and b["arc"] is not None
        if is_arc: edges.append(dict(n0=unit(a["arc"]), n1=unit(b["arc"]), arc=True, src=i))
        else:
            nn = unit(rot2(b["p"] - a["p"])); edges.append(dict(n0=nn, n1=nn, arc=False, src=i))
    nodes = []                                                        # after seam insertion
    for i in range(n0):
        nodes.append(dict(p=V[i]["p"], seam=False, e=edges[i]))
        if not edges[i]["arc"] and not lod:
            a, b = V[i]["p"], V[(i + 1) % n0]["p"]; cuts = []
            for (ax, val) in seams_ab:
                lo, hi = sorted((a[ax], b[ax]))
                if lo + 1e-6 < val < hi - 1e-6: cuts.append(((val - a[ax])/(b[ax] - a[ax]), ax))
            for f, ax in sorted(cuts): nodes.append(dict(p=a + (b - a)*f, seam=True, e=edges[i], sax=ax))
    N = len(nodes)
    if open_profile: nodes[-1]["e"] = dict(nodes[-1]["e"]); nodes[-1]["e"]["virtual"] = True
    for k in range(N):
        nd = nodes[k]; pe = nodes[k - 1]["e"]; ne = nd["e"]
        if open_profile and k in (0, N - 1):
            nn_ = ne["n0"] if k == 0 else pe["n1"]
            nd.update(n_in=nn_, n_out=nn_, hard=0, arcseg=(ne["arc"] if k == 0 else False), src=ne["src"], end=True)
            if abs(nd["p"][0]) < 1e-9: nd["sax"] = 0                     # stays on the axis of revolution
            continue
        same_prev = pe is ne or (nodes[k - 1]["e"]["src"] == ne["src"])
        nd["n_out"] = ne["n0"] if not nd["seam"] else ne["n0"]
        nd["n_in"] = pe["n1"] if not nd["seam"] else ne["n0"]
        if ne["arc"] is False: nd["n_out"] = ne["n0"]
        if pe["arc"] is False and not nd["seam"]: nd["n_in"] = pe["n1"]
        cr = nd["n_in"][0]*nd["n_out"][1] - nd["n_in"][1]*nd["n_out"][0]; dt = float(nd["n_in"] @ nd["n_out"])
        nd["hard"] = 0 if (abs(cr) < 1e-6 and dt > 0) else (1 if cr > 0 else -1)                   # 1 convex, -1 concave, 0 soft
        if nd["hard"] == 0 and not nd["seam"] and tangent_seams and (pe["arc"] != ne["arc"]) and not lod: nd["seam"] = True     # tangent join arc/flat
        nd["arcseg"] = ne["arc"]; nd["src"] = ne["src"]
    # ---------------------------------------------------------------- 2. frame offset (g) and fillets (w)
    def seam_off(nd, a):
        """inward offset of a seam node that stays ON its seam line (slides along the edge)"""
        n_ = nd["n_out"]; off = -a*n_
        ax = nd.get("sax")
        if ax is not None:
            tau = np.array([-n_[1], n_[0]])
            if abs(tau[ax]) > 1e-9: off = off + (a*n_[ax]/tau[ax])*tau
        return off
    for nd in nodes: nd["q"] = nd["p"] + (seam_off(nd, g) if nd.get("sax") is not None else -g*miter(nd["n_in"], nd["n_out"]))
    def seg_len(k): return float(np.linalg.norm(nodes[(k + 1) % N]["q"] - nodes[k]["q"]))
    O = []                                                            # final outline nodes
    def walk(k, direction, dist):
        """point at arc length dist from node k along the outline (direction +1 forward / -1 backward); returns (point, normal, consumed nodes, frac info)"""
        used = []; cur = k
        while True:
            a = cur if direction > 0 else (cur - 1) % N; L = seg_len(a)
            if L >= dist + 1e-9 or True and L >= dist - 1e-12:
                A = nodes[a]; B = nodes[(a + 1) % N]; f = dist/L if direction > 0 else 1 - dist/L
                p = A["q"] + (B["q"] - A["q"])*f; nn = slerp2(A["n_out"], B["n_in"], f) if A["arcseg"] else A["n_out"]
                return p, nn, used
            dist -= L; cur = (cur + direction) % N; used.append(cur)
            assert nodes[cur]["hard"] == 0, "bevel width longer than the edge next to a corner"
    drop = set(); fil = {}
    for k in range(N):
        if nodes[k]["hard"] == 1:
            pi, ni, u1 = walk(k, -1, w); pj, nj, u2 = walk(k, +1, w); drop.update(u1); drop.update(u2)
            den = float(np.linalg.norm(nj - ni)); rad = float(np.linalg.norm(pj - pi))/den
            corr = pj - (pi - rad*ni + rad*nj)
            arc = [(pi - rad*ni + rad*slerp2(ni, nj, s/seg) + (s/seg)*corr, slerp2(ni, nj, s/seg)) for s in range(seg + 1)]
            fil[k] = dict(arc=arc, rad=rad, Vg=nodes[k]["q"], m=miter(nodes[k]["n_in"], nodes[k]["n_out"]))
    for k in range(N):
        if k in drop: continue
        nd = nodes[k]
        if k in fil:
            f = fil[k]
            for s, (p, nn) in enumerate(f["arc"]):
                O.append(dict(q=p, n_in=nn, n_out=nn, kind="fillet" if s < seg else "run", fil=f, seam=False, hard=0, bound=(s in (0, seg)), src=nd["src"] if s == seg else None,
                              arcseg=nd["arcseg"] if s == seg else False, concave=False, end=False))
            O[-1]["src"] = nd["src"]
        else:
            O.append(dict(q=nd["q"], n_in=nd["n_in"], n_out=nd["n_out"], kind="virtual" if nd["e"].get("virtual") else "run", fil=None, seam=nd["seam"], hard=nd["hard"],
                          bound=(nd["hard"] == -1 or bool(nd.get("end"))), src=nd["src"], arcseg=nd["arcseg"], concave=nd["hard"] == -1, sax=nd.get("sax"), end=bool(nd.get("end"))))
    M = len(O)
    for k in range(M):                                                # the segment before a fillet start keeps the source edge of the previous node
        if O[k]["src"] is None: O[k]["src"] = -1
    # segment k: O[k] -> O[k+1]; kind = O[k]['kind']
    def ring(k, a):
        nd = O[k % M]
        if nd["fil"] is not None:
            f = nd["fil"]; return f["Vg"] - a*f["m"] + max(0.0, 1.0 - a/f["rad"])*(nd["q"] - f["Vg"])
        if nd.get("sax") is not None: return nd["q"] + seam_off(nd, a)
        return nd["q"] - a*miter(nd["n_in"], nd["n_out"])
    if outline_only: return O, dict(g=g, w=w, seg=seg, margin=margin, cham=cham, rho=rho0, seam_h=seam_h, min_plate=min_plate, smooth=smooth, st=st)
    def to3(p2, t): return org + p2[0]*e_a + p2[1]*e_b + t*e_t
    def n3(n2): return n2[0]*e_a + n2[1]*e_b
    mesh = Mesh(); ulen = [0.0]
    for k in range(M): ulen.append(ulen[-1] + float(np.linalg.norm(O[(k + 1) % M]["q"] - O[k]["q"])))
    # ---------------------------------------------------------------- 3. cap bevel rings
    tc = (t0 + g, t1 - g); t_lo, t_hi = tc[0] + w, tc[1] - w
    assert t_hi - t_lo > -1e-9, "prism too short for the bevel"
    for ci, sgn in ((0, -1.0), (1, 1.0)):
        ncap = sgn*e_t
        for j in range(seg):
            th0, th1 = math.pi/2*j/seg, math.pi/2*(j + 1)/seg
            a0, a1 = w*(1 - math.cos(th0)), w*(1 - math.cos(th1)); x0, x1 = tc[ci] - sgn*w*(1 - math.sin(th0)), tc[ci] - sgn*w*(1 - math.sin(th1))
            for k in range(M):
                A, B = O[k], O[(k + 1) % M]
                na, nb = n3(A["n_out"]), n3(B["n_in"])
                pts = [to3(ring(k, a0), x0), to3(ring(k + 1, a0), x0), to3(ring(k + 1, a1), x1), to3(ring(k, a1), x1)]
                nr = [math.cos(th0)*na + math.sin(th0)*ncap, math.cos(th0)*nb + math.sin(th0)*ncap, math.cos(th1)*nb + math.sin(th1)*ncap, math.cos(th1)*na + math.sin(th1)*ncap]
                fn = unit(sum(nr)); uv = [(ulen[k], x0), (ulen[k + 1], x0), (ulen[k + 1], x1), (ulen[k], x1)]
                mesh.face(pts, nr if smooth else [fn]*4, "bevel", uv, fn)
    # ---------------------------------------------------------------- 4. side surface
    tb = sorted(v for v in seams_t if t_lo + 1e-6 < v < t_hi - 1e-6) if not lod else []
    stations = [t_lo] + tb + [t_hi]
    def plate_parts(S, rho, loop_for_T=None):
        Q = RP.round_polygon(S, rho, cseg); T = RP.round_polygon(S, rho - cham, cseg); return Q, T
    def emit_plate(Q, T, lines, mapf, uvf, outer=None, holes_extra=()):
        """frame ring is emitted by the caller; this emits wall + top of one plate. Q/T: 2D loops (same vertex count), lines: internal split values (axis 0)"""
        m = len(Q); nq = G2.vertex_normals(Q)
        def cross_pts(a, b):
            out = []
            for v in lines:
                lo, hi = sorted((a[0], b[0]))
                if lo + 1e-9 < v < hi - 1e-9: f = (v - a[0])/(b[0] - a[0]); out.append((f, a + (b - a)*f))
            return [p for _, p in sorted(out, key=lambda x: x[0])]
        def wall_n(p2, o2):
            P_, Ns, tau = mapf(p2, 0.0, True); o3 = o2[0]*tau + o2[1]*e_t; return unit(g*o3 + cham*Ns)
        for k in range(m):
            k1 = (k + 1) % m; bot = [Q[k]] + cross_pts(Q[k], Q[k1]) + [Q[k1]]; top = [T[k]] + cross_pts(T[k], T[k1]) + [T[k1]]
            e = Q[k1] - Q[k]; en = unit(rot2(e)) if np.linalg.norm(e) > 1e-12 else nq[k]
            def nrm_at(i, lst, base):
                if not st.get("wall_smooth") or not smooth: return wall_n(lst[i], en)
                if i == 0: return wall_n(lst[i], nq[k])
                if i == len(lst) - 1: return wall_n(lst[i], nq[k1])
                return wall_n(lst[i], en)
            if len(bot) == 2 and len(top) == 2:
                pts = [mapf(bot[0], 0.0)[0], mapf(bot[1], 0.0)[0], mapf(top[1], g)[0], mapf(top[0], g)[0]]
                nr = [nrm_at(0, bot, 0), nrm_at(1, bot, 0), nrm_at(1, top, 1), nrm_at(0, top, 1)]
                mesh.face(pts, nr, "plate_wall", [uvf(bot[0]), uvf(bot[1]), uvf(top[1]), uvf(top[0])], unit(sum(nr)))
            else:
                loop = [(p, 0.0, nrm_at(i, bot, 0)) for i, p in enumerate(bot)] + [(p, g, nrm_at(i, top, 1)) for i, p in reversed(list(enumerate(top)))]
                P2, tris = G2.triangulate(np.array([q[0] for q in loop]))
                def find(p):
                    for q in loop:
                        if np.linalg.norm(q[0] - p) < 3e-7: return q
                    raise RuntimeError("wall vertex")
                for t in tris:
                    qs = [find(P2[i]) for i in t]
                    mesh.face([mapf(q[0], q[1])[0] for q in qs], [q[2] for q in qs], "plate_wall", [uvf(q[0]) for q in qs], unit(sum(q[2] for q in qs)))
        pieces = G2.split_by_lines(None, [T], 0, lines) if lines else [T]
        for pc in pieces:
            pts = []; nr = []
            for p in pc:
                P_, Ns = mapf(p, g); pts.append(P_); nr.append(Ns)
            mesh.face(pts, nr, "plate", [uvf(p) for p in pc], unit(sum(nr)))
    def emit_frame(outer, holes, lines, mapf, uvf):
        P2, tris = G2.triangulate(outer, holes)
        for t in tris:
            polys = G2.split_by_lines(None, [P2[list(t)]], 0, lines) if lines else [P2[list(t)]]
            for pc in polys:
                pts = []; nr = []
                for p in pc:
                    P_, Ns = mapf(p, 0.0); pts.append(P_); nr.append(Ns)
                mesh.face(pts, nr, "frame", [uvf(p) for p in pc], unit(sum(nr)))
    # runs
    k = 0; starts = [i for i in range(M) if O[i]["kind"] == "run" and (O[i]["bound"] or O[i]["fil"] is not None)]
    assert starts, "profile needs at least one hard corner (closed smooth profiles: use revolve)"
    visited = set()
    for s0 in starts:
        if O[s0]["kind"] != "run": continue
        idx = [s0]; k = s0
        while True:
            k = (k + 1) % M; idx.append(k)
            if O[k]["bound"] or O[k]["fil"] is not None or O[k]["kind"] == "fillet": break
        if len(idx) < 2 or (idx[0], idx[1]) in visited: continue
        visited.add((idx[0], idx[1]))
        S = [0.0]
        for a, b in zip(idx[:-1], idx[1:]): S.append(S[-1] + float(np.linalg.norm(O[b]["q"] - O[a]["q"])))
        curved = any(O[a]["arcseg"] for a in idx[:-1]); u0 = ulen[idx[0]]
        srcs = {O[a]["src"] for a in idx[:-1]}
        want = not lod and (edge_plate is None or all(edge_plate(sx) for sx in srcs if sx is not None and sx >= 0))
        def mapf(p2, lift, tangent=False, idx=idx, S=S):
            s = min(max(p2[0], S[0]), S[-1]); j = int(np.searchsorted(S, s, side="right")) - 1; j = min(max(j, 0), len(S) - 2)
            A, B = O[idx[j]], O[idx[j + 1]]; f = (s - S[j])/(S[j + 1] - S[j]) if S[j + 1] > S[j] else 0.0
            nn = slerp2(A["n_out"], B["n_in"], f) if A["arcseg"] else A["n_out"]
            qa = A["q"] + lift*A["n_out"]; qb = B["q"] + lift*(B["n_in"] if A["arcseg"] else A["n_out"])           # lifted chord: stays on the tangent / inscribed polygon
            P_ = to3(qa + (qb - qa)*f, p2[1]); Ns = n3(nn)
            if tangent: return P_, Ns, n3(np.array([-nn[1], nn[0]]))
            return P_, Ns
        uvf = lambda p2, u0=u0: (u0 + p2[0], p2[1])
        # s panels
        cuts = [0] + [i for i in range(1, len(idx) - 1) if O[idx[i]]["seam"]] + [len(idx) - 1]
        for ti in range(len(stations) - 1):
            ta, tb_ = stations[ti], stations[ti + 1]
            for ci in range(len(cuts) - 1):
                ia, ib = cuts[ci], cuts[ci + 1]; sa, sb = S[ia], S[ib]
                def endm(i):
                    nd = O[idx[i]]
                    if 0 < i < len(idx) - 1: return seam_h
                    return margin + (g + cham + 0.004 if nd["concave"] else 0.0)
                ms0, ms1 = endm(ia), endm(ib); mt0 = margin if ti == 0 else seam_h; mt1 = margin if ti == len(stations) - 2 else seam_h
                lines = [S[i] for i in range(ia + 1, ib)]
                outer = [(S[i], ta) for i in range(ia, ib + 1)] + [(S[i], tb_) for i in range(ib, ia - 1, -1)]
                ws, hs = sb - sa - ms0 - ms1, tb_ - ta - mt0 - mt1
                if want and ws >= min_plate and hs >= min_plate:
                    rho = min(rho0, min(ws, hs)/2 - 0.003)
                    Srect = np.array([(sa + ms0 + rho, ta + mt0 + rho), (sb - ms1 - rho, ta + mt0 + rho), (sb - ms1 - rho, tb_ - mt1 - rho), (sa + ms0 + rho, tb_ - mt1 - rho)])
                    Q, T = plate_parts(Srect, rho)
                    emit_frame(np.array(outer), [Q], lines, mapf, uvf); emit_plate(Q, T, lines, mapf, uvf)
                else:
                    for i in range(ia, ib):
                        quad = [np.array((S[i], ta)), np.array((S[i + 1], ta)), np.array((S[i + 1], tb_)), np.array((S[i], tb_))]
                        pts = []; nr = []
                        for p in quad:
                            # evaluate with the normal of THIS segment at shared nodes
                            A, B = O[idx[i]], O[idx[i + 1]]; f = 0.0 if p[0] == S[i] else 1.0
                            nn = (A["n_out"] if f == 0.0 else B["n_in"]); q = A["q"] if f == 0.0 else B["q"]
                            pts.append(to3(q, p[1])); nr.append(n3(nn))
                        mesh.face(pts, nr, "frame", [uvf(p) for p in quad], unit(sum(nr)))
    # fillet strips along the extrusion
    for k in range(M):
        if O[k]["kind"] != "fillet": continue
        A, B = O[k], O[(k + 1) % M]
        for ti in range(len(stations) - 1):
            ta, tb_ = stations[ti], stations[ti + 1]
            pts = [to3(A["q"], ta), to3(B["q"], ta), to3(B["q"], tb_), to3(A["q"], tb_)]; na, nb = n3(A["n_out"]), n3(B["n_in"]); fn = unit(na + nb)
            mesh.face(pts, [na, nb, nb, na] if smooth else [fn]*4, "bevel", [(ulen[k], ta), (ulen[k + 1], ta), (ulen[k + 1], tb_), (ulen[k], tb_)], fn)
    # ---------------------------------------------------------------- 5. caps
    hole_loops = [None, None]
    C2 = G2.dedupe([ring(k, w) for k in range(M)])
    for ci, sgn in ((0, -1.0), (1, 1.0)):
        ncap = sgn*e_t; lvl = tc[ci]
        mapc = lambda p2, lift, tangent=False, lvl=lvl, sgn=sgn, ncap=ncap: (to3(p2, lvl + sgn*lift), ncap) if not tangent else (to3(p2, lvl + sgn*lift), ncap, None)
        uvc = lambda p2: (p2[0], p2[1])
        def wall_cap(Q, T):
            m = len(Q); nq = G2.vertex_normals(Q)
            for k in range(m):
                k1 = (k + 1) % m; e = Q[k1] - Q[k]; en = unit(rot2(e)) if np.linalg.norm(e) > 1e-12 else nq[k]
                def wn(o2): return unit(g*n3(o2) + cham*ncap)
                sm = st.get("wall_smooth") and smooth
                nr = [wn(nq[k] if sm else en), wn(nq[k1] if sm else en), wn(nq[k1] if sm else en), wn(nq[k] if sm else en)]
                mesh.face([to3(Q[k], lvl), to3(Q[k1], lvl), to3(T[k1], lvl + sgn*g), to3(T[k], lvl + sgn*g)], nr, "plate_wall", [uvc(Q[k]), uvc(Q[k1]), uvc(T[k1]), uvc(T[k])], unit(sum(nr)))
        mode = caps[ci]; holes = [np.asarray(h, float) for h in (cap_holes[ci] or [])]
        pieces = [C2]
        if mode == "auto" and not lod:
            for (ax, val) in cap_seams:
                nxt = []; nn_ = np.zeros(2); nn_[ax] = 1.0
                for pc in pieces:
                    if pc[:, ax].min() < val - 1e-6 and pc[:, ax].max() > val + 1e-6:
                        for part in (G2.clip_halfplane(pc, nn_, val), G2.clip_halfplane(pc, -nn_, -val)):
                            if part is not None: nxt.append(part)
                    else: nxt.append(pc)
                pieces = nxt
        for pc in pieces:
            plates = []
            if lod or mode == "none": pass
            elif cap_custom[ci] is not None:
                for S_, rho in cap_custom[ci](pc): plates.append(plate_parts(np.asarray(S_), rho))
            elif mode == "auto":
                core = G2.drop_collinear(pc)
                if G2.is_convex(core):
                    m = len(core); d = []
                    for i in range(m):
                        a, b = core[i], core[(i + 1) % m]
                        d.append(seam_h if any(abs(a[ax] - val) < 1e-6 and abs(b[ax] - val) < 1e-6 for (ax, val) in cap_seams) else margin)
                    rho = rho0
                    while rho >= cham + 0.004:
                        S_ = RP.inset_convex(core, np.array(d) + rho)
                        if S_ is not None:
                            ext = S_.max(0) - S_.min(0) + 2*rho
                            if min(ext) >= min_plate: plates.append(plate_parts(S_, rho)); break
                        rho *= 0.75
                else:
                    for Q in G2.raster_open(pc, erode=margin, rho=min(rho0, st.get("rho_cap", rho0))):
                        if min(Q.max(0) - Q.min(0)) >= min_plate: plates.append((Q, G2.miter_inset(Q, cham)))
            hl = [Q for Q, _ in plates] + holes
            if lod and not holes: mesh.face([to3(p, lvl) for p in pc], [ncap]*len(pc), "plate" if mode != "none" else "frame", [uvc(p) for p in pc], ncap) if G2.is_convex(pc) else emit_frame(pc, hl, [], mapc, uvc)
            else: emit_frame(pc, hl, [], mapc, uvc)
            for Q, T in plates:
                wall_cap(Q, T)
                if G2.is_convex(T): mesh.face([to3(p, lvl + sgn*g) for p in T], [ncap]*len(T), "plate", [uvc(p) for p in T], ncap)
                else:
                    P2, tris = G2.triangulate(T)
                    for t in tris: mesh.face([to3(P2[i], lvl + sgn*g) for i in t], [ncap]*3, "plate", [uvc(P2[i]) for i in t], ncap)
        if holes: hole_loops[ci] = [[to3(p, lvl) for p in h] for h in holes]
    return mesh.result(st, cap_hole_loops=hole_loops, cap_level=tc, outline=[o["q"] for o in O])


def merge(mesh, m):
    """append the faces of a finished mesh dict to a Mesh under construction (welds coincident vertices)"""
    for f in m["faces"]:
        mesh.face([m["V"][i] for i in f["v"]], f["n"], f["group"], f.get("uv"), ref=None if len(f["v"]) < 3 else _fn(m["V"][f["v"]]))


def _fn(P):
    nn = np.zeros(3)
    for i in range(len(P)): nn += np.cross(P[i], P[(i + 1) % len(P)])
    return nn


def revolve_section(mesh, section, N, phase=0.0):
    """section: list of dict(p=(r, z), n=(nr, nz)) pairs per segment: [(A, B, group)], A/B = (p2, n2). Revolved about +Z in N steps."""
    ang = [phase + 2*math.pi*i/N for i in range(N + 1)]
    def P3(p, a): return np.array([p[0]*math.cos(a), p[0]*math.sin(a), p[1]])
    def N3(n, a): return np.array([n[0]*math.cos(a), n[0]*math.sin(a), n[1]])
    u = 0.0
    for (pa, na), (pb, nb), grp in section:
        L = float(np.linalg.norm(np.asarray(pb) - np.asarray(pa)))
        for i in range(N):
            a0, a1 = ang[i], ang[i + 1]
            pts = [P3(pa, a0), P3(pa, a1), P3(pb, a1), P3(pb, a0)]; nr = [N3(na, a0), N3(na, a1), N3(nb, a1), N3(nb, a0)]
            rr = max(pa[0], pb[0]); uv = [(rr*a0, u), (rr*a1, u), (rr*a1, u + L), (rr*a0, u + L)]
            mesh.face(pts, nr, grp, uv, unit(sum(nr)))
        u += L


def revolve(profile, N, style, lod=False, plate_ok=None, mesh=None):
    """solid of revolution about +Z. profile: OPEN CCW polyline in (r, z) from the bottom axis point to the top axis point (same vertex
    dicts as prism profiles). Bevels at hard corners, one plate BAND per run (disc plates where a run reaches the axis)."""
    O, c = prism(profile, 0.0, 1.0, (np.array([1.0, 0, 0]), np.array([0, 1.0, 0]), np.array([0, 0, 1.0]), np.zeros(3)), style, lod=lod, open_profile=True, outline_only=True)
    g, margin, cham, seam_h, min_plate = c["g"], c["margin"], c["cham"], c["seam_h"], c["min_plate"]; M = len(O); sec = []
    k = 0
    while k < M - 1:
        if O[k]["kind"] == "fillet":
            sec.append(((O[k]["q"], O[k]["n_out"]), (O[k + 1]["q"], O[k + 1]["n_in"]), "bevel")); k += 1; continue
        idx = [k]
        while True:
            k += 1; idx.append(k)
            if k == M - 1 or O[k]["bound"] or O[k]["fil"] is not None or O[k]["kind"] != "run": break
        S = [0.0]
        for a, b in zip(idx[:-1], idx[1:]): S.append(S[-1] + float(np.linalg.norm(O[b]["q"] - O[a]["q"])))
        def ev(s, lift, idx=idx, S=S):
            j = int(np.searchsorted(S, s, side="right")) - 1; j = min(max(j, 0), len(S) - 2)
            A, B = O[idx[j]], O[idx[j + 1]]; f = (s - S[j])/(S[j + 1] - S[j]) if S[j + 1] > S[j] else 0.0
            nn = slerp2(A["n_out"], B["n_in"], f) if A["arcseg"] else A["n_out"]
            qa = A["q"] + lift*A["n_out"]; qb = B["q"] + lift*(B["n_in"] if A["arcseg"] else A["n_out"])
            return qa + (qb - qa)*f, nn
        cuts = [0] + [i for i in range(1, len(idx) - 1) if O[idx[i]]["seam"]] + [len(idx) - 1]
        srcs = {O[a]["src"] for a in idx[:-1]}
        for ci in range(len(cuts) - 1):
            ia, ib = cuts[ci], cuts[ci + 1]; sa, sb = S[ia], S[ib]
            def endm(i):
                nd = O[idx[i]]
                if nd.get("end"): return 0.0
                return seam_h if 0 < i < len(idx) - 1 else margin
            ms0, ms1 = endm(ia), endm(ib); knots = [S[i] for i in range(ia, ib + 1)]
            want = not lod and (plate_ok is None or all(plate_ok(sx) for sx in srcs if sx is not None and sx >= 0)) and (sb - sa - ms0 - ms1) >= min_plate
            if not want:
                for a, b in zip(knots[:-1], knots[1:]): sec.append((ev(a + 1e-12, 0.0), ev(b - 1e-12, 0.0), "frame"))
                continue
            p0, p1 = sa + ms0, sb - ms1
            def chain(a, b, lift, grp):
                ks = [a] + [x for x in knots if a + 1e-9 < x < b - 1e-9] + [b]
                for x, y in zip(ks[:-1], ks[1:]):
                    if y - x > 1e-9: sec.append((ev(x + 1e-12, lift), ev(y - 1e-12, lift), grp))
            def wall(s_out, s_in):
                (pa, na), (pb, nb) = ev(s_out, 0.0), ev(s_in, g); d = pb - pa; wn = unit(np.array([d[1], -d[0]])); wn = wn if wn @ na > 0 else -wn
                return ((pa, wn), (pb, wn), "plate_wall")
            if ms0 > 0: chain(sa, p0, 0.0, "frame"); sec.append(wall(p0, p0 + cham)); t0 = p0 + cham
            else: t0 = sa
            if ms1 > 0: t1 = p1 - cham
            else: t1 = sb
            chain(t0, t1, g, "plate")
            if ms1 > 0:
                w_ = wall(p1, p1 - cham); sec.append((w_[1], w_[0], "plate_wall")); chain(p1, sb, 0.0, "frame")
    mesh = mesh or Mesh(); revolve_section(mesh, sec, N)
    return mesh.result(c["st"]) if True else mesh
