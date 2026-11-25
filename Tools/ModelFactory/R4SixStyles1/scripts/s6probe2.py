"""probe: islands (loose parts) of chosen nodes near given points, at a given frame. usage:
blender -b --factory-startup --python s6probe2.py -- GLB OUT_JSON FRAME '[[x,y,z,r], ...]'"""
import bpy, bmesh, sys, json
from mathutils import Vector
A = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=A[0]); fr = int(A[2]); P = json.loads(A[3])
sc = bpy.context.scene; out = {}
for f in (1, fr):
    sc.frame_set(f); dg = bpy.context.evaluated_depsgraph_get(); res = []
    for o in bpy.data.objects:
        if o.type != "MESH": continue
        oe = o.evaluated_get(dg); me = oe.to_mesh(); M = oe.matrix_world.copy()
        mats = [(s.material.name if s.material else "") for s in o.material_slots]
        bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); seen = set()
        for f0 in bm.faces:
            if f0.index in seen: continue
            st = [f0]; comp = []; seen.add(f0.index)
            while st:
                g = st.pop(); comp.append(g)
                for v in g.verts:
                    for h in v.link_faces:
                        if h.index not in seen: seen.add(h.index); st.append(h)
            vs = list({v.index: M @ v.co for g in comp for v in g.verts}.values())
            lo = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))); hi = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
            for (x, y, z, r) in P:
                c = Vector((x, y, z))
                if all(lo[k] - r <= c[k] <= hi[k] + r for k in range(3)):
                    mt = mats[comp[0].material_index] if comp[0].material_index < len(mats) else ""
                    res.append(dict(obj=o.name, mat=mt, nf=len(comp), lo=[round(1000*a, 1) for a in lo], hi=[round(1000*a, 1) for a in hi], near=[x, y, z])); break
        bm.free(); oe.to_mesh_clear()
    out[str(f)] = res
json.dump(out, open(A[1], "w"), indent=1); print("[PROBE] done", {k: len(v) for k, v in out.items()})
