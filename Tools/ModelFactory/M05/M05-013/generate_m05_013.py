#!/usr/bin/env python3
"""M05-013 — side-mounted articulated REPAIR manipulator (visual only).

Unity metres, X outward from side mount, Y up, Z lateral. MOUNT_SIDE_SOCKET
at (0,0,0), mounting plate flush with X=0. Three *independent* planar joints
about local +Z: shoulder at X=.27, elbow +.38, wrist +.34. No gameplay code.
Blender 4.0+ creates rig, 36-frame animation, FBX/GLB/BLEND; CPython can
generate a genuine static OBJ/MTL and run the geometry/kinematic QA.
"""
from __future__ import annotations
import argparse
import json
import math
import sys
from pathlib import Path

ASSET_ID = "M05-013"
FPS = 30
KEYS = ((1, (0,0,0)), (10, (12,-16,8)), (19, (24,-32,14)),
        (28, (8,-8,-6)), (36, (0,0,0)))
SHOULDER_X = .27
UPPER_LENGTH = .38
FORE_LENGTH = .34
ORIGINS = {"BASE":(0.,0.,0.), "SHOULDER":(.27,0.,0.),
           "ELBOW":(.65,0.,0.), "WRIST":(.99,0.,0.)}
# linear RGBA, metallic, roughness, emission RGB, strength
MATERIALS = {
    "Armor_Graphite": ((.07,.095,.12,1),.72,.37,(0,0,0),0),
    "Ceramic_White": ((.72,.79,.78,1),.24,.30,(0,0,0),0),
    "Titanium": ((.34,.40,.46,1),.85,.31,(0,0,0),0),
    "Copper": ((.57,.24,.09,1),.82,.28,(0,0,0),0),
    "Rubber": ((.024,.032,.040,1),.05,.86,(0,0,0),0),
    "Repair_Cyan": ((.08,.63,.95,1),.13,.26,(.04,.67,1),3.1),
    "Signal_Amber": ((.95,.42,.06,1),.18,.34,(1,.23,.02),.5),
    "Optic_Black": ((.015,.055,.083,1),.34,.17,(.04,.13,.19),.22),
}
def addv(a,b): return tuple(x+y for x,y in zip(a,b))
def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def mul(a,s): return tuple(x*s for x in a)
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2],
            a[0]*b[1]-a[1]*b[0])
def norm(a):
    m=math.sqrt(dot(a,a))
    assert m>1e-9
    return mul(a,1/m)
def make(name,mat,group,v,f,smooth=False):
    return dict(name=name,material=mat,group=group,vertices=v,faces=f,
                smooth=smooth)

def bevel_box(name,mat,group,c,size,cut=.008):
    sx,sy,sz=size
    assert min(size)>0 and 0<cut<min(sx,sy)/2
    a,b=sx/2,sy/2
    profile=((-a+cut,-b),(a-cut,-b),(a,-b+cut),(a,b-cut),
             (a-cut,b),(-a+cut,b),(-a,b-cut),(-a,-b+cut))
    x,y,z=c
    v=[(x+dx,y+dy,z+dz) for dz in (-sz/2,sz/2)
       for dx,dy in profile]
    v.extend(((x,y,z-sz/2),(x,y,z+sz/2)))
    f=[]
    for j in range(8):
        k=(j+1)%8
        f.extend(((j,k,8+k),(j,8+k,8+j)))
        f.extend(((16,k,j),(17,8+j,8+k)))
    return make(name,mat,group,v,f)

def tube(name,mat,group,a,b,r=.02,n=16):
    """Watertight tube between any two 3D endpoints, hard cap normals."""
    axis=norm(sub(b,a))
    candidate=(0,0,1) if abs(axis[2])<.92 else (0,1,0)
    u=norm(cross(candidate,axis))
    v=cross(axis,u)
    pts=[]
    for p in (a,b):
        for j in range(n):
            t=2*math.pi*j/n
            radial=addv(mul(u,r*math.cos(t)),mul(v,r*math.sin(t)))
            pts.append(addv(p,radial))
    pts.extend((a,b))
    faces=[]
    for j in range(n):
        k=(j+1)%n
        faces.extend(((j,k,n+k),(j,n+k,n+j)))
        faces.extend(((2*n,k,j),(2*n+1,n+j,n+k)))
    return make(name,mat,group,pts,faces,True)

def build():
    p=[]
    def box(name,mat,g,c,size,cut=.008):
        p.append(bevel_box(name,mat,g,c,size,cut))
    def cyl(name,mat,g,x,y,z0,z1,r,n=20):
        p.append(tube(name,mat,g,(x,y,z0),(x,y,z1),r,n))
    def shaft(name,mat,g,a,b,r,n=16):
        p.append(tube(name,mat,g,a,b,r,n))
    # Side socket X=0; structural motor extends toward +X.
    box("Mount_Flush_Plate","Armor_Graphite","BASE",
        (.052,0,0),(.104,.43,.39),.021)
    box("Mount_Insulator","Rubber","BASE",
        (.109,0,0),(.027,.32,.30),.009)
    box("Motor_Housing","Titanium","BASE",
        (.170,0,0),(.105,.225,.225),.024)
    box("Motor_Ceramic_Cap","Ceramic_White","BASE",
        (.178,.115,0),(.086,.028,.190),.012)
    box("Socket_Key_Top","Copper","BASE",
        (.067,.175,0),(.052,.030,.14),.010)
    box("Socket_Key_Bottom","Copper","BASE",
        (.067,-.175,0),(.052,.030,.14),.010)
    for iy,y in enumerate((-.145,.145)):
        for iz,z in enumerate((-.134,.134)):
            shaft("Mount_Bolt_%d_%d"%(iy,iz),"Titanium","BASE",
                  (.107,y,z),(.133,y,z),.017,12)
    cyl("Shoulder_Stationary_Bearing","Rubber","BASE",
        SHOULDER_X,0,-.096,-.032,.082,24)
    cyl("Shoulder_Stationary_Axle","Titanium","BASE",
        SHOULDER_X,0,-.091,.081,.021,20)
    shaft("Power_Port","Copper","BASE",
          (.115,-.102,-.135),(.198,-.102,-.135),.019)
    box("Motor_Status","Signal_Amber","BASE",
        (.174,.130,.086),(.040,.014,.047),.005)
    # First articulated link: hinge, structural spine, armour and coolant.
    cyl("Shoulder_Rotating_Collar","Titanium","SHOULDER",
        0,0,.030,.081,.077,24)
    cyl("Shoulder_Indicator","Repair_Cyan","SHOULDER",
        0,0,.083,.090,.052,24)
    box("Shoulder_Link","Armor_Graphite","SHOULDER",
        (.205,0,0),(.265,.106,.068),.017)
    box("Shoulder_Hub_Bridge","Titanium","SHOULDER",
        (.077,0,.018),(.083,.067,.065),.012)
    box("Shoulder_Ceramic_Cover","Ceramic_White","SHOULDER",
        (.195,.059,.022),(.188,.020,.048),.007)
    box("Shoulder_Copper_Rib","Copper","SHOULDER",
        (.205,-.060,.026),(.172,.018,.033),.006)
    shaft("Shoulder_Coolant_Pipe","Copper","SHOULDER",
          (.095,-.087,-.016),(.315,-.087,-.016),.013)
    for i,x in enumerate((.140,.240)):
        box("Shoulder_Service_Tab_"+str(i),"Signal_Amber","SHOULDER",
            (x,.075,.045),(.032,.010,.028),.003)
    cyl("Elbow_Stationary_Bearing","Rubber","SHOULDER",
        UPPER_LENGTH,0,-.090,-.033,.075,24)
    cyl("Elbow_Stationary_Axle","Titanium","SHOULDER",
        UPPER_LENGTH,0,-.086,.080,.019,20)
    # Elbow link, intentionally shorter and asymmetric from shoulder.
    cyl("Elbow_Rotating_Collar","Titanium","ELBOW",
        0,0,.030,.079,.070,24)
    cyl("Elbow_Indicator","Signal_Amber","ELBOW",
        0,0,.080,.087,.044,20)
    box("Elbow_Link","Armor_Graphite","ELBOW",
        (.190,0,0),(.210,.091,.062),.015)
    box("Elbow_Hub_Bridge","Titanium","ELBOW",
        (.081,0,.018),(.085,.059,.062),.010)
    box("Elbow_Ceramic_Cover","Ceramic_White","ELBOW",
        (.194,.053,.024),(.143,.017,.044),.006)
    box("Elbow_Copper_Rib","Copper","ELBOW",
        (.193,-.052,.020),(.137,.016,.033),.005)
    shaft("Elbow_Coolant_Pipe","Copper","ELBOW",
          (.091,-.076,-.011),(.280,-.076,-.011),.011)
    box("Elbow_Service_Marker","Signal_Amber","ELBOW",
        (.214,.068,.042),(.044,.010,.021),.003)
    cyl("Wrist_Stationary_Bearing","Rubber","ELBOW",
        FORE_LENGTH,0,-.086,-.033,.068,24)
    cyl("Wrist_Stationary_Axle","Titanium","ELBOW",
        FORE_LENGTH,0,-.082,.080,.017,20)
    # Third articulated link and visually distinct repair head.
    cyl("Wrist_Rotating_Collar","Titanium","WRIST",
        0,0,.030,.076,.063,24)
    cyl("Wrist_Indicator","Repair_Cyan","WRIST",
        0,0,.078,.085,.039,20)
    box("Wrist_Link","Armor_Graphite","WRIST",
        (.148,0,0),(.155,.077,.056),.012)
    box("Wrist_Hub_Bridge","Titanium","WRIST",
        (.076,0,.018),(.075,.053,.056),.009)
    box("Wrist_Ceramic_Cover","Ceramic_White","WRIST",
        (.157,.045,.024),(.111,.014,.036),.005)
    box("Tool_Head_Housing","Titanium","WRIST",
        (.229,0,0),(.090,.116,.102),.013)
    shaft("Repair_Nozzle","Ceramic_White","WRIST",
          (.265,0,0),(.310,0,0),.035,24)
    shaft("Nozzle_Cyan_Lip","Repair_Cyan","WRIST",
          (.308,0,0),(.321,0,0),.031,24)
    shaft("Nozzle_Dark_Inner","Optic_Black","WRIST",
          (.322,0,0),(.326,0,0),.021,20)
    for i,side in enumerate((-1,1)):
        box("Tool_Guard_"+str(i),"Ceramic_White","WRIST",
            (.274,0,side*.078),(.085,.026,.025),.008)
        box("Tool_Amber_Marker_"+str(i),"Signal_Amber","WRIST",
            (.247,.027,side*.078),(.029,.008,.017),.003)
    shaft("Inspection_Camera","Optic_Black","WRIST",
          (.224,.075,.047),(.267,.075,.047),.025,20)
    shaft("Camera_Glass","Repair_Cyan","WRIST",
          (.266,.075,.047),(.271,.075,.047),.019,20)
    shaft("Tool_Coolant_Feed","Copper","WRIST",
          (.097,-.061,-.018),(.227,-.061,-.018),.010)
    box("Tool_Safety_Stripe","Signal_Amber","WRIST",
        (.182,-.047,.032),(.057,.011,.017),.003)
    return p

def signed_volume(vertices,faces):
    return sum(dot(a,cross(b,c))/6
               for i,j,k in faces
               for a,b,c in ((vertices[i],vertices[j],vertices[k]),))

def check_mesh(parts):
    names=set()
    counts={"parts":0,"vertices":0,"triangles":0,"materials":len(MATERIALS)}
    group_counts={k:0 for k in ORIGINS}
    for p in parts:
        assert p["name"] not in names,p["name"]
        names.add(p["name"])
        assert p["material"] in MATERIALS
        assert p["group"] in group_counts
        assert len(p["vertices"])>=4 and p["faces"]
        edges={}
        for vertex in p["vertices"]:
            assert all(math.isfinite(v) for v in vertex)
        for i,j,k in p["faces"]:
            assert len({i,j,k})==3 and min(i,j,k)>=0
            assert max(i,j,k)<len(p["vertices"])
            a,b,c=(p["vertices"][q] for q in (i,j,k))
            n=cross(sub(b,a),sub(c,a))
            assert dot(n,n)>1e-15,(p["name"],"degenerate")
            for x,y in ((i,j),(j,k),(k,i)):
                edge=tuple(sorted((x,y)))
                edges[edge]=edges.get(edge,0)+1
        assert all(v==2 for v in edges.values()),(p["name"],"nonmanifold")
        vol=signed_volume(p["vertices"],p["faces"])
        assert vol>1e-9,(p["name"],"inverted volume",vol)
        counts["parts"]+=1
        counts["vertices"]+=len(p["vertices"])
        counts["triangles"]+=len(p["faces"])
        group_counts[p["group"]]+=1
    assert counts["parts"]>=40
    assert len(MATERIALS)>=3
    assert all(group_counts.values())
    return counts,group_counts

def rotate_xy(xy,theta):
    x,y=xy
    c,s=math.cos(theta),math.sin(theta)
    return (x*c-y*s,x*s+y*c)
def plus2(a,b): return (a[0]+b[0],a[1]+b[1])
def point_segment_dist(p,a,b):
    v=(b[0]-a[0],b[1]-a[1])
    t=max(0.,min(1.,((p[0]-a[0])*v[0]+(p[1]-a[1])*v[1])/
                     (v[0]*v[0]+v[1]*v[1])))
    return math.dist(p,(a[0]+t*v[0],a[1]+t*v[1]))
def segment_dist(a,b,c,d):
    return min(point_segment_dist(p,u,v) for p,u,v in
               ((a,c,d),(b,c,d),(c,a,b),(d,a,b)))
def joint_angles(frame):
    for (f0,v0),(f1,v1) in zip(KEYS,KEYS[1:]):
        if f0<=frame<=f1:
            t=(frame-f0)/(f1-f0)
            return tuple(math.radians(a+(b-a)*t) for a,b in zip(v0,v1))
    return tuple(math.radians(v) for v in KEYS[-1][1])
def poses(frame):
    a,b,c=joint_angles(frame)
    o0=(SHOULDER_X,0.)
    o1=plus2(o0,rotate_xy((UPPER_LENGTH,0),a))
    o2=plus2(o1,rotate_xy((FORE_LENGTH,0),a+b))
    return (o0,o1,o2),(a,a+b,a+b+c)
def check_kinematics():
    # The arm is fully outside the base: base housing X max .2225.
    # A continuous 1-frame sample covers each key segment; test 360
    # sub-frame positions for conservative clearance (not physics).
    tested=0
    min_base=100.
    min_self=100.
    for k in range(361):
        frame=1+35*k/360
        (s,e,w),(a,b,c)=poses(frame)
        # Non-joint regions only; shared hinge interfaces are intended.
        shoulder0=plus2(s,rotate_xy((.095,0),a))
        shoulder1=plus2(s,rotate_xy((.315,0),a))
        elbow0=plus2(e,rotate_xy((.095,0),b))
        elbow1=plus2(e,rotate_xy((.280,0),b))
        wrist0=plus2(w,rotate_xy((.092,0),c))
        wrist1=plus2(w,rotate_xy((.326,0),c))
        min_base=min(min_base,*(p[0]-.2225 for p in
                   (shoulder0,shoulder1,elbow0,elbow1,wrist0,wrist1)))
        dist=segment_dist(shoulder0,shoulder1,wrist0,wrist1)
        min_self=min(min_self,dist)
        assert min_base>.03,(frame,"base collision",min_base)
        assert dist>.13,(frame,"nonadjacent link collision",dist)
        tested+=1
    return {"kinematic_samples":tested,
            "min_link_base_clearance_m":round(min_base,5),
            "min_nonadjacent_centerline_distance_m":round(min_self,5),
            "collision_scope":"analytical 2D link corridors; shared joints excluded"}

def static_obj(parts,out):
    stem="M05-013_repair_manipulator"
    obj=out/(stem+".obj")
    mtl=out/(stem+".mtl")
    lines=["# M05-013 1 unit = 1 metre, mount X=0, neutral pose",
           "mtllib "+mtl.name]
    index=0
    for p in parts:
        lines.extend(("o "+p["name"],"usemtl "+p["material"]))
        origin=ORIGINS[p["group"]]
        for vertex in p["vertices"]:
            xyz=addv(vertex,origin)
            lines.append("v %.9f %.9f %.9f"%xyz)
        for a,b,c in p["faces"]:
            lines.append("f %d %d %d"%(index+a+1,index+b+1,index+c+1))
        index+=len(p["vertices"])
    obj.write_text("\n".join(lines)+"\n",encoding="utf-8")
    mats=["# M05-013 PBR emission in GLB/FBX; MTL approximate preview"]
    for name,(rgba,metal,rough,emission,strength) in MATERIALS.items():
        mats.extend(("newmtl "+name,"Kd %.6f %.6f %.6f"%rgba[:3],
                     "Ks %.6f %.6f %.6f"%(metal,metal,metal),
                     "Ns %.2f"%((1-rough)*1000),
                     "Ke %.6f %.6f %.6f"%tuple(
                         min(1,x*strength) for x in emission),
                     "d %.4f"%rgba[3],"illum 2",""))
    mtl.write_text("\n".join(mats)+"\n",encoding="utf-8")
    return [str(obj),str(mtl)]

def blender_export(parts,out,repeat_check=False):
    import bpy
    from mathutils import Vector
    cname="M05_013_REPAIR_MANIPULATOR"
    if repeat_check and not bpy.data.objects.get("M05_013_FOREIGN_QA"):
        # Foreign marker must survive repeated calls, but is NOT exported.
        mesh=bpy.data.meshes.new("M05_013_FOREIGN_QA_MESH")
        marker=bpy.data.objects.new("M05_013_FOREIGN_QA",mesh)
        bpy.context.scene.collection.objects.link(marker)
    def construct():
        previous=bpy.data.collections.get(cname)
        if previous:
            for ob in list(previous.objects):
                mesh=ob.data if ob.type=="MESH" else None
                bpy.data.objects.remove(ob,do_unlink=True)
                if mesh and mesh.users==0:
                    bpy.data.meshes.remove(mesh)
            bpy.data.collections.remove(previous)
        for action in list(bpy.data.actions):
            if action.name.startswith("M05_013_") and action.users==0:
                bpy.data.actions.remove(action)
        coll=bpy.data.collections.new(cname)
        bpy.context.scene.collection.children.link(coll)
        def empty(name,parent=None,location=(0,0,0)):
            ob=bpy.data.objects.new(name,None)
            coll.objects.link(ob)
            ob.parent=parent
            ob.location=location
            ob.empty_display_size=.055
            return ob
        mount=empty("MOUNT_SIDE_SOCKET")
        shoulder=empty("M05_013_SHOULDER",mount,(SHOULDER_X,0,0))
        elbow=empty("M05_013_ELBOW",shoulder,(UPPER_LENGTH,0,0))
        wrist=empty("M05_013_WRIST",elbow,(FORE_LENGTH,0,0))
        empty("M05_013_SERVICE_TIP",wrist,(.326,0,0))
        empty("M05_013_POWER_PORT",mount,(.115,-.102,-.135))
        empty("M05_013_MOUNT_BOLT_UPPER",mount,(0,.145,.134))
        empty("M05_013_MOUNT_BOLT_LOWER",mount,(0,-.145,-.134))
        parents={"BASE":mount,"SHOULDER":shoulder,
                 "ELBOW":elbow,"WRIST":wrist}
        mats={}
        for name,(rgba,metal,rough,emission,strength) in MATERIALS.items():
            key="M05_013_"+name
            mat=bpy.data.materials.get(key) or bpy.data.materials.new(key)
            mat.use_nodes=True
            bsdf=mat.node_tree.nodes.get("Principled BSDF")
            if bsdf:
                bsdf.inputs["Base Color"].default_value=rgba
                bsdf.inputs["Metallic"].default_value=metal
                bsdf.inputs["Roughness"].default_value=rough
                if "Emission Color" in bsdf.inputs:
                    bsdf.inputs["Emission Color"].default_value=(*emission,1)
                if "Emission Strength" in bsdf.inputs:
                    bsdf.inputs["Emission Strength"].default_value=strength
                elif "Emission" in bsdf.inputs:
                    bsdf.inputs["Emission"].default_value=(*emission,1)
            mat.diffuse_color=rgba
            mats[name]=mat
        for p in parts:
            mesh=bpy.data.meshes.new("M05_013_"+p["name"])
            mesh.from_pydata(p["vertices"],[],p["faces"])
            mesh.update(calc_edges=True)
            ob=bpy.data.objects.new("M05_013_"+p["name"],mesh)
            coll.objects.link(ob)
            ob.parent=parents[p["group"]]
            mesh.materials.append(mats[p["material"]])
            for face in mesh.polygons:
                # Curved side walls are smooth; both end caps remain flat.
                # tube() emits [side, side, lower cap, upper cap] per ring.
                face.use_smooth=p["smooth"] and face.index % 4 in (0,1)
        for frame,angles in KEYS:
            for ob,deg in zip((shoulder,elbow,wrist),angles):
                ob.rotation_euler[2]=math.radians(deg)
                ob.keyframe_insert(data_path="rotation_euler",index=2,
                                   frame=frame,group="M05_013_JOINTS")
        for ob in (shoulder,elbow,wrist):
            action=ob.animation_data.action
            action.name="M05_013_"+ob.name+"_Action"
            try:
                for fc in action.fcurves:
                    for key in fc.keyframe_points:
                        key.interpolation="LINEAR"
            except AttributeError:
                pass  # Blender 4.4+ layered actions; sampled QA still runs.
        scene=bpy.context.scene
        scene.frame_start=1
        scene.frame_end=36
        scene.render.fps=FPS
        scene.frame_set(1)
        return coll,parents
    coll,parents=construct()
    if repeat_check:
        before=(len(coll.objects),
                sum(ob.type=="MESH" for ob in coll.objects),
                len([m for m in bpy.data.materials
                     if m.name.startswith("M05_013_")]))
        coll,parents=construct()
        after=(len(coll.objects),
               sum(ob.type=="MESH" for ob in coll.objects),
               len([m for m in bpy.data.materials
                    if m.name.startswith("M05_013_")]))
        assert before==after,(before,after)
        assert bpy.data.objects.get("M05_013_FOREIGN_QA")
    assert sum(ob.type=="MESH" for ob in coll.objects)==len(parts)
    assert len(coll.objects)==len(parts)+8
    scene=bpy.context.scene
    for frame in range(1,37):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        for label,ob in (("SHOULDER",parents["SHOULDER"]),
                         ("ELBOW",parents["ELBOW"]),
                         ("WRIST",parents["WRIST"])):
            angle=joint_angles(frame)[("SHOULDER","ELBOW","WRIST").index(label)]
            assert abs(ob.rotation_euler.z-angle)<1e-5,(label,frame)
        # Measure actual Blender-evaluated tool location, not just script keys.
        tip=bpy.data.objects["M05_013_SERVICE_TIP"]
        xyz=tip.matrix_world.translation
        (s,e,w),(a,b,c)=poses(frame)
        expected=plus2(w,rotate_xy((.326,0),c))
        assert abs(xyz.x-expected[0])<1e-5,(frame,"tip x")
        assert abs(xyz.y-expected[1])<1e-5,(frame,"tip y")
    scene.frame_set(1)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in coll.objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=parents["BASE"]
    fbx=out/"M05-013_repair_manipulator.fbx"
    glb=out/"M05-013_repair_manipulator.glb"
    blend=out/"M05-013_repair_manipulator.blend"
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,
        axis_forward="-Z",axis_up="Y",add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format="GLB",
        use_selection=True,export_animations=True)
    # Foreign marker is in scene, but export selection excludes it.
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    for path in (fbx,glb,blend):
        assert path.exists() and path.stat().st_size>1000,path
    return {"backend":"bpy","blender_export":"PASS",
            "fbx":str(fbx),"glb":str(glb),"blend":str(blend),
            "blender_animated_frames_verified":36,
            "blender_repeat_build":"PASS" if repeat_check else "NOT RUN",
            "blender_foreign_objects_preserved":"PASS" if repeat_check else "NOT RUN"}

def main():
    argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else sys.argv[1:]
    parser=argparse.ArgumentParser()
    parser.add_argument("--out",default="build/M05-013")
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--repeat-check",action="store_true")
    args=parser.parse_args(argv)
    parts=build()
    counts,groups=check_mesh(parts)
    qa=check_kinematics()
    report={"asset_id":ASSET_ID,"model":"Moduł naprawczy z manipulatorem",
            **counts,"group_counts":groups,"material_names":list(MATERIALS),
            "mount":"MOUNT_SIDE_SOCKET at (0,0,0), X outward, Y up",
            "joint_pivots_m":{"shoulder":[.27,0,0],"elbow":[.65,0,0],
                              "wrist":[.99,0,0]},
            "animation":{"fps":FPS,"frame_start":1,"frame_end":36,
                         "joint_axes":"local Z (3 joints)",
                         "keys":[[f,list(a)] for f,a in KEYS]},
            "mesh_manifold_oriented":"PASS",
            **qa,"unity_import":"NOT RUN","unity_editmode":"NOT RUN",
            "unity_playmode":"NOT RUN","visual_review":"NOT RUN",
            "blender_export":"NOT RUN"}
    if args.self_test:
        print(json.dumps(report,indent=2,ensure_ascii=False))
        return
    out=Path(args.out)
    out.mkdir(parents=True,exist_ok=True)
    report["static_files"]=static_obj(parts,out)
    try:
        import bpy
    except ImportError:
        report["backend"]="plain-python-OBJ"
    else:
        report.update(blender_export(parts,out,args.repeat_check))
    (out/"M05-013_manifest.json").write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(report,indent=2,ensure_ascii=False))
if __name__=="__main__":
    main()
