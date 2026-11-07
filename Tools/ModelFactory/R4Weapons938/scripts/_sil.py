# black-silhouette renders (side / front / top / gameplay) of rest-pose GLBs for the similarity check
import sys, os, math, bpy
from mathutils import Vector
a = sys.argv[sys.argv.index("--") + 1:]
OUT, GLBS = a[0], a[1:]
os.makedirs(OUT, exist_ok=True)
for g in GLBS:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    bpy.ops.import_scene.gltf(filepath=g)
    scn.frame_set(1)
    ms = [o for o in scn.objects if o.type == 'MESH']
    for o in ms:   # hide FX meshes (scaled to 0.001 at rest) and collider helpers
        if max(o.matrix_world.to_scale()) < 0.01: o.hide_render = True
    pts = [o.matrix_world @ Vector(c) for o in ms if not o.hide_render for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts))); hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c = (lo + hi)/2; ext = max(hi - lo)
    scn.render.engine = 'BLENDER_WORKBENCH'
    sh = scn.display.shading; sh.light = 'FLAT'; sh.color_type = 'SINGLE'; sh.single_color = (0, 0, 0); sh.background_type = 'VIEWPORT'
    sh.background_color = (1, 1, 1); sh.show_cavity = False; sh.show_shadows = False; sh.show_object_outline = False
    scn.view_settings.view_transform = 'Standard'
    scn.render.resolution_x = scn.render.resolution_y = 256; scn.render.film_transparent = False
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); scn.collection.objects.link(cam); scn.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ext*1.15
    nm = os.path.splitext(os.path.basename(g))[0]
    # glTF import: Y up -> Blender Z up, Unity +Z forward -> Blender -Y
    views = {"side": Vector((1, 0, 0)), "front": Vector((0, -1, 0)), "top": Vector((0, 0, 1)), "gameplay": Vector((0.55, -0.75, 0.95))}
    for v, d in views.items():
        cam.location = c + d.normalized()*3.0
        up = 'Y' if v != "top" else 'Y'
        cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y' if v != "top" else 'Y').to_euler()
        scn.render.filepath = os.path.join(OUT, f"{nm}_sil_{v}.png"); bpy.ops.render.render(write_still=True)
print("SIL_DONE")
