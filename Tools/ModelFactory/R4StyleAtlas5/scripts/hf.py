"""#1239 Hero Benchmark Pass 2: high-fidelity modelling kit. RUNS INSIDE BLENDER (bpy / bmesh).

A recipe is a function recipe(B) that models ONE asset with real modelling operations: spline profiles revolved into surfaces, lofted hulls with
changing cross-sections, Boolean cuts, bevels, subdivision surfaces, radial arrays. The recipe is executed twice:
  B.hi = True   high-poly source: dense, subdivided, every recess, bolt, weld and panel line is geometry ("detail" and "floater" pieces included)
  B.hi = False  game mesh: the same forms at game resolution, small details left out
The details that exist only in the source are then BAKED into the game mesh (tangent normals, AO, edge mask, material id, object normal, position)
and a per-style skin is composed from those maps, so wear, dirt and rust follow the geometry of this particular model (see hskin.py).
Axes: Blender, X right, -Y forward, Z up. Units: metres."""
import bpy, bmesh, math
import numpy as np
from mathutils import Vector, Matrix

ZONES = ["base", "sec", "dark", "metal", "glow", "rubber", "rust", "bare", "glass", "alt", "alt2"]            # material ids of the source model
ZCOL = {z: c for z, c in zip(ZONES, [(1, 0, 0), (0, 1, 0), (0, 0, 1), (1, 1, 0), (0, 1, 1), (1, 0, 1), (1, 0.5, 0), (0.5, 1, 0), (0, 0.5, 1), (0.5, 0, 1), (1, 0, 0.5)])}
AX = {"x": Vector((1, 0, 0)), "y": Vector((0, 1, 0)), "z": Vector((0, 0, 1))}


# ------------------------------------------------------------------------------------------------ curves and sections
def spl(pts, n=8):
    """smooth polyline through 2D / 3D points (Catmull-Rom); a point given as (..., 'c') is a hard corner. n = samples per span"""
    P = [np.array(p[:-1] if isinstance(p[-1], str) else p, float) for p in pts]; C = [isinstance(p[-1], str) for p in pts]; out = []
    for i in range(len(P) - 1):
        a, b = P[i], P[i + 1]; p0 = a if (C[i] or i == 0) else P[i - 1]; p3 = b if (C[i + 1] or i + 2 >= len(P)) else P[i + 2]
        if C[i] and C[i + 1]: out.append(a); continue
        for k in range(n):
            t = k/n; t2, t3 = t*t, t*t*t
            m0 = (b - p0)*0.5 if not C[i] else (b - a); m1 = (p3 - a)*0.5 if not C[i + 1] else (b - a)
            out.append((2*t3 - 3*t2 + 1)*a + (t3 - 2*t2 + t)*m0 + (-2*t3 + 3*t2)*b + (t3 - t2)*m1)
    out.append(P[-1]); return [tuple(float(x) for x in p) for p in out]


def sect(w, h, n=32, e=2.5, top=None, bot=None):
    """closed cross-section: superellipse of half-sizes w, h and exponent e (2 = ellipse, 5+ = rounded box); top / bot override e for z > 0 / z < 0"""
    out = []
    for i in range(n):
        a = 2*math.pi*i/n; c, s = math.cos(a), math.sin(a); ee = (top if (s > 0 and top) else bot if (s < 0 and bot) else e)
        out.append((w*math.copysign(abs(c)**(2/ee), c), h*math.copysign(abs(s)**(2/ee), s)))
    return out


def lerp(a, b, t): return a + (b - a)*t
def smooth(t): return t*t*(3 - 2*t)


# ------------------------------------------------------------------------------------------------ builder
class Builder:
    def __init__(s, hi, style, col):
        s.hi = hi; s.style = style; s.col = col; s.objs = []; s.cutters = []
    def q(s, hi, lo): return hi if s.hi else lo

    def _obj(s, bm, node, zone, kind, name):
        if kind in ("detail", "floater") and not s.hi: bm.free(); return None
        me = bpy.data.meshes.new(name or "p"); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); bm.to_mesh(me); bm.free()
        o = bpy.data.objects.new(("HI_" if s.hi else "LO_") + (name or "p"), me); s.col.objects.link(o); o["node"] = node; o["zone"] = zone; o["kind"] = kind
        if kind == "cutter": o.display_type = "WIRE"; o.hide_render = True; s.cutters.append(o)
        else: s.objs.append(o)
        return o

    # ---- primitives -------------------------------------------------------------------------------------------------
    def rev(s, node, zone, prof, seg=(96, 28), axis="x", at=(0, 0, 0), closed=False, kind="solid", name=None, arc=None, phase=0.0):
        """surface of revolution. prof = [(radius, t)] along the axis (use spl() for smooth profiles); closed = the profile is a loop (tyre, ring)"""
        n = s.q(*seg); bm = bmesh.new(); A = AX[axis]; U = AX["y" if axis != "y" else "z"]; W = A.cross(U); at = Vector(at); rings = []
        prof = [tuple(p[:2]) for p in prof]; a0, a1 = (phase, phase + 2*math.pi) if arc is None else (math.radians(arc[0]), math.radians(arc[1])); full = arc is None; steps = n if full else n + 1
        for (r, t) in prof:
            if r < 1e-6: rings.append([bm.verts.new(at + A*t)]*steps)
            else: rings.append([bm.verts.new(at + A*t + (U*math.cos(a0 + (a1 - a0)*k/n) + W*math.sin(a0 + (a1 - a0)*k/n))*r) for k in range(steps)])
        m = len(rings); pairs = [(i, (i + 1) % m) for i in range(m if closed else m - 1)]
        for i, j in pairs:
            for k in range(n if full else n):
                k2 = (k + 1) % steps if full else k + 1; a, b, c, d = rings[i][k], rings[i][k2], rings[j][k2], rings[j][k]; f = [v for n_, v in enumerate((a, b, c, d)) if v not in (a, b, c, d)[:n_]]
                if len(f) >= 3:
                    try: bm.faces.new(f)
                    except ValueError: pass
        if not full and closed:
            try: bm.faces.new([rings[i][0] for i in range(m)]); bm.faces.new([rings[i][n] for i in reversed(range(m))])
            except ValueError: pass
        return s._obj(bm, node, zone, kind, name)

    def loft(s, node, zone, rings, cap=True, kind="solid", name=None, closed=False):
        """skin between cross-section loops (each a list of the same number of 3D points)"""
        bm = bmesh.new(); R = [[bm.verts.new(p) for p in r] for r in rings]; n = len(R[0]); m = len(R)
        for i in range(m if closed else m - 1):
            j = (i + 1) % m
            for k in range(n): bm.faces.new((R[i][k], R[i][(k + 1) % n], R[j][(k + 1) % n], R[j][k]))
        if cap and not closed: bm.faces.new(list(reversed(R[0]))); bm.faces.new(R[-1])
        return s._obj(bm, node, zone, kind, name)

    def hull(s, node, zone, stations, n=(40, 14), m=(8, 3), axis="y", kind="solid", name=None, cap=True):
        """streamlined body: stations = [(t, cu, cv, w, h, e)] along the axis, cross-sections are superellipses, interpolated smoothly in between"""
        nn = s.q(*n); mm = s.q(*m); A = AX[axis]; U, V = {"y": (AX["x"], AX["z"]), "x": (AX["y"], AX["z"]), "z": (AX["x"], AX["y"])}[axis]; rings = []
        P = spl([tuple(st) for st in stations], mm)
        for (t, cu, cv, w, h, e) in P: rings.append([A*t + U*(cu + x) + V*(cv + y) for (x, y) in sect(max(w, 1e-4), max(h, 1e-4), nn, max(e, 1.2))])
        return s.loft(node, zone, rings, cap, kind, name)

    def prism(s, node, zone, poly, t0, t1, frame=("x", "y", "z"), kind="solid", name=None, at=(0, 0, 0)):
        """polygon (in the plane of the first two frame axes) extruded along the third"""
        ea, eb, et = (AX[a] if isinstance(a, str) else Vector(a) for a in frame); at = Vector(at); bm = bmesh.new()
        lo = [bm.verts.new(at + ea*x + eb*y + et*t0) for (x, y) in poly]; hi_ = [bm.verts.new(at + ea*x + eb*y + et*t1) for (x, y) in poly]; n = len(poly)
        bm.faces.new(lo); bm.faces.new(list(reversed(hi_)))
        for k in range(n): bm.faces.new((lo[k], hi_[k], hi_[(k + 1) % n], lo[(k + 1) % n]))
        return s._obj(bm, node, zone, kind, name)

    def box(s, node, zone, a, b, kind="solid", name=None, R=None):
        bm = bmesh.new(); a = Vector(a); b = Vector(b); c = (a + b)/2; h = (b - a)/2
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(c) @ Matrix.Diagonal((abs(h.x)*2, abs(h.y)*2, abs(h.z)*2, 1)))
        o = s._obj(bm, node, zone, kind, name)
        if o is not None and R is not None: o.data.transform(Matrix(R).to_4x4())
        return o

    def cyl(s, node, zone, p0, p1, r, seg=(32, 12), kind="solid", name=None, r1=None):
        p0 = Vector(p0); p1 = Vector(p1); d = p1 - p0; L = d.length; bm = bmesh.new(); n = s.q(*seg)
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=n, radius1=r, radius2=r if r1 is None else r1, depth=L, matrix=Matrix.Translation((0, 0, L/2)))
        o = s._obj(bm, node, zone, kind, name)
        if o is not None: o.data.transform(Matrix.Translation(p0) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4())
        return o

    def tor(s, node, zone, R, r, seg=(64, 24), sec=(16, 6), axis="x", at=(0, 0, 0), kind="solid", name=None):
        k = s.q(*sec); return s.rev(node, zone, [(R + r*math.cos(2*math.pi*i/k), r*math.sin(2*math.pi*i/k)) for i in range(k)], seg, axis, at, True, kind, name)

    def pipe(s, node, zone, pts, r, seg=(16, 8), n=(6, 3), kind="solid", name=None, rfn=None, closed=False):
        """tube along a smooth path through pts (hoses, pipes, roll bars, weld beads). rfn(t) scales the radius along the path (t = 0..1)"""
        P = [Vector(p) for p in spl([tuple(p) for p in pts], s.q(*n))]; k = s.q(*seg); rings = []; up = Vector((0, 0, 1)); m = len(P)
        for i, p in enumerate(P):
            t = (P[min(i + 1, m - 1)] - P[max(i - 1, 0)]).normalized(); u = (up - t*up.dot(t)); u = u.normalized() if u.length > 1e-4 else t.orthogonal().normalized(); v = t.cross(u); up = u
            rr = r*(rfn(i/(m - 1)) if rfn else 1.0); rings.append([p + (u*math.cos(2*math.pi*j/k) + v*math.sin(2*math.pi*j/k))*rr for j in range(k)])
        return s.loft(node, zone, rings, True, kind, name, closed)
    def helix(s, node, zone, p0, p1, R, r, turns, seg=(14, 7), n=(18, 8), kind="solid", name=None):
        """coil spring from p0 to p1"""
        p0 = Vector(p0); p1 = Vector(p1); ax = (p1 - p0); L = ax.length; ax.normalize(); u = ax.orthogonal().normalized(); v = ax.cross(u); steps = int(turns*s.q(*n)); pts = []
        for i in range(steps + 1): a = 2*math.pi*turns*i/steps; pts.append(p0 + ax*(L*i/steps) + (u*math.cos(a) + v*math.sin(a))*R)
        k = s.q(*seg); rings = []
        for i, p in enumerate(pts):
            t = (pts[min(i + 1, steps)] - pts[max(i - 1, 0)]).normalized(); rad = (p - p0 - ax*(p - p0).dot(ax)).normalized(); b = t.cross(rad)
            rings.append([p + (rad*math.cos(2*math.pi*j/k) + b*math.sin(2*math.pi*j/k))*r for j in range(k)])
        return s.loft(node, zone, rings, True, kind, name)
    def weld(s, node, pts, r=0.004, zone="bare"):
        """weld bead: a lumpy tube laid on a seam. Source model only - it is baked into the normal map and the skin"""
        if not s.hi: return None
        L = sum((Vector(pts[i + 1]) - Vector(pts[i])).length for i in range(len(pts) - 1)); k = max(4, int(L/(r*1.5)))
        return s.pipe(node, zone, pts, r, (8, 4), (max(2, k//max(1, len(pts) - 1)), 2), "detail", "weld", lambda t: 0.75 + 0.35*abs(math.sin(t*k*math.pi)))
    def arcs(s, node, zone, r0, r1, t0, t1, n, gap=0.25, axis="x", at=(0, 0, 0), seg=(48, 12), kind="solid", name=None, phase=0.0):
        """n ring segments with gaps (light arcs, segmented armour rings). gap = fraction of the pitch left open"""
        out = []; pitch = 360.0/n; segs = (max(3, seg[0]//n), max(2, seg[1]//n))
        for i in range(n): out.append(s.rev(node, zone, [(r0, t0), (r1, t0), (r1, t1), (r0, t1)], segs, axis, at, True, kind, name, arc=(phase + i*pitch + pitch*gap/2, phase + (i + 1)*pitch - pitch*gap/2)))
        return out
    def bolts(s, node, zone, R, n, t, size=0.008, h=0.006, axis="x", at=(0, 0, 0), kind="detail", phase=0.0, sides=6):
        A = AX[axis]; U = AX["y" if axis != "y" else "z"]; W = A.cross(U); at = Vector(at); out = []
        for i in range(n):
            a = phase + 2*math.pi*i/n; c = at + A*t + (U*math.cos(a) + W*math.sin(a))*R; out.append(s.cyl(node, zone, c, c + A*h, size, (sides, sides), kind, "bolt"))
        return out
    # ---- operations -------------------------------------------------------------------------------------------------
    def bevel(s, o, w, seg=(3, 1), angle=30, lo=True):
        if o is None or (not s.hi and (not lo or seg[1] == 0)): return o
        m = o.modifiers.new("bev", "BEVEL"); m.width = w; m.segments = s.q(*seg); m.limit_method = "ANGLE"; m.angle_limit = math.radians(angle); m.harden_normals = False; return o
    def sub(s, o, lv=(2, 0), crease=None):
        if o is None: return o
        if crease is not None: o["crease_angle"] = crease
        k = s.q(*lv)
        if k > 0: m = o.modifiers.new("sub", "SUBSURF"); m.levels = k; m.render_levels = k
        return o
    def solid(s, o, th, off=-1):
        if o is None: return o
        m = o.modifiers.new("sol", "SOLIDIFY"); m.thickness = th; m.offset = off; return o
    def cut(s, o, cutter, lo=True, op="DIFFERENCE"):
        """Boolean. lo = False: the cut exists only in the source model (and is baked)"""
        if o is None or cutter is None or (not s.hi and not lo): return o
        m = o.modifiers.new("cut", "BOOLEAN"); m.object = cutter; m.operation = op; m.solver = "EXACT"; return o
    def radial(s, objs, n, axis="x", at=(0, 0, 0), phase=0.0):
        """n copies around the axis (the originals are the first copy)"""
        out = []; at = Vector(at)
        for o in [x for x in objs if x is not None]:
            lst = s.cutters if o["kind"] == "cutter" else s.objs
            for k in range(n):
                M = Matrix.Translation(at) @ Matrix.Rotation(2*math.pi*k/n + phase, 4, AX[axis]) @ Matrix.Translation(-at)
                if k == 0: o.data.transform(Matrix.Translation(at) @ Matrix.Rotation(phase, 4, AX[axis]) @ Matrix.Translation(-at)) if phase else None; out.append(o); continue
                c = o.copy(); c.data = o.data.copy(); c.data.transform(M @ (Matrix.Translation(at) @ Matrix.Rotation(-phase, 4, AX[axis]) @ Matrix.Translation(-at) if phase else Matrix.Identity(4))); s.col.objects.link(c); lst.append(c); out.append(c)
        return out
    def mirror(s, objs, axis="x"):
        out = []; S = Matrix.Diagonal([-1 if a == axis else 1 for a in "xyz"] + [1])
        for o in [x for x in objs if x is not None]:
            c = o.copy(); c.data = o.data.copy(); c.data.transform(S); c.data.flip_normals(); s.col.objects.link(c); (s.cutters if o["kind"] == "cutter" else s.objs).append(c); out.append(c)
        return out
    def move(s, o, M):
        if o is not None: o.data.transform(Matrix(M))
        return o


# ------------------------------------------------------------------------------------------------ finishing
def _active(o):
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o


def apply_mods(o):
    _active(o)
    if "crease_angle" in o:                                         # crease edges sharper than the angle so that subdivision keeps them
        me = o.data; bm = bmesh.new(); bm.from_mesh(me); cl = bm.edges.layers.float.get("crease_edge") or bm.edges.layers.float.new("crease_edge"); lim = math.radians(o["crease_angle"])
        for e in bm.edges:
            if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > lim: e[cl] = 1.0
        bm.to_mesh(me); bm.free()
    for m in list(o.modifiers):
        try: bpy.ops.object.modifier_apply(modifier=m.name)
        except Exception as ex: print("[HERO2] modifier failed", o.name, m.type, ex); o.modifiers.remove(m)


def shade(o, angle=35, weighted=True):
    me = o.data
    for p in me.polygons: p.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(angle)); me.update()
    if weighted:
        _active(o); m = o.modifiers.new("wn", "WEIGHTED_NORMAL"); m.keep_sharp = True; m.weight = 60
        try: bpy.ops.object.modifier_apply(modifier="wn")
        except Exception as ex: print("[HERO2] weighted normals failed", o.name, ex)


def finish(B, lo_angle=38):
    """apply the modifier stacks; returns {node: [objects]} for the source model or {node: one joined object with zone material slots} for the game mesh"""
    for o in B.objs: apply_mods(o)
    for c in B.cutters: bpy.data.objects.remove(c, do_unlink=True)
    nodes = {}
    for o in B.objs: nodes.setdefault(o["node"], []).append(o)
    if B.hi:
        for objs in nodes.values():
            for o in objs: shade(o, 45, False)
        return nodes
    out = {}
    for node, objs in nodes.items():
        for o in objs:
            z = o["zone"]; mat = bpy.data.materials.get("Z_" + z) or bpy.data.materials.new("Z_" + z); o.data.materials.clear(); o.data.materials.append(mat)
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs: o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        if len(objs) > 1: bpy.ops.object.join()
        j = bpy.context.view_layer.objects.active; j.name = "LO_" + node; j["node"] = node
        bm = bmesh.new(); bm.from_mesh(j.data); bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=bm.edges); bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4]); bm.to_mesh(j.data); bm.free()
        shade(j, lo_angle, True); out[node] = j
    return out


def tris(o): return sum(len(p.vertices) - 2 for p in o.data.polygons)
