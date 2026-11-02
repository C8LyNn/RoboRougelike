#!/usr/bin/env python3
"""B01-41 W28: compile REAL immutable producer #797 bytes against netstandard2.1.
This is a compatibility negative probe, NOT licensed Unity or game Save QA.
"""
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
PACKAGE = "B01-41-P17W28-SAVE-UNITY-COMPILE-GATE-001"
EXPECTED_PRODUCER = "2b7983f0f9e09dd1d56906b379a3f7af7b02f21a"
CODEx_W27 = "e03d36f2b86f8c73a067e574b67237a8921f622a"
CODEx_W26 = "e7a85944465d908f175a6f5e2ae07f4aedcc593f"

# Source, not branch names: exact original Git commits plus original Git blob IDs.
SOURCES = [
    ("slot", EXPECTED_PRODUCER, "Tools/B01/B01-62/P17W25WholeGameEconomyV7/WholeGameEconomySlotV2.cs", "38f56d487d0a7ed44766beb0d46d4d294513694a"),
    ("session", EXPECTED_PRODUCER, "Tools/B01/B01-62/P17W25WholeGameEconomyV7/CanonicalWholeGameSessionV2.cs", "06a515a992d2ead8f59408a1544f1d6f34863659"),
    ("policy", "b4df10a7ad7e6c758f8331013f6b216b3a3689a3", "Assets/B01Packages/ECONAPPROVED007/Runtime/ApprovedEconomyPolicyV7.cs", "53ebce6c07388dc9c8a8ed79b6de4d3ccbee4773"),
    ("economy_section", "b4df10a7ad7e6c758f8331013f6b216b3a3689a3", "Assets/B01Packages/ECONAPPROVED007/Runtime/CanonicalEconomySectionV7.cs", "d8b71de91db36295b6220e9c4baad71eb54455f6"),
    ("gateway", "b8ca29c7707cd6b4338d0b9772d74dee676d49d2", "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs", "21733278b6ab0669fccb6d085e0b6cf242792de2"),
    ("lease", "b8ca29c7707cd6b4338d0b9772d74dee676d49d2", "Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldSlotLease.cs", "a9331dd620336d8c42165d134dc8a5e05ed51873"),
    ("world", "33094f154c7b91119b36d58aab285e6e83c5de57", "Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs", "b52e511c370f5fca56c74029050844dc8cbb7f9a"),
    ("rig", "d94400d95d13db7f27e1533df05c9292d5380431", "Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs", "b680ed0ec969ac23203fa55eab7b90f8daa86e43"),
    ("w06", "d94400d95d13db7f27e1533df05c9292d5380431", "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs", "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
]
W27_FILES = {
    "beam_binding": ("Assets/WeaponIntegration/RRLBeamBinding.cs", "fd91d20979cafbb706121e2e4943d6d317b0dcd9"),
    "beam_unity_test": ("Assets/Tests/Editor/W27BeamClearanceTests.cs", "39147e59ef43d50514e74875642a9af7833855a6"),
    "special_unity_test": ("Assets/Tests/Editor/W27SpecialWeaponClearanceTests.cs", "5041bbb68bbf10d859bec25dc52ff4356d39e0c4"),
    "player_harness": ("Assets/R1/W27WeaponPlayerQA.cs", "1f9023317f49488eb5ebbad93fb079d6b0d3b024"),
    "vehicle_assembly": ("Assets/R3/R3VehicleAssembly.cs", "ed97dd1b0ee3b77c839615497b249dcaee479bb8"),
}
# All three are confirmed by source, but NOT sufficient for source compile on their own.
UNSUPPORTED = {
    "hashdata": re.compile(r"\bSHA256\s*\.\s*HashData\s*\("),
    "tohex": re.compile(r"\bConvert\s*\.\s*ToHexString\s*\("),
    "move_overwrite": re.compile(r"\bFile\s*\.\s*Move\s*\(\s*staging\s*,\s*authoritative\s*,\s*true\s*\)"),
}
EXPECTED_COVERAGE = {
    "beam_unity_test": "BeamDoesNotHeatWhileOwnArmorBlocksItsPathAndResumesWhenClear",
    "special_unity_test": "ShotgunRejectsSpreadPelletCrossingOwnArmor",
    "player_harness": "-w27-weapon-player-qa",
}

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, stderr=subprocess.PIPE)

def git_oid(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\x00" + data).hexdigest()

def verified_original(ref, path, oid):
    if not re.fullmatch("[0-9a-f]{40}", ref) or not re.fullmatch("[0-9a-f]{40}", oid):
        raise ValueError("NON_IMMUTABLE_SOURCE_REFERENCE")
    if path.startswith("/") or ".." in Path(path).parts or "\\" in path or "\x00" in path:
        raise ValueError("UNSAFE_GIT_PATH")
    data = git("show", ref + ":" + path)
    if git_oid(data) != oid:
        raise AssertionError("PINNED_GIT_BLOB_MISMATCH " + path)
    return data

def unsupported_hits(sources):
    joined = "\n".join(sources)
    return {k: len(p.findall(joined)) for k, p in UNSUPPORTED.items()}

def coverage_contract(contents):
    for key, term in EXPECTED_COVERAGE.items():
        if term not in contents[key]:
            raise AssertionError("W27_SOURCE_TEST_IDENTITY_DRIFT " + key)
    # This harness is conditional; it is NOT evidence it ran in Windows Player.
    if "#if B01_UNIFIED_RC2_ACCEPTANCE" not in contents["player_harness"]:
        raise AssertionError("W27_HARNESS_COMPILATION_GATE_CHANGED")
    return {
        "basic_automatic_heavy": "SOURCE_PRESENT_W26_NATIVE_EVIDENCE_REPORTED_SEPARATELY",
        "shotgun_railgun_rocket": "SOURCE_PRESENT_W27_TARGETED_NATIVE_REPORTED_SEPARATELY",
        "shotgun_spread": "SOURCE_TEST_AUTHORED_EXACT_GIT_NOT_NATIVE_AT_THIS_GATE",
        "beam": "SOURCE_TEST_AUTHORED_EXACT_GIT_NOT_NATIVE_AT_THIS_GATE",
        "rotary_saw": "FULL_SELF_COLLISION_AND_PLAYER_QA_NOT_CONFIRMED",
        "emp": "PHYSICAL_AREA_OWNER_CLEARANCE_NOT_CONFIRMED",
        "moving_colliders_and_multi_group": "NOT_CONFIRMED",
        "garage_3d_zones": "NOT_CONFIRMED",
        "player_zip": "NOT_RUN_AT_THIS_GATE",
        "human_playtest": "NOT_RUN",
    }

def create_project(output, contents):
    output.mkdir(parents=True, exist_ok=True)
    src = output / "exact-original-sources"
    src.mkdir(exist_ok=True)
    staged = []
    for idx, (name, ref, path, oid) in enumerate(SOURCES):
        target = src / (f"{idx:02d}_{name}.cs")
        target.write_bytes(contents[name])
        staged.append(target)
    xml = (
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
        '<TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion>'
        '<Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'
        '<ImplicitUsings>disable</ImplicitUsings><GenerateAssemblyInfo>false</GenerateAssemblyInfo>'
        '<TreatWarningsAsErrors>false</TreatWarningsAsErrors>'
        '</PropertyGroup><ItemGroup>'
        + "".join(f'<Compile Include="exact-original-sources/{p.name}" />' for p in staged)
        + '</ItemGroup></Project>'
    )
    (output / "W28ExactOriginalUnityApiSurface.csproj").write_text(xml, encoding="utf-8")
    return output / "W28ExactOriginalUnityApiSurface.csproj"

def main(output):
    output.mkdir(parents=True, exist_ok=True)
    contents, provenance = {}, {}
    for name, ref, path, oid in SOURCES:
        b = verified_original(ref, path, oid)
        contents[name] = b
        provenance[name] = {"commit": ref, "path": path, "git_blob": oid, "sha256": hashlib.sha256(b).hexdigest()}
    w27 = {}
    for name, (path, oid) in W27_FILES.items():
        b = verified_original(CODEx_W27, path, oid)
        w27[name] = b.decode("utf-8-sig")
        provenance[name] = {"commit": CODEx_W27, "path": path, "git_blob": oid, "sha256": hashlib.sha256(b).hexdigest()}
    game_delta = git("diff", "--name-only", CODEx_W26, CODEx_W27).decode().strip().splitlines()
    if set(p for p, _ in W27_FILES.values()) - set(game_delta):
        raise AssertionError("W27_DELTA_NOT_817_DESCENDANT")
    coverage = coverage_contract(w27)
    hits = unsupported_hits([contents["slot"].decode("utf-8-sig"), contents["session"].decode("utf-8-sig")])
    if not all(hits.values()):
        raise AssertionError("OLD_797_EXPECTED_COMPATIBILITY_WITNESS_CHANGED_CHECK_PRODUCER_REVISION")
    project = create_project(output / "compiler-scratch", contents)
    run = subprocess.run(["dotnet", "build", str(project), "-c", "Release", "--nologo", "-v", "quiet"],
                         cwd=project.parent, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "real-original-netstandard21-compiler.log").write_text(run.stdout, encoding="utf-8")
    errors = sorted(set(re.findall(r"\berror (CS\d{4}|NETSDK\d{4})\b", run.stdout)))
    has_wrong_api = ("CS0117" in errors and "CS1501" in errors)
    # Exact legacy source is expected RED. A green compilation (or a different kind of failure) must NOT be misreported.
    outcome = "EXPECTED_LEGACY_UNITY_API_RED" if run.returncode != 0 and has_wrong_api else "UNEXPECTED_RESULT_REVIEW_REQUIRED"
    result = {
        "package": PACKAGE,
        "producer_797": EXPECTED_PRODUCER, "codex_w26_817": CODEx_W26,
        "codex_w27_824": CODEx_W27,
        "source_git_exact": "PASS", "source_files": provenance,
        "target": "netstandard2.1_CSharp9_isolated_original_source_COMPILER_NOT_UNITY_EDITOR",
        "original_api_hits": hits, "compiler_exit": run.returncode, "compiler_errors": errors,
        "compatibility": outcome, "w27_source_coverage": coverage,
        "unity_6000_3_14f1": "NOT_RUN", "windows_player": "NOT_RUN",
        "native_editmode_playmode": "NOT_RUN", "gameplay_integrated": "NO",
        "formal_G0": "HOLD_B01_40_61", "G2": "HOLD", "codeX_ack": "NONE",
        "next_owner": "B01-62_NEW_COMPATIBLE_SINGLE_WRITER_SOURCE_THEN_B01-40_61_INDEPENDENT_G0",
    }
    (output / "source-probe-receipt.json").write_text(json.dumps(result, indent=2, sort_keys=True) + "\n")
    print("CI41_W28_EXACT_ORIGINAL_PRODUCER_AND_W27_SHA_PASS files=" + str(len(provenance)))
    print("CI41_W28_REAL_CSHARP9_NETSTANDARD21_COMPILER " + outcome, "errors=" + ",".join(errors))
    print("CI41_W28_GAMEPLAY_COVERAGE_NATIVE_G1_NOT_RUN_PLAYER_NOT_RUN_G2_HOLD")
    if outcome != "EXPECTED_LEGACY_UNITY_API_RED":
        raise AssertionError("DO_NOT_TREAT_UNEXPECTED_COMPILER_RESULT_AS_COMPATIBILITY_PASS")

if __name__ == "__main__":
    try:
        main(Path(sys.argv[1]).resolve() if len(sys.argv) == 2 else (ROOT / ".b01-41-w28-evidence"))
    except (ValueError, AssertionError, OSError, subprocess.CalledProcessError) as e:
        print("CI41_W28_FAIL_CLOSED", repr(e), file=sys.stderr)
        sys.exit(2)
