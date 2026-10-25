#!/usr/bin/env python3
"""M02-016: 90-degree straight-bevel gearbox. Visual-only, not involute engineering CAD.
Blender +Z up, 1 unit = 1 metre. Chassis mount pivot (0,0,0);
input shaft +X, output shaft +Y, common pitch-cone apex (0,0,0.46).
Run: blender -b --factory-startup --python-exit-code 1 --python generate_m02_016.py -- --out DIR
"""
from __future__ import annotations
import argparse, json, math, sys
from pathlib import Path

ID="M02-016"
N1,N2=16,24
RATIO=-N1/N2
D1=math.atan(N1/N2)
D2=math.pi/2-D1
APEX=(0,0,.46)
S0,S1=.245,.35
FRAMES=121
MATERIALS=("graphite_frame","machined_titanium","hardened_steel",
           "copper_fittings","rubber_isolators","safety_amber","status_cyan")

def source_self_test():
    assert abs(D1+D2-math.pi/2)<1e-12
    assert abs(math.tan(D1)-N1/N2)<1e-12
    assert len(MATERIALS)>=3
    # One gear tooth at the pitch generator must meet the other gear's gap.
    for frame in range(FRAMES):
        a=4*math.pi*frame/(FRAMES-1)
        b=RATIO*a
        d1=abs(((a+math.pi/N1)%(2*math.pi/N1))-math.pi/N1)/(2*math.pi/N1)
        d2=abs(((b+math.pi/N2+math.pi/N2)%(2*math.pi/N2))-math.pi/N2)/(2*math.pi/N2)
        assert not(d1<.11 and d2<.11),(frame,d1,d2)
    return {"id":ID,"sampled_gear_phases":FRAMES,"teeth_input":N1,
            "teeth_output":N2,"ratio":RATIO,
            "pitch_cone_half_angles_deg":[math.degrees(D1),math.degrees(D2)],
            "mount_pivot_m":[0,0,0],"gear_apex_m":list(APEX)}

def build(out):
    import bpy, bmesh
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    out.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'
    scene.unit_settings.scale_length=1.
    scene.frame_start,scene.frame_end=1,FRAMES
    scene.render.fps=30
    apex=Vector(APEX)

    def material(name,c,metal,rough,glow=0):
        m=bpy.data.materials.new(name)
        m.diffuse_color=(*c,1)
        m.use_nodes=True
        p=m.node_tree.nodes.get('Principled BSDF')
        p.inputs['Base Color'].default_value=(*c,1)
        p.inputs['Metallic'].default_value=metal
        p.inputs['Roughness'].default_value=rough
        if glow and p.inputs.get('Emission Color'):
            p.inputs['Emission Color'].default_value=(*c,1)
            p.inputs['Emission Strength'].default_value=glow
        return m
    mats={
      "graphite_frame":material("graphite_frame",(.08,.10,.12),.7,.4),
      "machined_titanium":material("machined_titanium",(.38,.47,.54),.9,.27),
      "hardened_steel":material("hardened_steel",(.15,.20,.25),.92,.23),
      "copper_fittings":material("copper_fittings",(.62,.25,.09),.85,.28),
      "rubber_isolators":material("rubber_isolators",(.035,.045,.05),0,.82),
      "safety_amber":material("safety_amber",(.94,.35,.025),.35,.32),
      "status_cyan":material("status_cyan",(.04,.72,.84),.25,.23,1.5)}

    def mesh(name,vertices,faces,mat,parent=None,smooth=False):
        data=bpy.data.meshes.new(name+'_mesh')
        data.from_pydata(vertices,[],faces)
        data.update()
        ob=bpy.data.objects.new(name,data)
        bpy.context.collection.objects.link(ob)
        if parent: ob.parent=parent
        data.materials.append(mats[mat])
        bm=bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
        bm.to_mesh(data)
        bm.free()
        if smooth:
            for f in data.polygons: f.use_smooth=True
        return ob

    def empty(name,pos):
        o=bpy.data.objects.new(name,None)
        bpy.context.collection.objects.link(o)
        o.location=pos
        o.empty_display_type='PLAIN_AXES'
        o.empty_display_size=.08
        return o
    input_rot=empty("INPUT_BEVEL_ROT_X",APEX)
    output_rot=empty("OUTPUT_BEVEL_ROT_Y",APEX)
    empty("MOUNT_DATUM_ORIGIN",(0,0,0))
    empty("INPUT_SHAFT_SOCKET_X",(.74,0,.46))
    empty("OUTPUT_SHAFT_SOCKET_Y",(0,.74,.46))

    def tube(name,p,q,outer,mat,parent=None,inner=0,segments=32):
        p,q=Vector(p),Vector(q)
        axis=(q-p).normalized()
        u=axis.cross(Vector((0,0,1)))
        if u.length<.01: u=axis.cross(Vector((0,1,0)))
        u.normalize()
        v=axis.cross(u).normalized()
        off=apex if parent else Vector((0,0,0))
        rings=[]
        for center in (p,q):
            for radius in ((outer,inner) if inner else (outer,)):
                rings.append([tuple(center-off+radius*(u*math.cos(2*math.pi*i/segments)+
                                    v*math.sin(2*math.pi*i/segments)))
                              for i in range(segments)])
        vertices=[pt for ring in rings for pt in ring]
        faces=[]
        def join(a,b):
            for i in range(segments):
                j=(i+1)%segments
                faces.append((a+i,a+j,b+j,b+i))
        if inner:
            join(0,2*segments)
            join(segments,3*segments)
            join(0,segments)
            join(2*segments,3*segments)
        else:
            join(0,segments)
            faces.append(tuple(reversed(range(segments))))
            faces.append(tuple(range(segments,2*segments)))
        return mesh(name,vertices,faces,mat,parent,True)

    def block(name,center,size,mat):
        bpy.ops.mesh.primitive_cube_add(size=1,location=center)
        ob=bpy.context.object
        ob.name=name
        ob.dimensions=size
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        ob.data.materials.append(mats[mat])
        bev=ob.modifiers.new("edge_chamfer",'BEVEL')
        bev.width=.006
        bev.segments=2
        ob.modifiers.new("weighted_normals",'WEIGHTED_NORMAL')
        return ob

    x,y,z=Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1))
    def body(name,parent,axis,u,v,delta):
        count=96
        vertices=[]
        for s in (S0,S1):
            r=s*math.sin(delta)-.018*s/S1
            for i in range(count):
                angle=2*math.pi*i/count
                vertices.append(tuple(axis*(s*math.cos(delta))+
                                      r*(u*math.cos(angle)+v*math.sin(angle))))
        faces=[(i,(i+1)%count,count+(i+1)%count,count+i) for i in range(count)]
        faces += [tuple(reversed(range(count))),tuple(range(count,2*count))]
        mesh(name,vertices,faces,"machined_titanium",parent,True)

    def tooth(name,parent,axis,u,v,delta,angle,pitch):
        # Narrow tips and generous root gaps for mechanical preview clearance.
        profile=((-0.32,-.019),(-.22,-.019),(-.105,.0065),
                 (.105,.0065),(.22,-.019),(.32,-.019))
        vertices=[]
        for s in (S0,S1):
            for frac,dr in profile:
                t=angle+frac*pitch
                r=s*math.sin(delta)+dr*s/S1
                vertices.append(tuple(axis*(s*math.cos(delta))+
                                      r*(u*math.cos(t)+v*math.sin(t))))
        faces=[(i,(i+1)%6,6+(i+1)%6,6+i) for i in range(6)]
        faces += [tuple(reversed(range(6))),tuple(range(6,12))]
        mesh(name,vertices,faces,"hardened_steel",parent)

    body("INPUT_PITCH_CONE_16T",input_rot,x,y,z,D1)
    body("OUTPUT_PITCH_CONE_24T",output_rot,y,x,-z,D2)
    for i in range(N1):
        tooth("INPUT_TOOTH_%02d"%i,input_rot,x,y,z,D1,2*math.pi*i/N1,2*math.pi/N1)
    for i in range(N2):
        tooth("OUTPUT_TOOTH_%02d"%i,output_rot,y,x,-z,D2,
              math.pi/N2+2*math.pi*i/N2,2*math.pi/N2)
    tube("INPUT_SHAFT",(.19,0,.46),(.74,0,.46),.0225,"hardened_steel",input_rot)
    tube("OUTPUT_SHAFT",(0,.14,.46),(0,.74,.46),.0225,"hardened_steel",output_rot)
    for label,parent,axis,u,v,delta in (
        ("INPUT",input_rot,x,y,z,D1),("OUTPUT",output_rot,y,x,-z,D2)):
        c=apex+axis*(S1*math.cos(delta)+.014)
        tube(label+"_COPPER_HUB",c-axis*.012,c+axis*.012,.064,
             "copper_fittings",parent)
        tube(label+"_HUB_CAP",c+axis*.012,c+axis*.023,.036,
             "graphite_frame",parent)
        for i in range(6):
            t=2*math.pi*i/6
            p=c+.083*(u*math.cos(t)+v*math.sin(t))
            tube(label+"_BOLT_%02d"%i,p-axis*.006,p+axis*.010,.008,
                 "safety_amber",parent,segments=12)

    # Static mounting sled, four chassis fasteners and two shaft bearing pedestals.
    block("FRAME_UNDERSIDE_DATUM",(.22,.22,.035),(1.15,1.15,.07),"graphite_frame")
    for ix,a in enumerate((-.23,.67)):
        for iy,b in enumerate((-.23,.67)):
            tube("MOUNT_BOLT_%d_%d"%(ix,iy),(a,b,.072),(a,b,.10),
                 .023,"copper_fittings")
            tube("MOUNT_GASKET_%d_%d"%(ix,iy),(a,b,.073),(a,b,.083),
                 .033,"rubber_isolators",inner=.024)
    for label,p,q,support in (
        ("INPUT",(.59,0,.46),(.69,0,.46),(.64,0,.235)),
        ("OUTPUT",(0,.59,.46),(0,.69,.46),(0,.64,.235))):
        tube(label+"_BEARING_RING",p,q,.09,"machined_titanium",inner=.03)
        block(label+"_BEARING_PEDESTAL",support,(.115,.115,.40),"graphite_frame")
    tube("INPUT_DIAGONAL",(.76,-.18,.12),(.64,-.07,.32),.017,"machined_titanium")
    tube("OUTPUT_DIAGONAL",(-.18,.76,.12),(-.07,.64,.32),.017,"machined_titanium")
    block("AMBER_SERVICE_STRIPE",(.70,.22,.077),(.03,.38,.007),"safety_amber")
    tube("CYAN_STATUS_LAMP",(.70,.70,.077),(.70,.70,.103),.02,"status_cyan")

    for frame in range(1,FRAMES+1):
        a=4*math.pi*(frame-1)/(FRAMES-1)
        input_rot.rotation_euler=(a,0,0)
        output_rot.rotation_euler=(0,RATIO*a,0)
        input_rot.keyframe_insert(data_path='rotation_euler',frame=frame)
        output_rot.keyframe_insert(data_path='rotation_euler',frame=frame)
    for ob in (input_rot,output_rot):
        act=ob.animation_data.action
        if hasattr(act,'fcurves'):
            for curve in act.fcurves:
                for k in curve.keyframe_points: k.interpolation='LINEAR'
    scene.frame_set(1)
    mesh_objects=[o for o in bpy.data.objects if o.type=='MESH']
    for ob in mesh_objects:
        bm=bmesh.new()
        bm.from_mesh(ob.data)
        nonmanifold=sum(not e.is_manifold for e in bm.edges)
        bm.free()
        assert nonmanifold==0,(ob.name,nonmanifold)

    def group_bvh(group):
        vertices,triangles=[],[]
        for ob in mesh_objects:
            if ob.parent!=group: continue
            offset=len(vertices)
            vertices.extend([tuple(ob.matrix_world @ v.co) for v in ob.data.vertices])
            ob.data.calc_loop_triangles()
            triangles.extend([tuple(offset+i for i in t.vertices)
                              for t in ob.data.loop_triangles])
        return BVHTree.FromPolygons(vertices,triangles,all_triangles=True,epsilon=.00005)
    sampled=[1,11,21,31,41,51,61,71,81,91,101,111,121]
    collisions={}
    for frame in sampled:
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        hits=group_bvh(input_rot).overlap(group_bvh(output_rot))
        if hits: collisions[str(frame)]=len(hits)
    assert not collisions,("cross-gear triangle intersections",collisions)
    scene.frame_set(1)
    qa={**source_self_test(),"blender_version":bpy.app.version_string,
        "mesh_objects":len(mesh_objects),"materials":len(mats),
        "sampled_bvh_frames":sampled,"cross_gear_bvh_hits":0,
        "bvh_limitation":"13 discrete frames, not continuous swept collision or PhysX",
        "glb_status":"NOT RUN","fbx_status":"NOT RUN"}
    bpy.ops.wm.save_as_mainfile(filepath=str(out/(ID+".blend")))
    bpy.ops.export_scene.gltf(filepath=str(out/(ID+"_animated.glb")),
                              export_format='GLB',export_animations=True)
    qa["glb_status"]="EXPORTED_NOT_UNITY_IMPORTED"
    try:
        fbx=str(out/(ID+"_animated.fbx"))
        try:
            bpy.ops.export_scene.fbx(filepath=fbx,use_selection=False,
                                     bake_anim=True,add_leaf_bones=False)
        except TypeError:
            # Blender 5 FBX operator differs from Blender 4.
            bpy.ops.export_scene.fbx(filepath=fbx)
        qa["fbx_status"]="EXPORTED_NOT_UNITY_IMPORTED"
    except Exception as e:
        qa["fbx_status"]="BLOCKED_FBX_OPERATOR: "+str(e)
    (out/(ID+"_qa.json")).write_text(json.dumps(qa,indent=2)+"\n",encoding="utf-8")
    print("M02-016 QA",json.dumps(qa))

if __name__=="__main__":
    if "--" not in sys.argv:
        print(json.dumps(source_self_test(),indent=2))
    else:
        ap=argparse.ArgumentParser()
        ap.add_argument("--out",required=True,type=Path)
        build(ap.parse_args(sys.argv[sys.argv.index("--")+1:]).out)
