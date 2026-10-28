#!/usr/bin/env python3
"""B01-60 PASS15 — independent original CHL owner safety RED-to-GREEN receiver.

Imports the exact original B01-20 Python code at immutable QA commit, not a copy
of its implementation. Two real detached linked Git worktrees, 6 original
function calls and source clean-state controls. NO Unity/PlayMode/native call.
"""
import importlib.util
import json
import subprocess
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[3]
OWNER_COMMIT = "9a6acca417524545fbce7ac5962fe0944c5237c6"
OWNER_FILE = "Tools/B01/QA-CHL003/W66Receipt/chl003_w66_operator.py"
OWNER_BLOB = "96aec2f870ccc510727048a7208292f781e80c36"

def command(*argv, cwd=None):
    p = subprocess.run([str(x) for x in argv], cwd=cwd, text=True,
                       stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if p.returncode:
        raise RuntimeError("command failed: " + " ".join(map(str, argv)) +
                           " " + p.stderr[:500])
    return p.stdout.strip()

def git(*args, cwd=REPO):
    return command("git", "-C", cwd, *args)

def main():
    # Original source must be an immutable Git object. Fetch only a QA owner ref.
    command("git", "-C", REPO, "fetch", "--no-tags", "origin", OWNER_COMMIT)
    with tempfile.TemporaryDirectory(prefix="b0160_chl_source_path_") as td:
        root = Path(td).resolve()
        script = root / "original_b0120_chl_operator.py"
        blob = subprocess.run(["git", "-C", str(REPO), "show",
                               f"{OWNER_COMMIT}:{OWNER_FILE}"],
                              stdout=subprocess.PIPE, check=True).stdout
        script.write_bytes(blob)
        actual = git("hash-object", script)
        assert actual == OWNER_BLOB, ("CHL_OWNER_SOURCE_BLOB_DRIFT", actual)
        spec = importlib.util.spec_from_file_location("original_chl_owner", script)
        assert spec is not None and spec.loader is not None
        op = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(op)

        primary = root / "qa-primary"
        own = root / "chl-detached"
        sibling = root / "part-detached"
        git("init", primary)
        git("config", "user.email", "qa@invalid.example", cwd=primary)
        git("config", "user.name", "Read Only QA", cwd=primary)
        (primary / "Assets").mkdir()
        (primary / "Assets" / ".keep").write_text("tracked fixture\\n")
        git("add", "Assets/.keep", cwd=primary)
        git("commit", "-m", "QA fixture (not CHL production)", cwd=primary)
        pin = git("rev-parse", "HEAD", cwd=primary)
        git("worktree", "add", "--detach", own, pin, cwd=primary)
        git("worktree", "add", "--detach", sibling, pin, cwd=primary)
        def snapshot():
            return [git("status", "--porcelain=v1", "--untracked-files=all", cwd=w)
                    for w in (primary, own, sibling)]
        before = snapshot()
        def accepted(project, evidence):
            try:
                op.safe_output(project, evidence)
                return True
            except op.ReceiptError:
                return False
        cases = [
            ("OUTSIDE_BOTH_ACCEPTED", accepted(own, root / "outputs"), True),
            ("OWN_WORKTREE_REJECTED", accepted(own, own / "evidence"), False),
            ("ANCESTOR_OF_OWN_REJECTED", accepted(own, root), False),
            ("SIBLING_WORKTREE_ROOT_REJECTED", accepted(own, sibling), False),
            ("SIBLING_ASSETS_DESCENDANT_REJECTED",
             accepted(own, sibling / "Assets" / "evidence"), False),
            ("PRIMARY_SOURCE_ROOT_REJECTED", accepted(own, primary), False),
            ("SIBLING_PREFIX_OUTSIDE_ACCEPTED", accepted(own, Path(str(sibling)+"-evidence")), True),
            ("NO_SOURCE_FILES_TOUCHED", before == snapshot() and
             all(not (w / "evidence").exists() for w in (own, sibling)), True)
        ]
        result = [{"case": name, "pass": observation == expected,
                   "observed_acceptance": observation if name != "NO_SOURCE_FILES_TOUCHED" else None}
                  for name, observation, expected in cases]
        output = {
            "qa_owner": "B01-60", "operator_owner": "B01-20",
            "original_owner_commit": OWNER_COMMIT, "original_operator_git_blob": actual,
            "source_type": "ORIGINAL_OWNER_PYTHON_IMPORTED",
            "test_type": "REAL_GIT_WORKTREES_ORIGINAL_SAFE_OUTPUT",
            "native_unity": "NOT_RUN", "native_g1": "NOT_RUN",
            "cases": result,
        }
        print("B0160_PASS15_CHL_ORIGINAL_JSON=" + json.dumps(output, sort_keys=True))
        if not all(c["pass"] for c in result):
            raise AssertionError("Original CHL path behavior changed; inspect actual cases")
        print("B0160_PASS15_CHL_RESULT=ORIGINAL_SOURCE_CROSS_WORKTREE_GUARD_GREEN")
if __name__ == "__main__":
    main()
