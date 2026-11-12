# RoboRougelike #1239 - pilot 2 (five independently designed constructions per type): Blender build / export / source-side QA of one variant.
# Derived from abuild.py (same contract checks: envelope, contact patches, approved interface, source node bounds, animation transfer, export
# reimport). Differences: PBR texture SETS per style (base colour x paint, ORM, normal map), paint zone R4P_Wear, and the artistic-review renders:
# full materials front / side / top / 3-4, neutral clay front / side / 3-4 (transparent, silhouettes are cut from them), wireframe 3-4 and
# frames of the REAL animation (board_frames). Reads pilot2/mesh_source/<Style>/<Id>.json (pgen.py), writes pilot2/variants/<Style>/<Id>/.
# usage: blender -b --factory-startup --python pbuild.py -- OUT_DIR MESH_SOURCE_DIR R4_REF_DIR [Id,Id..|*] [ABCDE]
import sys, os, math, json, time, hashlib
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion
from mathutils.bvhtree import BVHTree
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import astyle as ST, aanim as AN, pstyle as PS

argv = sys.argv[sys.argv.index("--") + 1:]
OUT, SRC, REF = argv[0], argv[1], argv[2]; TEX = os.path.join(os.path.dirname(SRC), "textures")
ONLY = None if len(argv) < 4 or argv[3] == "*" else argv[3].split(",")
STYLES = list(argv[4]) if len(argv) > 4 else ST.STYLES
G, H, FPS = 0.55, 0.26, 24
LIB = {"blocks": "Tools/ModelFactory/R4Blocks", "weapons": "Tools/ModelFactory/R4Weapons938", "movement": "Tools/ModelFactory/R4Movement940", "wheels": "Tools/ModelFactory/R4Wheels", "connectors": "Tools/ModelFactory/R4ConnectorRods1182"}
DIRS = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 0, 1), "down": (0, 0, -1), "forward": (0, -1, 0), "back": (0, 1, 0)}
VIEWS = (("front", (0.0, -1.0, 0.10)), ("side", (1.0, 0.0, 0.10)), ("top", (0.0, -0.02, 1.0)), ("34", (1.0, -1.25, 0.85)))
def log(*a): print("[PILOT2]", *a, flush=True)
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
def base_name(n): return n.rsplit(".", 1)[0] if n.rsplit(".", 1)[-1].isdigit() else n


def reset_scene(F=1):
    bpy.ops.wm.read_factory_settings(use_empty=True); scn = bpy.context.scene
    scn.unit_settings.system = 'METRIC'; scn.unit_settings.scale_length = 1.0; scn.render.fps = FPS; scn.frame_start = 1; scn.frame_end = max(1, F); scn.frame_set(1)
    return scn


def frame(n):
    n = Vector(n).normalized(); u = Vector((0, 0, 1)).cross(n) if abs(n.z) < 0.9 else Vector((1, 0, 0)).cross(n); u.normalize(); return u, n.cross(u), n


def _img(name, colorspace):
    img = bpy.data.images.get(os.path.basename(name)) or bpy.data.images.load(os.path.join(TEX, *name.split("/"))); img.colorspace_settings.name = colorspace; return img
def make_material(zone, src):
    """paint colour and PBR factors stay FACTORS of the zone; the style texture set modulates them (glTF: baseColorTexture x factor,
    metallicRoughnessTexture G / B x factors, normalTexture with scale)"""
    m = bpy.data.materials.new(zone); m.use_nodes = True; nt = m.node_tree; b = nt.nodes.get("Principled BSDF"); tx = src["textures"]; m["paint_zone"] = zone
    if zone == "R4P_Glow":
        gl = src["glow"]; k = src["glow_strength"]; rgb = [c*(0.55 if k > 0 else 1.0) for c in gl]; metal, rough = 0.0, 0.45
        b.inputs["Emission Color"].default_value = (*gl, 1.0); b.inputs["Emission Strength"].default_value = k; m["emissive_default"] = gl; m["emissive_strength_default"] = k
        m.diffuse_color = (*rgb, 1.0); b.inputs["Base Color"].default_value = (*rgb, 1.0); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough; return m
    if tx.get("baked"):                                            # hero pass 2: one authored texture set per model (unique UV, baked from its high-poly source)
        d = src["materials"][zone]; m.diffuse_color = (*d["rgb"], 1.0); m.metallic = d["metallic"]; m.roughness = d["roughness"]; m["baked"] = True
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["base"], "sRGB"); nt.links.new(t.outputs["Color"], b.inputs["Base Color"]); m["base_texture"] = tx["base"]
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["orm"], "Non-Color"); sp = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(t.outputs["Color"], sp.inputs[0])
        nt.links.new(sp.outputs["Green"], b.inputs["Roughness"]); nt.links.new(sp.outputs["Blue"], b.inputs["Metallic"]); m["orm_texture"] = tx["orm"]
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["normal"], "Non-Color"); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = tx.get("normal_strength", 1.0)
        nt.links.new(t.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"]); m["normal_texture"] = tx["normal"]
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["emission"], "sRGB"); nt.links.new(t.outputs["Color"], b.inputs["Emission Color"]); b.inputs["Emission Strength"].default_value = src["glow_strength"]; m["emission_texture"] = tx["emission"]
        return m
    d = src["materials"][zone]; rgb, metal, rough = d["rgb"], d["metallic"], d["roughness"]
    m.diffuse_color = (*rgb, 1.0); m.metallic = metal; m.roughness = rough
    b.inputs["Base Color"].default_value = (*rgb, 1.0); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough
    if d.get("coat"):                                              # clear coat (glTF KHR_materials_clearcoat)
        try: b.inputs["Coat Weight"].default_value = d["coat"]; b.inputs["Coat Roughness"].default_value = 0.08; m["coat"] = d["coat"]
        except Exception: pass
    if tx.get("base"):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["base"], "sRGB"); mx = nt.nodes.new("ShaderNodeMix"); mx.data_type = 'RGBA'; mx.blend_type = 'MULTIPLY'; mx.inputs[0].default_value = 1.0
        nt.links.new(t.outputs["Color"], mx.inputs[6]); mx.inputs[7].default_value = (*rgb, 1.0); nt.links.new(mx.outputs[2], b.inputs["Base Color"]); m["base_texture"] = tx["base"]
    if tx.get("orm"):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["orm"], "Non-Color"); sp = nt.nodes.new("ShaderNodeSeparateColor"); nt.links.new(t.outputs["Color"], sp.inputs[0])
        for ch, sock, val in (("Green", "Roughness", rough), ("Blue", "Metallic", metal)):
            mu = nt.nodes.new("ShaderNodeMath"); mu.operation = 'MULTIPLY'; nt.links.new(sp.outputs[ch], mu.inputs[0]); mu.inputs[1].default_value = val; nt.links.new(mu.outputs[0], b.inputs[sock])
        m["orm_texture"] = tx["orm"]
    if tx.get("normal"):
        t = nt.nodes.new("ShaderNodeTexImage"); t.image = _img(tx["normal"], "Non-Color"); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = tx["normal_strength"]
        nt.links.new(t.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs["Normal"], b.inputs["Normal"]); m["normal_texture"] = tx["normal"]
    return m


BEVEL = None
def bevel_object(o, bv):
    """hero pass: real rounded / chamfered edges. Hard edges (dihedral > 38 deg) get a bevel of the style's width; the result is kept only if it is still
    a closed manifold, otherwise the node keeps its unbevelled mesh"""
    me0 = o.data; md = o.modifiers.new("Bevel", 'BEVEL'); md.width = bv["width"]; md.segments = bv["segments"]; md.limit_method = 'ANGLE'; md.angle_limit = math.radians(38); md.use_clamp_overlap = True; md.miter_outer = 'MITER_ARC' if bv["segments"] > 1 else 'MITER_SHARP'
    try: md.harden_normals = False
    except Exception: pass
    bpy.context.view_layer.update(); ev = o.evaluated_get(bpy.context.evaluated_depsgraph_get()); me = bpy.data.meshes.new_from_object(ev); o.modifiers.clear(); o.data = me
    bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=2e-6); bmesh.ops.dissolve_degenerate(bm, dist=2e-6, edges=bm.edges)     # clamped bevels leave slivers
    ok = len(bm.faces) > 0 and all(e.is_manifold and not e.is_boundary and (len(e.link_faces) != 2 or e.is_contiguous) for e in bm.edges) and all(f.calc_area() >= 1e-10 for f in bm.faces)
    if ok: bm.to_mesh(me)
    bm.free()
    if not ok: o.data = me0; bpy.data.meshes.remove(me); return False
    for p_ in me.polygons: p_.use_smooth = True
    me.set_sharp_from_angle(angle=math.radians(38)); me.update()
    me.name = me0.name; return True
def mesh_from_source(ms, name, mats, col):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(v) for v in ms["verts"]], [], [tuple(f["v"]) for f in ms["faces"]])
    for z in PS.ZONE_ORDER:
        if any(f["zone"] == z for f in ms["faces"]): me.materials.append(mats[z])
    slot = {m.name: i for i, m in enumerate(me.materials)}; uv = me.uv_layers.new(name="UVMap"); normals = []
    for poly, f in zip(me.polygons, ms["faces"]):
        poly.material_index = slot[f["zone"]]; poly.use_smooth = True
        for k, li in enumerate(poly.loop_indices): uv.data[li].uv = f["uv"][k]; normals.append(tuple(f["n"][k]))
    me.update()
    if BEVEL and not name.endswith("_LOD1"):
        me.set_sharp_from_angle(angle=math.radians(38)); o = bpy.data.objects.new(name, me); col.objects.link(o); o["beveled"] = bevel_object(o, BEVEL); return o
    me.normals_split_custom_set(normals); me.update()
    o = bpy.data.objects.new(name, me); col.objects.link(o); return o


def mesh_report(o):
    bm = bmesh.new(); bm.from_mesh(o.data)
    nm_ = sum(1 for e in bm.edges if not e.is_manifold); bd = sum(1 for e in bm.edges if e.is_boundary); za = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    inc = sum(1 for e in bm.edges if e.is_manifold and len(e.link_faces) == 2 and not e.is_contiguous); tris = sum(len(f.verts) - 2 for f in bm.faces); nv = len(bm.verts); bm.free()
    return {"object": o.name, "non_manifold_edges": nm_, "boundary_edges": bd, "zero_area_faces": za, "inconsistent_normals": inc, "triangles": tris, "vertices": nv, "uv": len(o.data.uv_layers) > 0,
            "material_slots": len(o.material_slots), "ok": nm_ == 0 and bd == 0 and za == 0 and inc == 0 and len(o.data.uv_layers) > 0}


def wverts(o): return [o.matrix_world @ v.co for v in o.data.vertices]
def bvh_of(objs):
    bm = bmesh.new()
    for o in objs:
        t = bmesh.new(); t.from_mesh(o.data); t.transform(o.matrix_world); me = bpy.data.meshes.new("tmp"); t.to_mesh(me); t.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    tree = BVHTree.FromBMesh(bm); bm.free(); return tree


def patch(tree, c, n, half_u, half_v, N=9):                    # approved patch_test ruler around an explicit centre
    n = Vector(n); u, v, _ = frame(n); c = Vector(c); flush = prot = miss = rec = 0
    for i in range(N):
        for j in range(N):
            p = c + u*(-half_u + 2*half_u*i/(N - 1)) + v*(-half_v + 2*half_v*j/(N - 1)) + n*0.2; hit = tree.ray_cast(p, -n, 0.5)
            if hit[0] is None: miss += 1; continue
            k = (hit[0] - c).dot(n)
            if k > 0.0005: prot += 1
            elif k > -0.0010: flush += 1
            else: rec += 1
    return {"flush_pct": round(100*flush/(N*N), 1), "recessed_pct": round(100*rec/(N*N), 1), "protruding": prot, "missing": miss}


def port_test(tree, p):
    if "center_bl" in p: r = patch(tree, p["center_bl"], p["normal_bl"], p["half_m"], p["half_m"]); need = 65
    else:
        n = Vector(DIRS[p["dir"]]); u, v, _ = frame(n); c = Vector(p["cell"])*G + n*H + (u*p["offset"][0] + v*p["offset"][1])*G
        r = patch(tree, c, n, p["half"][0]*0.85*G, p["half"][1]*0.85*G); need = 80 if p["kind"] == "full" else 65
    r.update(dir=p["dir"], kind=p["kind"], cell=p.get("cell"), need_pct=need); r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= need; return r


_HEMI = [(math.sin(t)*math.cos(p), math.sin(t)*math.sin(p), math.cos(t)) for (t, p) in [(0.0, 0.0)] + [(0.62, k*math.tau/5 + 0.3) for k in range(5)] + [(1.15, k*math.tau/7) for k in range(7)]]
def bake_vertex_shading(objs, occluders, recipe, skip_names):
    """polish pass: per-corner colour attribute 'Col' = baked ambient occlusion (rays against the whole part, rest pose) with a cavity tint, convex
    hard edges pulled towards the worn-edge colour. Exported as glTF COLOR_0 / FBX vertex colours (multiplies the base colour)."""
    tree = bvh_of(occluders); dist = recipe["dist"]; flat = recipe["flat"]; cav = recipe["cavity"]; edge = recipe["edge"]; ea = recipe["edge_amount"]; g = recipe["gamma"]; n_edge = 0; n_corner = 0
    for o in objs:
        me = o.data; ca = me.color_attributes.get("Col") or me.color_attributes.new("Col", 'FLOAT_COLOR', 'CORNER'); mw = o.matrix_world; R3 = mw.to_3x3().normalized(); glow = {i for i, m in enumerate(me.materials) if m and m.name.startswith("R4P_Glow")}
        if o.name in skip_names or abs(mw.to_3x3().determinant()) < 1e-9:
            for d in ca.data: d.color = (1, 1, 1, 1)
            continue
        fn = {}                                                    # vertex -> normals of its faces (hard-edge test)
        for p in me.polygons:
            for v in p.vertices: fn.setdefault(v, []).append(p.normal)
        sharp = {v for v, ns in fn.items() if min(a.dot(b) for a in ns for b in ns) < 0.82}
        cn = me.corner_normals; cache = {}
        for p in me.polygons:
            for li in p.loop_indices:
                n_corner += 1
                if p.material_index in glow: ca.data[li].color = (1, 1, 1, 1); continue
                v = me.loops[li].vertex_index; nl = cn[li].vector; key = (v, round(nl.x, 1), round(nl.y, 1), round(nl.z, 1)); c = cache.get(key)
                if c is None:
                    n = (R3 @ nl).normalized(); org = mw @ me.vertices[v].co + n*0.004; u = n.orthogonal().normalized(); w = n.cross(u); occ = 0.0
                    for (x, y, z) in _HEMI:
                        h = tree.ray_cast(org, u*x + w*y + n*z, dist)
                        if h[0] is not None: occ += (1.0 - h[3]/dist)*z
                    ao = max(0.0, 1.0 - occ/sum(z for (_, _, z) in _HEMI)*1.6)**g; c = [flat*(cav[i] + (1.0 - cav[i])*ao) for i in range(3)]
                    if v in sharp and ao > 0.78: c = [c[i] + (edge[i] - c[i])*ea for i in range(3)]; n_edge += 1
                    cache[key] = c
                ca.data[li].color = (c[0], c[1], c[2], 1.0)
        try: me.color_attributes.active_color = ca; me.color_attributes.render_color_index = list(me.color_attributes).index(ca)
        except Exception: pass
    return {"corners": n_corner, "worn_edge_samples": n_edge, "recipe": recipe}


def setup_render(scn):
    scn.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
    scn.render.resolution_x, scn.render.resolution_y = 720, 548; scn.render.image_settings.file_format = 'JPEG'; scn.render.image_settings.quality = 88
    w = bpy.data.worlds.new("W"); scn.world = w; w.use_nodes = True; bg = w.node_tree.nodes.get("Background"); bg.inputs[0].default_value = (0.20, 0.21, 0.23, 1); bg.inputs[1].default_value = 0.9
    scn.view_settings.view_transform = 'AgX' if 'AgX' in [i.identifier for i in scn.view_settings.bl_rna.properties['view_transform'].enum_items] else 'Filmic'
    for (nm, typ, en, loc, rot) in (("Key", 'SUN', 3.2, (2, -2, 3), (math.radians(50), 0, math.radians(35))), ("Fill", 'AREA', 220.0, (-3, -1.5, 1.5), (math.radians(70), 0, math.radians(-65))),
                                    ("Rim", 'AREA', 260.0, (0.5, 3.5, 2.2), (math.radians(-60), 0, math.radians(180)))):
        ld = bpy.data.lights.new(nm, typ); ld.energy = en
        if typ == 'AREA': ld.size = 2.5
        lo = bpy.data.objects.new(nm, ld); scn.collection.objects.link(lo); lo.location = loc; lo.rotation_euler = rot
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam); scn.camera = cam; cam.data.lens = 60; return cam, bg


def shoot(scn, cam, target, dist, d, path):
    cam.location = Vector(target) + Vector(d).normalized()*dist; cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scn.render.filepath = path; bpy.ops.render.render(write_still=True)


VCOL_EXPORT = {}
def export_fbx(path, anim):
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=False, object_types={'MESH', 'EMPTY'},
                             mesh_smooth_type='FACE', add_leaf_bones=False, use_mesh_modifiers=True, bake_anim=anim, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0, path_mode='STRIP')
def export_glb(path, anim, anim_objs=()):
    """animated: every object action is pushed to an NLA track with ONE shared name, so the file holds a single animation 'Timeline' (all nodes)"""
    kw = dict(filepath=path, export_format='GLB', use_selection=True, export_yup=True, export_apply=not anim, export_extras=True, export_animations=anim)
    if VCOL_EXPORT: kw.update(VCOL_EXPORT)
    if not anim: bpy.ops.export_scene.gltf(**kw); return "static"
    for o in anim_objs:
        ad = o.animation_data
        if ad is None or ad.action is None: continue
        act = ad.action; slot = getattr(ad, "action_slot", None); tr = ad.nla_tracks.new(); tr.name = "Timeline"; st = tr.strips.new("Timeline", 1, act)
        try:
            if slot is not None: st.action_slot = slot
        except Exception: pass
        ad.action = None
    try: bpy.ops.export_scene.gltf(export_animation_mode='NLA_TRACKS', export_frame_range=True, **kw); return "NLA_TRACKS"
    except TypeError: bpy.ops.export_scene.gltf(**kw); return "default"
def select(objs, active):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = active


# ------------------------------------------------------------------------------------------------ reference pass (approved GLB)
REFC = {}
def reference(src):
    cid = src["id"]
    if cid in REFC: return REFC[cid]
    lib, name = src["ref"]; path = os.path.join(REF, lib, LIB[lib].replace("/", os.sep), name, name + ".glb"); an = src["animation"]; F = an["frames"] if an["mode"] == "transfer" else 1
    scn = reset_scene(F); bpy.ops.import_scene.gltf(filepath=path); bpy.context.view_layer.update()
    objs = {base_name(o.name): o for o in bpy.data.objects}; off = 0.0
    for a in bpy.data.actions:                                     # the glTF importer activates only one animation: give every node its own action
        o = objs.get(base_name(a.name)) or objs.get(a.name)
        if o is None: continue
        ad = o.animation_data or o.animation_data_create(); ad.action = a
        try:
            if getattr(a, "slots", None) and len(a.slots): ad.action_slot = a.slots[0]
        except Exception: pass
        for tr_ in list(ad.nla_tracks): tr_.mute = True
    starts = [a.frame_range[0] for a in bpy.data.actions]
    if starts: off = min(starts) - 1.0
    scn.frame_set(1 + int(off)); bpy.context.view_layer.update()
    emp = {}; mesh = {}
    for n, o in objs.items():
        par = base_name(o.parent.name) if o.parent else None
        if o.type == 'EMPTY': emp[n] = dict(M=[list(r) for r in o.matrix_world], parent=par)
        elif o.type == 'MESH':
            V = wverts(o); mesh[n] = dict(M=[list(r) for r in o.matrix_world], parent=par, det=o.matrix_world.to_3x3().determinant(), tris=sum(len(p.vertices) - 2 for p in o.data.polygons),
                                         lo=[min(p[i] for p in V) for i in range(3)] if V else None, hi=[max(p[i] for p in V) for i in range(3)] if V else None)
    tracks = {}
    if an["mode"] == "transfer":
        names = [v for v in src["node_names"].values() if v in objs]
        for s in ST.STYLES:
            tr = {n: [] for n in names}
            for f in range(1, F + 1):
                t = AN.warp(s, an["clips"], f, 1.0) + off; scn.frame_set(int(math.floor(t)), subframe=t - math.floor(t)); bpy.context.view_layer.update()
                for n in names: tr[n].append(objs[n].matrix_world.copy())
            tracks[s] = tr
    REFC[cid] = dict(path=path.replace("\\", "/").split("/Tools/")[-1], sha256=sha(path), empties=emp, meshes=mesh, tracks=tracks, frame_offset=off, actions=len(bpy.data.actions))
    return REFC[cid]


def qdev(a, b):
    qa = a.to_quaternion(); qb = b.to_quaternion(); d = abs(qa.dot(qb)); return math.degrees(2*math.acos(min(1.0, d)))


def reimport_check(base, samples, body_bounds, emp_names, tris, n_mesh, has_anim, fx_names):
    res = {}
    for ext, op in ((".fbx", lambda p: bpy.ops.import_scene.fbx(filepath=p, anim_offset=0.0)), (".glb", lambda p: bpy.ops.import_scene.gltf(filepath=p))):
        scn = reset_scene(max(samples) if samples else 1); op(base + ext); scn.frame_set(1); bpy.context.view_layer.update()
        objs = {base_name(o.name): o for o in bpy.data.objects}; n_gltf_anim = None
        if ext == ".glb":
            try: n_gltf_anim = len(json.loads(open(base + ext, "rb").read()[20:20 + int.from_bytes(open(base + ext, "rb").read()[12:16], "little")]).get("animations", []))
            except Exception: pass
        ms = [o for o in bpy.data.objects if o.type == 'MESH']
        missing = [n for n in emp_names if n not in objs]; t = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in ms)
        sc = max((abs(s - 1.0) for n, o in objs.items() if o.type == 'MESH' and n not in fx_names for s in o.matrix_world.to_scale()), default=0.0)
        best = None; nodes_seen = set()
        for shift in (0, 1, -1, 2, -2):                                # exporter / importer time origin conventions: find the constant frame shift, then compare
            pos_dev = 0.0; rot_dev = 0.0; worst = None
            for f, mats in samples.items():
                scn.frame_set(f + shift); bpy.context.view_layer.update()
                for n, M in mats.items():
                    if n not in objs:
                        if shift == 0: missing.append(n)
                        continue
                    if n in fx_names and M.to_scale().length < 0.08: continue      # hidden FX mesh (zero scale): nothing to compare
                    nodes_seen.add(n); Wm = objs[n].matrix_world; dp = (Wm.translation - M.translation).length*1000
                    dr = 0.0 if (n in fx_names or abs(M.to_3x3().determinant()) < 1e-9 or abs(Wm.to_3x3().determinant()) < 1e-12) else qdev(Wm.to_3x3().normalized().to_4x4(), M.to_3x3().normalized().to_4x4())
                    if dp > pos_dev or dr > rot_dev: worst = [f, n, round(dp, 3), round(dr, 3)]
                    pos_dev = max(pos_dev, dp); rot_dev = max(rot_dev, dr)
            if best is None or pos_dev + rot_dev < best[0] + best[1]: best = (pos_dev, rot_dev, worst, shift)
            if pos_dev < 0.5 and rot_dev < 0.5: break
        pos_dev, rot_dev, worst, shift = best
        res[ext] = {"PASS": not missing and t == tris and len(ms) == n_mesh and sc < 1e-3 and pos_dev < 0.5 and rot_dev < 0.5 and (len(bpy.data.actions) >= 1 if has_anim else True),
                    "meshes": len(ms), "triangles": t, "world_scale_dev": round(sc, 6), "missing": sorted(set(missing)), "actions": len(bpy.data.actions), "key_frames_compared": sorted(samples),
                    "nodes_compared": len(nodes_seen), "max_pos_dev_mm": round(pos_dev, 4), "max_rot_dev_deg": round(rot_dev, 4), "worst": worst, "frame_shift_on_import": shift, "gltf_animations": n_gltf_anim}
    return res


def build(cid, style):
    t0 = time.time(); src = json.load(open(os.path.join(SRC, ST.LABEL[style], cid + ".json"))); an = src["animation"]; F = an["frames"]
    ref = reference(src) if src["ref"] else None
    od = os.path.join(OUT, ST.LABEL[style], cid); os.makedirs(os.path.join(od, "renders"), exist_ok=True)
    global BEVEL
    BEVEL = src.get("bevel"); scn = reset_scene(F); env = src["envelope"]; mats = {z: make_material(z, src) for z in PS.ZONE_ORDER if z == "R4P_Glow" or z in src["materials"]}; col = bpy.data.collections.new(cid); scn.collection.children.link(col)
    lcol = bpy.data.collections.new(cid + "_LOD1"); scn.collection.children.link(lcol)
    root = bpy.data.objects.new(cid + "_ROOT", None); col.objects.link(root); root.empty_display_size = 0.1
    objs = {cid + "_ROOT": root}; pending = []; meshes = {}; fxn = {src["node_names"][n] for n in src["fx"]}
    for n, ms in src["meshes"].items():
        name = src["body"] if n == "Body" else src["node_names"][n]; o = mesh_from_source(ms, name, mats, col); meshes[n] = o; o.rotation_mode = 'QUATERNION'
        piv = (0, 0, 0) if n == "Body" else src["nodes"][n]["pivot"]; par = None if n == "Body" else src["nodes"][n].get("parent")
        par = src["node_names"].get(par, par) if par else None
        pending.append((name, o, par, Matrix.Translation(piv)))
    empties = []
    if ref:
        for n, e in ref["empties"].items():
            if n == cid + "_ROOT" or e["parent"] is None and n.endswith("_ROOT"): continue
            o = bpy.data.objects.new(n, None); col.objects.link(o); o.empty_display_type = 'SINGLE_ARROW'; o.empty_display_size = 0.06; empties.append(o)
            pending.append((n, o, e["parent"] if e["parent"] != cid + "_ROOT" else None, Matrix(e["M"])))
    else:
        for p in src["ports"]:
            n = Vector(DIRS[p["dir"]]); u, v, _ = frame(n); c = n*H + (u*p["offset"][0] + v*p["offset"][1])*G
            o = bpy.data.objects.new(f"Port_{p['dir']}_000" + ("" if p["kind"] == "full" else "_part"), None); col.objects.link(o); o.empty_display_type = 'SINGLE_ARROW'; o.empty_display_size = 0.06
            o["kind"] = p["kind"]; empties.append(o); pending.append((o.name, o, None, Matrix.Translation(c) @ n.to_track_quat('Z', 'Y').to_matrix().to_4x4()))
        if src["mount"]:
            n = Vector(DIRS[src["mount"]]); o = bpy.data.objects.new("Socket_Mount", None); col.objects.link(o); o.empty_display_type = 'SINGLE_ARROW'; o.empty_display_size = 0.08; empties.append(o)
            pending.append(("Socket_Mount", o, None, Matrix.Translation(n*H) @ n.to_track_quat('Z', 'Y').to_matrix().to_4x4()))
        for e_ in src["empties"]:
            o = bpy.data.objects.new(e_["name"], None); col.objects.link(o); o.empty_display_type = 'SINGLE_ARROW'; o.empty_display_size = 0.05; empties.append(o)
            pending.append((e_["name"], o, None, Matrix.Translation(e_["pos"]) @ Vector(e_["dir"]).to_track_quat('Z', 'Y').to_matrix().to_4x4()))
    order = []; guard = 0
    while pending and guard < 50:
        guard += 1; rest_ = []
        for (name, o, par, M) in pending:
            p = root if par is None else objs.get(par)
            if p is None: rest_.append((name, o, par, M)); continue
            bpy.context.view_layer.update(); o.parent = p; o.matrix_parent_inverse = p.matrix_world.inverted(); o.matrix_world = M; objs[name] = o; order.append(name)
        pending = rest_
    unplaced = [x[0] for x in pending]
    lod = mesh_from_source(src["lod1"], cid + "_LOD1", mats, lcol); lod.parent = root
    for k, v in {"canonical_id": cid, "style": style, "style_label": ST.LABEL[style], "style_name": ST.NAME[style], "family": src["family"], "grid_m": G, "unity_up": "+Y", "unity_forward": "+Z",
                 "paint_zones": json.dumps(PS.ZONE_ORDER), "status": "pilot 2 #1239 (independent constructions) - experimental, not integrated", "pilot_type": src["type"]}.items(): root[k] = v
    bpy.context.view_layer.update()
    rest = {n: objs[n].matrix_world.copy() for n in order}
    # ---- animation
    anim_objs = []
    if an["mode"] == "procedural":
        for n, tr in an["tracks"].items():
            o = meshes.get(n)
            if o is None or not (any(abs(c) > 1e-9 for v in tr["loc"] for c in v) or any(abs(v[0] - 1.0) > 1e-9 for v in tr["quat"]) or any(abs(v - 1.0) > 1e-9 for v in tr["scl"])): continue
            base = o.location.copy(); anim_objs.append(o); amax = None
            if n == "Body":                                         # the cosmetic body shake may sweep at most 5 mm at the farthest corner of the envelope
                reach = max(Vector((env[i][0], env[j][1], env[k][2])).length for i in (0, 1) for j in (0, 1) for k in (0, 1)); amax = math.asin(min(1.0, 0.005/reach))
            for f in range(1, F + 1):
                q = Quaternion(tr["quat"][f - 1])
                if amax is not None and q.angle > amax: q = Quaternion(q.axis, amax)
                o.location = base + Vector(tr["loc"][f - 1]); o.rotation_quaternion = q; s = tr["scl"][f - 1]; o.scale = (s, s, s)
                o.keyframe_insert("location", frame=f); o.keyframe_insert("rotation_quaternion", frame=f); o.keyframe_insert("scale", frame=f)
            o.location = base; o.rotation_quaternion = Quaternion((1, 0, 0, 0)); o.scale = (1, 1, 1)
    else:
        tr = ref["tracks"][style]; names = [n for n in order if n in tr]; anim_objs = [objs[n] for n in names]; inv = {}; fxmax = {}
        for n in names:
            R0 = Matrix(ref["meshes"][n]["M"]) if n in ref["meshes"] else Matrix(ref["empties"][n]["M"])
            if abs(R0.to_3x3().determinant()) < 1e-9 or n in fxn: inv[n] = None; fxmax[n] = max((M.to_scale().length for M in tr[n]), default=1.0) or 1.0
            else: inv[n] = R0.inverted()
        for f in range(1, F + 1):
            scn.frame_set(f)
            for n in names:
                o = objs[n]; M = tr[n][f - 1]
                if inv[n] is None: s = max(1e-4, M.to_scale().length/fxmax[n]); o.matrix_world = Matrix.Translation(M.translation) @ Matrix.Diagonal((s, s, s, 1.0))
                else: o.matrix_world = (M @ inv[n]) @ rest[n]
                bpy.context.view_layer.update()
                o.keyframe_insert("location", frame=f); o.keyframe_insert("rotation_quaternion", frame=f); o.keyframe_insert("scale", frame=f)
    for n, tr in (an.get("extra_tracks") or {}).items():           # pass 2: mechanism nodes of rig assets (procedural, in the rest axes of their parent)
        o = meshes.get(n)
        if o is None: continue
        base = o.location.copy(); anim_objs.append(o)
        for f in range(1, F + 1):
            o.location = base + Vector(tr["loc"][f - 1]); o.rotation_quaternion = Quaternion(tr["quat"][f - 1]); s_ = tr["scl"][f - 1]; o.scale = (s_, s_, s_)
            o.keyframe_insert("location", frame=f); o.keyframe_insert("rotation_quaternion", frame=f); o.keyframe_insert("scale", frame=f)
        o.location = base; o.rotation_quaternion = Quaternion((1, 0, 0, 0)); o.scale = (1, 1, 1)
    has_anim = bool(anim_objs)
    for c in an["clips"]: scn.timeline_markers.new(c["name"], frame=c["start"])
    for k, f in an["events"].items(): scn.timeline_markers.new("EV_" + k, frame=f)
    scn.frame_set(1); bpy.context.view_layer.update()
    # ---- QA
    mobjs = list(meshes.values()); reports = [mesh_report(o) for o in mobjs]; lr = mesh_report(lod); tris = sum(r["triangles"] for r in reports)
    solid = [o for n, o in meshes.items() if n not in src["fx"]]; V = [p for o in solid for p in wverts(o)]
    lo = [min(p[i] for p in V) for i in range(3)]; hi = [max(p[i] for p in V) for i in range(3)]
    inside = all(lo[i] >= env[0][i] - 0.0006 and hi[i] <= env[1][i] + 0.0006 for i in range(3))
    LV = wverts(lod); lod_inside = all(min(p[i] for p in LV) >= env[0][i] - 0.0006 and max(p[i] for p in LV) <= env[1][i] + 0.0006 for i in range(3))
    node_vs_src = None
    if ref and an["mode"] == "transfer":
        worst = 0.0; rows = {}
        for n, o in meshes.items():
            nm = o.name
            if nm in ref["meshes"] and ref["meshes"][nm]["lo"] and nm not in fxn and abs(ref["meshes"][nm]["det"]) > 1e-9:
                Vn = wverts(o); d = max(max(ref["meshes"][nm]["lo"][i] - min(p[i] for p in Vn), max(p[i] for p in Vn) - ref["meshes"][nm]["hi"][i]) for i in range(3)); rows[nm] = round(max(d, 0)*1000, 2); worst = max(worst, d)
        node_vs_src = {"max_outside_source_node_bounds_mm": round(max(worst, 0)*1000, 2), "per_node_mm": rows, "PASS": worst <= 0.003}
    # animation: sweep, loop seams, key-frame samples for the reimport comparison
    key = sorted({f for c in an["clips"] for f in (c["start"], (c["start"] + c["end"])//2, c["end"])} | set(an["events"].values())) if has_anim else []
    samples = {}; sweep = 0.0; seams = []; motion = {}
    if has_anim:
        for f in range(1, F + 1):
            scn.frame_set(f); bpy.context.view_layer.update()
            if an["mode"] == "procedural":
                for o in solid:
                    for p in wverts(o): sweep = max(sweep, max(max(env[0][i] - p[i], p[i] - env[1][i]) for i in range(3)))
            if f in key: samples[f] = {o.name: o.matrix_world.copy() for o in mobjs}
            for o in anim_objs:
                M = o.matrix_world; R0 = rest[o.name]; dmot = (M.translation - R0.translation).length*1000 + abs(M.to_scale().length - R0.to_scale().length)*100
                if abs(R0.to_3x3().determinant()) > 1e-9 and abs(M.to_3x3().determinant()) > 1e-9: dmot += qdev(M.to_3x3().normalized().to_4x4(), R0.to_3x3().normalized().to_4x4())
                motion[o.name] = max(motion.get(o.name, 0.0), dmot)
        for c in an["clips"]:
            if c.get("loop"):
                scn.frame_set(c["start"]); bpy.context.view_layer.update(); A = {o.name: o.matrix_world.copy() for o in anim_objs}
                scn.frame_set(c["end"]); bpy.context.view_layer.update()
                d = max(((o.matrix_world.translation - A[o.name].translation).length*1000 + (0.0 if o.name in fxn else qdev(o.matrix_world.to_3x3().normalized().to_4x4(), A[o.name].to_3x3().normalized().to_4x4())) for o in anim_objs), default=0.0)
                seams.append({"clip": c["name"], "start_vs_end_dev": round(d, 4), "PASS": d < 0.15 or an["mode"] == "transfer",
                              "note": "source-defined loop: seamless by the symmetry of the approved rotor, not by equal matrices" if (an["mode"] == "transfer" and d >= 0.05) else None})
        scn.frame_set(1); bpy.context.view_layer.update()
    moving = sum(1 for v in motion.values() if v > 0.01)
    tree = bvh_of(solid); ltree = bvh_of([lod]); ptest = [port_test(tree, p) for p in src["ports"]]; lptest = [port_test(ltree, p) for p in src["ports"]]
    iface = None
    if ref:
        dev = 0.0; adev = 0.0; miss = []
        for n, e in ref["empties"].items():
            if n.endswith("_ROOT"): continue
            if n not in objs: miss.append(n); continue
            R0 = Matrix(e["M"]); dev = max(dev, (objs[n].matrix_world.translation - R0.translation).length*1000); adev = max(adev, qdev(objs[n].matrix_world.to_3x3().normalized().to_4x4(), R0.to_3x3().normalized().to_4x4()))
        iface = {"source_glb": ref["path"], "source_sha256": ref["sha256"], "empties": sorted(n for n in ref["empties"] if not n.endswith("_ROOT")), "missing": miss, "pos_dev_mm": round(dev, 4), "rot_dev_deg": round(adev, 4),
                 "mesh_nodes_source": sorted(ref["meshes"]), "mesh_nodes_missing": [n for n in ref["meshes"] if n not in objs], "PASS": not miss and dev < 0.01 and adev < 0.1 and not [n for n in ref["meshes"] if n not in objs]}
    mat_names = sorted({s_.material.name for o in mobjs for s_ in o.material_slots if s_.material}); vshade = None
    bev_ok = sorted(o.name for o in mobjs if o.get("beveled")); bev_rej = sorted(o.name for o in mobjs if src.get("bevel") and not o.get("beveled"))
    global VCOL_EXPORT
    VCOL_EXPORT = {}
    if src.get("vcol"):
        vshade = bake_vertex_shading(mobjs, solid, src["vcol"], fxn); vshade["lod1"] = bake_vertex_shading([lod], [lod], src["vcol"], set())["corners"]
        props = {p.identifier for p in bpy.ops.export_scene.gltf.get_rna_type().properties}
        if "export_vertex_color" in props: VCOL_EXPORT = {"export_vertex_color": 'ACTIVE'}
        if "export_active_vertex_color_when_no_material" in props: VCOL_EXPORT["export_active_vertex_color_when_no_material"] = True
        vshade["gltf_export_options"] = dict(VCOL_EXPORT)
    base = os.path.join(od, cid)
    select([root] + mobjs + empties, root); export_fbx(base + ".fbx", has_anim); glb_mode = export_glb(base + ".glb", has_anim, anim_objs)
    small = [im for im in bpy.data.images if src["textures"].get("baked") and im.source == 'FILE' and im.size[0] > 512]
    for im in small: im.scale(512, 512)                              # LOD1 of an authored model carries its own 512 px copy of the texture set
    select([root, lod], root); export_fbx(base + "_LOD1.fbx", False); export_glb(base + "_LOD1.glb", False)
    for im in small: im.reload()
    failures = []
    if unplaced: failures.append("hierarchy: unplaced " + ",".join(unplaced))
    if not (all(r["ok"] for r in reports) and lr["ok"]): failures.append("topology")
    if not inside or not lod_inside: failures.append("geometry outside the envelope")
    allow = 0.030 if src["world_object"] else 0.010                    # mounted parts: a third of the 30 mm grid gap; world traps have no neighbours
    if an["mode"] == "procedural" and sweep > allow: failures.append("animated sweep leaves the envelope by more than %d mm" % round(allow*1000))
    if node_vs_src and not node_vs_src["PASS"]: failures.append("node exceeds the bounds of its approved source node")
    if not all(p["PASS"] for p in ptest): failures.append("contact patch")
    if not all(p["PASS"] for p in lptest): failures.append("LOD1 contact patch")
    if iface and not iface["PASS"]: failures.append("interface differs from the approved source")
    if not set(mat_names) <= set(PS.ZONE_ORDER): failures.append("paint zones")
    n_anim = len(anim_objs)
    if has_anim and moving < n_anim: failures.append("animated node without motion")
    if not all(s["PASS"] for s in seams): failures.append("loop seam")
    lod.hide_render = True
    for o in mobjs:
        if o.name in fxn: o.hide_render = True                    # FX meshes are zero-scale at rest
    cam, bg = setup_render(scn); ctr = Vector([(env[0][i] + env[1][i])/2 for i in range(3)]); ext = max(env[1][i] - env[0][i] for i in range(3)); renders = []
    for tag, dvec in VIEWS:
        p_ = os.path.join(od, "renders", f"{cid}_{tag}.jpg"); shoot(scn, cam, ctr, ext*2.2 + 0.65, dvec, p_); renders.append(os.path.basename(p_))
    for i_, bf in enumerate(src["board_frames"]):               # frames of the real animation (FX meshes visible: they scale up from zero in their clips)
        scn.frame_set(bf["frame"]); bpy.context.view_layer.update()
        for o in mobjs: o.hide_render = False
        p_ = os.path.join(od, "renders", f"{cid}_anim{i_ + 1}.jpg"); shoot(scn, cam, ctr, ext*2.2 + 0.65, VIEWS[3][1], p_); renders.append(os.path.basename(p_))
    scn.frame_set(1); bpy.context.view_layer.update()
    for o in mobjs: o.hide_render = o.name in fxn
    lod.hide_render = False
    for im in bpy.data.images:                                      # the .blend references the shared texture set relative to the package (not packed)
        tn = {os.path.basename(v): v for v in src["textures"].values() if isinstance(v, str) and v.lower().endswith((".png", ".jpg"))}
        if im.name in tn: im.filepath = "//../../../textures/" + tn[im.name]
    bpy.ops.wm.save_as_mainfile(filepath=base + ".blend", compress=True)
    lod.hide_render = True; clay = bpy.data.materials.new("Clay"); clay.use_nodes = True; cb = clay.node_tree.nodes.get("Principled BSDF"); cb.inputs["Base Color"].default_value = (0.5, 0.5, 0.5, 1); cb.inputs["Roughness"].default_value = 0.7
    bpy.context.view_layer.material_override = clay; scn.render.film_transparent = True; scn.render.image_settings.file_format = 'PNG'; scn.render.image_settings.color_mode = 'RGBA'
    for tag, dvec in (VIEWS[0], VIEWS[1], VIEWS[3]):
        p_ = os.path.join(od, "renders", f"{cid}_clay_{tag}.png"); shoot(scn, cam, ctr, ext*2.2 + 0.65, dvec, p_); renders.append(os.path.basename(p_))
    scn.render.film_transparent = False; scn.render.image_settings.file_format = 'JPEG'; scn.render.image_settings.color_mode = 'RGB'; bpy.context.view_layer.material_override = None
    lod.hide_render = True; wm = bpy.data.materials.new("Wire"); wm.use_nodes = True; wb = wm.node_tree.nodes.get("Principled BSDF"); wb.inputs["Base Color"].default_value = (0.01, 0.01, 0.012, 1); wb.inputs["Roughness"].default_value = 1.0
    for o in mobjs: md = o.modifiers.new("W", 'WIREFRAME'); md.thickness = 0.0016; md.use_replace = True
    bpy.context.view_layer.material_override = wm; bg.inputs[0].default_value = (0.75, 0.76, 0.78, 1)
    p_ = os.path.join(od, "renders", f"{cid}_wire.jpg"); shoot(scn, cam, ctr, ext*2.2 + 0.65, VIEWS[3][1], p_); renders.append(os.path.basename(p_))
    emp_names = [e.name for e in empties] + [root.name]; hier = {o.name: (o.parent.name if o.parent else None) for o in [root] + mobjs + empties}; anim_names = [o.name for o in anim_objs]; n_mobjs = len(mobjs)
    re_ = reimport_check(base, samples, (lo, hi), emp_names, tris, n_mobjs, has_anim, fxn)
    if not all(v["PASS"] for v in re_.values()): failures.append("export reimport / animation tracks")
    files = {}
    for f in sorted(os.listdir(od)):
        fp = os.path.join(od, f)
        if os.path.isfile(fp) and not f.endswith(".json") and not f.endswith(".blend1"): files[f] = {"sha256": sha(fp), "bytes": os.path.getsize(fp)}
    stat = "BLENDER_PASS" if not failures else "HOLD"
    tex_used = {m_.name: {k: m_.get(k) for k in ("base_texture", "orm_texture", "normal_texture") if m_.get(k)} for m_ in bpy.data.materials if m_.name in mat_names}
    qa = {"id": cid, "pilot_type": src["type"], "construction": src["construction"], "style_character": src["character"], "artistic_acceptance": "PENDING_OWNER_REVIEW", "textures": src["textures"], "material_textures": tex_used, "board_frames": src["board_frames"], "style": style, "style_label": ST.LABEL[style], "family": src["family"], "status": stat, "export_reimport": "EXPORT_REIMPORT_PASS" if all(v["PASS"] for v in re_.values()) else "EXPORT_REIMPORT_FAIL",
          "failures": failures, "unity": "UNITY_NOT_RUN", "windows_player": "WINDOWS_PLAYER_NOT_RUN", "interface_status": src["interface"], "source": src["source"],
          "meshes": reports, "mesh_lod1": lr, "triangles": tris, "triangles_lod1": lr["triangles"], "mesh_objects": n_mobjs, "material_slots_total": sum(r["material_slots"] for r in reports), "materials": mat_names,
          "bounds_m": [[round(x, 4) for x in lo], [round(x, 4) for x in hi]], "envelope_m": env, "inside_envelope": inside, "lod1_inside_envelope": lod_inside, "node_bounds_vs_source": node_vs_src,
          "ports": src["ports"], "port_contact_test": ptest, "lod1_port_contact_test": lptest, "interface_vs_source": iface, "hierarchy": hier, "fx_objects": sorted(fxn),
          "animation": {"glb_animation_mode": glb_mode, "mode": an["mode"], "fps": FPS, "frames": F, "clips": an["clips"], "events": an["events"], "warped_clips": an["warped_clips"], "animated_objects": anim_names, "objects_with_real_motion": moving,
                        "sweep_outside_envelope_mm": round(max(sweep, 0)*1000, 3), "sweep_allowance_mm": round(allow*1000), "loop_seams": seams, "motion_character": ST.MOTION[style]}, "parts": src["stats"]["parts"], "vertex_shading": vshade, "polish_pass": src.get("polish_pass"), "mechanisms": src.get("mechanisms"), "bevel": src.get("bevel"), "beveled_nodes": bev_ok, "bevel_rejected_nodes": bev_rej,
          "reimport": re_, "renders": renders, "files": files, "seconds": round(time.time() - t0, 1), "blender": bpy.app.version_string}
    json.dump(qa, open(base + "_QA.json", "w", encoding="utf-8"), indent=1)
    log("VARIANT", ST.LABEL[style], cid, stat, failures, tris, "tris", "LOD1", lr["triangles"])
    return stat


summary = {}
ids = ONLY or sorted(f[:-5] for f in os.listdir(os.path.join(SRC, ST.LABEL["A"])) if f.endswith(".json"))
for cid in ids:
    for s in STYLES:
        try: summary[f"{ST.LABEL[s]}/{cid}"] = build(cid, s)
        except Exception as ex:
            import traceback; traceback.print_exc(); summary[f"{ST.LABEL[s]}/{cid}"] = "ERROR " + repr(ex)[:200]; log("ERROR", s, cid, repr(ex)[:300])
json.dump({"summary": summary, "blender": bpy.app.version_string}, open(os.path.join(OUT, "_build_summary.json"), "w"), indent=1)
log("BUILD_DONE", sum(1 for v in summary.values() if v == "BLENDER_PASS"), "/", len(summary))
