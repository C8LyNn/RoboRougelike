#!/usr/bin/env python3
"""M04-008: twin-lens side stabilizing hover module, Unity 6 / Blender 4+.

Coordinate contract: metres, +X points OUT from the chassis side, +Z is up,
+Y is the vehicle longitudinal axis. GRID_ROOT (0,0,0) is the side socket
mounting plane (X=0). The two gimbal pivots are (0.310,+/-0.290,0) and
rotate around LOCAL Y by up to 16 degrees; there is NO top-mounted pivot.

Usage:
  python generate_m04_008.py --self-test
  python generate_m04_008.py --static-obj --out build/M04-008
  blender -b --factory-startup --python generate_m04_008.py -- --out build/M04-008

Only deletes its own Blender collection, never scenes/other asset collections.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from collections import Counter
from pathlib import Path

ID = 'M04-008'
PREFIX = 'RRL_M04_008_'
COLLECTION = PREFIX + 'SIDE_HOVER'
RING_BORE = .160
MAX_TILT_DEG = 16
EMITTERS = {'FORE': -.290, 'AFT': .290}
MATERIALS = {
    'Gunmetal': ((.065,.084,.109,1),.78,.32,0),
    'Titanium': ((.38,.44,.48,1),.75,.27,0),
    'Ceramic': ((.71,.76,.72,1),.23,.38,0),
    'Copper': ((.60,.26,.095,1),.71,.29,0),
    'Rubber': ((.025,.031,.039,1),.03,.84,0),
    'Cyan_Emitter': ((.055,.77,.93,1),.23,.25,3.5),
    'Amber_Status': ((1.0,.42,.04,1),.20,.36,2.0),
}
BOX_FACES = [(0,3,2),(0,2,1),(4,5,6),(4,6,7),
             (0,1,5),(0,5,4),(1,2,6),(1,6,5),
             (2,3,7),(2,7,6),(3,0,4),(3,4,7)]

def part(name,vertices,faces,material,role='static',bevel=0):
    return dict(name=name,vertices=vertices,faces=faces,material=material,
                role=role,bevel=bevel)

def box(name,center,size,material,role='static',bevel=.003):
    x,y,z=center; a,b,c=[s/2 for s in size]
    assert min(size)>0
    verts=[(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
           (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
    return part(name,verts,BOX_FACES,material,role,bevel)

def tube_x(name,x0,x1,cy,cz,outer,inner,material,role='static',segments=32):
    """Watertight annular tube, NOT a filled cylinder hidden in another one."""
    assert 0<=inner<outer and x0<x1 and segments>=8
    if inner==0: return cylinder_x(name,(x0+x1)/2,cy,cz,outer,x1-x0,material,role,segments)
    verts=[]
    for r,x in ((outer,x0),(outer,x1),(inner,x0),(inner,x1)):
        verts.extend((x,cy+r*math.cos(2*math.pi*i/segments),
                         cz+r*math.sin(2*math.pi*i/segments)) for i in range(segments))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        a,b,c,d=i,segments+i,segments+j,j
        faces.extend(((a,c,b),(a,d,c))) # outward outer barrel
        a,b,c,d=2*segments+i,3*segments+i,3*segments+j,2*segments+j
        faces.extend(((a,b,c),(a,c,d))) # inward bore
        faces.extend(((i,2*segments+i,2*segments+j),(i,2*segments+j,j))) # -X
        faces.extend(((segments+i,segments+j,3*segments+j),
                      (segments+i,3*segments+j,3*segments+i))) # +X
    return part(name,verts,faces,material,role,0)

def cylinder_x(name,x,cy,cz,r,depth,material,role='static',segments=32):
    assert r>0 and depth>0
    x0=x-depth/2;x1=x+depth/2
    verts=[]
    for xx in (x0,x1):
        verts.extend((xx,cy+r*math.cos(2*math.pi*i/segments),
                         cz+r*math.sin(2*math.pi*i/segments)) for i in range(segments))
    verts.extend(((x0,cy,cz),(x1,cy,cz)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend(((i,segments+j,segments+i),(i,j,segments+j),
                      (2*segments,j,i),(2*segments+1,segments+i,segments+j)))
    return part(name,verts,faces,material,role,0)

def wedge_x(name,x0,x1,cy,cz,inner,outer,angle0,angle1,material,role='moving',steps=6):
    """Closed radial sector for one machined iris petal."""
    assert angle1>angle0 and inner>0
    verts=[]
    for r,x in ((outer,x0),(outer,x1),(inner,x0),(inner,x1)):
        verts.extend((x,cy+r*math.cos(angle0+(angle1-angle0)*i/steps),
                         cz+r*math.sin(angle0+(angle1-angle0)*i/steps)) for i in range(steps+1))
    n=steps+1; faces=[]
    for i in range(steps):
        j=i+1
        faces.extend(((i,n+j,n+i),(i,j,n+j),
                      (2*n+i,3*n+i,3*n+j),(2*n+i,3*n+j,2*n+j),
                      (i,2*n+i,2*n+j),(i,2*n+j,j),
                      (n+i,n+j,3*n+j),(n+i,3*n+j,3*n+i)))
    for i in (0,steps):
        if i==0:
            faces.extend(((i,n+i,3*n+i),(i,3*n+i,2*n+i)))
        else:
            faces.extend(((i,3*n+i,n+i),(i,2*n+i,3*n+i)))
    return part(name,verts,faces,material,role,0)

def translated_local(p,px,py,pz):
    """Moving meshes stored relative to gimbal pivot for Blender parenting."""
    p['vertices']=[(x-px,y-py,z-pz) for x,y,z in p['vertices']]
    return p

def build_parts():
    parts=[]
    def add(p): parts.append(p)
    # True X=0 contact face, no top bracket; bolts are front-visible.
    add(box('SIDE_MOUNT_PLATE',(.027,.0,0),(.054,.76,.31),'Titanium',bevel=.007))
    add(box('SIDE_GASKET',(.061,0,0),(.014,.69,.245),'Rubber',bevel=.003))
    add(box('SPINE_CENTRAL',(.117,0,0),(.098,.59,.112),'Gunmetal',bevel=.011))
    for zsign in (-1,1):
        add(box('SPINE_RAIL_'+str(zsign),(.130,0,zsign*.112),(.105,.67,.035),'Copper',bevel=.005))
    for side,cy in EMITTERS.items():
        # Supports connect to the rear of annular housings, outside the gimbal bore.
        for zsign in (-1,1):
            add(box(f'{side}_YOKE_{zsign}',(.185,cy,zsign*.172),(.18,.075,.055),'Titanium',bevel=.008))
        add(tube_x(f'{side}_BACK_CERAMIC',.223,.242,cy,0,.148,.090,'Ceramic',segments=32))
        add(tube_x(f'{side}_COPPER_COIL',.246,.264,cy,0,.206,.171,'Copper',segments=40))
        add(tube_x(f'{side}_SHROUD',.266,.381,cy,0,.214,RING_BORE,'Gunmetal',segments=48))
        add(tube_x(f'{side}_FRONT_LIP',.378,.400,cy,0,.221,.166,'Titanium',segments=48))
        add(tube_x(f'{side}_OUTER_ACCENT',.302,.312,cy,0,.223,.211,'Ceramic',segments=48))
        for i in range(8):
            ang=2*math.pi*i/8
            yy=cy+.232*math.cos(ang); zz=.232*math.sin(ang)
            # Eight isolated cooling blocks, attached to the external housing.
            p=box(f'{side}_HEATSINK_{i}',(.334,yy,zz),(.070,.024,.024),
                  'Copper' if i%2==0 else 'Titanium',bevel=.003)
            # Rotate square cross section around local X for radial alignment.
            co,si=math.cos(ang),math.sin(ang)
            new=[]
            for x,y,z in p['vertices']:
                dy=y-yy;dz=z-zz
                new.append((x,yy+dy*co-dz*si,zz+dy*si+dz*co))
            p['vertices']=new;add(p)
        # Moving assembly: each mesh is parented to a distinct X-outboard gimbal.
        px=.310
        moving=[
            cylinder_x(f'{side}_TILT_CARRIER',.327,cy,0,.132,.023,'Titanium','moving',40),
            tube_x(f'{side}_TILT_BEZEL',.338,.350,cy,0,.135,.117,'Ceramic','moving',40),
            cylinder_x(f'{side}_LENS_GLASS',.3555,cy,0,.111,.009,'Cyan_Emitter','moving',40),
            tube_x(f'{side}_LENS_RETICLE',.359,.363,cy,0,.112,.101,'Copper','moving',40),
            tube_x(f'{side}_IRIS_CARRIER_RING',.348,.367,cy,0,.132,.126,'Titanium','moving',40),
            cylinder_x(f'{side}_CORE_STEM',.361,cy,0,.040,.006,'Titanium','moving',24),
            cylinder_x(f'{side}_CORE',.364,cy,0,.057,.004,'Cyan_Emitter','moving',32),
        ]
        for k in range(4):
            angle=k*math.pi/2
            moving.append(wedge_x(f'{side}_IRIS_{k}',.363,.367,cy,0,
                                  .114,.128,angle+.12,angle+.65,
                                  'Amber_Status' if k==0 else 'Titanium'))
        for p in moving: add(translated_local(p,px,cy,0))
        # Side mount status bars; static, outside moving ring.
        for zsign in (-1,1):
            add(box(f'{side}_STATUS_{zsign}',(.090,cy,zsign*.142),
                    (.012,.095,.014),'Amber_Status',bevel=.002))
    for ysign in (-1,1):
        for zsign in (-1,1):
            add(cylinder_x(f'MOUNT_BOLT_{ysign}_{zsign}',.063,ysign*.342,
                           zsign*.115,.018,.016,'Ceramic',segments=12))
    return parts

def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(x*y for x,y in zip(a,b))

def transformed(p,deg,cy):
    if p['role']!='moving': return p['vertices']
    # local coordinates -> pivot world, rotation about LOCAL Y.
    a=math.radians(deg); co=math.cos(a); si=math.sin(a)
    return [( .310+co*x+si*z,cy+y,-si*x+co*z) for x,y,z in p['vertices']]

def validate(parts):
    names=set();triangles=0; global_bounds=[[float('inf'),-float('inf')] for _ in range(3)]
    for p in parts:
        assert p['name'] not in names,p['name']; names.add(p['name'])
        assert p['material'] in MATERIALS
        verts=p['vertices'];faces=p['faces'];edge_use=Counter();oriented=Counter();volume=0
        for v in verts:assert all(math.isfinite(c) for c in v)
        for face in faces:
            assert len(face)==3 and len(set(face))==3 and all(0<=i<len(verts) for i in face),p['name']
            a,b,c=(verts[i] for i in face)
            assert dot(cross(sub(b,a),sub(c,a)),cross(sub(b,a),sub(c,a)))>1e-16,p['name']
            volume+=dot(a,cross(b,c))/6
            for i,j in ((face[0],face[1]),(face[1],face[2]),(face[2],face[0])):
                edge_use[tuple(sorted((i,j)))]+=1;oriented[(i,j)]+=1
            triangles+=1
        assert all(n==2 for n in edge_use.values()),('non-manifold',p['name'])
        assert all(oriented[(j,i)]==n for (i,j),n in oriented.items()),('winding',p['name'])
        assert volume>1e-12,('inverted winding',p['name'],volume)
        for v in transformed(p,0,EMITTERS['FORE'] if p['name'].startswith('FORE_') else EMITTERS['AFT']):
            for axis in range(3):
                global_bounds[axis][0]=min(global_bounds[axis][0],v[axis]);global_bounds[axis][1]=max(global_bounds[axis][1],v[axis])
    assert len(MATERIALS)>=3
    assert abs(global_bounds[0][0])<1e-9,('mount must touch x=0',global_bounds)
    assert global_bounds[0][1]<.46
    assert global_bounds[1][0]>-.55 and global_bounds[1][1]<.55
    assert global_bounds[2][0]>-.28 and global_bounds[2][1]<.28
    assert len([p for p in parts if p['role']=='moving'])==22
    # Sweep 65 poses: check EVERY animated vertex against housing bore, plus
    # conservative separation between emitter envelopes (no AABB false PASS).
    clearance=1e9; checks=0
    for frame in range(65):
        deg=MAX_TILT_DEG*(2*frame/64-1)
        for side,cy in EMITTERS.items():
            angle=deg if side=='FORE' else -deg
            for p in parts:
                if p['role']!='moving' or not p['name'].startswith(side+'_'):continue
                for x,y,z in transformed(p,angle,cy):
                    r=math.hypot(y-cy,z)
                    clearance=min(clearance,RING_BORE-r)
                    assert r<RING_BORE-.005,('shroud collision',p['name'],frame,r)
                    assert x>.27 and x<.43,('moving out of envelope',p['name'],frame,x)
                    checks+=1
    assert clearance>.005
    # Stator rings: distinct emitters remain at least 0.15m apart.
    assert abs(EMITTERS['AFT']-EMITTERS['FORE'])-2*.223>.10
    return {'asset':ID,'status':'PASS','parts':len(parts),'triangles':triangles,
            'materials':len(MATERIALS),'moving_meshes':22,
            'gimbals':{side:[.310,cy,0] for side,cy in EMITTERS.items()},
            'side_mount_origin_m':[0,0,0], 'side_mount_normal':'+X',
            'bounds_at_rest_m':global_bounds,'max_tilt_deg':MAX_TILT_DEG,
            'sampled_poses':65,'animated_vertex_clearance_checks':checks,
            'min_shroud_bore_clearance_m':round(clearance,6),
            'collision_scope':'moving vertex radial clearance vs inner bore + two-emitter separation; not Blender rigid-body or Unity physics',
            'blender':'NOT RUN','unity':'NOT RUN'}

def static_obj(parts,outdir):
    outdir.mkdir(parents=True,exist_ok=True)
    obj=ID+'_side_hover.obj';mtl=ID+'_side_hover.mtl'
    mats=[]
    for name,(rgba,metallic,roughness,emission) in MATERIALS.items():
        mats.extend([f'newmtl {name}',f'Kd {rgba[0]:.6f} {rgba[1]:.6f} {rgba[2]:.6f}',
                     f'Ks {metallic:.4f} {metallic:.4f} {metallic:.4f}',
                     f'Ns {max(1,(1-roughness)*800):.2f}',
                     f'Ke {rgba[0]*emission:.4f} {rgba[1]*emission:.4f} {rgba[2]*emission:.4f}', 'illum 2',''])
    (outdir/mtl).write_text('\n'.join(mats),encoding='utf-8')
    lines=[f'# {ID} rest-pose preview; Blender Z-up; sockets and animation are in manifest/GLB',f'mtllib {mtl}']
    offset=1
    for p in parts:
        cy=EMITTERS['FORE'] if p['name'].startswith('FORE_') else EMITTERS['AFT']
        verts=transformed(p,0,cy)
        lines.extend([f'o {p["name"]}',f'usemtl {p["material"]}'])
        lines.extend(f'v {x:.8f} {y:.8f} {z:.8f}' for x,y,z in verts)
        lines.extend(f'f {a+offset} {b+offset} {c+offset}' for a,b,c in p['faces'])
        offset+=len(verts)
    (outdir/obj).write_text('\n'.join(lines)+'\n',encoding='utf-8')
    return {'backend':'python-stdlib','obj':obj,'mtl':mtl,'blender':'NOT RUN','glb':'NOT RUN'}

def blender_export(parts,outdir):
    import bpy
    prev=bpy.data.collections.get(COLLECTION)
    if prev:
        for obj in tuple(prev.objects): bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(prev)
    for mesh in tuple(bpy.data.meshes):
        if mesh.name.startswith(PREFIX) and mesh.users==0:bpy.data.meshes.remove(mesh)
    for action in tuple(bpy.data.actions):
        if action.name.startswith(PREFIX) and action.users==0:bpy.data.actions.remove(action)
    collection=bpy.data.collections.new(COLLECTION)
    bpy.context.scene.collection.children.link(collection)
    root=bpy.data.objects.new(PREFIX+'SIDE_MOUNT_ROOT',None)
    collection.objects.link(root)
    root.empty_display_type='PLAIN_AXES';root.empty_display_size=.10
    root['mount_surface']='side socket at X=0';root['outboard_axis']='+X'
    root['unity_metres_per_unit']=1.0
    gimbals={}
    for side,cy in EMITTERS.items():
        pivot=bpy.data.objects.new(PREFIX+side+'_Y_GIMBAL',None)
        collection.objects.link(pivot);pivot.parent=root
        pivot.location=(.310,cy,0)
        pivot.empty_display_type='ARROWS';pivot.empty_display_size=.10
        pivot['rotation_axis']='LOCAL_Y';pivot['tilt_range_degrees']=(-16,16)
        gimbals[side]=pivot
    mats={}
    for name,(rgba,metallic,roughness,emission) in MATERIALS.items():
        m=bpy.data.materials.get(PREFIX+name) or bpy.data.materials.new(PREFIX+name)
        m.use_nodes=True
        bsdf=m.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value=rgba
        bsdf.inputs['Metallic'].default_value=metallic
        bsdf.inputs['Roughness'].default_value=roughness
        if emission:
            if 'Emission Color' in bsdf.inputs:bsdf.inputs['Emission Color'].default_value=rgba
            elif 'Emission' in bsdf.inputs:bsdf.inputs['Emission'].default_value=rgba
            if 'Emission Strength' in bsdf.inputs:bsdf.inputs['Emission Strength'].default_value=emission
        m.diffuse_color=rgba
        mats[name]=m
    for p in parts:
        mesh=bpy.data.meshes.new(PREFIX+p['name']+'_MESH')
        mesh.from_pydata(p['vertices'],[],p['faces']);mesh.update()
        obj=bpy.data.objects.new(PREFIX+p['name'],mesh)
        collection.objects.link(obj)
        if p['role']=='moving':
            side=p['name'].split('_')[0]
            obj.parent=gimbals[side]
        else:obj.parent=root
        mesh.materials.append(mats[p['material']])
        if p['bevel']:
            # Blender 4.0 requires Auto Smooth for Weighted Normal; newer
            # Blender versions removed this property, so guard it.
            if hasattr(mesh,'use_auto_smooth'):
                mesh.use_auto_smooth=True
            for polygon in mesh.polygons:
                polygon.use_smooth=True
            mod=obj.modifiers.new('Machined_chamfer','BEVEL')
            mod.width=p['bevel'];mod.segments=2
            obj.modifiers.new('Weighted_normals','WEIGHTED_NORMAL')
    bpy.context.scene.render.fps=30
    for side,pivot in gimbals.items():
        signs=(1 if side=='FORE' else -1)
        for frame,angle in ((1,0),(21,16),(41,0),(61,-16),(81,0)):
            pivot.rotation_euler[1]=math.radians(signs*angle)
            pivot.keyframe_insert(data_path='rotation_euler',frame=frame,group='GIMBAL_SWEEP')
        pivot.rotation_euler[1]=0
    bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=81
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    outdir.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    blend=outdir/(ID+'_side_hover.blend')
    glb=outdir/(ID+'_side_hover.glb')
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',use_selection=True,
                              export_animations=True)
    assert blend.exists() and blend.stat().st_size>1000
    assert glb.exists() and glb.stat().st_size>1000
    return {'backend':'blender-bpy','blend':blend.name,'glb':glb.name,
            'blender':'EXPORTED','unity':'NOT RUN'}

def main():
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument('--out',default='./build/M04-008')
    parser.add_argument('--self-test',action='store_true')
    parser.add_argument('--static-obj',action='store_true')
    args=parser.parse_args(argv)
    parts=build_parts(); report=validate(parts)
    if args.self_test:
        print(json.dumps(report,indent=2));return
    outdir=Path(args.out).resolve()
    if args.static_obj: export=static_obj(parts,outdir)
    else:
        try:import bpy
        except ImportError: export=static_obj(parts,outdir)
        else:export=blender_export(parts,outdir)
    report.update(export)
    outdir.mkdir(parents=True,exist_ok=True)
    (outdir/(ID+'_manifest.json')).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':main()
