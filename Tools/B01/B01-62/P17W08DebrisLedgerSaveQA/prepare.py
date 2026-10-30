#!/usr/bin/env python3
"""Fail-closed extraction of UNMODIFIED original B01-23/PR421 C#9 ledger source."""
import argparse
from pathlib import Path
import subprocess

COMMIT="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
PATH="Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"
BLOB="fa1aa5e531927cbe73cf5fdfe161b4a981316db5"

def git(repo,*args):
    return subprocess.check_output(["git","-C",str(repo),*args])

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True)
    p.add_argument("--out",required=True)
    a=p.parse_args()
    blob=git(a.repo,"rev-parse",f"{COMMIT}:{PATH}").decode().strip()
    if blob!=BLOB:raise RuntimeError("FAIL_CLOSED_SOURCE_GIT_BLOB_DRIFT "+blob)
    data=git(a.repo,"show",f"{COMMIT}:{PATH}")
    if b"public sealed class DebrisImpulseLedger" not in data or b"public bool TryDetach(" not in data:
        raise RuntimeError("FAIL_CLOSED_METHOD_SHAPE_DRIFT")
    out=Path(a.out)
    if out.exists():raise RuntimeError("FAIL_CLOSED_SCRATCH_ALREADY_EXISTS")
    out.mkdir(parents=True)
    (out/"OriginalDebrisImpulseCore.cs").write_bytes(data)
    here=Path(__file__).resolve().parent
    for name in ("DebrisLedgerSaveQA.csproj","Program.cs"):
        (out/name).write_bytes((here/name).read_bytes())
    print("PINNED_ORIGINAL_PR421_DEBRIS_LEDGER=PASS sha="+COMMIT+" git_blob="+BLOB)

if __name__=="__main__":
    main()
