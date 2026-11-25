"""dump node hierarchy, transforms, bounds, materials and animation of GLB files. usage: blender -b --python probe.py -- OUT_JSON file.glb [file.glb ...]"""
import bpy, sys, json, numpy as np
a = sys.argv[sys.argv.index("--") + 1:]; OUT = a[0]; res = {}
for p in a[1:]:
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=p); sc = bpy.context.scene
    info = {}
    for o in bpy.data.objects:
        d = {"type": o.type, "parent": o.parent.name if o.parent else None, "loc": [round(x, 4) for x in o.matrix_world.translation], "rot": [round(x, 3) for x in o.matrix_world.to_euler()], "scale": [round(x, 3) for x in o.matrix_world.to_scale()]}
        if o.type == "MESH":
            V = np.array([o.matrix_world @ v.co for v in o.data.vertices]); d["bb"] = [V.min(0).round(4).tolist(), V.max(0).round(4).tolist()]
            d["tris"] = sum(len(pl.vertices) - 2 for pl in o.data.polygons); d["mats"] = [m.name for m in o.data.materials if m]
        if o.animation_data and o.animation_data.action: d["action"] = o.animation_data.action.name
        info[o.name] = d
    acts = {}
    for ac in bpy.data.actions:
        acts[ac.name] = {"range": list(ac.frame_range)}
    mats = {}
    for m in bpy.data.materials:
        b = m.node_tree.nodes.get("Principled BSDF") if m.use_nodes else None
        if b: mats[m.name] = {"col": [round(x, 3) for x in b.inputs["Base Color"].default_value[:3]], "met": round(b.inputs["Metallic"].default_value, 2), "rough": round(b.inputs["Roughness"].default_value, 2), "tex": any(n.type == "TEX_IMAGE" for n in m.node_tree.nodes)}
    # sample animated world transforms of animated objects
    samp = {}
    for o in bpy.data.objects:
        if o.animation_data and o.animation_data.action:
            fr = o.animation_data.action.frame_range; fs = list(range(int(fr[0]), int(fr[1]) + 1, 6))
            samp[o.name] = []
            for f in fs:
                sc.frame_set(f); samp[o.name].append([f, [round(x, 4) for x in o.matrix_world.translation], [round(x, 3) for x in o.matrix_world.to_euler()]])
    res[p] = {"objects": info, "actions": acts, "materials": mats, "samples": samp, "frame": [sc.frame_start, sc.frame_end]}
json.dump(res, open(OUT, "w"), indent=1); print("PROBE_DONE", flush=True)
