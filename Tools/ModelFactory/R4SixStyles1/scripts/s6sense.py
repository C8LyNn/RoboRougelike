"""Sense check of a delivered model (does every part hold on to something, do moving parts clear the rest).
usage:
  blender -b --factory-startup --python s6sense.py -- MODEL GLB CID OUT_JSON
  blender -b --factory-startup --python s6sense.py -- PAIR DEP_GLB DRONE_GLB DOCK_Z OUT_JSON
MODEL: at rest (frame 1) the mesh is split into loose parts (outline shells and the muzzle flash ignored); parts that touch, intersect or
sit inside each other are joined; every group that does not reach the main group is FLOATING (reported with its gap). Then the moving
nodes are swept through their clips and every triangle overlap with the rest outside the joint zone is a COLLISION.
FUNCTION (MODEL mode, v2): the part must also be able to do its job - the wheel tyre touches the ground at GroundProbe, is centred on the
axle and stays on the ground while it rolls; the gun Muzzle sits on the barrel tip, on the bore axis, with a clear line of fire at rest and
over the aim clips; the drone stands on its Socket_Dock, its rotors spin about their own hubs and its gun has a clear line of fire; the
launcher iris turns about the launch axis. LOD1 files are checked the same way (QA meta is read from the LOD0 QA).
PAIR: the drone (frame 1, Docked) is put on the launcher at dock_z: it must touch the pad and must not sink into it. v2: the drone must
also leave the pad - it is played through its Launch clip and lifted straight up 0-300 mm over the turning iris, and every cut through
the launcher is a LAUNCH collision."""
import bpy, bmesh, sys, json, math
from mathutils import Vector
from mathutils.bvhtree import BVHTree

A = sys.argv[sys.argv.index("--") + 1:]
TOL = 0.0025          # a seam up to 2.5 mm reads as joined at game scale
TOLJ = 0.0045         # running clearance between moving nodes (the R4 contract itself has a 2 mm slewing gap)
STATIC = False        # True for a LOD1 file: one merged mesh without rig, the moving joints are fused, so their running clearance applies everywhere
JOINT = {"Yaw": "Base", "Pitch": "Yaw", "Barrel": "Pitch", "Rotor": "Mount", "Iris": "Body"}


def load(path, offset=(0, 0, 0)):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.parent is None: o.location = Vector(o.location) + Vector(offset)
    return new


def owner(o):
    """the contract node a mesh belongs to: the nearest ancestor whose name is (or ends with _) a contract node name"""
    import re
    keys = ("Base", "Yaw", "Pitch", "Barrel", "Mount", "Rotor", "Body", "Iris")
    p = o
    while p is not None:
        n = re.sub(r"_LOD1$", "", p.name.split(".")[0])
        m = re.search(r"(?:^|_)(Rotor_\d+)$", n)
        if m: return m.group(1)
        for k in keys:
            if n == k or n.endswith("_" + k): return k
        p = p.parent
    return o.name.split(".")[0]


def islands(objs, frame):
    sc = bpy.context.scene; sc.frame_set(frame); dg = bpy.context.evaluated_depsgraph_get(); out = []
    for o in objs:
        if o.type != "MESH" or "MuzzleFlash" in o.name: continue
        oe = o.evaluated_get(dg); me = oe.to_mesh(); M = oe.matrix_world.copy()
        mats = [(s.material.name if s.material else "") for s in o.material_slots]
        bm = bmesh.new(); bm.from_mesh(me)
        dead = [f for f in bm.faces if f.material_index < len(mats) and mats[f.material_index].startswith("R4P_Outline")]
        bmesh.ops.delete(bm, geom=dead, context="FACES")
        bm.verts.index_update(); bm.faces.index_update(); bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
        seen = set()
        for f0 in bm.faces:
            if f0.index in seen: continue
            stack = [f0]; comp = []; seen.add(f0.index)
            while stack:
                f = stack.pop(); comp.append(f)
                for v in f.verts:
                    for g in v.link_faces:
                        if g.index not in seen: seen.add(g.index); stack.append(g)
            vid = {}; verts = []; polys = []
            for f in comp:
                poly = []
                for v in f.verts:
                    if v.index not in vid: vid[v.index] = len(verts); verts.append(M @ v.co)
                    poly.append(vid[v.index])
                polys.append(poly)
            lo = Vector((min(v.x for v in verts), min(v.y for v in verts), min(v.z for v in verts)))
            hi = Vector((max(v.x for v in verts), max(v.y for v in verts), max(v.z for v in verts)))
            out.append(dict(node=owner(o), obj=o.name, verts=verts, polys=polys, lo=lo, hi=hi, bvh=BVHTree.FromPolygons(verts, polys, epsilon=0.0)))
        bm.free(); oe.to_mesh_clear()
    return out


def near(a, b, pad):
    return all(a["lo"][k] - pad <= b["hi"][k] and b["lo"][k] - pad <= a["hi"][k] for k in range(3))


def inside(p, bvh):
    d = Vector((0.31, 0.17, 0.93)).normalized(); n = 0; o = Vector(p)
    for _ in range(64):
        hit = bvh.ray_cast(o, d)
        if hit[0] is None: break
        n += 1; o = hit[0] + d*1e-5
    return n % 2 == 1


def gap(a, b):
    """0 when touching / intersecting / one inside the other, else the smallest vertex-to-surface distance (both ways, sampled)"""
    if a["bvh"].overlap(b["bvh"]): return 0.0
    best = 9.0
    for (p, q) in ((a, b), (b, a)):
        lo, hi = q["lo"] - Vector((0.012,)*3), q["hi"] + Vector((0.012,)*3)              # only the vertices that can be near the other part, all of them
        vs = [v for v in p["verts"] if lo.x <= v.x <= hi.x and lo.y <= v.y <= hi.y and lo.z <= v.z <= hi.z]
        step = max(1, len(vs)//40000)
        for v in vs[::step]:
            r = q["bvh"].find_nearest(v)
            if r[0] is not None: best = min(best, r[3])
            if best <= TOL: return best
    if within(a, b) and inside(a["verts"][0], b["bvh"]): return 0.0
    if within(b, a) and inside(b["verts"][0], a["bvh"]): return 0.0
    return best


def within(a, b):
    return all(b["lo"][k] - TOL <= a["lo"][k] and a["hi"][k] <= b["hi"][k] + TOL for k in range(3))


def floating(isl):
    n = len(isl); par = list(range(n))
    def f(i):
        while par[i] != i: par[i] = par[par[i]]; i = par[i]
        return i
    for i in range(n):
        for j in range(i + 1, n):
            if f(i) == f(j): continue
            tol = TOL if isl[i]["node"] == isl[j]["node"] and not STATIC else TOLJ
            if not near(isl[i], isl[j], tol): continue
            if gap(isl[i], isl[j]) <= tol: par[f(i)] = f(j)
    groups = {}
    for i in range(n): groups.setdefault(f(i), []).append(i)
    root = [i for i in range(n) if isl[i]["node"] in ("Base", "Mount", "Body")] or list(range(n))
    big = max(root, key=lambda i: (isl[i]["hi"] - isl[i]["lo"]).length)
    main = groups[f(big)]; res = []
    for g in groups.values():
        if g is main: continue
        lo = Vector((min(isl[i]["lo"].x for i in g), min(isl[i]["lo"].y for i in g), min(isl[i]["lo"].z for i in g)))
        hi = Vector((max(isl[i]["hi"].x for i in g), max(isl[i]["hi"].y for i in g), max(isl[i]["hi"].z for i in g)))
        gp = 9.0; ga = 9.0; who = None; gs = set(g)
        for i in g:
            for j in range(n):
                if j in gs or not near(isl[i], isl[j], 0.05): continue
                d = gap(isl[i], isl[j])
                if j in main: gp = min(gp, d)
                if d < ga: ga = d; who = (isl[j]["node"], isl[j]["obj"], [round(1000*c) for c in (isl[j]["lo"] + isl[j]["hi"])/2])
        res.append(dict(nodes=sorted(set(isl[i]["node"] for i in g)), objs=sorted(set(isl[i]["obj"] for i in g))[:6], n_parts=len(g),
                        center_mm=[round(1000*c) for c in (lo + hi)/2], size_mm=[round(1000*c) for c in (hi - lo)], gap_mm=round(1000*gp, 1) if gp < 9 else None,
                        nearest_mm=round(1000*ga, 1) if ga < 9 else None, nearest=who))
    return res, len(isl), len(groups)


def cross_point(P, Q):
    """where two polygons actually cut each other: edge-plane crossings that fall inside the other polygon (None when they only touch
    or lie in one plane - a contact, not a collision)"""
    from mathutils.geometry import intersect_point_tri
    pts = []
    for (Aa, Bb) in ((P, Q), (Q, P)):
        n = (Bb[1] - Bb[0]).cross(Bb[2] - Bb[0])
        if n.length < 1e-12: continue
        n.normalize(); d = n.dot(Bb[0])
        for i in range(len(Aa)):
            p0, p1 = Aa[i], Aa[(i + 1) % len(Aa)]; s0, s1 = n.dot(p0) - d, n.dot(p1) - d
            if s0*s1 < -1e-14 and min(abs(s0), abs(s1)) > 2e-5:
                x = p0 + (p1 - p0)*(s0/(s0 - s1))
                if any(intersect_point_tri(x, Bb[0], Bb[k], Bb[k + 1]) is not None for k in range(1, len(Bb) - 1)): pts.append(x)
    return (sum(pts, Vector())/len(pts)) if pts else None


def tri_centroids(isl, idx):
    return [sum((isl["verts"][k] for k in isl["polys"][t]), Vector())/len(isl["polys"][t]) for t in idx]


def collisions(objs, cid, frames, meta):
    res = []
    for fr in frames:
        isl = islands(objs, fr); hits = {}
        yo = [o for o in objs if o.name.split(".")[0].endswith("_Yaw") or o.name.split(".")[0] == "Yaw"]
        pax = yo[0].matrix_world.col[0].xyz.normalized() if yo else Vector((1, 0, 0))                  # pitch axis turns with the yaw
        axd = lambda m: (m - Vector((0, 0, 0.135))).cross(pax).length
        for a in isl:
            for b in isl:
                if a is b or a["node"] >= b["node"] or not near(a, b, 0): continue
                na, nb = a["node"], b["node"]
                movers = {na, nb}
                if cid == "R4M_Wheel" and movers != {"Rotor", "Mount"}: continue
                if cid == "R4W_Basic" and not (movers & {"Yaw", "Pitch", "Barrel"}): continue
                if cid == "R4W_Basic" and movers == {"Barrel", "Pitch"}: continue                  # the barrel slides in its receiver
                if cid == "FlyingDrone" and not any(n.startswith("Rotor_") for n in movers): continue
                if cid == "R4X_FlyingDroneDeployer" and movers != {"Iris", "Body"}: continue
                pairs = a["bvh"].overlap(b["bvh"])
                if not pairs: continue
                keep = []
                for (ia, ib) in pairs:
                    PA = [a["verts"][k] for k in a["polys"][ia]]; PB = [b["verts"][k] for k in b["polys"][ib]]
                    m = cross_point(PA, PB)
                    if m is None: continue
                    na_ = (PA[1] - PA[0]).cross(PA[2] - PA[0]); nb_ = (PB[1] - PB[0]).cross(PB[2] - PB[0])
                    if na_.length > 1e-12 and nb_.length > 1e-12 and abs(na_.normalized().dot(nb_.normalized())) > 0.95: continue   # parallel faces brushing: a sliding contact
                    dbg = ([round(1000*c) for c in sum(PA, Vector())/len(PA)], [round(1000*c) for c in sum(PB, Vector())/len(PB)])
                    if cid == "R4M_Wheel" and math.hypot(m.y, m.z + 0.26) < 0.075: continue           # axle / hub junction
                    if cid == "R4W_Basic":
                        if movers == {"Yaw", "Base"} and -0.010 <= m.z <= 0.040 and math.hypot(m.x, m.y) < 0.26: continue      # slewing ring
                        if movers == {"Pitch", "Yaw"} and axd(m) < 0.060: continue                                            # trunnions / bearings on the pitch axis
                        if movers == {"Barrel", "Yaw"} and axd(m) < 0.060: continue
                    if cid == "FlyingDrone":
                        rn = na if na.startswith("Rotor_") else nb; c = meta.get("rotors", {}).get(rn)
                        other = nb if rn == na else na
                        if c and not other.startswith("Rotor_") and math.hypot(m.x - c[0], m.y - c[1]) < 0.030: continue        # motor / hub
                    if cid == "R4X_FlyingDroneDeployer" and math.hypot(m.x, m.y) < 0.03: continue
                    keep.append(m); kd = dbg
                if keep:
                    k = (na, nb); h = hits.setdefault(k, dict(n=0, at=None)); h["n"] += len(keep); h["at"] = [round(1000*c) for c in keep[0]]; h["poly_centres"] = {na: kd[0], nb: kd[1]}
        for (na, nb), h in hits.items(): res.append(dict(frame=fr, a=na, b=nb, n=h["n"], at_mm=h["at"], poly_centres_mm=h.get("poly_centres")))
    return res



def empty(objs, key):
    e = [o for o in objs if o.type == "EMPTY" and (o.name.split(".")[0] == key or o.name.split(".")[0].endswith("_" + key))]
    return e[0] if e else None


def obj_of(objs, key):
    e = [o for o in objs if o.type == "MESH" and (o.name.split(".")[0] == key or o.name.split(".")[0].endswith("_" + key))]
    return e[0] if e else None


def box(isl):
    return (Vector((min(i["lo"].x for i in isl), min(i["lo"].y for i in isl), min(i["lo"].z for i in isl))),
            Vector((max(i["hi"].x for i in isl), max(i["hi"].y for i in isl), max(i["hi"].z for i in isl))))


def ray_hits(isl, o, d, far):
    best = None
    for i in isl:
        h = i["bvh"].ray_cast(o, d, far)
        if h[0] is not None and (best is None or h[3] < best[0]): best = (h[3], i["node"], i["obj"])
    return best


def function(objs, cid, meta):
    sc = bpy.context.scene; F = {}; bad = []
    if STATIC:
        if cid == "R4M_Wheel":
            isl = islands(objs, 1); lo, hi = box(isl)
            F = dict(ground_gap_mm=round(1000*(lo.z + 0.65), 1))
            if abs(F["ground_gap_mm"]) > 3: bad.append("tyre does not touch the ground (z -0.65)")
        F["static_lod1"] = True
    elif cid == "R4M_Wheel":
        isl = islands(objs, 1); rot = [i for i in isl if i["node"] == "Rotor"]; lo, hi = box(rot)
        gp = empty(objs, "GroundProbe"); gz = gp.matrix_world.translation.z if gp else -0.65
        mins = []
        for fr in range(27, 52, 2):
            r_ = [i for i in islands(objs, fr) if i["node"] == "Rotor"]; mins.append(min(v.z for i in r_ for v in i["verts"]))
        mlo, _ = box([i for i in isl if i["node"] == "Mount"])
        F = dict(ground_gap_mm=round(1000*(lo.z - gz), 1), rotor_centre_off_mm=[round(1000*(lo.y + hi.y)/2, 1), round(1000*((lo.z + hi.z)/2 + 0.26), 1)],
                 roll_ground_var_mm=round(1000*(max(mins) - min(mins)), 1), roll_ground_gap_max_mm=round(1000*(max(mins) - gz), 1), mount_clearance_mm=round(1000*(mlo.z - gz), 1))
        if abs(F["ground_gap_mm"]) > 3: bad.append("tyre does not touch the ground at GroundProbe")
        if max(abs(c) for c in F["rotor_centre_off_mm"]) > 2: bad.append("rotor not centred on the axle (wobbles)")
        if F["roll_ground_var_mm"] > 5: bad.append("tyre bumps more than 5 mm while rolling (a lugged tyre rolls over its lugs - up to 5 mm is accepted)")
        if F["mount_clearance_mm"] < 20: bad.append("mount scrapes the ground")
    elif cid == "R4W_Basic":
        mz = empty(objs, "Muzzle"); bo = obj_of(objs, "Barrel"); res = []
        sc.frame_set(1); M1 = bo.matrix_world.to_3x3().inverted() if bo else None
        for fr in (1, 51, 95, 104, 113, 128):
            isl = islands(objs, fr); bar = [i for i in isl if i["node"] == "Barrel"]; rest = [i for i in isl if i["node"] != "Barrel"]
            m = mz.matrix_world.translation.copy(); d = (bo.matrix_world.to_3x3() @ M1 @ Vector((0, -1, 0))).normalized()
            vs = [v for i in bar for v in i["verts"]]
            ahead = min((m - v).dot(d) for v in vs) if vs else 0                                                 # muzzle ahead of the foremost barrel vertex (negative: inside)
            near_ = [v for v in vs if (m - v).dot(d) < ahead + 0.012]
            c = sum(near_, Vector())/len(near_) if near_ else m; off = ((m - c) - d*((m - c).dot(d))).length
            blk = ray_hits(rest + bar, m + d*0.002, d, 1.5)
            res.append(dict(frame=fr, muzzle_ahead_mm=round(1000*ahead, 1), muzzle_off_axis_mm=round(1000*off, 1), line_of_fire=None if blk is None else [round(1000*blk[0]), blk[1], blk[2]]))
        F = dict(muzzle=res)
        if any(not (0 <= r["muzzle_ahead_mm"] <= 50) for r in res if r["frame"] != 128): bad.append("Muzzle is not at the barrel tip (0-50 mm ahead, like the original #939: 40 mm)")
        if any(r["muzzle_off_axis_mm"] > 6 for r in res): bad.append("Muzzle is off the bore axis")
        if any(r["line_of_fire"] for r in res): bad.append("line of fire blocked")
    elif cid == "FlyingDrone":
        isl = islands(objs, 1); lo, hi = box(isl); sd = empty(objs, "Socket_Dock"); sz = sd.matrix_world.translation.z if sd else 0.0
        mz = empty(objs, "Muzzle"); m = mz.matrix_world.translation.copy() if mz else None
        body = [i for i in isl if not i["node"].startswith("Rotor_")]
        nb = min(i["bvh"].find_nearest(m)[3] for i in body) if m is not None else None
        blk = ray_hits(body, m + Vector((0, -0.001, 0)), Vector((0, -1, 0)), 1.5) if m is not None else None
        back = ray_hits(body, m, Vector((0, 1, 0)), 0.05) if m is not None else None
        thru = []
        for fr in range(53, 78, 3):
            r_ = [i for i in islands(objs, fr) if i["node"].startswith("Rotor_")]
            h = ray_hits(r_, m + Vector((0, -0.002, 0)), Vector((0, -1, 0)), 1.5) if m is not None else None
            if h: thru.append([fr, round(1000*h[0]), h[1]])
        hubs = {}
        for o in objs:
            n = owner(o)
            if o.type != "MESH" or not n.startswith("Rotor_"): continue
            ri = [i for i in isl if i["obj"] == o.name]
            if not ri: continue
            vv = [v for i in ri for v in i["verts"]]; c = sum(vv, Vector())/len(vv); org = o.matrix_world.translation; ax = o.matrix_world.col[2].xyz.normalized()
            hubs[n] = dict(off_mm=round(1000*((c - org) - ax*((c - org).dot(ax))).length, 1), axis_up=round(ax.z, 3))
        F = dict(stand_gap_mm=round(1000*(lo.z - sz), 1), muzzle_to_body_mm=None if nb is None else round(1000*nb, 1), gun_behind_muzzle_mm=None if back is None else round(1000*back[0], 1),
                 line_of_fire=None if blk is None else [round(1000*blk[0]), blk[1], blk[2]], fire_through_rotor_disc=thru[:4], rotor_hubs=hubs)
        if abs(F["stand_gap_mm"]) > 2: bad.append("drone does not stand on its Socket_Dock")
        if nb is None or nb > 0.045 or back is None: bad.append("Muzzle is not just ahead of the drone gun (0-45 mm)")
        if blk: bad.append("line of fire blocked by the drone body")
        if thru: bad.append("line of fire crosses a rotor disc")
        if any(h["off_mm"] > 3 for h in hubs.values()): bad.append("rotor not centred on its hub")
    elif cid == "R4X_FlyingDroneDeployer":
        ir = [i for i in islands(objs, 1) if i["node"] == "Iris"]; lo, hi = box(ir)
        F = dict(iris_centre_off_mm=round(1000*math.hypot((lo.x + hi.x)/2, (lo.y + hi.y)/2), 1))
        if F["iris_centre_off_mm"] > 3: bad.append("iris does not turn about the launch axis")
    F["issues"] = bad
    return F

bpy.ops.wm.read_factory_settings(use_empty=True)
if A[0] == "MODEL":
    path, cid, out = A[1], A[2], A[3]
    objs = load(path)
    STATIC = not any(o.type == "EMPTY" and o.name.split(".")[0] in ("Muzzle", "GroundProbe", "Socket_Dock", "Deploy_Out", "Socket_Mount", "Socket_Mount_Bottom") for o in objs) and "_LOD1" in path
    meta = {}
    try:
        q = json.load(open(path[:-4].replace("_LOD1", "") + "_QA.json")); meta = q.get("meta", {})
    except Exception: pass
    fl, n_isl, n_grp = floating(islands(objs, 1))
    frames = {"R4M_Wheel": list(range(27, 52, 3)), "R4W_Basic": list(range(27, 76, 4)) + list(range(77, 114, 3)) + [126, 127, 128, 129],
              "FlyingDrone": [1] + list(range(53, 78, 3)), "R4X_FlyingDroneDeployer": list(range(1, 50, 6))}.get(cid, [1])
    col = collisions(objs, cid, frames, meta)
    worst = {}
    for c in col:
        k = c["a"] + "|" + c["b"]
        if k not in worst or c["n"] > worst[k]["n"]: worst[k] = c
    fn = function(objs, cid, meta)
    rep = dict(mode="MODEL", file=path, cid=cid, islands=n_isl, groups=n_grp, floating=fl, collisions_worst=list(worst.values()), collision_frames=len(col),
               function=fn, status="OK" if not fl and not col and not fn["issues"] else "ISSUES")
else:
    dep, drn, dock, out = A[1], A[2], float(A[3]), A[4]
    od = load(dep); ob = load(drn, (0, 0, dock))
    i_d = islands(od, 1); i_b = islands(ob, 1)
    pen = 0; touch = 9.0; at = None; close = []
    for a in i_b:
        for b in i_d:
            if not near(a, b, 0.01): continue
            for (ia, ib) in a["bvh"].overlap(b["bvh"]):
                m = cross_point([a["verts"][k] for k in a["polys"][ia]], [b["verts"][k] for k in b["polys"][ib]])
                if m is not None: pen += 1; at = [round(1000*c) for c in m]
            g = gap(a, b); touch = min(touch, g); close.append((round(1000*g, 1), a["obj"], b["obj"]))
    low = min(v.z for a in i_b for v in a["verts"])
    drop = 9.0; dn = Vector((0, 0, -1))
    for a in i_b:
        vs = a["verts"]; step = max(1, len(vs)//4000)
        for v in vs[::step]:
            for b in i_d:
                if not (b["lo"].x <= v.x <= b["hi"].x and b["lo"].y <= v.y <= b["hi"].y and b["lo"].z <= v.z + 1e-4): continue
                h = b["bvh"].ray_cast(v + Vector((0, 0, 1e-4)), dn)
                if h[0] is not None: drop = min(drop, h[3] - 1e-4)
    def cuts(ia, ib, dz=0.0):
        n = 0; at_ = None
        for a in ia:
            vs = [v + Vector((0, 0, dz)) for v in a["verts"]] if dz else a["verts"]
            bv = BVHTree.FromPolygons(vs, a["polys"], epsilon=0.0) if dz else a["bvh"]
            alo = a["lo"] + Vector((0, 0, dz)); ahi = a["hi"] + Vector((0, 0, dz))
            for b in ib:
                if not all(alo[k] <= b["hi"][k] and b["lo"][k] <= ahi[k] for k in range(3)): continue
                for (x, y) in bv.overlap(b["bvh"]):
                    m = cross_point([vs[k] for k in a["polys"][x]], [b["verts"][k] for k in b["polys"][y]])
                    if m is not None: n += 1; at_ = (round(1000*m.x), round(1000*m.y), round(1000*m.z), a["node"], b["node"], b["obj"])
        return n, at_
    clip = []
    z1 = min(v.z for a in i_b for v in a["verts"])
    for fr in range(27, 52, 2):
        jb = islands(ob, fr); jd = islands(od, fr); lift = min(v.z for a in jb for v in a["verts"]) - z1
        n_, a_ = cuts(jb, jd)
        if n_ and lift > 0.003: clip.append(dict(frame=fr, lift_mm=round(1000*lift), cuts=n_, at=a_))
    lifts = []
    for fd in (1, 13, 25, 37):
        jd = islands(od, fd)
        for h in (0.005, 0.010, 0.020, 0.035, 0.050, 0.075, 0.100, 0.130, 0.160, 0.200, 0.250, 0.300):
            n_, a_ = cuts(i_b, jd, h)
            if n_: lifts.append(dict(dep_frame=fd, lift_mm=round(1000*h), cuts=n_, at=a_))
    rep = dict(mode="PAIR", dep=dep, drone=drn, dock_z=dock, drone_min_z_mm=round(1000*low, 1), free_drop_mm=round(1000*drop, 1) if drop < 9 else None, touch_gap_mm=round(1000*touch, 1), overlap_tris=pen, overlap_at_mm=at, closest=sorted(close)[:5],
               launch_clip_cuts=clip[:6], straight_lift_cuts=lifts[:8],
               status="OK" if touch <= 0.003 and pen <= 40 and not clip and not lifts else "ISSUES")
json.dump(rep, open(out, "w"), indent=1)
print("[S6S] SENSE_DONE", rep["status"], json.dumps({k: v for k, v in rep.items() if k in ("cid", "islands", "groups", "touch_gap_mm", "overlap_tris")}))
