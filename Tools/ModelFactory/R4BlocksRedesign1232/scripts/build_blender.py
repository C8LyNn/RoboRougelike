# RoboRougelike #1232 - R4 base blocks visual redesign pilot: Blender build / export / source-side QA.
# Step 2 of the pipeline. Reads the generated meshes (mesh_source/*.json, written by gen_mesh_source.py) and the APPROVED
# interface of PR #963 (R4B_*_QA.json ports + the approved GLB), then writes for every pilot block and art direction:
#   R4B_<Kind>.blend / .fbx / .glb, R4B_<Kind>_LOD1.fbx / .glb, renders, R4B_<Kind>_QA.json
# The interface code below (frame / port empties / patch_test / export settings / reimport check) is the approved
# R4Blocks/scripts/build_blocks.py logic, kept identical on purpose so the result is measured with the same ruler.
# usage: blender -b --factory-startup --python build_blender.py -- OUT_DIR R4_REF_DIR MESH_SOURCE_DIR [Kind,Kind...]
import sys, os, math, json, time, hashlib
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

argv = sys.argv[sys.argv.index("--") + 1:]
OUT, REF, SRC = argv[0], argv[1], argv[2]
ONLY = argv[3].split(",") if len(argv) > 3 else None
REF_BLOCKS = os.path.join(REF, "blocks", "Tools", "ModelFactory", "R4Blocks")

G, H = 0.55, 0.26
DIRS = {"right": (1, 0, 0), "left": (-1, 0, 0), "up": (0, 0, 1), "down": (0, 0, -1), "forward": (0, -1, 0), "back": (0, 1, 0)}   # Blender axes (approved)
UNITY_BIT = {"right": 0, "left": 1, "up": 2, "down": 3, "forward": 4, "back": 5}
PILOT = ["Cube", "Wedge", "Tetra", "CornerWedge", "HalfBlock"]
DIRECTIONS = {"A": "A_Sleek", "B": "B_Industrial"}
R41 = {"Armor_Dark": ((0.035, 0.038, 0.045), 0.55, 0.45), "Armor_Gunmetal": ((0.16, 0.17, 0.18), 0.35, 0.55),
       "Armor_Plate_Light": ((0.27, 0.28, 0.285), 0.30, 0.58)}
ZONE_ORDER = ["R4P_Base", "R4P_Secondary", "R4P_Trim"]
VIEWS = (("34front", (1.0, -1.25, 0.85)), ("34rear", (-1.0, 1.25, 0.85)), ("under", (0.8, -0.9, -0.9)))


def log(*a):
    print("[1232]", *a, flush=True)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    scn.unit_settings.system = 'METRIC'; scn.unit_settings.scale_length = 1.0
    return scn


def new_col(name):
    c = bpy.data.collections.new(name); bpy.context.scene.collection.children.link(c); return c


def frame(n):                                   # approved build_blocks.frame
    n = Vector(n).normalized()
    u = Vector((0, 0, 1)).cross(n) if abs(n.z) < 0.9 else Vector((1, 0, 0)).cross(n)
    u.normalize(); v = n.cross(u)
    return u, v, n


def make_zone_material(zone, r41_name):
    rgb, metal, rough = R41[r41_name]; m = bpy.data.materials.new(zone)
    m.use_nodes = True
    m.diffuse_color = (*rgb, 1.0); m.metallic = metal; m.roughness = rough
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*rgb, 1.0); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rough
    m["paint_zone"] = zone; m["r41_default"] = r41_name
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


def mesh_report(o):                              # approved build_blocks.mesh_report
    bm = bmesh.new(); bm.from_mesh(o.data)
    nm_ = sum(1 for e in bm.edges if not e.is_manifold); bd = sum(1 for e in bm.edges if e.is_boundary)
    za = sum(1 for f in bm.faces if f.calc_area() < 1e-10)
    inc = sum(1 for e in bm.edges if e.is_manifold and len(e.link_faces) == 2 and not e.is_contiguous)
    tris = sum(len(f.verts) - 2 for f in bm.faces); nverts = len(bm.verts); bm.free()
    uv = len(o.data.uv_layers) > 0
    return {"object": o.name, "non_manifold_edges": nm_, "boundary_edges": bd, "zero_area_faces": za, "inconsistent_normals": inc, "triangles": tris,
            "vertices": nverts, "uv": uv, "ok": nm_ == 0 and bd == 0 and za == 0 and inc == 0 and uv}


def verts(o): return [o.matrix_world @ v.co for v in o.data.vertices]


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


def contact_mask(ports):
    m = 0
    for p in ports:
        if p["cell"] == [0, 0, 0]: m |= 1 << UNITY_BIT[p["dir"]]
    return m


def setup_render(scn):                           # approved build_blocks.setup_render (same lights / camera / view transform)
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


def export_fbx(path):
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=False, object_types={'MESH', 'EMPTY'}, mesh_smooth_type='FACE', add_leaf_bones=False, use_mesh_modifiers=True)


def export_glb(path):
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True, export_yup=True, export_apply=True, export_extras=True)


def select(objs, active):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = active


def base_name(n): return n.split(".")[0] if n.rsplit(".", 1)[-1].isdigit() else n


def sha(path): return hashlib.sha256(open(path, "rb").read()).hexdigest()


def cell_bounds(kind):
    src = json.load(open(os.path.join(SRC, f"R4B_{kind}_A.json")))
    P = src["solid_points"]
    return [min(p[i] for p in P) for i in range(3)], [max(p[i] for p in P) for i in range(3)]


# ------------------------------------------------------------------------------------------------ pass 0: approved reference
def reference_pass(kind):
    """import the APPROVED PR #963 GLB: record its interface empties + bounds and render the 'before' views with the shared camera"""
    scn = reset_scene(); ID = "R4B_" + kind
    path = os.path.join(REF_BLOCKS, ID, ID + ".glb")
    bpy.ops.import_scene.gltf(filepath=path); bpy.context.view_layer.update()
    emp = {}
    for o in bpy.data.objects:
        if o.type == 'EMPTY':
            z = (o.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()
            emp[o.name] = {"loc": [round(x, 6) for x in o.matrix_world.translation], "z_axis": [round(x, 5) for x in z]}
    ms = [o for o in bpy.data.objects if o.type == 'MESH']
    V = [p for o in ms for p in verts(o)]
    lo = [min(p[i] for p in V) for i in range(3)]; hi = [max(p[i] for p in V) for i in range(3)]
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in ms)
    mats = sorted({s.material.name for o in ms for s in o.material_slots if s.material})
    clo, chi = cell_bounds(kind)
    cam = setup_render(scn); ctr = Vector([(clo[i] + chi[i])/2 for i in range(3)]); ext = max(chi[i] - clo[i] for i in range(3))
    od = os.path.join(OUT, "compare", "before"); os.makedirs(od, exist_ok=True); renders = []
    for tag, dvec in VIEWS:
        p_ = os.path.join(od, f"{ID}_before_{tag}.jpg"); shoot(scn, cam, ctr, ext*3.1 + 0.6, dvec, p_); renders.append(os.path.basename(p_))
    return {"glb": path.replace("\\", "/").split("/Tools/")[-1], "sha256": sha(path), "empties": emp, "bounds_m": [[round(x, 4) for x in lo], [round(x, 4) for x in hi]],
            "triangles": tris, "materials": mats, "renders": renders}


# ------------------------------------------------------------------------------------------------ pass 2: fresh-scene reimport
def reimport_check(base, ref_bounds, ref_empties, ref_tris):
    res = {}
    for ext, op in ((".fbx", lambda p: bpy.ops.import_scene.fbx(filepath=p)), (".glb", lambda p: bpy.ops.import_scene.gltf(filepath=p))):
        reset_scene(); op(base + ext); bpy.context.view_layer.update()
        ms = [o for o in bpy.data.objects if o.type == 'MESH']
        if not ms: res[ext] = {"PASS": False, "why": "no mesh"}; continue
        v = verts(ms[0]); lo = [min(p[i] for p in v) for i in range(3)]; hi = [max(p[i] for p in v) for i in range(3)]
        dmm = max(max(abs(lo[i] - ref_bounds[0][i]), abs(hi[i] - ref_bounds[1][i])) for i in range(3))*1000
        emp = {base_name(o.name): o for o in bpy.data.objects if o.type == 'EMPTY'}
        edev = 0.0; missing = [n for n in ref_empties if n not in emp]; adev = 0.0
        for n, r in ref_empties.items():
            if n in emp:
                edev = max(edev, (emp[n].matrix_world.translation - Vector(r["loc"])).length*1000)
                z = (emp[n].matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()
                adev = max(adev, math.degrees(z.angle(Vector(r["z_axis"]))))
        tris = sum(len(p.vertices) - 2 for p in ms[0].data.polygons)
        sc = max(abs(s - 1.0) for o in ms for s in o.matrix_world.to_scale())
        res[ext] = {"PASS": dmm < 0.05 and not missing and edev < 0.05 and adev < 0.05 and tris == ref_tris and sc < 1e-4,
                    "bounds_dev_mm": round(dmm, 4), "verts": len(ms[0].data.vertices), "triangles": tris, "world_scale_dev": round(sc, 6),
                    "empties": sorted(emp), "empties_missing": missing, "empty_pos_dev_mm": round(edev, 4), "empty_axis_dev_deg": round(adev, 4),
                    "materials": sorted({s.material.name.split(".")[0] for s in ms[0].material_slots if s.material})}
    return res


# ------------------------------------------------------------------------------------------------ pass 1: build one block
def build_block(kind, key, ref):
    t0 = time.time(); ID = "R4B_" + kind; label = DIRECTIONS[key]
    od = os.path.join(OUT, label, ID); os.makedirs(os.path.join(od, "renders"), exist_ok=True)
    src = json.load(open(os.path.join(SRC, f"{ID}_{key}.json"))); lsrc = json.load(open(os.path.join(SRC, f"{ID}_{key}_LOD1.json")))
    approved = json.load(open(os.path.join(REF_BLOCKS, ID, ID + "_QA.json")))            # interface comes from the approved QA record
    ports = approved["ports"]; cells = approved["cells"]
    scn = reset_scene()
    mats = {z: make_zone_material(z, src["zone_default"][z]) for z in ZONE_ORDER}
    col = new_col(ID); lcol = new_col(ID + "_LOD1")
    ob = mesh_from_source(src, ID, mats, col); lod = mesh_from_source(lsrc, ID + "_LOD1", mats, lcol)
    root = bpy.data.objects.new(ID + "_ROOT", None); col.objects.link(root); root.empty_display_size = 0.1
    ob.parent = root; lod.parent = root
    empties = [root]
    for p in ports:                                                                        # approved empties code
        n = Vector(DIRS[p["dir"]]); u, v, _ = frame(n)
        c = Vector(p["cell"])*G + n*H + (u*p["offset"][0] + v*p["offset"][1])*G
        e = bpy.data.objects.new(f"Port_{p['dir']}_{p['cell'][0]}{p['cell'][1]}{p['cell'][2]}" + ("" if p["kind"] == "full" else "_part"), None)
        col.objects.link(e); e.parent = root; e.location = c; e.empty_display_type = 'SINGLE_ARROW'; e.empty_display_size = 0.06
        e.rotation_euler = n.to_track_quat('Z', 'Y').to_euler(); e["kind"] = p["kind"]; e["patch_offset_cells"] = p["offset"]; e["patch_half_cells"] = p["half"]
        empties.append(e)
    s = bpy.data.objects.new("Socket_Mount", None); col.objects.link(s); s.parent = root; s.empty_display_type = 'SINGLE_ARROW'; s.empty_display_size = 0.08
    s.location = (0, 0, -H); s.rotation_euler = (math.pi, 0, 0); empties.append(s)
    cmask = contact_mask(ports)
    for k, v in {"block_kind": kind, "description": approved["description"], "grid_m": G, "block_m": 2*H, "cells": json.dumps(cells), "contact_mask_local": cmask,
                 "unity_up": "+Y", "unity_forward": "+Z", "style": "R4 redesign pilot #1232 - " + label, "art_direction": key, "port": "R4 port (interface unchanged from PR #963)",
                 "paint_zones": json.dumps(src["zone_default"]), "interface_source": "PR #963 art/r4-building-blocks @03af520"}.items(): root[k] = v
    bpy.context.view_layer.update()
    # ---- QA (same checks as the approved pipeline + interface comparison against the approved GLB)
    mr = mesh_report(ob); lr = mesh_report(lod); V = verts(ob)
    lo = [min(p[i] for p in V) for i in range(3)]; hi = [max(p[i] for p in V) for i in range(3)]
    cx = [c[0] for c in cells]; cy = [c[1] for c in cells]; cz = [c[2] for c in cells]
    lim_lo = [min(cx)*G - H, min(cy)*G - H, min(cz)*G - H]; lim_hi = [max(cx)*G + H, max(cy)*G + H, max(cz)*G + H]
    in_cells = all(lo[i] >= lim_lo[i] - 0.0005 and hi[i] <= lim_hi[i] + 0.0005 for i in range(3))
    # inside the approved solid (the convex cell solid the approved collider is built from)
    bmh = bmesh.new(); hv = [bmh.verts.new(p) for p in src["solid_points"]]; bmesh.ops.convex_hull(bmh, input=hv); bmh.faces.ensure_lookup_table()
    planes = [(f.normal.copy(), f.normal.dot(f.verts[0].co)) for f in bmh.faces]; bmh.free()
    out_mm = max(max((n.dot(p) - d) for (n, d) in planes) for p in V)*1000
    lod_out_mm = max(max((n.dot(p) - d) for (n, d) in planes) for p in verts(lod))*1000
    bm = bmesh.new(); bm.from_mesh(ob.data); bm.transform(ob.matrix_world); tree = BVHTree.FromBMesh(bm); bm.free()
    ptest = []
    for p in ports:
        r = patch_test(tree, p["cell"], p["dir"], p["offset"], p["half"])
        old = next((q for q in approved["port_contact_test"] if q["dir"] == p["dir"] and q["cell"] == p["cell"]), None)
        r.update({"cell": p["cell"], "dir": p["dir"], "kind": p["kind"], "approved_flush_pct": old["flush_pct"] if old else None})
        r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65)
        ptest.append(r)
    bm = bmesh.new(); bm.from_mesh(lod.data); ltree = BVHTree.FromBMesh(bm); bm.free()
    lptest = []
    for p in ports:
        r = patch_test(ltree, p["cell"], p["dir"], p["offset"], p["half"]); r.update({"dir": p["dir"], "kind": p["kind"]})
        r["PASS"] = r["protruding"] == 0 and r["missing"] == 0 and r["flush_pct"] >= (80 if p["kind"] == "full" else 65); lptest.append(r)
    emp_now = {e.name: e for e in empties}; edev = 0.0; adev = 0.0
    missing = [n for n in ref["empties"] if n not in emp_now]; extra = [n for n in emp_now if n not in ref["empties"]]
    for n, r in ref["empties"].items():
        if n in emp_now:
            edev = max(edev, (emp_now[n].matrix_world.translation - Vector(r["loc"])).length*1000)
            z = (emp_now[n].matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized(); adev = max(adev, math.degrees(z.angle(Vector(r["z_axis"]))))
    iface = {"approved_glb": ref["glb"], "approved_sha256": ref["sha256"], "empties_missing": missing, "empties_extra": extra, "pos_dev_mm": round(edev, 4), "axis_dev_deg": round(adev, 4),
             "ports_identical": True, "cells_identical": True, "contact_mask_approved": approved["contact_mask_local_anchor"], "contact_mask": cmask,
             "PASS": not missing and not extra and edev < 0.01 and adev < 0.01 and cmask == approved["contact_mask_local_anchor"]}
    mat_names = sorted({s_.material.name for s_ in ob.material_slots if s_.material})
    base = os.path.join(od, ID)
    select([root, ob] + empties[1:], root); export_fbx(base + ".fbx"); export_glb(base + ".glb")
    select([root, lod], root); export_fbx(base + "_LOD1.fbx"); export_glb(base + "_LOD1.glb")
    failures = []
    if not (mr["ok"] and lr["ok"]): failures.append("topology")
    if not in_cells: failures.append("geometry outside its grid cells")
    if out_mm > 0.01 or lod_out_mm > 0.01: failures.append("geometry outside the approved solid")
    if not all(p["PASS"] for p in ptest): failures.append("port contact patch")
    if not all(p["PASS"] for p in lptest): failures.append("LOD1 port contact patch")
    if not iface["PASS"]: failures.append("interface differs from approved PR #963")
    if not set(mat_names) <= set(ZONE_ORDER): failures.append("paint zones")
    # ---- renders (same camera / light as the 'before' pass)
    lod.hide_render = True
    cam = setup_render(scn); clo, chi = cell_bounds(kind)
    ctr = Vector([(clo[i] + chi[i])/2 for i in range(3)]); ext = max(chi[i] - clo[i] for i in range(3)); renders = []
    for tag, dvec in VIEWS:
        p_ = os.path.join(od, "renders", f"{ID}_{tag}.jpg"); shoot(scn, cam, ctr, ext*3.1 + 0.6, dvec, p_); renders.append(os.path.basename(p_))
    lod.hide_render = False; lod.hide_viewport = False
    for o in list(scn.collection.objects):
        if o.type in ('LIGHT', 'CAMERA'): pass
    bpy.ops.wm.save_as_mainfile(filepath=base + ".blend", compress=False)
    # ---- reimport in a fresh scene
    re_ = reimport_check(base, (lo, hi), ref["empties"], mr["triangles"])
    if not all(v["PASS"] for v in re_.values()): failures.append("export reimport")
    files = {}
    for f in sorted(os.listdir(od)):
        fp = os.path.join(od, f)
        if os.path.isfile(fp) and not f.endswith(".json") and not f.endswith(".blend1"): files[f] = {"sha256": sha(fp), "bytes": os.path.getsize(fp)}
    qa = {"id": ID, "block_kind": kind, "issue": 1232, "art_direction": key, "art_direction_label": label, "description": approved["description"],
          "status": "BLENDER_PASS" if not failures else "HOLD", "failures": failures, "unity": "UNITY_NOT_RUN",
          "cells": cells, "bonds": approved.get("bonds", []), "note": approved.get("note"), "contact_mask_local_anchor": cmask,
          "contact_mask_bits": approved["contact_mask_bits"], "ports": ports, "port_contact_test": ptest, "lod1_port_contact_test": lptest,
          "interface_vs_approved": iface, "mesh": mr, "mesh_lod1": lr, "approved_triangles": ref["triangles"],
          "bounds_m": [[round(x, 4) for x in lo], [round(x, 4) for x in hi]], "approved_bounds_m": ref["bounds_m"], "inside_cells": in_cells,
          "max_outside_approved_solid_mm": round(out_mm, 4), "lod1_max_outside_approved_solid_mm": round(lod_out_mm, 4),
          "materials": mat_names, "paint_zones": src["zone_default"], "style_parameters": src["style"], "reimport": re_, "renders": renders,
          "collider": "unchanged - approved R4Blocks/%s/%s_colliders.glb (not re-authored in this pilot)" % (ID, ID),
          "files": files, "seconds": round(time.time() - t0, 1), "blender": bpy.app.version_string}
    json.dump(qa, open(base + "_QA.json", "w", encoding="utf-8"), indent=1)
    log("BLOCK", label, ID, qa["status"], failures, mr["triangles"], "tris", "LOD1", lr["triangles"])
    return qa["status"], failures, mr["triangles"]


summary = {}; refs = {}
os.makedirs(OUT, exist_ok=True)
for kind in PILOT:
    if ONLY and kind not in ONLY: continue
    try:
        refs[kind] = reference_pass(kind)
        for key in DIRECTIONS:
            summary[f"{DIRECTIONS[key]}/R4B_{kind}"] = build_block(kind, key, refs[kind])
    except Exception as ex:
        import traceback; traceback.print_exc(); summary[f"R4B_{kind}"] = ("ERROR", [repr(ex)], 0); log("ERROR", kind, repr(ex))
json.dump({"summary": summary, "reference": refs, "blender": bpy.app.version_string}, open(os.path.join(OUT, "_build_summary.json"), "w"), indent=1)
log("BUILD_DONE", json.dumps({k: v[0] for k, v in summary.items()}))
