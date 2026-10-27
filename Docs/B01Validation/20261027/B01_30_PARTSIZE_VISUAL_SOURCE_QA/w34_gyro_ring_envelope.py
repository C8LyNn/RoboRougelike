#!/usr/bin/env python3
"""B01-30 W34: original existing Gyroscope decorative ring AABB SOURCE proxy.

The ring is a Unity Cylinder child rotated by Quaternion.Euler(45,0,20), of the
actual R3 block with scale 0.52. Continuous-cylinder analytical support = a
CONSERVATIVE envelope of Unity's tessellated mesh, NOT observed Renderer.bounds.
Actual collider disabled/destroyed; mounting and PhysX are NEVER approved here.
"""
from __future__ import annotations
import argparse, json, math, re
from pathlib import Path

GRID=.55
R41_BLOB="76a14de4c347dd3e91b0a37487d804023eab5da1"
R3_BLOB="1b49c7365388694ebffacc5a4f20b3572150577a"
CODEX303_HEAD="c89744d5ab43f4a795abe62527d94439be573693"
SOURCE_PRODUCER_PART="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
SOURCE_B01_23_QA="60c91b8172229bde9b02042125a79c3ebbfe5faa"
ROOT_ANCHORS=(
 "item.transform.SetParent(root.transform,false);",
 "item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);",
 "item.transform.localScale=Vector3.one*0.52f;",
 "if(!wide)payload=R41PartGeometry.Payload(item.transform,state.Kind,state.Weapon,",
 "true,out partMuzzle);")
GEOM_ANCHORS=(
 'var ring=Primitive(PrimitiveType.Cylinder,block,material,false,"Pierścień żyroskopu").transform;',
 "ring.localScale=new Vector3(1.15f,.05f,1.15f);ring.localRotation=Quaternion.Euler(45,0,20);",
 "if(!solid){var collider=item.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);}",
 "device.localPosition=Vector3.zero;device.localScale=Vector3.one*.85f;",
 'kind==R3PartKind.Blink||kind==R3PartKind.Gyroscope?PrimitiveType.Sphere:',
)
def one(source,expression,label):
 found=re.findall(expression,source)
 if len(found)!=1:raise ValueError("SOURCE_ANCHOR_DRIFT:"+label)
 return found[0]
def anchors(r41,r3):
 for name,s,tokens in (("R41",r41,GEOM_ANCHORS),("R3",r3,ROOT_ANCHORS)):
  for token in tokens:
   if token not in s:raise ValueError("SOURCE_ANCHOR_DRIFT:"+name+":"+token)
 # source must be *actual* visual decorative Gyro and not a changed physical ring
 scale=[float(t) for t in one(r41,r"ring\.localScale=new Vector3\(([^)]+)\);ring\.localRotation=", "ringScale").replace("f","").split(",")]
 angles=[float(t) for t in one(r41,r"ring\.localRotation=Quaternion\.Euler\(([^)]+)\);", "ringEuler").split(",")]
 root=float(one(r3,r"item\.transform\.localScale=Vector3\.one\*([\d.]+)f;", "rootScale"))
 if scale!=[1.15,.05,1.15] or angles!=[45,0,20] or root!=.52:
  raise ValueError("SOURCE_TRANSFORM_NOT_SUPPORTED")
 return scale,angles,root

def rotation_zyx_unity_zxy_deg(x,y,z):
 # Unity Quaternion.Euler: rotate Z, then X, then Y; column vectors R=Ry @ Rx @ Rz.
 a,b,c=map(math.radians,(x,y,z))
 sx,cx=math.sin(a),math.cos(a)
 sy,cy=math.sin(b),math.cos(b)
 sz,cz=math.sin(c),math.cos(c)
 rx=((1,0,0),(0,cx,-sx),(0,sx,cx))
 ry=((cy,0,sy),(0,1,0),(-sy,0,cy))
 rz=((cz,-sz,0),(sz,cz,0),(0,0,1))
 def mul(a,b):
  return tuple(tuple(sum(a[i][k]*b[k][j] for k in range(3)) for j in range(3)) for i in range(3))
 return mul(mul(ry,rx),rz)

def analytical_continuous_cylinder_half_extents(local_scale,angles,root):
 # Unity Cylinder primitive: diameter=1, length=2 along local Y.
 # Transformed continuous round cylinder support function per axis:
 # radius * norm(row projection onto transformed XZ circle) + height/2*abs(axis Y).
 # Unity low-poly mesh is INSIDE this ideal shape: results are conservative proxies.
 r=rotation_zyx_unity_zxy_deg(*angles)
 x,y,z=local_scale
 if abs(x-z)>1e-10:raise ValueError("ELLIPTICAL_CYLINDER_UNSUPPORTED")
 return [root*((x/2)*math.hypot(r[i][0],r[i][2])+y*abs(r[i][1])) for i in range(3)]

def signed_cells(half):
 return [list(range(math.floor((-v+GRID/2)/GRID),math.floor((v+GRID/2)/GRID)+1))
   for v in half]

def audit(r41,r3):
 local,angles,root=anchors(r41,r3)
 h=analytical_continuous_cylinder_half_extents(local,angles,root)
 # R3 yaw quarter turns: X/Z projected axes swap for a centered part; Y invariant.
 cases=[]
 for q in range(4):
  ext=(h[0],h[1],h[2]) if q%2==0 else (h[2],h[1],h[0])
  total=[max(root/2,v) for v in ext] # root 0.52 cube and decorative ring, NOT the gyro main sphere if larger
  cases.append({"yawQuarterTurns":q,"orientationDegrees":q*90,
      "decorativeRingConservativeHalfExtentXYZm":[round(v,9) for v in ext],
      "visualRootPlusRingConservativeHalfExtentXYZm":[round(v,9) for v in total],
      "decorativeRingConservativeSignedCellsXYZ":signed_cells(ext),
      "overrunBeyondOneCellMetersXYZ":[round(max(0,v-GRID/2),9) for v in total],
      "actualUnityRendererBounds":"NOT_RUN",
      "actualUnityColliderBounds":"NOT_RUN",
      "ringCollider":"SOURCE_DISABLED_NOT_PHYSX_VERIFIED"})
 assert h[0]>GRID/2 and h[1]<GRID/2 and h[2]<GRID/2, "geometry analytical guard"
 assert abs(h[0]-0.28986062)<0.00001,"analytical geometry drift"
 assert cases[0]["decorativeRingConservativeSignedCellsXYZ"]==[[-1,0,1],[0],[0]]
 assert cases[1]["decorativeRingConservativeSignedCellsXYZ"]==[[0],[0],[-1,0,1]]
 return {"schema":"B01-30-W34-GYROSCOPE-EXTRA-DECORATION-SOURCE-PROXY-V1",
   "inputCodexPR303Head":CODEX303_HEAD,"R41GitBlob":R41_BLOB,"R3AssemblyGitBlob":R3_BLOB,
   "inputPARTSIZEHead":SOURCE_PRODUCER_PART,"B01_23QAHead":SOURCE_B01_23_QA,
   "subject":"existing R3 Gyroscope extra nonphysical Cylinder ring only, previously omitted from 7-kind main-payload QA",
   "sourcePrimitiveAssumption":"nominal Unity Cylinder diameter 1 height 2, ideal continuous circular profile encloses tessellated mesh; 0.52 parent, Unity Euler Z then X then Y",
   "nonphysicalRingSource":True,
   "actualVisualMeshMeasurements":"NOT_RUN",
   "actualEngine":"NOT_RUN","G2":"HOLD","typedSocket":"UNVERIFIED",
   "rootCubeNominalHalfExtentM":root/2,"gridCellHalfExtentM":GRID/2,
   "ringYawCases":cases,
   "conclusion":"POTENTIAL_VISUAL_CROSS_CELL_OVERHANG_CONSERVATIVE_PROXY_NOT_VERIFIED",
   "assetSourceChanged":False}

def main():
 p=argparse.ArgumentParser()
 p.add_argument("--r41",required=True,type=Path);p.add_argument("--r3",required=True,type=Path)
 p.add_argument("--output",required=True,type=Path)
 args=p.parse_args()
 r=audit(args.r41.read_text(encoding="utf8"),args.r3.read_text(encoding="utf8"))
 args.output.write_text(json.dumps(r,sort_keys=True,indent=2)+"\n","utf8")
 print("SOURCE_CONTINUOUS_RING_ENVELOPE_PROXY_PASS / ACTUAL_UNITY_NOT_RUN")
 print("yaw0 overrun +X/-X",r['ringYawCases'][0]['overrunBeyondOneCellMetersXYZ'][0])

if __name__=="__main__":main()
