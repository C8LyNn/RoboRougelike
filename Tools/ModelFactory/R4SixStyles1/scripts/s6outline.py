"""RoboRougelike six-style series 1: add the Comic Outline contour (inverted hull, back-face culled material R4P_Outline) to an EXISTING rigged
model and export it again with its animation. Used for the Pass 4 Heroic Toon gun (kept geometry, outline added). RUNS INSIDE BLENDER.
usage: blender -b --factory-startup --python s6outline.py -- SRC_GLB OUT_DIR CID WIDTH"""
import bpy, bmesh, sys, os, json, math, hashlib
import numpy as np
a = sys.argv[sys.argv.index("--") + 1:]; SRC, OUT, CID, W = a[0], a[1], a[2], float(a[3])
def log(*x): print("[S6O]", *x, flush=True)


def add_outline(o, width, mat):
    me = o.data; bm = bmesh.new(); bm.from_mesh(me); bm.normal_update()
    src = bmesh.new(); src.from_mesh(me); bmesh.ops.remove_doubles(src, verts=src.verts, dist=1e-5); src.normal_update()
    nrm = {tuple(round(c, 5) for c in v.co): v.normal.copy() for v in src.verts}
    geom = bmesh.ops.duplicate(bm, geom=bm.faces[:])["geom"]; nf = [g for g in geom if isinstance(g, bmesh.types.BMFace)]
    for v in [g for g in geom if isinstance(g, bmesh.types.BMVert)]: v.co += nrm.get(tuple(round(c, 5) for c in v.co), v.normal)*width
    bmesh.ops.reverse_faces(bm, faces=nf); slot = len(me.materials); me.materials.append(mat)
    for f in nf: f.material_index = slot
    bm.to_mesh(me); bm.free(); src.free()


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; bpy.ops.import_scene.gltf(filepath=SRC)
    om = bpy.data.materials.new("R4P_Outline"); om.use_nodes = True; b = om.node_tree.nodes["Principled BSDF"]; b.inputs["Base Color"].default_value = (0.006, 0.006, 0.012, 1)
    b.inputs["Roughness"].default_value = 1.0; b.inputs["Specular IOR Level"].default_value = 0.0; om.use_backface_culling = True
    objs = list(bpy.data.objects); n = 0
    for o in objs:
        if o.type == "MESH" and not o.name.endswith("MuzzleFlash"):
            if o.data.users > 1: o.data = o.data.copy()
            S = o.matrix_world.to_scale(); add_outline(o, W/max(1e-6, S.x), om); n += 1
    acts = [o for o in objs if o.animation_data and o.animation_data.action]
    F = max([int(o.animation_data.action.frame_range[1]) for o in acts] + [1]); sc.frame_start = 1; sc.frame_end = F
    test = [1, 39, 63, 89, 101, 117, 128, 135]
    exp = {}
    for f in test:
        sc.frame_set(f); exp[f] = {o.name: [list(r) for r in o.matrix_world] for o in objs}
    sc.frame_set(1)
    os.makedirs(OUT, exist_ok=True); d = os.path.join(OUT, CID + ".glb")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=d[:-4] + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=False, object_types={'MESH', 'EMPTY'},
                             mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True)
    bpy.ops.object.select_all(action="SELECT"); mode = None
    for m in ("ACTIONS", "SCENE", "ACTIVE_ACTIONS"):                                                  # the imported take is one slotted action shared by all nodes
        try:
            bpy.ops.export_scene.gltf(filepath=d, export_format='GLB', use_selection=True, export_yup=True, export_apply=False, export_extras=True, export_animations=True, export_animation_mode=m, export_frame_range=True); mode = m; break
        except Exception as ex: log("gltf export mode", m, "failed:", str(ex)[:200])
    log("gltf animation mode", mode)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, CID + "_SOURCE.blend"), compress=True)
    rq = {}
    for ext in (".glb", ".fbx"):
        for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)
        if ext == ".glb": bpy.ops.import_scene.gltf(filepath=d)
        else: bpy.ops.import_scene.fbx(filepath=d[:-4] + ".fbx", use_anim=True)
        imp = {o.name.split(".")[0]: o for o in bpy.data.objects}; best = None
        for off in (0, -1, 1):
            dv = 0.0
            for f in test:
                sc.frame_set(f + off)
                for nme, M in exp[f].items():
                    if nme in imp: dv = max(dv, float(np.abs(np.array(M)[:3, 3] - np.array(imp[nme].matrix_world)[:3, 3]).max()))
            if best is None or dv < best[0]: best = (dv, off)
        mats = sorted({s.material.name.split(".")[0] for o in imp.values() if o.type == "MESH" for s in o.material_slots if s.material})
        rq[ext] = dict(missing=sorted(set(exp[1]) - set(imp)), anim_pos_dev_mm=round(best[0]*1000, 2), frame_offset=best[1], materials=mats, PASS=(not (set(exp[1]) - set(imp))) and best[0] < 0.002 and "R4P_Outline" in mats)
    T = dict(cid=CID, source=SRC, change="Comic Outline contour added (inverted hull %.4f m, material R4P_Outline, back-face culled); geometry, rig and animation of the source kept" % W,
             meshes_outlined=n, reimport=rq, status="BLENDER_PASS" if all(r["PASS"] for r in rq.values()) else "HOLD", unity="UNITY_NOT_RUN",
             files={os.path.basename(p): hashlib.sha256(open(p, "rb").read()).hexdigest()[:16] for p in (d, d[:-4] + ".fbx")})
    json.dump(T, open(os.path.join(OUT, CID + "_QA.json"), "w"), indent=1); log("OUTLINE_DONE", T["status"], json.dumps(rq))


try: main()
except Exception:
    import traceback; traceback.print_exc(); log("OUTLINE_FAILED")
