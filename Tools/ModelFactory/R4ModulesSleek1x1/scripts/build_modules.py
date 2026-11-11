# RoboRougelike - R4 Sleek module library 1x1x1: Blender build / export / source-side QA (step 2).
# Reads mesh_source/<ID>.json (gen_mesh_source.py) and writes for every module:
#   modules/<ID>/<ID>.blend / .fbx / .glb, <ID>_LOD1.fbx / .glb, renders (front, side, 3/4 front, 3/4 rear), <ID>_QA.json
# The QA ruler (mesh_report, patch_test, frame, export settings, render rig) is the approved one of the A_Sleek blocks (#1232 / #1235).
# usage: blender -b --factory-startup --python build_modules.py -- OUT_DIR MESH_SOURCE_DIR [ID,ID...]
import sys, os, math, json, time, hashlib
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:]
OUT, SRC = argv[0], argv[1]
ONLY = argv[2].split(",") if len(argv) > 2 else None
G, H = 0.55, 0.26
DIRS = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 0, 1), "down": (0, 0, -1), "forward": (0, -1, 0), "back": (0, 1, 0)}
UNITY_BIT = {"right": 0, "left": 1, "up": 2, "down": 3, "forward": 4, "back": 5}
IDS = ONLY or sorted(f[:-5] for f in os.listdir(SRC) if f.endswith(".json"))
R41 = {"Armor_Dark": ((0.035, 0.038, 0.045), 0.55, 0.45), "Armor_Gunmetal": ((0.16, 0.17, 0.18), 0.35, 0.55),
       "Armor_Plate_Light": ((0.27, 0.28, 0.285), 0.30, 0.58), "Steel_Light": ((0.55, 0.62, 0.68), 0.90, 0.25)}
ZONE_ORDER = ["R4P_Base", "R4P_Secondary", "R4P_Trim", "R4P_Metal", "R4P_Glow"]
VIEWS = (("front", (0.0, -1.0, 0.10)), ("side", (1.0, 0.0, 0.10)), ("34front", (1.0, -1.25, 0.85)), ("34rear", (-1.0, 1.25, 0.85)))
FPS, F0, F1 = 24, 1, 49


def log(*a): print("[R4X]", *a, flush=True)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    scn.unit_settings.system = 'METRIC'; scn.unit_settings.scale_length = 1.0; scn.render.fps = FPS; scn.frame_start = F0; scn.frame_end = F1; scn.frame_set(F0)
    return scn


def new_col(name):
    c = bpy.data.collections.new(name); bpy.context.scene.collection.children.link(c); return c


def frame(n):
    n = Vector(n).normalized()
    u = Vector((0, 0, 1)).cross(n) if abs(n.z) < 0.9 else Vector((1, 0, 0)).cross(n)
    u.normalize(); v = n.cross(u)
    return u, v, n


def make_zone_material(zone, src):
    m = bpy.data.materials.new(zone); m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    if zone == "R4P_Glow":
        gl = src["glow"]; rgb = [c*0.55 for c in gl]; metal, rough = 0.0, 0.40
        b.inputs["Emission Color"].default_value = (*gl, 1.0); b.inputs["Emission Strength"].default_value = 0.9; m["emissive_strength_default"] = 0.9
        m["emissive_default"] = gl; m["r41_default"] = "(emissive accent, colour per module)"
    else:
        rgb, metal, rough = R41[src["zone_default"][zone]]; m["r41_default"] = src["zone_default"][zone]
    m.diffuse_color = (*rgb, 1.0); m.metallic = metal; m.roughness = rough
    b.inputs["Base Color"].default_value = (*rgb, 1.0); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough
    m["paint_zone"] = zone
    return m


def mesh_from_source(src, name, mats, col):
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in src["verts"]], [], [tuple(f["v"]) for f in src["faces"]])
    for z in ZONE_ORDER:
        if any(f["zone"] == z for f in src["faces"]): me.materials.append(mats[z])
    slot = {m.name: i for i, m in enumerate(me.materials)}
    uv = me.uv_layers.new(name="UVMap")
    normals = []
    for poly, f in zip(me.polygons, src["faces"]):
        assert list(poly.vertices) == list(f["v"]), "polygon order changed"
        poly.material_index = slot[f["zone"]]; poly.use_smooth = True
        for k, li in enumerate(poly.loop_indices):
            uv.data[li].uv = f["uv"][k]; normals.append(tuple(f["n"][k]))
    me.update()
    me.normals_split_custom_set(normals)
    me.update()
    ob = bpy.data.objects.new(name, me); col.objects.link(ob)
    return ob


def mesh_report(o):
    bm = bmesh.new(); bm.from_mesh(o.data)
    nm_ = sum(1 for e in bm.edges if not e.is_manifold); bd = sum(1 for e in bm.edges if e.is_boundary)
    za = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    inc = sum(1 for e in bm.edges if e.is_manifold and len(e.link_faces) == 2 and not e.is_contiguous)
    tris = sum(len(f.verts) - 2 for f in bm.faces); nverts = len(bm.verts); bm.free()
    uv = len(o.data.uv_layers) > 0
    return {"object": o.name, "non_manifold_edges": nm_, "boundary_edges": bd, "zero_area_faces": za, "inconsistent_normals": inc, "triangles": tris,
            "vertices": nverts, "uv": uv, "ok": nm_ == 0 and bd == 0 and za == 0 and inc == 0 and uv}


def verts(o): return [o.matrix_world @ v.co for v in o.data.vertices]


def bvh_of(objs):
    bm = bmesh.new()
    for o in objs:
        t = bmesh.new(); t.from_mesh(o.data); t.transform(o.matrix_world); me = bpy.data.meshes.new("tmp"); t.to_mesh(me); t.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    tree = BVHTree.FromBMesh(bm); bm.free(); return tree


def patch_test(tree, cell, d, off, half, N=9):   # approved build_blocks.patch_test
    n = Vector(DIRS[d]); u, v, _ = frame(n)
    c = Vector(cell)*G + n*H; flush = prot = miss = rec = 0
    for i in range(N):
        for j in range(N):
            a = (off[0] - half[0]*0.85 + 2*half[0]*0.85*i/(N - 1))*G; b = (off[1] - half[1]*0.85 + 2*half[1]*0.85*j/(N - 1))*G
            p = c + u*a + v*b + n*0.2; hit = tree.ray_cast(p, -n, 0.5)
            if hit[0] is None: miss += 1; continue
            k = (hit[0] - c).dot(n)
            if k > 0.0005: prot += 1
            elif k > -0.0010: flush += 1
            else: rec += 1
    return {"flush_pct": round(100*flush/(N*N), 1), "recessed_pct": round(100*rec/(N*N), 1), "protruding": prot, "missing": miss}


def setup_render(scn):                           # approved render rig (same lights / camera / view transform as the block renders)
    scn.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE_NEXT'
    scn.render.resolution_x, scn.render.resolution_y = 1000, 760; scn.render.film_transparent = False
    scn.render.image_settings.file_format = 'JPEG'; scn.render.image_settings.quality = 90
    w = scn.world or bpy.data.worlds.new("W"); scn.world = w; w.use_nodes = True
    bg = w.node_tree.nodes.get("Background"); bg.inputs[0].default_value = (0.20, 0.21, 0.23, 1); bg.inputs[1].default_value = 0.9
    scn.view_settings.view_transform = 'AgX' if 'AgX' in [i.identifier for i in scn.view_settings.bl_rna.properties['view_transform'].enum_items] else 'Filmic'
    for (nm, typ, en, loc, rot) in (("Key", 'SUN', 3.2, (2, -2, 3), (math.radians(50), 0, math.radians(35))), ("Fill", 'AREA', 220.0, (-3, -1.5, 1.5), (math.radians(70), 0, math.radians(-65))),
                                    ("Rim", 'AREA', 260.0, (0.5, 3.5, 2.2), (math.radians(-60), 0, math.radians(180)))):
        ld = bpy.data.lights.new(nm, typ); ld.energy = en
        if typ == 'AREA': ld.size = 2.5
        lo = bpy.data.objects.new(nm, ld); scn.collection.objects.link(lo); lo.location = loc; lo.rotation_euler = rot
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); scn.collection.objects.link(cam); scn.camera = cam; cam.data.lens = 60
    return cam


def shoot(scn, cam, target, dist, d, path):
    cam.location = Vector(target) + Vector(d).normalized()*dist
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scn.render.filepath = path; bpy.ops.render.render(write_still=True)


def export_fbx(path, anim):
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=False, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE', add_leaf_bones=False, use_mesh_modifiers=True,
                             bake_anim=anim, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0)


def export_glb(path, anim):
    kw = dict(filepath=path, export_format='GLB', use_selection=True, export_yup=True, export_apply=not anim, export_extras=True, export_animations=anim)
    if anim:
        try: bpy.ops.export_scene.gltf(export_animation_mode='SCENE', export_frame_range=True, **kw); return "SCENE"
        except TypeError: pass
    bpy.ops.export_scene.gltf(**kw); return "default"


def select(objs, active):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = active


def base_name(n): return n.split(".")[0] if n.rsplit(".", 1)[-1].isdigit() else n


def sha(path): return hashlib.sha256(open(path, "rb").read()).hexdigest()


def animate(o, an):
    """preview clip 'Active' (frames 1..49 @ 24 fps, seamless loop; frame 1 = rest pose). PROPOSAL - visual only."""
    ax = Vector(an["axis"]).normalized(); typ = an["type"]
    pr = bpy.context.preferences.edit
    if typ == "slide":
        pr.keyframe_new_interpolation_type = 'BEZIER'; base = o.location.copy()
        for f, k in ((1, 0.0), (25, 1.0), (49, 0.0)): o.location = base + ax*an["amount"]*k; o.keyframe_insert("location", frame=f)
        o.location = base; return
    o.rotation_mode = 'AXIS_ANGLE'
    if typ == "spin":
        pr.keyframe_new_interpolation_type = 'LINEAR'; n = int(an.get("frames", 24)); turns = 48//n
        keys = [(1 + i*n/4.0, math.radians(90.0*i)) for i in range(4*turns + 1)]
    else:
        pr.keyframe_new_interpolation_type = 'BEZIER'; a = math.radians(an["amount"]); keys = [(1, 0.0), (13, a), (25, 0.0), (37, -a), (49, 0.0)]
    for f, ang in keys:
        o.rotation_axis_angle = (ang, ax.x, ax.y, ax.z); o.keyframe_insert("rotation_axis_angle", frame=f)
    o.rotation_axis_angle = (0.0, ax.x, ax.y, ax.z)


def reimport_check(base, body_bounds, emp_names, tris, n_mesh):
    res = {}
    for ext, op in ((".fbx", lambda p: bpy.ops.import_scene.fbx(filepath=p)), (".glb", lambda p: bpy.ops.import_scene.gltf(filepath=p))):
        reset_scene(); op(base + ext); bpy.context.scene.frame_set(F0); bpy.context.view_layer.update()
        ms = [o for o in bpy.data.objects if o.type == 'MESH']
        body = [o for o in ms if base_name(o.name).endswith("_Body")]
        if not body: res[ext] = {"PASS": False, "why": "no body mesh"}; continue
        v = verts(body[0]); lo = [min(p[i] for p in v) for i in range(3)]; hi = [max(p[i] for p in v) for i in range(3)]
        dmm = max(max(abs(lo[i] - body_bounds[0][i]), abs(hi[i] - body_bounds[1][i])) for i in range(3))*1000
        emp = sorted({base_name(o.name) for o in bpy.data.objects if o.type == 'EMPTY'}); missing = [n for n in emp_names if n not in emp]
        t = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in ms)
        sc = max(abs(s - 1.0) for o in ms for s in o.matrix_world.to_scale())
        acts = len(bpy.data.actions)
        res[ext] = {"PASS": dmm < 0.05 and not missing and t == tris and len(ms) == n_mesh and sc < 1e-4, "body_bounds_dev_mm": round(dmm, 4), "meshes": len(ms), "triangles": t,
                    "world_scale_dev": round(sc, 6), "empties_missing": missing, "actions": acts,
                    "materials": sorted({s.material.name.split(".")[0] for o in ms for s in o.material_slots if s.material})}
    return res


def build_module(ID):
    t0 = time.time(); src = json.load(open(os.path.join(SRC, ID + ".json")))
    od = os.path.join(OUT, ID); os.makedirs(os.path.join(od, "renders"), exist_ok=True)
    scn = reset_scene(); env = src["envelope"]
    mats = {z: make_zone_material(z, src) for z in ZONE_ORDER}
    col = new_col(ID); lcol = new_col(ID + "_LOD1")
    root = bpy.data.objects.new(ID + "_ROOT", None); col.objects.link(root); root.empty_display_size = 0.1
    objs = {}
    for n, ms in src["meshes"].items():
        o = mesh_from_source(ms, f"{ID}_{n}", mats, col); o.parent = root; objs[n] = o
        if n != "Body": o.location = src["nodes"][n]["pivot"]; o["role"] = "movable part (proposal)"; o["anim"] = json.dumps(src["nodes"][n]["anim"])
    lod = mesh_from_source(src["lod1"], ID + "_LOD1", mats, lcol); lod.parent = root
    empties = []
    for p in src["ports"]:
        n = Vector(DIRS[p["dir"]]); u, v, _ = frame(n)
        c = n*H + (u*p["offset"][0] + v*p["offset"][1])*G
        e = bpy.data.objects.new(f"Port_{p['dir']}_000" + ("" if p["kind"] == "full" else "_part"), None)
        col.objects.link(e); e.parent = root; e.location = c; e.empty_display_type = 'SINGLE_ARROW'; e.empty_display_size = 0.06
        e.rotation_euler = n.to_track_quat('Z', 'Y').to_euler(); e["kind"] = p["kind"]; e["patch_offset_cells"] = p["offset"]; e["patch_half_cells"] = p["half"]
        empties.append(e)
    if not src["world_object"]:
        n = Vector(DIRS[src["mount"]]); s = bpy.data.objects.new("Socket_Mount", None); col.objects.link(s); s.parent = root; s.empty_display_type = 'SINGLE_ARROW'; s.empty_display_size = 0.08
        s.location = n*H; s.rotation_euler = n.to_track_quat('Z', 'Y').to_euler(); s["mount_dir"] = src["mount"]; empties.append(s)
    for e_ in src["empties"]:
        e = bpy.data.objects.new(e_["name"], None); col.objects.link(e); e.parent = root; e.location = e_["pos"]; e.empty_display_type = 'SINGLE_ARROW'; e.empty_display_size = 0.05
        e.rotation_euler = Vector(e_["dir"]).to_track_quat('Z', 'Y').to_euler(); empties.append(e)
    cmask = 0
    for p in src["ports"]: cmask |= 1 << UNITY_BIT[p["dir"]]
    for k, v in {"module": src["name"], "catalog_number": src["number"], "catalog_group": src["group"], "name_pl": src["name_pl"], "function_pl": src["function_pl"], "grid_m": G, "block_m": 2*H,
                 "cells": "[[0,0,0]]", "contact_mask_local_proposal": cmask, "unity_up": "+Y", "unity_forward": "+Z", "style": "R4 A_Sleek module library 1x1x1",
                 "paint_zones": json.dumps(ZONE_ORDER), "status": "art pack - not integrated, interface = proposal"}.items(): root[k] = v
    anim_nodes = [n for n in src["nodes"] if src["nodes"][n].get("anim")]
    for n in anim_nodes: animate(objs[n], src["nodes"][n]["anim"])
    scn.frame_set(F0); bpy.context.view_layer.update()
    # ---- QA
    reports = [mesh_report(o) for o in objs.values()]; lr = mesh_report(lod); tris = sum(r["triangles"] for r in reports)
    V = [p for o in objs.values() for p in verts(o)]
    lo = [min(p[i] for p in V) for i in range(3)]; hi = [max(p[i] for p in V) for i in range(3)]
    inside = all(lo[i] >= env[0][i] - 0.0005 and hi[i] <= env[1][i] + 0.0005 for i in range(3))
    bv = verts(objs["Body"]); blo = [min(p[i] for p in bv) for i in range(3)]; bhi = [max(p[i] for p in bv) for i in range(3)]
    sweep = {"frames": 0, "max_outside_mm": 0.0}
    if anim_nodes:
        worst = 0.0; nf = 0
        for f in range(F0, F1 + 1, 2):
            scn.frame_set(f); bpy.context.view_layer.update(); nf += 1
            for n in anim_nodes:
                for p in verts(objs[n]): worst = max(worst, max(max(env[0][i] - p[i], p[i] - env[1][i]) for i in range(3)))
        scn.frame_set(F0); bpy.context.view_layer.update(); sweep = {"frames": nf, "max_outside_mm": round(max(worst, 0.0)*1000, 3)}
    tree = bvh_of(list(objs.values())); ltree = bvh_of([lod]); ptest = []; lptest = []
    for p in src["ports"]:
        for tr, acc in ((tree, ptest), (ltree, lptest)):
            r = patch_test(tr, p["cell"], p["dir"], p["offset"], p["half"]); r.update({"dir": p["dir"], "kind": p["kind"]})
            r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65); acc.append(r)
    mat_names = sorted({s_.material.name for o in objs.values() for s_ in o.material_slots if s_.material})
    base = os.path.join(od, ID); has_anim = bool(anim_nodes)
    select([root] + list(objs.values()) + empties, root); export_fbx(base + ".fbx", has_anim); glb_mode = export_glb(base + ".glb", has_anim)
    select([root, lod], root); export_fbx(base + "_LOD1.fbx", False); export_glb(base + "_LOD1.glb", False)
    failures = []
    if not (all(r["ok"] for r in reports) and lr["ok"]): failures.append("topology")
    if not inside: failures.append("geometry outside the 1x1x1 envelope")
    if sweep["max_outside_mm"] > 0.5: failures.append("animated part leaves the envelope")
    if not all(p["PASS"] for p in ptest): failures.append("contact patch")
    if not all(p["PASS"] for p in lptest): failures.append("LOD1 contact patch")
    if not set(mat_names) <= set(ZONE_ORDER): failures.append("paint zones")
    lod.hide_render = True
    cam = setup_render(scn); ctr = Vector([(env[0][i] + env[1][i])/2 for i in range(3)]); ext = max(env[1][i] - env[0][i] for i in range(3)); renders = []
    for tag, dvec in VIEWS:
        p_ = os.path.join(od, "renders", f"{ID}_{tag}.jpg"); shoot(scn, cam, ctr, ext*3.1 + 0.6, dvec, p_); renders.append(os.path.basename(p_))
    lod.hide_render = False
    bpy.ops.wm.save_as_mainfile(filepath=base + ".blend", compress=True)
    emp_names = [e.name for e in empties] + [root.name]
    re_ = reimport_check(base, (blo, bhi), emp_names, tris, len(objs))
    if not all(v["PASS"] for v in re_.values()): failures.append("export reimport")
    if has_anim and not all(v.get("actions", 0) >= 1 for v in re_.values()): failures.append("animation missing after reimport")
    files = {}
    for f in sorted(os.listdir(od)):
        fp = os.path.join(od, f)
        if os.path.isfile(fp) and not f.endswith(".json") and not f.endswith(".blend1"): files[f] = {"sha256": sha(fp), "bytes": os.path.getsize(fp)}
    qa = {"id": ID, "module": src["name"], "catalog_number": src["number"], "catalog_group": src["group"], "wave": src["wave"], "name_pl": src["name_pl"], "function_pl": src["function_pl"],
          "status": "BLENDER_PASS" if not failures else "HOLD", "failures": failures, "unity": "UNITY_NOT_RUN", "windows_player": "WINDOWS_PLAYER_NOT_RUN",
          "cells": [[0, 0, 0]], "ports_proposal": src["ports"], "mount_proposal": src["mount"], "contact_count": len(src["ports"]), "contact_mask_local_proposal": cmask,
          "port_contact_test": ptest, "lod1_port_contact_test": lptest, "meshes": reports, "mesh_lod1": lr, "triangles": tris, "triangles_lod1": lr["triangles"], "shells": src["stats"]["shells"],
          "bounds_m": [[round(x, 4) for x in lo], [round(x, 4) for x in hi]], "envelope_m": env, "inside_envelope": inside,
          "movable_nodes": {n: src["nodes"][n] for n in src["nodes"]}, "animation": {"clip": "Active", "fps": FPS, "frames": [F0, F1], "loop": True, "nodes": anim_nodes, "glb_mode": glb_mode} if has_anim else None,
          "animation_sweep": sweep, "function_empties": [e_["name"] for e_ in src["empties"]], "materials": mat_names, "glow_rgb": src["glow"], "reimport": re_, "renders": renders,
          "world_object": src["world_object"], "notes": src["notes"], "files": files, "seconds": round(time.time() - t0, 1), "blender": bpy.app.version_string}
    json.dump(qa, open(base + "_QA.json", "w", encoding="utf-8"), indent=1)
    log("MODULE", ID, qa["status"], failures, tris, "tris", "LOD1", lr["triangles"])
    return qa["status"], failures, tris


summary = {}
os.makedirs(OUT, exist_ok=True)
for ID in IDS:
    try: summary[ID] = build_module(ID)
    except Exception as ex:
        import traceback; traceback.print_exc(); summary[ID] = ("ERROR", [repr(ex)], 0); log("ERROR", ID, repr(ex))
json.dump({"summary": summary, "blender": bpy.app.version_string}, open(os.path.join(OUT, "_build_summary.json"), "w"), indent=1)
log("BUILD_DONE", json.dumps({k: v[0] for k, v in summary.items()}))
