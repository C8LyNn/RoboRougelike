#!/usr/bin/env python3
"""CI41 W33: execute actual #915/#910 original R3 methods under explicit non-Unity C#9 shim.
A green gate means PRODUCT NEXT-WIN RED was found, not gameplay acceptance.
"""
import ast,hashlib,json,os,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
BASE="3bd9b159997924dcecb4e1cd1d41137896e5199e"
SAVE="a0282f2de0250d2a8fe421bbe182b4cb6fab2a11"
V7="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
CANDIDATES={
 "915":("95060c1d254624355737fd2c48dd87cf79099ebe","7ac34f5c5dca00f51ffe6b3b79ec0e2bea114ba2"),
 "910":("28841161afb56ad78ec8506121aa12fde59bf984","6e84dded592bc8643b4377fb04873eea8db6aeb3")}
PINS={
 "r3_915":(CANDIDATES["915"][0],"Assets/R3/R3VehicleModel.cs",CANDIDATES["915"][1]),
 "r3_910":(CANDIDATES["910"][0],"Assets/R3/R3VehicleModel.cs",CANDIDATES["910"][1]),
 "host":(CANDIDATES["915"][0],"Tools/B01/B0123/W32/verify_economy.py","b8fcc8b0c6e99afd0f7f504c5425d336915708b1"),
 "policy":(V7,"Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs","53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
 "catalog":(BASE,"Assets/R3/R3VehicleCatalog.cs","f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584"),
 "world":(SAVE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs","b52e511c370f5fca56c74029050844dc8cbb7f9a"),
 "writer":(SAVE,"Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs","e6e2068e4a59d6240d8516ae0899541263a6d5c6")}
SIGNATURES=("public int RepairCost(R3PartState part)","public bool Repair(Vector3Int cell)","public bool RepairInventory(string id)","public bool EmergencyRepair()")
PROJECT='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><LangVersion>9.0</LangVersion></PropertyGroup></Project>'
ORACLE=r"""
class Program {
 static int count=0;
 static void Check(bool ok,string name){if(!ok)throw new Exception("HARNESS_SOURCE_CHANGED: "+name);count++;}
 public static void Main(){
  int[] purchase={1,5,15,30,100,20};
  int[] quote={0,3,9,19,65,13};
  for(int i=0;i<purchase.Length;i++){
   var model=new R3VehicleModel{NewPrice=purchase[i]};
   Check(model.RepairCost(new R3PartState(R3PartKind.Armor,0f))==quote[i],"floor65");
  }
  var partial=new R3VehicleModel{NewPrice=20};
  Check(partial.RepairCost(new R3PartState(R3PartKind.Armor,.5f))==4,"partialHP unchanged");
  var mounted=new R3VehicleModel{NewPrice=1};
  var p=new R3PartState(R3PartKind.Armor,0f,12);string id=p.Id;
  mounted.Parts.Add(p);int physical=0;mounted.restorationGate=parts=>{physical++;return true;};
  Check(mounted.Repair(p.Cell)&&p.Health==1f&&p.Id==id&&mounted.Scrap==0&&physical==1,
       "mounted paid0 immediately sets HP1 without any genuine Win");
  var inv=new R3VehicleModel{NewPrice=1};
  var iPart=new R3PartState(R3PartKind.Wheel,0f);string iid=iPart.Id;
  inv.inventory.Add(iPart);
  Check(inv.RepairInventory(iid)&&iPart.Health==1f&&inv.Scrap==0&&iPart.Id==iid,
       "inventory paid0 immediately HP1 without genuine Win");
  var emer=new R3VehicleModel{NewPrice=1};
  var ePart=new R3PartState(R3PartKind.Wheel,0f);string eid=ePart.Id;
  emer.emergency=new List<R3PartState>{ePart};emer.restorationGate=parts=>true;
  Check(emer.EmergencyRepair()&&ePart.Health==1f&&emer.Scrap==0&&ePart.Id==eid,
       "emergency paid0 immediately HP1 without genuine Win");
  var denied=new R3VehicleModel{NewPrice=1};
  var d=new R3PartState(R3PartKind.Armor,0f,5);denied.Parts.Add(d);
  denied.restorationGate=parts=>false;
  Check(!denied.Repair(d.Cell)&&denied.Scrap==0&&d.Health==0f,"physical fail-closed");
  Console.WriteLine("CI41_W33_ORIGINAL_CSHARP9_PRICE65_PASS_AND_NEXTWIN_EARLY_HP_PRODUCT_RED checks="+count);
 }
}
"""
def blob(x):
 return hashlib.sha1(b"blob "+str(len(x)).encode()+b"\0"+x).hexdigest()
def safe(path):
 if not path or path.startswith("/") or "\\" in path or "\0" in path or ".." in Path(path).parts:
  raise ValueError("unsafe Git path")
 return path
def git(*a):return subprocess.check_output(["git",*a],cwd=ROOT,stderr=subprocess.PIPE)
def read(key):
 commit,path,expected=PINS[key]
 if not re.fullmatch(r"[0-9a-f]{40}",commit) or not re.fullmatch(r"[0-9a-f]{40}",expected):
  raise AssertionError("non immutable git ref")
 raw=git("show",commit+":"+safe(path))
 if blob(raw)!=expected:raise AssertionError("original Git source bytes altered: "+key)
 return raw.decode("utf-8-sig")
def method(src,signature):
 st=src.find(signature)
 if st<0:raise AssertionError("original method absent: "+signature)
 op=src.find("{",st);depth=0
 for j in range(op,len(src)):
  if src[j]=="{":depth+=1
  elif src[j]=="}":
   depth-=1
   if depth==0:return src[st:j+1]
 raise AssertionError("unclosed original method")
def inspect(src):
 methods=[method(src,s) for s in SIGNATURES]
 if "purchase*65L" not in methods[0] or "100L" not in methods[0]:
  raise AssertionError("new quote is not V7 65 floor")
 if "Mathf.CeilToInt((1-part.Health)*purchase*0.35f)" not in methods[0]:
  raise AssertionError("partial repair policy was changed")
 for m in methods[1:]:
  if not re.search(r"\bpart\.Health\s*=\s*1\s*;",m):raise AssertionError("temporal source witness changed: new proof required")
 if "cost<=0" in methods[2] or "price <= 0" in methods[1]:
  raise AssertionError("paid0 rejected")
 return methods
def policy_check(src):
 for s in ("scrap=(int)((long)purchase*65L/100L)","TryReserveDestroyed(",
           "AfterWins=x.Wins","TryStageTrustedVictory(","t.AfterWins>=x.Wins+1",
           "p.Hp=1;p.ReservationId=null"):
  if s not in src:raise AssertionError("owner approved original V7 drift: "+s)
 return True
def schema_check(world,catalog,writer):
 if (not re.search(r"\bpublic\s+const\s+int\s+Slots\s*=\s*2\s*;",world) or
     any(s not in world for s in ("s.Parts.Length!=FrozenPartCatalog.Slots",
                                  "if(!SnapshotRules.Valid(snap))throw new InvalidDataException"))):
  raise AssertionError("frozen two-slot codec changed")
 if "ColdRestore.TryStage(world,out ColdRestore? staged)" not in writer:
  raise AssertionError("sole Save semantic binding changed")
 if "int wheels = light ? 2 : 6;" not in catalog or "for (int y = 0; y < layers; y++)" not in catalog:
  raise AssertionError("original vehicle factory changed")
 return {"Light":20,"Standard":83,"Heavy":139}
def host_prefix(producer):
 node=ast.parse(producer);original=None
 for n in node.body:
  if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=="stub" for t in n.targets):
   original=ast.literal_eval(n.value)
 if not isinstance(original,str) or "__EXTRACTED__" not in original or "class Program {" not in original:
  raise AssertionError("original B01-23 C#9 stub changed")
 return original.split("class Program {")[0]
def execute(out):
 out=Path(out);out.mkdir(parents=True,exist_ok=False)
 policy_check(read("policy"))
 parts=schema_check(read("world"),read("catalog"),read("writer"))
 prefix=host_prefix(read("host"))
 results={}
 for key in ("915","910"):
  body="\n".join(inspect(read("r3_"+key)))
  d=out/("original-"+key);d.mkdir()
  (d/"Original.csproj").write_text(PROJECT,encoding="utf-8")
  (d/"Program.cs").write_text(prefix.replace("__EXTRACTED__",body)+ORACLE,encoding="utf-8")
  cmd=["dotnet","run","--project",str(d/"Original.csproj"),"-c","Release","--nologo"]
  process=subprocess.run(cmd,cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,
                         encoding="utf-8",timeout=180)
  (out/("original-"+key+"-compiler-and-methods.log")).write_text(process.stdout,encoding="utf-8")
  if process.returncode or "CI41_W33_ORIGINAL_CSHARP9_PRICE65_PASS_AND_NEXTWIN_EARLY_HP_PRODUCT_RED" not in process.stdout:
   raise AssertionError("original C#9 executable source witness failed: "+process.stdout[-2600:])
  results[key]={"head":CANDIDATES[key][0],"original_blob":CANDIDATES[key][1],
                "compiler":"ACTUAL_DOTNET8_CSHARP9_PASS",
                "price65":"PASS","paid0":"PASS",
                "temporal":"PRODUCT_RED_MOUNTED_INVENTORY_EMERGENCY_HP1_BEFORE_NEXT_AUTHENTIC_WIN",
                "log_sha256":hashlib.sha256(process.stdout.encode()).hexdigest()}
 report={"kind":"CI41_W33_ORIGINAL_SOURCE_NEGATIVE_WITNESS_NOT_UNITY",
         "source_base_game":BASE,"one_save_writer_896":SAVE,"owner_v7_policy":V7,
         "original_sha_pins":{key:oid for key,(_,_,oid) in PINS.items()},
         "original_vehicles":parts,"original_frozen_world_slots":2,
         "save_R3_dynamic_schema":"PRODUCT_RED_ISSUE_913",
         "two_candidate_single_r3_file_conflict":"DO_NOT_INSTALL_BOTH",
         "next_win_deferred":"PRODUCT_RED_ON_BOTH_CANDIDATES",
         "actual_unity":"NOT_RUN","windows_x64_player":"NOT_BUILT",
         "results":results}
 (out/"w33-receipt.json").write_text(json.dumps(report,sort_keys=True,indent=2),encoding="utf-8")
 print("CI41_W33_EXACT_ORIGINAL_GIT_BLOB_PINS 7/7 PASS")
 print("CI41_W33_REAL_ORIGINAL_METHOD_CSHARP9_BOTH_CANDIDATES 2/2 PASS")
 print("CI41_W33_V7_FLOOR65_PAID0_SOURCE_PASS_BUT_NEXTWIN_PRODUCT_RED")
 print("CI41_W33_REAL_R3_20_83_139_VS_FROZEN556_2_SLOTS_PRODUCT_RED")
 print("CI41_W33_ACTUAL_UNITY_G1_NOT_RUN_WINDOWS_PLAYER_NOT_BUILT")
 return report
if __name__=="__main__":
 if len(sys.argv)!=2:raise SystemExit("usage: audit.py FRESH_OUTPUT_DIR")
 execute(sys.argv[1])
