#!/usr/bin/env python3
"""Read ACTUAL glTF 2.0 mesh vertices and LINEAR animated TRS at sampled times.

Reports precise *sampled vertex* AABB, NOT collider/AABB narrowphase or
continuous triangle collision. Standard library only; no Blender/Unity stub.
"""
import argparse
import bisect
import hashlib
import importlib.util
import json
import math
import struct
from pathlib import Path

spec=importlib.util.spec_from_file_location("legend_glb",Path(__file__).with_name("rework_legendary_glb.py"))
glb=importlib.util.module_from_spec(spec);spec.loader.exec_module(glb)
COMP={5126:("f",4),5125:("I",4),5123:("H",2),5121:("B",1)}
WIDTH={"SCALAR":1,"VEC2":2,"VEC3":3,"VEC4":4}
def get_bin(doc,chunks):
 if len(chunks)!=2 or chunks[1][0]!=b"BIN\x00":raise ValueError("exactly one internal GLB BIN chunk is required")
 return chunks[1][1]

def accessor(doc,binary,index):
 a=doc["accessors"][index]
 if a.get("sparse"):raise ValueError("sparse glTF accessor not supported")
 if "bufferView" not in a:raise ValueError("accessor missing bufferView")
 fmt,sz=COMP[a["componentType"]];width=WIDTH[a["type"]]
 view=doc["bufferViews"][a["bufferView"]]
 if view.get("buffer",0)!=0:raise ValueError("external buffer unsupported")
 stride=view.get("byteStride",width*sz)
 if stride<width*sz:raise ValueError("invalid strided accessor")
 off=view.get("byteOffset",0)+a.get("byteOffset",0)
 total=a["count"]
 if off<0 or off+(total-1)*stride+width*sz>len(binary):raise ValueError("out-of-bounds glTF accessor")
 unpack=struct.Struct("<"+fmt*width).unpack_from
 values=[unpack(binary,off+i*stride) for i in range(total)]
 if any(not math.isfinite(x) for row in values for x in row):raise ValueError("nonfinite accessor value")
 return values

def normalize(q):
 length=math.sqrt(sum(x*x for x in q))
 if length<1e-12:raise ValueError("zero quaternion")
 return tuple(x/length for x in q)
def slerp(a,b,t):
 a=normalize(a);b=normalize(b)
 dot=sum(x*y for x,y in zip(a,b))
 if dot<0:b=tuple(-x for x in b);dot=-dot
 if dot>0.9995:return normalize(tuple((1-t)*x+t*y for x,y in zip(a,b)))
 th=math.acos(max(-1,min(1,dot)));s=math.sin(th)
 return tuple((math.sin((1-t)*th)*x+math.sin(t*th)*y)/s for x,y in zip(a,b))
def pose_value(track,now,kind):
 times,values,interpolation=track
 if interpolation!="LINEAR":raise ValueError("only LINEAR animation supported; refuse approximate QA")
 j=bisect.bisect_right(times,now)
 if j==0:return values[0]
 if j==len(times):return values[-1]
 a,b=j-1,j;t=(now-times[a])/(times[b]-times[a])
 return slerp(values[a],values[b],t) if kind=="rotation" else tuple(values[a][i]*(1-t)+values[b][i]*t for i in range(len(values[a])))

def tracks(doc,binary,clip):
 result={}
 for channel in clip["channels"]:
  target=channel["target"];path=target["path"]
  if path not in ("translation","rotation","scale"):raise ValueError("unsupported animation target "+path)
  sampler=clip["samplers"][channel["sampler"]]
  inp=[v[0] for v in accessor(doc,binary,sampler["input"])]
  out=accessor(doc,binary,sampler["output"])
  if len(inp)!=len(out) or not inp or any(inp[i]>=inp[i+1] for i in range(len(inp)-1)):raise ValueError("invalid animation sample times")
  key=(target["node"],path)
  if key in result:raise ValueError("duplicate animated channel")
  result[key]=(inp,out,sampler.get("interpolation","LINEAR"))
 return result

def local_matrix(node,channels):
 if "matrix" in node:raise ValueError("node with matrix transform unsupported by strict sampler")
 t=channels.get("translation",node.get("translation",[0,0,0]))
 q=normalize(channels.get("rotation",node.get("rotation",[0,0,0,1])))
 sc=channels.get("scale",node.get("scale",[1,1,1]))
 x,y,z,w=q; xx,yy,zz=x*x,y*y,z*z;xy,xz,yz=x*y,x*z,y*z;wx,wy,wz=w*x,w*y,w*z
 return ((1-2*(yy+zz))*sc[0],(2*(xy-wz))*sc[1],(2*(xz+wy))*sc[2],t[0],
         (2*(xy+wz))*sc[0],(1-2*(xx+zz))*sc[1],(2*(yz-wx))*sc[2],t[1],
         (2*(xz-wy))*sc[0],(2*(yz+wx))*sc[1],(1-2*(xx+yy))*sc[2],t[2])
IDENTITY=(1,0,0,0,0,1,0,0,0,0,1,0)
def mul(a,b):
 rows=[]
 for i in (0,4,8):
  for j in (0,1,2):
   rows.append(a[i]*b[j]+a[i+1]*b[4+j]+a[i+2]*b[8+j])
  rows.append(a[i]*b[3]+a[i+1]*b[7]+a[i+2]*b[11]+a[i+3])
 return tuple(rows)

def world_transforms(doc,channels):
 nodes=doc["nodes"];out={};visited=set()
 def walk(i,parent):
  if i in visited:raise ValueError("cyclic/duplicate scene node")
  visited.add(i)
  mat=mul(parent,local_matrix(nodes[i],channels.get(i,{})))
  out[i]=mat
  for child in nodes[i].get("children",[]):walk(child,mat)
 for root in doc["scenes"][doc.get("scene",0)]["nodes"]:walk(root,IDENTITY)
 return out

def measured_bounds(doc,binary,matrices,vertices):
 mn=[float("inf")]*3;mx=[float("-inf")]*3;point_count=0
 for node_index,world in matrices.items():
  node=doc["nodes"][node_index]
  if "mesh" not in node:continue
  for xyz in vertices[node["mesh"]]:
   x,y,z=xyz
   p=(world[0]*x+world[1]*y+world[2]*z+world[3],
      world[4]*x+world[5]*y+world[6]*z+world[7],
      world[8]*x+world[9]*y+world[10]*z+world[11])
   for j in range(3):mn[j]=min(mn[j],p[j]);mx[j]=max(mx[j],p[j])
   point_count+=1
 if not point_count:raise ValueError("no reachable mesh vertices")
 return mn,mx,point_count

def inspect(path,samples=121):
 raw=Path(path).read_bytes();doc,chunks=glb.parse_glb(raw);binary=get_bin(doc,chunks)
 vertices={}
 for i,mesh in enumerate(doc["meshes"]):
  points=[]
  for primitive in mesh["primitives"]:
   if primitive.get("mode",4)!=4:raise ValueError("only triangle primitives supported")
   points.extend(accessor(doc,binary,primitive["attributes"]["POSITION"]))
  vertices[i]=points
 results=[]
 for clip in doc.get("animations",[]):
  tr=tracks(doc,binary,clip)
  keys=[x for times,_,_ in tr.values() for x in times]
  t0=min(keys);t1=max(keys)
  if t1<=t0:raise ValueError("zero-duration animation")
  ts=[t0+(t1-t0)*i/(samples-1) for i in range(samples)]
  low=[float("inf")]*3;high=[float("-inf")]*3
  snapshots=[]
  for i,t in enumerate(ts):
   ch={}
   for (node,kind),track in tr.items():ch.setdefault(node,{})[kind]=pose_value(track,t,kind)
   mn,mx,count=measured_bounds(doc,binary,world_transforms(doc,ch),vertices)
   for j in range(3):low[j]=min(low[j],mn[j]);high[j]=max(high[j],mx[j])
   if i in (0,(samples-1)//4,(samples-1)//2,3*(samples-1)//4,samples-1):
    snapshots.append({"timeSeconds":round(t,6),"minimum":list(map(lambda x:round(x,6),mn)),
      "maximum":list(map(lambda x:round(x,6),mx))})
  results.append({"clip":clip.get("name","UNNAMED"),"channels":len(clip["channels"]),
    "startSeconds":round(t0,5),"endSeconds":round(t1,5),"samples":samples,
    "sampledSweptMin":list(map(lambda x:round(x,6),low)),
    "sampledSweptMax":list(map(lambda x:round(x,6),high)),
    "sampledSweptSize":list(map(lambda a:round(high[a]-low[a],6),range(3))),
    "snapshots":snapshots,"verticesPerSample":count})
 if not results:raise ValueError("no animated clips")
 if any(not all(math.isfinite(x) for x in r["sampledSweptSize"]) for r in results):raise ValueError("bad mesh bounds")
 return {"source_sha256":hashlib.sha256(raw).hexdigest(),"meshCount":len(doc["meshes"]),
         "animations":results,"disclaimer":"E2 SAMPLED ACTUAL GLB VERTICES, not BVH triangle collision or Unity/PhysX",
         "samplesPerClip":samples}

def main():
 p=argparse.ArgumentParser()
 p.add_argument("--glb",type=Path,required=True)
 p.add_argument("--output",type=Path,required=True)
 p.add_argument("--samples",type=int,default=121)
 args=p.parse_args()
 if not 3<=args.samples<=1001:raise ValueError("samples must be between 3 and 1001")
 result=inspect(args.glb,args.samples)
 args.output.parent.mkdir(parents=True,exist_ok=True)
 args.output.write_text(json.dumps(result,indent=2,sort_keys=True)+"\n",encoding="utf8")
 for row in result["animations"]:print(row["clip"],row["sampledSweptSize"],row["channels"])
if __name__=="__main__":main()
