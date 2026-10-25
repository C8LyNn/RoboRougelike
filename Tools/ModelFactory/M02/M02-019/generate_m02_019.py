#!/usr/bin/env python3
"""M02-019: hydraulic drum brake. Blender 4.x/5.x, meters, +Y axle."""
import bpy, bmesh, math, json, os, sys
from mathutils import Vector
from math import sin, cos, pi, radians

ID = "M02-019"
ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "generated")
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.curves, bpy.data.actions):
    for datablock in list(datablocks):
        datablocks.remove(datablock, do_unlink=True)
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
scene.frame_start = 1
scene.frame_end = 121
scene.render.fps = 30

def material(name, rgba, metallic=0.0, roughness=0.45):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgba, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgba, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return mat

steel = material("01_BlueGrey_ForgedSteel", (.15,.22,.27), .78,.3)
iron = material("02_Machined_Iron", (.35,.39,.42), .83,.32)
ceramic = material("03_Ceramic_Friction", (.48,.39,.30), .06,.83)
bronze = material("04_Bronze_Hydraulic", (.60,.39,.16), .7,.33)
blue = material("05_Hydraulic_Blue", (.06,.24,.53), .5,.3)
orange = material("06_Service_Orange", (.95,.34,.05), .25,.37)
rubber = material("07_Sealing_Rubber", (.05,.06,.07), .05,.88)
bright = material("08_Polished_Shaft", (.66,.72,.76), .92,.18)

def empty(name, location=(0,0,0), parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.empty_display_size = .045
    if parent:
        obj.parent = parent
    return obj

root = empty("MOUNT_AXLE_Y__PIVOT_0_0_0")
rotor = empty("DRUM_ROTOR__ROTATE_Y", parent=root)
left = empty("LEFT_SHOE__PIVOT_LOWER", (-.097,0,-.267), root)
right = empty("RIGHT_SHOE__PIVOT_LOWER", (.097,0,-.267), root)
piston_l = empty("WHEEL_CYLINDER_PISTON_L__SLIDE_X", parent=root)
piston_r = empty("WHEEL_CYLINDER_PISTON_R__SLIDE_X", parent=root)
spring_top = empty("RETURN_SPRING_TOP__STRETCH_X", parent=root)

def finish(obj, mat, parent):
    obj.data.materials.append(mat)
    if parent:
        obj.parent = parent
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj

def box(name, loc, size, mat, parent=root, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj=bpy.context.object
    obj.name=name
    obj.dimensions=size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    finish(obj,mat,parent)
    if bevel:
        mod=obj.modifiers.new("Machined_edge_radii","BEVEL")
        mod.width=bevel
        mod.segments=2
        obj.modifiers.new("Weighted_normals","WEIGHTED_NORMAL")
    return obj

def cylinder(name, loc, radius, depth, mat, parent=root, vertices=40, axis="Y"):
    rotation=(pi/2,0,0) if axis=="Y" else ((0,pi/2,0) if axis=="X" else (0,0,0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    obj=bpy.context.object
    obj.name=name
    return finish(obj,mat,parent)

def annulus(name, ri, ro, y0, y1, a0=0, a1=2*pi, n=80, mat=steel, parent=root, pivot=(0,0,0)):
    """Closed manifold ring or arc segment; coordinates in XZ, extrusion along Y."""
    full=abs((a1-a0)-2*pi)<1e-7
    count=n if full else n+1
    verts=[]
    for i in range(count):
        a=a0+(a1-a0)*i/n
        for r,y in ((ri,y0),(ro,y0),(ri,y1),(ro,y1)):
            verts.append((r*cos(a)-pivot[0],y-pivot[1],r*sin(a)-pivot[2]))
    faces=[]
    steps=n
    for i in range(steps):
        j=(i+1)%count
        a,b=4*i,4*j
        faces.extend(((a,a+1,b+1,b),(a+2,b+2,b+3,a+3),
                      (a,b,b+2,a+2),(a+1,a+3,b+3,b+1)))
    if not full:
        faces.extend(((0,2,3,1),(4*n,4*n+1,4*n+3,4*n+2)))
    mesh=bpy.data.meshes.new(name+"_MESH")
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    if parent:
        obj.parent=parent
    obj.data.materials.append(mat)
    bm=bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth=True
    return obj

def tube(name, points, thickness, mat, parent=root):
    curve=bpy.data.curves.new(name+"_CURVE","CURVE")
    curve.dimensions="3D"
    curve.resolution_u=16
    curve.bevel_depth=thickness
    curve.bevel_resolution=3
    spline=curve.splines.new("POLY")
    spline.points.add(len(points)-1)
    for p,xyz in zip(spline.points,points):
        p.co=(*xyz,1)
    obj=bpy.data.objects.new(name,curve)
    bpy.context.collection.objects.link(obj)
    curve.materials.append(mat)
    obj.parent=parent
    return obj

# Nonrotating rear backplate, axle mounting, service mounting bolts.
annulus("STATIONARY_BACKPLATE", .080,.304,-.104,-.088,n=96,mat=steel)
cylinder("AXLE_SUPPORT_BEARING", (0,-.082,0),.085,.12,bright)
for sx in (-1,1):
    for sz in (-1,1):
        x,z=sx*.267,sz*.267
        box("CHASSIS_BRACKET_%s_%s"%(sx,sz),(x,-.132,z),(.072,.052,.072),steel,bevel=.007)
        cylinder("MOUNT_BOLT_%s_%s"%(sx,sz),(x,-.158,z),.015,.022,bronze)
for angle in (pi/4,3*pi/4,5*pi/4,7*pi/4):
    x,z=.26*cos(angle),.26*sin(angle)
    cylinder("BACKPLATE_RIVET",(x,-.087,z),.012,.009,bronze)

# Rotating drum, genuine open inspection windows in the face (not texture).
annulus("DRUM_CYLINDRICAL_FRICTION_SHELL",.306,.343,-.052,.078,n=128,mat=iron,parent=rotor)
for k in range(8):
    start=radians(k*45+7.5)
    end=radians((k+1)*45-7.5)
    annulus("OPEN_WINDOW_FACE_SPOKE_%02d"%k,.175,.343,.078,.097,start,end,n=12,mat=steel,parent=rotor)
annulus("DRUM_HUB_RING",.080,.142,.077,.116,n=64,mat=bright,parent=rotor)
cylinder("ROTATING_WHEEL_FLANGE",(0,.122,0),.079,.032,iron,rotor)
for k in range(6):
    a=k*pi/3
    cylinder("WHEEL_FLANGE_STUD_%02d"%k,(.115*cos(a),.133,.115*sin(a)),.011,.028,bronze,rotor,16)
for k in range(8):
    a=k*pi/4
    annulus("ORANGE_WINDOW_MARKER_%02d"%k,.323,.338,.098,.102,a-.045,a+.045,4,orange,rotor)

# Pair of arcuate brake shoes: lower fixed pivot, upper hydraulic actuation.
# Arc material and metal backing have different radial layers.
for name,grp,s,angles in (
    ("LEFT",left,-1,(110,250)),
    ("RIGHT",right,1,(-70,70))
):
    p=(s*.097,0,-.267)
    a0,a1=map(radians,angles)
    annulus(name+"_SHOE_STEEL_BACKING",.248,.283,-.033,.027,a0,a1,48,steel,grp,p)
    annulus(name+"_FRICTION_CERAMIC",.283,.293,-.027,.022,a0,a1,48,ceramic,grp,p)
    # Small anchor collar remains stationary; shoes rotate around its axle.
    cylinder(name+"_ANCHOR_PIN",(s*.097,.038,-.267),.022,.068,bronze,root,24)
    cylinder(name+"_ANCHOR_BUSHING",(s*.097,.080,-.267),.029,.013,rubber,root,24)
    # Separate service rivets follow the shoe motion.
    for a in (angles[0]+25,angles[0]+70,angles[1]-25):
        th=radians(a)
        x,z=.267*cos(th)-p[0],.267*sin(th)-p[2]
        cylinder(name+"_LINING_RIVET",(x,.030,z),.008,.008,bronze,grp,16)

# Hydraulic wheel cylinder mounted at top, with opposed moving plungers.
cylinder("HYDRAULIC_WHEEL_CYLINDER", (0,.046,.265),.024,.144,blue,root,40,"X")
for sign,grp in ((-1,piston_l),(1,piston_r)):
    cylinder("PISTON_ROD_%s"%sign,(sign*.084,.046,.265),.010,.052,bright,grp,24,"X")
    cylinder("PISTON_HEAD_%s"%sign,(sign*.107,.046,.265),.015,.012,bronze,grp,24,"X")
    cylinder("RUBBER_SEAL_%s"%sign,(sign*.071,.046,.265),.018,.013,rubber,grp,24,"X")
cylinder("BLEED_SCREW",(0,.082,.289),.009,.025,bronze,root,16)
tube("EXTERNAL_HYDRAULIC_PIPE",[(0,.046,.290),(0,-.116,.311),(-.10,-.14,.35),(-.28,-.14,.36)],.005,blue,root)
cylinder("HYDRAULIC_INLET_FITTING",(-.28,-.14,.36),.013,.027,bronze,root,16,"X")

# Return springs are in front of the shoes; do not intersect friction material.
def spring(name, z, y, radius, turns, parent=root):
    pts=[]
    for i in range(201):
        t=i/200
        x=-.145+.290*t
        pts.append((x,y+radius*cos(2*pi*turns*t),z+radius*sin(2*pi*turns*t)))
    return tube(name,pts,.0035,bright,parent)
spring("RETURN_SPRING_UPPER",.166,.062,.011,10,spring_top)
spring("RETURN_SPRING_LOWER",-.167,.065,.011,9,root)

# Hydraulic response: positive Y rotation pushes the right upper shoe outwards.
# At full engagement 1.5 degrees; minimum clearance > 5 mm.
for frame, alpha in ((1,0),(30,1),(64,1),(94,0),(121,0)):
    left.rotation_euler[1]=-radians(1.5)*alpha
    right.rotation_euler[1]=radians(1.5)*alpha
    piston_l.location.x=-.011*alpha
    piston_r.location.x=.011*alpha
    spring_top.scale.x=1+.075*alpha
    for obj in (left,right):
        obj.keyframe_insert(data_path="rotation_euler",frame=frame)
    for obj in (piston_l,piston_r):
        obj.keyframe_insert(data_path="location",frame=frame)
    spring_top.keyframe_insert(data_path="scale",frame=frame)
for frame, turns in ((1,0),(121,2)):
    rotor.rotation_euler[1]=2*pi*turns
    rotor.keyframe_insert(data_path="rotation_euler",frame=frame)
if rotor.animation_data and rotor.animation_data.action:
    try:
        for fc in rotor.animation_data.action.fcurves:
            for kp in fc.keyframe_points:
                kp.interpolation="LINEAR"
    except AttributeError:
        pass

# Analytic swept-envelope QA, independent of display shading.
minimum=1.0
for frame in range(121):
    alpha=(frame/29 if frame<29 else (1 if frame<=63 else
           ((93-frame)/30 if frame<93 else 0)))
    alpha=max(0,min(1,alpha))
    for s,angles in ((1,(-70,70)),(-1,(110,250))):
        px,pz=s*.097,-.267
        theta=s*radians(1.5)*alpha
        for i in range(281):
            a=radians(angles[0]+(angles[1]-angles[0])*i/280)
            x,z=.293*cos(a),.293*sin(a)
            dx,dz=x-px,z-pz
            X=px+cos(theta)*dx+sin(theta)*dz
            Z=pz-sin(theta)*dx+cos(theta)*dz
            minimum=min(minimum,.306-math.hypot(X,Z))
assert minimum>.004, "Shoe collides with drum: %.6f"%minimum
assert len(bpy.data.materials)==8, "Unexpected material duplication"
assert scene.unit_settings.scale_length==1
mesh_objects=[o for o in bpy.data.objects if o.type=="MESH"]
assert len(mesh_objects)>=30
# Topological QA of source meshes (before glTF triangulation).
nonmanifold=[]
degenerate=[]
for obj in mesh_objects:
    bm=bmesh.new()
    bm.from_mesh(obj.data)
    if any(not edge.is_manifold for edge in bm.edges):
        nonmanifold.append(obj.name)
    if any(face.calc_area()<1e-11 for face in bm.faces):
        degenerate.append(obj.name)
    bm.free()
assert not nonmanifold, "Non-manifold source meshes: "+str(nonmanifold)
assert not degenerate, "Degenerate source faces: "+str(degenerate)
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,ID+".blend"))
bpy.ops.object.select_all(action="DESELECT")
for obj in bpy.context.scene.objects:
    if obj.type in {"MESH","EMPTY","CURVE"}:
        obj.select_set(True)
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,ID+"_animated.glb"),
    export_format="GLB",use_selection=True,export_animations=True)
report={
    "id":ID,"status":"BLENDER_EXPORT_COMPLETE",
    "scale":"1 Blender unit = 1 m","mount_pivot":[0,0,0],"axle":"+Y",
    "moving":["DRUM_ROTOR","LEFT_SHOE","RIGHT_SHOE",
              "WHEEL_CYLINDER_PISTON_L","WHEEL_CYLINDER_PISTON_R",
              "RETURN_SPRING_TOP"],
    "mesh_count":len(mesh_objects),"material_count":len(bpy.data.materials),
    "manifold_meshes_checked":len(mesh_objects),"degenerate_faces":0,
    "motion_samples":121,"minimum_radial_shoe_to_drum_clearance_m":round(minimum,6),
    "files":[ID+".blend",ID+"_animated.glb"],
    "limitations":["Analytic swept envelope, not full triangle-triangle CCD",
                   "Unity import and gameplay physics not tested"]
}
with open(os.path.join(OUT,ID+"_qa.json"),"w",encoding="utf-8") as f:
    json.dump(report,f,indent=2)
print("M02_019_EXPORT_QA "+json.dumps(report))
