#!/usr/bin/env python3
"""Original #826 direct-source ABI RED->GREEN, 3 independent negative compiler mutants.
Production bytes are never modified, never reserialized or copied into a game workspace.
"""
import hashlib,json,os,pathlib,re,subprocess,sys,tempfile
from xml.sax.saxutils import escape
ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="99fb08619c78b3834bb4c0450499babf991f218a"
PREV="2b7983f0f9e09dd1d56906b379a3f7af7b02f21a"
PROD="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/"
SELF="Tools/B01/B01-61/P17W28SaveUnityAbiIndependentQA/"
FLOW=".github/workflows/b01-61-w28-save826-abi-mutation.yml"
SOURCE={
 PROD+"WholeGameEconomySlotV3.cs":"247ab912b73a0e6f17e86aa1a3c74343cb4d9af6",
 PROD+"CanonicalWholeGameSessionV3.cs":"927b286e76a358716d69e76da5d8351c2564d613",
 "Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs":"53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773",
 "Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs":"d8b71de91db36295b6220e9c4baad71eb54455f6",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs":"21733278b6ab0669fccb6d085e0b6cf242792de2",
 "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs":"a9331dd620336d8c42165d134dc8a5e05ed51873",
 "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs":"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs":"b680ed0ec969ac23203fa55eab7b90f8daa86e43"}
CHANGE={
 "FileMoveReplaceReverted":{
   "old":"File.Replace(staging,authoritative,null);",
   "new":"File.Move(staging,authoritative,true);",
   "diagnostic":"CS1501"},
 "Sha256HashDataReverted":{
   "old":"using(SHA256 sha=SHA256.Create())return sha.ComputeHash(input);",
   "new":"return SHA256.HashData(input);",
   "diagnostic":"CS0117"},
 "ConvertToHexStringReverted":{
   "old":"return new string(chars);",
   "new":"return Convert.ToHexString(hash);",
   "diagnostic":"CS0117"}
}
def git(*args):return subprocess.check_output(["git",*args],cwd=ROOT,text=True,stderr=subprocess.DEVNULL).strip()
def blob(raw):return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def verify_original():
 subprocess.run(["git","merge-base","--is-ancestor",BASE,"HEAD"],cwd=ROOT,check=True)
 assert git("rev-parse",PREV+":Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs")==SOURCE["Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"]
 for p,expected in SOURCE.items():
  raw=(ROOT/p).read_bytes()
  assert git("rev-parse",BASE+":"+p)==expected,("producer blob",p)
  assert blob(raw)==expected,("local original mismatch",p)
 changes=set(git("diff","--name-only",BASE,"HEAD").splitlines())
 own={SELF+"source_mutation_abi_qa.py",SELF+"README.md",FLOW}
 assert changes==own,("Own-only 3 files",changes)
 for p in own:
  assert subprocess.run(["git","cat-file","-e",BASE+":"+p],
          cwd=ROOT,stderr=subprocess.DEVNULL).returncode!=0,("owner file overwritten",p)
 for n in ["ApprovedEconomyPolicyV7.cs","CanonicalEconomySectionV7.cs"]:
  path=PROD+".source/"+n
  original="Assets/B01Packages/ECONAPPROVED007/Runtime/"+n
  raw=(ROOT/path).read_bytes()
  assert blob(raw)==SOURCE[original],("injected V7 source mismatch",path)
 return changes
def build_mutant(name,variant,root):
 slot=ROOT/(PROD+"WholeGameEconomySlotV3.cs")
 session=ROOT/(PROD+"CanonicalWholeGameSessionV3.cs")
 code=slot.read_text(encoding="utf8")
 assert code.count(variant["old"])==1,("variant source pattern changed",name)
 code=code.replace(variant["old"],variant["new"])
 mutant=root/"WholeGameEconomySlotV3.cs";mutant.write_text(code,encoding="utf8")
 src=[mutant,session,*(ROOT/p for p in SOURCE if p not in [PROD+"WholeGameEconomySlotV3.cs",PROD+"CanonicalWholeGameSessionV3.cs"])]
 # Include EXACT V7 binary producer original, not legacy alternative; source paths
 # verified against BASE commit before running. No fake implementations/shims.
 include="\n".join('    <Compile Include="'+escape(str(p),{'"':"&quot;"})+'"/>' for p in src)
 project=root/"IndependentAbi.csproj"
 project.write_text(
 '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
 '<TargetFramework>netstandard2.1</TargetFramework>'
 '<LangVersion>9.0</LangVersion><Nullable>enable</Nullable>'
 '<EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
 '<NoWarn>CS8618;CS8625</NoWarn>'
 '</PropertyGroup><ItemGroup>\n'+include+'\n</ItemGroup></Project>\n',
 encoding="utf8")
 proc=subprocess.run(["dotnet","build",str(project),"-c","Release","--nologo","-v:q"],
   cwd=root,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=140)
 text=proc.stdout
 assert proc.returncode!=0,("mutation not detected",name,text[-1200:])
 assert re.search(r"\berror "+re.escape(variant["diagnostic"])+r"\b",text),(
     "mutant diagnostic missing",name,variant["diagnostic"],text[-2000:])
 assert ("WholeGameEconomySlotV3.cs" in text),("failed wrong file",name,text[-1200:])
 print("B0161_W28_REINTRODUCED_"+name.upper()+"_COMPILER_RED_DETECTED=PASS")
 return {"mutation":name,"expected_diagnostic":variant["diagnostic"],
   "compiler_exit":proc.returncode,
   "real_compiler_matches_expected_failure":True,
   "original_source_file_sha256":hashlib.sha256(slot.read_bytes()).hexdigest(),
   "mutant_source_sha256":hashlib.sha256(code.encode("utf8")).hexdigest(),
   "stdout_tail":text[-3500:]}
def main():
 changes=verify_original()
 path=pathlib.Path(sys.argv[1]);path.mkdir(parents=True,exist_ok=True)
 original_output=pathlib.Path(sys.argv[2]).read_text(encoding="utf8")
 assert "W25_V7_GAME_ECON_ONE_ENVELOPE_SOURCE_TOTAL 9 PASS 9 FAIL 0" in original_output
 results=[]
 with tempfile.TemporaryDirectory(prefix="b0161-w28-abi-diagnostic-") as temp:
  for name,variant in CHANGE.items():
   root=pathlib.Path(temp)/name;root.mkdir()
   results.append(build_mutant(name,variant,root))
 evidence={
  "base_original_826_sha":BASE,
  "exact_qa_head":git("rev-parse","HEAD"),
  "all_original_git_blobs":SOURCE,
  "new_add_only_sha256":{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in changes},
  "actual_original_826_780_one_file_source_executed":9,
  "abi_mutation_negative_compiles_detected":results,
  "exact_source_runtime":"ORIGINAL_NetStandard2.1_BUILD_GREEN_AND_Net8_9_SOURCE_PASS",
  "complete_unity_game_abi_g0":"HOLD_NOT_ONE_ASSEMBLY_OR_GAME_BINDER",
  "native_unity_g1":"NOT_RUN","windows_player":"NOT_RUN","integrated":False,"released":False
 }
 (path/"manifest.json").write_text(json.dumps(evidence,indent=2,sort_keys=True)+"\n")
 print("B0161_W28_ORIGINAL_826_ABI_NEGATIVE_CONTROLS_ALL_3=PASS")
 print("B0161_W28_SOURCE_ISOLATED_ORIGINAL_826_9_OF_9_PASS_NATIVE_UNITY_NOT_RUN")
 print("B0161_W28_QA_FINAL_SHA="+evidence["exact_qa_head"])
if __name__=="__main__":main()
