#!/usr/bin/env python3
"""Independent B01-62 #510 source-pinned save/migration QA. NOT a game serializer or Unity."""
import argparse, hashlib, json, re, shutil, subprocess
from pathlib import Path

SHA="d94400d95d13db7f27e1533df05c9292d5380431"
SCHEMA="b01-62-migration-qa-fixture-v1"
COMPONENT="B01.SecureReattachV2.SecureDebrisPlaygroundV2"
IDS=(2310001,2310002)
PINS={
 "ledger":("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs","b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
 "host":("Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs","328977b4cdcdcd7c9f128c01f11ff67f8e46086a"),
 "policy":("Assets/B0123SecureReattachV2/Runtime/SecureDockPolicy.cs","9f80f705475bd312f209033a57bd07e03bf95628"),
 "spec":("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs","b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
 "scene":("Assets/B0123SecureReattachV2/Editor/SecureReattachSceneBuilder.cs","07de6c8ce784b714699ce5598564aab2b2f1db29")
}
class Hold(ValueError):pass

def data_json(x):
    return (json.dumps(x,sort_keys=True,separators=(",",":"),ensure_ascii=True)+"\n").encode()

def blob(x):
    return hashlib.sha1(b"blob "+str(len(x)).encode()+b"\0"+x).hexdigest()

def originals(repo):
    result={}
    for label,(path,expected) in PINS.items():
        ref=SHA+":"+path
        raw=subprocess.check_output(["git","-C",str(repo),"show",ref])
        actual=subprocess.check_output(["git","-C",str(repo),"rev-parse",ref]).decode().strip()
        if blob(raw)!=expected or actual!=expected:raise Hold("PINNED_GIT_BLOB_DRIFT:"+label)
        result[label]=raw.decode("utf-8-sig")
    return result

def no_comments(s):
    return re.sub(r"/\*.*?\*/|//[^\n]*","",s,flags=re.S)

def source_gate(src):
    host,ledger,policy,scene,spec=[no_comments(src[i]) for i in ("host","ledger","policy","scene","spec")]
    for alias in ("Reattach","TrySafeReattach","ReattachFromGui"):
        pattern=rf"public\s+bool\s+{alias}\s*\(int\s+index\)\s*=>\s*DockGate\(\)\.{alias}\(index\)"
        if not re.search(pattern,host):raise Hold("PUBLIC_ROUTE_DRIFT:"+alias)
    tokens=[
      (ledger,"private readonly DebrisImpulseLedger consumed","W06_LEDGER_DRIFT"),
      (ledger,"consumed.RearmAfterReattach(id)","ATOMIC_RESERVE_MISSING"),
      (ledger,"applyPhysical();","APPLY_CALLBACK_MISSING"),
      (ledger,"rollbackPhysical();","ROLLBACK_CALLBACK_MISSING"),
      (ledger,"consumed.TryDetach(","ROLLBACK_LEDGER_COMPENSATION_MISSING"),
      (host,"rig.Ledger.TryReattach(rig.Spec.PartId,","ATOMIC_COMMIT_CALL_MISSING"),
      (host,"Physics.OverlapBoxNonAlloc(","ALL_LAYER_BROADPHASE_MISSING"),
      (host,"Physics.ComputePenetration(","NARROWPHASE_MISSING"),
      (policy,"DockResult.TransactionRejected","TRANSACTION_REJECT_VERDICT_MISSING"),
      (scene,"NewScene(NewSceneSetup.EmptyScene","SCENE_NOT_ISOLATED"),
      (scene,"controller.AddComponent<SecureDebrisPlaygroundV2>()","WRONG_SCENE_CONTROLLER")
    ]
    for s,token,error in tokens:
        if token not in s:raise Hold(error)
    if any(re.search(r"AddComponent\s*<\s*"+cls+r"\s*>",scene) for cls in
           ("DebrisRigPlayground","DebrisSafeReattachGate","HardenedDockDemo")):
        raise Hold("LEGACY_CONTROLLER_IN_SELECTED_SCENE")
    if "new SecureRigSpec(2310001" not in spec or "new SecureRigSpec(2310002" not in spec:
        raise Hold("STABLE_ID_DRIFT")
    persisted=any(re.search(pattern,s) for s in (host,ledger,policy) for pattern in
       (r"\bSaveGame\s*\(",r"\bLoadGame\s*\(",r"\bSaveState\s*\(",r"\bLoadState\s*\(",
        r"\bISerializationCallbackReceiver\b",r"\bJsonUtility\.ToJson\s*\(",
        r"\bFile\.WriteAll(?:Text|Bytes)\s*\("))
    return {"source_sha":SHA,"public_routes":3,"isolated_scene_controller":COMPONENT,
            "ledger_rollback_source_present":True,"persistent_save_api_detected":persisted,
            "save_load_acceptance":"NOT_VERIFIED" if persisted else "HOLD_NO_PERSISTENT_SAVE_CONTRACT",
            "native_unity":"NOT_RUN"}

def clean_fixture():
    return {"schema":SCHEMA,"source_sha":SHA,"components":[COMPONENT],
            "rigs":[{"part_id":part,"detached":False,"consumed":False} for part in IDS]}

def preflight(snapshot):
    if not isinstance(snapshot,dict) or set(snapshot)!=set(clean_fixture()):
        raise Hold("BAD_QA_FIXTURE_SCHEMA")
    if snapshot["schema"]!=SCHEMA or snapshot["source_sha"]!=SHA:
        raise Hold("WRONG_VERSION_OR_CANDIDATE")
    if snapshot["components"]!=[COMPONENT]:
        raise Hold("LEGACY_OR_ALT_COMPONENT")
    rigs=snapshot["rigs"]
    if not isinstance(rigs,list) or len(rigs)!=2:raise Hold("BAD_RIG_COUNT")
    found=[]
    for rig in rigs:
        if not isinstance(rig,dict) or set(rig)!={"part_id","detached","consumed"}:
            raise Hold("BAD_RIG_RECORD")
        if type(rig["part_id"]) is not int or type(rig["detached"]) is not bool or type(rig["consumed"]) is not bool:
            raise Hold("BAD_RIG_TYPE")
        found.append(rig["part_id"])
        if rig["detached"] or rig["consumed"]:
            raise Hold("UNSUPPORTED_DETACHED_OR_CONSUMED_SAVE")
    if found!=list(IDS):raise Hold("DUPLICATE_OR_REORDERED_STABLE_IDS")
    return "QA_FIXTURE_ONLY"

def safe_target(target,repo,new=False):
    target=Path(target).resolve(strict=False); root=Path(repo).resolve()
    if target==root or root in target.parents or target.is_symlink():raise Hold("UNSAFE_TARGET")
    if new and (target.exists() or not target.parent.is_dir()):raise Hold("STAGE_TARGET_EXISTS")
    if not new and not target.is_dir():raise Hold("MISSING_STAGE")
    return target

def stage(snapshot,target,repo):
    preflight(snapshot)
    target=safe_target(target,repo,True)
    raw=data_json(snapshot)
    receipt=data_json({"schema":"b01-62-stage-receipt-v1","source_sha":SHA,
         "fixture_sha256":hashlib.sha256(raw).hexdigest(),
         "mode":"DISPOSABLE_QA_ONLY","unity":"NOT_RUN"})
    target.mkdir()
    try:
        (target/"snapshot.json").write_bytes(raw)
        (target/"receipt.json").write_bytes(receipt)
    except Exception:
        shutil.rmtree(target);raise
    return hashlib.sha256(receipt).hexdigest()

def rollback(target,repo):
    target=safe_target(target,repo)
    if any(p.is_symlink() for p in target.rglob("*")):raise Hold("STAGE_HAS_SYMLINK")
    files={p.relative_to(target).as_posix():p for p in target.rglob("*") if p.is_file()}
    if set(files)!={"snapshot.json","receipt.json"}:raise Hold("FOREIGN_OR_MISSING_FILE")
    try:
        raw=files["snapshot.json"].read_bytes()
        receipt_raw=files["receipt.json"].read_bytes()
        snapshot=json.loads(raw);receipt=json.loads(receipt_raw)
    except (ValueError,UnicodeDecodeError) as err:raise Hold("BAD_STAGE_JSON") from err
    preflight(snapshot)
    valid={"schema":"b01-62-stage-receipt-v1","source_sha":SHA,
           "fixture_sha256":hashlib.sha256(data_json(snapshot)).hexdigest(),
           "mode":"DISPOSABLE_QA_ONLY","unity":"NOT_RUN"}
    if raw!=data_json(snapshot) or receipt_raw!=data_json(valid) or receipt!=valid:
        raise Hold("DIRTY_OR_FORGED_STAGE")
    shutil.rmtree(target)

def evidence(repo):
    check=source_gate(originals(repo))
    return {"schema":"b01-62-w12-migration-gate-v1","selected":SHA,
        "source_pins":{k:{"commit":SHA,"path":p,"git_blob":b} for k,(p,b) in sorted(PINS.items())},
        "source":check,"fixture_type":"NOT_CANONICAL_GAME_SAVE",
        "migration_approved":False,"g0":"SOURCE_QA_ONLY","g1":"NOT_RUN",
        "g2":"HOLD","codex_ack":False,"integrated":False}

if __name__=="__main__":
    ap=argparse.ArgumentParser();ap.add_argument("--repo",required=True);ap.add_argument("--output",required=True)
    args=ap.parse_args();p=Path(args.output)
    if p.exists() or not p.parent.is_dir():raise Hold("OUTPUT_MUST_BE_NEW")
    raw=data_json(evidence(args.repo));p.write_bytes(raw)
    print("PINNED_ORIGINAL_SOURCE=5/5")
    print("SAVE_LOAD_CONTRACT=HOLD_NO_PERSISTENT_SAVE_CONTRACT")
    print("RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
    print("G1_UNITY=NOT_RUN")
