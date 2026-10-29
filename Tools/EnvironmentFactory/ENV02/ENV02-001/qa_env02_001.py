#!/usr/bin/env python3
"""Independent stdlib validation of physical Blender outputs for ENV02-001."""
import hashlib
import json
import math
import struct
import sys
from pathlib import Path

EXPECTED = ('ENV02-001_v002.blend', 'ENV02-001_v002_visual.glb',
            'ENV02-001_v002_collider_proposal.glb', 'ENV02-001_v002_view_3q.png',
            'ENV02-001_v002_view_side.png', 'ENV02-001_v002_blender_qa.json')


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def glb_json(path):
    raw = path.read_bytes()
    assert len(raw) >= 20 and raw[:4] == b'glTF', path
    version, total = struct.unpack_from('<II', raw, 4)
    assert version == 2 and total == len(raw), (version, total, len(raw))
    chunk_size, chunk_type = struct.unpack_from('<I4s', raw, 12)
    assert chunk_type == b'JSON'
    document = json.loads(raw[20:20 + chunk_size].decode('utf-8'))
    assert document['asset']['version'] == '2.0'
    binary_start=20+chunk_size+8
    return document,raw[binary_start:]


def accessor_data(doc,binary,index):
    acc=doc['accessors'][index]
    view=doc['bufferViews'][acc['bufferView']]
    comps={'SCALAR':1,'VEC3':3}[acc['type']]
    fmt={5126:'f',5125:'I',5123:'H',5121:'B'}[acc['componentType']]
    width=struct.calcsize('<'+fmt*comps)
    start=view.get('byteOffset',0)+acc.get('byteOffset',0)
    stride=view.get('byteStride',width)
    return [struct.unpack_from('<'+fmt*comps,binary,start+i*stride)
            for i in range(acc['count'])]


def validate_mesh_triangles(doc,binary):
    for mesh in doc.get('meshes',[]):
        for prim in mesh['primitives']:
            vertices=accessor_data(doc,binary,prim['attributes']['POSITION'])
            indices=[v[0] for v in accessor_data(doc,binary,prim['indices'])]
            assert len(indices)%3==0
            for i in range(0,len(indices),3):
                a,b,c=[vertices[j] for j in indices[i:i+3]]
                u=[b[k]-a[k] for k in range(3)]
                v=[c[k]-a[k] for k in range(3)]
                cross=(u[1]*v[2]-u[2]*v[1],
                       u[2]*v[0]-u[0]*v[2],
                       u[0]*v[1]-u[1]*v[0])
                area2=sum(x*x for x in cross)
                assert area2>1e-16,(mesh.get('name'),i,area2)


def check_matching_road_top(visual,visual_binary,collider,collider_binary,profile):
    def positions(doc,binary,name):
        mesh=next(m for m in doc['meshes'] if name in m.get('name',''))
        return accessor_data(doc,binary,mesh['primitives'][0]['attributes']['POSITION'])
    deck=positions(visual,visual_binary,'CONTINUOUS_STEEL_DECK')
    proposal=positions(collider,collider_binary,'COLLIDER_PROPOSAL_RAMP')
    # glTF Y-up: Blender (x,y,z) -> (x,z,-y).
    for x in (-1.3,1.3):
        for y,z in profile:
            target=(x,z,-y)
            assert any(max(abs(p[k]-target[k]) for k in range(3))<2e-5
                       for p in deck),('deck',target)
            assert any(max(abs(p[k]-target[k]) for k in range(3))<2e-5
                       for p in proposal),('collider',target)


def count_tris(doc):
    accessors = doc.get('accessors', [])
    result = 0
    for mesh in doc.get('meshes', []):
        for prim in mesh['primitives']:
            assert prim.get('mode', 4) == 4, 'Only TRIANGLES are supported'
            assert 'indices' in prim, 'Non-indexed primitive not expected'
            count = accessors[prim['indices']]['count']
            assert count % 3 == 0
            result += count // 3
    return result


def check_png(path):
    raw = path.read_bytes()
    assert raw[:8] == b'\x89PNG\r\n\x1a\n'
    assert raw[12:16] == b'IHDR'
    w, h = struct.unpack_from('>II', raw, 16)
    assert (w, h) == (960, 640), (path.name, w, h)


def main(out):
    out = Path(out)
    for name in EXPECTED:
        path = out / name
        assert path.is_file() and path.stat().st_size > 1000, 'Missing file: ' + str(path)
    qa = json.loads((out / EXPECTED[-1]).read_text(encoding='utf-8'))
    assert qa['id'] == 'ENV02-001'
    assert qa['revision']=='v002'
    assert qa['collider_top_matches_visual'] is True
    m=qa['profile_metrics']
    assert m['overall_length_m']==5.0 and m['drive_width_m']==2.6
    assert m['crest_height_m']==0.52 and m['crest_flat_length_m']==0.8
    assert m['floor_entry_step_m']<=0.005001
    assert m['maximum_abs_segment_grade_deg']<=16.0
    assert m['maximum_adjacent_grade_change_deg']<=4.0
    assert m['top_profile_points']>=24
    assert qa['unit_m_per_blender_unit'] == 1.0
    assert qa['manifold_all'] is True
    assert qa['animation'] == 'NOT_APPLICABLE_STATIC'
    assert 200 <= qa['visual_triangles'] <= 1500
    assert len(qa['material_names']) <= 3
    assert set(qa['material_names']) == {'CONCRETE', 'STEEL', 'SAFETY_AMBER'}
    assert qa['meshes_audited'] == qa['visual_meshes'] + 1
    for name, receipt in qa['files'].items():
        path = out / name
        assert receipt['bytes'] == path.stat().st_size
        assert receipt['sha256'] == sha256(path), name

    visual,vbin=glb_json(out / 'ENV02-001_v002_visual.glb')
    collider,cbin=glb_json(out / 'ENV02-001_v002_collider_proposal.glb')
    validate_mesh_triangles(visual,vbin)
    validate_mesh_triangles(collider,cbin)
    check_matching_road_top(visual,vbin,collider,cbin,qa['top_profile_yz_m'])
    assert not visual.get('animations') and not visual.get('skins')
    assert not collider.get('animations') and not collider.get('skins')
    assert len(visual.get('materials', [])) <= 3
    assert len(visual.get('meshes', [])) == qa['visual_meshes']
    assert count_tris(visual) == qa['visual_triangles'], (count_tris(visual), qa['visual_triangles'])
    assert len(collider.get('meshes', [])) == 1
    assert count_tris(collider) <= 140
    check_png(out / 'ENV02-001_v002_view_3q.png')
    check_png(out / 'ENV02-001_v002_view_side.png')
    # Blender may save compressed .blend (gzip or Zstandard); CI separately reopens it in Blender.
    blend_magic = (out / 'ENV02-001_v002.blend').read_bytes()[:8]
    assert (blend_magic.startswith(b'BLENDER') or
            blend_magic.startswith(bytes.fromhex('1f8b')) or
            blend_magic.startswith(bytes.fromhex('28b52ffd'))), blend_magic.hex()
    manifest = {
        'asset_id': 'ENV02-001',
        'revision':'v002',
        'previous_source_sha':'daedcd618d3426ceffea0d1d093fdc3159844c90',
        'previous_artifact_url':'https://github.com/C8LyNn/RoboRougelike/actions/runs/40484667955/artifacts/12723734098',
        'previous_metrics':{'triangles':808,'visual_meshes':44,'materials':3,'length_m':5.0,'width_m':2.6,'crest_height_m':0.52,'flat_crest_length_m':1.4,'tip_height_m':0.045,'grade_deg':14.78},
        'v002_metrics':qa['profile_metrics'],
        'status': 'BLENDER_QA_PASS_UNITY_NOT_RUN',
        'blender_version': qa['blender_version'],
        'visual_triangles': qa['visual_triangles'],
        'visual_meshes': qa['visual_meshes'],
        'material_count': len(visual.get('materials', [])),
        'collider_triangles': count_tris(collider),
        'files': {name: {'bytes': (out / name).stat().st_size,
                         'sha256': sha256(out / name)} for name in EXPECTED},
        'unity_editmode': 'NOT_RUN', 'unity_playmode': 'NOT_RUN',
        'windows': 'NOT_RUN', 'manual_visual_review': 'PENDING',
    }
    (out / 'ENV02-001_v002_manifest.json').write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print('ENV02_001_V002_INDEPENDENT_QA_PASS ' + json.dumps({
        'triangles': manifest['visual_triangles'],
        'meshes': manifest['visual_meshes'],
        'materials': manifest['material_count'],
        'collider_tris': manifest['collider_triangles'],
        'file_count': len(EXPECTED),
    }))


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python qa_env02_001.py <output_directory>')
    main(sys.argv[1])
