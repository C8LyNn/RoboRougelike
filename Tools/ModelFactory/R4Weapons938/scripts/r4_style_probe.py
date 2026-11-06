
"""R4 #938 STYLE LOCK: measure R41_Turret_v6.1 FBX (verified SHA) + v6 .blend: materials, colour samples, area ratio, tris, bounds."""
import bpy, bmesh, json, sys, os, hashlib, math
from mathutils import Vector
OUT = sys.argv[sys.argv.index("--")+1]
B = os.path.join(os.path.expanduser("~"), "Desktop", "Blender_Modele", "wynik")
FBX = os.path.join(B, "v6_1_wiezyczka", "R41_Turret_v6.1.fbx"); BLEND = os.path.join(B, "v6_wiezyczka", "R41_Turret_v6.blend")
sha = lambda p: hashlib.sha256(open(p,"rb").read()).hexdigest()
R = {"fbx": FBX, "fbx_sha256": sha(FBX), "blend": BLEND, "blend_sha256": sha(BLEND), "blender": bpy.app.version_string}
def survey(tag):
    mats = {}; area = {}; tris = 0; objs = {}
    for o in bpy.data.objects:
        objs[o.name] = dict(type=o.type, parent=o.parent.name if o.parent else None,
                            loc=[round(x,5) for x in o.location], rot_deg=[round(math.degrees(x),3) for x in o.rotation_euler])
        if o.type != "MESH" or o.name.startswith(("COL_","GRID","MuzzleFlash")) or "_WIRE" in o.name: continue
        if o.users_collection and any(c.name.startswith(("GRID","COLLIDERS","BEFORE")) for c in o.users_collection): continue
        me = o.data; me.calc_loop_triangles(); tris += len(me.loop_triangles)
        Mw = o.matrix_world
        for p in me.polygons:
            m = me.materials[p.material_index].name if me.materials else "none"
            vs = [Mw @ me.vertices[i].co for i in p.vertices]
            a = sum(((vs[0]-vs[i]).cross(vs[0]-vs[i+1])).length/2 for i in range(1,len(vs)-1))
            area[m] = area.get(m,0)+a
    for m in bpy.data.materials:
        b = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None
        if not b: continue
        e = b.inputs.get("Emission Color"); es = b.inputs.get("Emission Strength")
        mats[m.name] = dict(base=[round(x,4) for x in b.inputs["Base Color"].default_value[:3]],
                            metallic=round(b.inputs["Metallic"].default_value,3), roughness=round(b.inputs["Roughness"].default_value,3),
                            emission=[round(x,4) for x in e.default_value[:3]] if e else None, emission_strength=round(es.default_value,3) if es else None)
    tot = sum(area.values()) or 1
    return dict(materials=mats, area_m2={k: round(v,5) for k,v in sorted(area.items(), key=lambda kv:-kv[1])},
                area_share={k: round(v/tot,4) for k,v in sorted(area.items(), key=lambda kv:-kv[1])}, triangles=tris, objects=objs)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_anim=False)
R["fbx_survey"] = survey("fbx")
bpy.ops.wm.open_mainfile(filepath=BLEND, load_ui=False)
R["blend_survey"] = survey("blend")
R["blend_frame_range"] = [bpy.context.scene.frame_start, bpy.context.scene.frame_end, bpy.context.scene.render.fps]
R["blend_actions"] = [a.name for a in bpy.data.actions]
R["blend_custom_props"] = {o.name: {k: str(o[k]) for k in o.keys() if not k.startswith("_")} for o in bpy.data.objects if len(o.keys())}
json.dump(R, open(os.path.join(OUT, "R41_style_probe.json"), "w", encoding="utf-8"), indent=1)
print("STYLE_PROBE_DONE")
