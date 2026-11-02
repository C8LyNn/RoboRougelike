#!/usr/bin/env python3
"""Prepare an ephemeral .NET8 build containing EXACT owner Git blobs, no forks."""
import argparse,hashlib,json,subprocess
from pathlib import Path
ORIGINAL={
 "CanonicalSingleAuthorityStoreV3R2.cs":("3aaaec4027ee246fbfb6f209179f0e9507363473","Tools/B01/B01-62/P17W24CanonicalPathFixR2/CanonicalSingleAuthorityStoreV3R2.cs"),
 "CanonicalWorldSlotLease.cs":("b8ca29c7707cd6b4338d0b9772d74dee676d49d2","Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs"),
 "DetachedSnapshotContract.cs":("33094f154c7b91119b36d58aab285e6e83c5de57","Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"),
 "DebrisImpulseCore.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"),
 "SecureRigSpec.cs":("d94400d95d13db7f27e1533df05c9292d5380431","Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs"),
}
def git(*args):return subprocess.check_output(["git",*args])
def main():
 a=argparse.ArgumentParser();a.add_argument("--out",required=True);args=a.parse_args()
 root=Path(args.out);root.mkdir(parents=True,exist_ok=False)
 originals={}
 for name,(sha,path) in ORIGINAL.items():
  data=git("show",sha+":"+path)
  (root/name).write_bytes(data)
  originals[name]={"git_sha":sha,"source_path":path,"sha256":hashlib.sha256(data).hexdigest()}
 qa=Path("Tools/B01/B01-40/P17W25IndependentG0/CaseAliasNegative.cs").read_bytes()
 (root/"CaseAliasNegative.cs").write_bytes(qa)
 (root/"CaseAliasNegative.csproj").write_text(
  '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'+
  '<TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType>'+
  '<LangVersion>9.0</LangVersion><Nullable>enable</Nullable>'+
  '<TreatWarningsAsErrors>true</TreatWarningsAsErrors>'+
  '</PropertyGroup></Project>\n')
 (root/"sources-original-sha.json").write_text(json.dumps(originals,indent=2,sort_keys=True)+"\n")
 print("B0140_W25_ORIGINAL_SOURCE_BYTES_PREPARED=5_GIT_BLOBS")
if __name__=="__main__":main()
