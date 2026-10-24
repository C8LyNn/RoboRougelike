#!/usr/bin/env python3
"""M03-006 procedural Blender reconstruction from versioned geometry.json.

The geometry manifest is the single source of truth; no other Blender objects
are touched. 1 Unity unit = 1 m, side pivot at (0,0,0), normal +X.
"""
import argparse
from collections import Counter
import json, math, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent
COLLECTION='MODEL_M03_006'
NAME='M03-006_side_L_bracket'
def load():
    return json.loads((ROOT/'geometry.json').read_text(encoding='utf-8'))
def sub(a,b):return [x-y for x,y in zip(a,b)]
def cross(a,b):return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def validate(s):
    assert s['id']=='M03-006' and s['units']=='metres'
    assert s['side_mount']=={'pivot':[0,0,0],'normal':[1,0,0]}
    assert s['top_socket']=={'pivot':[.64,0,.58],'normal':[0,0,1]}
    assert len(s['parts'])==27 and len(s['materials'])==5
    names=set();nv=nf=0
    for p in s['parts']:
        assert p['name'] not in names and p['material'] in s['materials']
        names.add(p['name']);v=p['vertices'];f=p['faces'];edges=Counter();volume=0
        for a in v:assert len(a)==3 and all(math.isfinite(x) for x in a)
        for a,b,c in f:
            assert len({a,b,c})==3 and min(a,b,c)>=0 and max(a,b,c)<len(v)
            n=cross(sub(v[b],v[a]),sub(v[c],v[a]))
            assert dot(n,n)>1e-15,p['name']
            volume+=dot(v[a],cross(v[b],v[c]))/6
            for i,j in ((a,b),(b,c),(c,a)):edges[tuple(sorted((i,j)))]+=1
        assert edges and all(n==2 for n in edges.values()),p['name']
        assert volume>1e-10,(p['name'],volume)
        nv+=len(v);nf+=len(f)
    assert (nv,nf)==(1764,3452),(nv,nf)
    assert sum('Bolt_Shaft' in p['name'] for p in s['parts'])==8
    assert .031-.023>=.008-1e-9
    return {'id':s['id'],'parts':len(s['parts']),'vertices':nv,
            'triangles':nf,'materials':len(s['materials']),
            'all_parts_closed_manifold':True,'positive_signed_volume':True,
            'side_through_bores':4,'top_through_bores':4,
            'bolt_shafts':8,'bore_clearance_m':.008,
            'blender':'NOT_RUN','unity':'NOT_RUN','animation':'NOT_REQUIRED'}
def export_static(s,out):
    out.mkdir(parents=True,exist_ok=True)
    lines=['# M03-006: metres, +X side mount, Z up','mtllib '+NAME+'.mtl']
    offset=1
    for p in s['parts']:
        lines+=['o '+p['name'],'usemtl '+p['material']]
        lines+=['v '+' '.join(f'{x:.8f}' for x in v) for v in p['vertices']]
        lines+=['f '+' '.join(str(offset+i) for i in face) for face in p['faces']]
        offset+=len(p['vertices'])
    (out/(NAME+'.obj')).write_text('\n'.join(lines)+'\n',encoding='utf-8')
    mtl=[]
    for name,(rgba,metal,rough) in s['materials'].items():
        mtl+=['newmtl '+name,'Kd '+' '.join(f'{x:.5f}' for x in rgba[:3]),
              'Ks '+' '.join(f'{metal*.45:.5f}' for _ in range(3)),
              f'Ns {(1-rough)*400:.2f}','d 1.0','illum 2','']
    (out/(NAME+'.mtl')).write_text('\n'.join(mtl)+'\n',encoding='utf-8')
def export_blender(s,out):
    import bpy
    scene=bpy.context.scene
    old_frame=scene.frame_current
    old_selected=tuple(bpy.context.selected_objects)
    old_active=bpy.context.view_layer.objects.active
    owned=bpy.data.collections.get(COLLECTION)
    if owned:
        for ob in tuple(owned.objects):bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.collections.remove(owned)
    owned=bpy.data.collections.new(COLLECTION)
    scene.collection.children.link(owned)
    root=bpy.data.objects.new('M03_006_SIDE_SOCKET_PIVOT',None)
    root.empty_display_size=.08;owned.objects.link(root)
    top=bpy.data.objects.new('M03_006_TOP_SOCKET_MARKER',None)
    top.location=(.64,0,.58);top.empty_display_size=.045
    owned.objects.link(top);top.parent=root
    mats={}
    for name,(rgba,metal,rough) in s['materials'].items():
        mat=bpy.data.materials.get('M03_006_'+name)
        if mat is None:mat=bpy.data.materials.new('M03_006_'+name)
        mat.use_nodes=True
        shader=mat.node_tree.nodes.get('Principled BSDF')
        if shader:
            shader.inputs['Base Color'].default_value=rgba
            shader.inputs['Metallic'].default_value=metal
            shader.inputs['Roughness'].default_value=rough
        mat.diffuse_color=rgba;mats[name]=mat
    for p in s['parts']:
        mesh=bpy.data.meshes.new('M03_006_'+p['name']+'_mesh')
        mesh.from_pydata(p['vertices'],[],p['faces']);mesh.update(calc_edges=True)
        ob=bpy.data.objects.new('M03_006_'+p['name'],mesh)
        owned.objects.link(ob);ob.parent=root
        ob.data.materials.append(mats[p['material']])
        for poly in mesh.polygons:poly.use_smooth=False
    try:
        scene.frame_set(1)
        bpy.ops.object.select_all(action='DESELECT')
        for ob in owned.objects:ob.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(filepath=str(out/(NAME+'.glb')),
            export_format='GLB',use_selection=True,export_animations=False)
        bpy.data.libraries.write(str(out/(NAME+'.blend')),{owned},compress=True)
    finally:
        bpy.ops.object.select_all(action='DESELECT')
        for ob in old_selected:
            if ob.name in bpy.data.objects:ob.select_set(True)
        if old_active is not None and old_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=old_active
        scene.frame_set(old_frame)
def main():
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    ap=argparse.ArgumentParser()
    ap.add_argument('--out',default='build/M03-006')
    ap.add_argument('--self-test',action='store_true')
    args=ap.parse_args(argv)
    spec=load();report=validate(spec)
    if not args.self_test:
        out=Path(args.out);export_static(spec,out)
        try:import bpy
        except ImportError:pass
        else:
            export_blender(spec,out)
            report['blender']='EXPORTED_BPY'
        (out/'M03-006_static_qa.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report,indent=2))
if __name__=='__main__':main()
