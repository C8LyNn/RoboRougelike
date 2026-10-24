#!/usr/bin/env python3
"""M03-014 skid chassis runner. Blender bpy + dependency-free OBJ/MTL fallback."""
import argparse
import json
import math
import sys
from pathlib import Path

ID = 'M03-014'
STEM = 'M03-014_skid_chassis_runner'
MATERIALS = {
    'GraphiteSteel': ((0.10, 0.14, 0.18, 1), 0.72, 0.32),
    'IvoryCeramic': ((0.79, 0.82, 0.76, 1), 0.12, 0.40),
    'CopperMount': ((0.62, 0.30, 0.12, 1), 0.75, 0.29),
    'BlackElastomer': ((0.025, 0.035, 0.045, 1), 0.00, 0.78),
    'AmberIndicator': ((0.98, 0.45, 0.025, 1), 0.10, 0.28),
}
PARTS = []


def signed_volume(verts, faces):
    vol = 0.0
    for face in faces:
        a = verts[face[0]]
        for i in range(1, len(face)-1):
            b, c = verts[face[i]], verts[face[i+1]]
            vol += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                    a[1]*(b[2]*c[0]-b[0]*c[2]) +
                    a[2]*(b[0]*c[1]-b[1]*c[0])) / 6.0
    return vol


def add(name, verts, faces, material):
    assert material in MATERIALS
    assert len(set(tuple(v) for v in verts)) == len(verts), name
    if signed_volume(verts, faces) < 0:
        faces = [tuple(reversed(f)) for f in faces]
    assert signed_volume(verts, faces) > 1e-9, name
    PARTS.append((name, verts, faces, material))


def box(name, center, size, material):
    x,y,z = center; a,b,c = (v/2 for v in size)
    verts = [(x+sx*a,y+sy*b,z+sz*c) for sx,sy,sz in
             [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),
              (-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(3,7,6,2),
             (0,4,7,3),(1,2,6,5)]
    add(name, verts, faces, material)


def extrusion(name, profile_yz, x0, x1, material):
    n=len(profile_yz)
    verts=[(x,y,z) for x in (x0,x1) for y,z in profile_yz]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    add(name,verts,faces,material)


def ring(name, cx, cz, y0, y1, ro, ri, material, sides=24):
    assert 0 < ri < ro
    verts=[]
    for y,r in [(y0,ro),(y1,ro),(y0,ri),(y1,ri)]:
        verts += [(cx+r*math.cos(2*math.pi*i/sides),y,
                   cz+r*math.sin(2*math.pi*i/sides)) for i in range(sides)]
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        faces.extend([(i,j,sides+j,sides+i),
                      (2*sides+i,3*sides+i,3*sides+j,2*sides+j),
                      (sides+i,sides+j,3*sides+j,3*sides+i),
                      (i,2*sides+i,2*sides+j,j)])
    add(name,verts,faces,material)


def build():
    PARTS.clear()
    # Ceramic belly is a continuous closed, rising-at-both-ends ski, not a texture.
    z_bottom=[(-1.00,-.172),(-.90,-.194),(-.72,-.220),(-.45,-.237),
              (0.0,-.242),(.45,-.237),(.72,-.220),(.90,-.194),(1.00,-.172)]
    profile=[(-.150,-1.00),(-.150,1.00)] + [(y,z) for z,y in reversed(z_bottom)]
    extrusion('Ceramic_Belly_Ramped',profile,-.240,.240,'IvoryCeramic')
    box('Graphite_CenterSpine',(0,-.1225,0),(.480,.055,1.70),'GraphiteSteel')
    for side,x in [('L',-.285),('R',.285)]:
        box('Steel_SideRail_'+side,(x,-.1225,0),(.090,.055,1.78),'GraphiteSteel')
        box('Ceramic_SideWear_'+side,((-.345 if side=='L' else .345),-.153,0),
            (.030,.060,1.64),'IvoryCeramic')
        for end,z in [('Aft',-.50),('Fore',.50)]:
            # Pads sit on steel. Annular copper mounts are intentionally hollow.
            box('Rubber_Pad_'+side+'_'+end,(x*.67,-.075,z),(.140,.040,.140),'BlackElastomer')
            ring('Copper_Socket_'+side+'_'+end,x*.67,z,-.055,0,.064,.027,'CopperMount')
        for end,z in [('Aft',-.79),('Fore',.79)]:
            box('Amber_Marker_'+side+'_'+end,(x,-.091,z),(.064,.008,.060),'AmberIndicator')
    return PARTS


def manifest(backend):
    return {'id':ID,'model':'Płoza podwozia ślizgowego','backend':backend,
            'unit':'metre','coordinate_system':'X right, Y up, Z forward',
            'pivot':[0,0,0],'mount_axis':'-Y (bottom of chassis)',
            'mounts':[{'name':f'SNAP_BOTTOM_{side}_{end}',
                       'position':[round(x*.67,6),0,z],'axis':[0,-1,0]}
                      for side,x in [('L',-.285),('R',.285)]
                      for end,z in [('Aft',-.5),('Fore',.5)]],
            'mesh_count':len(PARTS),'materials':list(MATERIALS),
            'animation':'not required (static)','unity_import':'NOT RUN'}


def write_obj(out):
    out.mkdir(parents=True,exist_ok=True)
    lines=[f'mtllib {STEM}.mtl']; index=1
    for name,verts,faces,mat in PARTS:
        lines.extend([f'o {name}',f'usemtl {mat}'])
        lines.extend('v %.7f %.7f %.7f'%v for v in verts)
        for f in faces:
            lines.append('f '+' '.join(str(index+i) for i in f))
        index+=len(verts)
    (out/(STEM+'.obj')).write_text('\n'.join(lines)+'\n',encoding='utf-8')
    mtl=[]
    for name,(color,metal,rough) in MATERIALS.items():
        mtl.extend([f'newmtl {name}','Kd %.6f %.6f %.6f'%color[:3],
                    f'Ns {max(1,100*(1-rough)):.4f}','illum 2',''])
    (out/(STEM+'.mtl')).write_text('\n'.join(mtl),encoding='utf-8')


def write_blender(out):
    import bpy
    coll=bpy.data.collections.get('MODEL_M03_014')
    if coll:
        for obj in list(coll.objects): bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(coll)
    coll=bpy.data.collections.new('MODEL_M03_014')
    bpy.context.scene.collection.children.link(coll)
    root=bpy.data.objects.new('MOUNT_BOTTOM_FRAME',None)
    coll.objects.link(root)
    # Source geometry is Y-up. Blender scene is Z-up; GLB export restores Y-up.
    root.rotation_euler[0]=math.pi/2
    mats={}
    for name,(color,metal,rough) in MATERIALS.items():
        mat=bpy.data.materials.get('M03_014_'+name) or bpy.data.materials.new('M03_014_'+name)
        mat.diffuse_color=color;mat.use_nodes=True
        bsdf=mat.node_tree.nodes.get('Principled BSDF')
        if bsdf:
            bsdf.inputs['Base Color'].default_value=color
            bsdf.inputs['Metallic'].default_value=metal
            bsdf.inputs['Roughness'].default_value=rough
        mats[name]=mat
    for name,verts,faces,material in PARTS:
        mesh=bpy.data.meshes.new('M03_014_'+name)
        mesh.from_pydata(verts,[],faces);mesh.update()
        mesh.materials.append(mats[material])
        obj=bpy.data.objects.new(name,mesh);coll.objects.link(obj);obj.parent=root
    for item in manifest('bpy')['mounts']:
        obj=bpy.data.objects.new(item['name'],None)
        obj.empty_display_type='ARROWS';obj.empty_display_size=.08
        coll.objects.link(obj);obj.parent=root;obj.location=item['position']
    bpy.ops.object.select_all(action='DESELECT')
    for obj in coll.objects: obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.gltf(filepath=str(out/(STEM+'.glb')),export_format='GLB',use_selection=True)
    bpy.ops.export_scene.fbx(filepath=str(out/(STEM+'.fbx')),use_selection=True,
                             axis_forward='-Z',axis_up='Y')
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(STEM+'.blend')))
    return len([o for o in coll.objects if o.type=='MESH'])


def main():
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser();parser.add_argument('--out',default='build/M03-014')
    ns=parser.parse_args(args);out=Path(ns.out).resolve()
    build();write_obj(out)
    try:
        import bpy
    except ImportError:
        backend='static'
    else:
        assert write_blender(out)==len(PARTS)
        backend='bpy'
    (out/(ID+'_manifest.json')).write_text(json.dumps(manifest(backend),indent=2)+'\n',encoding='utf-8')
    print('M03-014',backend,len(PARTS),'meshes',len(MATERIALS),'materials')


if __name__=='__main__': main()
