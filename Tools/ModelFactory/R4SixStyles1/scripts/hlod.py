"""#1239 Hero Benchmark Pass 2: LOD1 and mesh <-> mesh_source helpers. RUNS INSIDE BLENDER.
LOD1 = planar dissolve of the game mesh (flat faces and the mount faces stay exact, curved surfaces lose tessellation), inside UV islands and material
borders, so the LOD uses the same texture set; if the result is not a clean closed mesh the angle is lowered, and at worst the game mesh is kept."""
import bpy, bmesh, math
import hf
ZMAP = dict(base="R4P_Base", sec="R4P_Secondary", alt="R4P_Secondary", alt2="R4P_Secondary", dark="R4P_Trim", rubber="R4P_Trim", glass="R4P_Trim", glow="R4P_Trim", metal="R4P_Metal", bare="R4P_Metal", rust="R4P_Wear")


def mesh_json(o, pivot=(0, 0, 0), zone_of=None):
    """mesh_source node: vertices RELATIVE TO THE NODE PIVOT (as pgen.pack writes them), per-corner normals and UV, paint zone per face"""
    me = o.data; cn = me.corner_normals; uv = me.uv_layers.active.data if me.uv_layers.active else None; zone_of = zone_of or (lambda name: ZMAP[name[2:]]); mats = [zone_of(m.name) for m in me.materials]; faces = []
    for p in me.polygons:
        li = list(p.loop_indices); faces.append(dict(v=[me.loops[i].vertex_index for i in li], n=[[round(x, 4) for x in cn[i].vector] for i in li], uv=[[round(uv[i].uv[0], 5), round(uv[i].uv[1], 5)] if uv else [0, 0] for i in li], zone=mats[p.material_index]))
    return dict(verts=[[round(c - q, 5) for c, q in zip(v.co, pivot)] for v in me.vertices], faces=faces)


def from_json(parts, name, col):
    """one object from several mesh_source nodes given as [(mesh dict, pivot)] - back in asset space, one material slot per paint zone"""
    V = []; F = []; Z = []; UV = []; off = 0
    for ms, piv in parts:
        V += [tuple(c + q for c, q in zip(v, piv)) for v in ms["verts"]]
        for f in ms["faces"]: F.append([i + off for i in f["v"]]); Z.append(f["zone"]); UV.append(f["uv"])
        off += len(ms["verts"])
    me = bpy.data.meshes.new(name); me.from_pydata(V, [], F); zones = sorted(set(Z))
    for z in zones: me.materials.append(bpy.data.materials.get(z) or bpy.data.materials.new(z))
    uv = me.uv_layers.new(name="UVMap")
    for p, z, u in zip(me.polygons, Z, UV):
        p.material_index = zones.index(z)
        for k, li in enumerate(p.loop_indices): uv.data[li].uv = u[k]
    me.update(); o = bpy.data.objects.new(name, me); col.objects.link(o); return o


def report(o):
    bm = bmesh.new(); bm.from_mesh(o.data); nm = sum(1 for e in bm.edges if not e.is_manifold); za = sum(1 for f in bm.faces if f.calc_area() < 1e-10); inc = sum(1 for e in bm.edges if e.is_manifold and len(e.link_faces) == 2 and not e.is_contiguous); bm.free(); return nm, za, inc


def lod1(src_obj, col, angles=(14.0, 9.0, 5.0)):
    """returns (object, info)"""
    base = hf.tris(src_obj)
    for a in angles:
        c = src_obj.copy(); c.data = src_obj.data.copy(); col.objects.link(c); c.name = "LOD1"; bpy.ops.object.select_all(action="DESELECT"); c.select_set(True); bpy.context.view_layer.objects.active = c
        m = c.modifiers.new("d", "DECIMATE"); m.decimate_type = "DISSOLVE"; m.angle_limit = math.radians(a); m.delimit = {"UV", "MATERIAL"}; bpy.ops.object.modifier_apply(modifier="d")
        bm = bmesh.new(); bm.from_mesh(c.data); bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4]); bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=bm.edges); bm.to_mesh(c.data); bm.free()
        r = report(c)
        if r == (0, 0, 0) or (report(src_obj) != (0, 0, 0) and r[1] == 0): hf.shade(c, 40, True); return c, dict(method="planar dissolve", angle_deg=a, triangles=hf.tris(c), of_lod0=round(hf.tris(c)/max(base, 1), 3))
        bpy.data.objects.remove(c, do_unlink=True)
    c = src_obj.copy(); c.data = src_obj.data.copy(); col.objects.link(c); c.name = "LOD1"; return c, dict(method="game mesh kept (dissolve did not give a clean mesh)", triangles=base, of_lod0=1.0)
