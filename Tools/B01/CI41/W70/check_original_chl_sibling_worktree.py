#!/usr/bin/env python3
"""B01-41 PASS14: original B01-20 CHL W66 operator cross-worktree provenance witness.

Reads the ORIGINAL pinned Git blob. Executes actual safe_output() and
registered_detached() on two REAL git linked worktrees. Neither Unity nor
synthetic NUnit test data is involved. PASS means the known VULNERABILITY
was reproduced, NOT that the original operator is safe.
"""
import argparse
import importlib.util
from pathlib import Path
import subprocess
import tempfile

PRODUCER = "c7b2a234a286089383503da41883d624fe650551"
OPERATOR_COMMIT = "e395b8b1e25b56285e39d4d69cf472af3f3d5ee8"
OPERATOR_BLOB = "e342d882bbce93f8d4c35b225c90f04a2c71ddea"
OP_PATH = "Tools/B01/CHL003/W66Receipt/chl003_w66_operator.py"


def git(repo, *args):
    p = subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True)
    if p.returncode:
        raise AssertionError("GIT_FAILURE:" + p.stderr[-500:])
    return p.stdout.strip()


def check(label, cond):
    if not cond:
        raise AssertionError("FAIL:" + label)
    print("PASS:" + label, flush=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", required=True)
    args = ap.parse_args()
    repo = Path(args.repo).resolve(strict=True)
    check("original_operator_git_blob_pin", git(repo, "rev-parse", OPERATOR_COMMIT + ":" + OP_PATH) == OPERATOR_BLOB)
    check("authored_source_SHA_pinned", git(repo, "rev-parse", PRODUCER + "^{commit}") == PRODUCER)
    with tempfile.TemporaryDirectory(prefix="B0141_CHLW66_ORIGINAL_GIT_") as d:
        temp = Path(d)
        source = temp / "original_chl_operator.py"
        source.write_bytes(subprocess.check_output(["git", "-C", str(repo),
                                                      "show", OPERATOR_COMMIT + ":" + OP_PATH]))
        check("original_git_object_bytes", git(repo, "hash-object", str(source)) == OPERATOR_BLOB)
        spec = importlib.util.spec_from_file_location("actual_chl_w66", source)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        first = temp / "chl-source-1"
        sibling = temp / "chl-source-2"
        git(repo, "worktree", "add", "--detach", str(first), PRODUCER)
        git(repo, "worktree", "add", "--detach", str(sibling), PRODUCER)
        try:
            check("real_pristine_first_linked_worktree",
                  module.registered_detached(first, PRODUCER) == first.resolve())
            check("real_pristine_sibling_linked_worktree",
                  module.registered_detached(sibling, PRODUCER) == sibling.resolve())
            external = temp / "private-evidence-outside-all-worktrees"
            check("outside_all_worktrees_allowed", module.safe_output(first, external)[1] == external)
            for title, path, token in [
                ("own_worktree_root_DENIED", first, "EVIDENCE_IN_PROJECT"),
                ("own_worktree_child_DENIED", first / "Assets" / "records", "EVIDENCE_IN_PROJECT"),
                ("worktree_ancestor_DENIED", temp, "EVIDENCE_ANCESTOR_OF_PROJECT"),
            ]:
                try:
                    module.safe_output(first, path)
                except module.ReceiptError as exc:
                    check(title, token in str(exc))
                else:
                    raise AssertionError("FAIL:unsafe own worktree accepted:" + title)
            # These are expected RED source behaviors on original Git code:
            # a sibling may be destroyed if output writes to an already registered
            # producer source root. We only CALL safe_output; no files are written.
            for title, path in [
                ("SIBLING_ROOT_FAIL_OPEN_REPRODUCED", sibling),
                ("SIBLING_ASSETS_CHILD_FAIL_OPEN_REPRODUCED", sibling / "Assets" / "records"),
            ]:
                try:
                    pr, ev = module.safe_output(first, path)
                except module.ReceiptError as exc:
                    raise AssertionError("SOURCE_CHANGED_OR_FIX_APPLIED:" + title + " " + str(exc)) from exc
                check(title, pr == first.resolve() and ev == path.resolve(strict=False))
            check("first_worktree_STILL_CLEAN", git(first, "status", "--porcelain", "--untracked-files=all") == "")
            check("sibling_worktree_STILL_CLEAN", git(sibling, "status", "--porcelain", "--untracked-files=all") == "")
            print("ORIGINAL_CHL_W66_OPERATOR_CROSS_WORKTREE_P0_FAIL_OPEN_REPRODUCED=TRUE", flush=True)
            print("NATIVE_UNITY=NOT_RUN; G1=HOLD; this is source-control preflight only", flush=True)
        finally:
            git(repo, "worktree", "remove", "--force", str(first))
            git(repo, "worktree", "remove", "--force", str(sibling))


if __name__ == "__main__":
    main()
