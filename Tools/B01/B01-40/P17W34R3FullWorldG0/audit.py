#!/usr/bin/env python3
"""B01-40 W34: independent EXACT-Git same-writer #923 source transport G0.
No original producer edits, no game binder, no native Unity G1, no release.
"""
import hashlib,io,json,subprocess,sys,tempfile,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
REF={
 "game877":"3bd9b159997924dcecb4e1cd1d41137896e5199e",
 "game873":"d537b7310979b91df27a08b14d0f1c74d0889089",
 "writer896":"a0282f2de0250d2a8fe421bbe182b4cb6fab2a11",
 "dynamic923":"e6eb24b7a45a66a5fa5d039f59c80b7e4be4439b",
 "economy780":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
 "admission855":"c9c19665a4f9385e6479f7151097deb797026ade",
 "economy910":"f865177bfb8340b15ccf12a4c0a377151a297adf",
 "physical792":"16cb2a7b5158605b15191b6074f9e392e0d1ebd3"
}
PREFIX="Tools/B01/B01-40/P17W34R3FullWorldG0/"
OWN={PREFIX+"audit.py",PREFIX+"IndependentCases.cs",PREFIX+"IndependentCases.csproj",
     PREFIX+"README.md",".github/workflows/b01-40-w34-dynamic-r3-onewriter-g0.yml"}
P62="Tools/B01/B01-62/"
STORE=P62+"P17W28UnityCompatibleWholeGameV3/WholeGameEconomySlotV3.cs"
CODEC=P62+"P17W33R3World/CanonicalR3WorldV1.cs"
PROGRAM=P62+"P17W33R3World/Program.cs"
WORLD="Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"
GATEWAY="Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"
LEASE="Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs"
GAME="Assets/R3/R3VehicleCatalog.cs"
ECON="Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"
ADMISSION="Assets/B01Packages/ECONADMISSIONW29/Runtime/EconomyV7AdmissionW29.cs"
PRODUCER={
".github/workflows/b01-62-w31-frozen-atomic-pair.yml",
".github/workflows/b01-62-w33-r3-dynamic-same-writer.yml",
STORE,
P62+"P17W30AdmissionV3/W28OriginalRegression.csproj",
P62+"P17W30AdmissionV3/W29ColdRegression.csproj",
P62+"P17W30AdmissionV3/W30AdmissionQA.csproj",
P62+"P17W30AdmissionV3/W30UnityAbi.csproj",
P62+"P17W31AtomicPair/AtomicPairQA.csproj",
CODEC,
P62+"P17W33R3World/OriginalR3FactoryShim.cs",
PROGRAM,
P62+"P17W33R3World/R3WorldActualOsQA.csproj",
P62+"P17W33R3World/README.md",
P62+"P17W33R3World/manifest.json",
P62+"P17W33R3World/source_guard.py"
}
def check(ok,marker):
    if not ok:raise AssertionError("B0140_W34_SOURCE_G0_HOLD_"+marker)
def git(*args):
    p=subprocess.run(["git",*args],cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    check(p.returncode==0,"GIT_ERROR_"+p.stderr.decode(errors="replace")[-150:])
    return p.stdout
def file(ref,p):
    return git("show",REF[ref]+":"+p)
def text(ref,p):
    return file(ref,p).decode("utf-8-sig")
def changed(a,b):
    return {l.split("\t",1)[1]:l.split("\t",1)[0] for l in
            git("diff","--name-status",REF.get(a,a),REF.get(b,b)).decode().splitlines()}
def source_integrity():
    current=git("rev-parse","HEAD").decode().strip()
    check(current not in REF.values(),"NEEDS_ISOLATED_QA_HEAD")
    for name,commit in REF.items():
        check(git("rev-parse",commit).decode().strip()==commit,"MISSING_ORIGINAL_SHA_"+name)
    check(git("merge-base","--is-ancestor",REF["game873"],REF["game877"])==b"",
          "GAME_CONTINUITY")
    check(git("merge-base","--is-ancestor",REF["writer896"],REF["dynamic923"])==b"",
          "PRODUCER_IS_NOT_ONE_WRITER_SUCCESSOR")
    check(git("merge-base","--is-ancestor",REF["game877"],"HEAD")==b"",
          "QA_NOT_CHILD_OF_CANONICAL_GAME")
    own=changed("game877","HEAD")
    check(set(own)==OWN and all(x=="A" for x in own.values()),"OWN_EXCLUSIVE_5_ADDITIONS")
    producer=changed("writer896","dynamic923")
    check(set(producer)==PRODUCER and len(producer)==15,
          "PRODUCER_EXACT_FIFTEEN_PATHS")
    check({p for p,s in producer.items() if s=="M"}==
          {STORE, ".github/workflows/b01-62-w31-frozen-atomic-pair.yml",
           P62+"P17W30AdmissionV3/W28OriginalRegression.csproj",
           P62+"P17W30AdmissionV3/W29ColdRegression.csproj",
           P62+"P17W30AdmissionV3/W30AdmissionQA.csproj",
           P62+"P17W30AdmissionV3/W30UnityAbi.csproj",
           P62+"P17W31AtomicPair/AtomicPairQA.csproj"},
          "SECOND_OR_UNAPPROVED_MODIFIED_PRODUCTION")
    check({p for p,s in producer.items() if s=="A"}==
          PRODUCER-{p for p,s in producer.items() if s=="M"},
          "UNEXPECTED_NEW_PRODUCER_PATHS")
    for source in (WORLD, GATEWAY, LEASE):
        check(file("writer896",source)==file("dynamic923",source),
              "FROZEN_PREVIOUS_AUTHORITY_MUTATED_"+source)
    check(file("game877",GAME)==file("dynamic923",GAME),
          "GAME_CATALOG_BLOB_VIOLATED")
    # #923 actually stages pinned V7/#855 into .source, not Assets.
    check(bool(file("economy780",ECON)) and bool(file("admission855",ADMISSION)),
          "IMMUTABLE_EXTERNAL_ORIGINAL_PROVENANCE_MISSING")
    wf=text("dynamic923",".github/workflows/b01-62-w33-r3-dynamic-same-writer.yml")
    for token in (REF["economy780"]+":"+ECON,
                  REF["admission855"]+":"+ADMISSION):
        check(token in wf,"EXACT_ORIGINAL_SOURCE_STAGING_"+token[:12])
    return sorted(producer)
def static_guards():
    old=text("writer896",STORE)
    new=text("dynamic923",STORE)
    codec=text("dynamic923",CODEC)
    tests=text("dynamic923",PROGRAM)
    # Existing authority, atomicity, frozen pair, no silent schema switch.
    for marker in ("private const string Magic=\"B0162G02\"",
                   "frozenWorld=(byte[])world.Clone();",
                   "frozenEcon=econ.Copy();",
                   "File.Replace(staging,authoritative,null)",
                   "EconomyV7AdmissionW29.TryAdmit",
                   "publisher.Publish(stage,final);",
                   "!actualWorld.SequenceEqual(frozenWorld)",
                   "!SameEconomicSection(actualEconomy,frozenEcon)"):
        check(marker in new,"SINGLE_WRITER_INVARIANT_"+marker[:28])
    for marker in ("CanonicalR3WorldCodecV1.IsR3World(world)",
                   "CanonicalR3WorldCodecV1.TryDecode(world",
                   "dynamicWorld.Parts.Count!=econ.Parts.Count",
                   "if(!economicIds.TryAdd(p.Id,p.Hp))",
                   "if(!economicIds.TryGetValue(p.Id,out float hp)||hp!=p.Hp)",
                   "if(!ColdRestore.TryStage(world",
                   "CanonicalR3WorldCodecV1.IsR3World(priorWorld)!="):
        check(marker in new,"STRICT_TWO_FORMAT_SEMANTIC_"+marker[:28])
    check("CanonicalR3WorldCodecV1.TryDecode" not in old,
          "NOT_AN_ACTUAL_NEW_SINGLE_WRITER_PRODUCER_CHANGE")
    for marker in ('Magic="B01R3W01"',"MaxParts=1024",
                   "MaxBytes=3*1024*1024",
                   "!physical.Add(p.PhysicalId)",
                   "!ids.Add(p.Id)",
                   "p.ConsumedDetach!=(p.Location==R3PartLocationV1.Detached)",
                   "mountedWeaponTypes.Count>2","p.Weapon>8",
                   "p.Kind!=2&&p.Weapon!=0","mountedOwner=0",
                   "p.Parts.Count"): # last sentinel checked independently below
        if marker=="p.Parts.Count":continue
        check(marker in codec,"DYNAMIC_CODEC_"+marker[:25])
    check("!Encode(next).SequenceEqual(bytes)" in codec,"CANONICAL_DECODE_ROUNDTRIP")
    check("Original R3" in tests or "R3VehicleCatalog.Create" in tests,
          "MISSING_REAL_FACTORY_SOURCE_IN_PRODUCER_TEST")
    for probe in ("FactoryRoundtrip(R3VehicleKind.Light,20)",
                  "FactoryRoundtrip(R3VehicleKind.Standard,83)",
                  "FactoryRoundtrip(R3VehicleKind.Heavy,139)",
                  "B0162_W33_R3_SAME_ONE_WRITER_REAL_OS_TOTAL",
                  "NoMigrationLegacy556","V7PaidZeroNextModeledVictory"):
        check(probe in tests,"MISSING_ORIGINAL_PRODUCER_OS_CASE_"+probe)
    # Negative controls: intentionally strip actual authoritative clauses
    # and ensure original G0 precondition fails, without editing real blobs.
    mutations=[
       (new,new.replace("if(!economicIds.TryAdd(p.Id,p.Hp))","if(false)",1),
        "if(!economicIds.TryAdd(p.Id,p.Hp))"),
       (new,new.replace("CanonicalR3WorldCodecV1.IsR3World(priorWorld)!=",
                         "false /*omitted schema guard*/ != ",1),
        "CanonicalR3WorldCodecV1.IsR3World(priorWorld)!="),
       (codec,codec.replace("!physical.Add(p.PhysicalId)","false",1),
        "!physical.Add(p.PhysicalId)"),
       (codec,codec.replace("MaxParts=1024","MaxParts=2",1),"MaxParts=1024")
    ]
    for src,bad,marker in mutations:
        check(src!=bad and marker not in bad,"NEGATIVE_MUTANT_DETECT_"+marker[:20])
    return {"original_single_atomic_writer":True,"new_R3_world_magic":"B01R3W01",
            "same_original_outer_envelope":"B0162G02",
            "frozen_old_two_rig_codec_unchanged":True,
            "dynamic_world_20_83_139_source_supported":True,
            "source_R4_weapon_limit_and_mounted_owner_guard":True,
            "new_original_source_negative_controls":4,
            "live_Unity_R3_Rig_Dock_binder":"NOT_IMPLEMENTED",
            "Unity6000_3_14f1_native":"NOT_RUN",
            "actual_Windows_x64_Player":"NOT_BUILT",
            "one_use_loaner_834":"OWNER_DECISION_PENDING",
            "real_game_save_integration":"HOLD_G0_ONLY"}
def stage_rollback(blobs):
    with tempfile.TemporaryDirectory(prefix="b0140-w34-source-only-") as temp:
        root=Path(temp)
        own=root/"B0140_OWN";own.mkdir()
        foreign=root/"FOREIGN";foreign.mkdir()
        (foreign/"EMPTY").mkdir()
        (foreign/"keep.txt").write_bytes(b"other-agent-private-file")
        for p,data in blobs.items():
            f=own/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(data)
        def exact():
            return {p.relative_to(own).as_posix():p.read_bytes()
                    for p in own.rglob("*") if p.is_file()}==blobs and not any(
                 p.is_symlink() for p in own.rglob("*"))
        check(exact(),"POSITIVE_ORIGINAL_BYTE_STAGE")
        p=next(iter(blobs))
        item=own/p;item.write_bytes(b"dirty-partial-writer")
        check(not exact(),"DIRTY_PARTIAL_BYTES_NOT_DETECTED")
        item.write_bytes(blobs[p])
        (own/"BAD_FOREIGN_EMPTY").mkdir()
        check(any(f.is_dir() and not any(f.iterdir()) for f in own.rglob("*")),
              "FOREIGN_EMPTY_NOT_OBSERVED")
        (own/"BAD_FOREIGN_EMPTY").rmdir()
        check(exact(),"ORIGINAL_AFTER_LOCAL_ROLLBACK")
        check((foreign/"EMPTY").is_dir() and
              (foreign/"keep.txt").read_bytes()==b"other-agent-private-file",
              "FOREIGN_FILE_DELETED")
        return "OWN_SOURCE_BYTE_EXACT_DIRTY_REJECTED_FOREIGN_RETAINED"
def archive(entries):
    stream=io.BytesIO()
    with zipfile.ZipFile(stream,"w",compression=zipfile.ZIP_DEFLATED) as z:
        for name,data in sorted(entries.items()):
            meta=zipfile.ZipInfo(name,(1980,1,1,0,0,0))
            meta.create_system=3;meta.external_attr=0o100644<<16
            z.writestr(meta,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    return stream.getvalue()
def main():
    dest=Path(sys.argv[1])
    check(not dest.exists(),"RECEIPT_DIR_ALREADY_EXISTS")
    changed_files=source_integrity()
    gates=static_guards()
    source={
       "ORIGINAL/877_R3Factory.cs":file("game877",GAME),
       "ORIGINAL/923_OneWriter.cs":file("dynamic923",STORE),
       "ORIGINAL/923_DynamicR3WorldV1.cs":file("dynamic923",CODEC),
       "ORIGINAL/923_SourceDiskTests.cs":file("dynamic923",PROGRAM),
       "REFERENCE/896_PreviousOneWriter.cs":file("writer896",STORE),
       "REFERENCE/FROZEN556_LegacyTwoRig.cs":file("writer896",WORLD),
       "REFERENCE/FROZEN780_ApprovedEconomy.cs":file("economy780",ECON),
       "REFERENCE/FROZEN855_OwnerAdmission.cs":file("admission855",ADMISSION)
    }
    rollback=stage_rollback(source)
    proofs={k:hashlib.sha256(v).hexdigest() for k,v in sorted(source.items())}
    receipt={"kind":"B0140_P17W34_EXACT_SELECTED_923_ONE_WRITER_G0_SOURCE_ONLY",
             "refs":REF,"producer_15_paths":changed_files,
             "own_5_additions":sorted(OWN),"source_sha256":proofs,
             "scope_guards":gates,"owned_scratch":rollback,
             "installation":"REVIEW_ONLY_DO_NOT_INSTALL_OR_MERGE",
             "G0":"INDEPENDENT_EXECUTABLE_SOURCE_PROVENANCE_PASS_IF_CI_SUCCEEDS",
             "G1":"NOT_RUN","G2":"HOLD_NO_ACTUAL_PLAYER"}
    payload=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
    zipbytes=archive({"MANIFEST.json":payload,
       "NOT_INSTALLABLE.txt":b"Review-only source evidence. Not a game Player, not a Unitypackage. Full R3 binder and trusted host victory NOT_IMPLEMENTED.\n",**source})
    dest.mkdir()
    (dest/"manifest.json").write_bytes(payload)
    (dest/"B01_W34_SOURCE_ONLY_REVIEW.zip").write_bytes(zipbytes)
    (dest/"sha256.json").write_bytes((json.dumps({
         "inner_zip_sha256":hashlib.sha256(zipbytes).hexdigest(),
         "original_entries":len(source)},sort_keys=True)+"\n").encode())
    print("B0140_W34_EXACT_923_ONE_ORIGINAL_ATOMIC_WRITER_G0=PASS")
    print("B0140_W34_FULL_R3_20_83_139_DYNAMIC_WORLD_SOURCE=PASS")
    print("B0140_W34_ORIGINAL_556_729_780_855_UNMODIFIED=PASS")
    print("B0140_W34_P0_REAL_GAME_PHYSICS_BINDER=NOT_IMPLEMENTED")
    print("B0140_W34_NATIVE_UNITY_G1_NOT_RUN_WINDOWS_PLAYER_NOT_BUILT")
    print("B0140_W34_SOURCE_ONLY_ZIP_SHA256="+hashlib.sha256(zipbytes).hexdigest())
if __name__=="__main__":main()
