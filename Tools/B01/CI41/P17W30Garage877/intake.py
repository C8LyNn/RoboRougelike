#!/usr/bin/env python3
"""B01-41 W30 independent exact #873 + #877 REAL source intake (NOT native Unity).
Anchors source to owner commits; inspects the ENTIRE actual HEAD Unity asset tree
for GUID/assembly conflicts, and traces canonical preview->BUY/MOUNT acceptance.
"""
import hashlib,json,re,subprocess,sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
GAME="d537b7310979b91df27a08b14d0f1c74d0889089"
GARAGE="20273c3580079dfb299c30c85a67e76f8d893e7f"
BASE="ed8cf4bcc131688b9a93968a89683d2496586683"
SCOPE={
 "Assets/R3/R3BuildValidation.cs":"5f3163c68629535fe008ce3e7dd8eb51e910a0ba",
 "Assets/Tests/Editor/GarageEditFreedomTests.cs":"51afabcf5fd03de4e577613bff324d9f485002fb",
 "Docs/W29/GARAGE_FUNCTIONAL_SWEEP_GATE.md":"0373d99729e6c045bdb6176ae925b35c52644529",
}
REQUIRED={
 "Assets/R3/R3VehicleModel.cs":None,
 "Assets/R3/R3GarageView.cs":None,
 "Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs":None,
 "Assets/R5/R5Arena.cs":None,
 "Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs":None,
 "Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs":None,
 "Tools/B01/Codex/W29/verify_source.py":None,
}
COLORS=("OccupiedRed","ProtrusionBlue","WeaponPurple","MechanismOrange","ValidGreen")
class GateError(Exception): pass

def git(*args):
    p=subprocess.run(["git",*args],cwd=ROOT,capture_output=True)
    if p.returncode:raise GateError(f"Git command failed {args}: "+p.stderr.decode("utf-8","replace")[-200:])
    return p.stdout

def oid(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()

def show(ref,path):
    if not re.fullmatch(r"[0-9a-f]{40}",ref) or not path.startswith(("Assets/","Docs/","Tools/")) or ".." in Path(path).parts or "\\" in path:
        raise GateError("unsafe or mutable file ref "+path)
    return git("show",ref+":"+path)

def test_origins(fetch=show):
    actual=set(git("diff","--name-only",GAME,GARAGE).decode().splitlines())
    if actual!=set(SCOPE):raise GateError("original #877 exactly-three-file scope drift "+str(sorted(actual^set(SCOPE))))
    if git("merge-base",GAME,GARAGE).decode().strip()!=GAME:raise GateError("#877 not descendant of #873")
    if git("merge-base",BASE,GAME).decode().strip()!=BASE:raise GateError("game not descendant of playable #824")
    result={}
    for path,expected in SCOPE.items():
        content=fetch(GARAGE,path)
        if oid(content)!=expected:raise GateError("producer Git blob changed "+path)
        result[path]={"git_blob":expected,"sha256":hashlib.sha256(content).hexdigest(),"bytes":len(content)}
    return result

def source_contract(texts):
    build=texts["Assets/R3/R3BuildValidation.cs"]
    model=texts["Assets/R3/R3VehicleModel.cs"]
    preview=texts["Assets/R3/R3GarageView.cs"]
    overlay=texts["Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs"]
    arena=texts["Assets/R5/R5Arena.cs"]
    emp=texts["Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs"]
    jump=texts["Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs"]
    tests=texts["Assets/Tests/Editor/GarageEditFreedomTests.cs"]
    def before(src, earlier, later):
        p=src.find(earlier);q=src.find(later)
        return p>=0 and q>=0 and p<q
    policies={
      "single_installation_gate":"canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate)" in build and
                                 "return installationGate(candidate)" in model,
      "preview_uses_both_model_errors":"OwnedPlacementError(Owned.Id,Cell,SelectedRotation)" in preview and
                                        "PlacementError(Cell,MountKind,false,MountWeapon,SelectedRotation,SelectedContactMask)" in preview,
      "buy_checks_before_scrap":before(model,"PlacementError(cell,kind,false,weapon,rotation)!=null","Scrap-=Price(kind,weapon)"),
      "mount_checks_before_inventory_mutation":before(model,"OwnedPlacementError(part.Id,cell,rotation)!=null","inventory.Remove(part);part.Cell=cell"),
      "physical_original_BUILD001_before_sweep":before(build,"BuildPlacementRules.CheckForInstallation(","return FunctionalWeaponPlacementIssue(candidate,probe);"),
      "saw_physics_not_a_projectile":"RotarySawContactMath.IsOnCuttingDisc(" in build and
                                "Physics.OverlapSphere(muzzle.position,extent" in build,
      "directional_hard_segment_only":"Physics.SphereCastAll(pivot.position,radius,delta.normalized," in build and
                                     "delta.magnitude" in build,
      "emp_radial_exemption":"candidate.Weapon==R4WeaponKind.EmpEmitter)return null;" in build and
                             "activationGate != null && !activationGate()" in emp,
      "probe_self_excluded":"!other.transform.IsChildOf(probe.transform)" in build and
                            "other.transform.IsChildOf(probe.transform)" in build,
      "physical_probe_cleaned":"probe.SetActive(false)" in build and
                               "DestroyImmediate(probe)" in build,
      "source_nunit_wallet_and_id":"RealGarageRejectsOwnedSawDiscPenetrationBeforeInventoryMutation" in tests and
                             "beforeWallet" in tests and "beforeParts" in tests and
                             "run.GarageMount(id,oldCell,default),Is.False" in tests,
      "five_colors_observer_only":all(x in overlay for x in COLORS) and
                      "source.PlacementIssue" in overlay and
                      "garage.PreviewValid" in overlay,
      "actual_R5_map_uses_jump_pad":"if(kind==R5ArenaKind.SalvageYard)JumpPad(" in arena and
                                    "AddComponent<B01.MapJumpPad001.MapJumpPad>()" in arena and
                                    "ForceMode.VelocityChange" in jump,
      "runtime_probe_uses_actual_game_scaling":"R41PartGeometry.Grid" in build and "Collider" in build,
    }
    if not all(policies.values()):
        raise GateError("canonical garage/gameplay source policy RED "+str([k for k,v in policies.items() if not v]))
    return policies

def original_unity_registry(ref=GARAGE):
    paths=git("ls-tree","-r","--name-only",ref,"Assets").decode().splitlines()
    guids={};asmdefs={};aliases={}
    for path in paths:
        cf=path.casefold()
        if cf in aliases and aliases[cf]!=path:raise GateError("case-fold path collision "+path)
        aliases[cf]=path
        if not (path.endswith(".meta") or path.endswith(".asmdef")):continue
        content=show(ref,path).decode("utf-8","replace")
        if path.endswith(".meta"):
            m=re.search(r"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$",content)
            if not m:raise GateError("Unity .meta lacks GUID "+path)
            guid=m.group(1).lower()
            if guid in guids:raise GateError("duplicate Unity GUID "+guid+" at "+path)
            guids[guid]=path
        else:
            try: title=json.loads(content)["name"]
            except (ValueError,KeyError,TypeError):raise GateError("invalid asmdef name "+path)
            if title in asmdefs:raise GateError("duplicate assembly name "+title)
            asmdefs[title]=path
    if len(guids)<100 or len(asmdefs)<10:
        raise GateError("unexpectedly small Unity source tree")
    return {"source_asset_files":len(paths),"unique_guids":len(guids),
            "unique_assembly_names":len(asmdefs),"casefold_collisions":0}

def main(outpath):
    if outpath.exists():raise GateError("disposable evidence directory already exists (refuse overwrite)")
    outpath.mkdir(parents=True)
    provenance=test_origins()
    refs=dict(REQUIRED)
    refs["Assets/R3/R3BuildValidation.cs"]=None
    refs["Assets/Tests/Editor/GarageEditFreedomTests.cs"]=None
    contents={p:show(GARAGE,p).decode("utf-8-sig","replace") for p in refs}
    contracts=source_contract(contents)
    registry=original_unity_registry()
    data={"package":"B01-41-P17W30-CANONICAL-GARAGE877-SOURCE-G0-R1",
          "base_playable_codex824":BASE,"selected_game873":GAME,"selected_garage877":GARAGE,
          "files":provenance,"policies":contracts,"source_registry":registry,
          "unity_native_G1":"NOT_RUN","win_player":"NOT_RUN",
          "formal_source_G0":"PARTIAL_SELECTED_SOURCE_ONLY",
          "full_game_G0":"HOLD_UNTIL_B01_40_61_AND_NATIVE_ABI",
          "garage_five_colors":"PRESENT_SOURCE_NOT_NATIVE_RENDER_ACCEPTED",
          "purple_orange_dynamic_sweep":"ADVISORY_NOT_FULL_PHYSICAL_SWEEP",
          "real_wallet_id_and_saw_gameplay":"NATIVE_UNVERIFIED",
          "save826_game_binder":"NOT_IN_877",
          "saw861":"NOT_IN_877","softlock812":"PRODUCT_RED",
          "integrated_gameplay_verified":"NO","windows_extracted_build":"NOT_AVAILABLE_FROM_THIS_CI"}
    data_bytes=(json.dumps(data,sort_keys=True,indent=2,ensure_ascii=False)+"\n").encode("utf-8")
    (outpath/"receipt.json").write_bytes(data_bytes)
    print("CI41_W30_877_EXACT_PARENT_AND_3_ORIGINAL_BLOBS_PASS")
    print("CI41_W30_REAL_GAME_SOURCE_GATES_PASS",len(contracts))
    print("CI41_W30_REAL_UNITY_GUID_ASMDEF_CASEFOLD_PASS",registry)
    print("CI41_W30_EVIDENCE_RECEIPT_SHA256="+hashlib.sha256(data_bytes).hexdigest())
    print("CI41_W30_G1_UNITY_NOT_RUN_WINDOWS_PLAYER_NOT_RUN_FULL_SWEEP_HOLD")
if __name__=="__main__":
    try:
        if len(sys.argv)!=2:raise GateError("usage: intake.py <fresh-output-dir>")
        main(Path(sys.argv[1]).resolve())
    except (GateError,AssertionError,ValueError,OSError) as ex:
        print("CI41_W30_SOURCE_G0_FAIL_CLOSED",str(ex),file=sys.stderr)
        sys.exit(2)
