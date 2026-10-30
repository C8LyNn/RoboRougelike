#!/usr/bin/env python3
"""Genuine compiled historic R1 expected-RED and corrected R2 GREEN.
Never writes to repository or owner game; all dotnet outputs in temporary dir.
"""
import hashlib, pathlib, subprocess, sys, tempfile

ROOT=pathlib.Path(__file__).resolve().parents[4]
P="Assets/B01Packages/RECOVERYHUD001/Runtime/RecoveryNotice.cs"
OLD="d1a41d60cd74eefa96915f0c450ce36e70737efd"
BLOB="1bb118783458de15584a913feb29c07ade7b4fc2"
SOURCE=(ROOT/P).read_bytes()
OLD_SOURCE=subprocess.check_output(["git","show",OLD+":"+P],cwd=ROOT)
def sha1(b):
    return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
assert sha1(OLD_SOURCE)==BLOB, "historical source Git blob mismatch"
assert SOURCE!=OLD_SOURCE, "R2 fix not present"
PROJECT="""<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<LangVersion>9.0</LangVersion><Nullable>enable</Nullable>
<ImplicitUsings>disable</ImplicitUsings><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
</PropertyGroup><ItemGroup>
<Compile Include="Probe.cs"/><Compile Include="RecoveryNotice.cs"/>
</ItemGroup></Project>
"""
PROBE="""using System;
using B01.RecoveryHud;
public static class Probe {
 public static int Main() {
  foreach (var pair in new[]{(1,1),(100,64),(100,65),(1,0)}) {
   var r=new RecoveryRead {
    RunId="run",SlotId="slot",Epoch=1,
    VerifiedOwnerRead=true,SingleLiveOwner=true,
    PhysicalStateVerified=true,DurableCommitCertain=true,
    CoreAlive=true,NextChallengeEligible=true,
    NormalConstructionFunctional=true,
    OriginalPartPriceScrap=pair.Item1,HostQuotedPaidScrap=pair.Item2
   };
   Console.WriteLine(pair.Item1+"x"+pair.Item2+"="+
     RecoveryNoticeBuilder.Build(r,"run","slot",1).Kind);
  }
  return 0;
 }
}
"""
with tempfile.TemporaryDirectory(prefix="b0111_w25_quote_",ignore_cleanup_errors=True) as temp:
    folder=pathlib.Path(temp)
    (folder/"Probe.csproj").write_text(PROJECT,encoding="utf-8")
    (folder/"Probe.cs").write_text(PROBE,encoding="utf-8")
    results=[]
    for source in (OLD_SOURCE,SOURCE):
        (folder/"RecoveryNotice.cs").write_bytes(source)
        result=subprocess.run(["dotnet","run","--project",str(folder/"Probe.csproj"),
                                "--configuration","Release","--no-launch-profile"],
                                stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,
                                check=False)
        if result.returncode!=0:
            print(result.stdout,result.stderr,file=sys.stderr)
            raise SystemExit("source compile or execution FAILED")
        samples=[line.strip() for line in result.stdout.splitlines() if "x" in line and "=" in line]
        if len(samples)!=4:raise SystemExit("incomplete original-source probe: "+str(samples))
        results.append(samples)
    expect_r1=["1x1=Ready","100x64=Ready","100x65=Ready","1x0=Ready"]
    expect_r2=["1x1=Unknown","100x64=Unknown","100x65=Ready","1x0=Ready"]
    assert results[0]==expect_r1,(results[0],expect_r1)
    assert results[1]==expect_r2,(results[1],expect_r2)
    print("R1_ORIGINAL_SOURCE_EXPECTED_PRODUCT_RED",results[0])
    print("R2_CORRECTED_ORIGINAL_SOURCE_GREEN",results[1])
    print("NEGATIVE_SOURCE_WITNESS_PASS 8/8 PROBE_ASSERTIONS")
