#!/usr/bin/env python3
"""B01-31 W33 exact-parent ORIGINAL Editor source QA; NOT native Unity."""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = "b611776842bb336722144b09a062d22abe823549"
EDITOR = "Assets/B01_31/W30R41V5Adapter/Editor/B0131W30CreateR41V5AdapterCandidate.cs"
TEST = "Assets/B01_31/W30R41V5Adapter/Tests/EditMode/R41V5W33FixedRigEditModeTests.cs"
META = TEST + ".meta"
OWN = {
    EDITOR: "M",
    TEST: "A",
    META: "A",
    "Docs/W33/B01-31-R41V5-fixed-rig.md": "A",
    "Tools/B01-31/P17/W33RigFixed001/check_source.py": "A",
    ".github/workflows/b01-31-p17w33-r41v5-fixed-rig.yml": "A",
}
BASE_SHA256 = "34e1f8d54cf340cb839659764493b3e73a7dafda438876fab7eb3c09c3a8ce8a"
MODEL_SHA256 = "fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533"
GUID = "d3707ce5fd734f81b8587e3c86f0e298"


def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def check_editor(text: str) -> list[str]:
    errors = []
    def require(ok: bool, key: str) -> None:
        if not ok:
            errors.append(key)
    section = text.split("public static GameObject CreateCandidatePreview(", 1)
    require(len(section) == 2, "original_preview_missing")
    if len(section) != 2:
        return errors
    preview = section[1].split("[MenuItem(", 1)[0]
    pre = preview.find("if (!TryValidateSourceVisualHierarchy(originalFbx, out rejection))")
    rig = preview.find("if (!TryValidateSourceRigHierarchy(originalFbx, out rejection))")
    clone = preview.find("copy = Object.Instantiate(originalFbx);")
    require(0 <= pre < rig < clone, "rig_gate_not_before_clone")
    require("public static bool TryValidateSourceRigHierarchy(" in text, "real_source_gate_missing")
    require("!sourcePitch.IsChildOf(sourceYaw)" in text, "pitch_ancestry_check_missing")
    require("!left.IsChildOf(sourcePitch) || !right.IsChildOf(sourcePitch)" in text,
            "source_muzzle_ancestry_check_missing")
    require("staticRenderers == 0 || yawRenderers == 0 || pitchRenderers == 0" in text,
            "three_renderer_branches_required")
    require("copy.transform.SetParent(root.transform, true);" in preview,
            "fixed_base_root_missing")
    require("copy.transform.SetParent(pitch, true)" not in preview,
            "WHOLE_ORIGINAL_FBX_STILL_UNDER_PITCH")
    require("visualYaw.SetParent(yaw, true);" in preview, "yaw_shell_not_reparented")
    require("visualPitch.SetParent(pitch, true);" in preview, "elevation_barrel_not_reparented")
    require("visualPitch.IsChildOf(visualYaw)" in preview,
            "bad_reparent_contract_absent")
    require("Vector3.Distance(copy.transform.position,fixedMountPosition)" in preview and
            "Quaternion.Angle(copy.transform.rotation,fixedMountRotation)" in preview,
            "static_root_pose_assertion_missing")
    require("Vector3.Distance(originalLeft.position,left.position)" in preview and
            "Vector3.Distance(originalRight.position,right.position)" in preview,
            "imported_real_muzzle_pose_check_missing")
    require("TryValidateSourceVisualHierarchy(originalFbx, out rejection)" in preview,
            "W31_preclone_safety_removed")
    require("KnownV5FbxSha256" in text and MODEL_SHA256 in text,
            "original_external_v5_sha_changed")
    require("R41_Turret_v5_ADAPTER_CANDIDATE_ONLY.prefab" in text,
            "old_generated_candidate_safety_changed")
    require("Object.DestroyImmediate(rb)" in text and
            "Object.DestroyImmediate(col)" in text,
            "clone_visual_physics_strip_removed")
    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--receipt", default="")
    a = parser.parse_args()
    assert git("rev-parse", "--is-inside-work-tree") == "true"
    assert subprocess.run(
        ["git", "merge-base", "--is-ancestor", BASE, "HEAD"],
        cwd=ROOT, check=False).returncode == 0, "W33 parent not in ancestry"
    actual = {}
    for line in git("diff", "--name-status", BASE + "..HEAD").splitlines():
        status, file_name = line.split("\t", 1)
        actual[file_name] = status
    assert actual == OWN, f"W33 unexpectedly modifies foreign paths: {actual!r}"
    base_source = subprocess.check_output(
        ["git", "show", BASE + ":" + EDITOR], cwd=ROOT)
    assert hashlib.sha256(base_source).hexdigest() == BASE_SHA256, "Unpinned W31 original Editor"
    code = (ROOT / EDITOR).read_text(encoding="utf-8")
    assert not check_editor(code), f"Source W33 gate failed: {check_editor(code)}"
    tests = (ROOT / TEST).read_text(encoding="utf-8")
    assert tests.count("[Test]") == 4, "Expected four original W33 native cases"
    assert "Assert.Inconclusive(" in tests, "Missing real FBX must not be counted as PASS"
    assert "AssetDatabase.LoadAssetAtPath<GameObject>(Incoming)" in tests
    assert "Assert.Pass(" not in tests
    assert "R41V5W33FixedRigEditModeTests.cs" in (ROOT / "Docs/W33/B01-31-R41V5-fixed-rig.md").read_text() or "4 Unity Editor NUnit" in (ROOT / "Docs/W33/B01-31-R41V5-fixed-rig.md").read_text()
    own_meta = (ROOT / META).read_text()
    assert re.search(r"(?m)^guid: " + GUID + r"$", own_meta)
    seen = {}
    for meta_path in sorted((ROOT / "Assets").rglob("*.meta")):
        content = meta_path.read_text(encoding="utf-8", errors="replace")
        match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", content)
        if not match:
            continue
        key = match.group(1)
        relative = meta_path.relative_to(ROOT).as_posix()
        assert key not in seen, f"DUPLICATE_UNITY_GUID {key}: {seen[key]} and {relative}"
        seen[key] = relative
    assert seen.get(GUID) == META

    corruptions = [
        code.replace("copy.transform.SetParent(root.transform, true);",
                     "copy.transform.SetParent(pitch, true);", 1),
        code.replace("visualYaw.SetParent(yaw, true);", "// REMOVED", 1),
        code.replace("visualPitch.SetParent(pitch, true);", "// REMOVED", 1),
        code.replace("if (!TryValidateSourceRigHierarchy(originalFbx, out rejection))",
                     "if (false)", 1),
        code.replace("staticRenderers == 0 || yawRenderers == 0 || pitchRenderers == 0",
                     "false", 1),
        code.replace("Quaternion.Angle(copy.transform.rotation,fixedMountRotation)",
                     "Quaternion.Angle(copy.transform.rotation,copy.transform.rotation)", 1),
    ]
    # Sixth negative: immutable base pose comparison no longer has the original
    # symbol; require two honest fixed-pose probes, not mere any expression.
    assert len(set(corruptions)) == 6 and all(c != code for c in corruptions)
    for index, mutant in enumerate(corruptions):
        errors = check_editor(mutant)
        if index == 5:
            errors += ([] if
                       "Quaternion.Angle(copy.transform.rotation,fixedMountRotation)" in mutant
                       else ["static_rotation_reference_mutated"])
        assert errors, f"Source adversarial mutant {index + 1} accepted"
    payload = {
        "kind": "SOURCE_ONLY_NOT_UNITY",
        "base_sha": BASE,
        "head_sha": git("rev-parse", "HEAD"),
        "changed_paths": len(actual),
        "changed_paths_exact": sorted(actual.keys()),
        "original_editor_base_sha256": BASE_SHA256,
        "current_editor_sha256": hashlib.sha256(code.encode("utf-8")).hexdigest(),
        "unique_unity_asset_guids": len(seen),
        "new_guid": GUID,
        "own_native_test_sources": 4,
        "native_unity": "NOT_RUN",
        "original_producer_fbx": "EXTERNAL_NOT_PROVIDED",
        "negative_mutants_rejected": len(corruptions),
        "result": "W33_SOURCE_SCOPE_AND_RIG_GUARDS_PASS_NOT_NATIVE_G1",
    }
    if a.receipt:
        output = ROOT / a.receipt
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(payload, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
