#!/usr/bin/env python3
"""CI41 W28 anti-fake exact Codex W27 source provenance + authored Unity acceptance QA.
Source PASS DOES NOT certify Unity tests, which are NOT_RUN without Editor/NUnit.
"""
import hashlib,json,re,subprocess,sys
from pathlib import Path
BASE="e03d36f2b86f8c73a067e574b67237a8921f622a"
OWN_NATIVE="Assets/Tests/Editor/CI41W28SawEmpNativeRegressionTests.cs"
OWN_META=OWN_NATIVE+".meta"
PACKAGE="Tools/B01/CI41/P17W28/"
WORKFLOW=".github/workflows/b01-41-p17w28-saw-emp-native-source-g0.yml"
OWN={OWN_NATIVE,OWN_META,WORKFLOW}|{PACKAGE+x for x in ("README.md","manifest.json","source_gate.py","test_source_gate.py")}
ORIGINAL={
"Assets/R3/R3VehicleAssembly.cs":"ed97dd1b0ee3b77c839615497b249dcaee479bb8",
"Assets/WeaponIntegration/RRLSawBinding.cs":"a0bc49151fcecde159685f2a0841447606a9d291",
"Assets/WeaponIntegration/RRLEmpBinding.cs":"6ff22154024dd04ee72043f9766db1840218df03",
"Assets/RoboRougelikeMechanics/WPN005RotarySaw/Runtime/RotarySawModule.cs":"a79a81280d2ebd3a6f68c26619f7dda6335a5b5f",
"Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs":"f38da021c22de609a196876d508ec9d92fb9ff56",
"Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpReceiver.cs":"d04bfb3e132adb29d78fab3c4a2fdf7104d2b8ca",
"Assets/Tests/Editor/W27BeamClearanceTests.cs":"39147e59ef43d50514e74875642a9af7833855a6",
"Assets/Tests/Editor/W27SpecialWeaponClearanceTests.cs":"5041bbb68bbf10d859bec25dc52ff4356d39e0c4",
"Assets/R3/R3GarageView.cs":"fb6f4f21c2f3953e43e1a7b223ef5110dbcc26c1",
"Assets/R3/R3BuildValidation.cs":"d176f5b635194dcd07038461e2a1a9914b5b0b18",
"Assets/R1/W27WeaponPlayerQA.cs":"1f9023317f49488eb5ebbad93fb079d6b0d3b024"
}
GUID="b0141f28ca52fa78f212b07e4c241001"
TESTS=[
"Saw_ActualRotatingOwnedArmor_StopsBlockedMotorHeat_ThenResumes",
"Saw_OriginalOwnerContactsNeverReceiveDamageFromRotatingBlade",
"Emp_OriginalDirectModule_ExternalWallShieldsTarget_NotOwnVehicle",
"Emp_ActualDirectBinding_DedupOwnPhysicalModuleAndConsumesCooldownOnce"
]
def git(*args):
    p=subprocess.run(["git",*args],capture_output=True)
    if p.returncode:raise AssertionError("GIT_FAIL:"+str(args)+" "+p.stderr.decode(errors="replace")[-160:])
    return p.stdout.decode().strip()
def validate_native(src):
    needed=[
        "EditorSceneManager.NewScene(", "new EnterPlayMode()",
        "new ExitPlayMode()", "R3VehicleAssembly.Build(",
        "R4WeaponKind.RotarySaw","R4WeaponKind.EmpEmitter",
        "GetComponent<RRLSawBinding>()","GetComponent<RRLEmpBinding>()",
        "Physics.SyncTransforms()","GetComponent<RRLEmpPartState>()",
        "SetMotorRequested(true)","SendMessage(\"FixedUpdate\")",
        "saw.Module.Motor.Rpm","saw.Module.Motor.Heat",
        "saw.Module.DamageEventsLastTick",
        "binding.Module.ConfigurePhysics(","binding.Fire()",
        "LastReport.ReceiversAffected","enemy.IsDisrupted",
        "own.Receiver.IsDisrupted","state.DeviceReadyAt",
        "state.Ammo","Assert.That(","Object.Destroy("
    ]
    for token in needed:
        if token not in src:raise AssertionError("MISSING_ORIGINAL_PLAYER_REAL_UNITY_SCENARIO:"+token)
    for name in TESTS:
        if src.count("public IEnumerator "+name+"(")!=1:
            raise AssertionError("MISSING_OR_DUPLICATE_ACTUAL_UNITY_SCENARIO:"+name)
    if src.count("[UnityTest]")!=4:raise AssertionError("NOT_EXACTLY_4_GENUINE_NATIVE_UNITY_TESTS")
    for fake in ("class R3VehicleAssembly","class RRLSawBinding","class RRLEmpBinding",
                 "class EmpEmitter","class RotarySawModule","class Rigidbody","class Physics"):
        if fake in src:raise AssertionError("FAKE_GAMEPLAY_OR_PHYSICS_TYPE:"+fake)
    return len(needed)
def audit():
    assert git("rev-parse","--verify",BASE+"^{commit}")==BASE
    git("merge-base","--is-ancestor",BASE,"HEAD")
    changed=git("diff","--name-status",BASE,"HEAD").splitlines()
    expected={"A\t"+p for p in OWN}
    assert set(changed)==expected, "FOREIGN_CODEX_OR_PRODUCER_PATH_CHANGE:"+repr(changed)
    for path,blob in ORIGINAL.items():
        if git("rev-parse","HEAD:"+path)!=blob or git("rev-parse",BASE+":"+path)!=blob:
            raise AssertionError("LIVE_W27_GAME_ORIGINAL_BLOB_DRIFT:"+path)
    test=Path(OWN_NATIVE).read_text(encoding="utf-8")
    testtokens=validate_native(test)
    meta=Path(OWN_META).read_text(encoding="utf-8")
    assert meta.count("guid: "+GUID)==1 and len(re.findall(r"(?m)^guid:\s*[0-9a-f]{32}\s*$",meta))==1
    # Global unique GUID check on the original immutable tree: 5k+ file scan,
    # no new GUID may alias any existing original Unity asset.
    probe=subprocess.run(["git","grep","-l","-F",GUID,BASE,"--","Assets"],capture_output=True,text=True)
    assert probe.returncode==1 and not probe.stdout.strip(), (
        "REUSED_EXISTING_UNITY_GUID_OR_GIT_SCAN_FAILURE:"+probe.stdout[:100]+probe.stderr[:100])
    return {
      "package":"CI41-W28-CODEX824-SAW-EMP-NATIVE-QA-001/R1",
      "original_W27_selected_SHA":BASE,
      "original_source_Git_blobs":ORIGINAL,
      "exact_owner_added_files":sorted(OWN),
      "actual_native_author_tests":TESTS,
      "native_cases":4,
      "source_test_required_contract_tokens":testtokens,
      "native_Editor_NUnit_XML":"NOT_RUN",
      "Unity_G1":"NOT_RUN",
      "Windows_Player":"NOT_RUN",
      "source_security_status":"SELECTED_W27_SHA_AND_OWN_SCOPE_AUTHENTICATED",
      "product_SAW_own_sweep":"P0_PENDING_NATIVE_REGRESSION",
      "product_EMP_area":"P0_PENDING_NATIVE_REGRESSION",
      "garage_3D_zones":"NOT_TESTED",
      "economy_softlock":"RED_SOURCE_WITNESS_812_NOT_LIVE_REPRO",
      "Codex_package_ACK":"NONE",
      "merges":0
    }
if __name__=="__main__":
    dest=Path(sys.argv[1]) if len(sys.argv)>1 else Path("ci41-w28-source-gate.json")
    try:
        x=audit();code=0
    except Exception as e:
        x={"status":"REJECTED","error":str(e),"Unity_G1":"NOT_RUN","Codex_package_ACK":"NONE"};code=2
    dest.write_text(json.dumps(x,sort_keys=True,indent=2)+"\n")
    print(json.dumps(x,sort_keys=True))
    sys.exit(code)
