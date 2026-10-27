#!/usr/bin/env python3
"""B01-30 W16: source-anchored visual/physical AABB fixtures for PARTSIZE-001.

Read-only replay of literal Unity primitive transforms from a pinned Codex SHA.
AABB != Physics.ComputePenetration, art != logical footprint, and Unity is NOT_RUN.
No edits to PARTSIZE producer, active Codex game, or any Unity Assets.
"""
import argparse
import json
import math
from pathlib import Path

SOURCE_HEAD="9cd5a5272a9ebda130a97a5f1f73ce3eaf24b976"
GEOMETRY_BLOB="76a14de4c347dd3e91b0a37487d804023eab5da1"
ASSEMBLY_BLOB="fetched_in_CI_sha_receipt"
GRID=.55
BLOCK=.52

# These MUST match source literals. Any changed declaration rejects this
# fixture rather than presenting a stale calculated size as a measured one.
GEOMETRY_ANCHORS=[
 "public const float Grid=0.55f, BlockSize=0.52f;",
 "device.localScale=Vector3.one*.85f;",
 "device.localPosition=Vector3.down*.55f;device.localScale=new Vector3(.85f,.5f,1.8f);",
 "device.localPosition=Vector3.down*.45f;device.localScale=new Vector3(1,.16f,1);",
 "device.localRotation=Quaternion.Euler(90,0,0);device.localPosition=Vector3.forward*(kind==R3PartKind.Booster?-.6f:.6f);",
 "device.localScale=new Vector3(.65f,.4f,.65f);",
 "piston.localPosition=new Vector3(0,-.55f,0);piston.localScale=new Vector3(.3f,.45f,.3f);",
 "wheel.localPosition=new Vector3(0,-0.5f,0);wheel.localRotation=Quaternion.Euler(0,0,90);",
 "wheel.localScale=new Vector3(1.5f,0.35f,1.5f);",
 "tire.sharedMesh=wheel.GetComponent<MeshFilter>().sharedMesh;tire.convex=true;",
 "pivot.localPosition=Vector3.up*0.6f;",
 "barrel.localPosition=Vector3.forward*(heavy?1.1f:0.85f);",
 "barrel.localScale=heavy?new Vector3(0.48f,0.4f,2):"
]
ASSEMBLY_ANCHORS=[
 "item.transform.localScale=Vector3.one*0.52f;",
 "item.transform.localPosition=(Vector3)state.Cell*0.55f;",
 "item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);",
 "if(wide)R3BonusModuleGeometry.ConfigureCube(item,true);",
 "if(!wide)payload=R41PartGeometry.Payload("
]
BONUS_ANCHORS=[
 "public const float Grid=0.55f;",
 "public const float Height=0.52f;",
 "public const float Width=Grid*2f-0.03f;",
 "box.center=new Vector3(Grid*.5f,0,Grid*.5f);",
 "box.size=new Vector3(Width,Height,Width);",
 "cube.transform.localScale=Vector3.one;"
]
MODEL_ANCHORS=[
 "public static bool UsesWideFootprint(R3PartKind kind)",
 "yield return anchor+R3MountingGrid.Rotate(Vector3Int.right,turns);",
 "yield return anchor+R3MountingGrid.Rotate(Vector3Int.forward,turns);",
 "yield return anchor+R3MountingGrid.Rotate(Vector3Int.right+Vector3Int.forward,turns);"
]
def verify_text(text,anchors,filename):
 missing=[t for t in anchors if t not in text]
 if missing:raise ValueError("SOURCE_DRIFT:"+filename+":"+repr(missing))
 return len(anchors)

def verify_sources(paths):
 inputs=[("R41PartGeometry.cs",GEOMETRY_ANCHORS),("R3VehicleAssembly.cs",ASSEMBLY_ANCHORS),
         ("R3BonusModuleGeometry.cs",BONUS_ANCHORS),("R3VehicleModel.cs",MODEL_ANCHORS)]
 if len(paths)!=len(inputs):raise ValueError("four exact source paths required")
 return {name:verify_text(Path(p).read_text(encoding="utf8"),anchors,name)
         for p,(name,anchors) in zip(paths,inputs)}

def rotate_y(p,quarters):
 x,y,z=p; q=quarters%4
 return ((x,y,z),(z,y,-x),(-x,y,-z),(-z,y,x))[q]

def rotate_z90(p):
 x,y,z=p
 return (-y,x,z)

def union_bounds(components,quarters,physical):
 low=[math.inf]*3;high=[-math.inf]*3
 for o in components:
  if physical and not o["collider"]:continue
  scale=o["scale"]
  primitive=o["primitive"]
  radii=(.5,1,.5) if primitive=="cylinder" else (.5,.5,.5)
  half=[BLOCK*scale[i]*radii[i] for i in range(3)] if not o.get("wide") else [scale[i]*radii[i] for i in range(3)]
  t=o["at"]
  # An axis-aligned primitive rotated 90 degrees around Z, then yaw around Y:
  corners=[]
  for sx in (-1,1):
   for sy in (-1,1):
    for sz in (-1,1):
     p=(sx*half[0],sy*half[1],sz*half[2])
     if o.get("rotZ90"):p=rotate_z90(p)
     p=tuple(p[i]+(t[i] if o.get("wide") else BLOCK*t[i]) for i in range(3))
     corners.append(rotate_y(p,quarters))
  for p in corners:
   for i in range(3):
    low[i]=min(low[i],p[i]);high[i]=max(high[i],p[i])
 if not math.isfinite(low[0]):raise ValueError("no collider geometry")
 return [round(x,6) for x in low],[round(x,6) for x in high]

def item(primitive,scale,at=(0,0,0),collider=True,rotZ90=False,wide=False):
 return dict(primitive=primitive,scale=scale,at=at,collider=collider,rotZ90=rotZ90,wide=wide)

# Core cube uses the exact collider and material primitive in R3VehicleAssembly.
CUBE=item("box",(1,1,1))
# Child primitives and capsule->convex MeshCollider retention from R41 source.
PARTS={
 "Armor":{"cells":(1,1,1),"shapes":[CUBE]},
 "Wheel":{"cells":(1,1,1),"shapes":[CUBE,item("cylinder",(1.5,.35,1.5),(0,-.5,0),True,True)]},
 "CrawlerTrack":{"cells":(1,1,1),"shapes":[CUBE,item("box",(.85,.5,1.8),(0,-.55,0))]},
 "Suspension":{"cells":(1,1,1),"shapes":[CUBE,item("cylinder",(.3,.45,.3),(0,-.55,0),False)]},
 "HoverEngine":{"cells":(1,1,1),"shapes":[CUBE,item("cylinder",(1,.16,1),(0,-.45,0))]},
 "Booster":{"cells":(1,1,1),"shapes":[CUBE,item("cylinder",(.65,.4,.65),(0,0,-.6),True,False)]},
 "GunHeavy":{"cells":(1,1,1),"shapes":[CUBE,item("box",(.48,.4,2),(0,.6,1.1))]},
 "BonusModule":{"cells":(2,1,2),"shapes":[item("box",(1.07,.52,1.07),(.275,0,.275),True,False,True)]},
}
# Booster's child cylinder has X90 rotation (cylinder height axis -> Z), not Z90.
# Apply a dedicated half-axis permutation for this source-authenticated type.
def booster_corrected():
 # Represents box of local cylinder under +90deg X: X radius .5, Y radius .5, Z halfheight 1.
 return item("box",(.65,.65,.8),(0,0,-.6))

def physical_logical_grid_bounds(cells,q):
 nx,ny,nz=cells
 # Logical cell union anchored at cell (0,0,0), rotates with part root.
 corners=[]
 for x in (-.5,nx-.5):
  for y in (-.5,ny-.5):
   for z in (-.5,nz-.5):
    corners.append(rotate_y((x*GRID,y*GRID,z*GRID),q))
 return [round(min(p[i] for p in corners),6) for i in range(3)], \
        [round(max(p[i] for p in corners),6) for i in range(3)]

def measure(name,q):
 if name not in PARTS:raise ValueError("unsupported source-grounded part")
 src=PARTS[name]
 parts=src["shapes"]
 if name=="Booster":
  # source cylinder rotated X90, local radii .5 X and .5 Y / height Z
  # scaled .65,.4,.65 => original shape half [0.325,0.325,0.4]
  parts=[CUBE,item("box",(.65,.65,.8),(0,0,-.6))]
 physicalMin,physicalMax=union_bounds(parts,q,True)
 visualMin,visualMax=union_bounds(parts,q,False)
 cellMin,cellMax=physical_logical_grid_bounds(src["cells"],q)
 exceed=[round(max(0,cellMin[i]-physicalMin[i],physicalMax[i]-cellMax[i]),6) for i in range(3)]
 vExceed=[round(max(0,cellMin[i]-visualMin[i],visualMax[i]-cellMax[i]),6) for i in range(3)]
 return {"part":name,"yawQuarterTurns":q,
  "logicalCellsWHD":list(src["cells"]),"logicalBoundsMinXYZ":cellMin,
  "logicalBoundsMaxXYZ":cellMax,
  "physicalColliderMinXYZ":physicalMin,"physicalColliderMaxXYZ":physicalMax,
  "physicalColliderSizeXYZ":[round(physicalMax[i]-physicalMin[i],6) for i in range(3)],
  "renderMinXYZ":visualMin,"renderMaxXYZ":visualMax,
  "physicalOverrunByAxisXYZ":exceed,"renderOverrunByAxisXYZ":vExceed,
  "physicalFitsLogicalAABB":all(v==0 for v in exceed),
  "visualFitsLogicalAABB":all(v==0 for v in vExceed),
  "unityNative":"NOT_RUN","physxComputePenetration":"NOT_RUN"}

def run(paths,out):
 anchors=verify_sources(paths)
 records=[measure(k,q) for k in PARTS for q in range(4)]
 report={"kind":"B01_30_W16_PARTSIZE_VISUAL_QA_ONLY","sourceHead":SOURCE_HEAD,
  "sourceAnchorsVerified":anchors,"gridM":GRID,"baseCubeM":BLOCK,
  "records":records,"sourceScope":"EXISTING_CANONICAL_READ_ONLY",
  "geometryMethod":"analytical transformed Unity primitive bounding boxes, not actual Unity engine render or collision solve",
  "G2":"HOLD","unityNative":"NOT_RUN","approvedFootprints":False}
 out=Path(out);out.parent.mkdir(parents=True,exist_ok=True)
 out.write_text(json.dumps(report,indent=2,sort_keys=True)+"\n",encoding="utf8")
 return report

def main():
 ap=argparse.ArgumentParser()
 for n in ("geometry","assembly","bonus","model"):ap.add_argument("--"+n,required=True)
 ap.add_argument("--out",required=True)
 args=ap.parse_args()
 obj=run([args.geometry,args.assembly,args.bonus,args.model],args.out)
 counts={}
 for r in obj["records"]:
  if r["yawQuarterTurns"]==0:counts[r["part"]]=r["physicalColliderSizeXYZ"]
 print(json.dumps({"source":SOURCE_HEAD,"actualRecords":len(obj["records"]),
   "physicalSizeYaw0":counts,"Unity":"NOT_RUN"},sort_keys=True))

if __name__=="__main__":main()
