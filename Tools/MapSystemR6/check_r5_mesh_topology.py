"""Offline equivalent of MapR5RoadMesh.Build triangle indexing and edge manifold QA.
Cannot verify rendering/PhysX. Tests indexed closed surface winding.
"""
from collections import Counter
from pathlib import Path
import json,math,sys
root=Path(__file__).resolve().parents[1]
m=json.loads((root/'Assets/RoboRougelike/MapSystemR4/Samples/Resources/MAP_R4_quarry_03.json').read_text())
errors=[]
for road in m['edges']:
    n=road['roadSegments']
    count=n+1
    tris=[]
    def add(a,b,c):tris.append((a,b,c))
    for i in range(n):
        l=2*i;r=l+1;nl=l+2;nr=l+3
        bl=2*count+l;br=bl+1;bnl=bl+2;bnr=bl+3
        add(l,nl,r);add(r,nl,nr)
        add(bl,br,bnl);add(br,bnr,bnl)
        add(l,bl,nl);add(nl,bl,bnl)
        add(r,nr,br);add(nr,bnr,br)
    bottom=2*count
    add(0,1,bottom);add(1,bottom+1,bottom)
    last=2*(count-1)
    add(last,bottom+last,last+1);add(last+1,bottom+last,bottom+last+1)
    edge_counts=Counter(tuple(sorted((a,b))) for tri in tris for a,b in
                        ((tri[0],tri[1]),(tri[1],tri[2]),(tri[2],tri[0])))
    winding=Counter((a,b) for tri in tris for a,b in
                    ((tri[0],tri[1]),(tri[1],tri[2]),(tri[2],tri[0])))
    if len(tris)!=8*n+4:errors.append(f"Bad triangle count {road['id']}")
    for k,v in edge_counts.items():
        if v!=2: errors.append(f"Non-manifold edge {road['id']} {k}: {v}")
        a,b=k
        if winding[(a,b)]!=1 or winding[(b,a)]!=1:
            errors.append(f"Inconsistent winding {road['id']} {k}")
print(f"R5 mesh topology: {len(m['edges'])} meshes / {sum((8*e['roadSegments']+4) for e in m['edges'])} triangles / {len(errors)} errors")
for e in errors[:20]: print('ERROR:',e)
sys.exit(1 if errors else 0)
