#!/usr/bin/env python3
"""B01-62 W22: ORIGINAL owner R5 + selected API V2 + frozen game Save
Combined physical Git provenance, scratch receiver and rollback; no Unity install.
Only exact immutable producer SHAs; NEVER operate on a user-owned receiver.
"""
from __future__ import annotations
import hashlib
import importlib.util
import json
import os
import re
import shutil
import subprocess
import tempfile
import zipfile
from pathlib import Path

GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
R4="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
API1="eed2e39636edfd8b8db754c426db452164cbf9d7"
API2="809f82575c638e1b4d2806b5f417bddab6c0871a"
STORE="d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121"
SESSION="bd8905ad549a7b26854b671be971f7d781308311"
GUID_COUNTS={"game":14,"W06":5,"save":4,"R5":11,"API2":4}
SOURCE_PATHS={
 "game":"Assets/B0123SecureReattachV2",
 "W06":"Assets/B0123DebrisRepulse001",
 "save":"Assets/B0123DetachedSaveContract001",
}
PRODUCERS={
 "R5":{"sha":R5,"pack":"Tools/B01/B01-23/P17W15UnitySaveBridge001/package.py",
       "sha256":"a37e942d6a0cc5236570edaf7cba49384a84d0553f88fb29988a855d692a1496",
       "size":129088,"members":27,"guid_count":11},
 "API2":{"sha":API2,"pack":"Tools/B01/B01-23/P17W20CanonicalSaveApi001/package.py",
       "sha256":"90198e6429d912a40e4e2df3935281627282f14799c4f34479439af150f5bccd",
       "size":61736,"members":15,"guid_count":4},
}
PIN_BLOBS={
 (GAME,"Assets/B0123SecureReattachV2/Runtime/SecureRigSpec.cs"):"b680ed0ec969ac23203fa55eab7b90f8daa86e43",
 (GAME,"Assets/B0123SecureReattachV2/Runtime/AtomicDockLedger.cs"):"b6a4d04b0c6407c8c84451b4d599d96fe0e4d1a9",
 (GAME,"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"):"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 (SAVE,"Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs"):"b52e511c370f5fca56c74029050844dc8cbb7f9a",
 (R5,"Assets/B0123UnitySaveBridge001/Core/OwnerWorldLeaseRetirement.cs"):"1ef69de9b4029ce89ba2db5bf3331708da1b91e4",
 (R5,"Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs"):"7da9a9efe489cf8c441d7bb9a47ba3f60ba4c58c",
 (API2,"Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"):"704e774dedd5be5af26e0791fba87ecdb109ae2a",
 (STORE,"Tools/B01/B01-62/P17W20SingleFileSaveSlot/SingleFileSaveSlot.cs"):"db7745635164d9eb6b10f3246538700158130b7b",
 (SESSION,"Tools/B01/B01-62/P17W21CanonicalSessionLeaseR2/CanonicalSlotSessionR2.cs"):"fefc2e263e068b913c6e59ec3c8375bbbe43db4e",
}
checks=0
def insist(ok,reason):
 global checks
 if not ok:raise AssertionError(reason)
 checks+=1
 print("PASS",reason)
def git(repo,*args)->bytes:
 return subprocess.check_output(["git","-C",str(repo),*args],stderr=subprocess.PIPE)
def full(repo,sha,path)->bytes:
 return git(repo,"show",sha+":"+path)
def h(raw):return hashlib.sha256(raw).hexdigest()
def oid(raw):
 return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()
def load_python(path,module):
 spec=importlib.util.spec_from_file_location(module,path)
 assert spec and spec.loader
 mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
 return mod
def originals(repo):
 for (commit,path),expected in PIN_BLOBS.items():
  raw=full(repo,commit,path)
  ref=git(repo,"rev-parse",commit+":"+path).decode().strip()
  insist(ref==expected and oid(raw)==expected,"immutable original Git blob "+path)
 for sha in (GAME,SAVE,R4,R5,API1,API2,STORE,SESSION):
  insist(git(repo,"rev-parse",sha).decode().strip()==sha,"exact original commit "+sha[:10])
 # Explicitly avoid ALT source inclusion. This is a whitelist contract,
 # not a claim no other Git blobs exist globally.
 insist(R5!=R4 and API2!=API1,"new version SHA distinct from frozen predecessors")

def paths(repo,commit,prefix):
 data=git(repo,"ls-tree","-r","--name-only",commit,"--",prefix).decode()
 result=[x for x in data.splitlines() if x.startswith(prefix+"/")]
 root=prefix+".meta"
 if git(repo,"cat-file","-e",commit+":"+root)==b"":result.append(root)
 return sorted(result)

def read_meta(raw):
 tags=re.findall(r"(?m)^guid:\s*([a-f0-9]{32})\s*$",raw.decode("utf-8"))
 if len(tags)!=1:raise AssertionError("malformed or duplicate Unity meta GUID")
 return tags[0]

def neg(fn,reason):
 rejected=False
 try:fn()
 except (OSError,ValueError,AssertionError,RuntimeError,zipfile.BadZipFile):
  rejected=True
 insist(rejected,reason)

def run(root:Path):
 with tempfile.TemporaryDirectory(prefix="b0162-w22-physical-") as t:
  tmp=Path(t)
  originals(root)
  receipts={}
  imported={}
  worktrees=[]
  try:
   for label,pin in PRODUCERS.items():
    checkout=tmp/("exact-"+label)
    git(root,"worktree","add","--detach",str(checkout),pin["sha"])
    worktrees.append(checkout)
    insist(git(checkout,"rev-parse","HEAD").decode().strip()==pin["sha"],
           label+" actual detached original Git worktree")
    module=load_python(checkout/pin["pack"],"original_producer_"+label)
    blob=module.build()
    source=module.verify(blob)
    insist(len(blob)==pin["size"] and h(blob)==pin["sha256"],
           label+" ORIGINAL inner ZIP exact bytes and SHA256")
    with zipfile.ZipFile(__import__("io").BytesIO(blob)) as z:
     names=z.namelist()
     insist(len(names)==pin["members"] and len(names)==len(set(names)),
            label+" original ZIP exact members and no duplicate")
     insist(all(not n.startswith("/") and "\\" not in n and
               ".." not in Path(n).parts for n in names),
            label+" original ZIP no traversal")
     metas={n:read_meta(z.read(n)) for n in names if n.endswith(".meta")}
     insist(len(metas)==pin["guid_count"] and len(set(metas.values()))==len(metas),
            label+" original Unity GUID count and uniqueness")
     imported[label]={"members":len(names),"meta":metas,
                      "files":{n:z.read(n) for n in names if n!="PACKAGE_MANIFEST.json"}}
     receipts[label]={"sha":pin["sha"],"bytes":len(blob),
                      "sha256":h(blob),"entries":len(names),
                      "owner_unity_guid_count":len(metas),
                      "manifest":json.loads(z.read("PACKAGE_MANIFEST.json"))}
    target=tmp/("receiver-"+label)
    module.stage(blob,target)
    insist(target.is_dir(),"original "+label+" stage new disposable external receiver")
    neg(lambda:module.stage(blob,target),label+" refuse occupied live receiver")
    # Test safe rollback with clean original owner source, then re-stage.
    module.rollback(blob,target)
    if label=="R5":
     insist(not target.exists(),"original R5 clean exact byte rollback removes only owned scratch")
    else:
     insist(target.exists() and not any(target.iterdir()),
            "original API V2 clean rollback leaves only empty receiver")
     target.rmdir()
    module.stage(blob,target)
    # Poison exact owner file: neither packager may erase a modified user's file.
    f=next(target/n for n in imported[label]["files"] if n.endswith(".cs"))
    old=f.read_bytes()
    f.write_bytes(old+b"\n// deliberate test-only modification\n")
    neg(lambda:module.rollback(blob,target),
        label+" dirty owner source rollback denied, untouched alien data")
    insist(f.read_bytes()!=old,label+" modified file preserved on denied rollback")
    f.write_bytes(old)
    # Foreign empty path must not be recursively destroyed.
    foreign=target/"B0162_W22_FOREIGN_EMPTY_DIR"
    foreign.mkdir()
    if label=="R5":
     neg(lambda:module.rollback(blob,target),
         "R5 rollback refuses foreign EMPTY directory")
    else:
     module.rollback(blob,target)
     insist(foreign.is_dir(),
            "API V2 rollback preserves foreign EMPTY directory (does not claim rejection)")
    insist(foreign.exists(),"foreign EMPTY folder still exists after denied/partial rollback")
    # All temp receiver trees belong exclusively to this synthetic UUID context.
    shutil.rmtree(target)
    # Also refuse foreign *empty* receiver on staging.
    occupied=tmp/("foreign-empty-"+label)
    occupied.mkdir()
    neg(lambda:module.stage(blob,occupied),
        label+" original stage refuses foreign EMPTY receiver")
    insist(occupied.is_dir() and not any(occupied.iterdir()),
           label+" original preserves foreign EMPTY receiver")

   # Exact frozen source dependencies with their real .meta files.
   for label,prefix in SOURCE_PATHS.items():
    commit=SAVE if label=="save" else GAME
    files=paths(root,commit,prefix)
    metas={name:read_meta(full(root,commit,name))
           for name in files if name.endswith(".meta")}
    insist(len(metas)==GUID_COUNTS[label],
           label+" frozen root original GUID count")
    imported[label]={"files":{name:full(root,commit,name) for name in files},
                     "meta":metas,"members":len(files)}
   allguids={}
   for label,collection in imported.items():
    for f,guid in collection["meta"].items():
     if guid in allguids:
      raise AssertionError("cross-package Unity GUID COLLISION "+label+" "+f+
                           " vs "+allguids[guid])
     allguids[guid]=label+":"+f
   insist(len(allguids)==38 and len(set(allguids))==38,
          "CROSS-OWNER EXACT 14+5+4+11+4=38 UNIQUE Unity GUIDs")
   names=[]
   for label,collection in imported.items():
    names.extend(collection["files"].keys())
   insist(len(names)==len(set(names)),
          "game/W06/save/R5/API2 exact full Git paths do not overlap")
   # The two author ZIPs each ship their own root PACKAGE_MANIFEST.json.
   # Those MUST NOT both be installed at one project root: relocate receipts.
   with zipfile.ZipFile(__import__("io").BytesIO(
      load_python(worktrees[0]/PRODUCERS["R5"]["pack"],"reopened_r5").build())) as z:
    insist("PACKAGE_MANIFEST.json" in z.namelist(),
           "R5 manifest root collision requires separate intake receipt")
   with zipfile.ZipFile(__import__("io").BytesIO(
      load_python(worktrees[1]/PRODUCERS["API2"]["pack"],"reopened_api").build())) as z:
    insist("PACKAGE_MANIFEST.json" in z.namelist(),
           "API2 manifest root collision requires separate intake receipt")
   # Build ONE disposable combined receiver with exact immutable original Git bytes.
   # NEVER replace a pre-existing project, never touch actual main/Assets.
   joined=tmp/"RECEIVER_ONLY_TEST_38GUID"
   insist(not joined.exists(),"combined receiver is newly owned ephemeral path")
   joined.mkdir()
   for label,collection in imported.items():
    for rel,raw in collection["files"].items():
     dest=joined/rel
     if dest.exists():raise AssertionError("EXACT PATH COLLISION "+rel)
     dest.parent.mkdir(parents=True,exist_ok=True)
     with dest.open("xb") as stream:stream.write(raw)
   insist(all((joined/p).read_bytes()==raw
              for c in imported.values() for p,raw in c["files"].items()),
          "combined receiver exactly matches all five original Git source roots")
   guid_map={}
   for p in joined.rglob("*.meta"):
    g=read_meta(p.read_bytes())
    insist(g not in guid_map,"serialized combined physical GUID collision "+g)
    guid_map[g]=str(p.relative_to(joined))
   insist(len(guid_map)==38,"physical staged receiver has exact 38 owner GUIDs")
   # Owner manifest colocation: original ZIPs are different roots;
   # only an integrator could authorize a unified project import.
   approved_receipts=joined/"B0162_DISPOSABLE_ONLY_RECEIPTS"
   approved_receipts.mkdir()
   for label,receipt in receipts.items():
    (approved_receipts/(label+".json")).write_text(
      json.dumps(receipt,sort_keys=True,indent=2)+"\n",encoding="utf-8")
   insist(set(p.name for p in approved_receipts.iterdir())=={"R5.json","API2.json"},
          "two owner manifests remain distinct; never overwrite root package receipt")
   # Verify synthetic code symbols without pretending to compile real private Rig[].
   host=imported["R5"]["files"]["Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs"].decode()
   api=imported["API2"]["files"]["Assets/B0123CanonicalSaveApi001/Core/CanonicalWorldGateway.cs"].decode()
   insist("OwnerWorldLeaseRetirement.Hold" in host and
          "AllOwnedRootsDestroyed" in host and
          "CleanupUnpublishedAndVerifyDestroyed" in api,
          "original selected R5 lifecycle and API V2 owned-stage source anchors")
   insist(not (joined/"Assets/B0123Canonical510PrivateConcreteAdapter001").exists(),
          "NO false claim to canonical #510 private live Unity Rig adapter")
   summary={
    "package":"B01-62-P17W22-R5-API2-COMBINED-G0-SOURCE-001",
    "owner":"B01-62",
    "producers":receipts,
    "frozen_dependencies":{"game_510":GAME,"save_556":SAVE,"R4_647":R4,
                           "api_v1_681":API1,"slotstore_669":STORE,
                           "slot_session_r2_712":SESSION},
    "guid_counts":GUID_COUNTS,
    "combined_unity_guids":len(guid_map),
    "combined_source_root_paths":len(names),
    "manifest_root_conflict":"two ZIP roots both PACKAGE_MANIFEST.json; separate receipts required",
    "source_transport_g0":"PASS_IF_ALL_ASSERTIONS_PASS",
    "host_r5_security_g0":"PENDING_OWNER_INDEPENDENT_QA",
    "canonical_game_private_510_live_adapter":"NOT_IMPLEMENTED",
    "native_unity_g1":"NOT_RUN",
    "codex_ack":False,"integrated":False,"released":False,
    "safe_migration":"DISPOSABLE_SCRATCH_ONLY; no project install",
   }
   data=json.dumps(summary,indent=2,sort_keys=True)+"\n"
   yield data
  finally:
   for w in reversed(worktrees):
    git(root,"worktree","remove","--force",str(w))

def main():
 import argparse
 a=argparse.ArgumentParser()
 a.add_argument("--repo",required=True)
 a.add_argument("--receipt",required=True)
 v=a.parse_args()
 path=Path(v.receipt)
 if path.exists() or not path.parent.is_dir():
  raise ValueError("refuse existing/foreign physical receipt")
 # We only generate evidence at the VERY END of all original worktree tests.
 final=next(run(Path(v.repo).resolve()))
 path.write_text(final,encoding="utf-8")
 print("B0162_W22_COMBINED_SOURCE_G0_CHECKS_PASS="+str(checks))
 print("B0162_W22_CROSS_OWNER_GUIDS_UNIQUE=38/38")
 print("B0162_W22_ORIGINAL_R5_INNER_ZIP_SHA256="+PRODUCERS["R5"]["sha256"])
 print("B0162_W22_ORIGINAL_API2_INNER_ZIP_SHA256="+PRODUCERS["API2"]["sha256"])
 print("B0162_W22_COMBINED_G0=SOURCE_TRANSPORT_PASS_ONLY")
 print("B0162_W22_HOST_SECURITY_G0=PENDING")
 print("B0162_W22_REAL_UNITY_G1=NOT_RUN")
 print("B0162_W22_PRIVATE_510_CANONICAL_SAVE=HOLD G2=HOLD CODEX_ACK=NONE")
if __name__=="__main__":
 main()
