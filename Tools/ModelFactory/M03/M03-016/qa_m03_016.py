#!/usr/bin/env python3
"""Independent geometry and exported OBJ/MTL QA for M03-016 (stdlib only)."""
import hashlib
import importlib.util
import json
import math
import tempfile
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("m03_016", HERE/"generate_m03_016.py")
model = importlib.util.module_from_spec(spec)
spec.loader.exec_module(model)


def bounds(m):
    return [(min(v[i] for v in m["vertices"]),max(v[i] for v in m["vertices"]))
            for i in range(3)]


def verify_mesh(m):
    vertices, faces = m["vertices"], m["faces"]
    assert vertices and faces, m["name"]
    edge_counts = Counter()
    directed = Counter()
    for face in faces:
        assert len(face) >= 3 and len(set(face)) == len(face), m["name"]
        for i in face:
            assert 0 <= i < len(vertices), m["name"]
        for i,j,k in model.triangulate(face):
            a,b,c = vertices[i],vertices[j],vertices[k]
            ab = [b[t]-a[t] for t in range(3)]
            ac = [c[t]-a[t] for t in range(3)]
            cross = (ab[1]*ac[2]-ab[2]*ac[1],
                     ab[2]*ac[0]-ab[0]*ac[2],
                     ab[0]*ac[1]-ab[1]*ac[0])
            assert sum(x*x for x in cross) > 1e-16, m["name"]
        for i,j in zip(face,face[1:]+face[:1]):
            edge_counts[tuple(sorted((i,j)))] += 1
            directed[(i,j)] += 1
    assert edge_counts and all(n == 2 for n in edge_counts.values()), m["name"]
    for (i,j),n in directed.items():
        assert n == 1 and directed[(j,i)] == 1, m["name"]
    assert model.volume(vertices,faces) > 1e-8, m["name"]
    return bounds(m)


def main():
    meshes = model.build()
    assert len(meshes) == 24
    assert len({m["name"] for m in meshes}) == len(meshes)
    assert len({m["material"] for m in meshes}) >= 3
    bb = [verify_mesh(m) for m in meshes]
    # Positive-volume overlap is forbidden; face-to-face structural joins are allowed.
    tested = 0
    for i in range(len(meshes)):
        for j in range(i+1,len(meshes)):
            tested += 1
            overlaps = [min(bb[i][axis][1],bb[j][axis][1]) -
                        max(bb[i][axis][0],bb[j][axis][0]) for axis in range(3)]
            assert not all(d > 1e-6 for d in overlaps), (
                "Unintended AABB intersection",meshes[i]["name"],meshes[j]["name"],overlaps)
    rails = [m for m in meshes if m["name"].endswith("_THROUGH_SLOT")]
    assert len(rails) == 4
    for rail in rails:
        # Outer and inner contours on both front/back, 4 inner wall faces:
        assert len(rail["vertices"]) == 16 and len(rail["faces"]) == 16
        assert sum(1 for f in rail["faces"] if
                   len(f)==4 and all(v in (4,5,6,7,12,13,14,15) for v in f)) == 4
    assert len(model.SNAPS) == 4 and len(set(model.SNAPS)) == 4
    assert all(z == 0.044 for x,y,z in model.SNAPS)
    with tempfile.TemporaryDirectory() as d:
        p = Path(d)
        manifest = model.write_obj(p,meshes)
        first = {n:(p/n).read_bytes() for n in
                 ("M03-016.obj","M03-016.mtl","M03-016.manifest.json")}
        model.write_obj(p,model.build())
        for n,data in first.items():
            assert (p/n).read_bytes() == data, "Nondeterministic "+n
        obj = first["M03-016.obj"].decode()
        mtl = first["M03-016.mtl"].decode()
        verts = [line for line in obj.splitlines() if line.startswith("v ")]
        faces = [line for line in obj.splitlines() if line.startswith("f ")]
        assert len(verts) == manifest["vertex_count"]
        assert len(faces) == manifest["triangle_count"]
        assert len([l for l in obj.splitlines() if l.startswith("o ")]) == 24
        assert len([l for l in mtl.splitlines() if l.startswith("newmtl ")]) == 5
        for line in faces:
            ids = [int(v) for v in line.split()[1:]]
            assert len(ids)==3 and all(1<=i<=len(verts) for i in ids)
        assert manifest["through_slots"] == 4
        assert manifest["animation"] == "none"
        print(json.dumps({
            "status":"PASS", "id":model.ID, "meshes":len(meshes),
            "materials":len(model.MATERIALS),
            "through_slots":len(rails),"snap_mounts":len(model.SNAPS),
            "aabb_pairs_checked":tested,
            "vertices":len(verts),"triangles":len(faces),
            "obj_sha256":hashlib.sha256(first["M03-016.obj"]).hexdigest(),
            "deterministic":True},sort_keys=True))


if __name__ == "__main__":
    main()
