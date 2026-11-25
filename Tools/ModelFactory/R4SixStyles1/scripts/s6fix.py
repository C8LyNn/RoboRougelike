"""RoboRougelike six-style series 1: repair an EXISTING rigged model (a kept Pass 4 gun) where the sense check found parts that do not hold on
to anything, then export it again with its animation (same export + re-import QA as s6outline.py). New parts take the colour of the nearest
face of the node they join (their UVs are pinned to that face), so they read as the same material.
usage: blender -b --factory-startup --python s6fix.py -- SRC_GLB OUT_DIR CID OPS_JSON [OUTLINE_WIDTH]
OPS_JSON: [{"op": "box", "node": obj, "min": [..], "max": [..], "why": ".."},
           {"op": "tube", "node": obj, "axis": "x|y|z", "c": [x, y, z], "r0": .., "r1": .., "t0": .., "t1": .., "seg": 12, "why": ".."},
           {"op": "move", "node": obj, "d": [dx, dy, dz], "why": ".."}]          (world coordinates of the rest pose, frame 1)"""
import bpy, bmesh, sys, os, json, math, hashlib
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
a = sys.argv[sys.argv.index("--") + 1:]; SRC, OUT, CID, OPS = a[0], a[1], a[2], json.load(open(a[3])); W = float(a[4]) if len(a) > 4 else 0.0
def log(*x): print("[S6F]", *x, flush=True)


def add_outline(o, width, mat):
    """inverted hull, the same as s6outline.add_outline (copied: importing s6outline would run its main)"""
    me = o.data; bm = bmesh.new(); bm.from_mesh(me); bm.normal_update()
    src = bmesh.new(); src.from_mesh(me); bmesh.ops.remove_doubles(src, verts=src.verts, dist=1e-5); src.normal_update()
    nrm = {tuple(round(c, 5) for c in v.co): v.normal.copy() for v in src.verts}
    geom = bmesh.ops.duplicate(bm, geom=bm.faces[:])["geom"]; nf = [g for g in geom if isinstance(g, bmesh.types.BMFace)]
    for v in [g for g in geom if isinstance(g, bmesh.types.BMVert)]: v.co += nrm.get(tuple(round(c, 5) for c in v.co), v.normal)*width
    bmesh.ops.reverse_faces(bm, faces=nf); slot = len(me.materials); me.materials.append(mat)
    for f in nf: f.material_index = slot
    bm.to_mesh(me); bm.free(); src.free()


def find(name):
    for o in bpy.data.objects:
        if o.type == "MESH" and (o.name == name or o.name.split(".")[0] == name): return o
    raise KeyError(name)


def part_bm(op):
    bm = bmesh.new()
    if op["op"] == "box":
        lo, hi = Vector(op["min"]), Vector(op["max"]); c = (lo + hi)/2; h = hi - lo
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(c) @ Matrix.Diagonal((h.x, h.y, h.z, 1)))
    else:
        n = op.get("seg", 12); ax = op["axis"]; c = Vector(op["c"]); rings = []
        for t in (op["t0"], op["t1"]):
            for r in (op["r0"], op["r1"]):
                ring = []
                for i in range(n):
                    u, v = r*math.cos(2*math.pi*i/n), r*math.sin(2*math.pi*i/n)
                    p = {"x": Vector((t, u, v)), "y": Vector((u, t, v)), "z": Vector((u, v, t))}[ax]
                    q = Vector(c); q[{"x": 0, "y": 1, "z": 2}[ax]] = 0.0; ring.append(bm.verts.new(q + p))
                rings.append(ring)
        (a0, b0, a1, b1) = rings                                                              # t0 inner, t0 outer, t1 inner, t1 outer
        for i in range(n):
            j = (i + 1) % n
            for (p, q) in ((a0, b0), (b0, b1), (b1, a1), (a1, a0)): bm.faces.new((p[i], p[j], q[j], q[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def add_part(op):
    o = find(op["node"]); me = o.data; Mi = o.matrix_world.inverted()
    pb = part_bm(op); cen = sum((v.co for v in pb.verts), Vector())/len(pb.verts)
    # donor: the face of this node nearest to the new part (world space)
    src = bmesh.new(); src.from_mesh(me); src.transform(o.matrix_world); src.faces.ensure_lookup_table()
    tree = BVHTree.FromBMesh(src); hit = tree.find_nearest(cen); fi = hit[2]
    uvl = src.loops.layers.uv.active; donor = src.faces[fi]; uv = sum((l[uvl].uv for l in donor.loops), Vector((0, 0)))/len(donor.loops) if uvl else None
    mat = donor.material_index; src.free()
    pb.transform(Mi)
    bm = bmesh.new(); bm.from_mesh(me); uvb = bm.loops.layers.uv.active
    vmap = {v: bm.verts.new(v.co) for v in pb.verts}; new = []
    for f in pb.faces:
        try: nf = bm.faces.new([vmap[v] for v in f.verts])
        except ValueError: continue
        nf.material_index = mat; new.append(nf)
        if uvb is not None and uv is not None:
            for l in nf.loops: l[uvb].uv = uv
    bm.to_mesh(me); bm.free(); pb.free(); me.update()
    return dict(node=o.name, faces=len(new), donor_face=fi, centre_mm=[round(1000*c) for c in cen])


def squash(op):
    """scale the vertices of one region of a node along one axis towards an anchor plane (world coordinates of the rest pose)"""
    o = find(op["node"]); M = o.matrix_world.copy(); Mi = M.inverted(); lo, hi = Vector(op["min"]), Vector(op["max"]); k = "xyz".index(op["axis"]); n = 0
    for v in o.data.vertices:
        w = M @ v.co
        if all(lo[i] <= w[i] <= hi[i] for i in range(3)): w[k] = op["anchor"] + (w[k] - op["anchor"])*op["factor"]; v.co = Mi @ w; n += 1
    o.data.update(); return dict(node=o.name, verts=n)


def freeze(op):
    o = find(op["node"]); o.animation_data_clear(); return dict(node=o.name, animation="removed - the part now only follows its parent")


def move(op):
    o = find(op["node"]); d = o.matrix_world.inverted().to_3x3() @ Vector(op["d"]); o.data.transform(Matrix.Translation(d)); o.data.update()
    return dict(node=o.name, local_d=[round(x, 5) for x in d])


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; bpy.ops.import_scene.gltf(filepath=SRC)
    objs = list(bpy.data.objects)
    for o in objs:
        if o.type == "MESH" and o.data.users > 1: o.data = o.data.copy()
    acts = [o for o in objs if o.animation_data and o.animation_data.action]
    F = max([int(o.animation_data.action.frame_range[1]) for o in acts] + [1]); sc.frame_start = 1; sc.frame_end = F
    sc.frame_set(1); done = []
    for op in OPS:
        r = move(op) if op["op"] == "move" else freeze(op) if op["op"] == "freeze" else squash(op) if op["op"] == "squash" else add_part(op); r["why"] = op.get("why", ""); r["op"] = op["op"]; done.append(r); log("op", json.dumps(r))
    n_out = 0
    if W > 0:
        om = bpy.data.materials.new("R4P_Outline"); om.use_nodes = True; b = om.node_tree.nodes["Principled BSDF"]; b.inputs["Base Color"].default_value = (0.006, 0.006, 0.012, 1)
        b.inputs["Roughness"].default_value = 1.0; b.inputs["Specular IOR Level"].default_value = 0.0; om.use_backface_culling = True
        for o in objs:
            if o.type == "MESH" and not o.name.endswith("MuzzleFlash"):
                S = o.matrix_world.to_scale(); add_outline(o, W/max(1e-6, S.x), om); n_out += 1
    test = [1, 39, 63, 89, 101, 117, 128, 135]; exp = {}
    for f in test:
        sc.frame_set(f); exp[f] = {o.name: [list(r) for r in o.matrix_world] for o in objs}
    sc.frame_set(1)
    os.makedirs(OUT, exist_ok=True); d = os.path.join(OUT, CID + ".glb")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(filepath=d[:-4] + ".fbx", use_selection=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=False, object_types={'MESH', 'EMPTY'},
                             mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True)
    bpy.ops.object.select_all(action="SELECT"); mode = None
    for m in ("ACTIONS", "SCENE", "ACTIVE_ACTIONS"):
        try:
            bpy.ops.export_scene.gltf(filepath=d, export_format='GLB', use_selection=True, export_yup=True, export_apply=False, export_extras=True, export_animations=True, export_animation_mode=m, export_frame_range=True); mode = m; break
        except Exception as ex: log("gltf export mode", m, "failed:", str(ex)[:200])
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
        rq[ext] = dict(missing=sorted(set(exp[1]) - set(imp)), anim_pos_dev_mm=round(best[0]*1000, 2), frame_offset=best[1], materials=mats,
                       PASS=(not (set(exp[1]) - set(imp))) and best[0] < 0.002 and (W == 0 or "R4P_Outline" in mats))
    T = dict(cid=CID, source=SRC, change="sense repair of the kept model: %d parts added / moved so every part holds on to the structure%s" % (len(done), (", outline %.4f m" % W) if W else ""),
             ops=done, meshes_outlined=n_out, gltf_animation_mode=mode, reimport=rq, status="BLENDER_PASS" if all(r["PASS"] for r in rq.values()) else "HOLD", unity="UNITY_NOT_RUN",
             files={os.path.basename(p): hashlib.sha256(open(p, "rb").read()).hexdigest()[:16] for p in (d, d[:-4] + ".fbx")})
    json.dump(T, open(os.path.join(OUT, CID + "_QA.json"), "w"), indent=1); log("FIX_DONE", T["status"], json.dumps(rq))


try: main()
except Exception:
    import traceback; traceback.print_exc(); log("FIX_FAILED")
