"""RoboRougelike six-style series 1: render shots from DELIVERED model files (GLB / FBX). RUNS INSIDE BLENDER (background).
One spec = many shots; every shot imports its models fresh, so what is rendered is exactly what was exported.
usage: blender -b --factory-startup --python s6render.py -- SPEC_JSON
spec: {"shots": [{"out": path, "res": [w, h], "cam": {"loc": [..], "target": [..], "lens": 50} | {"ortho": 1.2, ...}, "mode": "color"|"clay",
        "outline": true, "ground": "studio"|"arena", "samples": 32, "items": [{"file": path, "loc": [x, y, z], "rot_z": deg, "frame": 1, "scale": 1}]}]}"""
import bpy, bmesh, sys, os, json, math, time
from mathutils import Vector, Matrix, Euler
a = sys.argv[sys.argv.index("--") + 1:]; SPEC = json.load(open(a[0]))


def setup(shot):
    bpy.ops.wm.read_factory_settings(use_empty=True); sc = bpy.context.scene; col = sc.collection
    eng = [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]; sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in eng else 'BLENDER_EEVEE_NEXT'
    try: sc.eevee.taa_render_samples = shot.get("samples", 48); sc.eevee.use_shadows = True; sc.eevee.use_raytracing = True
    except Exception: pass
    sc.render.resolution_x, sc.render.resolution_y = shot.get("res", [1200, 1200]); sc.render.film_transparent = False
    vt = [i.identifier for i in sc.view_settings.bl_rna.properties['view_transform'].enum_items]; sc.view_settings.view_transform = "AgX" if "AgX" in vt else "Filmic"
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.36, 0.38, 0.42, 1) if shot.get("ground", "studio") == "studio" else (0.42, 0.46, 0.52, 1); bg.inputs[1].default_value = 0.9
    for (rot, e, c, ang) in (((math.radians(48), 0, math.radians(32)), 3.4, (1.0, 0.96, 0.90), 4), ((math.radians(62), 0, math.radians(-128)), 1.1, (0.78, 0.85, 1.0), 12), ((math.radians(-70), 0, math.radians(160)), 0.0, (1, 1, 1), 1)):
        if e <= 0: continue
        l = bpy.data.lights.new("sun", "SUN"); l.energy = e; l.color = c; l.angle = math.radians(ang); o = bpy.data.objects.new("sun", l); o.rotation_euler = rot; col.objects.link(o)
    gm = bpy.data.materials.new("ground"); gm.use_nodes = True; gb = gm.node_tree.nodes["Principled BSDF"]
    if shot.get("ground", "studio") == "studio": gb.inputs["Base Color"].default_value = (0.30, 0.31, 0.33, 1); gb.inputs["Roughness"].default_value = 0.85
    else:                                                                                            # arena floor: concrete tiles 1.1 m
        nt = gm.node_tree; ch = nt.nodes.new("ShaderNodeTexChecker"); ch.inputs["Scale"].default_value = 1.0; ch.inputs["Color1"].default_value = (0.27, 0.27, 0.28, 1); ch.inputs["Color2"].default_value = (0.31, 0.31, 0.32, 1)
        tc = nt.nodes.new("ShaderNodeTexCoord"); mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (1/2.2, 1/2.2, 1); nt.links.new(tc.outputs["Object"], mp.inputs[0]); nt.links.new(mp.outputs[0], ch.inputs[0])
        nt.links.new(ch.outputs[0], gb.inputs["Base Color"]); gb.inputs["Roughness"].default_value = 0.9
    g = bpy.data.meshes.new("g"); S = 40; g.from_pydata([(-S, -S, 0), (S, -S, 0), (S, S, 0), (-S, S, 0)], [], [(0, 1, 2, 3)]); go = bpy.data.objects.new("ground", g); col.objects.link(go); g.materials.append(gm)
    go.location.z = shot.get("ground_z", 0.0)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); col.objects.link(cam); sc.camera = cam; C = shot["cam"]
    if "ortho" in C: cam.data.type = "ORTHO"; cam.data.ortho_scale = C["ortho"]
    else: cam.data.lens = C.get("lens", 50)
    cam.data.clip_start = 0.02; cam.data.clip_end = 400
    cam.location = Vector(C["loc"]); cam.rotation_euler = (Vector(C["target"]) - Vector(C["loc"])).to_track_quat("-Z", "Y").to_euler()
    return sc, col


def load(item, sc):
    before = set(bpy.data.objects); f = item["file"]
    if f.lower().endswith(".glb") or f.lower().endswith(".gltf"): bpy.ops.import_scene.gltf(filepath=f)
    else: bpy.ops.import_scene.fbx(filepath=f, use_anim=True)
    new = [o for o in bpy.data.objects if o not in before]; roots = [o for o in new if o.parent is None]
    piv = bpy.data.objects.new("item", None); sc.collection.objects.link(piv)
    for r in roots: r.parent = piv
    piv.location = Vector(item.get("loc", (0, 0, 0))); piv.rotation_euler = (0, 0, math.radians(item.get("rot_z", 0.0))); s = item.get("scale", 1.0); piv.scale = (s, s, s)
    return new, item.get("frame", 1)


def main():
    for shot in SPEC["shots"]:
        t = time.time(); sc, col = setup(shot); frames = []; allnew = []
        for it in shot["items"]:
            new, fr = load(it, sc); allnew += new; frames.append((new, fr))
        # per-item frame: the scene frame is shared, so items that need another frame get their animation evaluated and frozen
        for new, fr in frames:
            sc.frame_set(fr); bpy.context.view_layer.update()
            for o in new:
                if o.animation_data: M = o.matrix_world.copy(); o.animation_data_clear(); o.matrix_world = M
        meshes = [o for o in allnew if o.type == "MESH"]
        if not shot.get("outline", True):                                                              # drop the inverted-hull faces
            for o in meshes:
                idx = [i for i, s in enumerate(o.material_slots) if s.material and s.material.name.startswith("R4P_Outline")]
                if idx:
                    bm = bmesh.new(); bm.from_mesh(o.data); bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index in idx], context="FACES"); bm.to_mesh(o.data); bm.free()
        for m in bpy.data.materials:
            if m.name.startswith("R4P_Outline"): m.use_backface_culling = True
            if m.name.startswith("Muzzle_Flash") or "Glow" in m.name: pass
        if shot.get("mode") == "clay":
            clay = bpy.data.materials.new("CLAY"); clay.use_nodes = True; cb = clay.node_tree.nodes["Principled BSDF"]; cb.inputs["Base Color"].default_value = (0.56, 0.56, 0.56, 1); cb.inputs["Roughness"].default_value = 0.62
            for o in meshes:
                for s in o.material_slots:
                    if not (s.material and s.material.name.startswith("R4P_Outline")): s.material = clay
        for o in meshes:                                                                              # the muzzle flash rest scale is 0.001 - hide it in stills
            if o.name.endswith("MuzzleFlash") and o.matrix_world.to_scale().x < 0.01: o.hide_render = True
        sc.render.filepath = shot["out"]; os.makedirs(os.path.dirname(shot["out"]), exist_ok=True)
        sc.render.image_settings.file_format = "PNG" if shot["out"].endswith(".png") else "JPEG"
        if not shot["out"].endswith(".png"): sc.render.image_settings.quality = 92
        bpy.ops.render.render(write_still=True); print("[S6R] SHOT", shot["out"], round(time.time() - t, 1), flush=True)
    print("[S6R] ALL_DONE", flush=True)


try: main()
except Exception:
    import traceback; traceback.print_exc(); print("[S6R] FAILED", flush=True)
