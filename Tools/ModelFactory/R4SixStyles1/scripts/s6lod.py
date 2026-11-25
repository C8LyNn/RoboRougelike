"""RoboRougelike six-style series 1: rebuild the LOD1 of a delivered (repaired) rigged model, so LOD1 carries the same sense repairs as LOD0.
Same LOD1 rule as the series generator (s6model.py -> hlod.lod1): the game meshes in the rest pose (frame 1), MuzzleFlash and the outline
shell left out, joined into one static mesh, planar dissolve 14/9/5 deg; exported as <CID>_LOD1.glb / .fbx with the generator's settings,
then re-imported and compared (triangles, materials, bounds).
usage: blender -b --factory-startup --python s6lod.py -- SRC_GLB OUT_DIR CID [collapse:RATIO]
dissolve:A[,B..] = the generator rule with other dissolve angles (smaller angle = closer to LOD0, for a model whose joints come loose at 14 deg);
keep = the joined game mesh as LOD1 (the hlod fallback);
collapse:RATIO = the rule of the original R4 model instead (R4Weapons938: DECIMATE collapse 0.5, automatic), used for the repaired 06 gun"""
import bpy, bmesh, sys, os, json
sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import hf, hlod
from mathutils import Vector
A = sys.argv[sys.argv.index("--") + 1:]; SRC, OUT, CID = A[0], A[1], A[2]; METHOD = A[3] if len(A) > 3 else "dissolve"


def bounds(objs):
    vs = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    return [[round(min(v[k] for v in vs), 5) for k in range(3)], [round(max(v[k] for v in vs), 5) for k in range(3)]]


bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene
bpy.ops.import_scene.gltf(filepath=SRC); sc.frame_set(1)
col = sc.collection; cp = []
for o in list(bpy.data.objects):
    if o.type != "MESH" or o.name.split(".")[0].endswith("MuzzleFlash"): continue
    c = o.copy(); c.data = o.data.copy(); c.parent = None; c.animation_data_clear(); col.objects.link(c); c.matrix_world = o.matrix_world.copy()
    bm = bmesh.new(); bm.from_mesh(c.data); mats = [(s.material.name if s.material else "") for s in c.material_slots]
    dead = [f for f in bm.faces if f.material_index < len(mats) and mats[f.material_index].startswith("R4P_Outline")]
    bmesh.ops.delete(bm, geom=dead, context="FACES"); bm.to_mesh(c.data); bm.free(); cp.append(c)
src_b = bounds(cp)
for o in list(bpy.data.objects):
    if o not in cp: bpy.data.objects.remove(o, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
for c in cp: c.select_set(True)
bpy.context.view_layer.objects.active = cp[0]; bpy.ops.object.join(); allo = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# the glTF import splits vertices at every UV / normal seam and gives triangles: weld them back and rebuild quads first, otherwise the
# planar dissolve finds almost nothing to merge (the generator runs it on the un-exported part meshes)
if METHOD != "keep":
    bm = bmesh.new(); bm.from_mesh(allo.data); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.join_triangles(bm, faces=bm.faces, angle_face_threshold=0.0349, angle_shape_threshold=0.698, cmp_uvs=True, cmp_materials=True)
    bm.to_mesh(allo.data); bm.free()
ms = allo.data.materials
for i in reversed(range(len(ms))):
    if ms[i] is not None and ms[i].name.startswith("R4P_Outline"): ms.pop(index=i)
if METHOD.startswith("collapse:"):
    ratio = float(METHOD.split(":")[1]); lod = allo; bpy.ops.object.select_all(action="DESELECT"); lod.select_set(True); bpy.context.view_layer.objects.active = lod
    m = lod.modifiers.new("d", "DECIMATE"); m.decimate_type = "COLLAPSE"; m.ratio = ratio; m.use_collapse_triangulate = True; bpy.ops.object.modifier_apply(modifier="d")
    info = dict(method="DECIMATE collapse %.2f (automatic, the rule of the original R4 model)" % ratio, triangles=hf.tris(lod))
elif METHOD == "keep":
    lod = allo; info = dict(method="game mesh kept as LOD1 (hlod fallback): every dissolve angle (14/9/5 deg) lifted a part of this model off its neighbour in the LOD1 sense check", triangles=hf.tris(lod))
elif METHOD.startswith("dissolve:"):
    lod, info = hlod.lod1(allo, col, angles=tuple(float(a) for a in METHOD.split(":")[1].split(","))); bpy.data.objects.remove(allo, do_unlink=True)
else:
    lod, info = hlod.lod1(allo, col); bpy.data.objects.remove(allo, do_unlink=True)
lod.name = CID + "_LOD1"; lod.data.name = CID + "_LOD1"
# small details (bolts, rivets) that the simplification lifted off their face are dropped: a loose part under 30 mm that is more than
# 2.5 mm from everything else would float in LOD1 (the sense check of LOD1); at LOD1 distance it is under a pixel anyway
from mathutils.bvhtree import BVHTree
bm = bmesh.new(); bm.from_mesh(lod.data); bm.faces.ensure_lookup_table(); seen = set(); isl = []
for f0 in bm.faces:
    if f0.index in seen: continue
    st = [f0]; comp = []; seen.add(f0.index)
    while st:
        g = st.pop(); comp.append(g)
        for v in g.verts:
            for h in v.link_faces:
                if h.index not in seen: seen.add(h.index); st.append(h)
    isl.append(comp)
dropped = []
if len(isl) > 1:
    for comp in isl:
        vs = {v for f in comp for v in f.verts}; co = [v.co for v in vs]
        lo = Vector((min(c.x for c in co), min(c.y for c in co), min(c.z for c in co))); hi = Vector((max(c.x for c in co), max(c.y for c in co), max(c.z for c in co)))
        if (hi - lo).length >= 0.030: continue
        cs = set(comp); rest = [f for f in bm.faces if f not in cs]
        tree = BVHTree.FromPolygons([v.co for v in bm.verts], [[v.index for v in f.verts] for f in rest]) if rest else None
        dmin = min((tree.find_nearest(c)[3] for c in co), default=9) if tree else 9
        if dmin > 0.0025: dropped.append(dict(centre_mm=[round(1000*c) for c in (lo + hi)/2], size_mm=round(1000*(hi - lo).length, 1), gap_mm=round(1000*dmin, 1), faces=len(comp)))
    dead = []
    for comp in isl:
        vs = {v for f in comp for v in f.verts}; co = [v.co for v in vs]
        lo = Vector((min(c.x for c in co), min(c.y for c in co), min(c.z for c in co))); hi = Vector((max(c.x for c in co), max(c.y for c in co), max(c.z for c in co)))
        if any(d["centre_mm"] == [round(1000*c) for c in (lo + hi)/2] and d["faces"] == len(comp) for d in dropped): dead += comp
    if dead: bmesh.ops.delete(bm, geom=dead, context="FACES"); bm.to_mesh(lod.data)
bm.free(); info["dropped_loose_details"] = dropped
os.makedirs(OUT, exist_ok=True); d = os.path.join(OUT, CID + "_LOD1.glb")
bpy.ops.object.select_all(action="DESELECT"); lod.select_set(True); bpy.context.view_layer.objects.active = lod
exp = dict(tris=hf.tris(lod), mats=sorted(m.name for m in lod.data.materials if m), bounds=bounds([lod]))
bpy.ops.export_scene.gltf(filepath=d, export_format='GLB', use_selection=True, export_yup=True, export_apply=True, export_animations=False)
bpy.ops.export_scene.fbx(filepath=d[:-4] + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', object_types={'MESH'}, mesh_smooth_type='FACE', bake_anim=False, path_mode='COPY', embed_textures=True)
rq = {}
for ext in (".glb", ".fbx"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    (bpy.ops.import_scene.gltf if ext == ".glb" else bpy.ops.import_scene.fbx)(filepath=d[:-4] + ext)
    ob = [o for o in bpy.data.objects if o.type == "MESH"]
    b = bounds(ob); dev = max(abs(b[i][k] - exp["bounds"][i][k]) for i in range(2) for k in range(3))
    mats = sorted({s.material.name.split(".")[0] for o in ob for s in o.material_slots if s.material})
    rq[ext] = dict(objects=[o.name for o in ob], tris=sum(hf.tris(o) for o in ob), bounds_dev_mm=round(1000*dev, 3), materials=mats,
                   PASS=len(ob) == 1 and dev < 0.0005 and set(mats) == set(m.split(".")[0] for m in exp["mats"]))
rep = dict(cid=CID, source=SRC, rule="rest pose, MuzzleFlash and outline shell left out, joined, hlod.lod1 planar dissolve", lod1=info, tris_lod1=exp["tris"], source_bounds=src_b, lod1_bounds=exp["bounds"],
           reimport=rq, status="BLENDER_PASS" if all(r["PASS"] for r in rq.values()) else "FAIL")
json.dump(rep, open(d[:-4] + "_info.json", "w"), indent=1); print("[S6L] LOD_DONE", rep["status"], exp["tris"], json.dumps({k: v["PASS"] for k, v in rq.items()}))
