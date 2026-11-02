#!/usr/bin/env python3
"""Compile ORIGINAL exact-source composite as net8 and netstandard2.1 API boundary."""
import argparse,hashlib,json,subprocess,tempfile
from pathlib import Path
REFS={
 "WholeGameEconomySlotV2.cs":("2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
   "Tools/B01/B01-62/P17W25WholeGameEconomyV7/WholeGameEconomySlotV2.cs"),
 "CanonicalWholeGameSessionV2.cs":("2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
   "Tools/B01/B01-62/P17W25WholeGameEconomyV7/CanonicalWholeGameSessionV2.cs"),
 "ApprovedEconomyPolicyV7.cs":("b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
   "Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"),
 "CanonicalEconomySectionV7.cs":("b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
   "Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs"),
 "CanonicalWorldGateway.cs":("b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
   "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"),
 "CanonicalWorldSlotLease.cs":("b8ca29c7707cd6b4338d0b9772d74dee676d49d2",
   "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs"),
 "DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57",
   "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"),
 "DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431",
   "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"),
 "SecureRigSpec.cs":("d94400d95d13db7f27e1533df05c9292d5380431",
   "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs")
}
def git(commit,path):
 p=subprocess.run(["git","show",commit+":"+path],capture_output=True)
 if p.returncode:raise RuntimeError("MISSING_GIT_SOURCE "+path+" "+p.stderr.decode(errors="replace")[-200:])
 return p.stdout
def project(framework):
 return ('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'+
  '<TargetFramework>'+framework+'</TargetFramework>'+
  '<LangVersion>9.0</LangVersion><Nullable>enable</Nullable>'+
  '<NoWarn>CS8618;CS8625</NoWarn><TreatWarningsAsErrors>false</TreatWarningsAsErrors>'+
  '</PropertyGroup></Project>\n')
def run():
 a=argparse.ArgumentParser();a.add_argument("--out",required=True);opt=a.parse_args()
 out=Path(opt.out);out.mkdir(parents=True,exist_ok=True)
 provenance={}
 with tempfile.TemporaryDirectory(prefix="B01-W27-COMPOSITE-ABI-") as tmp:
  scratch=Path(tmp)
  for framework in ("net8.0","netstandard2.1"):
   wd=scratch/framework;wd.mkdir()
   (wd/"Composite.csproj").write_text(project(framework))
   for name,(ref,path) in REFS.items():
    data=git(ref,path);(wd/name).write_bytes(data)
    provenance[name]={"exact_commit":ref,"original_path":path,
     "sha256":hashlib.sha256(data).hexdigest(),"bytes":len(data)}
   cmd=["dotnet","build","Composite.csproj","-c","Release","--verbosity","quiet"]
   p=subprocess.run(cmd,cwd=wd,capture_output=True,text=True,errors="replace")
   (out/("original-source-"+framework.replace(".","_")+".log")).write_text(
    "$ "+" ".join(cmd)+"\nEXIT="+str(p.returncode)+"\n"+p.stdout+"\n"+p.stderr)
   if framework=="net8.0":
    if p.returncode!=0:raise RuntimeError("EXACT_NET8_COMPOSITE_COMPILATION_FAILED:"+p.stdout[-1500:]+p.stderr[-500:])
    print("B0140_W27_ORIGINAL_797_780_729_556_510_NET8_BUILD=PASS")
   else:
    log=p.stdout+p.stderr
    known=("HashData","ToHexString","No overload for method 'Move' takes 3 arguments")
    if p.returncode==0:raise RuntimeError("UNEXPECTED_NETSTANDARD21_BUILD_PASS_REVIEW_ACTUAL_ABI")
    if not all(k in log for k in known):
     raise RuntimeError("ABI_COMPILER_ERRORS_NOT_EXPECTED:"+log[-2200:])
    print("B0140_W27_ORIGINAL_UNITY_COMPAT_NETSTANDARD21_EXPECTED_ABI_RED=CONFIRMED")
    print("B0140_W27_ABI_ERRORS=SHA256.HashData;Convert.ToHexString;File.Move(3args)")
 (out/"exact-compiled-files-sha256.json").write_text(json.dumps(provenance,indent=2,sort_keys=True)+"\n")
if __name__=="__main__":run()
