#!/usr/bin/env python3
"""B01-40 W11: actual frozen-source producer physical ZIP and exclusive safe-dock admission.

Evidence mode exits 0 when it correctly reproduces HOLD; release mode fails closed.
Never writes to a source repository, never runs Unity, never merges.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import zipfile

V2 = "33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
GUARD = "6efe45961ed0ffdf183e8016e350978b9f3dc51c"
W06 = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
W07 = "cdcaea750fda1a9d7db6018e34970f4b9f206427"
W09 = "763748f1be0c61356fb4462144640bb359364226"
SOURCE_V2 = "Assets/B0123SecureReattachV2/Runtime/SecureDebrisPlaygroundV2.cs"
SOURCE_GUARD = "Assets/B0123DebrisPlayground001/Runtime/DebrisRigPlayground.cs"
PACK_V2 = "Tools/B01/B01-23/P17W10SecureReattachV2/package.py"
PACK_GUARD = "Tools/B01/B01-23/P17W10PublicDockGuardR2/package.py"
EXPECTED = {
    "v2": ("e7ce7d735d81ceaf02d4eba9672d22555330806e7abdd0074be106faa1d6b068", 72216, 28),
    "guard": ("55acf5f3176856f64ca66ba4449b165f528e8be2215d29a20618d7b3d38c1b80", 15948, 4),
}

class Rejected(Exception):
    pass

def require(v, msg):
    if not v:
        raise Rejected(msg)

def command(*args, cwd=None):
    p = subprocess.run(list(args), cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    require(p.returncode == 0, "command fail " + repr(args) + ": " + p.stderr.decode(errors="replace"))
    return p.stdout

def git(repo, *args):
    return command("git", "-C", str(repo), *args)

def sha256(data):
    return hashlib.sha256(data).hexdigest()

def blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()

def head(repo, expected):
    got = git(repo, "rev-parse", "HEAD").decode().strip()
    require(got == expected, "SOURCE_HEAD_MISMATCH:" + got)

def source(repo, filename):
    return git(repo, "show", "HEAD:" + filename)

def scope(repo, parent):
    return set(git(repo, "diff", "--name-only", parent, "HEAD").decode().splitlines())

def section(text, signature):
    match = re.search(signature, text)
    require(match is not None, "METHOD_MISSING: " + signature)
    i = text.find("{", match.end())
    require(i >= 0, "NO_BODY")
    depth = 0
    for j in range(i, len(text)):
        if text[j] == "{":
            depth += 1
        elif text[j] == "}":
            depth -= 1
            if depth == 0:
                return text[i + 1:j]
    raise Rejected("UNCLOSED_METHOD: " + signature)

def ordered_ledger_commit(method):
    """Source invariant only: never certify Unity physics or atomicity from the ordering alone."""
    m = method.find("RearmAfterReattach(")
    physical = [
        method.find("linearVelocity="), method.find("angularVelocity="),
        method.find("isKinematic="), method.find("SetParent("),
        method.find("localPosition="), method.find("Detached=false"),
    ]
    # W07 W10 format has spaces around '=' and camelcase consistent; use regex for both formats.
    mutations = [x.start() for rx in (
        r"\.linearVelocity\s*=", r"\.angularVelocity\s*=", r"\.isKinematic\s*=",
        r"\.SetParent\s*\(", r"\.localPosition\s*=", r"\.Detached\s*=",
    ) for x in re.finditer(rx, method)]
    require(m >= 0 and mutations, "MISSING_REARM_OR_MUTATION")
    return {"rearm_offset": m, "first_mutation_offset": min(mutations),
            "mutation_before_rearm": min(mutations) < m,
            "mutations_count": len(mutations),
            "explicit_rollback": bool(re.search(r"\b(?:Rollback|RestoreSnapshot|UndoCommit)\s*\(", method))}

def verify_v2(source_text, old_text):
    for api in ("Reattach", "TrySafeReattach", "ReattachFromGui"):
        require(re.search(r"public bool " + api + r"\(int index\)\s*=>\s*DockGate\(\)\." + api + r"\(index\)", source_text),
                "V2_PUBLIC_ALIAS_BYPASS:" + api)
    require("private bool CommitDock(int index)" in source_text and
            "new GuardedDockCommand(InspectDock,CommitDock)" in source_text,
            "V2_PRIVATE_MUTATOR_REQUIRED")
    require("Physics.OverlapBoxNonAlloc(" in source_text and
            "Physics.ComputePenetration(" in source_text and
            "SecureDockPolicy.AllLayers" in source_text,
            "V2_ALL_LAYER_PHYSICS_ROUTE_MISSING")
    require(re.search(r"if\(GUILayout.Button\(\"Reattach \(GUARDED\)\"\)\)ReattachFromGui\(i\)", source_text),
            "V2_GUI_ROUTE_BYPASS")
    require("ValidatePhysicalReattach" not in section(old_text, r"public bool Reattach\s*\(int index\)"),
            "ORIGINAL_W07_WAS_NOT_THE_KNOWN_BYPASS")
    result = ordered_ledger_commit(section(source_text, r"private bool CommitDock\s*\(int index\)"))
    return result

def verify_guard(text):
    method = section(text, r"public bool Reattach\s*\(int index\)")
    guard = method.find("ValidatePhysicalReattach(rig)")
    mutation = re.search(r"\.linearVelocity\s*=", method)
    require(guard >= 0 and mutation is not None and guard < mutation.start(),
            "W10_GUARD_DIRECT_PUBLIC_ROUTE_BYPASS")
    require("Physics.OverlapBoxNonAlloc(" in text and "Physics.ComputePenetration(" in text and
            "rotation, ~0, QueryTriggerInteraction.Collide" in text,
            "W10_GUARD_ALL_LAYER_PHYSICS_ROUTE_MISSING")
    return ordered_ledger_commit(method)

def verify_archive(archive, repo, label):
    expected_hash, expected_size, expected_count = EXPECTED[label]
    require(len(archive) == expected_size and sha256(archive) == expected_hash,
            label + " BINARY_ZIP_MISMATCH: " + sha256(archive))
    with zipfile.ZipFile(io.BytesIO(archive)) as zipfile_obj:
        names = zipfile_obj.namelist()
        require(len(names) == expected_count and len(names) == len(set(names)), "ZIP_DUPLICATE_OR_COUNT")
        require("PACKAGE_MANIFEST.json" in names, "ZIP_MANIFEST_MISSING")
        require(all(name and not name.startswith("/") and "\\" not in name and
                    ".." not in name.split("/") for name in names), "ZIP_UNSAFE_PATH")
        manifest = json.loads(zipfile_obj.read("PACKAGE_MANIFEST.json"))
        rows = manifest["files"]
        require(len(rows) == expected_count - 1, "ZIP_FILE_MANIFEST_COUNT")
        require(set(names) == {"PACKAGE_MANIFEST.json"} | {r["path"] for r in rows}, "ZIP_MANIFEST_MEMBERSHIP")
        for row in rows:
            b = zipfile_obj.read(row["path"])
            require(b == source(repo, row["path"]) and
                    row["sha256"] == sha256(b) and row["bytes"] == len(b),
                    "ZIP_SOURCE_PIN_MISMATCH:" + row["path"])
            if "git_blob" in row:
                require(blob(b) == row["git_blob"], "ZIP_GIT_BLOB_MISMATCH")
        require(manifest.get("codex_ack") is False and
                manifest.get("native_unity") == "NOT_RUN",
                "FALSE_ACK_OR_UNITY_PASS")
        return {"sha256": sha256(archive), "bytes": len(archive), "entries": len(names),
                "source_files": len(rows)}

def build_original_zip(repo, producer, toolpath, label):
    with tempfile.TemporaryDirectory(prefix="b01-40-w11-") as tmp:
        archive = Path(tmp) / "original.zip"
        receipt = Path(tmp) / "producer.json"
        if label == "v2":
            command(sys.executable, str((repo / toolpath).resolve()), "build",
                    "--archive", str(archive), cwd=repo)
        else:
            command(sys.executable, str((repo / toolpath).resolve()),
                    str(archive), str(receipt), cwd=repo)
        real = archive.read_bytes()
        return real, verify_archive(real, repo, label)

def preflight(v2, guard, legacy, w06, w09):
    head(v2, V2)
    head(guard, GUARD)
    head(legacy, W07)
    head(w06, W06)
    head(w09, W09)
    files_v2 = scope(v2, W06)
    require(len(files_v2) == 27 and all(
        p.startswith("Assets/B0123SecureReattachV2") or
        p.startswith("Tools/B01/B01-23/P17W10SecureReattachV2/") or
        p == ".github/workflows/b01-23-p17w10-secure-reattach-v2.yml"
        for p in files_v2), "V2_OWNERSHIP_SCOPE_DRIFT")
    files_guard = scope(guard, W09)
    require(len(files_guard) == 10 and SOURCE_GUARD in files_guard and all(
        p == SOURCE_GUARD or
        p.startswith("Tools/B01/B01-23/P17W10PublicDockGuardR2/") or
        p.startswith("Assets/B0123DockHardening001/Tests/PlayMode/W10PublicDockGuardPlayModeTests.cs") or
        p == ".github/workflows/b01-23-p17w10-reattach-public-api-guard.yml"
        for p in files_guard), "GUARD_OWNERSHIP_SCOPE_DRIFT")
    immutable = "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"
    expect = "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
    for r in (w06, v2, guard):
        require(blob(source(r, immutable)) == expect, "ORIGINAL_W06_LEDGER_DRIFT")
    require(blob(source(legacy, SOURCE_GUARD)) ==
            "e4e31c32cb5abda2fef987efa4b45fd61e828312",
            "LEGACY_W07_BLOB_DRIFT")
    assert source(w06, immutable).count(b"seen.Remove(id)") == 1
    assert source(w06, immutable).count(b"seen.Clear()") == 1
    v2_text = source(v2, SOURCE_V2).decode()
    patch_text = source(guard, SOURCE_GUARD).decode()
    old = source(legacy, SOURCE_GUARD).decode()
    return {"v2_order": verify_v2(v2_text, old),
            "guard_order": verify_guard(patch_text),
            "v2_changed_paths": len(files_v2), "guard_changed_paths": len(files_guard),
            "legacy_original_blob": blob(old.encode())}

def gate_tests(v2, guard, legacy):
    v2_text = source(v2, SOURCE_V2).decode()
    guard_text = source(guard, SOURCE_GUARD).decode()
    old = source(legacy, SOURCE_GUARD).decode()
    def fail(action):
        try:
            action()
        except Rejected:
            return True
        return False
    tests = {
      "V2 rejection of unguarded public alias": fail(lambda: verify_v2(
          v2_text.replace("DockGate().Reattach(index)", "CommitDock(index)"), old)),
      "V2 rejection of bypassed GUI": fail(lambda: verify_v2(
          v2_text.replace("ReattachFromGui(i);", "CommitDock(i);"), old)),
      "V2 rejection of deleted inspector": fail(lambda: verify_v2(
          v2_text.replace("Physics.ComputePenetration(", "Physics.MissingPenetration("), old)),
      "guard rejection of removed API validation": fail(lambda: verify_guard(
          guard_text.replace("ValidatePhysicalReattach(rig)", "true"))),
      "guard rejection of removed all-layer query": fail(lambda: verify_guard(
          guard_text.replace("rotation, ~0, QueryTriggerInteraction.Collide",
                             "rotation, 0, QueryTriggerInteraction.Collide"))),
      "V2 source detects commit-before-rearm": verify_v2(v2_text, old)["mutation_before_rearm"],
      "guard source detects commit-before-rearm": verify_guard(guard_text)["mutation_before_rearm"],
      "V2 ledger source has independent clear and rearm": (
          "ClearForNewRun" in source(v2, "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs").decode()
          and "RearmAfterReattach" in source(v2, "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs").decode()),
    }
    require(all(tests.values()), "SELF_TEST_FAILED:" + repr([k for k,v in tests.items() if not v]))
    return tests

def main():
    parser = argparse.ArgumentParser()
    for a in ("v2", "guard", "legacy", "w06", "w09", "out"):
        parser.add_argument("--" + a, required=True, type=Path)
    parser.add_argument("--choice", choices=("none", "v2", "guard", "both"), default="none")
    parser.add_argument("--release", action="store_true")
    a = parser.parse_args()
    repos = [getattr(a, x).resolve() for x in ("v2", "guard", "legacy", "w06", "w09")]
    pre = preflight(*repos)
    tests = gate_tests(repos[0], repos[1], repos[2])
    archives = {}
    for label, path, toolpath in (("v2", repos[0], PACK_V2), ("guard", repos[1], PACK_GUARD)):
        _, receipt = build_original_zip(path, None, toolpath, label)
        archives[label] = receipt
    vulnerability = [name for name in ("v2_order", "guard_order")
                     if pre[name]["mutation_before_rearm"] and not pre[name]["explicit_rollback"]]
    reason = []
    if a.choice in ("none", "both"):
        reason.append("EXACTLY_ONE_OWNER_SELECTED_VARIANT_REQUIRED")
    if a.choice in ("v2", "guard") and (a.choice + "_order") in vulnerability:
        reason.append("COMMIT_BEFORE_REARM_NO_ATOMIC_ROLLBACK")
    # Report all known risks, regardless of which variant has been selected.
    if vulnerability:
        reason.append("FROZEN_VARIANTS_HAVE_TRANSACTION_DEFECT:" + ",".join(vulnerability))
    reason.append("NO_NATIVE_UNITY_G1_OR_AUTHORIZED_CODEX_MIGRATION")
    payload = {
        "gate": "B01-40-P17-W11-EXCLUSIVE-DOCK-TXN-001",
        "mode": "READ_ONLY_EXACT_GIT_SOURCE_AND_GENUINE_ZIP_VERIFICATION",
        "choice": a.choice,
        "frozen_sha": {"v2": V2, "guard": GUARD, "legacy": W07, "w06": W06, "w09": W09},
        "audit": pre, "mutation_tests": tests, "original_producer_archives": archives,
        "gate_result": "G0_TRANSPORT_PASS__RELEASE_SAFETY_HOLD",
        "release_blockers": reason,
        "g1_native_unity": "NOT_RUN", "g2": "HOLD",
        "codex_ack": False, "integrated": False, "merged": False,
        "receivers": ["B01-23_NEW_IMMUTABLE_OWNER_REVISION", "B01-60/61/62_INDEPENDENT_NEW_SHA_QA",
                      "B01-40_NEW_SOURCE_PHYSICAL_INTAKE", "B01-00_QUEUE_DECISION"],
        "rollback": "Read only source checkouts; original ZIPs in ephemeral scratch; no canonical edits"
    }
    a.out.mkdir(exist_ok=True, parents=True)
    (a.out / "w11-dock-intake.json").write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n")
    print("SOURCE_ARCHIVE_VERIFIED", json.dumps(archives, sort_keys=True))
    print("INDEPENDENT_NEGATIVE_TESTS", len(tests), "PASS")
    print("RELEASE_GATE_HOLD", ";".join(reason))
    if a.release:
        print("FAIL_CLOSED_NO_SELECTED_ATOMIC_DOCK_VERSION", file=sys.stderr)
        sys.exit(3)

if __name__ == "__main__":
    try:
        main()
    except (Rejected, OSError, ValueError, KeyError, subprocess.SubprocessError, zipfile.BadZipFile, AssertionError) as e:
        print("B01-40 W11 INTAKE FAIL", str(e), file=sys.stderr)
        sys.exit(1)
