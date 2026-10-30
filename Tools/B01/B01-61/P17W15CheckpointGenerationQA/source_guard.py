#!/usr/bin/env python3
"""Pin immutable original #556/510/W06 source, refuse any QA scope drift."""
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
SELECTED_SAVE = "33094f154c7b91119b36d58aab285e6e83c5de57"
SELECTED_GAME = "d94400d95d13db7f27e1533df05c9292d5380431"
ORIGINAL_W06 = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PINS = (
    ("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
     SELECTED_SAVE, "b52e511c370f5fca56c74029050844dc8cbb7f9a"),
    ("Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs",
     ORIGINAL_W06, "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"),
    ("Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs",
     SELECTED_GAME, "b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9"),
    ("Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs",
     SELECTED_GAME, "b680ed0ec969ac23203fa55eab7b90f8daa86e43")
)
DIR = "Tools/B01/B01-61/P17W15CheckpointGenerationQA/"
OWNED = {DIR+n for n in ("Program.cs", "Original556CheckpointQA.csproj",
                          "source_guard.py", "README.md", "manifest.json")}
OWNED.add(".github/workflows/b01-61-p17w15-cross-process-generation.yml")
def git(*args):
    return subprocess.check_output(["git", "-C", str(ROOT), *args], text=True).strip()
def check(condition, message):
    if not condition:
        raise AssertionError(message)
def main():
    # The PR synthetic checkout may include producer changes from a later base;
    # when that happens, compare own PR second parent with frozen source, not merge.
    try:
        candidate = git("rev-parse", "HEAD^2")
        is_merge = len(git("rev-list", "--parents", "-n", "1", "HEAD").split()) > 2
        if not is_merge:
            candidate = git("rev-parse", "HEAD")
    except subprocess.CalledProcessError:
        candidate = git("rev-parse", "HEAD")
    observed = set(filter(None, git("diff", "--name-only", SELECTED_SAVE, candidate).splitlines()))
    check(observed == OWNED, "QA_SCOPE_NOT_EXCLUSIVE: "+str(sorted(observed^OWNED)))
    for path, original_ref, expected_blob in PINS:
        check(git("rev-parse", original_ref+":"+path) == expected_blob,
              "ORIGINAL_SOURCE_REF_DRIFT "+path)
        check(git("rev-parse", "HEAD:"+path) == expected_blob,
              "PRODUCER_SOURCE_MUTATED "+path)
        check(git("hash-object", path) == expected_blob,
              "WORKTREE_SOURCE_MUTATED "+path)
    contract = (ROOT/PINS[0][0]).read_text()
    check("Current!=null&&ready.Snapshot.Generation<Current.Snapshot.Generation" in contract,
          "GENERATION_SEMANTICS_CHANGED_RETEST_DIFFERENT_SHA")
    check("swapping=true; // guard validation, callbacks AND publication" in contract,
          "556_REENTRANCY_FIX_MISSING")
    print("SOURCE_PIN_PASS=4/4 ORIGINAL_556_ORIGINAL_W06_FROZEN510")
    print("OWNED_DIFF_PASS=6/6 independent B01-61 only; original SHA unchanged")
    print("SAME_GENERATION_CONFLICT_CHECK_IS_RELEASE_HOLD_WHEN_REPRODUCED")
    print("UNITY_NATIVE_NOT_RUN; CANONICAL_HOST_NOT_INTEGRATED; NO_MERGE")
if __name__ == "__main__":
    main()
