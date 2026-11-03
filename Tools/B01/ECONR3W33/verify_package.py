#!/usr/bin/env python3
"""W33 B01-22: compile original #915 R3 methods + original frozen #780 V7; report truth."""
from pathlib import Path
import json, re, subprocess
root=Path(__file__).resolve().parents[3]
here=Path(__file__).resolve().parent
meta=json.loads((here/"manifest.json").read_text())
def git(*a):
    return subprocess.check_output(["git",*a],cwd=root,text=True).strip()
assert meta["selected_producer_sha"]=="95060c1d254624355737fd2c48dd87cf79099ebe"
assert meta["product_patches"]==0
changed=set(git("diff","--name-only",meta["selected_producer_sha"],"HEAD").splitlines())
assert changed==set(meta["own_added_paths"]) and len(changed)==9,("NON_QA_CHANGE",sorted(changed))
for p,sha in meta["own_pinned_blobs"].items():
    assert git("rev-parse","HEAD:"+p)==sha,("OWN_SHA_CHANGED",p)
assert git("rev-parse","HEAD:Assets/R3/R3VehicleModel.cs")==meta["producer_original_r3_blob"]
assert git("rev-parse","HEAD:Assets/Tests/Editor/B0110W31EconomyParityTests.cs")==meta["producer_old_w31_test_blob"]
for p,oid in meta["imported_authoritative_b0110_w32"].items():
    assert git("rev-parse","HEAD:"+p)==oid
    assert git("rev-parse",meta["b0110_original_sha"]+":"+p)==oid
for p in meta["native_unity_meta"]:
    content=(root/p).read_text()
    matches=re.findall(r"^guid: ([0-9a-f]{32})$",content,re.M)
    assert len(matches)==1
    names=git("grep","-l","-F","guid: "+matches[0],"HEAD","--","Assets").splitlines()
    assert names==["HEAD:"+p],("GUID_COLLISION",p,names)
actual=(root/"Assets/R3/R3VehicleModel.cs").read_text()
def method(name):
    start=actual.index(name)
    begin=actual.index("{",start)
    depth=0
    for pos in range(begin,len(actual)):
        if actual[pos]=="{":depth+=1
        elif actual[pos]=="}":
            depth-=1
            if depth==0:return actual[start:pos+1]
    raise ValueError("Method not closed "+name)
methods=[method(s) for s in [
"public int RepairCost(R3PartState part)",
"public bool Repair(Vector3Int cell)",
"public bool RepairInventory(string id)",
"public bool EmergencyRepair()"]]
assert "part.Health = 1; return true;" in methods[1], "NOT_CURRENT_EXPECTED_PRODUCT_RED"
assert "Scrap-=cost;part.Health=1;return true;" in methods[2]
assert "foreach(var part in plan)part.Health=1;" in methods[3]
assert "((long)purchase*65L)/100L" in methods[0]
assert "Mathf.CeilToInt((1-part.Health)*purchase*0.35f)" in methods[0]
assert "restorationGate!=null&&!restorationGate(new[]{part})" in methods[1]
assert "restorationGate!=null&&!restorationGate(plan)" in methods[3]
assert "cost<=0" not in methods[2]
assert "RepairCost(p)<=0" not in methods[3]
wrong=[
 methods[0].replace("((long)purchase*65L)/100L","((long)purchase*35L)/100L"),
 methods[1].replace("part.Health = 1; return true;","part.Health = 0; return true;"),
 methods[2].replace("Scrap-=cost;part.Health=1;return true;","Scrap-=cost;part.Health=0;return true;"),
 methods[3].replace("foreach(var part in plan)part.Health=1;","foreach(var part in plan)part.Health=0;")
]
assert all(x!=y for x,y in zip(wrong,methods))
assert meta["native_G1"]=="NOT_RUN" and meta["owner_834"]=="PENDING"
v7=here/".source/ApprovedEconomyPolicyV7.cs"
assert v7.exists() and git("hash-object",str(v7))==meta["original_v7_blob"]
template=r"""// Generated from exact Git original #915 methods, NOT committed and NOT Unity.
using System;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.B01.Economy.ApprovedV7;
enum R3PartKind { Armor, Wheel, Gun }
enum R4WeaponKind { Basic }
struct Vector3Int { public int X; public Vector3Int(int x){X=x;} }
class R3PartState {
 public string Id=Guid.NewGuid().ToString("N");
 public R3PartKind Kind;public R4WeaponKind Weapon;public float Health;
 public Vector3Int Cell,RotationQuarterTurns;public int ContactMask=63;
 public R3PartState(R3PartKind kind,float hp,int cell=0){Kind=kind;Health=hp;Cell=new Vector3Int(cell);}
}
static class Mathf{public static int CeilToInt(float x)=>(int)Math.Ceiling(x);}
class R3VehicleModel {
 public int Scrap; public int NewPrice=5;public bool InvalidWeaponTypes,Neighbour=true;
 public readonly List<R3PartState> Parts=new List<R3PartState>();
 public readonly List<R3PartState> inventory=new List<R3PartState>();
 public Func<IReadOnlyList<R3PartState>,bool> restorationGate;
 public List<R3PartState> emergency;
 public int Price(R3PartKind kind,R4WeaponKind weapon)=>NewPrice;
 public static bool ValidEconomicPart(R3PartState p)=>p!=null&&!float.IsNaN(p.Health)&&!float.IsInfinity(p.Health)&&p.Health>=0&&p.Health<=1;
 public R3PartState At(Vector3Int cell)=>Parts.FirstOrDefault(p=>p.Cell.X==cell.X);
 public bool HasAliveExternalNeighbour(Vector3Int cell,R3PartKind kind,Vector3Int turns,int contacts)=>Neighbour;
 private List<R3PartState> EmergencyRepairPlan()=>emergency;
__METHODS__
}
class Program {
 static int count=0;
 static void Need(bool result,string msg) {if(!result)throw new Exception("ORIGINAL_915_ASSERTION_FAILED "+msg);count++;}
 static int Main(){
  try {
   for(int price=1;price<=1000;price++){
    var m=new R3VehicleModel{NewPrice=price};
    Need(ApprovedEconomyPolicyV7.TryPaidQuote(price,out int approved),"real V7 quote");
    Need(m.RepairCost(new R3PartState(R3PartKind.Armor,0f))==approved,"real R3 production quote parity "+price);
    Need(approved<price,"cheaper than new even at P1");
   }
   var high=new R3VehicleModel{NewPrice=int.MaxValue};
   Need(ApprovedEconomyPolicyV7.TryPaidQuote(int.MaxValue,out var hv7)
      && high.RepairCost(new R3PartState(R3PartKind.Armor,0f))==hv7,
      "CSharp9 intMax overflow safe quote");
   var inventory=new R3VehicleModel{NewPrice=1};
   var wheel=new R3PartState(R3PartKind.Wheel,0f);string wid=wheel.Id;
   inventory.inventory.Add(wheel);
   Need(inventory.RepairInventory(wid),"original PAID0 booking does go through");
   Need(inventory.Scrap==0&&wheel.Id==wid,"zero amount + owned ID stable");
   Need(wheel.Health==1f,"CURRENT PRODUCT DEFECT: PAID0 restores HP instantly, no next-win wait");
   Need(!inventory.RepairInventory(wid),"healthy repeat denied but NOT a durable queued ticket");
   var mounted=new R3VehicleModel{NewPrice=100,Scrap=100};
   var armor=new R3PartState(R3PartKind.Armor,0f,3);string aid=armor.Id;
   mounted.Parts.Add(armor);int gate=0;
   mounted.restorationGate=parts=>{gate++;return true;};
   Need(mounted.Repair(armor.Cell),"original valid mounted owned booking");
   Need(mounted.Scrap==35&&armor.Id==aid&&gate==1,"one 65 charge + single physical gate");
   Need(armor.Health==1f,"CURRENT PRODUCT DEFECT: PAID65 HP1 before authentic win");
   Need(!mounted.Repair(armor.Cell),"immediate-heal prevents repeat, NOT queued invoice");
   var partial=new R3VehicleModel{NewPrice=100,Scrap=100};
   var damaged=new R3PartState(R3PartKind.Armor,.5f,4);partial.Parts.Add(damaged);
   Need(partial.Repair(damaged.Cell)&&partial.Scrap==82&&damaged.Health==1f,
      "permitted immediate PARTIAL .5HP repair still costs 18");
   var deny=new R3VehicleModel{NewPrice=100,Scrap=100};
   var blocked=new R3PartState(R3PartKind.Armor,0f,9);deny.Parts.Add(blocked);
   deny.restorationGate=parts=>false;
   Need(!deny.Repair(blocked.Cell)&&deny.Scrap==100&&blocked.Health==0f,
      "real restoration gate rejects before money or HP mutation");
   var emergency=new R3VehicleModel{NewPrice=1};
   var engine=new R3PartState(R3PartKind.Wheel,0f,7);string eid=engine.Id;
   emergency.emergency=new List<R3PartState>{engine};
   emergency.restorationGate=parts=>true;
   Need(emergency.EmergencyRepair(),"original PAID0 emergency booking accepted");
   Need(emergency.Scrap==0&&engine.Id==eid,"emergency amount0 / original GUID");
   Need(engine.Health==1f,"CURRENT PRODUCT DEFECT: 0HP emergency immediately restores");
   Need(!emergency.EmergencyRepair(),"emergency healthy repeat refused");
   Console.WriteLine("W33_ORIGINAL_915_PRICE_V7_SOURCE_PASS checks="+count);
   Console.WriteLine("W33_ORIGINAL_915_NEXT_WIN_PRODUCT_RED inventory=IMMEDIATE_HP1 mounted=IMMEDIATE_HP1 emergency=IMMEDIATE_HP1 actual_signed_winids=NONE");
   return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
"""
out=here/".generated";out.mkdir(exist_ok=True)
(out/"OriginalR3Methods.cs").write_text(template.replace("__METHODS__","\n".join(methods)))
print("W33_ORIGINAL_915_SCOPE_PASS 0 producer product changes, 9 ADD-only QA, 2 exact B01-10 W32 native blobs; source-authenticated expected next-win RED; Unity G1 NOT_RUN")
