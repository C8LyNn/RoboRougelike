#!/usr/bin/env python3
"""Independent B01-30 W34 exact B01-31 #922 original rig SOURCE review.
PASS = source scope and fixed-rig guards only. No external FBX/Unity native PASS.
"""
from __future__ import annotations
import hashlib
import json
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[4]
BASE = "b61bbe0643a6547ccb3087ada44474e8b33550fd"
PREVIOUS_QA = "f8517d85549cdfbd2049a33d1881c843a68eee76"
ORIGINAL_EDITOR = "Assets/B01_31/W30R41V5Adapter/Editor/B0131W30CreateR41V5AdapterCandidate.cs"
ORIGINAL_EDITOR_GIT_BLOB = "d96ff8de81e132c2771c88e895e621cf8f29c43b"
FBX = "Assets/B01_31/W30R41V5Adapter/Incoming/R41_Turret_v5.fbx"
EXPECTED_FBX_SHA256 = "fc83d677744d7e60b5f5f5f3ad3d9e72c97f84b66bbacf142c14959becfa5533"
REUSED = [
  "Assets/B01_30.meta",
  "Assets/B01_30/W32R41V5VisualQA.meta",
  "Assets/B01_30/W32R41V5VisualQA/Editor.meta",
  "Assets/B01_30/W32R41V5VisualQA/Editor/R41V5VisualRigAcceptanceTests.cs",
  "Assets/B01_30/W32R41V5VisualQA/Editor/R41V5VisualRigAcceptanceTests.cs.meta",
  "Assets/B01_30/W32R41V5VisualQA/Editor/RoboRougelike.B0130.W32.RealR41V5VisualQA.asmdef",
  "Assets/B01_30/W32R41V5VisualQA/Editor/RoboRougelike.B0130.W32.RealR41V5VisualQA.asmdef.meta"
]
OWN = set(REUSED) | {
    "Tools/B01-30/P17/W34R41V5Receiver/check_source.py",
    "Tools/B01-30/P17/W34R41V5Receiver/README.md",
    ".github/workflows/b01-30-p17w34-r41v5-independent-source-g0.yml",
}


def git(*args: str) -> bytes:
    return subprocess.check_output(["git", *args], cwd=ROOT)


def must(ok: bool, text: str) -> None:
    if not ok:
        raise ValueError("B0130_W34_SOURCE_G0_RED " + text)


def check_actual_rig_source(code: str) -> list[str]:
    problems: list[str] = []
    def need(test: bool, code_name: str) -> None:
        if not test:
            problems.append(code_name)
    parts = code.split("public static GameObject CreateCandidatePreview(", 1)
    need(len(parts) == 2, "REAL_PREVIEW_MISSING")
    if len(parts) != 2:
        return problems
    visual = parts[1].split("[MenuItem(", 1)[0]
    preclone = visual.find("if (!TryValidateSourceVisualHierarchy(originalFbx, out rejection))")
    prerig = visual.find("if (!TryValidateSourceRigHierarchy(originalFbx, out rejection))")
    clone = visual.find("copy = Object.Instantiate(originalFbx);")
    need(0 <= preclone < prerig < clone, "REQUIRED_PREFLIGHT_BEFORE_CLONE")
    need("!sourcePitch.IsChildOf(sourceYaw)" in code, "BAD_ORIGINAL_YAW_PITCH_PROOF")
    need("!left.IsChildOf(sourcePitch) || !right.IsChildOf(sourcePitch)" in code, "ORIGINAL_MUZZLES_OUTSIDE_PITCH")
    need("staticRenderers == 0 || yawRenderers == 0 || pitchRenderers == 0" in code, "MISSING_THREE_RENDERER_PARTITIONS")
    need("copy.transform.SetParent(root.transform, true);" in visual, "FULL_FBX_ROOT_NOT_STATIC")
    need("copy.transform.SetParent(pitch, true)" not in visual, "OLD_W30_P0_FULL_FBX_PITCH")
    need("visualYaw.SetParent(yaw, true);" in visual, "NO_YAW_VISUAL_REPARENT")
    need("visualPitch.SetParent(pitch, true);" in visual, "NO_PITCH_VISUAL_REPARENT")
    need("!visualPitch.IsChildOf(visualYaw)" in visual, "NO_DOUBLE_PITCH_HIERARCHY_GUARD")
    need("Vector3.Distance(copy.transform.position,fixedMountPosition)" in visual, "STATIC_MOUNT_POSITION_NOT_CHECKED")
    need("Quaternion.Angle(copy.transform.rotation,fixedMountRotation)" in visual, "STATIC_MOUNT_ROTATION_NOT_CHECKED")
    need("Vector3.Distance(originalLeft.position,left.position)" in visual and
         "Vector3.Distance(originalRight.position,right.position)" in visual, "IMPORTED_MUZZLE_PAIR_NOT_MEASURED")
    need("KnownV5FbxSha256" in code and EXPECTED_FBX_SHA256 in code and
         "SHA256.Create()" in code, "CLAUDE_V5_SHA_PIN_ABSENT")
    need("Object.DestroyImmediate(rb)" in code and "Object.DestroyImmediate(col)" in code,
         "CLONE_VISUAL_PHYSICS_REMOVAL_MISSING")
    need("PrefabUtility.SaveAsPrefabAsset(candidate,CandidatePath)" in code and
         "EditorUtility.DisplayDialog(" in code, "OPT_IN_OUTPUT_AND_FALLBACK_GUARDS_MISSING")
    return problems


def main() -> None:
    subprocess.run(["git", "merge-base", "--is-ancestor", BASE, "HEAD"], cwd=ROOT, check=True)
    rows = [line.split("\t", 1) for line in git("diff", "--name-status", BASE + "..HEAD").decode().splitlines()]
    must(len(rows) == 10 and {p for status, p in rows} == OWN and
         all(status == "A" for status, _ in rows), "EXACT_10_ADD_ONLY_QA_PATHS")
    raw = git("show", BASE + ":" + ORIGINAL_EDITOR)
    file_content = (ROOT / ORIGINAL_EDITOR).read_bytes()
    must(raw == file_content, "CURRENT_W33_PRODUCER_CHANGED")
    must(git("rev-parse", BASE + ":" + ORIGINAL_EDITOR).decode().strip()
         == ORIGINAL_EDITOR_GIT_BLOB, "ORIGINAL_W33_EDITOR_BLOB_UNEXPECTED")
    code = file_content.decode("utf-8")
    must(not check_actual_rig_source(code), "W33_RIG_GUARDS " + repr(check_actual_rig_source(code)))
    for path in REUSED:
        must((ROOT / path).read_bytes() == git("show", PREVIOUS_QA + ":" + path),
             "PR909_ORIGINAL_NATIVE_QA_SOURCE_MUST_BE_UNMODIFIED " + path)
    tests = (ROOT / "Assets/B01_30/W32R41V5VisualQA/Editor/R41V5VisualRigAcceptanceTests.cs").read_text()
    must(tests.count("[Test]") == 2 and "Assert.Inconclusive(" in tests and
         "Assert.IsFalse(visual.IsChildOf(pitch)" in tests and
         "Quaternion.Angle(q0,visual.rotation)" in tests,
         "ORIGINAL_TWO_REAL_FBX_NATIVE_CASES_MISSING")
    must(not (ROOT / FBX).exists(), "DO_NOT_PACKAGE_EXTERNAL_PRODUCER_BINARY")
    guids = {}
    for file in (ROOT / "Assets").rglob("*.meta"):
        match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",
                          file.read_text(encoding="utf-8", errors="replace"))
        if match:
            guid = match.group(1)
            rel = file.relative_to(ROOT).as_posix()
            must(guid not in guids, "DUPLICATE_UNITY_GUID " + guid)
            guids[guid] = rel
    metas = [p for p in REUSED if p.endswith(".meta")]
    must(len(metas) == 5, "ORIGINAL_FIVE_GUIDS_MUST_BE_REUSED")
    for path in metas:
        match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",
                          (ROOT / path).read_text())
        must(match is not None and guids.get(match.group(1)) == path,
             "ORIGINAL_QA_META_GUID_MISSING_OR_NOT_UNIQUE " + path)

    # Independent adversarial edits: every loss of source safety must be RED.
    swaps = [
        ("copy.transform.SetParent(root.transform, true);",
         "copy.transform.SetParent(pitch, true);"),
        ("visualYaw.SetParent(yaw, true);", "// removed yaw visual"),
        ("visualPitch.SetParent(pitch, true);", "// removed pitch visual"),
        ("if (!TryValidateSourceRigHierarchy(originalFbx, out rejection))",
         "if (false)"),
        ("staticRenderers == 0 || yawRenderers == 0 || pitchRenderers == 0",
         "false"),
        ("Quaternion.Angle(copy.transform.rotation,fixedMountRotation)",
         "Quaternion.Angle(copy.transform.rotation,copy.transform.rotation)"),
        ("Vector3.Distance(originalLeft.position,left.position)",
         "Vector3.Distance(left.position,left.position)"),
        (EXPECTED_FBX_SHA256, "0" * 64),
    ]
    for i, (old, new) in enumerate(swaps, 1):
        must(old in code and old != new, "MUTATION_WITNESS_NOT_APPLICABLE " + str(i))
        mutated = code.replace(old, new, 1)
        must(check_actual_rig_source(mutated), "UNSAFE_MUTANT_NOT_REJECTED " + str(i))

    # Separate source finding: old relative CWD-based output collision tests.
    menu = code.split("[MenuItem(", 1)[-1]
    output_path_risk = ("File.Exists(CandidatePath)" in menu and
                        "File.Exists(CandidatePath+\".meta\")" in menu and
                        "TryResolveCandidatePath(" not in menu)
    must(output_path_risk, "OUTPUT_PATH_FIX_CHANGED_REVIEW_AGAIN")

    payload = {
        "source": "B01-30 independent W34 SHA-pinned witness; NOT Unity",
        "upstream_922_sha": BASE,
        "source_editor_blob": ORIGINAL_EDITOR_GIT_BLOB,
        "source_editor_sha256": hashlib.sha256(raw).hexdigest(),
        "scope": "EXACT_TEN_ADD_ONLY",
        "ported_original_909_native_case_sources": 2,
        "ported_original_909_git_blobs": len(REUSED),
        "unique_all_assets_unity_guids": len(guids),
        "negative_source_mutants_rejected": len(swaps),
        "original_full_fbx_root_pitch_defect": "SOURCE_FIXED",
        "candidate_output_relative_cwd_collision": "P1_SOURCE_RISK_OPEN",
        "exact_original_fbx": "NOT_INCLUDED",
        "native_unity_g1": "AUTHORED_NOT_RUN",
        "real_win64_player": "NOT_RUN",
        "game_prefab": "HOLD",
        "integrated": False,
        "released": False,
    }
    output = ROOT / "build/B0130W34/receipt.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, sort_keys=True, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(payload, sort_keys=True))
    print("B0130_W34_SOURCE_G0_PASS__PRODUCT_NATIVE_FBX_UNVERIFIED__OUTPUT_CWD_P1_OPEN")


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(str(exc), file=sys.stderr)
        sys.exit(1)
