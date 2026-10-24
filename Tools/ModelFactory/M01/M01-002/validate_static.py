#!/usr/bin/env python3
"""M01-002 static OBJ/MTL validation; requires Python 3 standard library only."""
from pathlib import Path
import json
import math
p=Path(__file__).resolve().parent
obj=p/"M01-002.obj"
mtl=p/"M01-002.mtl"
verts=[]
faces=0
groups=set()
used=set()
for line in obj.read_text(encoding="utf-8").splitlines():
    tokens=line.split()
    if not tokens: continue
    if tokens[0]=="v":
        v=[float(x) for x in tokens[1:]]
        assert len(v)==3 and all(math.isfinite(x) for x in v)
        verts.append(v)
    elif tokens[0]=="f":
        ix=[int(x.split("/")[0]) for x in tokens[1:]]
        assert len(ix)==3 and len(set(ix))==3
        assert all(1<=i<=len(verts) for i in ix)
        faces+=1
    elif tokens[0]=="o":groups.add(tokens[1])
    elif tokens[0]=="usemtl":used.add(tokens[1])
defined={line.split()[1] for line in mtl.read_text(encoding="utf-8").splitlines() if line.startswith("newmtl ")}
assert len(verts)>1000 and faces>1000
assert len(groups)>=45 and len(used)>=3
assert used<=defined
mn=[min(v[i] for v in verts) for i in range(3)]
mx=[max(v[i] for v in verts) for i in range(3)]
assert abs((mn[0]+mx[0])/2)<.03, "asymmetric weapon"
assert mn[2]>=-.0001 and mx[2]<1.2, "unexpected Z bounds"
assert mn[1]<-1.35 and mx[1]<.8, "unexpected Y bounds"
print(json.dumps({"status":"PASS","groups":len(groups),"vertices":len(verts),
 "triangles":faces,"materials":len(used),"bounds_m":[mn,mx]},indent=2))
