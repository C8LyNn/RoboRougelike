#!/usr/bin/env python3
"""B01-10 GHOST-001/R11 deterministic Unity import ZIP, provenance and 6 negatives.

Python 3.8+, stdlib only. SOURCE/IMPORT-READY is NOT a Unity G1 or Codex ACK.
"""
import argparse
import hashlib
import json
import re
import sys
import tempfile
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
SOURCES = (
    "GhostCore.cs", "GhostVisualProxy.cs",
    "GhostSplitCoach.cs", "GhostSplitHud.cs",
    "GhostCore.cs.meta", "GhostVisualProxy.cs.meta",
    "GhostSplitCoach.cs.meta", "GhostSplitHud.cs.meta",
    "B01TrainingGhost.meta",
    "Program.cs", "Ghost.Runner.csproj", "README.md",
)
FOLDER = "UnityInstall/Assets/B01TrainingGhost.meta"
GAME = "UnityInstall/Assets/B01TrainingGhost/"
EVIDENCE = "Evidence/"
RECEIPT = "SOURCE_RECEIPT.json"
GUIDS = {
    "B01TrainingGhost.meta": "8d7f2b090c0d4ef3ab2436c855d0b104",
    "GhostCore.cs.meta": "74f1111e2e76821101d5ee4b43683a33",
    "GhostVisualProxy.cs.meta": "8e613294fbd6ad5dff7a0b23a5aa96e0",
    "GhostSplitCoach.cs.meta": "7cf6f79b815d4778ab69d561811a6001",
    "GhostSplitHud.cs.meta": "407311dc82d84a41baf4fe8f1ae028a6",
}

def sha256(data):
    return hashlib.sha256(data).hexdigest()

def git_blob(data):
    return hashlib.sha1(
        b"blob " + str(len(data)).encode("ascii") + b"\0" + data
    ).hexdigest()

def require(condition, error):
    if not condition:
        raise ValueError(error)

def source_files():
    manifest = json.loads((HERE / "MANIFEST.json").read_text(encoding="utf-8"))
    require(manifest.get("packageId") == "B01-10-GHOST-001",
            "Unexpected package identity")
    require(manifest.get("tests", {}).get("nativeUnity") == "NOT_RUN",
            "Fake Unity-native PASS in producer manifest")
    require(manifest.get("gates", {}).get("CodeXACK") == "PENDING",
            "Fake Codex ACK in producer manifest")
    require(manifest.get("originalGameCodeTouched") is False,
            "Producer manifest claims canonical game modification")
    require(set(manifest["codeFiles"]) == set(SOURCES),
            "Manifest must pin exactly the twelve original source files")
    require(manifest.get("importLayout", {}).get("GUIDS") == GUIDS,
            "Manifest GUID contract mismatch")
    found = {}
    for name in SOURCES:
        data = (HERE / name).read_bytes()
        require(git_blob(data) == manifest["codeFiles"][name]["gitBlob"],
                "Git blob identity mismatch: " + name)
        found[name] = data
    for name, guid in GUIDS.items():
        require(re.search(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$", found[name])
                is not None, "No valid Unity GUID in " + name)
        values = re.findall(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$", found[name])
        require(len(values) == 1 and values[0].decode("ascii") == guid,
                "GUID source drift: " + name)
    require(len(set(GUIDS.values())) == len(GUIDS), "Duplicate Unity GUID")
    require(b"UnityEngine" not in found["GhostCore.cs"],
            "Portable ghost core has a new Unity compile dependency")
    require(b"#if UNITY_5_3_OR_NEWER" in found["GhostVisualProxy.cs"],
            "Unity-only visual is no longer protected for .NET8 runner")
    return found, (HERE / "MANIFEST.json").read_bytes()

def expected_members(sha):
    require(re.fullmatch(r"[0-9a-f]{40}", sha) is not None,
            "Require exact 40-hex original GitHub source SHA")
    original, manifest = source_files()
    members = {}
    for name in SOURCES:
        if name == "B01TrainingGhost.meta":
            output = FOLDER
        elif name.startswith("Ghost") and name not in ("Ghost.Runner.csproj",):
            output = GAME + name
        else:
            output = EVIDENCE + name
        members[output] = original[name]
    members[EVIDENCE + "MANIFEST.json"] = manifest
    require(len(members) == 13, "Source bundle expected 13 physical members")
    entries = [
        {"path": name, "bytes": len(blob), "sha256": sha256(blob)}
        for name, blob in sorted(members.items())
    ]
    receipt = {
        "package": "B01-10-GHOST-001/R11",
        "original_source_sha": sha,
        "original_pr": "https://github.com/C8LyNn/RoboRougelike/pull/422",
        "game_import_path": "Assets/B01TrainingGhost",
        "source_members": entries,
        "native_unity": "NOT_RUN",
        "independent_G0": "PENDING",
        "independent_QA": "PENDING",
        "codex_ack": "PENDING",
        "integrated": False,
        "warning": "Importable sources ONLY; never install into canonical Codex Assets without authorization",
    }
    members[RECEIPT] = (
        json.dumps(receipt, sort_keys=True, separators=(",", ":")) + "\n"
    ).encode("utf-8")
    require(len(members) == 14, "Final ZIP must contain fourteen exact members")
    return members

def write_zip(path, members):
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_STORED,
                         allowZip64=False) as out:
        for name in sorted(members):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_STORED
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            out.writestr(info, members[name])

def verify_zip(path, members, sha):
    require(path.is_file(), "Missing physical ZIP file")
    with zipfile.ZipFile(path, "r") as z:
        names = z.namelist()
        require(len(names) == 14 and len(set(names)) == 14,
                "Missing/duplicated physical ZIP member")
        require(sorted(names) == sorted(members), "Injected/missing file")
        for name in names:
            require(not name.startswith("/") and
                    "\\" not in name and
                    ".." not in Path(name).parts,
                    "Path traversal / unsafe archive")
            meta = z.getinfo(name)
            require(meta.date_time == (1980, 1, 1, 0, 0, 0)
                    and meta.compress_type == zipfile.ZIP_STORED,
                    "ZIP timestamp/compression not reproducible")
            require((meta.external_attr >> 16) == 0o100644,
                    "Wrong archived POSIX file permissions")
            require(z.read(name) == members[name],
                    "Source byte / checksum drift: " + name)
        receipt = json.loads(z.read(RECEIPT).decode("utf-8"))
        require(receipt["original_source_sha"] == sha, "Forged source SHA")
        require(receipt["native_unity"] == "NOT_RUN" and
                receipt["codex_ack"] == "PENDING" and
                receipt["independent_G0"] == "PENDING",
                "Forged native test / independent acceptance / Codex ACK")
        require(receipt["integrated"] is False,
                "False game integration receipt")
        entries = receipt["source_members"]
        require(len(entries) == 13, "Receipt has wrong count")
        for row in entries:
            blob = z.read(row["path"])
            require(row["bytes"] == len(blob) and
                    row["sha256"] == sha256(blob),
                    "Member/receipt content digest drift")
    return sha256(path.read_bytes())

def negatives(members, sha):
    cases = []
    key = GAME + "GhostCore.cs"
    def check_case(label, mutation):
        modified = dict(members)
        mutation(modified)
        with tempfile.TemporaryDirectory() as tmp:
            trial = Path(tmp) / "tampered.zip"
            write_zip(trial, modified)
            try:
                verify_zip(trial, members, sha)
            except (ValueError, KeyError, zipfile.BadZipFile):
                cases.append(label)
            else:
                raise AssertionError("Bad archive accepted: " + label)
    def false_ack(d):
        data = json.loads(d[RECEIPT])
        data["codex_ack"] = "ACCEPTED"
        d[RECEIPT] = (json.dumps(data, sort_keys=True,
                        separators=(",", ":")) + "\n").encode()
    def false_unity(d):
        data = json.loads(d[RECEIPT])
        data["native_unity"] = "PASS"
        d[RECEIPT] = (json.dumps(data, sort_keys=True,
                        separators=(",", ":")) + "\n").encode()
    check_case("original_source_tamper", lambda d: d.__setitem__(
        key, d[key] + b"\n// malicious replacement\n"))
    check_case("original_meta_GUID_drift", lambda d: d.__setitem__(
        GAME + "GhostCore.cs.meta", d[GAME + "GhostCore.cs.meta"].replace(
            GUIDS["GhostCore.cs.meta"].encode(), b"0" * 32)))
    check_case("forged_Codex_ACK", false_ack)
    check_case("forged_Unity_PASS", false_unity)
    check_case("injected_Codex_game_path", lambda d: d.__setitem__(
        "../Assets/R1/R1RunController.cs", b"// unauthorized patch\n"))
    check_case("missing_real_visual_source", lambda d: d.pop(
        GAME + "GhostVisualProxy.cs"))
    return cases

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("mode", choices=("pack", "verify", "negatives"))
    ap.add_argument("--sha", required=True)
    ap.add_argument("--zip", required=True)
    ap.add_argument("--report", required=True)
    args = ap.parse_args()
    expected = expected_members(args.sha)
    target = Path(args.zip)
    if args.mode == "pack":
        target.parent.mkdir(parents=True, exist_ok=True)
        write_zip(target, expected)
    if args.mode in ("pack", "verify"):
        digest = verify_zip(target, expected, args.sha)
        outcome = {"pass": True, "mode": args.mode, "sha": args.sha,
                   "inner_zip_sha256": digest, "members": 14,
                   "importable_csharp": 4, "unity_meta": 5,
                   "native_unity": "NOT_RUN", "codex_ack": "PENDING"}
    else:
        rejected = negatives(expected, args.sha)
        require(len(rejected) == 6, "Not all 6 mutations rejected")
        outcome = {"pass": True, "mode": args.mode, "sha": args.sha,
                   "rejected_cases": rejected, "native_unity": "NOT_RUN"}
    Path(args.report).parent.mkdir(parents=True, exist_ok=True)
    Path(args.report).write_text(json.dumps(outcome, indent=2,
                                             sort_keys=True) + "\n")
    print("B01-10 GHOST PHYSICAL_HANDOFF " + json.dumps(outcome, sort_keys=True))

if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, KeyError, AssertionError,
            zipfile.BadZipFile) as err:
        print("B01-10 GHOST HANDOFF FAIL: " + str(err), file=sys.stderr)
        sys.exit(2)
