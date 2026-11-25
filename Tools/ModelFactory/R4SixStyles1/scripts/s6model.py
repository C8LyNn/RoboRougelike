"""RoboRougelike six-style series 1: build ONE model end to end. RUNS INSIDE BLENDER (background).
recipe (s6defs) -> source model (hi) + game mesh (lo) -> contract check -> unwrap + Cycles bake -> skin (palette + wear of the style) -> [style 5: inverted-hull
outline in the mesh] -> rig with the contract node names / pivots / empties -> baked animation clips (one timeline, frame ranges in the manifest)
-> GLB + FBX + LOD1 + source .blend -> re-import of the exported GLB and FBX and comparison with the scene that was exported -> QA json.
usage: blender -b --factory-startup --python s6model.py -- OUT CID STYLE [SIZE]"""
import bpy, bmesh, sys, os, math, time, json, hashlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import hf, hbake, hskin, hlod, s6defs as D
from mathutils import Vector, Matrix, Euler
from mathutils.bvhtree import BVHTree
a = sys.argv[sys.argv.index("--") + 1:]; OUT, CID, STYLE = a[:3]; SIZE = int(a[3]) if len(a) > 3 else 2048
LABEL = D.STYLES[STYLE]; C = D.CONTRACT[CID]; FPS = 24
def P(*p): d = os.path.join(OUT, *p[:-1]); os.makedirs(d, exist_ok=True); return os.path.join(d, p[-1])
def log(*x): print("[S6]", *x, flush=True)
def bbox(objs, world=True):
    V = np.array([(o.matrix_world @ v.co) if world else v.co for o in objs for v in o.data.vertices]); return V.min(0), V.max(0)
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()


# ------------------------------------------------------------------------------------------------ animation (baked per frame, style flavour)
def ease(u): return u*u*(3 - 2*u)
def spin(u):
    if STYLE == "2": return u + 0.035*math.sin(TAU*u)                    # heavy: lags, catches up
    if STYLE == "3": return math.floor(u*12 + 1e-9)/12.0                 # arcade: twelve snaps per turn
    if STYLE == "4": return u + 0.02*math.sin(TAU*3*u) + 0.01*math.sin(TAU*7*u)   # rattle
    if STYLE == "5": return u + 0.05*math.sin(TAU*2*u)                    # surging
    return u
TAU = 2*math.pi


ANIM = {"R4W_Basic": ("Yaw", "Pitch", "Barrel", "MuzzleFlash"), "R4M_Wheel": ("Rotor",), "R4X_FlyingDroneDeployer": ("Iris",), "FlyingDrone": ("Body", "Rotor_")}
def pose(node, f):
    """local transform offsets of an animated node at frame f: (dloc, rot euler, scale); None = the node is not animated"""
    if not any(node == n or (n.endswith("_") and node.startswith(n)) for n in ANIM[CID]): return None
    r = _pose(node, f); return r if r is not None else ((0.0, 0.0, 0.0), (0.0, 0.0, 0.0), 1.0)


def _pose(node, f):
    z3 = (0.0, 0.0, 0.0)
    if CID == "R4W_Basic":
        if node == "Yaw" and 27 <= f <= 75:
            k = [(27, 0), (39, 90), (63, -90), (75, 0)]
            for (f0, v0), (f1, v1) in zip(k, k[1:]):
                if f0 <= f <= f1: return z3, (0, 0, math.radians(v0 + (v1 - v0)*ease((f - f0)/(f1 - f0)))), 1.0
        if node == "Pitch" and 77 <= f <= 113:                            # +35 = muzzle up = negative rotation about X (barrel along -Y)
            k = [(77, 0), (89, 35), (101, -10), (113, 0)]
            for (f0, v0), (f1, v1) in zip(k, k[1:]):
                if f0 <= f <= f1: return z3, (-math.radians(v0 + (v1 - v0)*ease((f - f0)/(f1 - f0))), 0, 0), 1.0
        if node == "Barrel":
            r = 0.0
            if 115 <= f <= 124: r = 0.045 if f in (116, 117) else (0.045*(1 - ease((f - 117)/7)) if f > 117 else 0.0)
            if 126 <= f <= 129: r = 0.045*ease((f - 126)/3)
            if 131 <= f <= 139: r = 0.045*(1 - ease((f - 131)/8))
            return (0, r, 0), z3, 1.0
        if node == "MuzzleFlash": return z3, (0, (f % 5)*0.6, 0), (1.0 if f in (115, 116) else 0.001)
    if CID == "R4M_Wheel" and node == "Rotor":
        if 27 <= f <= 51: return z3, (TAU*spin((f - 27)/24), 0, 0), 1.0
        if 53 <= f <= 77: return z3, (-TAU*spin((f - 53)/24), 0, 0), 1.0
        if 79 <= f <= 103: u = (f - 79)/24; return z3, (math.pi*(2*u - u*u), 0, 0), 1.0      # decelerates from loop speed to rest at 180 deg
        return z3, z3, 1.0
    if CID == "R4X_FlyingDroneDeployer" and node == "Iris":
        return z3, (0, 0, TAU*spin((f - 1)/48)), 1.0
    if CID == "FlyingDrone":
        if node == "Body":
            if 27 <= f <= 51: u = (f - 27)/24; return (0, 0, 0.040*ease(min(1, u*1.4)) - 0.006*math.sin(math.pi*u)), z3, 1.0
            if 53 <= f <= 77: u = (f - 53)/24; return (0, 0, 0.040 + 0.008*math.sin(TAU*u)), (0.03*math.sin(TAU*u), 0.02*math.sin(TAU*2*u), 0), 1.0
            if 79 <= f <= 103: u = (f - 79)/24; return (0, 0, 0.040 + 0.004*math.sin(TAU*u)), (math.radians(14), 0.03*math.sin(TAU*u), 0), 1.0
            return z3, z3, 1.0
        if node.startswith("Rotor_"):
            k = int(node.split("_")[1]); d = 1 if k % 2 else -1
            if 27 <= f <= 51: u = (f - 27)/24; return z3, (0, 0, d*TAU*3*u*u), 1.0                       # spin-up: 3 turns, accelerating
            if 53 <= f <= 103: u = ((f - 53) % 24)/24; return z3, (0, 0, d*TAU*3*u), 1.0                # 3 turns per loop
            return z3, z3, 1.0
    return None


# ------------------------------------------------------------------------------------------------ outline (Comic Outline style)
def add_outline(o, width, mat):
    """inverted hull: an offset copy of the mesh with flipped faces in a back-face-culled black material, joined into the node mesh"""
    me = o.data; bm = bmesh.new(); bm.from_mesh(me); bm.normal_update()
    src = bmesh.new(); src.from_mesh(me); bmesh.ops.remove_doubles(src, verts=src.verts, dist=1e-5); src.normal_update()
    tree = {}
    for v in src.verts: tree[tuple(round(c, 5) for c in v.co)] = v.normal.copy()
    geom = bmesh.ops.duplicate(bm, geom=bm.faces[:])["geom"]; nf = [g for g in geom if isinstance(g, bmesh.types.BMFace)]; nv = [g for g in geom if isinstance(g, bmesh.types.BMVert)]
    for v in nv:
        n = tree.get(tuple(round(c, 5) for c in v.co), v.normal); v.co += n*width
    bmesh.ops.reverse_faces(bm, faces=nf)
    slot = len(me.materials); me.materials.append(mat)
    for f in nf: f.material_index = slot
    bm.to_mesh(me); bm.free(); src.free()


def main():
    T = dict(cid=CID, style=STYLE, label=LABEL, size=SIZE); t0 = time.time(); bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; col = sc.collection
    fn = D.RECIPES[(CID, STYLE)]; T["construction"] = " ".join((fn.__doc__ or "").split())
    Bh = hf.Builder(True, STYLE, col); Bh.meta = {}; fn(Bh); meta = dict(Bh.meta); hi = hf.finish(Bh)
    Bl = hf.Builder(False, STYLE, col); Bl.meta = {}; fn(Bl); lo = hf.finish(Bl, 12 if STYLE == "3" else 38)
    T["meta"] = {k: (v if not isinstance(v, dict) else {kk: list(vv) for kk, vv in v.items()}) for k, v in meta.items()}
    T["tris_hi"] = {n: sum(hf.tris(o) for o in v) for n, v in hi.items()}; T["tris_lo"] = {n: hf.tris(o) for n, o in lo.items()}
    T["s_build"] = round(time.time() - t0, 1); t = time.time()
    # ---- contract: nodes, pivots, parents
    nodes = {n: dict(v) for n, v in C["nodes"].items()}
    if CID == "FlyingDrone":
        nodes["Body"]["pivot"] = tuple(meta["body_pivot"])
        for n, c in meta["rotors"].items(): nodes[n] = dict(parent="Body", pivot=tuple(c), bounds=None)
    extra = sorted(set(lo) - set(nodes)); missing = sorted(n for n in nodes if n not in lo and n != "MuzzleFlash")
    viol = []
    if extra: viol.append(dict(kind="unknown_nodes", nodes=extra))
    if missing: viol.append(dict(kind="missing_nodes", nodes=missing))
    e0, e1 = np.array(C["env"][0]), np.array(C["env"][1]); l_, h_ = bbox(list(lo.values()))
    if (l_ < e0 - 0.0005).any() or (h_ > e1 + 0.0005).any(): viol.append(dict(kind="envelope", min=l_.round(4).tolist(), max=h_.round(4).tolist(), env=C["env"]))
    for n, o in lo.items():
        b = (nodes.get(n) or {}).get("bounds")
        if b:
            l, h = bbox([o]); ex = max(float((np.array(b[0]) - l).max()), float((h - np.array(b[1])).max()))
            if ex > 0.003: viol.append(dict(kind="node_bounds", node=n, exceed_mm=round(ex*1000, 1), min=l.round(4).tolist(), max=h.round(4).tolist(), allowed=b))
    T["violations"] = viol; T["bounds"] = {n: [x.round(4).tolist() for x in bbox([o])] for n, o in lo.items()}
    # ---- bake + skin
    hbake.unwrap(lo); maps, tb = hbake.bake(hi, lo, SIZE, C["env"]); maps = hbake.dilate(maps, 12); T["s_bake"] = round(sum(tb.values()), 1)
    fx = D.FX[STYLE]; tex = hskin.compose(maps, C["env"], D.PAL[STYLE], fx, fx.get("seed", 0) + {"R4W_Basic": 0, "R4M_Wheel": 100, "R4X_FlyingDroneDeployer": 300, "FlyingDrone": 400}[CID])
    EXT = dict(base=".png", orm=".png", normal=".png", emission=".png"); tname = {}
    for k, srgb in (("base", True), ("orm", False), ("normal", False), ("emission", True)):
        tname[k] = "%s_%s_%s%s" % (CID, STYLE, k, EXT[k]); hbake.save(tex[k], P("textures", tname[k]), srgb)
    # ---- material (one texture set per model) + outline
    m = bpy.data.materials.new("%s_%s" % (CID, LABEL)); m.use_nodes = True; nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    def tx(k, cs):
        n = nt.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(P("textures", tname[k])); n.image.colorspace_settings.name = cs; return n
    nt.links.new(tx("base", "sRGB").outputs[0], b.inputs["Base Color"]); sp = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(tx("orm", "Non-Color").outputs[0], sp.inputs[0])
    nt.links.new(sp.outputs["Green"], b.inputs["Roughness"]); nt.links.new(sp.outputs["Blue"], b.inputs["Metallic"])
    nm = nt.nodes.new("ShaderNodeNormalMap"); nt.links.new(tx("normal", "Non-Color").outputs[0], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"])
    nt.links.new(tx("emission", "sRGB").outputs[0], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = D.GLOW_K[STYLE]
    for o in lo.values(): o.data.materials.clear(); o.data.materials.append(m)
    if STYLE in D.OUTLINE:
        ol = D.OUTLINE[STYLE]; om = bpy.data.materials.new("R4P_Outline"); om.use_nodes = True; ob = om.node_tree.nodes["Principled BSDF"]
        ob.inputs["Base Color"].default_value = (*ol["rgb"], 1); ob.inputs["Roughness"].default_value = 1.0; ob.inputs["Specular IOR Level"].default_value = 0.0
        om.use_backface_culling = True
        for n, o in lo.items():
            if n == "MuzzleFlash": continue
            add_outline(o, ol["width"], om)
        T["outline"] = dict(method="inverted hull in the node mesh, material R4P_Outline (back-face culled)", width_m=ol["width"])
    T["s_skin"] = round(time.time() - t, 1); t = time.time()
    # ---- muzzle flash (gun): a small emissive star; idle scale 0.001
    if CID == "R4W_Basic" and "MuzzleFlash" not in lo:
        fm = bpy.data.materials.new("R4P_Glow"); fm.use_nodes = True; fb = fm.node_tree.nodes["Principled BSDF"]; g = D.PAL[STYLE]["glow"][0]
        fb.inputs["Base Color"].default_value = (*g, 1); fb.inputs["Emission Color"].default_value = (*g, 1); fb.inputs["Emission Strength"].default_value = 8.0
        bm = bmesh.new(); pts = D.star(0.018, 0.060, 6)
        for k in range(2):
            ring_ = [bm.verts.new((x, -0.624 - 0.04*k, 0.135 + y)) for (x, y) in pts]; bm.faces.new(ring_ if k else list(reversed(ring_)))
        me = bpy.data.meshes.new("flash"); bm.to_mesh(me); bm.free(); fo = bpy.data.objects.new("LO_MuzzleFlash", me); col.objects.link(fo); me.materials.append(fm); lo["MuzzleFlash"] = fo
    # ---- rig: every node and empty is placed once its parent exists
    root = bpy.data.objects.new(CID + "_ROOT", None); col.objects.link(root); objs = {"ROOT": root}; piv = {"ROOT": Vector((0, 0, 0))}; order = []
    items = {n: ("node", v) for n, v in nodes.items() if n in lo}; items.update({n: ("empty", e) for n, e in C["empties"].items()})
    while items:
        placed = False
        for n, (kind, v) in list(items.items()):
            par = v["parent"]
            if par not in objs: continue
            if kind == "node":
                o = lo[n]; pv = Vector(v["pivot"]); o.data.transform(Matrix.Translation(-pv)); o.name = CID + "_" + n; o.data.name = o.name; order.append(n)
            else:
                pv = Vector(v["loc"] if v["loc"] is not None else meta.get("muzzle", (0, 0, 0))); o = bpy.data.objects.new(n, None); col.objects.link(o); o.empty_display_size = 0.05; o.rotation_euler = v["rot"]
            o.parent = objs[par]; o.location = pv - piv[par]; objs[n] = o; piv[n] = pv; del items[n]; placed = True
        if not placed: raise RuntimeError("rig: unresolved parents %s" % list(items))
    # ---- animation, baked per frame
    F = max(c[2] for c in C["clips"]); sc.render.fps = FPS; sc.frame_start = 1; sc.frame_end = F; anim = []
    base_loc = {n: objs[n].location.copy() for n in order}
    for n in order:
        if pose(n, 1) is None: continue
        o = objs[n]; anim.append(o)
        for f in range(1, F + 1):
            dl, r, s = pose(n, f); o.location = base_loc[n] + Vector(dl); o.rotation_euler = r; o.scale = (s, s, s)
            o.keyframe_insert("location", frame=f); o.keyframe_insert("rotation_euler", frame=f); o.keyframe_insert("scale", frame=f)
    sc.frame_set(1)
    T["animated_nodes"] = [o.name for o in anim]; T["clips"] = [dict(name=c[0], start=c[1], end=c[2], loop=c[3]) for c in C["clips"]]
    # ---- LOD1
    bpy.ops.object.select_all(action="DESELECT"); cp = []
    for n in order:
        if n == "MuzzleFlash": continue
        o = objs[n]; c = o.copy(); c.data = o.data.copy(); c.parent = None; c.animation_data_clear(); col.objects.link(c); c.matrix_world = Matrix.Translation(piv[n]); cp.append(c)
    for c in cp: c.select_set(True)
    bpy.context.view_layer.objects.active = cp[0]; bpy.ops.object.join(); allo = bpy.context.view_layer.objects.active; bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    lod, lodinfo = hlod.lod1(allo, col); bpy.data.objects.remove(allo, do_unlink=True); lod.name = CID + "_LOD1"; T["lod1"] = lodinfo; T["tris_lod1"] = hf.tris(lod)
    # ---- source collections
    hc = bpy.data.collections.new("SOURCE_highpoly"); gc = bpy.data.collections.new("GAME_rig"); lc = bpy.data.collections.new("LOD1")
    for c in (hc, gc, lc): sc.collection.children.link(c)
    clay = bpy.data.materials.new("CLAY"); clay.use_nodes = True; clay.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.55, 0.55, 0.55, 1)
    for v in hi.values():
        for o in v: o.data.materials.clear(); o.data.materials.append(clay); col.objects.unlink(o); hc.objects.link(o)
    for o in objs.values(): col.objects.unlink(o); gc.objects.link(o)
    col.objects.unlink(lod); lc.objects.link(lod)
    # ---- export (game rig only)
    rig = list(objs.values()); d = P("models", LABEL, CID, CID + ".glb")
    def sel(L):
        bpy.ops.object.select_all(action="DESELECT")
        for o in L: o.hide_set(False); o.select_set(True)
        bpy.context.view_layer.objects.active = L[0]
    sel(rig)
    bpy.ops.export_scene.fbx(filepath=d[:-4] + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=False, object_types={'MESH', 'EMPTY'},
                             mesh_smooth_type='FACE', add_leaf_bones=False, use_mesh_modifiers=True, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True)
    for o in anim:
        ad = o.animation_data; act = ad.action; slot = getattr(ad, "action_slot", None); tr = ad.nla_tracks.new(); tr.name = "Timeline"; st = tr.strips.new("Timeline", 1, act)
        try:
            if slot is not None: st.action_slot = slot
        except Exception: pass
        ad.action = None
    sel(rig)
    bpy.ops.export_scene.gltf(filepath=d, export_format='GLB', use_selection=True, export_yup=True, export_apply=False, export_extras=True, export_animations=True, export_animation_mode='NLA_TRACKS', export_frame_range=True)
    sel([lod])
    bpy.ops.export_scene.gltf(filepath=d[:-4] + "_LOD1.glb", export_format='GLB', use_selection=True, export_yup=True, export_apply=True, export_animations=False)
    bpy.ops.export_scene.fbx(filepath=d[:-4] + "_LOD1.fbx", use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', object_types={'MESH'}, mesh_smooth_type='FACE', bake_anim=False, path_mode='COPY', embed_textures=True)
    for o in anim:                                                                                   # back to plain actions for the source file
        tr = o.animation_data.nla_tracks[0]; o.animation_data.action = tr.strips[0].action; o.animation_data.nla_tracks.remove(tr)
    bpy.ops.wm.save_as_mainfile(filepath=P("source", LABEL, CID + "_SOURCE.blend"), compress=True, relative_remap=True)
    T["s_export"] = round(time.time() - t, 1); t = time.time()
    # ---- expected state for the QA (world matrices at test frames)
    test_frames = sorted({1} | {c[1] + (c[2] - c[1])//2 for c in C["clips"]} | {c[2] for c in C["clips"]})
    def snap(objects):
        out = {}
        for f in test_frames:
            sc.frame_set(f); out[f] = {o.name: [list(r) for r in o.matrix_world] for o in objects}
        sc.frame_set(1); return out
    for o in anim:                                                                                   # NLA was removed; actions are back on the objects
        pass
    exp = snap(rig); exp_b = {o.name: [x.round(4).tolist() for x in bbox([o])] for o in rig if o.type == "MESH"}
    exp_par = {o.name: (o.parent.name if o.parent else None) for o in rig}
    # port contact (flush coverage of the mount face)
    ports = []
    meshes = [o for o in rig if o.type == "MESH" and not o.name.endswith("MuzzleFlash")]
    for pt in C["ports"]:
        dg = bpy.context.evaluated_depsgraph_get(); hit = tot = 0
        V = []; Fc = []
        for o in meshes:
            me = o.evaluated_get(dg).to_mesh(); off = len(V); V += [o.matrix_world @ v.co for v in me.vertices]; Fc += [[off + i for i in p.vertices] for p in me.polygons if (p.material_index == 0)]; o.evaluated_get(dg).to_mesh_clear()
        bvh = BVHTree.FromPolygons(V, Fc); hz = pt["half"]; sgn = -1 if pt["dir"] == "down" else 1
        for i in range(24):
            for j in range(24):
                x = -hz + (i + 0.5)*2*hz/24; y = -hz + (j + 0.5)*2*hz/24; tot += 1
                r = bvh.ray_cast(Vector((x, y, pt["z"] + sgn*0.02)), Vector((0, 0, -sgn)), 0.05)
                if r[0] is not None and abs(r[0].z - pt["z"]) <= 0.0015: hit += 1
        ports.append(dict(dir=pt["dir"], flush_pct=round(100*hit/tot, 1), PASS=hit/tot >= 0.80))
    T["port_contact"] = ports
    # ---- re-import
    rq = {}
    for ext in (".glb", ".fbx"):
        for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
        if ext == ".glb": bpy.ops.import_scene.gltf(filepath=d)
        else: bpy.ops.import_scene.fbx(filepath=d[:-4] + ".fbx", use_anim=True, anim_offset=0.0)
        imp = {o.name.split(".")[0]: o for o in bpy.data.objects}
        names_ok = sorted(set(exp_par) - set(imp)); r = dict(missing=names_ok)
        r["parents_ok"] = all((imp[n].parent.name.split(".")[0] if imp[n].parent else None) == p for n, p in exp_par.items() if n in imp and p is not None)
        dev = 0.0; adev = 0.0
        for n, (l, h) in exp_b.items():
            if n in imp and imp[n].type == "MESH":
                l2, h2 = bbox([imp[n]]); dev = max(dev, float(np.abs(np.array(l) - l2).max()), float(np.abs(np.array(h) - h2).max()))
        best = None
        for off in (0, -1, 1):                                                                       # importers may shift the timeline by one frame
            dv = 0.0
            for f in test_frames:
                sc.frame_set(f + off)
                for n, M in exp[f].items():
                    if n in imp: dv = max(dv, float(np.abs(np.array(M)[:3, :4] - np.array(imp[n].matrix_world)[:3, :4]).max()))
            if best is None or dv < best[0]: best = (dv, off)
        adev = best[0]; r["frame_offset"] = best[1]; sc.frame_set(1)
        r["bounds_dev_mm"] = round(dev*1000, 2); r["anim_pos_dev_mm"] = round(adev*1000, 2)
        r["materials"] = sorted({s.material.name.split(".")[0] for o in imp.values() if o.type == "MESH" for s in o.material_slots if s.material})
        r["textures"] = sorted({n.image.name for mm in bpy.data.materials if mm.use_nodes for n in mm.node_tree.nodes if n.type == "TEX_IMAGE" and n.image})
        r["actions"] = len(bpy.data.actions); r["triangles"] = sum(hf.tris(o) for o in imp.values() if o.type == "MESH")
        r["scale"] = sorted({round(c, 4) for o in imp.values() for c in o.matrix_world.to_scale()})[:3]
        r["PASS"] = (not r["missing"]) and r["parents_ok"] and dev < 0.002 and adev < 0.002 and len(r["textures"]) >= 3
        rq[ext] = r
    T["reimport"] = rq
    T["status"] = "BLENDER_PASS" if (not viol and all(p["PASS"] for p in ports) and all(r["PASS"] for r in rq.values())) else "HOLD"
    T["unity"] = "UNITY_NOT_RUN"; T["files"] = {os.path.basename(p): sha(p)[:16] for p in (d, d[:-4] + ".fbx", d[:-4] + "_LOD1.glb", d[:-4] + "_LOD1.fbx")}
    T["triangles"] = sum(T["tris_lo"].values()); T["s_qa"] = round(time.time() - t, 1)
    json.dump(T, open(P("models", LABEL, CID, CID + "_QA.json"), "w"), indent=1)
    man = dict(id=CID, style=LABEL, units="metre 1:1", axes="file/Unity +Z forward, +Y up (Blender -Y forward, Z up)", source_contract=C["source"],
               hierarchy=exp_par, pivots={CID + "_" + n: list(piv[n]) for n in order}, empties={n: dict(parent=e["parent"], loc=list(piv[n])) for n, e in C["empties"].items() if n in piv},
               clips=T["clips"], fps=FPS, animated_nodes=T["animated_nodes"], materials=rq[".glb"]["materials"], textures=tname, construction=T["construction"], meta=T["meta"],
               gameplay="visual only: no gameplay capability, timing or collider is added")
    json.dump(man, open(P("models", LABEL, CID, CID + "_manifest.json"), "w"), indent=1)
    log("MODEL_DONE", CID, STYLE, T["status"], json.dumps({k: T[k] for k in ("violations", "port_contact", "triangles", "tris_lod1")}), json.dumps({k: {kk: vv for kk, vv in v.items() if kk in ("missing", "parents_ok", "bounds_dev_mm", "anim_pos_dev_mm", "PASS", "materials")} for k, v in rq.items()}))


try: main()
except Exception:
    import traceback; traceback.print_exc(); log("MODEL_FAILED", CID, STYLE)
