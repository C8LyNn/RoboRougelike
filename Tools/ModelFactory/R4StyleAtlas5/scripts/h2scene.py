# RoboRougelike #1239 - Hero Benchmark Pass: hero robot, weapon, drive and energy module of every style in one arena, rendered from the EXPORTED GLB
# files (Blender / EEVEE). BEFORE = Visual Polish Pass 2 (its variants, hull kit in its palette). AFTER = hero variants (bevelled geometry, benchmark
# palettes, clear coat) + hull kit in the hero palette. Look-dev that lives in THIS script and must be rebuilt in the game: key + rim light, depth of
# field in close-ups, per-object rust offset (Scrap) and, for Heroic Toon, the banded-light shader with rim and the inverted-hull outline.
# hero pass 2 variant of hscene.py (authored texture sets, repaint of kept parts).
# usage: blender -b --factory-startup --python h2scene.py -- OUT SCENE_SRC KIT_SRC SHOTS_JSON PASS2_VARIANTS PASS2_VARIANTS HERO_VARIANTS TEXTURES KIT_OUT [ABCDE] [shot,..]
import bpy, sys, os, json, math, time
from mathutils import Matrix, Vector
from bpy_extras.object_utils import world_to_camera_view
argv = sys.argv[sys.argv.index("--") + 1:]
OUT, SRC, KSRC, SHOTS, PASS1, PILOT2, AFTER, TEX, KOUT = argv[:9]; STYLES = list(argv[9]) if len(argv) > 9 else list("ABCDE"); ONLY = argv[10].split(",") if len(argv) > 10 else None
LABEL = dict(A="A_Sleek", B="B_Brutal", C="C_Arcade", D="D_Scrap", E="E_Toon"); ZONES = ["R4P_Base", "R4P_Secondary", "R4P_Trim", "R4P_Metal", "R4P_Glow", "R4P_Wear"]
CID = dict(Cube="R4B_Cube", Strut="R4C_STRUT_STANDARD", Gun="R4W_Basic", Wheel="R4M_WheelM_Side_Offroad", Hover="R4M_HoverEngine", Battery="R4X_Battery", Deployer="R4X_ExplosiveTrapDeployer", Trap="R4D_ExplosiveTrap")
def log(*a): print("[POLISH]", *a, flush=True)
def _img(name, cs):
    img = bpy.data.images.get(name) or bpy.data.images.load(os.path.join(TEX, name)); img.colorspace_settings.name = cs; return img
def make_material(zone, src, pre="HL_"):
    m = bpy.data.materials.new(pre + zone); m.use_nodes = True; nt = m.node_tree; b = nt.nodes.get("Principled BSDF"); tx = src["textures"]
    if zone == "R4P_Glow":
        gl = src["glow"]; k = src["glow_strength"]; b.inputs["Emission Color"].default_value = (*gl, 1.0); b.inputs["Emission Strength"].default_value = k; b.inputs["Base Color"].default_value = (*[c*0.5 for c in gl], 1.0); return m
    d = src["materials"][zone]; rgb, metal, rough = d["rgb"], d["metallic"], d["roughness"]; b.inputs["Base Color"].default_value = (*rgb, 1.0); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough
    if tx.get("base"):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["base"], "sRGB"); mx = nt.nodes.new("ShaderNodeMix"); mx.data_type = 'RGBA'; mx.blend_type = 'MULTIPLY'; mx.inputs[0].default_value = 1.0
        nt.links.new(t.outputs["Color"], mx.inputs[6]); mx.inputs[7].default_value = (*rgb, 1.0); nt.links.new(mx.outputs[2], b.inputs["Base Color"])
    if tx.get("orm"):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["orm"], "Non-Color"); sp = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(t.outputs["Color"], sp.inputs[0])
        for ch, sock, val in (("Green", "Roughness", rough), ("Blue", "Metallic", metal)):
            mu = nt.nodes.new("ShaderNodeMath"); mu.operation = 'MULTIPLY'; nt.links.new(sp.outputs[ch], mu.inputs[0]); mu.inputs[1].default_value = val; nt.links.new(mu.outputs[0], b.inputs[sock])
    if tx.get("normal"):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["normal"], "Non-Color"); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = tx["normal_strength"]
        nt.links.new(t.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    return m
def vary(m):
    """Scrap, AFTER only: per-object offset of the tiling wear textures (a shader feature the game needs too), so no two parts repeat one rust pattern"""
    nt = m.node_tree; tex = [n for n in nt.nodes if n.type == 'TEX_IMAGE']
    if not tex or m.get("varied"): return
    oi = nt.nodes.new("ShaderNodeObjectInfo"); mul = nt.nodes.new("ShaderNodeVectorMath"); mul.operation = 'SCALE'; mul.inputs[3].default_value = 0.37; nt.links.new(oi.outputs["Location"], mul.inputs[0]); tc = nt.nodes.new("ShaderNodeTexCoord")
    for t in tex:
        add = nt.nodes.new("ShaderNodeVectorMath"); add.operation = 'ADD'; v = t.inputs["Vector"]; nt.links.new(v.links[0].from_socket if v.is_linked else tc.outputs["UV"], add.inputs[0]); nt.links.new(mul.outputs[0], add.inputs[1]); nt.links.new(add.outputs[0], v)
    m["varied"] = 1
def repaint(objs, src):
    """AFTER robots: structural parts kept from the previous pass (cube, deployer) take the palette of this pass, so the robot is one colour scheme"""
    pal = src["materials"]
    for o in objs:
        for sl in o.material_slots:
            m = sl.material
            if not m or not m.use_nodes or m.get("repainted"): continue
            z = m.name.split(".")[0]; b = m.node_tree.nodes.get("Principled BSDF"); m["repainted"] = 1
            if b is None: continue
            if z == "R4P_Glow": b.inputs["Emission Color"].default_value = (*src["glow"], 1.0); b.inputs["Base Color"].default_value = (*[c*0.5 for c in src["glow"]], 1.0); continue
            if z not in pal: continue
            rgb = (*pal[z]["rgb"], 1.0); done = False
            for n in m.node_tree.nodes:
                if n.type == 'MIX' and n.data_type == 'RGBA' and n.blend_type == 'MULTIPLY':
                    for i_ in (6, 7):
                        if not n.inputs[i_].is_linked: n.inputs[i_].default_value = rgb; done = True
                elif n.type == 'MIX_RGB' and n.blend_type == 'MULTIPLY':
                    for i_ in (1, 2):
                        if not n.inputs[i_].is_linked: n.inputs[i_].default_value = rgb; done = True
            if not done and not b.inputs["Base Color"].is_linked: b.inputs["Base Color"].default_value = rgb
            for k_, v_ in (("Metallic", pal[z]["metallic"]), ("Roughness", pal[z]["roughness"])):
                if not b.inputs[k_].is_linked: b.inputs[k_].default_value = v_
def toonify(m, cfg):
    """Heroic Toon look-dev: base colour (texture x paint x vertex colour) x banded diffuse light + facing rim; emission kept"""
    if m.get("toon") or not m.use_nodes: return
    nt = m.node_tree; b = nt.nodes.get("Principled BSDF"); out = next((n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL'), None)
    if b is None or out is None: return
    bc = b.inputs["Base Color"]; rgb = nt.nodes.new("ShaderNodeRGB"); rgb.outputs[0].default_value = bc.default_value; src_ = bc.links[0].from_socket if bc.is_linked else rgb.outputs[0]
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse"); s2r = nt.nodes.new("ShaderNodeShaderToRGB"); nt.links.new(dif.outputs[0], s2r.inputs[0]); ramp = nt.nodes.new("ShaderNodeValToRGB"); ramp.color_ramp.interpolation = 'CONSTANT'
    el = ramp.color_ramp.elements; bands = cfg["bands"]; el[0].position = bands[0][0]; el[0].color = (bands[0][1],)*3 + (1,); el[1].position = bands[1][0]; el[1].color = (bands[1][1],)*3 + (1,)
    for (p_, v_) in bands[2:]: e_ = el.new(p_); e_.color = (v_,)*3 + (1,)
    nt.links.new(s2r.outputs[0], ramp.inputs[0]); mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = 'RGBA'; mul.blend_type = 'MULTIPLY'; mul.inputs[0].default_value = 1.0; nt.links.new(src_, mul.inputs[6]); nt.links.new(ramp.outputs[0], mul.inputs[7])
    lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs[0].default_value = 0.42; rr = nt.nodes.new("ShaderNodeValToRGB"); rr.color_ramp.interpolation = 'CONSTANT'; rr.color_ramp.elements[0].color = (0, 0, 0, 1); rr.color_ramp.elements[1].position = 0.78; rr.color_ramp.elements[1].color = (cfg["rim"],)*3 + (1,)
    nt.links.new(lw.outputs["Facing"], rr.inputs[0]); add = nt.nodes.new("ShaderNodeMix"); add.data_type = 'RGBA'; add.blend_type = 'ADD'; add.inputs[0].default_value = 1.0; nt.links.new(mul.outputs[2], add.inputs[6]); nt.links.new(rr.outputs[0], add.inputs[7])
    em = nt.nodes.new("ShaderNodeEmission"); nt.links.new(add.outputs[2], em.inputs[0]); k = b.inputs["Emission Strength"].default_value; last = em.outputs[0]
    ec = b.inputs["Emission Color"]
    if k > 0.01 and (ec.is_linked or max(ec.default_value[:3]) > 0.01):
        e2 = nt.nodes.new("ShaderNodeEmission"); e2.inputs[0].default_value = b.inputs["Emission Color"].default_value; e2.inputs[1].default_value = k; ash = nt.nodes.new("ShaderNodeAddShader"); nt.links.new(em.outputs[0], ash.inputs[0]); nt.links.new(e2.outputs[0], ash.inputs[1]); last = ash.outputs[0]
        if ec.is_linked: nt.links.new(ec.links[0].from_socket, e2.inputs[0])
    nt.links.new(last, out.inputs["Surface"]); m["toon"] = 1
def outline(o, cfg):
    """inverted hull: a shell pushed outwards, flipped, black, back faces culled"""
    if o.type != 'MESH' or o.modifiers.get("Outline"): return
    om = bpy.data.materials.get("ToonOutline")
    if om is None:
        om = bpy.data.materials.new("ToonOutline"); om.use_nodes = True; nt = om.node_tree; nt.nodes.remove(nt.nodes.get("Principled BSDF")); em = nt.nodes.new("ShaderNodeEmission"); em.inputs[0].default_value = (0.004, 0.005, 0.01, 1); nt.links.new(em.outputs[0], next(n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL').inputs["Surface"]); om.use_backface_culling = True
        for a_ in ("use_backface_culling_shadow",):
            try: setattr(om, a_, True)
            except Exception: pass
    if om.name not in [m_.name for m_ in o.data.materials if m_]: o.data.materials.append(om)
    md = o.modifiers.new("Outline", 'SOLIDIFY'); md.thickness = cfg["outline_m"]; md.offset = 1.0; md.use_flip_normals = True; md.use_rim = False; md.material_offset = 64
def mesh_from_source(ms, name, mats):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(v) for v in ms["verts"]], [], [tuple(f["v"]) for f in ms["faces"]])
    for z in ZONES:
        if any(f["zone"] == z for f in ms["faces"]): me.materials.append(mats[z])
    slot = {m.name[3:].split('.')[0]: i for i, m in enumerate(me.materials)}; uv = me.uv_layers.new(name="UVMap"); normals = []
    for poly, f in zip(me.polygons, ms["faces"]):
        poly.material_index = slot[f["zone"]]; poly.use_smooth = True
        for k, li in enumerate(poly.loop_indices): uv.data[li].uv = f["uv"][k]; normals.append(tuple(f["n"][k]))
    me.update(); me.normals_split_custom_set(normals); me.update(); return me
def simple_mat(name, rgb, rough, metal=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes.get("Principled BSDF"); b.inputs["Base Color"].default_value = (*rgb, 1); b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal; return m
def ground_mat():
    m = bpy.data.materials.new("Ground"); m.use_nodes = True; nt = m.node_tree; b = nt.nodes.get("Principled BSDF"); tc = nt.nodes.new("ShaderNodeTexCoord")
    br = nt.nodes.new("ShaderNodeTexBrick"); br.inputs["Scale"].default_value = 1.0; br.inputs["Mortar Size"].default_value = 0.02; br.inputs["Color1"].default_value = (0.085, 0.082, 0.076, 1); br.inputs["Color2"].default_value = (0.105, 0.10, 0.092, 1); br.inputs["Mortar"].default_value = (0.035, 0.034, 0.033, 1)
    try: br.offset = 0.0; br.inputs["Brick Width"].default_value = 2.5; br.inputs["Row Height"].default_value = 2.5
    except Exception: pass
    no = nt.nodes.new("ShaderNodeTexNoise"); no.inputs["Scale"].default_value = 0.35; no.inputs["Detail"].default_value = 6.0; mx = nt.nodes.new("ShaderNodeMix"); mx.data_type = 'RGBA'; mx.blend_type = 'MULTIPLY'; mx.inputs[0].default_value = 1.0
    ramp = nt.nodes.new("ShaderNodeValToRGB"); ramp.color_ramp.elements[0].position = 0.3; ramp.color_ramp.elements[0].color = (0.62, 0.62, 0.62, 1); ramp.color_ramp.elements[1].position = 0.7; ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    nt.links.new(tc.outputs["Object"], br.inputs["Vector"]); nt.links.new(tc.outputs["Object"], no.inputs["Vector"]); nt.links.new(no.outputs[0], ramp.inputs[0]); nt.links.new(br.outputs["Color"], mx.inputs[6]); nt.links.new(ramp.outputs["Color"], mx.inputs[7])
    nt.links.new(mx.outputs[2], b.inputs["Base Color"]); b.inputs["Roughness"].default_value = 0.88; return m
def box(col, name, c, s, yaw, mat):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=c); o = bpy.context.active_object; o.name = name; o.scale = s; o.rotation_euler = (0, 0, math.radians(yaw)); o.data.materials.append(mat)
    for cc in list(o.users_collection): cc.objects.unlink(o)
    col.objects.link(o); return o
def tri_count(o): return sum(len(p.vertices) - 2 for p in o.data.polygons) if o.type == 'MESH' else 0
def style_scene(s, shots):
    t0 = time.time(); bpy.ops.wm.read_factory_settings(use_empty=True); scn = bpy.context.scene; scn.render.fps = 24; scn.frame_set(1); src = json.load(open(os.path.join(SRC, LABEL[s] + ".json"))); stats = {"style": s, "applied": [], "shots": {}, "crops": {}}
    kd = json.load(open(os.path.join(KSRC, LABEL[s] + ".json")))
    lib = bpy.data.collections.new("LIB"); scn.collection.children.link(lib); lib.hide_render = True; models = {}
    def model(mode, typ):
        k = (mode, typ)
        if k in models: return models[k]
        cid = CID[typ]; p = os.path.join(AFTER, LABEL[s], cid, cid + ".glb")
        if mode == "before" or not os.path.exists(p): p = os.path.join(PASS1, LABEL[s], cid, cid + ".glb")
        have = set(bpy.data.objects); bpy.ops.import_scene.gltf(filepath=p); new = [o for o in bpy.data.objects if o not in have]; kids = {}
        for o in new:
            for cc in list(o.users_collection): cc.objects.unlink(o)
            lib.objects.link(o)
            if o.parent in new: kids.setdefault(o.parent.name, []).append(o)
        baked = mode == "after" and os.path.exists(os.path.join(AFTER, LABEL[s], cid, cid + ".glb"))      # hero pass 2 model: authored texture set - nothing to offset or repaint
        if mode == "after" and not baked: repaint(new, src)
        if s == "D" and mode == "after" and not baked:
            for o in new:
                for sl in o.material_slots:
                    if sl.material: vary(sl.material)
        if mode == "after" and s in shots.get("toon", {}):
            for o in new:
                for sl in o.material_slots:
                    if sl.material and sl.material.name != "ToonOutline": toonify(sl.material, shots["toon"][s])
                outline(o, shots["toon"][s])
        roots = [o for o in new if o.parent not in new]; models[k] = (roots, kids, sum(tri_count(o) for o in new), "/".join(p.replace(os.sep, "/").split("/")[-5:])); return models[k]
    def instance(col, mode, typ, M):
        roots, kids, tris, _ = model(mode, typ); n = 0
        def rec(o, parent):
            nonlocal n
            c = o.copy(); col.objects.link(c); n += 1
            if parent is None: c.matrix_basis = M @ o.matrix_basis
            else: c.parent = parent
            for ch in kids.get(o.name, []): rec(ch, c)
        for r in roots: rec(r, None)
        return tris, n
    mats = {z: make_material(z, src) for z in ZONES if z == "R4P_Glow" or z in src["materials"]}; mats1 = {z: make_material(z, src["legacy"], "L1_") for z in ZONES if z == "R4P_Glow" or z in src["legacy"]["materials"]}; hull = {}; sets = {}
    if s == "D":
        for m_ in mats.values(): vary(m_)
    if s in shots.get("toon", {}):
        for m_ in mats.values(): toonify(m_, shots["toon"][s])
    kit = {n: mesh_from_source(ms, n, mats) for n, ms in kd["meshes"].items()}; kit_b = {n: mesh_from_source(ms, n + "_p2", mats1) for n, ms in kd["meshes"].items()}; ktris = {n: ms["triangles"] for n, ms in kd["meshes"].items()}
    kcol = bpy.data.collections.new("KIT"); scn.collection.children.link(kcol); kobjs = []
    for i, (n, me) in enumerate(sorted(kit.items())):
        o = bpy.data.objects.new(n, me); kcol.objects.link(o); o.location = ((i % 8)*0.7, (i//8)*0.7, 0); o["hull_kit"] = n; o["style"] = s; kobjs.append(o)
        if n.startswith("plate_"): o["mask"] = int(n.split("_")[1]); o["mask_bits"] = "bit0 +u, bit1 -u, bit2 +v, bit3 -v: the plate continues on that side"
    bpy.context.view_layer.update(); kd_ = os.path.join(KOUT, LABEL[s]); os.makedirs(kd_, exist_ok=True); bpy.ops.object.select_all(action='DESELECT')
    for o in kobjs: o.select_set(True)
    bpy.context.view_layer.objects.active = kobjs[0]
    bpy.ops.export_scene.gltf(filepath=os.path.join(kd_, "R4H_HullKit.glb"), export_format='GLB', use_selection=True, export_yup=True, export_apply=True, export_extras=True, export_animations=False)
    bpy.ops.export_scene.fbx(filepath=os.path.join(kd_, "R4H_HullKit.fbx"), use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', object_types={'MESH'}, mesh_smooth_type='FACE', bake_anim=False, path_mode='STRIP')
    stats["hull_kit"] = {"meshes": len(kobjs), "triangles": sum(ktris.values()), "files": ["R4H_HullKit.glb", "R4H_HullKit.fbx"]}; kcol.hide_render = True
    for sh in shots["shots"]:
        col = bpy.data.collections.new(sh["name"]); scn.collection.children.link(col); tris = 0; nobj = 0; pieces = 0; hl = 0; nfrag = 0
        for st in sh["sets"]:
            rb = src["robots"][st["robot"]]; M0 = Matrix(st["M"]); sc = rb["scenarios"].get(st.get("scenario")) if st.get("scenario") else None; alive = set(sc["alive"]) if sc else None
            for pi, p in enumerate(rb["parts"]):
                if alive is not None and pi not in alive: continue
                a, b = instance(col, st["mode"], p["type"], M0 @ Matrix(p["M"])); tris += a; nobj += b; pieces += 1
            if not st.get("hull", True): continue
            for f in (sc["fragments"] if sc else rb["fragments"]):
                if f.get("hero") and st["mode"] != "after": continue            # silhouette fairings exist only in the hero pass
                o = bpy.data.objects.new("HL_" + f["mesh"], (kit if st["mode"] == "after" else kit_b)[f["mesh"]]); col.objects.link(o); o.matrix_world = M0 @ Matrix(f["M"]); tris += ktris[f["mesh"]]; hl += ktris[f["mesh"]]; nobj += 1; nfrag += 1
                if st["mode"] == "after" and s in shots.get("toon", {}): outline(o, shots["toon"][s])
        for i, b in enumerate(sh.get("stands", [])): box(col, "stand%d" % i, b[:3], b[3:6], 0, bpy.data.materials.get("Concrete") or simple_mat("Concrete", (0.20, 0.20, 0.195), 0.9))
        sets[sh["name"]] = col; stats["shots"][sh["name"]] = {"pieces": pieces, "triangles": tris, "hull_layer_triangles": hl, "hull_fragments": nfrag, "objects": nobj}
    env = bpy.data.collections.new("ENV"); scn.collection.children.link(env); conc = bpy.data.materials.get("Concrete") or simple_mat("Concrete", (0.15, 0.15, 0.145), 0.9); crate = simple_mat("Crate", (0.16, 0.12, 0.07), 0.8); barr = simple_mat("Barrier", (0.24, 0.20, 0.10), 0.8)
    bpy.ops.mesh.primitive_plane_add(size=260.0, location=(0, -10, 0)); g = bpy.context.active_object; g.name = "Ground"; g.data.materials.append(ground_mat())   # unscaled: object coordinates = metres
    for cc in list(g.users_collection): cc.objects.unlink(g)
    env.objects.link(g)
    for i, e in enumerate(shots["env"]): box(env, "env%d" % i, e[:3], e[3:6], e[6], {"concrete": conc, "crate": crate, "barrier": barr}[e[7]])
    sun = bpy.data.lights.new("Sun", 'SUN'); sun.energy = shots["sun"]["strength"]; sun.color = shots["sun"]["color"]; sun.angle = math.radians(shots["sun"]["angle_deg"]); so = bpy.data.objects.new("Sun", sun); scn.collection.objects.link(so)
    so.rotation_euler = Vector(shots["sun"]["dir"]).to_track_quat('Z', 'Y').to_euler()
    if shots.get("rim"):
        rl = bpy.data.lights.new("Rim", 'SUN'); rl.energy = shots["rim"]["strength"]; rl.color = shots["rim"]["color"]; rl.angle = math.radians(6); ro = bpy.data.objects.new("Rim", rl); scn.collection.objects.link(ro); ro.rotation_euler = Vector(shots["rim"]["dir"]).to_track_quat('Z', 'Y').to_euler()
    w = bpy.data.worlds.new("W"); scn.world = w; w.use_nodes = True; bg = w.node_tree.nodes.get("Background"); bg.inputs[0].default_value = (*shots["sky"]["color"], 1); bg.inputs[1].default_value = shots["sky"]["strength"]
    eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]; scn.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in eng else 'BLENDER_EEVEE_NEXT'
    for obj, attr, val in ((scn.eevee, "taa_render_samples", 48), (scn.eevee, "use_raytracing", True), (scn.eevee, "use_gtao", True), (scn.eevee, "gtao_distance", 0.6), (scn.eevee, "use_shadows", True), (scn.eevee, "shadow_ray_count", 2), (scn.eevee, "shadow_step_count", 6),
                           (scn.eevee, "fast_gi_distance", 1.2), (scn.eevee, "use_fast_gi", True), (scn.view_settings, "view_transform", "AgX"), (scn.view_settings, "look", "AgX - Medium High Contrast"), (scn.view_settings, "exposure", shots.get("exposure", 0.0))):
        try: setattr(obj, attr, val); stats["applied"].append(attr)
        except Exception as ex: stats["applied"].append("!" + attr)
    try: scn.eevee.ray_tracing_options.trace_max_roughness = 0.5
    except Exception: pass
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam); scn.camera = cam; cam.data.clip_end = 400; scn.render.image_settings.file_format = 'JPEG'; scn.render.image_settings.quality = 90
    od = os.path.join(OUT, LABEL[s]); os.makedirs(od, exist_ok=True)
    for sh in shots["shots"]:
        if ONLY and not any(sh["name"].startswith(x) for x in ONLY): continue
        for n, c in sets.items(): c.hide_render = n != sh["name"]
        cam.data.dof.use_dof = bool(sh["cam"].get("dof")); cam.data.dof.focus_distance = sh["cam"].get("dof") or 10.0; cam.data.dof.aperture_fstop = 4.0
        cam.data.lens = sh["cam"]["lens"]; cam.location = sh["cam"]["pos"]; cam.rotation_euler = (Vector(sh["cam"]["target"]) - Vector(sh["cam"]["pos"])).to_track_quat('-Z', 'Y').to_euler()
        if sh.get("crops"):
            bpy.context.view_layer.update(); W0, H0 = shots["resolutions"][0]; scn.render.resolution_x, scn.render.resolution_y = W0, H0; cr = []
            for st in sh["sets"]:
                bb = src["robots"][st["robot"]]["aabb"]; M0 = Matrix(st["M"]); P = [world_to_camera_view(scn, cam, M0 @ Vector((bb[i][0], bb[j][1], bb[k][2]))) for i in (0, 1) for j in (0, 1) for k in (0, 1)]
                cr.append({"tag": st.get("tag", st["robot"]), "robot": st["robot"], "px_%d" % H0: [round(min(p.x for p in P)*W0, 1), round((1 - max(p.y for p in P))*H0, 1), round(max(p.x for p in P)*W0, 1), round((1 - min(p.y for p in P))*H0, 1)],
                           "distance_m": round((M0.translation - cam.location).length, 1), "fully_inside_frame": all(0.0 <= p.x <= 1.0 and 0.0 <= p.y <= 1.0 for p in P)})
            stats["crops"][sh["name"]] = cr
        for (W, H) in sh.get("res", shots["resolutions"]):
            t1 = time.time(); scn.render.resolution_x, scn.render.resolution_y = W, H; scn.render.resolution_percentage = 100; scn.render.filepath = os.path.join(od, f"{sh['name']}_{H}.jpg"); bpy.ops.render.render(write_still=True)
            stats["shots"][sh["name"]]["render_s_%d" % H] = round(time.time() - t1, 1)
        log("SHOT", s, sh["name"], stats["shots"][sh["name"]])
    stats["sources"] = sorted({v[3].replace(os.sep, "/") for v in models.values()}); stats["seconds"] = round(time.time() - t0, 1); stats["blender"] = bpy.app.version_string; stats["engine"] = scn.render.engine
    json.dump(stats, open(os.path.join(od, "scene_stats.json"), "w"), indent=1); log("STYLE_DONE", s, stats["seconds"], "s", stats["applied"])
shots = json.load(open(SHOTS))
for s in STYLES:
    try: style_scene(s, shots)
    except Exception as ex:
        import traceback; traceback.print_exc(); log("ERROR", s, repr(ex)[:300])
log("ALL_DONE")
