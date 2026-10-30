#!/usr/bin/env python3
"""Extract EXACT original Codex R1RunCheckpointStorage; do not edit its method bodies."""
import argparse
from pathlib import Path
import subprocess

PIN = "203a0805251ed28293888e43f2d49f0df63767c6"
PATH = "Assets/R1/R1RunCheckpoint.cs"
BLOB = "6eb8682c14329088d5698fc369009fb86b99b426"
BOUNDARY = "    public sealed partial class R1RunController"


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args]).decode("utf-8-sig")


def prepare(repo, out):
    actual = git(repo, "rev-parse", f"{PIN}:{PATH}").strip()
    if actual != BLOB:
        raise RuntimeError(f"FAIL_CLOSED_OWNER_BLOB_DRIFT {actual} != {BLOB}")
    raw = git(repo, "show", f"{PIN}:{PATH}")
    if raw.count(BOUNDARY) != 1:
        raise RuntimeError("FAIL_CLOSED_CONTROLLER_BOUNDARY")
    prefix = raw.split(BOUNDARY)[0]
    for exact in ("public static class R1RunCheckpointStorage",
                  "public static string TestPath;",
                  "public static void Write(string path,R1RunCheckpoint document)",
                  "public static R1RunCheckpoint Read(string path)",
                  "if(!string.IsNullOrEmpty(TestPath))return TestPath;",
                  "if(Application.isBatchMode)"):
        if prefix.count(exact) != 1:
            raise RuntimeError("FAIL_CLOSED_ORIGINAL_CONTRACT_DRIFT " + exact)
    out = Path(out)
    if out.exists():
        raise RuntimeError("FAIL_CLOSED_OUTPUT_EXISTS")
    out.mkdir(parents=True)
    (out / "OriginalCodexR1RunCheckpoint.cs").write_bytes((prefix + "}\n").encode("utf-8"))
    for name in ("SlotBoundary.csproj", "Shims.cs", "Program.cs"):
        (out / name).write_bytes((Path(__file__).resolve().parent / name).read_bytes())
    print(f"PINNED_ORIGINAL_CODEX_R1_STORAGE=PASS commit={PIN} blob={BLOB} "
          "source=Assets/R1/R1RunCheckpoint.cs")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    prepare(args.repo, args.out)
