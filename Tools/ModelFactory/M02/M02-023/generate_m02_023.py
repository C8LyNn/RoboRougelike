#!/usr/bin/env python3
"""M02-023 — animated side-mounted 24:16 roller chain drive. Blender 4+; metres."""
import bpy
import bmesh
import json
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
from kinematics_m02_023 import (
    ID, PITCH, LINKS, BIG_TEETH, SMALL_TEETH, BIG_R, SMALL_R,
    CENTRES, Z_CENTER, Y_PLANE, FRAMES, ROLLER_R, PLATE_LENGTH,
    link_pose, sprocket_angles, analytical_qa
)
OUT = HERE / "generated"
OUT.mkdir(parents=True,exist_ok=True)

def reset():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o,do_unlink=True)
    for col in list(bpy.data.collections):
        if col.name.startswith(ID+"_SIDE_CHAIN_DRIVE"):
            bpy.data.collections.remove(col)
    for datablocks in (bpy.data.meshes,bpy.data.materials,bpy.data.actions):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)

def build():
    reset()
    scene=bpy.context.scene
    scene.unit_settings.system="METRIC"
    scene.unit_settings.scale_length=1.0
    scene.frame_start=1
    scene.frame_end=FRAMES
    scene.render.fps=30
    collection=bpy.data.collections.new(ID+"_SIDE_CHAIN_DRIVE")
    scene.collection.children.link(collection)

    def material(name,color,metallic,roughness,emission=0.0):
        m=bpy.data.materials.new(ID+"_"+name)
        m.diffuse_color=(*color,1)
        m.use_nodes=True
        bsdf=m.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value=(*color,1)
        bsdf.inputs["Metallic"].default_value=metallic
        bsdf.inputs["Roughness"].default_value=roughness
        if emission:
            e=bsdf.inputs.get("Emission Color")
            strength=bsdf.inputs.get("Emission Strength")
            if e: e.default_value=(*color,1)
            if strength: strength.default_value=emission
        return m

    graphite=material("01_forged_graphite",(.075,.091,.115),.85,.34)
    alloy=material("02_brushed_alloy",(.56,.63,.69),.73,.29)
    bronze=material("03_bronze_bushings",(.68,.39,.14),.72,.29)
    orange=material("04_safety_orange",(.98,.27,.035),.26,.40)
    rubber=material("05_chain_seal",(.045,.054,.061),.04,.84)
    ceramic=material("06_cream_ceramic",(.80,.83,.78),.16,.43)
    steel=material("07_hardened_steel",(.20,.245,.285),.94,.25)
    cyan=material("08_service_led",(.02,.54,.85),.18,.24,1.2)
    mats=(graphite,alloy,bronze,orange,rubber,ceramic,steel,cyan)

    def relink(o,mat=None,parent=None):
        for old in list(o.users_collection):
            old.objects.unlink(o)
        collection.objects.link(o)
        if mat is not None:
            o.data.materials.append(mat)
        if parent is not None:
            o.parent=parent
        return o

    def empty(name,location=(0,0,0),parent=None):
        o=bpy.data.objects.new(name,None)
        collection.objects.link(o)
        o.empty_display_type="PLAIN_AXES"
        o.empty_display_size=.06
        o.location=location
        if parent: o.parent=parent
        return o

    def cube(name,loc,dimensions,mat,parent=None,bevel=0.0):
        bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
        o=bpy.context.object
        o.name=name
        o.dimensions=dimensions
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
        relink(o,mat,parent)
        if bevel:
            mod=o.modifiers.new("machined_edge_bevel","BEVEL")
            mod.width=bevel
            mod.segments=2
            o.modifiers.new("weighted_normals","WEIGHTED_NORMAL")
        return o

    def cylinder(name,loc,radius,depth,mat,parent=None,vertices=24):
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,
            depth=depth,location=loc,rotation=(math.pi/2,0,0))
        o=bpy.context.object
        o.name=name
        relink(o,mat,parent)
        for face in o.data.polygons:
            if len(face.vertices)==4: face.use_smooth=True
        return o

    def torus(name,loc,major,minor,mat,parent=None):
        bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=8,
            location=loc,rotation=(math.pi/2,0,0),
            major_radius=major,minor_radius=minor)
        o=bpy.context.object
        o.name=name
        relink(o,mat,parent)
        for face in o.data.polygons: face.use_smooth=True
        return o

    def tooth(name,angle,r0,r1,half_angle,half_y,mat,parent):
        def point(radius,a,y):
            return (radius*math.cos(a),y,radius*math.sin(a))
        corners=[point(r0,angle-half_angle,-half_y),
                 point(r1,angle-half_angle,-half_y),
                 point(r1,angle+half_angle,-half_y),
                 point(r0,angle+half_angle,-half_y)]
        vertices=corners+[ (x,half_y,z) for x,y,z in corners ]
        faces=[(3,2,1,0),(4,5,6,7),
               (0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
        mesh=bpy.data.meshes.new(name+"_mesh")
        mesh.from_pydata(vertices,[],faces)
        mesh.update()
        o=bpy.data.objects.new(name,mesh)
        collection.objects.link(o)
        mesh.materials.append(mat)
        o.parent=parent
        return o

    mount=empty("MOUNT_SIDE_SOCKET_UNITY_1M",(0,0,0))
    cube("FRAME_BOLT_PLATE",(0,-.015,0),(.24,.03,.12),graphite,mount,.006)
    for x in (-.087,.087):
        for z in (-.037,.037):
            cylinder("MOUNT_FASTENER",(x,-.034,z),.012,.009,alloy,mount,12)
    cube("BACKBONE_LOWER_RAIL",(0,-.077,Z_CENTER-.13),
         (CENTRES+.32,.065,.068),graphite,mount,.009)
    for x in (-.095,.095):
        cube("BACKBONE_VERTICAL_BRACE",(x,-.064,-.165),
             (.043,.053,.27),steel,mount,.005)
    for x in (-CENTRES/2,CENTRES/2):
        cube("AXLE_SUPPORT_PEDESTAL",(x,-.105,Z_CENTER-.065),
             (.084,.084,.12),alloy,mount,.008)
        cylinder("FIXED_AXLE_SHAFT",(x,-.152,Z_CENTER),.0175,.250,
                 steel,mount,32)
        torus("STATIONARY_BEARING_RING",(x,-.116,Z_CENTER),
              .030,.007,bronze,mount)
        cylinder("SERVICE_AXLE_END",(x,-.033,Z_CENTER),
                 .024,.014,orange,mount,24)

    gears=[]
    for tag,x,teeth,radius in (
        ("INPUT_BIG_24T",-CENTRES/2,BIG_TEETH,BIG_R),
        ("OUTPUT_SMALL_16T",CENTRES/2,SMALL_TEETH,SMALL_R)
    ):
        group=empty(tag+"_SPIN_Y",(x,Y_PLANE,Z_CENTER),mount)
        gears.append(group)
        # Open wheel: ring, eight radial spokes, hollow central bearing, no solid disk.
        torus(tag+"_RIM",(0,0,0),radius-.036,.010,steel,group)
        torus(tag+"_HOLLOW_HUB",(0,0,0),.032,.008,bronze,group)
        torus(tag+"_HUB_ORANGE_LOCK",(0,-.016,0),.035,.003,orange,group)
        for j in range(8):
            a=2*math.pi*j/8
            mid=(.046+radius-.048)/2
            length=radius-.048-.046
            spoke=cube(tag+"_SPOKE_%02d"%j,
                (mid*math.cos(a),0,mid*math.sin(a)),
                (length,.012,.012),alloy,group,.002)
            spoke.rotation_euler.y=-a
        for j in range(teeth):
            a=2*math.pi*j/teeth
            tooth(tag+"_TOOTH_%02d"%j,a,radius-.036,radius-.013,
                  .29*2*math.pi/teeth,.009,steel,group)
        for j in range(6):
            a=2*math.pi*j/6
            cylinder(tag+"_BOLT_%02d"%j,
                (.049*math.cos(a),-.014,.049*math.sin(a)),
                .005,.005,ceramic,group,12)

    # Rollers and paired steel plates are separate solids; their Y envelopes
    # are disjoint, and chain rollers clear the tooth tips by >=5 mm.
    links=[]
    for i in range(LINKS):
        root=empty("CHAIN_LINK_%02d_TRAVEL"%i,(0,0,0),mount)
        links.append(root)
        for side in (-1,1):
            cube("LINK_%02d_PLATE_%s"%(i,"L" if side<0 else "R"),
                (0,side*.023,0),(PLATE_LENGTH,.006,.010),
                graphite if i%2 else alloy,root,.002)
            cylinder("LINK_%02d_PIN_HEAD_%s"%(i,"L" if side<0 else "R"),
                (0,side*.029,0),.0045,.002,orange if i%8==0 else steel,
                root,12)
        cylinder("LINK_%02d_BRONZE_ROLLER"%i,(0,0,0),
                 ROLLER_R,.025,bronze,root,20)

    # Status LED and tension-index ruler stay fixed, behind the moving chain.
    cube("CHAIN_TENSION_SCALE",(0,-.104,Z_CENTER-.185),
         (.16,.008,.021),ceramic,mount,.002)
    for j in range(7):
        cube("TENSION_MARK_%02d"%j,(-.061+j*.020,-.112,Z_CENTER-.185),
             (.003,.003,.013),orange,mount)
    cube("SERVICE_LED",(0,-.115,Z_CENTER-.133),
         (.037,.008,.013),cyan,mount,.002)

    for frame in range(1,FRAMES+1):
        big,small=sprocket_angles(frame)
        for group,ang in zip(gears,(big,small)):
            group.rotation_euler.y=ang
            group.keyframe_insert(data_path="rotation_euler",frame=frame)
        for i,root in enumerate(links):
            x,z,angle=link_pose(i,frame)
            root.location=(x,Y_PLANE,z)
            root.rotation_euler.y=-angle
            root.keyframe_insert(data_path="location",frame=frame)
            root.keyframe_insert(data_path="rotation_euler",frame=frame)
    scene.frame_set(1)
    return {"collection":collection,"materials":len(mats),
        "mesh_objects":sum(o.type=="MESH" for o in collection.objects),
        "animated_groups":[o.name for o in [*gears,*links]]}

def topology():
    bad=[]
    count=0
    for o in bpy.data.objects:
        if o.type!="MESH": continue
        bm=bmesh.new()
        bm.from_mesh(o.data)
        if not bm.faces or any(not e.is_manifold for e in bm.edges):
            bad.append(o.name+":nonmanifold")
        if any(f.calc_area()<1e-11 for f in bm.faces):
            bad.append(o.name+":degenerate")
        bm.free()
        count+=1
    assert not bad,bad[:15]
    return count

def main():
    qa=analytical_qa()
    first=build()
    count1=topology()
    second=build()
    count2=topology()
    assert count1==count2==first["mesh_objects"]==second["mesh_objects"]
    assert first["materials"]==second["materials"]==8
    assert count2>250,count2
    blend=OUT/(ID+".blend")
    glb=OUT/(ID+"_animated.glb")
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    bpy.ops.object.select_all(action="DESELECT")
    for o in second["collection"].objects:
        o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True,
        export_frame_range=True,export_force_sampling=True)
    assert blend.stat().st_size>10000 and glb.stat().st_size>10000
    report={"id":ID,"blender":bpy.app.version_string,
        "scale_m_per_unit":1.0,"pivot":"MOUNT_SIDE_SOCKET_UNITY_1M",
        "sprocket_axis":"Blender +Y","chain_travel":"clockwise in XZ plane",
        "mesh_objects":count2,"materials":second["materials"],
        "animated_groups":second["animated_groups"],
        "analytical_qa":qa,"reentrant":"PASS","manifold":"PASS",
        "blend_bytes":blend.stat().st_size,"glb_bytes":glb.stat().st_size,
        "unity_import":"NOT RUN","full_triangle_collision":"NOT RUN"}
    (OUT/(ID+"_qa.json")).write_text(json.dumps(report,indent=2),encoding="utf-8")
    print("M02-023 RESULT "+json.dumps(report,sort_keys=True))

if __name__=="__main__":
    main()
