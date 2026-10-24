#!/usr/bin/env python3
"""Pure-stdlib CI smoke QA for M04-002; no Blender/Unity required."""
import collections
import json
import importlib.util
import math
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
        assert manifest['vertices']==8528, manifest['vertices']
        assert manifest['faces']==8010, manifest['faces']
        assert manifest['hollow_hub_shells']==6
        assert manifest['shaft_hub_radial_clearance_m']>=.008
        assert manifest['mount_root']=='M04_002_ROOT_SIDE_X_POS'
        assert manifest['intermediate_rotation_keys_per_pivot']==17
        assert manifest['blade_root_radial_station_m']==.076
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

        # Inspect the actual mesh source: all six hubs need annular through
        # bores around the fixed axle, not solid cylinders blocking the shaft.
        spec=importlib.util.spec_from_file_location('m04_002_generator',HERE/'generate_m04_002.py')
        mod=importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)
        parts=mod.build()
        assert len(parts)==62
        hubs=[p for p in parts if p[0].startswith(('Hub_', 'HubRing_', 'RotorCore_'))]
        assert len(hubs)==6, [p[0] for p in hubs]
        for name,vs,fs,material,rotor in hubs:
            radii=[math.hypot(v[1],v[2]) for v in vs]
            assert abs(min(radii)-.047)<1e-6, (name, min(radii))
            assert min(radii)-mod.STATIONARY_SHAFT_RADIUS>=.008
            assert len(vs)==192 and len(fs)==192, name
            assert rotor in (0,1)
        blades=[p for p in parts if p[0].startswith('Blade_')]
        assert len(blades)==12
        for name,vs,fs,material,rotor in blades:
            # Each blade root penetrates the hub shell by a few millimetres,
            # instead of floating away from it as in the original generator.
            root_tip=max(math.hypot(v[1],v[2]) for v in vs[:4])
            assert root_tip < .091, (name, root_tip)
        assert len(range(mod.FRAMES[0],mod.FRAMES[1]+1,
                         mod.BLENDER_ANIMATION_KEY_STEP))==17
        assert all(len(p[1])>0 for p in parts)
        print('PASS M04-002: 62 manifold meshes; 8528 vertices; 8010 faces;'
              ' 6 genuine hollow hubs; 12 attached blades; 17 keys each;'
              ' shaft clearance >= 8 mm; Blender/Unity NOT RUN')

if __name__=='__main__':main()
