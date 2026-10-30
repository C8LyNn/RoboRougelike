#!/usr/bin/env python3
"""B01-60 W23 read-only receipt auditor for the ORIGINAL, frozen #718 R5 runner.

Audits only already-created local evidence. It DOES NOT launch Unity, attest
execution, acquire Save slot leases, change game files, or accept full R5 G1.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import sys
import xml.etree.ElementTree as ET

R5_SHA = "cc7f73541517a18f4479bb155534bd4832d9985e"
GAME_SHA = "d94400d95d13db7f27e1533df05c9292d5380431"
SAVE_SHA = "33094f154c7b91119b36d58aab285e6e83c5de57"
PARTIAL = (
    "ColdLoad_RealRigidbody_RoundTripLightHeavy",
    "CorruptLoad_LeavesPreviouslyActiveRigidbodyAndIdsUntouched",
    "WrongGeneration_CannotPartiallyPublishRealPhysicalWorld",
    "SeparateRunSlots_CannotPoisonLaterRunOrReuseForeignOwner",
    "ComponentOnlyDestroy_RetainsLeaseUntilRealChildRigidbodyDestruction",
    "DisabledSameSlotHost_CannotStealOwnerUntilDestroyed",
)
HELD = "ColdRestart_RestoresRealRigidbodyAndConsumedIds"
SUITE = "B01.UnitySaveBridge001.Tests.IsolatedSaveWorldNativePlayModeTests."
PROBE = "B01.UnitySaveBridge001.Tests.B0160W22ScopeProbe.OnlyPrivateSavePath"
FIXTURE_PATH = "Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"
PROBE_PATH = "Assets/B0123UnitySaveBridge001/Tests/B0160W22ScopeProbe.cs"
SETTINGS_PATH = "ProjectSettings/ProjectSettings.asset"
ORIGINAL_GIT_BLOBS = {
    FIXTURE_PATH: "c8c603451a0668d50ba3af91ed888a64910bdc9f",
    "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":
        "7da9a9efe489cf8c441d7bb9a47ba3f60ba4c58c",
    "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":
        "6b3936d82b5e1f175856a9e138124b14c9dfd19c",
    "Assets/B0123UnitySaveBridge001/Core/OwnerWorldLeaseRetirement.cs":
        "1ef69de9b4029ce89ba2db5bf3331708da1b91e4",
}
EXPECTED_NATIVE_LOGS = {
    f"{kind}{i}{ext}" for i in range(len(PARTIAL))
    for kind in ("guard", "r5case")
    for ext in (".xml", "-Editor.log", "-process.log")
}

class Hold(ValueError):
    pass

def need(value, reason):
    if not value:
        raise Hold("W23_R5_EVIDENCE_HOLD: " + reason)

def sha256(raw):
    return hashlib.sha256(raw).hexdigest()

def git_blob_sha1(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode("ascii") + b"\0" + raw).hexdigest()

def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeError, ValueError) as exc:
        raise Hold("W23_R5_EVIDENCE_HOLD: missing/invalid JSON: " + path.name) from exc

def verify_path(path):
    p = Path(path).absolute()
    need(p.is_dir() and not p.is_symlink(), "evidence directory absent or linked")
    for ancestor in (p, *p.parents):
        need(not ancestor.is_symlink(), "symlink evidence ancestor")
    return p

def safe_member(evidence, name):
    need(isinstance(name, str) and name and "\\" not in name and "\x00" not in name,
         "invalid manifest member name")
    p = PurePosixPath(name)
    need(not p.is_absolute() and p.as_posix() == name and
         all(part not in (".", "..", "") for part in name.split("/")),
         "noncanonical/traversing evidence member")
    f = evidence.joinpath(*p.parts)
    need(f.is_file() and not f.is_symlink(), "missing/linked manifest file " + name)
    cur = f.parent
    while cur != evidence:
        need(not cur.is_symlink(), "linked member directory")
        cur = cur.parent
    return f

def verify_inventory(evidence, manifest):
    files = manifest.get("evidence_sha256")
    need(isinstance(files, dict) and len(files) > 0, "missing original SHA256 inventory")
    for name, digest in files.items():
        f = safe_member(evidence, name)
        need(isinstance(digest, str) and re.fullmatch("[0-9a-f]{64}", digest) is not None,
             "noncanonical SHA256 digest")
        need(sha256(f.read_bytes()) == digest, "source/evidence digest mismatch " + name)
    return set(files)

def verify_nunit(path, expected):
    try:
        root = ET.parse(path).getroot()
    except (ET.ParseError, OSError) as exc:
        raise Hold("W23_R5_EVIDENCE_HOLD: missing/malformed NUnit XML") from exc
    need(root.tag == "test-run" and root.attrib.get("result") == "Passed",
         "NUnit root is not Passed")
    cases = list(root.iter("test-case"))
    need(len(cases) == 1, "each isolated R5 NUnit filter must run exactly one case")
    only = cases[0]
    need(only.attrib.get("fullname", "").replace(" ", "") == expected and
         only.attrib.get("result") == "Passed", "wrong/failed/skipped/duplicate NUnit case")
    for key in ("total", "passed"):
        need(root.attrib.get(key) == "1", "NUnit root " + key + " !=1")
    need(root.attrib.get("failed") == "0", "NUnit root failed!=0")
    for key in ("skipped", "inconclusive"):
        need(root.attrib.get(key, "0") == "0", "NUnit " + key + "!=0")

def verify_process_log(path):
    data = path.read_bytes()
    need(len(data) > 11 and data.splitlines()[0] == b"exit_code=0",
         "missing/nonzero original Unity process exit")

def verify_native(evidence, inventory):
    need(EXPECTED_NATIVE_LOGS.issubset(inventory),
         "missing one or more of 12 XML, 12 Editor.logs, 12 process logs")
    raw_logs = {p.name for p in evidence.iterdir() if p.is_file() and
                (p.suffix == ".xml" or p.name.endswith("-Editor.log") or
                 p.name.endswith("-process.log"))}
    need(raw_logs == EXPECTED_NATIVE_LOGS, "unexpected/missing original native logs")
    products = set()
    for i, method in enumerate(PARTIAL):
        project = evidence / ("P" + str(i))
        need(project.is_dir() and not project.is_symlink(), "private project absent")
        settings = safe_member(evidence, "P" + str(i) + "/" + SETTINGS_PATH).read_text(
            "utf-8", errors="strict")
        found = re.findall(r"(?m)^\s*productName:\s*(\S+)\s*$", settings)
        need(len(found) == 1 and
             re.fullmatch("B0160W22Case" + str(i) + "[0-9a-f]{12}", found[0]) is not None,
             "private Unity PlayerSettings product scope violated")
        need(found[0] not in products, "reused product checkpoint scope")
        products.add(found[0])
        original_probe = safe_member(evidence, "P" + str(i) + "/" + PROBE_PATH).read_text(
            "utf-8", errors="strict")
        need(found[0] in original_probe and "OnlyPrivateSavePath" in original_probe,
             "private Save checkpoint guard missing")
        for rel, blob in ORIGINAL_GIT_BLOBS.items():
            source = safe_member(evidence, "P" + str(i) + "/" + rel).read_bytes()
            need(git_blob_sha1(source) == blob,
                 "not original selected #690 Git blob: " + rel)
        for tag, fullname in (("guard" + str(i), PROBE),
                               ("r5case" + str(i), SUITE + method)):
            verify_nunit(evidence / (tag + ".xml"), fullname)
            log = (evidence / (tag + "-Editor.log")).read_text(
                encoding="utf-8", errors="replace")
            need("6000.3.14f1" in log, "original Unity Editor.log version missing")
            verify_process_log(evidence / (tag + "-process.log"))
    return len(PARTIAL)

def audit(evidence):
    e = verify_path(evidence)
    receipt = read_json(e / "P22-receipt.json")
    manifest = read_json(e / "P22-sha256-manifest.json")
    need(isinstance(receipt, dict) and isinstance(manifest, dict),
         "invalid top-level original R5 receipt")
    need(manifest.get("unsigned") is True and manifest.get("receipt") == receipt,
         "manifest/receipt scope mismatch or false signature")
    need(receipt.get("package") == "B01-60-P17W22-R5-NATIVE-PARTIAL",
         "wrong frozen producer")
    need(receipt.get("selected_host_R5_SHA") == R5_SHA and
         receipt.get("original_510_SHA") == GAME_SHA and
         receipt.get("original_556_SHA") == SAVE_SHA,
         "original selected source SHA mismatch")
    need(receipt.get("R5_FULL_NATIVE_G1") ==
         "HOLD_ORIGINAL_COLDRESTART_GEN4_EXPECTS_GEN5", "ColdRestart HOLD dropped")
    need(tuple(receipt.get("native_partial_scope", ())) == PARTIAL and
         tuple(receipt.get("source_methods", ())) ==
         (PARTIAL[0], PARTIAL[1], HELD, *PARTIAL[2:]),
         "not exactly selected original seven tests / six-test scope")
    need(receipt.get("merged") is False and receipt.get("uploaded") is False and
         receipt.get("source_G0") == "B01-40_699_SOURCE_ONLY",
         "false merge/upload/product G0 claim")
    inventory = verify_inventory(e, manifest)
    need("P22-receipt.json" in inventory, "operator receipt lacks independent SHA")
    if receipt.get("native_G1") == "NOT_RUN":
        need(receipt.get("source_preflight") == "PASS_6_STAGE_ROLLBACK_NO_UNITY",
             "no original six-project preflight success")
        need(inventory == {"P22-receipt.json"} and
             not any(e.glob("P[0-5]")) and
             not (set(p.name for p in e.iterdir()) & EXPECTED_NATIVE_LOGS),
             "source-only receipt contains extra Unity/native evidence")
        return "SOURCE_PREFLIGHT_ONLY_NATIVE_NOT_RUN"
    need(receipt.get("native_G1") == "PARTIAL_R5_SIX_OF_SEVEN_ONLY",
         "unknown or falsely promoted Unity G1")
    need("source_or_native_error" not in receipt,
         "native original producer reports an error")
    n = verify_native(e, inventory)
    return f"STRUCTURALLY_CONSISTENT_{n}_R5_CASES_UNATTESTED_FULL_G1_HOLD"

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--evidence", required=True)
    a = p.parse_args()
    try:
        result = audit(a.evidence)
        print("B0160_W23_R5_RECEIPT=" + result)
        print("B0160_W23_AUDIT=READ_ONLY UNITY_NOT_LAUNCHED")
        print("B0160_W23_R5_FULL_G1=HOLD_COLDRESTART_4_VS_5 "
              "V3_729_PLUS_712_CANONICAL_G1=NOT_RUN G2=HOLD")
        return 0
    except (Hold, ValueError, OSError, UnicodeError) as exc:
        print("B0160_W23_REJECT=" + str(exc), file=sys.stderr)
        return 2

if __name__ == "__main__":
    sys.exit(main())
