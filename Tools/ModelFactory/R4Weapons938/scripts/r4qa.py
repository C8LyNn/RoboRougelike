# RoboRougelike #938 - shared QA / export / render pipeline (same rigour as R41 v6.1: real export, re-import, compare).
import bpy, bmesh, math, json, os, hashlib, time, traceback
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree
import r4lib as L

R41_FBX = os.path.join(os.path.expanduser("~"), "Desktop", "Blender_Modele", "wynik", "v6_1_wiezyczka", "R41_Turret_v6.1.fbx")
R41_SHA = "03c60486a6af5890f1106bc090d70bb3c09a67f8278e02c2942cab8e77845d6a"
sha = lambda p: hashlib.sha256(open(p, "rb").read()).hexdigest()
def fileco(v): return [round(v[0], 5), round(v[2], 5), round(-v[1], 5)]
def filedir(v): v = Vector(v).normalized(); return [round(v.x, 4), round(v.z, 4), round(-v.y, 4)]

FBX_OPTS = dict(use_selection=True, object_types={'EMPTY', 'MESH'}, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z',
                axis_up='Y', bake_space_transform=False, mesh_smooth_type='FACE', use_custom_props=True, add_leaf_bones=False,
                bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
                bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='AUTO')

def world_bm(ob, extra=Matrix()):
    bm = bmesh.new(); bm.from_mesh(ob.data); bm.transform(extra @ ob.matrix_world); return bm
def bvh(ob, extra=Matrix()):
    bm = world_bm(ob, extra); t = BVHTree.FromBMesh(bm); bm.free(); return t

def mesh_report(ob):
    bm = world_bm(ob)
    nonman = sum(1 for e in bm.edges if not e.is_manifold); boundary = sum(1 for e in bm.edges if e.is_boundary)
    nm_s = sorted({tuple(round(c, 3) for c in (e.verts[0].co + e.verts[1].co)/2) for e in bm.edges if not e.is_manifold})[:6]
    zero_f = sum(1 for f in bm.faces if f.calc_area() < 1e-9)
    kd = KDTree(len(bm.verts))
    for i, v in enumerate(bm.verts): kd.insert(v.co, i)
    kd.balance(); dups = sum(1 for v in bm.verts if len(kd.find_range(v.co, 1e-6)) > 1)
    # orientation consistency: both loops of every 2-face edge must run in opposite directions (exact, no heuristic)
    flipped = sum(1 for e in bm.edges if len(e.link_loops) == 2 and e.link_loops[0].vert == e.link_loops[1].vert)
    seen = set(); shells = 0
    for f in bm.faces:
        if f.index in seen: continue
        shells += 1; stack = [f]; seen.add(f.index)
        while stack:
            g = stack.pop()
            for e in g.edges:
                for hf in e.link_faces:
                    if hf.index not in seen: seen.add(hf.index); stack.append(hf)
    me = ob.data; uvb = None
    if me.uv_layers:
        arr = np.empty(len(me.loops)*2, np.float32); me.uv_layers[0].data.foreach_get("uv", arr)
        uvb = [round(float(arr[0::2].min()), 3), round(float(arr[1::2].min()), 3), round(float(arr[0::2].max()), 3), round(float(arr[1::2].max()), 3)]
    r = dict(object=ob.name, verts=len(bm.verts), triangles=sum(len(f.verts) - 2 for f in bm.faces), shells=shells,
             non_manifold_edges=nonman, boundary_edges=boundary, zero_area_faces=zero_f, duplicate_verts=dups,
             nonmanifold_samples_blender=nm_s, inconsistent_normals=flipped, signed_volume_m3=round(bm.calc_volume(signed=True), 6),
             uv_layers=len(me.uv_layers), uv_bounds=uvb, materials=[m.name for m in me.materials if m])
    bm.free()
    r["ok"] = nonman == 0 and boundary == 0 and zero_f == 0 and flipped == 0 and r["signed_volume_m3"] > 0 and r["uv_layers"] > 0
    return r

def palette_report(meshes):
    area = {}; bad = []
    for o in meshes:
        me = o.data; Mw = o.matrix_world
        for p in me.polygons:
            m = me.materials[p.material_index].name if me.materials else "none"
            vs = [Mw @ me.vertices[i].co for i in p.vertices]
            area[m] = area.get(m, 0) + sum(((vs[0]-vs[i]).cross(vs[0]-vs[i+1])).length/2 for i in range(1, len(vs)-1))
    for m in bpy.data.materials:
        if m.users == 0 or m.name not in area: continue
        if m.name not in L.LOCKED: bad.append(f"unlocked material {m.name}"); continue
        d = L.LOCKED[m.name]; b = m.node_tree.nodes["Principled BSDF"]
        if max(abs(a - c) for a, c in zip(b.inputs["Base Color"].default_value[:3], d["rgb"])) > 1e-4: bad.append(f"{m.name} colour drift")
        if abs(b.inputs["Metallic"].default_value - d["metal"]) > 1e-4 or abs(b.inputs["Roughness"].default_value - d["rough"]) > 1e-4: bad.append(f"{m.name} PBR drift")
    tot = sum(area.values()) or 1
    fam = {}
    for k, v in area.items(): fam[L.FAMILY.get(k, "?")] = fam.get(L.FAMILY.get(k, "?"), 0) + v/tot
    fam = {k: round(v, 4) for k, v in sorted(fam.items())}
    band = {k: (lo <= fam.get(k, 0) <= hi) for k, (lo, hi) in L.BANDS.items()}
    return dict(material_area_share={k: round(v/tot, 4) for k, v in sorted(area.items(), key=lambda kv: -kv[1])},
                family_share=fam, r41_family_share=L.R41_SHARE, bands=L.BANDS, in_band=band, violations=bad,
                PASS=not bad and all(band.values()))

def Ryaw(deg, P=L.P): return Matrix.Translation((0, 0, P)) @ Matrix.Rotation(math.radians(deg), 4, 'Z') @ Matrix.Translation((0, 0, -P))
def Rpit(up, P=L.P): return Matrix.Translation((0, 0, P)) @ Matrix.Rotation(math.radians(-up), 4, 'X') @ Matrix.Translation((0, 0, -P))   # +up = muzzle up (R41)

def cells_of(points):
    q = np.floor((np.asarray(points) + L.CELL_HALF)/L.CELL).astype(int)
    return sorted(set(map(tuple, np.unique(q, axis=0).tolist())))
def verts_file(ob, M=Matrix()):
    me = ob.data; co = np.empty(len(me.vertices)*3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    W = np.array(M @ ob.matrix_world); p = co @ W[:3, :3].T + W[:3, 3]
    return np.stack([p[:, 0], p[:, 2], -p[:, 1]], 1)

def sweep_report(S):
    """static_occupied / mount_socket / moving_sweep / fire_exit / line_of_fire, measured in 3D (0.55 grid, file coords)"""
    R = {}
    dn, up = S["pitch_range"]; yaws = list(range(0, 360, 10))
    elevs = list(range(-int(dn), int(up) + 1, 2)) if S.get("pitch") else [0]
    st = [bvh(o) for o in S["static"]]
    def gun_at(M): return [bvh(o, M) for o in S["gun"]]
    bad = []
    for yd in yaws:
        yb = [bvh(o, Ryaw(yd)) for o in S["yawparts"]]
        for e in elevs:
            g = gun_at(Ryaw(yd) @ Rpit(e))
            if any(a.overlap(b) for a in st + yb for b in g): bad.append((yd, e))
        if any(a.overlap(b) for a in st for b in yb): bad.append((yd, "yaw_vs_base"))
    R["aim_sweep"] = dict(yaw_deg="0..350 step 10", elevation_deg=f"{-dn}..{up} step 2", collisions=len(bad), samples=bad[:12])
    stat = np.vstack([verts_file(o) for o in S["static"]])
    rest = np.vstack([verts_file(o) for o in S["static"] + S["yawparts"] + S["gun"]])
    mov = []
    for yd in yaws:
        for e in (elevs[0], 0, elevs[-1]):
            mov.append(np.vstack([verts_file(o, Ryaw(yd)) for o in S["yawparts"]] + [verts_file(o, Ryaw(yd) @ Rpit(e)) for o in S["gun"]]))
    mov = np.vstack(mov)
    R["static_occupied"] = dict(cells=cells_of(stat), bounds_file=[stat.min(0).round(4).tolist(), stat.max(0).round(4).tolist()],
                                inside_own_cell=bool((np.abs(stat) <= L.CELL_HALF + 1e-4).all()))
    R["rest_pose"] = dict(cells=cells_of(rest), bounds_file=[rest.min(0).round(4).tolist(), rest.max(0).round(4).tolist()])
    R["moving_sweep"] = dict(cells=cells_of(mov), bounds_file=[mov.min(0).round(4).tolist(), mov.max(0).round(4).tolist()],
                             max_radius_from_pivot_axis_m=round(float(np.hypot(mov[:, 0], mov[:, 2]).max()), 4),
                             neighbour_cells_entered=[c for c in cells_of(mov) if c != (0, 0, 0)])
    yawv = np.vstack([verts_file(o) for o in S["yawparts"]]) if S["yawparts"] else np.zeros((1, 3))
    R["yaw_body_max_radius_m"] = round(float(np.hypot(yawv[:, 0], yawv[:, 2]).max()), 4)
    R["yaw_body_inside_cell_all_yaw"] = R["yaw_body_max_radius_m"] <= L.CELL_HALF + 1e-4
    so = S["socket"].matrix_world
    R["mount_socket"] = dict(position_file=fileco(so.translation), normal_file=filedir(so.to_3x3() @ Vector((0, 0, 1))),
                             on_cell_bottom_face=abs(so.translation.z - L.Z_PL0) < 1e-6)
    # line of fire / fire exit for every muzzle across the elevation range
    lof = []
    allm = S["static"] + S["yawparts"] + S["gun"]
    if S.get("lof_setup"): S["lof_setup"](True); bpy.context.view_layer.update()
    for mz in S["muzzles"]:
        for e in sorted(set([elevs[0], 0, elevs[-1], 15])) if S.get("pitch") else [0]:
            Mx = Rpit(e) if S.get("pitch") else Matrix()
            trees = [bvh(o, Mx if o in S["gun"] else Matrix()) for o in allm]
            o_ = Mx @ mz.matrix_world.translation; d_ = (Mx.to_3x3() @ mz.matrix_world.to_3x3() @ Vector((0, 0, 1))).normalized()
            hit = None
            for t in trees:
                h = t.ray_cast(o_ + d_*1e-4, d_, 6.0)
                if h[0] is not None and (hit is None or h[3] < hit): hit = h[3]
            back = None
            for t in trees:
                h = t.ray_cast(o_ - d_*1e-4, -d_, 0.02)
                if h[0] is not None: back = round(h[3], 4)
            pts = [o_ + d_*(0.05*i) for i in range(0, 61)]
            lof.append(dict(muzzle=mz.name, elevation=e, origin_file=fileco(o_), dir_file=filedir(d_), own_hit_within_6m=hit,
                            exit_clear=hit is None, cells=cells_of([fileco(p) for p in pts])))
    if S.get("lof_setup"): S["lof_setup"](False); bpy.context.view_layer.update()
    R["line_of_fire"] = lof
    R["line_of_fire_pose"] = S.get("lof_pose_note", "rest pose")
    R["fire_exit_clear_all"] = all(x["exit_clear"] for x in lof)
    return R

def timeline_clash(S, scn):
    out = []; worst = {}
    for f in range(scn.frame_start, scn.frame_end + 1):
        scn.frame_set(f)
        for a, b in S["clash_pairs"]:
            if bvh(a).overlap(bvh(b)):
                out.append((f, a.name, b.name)); worst[f"{a.name}|{b.name}"] = worst.get(f"{a.name}|{b.name}", 0) + 1
    scn.frame_set(1)
    return dict(frames_checked=scn.frame_end - scn.frame_start + 1, pairs=[[a.name, b.name] for a, b in S["clash_pairs"]],
                collisions=len(out), samples=out[:12], per_pair=worst, PASS=not out)

def snap(names, frame):
    sc = bpy.context.scene; sc.frame_set(frame); bpy.context.view_layer.update(); out = {}
    for n in names:
        o = bpy.data.objects.get(n)
        if o is None: continue
        d = dict(M=o.matrix_world.copy(), parent=o.parent.name if o.parent else None)
        if o.type == "MESH": d["V"] = [o.matrix_world @ v.co for v in o.data.vertices]
        out[n] = d
    return out
def kd_max(a, b):
    def one(src, dst):
        kd = KDTree(len(dst))
        for i, p in enumerate(dst): kd.insert(p, i)
        kd.balance(); return max(kd.find(p)[2] for p in src)
    return max(one(a, b), one(b, a))
def rot_deg(A, B):
    a = math.degrees(A.to_quaternion().rotation_difference(B.to_quaternion()).angle) % 360.0
    return min(a, 360 - a)
def compare(ref, got):
    w = dict(pos_mm=0.0, rot_deg=0.0, scale_err=0.0, geom_mm=0.0, missing=[], parent_bad=[])
    for n, r in ref.items():
        g = got.get(n)
        if g is None: w["missing"].append(n); continue
        w["pos_mm"] = max(w["pos_mm"], (r["M"].translation - g["M"].translation).length*1000)
        w["rot_deg"] = max(w["rot_deg"], rot_deg(r["M"], g["M"]))
        w["scale_err"] = max(w["scale_err"], max(abs(a - b) for a, b in zip(r["M"].to_scale(), g["M"].to_scale())))
        if r["parent"] != g["parent"]: w["parent_bad"].append(n)
        if "V" in r and g.get("V") and r["M"].to_scale().length > 0.01:
            w["geom_mm"] = max(w["geom_mm"], kd_max(r["V"], g["V"])*1000)
    for k in ("pos_mm", "rot_deg", "geom_mm"): w[k] = round(w[k], 4)
    w["scale_err"] = round(w["scale_err"], 6)
    return w

def select(objs):
    vl = bpy.context.view_layer
    for o in list(vl.objects): o.select_set(False)
    for o in objs: o.hide_set(False); o.select_set(True)
    vl.objects.active = objs[0]

# ---------------- renders (Workbench like r41_v61_render.py + EEVEE hero) --------------------------------
def setup_workbench(scn):
    scn.render.engine = "BLENDER_WORKBENCH"
    sh = scn.display.shading
    sh.light, sh.color_type, sh.show_cavity, sh.show_object_outline, sh.show_shadows = "STUDIO", "MATERIAL", True, True, True
    try: scn.view_settings.view_transform = "Standard"
    except Exception: pass
    for m in bpy.data.materials:
        try:
            c = m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value
            m.diffuse_color = (c[0]**(1/2.2), c[1]**(1/2.2), c[2]**(1/2.2), 1)
        except Exception: pass
    if scn.world is None: scn.world = bpy.data.worlds.new("w")
    scn.world.color = (0.16, 0.17, 0.19)
    scn.render.film_transparent = False
    scn.render.use_stamp = True; scn.render.use_stamp_note = True
    for k in ("use_stamp_date", "use_stamp_time", "use_stamp_render_time", "use_stamp_frame", "use_stamp_camera", "use_stamp_lens",
              "use_stamp_scene", "use_stamp_filename", "use_stamp_memory", "use_stamp_hostname"):
        if hasattr(scn.render, k): setattr(scn.render, k, False)
def camera(scn):
    cam = scn.camera
    if cam is None:
        cd = bpy.data.cameras.new("QA_Cam"); cd.clip_start = 0.01; cam = bpy.data.objects.new("QA_Cam", cd)
        scn.collection.objects.link(cam); scn.camera = cam
    return cam
FXHIDE = []
def shoot(scn, path, loc, tgt, ortho=None, note="", w=1100, h=820, lens=50):
    bpy.context.view_layer.update()
    for o in FXHIDE: o.hide_render = o.matrix_world.to_scale().x < 0.01
    cam = camera(scn); cd = cam.data
    if ortho: cd.type = "ORTHO"; cd.ortho_scale = ortho
    else: cd.type = "PERSP"; cd.lens = lens
    d = (Vector(loc) - Vector(tgt)).normalized()
    cam.location = Vector(tgt) + d*(6 if ortho else (Vector(loc) - Vector(tgt)).length)
    cam.rotation_euler = (Vector(tgt) - cam.location).to_track_quat("-Z", "Y").to_euler()
    scn.render.resolution_x, scn.render.resolution_y = w, h; scn.render.stamp_note_text = note
    scn.render.filepath = path; bpy.ops.render.render(write_still=True)
    return os.path.basename(path)
def view_set(c, r):
    """c = target centre (Blender), r = radius; standard turnaround (Blender -Y is the front)"""
    t = c
    return {"front": ((t[0], t[1] - 3, t[2]), t, 2.3*r), "back": ((t[0], t[1] + 3, t[2]), t, 2.3*r),
            "side": ((t[0] + 3, t[1], t[2]), t, 2.3*r), "top": ((t[0], t[1] - 0.0001, t[2] + 3), t, 2.3*r),
            "34front": ((t[0] - 1.5*r*1.05, t[1] - 2.0*r*1.05, t[2] + 1.1*r*1.05), t, None),
            "34rear": ((t[0] + 1.6*r*1.05, t[1] + 1.7*r*1.05, t[2] + 1.2*r*1.05), t, None)}

def bounds_center_radius(objs):
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return (lo + hi)/2, max((hi - lo).length/2, 0.2)

def wire_copies(objs, col):
    wm = bpy.data.materials.new("_Wire_Overlay"); wm.diffuse_color = (0.02, 0.03, 0.035, 1); out = []
    for o in objs:
        w = bpy.data.objects.new(o.name + "_WIRE", o.data); col.objects.link(w)
        w.parent = o.parent; w.matrix_parent_inverse = o.matrix_parent_inverse.copy()
        w.location = o.location; w.rotation_euler = o.rotation_euler; w.scale = o.scale
        md = w.modifiers.new("wire", 'WIREFRAME'); md.thickness = 0.0010; md.use_replace = True; md.use_even_offset = False
        for sl in w.material_slots: sl.link = 'OBJECT'; sl.material = wm
        out.append(w)
    return out

def renders(S, scn, rd):
    os.makedirs(rd, exist_ok=True); done = []; FXHIDE[:] = list(S["fx"])
    setup_workbench(scn)
    hide = [o for o in bpy.data.objects if o.name.startswith("COL_") or o.name.startswith("SWEEP_")]
    for o in hide: o.hide_render = True
    c, r = bounds_center_radius(S["parts"])
    tag = f'{S["id"]} | {S["kind"]}'
    for n, (loc, tgt, orth) in view_set(c, r).items():
        done.append(shoot(scn, os.path.join(rd, f'{S["id"]}_{n}.png'), loc, tgt, orth, f"{tag} | {n} | Blender {bpy.app.version_string}"))
    col = L.new_col("_wire"); ww = wire_copies(S["parts"], col)
    for o in S["parts"] + S["fx"]: o.hide_render = True
    v = view_set(c, r)
    done.append(shoot(scn, os.path.join(rd, f'{S["id"]}_34front_wireframe.png'), v["34front"][0], c, None, tag + " | wireframe"))
    done.append(shoot(scn, os.path.join(rd, f'{S["id"]}_side_wireframe.png'), v["side"][0], c, v["side"][2], tag + " | wireframe side"))
    for w in ww: bpy.data.objects.remove(w, do_unlink=True)
    bpy.data.collections.remove(col)
    for o in S["parts"] + S["fx"]: o.hide_render = False
    # collider + sweep preview
    for o in hide: o.hide_render = False
    done.append(shoot(scn, os.path.join(rd, f'{S["id"]}_colliders_sweep.png'), v["34front"][0], c, None, tag + " | colliders (yellow) + sweep volumes"))
    for o in hide: o.hide_render = True
    # key animation poses
    pd = os.path.join(rd, "poses"); os.makedirs(pd, exist_ok=True)
    for cl in S["clips"]:
        for fr in cl.get("render_frames", [(cl["start"] + cl["end"])//2]):
            scn.frame_set(fr)
            cc, rr = bounds_center_radius(S["parts"])
            loc = S.get("pose_cam", v["34front"][0])
            done.append("poses/" + shoot(scn, os.path.join(pd, f'{S["id"]}_{cl["name"]}_f{fr:03d}.png'), loc, c, None,
                                         f'{tag} | clip {cl["name"]} | frame {fr}', 900, 680))
    scn.frame_set(1)
    # EEVEE hero
    try:
        scn.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
        try: scn.eevee.taa_render_samples = 48
        except Exception: pass
        scn.world.use_nodes = True
        bg = scn.world.node_tree.nodes.get("Background")
        if bg: bg.inputs[0].default_value = (0.01, 0.012, 0.02, 1); bg.inputs[1].default_value = 1.0
        lights = []
        for nm, loc_, en, col_ in (("Key", (-1.2, -1.4, 1.6), 110, (1.0, 0.95, 0.9)), ("Rim", (1.0, 1.3, 0.9), 160, (0.4, 0.8, 1.0)), ("Fill", (1.6, -1.0, 0.2), 30, (1.0, 0.5, 0.4))):
            ld = bpy.data.lights.new(nm, 'AREA'); ld.energy = en; ld.color = col_; ld.size = 0.8
            lo = bpy.data.objects.new(nm, ld); scn.collection.objects.link(lo); lo.location = loc_
            lo.rotation_euler = (Vector((0, -0.15, 0.1)) - Vector(loc_)).to_track_quat('-Z', 'Y').to_euler(); lights.append(lo)
        fm = bpy.data.materials.new("_Floor"); fm.use_nodes = True
        fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.03, 0.035, 0.04, 1)
        fl = L.box("_floor", -9, 9, -9, 9, -0.27, -0.2601, fm, scn.collection)
        hf = S.get("hero_frame", 1); scn.frame_set(hf)
        hl = Vector(c) + (Vector(v["34front"][0]) - Vector(c))*1.05
        done.append(shoot(scn, os.path.join(rd, f'{S["id"]}_HERO_eevee.png'), hl, c, None, "", 1400, 1050))
        if S.get("hero_frame2"):
            scn.frame_set(S["hero_frame2"]); done.append(shoot(scn, os.path.join(rd, f'{S["id"]}_HERO2_eevee.png'), Vector(c) + (Vector(v["34rear"][0]) - Vector(c))*1.05, c, None, "", 1400, 1050))
        scn.frame_set(1)
        for lo in lights: bpy.data.objects.remove(lo, do_unlink=True)
        bpy.data.objects.remove(fl, do_unlink=True)
    except Exception:
        L.log("EEVEE ERROR\n" + traceback.format_exc())
    scn.render.engine = "BLENDER_WORKBENCH"
    return done

def colliders(S, colc):
    """simplified box colliders per rigid part, in the part's local space (R41 v6 convention)"""
    out = []; mat = bpy.data.materials.new("Collider_Preview"); mat.diffuse_color = (1.0, 0.85, 0.2, 1)
    for o in S["parts"]:
        co = [v.co for v in o.data.vertices]
        lo = Vector((min(c.x for c in co), min(c.y for c in co), min(c.z for c in co))); hi = Vector((max(c.x for c in co), max(c.y for c in co), max(c.z for c in co)))
        b = L.box("COL_" + o.name, lo.x, hi.x, lo.y, hi.y, lo.z, hi.z, mat, colc)
        b.parent = o; b.display_type = 'WIRE'; out.append(b)
    return out

def run(S):
    """S: spec dict from the weapon builder. Writes everything into S['out']."""
    t0 = time.time(); OUT = S["out"]; os.makedirs(OUT, exist_ok=True)
    scn = bpy.context.scene; R = dict(id=S["id"], kind=S["kind"], game_name=S["game_name"], blender=bpy.app.version_string,
                                      started=time.strftime("%Y-%m-%d %H:%M:%S"), failures=[], notes=S.get("notes", []))
    try:
        scn.frame_start, scn.frame_end = 1, S["frame_end"]
        for o in S["fx"]: o.scale = (1, 1, 1)
        bpy.context.view_layer.update()
        R["meshes"] = [mesh_report(o) for o in S["parts"]]
        R["fx_meshes"] = [mesh_report(o) for o in S["fx"]]
        for o in S["fx"]: o.scale = (0.001,)*3
        scn.frame_set(1); bpy.context.view_layer.update()
        R["triangles_total"] = sum(m["triangles"] for m in R["meshes"]); R["triangles_fx"] = sum(m["triangles"] for m in R["fx_meshes"])
        R["triangles_r41_reference"] = 11452
        R["topology_PASS"] = all(m["ok"] for m in R["meshes"] + R["fx_meshes"]) and R["triangles_total"] <= S.get("tri_budget", 22000)
        if not R["topology_PASS"]: R["failures"].append("topology")
        R["palette"] = palette_report(S["parts"])
        if not R["palette"]["PASS"]: R["failures"].append("palette")
        R["contract"] = {o.name: dict(position_file=fileco(o.matrix_world.translation), forward_file=filedir(o.matrix_world.to_3x3() @ Vector((0, 0, 1))),
                                      parent=o.parent.name if o.parent else None) for o in S["export"] if o.type == 'EMPTY'}
        R["contract"]["unity_axes"] = "file/Unity: +Z forward, +Y up, meters; Yaw about local Y, Pitch about local X"
        for mz in S["muzzles"]:
            fwd = filedir(mz.matrix_world.to_3x3() @ Vector((0, 0, 1)))
            exp = S.get("muzzle_forward", [0, 0, 1])
            if max(abs(a - b) for a, b in zip(fwd, exp)) > 1e-3: R["failures"].append(f"muzzle axis {mz.name}")
        pv = S["pivot"].matrix_world.translation
        if (pv - Vector((0, 0, L.P))).length > 1e-6: R["failures"].append("pivot")
        if S.get("yaw") is not None and (S["yaw"].matrix_world.translation - pv).length > 1e-6: R["failures"].append("yaw origin")
        if S.get("pitch") is not None and (S["pitch"].matrix_world.translation - pv).length > 1e-6: R["failures"].append("pitch origin")
        R["sweep"] = sweep_report(S)
        if R["sweep"]["aim_sweep"]["collisions"]: R["failures"].append("aim sweep collision")
        if not R["sweep"]["static_occupied"]["inside_own_cell"]: R["failures"].append("static outside cell")
        if not R["sweep"]["yaw_body_inside_cell_all_yaw"]: R["failures"].append("yaw body outside cell")
        if not S.get("skip_lof") and not R["sweep"]["fire_exit_clear_all"]: R["failures"].append("line of fire blocked")
        R["animation_clash"] = timeline_clash(S, scn)
        if not R["animation_clash"]["PASS"]: R["failures"].append("animation clash")
        R["extra_checks"] = S.get("extra_checks", lambda: {})()
        for k, v in R["extra_checks"].items():
            if isinstance(v, dict) and v.get("PASS") is False: R["failures"].append("extra:" + k)
        L.write_manifests(S, R)
        colc = L.new_col("COLLIDERS"); cols = colliders(S, colc) + [o for o in bpy.data.objects if o.name.startswith("SWEEP_")]
        R["renders"] = renders(S, scn, os.path.join(OUT, "renders"))
        # ---------------- save + export ------------------------------------------------------------
        scn.frame_set(1)
        for o in S["fx"]: o.scale = (0.001,)*3
        BL = os.path.join(OUT, S["id"] + ".blend"); FBX = os.path.join(OUT, S["id"] + ".fbx"); GLB = os.path.join(OUT, S["id"] + ".glb")
        CGLB = os.path.join(OUT, S["id"] + "_colliders.glb"); LOD = os.path.join(OUT, S["id"] + "_LOD1.fbx")
        for o in bpy.data.objects:
            if o.name.startswith("QA_Cam"): o.hide_render = True
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=BL, copy=True, compress=False)
        select(S["export"]); bpy.ops.export_scene.fbx(filepath=FBX, **FBX_OPTS)
        select(S["export"])
        kw = dict(filepath=GLB, export_format='GLB', use_selection=True, export_yup=True, export_apply=True, export_extras=True,
                  export_animations=True, export_animation_mode='SCENE', export_force_sampling=True, export_frame_range=True)
        try: bpy.ops.export_scene.gltf(**kw)
        except TypeError:
            for k in ("export_animation_mode", "export_force_sampling", "export_frame_range"): kw.pop(k, None)
            bpy.ops.export_scene.gltf(**kw)
        select([S["root"], S["pivot"]] + [o for o in S["export"] if o.type == 'MESH' and o not in S["fx"]] + cols)
        bpy.ops.export_scene.gltf(filepath=CGLB, export_format='GLB', use_selection=True, export_yup=True, export_apply=True, export_animations=False)
        # auto LOD1 (collapse-decimate copies; separate file, same names + _LOD1)
        lods = []; lodc = L.new_col("_LOD1"); ltri = 0
        for o in S["parts"]:
            c_ = o.copy(); c_.data = o.data.copy(); c_.name = o.name + "_LOD1"; lodc.objects.link(c_); c_.matrix_world = o.matrix_world.copy()
            c_.parent = None; c_.animation_data_clear()
            md = c_.modifiers.new("dec", 'DECIMATE'); md.ratio = 0.5; md.use_collapse_triangulate = True
            dg = bpy.context.evaluated_depsgraph_get(); me = bpy.data.meshes.new_from_object(c_.evaluated_get(dg), depsgraph=dg)
            c_.modifiers.clear(); c_.data = me; ltri += len(me.polygons); lods.append(c_)
        select(lods); bpy.ops.export_scene.fbx(filepath=LOD, **dict(FBX_OPTS, bake_anim=False))
        R["lod1"] = dict(file=os.path.basename(LOD), triangles=ltri, method="DECIMATE collapse 0.5 (automatic, not hand-authored)")
        names = [o.name for o in S["export"]]; ROOTN = S["root"].name; FXN = {o.name for o in S["fx"]}
        fr = sorted(set([1, S["frame_end"]] + [f for c in S["clips"] for f in (c["start"], (c["start"] + c["end"])//2, c["end"])]))
        # reference snapshot = the saved .blend
        FXHIDE[:] = []
        bpy.ops.wm.open_mainfile(filepath=BL, load_ui=False)
        REF = {f: snap(names, f) for f in fr}
        # ---------------- FBX re-import (Blender) ---------------------------------------------------
        bpy.ops.wm.read_factory_settings(use_empty=True); bpy.context.scene.render.fps = 24
        bpy.ops.import_scene.fbx(filepath=FBX, use_anim=True, anim_offset=0.0)
        agg = dict(pos_mm=0.0, rot_deg=0.0, geom_mm=0.0, scale_err=0.0, missing=set(), parent_bad=set()); per = {}
        for f in fr:
            w = compare(REF[f], snap(names, f)); per[f] = w
            for k in ("pos_mm", "rot_deg", "geom_mm", "scale_err"): agg[k] = max(agg[k], w[k])
            agg["missing"] |= set(w["missing"]); agg["parent_bad"] |= set(w["parent_bad"])
        agg["missing"] = sorted(agg["missing"]); agg["parent_bad"] = sorted(agg["parent_bad"])
        nodes = {n: dict(loc=[round(x, 5) for x in bpy.data.objects[n].location], rot_deg=[round(math.degrees(x), 3) for x in bpy.data.objects[n].rotation_euler],
                         parent=bpy.data.objects[n].parent.name if bpy.data.objects[n].parent else None) for n in names if n in bpy.data.objects}
        fbx_pass = agg["pos_mm"] < 0.05 and agg["rot_deg"] < 0.01 and agg["geom_mm"] < 0.05 and not agg["missing"] and not agg["parent_bad"]
        R["fbx_reimport"] = dict(frames=fr, worst=agg, per_frame=per, nodes_as_imported=nodes, PASS=fbx_pass,
                                 thresholds="pos<0.05mm rot<0.01deg geom<0.05mm, 0 missing, 0 parent changes (R41 v6.1 QA)")
        if not fbx_pass: R["failures"].append("fbx reimport")
        # comparison render: exported FBX next to the R41 v6.1 FBX (both imported)
        try:
            if os.path.exists(R41_FBX) and sha(R41_FBX) == R41_SHA:
                before = set(bpy.data.objects)
                bpy.ops.import_scene.fbx(filepath=R41_FBX, use_anim=False)
                r41 = [o for o in bpy.data.objects if o not in before]
                for o in r41:
                    if o.parent is None: o.location.x += S.get("compare_offset", 0.78)
                    if o.name.startswith("MuzzleFlash"): o.hide_render = True
                scn = bpy.context.scene; scn.frame_set(1); setup_workbench(scn)
                for o in bpy.data.objects:
                    if "Flash" in o.name or o.name in FXN: o.hide_render = True
                off = S.get("compare_offset", 0.78)/2
                R["renders"].append(shoot(scn, os.path.join(OUT, "renders", f'{S["id"]}_vs_R41_34front.png'), (off - 1.4, -1.9, 0.95), (off, -0.15, 0.05), None,
                                          f'{S["id"]} (left) vs R41_Turret_v6.1 (right) | both FBX imported', 1300, 800))
                R["renders"].append(shoot(scn, os.path.join(OUT, "renders", f'{S["id"]}_vs_R41_side.png'), (off, -3, 0.05), (off, -0.15, 0.05), 1.9*max(1, S.get("compare_offset", 0.78)),
                                          f'{S["id"]} (left) vs R41 (right) | front ortho', 1300, 700))
                R["r41_compare"] = "rendered from real R41_Turret_v6.1.fbx (SHA verified)"
            else: R["r41_compare"] = "REFERENCE_BLOCKED (sha mismatch)"
        except Exception:
            R["r41_compare"] = "ERROR " + traceback.format_exc()[-400:]; R["failures"].append("r41 compare render")
        # ---------------- GLB re-import (rest pose + animation present) ---------------------------------
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=GLB)
        sc = bpy.context.scene; sc.frame_set(1)
        got = {}
        for n in names:
            o = bpy.data.objects.get(n)
            if o: got[n] = dict(M=o.matrix_world.copy(), parent=o.parent.name if o.parent else None, V=[o.matrix_world @ v.co for v in o.data.vertices] if o.type == 'MESH' else None)
        # glTF importer may add an extra parent for Y-up conversion: compare world matrices only
        w = compare({k: dict(v, parent=None) for k, v in REF[1].items()}, {k: dict(v, parent=None) for k, v in got.items()})
        anims = [a.name for a in bpy.data.actions]
        glb_pass = w["pos_mm"] < 0.1 and w["rot_deg"] < 0.02 and w["geom_mm"] < 0.1 and not w["missing"] and len(anims) > 0
        R["glb_reimport"] = dict(rest_frame_worst=w, actions_imported=len(anims), PASS=glb_pass, note="glTF importer; world-space comparison at frame 1")
        if not glb_pass: R["failures"].append("glb reimport")
        R["files"] = {os.path.basename(p): dict(sha256=sha(p), bytes=os.path.getsize(p)) for p in (BL, FBX, GLB, CGLB, LOD)}
        R["unity_import_test"] = "UNITY_NOT_RUN (Unity not available in this session)"
        R["unity_import_settings"] = "Scale Factor 1, Convert Units ON, Bake Axis Conversion ON, Animation: split clips by animation_manifest.json frame ranges (24 fps)"
    except Exception:
        R["failures"].append("exception"); R["error"] = traceback.format_exc()
    R["status"] = "BLENDER_PASS" if not R["failures"] else "HOLD"
    R["seconds"] = round(time.time() - t0, 1)
    json.dump(R, open(os.path.join(OUT, S["id"] + "_QA.json"), "w", encoding="utf-8"), indent=1, default=str)
    open(os.path.join(OUT, "build_log.txt"), "w", encoding="utf-8").write("\n".join(L.LOG))
    print("R4QA_DONE", S["id"], R["status"], R["failures"])
    return R
