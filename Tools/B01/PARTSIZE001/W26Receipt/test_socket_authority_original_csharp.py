#!/usr/bin/env python3
"""B01-23 W58: compile and run ORIGINAL PARTSIZE C# socket method outside Unity.
Source is fetched from immutable producer Git blobs. NEVER a Unity/PhysX result.
Includes a deliberately corrupted return-value mutant that MUST FAIL.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
import tempfile

SHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
ROOT="Assets/PARTSIZE001/Runtime/"
BLOBS={
    "PartSizeContract.cs":"07455919a5544875fee5a78ecbbce36dc6cb49c2",
    "PartSizeBaselineCatalog.cs":"6153d1758e800ec5c4210323610b061c06e8ea85",
    "PartSizeNativePhysics.cs":"44c507eba175029fb2750acbcac44119c1d97001"
}
KIND11=("Wheel","Gun","Suspension","Booster","JumpDrive","Blink",
        "Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack")

def fetch(repo):
    sources={}
    for name,blob in BLOBS.items():
        address=SHA+":"+ROOT+name
        actual=subprocess.check_output(["git","-C",repo,"rev-parse",address],text=True).strip()
        if actual!=blob:
            raise ValueError("PRODUCER_BLOB_CHANGED:"+name+":"+actual)
        sources[name]=subprocess.check_output(["git","-C",repo,"show",address]).decode("utf-8")
    return sources

def extract_original_method(native):
    signature="public static string ValidateMountSocketAuthority("
    if native.count(signature)!=1:
        raise ValueError("SOCKET_METHOD_NOT_UNIQUE")
    i=native.index(signature)
    opening=native.index("{",i)
    depth=0
    for j in range(opening,len(native)):
        if native[j]=="{": depth+=1
        if native[j]=="}": depth-=1
        if depth==0:
            method=native[i:j+1]
            break
    else:
        raise ValueError("SOCKET_METHOD_UNTERMINATED")
    required=(
        'if(definition==null)return "INVALID_LOGICAL_FOOTPRINT";',
        'if(!PartSizeGrid.IsExposedFace(definition,localCell,face))',
        'return "NOT_EXPOSED_LOGICAL_FACE";',
        'return "TYPED_SOCKET_UNVERIFIED";',
    )
    if any(token not in method for token in required):
        raise ValueError("SOCKET_AUTHORITY_CONTRACT_CHANGED")
    return method

PROGRAM=r"""
using System;
using B01.PARTSIZE001;
static class Program {
  static int checks;
  static void Need(bool predicate,string label) {
    checks++;
    if(!predicate)throw new Exception("SOURCE_SOCKET_PROVENANCE_FAIL:"+label);
  }
  static void Main() {
    Need(PartSizeNativePhysics.ValidateMountSocketAuthority(
      null,new Cell3(0,0,0),new Cell3(1,0,0))=="INVALID_LOGICAL_FOOTPRINT","null-provenance");
    string[] kinds={"Wheel","Gun","Suspension","Booster","JumpDrive",
      "Blink","Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack"};
    Cell3 inside=new Cell3(0,0,0),face=new Cell3(1,0,0);
    foreach(string kind in kinds) {
      var spec=PartSizeBaselineCatalog.ForSourceKind(kind);
      Need(PartSizeGrid.DefinitionError(spec)=="UNMEASURED_PHYSICAL_ENVELOPE",
        "unmeasured-hull-"+kind);
      var forged=new PartSizeDefinition("FOREIGN-"+kind,spec.Dimensions,
        spec.LocalBoxCenter,spec.LocalBoxSize,"CLAIMED_TRUSTED_TYPED_SOCKET");
      for(int y=0;y<4;y++) {
        var turns=new Cell3(0,y,0);
        var rotated=PartSizeGrid.Rotate(face,turns);
        var recovered=PartSizeGrid.Rotate(rotated,new Cell3(0,4-y,0));
        Need(recovered.Equals(face),"yaw-local-normal-"+kind+"-"+y);
        Need(PartSizeNativePhysics.ValidateMountSocketAuthority(spec,inside,recovered)
          =="TYPED_SOCKET_UNVERIFIED","missing-trusted-socket-"+kind+"-"+y);
        Need(PartSizeNativePhysics.ValidateMountSocketAuthority(forged,inside,recovered)
          =="TYPED_SOCKET_UNVERIFIED","forged-part-id-and-provenance-"+kind+"-"+y);
        Need(PartSizeNativePhysics.ValidateMountSocketAuthority(spec,inside,
          new Cell3(1,1,0))=="NOT_EXPOSED_LOGICAL_FACE","diagonal-face-"+kind+"-"+y);
        Need(PartSizeNativePhysics.ValidateMountSocketAuthority(spec,
          new Cell3(12,0,0),face)=="NOT_EXPOSED_LOGICAL_FACE","foreign-cell-"+kind+"-"+y);
      }
    }
    string[] wideKinds={"BonusModule","DirectionalShield","PointDefense",
      "StructuralScanner","ScannerPulse","IndependentSteering"};
    foreach(string kind in wideKinds) {
      var spec=PartSizeBaselineCatalog.ForSourceKind(kind);
      for(int y=0;y<4;y++) {
        var t=new Cell3(0,y,0);
        Cell3 exposed=PartSizeGrid.Rotate(new Cell3(-1,0,0),t);
        Cell3 recovered=PartSizeGrid.Rotate(exposed,new Cell3(0,4-y,0));
        Need(recovered.Equals(new Cell3(-1,0,0)),"wide-rotation-"+kind+"-"+y);
        Need(PartSizeNativePhysics.ValidateMountSocketAuthority(spec,inside,
          recovered)=="TYPED_SOCKET_UNVERIFIED","wide-exposed-unauthorized-"+kind+"-"+y);
        Need(PartSizeNativePhysics.ValidateMountSocketAuthority(spec,inside,
          face)=="NOT_EXPOSED_LOGICAL_FACE","wide-internal-face-"+kind+"-"+y);
        Need(PartSizeNativePhysics.ValidateMountSocketAuthority(spec,
          new Cell3(1,0,0),face)=="TYPED_SOCKET_UNVERIFIED",
          "wide-opposite-exterior-no-typed-proof-"+kind+"-"+y);
      }
    }
    Console.WriteLine("ORIGINAL_PARTSIZE_CSHARP_SOCKET_SOURCE_PASS "+checks+
      "/"+checks+"; 11 kinds x 4 yaw + 6 wide x 4 yaw; UNITY_NOT_RUN; G2_HOLD");
  }
}
"""
CSPROJ="""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable>
</PropertyGroup></Project>"""

def run_dotnet(sources,method,folder):
    for name in ("PartSizeContract.cs","PartSizeBaselineCatalog.cs"):
        (folder/name).write_text(sources[name],encoding="utf-8")
    stub="namespace B01.PARTSIZE001 {\npublic static class PartSizeNativePhysics {\n"+method+"\n}\n}\n"
    (folder/"OriginalSocketMethod.cs").write_text(stub,encoding="utf-8")
    (folder/"Program.cs").write_text(PROGRAM,encoding="utf-8")
    (folder/"SourceSocketTests.csproj").write_text(CSPROJ,encoding="utf-8")
    return subprocess.run(["dotnet","run","--configuration","Release",
      "--project",str(folder/"SourceSocketTests.csproj")],cwd=str(folder),
      text=True,capture_output=True,timeout=110)

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--out",default="")
    args=p.parse_args()
    sources=fetch(args.repo)
    original=extract_original_method(sources["PartSizeNativePhysics.cs"])
    with tempfile.TemporaryDirectory(prefix="b01-23-original-socket-") as tmp:
        folder=Path(tmp)
        good=run_dotnet(sources,original,folder)
        if good.returncode!=0 or "ORIGINAL_PARTSIZE_CSHARP_SOCKET_SOURCE_PASS" not in good.stdout:
            raise AssertionError("ORIGINAL_CSHARP_SOURCE_TEST_RED:\n"+good.stdout+"\n"+good.stderr)
        false_green=original.replace('return "TYPED_SOCKET_UNVERIFIED";','return null;')
        if false_green==original:
            raise AssertionError("MUTATION_NOT_APPLIED")
        bad=run_dotnet(sources,false_green,folder)
        if bad.returncode==0 or "SOURCE_SOCKET_PROVENANCE_FAIL:" not in bad.stderr:
            raise AssertionError("FALSE_GREEN_MUTANT_WAS_NOT_REJECTED:\n"+bad.stdout+"\n"+bad.stderr)
        report={"input_producer_sha":SHA,"git_blobs":BLOBS,
           "native":"NOT_RUN","fixture":"ORIGINAL_CSHARP_METHOD_EXTRACTED_FROM_GIT",
           "actual_original_csharp":good.stdout.strip().splitlines()[-1],
           "negative_mutant":"EXPECTED_RED_SOURCE_SOCKET_PROVENANCE_FAIL",
           "no_unity_licence_used":True,"G2":"HOLD"}
        if args.out:Path(args.out).write_text(json.dumps(report,indent=2)+"\n",encoding="utf8")
        print(json.dumps(report,indent=2))
if __name__=="__main__":
    main()
