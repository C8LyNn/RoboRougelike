#!/usr/bin/env python3
"""W34 independent original #923 source G0. NOT Unity or a second Save."""
import argparse,hashlib,io,json,pathlib,subprocess,zipfile
G="3bd9b159997924dcecb4e1cd1d41137896e5199e"
S="e6eb24b7a45a66a5fa5d039f59c80b7e4be4439b"
B="a0282f2de0250d2a8fe421bbe182b4cb6fab2a11"
V="b4df10a7ad7e6c758f8331013f6b216b3a3689a3"
A="c9c19665a4f9385e6479f7151097deb797026ade"
P="Tools/B01/B01-40/P17W31Game877Save885G0/"
OWN={P+"audit.py",P+"README.md",".github/workflows/b01-40-w31-877-885-exclusive-original-g0.yml"}
DELTA={"M:.github/workflows/b01-62-w31-frozen-atomic-pair.yml",
"A:.github/workflows/b01-62-w33-r3-dynamic-same-writer.yml",
"M:Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs",
"M:Tools/B01/B01-62/P17W30AdmissionV3/W28OriginalRegression.csproj",
"M:Tools/B01/B01-62/P17W30AdmissionV3/W29ColdRegression.csproj",
"M:Tools/B01/B01-62/P17W30AdmissionV3/W30AdmissionQA.csproj",
"M:Tools/B01/B01-62/P17W30AdmissionV3/W30UnityAbi.csproj",
"M:Tools/B01/B01-62/P17W31AtomicPair/AtomicPairQA.csproj",
"A:Tools/B01/B01-62/P17W33R3World/CanonicalR3WorldV1.cs",
"A:Tools/B01/B01-62/P17W33R3World/OriginalR3FactoryShim.cs",
"A:Tools/B01/B01-62/P17W33R3World/Program.cs",
"A:Tools/B01/B01-62/P17W33R3World/R3WorldActualOsQA.csproj",
"A:Tools/B01/B01-62/P17W33R3World/README.md",
"A:Tools/B01/B01-62/P17W33R3World/manifest.json",
"A:Tools/B01/B01-62/P17W33R3World/source_guard.py"}
W="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs"
C="Tools/B01/B01-62/P17W33R3World/CanonicalR3WorldV1.cs"
T="Tools/B01/B01-62/P17W33R3World/Program.cs"
R="Assets/R3/R3VehicleCatalog.cs"
F="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
def git(*args):return subprocess.check_output(["git",*args],stderr=subprocess.PIPE)
def ref(r,p):return git("rev-parse",r+":"+p).decode().strip()
def obj(r,p):return git("show",r+":"+p)
def check(q,n):
 if not q:raise AssertionError("B0140_W34_STOP_"+n)
def diff(a,b):
 return {s+":"+p for s,p in
 (ln.split("\t",1) for ln in git("diff","--name-status",a,b).decode().splitlines())}
def digest(v):return hashlib.sha256(v).hexdigest()
def require(s,*parts):return all(p in s for p in parts)
def audit():
 for r in (G,S,B,V,A):check(git("rev-parse",r).decode().strip()==r,"REF_"+r[:8])
 check(diff(G,"HEAD")=={"A:"+p for p in OWN},"EXCLUSIVE_3_QA_FILES")
 check(subprocess.run(["git","merge-base","--is-ancestor",B,S]).returncode==0,"923_DESCENDS_896")
 check(diff(B,S)==DELTA,"923_EXACT_15_ORIGINAL_PRODUCER_SCOPE")
 check(ref(G,R)==ref(S,R)=="f76de5bbdcc1dbdb7ddb9fe9859c36c232fd8584","R3_FACTORY_PROVENANCE")
 check(ref(G,"Assets/R4/R4WeaponDefinition.cs")=="502f6d7f70d4e8078260de632ab181f244a4d730","REAL_9_R4_WEAPON_ENUM")
 check("MaxWeaponTypes=2" in obj(G,"Assets/R3/R3VehicleModel.cs").decode(),"REAL_R3_TWO_MOUNTED_WEAPON_TYPES")
 check(ref(B,F)==ref(S,F)=="b52e511c370f5fca56c74029050844dc8cbb7f9a","FROZEN_556_UNTOUCHED")
 session="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/CanonicalWholeGameSessionV3.cs"
 check(ref(B,session)==ref(S,session),"NO_SECOND_SESSION_OR_WRITER")
 check(bool(obj(V,"Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs")) and
       bool(obj(A,"Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs")),
       "IMMUTABLE_780_AND_855_AVAILABLE_SEPARATELY")
 w=obj(S,W).decode();prior=obj(B,W).decode();codec=obj(S,C).decode();program=obj(S,T).decode()
 check("CanonicalR3WorldCodecV1.IsR3World(world)" not in prior,"NEW_NOT_DUPLICATED")
 wguards=["CanonicalR3WorldCodecV1.IsR3World(world)",
 "CanonicalR3WorldCodecV1.TryDecode(world",
 "dynamicWorld.Parts.Count!=econ.Parts.Count",
 "economicIds.TryAdd(p.Id,p.Hp)",
 "economicIds.TryGetValue(p.Id,out float hp)||hp!=p.Hp",
 "CanonicalR3WorldCodecV1.IsR3World(priorWorld)!=",
 "ColdRestore.TryStage(world","File.Replace(staging,authoritative,null)",
 "EconomyV7AdmissionW29.TryAdmit"]
 cguards=['Magic="B01R3W01"',"MaxParts=1024","MaxBytes=3*1024*1024",
 "Guid.TryParseExact(p.Id","!ids.Add(p.Id)","!physical.Add(p.PhysicalId)",
 "world.Parts.Count>MaxParts",
 "p.Weapon>8","p.Kind!=2&&p.Weapon!=0",
 "mountedOwner=0","p.OwnerPhysicalId!=mountedOwner","mountedWeaponTypes.Count>2",
 "p.ConsumedDetach!=(p.Location==R3PartLocationV1.Detached)",
 "float.IsPositiveInfinity(p.DefenseRechargeAge)",
 "stream.Position!=stream.Length","!Encode(next).SequenceEqual(bytes)"]
 check(require(w,*wguards),"WRITER_ONE_ATOMIC_ADMISSION_PARITY_AND_MIGRATION")
 check(require(codec,*cguards),"R3_GUID_ID_COUNT_CORRUPTION_GUARDS")
 check(require(program,"R3VehicleKind.Light","R3VehicleKind.Standard","R3VehicleKind.Heavy",
             "ActualDisk83","ActualR4WeaponAndMountedOwnerNegatives",
             "DynamicAtomicFailClosedAndCold","ActualOriginalGatewayCannotStageR3Yet",
             "B0162_W33_R3_SAME_ONE_WRITER_REAL_OS_TOTAL"),"ACTUAL_OS_CASES")
 check(require(obj(S,F).decode(),"public const int Slots=2;",
               "s.Parts.Length!=FrozenPartCatalog.Slots"),"LEGACY_556_STILL_FIXED")
 mutants=[(w,"CanonicalR3WorldCodecV1.IsR3World(world)"),
          (w,"economicIds.TryAdd(p.Id,p.Hp)"),
          (w,"economicIds.TryGetValue(p.Id,out float hp)||hp!=p.Hp"),
          (w,"CanonicalR3WorldCodecV1.IsR3World(priorWorld)!="),
          (codec,"!ids.Add(p.Id)"),(codec,"!physical.Add(p.PhysicalId)"),
          (codec,"world.Parts.Count>MaxParts"),
          (codec,"p.Weapon>8"),
          (codec,"p.Kind!=2&&p.Weapon!=0"),
          (codec,"mountedWeaponTypes.Count>2"),
          (codec,"!Encode(next).SequenceEqual(bytes)")]
 for original,guard in mutants:
  check(original.count(guard)==1 and guard not in original.replace(guard,"REMOVED_GUARD",1),
        "NEGATIVE_MUTATION_"+guard)
 return {"selected_game":G,"selected_one_writer_successor":S,
 "writer_base":B,"one_writer_source_G0":"PASS_IF_OS_TESTS_SUCCEED",
 "guards_negative_mutants":len(mutants),"original_15_changed_paths":sorted(DELTA),
 "full_R3_schema":"SOURCE_POSITIVE_NEW_20_83_139",
 "legacy_fixed_556":"PRESERVED_NO_SILENT_MIGRATION",
 "actual_R3_Rig_and_Unity_stage":"NOT_IMPLEMENTED",
 "native_unity":"NOT_RUN","windows_game_zip":"NOT_BUILT",
 "owner_dynamic_schema_migration_authority":"PENDING",
 "loaner_policy_834":"PENDING","G2":"HOLD","released":False}
def pack(files):
 stream=io.BytesIO()
 with zipfile.ZipFile(stream,"w",compression=zipfile.ZIP_STORED) as z:
  for p,raw in sorted(files.items()):
   info=zipfile.ZipInfo(p,(1980,1,1,0,0,0));info.external_attr=0o100644<<16
   z.writestr(info,raw)
 return stream.getvalue()
def main():
 a=argparse.ArgumentParser();a.add_argument("--out",required=True)
 out=pathlib.Path(a.parse_args().out)
 check(not out.exists(),"OUTPUT_MUST_BE_NEW")
 report=audit()
 selected={"REVIEW_ONLY_ONE_WRITER/"+W:obj(S,W),
 "REVIEW_ONLY_DYNAMIC_CODEC/"+C:obj(S,C),
 "REVIEW_ONLY_REAL_FACTORY/"+R:obj(G,R),
 "REVIEW_ONLY_HISTORIC_556/"+F:obj(S,F),
 "REVIEW_ONLY_ORIGINAL_OS_TESTS/"+T:obj(S,T)}
 report["source_objects"]={p:{"sha256":digest(s),"bytes":len(s)} for p,s in sorted(selected.items())}
 raw=(json.dumps(report,sort_keys=True,indent=2)+"\n").encode()
 packet=pack({"MANIFEST.json":raw,"NOT_GAME_PLAYER.txt":
 b"ORIGINAL GIT SOURCE REVIEW ONLY. NO UNITY G1 / NO WINDOWS EXE / NO SECOND WRITER.\n",**selected})
 out.mkdir()
 (out/"manifest.json").write_bytes(raw)
 (out/"B0140_W34_923_REVIEW_ONLY_SOURCE.zip").write_bytes(packet)
 (out/"sha256.json").write_text(json.dumps({"inner_source_zip_sha256":digest(packet),
 "manifest_sha256":digest(raw)},sort_keys=True,indent=2)+"\n")
 print("B0140_W34_923_15_OF_15_PRODUCER_SOURCE_SCOPE_PASS")
 print("B0140_W34_923_SAME_ONE_WRITER_11_NEGATIVE_GUARDS_PASS")
 print("B0140_W34_923_DYNAMIC_R3_SCHEMA_SOURCE_POSITIVE_REAL_UNITY_NOT_RUN")
 print("B0140_W34_INNER_SOURCE_ZIP_SHA256="+digest(packet))
if __name__=="__main__":main()
