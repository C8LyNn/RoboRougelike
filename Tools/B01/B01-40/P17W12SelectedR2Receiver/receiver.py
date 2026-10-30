#!/usr/bin/env python3
"""B01-40 W12 exclusive #510 exact-Git physical source, receiver and migration gate.

Original producer package.py is executed unchanged in a separate checkout.
No Unity execution; no canonical game source write or merge; no alternative import.
"""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

SELECTED = "d94400d95d13db7f27e1533df05c9292d5380431"
SUPERSEDED = "33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
W06 = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
QA = "12ceba268a3828d28dc9e99a0a36ec797599c8c1"
CODEX = "203a0805251ed28293888e43f2d49f0df63767c6"
MAIN = "5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
ZIP_SHA = "d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"
ZIP_SIZE, ZIP_MEMBERS, SOURCE_MEMBERS, META_COUNT = 99666, 35, 34, 14
UNITY_ROOT = "Assets/B0123SecureReattachV2"
MANIFEST = "PACKAGE_MANIFEST.json"
PACKAGE_PY = "Tools/B01/B01-23/P17W10SecureReattachV2/package.py"
QA_MANIFEST = "Tools/B01/B01-61/P17W11AtomicR2Acceptance/manifest.json"
LEGACY_GUids = {
    "W07": "b017d070000000000000000000000008",
    "W08": "b018d080000000000000000000000008",
    "W09": "b019d090000000000000000000000008",
}
OLD_ROOTS = ("Assets/B0123DebrisPlayground001/", "Assets/B0123ReattachClearance001/",
             "Assets/B0123DockHardening001/")
ALTERNATIVE_ROOTS = ("Assets/B0123ReattachPublicGuard", "Assets/B0123SecureV2R1_1")
PINNED_SOURCE_FILES = [
    UNITY_ROOT + "/Runtime/SecureDebrisPlaygroundV2.cs",
    UNITY_ROOT + "/Runtime/SecureDockPolicy.cs",
    UNITY_ROOT + "/Runtime/SecureRigSpec.cs",
    UNITY_ROOT + "/Runtime/AtomicDockLedger.cs",
    "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
]
EXPECTED_ASM = {
    "B0123.SecureReattachV2.Runtime": ["B0123.DebrisRepulse.Runtime"],
    "B0123.SecureReattachV2.Editor": ["B0123.SecureReattachV2.Runtime"],
    "B0123.SecureReattachV2.PlayMode": ["B0123.SecureReattachV2.Runtime"],
}
class GateFailure(Exception):
    pass
def require(ok, message):
    if not ok:
        raise GateFailure(message)
def git(root, *args):
    p = subprocess.run(["git", "-C", str(root), *args], capture_output=True)
    require(p.returncode == 0, "git " + " ".join(args) + ": " + p.stderr.decode(errors="replace"))
    return p.stdout
def head(root, expected):
    got = git(root, "rev-parse", "HEAD").decode().strip()
    require(got == expected, "WRONG_HEAD expected=" + expected + " got=" + got)
def read_git(root, path):
    return git(root, "show", "HEAD:" + path)
def sha(data):
    return hashlib.sha256(data).hexdigest()
def blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()
def tree_paths(repo, scope="Assets/"):
    return git(repo, "ls-tree", "-r", "--name-only", "HEAD", scope).decode().splitlines()
def package(repo, where):
    cmd = [sys.executable, str(repo / PACKAGE_PY), "build", "--archive", str(where)]
    p = subprocess.run(cmd, cwd=repo, capture_output=True, text=True)
    require(p.returncode == 0, "producer physical package.py FAIL: " + p.stdout + p.stderr)
    return where.read_bytes(), p.stdout.strip()
def exact_files(producer, qa):
    head(producer, SELECTED)
    head(qa, QA)
    compare = set(git(producer, "diff", "--name-only", SUPERSEDED, "HEAD").decode().splitlines())
    require(len(compare) == 12 and all(
        name.startswith("Assets/B0123SecureReattachV2/") or
        name.startswith("Tools/B01/B01-23/P17W10SecureReattachV2/") or
        name.startswith("Tools/B01/B01-23/P17W11DockAtomicR2/") or
        name == ".github/workflows/b01-23-p17w11-atomic-dock-r2.yml"
        for name in compare), "incorrect 12-file R2-only source delta")
    cumulative = set(git(producer, "diff", "--name-only", W06, "HEAD").decode().splitlines())
    require(len(cumulative) == SOURCE_MEMBERS, "cumulative owner scope must be 34 source paths")
    qa_info = json.loads(read_git(qa, QA_MANIFEST))
    require(qa_info["exact_target"]["sha"] == SELECTED and qa_info["w06_sha"] == W06,
            "independent QA #517 targets another source")
    pinned = qa_info["source_blobs"]
    require(len(pinned) == 5, "independent QA source pin count")
    for path, gitblob in zip(PINNED_SOURCE_FILES, pinned):
        data = read_git(producer, path)
        require(blob(data) == gitblob, "QA#517 C# original source SHA mismatch: " + path)
    return {"delta_w10_to_r2": len(compare), "cumulative_vs_w06": len(cumulative),
            "qa_frozen_source_blobs": len(pinned), "qa_sha": QA}
def archive_check(raw, producer):
    require(len(raw) == ZIP_SIZE and sha(raw) == ZIP_SHA,
            "ORIGINAL #510 INNER ZIP SHA/SIZE MISMATCH " + sha(raw))
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        paths = z.namelist()
        require(len(paths) == ZIP_MEMBERS and len(set(paths)) == ZIP_MEMBERS, "bad member count/duplicate")
        require(all(p and not p.startswith("/") and "\\" not in p and ".." not in p.split("/")
                    for p in paths), "unsafe ZIP member")
        require(MANIFEST in paths, "manifest absent")
        mf = json.loads(z.read(MANIFEST))
        require(mf.get("source_sha") == SELECTED and mf.get("version") == "2.0.0" and
                mf.get("supersedes_source_sha") == SUPERSEDED and mf.get("parent_w06_sha") == W06 and
                mf.get("codex_ack") is False and mf.get("native_unity") == "NOT_RUN" and
                mf.get("owned_meta_guids") == META_COUNT, "source manifest unsafe or wrong revision")
        records = mf.get("files")
        require(len(records) == SOURCE_MEMBERS, "manifest source count mismatch")
        require(set(paths) == {MANIFEST} | {r["path"] for r in records}, "archive/manifest member mismatch")
        files = {}
        for row in records:
            p = row["path"]
            data = z.read(p)
            require(data == read_git(producer, p) and row["sha256"] == sha(data) and
                    row["bytes"] == len(data), "Git/physical archive bytes mismatch: " + p)
            files[p] = data
    require(not any(p.startswith(OLD_ROOTS) or p.startswith(ALTERNATIVE_ROOTS)
                    for p in files), "legacy/#511/#494 installed in selected original archive")
    return files, mf
def guid_and_asm(files, w06, receiver, main):
    metas = {p: d for p,d in files.items() if p.endswith(".meta")}
    require(len(metas) == META_COUNT, "missing Unity .meta files")
    guids = {}
    for name, data in metas.items():
        found = re.findall(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", data.decode())
        require(len(found) == 1 and found[0] not in guids, "invalid or repeated Unity GUID: " + name)
        guids[found[0]] = name
        counterpart = name[:-5]
        require(counterpart in files or any(x.startswith(counterpart + "/") for x in files),
                "orphan Unity metadata " + name)
    require(not set(guids).intersection(set(LEGACY_GUids.values())), "legacy GUI script GUID overlap")
    assembly = {}
    for name, data in files.items():
        if name.endswith(".asmdef"):
            obj = json.loads(data)
            require(obj["name"] not in assembly, "duplicate Unity assembly name")
            assembly[obj["name"]] = obj
    require(len(assembly) == 3 and {k:sorted(v["references"]) for k,v in assembly.items()} ==
            EXPECTED_ASM, "three asmdefs/ref dependency graph invalid")
    require(assembly["B0123.SecureReattachV2.Editor"].get("includePlatforms") == ["Editor"],
            "Editor assembly missing platform boundary")
    require("UNITY_INCLUDE_TESTS" in assembly["B0123.SecureReattachV2.PlayMode"].get("defineConstraints", []),
            "PlayMode assembly not test restricted")
    for label, repo in (("frozen W06",w06), ("frozen Codex R3",receiver), ("main",main)):
        existing = tree_paths(repo)
        known = set()
        for p in existing:
            if p.endswith(".meta"):
                raw = read_git(repo,p).decode(errors="replace")
                for uid in re.findall(r"(?m)^guid:\s*([0-9a-f]{32})",raw):
                    known.add(uid)
        collisions = set(guids) & known
        require(not collisions, label + " GUID collision: " + repr(collisions))
        overlap = set(files) & set(existing)
        # Only new isolated package files matter; no universal-tool path overlap claim.
        require(not overlap, label + " physical receiver file-scope collision: " + repr(overlap))
    return {"unique_unity_guids":len(guids), "asmdefs":sorted(assembly),
            "refgraph":{k:v["references"] for k,v in assembly.items()},
            "receiver_preflight": ["immutable-W06","read-only-Codex-R3-#383","main"],
            "current_live_codex_workspace_scanned":False}
def scan_readonly_receiver_sources(repo, label):
    """Inspect actual tracked Unity receivers without writing in their Git checkouts."""
    checked=0
    for p in tree_paths(repo):
        if p.startswith(OLD_ROOTS) or p.startswith(ALTERNATIVE_ROOTS):
            raise GateFailure(label+" has frozen competing Unity asset path "+p)
        if p.endswith((".cs",".unity",".prefab",".asset")):
            checked += 1
            data=read_git(repo,p).decode("utf-8",errors="replace")
            if re.search(r"\b(?:DebrisRigPlayground|DebrisSafeReattachGate|HardenedDockDemo)\b",data):
                raise GateFailure(label+" includes legacy W07/W08/W09 source type "+p)
            if any(guid in data for guid in LEGACY_GUids.values()):
                raise GateFailure(label+" serialized legacy script GUID "+p)
    return checked

def source_security(files):
    root = files[UNITY_ROOT + "/Runtime/SecureDebrisPlaygroundV2.cs"].decode()
    ledger = files[UNITY_ROOT + "/Runtime/AtomicDockLedger.cs"].decode()
    policy = files[UNITY_ROOT + "/Runtime/SecureDockPolicy.cs"].decode()
    scene = files[UNITY_ROOT + "/Editor/SecureReattachSceneBuilder.cs"].decode()
    for api in ("Reattach", "TrySafeReattach", "ReattachFromGui"):
        require(re.search(r"public bool " + api + r"\(int index\)\s*=>\s*DockGate\(\)\."+
                          api+r"\(index\)",root), "unsafe public API " + api)
    require("new GuardedDockCommand(InspectDock,CommitDock)" in root and
            "private bool CommitDock(int index)" in root, "single guarded private physical commit missing")
    require("Physics.OverlapBoxNonAlloc(" in root and "Physics.ComputePenetration(" in root and
            "SecureDockPolicy.AllLayers" in root, "real all-layer collider gate missing")
    require("rig.Ledger.TryReattach(rig.Spec.PartId," in root and
            "rig.Emitter.RearmAfterReattach" not in root, "old unsafe emitter rearm still reachable")
    require("private readonly DebrisImpulseLedger consumed" in ledger and
            "if (!consumed.RearmAfterReattach(id))" in ledger and
            "rollbackPhysical();" in ledger and "consumed.TryDetach(" in ledger, "transaction/rollback missing")
    require(ledger.index("consumed.RearmAfterReattach(id)") < ledger.index("applyPhysical();"),
            "commit before consumed-ID authorization")
    require("TransactionRejected" in policy, "failed transaction incorrectly reported as Allowed")
    require("NewScene(NewSceneSetup.EmptyScene" in scene and
            "controller.AddComponent<SecureDebrisPlaygroundV2>()" in scene and
            "AddComponent<DebrisRigPlayground>" not in scene,
            "new isolated V2 scene builder includes legacy W07")
    return {"public_routes_verified":3,"original_ledger_prephysical_authorization":True,
            "rollback_compensation_source_checked":True,"real_unity_native":"NOT_RUN"}
def validate_host_tree(host_root, guids, allow_empty=True):
    """Read-only destination preflight; rejects all legacy and competing routes.
    Does not try to auto-migrate canonical scenes or assume they are safe."""
    root=Path(host_root)
    require(root.is_dir() and not root.is_symlink(),"host root absent/symlink")
    selected = root / UNITY_ROOT
    require(not selected.exists() and not selected.is_symlink(), "selected package destination already exists")
    require(not (root / (UNITY_ROOT+".meta")).exists(), "selected package root .meta exists")
    for base in OLD_ROOTS:
        require(not (root/base).exists(), "legacy W07/W08/W09 on receiver: "+base)
    for base in ALTERNATIVE_ROOTS:
        require(not (root/base).exists(), "competing source on receiver: "+base)
    assets=root/"Assets"
    if assets.exists():
        for p in assets.rglob("*"):
            require(not p.is_symlink(), "symlink in target assets")
            if not p.is_file():
                continue
            if p.suffix in (".cs", ".unity", ".prefab", ".asset", ".meta"):
                content=p.read_text(encoding="utf-8", errors="replace")
                require(not re.search(r"\b(?:DebrisRigPlayground|DebrisSafeReattachGate|HardenedDockDemo)\b",
                                      content), "legacy source reference in receiver: "+str(p))
                require(not any(uid in content for uid in LEGACY_GUids.values()),
                        "legacy serialized MonoScript GUID in scene/prefab/asset "+str(p))
                if p.suffix == ".meta":
                    found = re.findall(r"(?m)^guid:\s*([0-9a-f]{32})",content)
                    require(not set(found)&guids, "selected Unity GUID already in receiver "+str(p))
    return {"host_preflight":"PASS", "real_host_migrated":False}
def safe_names(files):
    require(all(n and not n.startswith("/") and "\\" not in n and ".." not in n.split("/")
                for n in files), "stage path traversal")
def stage_atomic(files, destination, inject_after=None):
    safe_names(files)
    require(not destination.exists() and not destination.is_symlink(), "would overwrite stage destination")
    require(destination.parent.is_dir(), "stage parent absent")
    scratch=Path(tempfile.mkdtemp(dir=destination.parent, prefix="b01-40-partial-"))
    count=0
    try:
        for name,content in sorted(files.items()):
            dest=scratch/name
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(content)
            count+=1
            if inject_after is not None and count >= inject_after:
                raise GateFailure("INJECTED_FAILED_STAGE")
        require(not destination.exists(), "stage target appeared")
        os.replace(scratch,destination)
    except Exception:
        if scratch.exists(): shutil.rmtree(scratch)
        raise
def rollback_strict(files, destination):
    require(destination.is_dir() and not destination.is_symlink(), "not owned valid stage")
    found={}
    directories=set()
    for p in destination.rglob("*"):
        require(not p.is_symlink(), "stage contains symlink")
        rel=p.relative_to(destination).as_posix()
        if p.is_dir(): directories.add(rel)
        elif p.is_file(): found[rel]=p.read_bytes()
        else: raise GateFailure("unrecognized filesystem member")
    expected_dirs=set()
    for name in files:
        parts=Path(name).parts
        expected_dirs.update(Path(*parts[:i]).as_posix() for i in range(1,len(parts)))
    require(found == files and directories == expected_dirs, "rollback refuses changed/foreign/missing file or empty directory")
    shutil.rmtree(destination)
    require(not destination.exists(), "rollback did not remove clean owned stage")
def expect_reject(action, label):
    try:
        action()
    except (GateFailure, ValueError, zipfile.BadZipFile):
        return label
    raise GateFailure("UNSAFE NEGATIVE TEST ACCEPTED: "+label)
def negative_tests(files, archive, guids):
    checks=[]
    with tempfile.TemporaryDirectory(prefix="b01-40-w12-physical-") as d:
        base=Path(d);target=base/"stage"
        # True atomic staging: no destination or scratch debris after failure.
        checks.append(expect_reject(lambda: stage_atomic(files,target,inject_after=3),"partial-write-failure"))
        require(not target.exists() and not any(base.glob("b01-40-partial-*")), "partial stage did not clean up")
        stage_atomic(files,target)
        checks.append(expect_reject(lambda: stage_atomic(files,target),"destination-overwrite"))
        marker=target/next(iter(sorted(files)))
        saved=marker.read_bytes()
        marker.write_bytes(b"MODIFIED")
        checks.append(expect_reject(lambda: rollback_strict(files,target),"altered-owned-file-rollback"))
        marker.write_bytes(saved)
        foreign=target/"FOREIGN.txt"
        foreign.write_text("preserve")
        checks.append(expect_reject(lambda: rollback_strict(files,target),"foreign-file-rollback"))
        foreign.unlink()
        (target/"empty-foreign-dir").mkdir()
        checks.append(expect_reject(lambda: rollback_strict(files,target),"foreign-empty-directory-rollback"))
        (target/"empty-foreign-dir").rmdir()
        if hasattr(os,"symlink"):
            link=target/"foreign-link"
            try:
                link.symlink_to(base)
                checks.append(expect_reject(lambda: rollback_strict(files,target),"foreign-symlink-rollback"))
                link.unlink()
            except (OSError, NotImplementedError):
                checks.append("symlink-skipped-by-host")
        else: checks.append("symlink-skipped-by-host")
        rollback_strict(files,target)
        checks.append(expect_reject(lambda: stage_atomic({"../ESCAPE":b"bad"},target),"traversal-stage"))
        require(not target.exists(), "traversal wrote stage")
        stage_atomic(files,target)
        altered=dict(files)
        altered[next(iter(sorted(files)))]=b"MODIFIED"
        checks.append(expect_reject(lambda: rollback_strict(altered,target),"untrusted-expected-membership"))
        rollback_strict(files,target)
        host=base/"host";(host/"Assets").mkdir(parents=True)
        validate_host_tree(host,guids)
        old=(host/OLD_ROOTS[0]);old.mkdir(parents=True)
        checks.append(expect_reject(lambda: validate_host_tree(host,guids),"legacy-W07-component"))
        old.rmdir()
        alternative=host/"Assets/B0123SecureV2R1_1";alternative.mkdir()
        checks.append(expect_reject(lambda: validate_host_tree(host,guids),"alternate-#511-component"))
        alternative.rmdir()
        sample=host/"Assets/scene.prefab"
        sample.write_text("m_Script: {fileID: 11500000, guid: "+LEGACY_GUids["W07"]+", type: 3}")
        checks.append(expect_reject(lambda: validate_host_tree(host,guids),"legacy-W07-serialized-guid"))
        sample.unlink()
        sample.write_text("public DebrisRigPlayground oldScene;")
        checks.append(expect_reject(lambda: validate_host_tree(host,guids),"legacy-type-reference"))
        sample.unlink()
        existing=host/"Assets/another.cs.meta";existing.write_text("fileFormatVersion: 2\nguid: "+next(iter(guids))+"\n")
        checks.append(expect_reject(lambda: validate_host_tree(host,guids),"receiver-guid-collision"))
        existing.unlink()
        validate_host_tree(host,guids)
    return checks
def main():
    parser=argparse.ArgumentParser()
    for n in ("producer","qa","w06","codex","main","out"):
        parser.add_argument("--"+n,required=True,type=Path)
    a=parser.parse_args()
    producer=a.producer.resolve()
    qa=a.qa.resolve()
    w06=a.w06.resolve()
    codex=a.codex.resolve()
    main=a.main.resolve()
    head(w06,W06);head(codex,CODEX);head(main,MAIN)
    facts=exact_files(producer,qa)
    with tempfile.TemporaryDirectory(prefix="b01-40-w12-original-producer-") as t:
        archive,producer_log=package(producer,Path(t)/"selected-r2.zip")
        files,mf=archive_check(archive,producer)
        meta=guid_and_asm(files,w06,codex,main)
        meta["reviewed_readonly_receiver_assets"]={"codex_R3_files":scan_readonly_receiver_sources(codex,"Codex R3"),"main_files":scan_readonly_receiver_sources(main,"main")}
        security=source_security(files)
        guids={re.search(r"(?m)^guid:\s*([0-9a-f]{32})",d.decode()).group(1)
               for p,d in files.items() if p.endswith(".meta")}
        negatives=negative_tests(files,archive,guids)
        # Evidence output is a copy of the original exact binary ZIP, not a new author package.
        a.out.mkdir(parents=True,exist_ok=True)
        (a.out/"B01-23-SECURE-V2-R2-SELECTED-ORIGINAL.zip").write_bytes(archive)
    receipt={
        "status":"G0_ACCEPTED_SOURCE_ONLY",
        "package":"B01-23-SECURE-REATTACH-V2",
        "version":"2.0.0",
        "exact_producer_sha":SELECTED,
        "producer_pr":510,
        "physical_inner_zip_sha256":ZIP_SHA,
        "physical_inner_zip_bytes":ZIP_SIZE,
        "physical_member_count":ZIP_MEMBERS,
        "source_files_verified":SOURCE_MEMBERS,
        "new_guid_count":META_COUNT,
        "provenance":facts,
        "unity":meta,
        "security":security,
        "negative_receiver_tests":negatives,
        "negative_receiver_count":len(negatives),
        "receiver_producer_log":producer_log,
        "exclusive_install":["#510"],
        "excluded":["#511","#494","W07","W08","W09"],
        "live_codex_receiver_checked":False,
        "current_codex_scenes_migrated":False,
        "g1_unity_physx":"NOT_RUN",
        "g2":"HOLD",
        "codex_offered":False,"codex_ack":False,"integrated":False,"merged":False,
        "handoff":"B01-00 #79 sole offer authority; B01-60 real Unity G1; Codex only after ACK",
        "rollback":"Only own isolated, byte-identical newly staged directory; strict refusal of modified/foreign/empty dirs"
    }
    (a.out/"B01-40-W12-G0-RECEIPT.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    print("G0_ACCEPTED_SOURCE_ONLY",json.dumps({k:receipt[k] for k in
         ("exact_producer_sha","physical_inner_zip_sha256","physical_inner_zip_bytes",
          "physical_member_count","new_guid_count","negative_receiver_count","g1_unity_physx")},sort_keys=True))
    print("EXCLUSIVE_#510 #511_#494_W07_W08_W09_NOT_STAGED; CODEX_ACK_NONE; NO_MERGE; G2_HOLD")
if __name__=="__main__":
    try:
        main()
    except (GateFailure,ValueError,KeyError,OSError,zipfile.BadZipFile,subprocess.SubprocessError,UnicodeError) as exc:
        print("B01-40-W12-G0-FAIL "+str(exc),file=sys.stderr)
        sys.exit(1)
