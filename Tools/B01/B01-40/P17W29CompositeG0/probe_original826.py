#!/usr/bin/env python3
"""B01-40 W29: compile and EXECUTE exact original #826 V3 + #780/#729/#556/#510.
No fake Unity; real .NET standard API probe and real OS original disk tests.
"""
import argparse,hashlib,json,subprocess,tempfile
from pathlib import Path
ORIGINAL={
 "WholeGameEconomySlotV3.cs":("99fb08619c78b3834bb4c0450499babf991f218a","Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs"),
 "CanonicalWholeGameSessionV3.cs":("99fb08619c78b3834bb4c0450499babf991f218a","Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs"),
 "Program.cs":("99fb08619c78b3834bb4c0450499babf991f218a","Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/Program.cs"),
 "ApprovedEconomyPolicyV7.cs":("b4df10a7ad7e6c758f8331013f6b216b3a3689a3","Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"),
 "CanonicalEconomySectionV7.cs":("b4df10a7ad7e6c758f8331013f6b216b3a3689a3","Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs"),
 "CanonicalWorldGateway.cs":("b8ca29c7707cd6b4338d0b9772d74dee676d49d2","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"),
 "CanonicalWorldSlotLease.cs":("b8ca29c7707cd6b4338d0b9772d74dee676d49d2","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs"),
 "DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"),
 "DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"),
 "SecureRigSpec.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs")
}
def execute(args,work=None):
 run=subprocess.run(args,cwd=work,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,errors="replace",text=True)
 return run.returncode,run.stdout
def get(ref,path):
 result=subprocess.run(["git","show",ref+":"+path],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 if result.returncode:raise RuntimeError("SOURCE_MISSING_OR_WRONG_REF "+ref+" "+path)
 return result.stdout
def csproj(target,exe):
 fields=f'<TargetFramework>{target}</TargetFramework><LangVersion>9.0</LangVersion><Nullable>enable</Nullable><NoWarn>CS8618;CS8625</NoWarn><TreatWarningsAsErrors>true</TreatWarningsAsErrors>'
 return f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>{fields}<OutputType>{"Exe" if exe else "Library"}</OutputType></PropertyGroup></Project>\n'
def main():
 p=argparse.ArgumentParser();p.add_argument("--out",required=True);arg=p.parse_args()
 out=Path(arg.out);out.mkdir(parents=True,exist_ok=True)
 originals={name:get(commit,path) for name,(commit,path) in ORIGINAL.items()}
 baseline={name:{"source_git_sha":ORIGINAL[name][0],"original_path":ORIGINAL[name][1],
                 "sha256":hashlib.sha256(data).hexdigest(),"bytes":len(data)}
           for name,data in originals.items()}
 (out/"original-source-sha256.json").write_bytes((json.dumps(baseline,sort_keys=True,indent=2)+"\n").encode())
 with tempfile.TemporaryDirectory(prefix="B01-W29-826-only-") as temp:
  for framework,exe in (("netstandard2.1",False),("net8.0",True)):
   where=Path(temp)/framework;where.mkdir()
   for name,data in originals.items():
    if not exe and name=="Program.cs":continue
    (where/name).write_bytes(data)
   (where/"ExactOriginalComposite.csproj").write_bytes(csproj(framework,exe).encode())
   code,log=execute(["dotnet","build","ExactOriginalComposite.csproj","-c","Release","--verbosity","quiet"],where)
   (out/(framework.replace(".","_")+"-build.log")).write_text(log,encoding="utf-8",newline="\n")
   if code:raise RuntimeError("EXACT_ORIGINAL_826_COMPOSITE_"+framework+"_BUILD_FAIL: "+log[-2100:])
   print("B0140_W29_ORIGINAL_826_780_729_556_510_"+framework+"_C9_COMPILE_PASS")
   if exe:
    code,log=execute(["dotnet","run","--no-build","-c","Release","--project","ExactOriginalComposite.csproj"],where)
    (out/"original-9-physical-disk-tests.log").write_text(log,encoding="utf-8",newline="\n")
    if code or " PASS 9 FAIL 0" not in log:
     raise RuntimeError("EXACT_ORIGINAL_826_REAL_DISK_TESTS_RED "+log[-2200:])
    print("B0140_W29_ORIGINAL_V3_9_REAL_DISK_OS_SCENARIOS_PASS")
 print("B0140_W29_EXECUTED_SOURCE_ONLY_REAL_UNITY_G1_NOT_RUN")
if __name__=="__main__":main()
