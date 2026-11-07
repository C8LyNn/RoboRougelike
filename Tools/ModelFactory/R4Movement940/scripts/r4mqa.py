# RoboRougelike #940 - movement QA: mount cell, static bounds, moving sweep, collision envelope vs runtime collider,
# ground contact, clearance, quarter-turn + mirror tests, floating shells, silhouettes, MP4 of the real timeline.
import bpy, bmesh, math, os, json
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
import r4lib as L

def fileco(v): return [round(v[0], 5), round(v[2], 5), round(-v[1], 5)]
def verts_world(o):
    me = o.data; co = np.empty(len(me.vertices)*3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    W = np.array(o.matrix_world); return co @ W[:3, :3].T + W[:3, 3]
def to_file(p): return np.stack([p[:, 0], p[:, 2], -p[:, 1]], 1)
def cells_of(pf):
    q = np.floor((pf + L.CELL_HALF)/L.CELL).astype(int); return sorted(set(map(tuple, np.unique(q, axis=0).tolist())))
def bounds(pf): return [pf.min(0).round(4).tolist(), pf.max(0).round(4).tolist()]
def bvh(o):
    bm = bmesh.new(); bm.from_mesh(o.data); bm.transform(o.matrix_world); t = BVHTree.FromBMesh(bm); bm.free(); return t

def floating_shells(o):
    """shells of one mesh that touch no other shell of the same mesh (bbox +0.5 mm) - a levitating detail"""
    bm = bmesh.new(); bm.from_mesh(o.data); seen = set(); boxes = []
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        if f.index in seen: continue
        stack = [f]; seen.add(f.index); vs = []
        while stack:
            g = stack.pop(); vs += [v.co.copy() for v in g.verts]
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen: seen.add(h.index); stack.append(h)
        a = np.array(vs); boxes.append((a.min(0) - 0.0005, a.max(0) + 0.0005))
    bm.free()
    if len(boxes) < 2: return 0, len(boxes)
    iso = 0
    for i, (lo, hi) in enumerate(boxes):
        if not any(np.all(lo <= h2) and np.all(l2 <= hi) for j, (l2, h2) in enumerate(boxes) if j != i): iso += 1
    return iso, len(boxes)

def min_gap(a, b, max_v=500):
    tb = bvh(b); co = verts_world(a)
    if len(co) > max_v: co = co[np.linspace(0, len(co) - 1, max_v).astype(int)]
    d = 9.0
    for p in co:
        h = tb.find_nearest(Vector(p))
        if h[0] is not None: d = min(d, h[3])
    return d

def movement_report(S, scn):
    R = {"failures": []}
    parts = S["parts"]; frames = list(range(scn.frame_start, scn.frame_end + 1))
    scn.frame_set(1); bpy.context.view_layer.update()
    rest = to_file(np.vstack([verts_world(o) for o in parts]))
    R["mount_cell"] = [0, 0, 0]
    R["static_bounds_file"] = bounds(rest); R["static_cells"] = cells_of(rest)
    hous = to_file(np.vstack([verts_world(o) for o in S["housing"]]))
    R["housing_bounds_file"] = bounds(hous)
    R["housing_inside_mount_cell"] = bool((np.abs(hous) <= L.CELL_HALF + 1e-4).all())
    R["housing_policy"] = S.get("housing_note", "strict: static housing must stay inside the 0.55 m mount cell")
    if not R["housing_inside_mount_cell"] and S.get("housing_strict", True): R["failures"].append("housing outside mount cell")
    allp = []; gmin = {o.name: 9.0 for o in S.get("ground_parts", [])}
    for f in frames[::1]:
        scn.frame_set(f); bpy.context.view_layer.update()
        allp.append(to_file(np.vstack([verts_world(o) for o in parts])))
        for o in S.get("ground_parts", []): gmin[o.name] = min(gmin[o.name], float(verts_world(o)[:, 2].min()))
    mv = np.vstack(allp)
    R["moving_sweep"] = dict(bounds_file=bounds(mv), cells=cells_of(mv), neighbour_cells_entered=[c for c in cells_of(mv) if c != (0, 0, 0)], frames=len(frames))
    scn.frame_set(1); bpy.context.view_layer.update()
    # ground contact (file Y) vs runtime placeholder
    gc = S.get("ground_contact")
    if gc:
        lowest_rest = min(float(verts_world(o)[:, 2].min()) for o in S["ground_parts"])
        R["ground_contact"] = dict(runtime_y=gc["runtime_y"], model_y_rest=round(lowest_rest, 4), model_y_min_over_clips=round(min(gmin.values()), 4),
                                   deviation_mm=round((lowest_rest - gc["runtime_y"])*1000, 2), tolerance_mm=gc.get("tol_mm", 5.0), source=gc.get("source", ""))
        if abs(R["ground_contact"]["deviation_mm"]) > gc.get("tol_mm", 5.0): R["failures"].append("ground contact")
    else:
        R["ground_contact"] = dict(note=S.get("ground_note", "part does not touch the ground"))
    # collision envelope: runtime collider vs visual
    if S.get("envelope_fn"):
        R["collision_envelope"] = S["envelope_fn"]()
        if R["collision_envelope"].get("PASS") is False: R["failures"].append("collision envelope")
    # clearance between moving parts and static parts over the timeline
    cl = {}
    for a, b in S.get("clearance_pairs", []):
        d = 9.0
        for f in frames[::3]:
            scn.frame_set(f); bpy.context.view_layer.update(); d = min(d, min_gap(a, b))
        cl[f"{a.name}|{b.name}"] = round(d*1000, 2)
    scn.frame_set(1); bpy.context.view_layer.update()
    R["clearance_min_mm"] = cl
    if any(v <= 0.0 for v in cl.values()): R["failures"].append("clearance")
    # quarter turns (RotationQuarterTurns) and mirror (L/R variant by transform)
    qt = {}
    for ax in ("X", "Y", "Z"):
        for k in (1, 2, 3):
            Mx = Matrix.Rotation(k*math.pi/2, 3, {"X": 'X', "Y": 'Z', "Z": 'Y'}[ax])    # file axes -> Blender axes
            pr = np.array([list(Mx @ Vector(p)) for p in np.vstack([verts_world(o) for o in S["housing"]])])
            pf = to_file(pr); qt[f"{ax}{90*k}"] = dict(housing_inside_cell=bool((np.abs(pf) <= L.CELL_HALF + 1e-4).all()), static_cells=cells_of(to_file(np.array([list(Mx @ Vector(p)) for p in np.vstack([verts_world(o) for o in parts])]))))
    R["quarter_turns"] = qt
    if S.get("housing_strict", True) and not all(v["housing_inside_cell"] for v in qt.values()): R["failures"].append("housing leaves cell when rotated")
    mir = rest.copy(); mir[:, 0] *= -1
    sk = S["socket"].matrix_world.translation
    R["mirror_X"] = dict(static_bounds_file=bounds(mir), socket_file=fileco(sk), socket_mirrored_file=[-fileco(sk)[0]] + fileco(sk)[1:],
                         note="L/R variant = parent scale X -1 (Unity flips winding for negative scale); geometry is closed so lighting stays correct")
    # floating detail shells inside each rigid part
    fl = {}
    for o in parts:
        iso, n = floating_shells(o); fl[o.name] = dict(shells=n, isolated=iso)
    R["floating_shells"] = fl
    if any(v["isolated"] for v in fl.values()): R["failures"].append("floating shells")
    # sockets / probes
    R["nodes_file"] = {o.name: dict(position=fileco(o.matrix_world.translation),
                                    z_axis=[round(c, 4) for c in fileco(o.matrix_world.to_3x3() @ Vector((0, 0, 1)))]) for o in S["export"] if o.type == 'EMPTY'}
    return R

def silhouettes(S, scn, out):
    """black silhouettes for the 11-part comparison board (side, front, top, gameplay 3/4)"""
    os.makedirs(out, exist_ok=True); sh = scn.display.shading
    prev = (scn.render.engine, sh.light, sh.color_type, sh.single_color[:], sh.show_cavity, sh.show_object_outline, sh.show_shadows, scn.world.color[:])
    scn.render.engine = "BLENDER_WORKBENCH"; sh.light = 'FLAT'; sh.color_type = 'SINGLE'; sh.single_color = (0, 0, 0)
    sh.show_cavity = sh.show_object_outline = sh.show_shadows = False; scn.world.color = (1, 1, 1)
    for o in bpy.data.objects:
        if o.name.startswith(("COL_", "SWEEP_")) or o in S["fx"]: o.hide_render = True
    import r4qa as Q
    Q.FXHIDE[:] = list(S["fx"])
    scn.render.use_stamp = False
    files = []; W = 3.0
    for n, (d, up) in {"side": ((1, 0, 0), None), "front": ((0, -1, 0), None), "top": ((0, -0.0001, 1), None), "gameplay": ((0.9, 1.6, 1.25), None)}.items():
        cam = Q.camera(scn); cd = cam.data; cd.type = 'ORTHO'; cd.ortho_scale = S.get("sil_scale", 1.45)
        c = Vector(S.get("sil_center", (0, 0, -0.12))); dv = Vector(d).normalized()
        cam.location = c + dv*W; cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
        scn.render.resolution_x = scn.render.resolution_y = 256
        p = os.path.join(out, f'{S["id"]}_sil_{n}.png'); scn.render.filepath = p; bpy.ops.render.render(write_still=True); files.append(os.path.basename(p))
    (scn.render.engine, sh.light, sh.color_type, sc, sh.show_cavity, sh.show_object_outline, sh.show_shadows, wc) = prev
    sh.single_color = sc; scn.world.color = wc; scn.render.use_stamp = True
    for o in bpy.data.objects:
        if o.name.startswith("COL_") or o.name.startswith("SWEEP_"): o.hide_render = True
    return files

def video(S, scn, out):
    """MP4 (H.264) of the whole master timeline rendered from the real scene (Workbench), frame number stamped"""
    import r4qa as Q
    Q.setup_workbench(scn); Q.FXHIDE[:] = []
    for o in bpy.data.objects:
        if o.name.startswith(("COL_", "SWEEP_")): o.hide_render = True
    c, r = Q.bounds_center_radius(S["parts"]); v = Q.view_set(c, r)
    cam = Q.camera(scn); cam.data.type = 'PERSP'; cam.data.lens = 50
    loc = Vector(c) + (Vector(v["34front"][0]) - Vector(c))*1.15
    cam.location = loc; cam.rotation_euler = (Vector(c) - loc).to_track_quat("-Z", "Y").to_euler()
    rs = scn.render; rs.resolution_x, rs.resolution_y = 640, 480; rs.fps = 24
    try: rs.image_settings.media_type = 'VIDEO'
    except Exception: pass
    rs.image_settings.file_format = 'FFMPEG'
    rs.ffmpeg.format = 'MPEG4'; rs.ffmpeg.codec = 'H264'
    try: rs.ffmpeg.constant_rate_factor = 'MEDIUM'; rs.ffmpeg.ffmpeg_preset = 'REALTIME'
    except Exception: pass
    rs.use_stamp = True; rs.use_stamp_frame = True; rs.use_stamp_note = True
    rs.stamp_note_text = S["id"] + " | " + " ".join(f'{c_["name"]}:{c_["start"]}-{c_["end"]}' for c_ in S["clips"])
    rs.filepath = os.path.join(out, S["id"] + "_timeline_")
    bpy.ops.render.render(animation=True)
    try: rs.image_settings.media_type = 'IMAGE'
    except Exception: pass
    rs.image_settings.file_format = 'PNG'; rs.use_stamp_frame = False
    vids = [f for f in os.listdir(out) if f.startswith(S["id"] + "_timeline_") and f.endswith((".mp4", ".mpeg", ".avi", ".mkv"))]
    final = None
    for f in vids:
        final = os.path.join(out, S["id"] + "_timeline.mp4")
        if os.path.exists(final): os.remove(final)
        os.replace(os.path.join(out, f), final)
    scn.frame_set(1)
    return os.path.basename(final) if final else "VIDEO_FAILED"
