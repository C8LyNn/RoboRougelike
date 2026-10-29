#!/usr/bin/env python3
"""ENV02-001: original static low-poly terrain ramp; run with Blender --background.

No Unity/gameplay code. Generates visual GLB, independent collider-proposal GLB,
editable BLEND, two Workbench previews and Blender-native QA evidence.
"""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

ASSET_ID = 'ENV02-001'
HALF_LENGTH = 2.5
HALF_WIDTH = 1.3
BASE_HALF_WIDTH = 1.225
PLATE_THICKNESS = 0.030
# v002: C1 eased approaches, 5 mm entry lip, no sharp crest transition.
TIP_Z=0.005
CREST_Z=0.520
CREST_HALF_LENGTH=0.400
EASE_LENGTH=0.240
BASE_END_Y=2.200
RAMP_LENGTH=HALF_LENGTH-CREST_HALF_LENGTH
MAX_SLOPE=(CREST_Z-TIP_Z)/(RAMP_LENGTH-EASE_LENGTH)


def smooth_ramp_height(y):
    t=y+HALF_LENGTH
    if t <= EASE_LENGTH:
        u=max(0.0,t/EASE_LENGTH)
        return TIP_Z+MAX_SLOPE*EASE_LENGTH*(u**3-0.5*u**4)
    if t <= RAMP_LENGTH-EASE_LENGTH:
        return TIP_Z+MAX_SLOPE*(t-0.5*EASE_LENGTH)
    if t <= RAMP_LENGTH:
        u=(t-(RAMP_LENGTH-EASE_LENGTH))/EASE_LENGTH
        return CREST_Z-MAX_SLOPE*EASE_LENGTH*(0.5-u+u**3-0.5*u**4)
    return CREST_Z


def make_top_profile():
    # 40 mm chords in eased transitions, single planar middle section.
    left=[round(-HALF_LENGTH+i*EASE_LENGTH/6,6) for i in range(7)]
    left += [round(-CREST_HALF_LENGTH-EASE_LENGTH,6)]
    left += [round(-CREST_HALF_LENGTH-EASE_LENGTH+i*EASE_LENGTH/6,6)
             for i in range(1,7)]
    return [(y,round(smooth_ramp_height(y),9)) for y in left]+[
        (-y,round(smooth_ramp_height(y),9)) for y in reversed(left)]


TOP_PROFILE=make_top_profile()

MATERIAL_SPECS = (
    ('CONCRETE', (0.34, 0.37, 0.39, 1), 0.0, 0.89),
    ('STEEL', (0.17, 0.22, 0.27, 1), 0.66, 0.43),
    ('SAFETY_AMBER', (0.94, 0.46, 0.075, 1), 0.12, 0.49),
)


def sha256(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def top_z(y):
    for (y0, z0), (y1, z1) in zip(TOP_PROFILE, TOP_PROFILE[1:]):
        if y <= y1 + 1e-9:
            t = max(0.0, min(1.0, (y - y0) / (y1 - y0)))
            return z0 + (z1 - z0) * t
    return TOP_PROFILE[-1][1]


def underside_z(y):
    # 4 mm feathered steel tip, full 30 mm plate away from the ends.
    z=top_z(y)
    return z-min(PLATE_THICKNESS,0.8*z)


def profile_metrics():
    grades=[math.degrees(math.atan2(z1-z0,y1-y0))
            for (y0,z0),(y1,z1) in zip(TOP_PROFILE,TOP_PROFILE[1:])]
    return {
        'overall_length_m':2*HALF_LENGTH,
        'drive_width_m':2*HALF_WIDTH,
        'crest_height_m':CREST_Z,
        'crest_flat_length_m':2*CREST_HALF_LENGTH,
        'floor_entry_step_m':TIP_Z,
        'maximum_abs_segment_grade_deg':max(abs(g) for g in grades),
        'maximum_adjacent_grade_change_deg':max(
            abs(b-a) for a,b in zip(grades,grades[1:])),
        'top_profile_points':len(TOP_PROFILE),
    }


def material(name, rgba, metallic, roughness):
    m = bpy.data.materials.new(name)
    m.diffuse_color = rgba
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    return m


def recalc_outward(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if bm.calc_volume(signed=True) < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def prism_yz(name, x0, x1, profile, mat):
    n = len(profile)
    vertices = [(x0, y, z) for y, z in profile] + [(x1, y, z) for y, z in profile]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    mesh = bpy.data.meshes.new(name + '_MESH')
    mesh.from_pydata(vertices, [], faces)
    mesh.validate()
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    recalc_outward(obj)
    return obj


def block(name, center, size, mat):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    return obj


def bolt(name, side, y, mat):
    z = top_z(y) - PLATE_THICKNESS / 2
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.017, depth=0.012,
                                        end_fill_type='NGON',
                                        location=(side * 1.304, y, z - 0.025),
                                        rotation=(0, math.pi / 2, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return obj


def set_selected(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]


def export_glb(objects, path):
    set_selected(objects)
    bpy.ops.export_scene.gltf(filepath=str(path), export_format='GLB',
                              use_selection=True, export_animations=False,
                              export_cameras=False, export_lights=False)
    if not path.is_file() or path.stat().st_size < 1000:
        raise RuntimeError('GLB export missing or empty: ' + str(path))


def render_preview(scene, out, location, target, scale):
    cam_data = bpy.data.cameras.new('QA_CAMERA')
    cam = bpy.data.objects.new('QA_CAMERA', cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = location
    direction = Vector(target) - cam.location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = scale
    scene.camera = cam
    scene.render.filepath = str(out)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam, do_unlink=True)
    if not out.is_file() or out.stat().st_size < 1000:
        raise RuntimeError('Preview render missing: ' + str(out))


def manifold_audit(objects):
    result = {}
    for obj in objects:
        if obj.type != 'MESH':
            continue
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        invalid_edges = sum(1 for e in bm.edges if not e.is_manifold)
        bad_faces = sum(1 for f in bm.faces if f.calc_area() < 1e-9)
        signed_volume = bm.calc_volume(signed=True)
        bm.free()
        result[obj.name] = {
            'non_manifold_edges': invalid_edges,
            'degenerate_faces': bad_faces,
            'signed_volume_m3': round(signed_volume, 9),
        }
        if invalid_edges or bad_faces or signed_volume <= 0:
            raise AssertionError('Invalid geometry: %s: %s' % (obj.name, result[obj.name]))
    return result


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument('--out', required=True)
    args = parser.parse_args(argv)
    out = Path(args.out).resolve()
    out.mkdir(parents=True, exist_ok=True)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    scene.frame_start = scene.frame_end = 1
    materials = {name: material(name, rgba, met, rough)
                 for name, rgba, met, rough in MATERIAL_SPECS}
    concrete, steel, amber = (materials[k] for k in ('CONCRETE', 'STEEL', 'SAFETY_AMBER'))

    # The drivable top is three unbroken planar quads: approach, flat crest, exit.
    # Decorative elements are exclusively on OUTSIDE side cheeks, never on top.
    # 4.4 m continuous concrete support; 0.3 m tapered steel end wings.
    foundation_top=[(-BASE_END_Y,underside_z(-BASE_END_Y))]
    foundation_top += [(y,underside_z(y)) for y,z in TOP_PROFILE
                       if -BASE_END_Y < y < BASE_END_Y]
    foundation_top += [(BASE_END_Y,underside_z(BASE_END_Y))]
    base_profile=foundation_top+[(BASE_END_Y,0.0),(-BASE_END_Y,0.0)]
    plate_profile=list(TOP_PROFILE)+[
        (y,underside_z(y)) for y,z in reversed(TOP_PROFILE)]
    visual = [
        prism_yz('CONCRETE_FOUNDATION', -BASE_HALF_WIDTH, BASE_HALF_WIDTH,
                 base_profile, concrete),
        prism_yz('CONTINUOUS_STEEL_DECK', -HALF_WIDTH, HALF_WIDTH,
                 plate_profile, steel),
    ]

    for side, label in ((-1, 'L'), (1, 'R')):
        # Stiffener plates sit against the concrete side wall, below the steel.
        for idx, y in enumerate((-1.35, -0.85, -0.35, 0.35, 0.85, 1.35)):
            ztop = top_z(y) - PLATE_THICKNESS
            height = max(0.06, ztop - 0.06)
            visual.append(block('SIDE_RIB_%s_%02d' % (label, idx),
                                (side * 1.231, y, ztop / 2),
                                (0.028, 0.065, height), steel))
        # Low-visibility edge hazard panels: outside drive envelope.
        for idx, y in enumerate((-1.55, -1.05, -0.55, 0, 0.55, 1.05, 1.55)):
            ztop = top_z(y) - PLATE_THICKNESS
            visual.append(block('AMBER_SIDE_MARK_%s_%02d' % (label, idx),
                                (side * 1.231, y, min(0.23, ztop / 2)),
                                (0.025, 0.105, 0.045), amber))
        for idx, y in enumerate((-2.1, -1.5, -0.9, -0.3, 0.3, 0.9, 1.5, 2.1)):
            visual.append(bolt('DECK_SIDE_FASTENER_%s_%02d' % (label, idx),
                               side, y, amber))

    # Curved C1 cross-section is NONCONVEX: STATIC MeshCollider proposal
    # only (convex=false); not valid as a dynamic Rigidbody collider.
    # Road top coordinates are identical to the visible steel deck.
    collider_profile=list(TOP_PROFILE)+[
        (HALF_LENGTH,-0.080),(-HALF_LENGTH,-0.080)]
    collider = prism_yz('COLLIDER_PROPOSAL_RAMP', -HALF_WIDTH, HALF_WIDTH,
                        collider_profile, concrete)
    collider['role'] = 'STATIC_NONCONVEX_MESH_COLLIDER_PROPOSAL_ONLY'
    collider['not_gameplay_integrated'] = True
    collider.hide_render = True
    collider.display_type = 'WIRE'

    metrics=profile_metrics()
    assert metrics['floor_entry_step_m'] <= 0.00500001
    assert metrics['maximum_abs_segment_grade_deg'] <= 16.0
    assert metrics['maximum_adjacent_grade_change_deg'] <= 4.0
    assert len(TOP_PROFILE) >= 24
    assert all(underside_z(y) >= 0 for y,z in TOP_PROFILE)
    assert min(underside_z(y) for y,z in foundation_top) > 0
    assert all(abs(z-top_z(-y)) < 1e-8 for y,z in TOP_PROFILE)
    for x in (-HALF_WIDTH,HALF_WIDTH):
        def road_top(obj):
            return {(round(v.co.y,7),round(v.co.z,7))
                    for v in obj.data.vertices if abs(v.co.x-x)<1e-6
                    and any(abs(v.co.y-py)<1e-7 and abs(v.co.z-pz)<1e-7
                            for py,pz in TOP_PROFILE)}
        assert road_top(visual[1])==road_top(collider)
        assert len(road_top(collider))==len(TOP_PROFILE)
    tri_count = sum(sum(len(p.vertices) - 2 for p in obj.data.polygons)
                    for obj in visual)
    if not 200 <= tri_count <= 1500:
        raise AssertionError('Triangle budget exceeded: %d' % tri_count)
    audit = manifold_audit(visual + [collider])
    assert len(materials) <= 3
    assert not bpy.data.actions, 'Unexpected animation actions'
    assert abs(scene.unit_settings.scale_length - 1) < 1e-9
    assert all(abs(obj.scale.x - 1) < 1e-6 and
               abs(obj.scale.y - 1) < 1e-6 and
               abs(obj.scale.z - 1) < 1e-6 for obj in visual)

    visual_glb = out / 'ENV02-001_v002_visual.glb'
    collider_glb = out / 'ENV02-001_v002_collider_proposal.glb'
    blend_path = out / 'ENV02-001_v002.blend'
    export_glb(visual, visual_glb)
    export_glb([collider], collider_glb)
    set_selected(visual)
    collider.hide_set(False)
    collider.hide_render = True
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_cavity = True
    scene.render.resolution_x = 960
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.world.color = (0.12, 0.14, 0.17)
    render_preview(scene, out / 'ENV02-001_v002_view_3q.png',
                   (5.8, -7.5, 5.0), (0, 0, 0.22), 7.5)
    render_preview(scene, out / 'ENV02-001_v002_view_side.png',
                   (8.0, 0.0, 1.0), (0, 0, 0.22), 6.3)
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    evidence = {
        'id': ASSET_ID,
        'blender_version': bpy.app.version_string,
        'unit_m_per_blender_unit': scene.unit_settings.scale_length,
        'axes_blender': {'right': '+X', 'drive_forward': '+Y', 'up': '+Z'},
        'origin': 'world (0,0,0), centre of footprint at ground level',
        'visual_meshes': len(visual),
        'visual_triangles': tri_count,
        'material_names': sorted(materials),
        'animation': 'NOT_APPLICABLE_STATIC',
        'revision':'v002',
        'profile_metrics':metrics,
        'top_profile_yz_m':TOP_PROFILE,
        'deck_underside_yz_m':[(y,underside_z(y)) for y,z in TOP_PROFILE],
        'collider_top_matches_visual':True,
        'side_fastener_top_clearance_m':0.023,
        'collider': 'static nonconvex closed mesh, exact visual top, not Unity integrated',
        'manifold_all': all(not v['non_manifold_edges'] and not v['degenerate_faces']
                            and v['signed_volume_m3'] > 0 for v in audit.values()),
        'meshes_audited': len(audit),
        'files': {p.name: {'bytes': p.stat().st_size, 'sha256': sha256(p)}
                  for p in (blend_path, visual_glb, collider_glb,
                            out / 'ENV02-001_v002_view_3q.png',
                            out / 'ENV02-001_v002_view_side.png')},
    }
    (out / 'ENV02-001_v002_blender_qa.json').write_text(
        json.dumps(evidence, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print('ENV02_001_V002_BLENDER_QA_PASS ' + json.dumps({
        'triangles': tri_count, 'meshes': len(visual),
        'materials': len(materials), 'manifold': evidence['manifold_all'],
        'files': sorted(evidence['files'])}))


if __name__ == '__main__':
    main()
