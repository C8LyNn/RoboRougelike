#!/usr/bin/env python3
"""Independent stdlib validation of physical Blender outputs for ENV02-001."""
import hashlib
import json
import struct
import sys
from pathlib import Path

EXPECTED = ('ENV02-001.blend', 'ENV02-001_visual.glb',
            'ENV02-001_collider_proposal.glb', 'ENV02-001_view_3q.png',
            'ENV02-001_view_side.png', 'ENV02-001_blender_qa.json')


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
    return document


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

    visual = glb_json(out / 'ENV02-001_visual.glb')
    collider = glb_json(out / 'ENV02-001_collider_proposal.glb')
    assert not visual.get('animations') and not visual.get('skins')
    assert not collider.get('animations') and not collider.get('skins')
    assert len(visual.get('materials', [])) <= 3
    assert len(visual.get('meshes', [])) == qa['visual_meshes']
    assert count_tris(visual) == qa['visual_triangles'], (count_tris(visual), qa['visual_triangles'])
    assert len(collider.get('meshes', [])) == 1
    assert count_tris(collider) <= 30
    check_png(out / 'ENV02-001_view_3q.png')
    check_png(out / 'ENV02-001_view_side.png')
    # Blender may save compressed .blend (gzip or Zstandard); CI separately reopens it in Blender.
    blend_magic = (out / 'ENV02-001.blend').read_bytes()[:8]
    assert (blend_magic.startswith(b'BLENDER') or
            blend_magic.startswith(bytes.fromhex('1f8b')) or
            blend_magic.startswith(bytes.fromhex('28b52ffd'))), blend_magic.hex()
    manifest = {
        'asset_id': 'ENV02-001',
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
    (out / 'ENV02-001_manifest.json').write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print('ENV02_001_INDEPENDENT_QA_PASS ' + json.dumps({
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
