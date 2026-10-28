#!/usr/bin/env python3
"""B01-60 W70: independent RED-to-GREEN on ORIGINAL PARTSIZE owner preflight.

No Unity invocation. -PreflightOnly uses a non-executable placeholder file.
Original PowerShell Git blob must be exact; no producer or Codex changes.
"""
import json
import shutil
import subprocess
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[3]
PART_SHA = "be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
OP_SHA = "26634727ab2323a982bce893421c88c330be46e8"
PS_BLOB = "bd7bccd2a949d91b1901e3ec01ab93c62874eef4"
PS_PATH = "Tools/B01/PARTSIZE001/W26Receipt/Run-UnityNative.ps1"

def command(*args, cwd=None, check=True):
    return subprocess.run(list(map(str, args)), cwd=cwd, capture_output=True,
                          text=True, encoding="utf-8", errors="replace", check=check)

def git(*args, cwd=REPO, check=True):
    return command("git", "-C", cwd, *args, check=check)

def invoke(script, unity, project, evidence):
    return command("pwsh", "-NoProfile", "-NonInteractive", "-File", script,
                   "-UnityEditor", unity, "-ProjectPath", project,
                   "-EvidenceDir", evidence, "-PreflightOnly", check=False)

def snapshot(path):
    return (git("status", "--porcelain=v1", "--untracked-files=all", cwd=path).stdout,
            git("rev-parse", "HEAD", cwd=path).stdout.strip())

def main():
    if shutil.which("pwsh") is None:
        raise RuntimeError("POWERSHELL_NOT_INSTALLED: test not executed")
    for sha in (PART_SHA, OP_SHA):
        git("fetch", "--no-tags", "origin", sha)
    original = subprocess.run(["git", "-C", str(REPO), "show", f"{OP_SHA}:{PS_PATH}"],
                              stdout=subprocess.PIPE, check=True).stdout
    with tempfile.TemporaryDirectory(prefix="b01-60-w66-") as tmp:
        root = Path(tmp).resolve()
        script = root / "original_owner_Run-UnityNative.ps1"
        script.write_bytes(original)
        actual_blob = git("hash-object", script).stdout.strip()
        if actual_blob != PS_BLOB:
            raise AssertionError(f"ORIGINAL_OPERATOR_GIT_BLOB_DRIFT {actual_blob}")
        own = root / "partA"
        sibling = root / "partB"
        for worktree in (own, sibling):
            git("worktree", "add", "--detach", worktree, PART_SHA)
            if git("rev-parse", "HEAD", cwd=worktree).stdout.strip() != PART_SHA:
                raise AssertionError("WRONG_SOURCE_SHA")
        try:
            (own / ".b01-60-disposable-qa").write_text("test fixture only\n")
            fake_unity = root / "UnityEditor_NOT_AN_EXECUTABLE.txt"
            fake_unity.write_text("preflight only; never invoke\n")
            before = [snapshot(w) for w in (own, sibling)]
            cases = []
            good = invoke(script, fake_unity, own, root / "evidence-safe")
            cases.append({"name": "OUTSIDE_BOTH_ACCEPTED",
                          "pass": good.returncode == 0 and
                          "PARTSIZE_DISPOSABLE_PREFLIGHT_PASS" in good.stdout,
                          "exit": good.returncode})
            denied = invoke(script, fake_unity, own, own / "evidence-denied")
            cases.append({"name": "OWN_WORKTREE_REJECTED",
                          "pass": denied.returncode != 0 and
                          "RECEIPT_MUST_BE_OUTSIDE_PROJECT" in (denied.stderr + denied.stdout),
                          "exit": denied.returncode})
            other_root = invoke(script, fake_unity, own, sibling)
            cases.append({"name": "SIBLING_SOURCE_ROOT_NOW_REJECTED",
                          "pass": other_root.returncode != 0 and
                          "RECEIPT_OVERLAPS_REGISTERED_WORKTREE" in (other_root.stderr+other_root.stdout),
                          "exit": other_root.returncode})
            other_child = invoke(script, fake_unity, own, sibling / "Assets" / "evidence")
            cases.append({"name": "SIBLING_ASSETS_DESCENDANT_NOW_REJECTED",
                          "pass": other_child.returncode != 0 and
                          "RECEIPT_OVERLAPS_REGISTERED_WORKTREE" in (other_child.stderr+other_child.stdout),
                          "exit": other_child.returncode})
            sibling_prefix = invoke(script, fake_unity, own, Path(str(sibling)+"-evidence"))
            cases.append({"name": "SIMILAR_NAME_NOT_SIBLING_DESCENDANT_ACCEPTED",
                          "pass": sibling_prefix.returncode == 0 and
                          "PARTSIZE_DISPOSABLE_PREFLIGHT_PASS" in sibling_prefix.stdout,
                          "exit": sibling_prefix.returncode})
            after = [snapshot(w) for w in (own, sibling)]
            cases.append({"name": "BOTH_SOURCE_WORKTREES_CLEAN_AFTER_PREFLIGHT",
                          "pass": before == after and
                          not (own / ".b01-60-disposable-qa.used").exists(), "exit": 0})
            cases.append({"name": "NO_EVIDENCE_OR_EDITOR_EXECUTION",
                          "pass": not (root / "evidence-safe").exists() and
                          not (sibling / "evidence").exists() and
                          not (sibling / "Assets" / "evidence").exists(), "exit": 0})
            receipt = {"kind": "ORIGINAL_OWNER_PS1_REAL_PWSH_GIT_PREFLIGHT",
                       "source_sha": PART_SHA, "operator_sha": OP_SHA,
                       "operator_git_blob": actual_blob,
                       "unity": "NOT_RUN", "native_g1": "NOT_RUN",
                       "repro": "CROSS_WORKTREE_EVIDENCE_ISOLATION_RED_TO_GREEN",
                       "cases": cases}
            print("B01_60_W70_JSON=" + json.dumps(receipt, sort_keys=True))
            if not all(c["pass"] for c in cases):
                raise SystemExit("UNEXPECTED_TEST_RESULT; inspect original script/preflight")
        finally:
            for worktree in (own, sibling):
                git("worktree", "remove", "--force", worktree)
    print("B01_60_W70_RESULT=OWNER_CROSS_WORKTREE_GUARD_GREEN; UNITY_NOT_RUN; NO_SOURCE_MUTATION")

if __name__ == "__main__":
    main()
