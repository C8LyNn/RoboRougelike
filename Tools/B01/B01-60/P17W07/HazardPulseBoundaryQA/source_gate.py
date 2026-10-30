#!/usr/bin/env python3
"""B01-60 exact source/manifest and immutable scope gate for HAZARDPULSE #430."""
import hashlib
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[5]
BASE = "6095577ece27320b1a861a738cc23ba42d6793a6"
SOURCE = "Research/B01-25/P17/HAZARDPULSE001/"
QA = "Tools/B01/B01-60/P17W07/HazardPulseBoundaryQA/"
PIN = {
    "PulseCore.cs":"a7a2c69c80d43bf027f2b00896df72f892fa1a17",
    "Program.cs":"6c41dc23d66d004d2c514ef91bad7b4e55a67d9d",
    "profiles.json":"c0b03adee398ed12fa535c85ca2506d090d40cca",
    "HazardPulse.csproj":"e392417029ceb7a804b7cae84c66b71fb1806b9b",
    "PulseUnityCompat.csproj":"924e9c9fb5850a32ff715f0eac8629fbb6fc177a",
    ".gitignore":"6943b188d80a5efce450393ae612928f653e0876",
    "README.md":"30a20be6a537e84def08f1cb1075a4a3fc630bd3",
}
OTHER = {".github/workflows/b01-25-w06-hazardpulse.yml":"23fdaa41bc3a89ca83704d6ebaf0633b8e8fddd2"}
EXPECTED = {SOURCE+n:v for n,v in PIN.items()} | OTHER
EXPECTED[SOURCE+"manifest.json"]="ae6fa1d0b7e004c1af75578d60742747c8f6505b"
OWN = {
    QA+"Program.cs", QA+"HazardPulseIndependentQA.csproj", QA+"source_gate.py",
    QA+"README.md",QA+"manifest.json",
    ".github/workflows/b01-60-p17w07-hazardpulse-qa.yml"
}

def blob(payload):
    return hashlib.sha1(b"blob "+str(len(payload)).encode()+b"\0"+payload).hexdigest()

def must_match(root,name,hash_value):
    path=root/name
    if not path.is_file() or path.is_symlink() or blob(path.read_bytes())!=hash_value:
        raise ValueError("missing or modified producer Git blob "+name)

def scope(paths):
    if len(paths)!=len(OWN) or set(paths)!=OWN:
        raise ValueError("unexpected QA diff scope "+repr(sorted(set(paths)^OWN)))
    if any(name.startswith(SOURCE) or name.startswith("Assets/") for name in paths):
        raise ValueError("producer or canonical game modified")

def negatives():
    from tempfile import TemporaryDirectory
    with TemporaryDirectory() as d:
        root=pathlib.Path(d);f=root/"fixture";f.write_bytes(b"original")
        must_match(root,"fixture",blob(b"original"))
        for bad in (b"original\n",b"Original",b"",b"original\x00"):
            f.write_bytes(bad)
            try: must_match(root,"fixture",blob(b"original"))
            except ValueError: pass
            else: raise AssertionError("mutated blob was accepted")
        f.unlink()
        try: must_match(root,"fixture",blob(b"original"))
        except ValueError: pass
        else: raise AssertionError("missing source accepted")
    scope(sorted(OWN))
    for illegal in [sorted(OWN- {QA+"Program.cs"}),sorted(OWN|{"Assets/R3/Fake.cs"}),
                    sorted(OWN|{SOURCE+"PulseCore.cs"})]:
        try: scope(illegal)
        except ValueError: pass
        else: raise AssertionError("scope violation accepted")
    print("PASS 8/8 independent source-mutation and scope-negative fixtures")

def git(*args):
    return subprocess.check_output(["git",*args],cwd=ROOT,text=True).strip()

def main():
    negatives()
    for path,digest in EXPECTED.items(): must_match(ROOT,path,digest)
    author=json.loads((ROOT/SOURCE/"manifest.json").read_text(encoding="utf-8"))
    if author["gitBlobs"]!={**PIN,**OTHER}:
        raise ValueError("producer manifest/source pin divergence")
    if git("rev-parse","HEAD^") != BASE:
        raise ValueError("independent QA must be stacked only on immutable producer exact SHA")
    scope(git("diff","--name-only",BASE,"HEAD").splitlines())
    print("PASS 9/9 frozen producer Git objects; 6/6 QA-only paths")
    print("Native Unity/PhysX/Player NOT_RUN, Codex ACK pending, no merge")

if __name__=="__main__":
    try: main()
    except Exception as exc:
        print("FAIL "+str(exc),file=sys.stderr)
        raise SystemExit(1)
