#!/usr/bin/env python3
"""B01-31 W23 independent read-only real GLB source -> OBJ/MTL geometry parity.
Does not import W23 author. Physically parses all original GLB and all new OBJ
vertices/vertex normals/faces with exact FLOAT32 roundtrip; material groups,
pivots, original receipt, physical ZIP 8 entries, 8 corrupt mutation rejections.
Unity Editor/G1 NOT_RUN until legally activated operator testing.
"""
from __future__ import annotations
import argparse,copy,hashlib,io,json,math,struct,sys,zipfile
from pathlib import Path
from PIL import Image

ID="B01-31-W23-ARMOR-OBJ-IMPORT-BRIDGE-001";VER="P17-W23-R1"
BASE="B0131ArmorModule1x2x2"
STATES=("OK","DAMAGED","DESTROYED")
ORIG_SHA={"OK":"a063ef3d8750ef7e65bd9f7ba57925e68f112d4f51e78f2048fe6552fb33fdfd",
"DAMAGED":"9f710d0b88d7b6b4df1898c2c26b488987e89a83a382f0ad85df6af1ee16c8fe",
"DESTROYED":"f7c994a3bd80dbf6c24655d06f26f13f95ba5e6d65b6590c5795dfc71243c20f"}
COUNTS={"OK":204,"DAMAGED":264,"DESTROYED":288}
MATS=("FrameGraphite","ArmorTitanium","SafetyAmber","CopperInternals","SealRubber","StatusCyan")
MTL=BASE+"-W23-shared.mtl"
def sha(x):return hashlib.sha256(x).hexdigest()
def need(x,s):
    if not x:raise ValueError("B01-31-W23-INDEPENDENT-OBJ_RED "+s)
def u32f(x):return struct.pack("<f",float(x))
def same3(a,b):return all(u32f(a[i])==u32f(b[i]) for i in range(3))
def readglb(blob):
    need(len(blob)>28 and blob[:4]==b"glTF","actual physical original GLB magic")
    ver,total=struct.unpack_from("<II",blob,4)
    need(ver==2 and total==len(blob),"physical glTF2 length")
    js,typ=struct.unpack_from("<I4s",blob,12)
    need(typ==b"JSON" and js%4==0 and js+28<=len(blob),"actual GLB JSON bounds")
    d=json.loads(blob[20:20+js])
    sz,typ=struct.unpack_from("<I4s",blob,20+js)
    need(typ==b"BIN\0" and sz+28+js==len(blob),"actual GLB original BIN")
    decl=d["buffers"][0]["byteLength"]
    need(sz-decl in (0,1,2,3),"original real 3D bytes padding")
    return d,blob[28+js:28+js+decl]
def accessor(doc,bin,which,want):
    a=doc["accessors"][which];v=doc["bufferViews"][a["bufferView"]]
    need(a["componentType"]==want and a["type"] in ("VEC3","SCALAR"),
         "physical vertex float or original triangle uint16")
    count=a["count"]*(3 if a["type"]=="VEC3" else 1)
    fmt="f" if want==5126 else "H";width=4 if fmt=="f" else 2
    need(v["byteLength"]==count*width and v["byteOffset"]+v["byteLength"]<=len(bin),
         "physical original geometry byte size")
    vals=struct.unpack_from("<"+fmt*count,bin,v["byteOffset"])
    need(all(math.isfinite(float(z)) for z in vals),"no original NaN mesh")
    return vals
def source(source,state):
    raw=(source/(BASE+"-"+state+".glb")).read_bytes()
    need(sha(raw)==ORIG_SHA[state],"immutable W22 physically exact original geometry SHA")
    doc,buf=readglb(raw)
    need(doc["asset"]["generator"]=="B01-31-MODULAR-ARMOR-3GEOMETRY-STATES-001/P17-W22-R1"
         and doc["asset"]["extras"]["damage_visual_state"]==state
         and doc["nodes"][0]["translation"]==[0,0,0]
         and doc["nodes"][0]["rotation"]==[0,0,0,1]
         and [x["name"] for x in doc["materials"]]==list(MATS),
         "original W22 3D identity/material/pivot")
    triangles=[]
    for p in doc["meshes"][0]["primitives"]:
        mat=p["material"]
        vertices=accessor(doc,buf,p["attributes"]["POSITION"],5126)
        normals=accessor(doc,buf,p["attributes"]["NORMAL"],5126)
        indices=accessor(doc,buf,p["indices"],5123)
        need(len(indices)==len(vertices)//3==len(normals)//3,"original source triangle vertex buffers")
        for i in range(0,len(indices),3):
            corners=[(tuple(vertices[3*idx+k] for k in range(3)),
                      tuple(normals[3*idx+k] for k in range(3))) for idx in indices[i:i+3]]
            triangles.append((MATS[mat],corners))
    need(len(triangles)==COUNTS[state],"original immutable actual triangle count")
    return doc,triangles
def parse_obj(raw,state):
    need(raw.endswith(b"\n") and b"\x00" not in raw,"OBJ physical UTF8 and no control bytes")
    text=raw.decode("utf-8")
    vertices=[];normals=[];faces=[]
    name=None;currentmat=None;mtllib=None;nsmooth=None
    groups=[]
    for line in text.splitlines():
        line=line.strip()
        if not line or line.startswith("#"):continue
        parts=line.split();tag=parts[0]
        if tag=="mtllib":
            need(len(parts)==2 and mtllib is None and parts[1]==MTL,
                 "only relative actual W23 shared MTL path, never external path")
            mtllib=parts[1]
        elif tag=="o":
            need(len(parts)==2 and name is None,"one original model root")
            name=parts[1]
        elif tag=="s":
            need(parts==["s","off"],"original per-face normals must remain flat")
            nsmooth="off"
        elif tag=="g":
            need(len(parts)==2 and parts[1].startswith(state+"_"),"original source material group")
            groups.append(parts[1])
        elif tag=="usemtl":
            need(len(parts)==2 and parts[1] in MATS,
                 "original glTF material identity, no alternate/recolor")
            currentmat=parts[1]
        elif tag in ("v","vn"):
            need(len(parts)==4,"actual 3 vertex float components")
            xyz=tuple(float(p) for p in parts[1:])
            need(all(math.isfinite(v) for v in xyz),"no NaN, infinity or unsafe source")
            if tag=="v":vertices.append(xyz)
            else:normals.append(xyz)
        elif tag=="f":
            need(len(parts)==4 and currentmat is not None and mtllib==MTL,
                 "physical OBJ exactly three anchored materialized triangle corners")
            corner=[]
            for token in parts[1:]:
                pair=token.split("//")
                need(len(pair)==2 and pair[0].isdigit() and pair[1].isdigit(),
                     "actual face v//vn format, no UV invented")
                vi,ni=map(int,pair)
                need(0<vi<=len(vertices) and 0<ni<=len(normals)
                     and vi==ni,"unique original per-corner normal-to-vertex identity")
                corner.append((vertices[vi-1],normals[ni-1]))
            faces.append((currentmat,corner))
        else:raise ValueError("unexpected OBJ token "+tag)
    need(name==BASE+"-"+state and mtllib==MTL and nsmooth=="off",
         "original same-root, relative MTL, explicit per-face smoothing")
    need(len(faces)==COUNTS[state] and
         len(vertices)==len(normals)==COUNTS[state]*3,
         "genuine original triangle+vertex+normal count preserved")
    need(len(groups)>=4,"original source material groups kept separately")
    return faces
def parity(original,output,state):
    faces=parse_obj(output,state)
    need(len(original)==len(faces),"source face count parity")
    for index,(src,dst) in enumerate(zip(original,faces)):
        need(src[0]==dst[0],"real source material ID/per-face assignment "+str(index))
        for k in range(3):
            need(same3(src[1][k][0],dst[1][k][0]),
                 "exact original glTF FLOAT32 vertex position lost "+str(index))
            need(same3(src[1][k][1],dst[1][k][1]),
                 "exact original glTF FLOAT32 normal direction lost "+str(index))
    return len(faces)
def mtlcheck(raw,glbmaterials):
    txt=raw.decode("utf-8")
    need(txt.endswith("\n") and "\x00" not in txt and
         txt.count("newmtl ")==6,"actual one simple direct-OBJ MTL with six materials")
    blocks={}
    current=None
    for line in txt.splitlines():
        if not line or line.startswith("#"):continue
        parts=line.split()
        if parts[0]=="newmtl":
            need(len(parts)==2 and parts[1] in MATS and parts[1] not in blocks,
                 "exact original W22 names no aliases")
            current=parts[1];blocks[current]={}
        else:
            need(current is not None and parts[0] in ("Ka","Kd","Ks","Ns","d","illum"),
                 "only supported OBJ standard nonextended MTL, no path or exec")
            need(parts[0] not in blocks[current],"one material property each")
            blocks[current][parts[0]]=[float(x) for x in parts[1:]]
    need(set(blocks)==set(MATS),"exact six source-defined MTLs")
    for m in glbmaterials:
        b=blocks[m["name"]];source=m["pbrMetallicRoughness"]
        expect=source["baseColorFactor"][:3]
        need(len(b["Kd"])==3 and all(abs(b["Kd"][k]-expect[k])<2e-8 for k in range(3))
             and len(b["d"])==1 and abs(b["d"][0]-source["baseColorFactor"][3])<1e-8
             and len(b["Ks"])==3 and len(b["Ns"])==1 and
             b["illum"]==[2.],
             "original source material baseColor and valid direct MTL")
    return True
def negatives(original,good,state):
    text=good.decode("utf-8")
    cases=[
        text.replace("v ","v 99 ",1),
        text.replace("vn ","vn 99 ",1),
        text.replace("mtllib "+MTL,"mtllib ../foreign.mtl",1),
        text.replace("o "+BASE+"-"+state,"o foreign-model",1),
        text.replace("usemtl ArmorTitanium","usemtl SafetyAmber",1),
        text.replace("f ","f 1//1 1//1 1//1 ",1)]
    bad=0
    for test in cases:
        try:parity(original,test.encode(),state)
        except (ValueError,IndexError,KeyError,struct.error):bad+=1
        else:raise AssertionError("FALSE GREEN corrupted OBJ material/normal/position")
    need(bad==6,"6 invalid OBJ inputs rejected")
    return bad
def run(sourcepath,out):
    physical=json.loads((sourcepath/"receipt.json").read_text())
    need(physical["package_id"]=="B01-31-MODULAR-ARMOR-3GEOMETRY-STATES-001"
         and physical["state_counts"]==COUNTS
         and all(any(f["name"]==BASE+"-"+s+".glb" and f["sha256"]==ORIG_SHA[s]
                     for f in physical["actual_files"]) for s in STATES),
         "exact original W22 receipt per state SHA")
    machine=json.loads((out/"receipt.json").read_text())
    pbr=json.loads((out/"original-pbr-materials.json").read_text())
    need(machine["package_id"]==pbr["package_id"]==ID
         and machine["version"]==pbr["version"]==VER
         and machine["source_original_W22_model_SHA256"]==ORIG_SHA
         and pbr["source_exact_W22_sha256"]==ORIG_SHA
         and machine["physical_new_OBJ_geometry_states"]==3
         and machine["real_distribution_zip_members"]==8,
         "W23 actual source provenance and receipt")
    need(machine["native_Unity_G1"]=="NOT_RUN" and
         machine["SaveLoad"]=="NOT_RUN" and machine["G2"]=="HOLD"
         and machine["formal_G0"]=="PENDING" and
         machine["integrated"] is False and
         machine["frozen_510"]=="d94400d95d13db7f27e1533df05c9292d5380431"
         and machine["frozen_556"]=="33094f154c7b91119b36d58aab285e6e83c5de57"
         and machine["frozen_596"]=="efb2b06eafedab5469b57f163152d96aca3c5915",
         "OBJ import bridge must not falsely certify real Unity host or Save")
    original={};count={};modelmaterials=[]
    for state in STATES:
        model,source_triangles=source(sourcepath,state)
        modelmaterials.append(model["materials"])
        obj=(out/(BASE+"-"+state+"-W23.obj")).read_bytes()
        count[state]=parity(source_triangles,obj,state)
        original[state]=(source_triangles,obj)
    need(all(m==modelmaterials[0] for m in modelmaterials),
         "same six source original glTF material definitions for all states")
    need(pbr["original_glTF_materials"]==modelmaterials[0]
         and pbr["grid_illustrative_xyz"]==[1,2,2]
         and pbr["original_common_pivot_xyz"]==[0,0,0]
         and pbr["original_gltf_triangles"]==COUNTS,
         "original PBR sidecar exact six original material physics-free identity")
    mtlcheck((out/MTL).read_bytes(),modelmaterials[0])
    names={BASE+"-"+s+"-W23.obj" for s in STATES}
    names|={MTL,"original-pbr-materials.json","receipt.json"}
    names|={BASE+"-W23-"+camera+"-three-states.png" for camera in ("iso","face")}
    listed={r["name"]:r for r in machine["files"]}
    need(len(names)==8 and set(listed)==names-{"receipt.json"},
         "original seven actual files + receipt binary manifests")
    for name,entry in listed.items():
        payload=(out/name).read_bytes()
        need(sha(payload)==entry["sha256"] and len(payload)==entry["bytes"],
             "actual complete physical OBJ/MTL/PNG original byte receipt")
        if name.endswith(".png"):
            with Image.open(io.BytesIO(payload)) as im:
                im.load()
                need(im.format=="PNG" and im.mode=="RGB" and im.size==(1800,625),
                     "physical original source OBJ projected 1800x625 art board")
                crops=[sha(im.crop((i*600+80,90,i*600+520,560)).tobytes()) for i in range(3)]
                need(len(set(crops))==3,"three actually distinct original source OBJ geometries visible")
    need((out/(BASE+"-W23-iso-three-states.png")).read_bytes() !=
         (out/(BASE+"-W23-face-three-states.png")).read_bytes(),
         "two independent real 3D OBJ camera projections")
    physicalzip=out.parent/(out.name+"-physical.zip")
    with zipfile.ZipFile(physicalzip) as z:
        need(z.testzip() is None and set(z.namelist())==names and
             all(z.read(n)==(out/n).read_bytes() for n in names),
             "exact eight-member actual source ZIP CRC+binary parity")
    failed=negatives(*original["OK"],"OK")
    src=(sourcepath/(BASE+"-DAMAGED.glb")).read_bytes()
    for corrupted in (b"NOPE"+src[4:],src[:8]+struct.pack("<I",len(src)+1)+src[12:]):
        try:readglb(corrupted)
        except (ValueError,struct.error,json.JSONDecodeError):failed+=1
        else:raise AssertionError("FALSE GREEN original bad GLB binary header")
    need(failed==8,"eight original corrupt OBJ+GLB negative controls denied")
    print(json.dumps({"status":"PASS","actual_original_GLTF_to_OBJ_complete_triangle_parity":count,
      "source_exact_three_GLB_SHA256":ORIG_SHA,
      "all_triangle_normals_float32_bitwise_identical":True,
      "real_six_material_OBJ_MTL_assignments":True,
      "two_physically_decoded_3state_RGB_boards":2,
      "real_8member_ZIP_CRC":True,
      "negative_corrupt_inputs_rejected":failed,
      "unity_modelimporter_native":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True);p.add_argument("--output",required=True)
    args=p.parse_args()
    try:run(Path(args.original),Path(args.output))
    except Exception as e:
        print("W23_INDEPENDENT_GLTF_TO_OBJ_RECEIVER_RED "+type(e).__name__+": "+str(e),file=sys.stderr);raise
