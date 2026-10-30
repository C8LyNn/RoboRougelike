#!/usr/bin/env python3
"""B01-61/W12: read-only exact-#510 receiver admission + Unity YAML migration gate.
NEVER copies, deletes, or modifies receiver files. Not a native Unity/PhysX test.
"""
import argparse
import functools
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

SELECTED = "d94400d95d13db7f27e1533df05c9292d5380431"
ALTERNATIVE = "baf65f2664e38f3b9c38815c3f07285f8107c1b6"
OLDER_W07 = "cdcaea750fda1a9d7db6018e34970f4b9f206427"
OLDER_W08 = "99dfbd25d471f9983f9472b12d3725f4c002f7e9"
OLDER_W09 = "763748f1be0c61356fb4462144640bb359364226"
W06 = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
REPO = Path(__file__).resolve().parents[4]
ASSET = "Assets/B0123SecureReattachV2"
SCENE = ASSET + "/Generated/B0123SecureReattachV2.unity"
MANIFEST = "PACKAGE_MANIFEST.json"
GATE_ID = "B01-61-P17W12-RECEIVER-MIGRATION-STRICT-001"
KNOWN_META = {
    "selected": (SELECTED, ASSET + "/Runtime/SecureDebrisPlaygroundV2.cs.meta",
                 "6662be99db4e5384f8b01be232ea8c83de407390"),
    "W07": (OLDER_W07, "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs.meta",
            "ca6a0c273f66929eb0fd05a1bc640835055ba2c2"),
    "W08": (OLDER_W08, "Assets/B0123ReattachClearance001/Runtime/DebrisSafeReattachGate.cs.meta",
            "dbcb57292dd371e5e60258a7c651c387a17c0596"),
    "W09": (OLDER_W09, "Assets/B0123DockHardening001/Runtime/HardenedDockDemo.cs.meta",
            "9aec985a485c58f00be36f520af460a91b7e7e03")
}
SCRIPT = re.compile(r"m_Script:\s*\{[^\n}]*\bguid:\s*([0-9a-fA-F]{32})\b")
GUID = re.compile(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
LEGACY_CALL = re.compile(
    r"\b(?:AddComponent|GetComponent|FindObjectOfType|FindFirstObjectByType|FindAnyObjectByType)"
    r"\s*<\s*(?:[A-Za-z_][A-Za-z_0-9]*\.)?"
    r"(?:DebrisRigPlayground|DebrisSafeReattachGate|HardenedDockDemo)\s*>")
def git(*args):
    return subprocess.check_output(["git", *args], cwd=REPO, stderr=subprocess.PIPE)
@functools.lru_cache(maxsize=None)
def source_paths():
    selected=git("ls-tree","-r","--name-only",SELECTED,"--",ASSET).decode().splitlines()
    selected += [ASSET + ".meta"]
    selected += git("ls-tree","-r","--name-only",SELECTED,"--",
                    "Tools/B01/B01-23/P17W10SecureReattachV2",
                    "Tools/B01/B01-23/P17W11DockAtomicR2").decode().splitlines()
    selected += [".github/workflows/b01-23-p17w10-secure-reattach-v2.yml",
                 ".github/workflows/b01-23-p17w11-atomic-dock-r2.yml"]
    return tuple(sorted(set(selected)))
@functools.lru_cache(maxsize=None)
def original(ref, path):
    return git("show", ref + ":" + path)
def git_blob(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+bytes([0])+raw).hexdigest()
def sha256(raw):
    return hashlib.sha256(raw).hexdigest()
@functools.lru_cache(maxsize=1)
def trusted_guids():
    guids={}
    for label, (ref, path, expected_blob) in KNOWN_META.items():
        raw=original(ref,path)
        if git_blob(raw)!=expected_blob:
            raise ValueError("IMMUTABLE_GIT_META_PIN_FAILED "+label)
        matches=GUID.findall(raw.decode("utf-8"))
        if len(matches)!=1:
            raise ValueError("IMMUTABLE_META_GUID_INVALID "+label)
        guids[label]=matches[0].lower()
    if len(set(guids.values()))!=4:
        raise ValueError("TRUSTED_SCRIPT_GUID_COLLISION")
    return guids
def assert_git_owner():
    """Source-pins checked independent of QA PR HEAD, never confuse with #511."""
    paths=source_paths()
    if len(paths)!=34:
        raise ValueError("SOURCE_SELECTED_PATH_COUNT_DRIFT "+str(len(paths)))
    expected_files={
        ASSET+"/Runtime/AtomicDockLedger.cs":"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
        ASSET+"/Runtime/SecureDebrisPlaygroundV2.cs":"328977b4cdcdcd7c9f128c01f11ff67f8e46086a",
        ASSET+"/Runtime/SecureDockPolicy.cs":"9f80f705475bd312f209033a57bd07e03bf95628",
        ASSET+"/Editor/SecureReattachSceneBuilder.cs":"07de6c8ce784b714699ce5598564aab2b2f1db29"
    }
    for path,sha in expected_files.items():
        if git_blob(original(SELECTED,path))!=sha:
            raise ValueError("SOURCE_SELECTED_RUNTIME_DRIFT "+path)
    if git_blob(original(W06,"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"))!="fa1aa5e531927cbe73cf5fdfe161b4a981316db5":
        raise ValueError("W06_ID_LEDGER_DRIFT")
    trusted_guids()
    return paths
def make_manifest():
    """Synthetic *test-fixture-only* original-byte manifest; no owner package rewriting."""
    paths=assert_git_owner()
    return {"package":"B01-23-SECURE-REATTACH-V2",
            "version":"2.0.0","source_sha":SELECTED,
            "parent_w06_sha":W06,"native_unity":"NOT_RUN",
            "codex_ack":False,
            "files":[{"path":p,"sha256":sha256(original(SELECTED,p)),
                      "bytes":len(original(SELECTED,p))} for p in paths]}
def stop(reason):
    raise ValueError(reason)
def safe_file(receiver, relative):
    p=Path(relative)
    if p.is_absolute() or ".." in p.parts:
        stop("ESCAPED_PATH "+relative)
    dest=receiver/p
    if dest.is_symlink() or not dest.is_file():
        stop("MISSING_OR_SYMLINK_SOURCE "+relative)
    return dest
def validate_migration(receiver:Path, scene:str=SCENE):
    receiver=Path(receiver).resolve()
    if not receiver.is_dir():
        stop("NO_RECEIVER_DIRECTORY")
    trusted=trusted_guids()
    expected=assert_git_owner()
    manifest_path=safe_file(receiver,MANIFEST)
    try:
        m=json.loads(manifest_path.read_text(encoding="utf-8"))
    except (UnicodeError,json.JSONDecodeError):
        stop("MALFORMED_MANIFEST")
    if (m.get("package")!="B01-23-SECURE-REATTACH-V2" or
        m.get("source_sha")!=SELECTED or m.get("version")!="2.0.0" or
        m.get("parent_w06_sha")!=W06 or
        m.get("native_unity")!="NOT_RUN" or m.get("codex_ack") is not False):
        stop("WRONG_VARIANT_MANIFEST_510_REQUIRED")
    listed=m.get("files")
    if not isinstance(listed,list) or [r.get("path") for r in listed]!=list(expected):
        stop("NOT_EXACT_SELECTED_34_PATH_MANIFEST")
    for entry in listed:
        path=entry["path"]
        ref=original(SELECTED,path)
        data=safe_file(receiver,path).read_bytes()
        if (data!=ref or sha256(data)!=entry.get("sha256") or
            len(data)!=entry.get("bytes")):
            stop("RECEIVER_SOURCE_DRIFT_OR_511_MIX "+path)
    # Owner asset scope: no foreign source under selected V2 namespace, except
    # generated runtime Unity scene and its matching .meta. No destructive action.
    actual_assets={f.relative_to(receiver).as_posix()
                   for f in (receiver/ASSET).rglob("*") if f.is_file()}
    allowed={p for p in expected if p.startswith(ASSET+"/")}
    foreign={p for p in actual_assets if p not in allowed
             and not p.startswith(ASSET+"/Generated/")}
    if foreign:
        stop("FOREIGN_V2_ASSET_SOURCE "+sorted(foreign)[0])
    # Legacy W07, W08 and W09 runtime publicly callable classes are forbidden in
    # this *exclusive* selected-V2 receiver, even if their old scene is not loaded.
    for label in ("W07","W08","W09"):
        source=KNOWN_META[label][1][:-5]
        meta=KNOWN_META[label][1]
        if (receiver/source).exists() or (receiver/meta).exists():
            stop("LEGACY_COMPONENT_SOURCE_PRESENT "+label)
    # Strong .meta identity check; account for unrelated receiver asset GUIDs.
    guid_paths={}
    for f in (receiver/"Assets").rglob("*.meta"):
        if f.is_symlink():
            stop("SYMLINK_META_UNINSPECTABLE")
        raw=f.read_text(encoding="utf-8")
        ids=GUID.findall(raw)
        if len(ids)!=1:
            stop("INVALID_META "+f.relative_to(receiver).as_posix())
        key=ids[0].lower()
        if key in guid_paths:
            stop("DUPLICATE_UNITY_GUID "+key)
        guid_paths[key]=str(f.relative_to(receiver))
    if trusted["selected"] not in guid_paths:
        stop("SELECTED_RUNTIME_GUID_NOT_INSTALLED")
    # Parse serialized Unity scene/prefab/ScriptableObject script GUIDs explicitly.
    # Binary/non-UTF8 assets are UNKNOWN=>HOLD; never assume absent legacy scripts.
    scenefile=safe_file(receiver,scene)
    scene_fingerprint=scenefile.read_bytes()
    try:
        scene_guids=[s.lower() for s in SCRIPT.findall(scene_fingerprint.decode("utf-8"))]
    except UnicodeError:
        stop("BINARY_TARGET_SCENE_REQUIRES_NATIVE_INSPECTION")
    if scene_guids.count(trusted["selected"])!=1:
        stop("SELECTED_TARGET_SCENE_REQUIRES_EXACTLY_ONE_V2_CONTROLLER")
    count=0
    for ext in ("*.unity","*.prefab","*.asset"):
        for f in (receiver/"Assets").rglob(ext):
            if f.is_symlink():
                stop("SYMLINK_SERIALIZED_ASSET_UNINSPECTABLE")
            try:
                content=f.read_text(encoding="utf-8")
            except UnicodeError:
                stop("BINARY_SERIALIZED_ASSET_REQUIRES_NATIVE_INSPECTION")
            script_guids=[s.lower() for s in SCRIPT.findall(content)]
            bad=set(script_guids).intersection(trusted[k] for k in ("W07","W08","W09"))
            if bad:
                stop("LEGACY_MONOBEHAVIOUR_REACHABLE "+f.relative_to(receiver).as_posix())
            count+=1
    # Direct source callers of legacy unguarded components are unsafe; inspect
    # lexical patterns independently of serialized asset references.
    for f in (receiver/"Assets").rglob("*.cs"):
        if f.is_symlink():
            stop("SYMLINK_CSHARP_UNINSPECTABLE")
        if LEGACY_CALL.search(f.read_text(encoding="utf-8")):
            stop("DYNAMIC_LEGACY_COMPONENT_CALLER "+f.relative_to(receiver).as_posix())
    return {"gate":GATE_ID,"source_sha":SELECTED,
            "candidate":"#510 only","scene":scene,"serialization_assets_scanned":count,
            "original_files_pinned":len(expected),"original_script_guids_pinned":len(trusted),
            "synthetic_receiver_gate":"PASS","actual_codex_host":"UNVERIFIED",
            "unity_edit_play_physx":"NOT_RUN","G0":"RECEIVER_PREFLIGHT_ONLY",
            "G1":"NOT_RUN","G2":"HOLD","codeX_ack":False,
            "files_modified":0}
def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--receiver",required=True,help="already staged DISPOSABLE receiver directory; read-only")
    parser.add_argument("--scene",default=SCENE,help="relative to receiver")
    a=parser.parse_args()
    try:
        result=validate_migration(Path(a.receiver),a.scene)
        print(json.dumps(result,sort_keys=True))
    except (ValueError,OSError,subprocess.CalledProcessError) as e:
        print("SOURCE_RECEIVER_G0_MIGRATION_HOLD "+str(e),file=sys.stderr)
        return 3
    return 0
if __name__=="__main__":
    sys.exit(main())
