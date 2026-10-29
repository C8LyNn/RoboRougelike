#!/usr/bin/env python3
"""Independent B01-61 original-source acceptance of B01-23 braced robot game content.
Real 3-rig, all-original-owner 227 pair regression + all-new-armor-owner 291 pair
exhaustive comparison, without touching producer or Codex Assets.
The graph is SOURCE LOGICAL (six faces, source unit cell), never Unity PhysX.
"""
import argparse
import copy
import hashlib
import itertools
import json
import pathlib
import re
import subprocess
import sys

BASE="0978497561c8b6400490235d3e5a401ac32bea2d"
WEAK="098ccdbe8f129253fa4ada52318436b7cc2b4dc4"
BRACE="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
PS="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
FACES=((1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1))
ROOT=pathlib.Path(__file__).resolve().parent
SOURCES=[
 ("partsize",BASE,"Assets/PARTSIZE001/Runtime/PartSizeContract.cs","07455919a5544875fee5a78ecbbce36dc6cb49c2"),
 ("catalog",BASE,"Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs","6153d1758e800ec5c4210323610b061c06e8ea85"),
 ("rigvalidator",BASE,"Tools/B01/PARTSIZE001/P17Rigs/RigValidator.cs","988fc31d8633e5429fc4ba377856187e5ee7601c"),
 ("old_flanker",BASE,"Assets/PARTSIZE001/Blueprints/P17/P17_Flanker_Wheels.json","c4778df1db431ed1b51e8fb1e607ae1ede3e1bef"),
 ("old_bulwark",BASE,"Assets/PARTSIZE001/Blueprints/P17/P17_Bulwark_Tracks.json","2fe5b441d081ad3590ccbeabf1cbb1f266ec662a"),
 ("old_skirmisher",BASE,"Assets/PARTSIZE001/Blueprints/P17/P17_Skirmisher_Hover.json","4d7b4ab0adfb71fa2c20479eaa7c266687d28fa1"),
 ("weakdata",WEAK,"Assets/PARTSIZE001/Weakpoints/P17/P17_SingleBreakOutcomes.json","eb4ab2e2cd6d4c34b6a014b00b6c86efc472cfda"),
 ("bracing_engine",BRACE,"Tools/B01/PARTSIZE001/P17Bracing/BracingEngine.cs","fe1b49ac0f22cb30a901bed8754c6f08ab8af624"),
 ("new_flanker",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_Flanker_Reinforced.json","7259e3454b2f7dd72ca66de83673c143075a807f"),
 ("new_bulwark",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_Bulwark_Reinforced.json","8b00e5da047a22bc23644639e3292c447dd6b335"),
 ("new_skirmisher",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_Skirmisher_Reinforced.json","edf332a66645c5ea4374b1957d2cc1a7d356e4d2"),
 ("brace_kits",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_StructuralBracingKits.json","9b3d2403e7f6e32ea6e20f9f5bb0a83b859bc6e1"),
 ("brace_folder_meta",BRACE,"Assets/PARTSIZE001/Bracing.meta","4c55946b3623dc5eeb003267120b54c70ef81adc"),
 ("brace_p17_meta",BRACE,"Assets/PARTSIZE001/Bracing/P17.meta","041721c8ca141eccde7a6c8488b27bfc5db423ac"),
 ("flanker_meta",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_Flanker_Reinforced.json.meta","355d74ba783df457d176cb62acf980b5886f1ff4"),
 ("bulwark_meta",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_Bulwark_Reinforced.json.meta","db00bb565ca0e60d237caa0a2719357ecf2c62de"),
 ("skirmisher_meta",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_Skirmisher_Reinforced.json.meta","ff7a9e2a567840a020b1dceec42d7ee651b62d23"),
 ("kit_meta",BRACE,"Assets/PARTSIZE001/Bracing/P17/P17_StructuralBracingKits.json.meta","1a195e371c3106b2e059781248b0a50c8963bb92"),
]
def fail(message):
    raise AssertionError(message)

def ensure(value,message):
    if not value:fail(message)

def git(root,*args):
    cmd=["git","-C",str(root),*args]
    p=subprocess.run(cmd,capture_output=True,check=False)
    if p.returncode:
        fail("GIT_ERROR "+repr(args)+" "+p.stderr.decode(errors="replace")[-400:])
    return p.stdout

def load_pin(root):
    for ref in (BASE,WEAK,BRACE,PS):
        ensure(git(root,"rev-parse",ref+"^{commit}").decode().strip()==ref,"MISSING_IMMUTABLE_COMMIT "+ref)
    out={}
    for key,ref,path,sha in SOURCES:
        ensure(len(sha)==40,"MALFORMED_DECLARED_SOURCE_SHA "+key)
        actual=git(root,"rev-parse",ref+":"+path).decode().strip()
        ensure(actual==sha,"ORIGINAL_SOURCE_CHANGED "+key+" got "+actual)
        out[key]=git(root,"show",ref+":"+path)
    manifest=json.loads((ROOT/"manifest.json").read_text(encoding="utf-8"))
    for k in ("bracing_cross_receiver.py",):
        own=git(root,"hash-object",str(ROOT/k)).decode().strip()
        ensure(manifest["receiver_git_blob"]==own,"RECEIVER_SELF_SHA_DRIFT "+own)
    original_contract=out["partsize"].decode()
    original_catalog=out["catalog"].decode()
    validate=out["rigvalidator"].decode()
    brace_engine=out["bracing_engine"].decode()
    ensure("GridMeters=.55" in original_contract and "new Cell3(1,1,1)" in original_contract,"SOURCE_CELL_METRIC_DRIFT")
    ensure("new Cell3(1,1,1)" in original_catalog and '"Armor"' in original_catalog and '"Core"' in original_catalog,"SOURCE_CATALOG_DRIFT")
    ensure("part.ContactMask!=63" in validate and "DISCONNECTED_MODULE_FROM_CORE" in validate,"ORIGINAL_SOCKET_LOGIC_CHANGED")
    ensure("WeakpointEngine.Simulate" in brace_engine and "MandatoryParts" in brace_engine,"PRODUCER_BRACE_SOURCE_CHANGED")
    print("GIT_ORIGINAL_SOURCE_BLOBS=%d/18 PASS | PARTSIZE_LOGICAL_CELL=0.55m | UNIT_ALL_COVERED=YES"%len(out))
    return out

def locate(part):
    point=part["cell"]
    rot=part["rotationQuarterTurns"]
    ensure(rot=={"x":0,"y":0,"z":0},"UNSUPPORTED_NONZERO_ROTATION")
    ensure(part["kind"] in ("Core","Armor","Wheel","CrawlerTrack","HoverEngine","Gun"),"UNAPPROVED_KIND")
    ensure(part["contactMask"]==63,"NOT_SIX_FACE_CONTACT")
    return (point["x"],point["y"],point["z"])

def identity(parts):
    return {p["localId"]:p for p in parts}

def check_rig(rig):
    ids=[p["localId"] for p in rig["parts"]]
    ensure(len(set(ids))==len(ids),"DUPLICATED_ORIGINAL_OWNER")
    ensure(sum(p["kind"]=="Core" for p in rig["parts"])==1,"CORE_COUNT_DRIFT")
    cells=[locate(p) for p in rig["parts"]]
    ensure(len(cells)==len(set(cells)),"ORIGINAL_LOGICAL_CELL_OVERLAP")
    ensure(rig["nativePhysicalStatus"]=="UNMEASURED_COLLIDER_AND_SOCKET","FORGED_PHYSICS_APPROVAL")
    ensure(rig["contentStatus"]=="GAME_CONTENT_CANDIDATE_NOT_INTEGRATED","FORGED_INTEGRATION")
    return identity(rig["parts"])

def assess(rig,removed):
    owner=check_rig(rig)
    removed=set(removed)
    ensure(removed.issubset(owner),"REMOVAL_OF_UNKNOWN_OWNER")
    ensure(len(removed)<=2,"MORE_THAN_TWO_REMOVALS_UNSUPPORTED")
    remaining={p:info for p,info in owner.items() if p not in removed}
    located={locate(info):p for p,info in remaining.items()}
    core=next(p for p,v in owner.items() if v["kind"]=="Core")
    connected=set()
    if core in remaining:
        queue=[core];connected.add(core)
        while queue:
            part=queue.pop()
            x,y,z=locate(remaining[part])
            for dx,dy,dz in FACES:
                nxt=located.get((x+dx,y+dy,z+dz))
                if nxt and nxt not in connected:
                    connected.add(nxt);queue.append(nxt)
    lost=set(remaining)-connected
    existing=[remaining[x] for x in connected]
    drive=rig["selectedDrive"]
    ensure(drive in ("Wheels","Tracks","Hover"),"INVALID_DRIVE")
    wanted={"Wheels":"Wheel","Tracks":"CrawlerTrack","Hover":"HoverEngine"}[drive]
    works=(any(p["kind"]==wanted for p in existing) if drive=="Hover"
        else any(p["kind"]==wanted and p["cell"]["x"]<0 for p in existing)
           and any(p["kind"]==wanted and p["cell"]["x"]>0 for p in existing))
    return {"removed":sorted(removed),"connected":sorted(connected),
        "detached":sorted(lost),"drive":works,
        "guns":sum(p["kind"]=="Gun" for p in existing),
        "coreLost":core in removed,"nativePhysicalStatus":"NOT_RUN"}

def verify(source):
    raw=lambda name:json.loads(source[name].decode("utf-8"))
    base=[raw(x) for x in ("old_flanker","old_bulwark","old_skirmisher")]
    variants=[raw(x) for x in ("new_flanker","new_bulwark","new_skirmisher")]
    kits=raw("brace_kits")
    weak=raw("weakdata")
    ensure([len(x["parts"]) for x in base]==[14,14,10],"BASE_OWNER_COUNT")
    ensure([len(x["parts"]) for x in variants]==[15,16,12],"REINFORCED_OWNER_COUNT")
    ensure(kits["nativePhysicalStatus"]=="NOT_RUN" and kits["gameIntegrationStatus"]=="NOT_INTEGRATED","KIT_FORGED_NATIVE")
    ensure(len(kits["kits"])==3,"WRONG_KIT_COUNT")
    ensure(sum(len(x["mandatoryParts"]) for x in kits["kits"])==5,"FIVE_BRACES_EXPECTED")
    ensure(sum(len(x["expectedMitigations"]) for x in kits["kits"])==6,"SIX_AUTHOR_TARGETS_EXPECTED")
    ensure(weak["evidenceClass"]=="SOURCE_LOGICAL_ONLY_UNVERIFIED_PHYSX","WEAK_SOURCE_NOT_LOGICAL")
    ensure(len(weak["rigs"])==3,"ORIGINAL_WEAKPOINT_FIXTURE_COUNT")
    guids=[]
    for name in ("brace_folder_meta","brace_p17_meta","flanker_meta","bulwark_meta","skirmisher_meta","kit_meta"):
        matches=re.findall(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",source[name].decode("utf-8"))
        ensure(len(matches)==1,"UNITY_META_GUID_INVALID "+name)
        guids.append(matches[0].lower())
    ensure(len(set(guids))==6,"UNITY_META_GUID_DUPLICATED")

    single_source=0
    compared=0
    improved=0
    pair_report=[]
    supplemental=[]
    mitigations=0
    total_added=0
    for index,(b,v,k) in enumerate(zip(base,variants,kits["kits"])):
        old=check_rig(b);new=check_rig(v)
        ensure(k["baseBlueprintId"]==b["blueprintId"] and k["variantId"]==v["blueprintId"],"KIT_WRONG_BLUEPRINT_SCOPE")
        ensure(b["selectedDrive"]==v["selectedDrive"],"KIT_CHANGED_SELECTED_DRIVE")
        ensure(all(new[i]==p for i,p in old.items()),"KIT_MUTATED_ORIGINAL_PART")
        extra=set(new)-set(old)
        expected={p["localId"] for p in k["mandatoryParts"]}
        ensure(extra==expected,"KIT_PART_ID_MISMATCH")
        ensure(all(new[x]==p for x,p in ((p["localId"],p) for p in k["mandatoryParts"])),"KIT_PART_SPEC_MISMATCH")
        ensure(all(new[x]["kind"]=="Armor" and new[x]["weapon"] is None for x in extra),"KIT_NOT_ARMOR_ONLY")
        total_added+=len(extra)
        ensure(not assess(b,[])["detached"] and not assess(v,[])["detached"],"BASELINE_OR_VARIANT_ALREADY_DISCONNECTED")
        cases=weak["rigs"][index]["singleBreakCases"]
        ensure(len(cases)==len(old),"ORIGINAL_WEAKPOINT_CASES_MISSING")
        for case in cases:
            before=assess(b,[case["removedPartId"]])
            ensure(before["coreLost"]==case["coreLost"]
                and before["detached"]==case["detachedPartIds"]
                and len(before["detached"])==case["detachedCount"]
                and before["drive"]==case["selectedDriveOperable"]
                and before["guns"]==case["connectedGunCount"],"ORIGINAL_38_SINGLE_LOSS_DATA_CHANGED")
            ensure(case["nativePhysicalStatus"]=="NOT_RUN","FORGED_OLD_WEAKPOINT_NATIVE")
            single_source+=1
            after=assess(v,[case["removedPartId"]])
            ensure(old.keys() & set(after["detached"]) <= set(before["detached"]), "SINGLE_PART_REGRESSION_DISCONNECT")
            ensure(after["guns"]>=before["guns"],"SINGLE_PART_REGRESSION_GUNS")
            ensure(not before["drive"] or after["drive"],"SINGLE_PART_REGRESSION_DRIVE")

        for m in k["expectedMitigations"]:
            rem=m["removedPartId"];rescued=m["previouslyOrphanedPartId"]
            before=assess(b,[rem]);after=assess(v,[rem])
            ensure(rescued in before["detached"],"AUTHOR_MITIGATION_OLD_NOT_ORPHAN")
            ensure(rescued in after["connected"],"AUTHOR_MITIGATION_NEW_NOT_RESCUED")
            mitigations+=1
        for first,second in itertools.combinations(old,2):
            removed=[first,second]
            before=assess(b,removed)
            after=assess(v,removed)
            ensure(set(old)&set(after["detached"]) <= set(before["detached"]),"PAIR_REGRESSION_ORIGINAL_OWNER")
            ensure(after["guns"]>=before["guns"],"PAIR_REGRESSION_CONNECTED_GUNS")
            ensure(not before["drive"] or after["drive"],"PAIR_REGRESSION_DRIVE")
            ensure(after["coreLost"]==before["coreLost"],"PAIR_CORE_PARITY")
            saved=sorted((set(before["detached"])-set(after["detached"]))&set(old))
            if saved or after["guns"]>before["guns"] or (after["drive"] and not before["drive"]):
                improved+=1
            pair_report.append({"rig":b["blueprintId"],"destroyed":sorted(removed),
                "baseDetached":before["detached"],"reinforcedDetachedOriginal":sorted(set(after["detached"])&set(old)),
                "rescuedOriginalOwners":saved,"baseDrive":before["drive"],"reinforcedDrive":after["drive"],
                "baseGuns":before["guns"],"reinforcedGuns":after["guns"]})
            compared+=1
        for first,second in itertools.combinations(new,2):
            if first in old and second in old: continue
            a=assess(v,[first,second])
            supplemental.append({"rig":v["blueprintId"],"destroyed":a["removed"],
                "detached":a["detached"],"drive":a["drive"],"guns":a["guns"],"nativePhysicalStatus":"NOT_RUN"})
    ensure((single_source,compared,len(supplemental),mitigations,total_added)==(38,227,64,6,5),
           "CASE_COVERAGE_MISMATCH")
    ensure(improved>=6,"EXPECTED_MEASURABLE_DOUBLE_BREAK_IMPROVEMENTS_MISSING")
    print(f"ORIGINAL_38_SINGLE_FAILURE_CASES={single_source}/38 PASS")
    print(f"NEW_SAME_ORIGINAL_OWNER_DOUBLE_FAILURES={compared}/227 MONOTONIC_PASS")
    print(f"NEW_EXTRA_BRACE_OWNER_DOUBLE_FAILURES={len(supplemental)}/64 ENUMERATED")
    print(f"AUTHOR_EXPLICIT_MITIGATIONS={mitigations}/6 INDEPENDENTLY_RESCUED")
    print(f"PAIR_PROTECTION_IMPROVEMENTS={improved}/227 | ORIGINAL_OWNER_REGRESSIONS=0")
    print(f"ORIGINAL_NEW_ARMOR_OWNERS={total_added}/5 | ORIGINAL_META_GUIDS={len(guids)}/6 UNIQUE")
    return {"schema":"B01-61-P17-W03-BRACING-ACCEPTANCE-001","source":"SOURCE_LOGICAL_ONLY",
        "nativeUnity":"NOT_RUN","producerOriginalCommit":BASE,"producerBracingCommit":BRACE,
        "producerWeakpointCommit":WEAK,"manifestOriginalGitBlobCount":len(source),
        "oldSingleValidated":single_source,"commonPairCount":compared,"bracedAdditionalPairs":len(supplemental),
        "authorMitigationsVerified":mitigations,"improvedSharedPairCases":improved,
        "sharedPairOutcomes":pair_report,"additionalBracePairOutcomes":supplemental}

def mutations(source):
    checks=(
       ("fake_native_pass",lambda s:s["brace_kits"].__setitem__("nativePhysicalStatus","PASS")),
       ("mutated_original_owner",lambda s:s["new_flanker"]["parts"][0]["cell"].__setitem__("x",99)),
       ("armor_overlap",lambda s:s["new_bulwark"]["parts"][-1]["cell"].update(s["new_bulwark"]["parts"][0]["cell"])),
       ("kit_part_mismatch",lambda s:s["brace_kits"]["kits"][0]["mandatoryParts"][0]["cell"].__setitem__("z",900)),
       ("fake_mitigation",lambda s:s["brace_kits"]["kits"][0]["expectedMitigations"][0].__setitem__("previouslyOrphanedPartId","FAKE")),
       ("orphaned_new_brace",lambda s:s["new_skirmisher"]["parts"][-1]["cell"].__setitem__("z",123))
    )
    parsed={k:json.loads(v.decode("utf-8")) for k,v in source.items() if k in (
        "new_flanker","new_bulwark","new_skirmisher","brace_kits")}
    for name,change in checks:
        fake=copy.deepcopy(parsed)
        change(fake)
        altered=dict(source)
        for key,value in fake.items():
            altered[key]=json.dumps(value).encode()
        try:verify_quiet(altered)
        except AssertionError:print("NEGATIVE_RECEIVER_MUTANT_REJECTED="+name)
        else:fail("FALSE_GREEN_FAKE_GAME_CONTENT "+name)
    print("NEGATIVE_MUTATIONS=6/6 REJECTED")

def verify_quiet(source):
    import contextlib
    import io
    with contextlib.redirect_stdout(io.StringIO()):
        verify(source)

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--output",default="bracing_cross_acceptance.json")
    args=p.parse_args()
    repo=pathlib.Path(args.repo).resolve()
    src=load_pin(repo)
    result=verify(src)
    mutations(src)
    target=pathlib.Path(args.output)
    encoded=json.dumps(result,indent=2,ensure_ascii=False,sort_keys=True)+"\n"
    target.write_text(encoded,encoding="utf-8")
    digest=hashlib.sha256(encoded.encode("utf-8")).hexdigest()
    print("ORIGINAL_SOURCE_RECEIVER_REPORT="+str(target)+" SHA256="+digest)
    print("B0161_W03_BRACING_ACCEPTANCE=PASS | 38/38 HISTORICAL | 227/227 COMMON_PAIRS | 64/64 NEW_ARMOR_PAIRS | 6/6 MITIGATIONS | MUTANTS=6/6 REJECTED | UNITY_NATIVE=NOT_RUN | CODEX_ACK=PENDING")
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except (AssertionError,ValueError,KeyError,TypeError) as exc:
        print("B0161_W03_BRACING_RED="+repr(exc),file=sys.stderr)
        sys.exit(2)
