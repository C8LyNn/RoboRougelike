#!/usr/bin/env python3
"""B01-60 W08 independent immutable original-source and scoped change preflight."""
import hashlib
import json
import pathlib
import re
import subprocess
import sys
import tempfile

ROOT=pathlib.Path(__file__).resolve().parents[5]
PRODUCER="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
P="Assets/B0123DebrisRepulse001/"
Q="Tools/B01/B01-60/P17W08/DebrisKinematicsQA/"
PIN={
    P+"Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
    P+"Runtime/DebrisRepulsionEmitter.cs":"2b3a8465c550bd49a5c98be5a8064a4ce0947f4b",
    P+"Runtime/B0123.DebrisRepulse.Runtime.asmdef":"25a49dc836b62d791c0bf40c214c1c6cf09a9fea",
    P+"Runtime/B0123.DebrisRepulse.Runtime.asmdef.meta":"5576a828cb49cec88c79049f70c40d81dd13c4a9",
    P+"Runtime/DebrisImpulseCore.cs.meta":"ab3b0e3d291942bf90ca64583ab54e6511828dff",
    P+"Runtime/DebrisRepulsionEmitter.cs.meta":"e99a0e675058abcd60dc28d1191c02962af75c51",
    P+"Runtime.meta":"5e4ad70a12ace046c7712feccb9fc0cf11cc4e61",
    P.rstrip("/")+".meta":"4900cc8af114ab7fd40fe65429a4c3d8dd4be5cf",
}
OWN={Q+"Program.cs",Q+"DebrisKinematicsQA.csproj",Q+"source_gate.py",
     Q+"README.md",Q+"manifest.json",".github/workflows/b01-60-p17w08-debris-kinematics.yml"}

def object_hash(payload:bytes)->str:
    return hashlib.sha1(b"blob "+str(len(payload)).encode()+b"\0"+payload).hexdigest()

def verify_blob(root:pathlib.Path,path:str,sha:str)->None:
    p=root/path
    if not p.is_file() or p.is_symlink() or object_hash(p.read_bytes())!=sha:
        raise ValueError("original producer source missing/drift: "+path)

def verify_scope(changed:list[str])->None:
    if len(changed)!=len(OWN) or set(changed)!=OWN:
        raise ValueError("changed file scope invalid: "+repr(sorted(set(changed)^OWN)))
    if any(x.startswith("Assets/") or x.startswith("Research/") for x in changed):
        raise ValueError("producer/Codex source mutated")

def negatives()->None:
    with tempfile.TemporaryDirectory() as td:
        r=pathlib.Path(td);f=r/"t";f.write_bytes(b"immutable")
        expected=object_hash(f.read_bytes());verify_blob(r,"t",expected)
        for candidate in (b"immutable\n",b"Immutable",b"",b"immutable\x00"):
            f.write_bytes(candidate)
            try:verify_blob(r,"t",expected)
            except ValueError:pass
            else:raise AssertionError("mutated original blob was not detected")
        f.unlink()
        try:verify_blob(r,"t",expected)
        except ValueError:pass
        else:raise AssertionError("missing original blob was accepted")
    verify_scope(sorted(OWN))
    for changed in (sorted(OWN-{Q+"Program.cs"}),sorted(OWN|{P+"Runtime/DebrisImpulseCore.cs"}),
                    sorted(OWN|{"Assets/R3/UnsafeNewFile.cs"})):
        try:verify_scope(changed)
        except ValueError:pass
        else:raise AssertionError("nonexclusive scope accepted")
    print("PASS 8/8 original-byte mutation and scope negatives")

def git(*a):
    return subprocess.check_output(["git",*a],cwd=ROOT,text=True).strip()

def main():
    negatives()
    for p,s in PIN.items():verify_blob(ROOT,p,s)
    metas=[(ROOT/p).read_text(encoding="utf-8") for p in PIN if p.endswith(".meta")]
    guids=[re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",data).group(1) for data in metas]
    if len(guids)!=5 or len(set(guids))!=5:raise ValueError("invalid Unity GUID identity")
    asmdef=json.loads((ROOT/P/"Runtime/B0123.DebrisRepulse.Runtime.asmdef").read_text(encoding="utf-8"))
    if asmdef["autoReferenced"] is not False or asmdef["name"]!="B0123.DebrisRepulse.Runtime":
        raise ValueError("original opt-in Unity assembly contract drift")
    if git("rev-parse","HEAD^")!=PRODUCER:
        raise ValueError("QA branch must be directly stacked on exact immutable producer SHA")
    verify_scope(git("diff","--name-only",PRODUCER,"HEAD").splitlines())
    print("PASS 8/8 pinned original Git blobs + 5/5 stable unique GUID + asmdef opt-in + 6/6 QA-only paths")
    print("ORIGINAL #421 immutable; Unity licensed native PhysX G1 NOT_RUN; Codex ACK absent")

if __name__=="__main__":
    try:main()
    except Exception as e:
        print("FAIL "+str(e),file=sys.stderr);sys.exit(1)
