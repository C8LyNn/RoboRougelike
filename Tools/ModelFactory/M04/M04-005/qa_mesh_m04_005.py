#!/usr/bin/env python3
"""Independent OBJ/MTL integrity audit (no Blender, no external dependencies)."""
import argparse
import json
from pathlib import Path


def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--asset-dir',required=True)
    args=ap.parse_args()
    root=Path(args.asset_dir)
    obj=(root/'M04-005.obj').read_text(encoding='utf-8').splitlines()
    mtl=(root/'M04-005.mtl').read_text(encoding='utf-8').splitlines()
    qa=json.loads((root/'M04-005.static-qa.json').read_text(encoding='utf-8'))
    verts=[];faces=[];parts=[];materials=[];current=None
    for line in obj:
        if line.startswith('o '):
            current=line[2:];parts.append(current)
        elif line.startswith('usemtl '):
            assert current is not None
            materials.append(line[7:])
        elif line.startswith('v '):
            xyz=tuple(float(x) for x in line.split()[1:]);assert len(xyz)==3
            verts.append(xyz)
        elif line.startswith('f '):
            ids=tuple(int(x) for x in line.split()[1:]);assert len(ids)>=3
            assert len(ids)==len(set(ids))
            assert all(1<=i<=len(verts) for i in ids)
            faces.append(ids)
    material_defs=[line.split()[1] for line in mtl if line.startswith('newmtl ')]
    assert len(parts)==len(set(parts))==qa['parts']==62
    assert len(verts)==qa['vertices']==3792
    assert len(faces)==qa['polygons']==3700
    assert len(materials)==len(parts)
    assert len(set(material_defs))==8
    assert set(materials)==set(material_defs)==set(qa['materials'])
    assert qa['fixed_parts']==54 and qa['moving_parts']==8
    assert qa['minimum_flap_to_fixed_y_gap_m']>0.01
    assert qa['minimum_between_chambers_x_gap_m']>0.1
    assert qa['fixed_moving_plane_checks']==21*54*8
    print('M04-005 independent OBJ integrity PASS',len(parts),'parts',len(verts),'vertices',len(faces),'faces')


if __name__=='__main__':
    main()
