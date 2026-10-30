#!/usr/bin/env python3
"""B01-31 P17 W23: actual source-immutable GLB2 -> Unity importer-ready OBJ+MTL.
Exports 3 existing independently verified W22 armour geometry states without
re-meshing, repositioning or changing normals/material groups. New direct OBJ
workflow reduces glTF package dependency; genuine Unity G1 STILL NOT RUN.
"""
from __future__ import annotations
import argparse,hashlib,io,json,math,struct,sys,zipfile
from pathlib import Path
from PIL import Image,ImageDraw

ID="B01-31-W23-ARMOR-OBJ-IMPORT-BRIDGE-001"
VER="P17-W23-R1"
PREFIX="B0131ArmorModule1x2x2"
STATES=("OK","DAMAGED","DESTROYED")
HASHES={"OK":"a063ef3d8750ef7e65bd9f7ba57925e68f112d4f51e78f2048fe6552fb33fdfd",
        "DAMAGED":"9f710d0b88d7b6b4df1898c2c26b488987e89a83a382f0ad85df6af1ee16c8fe",
        "DESTROYED":"f7c994a3bd80dbf6c24655d06f26f13f95ba5e6d65b6590c5795dfc71243c20f"}
COUNTS={"OK":204,"DAMAGED":264,"DESTROYED":288}
MATS=("FrameGraphite","ArmorTitanium","SafetyAmber","CopperInternals","SealRubber","StatusCyan")
MTL=PREFIX+"-W23-shared.mtl"
def sha(data):return hashlib.sha256(data).hexdigest()
def need(cond,msg):
    if not cond:raise ValueError("W23_IMMUTABLE_ORIGINAL_OBJ_SOURCE_RED: "+msg)
def glb(raw):
    need(len(raw)>=28 and raw[:4]==b"glTF","original genuine GLB")
    version,total=struct.unpack_from("<II",raw,4)
    need(version==2 and total==len(raw),"glTF2 binary framing")
    jlen,jtyp=struct.unpack_from("<I4s",raw,12)
    need(jtyp==b"JSON" and jlen%4==0 and jlen+28<=len(raw),"GLB JSON chunk")
    doc=json.loads(raw[20:20+jlen])
    blen,btyp=struct.unpack_from("<I4s",raw,20+jlen)
    need(btyp==b"BIN\0" and 28+jlen+blen==len(raw),"GLB BIN chunk")
    length=doc["buffers"][0]["byteLength"]
    need(blen-length in (0,1,2,3),"GLB padding")
    return doc,raw[28+jlen:28+jlen+length]
def floats(doc,buf,aid,expected):
    acc=doc["accessors"][aid];view=doc["bufferViews"][acc["bufferView"]]
    n={"VEC3":3,"SCALAR":1}[acc["type"]]
    need(acc["componentType"]==expected and view["buffer"]==0,"source index/vertex accessor")
    typ="f" if expected==5126 else "H"
    num=acc["count"]*n
    need(view["byteLength"]==num*(4 if typ=="f" else 2)
         and view["byteOffset"]+view["byteLength"]<=len(buf),"source physical accessor bounds")
    return struct.unpack_from("<"+typ*num,buf,view["byteOffset"])
def readoriginal(folder,state):
    path=folder/(PREFIX+"-"+state+".glb")
    need(path.is_file(),"required physical W22 "+state+" GLB missing")
    raw=path.read_bytes();need(sha(raw)==HASHES[state],"frozen W22 GLB exact digest mismatch "+state)
    doc,buf=glb(raw)
    need(doc["asset"]["generator"]=="B01-31-MODULAR-ARMOR-3GEOMETRY-STATES-001/P17-W22-R1"
         and doc["asset"]["extras"]["damage_visual_state"]==state
         and doc["asset"]["extras"]["part_size_grid_xyz"]==[1,2,2],
         "original W22 source lineage and grid reference")
    need(len(doc["nodes"])==len(doc["meshes"])==1
         and doc["nodes"][0]["translation"]==[0,0,0]
         and doc["nodes"][0]["rotation"]==[0,0,0,1],
         "original mount pivot cannot shift")
    need([m["name"] for m in doc["materials"]]==list(MATS),
         "six original material identities must match")
    tris=[]
    for p in doc["meshes"][0]["primitives"]:
        need(p["mode"]==4 and 0<=p["material"]<6,"valid source triangles and material")
        positions=floats(doc,buf,p["attributes"]["POSITION"],5126)
        normals=floats(doc,buf,p["attributes"]["NORMAL"],5126)
        indices=floats(doc,buf,p["indices"],5123)
        need(len(positions)==len(normals)==len(indices)*3,"original W22 explicit triangle vertices")
        for off in range(0,len(indices),3):
            inds=indices[off:off+3]
            vertices=tuple(tuple(positions[j*3+k] for k in range(3)) for j in inds)
            norms=tuple(tuple(normals[j*3+k] for k in range(3)) for j in inds)
            tris.append((p["material"],vertices,norms))
    need(len(tris)==COUNTS[state],"actual original 3D triangle count")
    return doc,tris
def buildmtl(materials):
    rows=["# ORIGINAL W23 importer-friendly OBJ material bridge. NOT physically equivalent to glTF PBR.",
          "# MTL Kd maps source linear baseColor RGB; glTF metallic/roughness in accompanying JSON."]
    for m in materials:
        n=m["name"];p=m["pbrMetallicRoughness"]
        rgb=p["baseColorFactor"][:3];r=p["roughnessFactor"];metal=p["metallicFactor"]
        rows += [f"newmtl {n}", "Ka 0 0 0",
                 "Kd "+" ".join(f"{x:.9g}" for x in rgb),
                 "Ks "+" ".join(f"{(0.04*(1-metal)+rgb[i]*metal):.9g}" for i in range(3)),
                 "Ns "+f"{max(1.0,(1-r)*220):.9g}",
                 "d "+f"{p['baseColorFactor'][3]:.9g}","illum 2",
                 f"# original_gltf_metallicFactor {metal:.9g}",
                 f"# original_gltf_roughnessFactor {r:.9g}",""]
    return ("\n".join(rows)+"\n").encode()
def exportobj(state,tris):
    rows=["# B01-31 W23 EXACT W22 ORIGINAL geometric triangle soup / NO RESCALE",
          "# B01-31 source_art ONLY: no Rigidbody, no collider, no GUID, no live Unity claims.",
          "mtllib "+MTL,"o "+PREFIX+"-"+state,"s off"]
    nextid=1
    for mi,name in enumerate(MATS):
        selection=[item for item in tris if item[0]==mi]
        if not selection:continue
        rows.extend(["g "+state+"_"+name,"usemtl "+name])
        # vertices and normals use identical per-corner indices, no UV or texture placeholder.
        for _,v,n in selection:
            for pt in v:rows.append("v "+" ".join(f"{x:.9g}" for x in pt))
            for vec in n:rows.append("vn "+" ".join(f"{x:.9g}" for x in vec))
            rows.append("f "+" ".join(f"{nextid+k}//{nextid+k}" for k in range(3)))
            nextid+=3
    need(nextid==COUNTS[state]*3+1,"exact physical OBJ triangle count")
    return ("\n".join(rows)+"\n").encode()
def parse_for_preview(raw):
    verts=[];faces=[];material=0
    for l in raw.decode("utf-8").splitlines():
        if l.startswith("v "):
            verts.append(tuple(float(v) for v in l.split()[1:]))
        elif l.startswith("usemtl "):material=MATS.index(l[7:])
        elif l.startswith("f "):
            ids=[int(x.split("//")[0])-1 for x in l[2:].split()]
            faces.append((material,tuple(verts[i] for i in ids)))
    return faces
def render_faces(tris,state,camera):
    w,h=600,580
    image=Image.new("RGB",(w,h),(18,29,43));d=ImageDraw.Draw(image)
    yaw,pitch={"iso":(.65,.47),"face":(1.57,.0)}[camera]
    projected=[]
    for mat,points in tris:
        poly=[];depth=[]
        for x,y,z in points:
            xx=math.cos(yaw)*x+math.sin(yaw)*z
            zz=-math.sin(yaw)*x+math.cos(yaw)*z
            yy=math.cos(pitch)*y-math.sin(pitch)*zz
            dep=math.sin(pitch)*y+math.cos(pitch)*zz
            poly.append((round(w/2+xx*175),round(h/2-yy*185)));depth.append(dep)
        rgb=[(36,52,69),(132,160,182),(230,137,38),(186,91,43),(34,47,53),(29,170,204)][mat]
        projected.append((sum(depth)/3,poly,rgb))
    for _,poly,rgb in sorted(projected,key=lambda t:t[0]):d.polygon(poly,fill=rgb)
    d.text((13,15),state+" / OBJ REAL PHYSICAL 3D / "+camera.upper(),fill=(237,244,250))
    d.text((12,557),"SOURCE EXPORT ONLY - UNITY G1 NOT_RUN",fill=(213,228,241))
    return image
def board(allraw,camera):
    image=Image.new("RGB",(1800,625),(18,29,43))
    d=ImageDraw.Draw(image)
    for i,state in enumerate(STATES):
        tri=parse_for_preview(allraw[state])
        image.paste(render_faces(tri,state,camera),(i*600,30))
        d.text((i*600+12,8),"ORIGINAL 1x2x2 "+state,fill=(235,242,249))
    stream=io.BytesIO();image.save(stream,format="PNG",compress_level=0,optimize=False)
    return stream.getvalue()
def make_zip(files,dest):
    with zipfile.ZipFile(dest,"w",compression=zipfile.ZIP_STORED) as z:
        for name in sorted(files):
            info=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            info.create_system=3;info.external_attr=0o100644<<16
            info.compress_type=zipfile.ZIP_STORED;z.writestr(info,files[name])
    with zipfile.ZipFile(dest) as z:
        need(z.testzip() is None and all(z.read(n)==v for n,v in files.items()),
             "real physical distribution ZIP must byte match source")
def main(source,out):
    receipt=json.loads((source/"receipt.json").read_text())
    need(receipt["package_id"]=="B01-31-MODULAR-ARMOR-3GEOMETRY-STATES-001"
         and receipt["version"]=="P17-W22-R1"
         and receipt["state_counts"]==COUNTS,"actual frozen original W22 physical receipt")
    originals={};files={}
    for state in STATES:
        doc,tris=readoriginal(source,state);originals[state]=doc
        need(receipt["actual_files"] and any(x["name"]==PREFIX+"-"+state+".glb"
             and x["sha256"]==HASHES[state] for x in receipt["actual_files"]),
             "original immutable W22 GLB per-file actual provenance")
        files[PREFIX+"-"+state+"-W23.obj"]=exportobj(state,tris)
    first=originals["OK"]["materials"]
    need(all(doc["materials"]==first for doc in originals.values()),
         "all W22 source states have identical actual PBR material definitions")
    files[MTL]=buildmtl(first)
    description={"package_id":ID,"version":VER,"source_exact_W22_sha256":HASHES,
        "original_glTF_materials":first,"grid_illustrative_xyz":[1,2,2],
        "original_common_pivot_xyz":[0,0,0],"original_gltf_triangles":COUNTS,
        "OBJ_importer":"Unity includes ModelImporter support for OBJ; actual G1 NOT_RUN",
        "MTL_limit":"MTL != glTF PBR, original glTF metallic/roughness factors retained in JSON",
        "semantic":"geometry-only repair/damage look; no state machine or collider authority"}
    files["original-pbr-materials.json"]=(json.dumps(description,indent=2,sort_keys=True)+"\n").encode()
    for cam in ("iso","face"):
        files[PREFIX+"-W23-"+cam+"-three-states.png"]=board({
            s:files[PREFIX+"-"+s+"-W23.obj"] for s in STATES},cam)
    machine={"package_id":ID,"version":VER,"owner":"B01-31",
             "source_original_W22_model_SHA256":HASHES,"source_original_W22_triangles":COUNTS,
             "physical_new_OBJ_geometry_states":3,"physical_shared_MTL_files":1,
             "original_PBR_sidecar_JSON":1,"actual_1800x625_png_boards":2,
             "real_distribution_zip_members":8,
             "files":[{"name":n,"bytes":len(v),"sha256":sha(v)} for n,v in sorted(files.items())],
             "formal_G0":"PENDING","native_Unity_G1":"NOT_RUN","SaveLoad":"NOT_RUN",
             "G2":"HOLD","codex_ACK":False,"integrated":False,
             "frozen_510":"d94400d95d13db7f27e1533df05c9292d5380431",
             "frozen_556":"33094f154c7b91119b36d58aab285e6e83c5de57",
             "frozen_596":"efb2b06eafedab5469b57f163152d96aca3c5915"}
    files["receipt.json"]=(json.dumps(machine,indent=2,sort_keys=True)+"\n").encode()
    out.mkdir(parents=True,exist_ok=True)
    for name,content in files.items():
        (out/name).write_bytes(content)
        need((out/name).read_bytes()==content,"actual physical new OBJ/MTL byte verification")
    make_zip(files,out.parent/(out.name+"-physical.zip"))
    print(json.dumps({"status":"PASS","original_source_W22_glTF_verified_SHA256":HASHES,
      "new_actual_OBJ_models":3,"new_actual_MTL":1,"real_mesh_triangles":COUNTS,
      "real_projected_OBJ_boards":2,"original_8member_ZIP":True,"native_Unity":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True);p.add_argument("--output",required=True)
    args=p.parse_args()
    try:main(Path(args.original),Path(args.output))
    except Exception as e:
        print("W23_SOURCE_GLTF_TO_OBJ_RED "+type(e).__name__+": "+str(e),file=sys.stderr);raise
