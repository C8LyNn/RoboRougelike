# RoboRougelike #938 - R4 weapon art set: shared library (geometry / palette / rig / animation helpers).
# Style source: R41_Turret_v6 builder (owner's build_turret_v6.py) + R41_Turret_v6.1 FBX (SHA 03c60486...).
# Primitive builders and the boolean assembly are the same technique as R41 v6 so every weapon shares
# its bevel sizes, booleaned vents/seams, bolts and triangulation clean-up.
# Blender axes: X right, Z up, -Y = fire direction  ->  file/Unity: (x, y_up, z_fwd) = (X, Z, -Y)
import bpy, bmesh, math, json, os, traceback
import numpy as np
from mathutils import Matrix, Vector

P = 0.135                 # GunPivot height above mounting-cell centre (R41 v4-v6.1 contract)
CELL = 0.55; CELL_HALF = 0.275
Z_PL0, Z_PL1 = -0.260, -0.232   # mounting plate bottom/top (R41) -> Socket_Mount_Bottom at Z_PL0
PL_H = 0.255
def F(f): return -f       # forward distance -> Blender Y

LOG = []
def log(*a):
    s = " ".join(str(x) for x in a); LOG.append(s); print(s)

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scn = bpy.context.scene
    scn.unit_settings.system = 'METRIC'; scn.unit_settings.scale_length = 1.0
    scn.render.fps = 24
    return scn

def new_col(name, parent=None):
    c = bpy.data.collections.new(name); (parent or bpy.context.scene.collection).children.link(c); return c

# ---------------- LOCKED R41 v6.1 PALETTE (values read from the real FBX/.blend, style probe) ----------
LOCKED = {
 "Armor_Dark":        dict(rgb=(0.035, 0.038, 0.045), metal=0.55, rough=0.45),
 "Armor_Gunmetal":    dict(rgb=(0.16, 0.17, 0.18),    metal=0.35, rough=0.55),
 "Armor_Plate_Light": dict(rgb=(0.27, 0.28, 0.285),   metal=0.30, rough=0.58),
 "Steel_Light":       dict(rgb=(0.55, 0.62, 0.68),    metal=0.90, rough=0.25),
 "TeamColor_Red":     dict(rgb=(0.60, 0.035, 0.025),  metal=0.35, rough=0.38),
 "Hazard_Yellow":     dict(rgb=(0.95, 0.60, 0.03),    metal=0.15, rough=0.50),
 "Energy_Hot":        dict(rgb=(1.0, 0.38, 0.06),     metal=0.0,  rough=0.25, emit=(1.0, 0.22, 0.02), estr=3.5),
 "Barrel_Heat":       dict(rgb=(0.24, 0.13, 0.06),    metal=0.90, rough=0.32),
 "Sensor_Eye_Red":    dict(rgb=(1.0, 0.10, 0.06),     metal=0.0,  rough=0.2,  emit=(1.0, 0.06, 0.03), estr=12.0),
 "Muzzle_Flash":      dict(rgb=(1.0, 0.62, 0.15),     metal=0.0,  rough=0.3,  emit=(1.0, 0.50, 0.10), estr=12.0),
}
FAMILY = {"Armor_Dark": "1_dark_gunmetal", "Armor_Gunmetal": "1_dark_gunmetal",
          "Armor_Plate_Light": "2_light_steel", "Steel_Light": "2_light_steel",
          "TeamColor_Red": "3_team_red", "Hazard_Yellow": "4_hazard_yellow",
          "Energy_Hot": "T_technical", "Barrel_Heat": "T_technical", "Sensor_Eye_Red": "T_technical", "Muzzle_Flash": "T_technical"}
R41_SHARE = {"1_dark_gunmetal": 0.7913, "2_light_steel": 0.1229, "3_team_red": 0.0247, "4_hazard_yellow": 0.0015, "T_technical": 0.0595}
BANDS = {"1_dark_gunmetal": (0.62, 0.88), "2_light_steel": (0.06, 0.24), "3_team_red": (0.012, 0.07),
         "4_hazard_yellow": (0.001, 0.03), "T_technical": (0.0, 0.16)}
M = {}
def make_mat(name):
    d = LOCKED[name]; m = bpy.data.materials.new(name)
    if not m.node_tree: m.use_nodes = True
    m.diffuse_color = (*d["rgb"], 1.0); m.metallic = d["metal"]; m.roughness = d["rough"]
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*d["rgb"], 1.0)
    b.inputs["Metallic"].default_value = d["metal"]; b.inputs["Roughness"].default_value = d["rough"]
    if d.get("emit"):
        b.inputs["Emission Color"].default_value = (*d["emit"], 1.0); b.inputs["Emission Strength"].default_value = d["estr"]
    return m
def init_palette():
    M.clear()
    for n in LOCKED: M[n] = make_mat(n)
    return M

# ---------------- primitive builders (identical technique to R41 v6) ----------------------------------
def _finish(bm, name, mat, col):
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    ob = bpy.data.objects.new(name, me); col.objects.link(ob); return ob

def box(name, x0, x1, y0, y1, z0, z1, mat, col, bevel=0.0, seg=1):
    x0, x1 = sorted((x0, x1)); y0, y1 = sorted((y0, y1)); z0, z1 = sorted((z0, z1))
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    sx, sy, sz = x1-x0, y1-y0, z1-z0
    bmesh.ops.transform(bm, verts=bm.verts, matrix=Matrix.Translation(((x0+x1)/2, (y0+y1)/2, (z0+z1)/2)) @ Matrix.Diagonal((sx, sy, sz, 1)))
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=min(bevel, 0.45*min(sx, sy, sz)), offset_type='OFFSET',
                        segments=seg, profile=0.5, affect='EDGES', clamp_overlap=True)
    return _finish(bm, name, mat, col)

def _orient(bm, p0, p1):
    p0, p1 = Vector(p0), Vector(p1); d = p1 - p0
    rot = Vector((0, 0, 1)).rotation_difference(d.normalized()).to_matrix().to_4x4()
    bmesh.ops.transform(bm, verts=bm.verts, matrix=Matrix.Translation((p0+p1)/2) @ rot)

def cyl(name, r, p0, p1, mat, col, seg=16, bevel=0.0):
    L = (Vector(p1) - Vector(p0)).length
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r, radius2=r, depth=L)
    if bevel > 0:
        caps = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-7]
        bmesh.ops.bevel(bm, geom=caps, offset=min(bevel, 0.4*r, 0.4*L), offset_type='OFFSET', segments=1,
                        profile=0.5, affect='EDGES', clamp_overlap=True)
    _orient(bm, p0, p1)
    return _finish(bm, name, mat, col)

def cone(name, r0, r1, p0, p1, mat, col, seg=16):
    L = (Vector(p1) - Vector(p0)).length
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r0, radius2=r1, depth=L)
    _orient(bm, p0, p1)
    return _finish(bm, name, mat, col)

def revolve(name, prof, mat, col, seg, p0=(0, 0, 0), p1=(0, 0, 1), closed_ring=False, local=False):
    """solid of revolution around local Z; prof = [(r, z), ...] closed loop; r==0 points on the axis."""
    bm = bmesh.new(); rings = []
    for r, z in prof:
        if r < 1e-9: rings.append([bm.verts.new((0, 0, z))])
        else: rings.append([bm.verts.new((r*math.cos(2*math.pi*i/seg), r*math.sin(2*math.pi*i/seg), z)) for i in range(seg)])
    n = len(rings)
    pairs = range(n) if closed_ring else range(n-1)
    for k in pairs:
        a, b = rings[k], rings[(k+1) % n]
        for i in range(seg):
            j = (i+1) % seg
            if len(a) == 1 and len(b) == 1: continue
            if len(a) == 1: bm.faces.new((a[0], b[i], b[j]))
            elif len(b) == 1: bm.faces.new((a[i], a[j], b[0]))
            else: bm.faces.new((a[i], a[j], b[j], b[i]))
    if not local: _orient(bm, p0, p1)
    return _finish(bm, name, mat, col)

def tube(name, r_out, r_in, p0, p1, mat, col, seg=16):
    L = (Vector(p1) - Vector(p0)).length
    return revolve(name, [(r_in, -L/2), (r_out, -L/2), (r_out, L/2), (r_in, L/2)], mat, col, seg, p0, p1, closed_ring=True)

def hull(name, pts, mat, col, bevel=0.0, seg=1):
    bm = bmesh.new()
    vs = [bm.verts.new(p) for p in pts]
    res = bmesh.ops.convex_hull(bm, input=vs)
    junk = list({g for g in res.get("geom_interior", []) + res.get("geom_unused", []) if isinstance(g, bmesh.types.BMVert) and g.is_valid})
    if junk: bmesh.ops.delete(bm, geom=junk, context='VERTS')
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(0.5), verts=list(bm.verts), edges=list(bm.edges))
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, offset_type='OFFSET', segments=seg, profile=0.5,
                        affect='EDGES', clamp_overlap=True)
    return _finish(bm, name, mat, col)

def prism_x(name, x0, x1, prof, mat, col, bevel=0.0):
    """prism along X from a (forward f, z) profile"""
    return hull(name, [(x, F(f), z) for x in (x0, x1) for (f, z) in prof], mat, col, bevel=bevel)
def prism_y(name, f0, f1, prof, mat, col, bevel=0.0):
    """prism along the fire axis from an (x, z) profile, forward f0..f1"""
    return hull(name, [(x, F(f), z) for f in (f0, f1) for (x, z) in prof], mat, col, bevel=bevel)
def prism_z(name, z0, z1, prof, mat, col, bevel=0.0):
    """prism along Z from an (x, f) profile"""
    return hull(name, [(x, F(f), z) for z in (z0, z1) for (x, f) in prof], mat, col, bevel=bevel)
def chamfer_square(hs, c, z):
    return [(sx*hs, sy*(hs - c), z) for sx in (-1, 1) for sy in (-1, 1)] + [(sx*(hs - c), sy*hs, z) for sx in (-1, 1) for sy in (-1, 1)]
def rot_z(objs, k):
    for o in objs: o.data.transform(Matrix.Rotation(k*math.pi/2, 4, 'Z'))
def hexbolt(name, p0, p1, mat, col, r=0.005): return cyl(name, r, p0, p1, mat, col, seg=6)

def boolean_build(target, union_col, diff_col, post_col=None):
    for col, op in ((union_col, 'UNION'), (diff_col, 'DIFFERENCE'), (post_col, 'UNION')):
        if col is None: continue
        if not col.objects:
            bpy.data.collections.remove(col); continue
        mod = target.modifiers.new(op, 'BOOLEAN')
        mod.operation = op; mod.operand_type = 'COLLECTION'; mod.collection = col; mod.solver = 'EXACT'
        if hasattr(mod, "material_mode"): mod.material_mode = 'TRANSFER'
        if hasattr(mod, "use_hole_tolerant"): mod.use_hole_tolerant = True
        dg = bpy.context.evaluated_depsgraph_get()
        me = bpy.data.meshes.new_from_object(target.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
        target.modifiers.remove(mod)
        old = target.data; target.data = me; bpy.data.meshes.remove(old)
        for o in list(col.objects):
            m = o.data; bpy.data.objects.remove(o, do_unlink=True)
            if m.users == 0: bpy.data.meshes.remove(m)
        bpy.data.collections.remove(col)
    bm = bmesh.new(); bm.from_mesh(target.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=1e-6)
    loose = [v for v in bm.verts if not v.link_faces]
    if loose: bmesh.ops.delete(bm, geom=loose, context='VERTS')
    holes = [e for e in bm.edges if e.is_boundary]
    if holes:
        log(f"{target.name}: filling {len(holes)} boundary edges")
        bmesh.ops.holes_fill(bm, edges=holes, sides=0)
    bmesh.ops.triangulate(bm, faces=bm.faces, quad_method='BEAUTY', ngon_method='BEAUTY')
    for _ in range(5):
        bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-6)
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 3], quad_method='BEAUTY', ngon_method='BEAUTY')
        bad = [f for f in bm.faces if f.calc_area() < 1e-9]
        if not bad: break
        for f in bad:
            if not f.is_valid: continue
            e = max(f.edges, key=lambda e: e.calc_length())
            if len(e.link_faces) == 2:
                try: bmesh.utils.edge_rotate(e, True)
                except Exception: pass
    for _ in range(4):   # final pass: collapse any remaining zero-area triangles (sub-0.05 mm edges) and re-triangulate
        bad = [f for f in bm.faces if f.calc_area() < 1e-9]
        if not bad: break
        es = list({e for f in bad for e in f.edges})
        bmesh.ops.dissolve_degenerate(bm, edges=es, dist=5e-5)
        short = [e for e in es if e.is_valid and e.calc_length() < 5e-5]
        if short: bmesh.ops.collapse(bm, edges=short)
        bad = [f for f in bm.faces if f.is_valid and f.calc_area() < 1e-9]
        for f in bad:
            e = max(f.edges, key=lambda e: e.calc_length())
            if len(e.link_faces) == 2:
                try: bmesh.utils.edge_rotate(e, True)
                except Exception: pass
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 3], quad_method='BEAUTY', ngon_method='BEAUTY')
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(target.data); bm.free()

class Part:
    """one rigid mesh = base primitive + union / difference / post-union cutter sets (R41 three-pass assembly)"""
    def __init__(self, work, tag):
        self.U = new_col(f"_{tag}U", work); self.D = new_col(f"_{tag}D", work); self.Pc = new_col(f"_{tag}P", work)
    def build(self, target):
        boolean_build(target, self.U, self.D, self.Pc); return target

# ---------------- R41-family mounting base (fixed 0.51 plate + socket contract, per-weapon dressing) ---------
def build_base(name, work, glacis_top=0.190, glacis_mid=0.228, z_top=-0.004, vents=5, skirts=False,
               hazard_corners=False, team_faces=(0, 1, 2, 3), louvre_glow=True, extra_plates=False):
    """R41 base: chamfered mounting plate, sloped glacis, applique plates with team stripe, louvred hot vents,
    turret-ring collar. Parameters change the dressing, never the plate footprint or socket."""
    pt = Part(work, "base")
    base = hull(name, chamfer_square(PL_H, 0.040, Z_PL0) + chamfer_square(PL_H, 0.040, Z_PL1), M["Armor_Dark"], work, bevel=0.004)
    hull("glacis", chamfer_square(0.238, 0.070, Z_PL1 - 0.003) + chamfer_square(glacis_mid, 0.068, -0.150)
         + chamfer_square(glacis_top, 0.060, z_top), M["Armor_Gunmetal"], pt.U, bevel=0.005)
    revolve("collar", [(0, z_top - 0.006), (0.185, z_top - 0.006), (0.200, z_top + 0.002), (0.200, z_top + 0.020), (0, z_top + 0.020)],
            M["Armor_Dark"], pt.U, 48, local=True)
    cg = tube("collar_glow", 0.2015, 0.196, (0, 0, z_top + 0.007), (0, 0, z_top + 0.011), M["Energy_Hot"], pt.U, seg=48)
    cg.data.transform(Matrix.Rotation(math.pi/48, 4, 'Z'))
    slope = (glacis_mid - glacis_top) / (z_top + 0.150)
    def surf_up(z): return glacis_mid - slope*(z + 0.150)
    def uw_pl(z): return 0.150 + (0.118 - 0.150)*(z + 0.140)/0.120
    zt = min(-0.020, z_top - 0.016)
    for k in range(4):
        made = []
        made.append(hull("applique", [(surf_up(z) + d, s*uw_pl(z), z) for z in (-0.140, zt) for d in (-0.004, 0.008) for s in (-1, 1)],
                         M["Armor_Plate_Light"], pt.U, bevel=0.003))
        if k in team_faces:
            made.append(hull("team_stripe", [(surf_up(z) + d, s*(uw_pl(z) - 0.004), z) for z in (-0.112, -0.092) for d in (0.006, 0.0115) for s in (-1, 1)],
                             M["TeamColor_Red"], pt.Pc))
        for u in (-0.060, 0.060):
            made.append(hull("seam", [(surf_up(z) + d, u + s*0.001, z) for z in (-0.136, zt - 0.004) for d in (0.004, 0.020) for s in (-1, 1)], M["Armor_Dark"], pt.D))
        for (u, z) in ((-0.135, -0.130), (0.135, -0.130), (-0.105, zt - 0.012), (0.105, zt - 0.012)):
            made.append(hexbolt("pl_bolt", (surf_up(z) + 0.006, u, z), (surf_up(z) + 0.0115, u, z), M["Steel_Light"], pt.Pc))
        made.append(box("vent_cut", 0.222, 0.260, -0.120, 0.120, -0.215, -0.163, M["Armor_Dark"], pt.D))
        if louvre_glow: made.append(box("vent_glow", 0.2205, 0.2235, -0.118, 0.118, -0.213, -0.165, M["Energy_Hot"], pt.Pc))
        for i in range(vents):
            zl = -0.211 + i*(0.044/max(1, vents - 1))
            made.append(box("louvre", 0.2215, 0.2290, -0.119, 0.119, zl, zl + 0.004, M["Armor_Dark"], pt.Pc))
        made.append(box("seam_ring", 0.223, 0.250, -0.170, 0.170, -0.1515, -0.1485, M["Armor_Dark"], pt.D))
        if skirts:
            made.append(hull("skirt", [(x, s*0.205, z) for x in (0.236, 0.252) for s in (-1, 1) for z in (Z_PL1 + 0.002, -0.178)],
                             M["Armor_Gunmetal"], pt.U, bevel=0.003))
            for u in (-0.17, 0.0, 0.17):
                made.append(hexbolt("skirt_bolt", (0.251, u, -0.205), (0.257, u, -0.205), M["Steel_Light"], pt.Pc, r=0.006))
        if extra_plates:
            made.append(box("ring_plate", 0.150, 0.196, -0.060, 0.060, z_top - 0.012, z_top - 0.002, M["Armor_Plate_Light"], pt.U, bevel=0.002))
        rot_z(made, k)
    for (x0, x1, y0, y1) in ((0.2535, 0.2565, -0.20, 0.20), (-0.2565, -0.2535, -0.20, 0.20), (-0.20, 0.20, 0.2535, 0.2565), (-0.20, 0.20, -0.2565, -0.2535)):
        box("plate_glow", x0, x1, y0, y1, -0.250, -0.244, M["Energy_Hot"], pt.U)
    for (bx_, by_) in [(sx*0.218, sy*0.218) for sx in (-1, 1) for sy in (-1, 1)]:
        cyl("plate_bolt", 0.009, (bx_, by_, Z_PL1 - 0.002), (bx_, by_, Z_PL1 + 0.006), M["Steel_Light"], pt.U, seg=6)
    if hazard_corners:
        for k in range(4):
            made = [hull("haz_corner", [(0.212 + d, -0.212 + d2, z) for d in (0, 0.026) for d2 in (0, 0.026) for z in (Z_PL1 - 0.0005, Z_PL1 + 0.0025)],
                         M["Hazard_Yellow"], pt.Pc)]
            rot_z(made, k)
    pt.build(base)
    import r4port as P4; P4.port(base, work, -1)
    return base, z_top + 0.020

def turntable(name, work, z0=0.018, z1=0.042, r=0.190):
    """R41 turntable disk with hot ring (union target for the yaw body)"""
    pt = Part(work, "yawtt")
    yaw = cyl(name, r, (0, 0, z0), (0, 0, z1), M["Armor_Dark"], work, seg=48, bevel=0.004)
    tg = tube("tt_glow", r + 0.0015, r - 0.004, (0, 0, (z0+z1)/2 - 0.002), (0, 0, (z0+z1)/2 + 0.002), M["Energy_Hot"], pt.U, seg=48)
    tg.data.transform(Matrix.Rotation(math.pi/48, 4, 'Z'))
    for i in range(12):
        a = 2*math.pi*(i + 0.5)/12
        hexbolt("tt_bolt", (0.165*math.cos(a), 0.165*math.sin(a), z1 - 0.001), (0.165*math.cos(a), 0.165*math.sin(a), z1 + 0.005), M["Steel_Light"], pt.U, r=0.0055)
    boolean_build(yaw, pt.U, pt.D, pt.Pc)
    return yaw

def hazard_strip(prefix, x0, x1, f0, f1, z0, z1, col_plate, col_cut, n=6):
    """yellow plate + dark chevron stripes (R41 barrel-collar hazard plate)"""
    box(prefix + "_plate", x0, x1, F(f0), F(f1), z0, z1, M["Hazard_Yellow"], col_plate)
    w = (x1 - x0)/n
    for i in range(n):
        xa = x0 + i*w
        hull(prefix + "_chev", [(xx, F(ff), z) for (xx, ff) in ((xa, f0 + 0.0005), (xa + w*0.5, f0 + 0.0005), (xa + w, f1 - 0.0005), (xa + w*0.5, f1 - 0.0005))
                                 for z in (z1 - 0.001, z1 + 0.001)], M["Armor_Dark"], col_plate)

# ---------------- shading / UV ---------------------------------------------------------------------------
def shade_and_uv(ob):
    me = ob.data
    for p_ in me.polygons: p_.use_smooth = True
    try: me.set_sharp_from_angle(angle=math.radians(35))
    except Exception as e: log("set_sharp_from_angle failed:", e)
    vl = bpy.context.view_layer
    try:
        for o in list(vl.objects):
            if o is not None: o.select_set(False)
        ob.hide_set(False); ob.select_set(True); vl.objects.active = ob
        with bpy.context.temp_override(active_object=ob, object=ob, selected_objects=[ob], selected_editable_objects=[ob]):
            bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.006, scale_to_bounds=False)
            bpy.ops.object.mode_set(mode='OBJECT')
    except Exception:
        log("smart_project failed:\n" + traceback.format_exc())
        try: bpy.ops.object.mode_set(mode='OBJECT')
        except Exception: pass
    me.name = ob.name

# ---------------- rig helpers ------------------------------------------------------------------------------
def empty(name, parent, loc, col, disp='PLAIN_AXES', size=0.05, rot=(0, 0, 0)):
    e = bpy.data.objects.new(name, None); col.objects.link(e)
    e.empty_display_type = disp; e.empty_display_size = size
    e.parent = parent; e.location = loc; e.rotation_euler = rot; return e
UP90 = (math.radians(90), 0, 0)        # empty local +Z -> Blender -Y (fire direction) == file +Z
def to_local(ob, piv_world):
    """bake geometry into the object's pivot: mesh built in world space, origin moved to piv_world"""
    ob.data.transform(Matrix.Translation(-Vector(piv_world)))
def parent_keep(child, parent, world_origin):
    """child mesh built in world coords; place it as parent's child with clean local transform"""
    pw = parent.matrix_world.translation if parent else Vector()
    child.parent = parent; child.location = Vector(world_origin) - pw

# ---------------- animation helpers (master timeline, clip table) ------------------------------------------
class Timeline:
    def __init__(self):
        self.clips = []; self.cursor = 1; self.keyed = set()
    def clip(self, name, length, loop=False, desc=""):
        s = self.cursor; e = s + length
        self.clips.append(dict(name=name, start=s, end=e, loop=loop, desc=desc)); self.cursor = e + 2
        return s, e
    @property
    def end(self): return self.cursor - 2

def _prefs_interp(v):
    try: bpy.context.preferences.edit.keyframe_new_interpolation_type = v
    except Exception: pass
def _set_interp(ob, path, idx, frame, interp):
    """the preference alone is not reliable in background runs: set the inserted key's interpolation explicitly"""
    n = 0
    for fc in fcurves_of(ob):
        if fc.data_path == path and (idx is None or fc.array_index == idx):
            for kp in fc.keyframe_points:
                if abs(kp.co.x - frame) < 1e-4: kp.interpolation = interp; n += 1
    return n
def key(ob, path, idx, frame, value, interp='BEZIER'):
    _prefs_interp(interp)
    getattr(ob, path)[idx] = value
    ob.keyframe_insert(path, index=idx, frame=frame)
    _set_interp(ob, path, idx, frame, interp)
def key3(ob, path, frame, vec, interp='BEZIER'):
    _prefs_interp(interp)
    v = getattr(ob, path)
    for i in range(3): v[i] = vec[i]
    ob.keyframe_insert(path, frame=frame)
    _set_interp(ob, path, None, frame, interp)

def fcurves_of(ob):
    ad = ob.animation_data
    if not ad or not ad.action: return []
    a = ad.action; fcs = []
    try: fcs = list(a.fcurves)
    except Exception: pass
    if not fcs:
        try:
            for layer in a.layers:
                for strip in layer.strips:
                    cb = strip.channelbag(ad.action_slot)
                    if cb: fcs += list(cb.fcurves)
        except Exception as e: log("channelbag:", e)
    return fcs
def name_action(ob, name):
    if ob.animation_data and ob.animation_data.action: ob.animation_data.action.name = name


# ---------------- FX / rig / manifests ---------------------------------------------------------------------
def build_flash(name, col, work, length=0.135, radius=0.034, spikes=4):
    """muzzle-flash FX mesh around its own origin (muzzle face) pointing forward (-Y); idle scale 0.001 (R41 v6)"""
    fU = new_col("_fU", work)
    h = length/2
    ob = revolve(name, [(0, -h), (radius, -h + 0.010), (radius*0.76, -h + 0.050), (radius*0.35, h - 0.035), (0, h)], M["Muzzle_Flash"], col, 10,
                 (0, 0, 0), (0, -length, 0))
    for k in range(spikes):
        a = k*2*math.pi/spikes + math.pi/4; ca, sa = math.cos(a), math.sin(a)
        pts = [(0.008*ca - 0.006*sa*s1, -0.012 + 0.010*s2, 0.008*sa + 0.006*ca*s1) for s1 in (-1, 1) for s2 in (-1, 1)]
        pts.append((radius*2.05*ca, -0.035, radius*2.05*sa))
        hull("spike", pts, M["Muzzle_Flash"], fU)
    boolean_build(ob, fU, None)
    return ob

def finish_rig(prefix, col, base, yaw, pitch, pivot_kids=(), pitch_range=(10.0, 35.0), yaw_free=True):
    """R41 hierarchy: ROOT -> Base, Socket_Mount_Bottom, GunPivot -> Yaw -> Pitch -> ...; all meshes built in world space"""
    root = empty(prefix + "_ROOT", None, (0, 0, 0), col, 'PLAIN_AXES', 0.1)
    base.parent = root
    pivot = empty("GunPivot", root, (0, 0, P), col, 'ARROWS', 0.06)
    for o in (yaw, pitch):
        if o is not None: o.data.transform(Matrix.Translation((0, 0, -P)))
    if yaw is not None: yaw.parent = pivot
    if pitch is not None: pitch.parent = yaw if yaw is not None else pivot
    socket = empty("Socket_Mount_Bottom", root, (0, 0, Z_PL0), col, 'SINGLE_ARROW', 0.08, (math.pi, 0, 0))
    if yaw is not None:
        c_ = yaw.constraints.new('LIMIT_ROTATION'); c_.use_limit_x = True; c_.use_limit_y = True; c_.owner_space = 'LOCAL'
    if pitch is not None:
        dn, up = pitch_range
        c_ = pitch.constraints.new('LIMIT_ROTATION'); c_.use_limit_y = True; c_.use_limit_z = True
        c_.use_limit_x = True; c_.min_x = math.radians(-up); c_.max_x = math.radians(dn); c_.owner_space = 'LOCAL'
    return root, pivot, socket

def attach(ob, parent, pivot_world):
    """mesh built in world space -> origin at pivot_world, parented with clean local transform (no parent inverse)"""
    ob.data.transform(Matrix.Translation(-Vector(pivot_world)))
    pw = parent.matrix_world.translation if parent else Vector()
    bpy.context.view_layer.update()
    pw = parent.matrix_world.translation
    ob.parent = parent; ob.location = Vector(pivot_world) - pw
    return ob

def set_props(objs_props):
    for o, kv in objs_props.items():
        for k, v in kv.items(): o[k] = v

def write_manifests(S, R):
    out = S["out"]
    anim = dict(id=S["id"], kind=S["kind"], fps=24, frame_range=[1, S["frame_end"]],
                note="One baked take in FBX/GLB (master timeline). Unity: split into clips by these frame ranges; loop=true clips are seamless.",
                clips=[{k: v for k, v in c.items() if k != "render_frames"} for c in S["clips"]],
                animated_nodes=sorted({o.name for o in S["export"] if o.animation_data and o.animation_data.action}),
                gameplay="Animation is visual only: it adds no gameplay capability, shot count or timing authority.")
    anim.update(S.get("anim_extra", {}))
    json.dump(anim, open(os.path.join(out, S["id"] + "_animation_manifest.json"), "w", encoding="utf-8"), indent=1)
    mats = sorted({m.name for o in S["parts"] + S["fx"] for m in o.data.materials if m})
    pal = dict(id=S["id"], source="R41_Turret_v6.1 locked palette (values read from the real FBX by r4_style_probe.py)",
               four_dominant_families={"1_dark_gunmetal": ["Armor_Dark", "Armor_Gunmetal"], "2_light_steel": ["Armor_Plate_Light", "Steel_Light"],
                                       "3_team_red": ["TeamColor_Red"], "4_hazard_yellow": ["Hazard_Yellow"]},
               technical_slots=["Energy_Hot", "Barrel_Heat", "Sensor_Eye_Red", "Muzzle_Flash"],
               materials_used={n: LOCKED[n] for n in mats if n in LOCKED}, team_colour_note="TeamColor_Red is the only per-team tint slot (as R41).")
    json.dump(pal, open(os.path.join(out, S["id"] + "_palette_manifest.json"), "w", encoding="utf-8"), indent=1, default=str)
    model = dict(id=S["id"], kind=S["kind"], game_name=S["game_name"], units="meter 1:1", axes="file/Unity +Z forward, +Y up",
                 cell="logical 1x1x1 on 0.55 m grid; mount = Socket_Mount_Bottom", hierarchy={o.name: (o.parent.name if o.parent else None) for o in S["export"]},
                 node_roles={o.name: {k: str(o[k]) for k in o.keys() if not k.startswith("_") and k != "cycles"} for o in S["export"] if len(o.keys())},
                 muzzles=[m.name for m in S["muzzles"]], visible_exits_note=S.get("exits_note", ""), notes=S.get("notes", []))
    json.dump(model, open(os.path.join(out, S["id"] + "_model_manifest.json"), "w", encoding="utf-8"), indent=1, default=str)

def standard_aim(T, yaw, pitch, up=35, dn=10, yaw_len=48, pitch_len=36):
    s, e = T.clip("AimYaw", yaw_len, desc="yaw 0 -> +90 -> -90 -> 0 about local Y (Unity)")
    for f, d in ((s, 0), (s + yaw_len//4, 90), (s + 3*yaw_len//4, -90), (e, 0)): key(yaw, "rotation_euler", 2, f, math.radians(d))
    T.clips[-1]["render_frames"] = [s + yaw_len//4]
    if pitch is not None:
        s, e = T.clip("AimPitch", pitch_len, desc=f"pitch 0 -> +{up} -> -{dn} -> 0 about local X (Unity)")
        for f, d in ((s, 0), (s + pitch_len//3, up), (s + 2*pitch_len//3, -dn), (e, 0)): key(pitch, "rotation_euler", 0, f, math.radians(-d))
        T.clips[-1]["render_frames"] = [s + pitch_len//3, s + 2*pitch_len//3]

def hold(T, objs, name="Idle", length=24, desc="rest hold"):
    s, e = T.clip(name, length, loop=True, desc=desc)
    for o, path, idx, v in objs:
        key(o, path, idx, s, v); key(o, path, idx, e, v)
    return s, e

def pin_rest(objs, frames):
    """key rest values at the given frames so every clip boundary is a clean rest pose"""
    for o, path, idx, v in objs:
        for f in frames: key(o, path, idx, f, v)


def arc_plate(name, r0, r1, a0, a1, x0, x1, mat, col, centre, n=None, frame="YZ"):
    """closed manifold annular-sector plate. frame 'YZ': arc in the fire plane (phi 0 = forward, 90 = up) around
    centre=(x, f, z), thickness along X. frame 'XY': arc in the horizontal plane around centre=(x, f, z),
    phi 0 = forward, 90 = +X, thickness along Z (x0, x1 are z0, z1)."""
    if n is None: n = max(2, int(abs(a1 - a0)/4.0) + 1)
    bm = bmesh.new(); cx, cf, cz = centre; rows = []
    for i in range(n + 1):
        ph = math.radians(a0 + (a1 - a0)*i/n); ring = []
        for r in (r0, r1):
            for t in (x0, x1):
                if frame == "YZ": ring.append(bm.verts.new((t, F(cf + r*math.cos(ph)), cz + r*math.sin(ph))))
                else: ring.append(bm.verts.new((cx + r*math.sin(ph), F(cf + r*math.cos(ph)), t)))
        rows.append(ring)   # order: (r0,t0) (r0,t1) (r1,t0) (r1,t1)
    loop = (0, 1, 3, 2)
    for i in range(n):
        a, b = rows[i], rows[i + 1]
        for k in range(4):
            p, q = loop[k], loop[(k + 1) % 4]
            bm.faces.new((a[p], a[q], b[q], b[p]))
    bm.faces.new([rows[0][k] for k in loop]); bm.faces.new([rows[-1][k] for k in reversed(loop)])
    return _finish(bm, name, mat, col)


def yoke(pt, x_in, x_out, cheek, appl, team, boss_r=0.044, bolts=((0.090, 0.080), (-0.105, 0.068)), vents=((-0.110, -0.100), (-0.090, -0.080)),
         vent_z=(0.070, 0.140)):
    """R41-style pair of armoured trunnion cheeks with applique, team stripe, bosses, axle bores, hot vents"""
    for s in (-1, 1):
        prism_x(f"cheek{s}", s*x_in, s*x_out, cheek, M["Armor_Gunmetal"], pt.U, bevel=0.005)
        prism_x(f"cheek_appl{s}", s*(x_out - 0.0005), s*(x_out + 0.009), appl, M["Armor_Plate_Light"], pt.U, bevel=0.003)
        prism_x(f"cheek_team{s}", s*(x_out + 0.0085), s*(x_out + 0.0130), team, M["TeamColor_Red"], pt.Pc)
        cyl(f"boss{s}", boss_r, (s*(x_in - 0.004), 0, P), (s*(x_out + 0.016), 0, P), M["Armor_Dark"], pt.U, seg=24, bevel=0.004)
        tube(f"boss_glow{s}", boss_r - 0.008, boss_r - 0.017, (s*(x_out + 0.014), 0, P), (s*(x_out + 0.0185), 0, P), M["Energy_Hot"], pt.Pc, seg=24)
        cyl(f"bore{s}", 0.021, (s*(x_in - 0.010), 0, P), (s*(x_out + 0.002), 0, P), M["Armor_Dark"], pt.D, seg=20)
        for (f0, f1) in vents:
            box(f"vent{s}", s*(x_out + 0.002), s*(x_out + 0.020), F(f0), F(f1), vent_z[0], vent_z[1], M["Armor_Dark"], pt.D)
            box(f"vent_glow{s}", s*(x_out + 0.0005), s*(x_out + 0.0040), F(f0 + 0.001), F(f1 - 0.001), vent_z[0] + 0.002, vent_z[1] - 0.002, M["Energy_Hot"], pt.Pc)
        for (f_, z_) in bolts:
            hexbolt(f"appl_bolt{s}", (s*(x_out + 0.007), F(f_), z_), (s*(x_out + 0.0125), F(f_), z_), M["Steel_Light"], pt.Pc)

def trunnion(name, work, pt, half=0.100, r=0.048, axle=0.140):
    ob = cyl(name, r, (-half, 0, P), (half, 0, P), M["Armor_Dark"], work, seg=32, bevel=0.004)
    cyl("axle", 0.018, (-axle, 0, P), (axle, 0, P), M["Steel_Light"], pt.U, seg=16)
    return ob

def gather(colname, objs):
    col = new_col(colname)
    for o in objs:
        for c_ in o.users_collection: c_.objects.unlink(o)
        col.objects.link(o)
    w = bpy.data.collections.get("_build_tmp")
    if w: bpy.data.collections.remove(w)
    return col

def root_props(root, kind, version="v1"):
    for k, v in {"unity_up": "+Y", "unity_forward": "+Z", "unit": "meter", "version": version, "weapon_kind": kind,
                 "style": "R41_Turret_v6.1 standard (heavy industrial armour + team colour)", "issue": "#938"}.items(): root[k] = v

def recoil_keys(ob, s0, dist, shape=((0, 0.0), (1, 1.0), (3, 0.62), (6, 0.15), (9, 0.0)), axis=1):
    for df, k in shape: key(ob, "location", axis, s0 + df, dist*k, 'LINEAR')

def flash_keys(ob, s0, seq=((0, 1.0), (1, 0.65), (2, 0.001))):
    for df, sc in seq: key3(ob, "scale", s0 + df, (sc,)*3, 'CONSTANT')
