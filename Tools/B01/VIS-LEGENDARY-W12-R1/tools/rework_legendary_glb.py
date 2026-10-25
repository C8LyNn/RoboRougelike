#!/usr/bin/env python3
"""B01-30 W12: lossless visual GLB mount/weak-point retrofit, stdlib only.

Existing animation samplers, meshes, buffers, skins and geometry are not edited.
New empty nodes have ZERO mounting, physics or socket authorization.
"""
import argparse
import copy
import hashlib
import json
import struct
from pathlib import Path

MODELS = {
 "M04-024": dict(legend="LEG-MOV-001", root="ROOT_UNDERSIDE_MOUNT_Z0", tag="MobilityHub", weak="underside-emitter", weak_point=[0,-0.82,0], aim=[0,-1,0], footprint=[3,3,2]),
 "M01-018": dict(legend="LEG-WPN-001", root="MOUNT_TOP_SOCKET", tag="WeaponBase", weak="rear-breech", weak_point=[0,0.66,-1.22], aim=[0,0,1], footprint=[3,2,2])
}
def parse_glb(raw):
 if len(raw)<28: raise ValueError("truncated GLB")
 magic,version,declared=struct.unpack_from("<4sII",raw,0)
 if magic!=b"glTF" or version!=2 or declared!=len(raw):raise ValueError("invalid GLB header or declared size")
 pos=12;chunks=[]
 while pos<len(raw):
  if pos+8>len(raw):raise ValueError("truncated GLB chunk header")
  length,kind=struct.unpack_from("<I4s",raw,pos);pos+=8
  if length%4 or pos+length>len(raw):raise ValueError("invalid GLB chunk length")
  chunks.append((kind,raw[pos:pos+length]));pos+=length
 if not chunks or chunks[0][0]!=b"JSON" or any(k not in (b"JSON",b"BIN\x00") for k,_ in chunks):raise ValueError("invalid GLB chunk types")
 if sum(k==b"JSON" for k,_ in chunks)!=1:raise ValueError("duplicate JSON chunk")
 return json.loads(chunks[0][1].rstrip(b" \x00")),chunks
def write_glb(document,chunks):
 data=json.dumps(document,ensure_ascii=False,separators=(",",":"),sort_keys=True).encode("utf8")
 data+=b" "*((-len(data))%4)
 sections=[struct.pack("<I4s",len(data),b"JSON")+data]
 sections.extend(struct.pack("<I4s",len(v),k)+v for k,v in chunks[1:])
 payload=b"".join(sections)
 return struct.pack("<4sII",b"glTF",2,12+len(payload))+payload
def retrofit(raw,model):
 if model not in MODELS:raise ValueError("model not on allowlist")
 d,chunks=parse_glb(raw);s=MODELS[model]
 root=next((i for i,n in enumerate(d["nodes"]) if n.get("name")==s["root"]),None)
 if root is None:raise ValueError("named source root missing: "+s["root"])
 if any(n.get("name","").startswith("VIS_LEG_") for n in d["nodes"]):raise ValueError("already retrofitted")
 preserved={k:copy.deepcopy(d.get(k)) for k in ("meshes","buffers","bufferViews","accessors","animations","skins")}
 src=hashlib.sha256(raw).hexdigest()
 e=d.setdefault("asset",{}).setdefault("extras",{})
 e["b01VisualRetrofit"]={"package_id":"B01-VIS-LEGENDARY-W12-R1","modelId":model,"legendaryDefinitionId":s["legend"],"referenceGlbSha256":src,
  "unapprovedMassKg":None,"massStatus":"UNRESOLVED_W12_CONTRACT","mountSocketKind":s["tag"],"socketAuthority":False,"weakSide":s["weak"],
  "footprintCellsProposed":s["footprint"],"gridM":0.55,"noGameplayChanges":True,"unityImport":"NOT_RUN","triangleCollision":"NOT_RUN",
  "meshBuffersPreserved":True,"animationBuffersPreserved":True}
 index=len(d["nodes"])
 d["nodes"].append({"name":"VIS_LEG_"+s["legend"]+"_SOCKET_"+s["tag"].upper(),"translation":[0,0,0],"extras":{"visualOnly":True,"typeHint":s["tag"],"isAuthoritativeSocket":False,"canAuthorizeMount":False}})
 d["nodes"].append({"name":"VIS_LEG_"+s["legend"]+"_WEAK_"+s["weak"].upper().replace("-","_"),"translation":s["weak_point"],"extras":{"visualOnly":True,"weakSide":s["weak"],"isAuthoritativeCollider":False}})
 d["nodes"].append({"name":"VIS_LEG_"+s["legend"]+"_NOMINAL_AIM_AXIS","extras":{"directionLocal":s["aim"],"visualOnly":True}})
 d["nodes"][root].setdefault("children",[]).extend((index,index+1,index+2))
 for m in d.get("materials",[]):
  m.setdefault("alphaMode","OPAQUE");m.setdefault("doubleSided",False)
  m.setdefault("extras",{})["urpBindingPolicy"]="EXPLICIT_MATERIAL_ASSET_REFERENCE_REQUIRED"
  m["extras"]["mayFallbackToMagentaWithoutUrpMaterial"]=True
 for k,v in preserved.items():
  if d.get(k)!=v:raise AssertionError("geometry/animation changed: "+k)
 result=write_glb(d,chunks)
 rebuilt,chunk2=parse_glb(result)
 if chunk2[1:]!=chunks[1:]:raise AssertionError("original GLB BIN changed")
 return result,{"model":model,"source_sha256":src,"output_sha256":hashlib.sha256(result).hexdigest(),
                "bytes":len(result),"meshes":len(d.get("meshes",[])),"animations":len(d.get("animations",[])),
                "animation_channels":sum(len(a.get("channels",[])) for a in d.get("animations",[]))}
def main():
 p=argparse.ArgumentParser()
 p.add_argument("--model",choices=sorted(MODELS),required=True)
 p.add_argument("--input",required=True,type=Path)
 p.add_argument("--output",required=True,type=Path)
 args=p.parse_args()
 out,details=retrofit(args.input.read_bytes(),args.model)
 args.output.parent.mkdir(parents=True,exist_ok=True);args.output.write_bytes(out)
 print(json.dumps(details,sort_keys=True))
if __name__=="__main__":main()
