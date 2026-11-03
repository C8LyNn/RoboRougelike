#!/usr/bin/env python3
"""Compile/execute UNMODIFIED B01-31 W33 C# source methods with minimal .NET ancestry shims.
This is NOT Unity, NOT a real FBX import, not a turret and not G1.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
PACKAGE = Path(__file__).resolve().parent
PARENT = "b61bbe0643a6547ccb3087ada44474e8b33550fd"
EDITOR = "Assets/B01_31/W30R41V5Adapter/Editor/B0131W30CreateR41V5AdapterCandidate.cs"
EDITOR_SHA256 = "f3429796b0ea8c107cf60590b4a5c7721b98e5d5f02592d4aa6e9957320239a3"
OWN = {
    ".github/workflows/b01-31-p17w34-r41v5-semantic.yml": "A",
    "Tools/B01-31/P17/W34RigSemantic001/Program.cs": "A",
    "Tools/B01-31/P17/W34RigSemantic001/Harness.csproj": "A",
    "Tools/B01-31/P17/W34RigSemantic001/build_and_check.py": "A",
    "Tools/B01-31/P17/W34RigSemantic001/README.md": "A",
}


def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def extract_method(source: str, signature: str) -> str:
    index = source.find(signature)
    if index < 0 or source.find(signature, index + 1) >= 0:
        raise AssertionError("Missing or duplicated original method " + signature)
    start = source.find("{", index)
    if start < 0:
        raise AssertionError("Missing method body " + signature)
    depth = 0
    for end in range(start, len(source)):
        if source[end] == "{":
            depth += 1
        elif source[end] == "}":
            depth -= 1
            if depth == 0:
                return source[index:end + 1]
    raise AssertionError("Unbalanced original method " + signature)


def managed_run(project: Path, expect_success: bool, label: str) -> str:
    p = subprocess.run(
        ["dotnet", "run", "--project", str(project), "-c", "Release", "--no-launch-profile"],
        cwd=ROOT, text=True, capture_output=True, check=False,
        encoding="utf-8", errors="replace", timeout=90)
    merged = p.stdout + "\n" + p.stderr
    if expect_success:
        assert p.returncode == 0, label + " unexpectedly RED: " + merged[-4000:]
        assert "ORIGINAL_B0131_W33_CSHARP_SOURCE_SEMANTIC_PASS=14" in merged, (
            label + " missing 14 independent verified cases " + merged[-2000:])
        assert "UNITY_EDITOR_NOT_RUN" in merged
    else:
        assert p.returncode != 0 and "SOURCE_SEMANTIC_FAIL" in merged, (
            label + " unsafe mutant was NOT rejected " + merged[-3000:])
    return merged


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--receipt", default="Artifacts/B01-31/W34/receipt.json")
    args = parser.parse_args()
    assert git("rev-parse", "--is-inside-work-tree") == "true"
    assert subprocess.run(["git", "merge-base", "--is-ancestor", PARENT, "HEAD"],
                          cwd=ROOT, check=False).returncode == 0, "Wrong W34 owner parent"
    modified = {}
    for line in git("diff", "--name-status", PARENT + "..HEAD").splitlines():
        status, file_path = line.split("\t", 1)
        modified[file_path] = status
    assert modified == OWN, "Foreign/modified code paths in W34 " + repr(modified)
    source_bytes = (ROOT / EDITOR).read_bytes()
    assert hashlib.sha256(source_bytes).hexdigest() == EDITOR_SHA256, (
        "Original exact W33 producer source moved; cannot inherit old QA")
    original = source_bytes.decode("utf-8")
    # These are the EXACT existing methods under test, copied verbatim from
    # the file on the selected PR HEAD. NO rewritten product logic in the shim.
    unique = extract_method(original, "private static Transform FindUnique(")
    rig = extract_method(original, "public static bool TryValidateSourceRigHierarchy(")
    template = (PACKAGE / "Program.cs").read_text(encoding="utf-8")
    assert template.count("// __EXTRACT_ORIGINAL_FIND_UNIQUE__") == 1
    assert template.count("// __EXTRACT_ORIGINAL_RIG_GATE__") == 1
    assert template.count('Test("real-original-method_') == 14
    assembled = (template.replace("// __EXTRACT_ORIGINAL_FIND_UNIQUE__", unique)
                 .replace("// __EXTRACT_ORIGINAL_RIG_GATE__", rig))
    output = ROOT / "build/B0131W34OriginalSourceSemantic"
    output.mkdir(parents=True, exist_ok=True)
    project = output / "Harness.csproj"
    shutil.copyfile(PACKAGE / "Harness.csproj", project)
    generated = output / "Program.cs"
    generated.write_text(assembled, encoding="utf-8", newline="\n")
    managed_run(project, True, "ORIGINAL_W33_EXACT_CSHARP_SOURCE")
    mutants = {
        "remove_static_renderer_safety": (
            "staticRenderers == 0 || yawRenderers == 0 || pitchRenderers == 0",
            "false || yawRenderers == 0 || pitchRenderers == 0"),
        "bypass_pitch_yaw_ancestry": (
            "!sourcePitch.IsChildOf(sourceYaw)", "false"),
        "ignore_both_muzzle_ancestry_guards": (
            "!left.IsChildOf(sourcePitch) || !right.IsChildOf(sourcePitch)",
            "false"),
    }
    for label, (needle, replacement) in mutants.items():
        assert assembled.count(needle) == 1, label + " original gate absent/duplicated"
        generated.write_text(assembled.replace(needle, replacement, 1),
                             encoding="utf-8", newline="\n")
        managed_run(project, False, label)
    generated.write_text(assembled, encoding="utf-8", newline="\n")
    managed_run(project, True, "ORIGINAL_W33_RESTORE_AFTER_NEGATIVES")
    receipt = {
        "result": "ORIGINAL_W33_ACTUAL_METHOD_CSHARP_COMPILED_14_SEMANTIC_CONTROLS",
        "source_sha": git("rev-parse", "HEAD"),
        "original_editor_sha256": EDITOR_SHA256,
        "original_methods": ["FindUnique", "TryValidateSourceRigHierarchy"],
        "non_unity_original_method_cases_each_positive_run": 14,
        "negative_original_method_mutants_rejected": len(mutants),
        "cross_os_runners": "independent_on_each_GitHub_OS",
        "scope": "FIVE_ADD_ONLY_TEST_FILES_NO_SOURCE_CHANGES",
        "native_unity": "NOT_RUN",
        "original_fbx": "EXTERNAL_NOT_AVAILABLE",
        "windows_player": "NOT_RUN",
        "ready_for_game_integration": False,
    }
    destination = ROOT / args.receipt
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n",
                           encoding="utf-8", newline="\n")
    print(json.dumps(receipt, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
