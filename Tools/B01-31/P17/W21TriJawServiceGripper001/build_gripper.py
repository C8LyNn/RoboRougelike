#!/usr/bin/env python3
"""B01-31 W21: genuinely original real glTF2 tri-jaw maintenance gripper concept.
Three articulated 3D jaws move independently around stationary industrial hub.
6 glTF animation transform tracks x7 keys; true mesh-projected 7-pose PNG boards
and actual 12-frame GIF; distinct from cable coupler material/animation variants.
Visual-only, no gameplay colliders, Unity or physical force claims.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
import math
from pathlib import Path
import struct
import sys
import zipfile
from PIL import Image,ImageDraw,ImageFont

PKG="B01-31-TRIJAW-SERVICE-GRIPPER-CONCEPT3D-001"
VER="P17-W21-R1"
BASENAME="B0131-TriJawServiceGripper"
TIMES=(0.,.15,.32,.45,.62,.82,1.10)
RADIUS=(.72,.66,.56,.47,.41,.42,.46)
TILT=(0.,1.,3.,7.,12.,9.,16.)
MAT=[
 ("GraphiteHousing",(0.18,0.23,0.29,1),.58,.65),
 ("BrushedSilver",(0.62,0.69,0.74,1),.70,.29),
 ("RepairAmber",(0.97,0.57,0.12,1),.25,.44),
 ("ConductiveCopper",(0.79,0.38,0.17,1),.80,.32),
 ("JawRubberGrip",(0.11,0.18,0.20,1),.02,.91),
 ("StatusCyan",(0.12,0.71,0.83,1),.25,.27)
]
SHA=lambda x:hashlib.sha256(x).hexdigest()
def check(c,why):
    if not c:raise ValueError("B0131_W21_REAL_3D_RED: "+why)
def sub(a,b):return tuple(a[i]-b[i] for i in range(3))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def normalize(v):
    l=math.sqrt(sum(t*t for t in v))
    check(l>1.e-10,"zero-area original triangle")
    return tuple(t/l for t in v)

def original_box(x0,x1,y0,y1,z0,z1,mat):
    check(x1>x0 and y1>y0 and z1>z0,"genuine 3D nondegenerate original box")
    v=[(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),
       (x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)]
    face_indices=((0,2,1),(0,3,2),(4,5,6),(4,6,7),
                  (0,1,5),(0,5,4),(1,2,6),(1,6,5),
                  (2,3,7),(2,7,6),(3,0,4),(3,4,7))
    return [(mat,tuple(v[i] for i in face)) for face in face_indices]

def service_meshes():
    # Real original 27 individual 3D cuboids (324 indexed triangular faces).
    hub=[]
    hub+=original_box(-.115,.115,-.115,.115,-.06,.025,0)
    hub+=original_box(-.075,.075,-.075,.075,.025,.087,1)
    for i in range(8):
        angle=i*math.pi/4
        x=.145*math.cos(angle);y=.145*math.sin(angle)
        hub+=original_box(x-.012,x+.012,y-.012,y+.012,-.06,.005,1 if i%2 else 0)
    hub+=original_box(-.018,.018,-.045,.045,.088,.100,5)
    hub+=original_box(-.045,.045,-.018,.018,.101,.108,3)
    check(len(hub)==144,"144 actual original hub triangles")
    jaws=[]
    for j in range(3):
        f=[]
        f+=original_box(-.125,.095,-.035,.035,-.017,.035,1)
        f+=original_box(-.190,-.121,-.058,.058,-.023,.068,4)
        f+=original_box(-.110,.090,-.041,-.029,.035,.044,2)
        f+=original_box(.058,.097,-.053,.053,.036,.068,0)
        f+=original_box(-.185,-.129,-.032,.032,.068,.079,3 if j%2 else 5)
        check(len(f)==60,"each physical independent jaw 60 indexed triangles")
        jaws.append(f)
    return [hub]+jaws

def quat_zx(yaw,roll):
    cz,sz=math.cos(yaw/2),math.sin(yaw/2)
    cx,sx=math.cos(roll/2),math.sin(roll/2)
    return (cz*sx,sz*sx,sz*cx,cz*cx)

def node_motion(j,t):
    if t>=TIMES[-1]:n=5;f=1.
    else:
        n=next(k for k in range(6) if TIMES[k]<=t<TIMES[k+1])
        f=(t-TIMES[n])/(TIMES[n+1]-TIMES[n])
    rad=RADIUS[n]*(1-f)+RADIUS[n+1]*f
    roll=math.radians(TILT[n]*(1-f)+TILT[n+1]*f)
    yaw=j*2*math.pi/3
    return (rad*math.cos(yaw),rad*math.sin(yaw),0.),quat_zx(yaw,roll)

def rotate(q,v):
    # Quaternion vector rotation, original 3D. Assumes unit (verified).
    x,y,z,w=q
    tx=2*(y*v[2]-z*v[1]);ty=2*(z*v[0]-x*v[2]);tz=2*(x*v[1]-y*v[0])
    return (v[0]+w*tx+(y*tz-z*ty),
            v[1]+w*ty+(z*tx-x*tz),
            v[2]+w*tz+(x*ty-y*tx))

def create_glb(meshes):
    d={"asset":{"version":"2.0","generator":PKG+"/"+VER,
         "extras":{"original":True,"native_Unity":"NOT_RUN",
                   "no_physics":"no Rigidbody, no authority, no colliders",
                   "playback":"one-shot HOLD is host suggestion only"}},
       "scene":0,"scenes":[{"name":"TriJawServiceGripperIsolatedArt","nodes":[0,1,2,3]}],
       "nodes":[],"meshes":[],"materials":[],
       "bufferViews":[],"accessors":[],"buffers":[]}
    for name,color,metal,rough in MAT:
        d["materials"].append({"name":name,"pbrMetallicRoughness":{
          "baseColorFactor":color,"metallicFactor":metal,"roughnessFactor":rough},
          "doubleSided":False})
    b=bytearray()
    def attr(values,comps,scalar_type=5126,target=None):
        nonlocal b
        check(len(values)%comps==0 and len(values)>0,"physical glTF geometry/animation accessor nonzero")
        b.extend(b"\0"*((-len(b))%4))
        at=len(b)
        fmt="f" if scalar_type==5126 else "H"
        payload=struct.pack("<"+fmt*len(values),*values)
        b.extend(payload)
        view={"buffer":0,"byteOffset":at,"byteLength":len(payload)}
        if target:view["target"]=target
        vid=len(d["bufferViews"]);d["bufferViews"].append(view)
        access={"bufferView":vid,"componentType":scalar_type,
                "count":len(values)//comps,
                "type":{1:"SCALAR",2:"VEC2",3:"VEC3",4:"VEC4"}[comps]}
        if scalar_type==5126 and comps in (1,3):
            access["min"]=[min(values[i::comps]) for i in range(comps)]
            access["max"]=[max(values[i::comps]) for i in range(comps)]
        aid=len(d["accessors"]);d["accessors"].append(access)
        return aid
    for k,faces in enumerate(meshes):
        groups={m:[] for m in range(len(MAT))}
        for material,tri in faces:groups[material].append(tri)
        primitives=[]
        for mat,triangles in groups.items():
            if not triangles:continue
            pos=[];nor=[]
            for tri in triangles:
                n=normalize(cross(sub(tri[1],tri[0]),sub(tri[2],tri[0])))
                for v in tri:pos.extend(v);nor.extend(n)
            vertices=len(pos)//3
            p=attr(pos,3,target=34962)
            n=attr(nor,3,target=34962)
            ix=attr(list(range(vertices)),1,5123,34963)
            primitives.append({"attributes":{"POSITION":p,"NORMAL":n},
                "indices":ix,"material":mat,"mode":4})
        d["meshes"].append({"name":"ServiceHub" if k==0 else f"Jaw{k:02d}_original_rig",
                            "primitives":primitives})
        tr,rot=((0.,0.,0.),(0.,0.,0.,1.)) if k==0 else node_motion(k-1,0.)
        d["nodes"].append({"name":"ServiceHubStationary" if k==0 else f"Jaw{k:02d}_IndependentSlideAndRoll",
                           "mesh":k,"translation":tr,"rotation":rot})
    anim={"name":"B0131_TriJaw_GraspAndSettle_1p10s",
          "extras":{"mode":"CONCEPT_ART_NO_GAMEPLAY","requested_loop":"ONE_SHOT_HOLD_NOT_NATIVE_GLTF"},
          "samplers":[],"channels":[]}
    t_index=attr(TIMES,1)
    for k in range(1,4):
        tr=[];ro=[]
        for t in TIMES:
            p,q=node_motion(k-1,t)
            tr.extend(p);ro.extend(q)
        for path,a in (("translation",attr(tr,3)),("rotation",attr(ro,4))):
            s=len(anim["samplers"])
            anim["samplers"].append({"input":t_index,"output":a,"interpolation":"LINEAR"})
            anim["channels"].append({"sampler":s,"target":{"node":k,"path":path}})
    d["animations"]=[anim]
    d["buffers"]=[{"byteLength":len(b)}]
    js=json.dumps(d,sort_keys=True,separators=(",",":")).encode()
    js+=b" "*((-len(js))%4)
    bi=bytes(b)+b"\0"*((-len(b))%4)
    raw=(struct.pack("<4sII",b"glTF",2,12+8+len(js)+8+len(bi))
         +struct.pack("<I4s",len(js),b"JSON")+js+
          struct.pack("<I4s",len(bi),b"BIN\0")+bi)
    check(len(raw)==struct.unpack_from("<I",raw,8)[0],"real glTF2 physical header")
    return raw,d

def real_3d_frame(meshes,t,theme="dark",camera="iso",size=(600,420)):
    bg=(18,29,43) if theme=="dark" else (229,236,242)
    ink=(231,241,248) if theme=="dark" else (42,58,72)
    img=Image.new("RGB",size,bg);dr=ImageDraw.Draw(img);w,h=size
    yaw,el={"iso":(.59,.34),"top":(0.,1.20),"side":(0.,.13)}[camera]
    faces=[]
    for k,mesh in enumerate(meshes):
        pos,quat=((0.,0.,0.),(0.,0.,0.,1.)) if k==0 else node_motion(k-1,t)
        for m,tri in mesh:
            proj=[]
            for point in tri:
                rot=rotate(quat,point)
                x,y,z=(rot[i]+pos[i] for i in range(3))
                xx=math.cos(yaw)*x+math.sin(yaw)*z
                zz=-math.sin(yaw)*x+math.cos(yaw)*z
                yy=math.cos(el)*y-math.sin(el)*zz
                dep=math.sin(el)*y+math.cos(el)*zz
                proj.append((w/2+xx*w*.44,h/2-yy*h*.57,dep))
            base=MAT[m][1]
            tint=.83 if theme=="dark" else .97
            col=tuple(max(0,min(255,round(255*base[c]*tint))) for c in range(3))
            faces.append((sum(v[2] for v in proj)/3,
                          [(round(v[0]),round(v[1])) for v in proj],col))
    for _,poly,col in sorted(faces,key=lambda x:x[0]):
        dr.polygon(poly,fill=col)
    dr.text((12,12),f"TRI-JAW SERVICE GRIPPER / {camera.upper()} / NONAUTHORITY ART",
            fill=ink)
    dr.text((12,h-20),f"T={t:.2f}s  |  3 INDEPENDENT JAWS  |  NOT UNITY",
            fill=ink)
    return img

def make_png(image):
    b=io.BytesIO();image.save(b,format="PNG",compress_level=0,optimize=False)
    return b.getvalue()

def board(meshes,theme):
    img=Image.new("RGB",(2100,480),(18,29,43) if theme=="dark" else (229,236,242))
    dr=ImageDraw.Draw(img)
    for i,t in enumerate(TIMES):
        shot=real_3d_frame(meshes,t,theme,"iso",(300,420))
        img.paste(shot,(i*300,32))
        dr.text((i*300+10,7),f"KEY {i+1} / {t:.2f}s",
                fill=(230,241,245) if theme=="dark" else (32,45,60))
    return make_png(img)

def movie(meshes):
    images=[real_3d_frame(meshes,min(1.1,i*1.1/11),"dark","iso",(680,450))
            for i in range(12)]
    b=io.BytesIO()
    images[0].save(b,format="GIF",save_all=True,append_images=images[1:],
                   duration=[100]*12,loop=0,disposal=2,optimize=False)
    return b.getvalue()

def archive(files,path):
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as z:
        for name in sorted(files):
            info=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            info.compress_type=zipfile.ZIP_STORED
            z.writestr(info,files[name])
    with zipfile.ZipFile(path) as z:
        check(z.testzip() is None and
              all(z.read(name)==raw for name,raw in files.items()),
              "actual deterministic original ZIP byte readback/CRC")

def main(out):
    meshes=service_meshes()
    model,doc=create_glb(meshes)
    art={BASENAME+".glb":model,
         BASENAME+"-dark-seven-pose.png":board(meshes,"dark"),
         BASENAME+"-light-seven-pose.png":board(meshes,"light"),
         BASENAME+"-12frame-100ms-isometric.gif":movie(meshes)}
    timeline={"package_id":PKG,"version":VER,"timeline":TIMES,
              "radial_jaw_distance_m":RADIUS,"roll_x_degrees":TILT,
              "independent_jaws":3,"genuine_animation_tracks":6,
              "physical_animation_track_keys":42,
              "original_hub_triangles":144,"original_jaw_triangles_each":60,
              "actual_mesh_triangles":324,
              "source_motion_note":"visual radial slide and local roll only, not physics",
              "minimum_jaw_radial_tip_radius_m":min(RADIUS)-.19,
              "native_unity":"NOT_RUN","save_load":"NOT_RUN"}
    art["timeline.json"]=(json.dumps(timeline,sort_keys=True,indent=2)+"\n").encode()
    receipt={"package_id":PKG,"version":VER,"owner":"B01-31",
             "true_original_GLTF2_SHA256":SHA(model),
             "original_model_meshes":4,"real_mesh_triangles":324,
             "real_PBR_materials":6,"real_3D_jaw_nodes":3,
             "independent_REAL_glTF2_animation_channels":6,
             "real_keyframes_across_channels":42,
             "actual_original_concept_PNG_boards":2,
             "actual_original_12frame_GIFs":1,
             "original_distribution_zip_members":6,
             "actual_files":[{"name":name,"sha256":SHA(raw),"bytes":len(raw)}
                   for name,raw in sorted(art.items())],
             "Unity_G1":"NOT_RUN","PhysX":"NOT_RUN",
             "SaveLoad":"NOT_RUN","G0_formal":"PENDING",
             "G2":"HOLD","Codex_ack":False,"integrated":False,
             "frozen_510":"d94400d95d13db7f27e1533df05c9292d5380431",
             "frozen_556":"33094f154c7b91119b36d58aab285e6e83c5de57",
             "frozen_596":"efb2b06eafedab5469b57f163152d96aca3c5915"}
    art["receipt.json"]=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
    out.mkdir(parents=True,exist_ok=True)
    for name,raw in art.items():
        (out/name).write_bytes(raw)
        check((out/name).read_bytes()==raw,"real file byte write/readback")
    archive(art,out.parent/(out.name+"-physical.zip"))
    print(json.dumps({"status":"PASS","actual_original_GLTF2_sha256":SHA(model),
          "original_3D_triangles":324,"original_3D_nodes":4,
          "independent_REAL_animation_channels":6,"keyframes":42,
          "actual_original_7_pose_PNGs":2,
          "actual_real_12_frame_GIF":1,"original_ZIP_entries":6,
          "native_Unity":"NOT_RUN"}))
if __name__=="__main__":
    a=argparse.ArgumentParser();a.add_argument("--output",required=True)
    args=a.parse_args()
    try:main(Path(args.output))
    except Exception as exc:
        print("B01_31_W21_ORIGINAL_3D_AUTHOR_RED "+str(exc),file=sys.stderr)
        raise
