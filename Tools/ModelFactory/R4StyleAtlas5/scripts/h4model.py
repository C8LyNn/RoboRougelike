"""#1239 Hero Benchmark Pass 4: build ONE new model (same steps as h2model.py of Pass 2: source model + game mesh from the recipe, contract check,
unwrap, bake, skin, LOD1, export of the game mesh into the pilot mesh_source format; pbuild then adds the approved animation, QA, FBX / GLB).
Recipes, palettes and wear come from h4defs. SET = "p" (the proposal, or the first of two) or "q" (second proposal where the survey was a tie).
usage: blender -b --factory-startup --python h4model.py -- OUT TEMPLATES TYPE STYLE [SIZE] [SET]"""
import bpy, bmesh, sys, os, math, time, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import hf, hbake, hskin, hlod, h4defs as D
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]; OUT, TPL, TYPE, STYLE = a[:4]; SIZE = int(a[4]) if len(a) > 4 else 2048; SET = a[5] if len(a) > 5 else "p"; SK = D.skin_key(TYPE, STYLE, SET)
LABEL = dict(A="A_Sleek", B="B_Brutal", C="C_Arcade", D="D_Scrap", E="E_Toon")[STYLE]; CID = dict(Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Battery="R4X_Battery")[TYPE]
from hlod import ZMAP, mesh_json
def P(*p): d = os.path.join(OUT, *p[:-1]); os.makedirs(d, exist_ok=True); return os.path.join(d, p[-1])
def bbox(objs):
    V = np.array([o.matrix_world @ v.co for o in objs for v in o.data.vertices]); return V.min(0), V.max(0)

def preview(objs, path, loc, tgt, mat=None, wire=False):
    sc = bpy.context.scene; eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]; sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in eng else 'BLENDER_EEVEE_NEXT'
    for o in bpy.data.objects:
        if o.type == "MESH": o.hide_render = o not in objs
    old = {}
    if mat is not None:
        for o in objs: old[o] = list(o.data.materials); o.data.materials.clear(); o.data.materials.append(mat)
    sc.render.resolution_x, sc.render.resolution_y = 1200, 900; sc.render.filepath = path; sc.render.image_settings.file_format = "JPEG"; sc.render.image_settings.quality = 88
    cam = sc.camera; cam.location = loc; cam.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    if wire:
        for o in objs: m = o.modifiers.new("w", "WIREFRAME"); m.thickness = 0.0012; m.use_replace = False
    bpy.ops.render.render(write_still=True)
    if wire:
        for o in objs: o.modifiers.remove(o.modifiers["w"])
    for o, ms in old.items():
        o.data.materials.clear(); [o.data.materials.append(m) for m in ms]


def main():
    T = {"type": TYPE, "style": STYLE, "set": SET, "baseline": D.BASELINE[(TYPE, STYLE, SET)], "skin": SK}; t = time.time(); bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; col = sc.collection
    tpl = json.load(open(os.path.join(TPL, LABEL, CID + ".json"))); env = tpl["envelope"]; fn = D.RECIPES[(TYPE, STYLE, SET)]
    Bh = hf.Builder(True, STYLE, col); fn(Bh); hi = hf.finish(Bh); T["s_build_hi"] = round(time.time() - t, 1); t = time.time()
    Bl = hf.Builder(False, STYLE, col); fn(Bl); lo = hf.finish(Bl); T["s_build_lo"] = round(time.time() - t, 1); t = time.time()
    T["tris_hi"] = {n: sum(hf.tris(o) for o in v) for n, v in hi.items()}; T["tris_lo"] = {n: hf.tris(o) for n, o in lo.items()}; T["tris_hi_total"] = sum(T["tris_hi"].values()); T["tris_lo_total"] = sum(T["tris_lo"].values())
    # contract: envelope and bounds of the approved source nodes
    viol = []; lo_, hi_ = bbox(list(lo.values())); e0, e1 = np.array(env[0]), np.array(env[1])
    if (lo_ < e0 - 0.0005).any() or (hi_ > e1 + 0.0005).any(): viol.append(dict(node="*", kind="envelope", min=lo_.round(4).tolist(), max=hi_.round(4).tolist(), env=env))
    for n, b in (tpl.get("node_bounds") or {}).items():
        if n in lo:
            l, h = bbox([lo[n]]); ex = max(float((np.array(b[0]) - l).max()), float((h - np.array(b[1])).max()))
            if ex > 0.003: viol.append(dict(node=n, kind="node_bounds", exceed_mm=round(ex*1000, 1), min=l.round(4).tolist(), max=h.round(4).tolist(), allowed=b))
    known = set(tpl["meshes"].keys()); extra = sorted(set(lo) - known); missing = sorted(n for n in known - set(lo) if n not in tpl["fx"])
    if extra: viol.append(dict(kind="unknown_nodes", nodes=extra))
    T["violations"] = viol; T["template_nodes_not_modelled"] = missing; T["bounds"] = {n: [x.round(4).tolist() for x in bbox([o])] for n, o in lo.items()}
    hbake.unwrap(lo); T["s_unwrap"] = round(time.time() - t, 1); t = time.time()
    maps, tb = hbake.bake(hi, lo, SIZE, env); T["s_bake"] = round(sum(tb.values()), 1); t = time.time(); maps = hbake.dilate(maps, 12)
    fx = D.FX[SK]; tex = hskin.compose(maps, env, D.PAL[SK], fx, fx.get("seed", 0) + {"Gun": 0, "Wheel": 100, "Battery": 200}[TYPE]); T["s_skin"] = round(time.time() - t, 1); t = time.time()
    names = {}
    EXT = dict(base=".jpg", orm=".jpg", normal=".png", emission=".jpg")                                 # masters: lossless normal map, high-quality JPEG for the rest
    for k, srgb in (("base", True), ("orm", False), ("normal", False), ("emission", True)): names[k] = LABEL + "/" + CID + "_" + k + EXT[k]; hbake.save(tex[k], P("textures", LABEL, CID + "_" + k + EXT[k]), srgb)
    Z = hskin.zone_masks(maps["id"], hf.ZONES, hf.ZCOL); pm = np.stack([Z["base"], Z["sec"] | Z["alt"] | Z["alt2"], Z["dark"] | Z["metal"]], -1).astype(np.float32); hbake.save(pm, P("textures", LABEL, CID + "_paintmask.png"), False)
    # preview material = the exported texture set
    m = bpy.data.materials.new("SKIN"); m.use_nodes = True; nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    def tx(k, cs): n = nt.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(P("textures", LABEL, CID + "_" + k + EXT[k])); n.image.filepath = "//../../textures/" + LABEL + "/" + CID + "_" + k + EXT[k]; n.image.colorspace_settings.name = cs; return n
    nt.links.new(tx("base", "sRGB").outputs[0], b.inputs["Base Color"]); sp = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(tx("orm", "Non-Color").outputs[0], sp.inputs[0]); nt.links.new(sp.outputs["Green"], b.inputs["Roughness"]); nt.links.new(sp.outputs["Blue"], b.inputs["Metallic"])
    nm = nt.nodes.new("ShaderNodeNormalMap"); nt.links.new(tx("normal", "Non-Color").outputs[0], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"]); nt.links.new(tx("emission", "sRGB").outputs[0], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = dict(A=3.0, B=2.5, C=2.0, D=2.5, E=2.5)[STYLE]
    clay = bpy.data.materials.new("CLAY"); clay.use_nodes = True; cb = clay.node_tree.nodes["Principled BSDF"]; cb.inputs["Base Color"].default_value = (0.55, 0.55, 0.55, 1); cb.inputs["Roughness"].default_value = 0.6
    # LOD1 and export of the game mesh
    bpy.ops.object.select_all(action="DESELECT"); cp = []
    for o in lo.values(): c = o.copy(); c.data = o.data.copy(); col.objects.link(c); c.select_set(True); cp.append(c)
    bpy.context.view_layer.objects.active = cp[0]; bpy.ops.object.join(); allo = bpy.context.view_layer.objects.active; lod, lodinfo = hlod.lod1(allo, col); bpy.data.objects.remove(allo, do_unlink=True)
    T["tris_lod1"] = hf.tris(lod); T["lod1"] = lodinfo
    piv = lambda n: (0, 0, 0) if n == "Body" else tuple(tpl["nodes"][n]["pivot"])
    src = dict(tpl); meshes = {n: mesh_json(o, piv(n)) for n, o in lo.items()}; src["pivot_relative"] = True
    for n in tpl["meshes"]:
        if n in tpl["fx"]: meshes[n] = tpl["meshes"][n]
    src["meshes"] = meshes; src["lod1"] = mesh_json(lod); src["textures"] = dict(names, normal_strength=1.0, uv="baked", baked=True, paintmask=LABEL + "/" + CID + "_paintmask.png", size=SIZE)
    for n in missing:
        for k in ("nodes", "node_names", "mechanisms"): (src.get(k) or {}).pop(n, None)
        for k in ("extra_tracks", "tracks"): (src["animation"].get(k) or {}).pop(n, None)
    src["vcol"] = None; src["bevel"] = None; src["polish_pass"] = "hero4"; src["proposal"] = dict(set=SET, baseline=T["baseline"]); src["stats"] = dict(triangles=T["tris_lo_total"], shells=len(Bl.objs), parts=len(Bl.objs), triangles_source=T["tris_hi_total"], triangles_lod1=T["tris_lod1"], lod1=lodinfo)
    pal = D.PAL[SK]; src["materials"] = {z: dict(rgb=list(pal[k][0]), metallic=pal[k][1], roughness=pal[k][2]) for k, z in (("base", "R4P_Base"), ("sec", "R4P_Secondary"), ("dark", "R4P_Trim"), ("metal", "R4P_Metal"), ("rust" if "rust" in pal else "dark", "R4P_Wear"))}
    src["glow"] = list(pal["glow"][0]); src["glow_strength"] = dict(A=3.0, B=2.5, C=2.0, D=2.5, E=2.5)[STYLE]; src["construction"] = (fn.__doc__ or "").strip().replace("\n", " "); src["construction"] = " ".join(src["construction"].split())
    json.dump(src, open(P("mesh_source", LABEL, CID + ".json"), "w"), separators=(",", ":"))
    # scene for the previews and for the saved source file
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); col.objects.link(cam); sc.camera = cam; cam.data.lens = 70
    for (rot, e, c) in (((math.radians(50), 0, math.radians(35)), 3.6, (1.0, 0.96, 0.9)), ((math.radians(65), 0, math.radians(-140)), 1.2, (0.75, 0.83, 1.0))):
        l = bpy.data.lights.new("sun", "SUN"); l.energy = e; l.color = c; l.angle = math.radians(6); lo2 = bpy.data.objects.new("sun", l); lo2.rotation_euler = rot; col.objects.link(lo2)
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.44, 0.48, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    sc.view_settings.view_transform = "AgX" if "AgX" in [i.identifier for i in sc.view_settings.bl_rna.properties['view_transform'].enum_items] else "Filmic"
    g = bpy.data.meshes.new("g"); g.from_pydata([(-6, -6, env[0][2] - 0.001), (6, -6, env[0][2] - 0.001), (6, 6, env[0][2] - 0.001), (-6, 6, env[0][2] - 0.001)], [], [(0, 1, 2, 3)]); go = bpy.data.objects.new("ground", g); col.objects.link(go); go.data.materials.append(clay)
    hi_all = [o for v in hi.values() for o in v]; lo_all = list(lo.values())
    for o in lo_all: o.data.materials.clear(); o.data.materials.append(m)
    lod.data.materials.clear(); lod.data.materials.append(m)
    for o in hi_all: o.data.materials.clear(); o.data.materials.append(clay)
    hc = bpy.data.collections.new("SOURCE_highpoly"); gc = bpy.data.collections.new("GAME_lowpoly"); lc = bpy.data.collections.new("LOD1"); [sc.collection.children.link(c) for c in (hc, gc, lc)]
    for objs, c in ((hi_all, hc), (lo_all, gc), ([lod], lc)):
        for o in objs: col.objects.unlink(o); c.objects.link(o)
    bpy.ops.wm.save_as_mainfile(filepath=P("source", LABEL, CID + "_SOURCE.blend"), compress=True)
    ctr = (e0 + e1)/2; ext = float((e1 - e0).max()); d = ext*3.0; fwd = {"Wheel": (0.80, -0.52, 0.30), "Gun": (0.62, -0.70, 0.36), "Battery": (0.56, -0.74, 0.38)}[TYPE]; back = {"Wheel": (-0.80, -0.50, 0.33), "Gun": (-0.66, 0.62, 0.42), "Battery": (-0.60, 0.70, 0.40)}[TYPE]
    L = lambda v: tuple(ctr + np.array(v)/np.linalg.norm(v)*d)
    preview(lo_all + [go], P("preview", LABEL, CID + "_lo.jpg"), L(fwd), ctr); preview(lo_all + [go], P("preview", LABEL, CID + "_lo_back.jpg"), L(back), ctr)
    preview(hi_all + [go], P("preview", LABEL, CID + "_hi_clay.jpg"), L(fwd), ctr); preview(lo_all + [go], P("preview", LABEL, CID + "_lo_clay.jpg"), L(fwd), ctr, clay); preview(lo_all + [go], P("preview", LABEL, CID + "_lo_wire.jpg"), L(fwd), ctr, clay, True)
    preview([lod, go], P("preview", LABEL, CID + "_lod1.jpg"), L(fwd), ctr)
    T["s_export_render"] = round(time.time() - t, 1); json.dump(T, open(P("report", LABEL + "_" + CID + ".json"), "w"), indent=1); print("[HERO4] MODEL_DONE", TYPE, STYLE, {k: v for k, v in T.items() if k not in ("bounds",)}, flush=True)


try: main()
except Exception:
    import traceback; traceback.print_exc(); print("[HERO4] MODEL_FAILED", TYPE, STYLE, flush=True)
