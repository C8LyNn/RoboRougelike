#!/usr/bin/env python3
"""Compile the exact existing-game R3 restoration identity helper and test it.
Source-isolated CLR tests, not Unity/PhysX/Windows Player.
"""
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT=Path(__file__).resolve().parents[4]
BASE="6b3a8409d31d8598c3c58facc1970933c722a086"
MODEL="Assets/R3/R3RestorationValidation.cs"
NATIVE="Assets/Tests/Editor/GarageEditFreedomTests.cs"
SELF="Tools/B01/B0123/W34/verify_restoration_identity.py"
WORKFLOW=".github/workflows/b01-23-w34-r3-restore-identity-source.yml"
PATHS={MODEL,NATIVE,SELF,WORKFLOW}

def git(*args):
    return subprocess.check_output(
        ["git",*args],cwd=ROOT,encoding="utf-8",text=True
    ).rstrip("\n")

changed=set(git("diff","--name-only",BASE,"HEAD").splitlines())
assert changed==PATHS,("scope mismatch",sorted(changed^PATHS))
source=(ROOT/MODEL).read_text(encoding="utf-8-sig").replace("\r\n","\n")
original=git("show",BASE+":"+MODEL).replace("\r\n","\n")
tests=(ROOT/NATIVE).read_text(encoding="utf-8-sig").replace("\r\n","\n")
old_tests=git("show",BASE+":"+NATIVE).replace("\r\n","\n")
signature="bool ValidCanonicalRestorationRequest(IReadOnlyList<R3PartState> requested)"

def extract(text,signature):
    p=text.index(signature)
    start=text.rfind("\n",0,p)+1
    opening=text.index("{",p)
    balance=0
    for i in range(opening,len(text)):
        if text[i]=="{":balance+=1
        elif text[i]=="}":
            balance-=1
            if balance==0:return text[start:i+1]
    raise AssertionError("unclosed source helper")

helper=extract(source,signature)
assert source.count(signature)==2, "helper and call expected"
assert source.count("if(!ValidCanonicalRestorationRequest(requested))return false;")==1
assert source.index("if(!ValidCanonicalRestorationRequest(requested))return false;")<source.index("var probes=new List<GameObject>()")
old_prefix="using System.Collections.Generic;"
assert source.startswith("using System;\n"+old_prefix)
method_start=source.rfind("        // Identity is part of the SAME canonical physical restoration gate.")
method_end=source.index("        // Restoration keeps canonical reserved cells/connectivity and payment.",method_start)
without_helper=source[:method_start]+source[method_end:]
after_check="""            // Fail closed before touching PhysX when the canonical model does not
            // own the exact requested part objects or the request repeats an ID.
            if(!ValidCanonicalRestorationRequest(requested))return false;
"""
assert without_helper.count(after_check)==1
restored=without_helper.replace(after_check,"",1).replace("using System;\n","",1)
assert restored.rstrip("\n")==original.rstrip("\n"),"W34 unexpectedly changed prior physical gate behavior"

marker="    // W34 source safety: a Save/repair caller may not request"
end="    [UnityTest] public IEnumerator ActualMissionEntryRejectsIncompleteVehicleAndAcceptsRestoredGun()"
nstart=tests.index(marker);nend=tests.index(end,nstart)
assert (tests[:nstart]+tests[nend:]).rstrip("\n")==old_tests.rstrip("\n"),"Original suite changed other than W34 new native test"
assert tests.count("ActualLiveR3RestorationRejectsForeignDuplicateAndInvalidOwnedReferences")==1

csproj="""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<LangVersion>9.0</LangVersion><Nullable>disable</Nullable>
<ImplicitUsings>disable</ImplicitUsings>
</PropertyGroup></Project>"""
template=r"""
using System;
using System.Collections.Generic;
using System.Linq;
class R3PartState {
 public string Id;
 public float Health;
 public R3PartState(string id,float hp=0f){Id=id;Health=hp;}
}
class OwnedR3 {
 public List<R3PartState> Parts=new List<R3PartState>();
}
class R3BuildValidation {
 public object assembly=new object();
 public OwnedR3 model=new OwnedR3();
__REAL_METHOD__
}
class Program {
 static int checks;
 static void Check(bool value,string message){
  if(!value)throw new Exception("ASSERT_RED "+message);
  checks++;
 }
 static void Main(){
  var owner=new R3BuildValidation();
  var a=new R3PartState("a",0f);
  var b=new R3PartState("b",.5f);
  owner.model.Parts.Add(a);owner.model.Parts.Add(b);
  Check(owner.ValidCanonicalRestorationRequest(new[]{a}),"owned valid positive");
  Check(owner.ValidCanonicalRestorationRequest(new[]{a,b}),"valid multistage positive");
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a,a}),"same-ref duplicate");
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a,new R3PartState("a")}),"same-GUID counterfeit");
  Check(!owner.ValidCanonicalRestorationRequest(new[]{new R3PartState("x")}),"foreign GUID");
  Check(!owner.ValidCanonicalRestorationRequest(null),"null request");
  Check(!owner.ValidCanonicalRestorationRequest(Array.Empty<R3PartState>()),"empty request");
  Check(!owner.ValidCanonicalRestorationRequest(new R3PartState[]{null}),"null part");
  var fraud=new R3PartState("a",.4f);
  owner.model.Parts.Add(fraud);
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a,fraud}),"different objects share identity");
  owner.model.Parts.Remove(fraud);
  var fromInventory=new R3PartState("inventory",0);
  Check(!owner.ValidCanonicalRestorationRequest(new[]{fromInventory}),"inventory not mounted");
  a.Health=float.NaN;
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a}),"NaN HP");
  a.Health=float.PositiveInfinity;
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a}),"positive infinite HP");
  a.Health=-.1f;
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a}),"negative HP");
  a.Health=1.1f;
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a}),"over-max HP");
  a.Health=0f;
  string old=a.Id;a.Id=" ";
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a}),"empty ID");
  a.Id=old;
  owner.assembly=null;
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a}),"missing live assembly");
  owner.assembly=new object();owner.model=null;
  Check(!owner.ValidCanonicalRestorationRequest(new[]{a}),"missing canonical model");
  Console.WriteLine("SOURCE_ONLY_R3_RESTORATION_IDENTITY_PASS="+checks);
 }
}
"""

def run(text,expect_green):
    with tempfile.TemporaryDirectory(prefix="b0123-w34-") as tmp:
        p=Path(tmp)
        (p/"Gate.csproj").write_text(csproj,encoding="utf-8")
        # The original helper is private: expose only in the test-only stub
        # to exercise its exact production body with explicitly modeled owner.
        shim=text.replace("        bool ValidCanonicalRestorationRequest(","        public bool ValidCanonicalRestorationRequest(",1)
        (p/"Program.cs").write_text(template.replace("__REAL_METHOD__",shim),encoding="utf-8")
        env=os.environ.copy()
        env["DOTNET_CLI_TELEMETRY_OPTOUT"]="1"
        env["DOTNET_NOLOGO"]="1"
        result=subprocess.run(["dotnet","run","--project",str(p/"Gate.csproj"),"-c","Release","--nologo"],
            capture_output=True,text=True,encoding="utf-8",env=env,timeout=120)
        if expect_green:
            assert result.returncode==0,(result.stdout[-3000:],result.stderr[-3000:])
            assert "SOURCE_ONLY_R3_RESTORATION_IDENTITY_PASS=" in result.stdout
            return result.stdout.strip().splitlines()[-1]
        assert result.returncode!=0 and "ASSERT_RED" in (result.stdout+result.stderr),(
            "unsafe source mutant not rejected",result.stdout[-2000:],result.stderr[-2000:])
        return "MUTANT_REJECTED"

original_result=run(helper,True)
mutations=[
    helper.replace("!mounted.Contains(part)||",""),
    helper.replace("!ids.Add(part.Id)||",""),
    helper.replace("float.IsNaN(part.Health)||",""),
]
assert len(set(mutations))==3 and all(m!=helper for m in mutations)
for bad in mutations:run(bad,False)

print(json.dumps({
 "kind":"ORIGINAL_R3_IDENTITY_HELPER_SOURCE_ISOLATED_NOT_UNITY",
 "base":BASE,"head":git("rev-parse","HEAD"),
 "source_result":original_result,"unsafe_mutants_rejected":3,
 "paths":sorted(changed),"Unity_EditMode":"NOT_RUN",
 "Unity_PlayMode":"NOT_RUN","Windows_Player":"NOT_BUILT"},sort_keys=True))
