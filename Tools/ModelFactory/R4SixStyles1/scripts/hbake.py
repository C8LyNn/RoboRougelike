"""#1239 Hero Benchmark Pass 2: unwrap the game mesh and bake the source model into it. RUNS INSIDE BLENDER (Cycles).
Maps (float, read straight into numpy): nrm tangent normal, wn object-space normal, ao, id material id, edge edge mask (Bevel node), pos position."""
import bpy, math, time
import numpy as np
import hf

PASSES = [("id", "EMIT"), ("nrm", "NORMAL"), ("wn", "NORMAL"), ("ao", "AO"), ("edge", "EMIT"), ("pos", "EMIT")]


def unwrap(lo, margin=0.004, angle=66):
    objs = list(lo.values()); bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
        if not o.data.uv_layers: o.data.uv_layers.new(name="UVMap")
    bpy.context.view_layer.objects.active = objs[0]; bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin, area_weight=0.0, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.uv.select_all(action="SELECT"); bpy.ops.uv.average_islands_scale(); bpy.ops.uv.pack_islands(rotate=True, margin=margin)
    bpy.ops.object.mode_set(mode="OBJECT")


def _hi_material(zone, lo_, hi_, bevel_r):
    m = bpy.data.materials.new("HZ_" + zone); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear(); N = nt.nodes.new; L = nt.links.new
    out = N("ShaderNodeOutputMaterial"); em = N("ShaderNodeEmission"); L(em.outputs[0], out.inputs["Surface"])
    rgb = N("ShaderNodeRGB"); rgb.outputs[0].default_value = (*hf.ZCOL[zone], 1.0); rgb.name = "SRC_id"
    geo = N("ShaderNodeNewGeometry"); bev = N("ShaderNodeBevel"); bev.samples = 8; bev.inputs["Radius"].default_value = bevel_r
    dot = N("ShaderNodeVectorMath"); dot.operation = "DOT_PRODUCT"; L(bev.outputs[0], dot.inputs[0]); L(geo.outputs["Normal"], dot.inputs[1])
    inv = N("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0; L(dot.outputs["Value"], inv.inputs[1])
    gain = N("ShaderNodeMath"); gain.operation = "MULTIPLY"; gain.inputs[1].default_value = 6.0; gain.use_clamp = True; L(inv.outputs[0], gain.inputs[0]); gain.name = "SRC_edge"
    sub = N("ShaderNodeVectorMath"); sub.operation = "SUBTRACT"; L(geo.outputs["Position"], sub.inputs[0]); sub.inputs[1].default_value = lo_
    mul = N("ShaderNodeVectorMath"); mul.operation = "MULTIPLY"; L(sub.outputs[0], mul.inputs[0]); mul.inputs[1].default_value = [1.0/max(h - l, 1e-6) for l, h in zip(lo_, hi_)]; mul.name = "SRC_pos"
    return m


def _mode(mode):
    for m in bpy.data.materials:
        if m.name.startswith("HZ_"):
            nt = m.node_tree; src = nt.nodes.get("SRC_" + mode); em = next(n for n in nt.nodes if n.type == "EMISSION")
            if src: nt.links.new(src.outputs[0], em.inputs["Color"])


def bake(hi, lo, size, env, samples=12, ray=0.02, bevel_r=0.004):
    """hi = {node: [objects]}, lo = {node: object}. Returns {pass: float array (size, size, 4)} with row 0 at the BOTTOM of the image + timings"""
    sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = samples; sc.cycles.device = "CPU"; sc.cycles.use_denoising = False
    bk = sc.render.bake; bk.use_selected_to_active = True; bk.cage_extrusion = ray*0.5; bk.max_ray_distance = ray; bk.margin = 0; bk.use_clear = False
    for objs in hi.values():
        for o in objs:
            z = o["zone"]; m = bpy.data.materials.get("HZ_" + z) or _hi_material(z, env[0], env[1], bevel_r); o.data.materials.clear(); o.data.materials.append(m)
    lom = {s.material for o in lo.values() for s in o.material_slots}
    for m in lom:
        m.use_nodes = True; n = m.node_tree.nodes.get("BAKE") or m.node_tree.nodes.new("ShaderNodeTexImage"); n.name = "BAKE"; m.node_tree.nodes.active = n
    out = {}; T = {}
    for name, kind in PASSES:
        t = time.time(); img = bpy.data.images.new("bk_" + name, size, size, alpha=True, float_buffer=True); img.colorspace_settings.name = "Non-Color"
        for m in lom: m.node_tree.nodes["BAKE"].image = img
        if kind == "EMIT": _mode(name)
        bk.normal_space = "OBJECT" if name == "wn" else "TANGENT"; sc.cycles.samples = {"ao": 32, "edge": 8}.get(name, 2)
        for node, lo_o in lo.items():
            for n2, objs in hi.items():
                for o in objs: o.hide_render = (n2 != node)
            for n2, o in lo.items(): o.hide_render = (n2 != node)
            bpy.ops.object.select_all(action="DESELECT")
            for o in hi.get(node, []): o.select_set(True)
            lo_o.select_set(True); bpy.context.view_layer.objects.active = lo_o; bpy.ops.object.bake(type=kind)
        a = np.empty(size*size*4, np.float32); img.pixels.foreach_get(a); out[name] = a.reshape(size, size, 4).copy(); bpy.data.images.remove(img); T[name] = round(time.time() - t, 2)
    for objs in hi.values():
        for o in objs: o.hide_render = False
    for o in lo.values(): o.hide_render = False
    return out, T


def dilate(maps, n=10):
    """fill the empty texels around the islands (the bake itself runs without margin so that separately baked nodes cannot bleed into each other)"""
    cov = maps["id"][..., :3].max(axis=2) > 0.02; out = {}
    for k, a in maps.items():
        a = a[..., :3].copy(); c = cov.copy()
        for _ in range(n):
            acc = np.zeros_like(a); cnt = np.zeros(c.shape, np.float32)
            for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                s = np.roll(np.roll(a*c[..., None], dy, 0), dx, 1); m = np.roll(np.roll(c, dy, 0), dx, 1); acc += s; cnt += m
            new = (~c) & (cnt > 0); a[new] = acc[new]/cnt[new][:, None]; c = c | new
        out[k] = a
    out["cov"] = cov; return out


def save(arr, path, srgb):
    """arr: (H, W, 3) float 0..1, row 0 at the bottom, already in the colour space it is stored in"""
    h, w = arr.shape[:2]; img = bpy.data.images.new("out", w, h, alpha=False); img.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
    a = np.ones((h, w, 4), np.float32); a[..., :3] = np.clip(arr, 0, 1); img.pixels.foreach_set(a.ravel()); img.filepath_raw = path
    if path.lower().endswith(".jpg"): img.file_format = "JPEG"; img.save(quality=93)
    else: img.file_format = "PNG"; img.save()
    bpy.data.images.remove(img)
