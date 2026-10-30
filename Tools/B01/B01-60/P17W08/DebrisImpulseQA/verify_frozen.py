#!/usr/bin/env python3
"""B01-60 W08: exact frozen #421 source code and strict B01-60 owner-scope gate."""
from __future__ import annotations
import json
import subprocess
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[5]
OWNER="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
CORE="Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"
EMITTER="Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs"
BLOBS={
 CORE:"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 EMITTER:"2b3a8465c550bd49a5c98be5a8064a4ce0947f4b",
 "Assets/B0123DebrisRepulse001/Runtime/B0123.DebrisRepulse.Runtime.asmdef":"25a49dc836b62d791c0bf40c214c1c6cf09a9fea"}
QA="Tools/B01/B01-60/P17W08/DebrisImpulseQA/"
CI=".github/workflows/b01-60-p17w08-debris-impulse-qa.yml"
def g(*args):
 return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.STDOUT).decode().strip()
def assert_state(condition,what):
 if not condition: raise AssertionError(what)
def main():
 for path,expected in BLOBS.items():
  for commit in (OWNER,"HEAD"):
   assert_state(g("rev-parse",commit+":"+path)==expected,"BLOB_DRIFT "+path+" "+commit)
 # Exactly original source pinned, no change inside producer's Assets subtree.
 changes=[p.replace("\\","/") for p in g("diff","--name-only",OWNER,"HEAD").splitlines()]
 assert_state(set(changes)=={QA+"Program.cs",QA+"IndependentQA.csproj",
     QA+"verify_frozen.py",QA+"README.md",QA+"manifest.json",CI},
     "OWNER_SCOPE_COLLISION="+str(changes))
 # Fully inspect 13 original members directly via original author owner file enumeration.
 import importlib.util
 p=ROOT/"Tools/B01/B01-23/P17W06DebrisRepulse001/package.py"
 spec=importlib.util.spec_from_file_location("b0123_package",p)
 mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
 assert_state(len(mod.FILES)==13 and len(set(mod.FILES))==13,"ORIGINAL_MEMBER_COUNT")
 assert_state(mod.source_sha()==g("rev-parse","HEAD"),"OWNER_PACKAGE_WOULD_PIN_QA_HEAD")
 # Original author package.py intentionally packages HEAD; verify its 13 inputs unchanged
 # by comparing exact raw Git blobs between frozen original owner SHA and new QA branch HEAD.
 for path in mod.FILES:
  assert_state(g("rev-parse",OWNER+":"+path)==g("rev-parse","HEAD:"+path),
     "FROZEN_SOURCE_MEMBER_MUTATED "+path)
 print(json.dumps({"package":"B01-60-W08-DEBRIS-IMPULSE-INDEPENDENT-QA",
  "original_owner_sha":OWNER,"core_blob":BLOBS[CORE],"emitter_blob":BLOBS[EMITTER],
  "original_file_count":len(mod.FILES),"own_files":len(changes),
  "source_gate":"PASS","Unity":"NOT_RUN","Codex_ACK":"PENDING"},sort_keys=True))
if __name__=="__main__":
 try:main()
 except Exception as e:
  print("B01_60_W08_FROZEN_QA_FAIL "+repr(e),file=sys.stderr)
  sys.exit(2)
