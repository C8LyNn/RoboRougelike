#!/usr/bin/env python3
"""Original Git-byte source-integrity receipt. NOT a Unity build or PhysX test."""
import hashlib, json, pathlib, re, subprocess, sys, zipfile
from io import BytesIO

ROOT=pathlib.Path(__file__).resolve().parents[4]
BASE="ed8cf4bcc131688b9a93968a89683d2496586683"
EMP="fea000fe0a706df2b52b6f295bdb3135ba1f9384"
GARAGE="3b3f402ca2adcc9525d96388cca17f0b5e1ae06d"
PAD="05db0ddf0a48da592d924e68b7658eea6bba764e"
EMP_FILES=[
"Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs",
"Assets/WeaponIntegration/RRLEmpBinding.cs",
"Assets/RoboRougelikeMechanics/WPN006EMP/Tests/PlayMode/EmpEmitterPhysicsTests.cs"]
GARAGE_FILES=[
"Assets/B01UserExperience.meta",
"Assets/B01UserExperience/B0112GarageSpatialW28.meta",
"Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs",
"Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs.meta",
"Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs",
"Assets/Tests/Editor/B0112GarageSpatialW28Tests.cs.meta"]
def git(*args):
    result=subprocess.run(["git", *args],cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if result.returncode:
        raise RuntimeError("Git source unavailable "+repr(args)+" "+result.stderr.decode("utf8","replace")[-500:])
    return result.stdout
def blob(ref,path):
    return git("show",ref+":"+path)
def verify_source(ref,paths):
    for p in paths:
        if blob("HEAD",p)!=blob(ref,p):
            raise AssertionError(f"source provenance mismatch {ref} {p}")
def main():
    head=git("rev-parse","HEAD").decode().strip()
    assert git("merge-base","--is-ancestor",BASE,head)==b""
    # Explicit ORIGINAL #810 19-path manifest includes Unity folder .meta paths.
    # Git diff pathspec alone excludes the adjacent MapJumpPad001.meta root GUID.
    pad_paths=["Assets/B01Packages/MapJumpPad001.meta","Assets/B01Packages/MapJumpPad001/Editor.meta","Assets/B01Packages/MapJumpPad001/Editor/B01.MapJumpPad001.Editor.asmdef","Assets/B01Packages/MapJumpPad001/Editor/B01.MapJumpPad001.Editor.asmdef.meta","Assets/B01Packages/MapJumpPad001/Editor/MapJumpPadSceneBuilder.cs","Assets/B01Packages/MapJumpPad001/Editor/MapJumpPadSceneBuilder.cs.meta","Assets/B01Packages/MapJumpPad001/README.md","Assets/B01Packages/MapJumpPad001/Runtime.meta","Assets/B01Packages/MapJumpPad001/Runtime/B01.MapJumpPad001.Runtime.asmdef","Assets/B01Packages/MapJumpPad001/Runtime/B01.MapJumpPad001.Runtime.asmdef.meta","Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs","Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs.meta","Assets/B01Packages/MapJumpPad001/Tests.meta","Assets/B01Packages/MapJumpPad001/Tests/PlayMode.meta","Assets/B01Packages/MapJumpPad001/Tests/PlayMode/B01.MapJumpPad001.PlayMode.asmdef","Assets/B01Packages/MapJumpPad001/Tests/PlayMode/B01.MapJumpPad001.PlayMode.asmdef.meta","Assets/B01Packages/MapJumpPad001/Tests/PlayMode/MapJumpPadPhysicsTests.cs","Assets/B01Packages/MapJumpPad001/Tests/PlayMode/MapJumpPadPhysicsTests.cs.meta","Assets/B01Packages/MapJumpPad001/manifest.json"]
    assert len(pad_paths)==19, pad_paths
    verify_source(PAD,pad_paths)
    verify_source(EMP,EMP_FILES)
    verify_source(GARAGE,GARAGE_FILES)
    modified=git("diff","--name-only",BASE,head).decode().splitlines()
    legal=set(pad_paths+EMP_FILES+GARAGE_FILES+[
        "Assets/R5/R5Arena.cs","Assets/Tests/Editor/R5ArenaTests.cs",
        "Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs",
        "Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs.meta",
        "Docs/W29/LIVE_JUMPPAD_R5_GAME_INTAKE.md",
        "Docs/W29/EMP839_SELECTED_RECEIVER_REPORT.md",
        "Tools/B01/Codex/W29/verify_source.py",
        "Tools/B01/Codex/W29/README.md",
        ".github/workflows/b01-w29-live-gameplay-source.yml"
    ])
    assert set(modified)<=legal, sorted(set(modified)-legal)
    assert len(modified)==len(set(modified))
    r5=blob("HEAD","Assets/R5/R5Arena.cs").decode()
    assert "JumpPad(new Vector3(7f,0f,-16f))" in r5
    assert "pad.AddComponent<B01.MapJumpPad001.MapJumpPad>()" in r5
    assert "area.isTrigger=true" in r5 and "launch.Configure(8.5f,2f,1.25f)" in r5
    assert "visualCollider.enabled=false" in r5
    jump=blob("HEAD","Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs").decode()
    assert "ForceMode.VelocityChange" in jump and "Rigidbody body" in jump
    test=blob("HEAD","Assets/Tests/Editor/W29LiveArenaJumpPadTests.cs").decode()
    assert test.count("[UnityTest]")==2 and "WaitForFixedUpdate" in test
    assert "R3BuildValidation" not in modified and "Assets/R3/R3VehicleModel.cs" not in modified
    # An extra original R5 collider is deliberately a TRIGGER, not false evidence all solid.
    oldtest=blob("HEAD","Assets/Tests/Editor/R5ArenaTests.cs").decode()
    assert "c.isTrigger" in oldtest and "GetComponentsInChildren<B01.MapJumpPad001.MapJumpPad>" in oldtest
    own_sources=blob("HEAD","Assets/RoboRougelikeMechanics/WPN006EMP/Runtime/EmpEmitter.cs").decode()
    assert "activationGate != null && !activationGate()" in own_sources
    overlay=blob("HEAD","Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs").decode()
    for marker in ("OccupiedRed","ProtrusionBlue","WeaponPurple","MechanismOrange","ValidGreen","source.PlacementIssue"):
        assert marker in overlay, marker
    meta_paths=git("ls-tree","-r","--name-only","HEAD","--","Assets").decode().splitlines()
    known={}
    for path in meta_paths:
        if not path.endswith(".meta"):continue
        found=re.search(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",blob("HEAD",path).decode("utf8","replace"))
        if not found:continue
        guid=found.group(1).lower()
        if guid in known:raise AssertionError(f"duplicate original Unity GUID {guid}: {known[guid]} & {path}")
        known[guid]=path
    receipt={"head":head,"source_only":True,"native_unity":"NOT_RUN","windows_player":"NOT_RUN",
             "base_codex":BASE,"producer_jumppad":PAD,"producer_emp":EMP,"producer_garage":GARAGE,
             "file_count":len(modified),"original_jumppad_file_count":len(pad_paths),
             "distinct_guid_count":len(known),
             "files":[{"path":p,"sha256":hashlib.sha256(blob("HEAD",p)).hexdigest(),"bytes":len(blob("HEAD",p)),
                      "git_blob":git("rev-parse","HEAD:"+p).decode().strip()} for p in sorted(modified)]}
    out=ROOT/"w29-source-receipt"
    out.mkdir(exist_ok=True)
    data=json.dumps(receipt,sort_keys=True,indent=2,ensure_ascii=False).encode()
    (out/"receipt.json").write_bytes(data)
    zipped=out/"W29_GAMEPLAY_SOURCE_ONLY.zip"
    with zipfile.ZipFile(zipped,"w",compression=zipfile.ZIP_STORED) as archive:
        for p in sorted(modified):
            info=zipfile.ZipInfo(p,(1980,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_STORED
            info.external_attr=0o100644<<16
            archive.writestr(info,blob("HEAD",p))
        info=zipfile.ZipInfo("SOURCE_RECEIPT.json",(1980,1,1,0,0,0))
        archive.writestr(info,data)
    sha=hashlib.sha256(zipped.read_bytes()).hexdigest()
    print("B0100_W29_ORIGINAL_EMP_EXACT_3_FILES_PASS")
    print("B0100_W29_ORIGINAL_GARAGE_EXACT_6_FILES_PASS")
    print("B0100_W29_ORIGINAL_JUMPPAD_EXACT_19_FILES_PASS")
    print("B0100_W29_GAMEPLAY_WIRING=R5_SALVAGE_JUMPPAD_SOURCE_PASS")
    print("B0100_W29_SOURCE_SCOPE_AND_META_GUID=PASS")
    print(f"B0100_W29_ZIP_SOURCE_SHA256={sha}")
    print("B0100_W29_UNITY_EDITOR_AND_PLAYER=NOT_RUN")
if __name__=="__main__":
    try:main()
    except Exception as e:
        print("B0100_W29_SOURCE_FAIL_CLOSED: "+str(e),file=sys.stderr)
        sys.exit(2)
