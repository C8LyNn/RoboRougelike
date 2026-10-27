#!/usr/bin/env python3
"""B01-30 W30 read-only reconciliation: original Codex R3 root scale vs B01-23's
deliberately UNIT-PARENT 7 payload source-proxies.

Input is ACTUAL unchanged owner R41/R3/PartSize and B01-23 Python algorithm.
Computes conservative source-only AABB, never Unity Collider.bounds or G1.
"""
import argparse
import importlib.util
import json
import math
import os
import re
from pathlib import Path

GRID=.55
SOURCE_CODEX="d44fa09c227dffca1c1f8832383bbd80430bba90"
SOURCE_PART="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
SOURCE_PAYLOAD_QA="60c91b8172229bde9b02042125a79c3ebbfe5faa"
SOURCE_HEAD_ART="7a1416d3e10ee720d9d28931a3c268f95e2bba15"
ANCHOR_ASSEMBLY=[
 "item.transform.localPosition=(Vector3)state.Cell*0.55f;",
 "item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);",
 "item.transform.localScale=Vector3.one*0.52f;",
 "if(!wide)payload=R41PartGeometry.Payload(item.transform,state.Kind,state.Weapon,",
 "palette.Parts[(int)state.Kind],true,out partMuzzle);",
]
ANCHOR_GEOM=[
 "if(kind==R3PartKind.OmniDrive)",
 "device.localPosition=Vector3.down*.4f;device.localRotation=Quaternion.Euler(0,0,90);device.localScale=new Vector3(.9f,.18f,.9f);",
 "var device=Primitive(type,block,material,solid,\"MOV • \"+kind).transform;",
 'Primitive(PrimitiveType.Cylinder,block,material,false,"Pierścień żyroskopu")',
 "if(!solid){var collider=item.GetComponent<Collider>();collider.enabled=false;Object.Destroy(collider);}",
]
KINDS=("Booster","JumpDrive","Blink","Gyroscope","OmniDrive","MagneticGrapple","HoverEngine")

def check_anchors(text,anchors,name):
 absent=[a for a in anchors if a not in text]
 if absent: raise ValueError("PINNED_SOURCE_DRIFT "+name+" "+repr(absent))
 return True

def original_producer_module(path):
 spec=importlib.util.spec_from_file_location("actual_b01_23_source_payload",path)
 if not spec or not spec.loader: raise ValueError("producer module cannot load")
 m=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(m)
 return m

def root_scale(assembly):
 check_anchors(assembly,ANCHOR_ASSEMBLY,"R3VehicleAssembly.cs")
 match=re.findall(r'item\.transform\.localScale=Vector3\.one\*([\d.]+)f;',assembly)
 if match!=["0.52"]:raise ValueError("OWNER_SCALE_NOT_EXACT_SOURCE")
 return float(match[0])

def yrot(p,q):
 x,y,z=p
 return ((x,y,z),(z,y,-x),(-x,y,-z),(-z,y,x))[q%4]

def rotate_bounds(lo,hi,q):
 if len(lo)!=3 or len(hi)!=3:raise ValueError("invalid AABB arity")
 points=[yrot((x,y,z),q) for x in (lo[0],hi[0])
         for y in (lo[1],hi[1]) for z in (lo[2],hi[2])]
 return ([round(min(p[i] for p in points),7) for i in range(3)],
         [round(max(p[i] for p in points),7) for i in range(3)])

def cell_cover(lo,hi):
 if len(lo)!=3 or len(hi)!=3:raise ValueError("invalid AABB arity")
 ranges=[]
 for a,b in zip(lo,hi):
  if not(math.isfinite(a) and math.isfinite(b)) or a>b:
   raise ValueError("non-finite / inverted AABB")
  first=math.floor(a/GRID+0.5)
  last=math.ceil(b/GRID+0.5)-1
  ranges.append(list(range(first,last+1)))
 return ranges

def reconcile(assembly,geometry,catalog,producer_script):
 factor=root_scale(assembly)
 check_anchors(geometry,ANCHOR_GEOM,"R41PartGeometry.cs")
 source=original_producer_module(producer_script)
 prior=source.analyze(geometry,catalog,assembly)
 if prior.get("host_assembly_scale")!=factor:
  raise ValueError("B01_23_CURRENT_HOST_SCALE_DISAGREEMENT")
 if prior.get("source_only")!=True or prior.get("native")!="NOT_RUN":
  raise ValueError("OWNER_PROXY_NOT_SOURCE_ONLY")
 if [row['kind'] for row in prior['rows']]!=list(KINDS):
  raise ValueError("SEVEN_SOURCE_VARIANTS_DRIFT")
 out=[]
 for row in prior["rows"]:
  k=row["kind"]
  unitLo=row["payload_aabb_min_m"];unitHi=row["payload_aabb_max_m"]
  entries=[]
  for yaw in range(4):
   # Actual canonical R3 assembly scales the parent; nothing in owner
   # producer payload bounds included this scale (unit-parent assumption).
   scaledLo=[factor*x for x in unitLo]
   scaledHi=[factor*x for x in unitHi]
   newLo,newHi=rotate_bounds(scaledLo,scaledHi,yaw)
   # Actual root game object is a standard BoxCollider cube, nominal .52m.
   # For main-round decorative rings and extra Gyro ring these bounds are
   # explicitly INCOMPLETE as render envelope or dynamic movement.
   baseLo=[-factor/2]*3;baseHi=[factor/2]*3
   fullLo=[round(min(newLo[i],baseLo[i]),7) for i in range(3)]
   fullHi=[round(max(newHi[i],baseHi[i]),7) for i in range(3)]
   entries.append({
    "yawQuarterTurns":yaw,
    "actualBuildScaleSource":factor,
    "payloadSourceProxyWorldMinXYZ":newLo,
    "payloadSourceProxyWorldMaxXYZ":newHi,
    "unionWithCoreRootBoxMinXYZ":fullLo,
    "unionWithCoreRootBoxMaxXYZ":fullHi,
    "payloadSignedCellsXYZ":cell_cover(newLo,newHi),
    "rootPlusPayloadSignedCellsXYZ":cell_cover(fullLo,fullHi),
    "unityRealColliderBounds":"NOT_RUN",
    "sourceStaticOnly":True})
  if entries[0]["payloadSourceProxyWorldMinXYZ"]!=row["assembled_payload_aabb_min_m"] or entries[0]["payloadSourceProxyWorldMaxXYZ"]!=row["assembled_payload_aabb_max_m"]:
   raise ValueError("B01_23_CURRENT_YAW0_SOURCE_DISAGREEMENT "+k)
  if entries[0]["unionWithCoreRootBoxMinXYZ"]!=row["assembled_root_plus_payload_aabb_min_m"] or entries[0]["unionWithCoreRootBoxMaxXYZ"]!=row["assembled_root_plus_payload_aabb_max_m"]:
   raise ValueError("B01_23_CURRENT_YAW0_ROOT_DISAGREEMENT "+k)
  out.append({
   "kind":k,
   "b01_23_unitParentSourceProxyMinXYZ":unitLo,
   "b01_23_unitParentSourceProxyMaxXYZ":unitHi,
   "b01_23_unitParentSourceCellsXYZ":row["signed_cells_intersected_xyz"],
   "actualR3ScaledStaticYawCases":entries,
   "extraGyroDecorativeRingExcluded":k=="Gyroscope",
   "sweptGeometryStatus":"NOT_RUN",
   "hostSolidEnabled":"SOURCE_TRUE_NOT_ENGINE_VERIFIED",
   "typedSocket":"UNVERIFIED"})
 omni=next(x for x in out if x["kind"]=="OmniDrive")
 if omni["b01_23_unitParentSourceProxyMinXYZ"] != [-.18,-.85,-.45]:
  raise ValueError("OWNER_UNIT_PARENT_PROXY_DRIFT")
 q0=omni["actualR3ScaledStaticYawCases"][0]
 if q0["payloadSourceProxyWorldMinXYZ"] != [-.0936,-.442,-.234]:
  raise ValueError("SCALED_OMNI_YAW0_NOT_EXPECTED")
 if q0["payloadSignedCellsXYZ"][1] != [-1,0]:
  raise ValueError("SOURCE_OMNI_SIGNED_CELL_DRIFT")
 return {"schema":"B01-30-W30-CODEX-R3-PARENT-SCALE-RECONCILE-1",
  "inputCodexSourceHead":SOURCE_CODEX,
  "inputPartsizeHead":SOURCE_PART,
  "inputProducerSourceProxyQaHead":SOURCE_PAYLOAD_QA,
  "originalConceptHead":SOURCE_HEAD_ART,
  "sourceR3PartParentScale":factor,
  "purpose":"Independently verify UPDATED B01-23 owner host-scale yaw0 and extend to other three quarter-turns; NOT a real Collider.bounds measurement",
  "OmniDriveNewSourceFact":"R41 one cylinder child, NOT four ring pods in CM001-C",
  "fullVehicleDimensions":"UNKNOWN",
  "dynamicMotion":"NOT_RUN","actualUnity":"NOT_RUN","physx":"NOT_RUN",
  "HQConceptSelection":"PENDING","g2":"HOLD","changeProducerFiles":False,
  "rows":out}

def main():
 p=argparse.ArgumentParser()
 for s in ("assembly","geometry","catalog","producer"):
  p.add_argument("--"+s,type=Path,required=True)
 p.add_argument("--output",type=Path,required=True)
 a=p.parse_args()
 r=reconcile(a.assembly.read_text("utf8"),a.geometry.read_text("utf8"),
             a.catalog.read_text("utf8"),a.producer)
 a.output.parent.mkdir(parents=True,exist_ok=True)
 a.output.write_text(json.dumps(r,indent=2,sort_keys=True)+"\n","utf8")
 for x in r["rows"]:
  print(x['kind'],x['b01_23_unitParentSourceCellsXYZ'],
        "R3 q0",x['actualR3ScaledStaticYawCases'][0]['payloadSignedCellsXYZ'])
 print("SOURCE_R3_PARENT_SCALE_RECONCILED / UNITY_NOT_RUN / HQ_ART_PENDING")

if __name__=="__main__":main()
