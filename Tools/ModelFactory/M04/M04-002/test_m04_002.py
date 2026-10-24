#!/usr/bin/env python3
"""Pure-stdlib CI smoke QA for M04-002; no Blender/Unity required."""
import collections
import json
from pathlib import Path
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent

def main():
    with tempfile.TemporaryDirectory() as d:
        subprocess.run([sys.executable,str(HERE/'generate_m04_002.py'),'--out',d],check=True,capture_output=True,text=True)
        folder=Path(d)
        manifest=json.loads((folder/'M04-002.manifest.json').read_text())
        assert manifest['parts']==62, manifest['parts']
        assert manifest['vertices']==7952, manifest['vertices']
        assert manifest['faces']==7158, manifest['faces']
        assert manifest['minimum_nominal_radial_clearance_m']>=0.03
        assert manifest['minimum_nominal_axial_clearance_m']>=0.10
        assert manifest['glb'] is None, 'CPython should not claim Blender export'
        obj=(folder/'M04-002.obj').read_text().splitlines()
        mtl=(folder/'M04-002.mtl').read_text()
        vertex_count=0; objects={}; name=None; mats=set(); edges=collections.defaultdict(collections.Counter)
        for line in obj:
            if line.startswith('v '):
                vertex_count+=1
                coords=[float(v) for v in line.split()[1:]]
                assert len(coords)==3 and all(abs(c)<10 for c in coords)
            elif line.startswith('o '):
                name=line[2:]; assert name not in objects
                objects[name]=0
            elif line.startswith('usemtl '):
                mats.add(line.split()[1])
            elif line.startswith('f '):
                assert name
                face=[int(x) for x in line.split()[1:]]
                assert len(face)>=3 and all(0<i<=vertex_count for i in face)
                objects[name]+=1
                for i in range(len(face)):
                    a,b=face[i],face[(i+1)%len(face)]
                    edges[name][tuple(sorted((a,b)))]+=1
        assert vertex_count==manifest['vertices']
        assert len(objects)==manifest['parts']
        assert sum(objects.values())==manifest['faces']
        assert len(mats)>=3
        assert all('newmtl '+m in mtl for m in mats)
        assert all(all(n==2 for n in counts.values()) for counts in edges.values()), 'Nonmanifold object'
        assert sum(k.startswith('Blade_0_') for k in objects)==5
        assert sum(k.startswith('Blade_1_') for k in objects)==7
        print('PASS M04-002: 62 closed objects; 7952 vertices; 7158 polygons; 6+ materials; 12 blades; clearance bounds')

if __name__=='__main__':main()
