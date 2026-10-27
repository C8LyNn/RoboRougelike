#!/usr/bin/env python3
"""B01-30 QA companion to B01-23 PR286. Read-only, exact pinned source evidence.

Distinguishes actual C# contract/catalog from source-predicted R41 shapes.
Does NOT assert native Unity Collider.bounds, renderer, PhysX, socket geometry.
"""
import argparse
import csv
import hashlib
import importlib.util
import json
import math
import os
import re
from pathlib import Path

LOCAL=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location("legacy_source_geometry", LOCAL/"partsize_visual_source_qa.py")
legacy=importlib.util.module_from_spec(spec);spec.loader.exec_module(legacy)

CANONICAL_NAMES=["Armor","Wheel","Gun","Core","Suspension","Booster","JumpDrive",
 "Blink","Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack",
 "BonusModule","DirectionalShield","PointDefense","StructuralScanner","ScannerPulse","IndependentSteering"]
WIDE={"BonusModule","DirectionalShield","PointDefense","StructuralScanner","ScannerPulse","IndependentSteering"}
MAPPED={"Armor":"Armor","Wheel":"Wheel","CrawlerTrack":"CrawlerTrack","Suspension":"Suspension",
        "HoverEngine":"HoverEngine","Booster":"Booster","GunHeavy":"Gun","BonusModule":"BonusModule"}
EXPECTED_STABLE_ANCHORS={
 "PartSizeContract.cs":["public const double GridMeters=.55;","public const double LegacyBlockMeters=.52;",
  "public const double MaximumWorldOvershootMeters=.005;","return \"UNMEASURED_PHYSICAL_ENVELOPE\";",
  "public static Box3 PhysicalBounds(","public static bool IsExposedFace("],
 "PartSizeBaselineCatalog.cs":["public static readonly string[] CanonicalNames = {",
  "return PartSizeReferenceFixtures.LegacyWide(canonicalKind);",
  "return PartSizeReferenceFixtures.Unmeasured(canonicalKind,",
  "new Cell3(1,1,1));"],
 "PartSizeNativePhysics.cs":["public const float MaxPenetrationMeters=.005f;",
  "actual.min.x>=allowed.min.x-MaxPenetrationMeters-WorldBoundsEpsilon",
  "Physics.SyncTransforms();","Physics.ComputePenetration(",
  "Collider[] nearby=Physics.OverlapBox(","return \"INCOMPLETE_EXTERNAL_BROADPHASE\";",
  "fullExternalBroadphase==null||!completeExternalBroadphase",
  "foreach(var outside in fullExternalBroadphase)","return null;"],
 "PartSizeDemoEntry.cs":["Add(PartSizeReferenceFixtures.LegacyCube()",
  "Add(PartSizeReferenceFixtures.LegacyWide()","new Vector3(1.2f,.52f,.52f)",
  "Shader.Find(\"Universal Render Pipeline/Unlit\")",
  "GUILayout.BeginArea(new Rect(20,20,620,205)"],
 "PartSize001PlayModeTests.cs":["UnverifiedBroadphaseNeverCertifiesPhysicalContact",
  "new Collider[0],false","OmittedPhysicalChildCannotBypassAuthoritativeInventory",
  "DemoProducesThreePhysicallyCheckedColorStates"],
 "PartSize001EditModeTests.cs":["NoRealColliderMeasurementsNeverTurnIntoFakeApproval",
  "AllXYZQuarterTurnFixturesFitTheSamePhysicalEnvelope"]
}

def text(root,name):
 matches=list(Path(root).rglob(name))
 if len(matches)!=1:raise ValueError("expected exactly one PR286 source "+name)
 return matches[0].read_text("utf8")
def evaluate(producer_root,canonical_source_root):
 # Old target evidence must remain pinned to PR272, not silently compare new
 # sources against an imagined/current Codex worktree.
 old=[Path(canonical_source_root)/x for x in ("R41PartGeometry.cs","R3VehicleAssembly.cs",
     "R3BonusModuleGeometry.cs","R3VehicleModel.cs")]
 stamps=legacy.verify_sources(old)
 srcs={name:text(producer_root,name) for name in EXPECTED_STABLE_ANCHORS}
 for name,anchors in EXPECTED_STABLE_ANCHORS.items():
  missing=[a for a in anchors if a not in srcs[name]]
  if missing:raise ValueError("PR286_SOURCE_DRIFT:"+name+":"+str(missing))
 cat=srcs["PartSizeBaselineCatalog.cs"]
 block=cat.split("CanonicalNames = {",1)[1].split("};",1)[0]
 listed=re.findall(r'"([^"]+)"',block)
 if listed!=CANONICAL_NAMES:raise ValueError("canonical part catalog list drift")
 # Six true wide kinds must be source-led, not inferred by appearance.
 wides=cat.split("new[]{",1)[1].split("}",1)[0]
 if set(re.findall(r'"([^"]+)"',wides))!=WIDE:raise ValueError("wide kinds drift")
 cs=srcs["PartSizeNativePhysics.cs"]
 # Verified boundary: producer untrusted bool claims completeness; no automatic
 # Physics.OverlapBox/Sphere/Capsule/Collider broadphase recovery in this method.
 broadphase_recomputed=bool(re.search(r'Physics\.Overlap(?:Box|Sphere|Capsule|Collider)',cs))
 passed_claim=bool(re.search(r'foreach\s*\(\s*var outside in fullExternalBroadphase\s*\)',cs))
 has_null_return=cs.rstrip().count("return null;")>0
 if not (passed_claim and has_null_return):raise ValueError("unexpected external gate control flow")
 matrix=text(producer_root,"observed_r3_size_matrix.csv")
 rows=list(csv.DictReader(matrix.splitlines()))
 if [x["part_kind"] for x in rows]!=CANONICAL_NAMES:raise ValueError("matrix row names/order differ from real catalog")
 if any(x["actual_unity_collider_measured"]!="NOT_RUN" for x in rows):
  raise ValueError("unverifiable native measurement claim")
 if any(x["mesh_aabb_measured_m"] for x in rows):
  raise ValueError("unverifiable mesh AABB claim")
 if any(x["socket_contact_measured"] for x in rows):
  raise ValueError("unverifiable socket contact claim")
 for row in rows:
  k=row["part_kind"]
  expected=[2,1,2] if k in WIDE else [1,1,1]
  dims=[int(row[x]) for x in ("logical_x","logical_y","logical_z")]
  if dims!=expected:raise ValueError("logical footprint inconsistent with R3 baseline "+k)
 out=[]
 for geometry,canon in MAPPED.items():
  for q in range(4):
   rec=legacy.measure(geometry,q)
   # Original canonical source payload is the physical source fixture;
   # PR286 catalog intentionally marks these UNMEASURED, rather than
   # silently retrofitting a 1-cell collider/enlarged cells.
   status=("SOURCE_DERIVED_BASE_BOX_ONLY" if canon in WIDE|{"Armor","Core"} else
           "CATALOG_UNMEASURED_REQUIRES_NATIVE")
   out.append({"pr286Kind":canon,"legacyVariant":geometry,"yawQuarterTurns":q,
    "declaredCells":rec["logicalCellsWHD"],
    "sourcePredictedPhysicalAABBMinXYZ":rec["physicalColliderMinXYZ"],
    "sourcePredictedPhysicalAABBMaxXYZ":rec["physicalColliderMaxXYZ"],
    "sourcePredictedPhysicalAABBSizeXYZ":rec["physicalColliderSizeXYZ"],
    "sourcePredictedRenderMinXYZ":rec["renderMinXYZ"],
    "sourcePredictedRenderMaxXYZ":rec["renderMaxXYZ"],
    "sourcePredictedColliderOverrunXYZ":rec["physicalOverrunByAxisXYZ"],
    "sourcePredictedVisualOverrunXYZ":rec["renderOverrunByAxisXYZ"],
    "catalogProof":status,
    "unityActualColliderBounds":"NOT_RUN",
    "physicalMountApproval":False})
 sourceOverruns=[x for x in out if any(v>0 for v in x["sourcePredictedColliderOverrunXYZ"])]
 # All eight? W16 source uses static primitive hull; check not pretend
 # Source-only code paths cannot prove PhysX, pivot orientation or Collider
 # interaction for every possible installed host.
 demo=srcs["PartSizeDemoEntry.cs"]
 scenes=text(producer_root,"PARTSIZE001_Demo.unity")
 prefab=text(producer_root,"PARTSIZE001_Demo.prefab")
 meta=text(producer_root,"PartSizeDemoEntry.cs.meta")
 guid=re.search(r"guid:\s*([0-9a-f]{32})",meta)
 if not guid or ("guid: "+guid.group(1)) not in scenes or ("guid: "+guid.group(1)) not in prefab:
  raise ValueError("scene/prefab do not reference actual demo component GUID")
 return {"schema":"B01-30-W16-PR286-DELTA-QA-1",
   "producerPr286ExactHead":os.environ.get("B01_PARTSIZE_PR286_HEAD","UNSET"),
   "producerPR286PinnedOnly":True,"codexSourcePR272PinnedOnly":True,
   "sourcesVerified":stamps,"pr286CatalogKinds":len(listed),
   "physicalUnmeasuredCanonicalKinds":sorted(set(listed)-(WIDE|{"Armor","Core"})),
   "records":out,"sourcePredictedOverrunRows":len(sourceOverruns),
   "externalBroadphase":{"callerSuppliesComplete":True,
      "callerCanPassEmptyListAndCompleteTrue":True,
      "recomputesExternalCandidatesWithPhysicsOverlap":broadphase_recomputed,
      "potentialFalseGreenIfCallerOmitsNeighbor":not broadphase_recomputed,
      "nativeNeighborMissingNegativeTest":("AUTHORED_NATIVE_NOT_RUN" if \
      "W17_LyingCompleteExternalInventoryCannotHideOverlappingBox" in srcs["PartSize001PlayModeTests.cs"] else "NOT_AUTHORED"),
      "nativePhysicsExecution":"NOT_RUN"},
   "demo":{"sceneAndPrefabReferToExactMonoScriptGUID":True,
      "sourceSpawnsThreeCasesOnly":True,
      "hasPhysicalWheelOrTrackCase":any(t in demo for t in ('Add(PartSizeBaselineCatalog.ForSourceKind("Wheel")',
       'Add(PartSizeBaselineCatalog.ForSourceKind("CrawlerTrack")')),
      "showsOccupiedCellWireframes":"NOT_AUTHORED","visiblePivotGizmos":"NOT_AUTHORED",
      "renderResult720And1080":"NOT_RUN","URPShaderFindRuntimeRetention":"NOT_VERIFIED"},
   "note":"SOURCE_PREDICTED AABB only, not native Unity Collider.bounds/PhysX, actual link/current Codex target NOT_RUN",
   "g2":"HOLD","partsizeSourceModified":False}
def main():
 a=argparse.ArgumentParser()
 a.add_argument("--producer",required=True)
 a.add_argument("--canonical",required=True)
 a.add_argument("--output",required=True)
 x=a.parse_args()
 data=evaluate(x.producer,x.canonical)
 Path(x.output).parent.mkdir(parents=True,exist_ok=True)
 Path(x.output).write_text(json.dumps(data,indent=2,sort_keys=True)+"\n","utf8")
 print(json.dumps({k:data[k] for k in ("pr286CatalogKinds","sourcePredictedOverrunRows","physicalUnmeasuredCanonicalKinds","externalBroadphase","demo")},sort_keys=True))
if __name__=="__main__":main()
