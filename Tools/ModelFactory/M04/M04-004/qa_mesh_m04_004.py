#!/usr/bin/env python3
"""Independent manifold/winding/material audit for M04-004 static OBJ."""
import argparse
import collections
import json
from pathlib import Path


def audit(obj_path, mtl_path):
    materials=set()
    for line in mtl_path.read_text(encoding="utf-8").splitlines():
        if line.startswith("newmtl "):
            materials.add(line.split(maxsplit=1)[1])
    assert len(materials)>=3
    vertices=[]
    parts=[]
    current=None
    for line in obj_path.read_text(encoding="utf-8").splitlines():
        if line.startswith("v "):
            vertices.append(tuple(float(x) for x in line.split()[1:4]))
        elif line.startswith("o "):
            current={"name":line[2:],"material":None,"faces":0,
                     "edges":collections.Counter(),"volume":0.0}
            parts.append(current)
        elif line.startswith("usemtl "):
            assert current is not None
            current["material"]=line.split(maxsplit=1)[1]
            assert current["material"] in materials
        elif line.startswith("f "):
            assert current is not None and current["material"]
            indices=[int(s.split("/")[0])-1 for s in line.split()[1:]]
            assert len(indices)>=3
            assert all(0<=i<len(vertices) for i in indices)
            current["faces"]+=1
            for i in range(len(indices)):
                a,b=indices[i],indices[(i+1)%len(indices)]
                current["edges"][tuple(sorted((a,b)))]+=1
            for i in range(1,len(indices)-1):
                a,b,c=(vertices[indices[k]] for k in (0,i,i+1))
                cross=(b[1]*c[2]-b[2]*c[1],
                       b[2]*c[0]-b[0]*c[2],
                       b[0]*c[1]-b[1]*c[0])
                current["volume"]+=sum(a[k]*cross[k] for k in range(3))/6.0
    assert len(parts)>=20
    for p in parts:
        assert p["faces"]>=6,p["name"]
        bad=[(e,n) for e,n in p["edges"].items() if n!=2]
        assert not bad,(p["name"],"nonmanifold",bad[:4])
        assert p["volume"]>1e-9,(p["name"],"winding/zero-volume",p["volume"])
    return {"objects":len(parts),"vertices":len(vertices),
            "faces":sum(p["faces"] for p in parts),
            "materials":sorted({p["material"] for p in parts}),
            "manifold_closed":"PASS","outward_winding":"PASS",
            "minimum_signed_volume_m3":min(p["volume"] for p in parts)}


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--asset-dir",type=Path,required=True)
    args=parser.parse_args()
    result=audit(args.asset_dir/"M04-004.obj",args.asset_dir/"M04-004.mtl")
    print(json.dumps(result,indent=2))


if __name__=="__main__":
    main()
