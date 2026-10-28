#!/usr/bin/env python3
"""B01-13 read-only gate for three *existing* original Windows Player batch QA receipts.

This checks strict receipt structure and self-reported results; it NEVER proves that
Unity/Player actually executed. Independent raw log/command/artifact provenance
must be reviewed by B01-60/40. No source code or game files are changed.
"""
import argparse
import json
import re
import sys
from pathlib import Path

CASES = {
    "garage": {
        "flag": "-rrl-garage307-qa",
        "file": "garage307-player-qa.json",
        "fields": ("batchMode", "garage", "removedGun", "removedAllWheels", "blockedStart",
                   "protectedCore", "noPhantomShot", "restoredSameParts", "unchangedScrap",
                   "validDeployment", "enteredCombat"),
        "ints": {"removedWheels": 1},
        "strs": (),
    },
    "mov009": {
        "flag": "-rrl-suspension-ux-qa",
        "file": "suspension-ux-player-qa.json",
        "fields": ("batchMode", "missing", "garage", "off", "assist", "full", "height",
                   "actualForce", "paused", "hover", "damaged"),
        "ints": {},
        "strs": ("missingStatus", "readyStatus", "activeStatus", "hoverStatus", "damagedStatus"),
    },
    "editor": {
        "flag": "-rrl-map-editor-qa",
        "file": "map-editor-player-qa.json",
        "fields": ("batchMode", "menu", "garage", "mapPersistence", "testDrive", "actualWheelContact",
                   "driveAcrossRamp", "finish", "returnToEditor", "globalFloor",
                   "scaledObject", "ghost", "additionalObjects"),
        "ints": {},
        "strs": (),
    },
}
SHA40 = re.compile(r"^[a-f0-9]{40}$", re.I)
SHA64 = re.compile(r"^[a-f0-9]{64}$", re.I)


def path_in(root: Path, item: str) -> Path:
    if not isinstance(item, str) or not item or '\\' in item:
        raise ValueError("invalid evidence-relative path")
    p = Path(item)
    if p.is_absolute() or any(x in ("..", ".") for x in p.parts):
        raise ValueError("path escapes evidence root")
    resolved = (root / p).resolve()
    if not resolved.is_relative_to(root.resolve()):
        raise ValueError("symlink escapes evidence root")
    if not resolved.is_file():
        raise ValueError("missing evidence file: " + item)
    return resolved


def parse_json(path: Path) -> dict:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError("JSON root must be object")
    return value


def validate(root: Path, expected_source: str, expected_build: str):
    errors = []
    if not SHA40.fullmatch(expected_source) or not SHA40.fullmatch(expected_build):
        return ["expected source/build SHA must be full 40-hex hashes"]
    try:
        r = parse_json(path_in(root, "receipt.json"))
    except (ValueError, OSError, json.JSONDecodeError) as exc:
        return ["receipt.json: " + str(exc)]
    if r.get("source_sha", "").lower() != expected_source.lower():
        errors.append("source SHA mismatch")
    if r.get("build_sha", "").lower() != expected_build.lower():
        errors.append("player build SHA mismatch")
    if not isinstance(r.get("player_archive_sha256"), str) or not SHA64.fullmatch(r["player_archive_sha256"]):
        errors.append("missing complete player archive SHA256")
    if r.get("platform") != "Windows x64 Player":
        errors.append("platform not confirmed as Windows x64 Player")
    if r.get("unity_version") != "6000.3.14f1":
        errors.append("Unity version mismatch or unknown")
    if r.get("runtime_status") != "ACTUALLY_EXECUTED":
        errors.append("runtime_status is not ACTUALLY_EXECUTED")
    cases = r.get("cases")
    if not isinstance(cases, dict) or set(cases) != set(CASES):
        return errors + ["receipt must contain exactly garage/mov009/editor cases"]
    for name, cfg in CASES.items():
        info = cases[name]
        if not isinstance(info, dict):
            errors.append(name + ": not object")
            continue
        if type(info.get("exit_code")) is not int or info["exit_code"] != 0:
            errors.append(name + ": exit_code must be int 0 (false is NOT 0)")
        command = info.get("command")
        if not isinstance(command, str) or not all(x in command.split() for x in [cfg["flag"], "-batchmode"]):
            errors.append(name + ": command must identify original opt-in runner and -batchmode")
        try:
            js = parse_json(path_in(root, info.get("json_file")))
        except (ValueError, OSError, json.JSONDecodeError) as exc:
            errors.append(name + ": JSON file invalid: " + str(exc))
            continue
        if Path(info.get("json_file", "")).name != cfg["file"]:
            errors.append(name + ": unexpected JSON report basename")
        try:
            log = path_in(root, info.get("log_file"))
            if log.stat().st_size < 32:
                errors.append(name + ": Editor/Player log too short")
        except (ValueError, OSError) as exc:
            errors.append(name + ": log unavailable: " + str(exc))
        if js.get("status") != "PASS" or js.get("error") != "":
            errors.append(name + ": report status/error not clean")
        for field in cfg["fields"]:
            if js.get(field) is not True:
                errors.append(name + ": boolean " + field + " not true")
        for field, min_value in cfg["ints"].items():
            if type(js.get(field)) is not int or js[field] < min_value:
                errors.append(name + ": invalid integer " + field)
        for field in cfg["strs"]:
            if not isinstance(js.get(field), str) or not js[field].strip():
                errors.append(name + ": empty status field " + field)
        if name == "mov009":
            value = js.get("force")
            if type(value) not in (float, int) or not (0 < value < 1e9):
                errors.append(name + ": force not finite positive physical value")
        if name == "editor":
            value = js.get("maxZ")
            if type(value) not in (float, int) or not (2 < value < 1e6):
                errors.append(name + ": maxZ not a valid driven distance")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("evidence_dir", type=Path)
    parser.add_argument("--expected-source-sha", required=True)
    parser.add_argument("--expected-build-sha", required=True)
    args = parser.parse_args()
    errors = validate(args.evidence_dir, args.expected_source_sha, args.expected_build_sha)
    if errors:
        print("RECEIPT_STRUCTURAL_FAIL (NOT Unity FAIL)")
        for err in errors:
            print("-", err)
        return 1
    print("RECEIPT_STRUCTURE_PASS / NATIVE_EXECUTION_NOT_INDEPENDENTLY_VERIFIED")
    print("NEXT: B01-60/40 independently review provenance, log originals, build hash and host licence/receipt")
    return 0


if __name__ == "__main__":
    sys.exit(main())
