#!/usr/bin/env python3
"""Exact selected B01-62 W33 one-writer dynamic R3 payload gate."""
import subprocess,json,hashlib,sys
from pathlib import Path
BASE="a0282f2de0250d2a8fe421bbe182b4cb6fab2a11"
GAME="3bd9b159997924dcecb4e1cd1d41137896e5199e"
V7="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
ADMISSION="c9c19665a4f9385e6479f7151097deb797026ade"
ROOT="Tools/B01/B01-62/P17W33R3World/"
OWN={"CanonicalR3WorldV1.cs","Program.cs","OriginalR3FactoryShim.cs",
     "R3WorldActualOsQA.csproj","source_guard.py","README.md","manifest.json"}
STORE="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs"
OLD=".github/workflows/b01-62-w31-frozen-atomic-pair.yml"
PROJECTS={
"Tools/B01/B01-62/P17W30AdmissionV3/W28OriginalRegression.csproj",
"Tools/B01/B01-62/P17W30AdmissionV3/W29ColdRegression.csproj",
"Tools/B01/B01-62/P17W30AdmissionV3/W30AdmissionQA.csproj",
"Tools/B01/B01-62/P17W30AdmissionV3/W30UnityAbi.csproj",
"Tools/B01/B01-62/P17W31AtomicPair/AtomicPairQA.csproj"}
WF=".github/workflows/b01-62-w33-r3-dynamic-same-writer.yml"
def git(*args):
 return subprocess.check_output(["git",*args],stderr=subprocess.PIPE).decode().strip()
def check_ref(ref,path,sha):
 if git("rev-parse",ref+":"+path)!=sha:
  raise AssertionError("ORIGINAL_GIT_BLOB_MISMATCH "+path)
def run(out):
 if Path(out).exists():raise AssertionError("RECEIPT_EXISTS")
 expected={STORE,OLD,WF}|PROJECTS|{ROOT+x for x in OWN}
 actual=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 if actual!=expected:raise AssertionError("NOT_EXACT_15_OWN_PATHS "+str(actual^expected))
 check_ref(GAME,"Assets/R3/R3VehicleCatalog.cs","f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584")
 check_ref(GAME,"Assets/R3/R3VehicleModel.cs","1d0f378dc598e08b3d7c16975f419d84231511bb")
 check_ref(GAME,"Assets/R4/R4WeaponDefinition.cs","502f6d7f70d4e8078260de632ab181f244a4d730")
 check_ref(BASE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a")
 check_ref(V7,"Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773")
 check_ref(ADMISSION,"Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs","59ad0f42cec5385cc132c1a3917cffd8ebf65d2c")
 original=(Path(ROOT)/".source/R3VehicleCatalog.cs").read_bytes()
 sha=hashlib.sha1(b"blob "+str(len(original)).encode()+b"\0"+original).hexdigest()
 if sha!="f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584":
  raise AssertionError("R3_FACTORY_SOURCE_CHANGED")
 frozen="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
 if git("rev-parse",BASE+":"+frozen)!=git("rev-parse","HEAD:"+frozen):
  raise AssertionError("ORIGINAL_556_FROZEN_CHANGED")
 if "  pull_request:" in Path(OLD).read_text():
  raise AssertionError("INHERITED_W31_EXCLUSIVE_GUARD_TRIGGER")
 st=Path(STORE).read_text()
 for token in ("CanonicalR3WorldCodecV1.TryDecode",
               "CanonicalR3WorldCodecV1.IsR3World",
               "p.Id,out float hp","hp!=p.Hp",
               "File.Replace(staging,authoritative,null)",
               "EconomyV7AdmissionW29.TryEncodeForCanonicalOwner",
               "EconomyV7AdmissionW29.TryDecodeForCanonicalOwner",
               "ColdRestore.TryStage(world",
               "if(existing!=expectedGeneration)return WholeGameResult.Stale;"):
  if token not in st:raise AssertionError("SAME_WRITER_ORIGINAL_INVARIANT "+token)
 if "CanonicalR3WorldCodecV1.IsR3World(priorWorld)!=" not in st:
  raise AssertionError("UNAUTHORIZED_SILENT_SCHEMA_MIGRATION")
 codec=Path(ROOT+"CanonicalR3WorldV1.cs").read_text()
 for token in ('Magic="B01R3W01"',"MaxParts=1024","MaxBytes=3*1024*1024",
               "p.PhysicalId","p.OwnerPhysicalId","mountedOwner=0",
               "p.Weapon>8","p.Kind!=2&&p.Weapon!=0","p.DefenseRechargeAge",
               "float.IsPositiveInfinity","PartValid","!ids.Add",
               "stream.Position!=stream.Length"):
  if token not in codec:raise AssertionError("MISSING_DYNAMIC_CODEC_GUARD "+token)
 for p in PROJECTS:
  if 'P17W33R3World/CanonicalR3WorldV1.cs' not in Path(p).read_text():
   raise AssertionError("INHERITED_PRODUCTION_PROJECT_MISSING_CODEC "+p)
 for p in ("Assets/R3/R3VehicleCatalog.cs","Assets/R3/R3VehicleModel.cs",
           "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"):
  if p in actual:raise AssertionError("FOREIGN_PRODUCT_EDIT "+p)
 data={"base_896":BASE,"selected_game_877":GAME,"v7":V7,
       "admission_855":ADMISSION,"paths":sorted(expected),
       "writer_count":1,"outer_format":"B0162G02",
       "dynamic_world_format":"B01R3W01","frozen_world":"B01SV13! unchanged",
       "owner_schema_approval":"PENDING","game_r3_physical_stage":"NOT_IMPLEMENTED",
       "source_os_suite":"W28 9 + W29 6 + W30 4 + W31 2 + W33 11 + W34 4",
       "native_unity":"NOT_RUN","released":False}
 b=(json.dumps(data,indent=2,sort_keys=True)+"\n").encode()
 Path(out).write_bytes(b)
 print("W33_ONE_WRITER_P17_EXACT_DIFF_15_PATHS=PASS")
 print("W33_ORIGINAL_R3_556_V7_855_IMMUTABLE_SOURCE=PASS")
 print("W33_GUARD_SHA256="+hashlib.sha256(b).hexdigest())
if __name__=="__main__":run(sys.argv[1])
