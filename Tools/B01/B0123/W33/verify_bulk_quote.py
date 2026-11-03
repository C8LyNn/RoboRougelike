#!/usr/bin/env python3
"""Compile/run the EXACT R3 bulk-quote getter with a declared .NET8 game stub.
This is NOT an Editor/PhysX/Player test. Original game native tests are separate.
"""
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT=Path(__file__).resolve().parents[4]
BASE="3bd9b159997924dcecb4e1cd1d41137896e5199e"
MODEL="Assets/R3/R3VehicleModel.cs"
TESTS="Assets/Tests/Editor/GarageEditFreedomTests.cs"
SELF="Tools/B01/B0123/W33/verify_bulk_quote.py"
WORKFLOW=".github/workflows/b01-23-w33-bulk-quote-source.yml"
ALLOWED={MODEL,TESTS,SELF,WORKFLOW}
def git(*args):
    return subprocess.check_output(["git",*args],cwd=ROOT,text=True,encoding="utf-8").strip()

paths=set(git("diff","--name-only",BASE,"HEAD").splitlines())
assert paths==ALLOWED,("foreign game scope",sorted(paths^ALLOWED))
source=(ROOT/MODEL).read_text(encoding="utf-8-sig")
tests=(ROOT/TESTS).read_text(encoding="utf-8-sig")
original=git("show",BASE+":"+MODEL)
old_tests=git("show",BASE+":"+TESTS)
needle="        public int AffordableRepairCost"
def property_text(body):
    at=body.index(needle)
    first=body.index("{",at)
    depth=0
    for j in range(first,len(body)):
        if body[j]=="{":depth+=1
        elif body[j]=="}":
            depth-=1
            if depth==0:return body[at:j+1]
    raise AssertionError("unclosed game getter")
getter=property_text(source)
old_getter=property_text(original)
assert original.replace(old_getter,getter).replace("\r\n","\n").rstrip("\n")==source.replace("\r\n","\n").rstrip("\n"),"original product changed outside R3 getter"
test_add_start="    // W33: bulk price preview MUST exercise"
test_add_end="    [Test] public void ModelCanRemoveLastGunAndEveryWheelButCannotDeploy()"
i=tests.index(test_add_start);j=tests.index(test_add_end,i)
assert (tests[:i]+tests[j:]).rstrip("\r\n")==old_tests.rstrip("\r\n"),"original test suite changed outside W33 methods"
assert tests.count("BulkQuotePhysicalRejectionDoesNotInventAvailableRepairCost")==1
assert tests.count("BulkQuoteRechecksCumulativeRealOwnedRestorationBatch")==1
assert tests.count("BulkQuoteKeepsPartialHpRepairsImmediateAndOutsideRestoreGate")==1
assert "preview.SetRestorationGate(requested=>" in getter
assert "return batch.Count>0&&restorationGate(batch);" in getter
assert "return Scrap-preview.Scrap;" in getter
assert "new Dictionary<string,R3PartState>(StringComparer.Ordinal)" in getter

csproj='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>9.0</LangVersion><Nullable>disable</Nullable></PropertyGroup></Project>'
stub=r"""
using System;
using System.Collections.Generic;
using System.Linq;
class R3PartState {
 public string Id,Tag; public float Health;
 public R3PartState(string tag,float hp,string id){Tag=tag;Health=hp;Id=id;}
 public R3PartState Copy()=>new R3PartState(Tag,Health,Id);
}
class R3VehicleModel {
 public int Scrap=50;
 public List<R3PartState> Parts=new List<R3PartState>();
 private Func<IReadOnlyList<R3PartState>,bool> restorationGate;
 internal void SetRestorationGate(Func<IReadOnlyList<R3PartState>,bool> gate){restorationGate=gate;}
 public R3VehicleModel DeepCopy(){
  var copy=new R3VehicleModel{Scrap=Scrap};
  copy.Parts.AddRange(Parts.Select(p=>p.Copy()));
  return copy;
 }
 public int RepairAffordable(){
  bool changed;int total=0;
  do{
   changed=false;
   foreach(var p in Parts){
    if(p.Health>=1f)continue;
    if(p.Tag=="second"&&!Parts.Any(x=>x.Tag=="first"&&x.Health>0f))continue;
    if(p.Health<=0f&&restorationGate!=null&&!restorationGate(new[]{p}))continue;
    if(Scrap<4)continue;
    Scrap-=4;p.Health=1;total++;changed=true;
   }
  }while(changed);
  return total;
 }
__EXACT_R3_GETTER__
}
class Program {
 static int n;
 static void Ok(bool condition,string label){if(!condition)throw new Exception("ASSERT_RED "+label);n++;}
 static R3VehicleModel Model(params R3PartState[] parts){
  var m=new R3VehicleModel();m.Parts.AddRange(parts);return m;
 }
 static void Main(){
  var blocked=Model(new R3PartState("first",0f,"one"));
  var realBlocked=blocked.Parts[0];int visits=0;
  blocked.SetRestorationGate(batch=>{visits++;Ok(batch.Count==1&&ReferenceEquals(batch[0],realBlocked),"original identity");return false;});
  Ok(blocked.AffordableRepairCost==0,"blocked quote zero");
  Ok(visits>0&&realBlocked.Health==0f&&blocked.Scrap==50,"blocked getter no owner mutation");

  var first=new R3PartState("first",0f,"a");var second=new R3PartState("second",0f,"b");
  var staged=Model(first,second);int maxBatch=0;
  staged.SetRestorationGate(batch=>{
    maxBatch=Math.Max(maxBatch,batch.Count);
    Ok(batch.All(p=>ReferenceEquals(p,first)||ReferenceEquals(p,second)),"canonical part references");
    return batch.Count==1;
  });
  Ok(staged.AffordableRepairCost==4,"full prospective batch denies second repair");
  Ok(maxBatch==2,"recheck first and second geometry together");
  Ok(first.Health==0f&&second.Health==0f&&staged.Scrap==50,"staged read only");
  var partialPart=new R3PartState("first",.5f,"p");
  var partial=Model(partialPart);int partialVisits=0;
  partial.SetRestorationGate(batch=>{partialVisits++;return false;});
  Ok(partial.AffordableRepairCost==4,"partial HP retained");
  Ok(partialVisits==0&&partialPart.Health==.5f&&partial.Scrap==50,"partial skips new geometry authority");
  var unbound=Model(new R3PartState("first",0f,"l"));
  Ok(unbound.AffordableRepairCost==4,"source model without live actor preserves existing quote");
  var corrupt=Model(new R3PartState("first",0f,"dup"),new R3PartState("second",0f,"dup"));
  corrupt.SetRestorationGate(batch=>true);
  Ok(corrupt.AffordableRepairCost==0,"duplicate live IDs fail closed");
  Console.WriteLine("SOURCE_ISOLATED_R3_BULK_QUOTE_PASS="+n);
 }
}
"""

def execute(impl,pass_expected=True):
    with tempfile.TemporaryDirectory(prefix="b0123-w33-") as tmp:
        p=Path(tmp)
        (p/"Bulk.csproj").write_text(csproj,encoding="utf-8")
        (p/"Program.cs").write_text(stub.replace("__EXACT_R3_GETTER__",impl),encoding="utf-8")
        env=os.environ.copy();env["DOTNET_NOLOGO"]="1";env["DOTNET_CLI_TELEMETRY_OPTOUT"]="1"
        result=subprocess.run(["dotnet","run","--project",str(p/"Bulk.csproj"),"-c","Release","--nologo"],
            capture_output=True,text=True,encoding="utf-8",env=env,timeout=120)
        if pass_expected:
            assert result.returncode==0,(result.stdout[-2600:],result.stderr[-2600:])
            assert "SOURCE_ISOLATED_R3_BULK_QUOTE_PASS=" in result.stdout,result.stdout[-1000:]
            return result.stdout.strip().splitlines()[-1]
        assert result.returncode!=0 and "ASSERT_RED" in result.stdout+result.stderr,(
            "unsafe mutant not rejected",result.returncode,result.stdout[-1800:],result.stderr[-1800:])
        return "REJECTED"

real=execute(getter)
mutants=[
 getter.replace("if(restorationGate!=null)","if(false)"),
 getter.replace("seen.Add(already.Id))batch.Add(source);","false)batch.Add(source);"),
 getter.replace("return batch.Count>0&&restorationGate(batch);","return false;")
]
assert len(set(mutants))==3 and all(m!=getter for m in mutants)
for candidate in mutants:execute(candidate,False)
print(json.dumps({"tier":"ORIGINAL_R3_GETTER_COMPILED_IN_EXPLICIT_TEST_STUB_NOT_UNITY",
 "base":BASE,"head":git("rev-parse","HEAD"),"tested":real,
 "source_mutants_rejected":3,"native_Unity":"NOT_RUN","Windows_Player":"NOT_BUILT",
 "paths":sorted(paths)},sort_keys=True))
