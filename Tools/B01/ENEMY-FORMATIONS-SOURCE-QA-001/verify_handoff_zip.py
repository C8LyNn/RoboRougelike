#!/usr/bin/env python3
"""B01-60 independent *physical* G0 inspection of B01-40 #398 handoff ZIP.
Executes exact pinned original B01-40 builder to create the real ZIP, then
independently reads bytes/entries/CRC/manifest and detects corrupted samples.
No canonical Unity/producer/receiver source is edited or imported.
"""
from __future__ import annotations
import argparse, hashlib, io, json, os, re, subprocess, sys, tempfile, zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
RECEIVER="f4165fd2bac86741326253800c6160d8ca8bea02"
OWNER="c39a400cb07ae0fd6f0d8d419a128490cfbcfd8a"
CODEX="203a0805251ed28293888e43f2d49f0df63767c6"
EXPECTED="ef25e8759a02a9b5f0c22d19198e6fcbf8e6c9d13ee7963c7d1fa2f091d82d8a"
EXACT_SCRIPT_BLOB="5d66657a6216340513e14cde9ce8f6bc0b7fd16e"
PATH="Tools/B01Validation/P17W03/receive_formations.py"
NAMES={
 "binding/optional-spawn-arguments.json",
 "intake-receipt.json",
 "previews/B0124-SCRAP-PINCER-four.svg",
 "previews/B0124-WASTE-CHASE-four.svg",
 "previews/B0124-INDUSTRIAL-CROSSFIRE-four.svg",
 "source/formations.json",
}
def require(ok,why):
 if not ok:raise AssertionError(why)
def git(*cmd):
 return subprocess.check_output(["git","-C",str(ROOT),*cmd],stderr=subprocess.STDOUT)
def sha(b):return hashlib.sha256(b).hexdigest()
def reject_zip(raw,reason,mutate):
 try:
  mutate(raw)
  raise AssertionError(reason+"_NOT_REJECTED")
 except (AssertionError,ValueError,KeyError,zipfile.BadZipFile) as ex:
  if str(ex)==reason+"_NOT_REJECTED":raise
def verify_zip(raw):
 require(sha(raw)==EXPECTED,"ZIP_SHA_DRIFT")
 with zipfile.ZipFile(io.BytesIO(raw),"r") as z:
  names=z.namelist()
  require(len(names)==6 and set(names)==NAMES and names==sorted(names),"ZIP_MEMBER_SET")
  require(len(names)==len(set(names)),"ZIP_DUPLICATES")
  for i in z.infolist():
   require(not i.filename.startswith("/") and "\\" not in i.filename and
           ".." not in Path(i.filename).parts,"ZIP_TRAVERSAL")
   require(i.date_time==(1980,1,1,0,0,0) and
           i.external_attr>>16==0o100644 and i.compress_type==zipfile.ZIP_STORED,
           "ZIP_ENTRY_METADATA")
  b={n:z.read(n) for n in names}  # also validates CRC32
 return b
def verify_members(b,strict_hash=True):
 receipt=json.loads(b["intake-receipt.json"])
 manifest=receipt["receipt_members_sha256"]
 require(set(manifest)==NAMES-{"intake-receipt.json"},"RECEIPT_MEMBER_SET")
 for n in manifest:
  require(sha(b[n])==manifest[n],"MEMBER_HASH "+n)
 require(receipt["immutable_source_sha"]==OWNER and receipt["latest_codex_gameplay_sha"]==CODEX,"RECEIPT_SOURCE_DRIFT")
 require(receipt["G0"]=="B01_40_SOURCE_G0_PASS" and receipt["G1"]=="NOT_RUN"
         and receipt["G2"]=="HOLD" and receipt["codex_ack"]=="PENDING"
         and receipt["game_integrated"] is False,"FALSE_G1_OR_CODEX_ACK")
 source=git("show",OWNER+":Research/B01-24/P17/ENEMY-FORMATIONS-002/formations.json")
 require(b["source/formations.json"]==source,"ORIGINAL_SOURCE_BYTES_CHANGED")
 data=json.loads(source)
 vectors=json.loads(b["binding/optional-spawn-arguments.json"])
 require(vectors["source_sha"]==OWNER and vectors["codex_gameplay_sha"]==CODEX
         and vectors["native_unity"]=="NOT_RUN" and vectors["installed"] is False,
         "BINDING_PROVENANCE")
 calls=vectors["calls"]
 require(len(calls)==12 and sum(len(row["world_xz"]) for row in calls)==30,"BINDING_ROWS")
 mapped={(a["arena"],a["formation_id"],a["spawn_count"]):a for a in calls}
 require(len(mapped)==12,"DUPLICATE_FORMATION_OR_COUNT")
 count=0
 for f in data["formations"]:
  for n in (1,2,3,4):
   row=mapped[(f["arena"],f["id"],n)]
   coords=f["options"][str(n)]
   require(row["world_xz"]==coords,"BINDING_COORDINATE_MISMATCH")
   def fl(v):return (str(v)+".0" if isinstance(v,int) else str(v))+"f"
   expression="new Vector3[] { "+", ".join(
        "new Vector3("+fl(x)+", 0.0f, "+fl(z)+")" for x,z in coords)+" }"
   require(row["vector3_argument"]==expression,"C_SHARP_VECTOR3_BINDING_MISMATCH")
   count+=len(coords)
 require(count==30,"SPAWN_COUNT_WRONG")
 for n in NAMES:
  if n.endswith(".svg"):
   require(b[n].count(b"<svg")==1 and b[n].count(b"<circle")>=5 and b"</svg>" in b[n],"SVG_INVALID")
 return {"original_source_bytes":len(source),"source_blob":git("rev-parse",OWNER+":Research/B01-24/P17/ENEMY-FORMATIONS-002/formations.json").decode().strip(),
         "zip_entries":len(b),"bindings":len(calls),"spawn_positions":count,"receipt_members":len(manifest)}
def independent_negatives(b):
 cases=[]
 def negative(name,mutation,expected):
  altered={k:v for k,v in b.items()}
  mutation(altered)
  try:
   verify_members(altered)
   raise AssertionError("FALSE_ACCEPT_"+name)
  except AssertionError as e:
   require(str(e)==expected,name+" WRONG_ERROR "+str(e))
  cases.append(name)
 negative("corrupt_original_JSON",lambda d:d.__setitem__("source/formations.json",b'{}'),"MEMBER_HASH source/formations.json")
 negative("corrupt_binding",lambda d:d.__setitem__("binding/optional-spawn-arguments.json",b'[]'),"MEMBER_HASH binding/optional-spawn-arguments.json")
 negative("corrupt_svg",lambda d:d.__setitem__("previews/B0124-SCRAP-PINCER-four.svg",b'<svg/>'),"MEMBER_HASH previews/B0124-SCRAP-PINCER-four.svg")
 def change_receipt(d,part,value):
  o=json.loads(d["intake-receipt.json"])
  o[part]=value
  d["intake-receipt.json"]=json.dumps(o).encode()
 negative("forged_native_G1",lambda d:change_receipt(d,"G1","PASS"),"FALSE_G1_OR_CODEX_ACK")
 negative("forged_codex_ack",lambda d:change_receipt(d,"codex_ack","ACKNOWLEDGED"),"FALSE_G1_OR_CODEX_ACK")
 negative("wrong_original_SHA",lambda d:change_receipt(d,"immutable_source_sha","0"*40),"RECEIPT_SOURCE_DRIFT")
 negative("swapped_source_ZIP",lambda d:d.__setitem__("source/formations.json",b'not original'),"MEMBER_HASH source/formations.json")
 def forge_binding(d):
  o=json.loads(d["binding/optional-spawn-arguments.json"])
  o["calls"][0]["world_xz"]=[[999,999]]
  new=json.dumps(o).encode()
  receipt=json.loads(d["intake-receipt.json"])
  receipt["receipt_members_sha256"]["binding/optional-spawn-arguments.json"]=sha(new)
  d["binding/optional-spawn-arguments.json"]=new
  d["intake-receipt.json"]=json.dumps(receipt).encode()
 negative("forged_matching_hash_but_wrong_coords",forge_binding,"BINDING_COORDINATE_MISMATCH")
 return cases

def main():
 a=argparse.ArgumentParser()
 a.add_argument("--output",required=True)
 args=a.parse_args()
 out=Path(args.output).resolve()
 out.parent.mkdir(parents=True,exist_ok=True)
 require(git("rev-parse",RECEIVER+":"+PATH).decode().strip()==EXACT_SCRIPT_BLOB,"RECEIVER_IMPLEMENTATION_DRIFT")
 with tempfile.TemporaryDirectory(prefix="b0160_w04_") as d:
  tmp=Path(d)
  script=tmp/"receive_formations.py"
  script.write_bytes(git("show",RECEIVER+":"+PATH))
  target=tmp/"physical"
  proc=subprocess.run([sys.executable,str(script),"--repo",str(ROOT),"--out",str(target)],
      capture_output=True,text=True,timeout=300)
  require(proc.returncode==0,"B0140_RUN_FAILED "+(proc.stdout+proc.stderr)[-800:])
  binary=(target/"B01-40-W03-Codex-EnemyFormations.zip").read_bytes()
  archive=verify_zip(binary)
  checked=verify_members(archive)
  neg=independent_negatives(archive)
  outputReceipt=json.loads((target/"receiver-receipt.json").read_bytes())
  queue=json.loads((target/"codex-queue.json").read_bytes())
  require(outputReceipt["archive_sha256"]==EXPECTED and outputReceipt["entry_count"]==6,"OUTER_RECEIPT_WRONG")
  require(queue["ordered"][0]["pr"]==355 and
          queue["ordered"][0]["status"]=="G0_PASS_INDEPENDENT_B01_60_QA_PENDING" and
          queue["ordered"][1]["pr"]==333 and
          queue["ordered"][1]["status"]=="HOLD_CODEX_R3_BYTE_DRIFT" and
          all(x["codex_ack"]=="PENDING" for x in queue["ordered"]),
          "INVALID_HQ_QUEUE_GATES")
  report={"package":"B01-60-ENEMY-FORMATIONS-SOURCE-QA-001 W04-R2",
          "producer_355_sha":OWNER,"receiver_398_sha":RECEIVER,
          "codex_gameplay_sha":CODEX,"physical_zip_sha256":sha(binary),
          "original_receipt":"INDEPENDENTLY_REBUILT",
          "zip_and_original_source":checked,
          "mutation_rejections":len(neg),"mutation_case_names":neg,
          "independent_QA":"G0_PHYSICAL_SOURCE_PASS",
          "native_unity":"NOT_RUN","physx":"NOT_RUN","codex_ack":"PENDING",
          "b01_00_handoff":"PENDING","game_integrated":False,"merge":False}
  out.write_text(json.dumps(report,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
  print(json.dumps(report,ensure_ascii=False),flush=True)
  return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W04_FAIL "+str(e),file=sys.stderr)
  raise
