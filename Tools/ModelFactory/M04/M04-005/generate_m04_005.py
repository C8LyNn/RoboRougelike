#!/usr/bin/env python3
"""M04-005 dual-chamber afterburner: reproducible OBJ/MTL and Blender animated GLB.

Source: geometry.json. Works in plain Python for static export and QA:
  python generate_m04_005.py --self-test --output-dir /tmp/m04-005
Blender animated export (Blender 3.4+):
  blender -b --factory-startup --python generate_m04_005.py -- --output-dir /tmp/m04-005 --require-blender

Coordinate system: Blender Z up, +Y vehicle forward, exhaust points -Y.
1 Blender unit = 1 meter. Root (0,0,0) at the rear vehicle mounting plane.
Each of eight nozzle petals pivots about a tangential axis at Y=-0.827 m.
Animation: 0 -> +20 -> 0 degrees (frames 1,25,49); radial outward opening.
No gameplay logic, no changes to any Unity scenes or other assets.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
TAU = math.tau


def spec_read():
    spec = json.loads((HERE / 'geometry.json').read_text(encoding='utf-8'))
    if spec['id'] != 'M04-005':
        raise ValueError('Incorrect model ID')
    return spec


def orient(vertices, faces):
    """Make all closed convex/extruded components outward facing."""
    volume = signed_volume(vertices, faces)
    if abs(volume) < 1e-12:
        raise ValueError('Degenerate closed volume')
    if volume < 0:
        faces = [tuple(reversed(face)) for face in faces]
    return vertices, faces


def signed_volume(v, f):
    total = 0.0
    for face in f:
        a = v[face[0]]
        for i in range(1, len(face)-1):
            b, c = v[face[i]], v[face[i+1]]
            total += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                      a[1]*(b[2]*c[0]-b[0]*c[2]) +
                      a[2]*(b[0]*c[1]-b[1]*c[0])) / 6.0
    return total


def box(center, size, y_angle=0.0):
    a,b,c = (x/2.0 for x in size)
    verts = [(-a,-b,-c),(a,-b,-c),(a,b,-c),(-a,b,-c),
             (-a,-b,c),(a,-b,c),(a,b,c),(-a,b,c)]
    faces = [(3,2,1,0),(4,5,6,7),(0,1,5,4),
             (1,2,6,5),(2,3,7,6),(3,0,4,7)]
    cs,sn=math.cos(y_angle),math.sin(y_angle)
    verts = [(x*cs+z*sn+center[0],y+center[1],
              -x*sn+z*cs+center[2]) for x,y,z in verts]
    return orient(verts,faces)


def ring(cx,cz,front_y,back_y,outer_front,outer_back,inner_front,inner_back,segments=48):
    """Watertight tapered annular sleeve with genuine open bore (not solid disks)."""
    assert front_y > back_y and 0 < inner_front < outer_front and 0 < inner_back < outer_back
    verts=[]
    for radius,y in [(outer_front,front_y),(outer_back,back_y),
                     (inner_front,front_y),(inner_back,back_y)]:
        for i in range(segments):
            a=TAU*i/segments
            verts.append((cx+radius*math.cos(a),y,cz+radius*math.sin(a)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i),
                      (2*segments+i,3*segments+i,3*segments+j,2*segments+j),
                      (i,2*segments+i,2*segments+j,j),
                      (segments+i,segments+j,3*segments+j,3*segments+i)])
    return orient(verts,faces)


def petal(cx,cz,angle_center,half_width=math.radians(34),segments=12):
    """Hollow curved nozzle shroud sector, capped at both angular ends."""
    front_y,back_y=-0.845,-1.075
    r_inner,r_outer=0.216,0.285
    verts=[]
    for radius,y in [(r_outer,front_y),(r_outer,back_y),
                     (r_inner,front_y),(r_inner,back_y)]:
        for i in range(segments+1):
            a=angle_center-half_width+2*half_width*i/segments
            verts.append((cx+radius*math.cos(a),y,cz+radius*math.sin(a)))
    n=segments+1
    faces=[]
    for i in range(segments):
        j=i+1
        faces.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),
                      (i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    faces.extend([(0,n,3*n,2*n),(segments,2*n+segments,3*n+segments,n+segments)])
    return orient(verts,faces)


def build(spec):
    parts=[]
    def add(name,material,mesh,**kwargs):
        v,f=mesh
        parts.append(dict(name=name,material=material,vertices=v,faces=f,**kwargs))
    add('mount_gasket','rubber',box((0,-0.005,0),(1.43,0.01,0.53)))
    add('mount_cassette','graphite',box((0,-0.077,0),(1.48,0.145,0.56)))
    add('mount_spine','titanium',box((0,-0.16,-0.225),(1.28,0.16,0.075)))
    add('upper_bridge','titanium',box((0,-0.17,0.228),(1.22,0.13,0.066)))
    add('center_spacer','ceramic',box((0,-0.22,0),(0.12,0.27,0.39)))
    add('status_bar','ion_cyan',box((0,-0.159,0.245),(0.26,0.012,0.018)))
    add('warning_tab_L','warning_yellow',box((-0.66,-0.158,0.08),(0.07,0.012,0.11)))
    add('warning_tab_R','warning_yellow',box((0.66,-0.158,0.08),(0.07,0.012,0.11)))
    for ix in (-1,1):
        for iz in (-1,1):
            add(f'mount_bolt_{ix}_{iz}','copper',box((ix*0.65,-0.158,iz*0.2),(0.045,0.018,0.045)))
    for chamber,cx in enumerate(spec['chamber_centers_x']):
        prefix=f'C{chamber+1}'
        add(f'{prefix}_structural_rail','graphite',box((cx,-0.315,-0.185),(0.22,0.38,0.085)))
        add(f'{prefix}_main_shell','graphite',ring(cx,0,-0.19,-0.67,0.222,0.222,0.182,0.182))
        add(f'{prefix}_front_flange','titanium',ring(cx,0,-0.188,-0.242,0.252,0.248,0.185,0.183))
        add(f'{prefix}_ceramic_jacket','ceramic',ring(cx,0,-0.602,-0.745,0.241,0.252,0.186,0.190))
        add(f'{prefix}_heat_band','copper',ring(cx,0,-0.518,-0.552,0.244,0.244,0.221,0.221))
        add(f'{prefix}_nozzle_lip','titanium',ring(cx,0,-0.742,-0.815,0.25,0.255,0.190,0.199))
        add(f'{prefix}_inner_throat','hot_amber',ring(cx,0,-0.765,-0.818,0.186,0.191,0.151,0.154))
        add(f'{prefix}_ion_ring','ion_cyan',ring(cx,0,-0.801,-0.817,0.156,0.157,0.133,0.135))
        add(f'{prefix}_inner_shadow','rubber',ring(cx,0,-0.42,-0.60,0.181,0.181,0.175,0.175))
        for i in range(8):
            theta=TAU*i/8
            add(f'{prefix}_heat_rib_{i}','copper',box(
                (cx+0.238*math.cos(theta),-0.43,0.238*math.sin(theta)),
                (0.034,0.24,0.035),-theta))
        for i in range(4):
            theta=TAU*(i+0.5)/4
            add(f'{prefix}_outer_clamp_{i}','titanium',box(
                (cx+0.257*math.cos(theta),-0.27,0.257*math.sin(theta)),
                (0.034,0.13,0.045),-theta))
        for i in range(spec['petals_per_chamber']):
            theta=TAU*i/spec['petals_per_chamber']
            hinge=(cx+spec['petal_hinge_radius_m']*math.cos(theta),
                   spec['petal_hinge_y_m'],
                   spec['chamber_center_z']+spec['petal_hinge_radius_m']*math.sin(theta))
            tangent=(-math.sin(theta),0.0,math.cos(theta))
            add(f'{prefix}_variable_flap_{i}','ceramic',petal(cx,0,theta,
                math.radians(spec['petal_width_degrees']/2)),
                moving=True,pivot=hinge,axis=tangent,chamber=chamber,
                sector_center=theta,sector_half=math.radians(spec['petal_width_degrees']/2))
    names=[p['name'] for p in parts]
    assert len(names)==len(set(names))
    return parts


def rotate_point(point,pivot,axis,angle):
    x,y,z=(point[i]-pivot[i] for i in range(3))
    ux,uy,uz=axis
    c,s=math.cos(angle),math.sin(angle)
    dot=ux*x+uy*y+uz*z
    cross=(uy*z-uz*y,uz*x-ux*z,ux*y-uy*x)
    return (pivot[0]+x*c+cross[0]*s+ux*dot*(1-c),
            pivot[1]+y*c+cross[1]*s+uy*dot*(1-c),
            pivot[2]+z*c+cross[2]*s+uz*dot*(1-c))


def transformed_vertices(part,angle_deg):
    if not part.get('moving'):
        return part['vertices']
    return [rotate_point(v,part['pivot'],part['axis'],math.radians(angle_deg))
            for v in part['vertices']]


def verify_manifold(part):
    vertices,faces=part['vertices'],part['faces']
    edges=Counter()
    for f in faces:
        assert len(f)>=3 and len(f)==len(set(f)),part['name']
        for a,b in zip(f,f[1:]+f[:1]):
            assert 0<=a<len(vertices) and 0<=b<len(vertices)
            edges[tuple(sorted((a,b)))]+=1
        a=vertices[f[0]]
        for i in range(1,len(f)-1):
            b,c=vertices[f[i]],vertices[f[i+1]]
            ab=[b[j]-a[j] for j in range(3)]
            ac=[c[j]-a[j] for j in range(3)]
            cross=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
            assert sum(q*q for q in cross)>1e-15,(part['name'],f)
    assert all(n==2 for n in edges.values()),(part['name'],Counter(edges.values()))
    assert signed_volume(vertices,faces)>1e-9,part['name']
    return len(edges)


def qa(spec,parts):
    fixed=[p for p in parts if not p.get('moving')]
    moving=[p for p in parts if p.get('moving')]
    assert len(moving)==8 and len(fixed)>=40
    assert len(spec['materials'])>=3
    assert spec['mount_origin']==[0.0,0.0,0.0]
    assert spec['petal_angle_degrees']==[0,20,0]
    assert spec['keyframes']==[1,25,49]
    for p in parts:
        assert p['material'] in spec['materials']
        verify_manifold(p)
    fixed_rear=min(v[1] for p in fixed for v in p['vertices'])
    minimum_gap=float('inf')
    checks=0
    cross_chamber_gap=float('inf')
    for angle in range(0,21):
        pose=[(p,transformed_vertices(p,angle)) for p in moving]
        for p,vertices in pose:
            flap_front=max(v[1] for v in vertices)
            gap=flap_front-fixed_rear
            minimum_gap=min(minimum_gap,fixed_rear-flap_front)
            assert gap < -0.005,(p['name'],angle,gap)
            checks+=len(fixed)
            cx=spec['chamber_centers_x'][p['chamber']]
            for v in vertices:
                theta=math.atan2(v[2],v[0]-cx)
                d=(theta-p['sector_center']+math.pi)%(2*math.pi)-math.pi
                assert abs(d)<=p['sector_half']+1e-8,(p['name'],angle,d)
        left=[v[0] for p,vs in pose if p['chamber']==0 for v in vs]
        right=[v[0] for p,vs in pose if p['chamber']==1 for v in vs]
        cross_chamber_gap=min(cross_chamber_gap,min(right)-max(left))
        assert min(right)-max(left)>0.05,angle
    assert spec['petal_width_degrees']<360/spec['petals_per_chamber']
    assert all(math.isfinite(c) for p in parts for v in p['vertices'] for c in v)
    vertices=sum(len(p['vertices']) for p in parts)
    polygons=sum(len(p['faces']) for p in parts)
    triangles=sum(sum(len(f)-2 for f in p['faces']) for p in parts)
    return {
        'id':spec['id'],'status':'STATIC_QA_PASS',
        'parts':len(parts),'fixed_parts':len(fixed),'moving_parts':len(moving),
        'vertices':vertices,'polygons':polygons,'triangles':triangles,
        'materials':list(spec['materials']),
        'closed_manifold_parts':len(parts),'negative_volume_parts':0,
        'animation_degrees':[0,20,0],'animation_keyframes':[1,25,49],
        'swept_samples':21,'fixed_moving_plane_checks':checks,
        'minimum_flap_to_fixed_y_gap_m':round(minimum_gap,6),
        'minimum_between_chambers_x_gap_m':round(cross_chamber_gap,6),
        'sector_gap_degrees':round(90-spec['petal_width_degrees'],4),
        'mount_origin_m':spec['mount_origin'],
        'blender':'NOT_RUN','animated_glb':'NOT_RUN','unity_import':'NOT_RUN',
        'visual_review':'NOT_RUN',
    }


def write_obj(spec,parts,out):
    out.mkdir(parents=True,exist_ok=True)
    lines=['# M04-005 dual-chamber afterburner / Blender Z up / 1 unit = 1 m',
           '# Static neutral pose; animated petal rig is exported from Blender.',
           'mtllib M04-005.mtl']
    offset=1
    for p in parts:
        lines.extend(['o '+p['name'],'usemtl '+p['material']])
        lines.extend('v '+' '.join(f'{q:.6f}' for q in v) for v in p['vertices'])
        lines.extend('f '+' '.join(str(offset+i) for i in face) for face in p['faces'])
        offset+=len(p['vertices'])
    (out/'M04-005.obj').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    mtl=['# M04-005 material definitions']
    for name,mat in spec['materials'].items():
        rgb=mat['color'][:3]
        mtl.extend(['newmtl '+name,'Ka 0.04 0.04 0.04',
                    'Kd '+' '.join(f'{c:.5f}' for c in rgb),
                    'Ks 0.30 0.30 0.30',
                    f'Ns {20+100*mat["metallic"]:.1f}',
                    'd 1.0','illum 2',''])
    (out/'M04-005.mtl').write_text('\n'.join(mtl)+'\n',encoding='utf-8')


def make_blender(spec,parts,out):
    import bpy
    from mathutils import Quaternion
    collection_name='M04-005_ASSET'
    old=bpy.data.collections.get(collection_name)
    if old:
        for obj in tuple(old.objects):
            bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    collection=bpy.data.collections.new(collection_name)
    bpy.context.scene.collection.children.link(collection)
    mats={}
    for name,info in spec['materials'].items():
        m=bpy.data.materials.new('M04-005_'+name)
        m.use_nodes=True
        bsdf=m.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value=tuple(info['color'])
        bsdf.inputs['Metallic'].default_value=float(info['metallic'])
        bsdf.inputs['Roughness'].default_value=float(info['roughness'])
        if 'emission' in info:
            em=bsdf.inputs.get('Emission Color') or bsdf.inputs.get('Emission')
            if em is not None:
                em.default_value=tuple(info['emission'])
            strength=bsdf.inputs.get('Emission Strength')
            if strength is not None:
                strength.default_value=float(info['emission_strength'])
        mats[name]=m
    root=bpy.data.objects.new('M04-005_MOUNT_ROOT',None)
    collection.objects.link(root)
    root.empty_display_type='PLAIN_AXES'
    root.empty_display_size=0.12
    root['mount_surface']='rear_frame_Y_zero'
    root['unit']='metre'
    for p in parts:
        pivot=p.get('pivot',(0.0,0.0,0.0))
        vertices=[tuple(v[i]-pivot[i] for i in range(3)) for v in p['vertices']]
        mesh=bpy.data.meshes.new('M04-005_'+p['name']+'_mesh')
        mesh.from_pydata(vertices,[],p['faces'])
        mesh.update(calc_edges=True)
        obj=bpy.data.objects.new('M04-005_'+p['name'],mesh)
        collection.objects.link(obj)
        obj.parent=root
        obj.location=pivot
        obj.data.materials.append(mats[p['material']])
        obj['model_id']='M04-005'
        obj['material_key']=p['material']
        obj['animated']=bool(p.get('moving'))
        smooth=('_shell' in p['name'] or '_flange' in p['name'] or
                '_jacket' in p['name'] or '_lip' in p['name'] or
                '_throat' in p['name'] or '_ring' in p['name'])
        for polygon in mesh.polygons:
            polygon.use_smooth=smooth
        if p.get('moving'):
            obj.rotation_mode='QUATERNION'
            for frame,degrees in zip(spec['keyframes'],spec['petal_angle_degrees']):
                obj.rotation_quaternion=Quaternion(p['axis'],math.radians(degrees))
                obj.keyframe_insert(data_path='rotation_quaternion',frame=frame,group='EXHAUST_FLAP')
            if obj.animation_data and obj.animation_data.action:
                action=obj.animation_data.action
                if hasattr(action,'fcurves'):
                    for curve in action.fcurves:
                        for key in curve.keyframe_points:
                            key.interpolation='LINEAR'
    scene=bpy.context.scene
    old_frame=scene.frame_current
    old_start,old_end=scene.frame_start,scene.frame_end
    old_selected=tuple(bpy.context.selected_objects)
    old_active=bpy.context.view_layer.objects.active
    try:
        scene.frame_start=spec['keyframes'][0]
        scene.frame_end=spec['keyframes'][-1]
        scene.frame_set(spec['keyframes'][0])
        bpy.context.view_layer.update()
        assert len(collection.objects)==len(parts)+1
        assert all(o.name.startswith('M04-005_') for o in collection.objects)
        out.mkdir(parents=True,exist_ok=True)
        bpy.data.libraries.write(str(out/'M04-005.blend'),{collection},compress=True)
        bpy.ops.object.select_all(action='DESELECT')
        for obj in collection.objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.gltf(filepath=str(out/'M04-005_animated.glb'),
            export_format='GLB',use_selection=True,export_animations=True,
            export_apply=False)
    finally:
        bpy.ops.object.select_all(action='DESELECT')
        for obj in old_selected:
            if obj.name in bpy.data.objects:
                obj.select_set(True)
        if old_active is not None and old_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active=old_active
        scene.frame_start,scene.frame_end=old_start,old_end
        scene.frame_set(old_frame)


def main():
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument('--self-test',action='store_true')
    parser.add_argument('--require-blender',action='store_true')
    parser.add_argument('--output-dir',default=str(HERE/'generated'))
    opts=parser.parse_args(args)
    spec=spec_read()
    parts=build(spec)
    report=qa(spec,parts)
    out=Path(opts.output_dir).resolve()
    write_obj(spec,parts,out)
    if not opts.self_test:
        try:
            import bpy  # noqa: F401
        except ImportError:
            if opts.require_blender:
                raise RuntimeError('Blender bpy is required but unavailable')
        else:
            make_blender(spec,parts,out)
            report['blender']='EXECUTED'
            report['animated_glb']='GENERATED_NOT_YET_IMPORTED_IN_UNITY'
    (out/'M04-005.static-qa.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2,ensure_ascii=False))


if __name__=='__main__':
    main()
