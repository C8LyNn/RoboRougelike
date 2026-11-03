#!/usr/bin/env python3
"""B01-41 W34: stage independent C#9 tests against BYTE-EXACT original #923 writer.
Expected verdict: true original writer ACCEPTS Core/Armor category mismatch (PRODUCT RED).
No Unity game, physical binder, synthetic WinId, or second Save Store is created.
"""
import hashlib,json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
PRODUCER="e6eb24b7a45a66a5fa5d039f59c80b7e4be4439b"
GAME="3bd9b159997924dcecb4e1cd1d41137896e5199e"
V7="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
ADMISSION="c9c19665a4f9385e6479f7151097deb797026ade"
SOURCE="Tools/B01/B01-62/P17W33R3World/"
PINS={
 "codec":(PRODUCER,SOURCE+"CanonicalR3WorldV1.cs","029021b32538c309fc0e85eb48ca5202c96e239c"),
 "store":(PRODUCER,"Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs","65e58e579becedb86f1162d9a511508cb77dfb7b"),
 "program":(PRODUCER,SOURCE+"Program.cs","1e4fc651089da8fa7a34ced3a2c0bb937887a644"),
 "project":(PRODUCER,SOURCE+"R3WorldActualOsQA.csproj","30b51b5ddb0c54f359571b48e314b88f280836bc"),
 "catalog":(GAME,"Assets/R3/R3VehicleCatalog.cs","f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584"),
 "v7":(V7,"Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773")
}
def git(*args):
 return subprocess.check_output(["git",*args],cwd=ROOT,stderr=subprocess.PIPE)
def blob(data):
 return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()
def read(k):
 ref,path,oid=PINS[k]
 b=git("show",ref+":"+path)
 if blob(b)!=oid:raise AssertionError("original git OID changed: "+k)
 return b.decode("utf-8-sig")
def verify_semantic(original):
 section=original.split("private bool Semantic(byte[] world,",1)[1].split("private static bool SameEconomicSection(",1)[0]
 required=("CanonicalR3WorldCodecV1.TryDecode","dynamicWorld.Parts.Count!=econ.Parts.Count",
           "economicIds.TryAdd(p.Id,p.Hp)",
           "economicIds.TryGetValue(p.Id,out float hp)||hp!=p.Hp")
 for s in required:
  if s not in section:raise AssertionError("original writer acceptance contract drift: "+s)
 if "Category" in section or "PurchaseScrap" in section:
  raise AssertionError("W34 category negative witness obsolete; re-evaluate fixed producer")
 return True
def make_project(source):
 if not source.count('<Compile Include="Program.cs"/>')==1:
  raise AssertionError("original project reference changed")
 if '<OutputType>Exe</OutputType>' not in source:
  raise AssertionError("original project no EXE")
 return (source.replace('<OutputType>Exe</OutputType>',
             '<OutputType>Exe</OutputType><StartupObject>CI41IndependentQA</StartupObject>')
              .replace('<Compile Include="Program.cs"/>',
                '<Compile Include="Program.cs"/><Compile Include="../../CI41/P17W34R3Schema/IndependentQA.cs"/>'))
def execute(out):
 output=Path(out)
 output.mkdir(parents=True,exist_ok=False)
 sources={x:read(x) for x in PINS}
 verify_semantic(sources["store"])
 if "public const int MaxParts=1024" not in sources["codec"] or (
   "world.Parts.Count>MaxParts" not in sources["codec"]):
  raise AssertionError("original R3 codec bounds changed")
 if "Category=p.Kind==3?" not in sources["program"]:
  raise AssertionError("original producer Core category derivation changed")
 if "string.IsNullOrWhiteSpace(p.Category)" not in sources["v7"]:
  raise AssertionError("original V7 category validation changed")
 project=make_project(sources["project"])
 path=ROOT/SOURCE/"CI41Independent.csproj"
 if path.exists():raise AssertionError("scratch independent project unexpectedly exists")
 path.write_text(project,encoding="utf-8")
 try:
  p=subprocess.run(["dotnet","run","--project",str(path),"-c","Release","-v","quiet"],
       cwd=ROOT,capture_output=True,text=True,encoding="utf-8",timeout=240)
  logfile=output/"original923-independent-compiled-actual-OS.log"
  logfile.write_text(p.stdout+"\nSTDERR:\n"+p.stderr,encoding="utf-8")
  text=p.stdout+p.stderr
  if p.returncode!=0 or (
      "CI41_W34_ORIGINAL_923_STORE_INCONSISTENT_CORE_CATEGORY_ACCEPTED_PRODUCT_RED" not in text
      or "CI41_W34_ORIGINAL_923_REAL_OS_SOURCE_CROSS_FIELD_TOTAL" not in text):
   raise AssertionError("actual original C#9 test failed: "+text[-2500:])
 finally:
  path.unlink(missing_ok=True)
 receipt={"original_producer":PRODUCER,"game":GAME,"economy":V7,"admission":ADMISSION,
          "original_source_oids":{k:v[2] for k,v in PINS.items()},
          "tested_kind":"SOURCE_CSHARP9_REAL_OS_ONE_PRODUCTION_WRITER_NOT_UNITY",
          "world_core_kind":"R3_PART_KIND_CORE_3",
          "economy_same_guid_same_hp_category":"Armor",
          "original_writer_actual_result":"ACCEPTED_INCONSISTENT_CATEGORY_PRODUCT_RED",
          "negative_hp_guid":"REJECTED_BEFORE_WRITE",
          "original_dynamic_20_83_139_producer_tests":"PR923_32_PER_OS_PRIOR",
          "formal_composite_G0":"HOLD","game_R3_bind":"NOT_INTEGRATED",
          "Unity_6000_3_14f1":"NOT_RUN","Windows_Player":"NOT_BUILT"}
 (output/"independent-provenance.json").write_text(json.dumps(receipt,indent=2,sort_keys=True),encoding="utf-8")
 print("CI41_W34_GIT_OID_EXACT_6_OF_6_PASS")
 print("CI41_W34_ORIGINAL_ONE_SAVE_WRITER_CSHARP9_COMPILED_AND_EXECUTED")
 print("CI41_W34_CATEGORY_CORE_ARMOR_MISMATCH_ACCEPTED_PRODUCT_RED")
 print("CI41_W34_ORIGINAL_HP_GUID_NEGATIVES_REJECTED")
 print("CI41_W34_NATIVE_UNITY_NOT_RUN_WINDOWS_PLAYER_NOT_BUILT")
 return receipt
if __name__=="__main__":
 if len(sys.argv)!=2:raise SystemExit("Usage: audit.py NEW_REPORT_DIR")
 execute(sys.argv[1])
