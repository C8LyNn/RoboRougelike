#!/usr/bin/env python3
"""Fail-closed BUILD R1.4 profile for the EXISTING B01-60 licensed Unity runner.

This is a read-only source/fixture/checkout gate. It NEVER runs Unity, installs
assets, edits a worktree, approves G2 or claims independent native evidence.
"""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess
import zipfile

SOURCE_SHA = "95707c68c8071a949b5d9fec7ff57926e92f8198"
SOURCE_ZIP_SHA256 = "62321794e8f91f1660c32597632b40b9bccdbab1becd3c7e750632f06f8714aa"
RECEIVER_BLOB = "7266569bb447a73a523521fa68d1bf615d06ceeb"
RECEIVER_PATH = "Assets/B01Construction/BUILD001/Tests/EditMode/BuildRC2SafetyTests.cs"
MANIFEST_NAME = "PACKAGE_MANIFEST_R13.json"  # Historical file name, JSON schema is R14.
PREFIX = "Assets/B01Construction/"
BUILD_PREFIX = "Assets/B01Construction/BUILD001/"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def gitblob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()


def path_safe(name):
    p = PurePosixPath(name)
    return (name == str(p) and not p.is_absolute() and ".." not in p.parts
            and "\\" not in name and not name.startswith("./") and ":" not in name)


def verify_archive(archive_path):
    data = Path(archive_path).read_bytes()
    if digest(data) != SOURCE_ZIP_SHA256:
        raise ValueError("UNEXPECTED_BUILD_R14_ZIP_SHA256")
    import io
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        if z.testzip() is not None:
            raise ValueError("BROKEN_ARCHIVE_CRC")
        names = z.namelist()
        if len(names) != len(set(names)) or any(not path_safe(n) for n in names):
            raise ValueError("DUPLICATE_OR_UNSAFE_ARCHIVE_PATH")
        if MANIFEST_NAME not in names:
            raise ValueError("MISSING_R14_MANIFEST")
        m = json.loads(z.read(MANIFEST_NAME))
        if (m.get("schema") != "B01-PACKAGE-FULL-SHA256-R14"
                or m.get("source_head_sha") != SOURCE_SHA
                or m.get("package_id") != "B01-BUILD001-R1.4"
                or m.get("G2") != "HOLD"):
            raise ValueError("WRONG_MANIFEST_REVISION_OR_SOURCE")
        expected = {r["path"]: r for r in m["files"]}
        if set(names) != set(expected) | {MANIFEST_NAME}:
            raise ValueError("ARCHIVE_MANIFEST_PATH_MISMATCH")
        assets = {}
        for name, spec in expected.items():
            if not path_safe(name):
                raise ValueError("UNSAFE_MANIFEST_PATH")
            payload = z.read(name)
            if len(payload) != spec["bytes"] or digest(payload) != spec["sha256"]:
                raise ValueError("SOURCE_TAMPERING:" + name)
            if name.startswith("Assets/"):
                assets[name] = payload
        if len(assets) < 20 or not all(n.startswith(PREFIX) or n == "Assets/B01Construction.meta" for n in assets):
            raise ValueError("INVALID_BUILD_PACKAGE_SCOPE")
        # The exact Unity asset + metadata must be complete. No runtime file replacement.
        for name in assets:
            if not name.endswith(".meta") and name + ".meta" not in assets:
                raise ValueError("MISSING_META_PAIR:" + name)
        return assets


def assert_disposable(project, target_sha):
    project = Path(project).resolve(strict=True)
    if not (project / ".b01-60-disposable-qa").is_file():
        raise ValueError("NOT_B01_60_DISPOSABLE_QA")
    version = project / "ProjectSettings/ProjectVersion.txt"
    if not version.is_file() or "m_EditorVersion: 6000.3.14f1" not in version.read_text():
        raise ValueError("WRONG_UNITY_VERSION")
    if (project / "Temp/UnityLockfile").exists():
        raise ValueError("UNITY_EDITOR_LOCKED")
    head = subprocess.check_output(["git", "-C", str(project), "rev-parse", "HEAD"],
                                   stderr=subprocess.DEVNULL).decode().strip()
    if head != target_sha:
        raise ValueError("TARGET_SHA_MISMATCH")
    if target_sha == SOURCE_SHA:
        # Legitimate target could be the producer commit itself, but still require
        # explicit user-provided target pin rather than guessing from HEAD.
        pass
    return project


def create_plan(project, archive, target_sha):
    checked = assert_disposable(project, target_sha)
    assets = verify_archive(archive)
    receiver = checked / RECEIVER_PATH
    if not receiver.is_file() or gitblob(receiver.read_bytes()) != RECEIVER_BLOB:
        raise ValueError("MISSING_OR_CHANGED_ORIGINAL_RC2_RECEIVER_FIXTURE")
    hashes = {}
    for name, payload in sorted(assets.items()):
        installed = checked / name
        if not installed.is_file() or installed.read_bytes() != payload:
            raise ValueError("SOURCE_INSTALL_MISMATCH:" + name)
        hashes[name] = digest(payload)
    hashes[RECEIVER_PATH] = digest(receiver.read_bytes())
    return {
        "schema": "b01-60-native-plan-v1",
        "profile": "SCOPED_DIAGNOSTIC",
        "source_sha": SOURCE_SHA,
        "target_sha": target_sha,
        "source_files": hashes,
        "required_stages": ["Compile", "EditMode", "PlayMode"],
        "test_filter": "Build",
        "required_nunit": {
            "EditMode": [
                "RC2_NestedAcceptedAreaEqualsActualPhysicalSquareMetres",
                "AcceptedNestedPatchReportsItsTrueGeometricArea",
            ],
            "PlayMode": [
                "W9_04_ActualPhysXPenetrationRejectsLargePhysicalPart",
                "W9_01_DecorativeChildRayCannotBecomeStructuralSocket",
            ]
        },
        "purpose": "W14 BUILD R1.4 native acceptance, original RC2 receiver fixture intact",
        "release": "G2_HOLD",
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--project", required=True)
    ap.add_argument("--source-zip", required=True)
    ap.add_argument("--target-sha", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()
    if len(args.target_sha) != 40 or any(c not in "0123456789abcdef" for c in args.target_sha):
        raise SystemExit("TARGET_SHA_MUST_BE_EXPLICIT_40_HEX")
    dest = Path(args.out).resolve()
    proj = Path(args.project).resolve()
    if dest.is_relative_to(proj):
        raise SystemExit("PLAN_OUTPUT_MUST_BE_OUTSIDE_DISPOSABLE_CHECKOUT")
    if dest.exists():
        raise SystemExit("REFUSE_TO_OVERWRITE_EXISTING_PLAN")
    plan = create_plan(proj, args.source_zip, args.target_sha)
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(json.dumps(plan, sort_keys=True, indent=2)+"\n", encoding="utf8")
    print(json.dumps({
        "status": "PLAN_READY_NATIVE_NOT_RUN",
        "source_sha": SOURCE_SHA, "target_sha": args.target_sha,
        "pinned_asset_files": len(plan["source_files"]),
        "original_receiver_git_blob": RECEIVER_BLOB,
        "g2": "HOLD"
    }, sort_keys=True))


if __name__ == "__main__":
    main()
