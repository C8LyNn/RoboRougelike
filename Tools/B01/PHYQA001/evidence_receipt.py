#!/usr/bin/env python3
"""B01-00: offline RECEIPT checker for Codex PHY R1 published JSON evidence.
Does NOT run Unity, simulate physics, revalidate Unity XML or constitute B01-60 QA.
Run on a clean checkout of PR #125 (commit 916654634d1bfdc2bbc14e64b9eeac856ede1fa8).
"""
import argparse
import json
import sys
from pathlib import Path

REL = Path("Docs/Evidence/B01-PHY-R1")
SOURCE = "523046f5bf6ddab0caf62253615c962e808c3053"

def read_json(root: Path, subpath: str):
    return json.loads((root / REL / subpath).read_text(encoding="utf-8-sig"))

def run(root: Path):
    physics = read_json(root, "B01-native-Physics-20261024T182851Z/physics-native-result.json")
    process = read_json(root, "B01-native-Physics-20261024T182851Z/process.json")
    w8 = read_json(root, "B01-native-W8-20261024T182949Z/native-api-result.json")
    edit = read_json(root, "editmode-coverage-reconciliation.json")
    checks = {
        "published_native_physics_checks_75": len(physics.get("checks", [])) == 75,
        "published_native_physics_all_true": all(x.get("passed") is True for x in physics.get("checks", [])),
        "published_W8_checks_47": len(w8.get("checks", [])) == 47,
        "published_W8_all_true": all(x.get("passed") is True for x in w8.get("checks", [])),
        "published_native_process_exit_0": process.get("exitCode") == 0,
        "published_physics_build_source_sha": process.get("sourceSha") == SOURCE,
        "full_editmode_arithmetic": sum(int(edit["fullCounts"][k]) for k in ("passed", "failed", "skipped")) == int(edit["fullCounts"]["total"]),
        "full_editmode_has_7_failures": edit["fullCounts"]["failed"] == "7",
        "followup_28_pass": edit["correctionCounts"]["passed"] == "28" and edit["correctionCounts"]["failed"] == "0",
        "seven_failure_correspondences": len(edit.get("originalFailuresNowPassing", [])) == 7,
        "fresh_full_not_run_is_disclosed": "NOT_RUN" in edit.get("freshFullAfterFixtureOnlyCorrection", ""),
    }
    print(json.dumps({
        "kind": "B01-00_PUBLISHED_METADATA_RECEIPT_ONLY",
        "target_sha": "916654634d1bfdc2bbc14e64b9eeac856ede1fa8",
        "native_reexecution": "NOT_RUN",
        "unity_compilation": "NOT_RUN_BY_B01_00",
        "independent_B01_60": "PENDING",
        "checks": [{"id": name, "pass": passed} for name, passed in checks.items()],
        "passed": sum(checks.values()),
        "total": len(checks),
        "G1": "BLOCKED_PENDING_B01_60",
    }, indent=2))
    return 0 if all(checks.values()) else 1

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--repo-root", type=Path, default=Path("."))
    args = p.parse_args()
    try:
        sys.exit(run(args.repo_root.resolve()))
    except (OSError, ValueError, KeyError) as ex:
        print(f"BLOCKED: missing or invalid published evidence: {ex}", file=sys.stderr)
        sys.exit(2)
