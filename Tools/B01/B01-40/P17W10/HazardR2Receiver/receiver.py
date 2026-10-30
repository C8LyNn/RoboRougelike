#!/usr/bin/env python3
"""B01-40 W10: immutable source intake and reproducible optional Unity import ZIP.

This tool never writes into a Git source checkout, and DOES NOT run native Unity.
"""
import argparse
import hashlib
import io
import json
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

R1 = "6095577ece27320b1a861a738cc23ba42d6793a6"
R2 = "ebc3d6162f4516147092960291e50e15fe6c4582"
QA = "2538f60093d3e8732e3c7865a10ef81ac3a340d7"
CODEX = "203a0805251ed28293888e43f2d49f0df63767c6"
R2_DIR = "Research/B01-25/P17/HAZARDPULSE001R2/"
QA_MANIFEST = "Tools/B01/B01-60/P17W09/HazardPhaseReworkGate/manifest.json"
UNITY = "Assets/B0140HazardPulseR2"
MANIFEST = "B01-40-PHYSICAL-RECEIVER.json"

# Receiver-owned GUID identities: never modify author #470 or original #430.
GUIDS = {
    UNITY + ".meta": "84f1926a137e40a88f437ed136705001",
    UNITY + "/Runtime.meta": "84f1926a137e40a88f437ed136705002",
    UNITY + "/Runtime/PulseCore.cs.meta": "84f1926a137e40a88f437ed136705003",
    UNITY + "/Runtime/B0140.HazardPulseR2.Runtime.asmdef.meta": "84f1926a137e40a88f437ed136705004",
    UNITY + "/Data.meta": "84f1926a137e40a88f437ed136705005",
    UNITY + "/Data/profiles.json.meta": "84f1926a137e40a88f437ed136705006",
}

class GateError(Exception):
    pass

def check(condition, message):
    if not condition:
        raise GateError(message)

def git(root, *args):
    r = subprocess.run(["git", "-C", str(root), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if r.returncode:
        raise GateError("git " + " ".join(args) + ": " + r.stderr.decode(errors="replace"))
    return r.stdout

def head(root, expected, role):
    actual = git(root, "rev-parse", "HEAD").decode().strip()
    check(actual == expected, role + " unexpected HEAD " + actual)

def file_at(root, filename):
    return git(root, "show", "HEAD:" + filename)

def hash256(content):
    return hashlib.sha256(content).hexdigest()

def git_blob(content):
    return hashlib.sha1(b"blob " + str(len(content)).encode() + b"\0" + content).hexdigest()

def validate_source(source, qa, r1):
    head(source, R2, "R2")
    head(qa, QA, "INDEPENDENT_QA")
    head(r1, R1, "R1")
    manifest = json.loads(file_at(source, R2_DIR + "manifest.json"))
    check(manifest["packageId"] == "B01-25-HAZARDPULSE-001" and manifest["version"] == "R2", "wrong producer version")
    check(manifest["originalFrozen"]["sha"] == R1, "wrong original producer SHA")
    check(manifest["independentRed"]["sha"] == "5dc838f3342083ebc78bbf8704053b996dac4a8a", "wrong frozen RED")
    pins = manifest["gitBlobs"]
    check(len(pins) == 10, "R2 must pin 10 immutable original Git files")
    actual = set(git(source, "diff", "--name-only", R1, "HEAD").decode().splitlines())
    expected = {R2_DIR + "manifest.json"}
    for item, blob in pins.items():
        path = item if item.startswith(".github/") else R2_DIR + item
        check(path.startswith(R2_DIR) or path == ".github/workflows/b01-25-w09-hazardpulse-r2.yml", "foreign producer path")
        data = file_at(source, path)
        check(git_blob(data) == blob, "R2 producer Git blob mismatch: " + path)
        expected.add(path)
    check(actual == expected and len(actual) == 11, "R2 changed-scope mismatch, expected only 11 new owner paths")
    check(all(not item.startswith("Assets/") for item in actual), "original package modified canonical Assets")
    original_core = file_at(r1, "Research/B01-25/P17/HAZARDPULSE001/PulseCore.cs")
    check(git_blob(original_core) == manifest["originalFrozen"]["coreGitBlob"], "frozen R1 mutated")
    independent = json.loads(file_at(qa, QA_MANIFEST))
    candidate = independent["original"]["producer_r2"]
    check(candidate["source_sha"] == R2, "independent QA tests another producer SHA")
    core = file_at(source, R2_DIR + "PulseCore.cs")
    check(git_blob(core) == candidate["git_blob"] and
          candidate["git_blob"] == pins["PulseCore.cs"], "independent source core pin mismatch")
    check(independent["gates"]["G0_candidate"] == "INDEPENDENT_SOURCE_GREEN_R2_10PASS_0FAIL_0_MISMATCHES", "independent R2 verdict not green")
    check(independent["gates"]["G0_original"].startswith("INDEPENDENT_RED"), "original R1 RED must remain")
    profile = file_at(source, R2_DIR + "profiles.json")
    parsed = json.loads(profile)
    check(isinstance(parsed, (dict, list)), "invalid original source profiles")
    return core, profile, {"git_blob": git_blob(core), "profile_git_blob": git_blob(profile), "pins": len(pins), "diff": len(actual)}

def meta(guid, kind):
    if kind == "folder":
        return ("fileFormatVersion: 2\nguid: " + guid +
                "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()
    importer = "MonoImporter" if kind == "script" else "AssemblyDefinitionImporter" if kind == "asmdef" else "DefaultImporter"
    return ("fileFormatVersion: 2\nguid: " + guid +
            "\n" + importer + ":\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()

def all_guids(root):
    r = subprocess.run(["git", "-C", str(root), "grep", "-h", "-I", "^guid: ", "HEAD", "--", "*.meta"],
                       capture_output=True)
    check(r.returncode in (0, 1), "Git GUID scan failed: " + r.stderr.decode(errors="replace"))
    values = set()
    for s in r.stdout.decode(errors="replace").splitlines():
        uid = s.replace("guid: ", "").strip()
        check(re.fullmatch("[a-f0-9]{32}", uid) is not None, "invalid tracked Unity GUID " + uid)
        values.add(uid)
    return values

def build_payload(source, qa, r1, receiver):
    head(receiver, CODEX, "CODEX_READ_ONLY")
    core, profile, facts = validate_source(source, qa, r1)
    own = set(GUIDS.values())
    check(len(own) == len(GUIDS), "receiver overlay has duplicate GUID")
    for role, repo in [("producer", source), ("qa", qa), ("original", r1), ("Codex", receiver)]:
        collide = own & all_guids(repo)
        check(not collide, "receiver generated GUID collision with " + role + ": " + repr(collide))
    # Namespace/type cannot be imported twice in a single future target assembly.
    overlap = subprocess.run(["git", "-C", str(receiver), "grep", "-l", "class PulseZoneController",
                              "HEAD", "--", "Assets/**/*.cs"], capture_output=True)
    check(overlap.returncode == 1, "Codex receiver contains PulseZoneController already or inspection failed")

    asm = {"name": "B0140.HazardPulseR2.Runtime", "rootNamespace": "B01.LevelChallenges",
           "references": [], "includePlatforms": [], "excludePlatforms": [],
           "allowUnsafeCode": False, "overrideReferences": False,
           "precompiledReferences": [], "autoReferenced": False,
           "defineConstraints": [], "versionDefines": [], "noEngineReferences": True}
    check(asm["references"] == [] and asm["autoReferenced"] is False and
          asm["noEngineReferences"] is True, "unsafe asmdef")
    files = {
        UNITY + "/Runtime/PulseCore.cs": core,
        UNITY + "/Runtime/B0140.HazardPulseR2.Runtime.asmdef": (json.dumps(asm, indent=2) + "\n").encode(),
        UNITY + "/Data/profiles.json": profile,
    }
    for filename, uid in GUIDS.items():
        kind = "folder" if filename.endswith("HazardPulseR2.meta") or filename.endswith("Runtime.meta") or filename.endswith("Data.meta") else (
            "script" if filename.endswith(".cs.meta") else "asmdef" if filename.endswith(".asmdef.meta") else "asset")
        files[filename] = meta(uid, kind)
    evidence = {
        "schema": "b01-40-receiver-physical-v1",
        "package_id": "B01-25-HAZARDPULSE-001", "revision": "R2",
        "original_pr": 430, "original_sha": R1, "producer_pr": 470,
        "producer_sha": R2, "producer_core_git_blob": facts["git_blob"],
        "producer_profiles_git_blob": facts["profile_git_blob"], "producer_manifest_pins_verified": facts["pins"],
        "producer_owner_paths_verified": facts["diff"], "independent_qa_pr": 474, "independent_qa_sha": QA,
        "independent_qa_expected": "R1_RED_9PASS_1FAIL_38_MISMATCHES__R2_GREEN_10PASS_0FAIL_0_MISMATCHES",
        "receiver_readonly_codex_sha": CODEX,
        "owner": "B01-40", "scope": "new optional Assets/B0140HazardPulseR2 only on authorized disposable receiver",
        "assembly": asm["name"], "assembly_references": [], "guids": GUIDS,
        "limitations": ["source-only G0; native Unity/PhysX/HP/VFX/Player NOT_RUN",
                        "no canonical gameplay hookup or enemy/HP logic",
                        "no approved adapter, no CODEX_ACK, not integrated",
                        "profiles.json is source data; not validated as Unity JsonUtility schema",
                        "do not import R1 and R2 into a conflicting game namespace/assembly"],
        "source_g0": "SOURCE_PHYSICAL_ACCEPTABLE_IF_THIS_CI_PASSES",
        "native_g1": "NOT_RUN", "g2": "HOLD", "codex_ack": False, "integrated": False, "merge": False,
        "install": "extract only into NEW separate authorized Unity checkout; no overwrite of an existing file or folder",
        "rollback": "remove only original unmodified new B0140HazardPulseR2 subtree and .meta after backup; reject changed/unowned files",
        "files": [{"path": key, "bytes": len(value), "sha256": hash256(value),
                   "git_blob": git_blob(value)} for key, value in sorted(files.items())]
    }
    files[MANIFEST] = (json.dumps(evidence, sort_keys=True, separators=(",", ":")) + "\n").encode()
    return files, evidence

def deterministic_zip(files):
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w") as z:
        for filename, data in sorted(files.items()):
            check(filename and not filename.startswith("/") and not filename.startswith("\\") and
                  "\\" not in filename and ".." not in filename.split("/"), "ZIP path traversal")
            i = zipfile.ZipInfo(filename, (1980, 1, 1, 0, 0, 0))
            i.compress_type = zipfile.ZIP_STORED
            i.create_system = 3
            i.external_attr = 0o100644 << 16
            z.writestr(i, data)
    return output.getvalue()

def exact_archive(data, expected):
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        names = z.namelist()
        check(len(names) == len(set(names)) and set(names) == set(expected), "ZIP member mismatch/duplicate")
        for n in names:
            check(not n.startswith("/") and "\\" not in n and ".." not in n.split("/"), "ZIP unsafe traversal")
            check(z.read(n) == expected[n], "ZIP content != source: " + n)
        manifest = json.loads(z.read(MANIFEST))
    check(manifest["codex_ack"] is False and manifest["native_g1"] == "NOT_RUN", "false native/ACK metadata")
    return manifest

def strict_stage(files, dest):
    check(not dest.exists(), "target exists, overwrite forbidden: " + str(dest))
    dest.mkdir(parents=False)
    try:
        for n, data in files.items():
            p = dest / n
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_bytes(data)
    except Exception:
        shutil.rmtree(dest)
        raise

def strict_rollback(files, dest):
    check(dest.is_dir() and not dest.is_symlink(), "missing/unsafe stage")
    found = [p for p in dest.rglob("*")]
    check(not any(p.is_symlink() for p in found), "rollback refuses symlink")
    actual = {p.relative_to(dest).as_posix(): p.read_bytes() for p in found if p.is_file()}
    check(actual == files, "rollback refused changed/missing/foreign file")
    shutil.rmtree(dest)
    check(not dest.exists(), "rollback incomplete")

def expect_rejected(operation, label):
    try:
        operation()
    except GateError:
        return 1
    raise GateError(label + ": negative control unexpectedly accepted")

def negative_receipts(files, archive):
    count = 0
    with tempfile.TemporaryDirectory(prefix="b01-40-r2-g0-") as d:
        dest = Path(d) / "stage"
        strict_stage(files, dest)
        check(len([p for p in dest.rglob("*") if p.is_file()]) == len(files), "staged file count")
        count += expect_rejected(lambda: strict_stage(files, dest), "overwrite")
        ownpath = dest / sorted(files)[0]
        saved = ownpath.read_bytes()
        ownpath.write_bytes(b"modified-by-foreign-receiver")
        count += expect_rejected(lambda: strict_rollback(files, dest), "tampered rollback")
        ownpath.write_bytes(saved)
        extra = dest / "FOREIGN.txt"
        extra.write_text("preserve me")
        count += expect_rejected(lambda: strict_rollback(files, dest), "foreign member rollback")
        extra.unlink()
        strict_rollback(files, dest)
        count += 1
    changed = dict(files)
    changed[sorted(files)[0]] = b"tampered"
    count += expect_rejected(lambda: exact_archive(archive, changed), "corrupted source bytes")
    count += expect_rejected(lambda: exact_archive(
        deterministic_zip({**files, "FOREIGN": b"unexpected"}), files), "unexpected ZIP member")
    count += expect_rejected(lambda: exact_archive(
        deterministic_zip({k:v for k,v in files.items() if k != sorted(files)[0]}), files), "missing ZIP member")
    count += expect_rejected(lambda: deterministic_zip({"../ESCAPE": b"malicious"}), "ZIP traversal")
    return count

def main():
    cli = argparse.ArgumentParser()
    cli.add_argument("--source", required=True, type=Path)
    cli.add_argument("--qa", required=True, type=Path)
    cli.add_argument("--r1", required=True, type=Path)
    cli.add_argument("--receiver", required=True, type=Path)
    cli.add_argument("--out", required=True, type=Path)
    a = cli.parse_args()
    roots = [a.source.resolve(), a.qa.resolve(), a.r1.resolve(), a.receiver.resolve()]
    files, manifest = build_payload(*roots)
    data = deterministic_zip(files)
    exact_archive(data, files)
    checks = negative_receipts(files, data)
    check(checks == 8, "negative tests count not satisfied")
    a.out.mkdir(parents=True, exist_ok=True)
    archive = a.out / "B01-40-HAZARDPULSE-R2-UNITY-SOURCE.zip"
    archive.write_bytes(data)
    receipt = {"status": "G0_PHYSICAL_SOURCE_PASS", "producer_sha": R2, "qa_sha": QA,
               "physical_zip_sha256": hash256(data), "zip_bytes": len(data),
               "zip_members": len(files), "meta_guid_count": len(GUIDS),
               "producer_git_pins": manifest["producer_manifest_pins_verified"],
               "negative_staging_archive_tests": checks, "native_unity": "NOT_RUN",
               "codex_ack": False, "integrated": False, "merge": False}
    (a.out / "G0-RECEIPT.json").write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
    print("G0_PHYSICAL_SOURCE_PASS", json.dumps(receipt, sort_keys=True))
    print("R1_REJECTED_R2_SOURCE_ACCEPTED UNITY_G1_NOT_RUN G2_HOLD CODEX_ACK_NONE NO_MERGE")

if __name__ == "__main__":
    try:
        main()
    except (GateError, OSError, ValueError, KeyError, subprocess.SubprocessError, zipfile.BadZipFile) as e:
        print("B01-40 G0 FAIL:", str(e), file=sys.stderr)
        sys.exit(1)
