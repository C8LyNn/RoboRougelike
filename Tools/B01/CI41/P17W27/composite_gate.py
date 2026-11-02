#!/usr/bin/env python3
"""B01-41 W27: exact immutable multi-PR source intake; never installs into Unity."""
from __future__ import annotations
import hashlib
import io
import json
import re
import subprocess
import sys
import zipfile
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[4]
OWNER = "B01-41"
PACKAGES = {
    "strict_save_787": {
        "sha": "f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
        "base": "3aaaec4027ee246fbfb6f209179f0e9507363473",
        "root": "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/",
        "workflow": ".github/workflows/b01-62-w25-773-strict-case-path.yml",
        "count": 8,
        "manifest": "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/manifest.json",
    },
    "whole_game_797": {
        "sha": "2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
        "base": "ba3201644b252241429a6f493238c49d822adfae",
        "root": "Tools/B01/B01-62/P17W25WholeGameEconomyV7/",
        "workflow": ".github/workflows/b01-62-w25-whole-game-econ-v7.yml",
        "count": 8,
        "manifest": "Tools/B01/B01-62/P17W25WholeGameEconomyV7/manifest.json",
    },
    "physical_792": {
        "sha": "16cb2a7b5158605b15191b6074f9e392e0d1ebd3",
        "base": "ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1",
        "root": "Tools/B01/B01-23/P17W25PhysicalRollbackR2/",
        "workflow": ".github/workflows/b01-23-p17w25-physical-rollback-r2.yml",
        "count": 10,
        "manifest": "Tools/B01/B01-23/P17W25PhysicalRollbackR2/manifest.json",
        "extra": {
            "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs",
            "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs",
        },
    },
    "economy_780": {
        "sha": "b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
        "base": "cc5d65f66ad7a764af6515c566e489b9fc3be82e",
        "root": "Tools/B01/ECONAPPROVED007/",
        "workflow": ".github/workflows/b01-22-p17w25-econ-section-v7.yml",
        "count": 16,
        "manifest": "Tools/B01/ECONAPPROVED007/manifest.json",
        "extra_prefix": "Assets/B01Packages/ECONAPPROVED007",
    },
    "cold_fixture_750": {
        "sha": "60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
        "base": "cc7f73541517a18f4479bb155534bd4832d9985e",
        "root": "Tools/B01/B01-23/P17W23R5ColdRestart/",
        "workflow": ".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml",
        "count": 4,
        "extra": {"Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs"},
    },
}
FROZEN = {
    "canonical_api_729": ("b8ca29c7707cd6b4338d0b9772d74dee676d49d2", "Assets/B0123CanonicalSaveApi001"),
    "world_codec_556": ("33094f154c7b91119b36d58aab285e6e83c5de57", "Assets/B0123DetachedSaveContract001"),
    "ledger_510": ("d94400d95d13db7f27e1533df05c9292d5380431", "Assets/B0123SecureReattachV2"),
    "debris_W06_original_510_lineage": ("d94400d95d13db7f27e1533df05c9292d5380431", "Assets/B0123DebrisRepulse001"),
}
RUNTIME_PINS = {
    "strict_save_787": {
        "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSingleAuthorityStoreV3R3.cs": "bc34c2df4375ac7abdd638e5db7f7a238e8fa2fd",
        "Tools/B01/B01-62/P17W25StrictCanonicalPathR3/CanonicalSlotSessionV3R3.cs": "0fa335a67346ae600df163ec69c90e7550945ba1",
    },
    "whole_game_797": {
        "Tools/B01/B01-62/P17W25WholeGameEconomyV7/WholeGameEconomySlotV2.cs": "38f56d487d0a7ed44766beb0d46d4d294513694a",
        "Tools/B01/B01-62/P17W25WholeGameEconomyV7/CanonicalWholeGameSessionV2.cs": "06a515a992d2ead8f59408a1544f1d6f34863659",
    },
    "physical_792": {
        "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs": "7623f698b6e644b8d8247a011b453bf40adc2227",
        "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs": "358ef67f8f0f795d94a646e8e11c27158e2c8f0d",
    },
    "economy_780": {
        "Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs": "53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773",
        "Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs": "d8b71de91db36295b6220e9c4baad71eb54455f6",
    },
    "cold_fixture_750": {
        "Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs": "2508a04f57b04447112d2777ce4e3cd7478ed40e",
    },
    "canonical_api_729": {
        "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs": "a9331dd620336d8c42165d134dc8a5e05ed51873",
        "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs": "21733278b6ab0669fccb6d085e0b6cf242792de2",
    },
    "world_codec_556": {
        "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs": "b52e511c370f5fca56c74029050844dc8cbb7f9a",
    },
    "ledger_510": {
        "Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs": "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
    },
}
GUID_PATTERN = re.compile(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$")


def git(*args: str) -> bytes:
    return subprocess.check_output(["git", *args], cwd=ROOT, stderr=subprocess.PIPE)


def git_text(*args: str) -> str:
    return git(*args).decode("utf-8").strip()


def safe_path(path: str) -> str:
    if not isinstance(path, str) or not path or "\\" in path or path.startswith("/") or "\x00" in path:
        raise ValueError("UNSAFE_PATH")
    p = PurePosixPath(path)
    if any(x in (".", "..", "") for x in path.split("/")) or p.as_posix() != path or ":" in path:
        raise ValueError("UNSAFE_PATH")
    return path


def blob(ref: str, path: str) -> bytes:
    safe_path(path)
    return git("show", f"{ref}:{path}")


def oid(ref: str, path: str) -> str:
    safe_path(path)
    return git_text("rev-parse", f"{ref}:{path}")


def tree(ref: str, prefix: str) -> list[str]:
    safe_path(prefix)
    return sorted(git_text("ls-tree", "-r", "--name-only", ref, "--", prefix).splitlines())


def exact_delta(name: str, spec: dict) -> list[str]:
    sha, base = spec["sha"], spec["base"]
    git("cat-file", "-e", sha + "^{commit}")
    git("cat-file", "-e", base + "^{commit}")
    changed = sorted(git_text("diff", "--name-only", base, sha).splitlines())
    allowed = set()
    for p in changed:
        safe_path(p)
        if p == spec["workflow"] or p.startswith(spec["root"]) or p in spec.get("extra", set()) or (
            spec.get("extra_prefix") and
            (p == spec["extra_prefix"] + ".meta" or p.startswith(spec["extra_prefix"] + "/"))
        ):
            allowed.add(p)
    if len(changed) != spec["count"] or allowed != set(changed):
        raise AssertionError(f"{name}: UNEXPECTED_SOURCE_SCOPE {changed}")
    for p, expected in RUNTIME_PINS.get(name, {}).items():
        if oid(sha, p) != expected:
            raise AssertionError(f"{name}: BLOB_DRIFT {p}")
    return changed


def check_contracts() -> None:
    v7 = json.loads(blob(PACKAGES["whole_game_797"]["sha"], PACKAGES["whole_game_797"]["manifest"]))
    assert v7["exact_selected_strict_787"] == PACKAGES["strict_save_787"]["sha"]
    assert v7["exact_producer_780"] == PACKAGES["economy_780"]["sha"]
    assert v7["producer_original_blobs"] == {
        "policy": RUNTIME_PINS["economy_780"]["Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs"],
        "binary_section": RUNTIME_PINS["economy_780"]["Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs"],
    }
    assert v7["one_full_file"] == "save.envelope" and v7["auto_migration"] is False
    phys = json.loads(blob(PACKAGES["physical_792"]["sha"], PACKAGES["physical_792"]["manifest"]))
    assert phys["parent_772"] == PACKAGES["physical_792"]["base"]
    assert phys["native_unity"] == "NOT_RUN"
    econ = json.loads(blob(PACKAGES["economy_780"]["sha"], PACKAGES["economy_780"]["manifest"]))
    assert econ["pinned_blobs"][next(iter(RUNTIME_PINS["economy_780"]))] == next(iter(RUNTIME_PINS["economy_780"].values()))
    assert econ["native_unity"] == "NOT_RUN"
    strict_source = blob(PACKAGES["strict_save_787"]["sha"], next(iter(RUNTIME_PINS["strict_save_787"])))
    if b"StringComparison.OrdinalIgnoreCase" in strict_source or b"StringComparison.Ordinal" not in strict_source:
        raise AssertionError("STRICT_SAVE_CASE_ALIAS_REGRESSION")
    for path in RUNTIME_PINS["economy_780"]:
        source = blob(PACKAGES["economy_780"]["sha"], path)
        if b"FileStream" in source or b"UnityEngine" in source:
            raise AssertionError("V7_MUST_NOT_BE_SECOND_DISK_WRITER")
    phys_source = blob(PACKAGES["physical_792"]["sha"], next(iter(RUNTIME_PINS["physical_792"])))
    if b"resumeLinear" not in phys_source or b"resumeAngular" not in phys_source:
        raise AssertionError("R2_LIVE_VELOCITY_MISSING")
    fixture = blob(PACKAGES["cold_fixture_750"]["sha"], next(iter(RUNTIME_PINS["cold_fixture_750"])))
    if b"savedSnapshot.Generation" not in fixture:
        raise AssertionError("G5_FIXTURE_MISSING")


def combine_files() -> tuple[dict[str, bytes], dict[str, dict], dict[str, str]]:
    """Actual Unity-source closure from selected package roots and immutable dependencies."""
    asset_roots = {
        "physical_792": (PACKAGES["physical_792"]["sha"], "Assets/B0123CanonicalRigPortW24"),
        "economy_780": (PACKAGES["economy_780"]["sha"], "Assets/B01Packages/ECONAPPROVED007"),
        "cold_fixture_750": (PACKAGES["cold_fixture_750"]["sha"], "Assets/B0123UnitySaveBridge001"),
        **FROZEN,
    }
    members: dict[str, bytes] = {}
    provenance: dict[str, dict] = {}
    for name, (ref, root) in asset_roots.items():
        files = tree(ref, root)
        # git ls-tree with a directory path does not automatically include the folder .meta.
        try:
            oid(ref, root + ".meta")
            files.append(root + ".meta")
        except subprocess.CalledProcessError:
            pass
        if not files:
            raise AssertionError("EMPTY_ASSET_ROOT " + name)
        for path in sorted(set(files)):
            safe_path(path)
            data = blob(ref, path)
            if path in members and members[path] != data:
                raise AssertionError("CROSS_VERSION_FILE_COLLISION " + path)
            members[path] = data
            provenance[path] = {"source": name, "commit": ref, "git_blob": oid(ref, path),
                                "sha256": hashlib.sha256(data).hexdigest(), "size": len(data)}
    for name in ("strict_save_787", "whole_game_797"):
        for path in RUNTIME_PINS[name]:
            ref = PACKAGES[name]["sha"]
            data = blob(ref, path)
            if path in members and members[path] != data:
                raise AssertionError("CROSS_VERSION_WRITER_COLLISION " + path)
            members[path] = data
            provenance[path] = {"source": name, "commit": ref, "git_blob": oid(ref, path),
                                "sha256": hashlib.sha256(data).hexdigest(), "size": len(data)}
    guids: dict[str, str] = {}
    asmdefs: dict[str, str] = {}
    folded: dict[str, str] = {}
    for path, data in members.items():
        key = path.casefold()
        if key in folded and folded[key] != path:
            raise AssertionError("CASE_ALIAS_PATH_COLLISION " + path)
        folded[key] = path
        if path.endswith(".meta"):
            found = GUID_PATTERN.findall(data)
            if len(found) != 1:
                raise AssertionError("INVALID_UNITY_GUID " + path)
            guid = found[0].decode("ascii")
            if guid in guids and guids[guid] != path:
                raise AssertionError("DUPLICATE_UNITY_GUID " + path + " versus " + guids[guid])
            guids[guid] = path
        if path.endswith(".asmdef"):
            parsed = json.loads(data)
            name = parsed["name"]
            if name in asmdefs and asmdefs[name] != path:
                raise AssertionError("DUPLICATE_ASMDEF_NAME " + name)
            asmdefs[name] = path
    return members, provenance, {"guids": len(guids), "asmdefs": len(asmdefs)}


def check_zip(raw: bytes, expected: dict[str, bytes]) -> None:
    with zipfile.ZipFile(io.BytesIO(raw), "r") as z:
        names = z.namelist()
        if len(names) != len(set(names)) or set(names) != set(expected):
            raise ValueError("ZIP_DUPLICATE_OR_UNEXPECTED")
        if z.testzip() is not None:
            raise ValueError("ZIP_CRC_ERROR")
        for path in names:
            safe_path(path)
            if z.getinfo(path).is_dir() or z.read(path) != expected[path]:
                raise ValueError("ZIP_BYTES_MISMATCH")


def zip_catalog(members: dict[str, bytes]) -> bytes:
    output = io.BytesIO()
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_STORED) as z:
        for path in sorted(members):
            safe_path(path)
            item = zipfile.ZipInfo(path, date_time=(1980, 1, 1, 0, 0, 0))
            item.external_attr = 0o100644 << 16
            item.create_system = 3
            z.writestr(item, members[path])
    raw = output.getvalue()
    check_zip(raw, members)
    return raw


def main(outdir: Path) -> None:
    outdir.mkdir(parents=True, exist_ok=True)
    diffs = {name: exact_delta(name, spec) for name, spec in PACKAGES.items()}
    for name, (sha, _) in FROZEN.items():
        git("cat-file", "-e", sha + "^{commit}")
        for path, expected in RUNTIME_PINS[name].items():
            if oid(sha, path) != expected:
                raise AssertionError("FROZEN_BLOB_DRIFT " + path)
    check_contracts()
    files, provenance, counts = combine_files()
    # Reference artifact only: never run unzip into working game or auto-import two Save stores.
    raw = zip_catalog(files)
    (outdir / "composite-source-catalog-NOT-FOR-INSTALL.zip").write_bytes(raw)
    result = {
        "package": "B01-41-P17W27-COMPOSITE-RECEIPT-R1",
        "selection": {name: {"pr": int(name.rsplit("_", 1)[-1]), "sha": spec["sha"],
                             "base": spec["base"], "delta_paths": diffs[name]} for name, spec in PACKAGES.items()},
        "immutable_dependencies": {name: sha for name, (sha, _) in FROZEN.items()},
        "runtime_git_blob_pins": RUNTIME_PINS,
        "unity_source_catalog": provenance,
        "catalog_file_count": len(files),
        "catalog_unique_guids": counts["guids"],
        "catalog_unique_asmdefs": counts["asmdefs"],
        "catalog_sha256": hashlib.sha256(raw).hexdigest(),
        "catalog_bytes": len(raw),
        "policy": "REFERENCE_CATALOG_ONLY_NO_LIVE_STAGE_NO_SECOND_WRITER_NO_MERGE",
        "source_identity_and_selected_scope": "PASS",
        "cross_package_guid_and_asmdef_uniqueness": "PASS",
        "cross_package_gameplay_link_and_real_physx": "NOT_RUN",
        "native_unity_G1": "NOT_RUN",
        "composite_formal_G0": "HOLD_FOR_B01_40_AND_B01_61",
        "codeX_handoff": "NOT_OFFERED",
        "codeX_ack": "NONE",
        "G2": "HOLD",
        "rollback": "DELETE_ONLY_ISOLATED_QA_ARTIFACTS_NOT_USER_DATA",
    }
    destination = outdir / "composite-receipt.json"
    destination.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print("CI41_W27_REAL_GIT_PINNED_SELECTED_FIVE_SOURCE_DELTA_PASS")
    print("CI41_W27_ORIGINAL_UNITY_SOURCE_GUID_AND_ASMDEF_NO_COLLISION_PASS",
          "files=" + str(len(files)), "unique_guids=" + str(counts["guids"]),
          "unique_asmdefs=" + str(counts["asmdefs"]))
    print("CI41_W27_CATALOG_SHA256", result["catalog_sha256"], "bytes", len(raw))
    print("CI41_W27_G0_PARTIAL_QA_G1_NOT_RUN_G2_HOLD_Codex_NOT_OFFERED")


if __name__ == "__main__":
    try:
        main(Path(sys.argv[1]) if len(sys.argv) == 2 else ROOT / ".ci41-w27-evidence")
    except (AssertionError, ValueError, subprocess.CalledProcessError, KeyError, OSError) as exc:
        print("CI41_W27_FAIL_CLOSED", repr(exc), file=sys.stderr)
        sys.exit(1)
