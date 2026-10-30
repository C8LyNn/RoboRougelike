#!/usr/bin/env python3
"""B01-00 W23: simulated one-authority migration contract, NEVER product Save/Load.

Reference-model transitions support integration review. No actual OS lock, Unity,
frozen private Rig[], trusted highwater root, physical destruction, or fsync.
Do not extrapolate unit PASS into owner implementation or G0/G1 release.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

SCHEMA="b01-w23-single-authority-migration-model-r1"
SOURCES={
 "host":{"pr":690,"sha":"cc7f73541517a18f4479bb155534bd4832d9985e","physical_g0":"ACCEPTED_SOURCE_ONLY","native":"NOT_RUN"},
 "canonical_v3":{"pr":729,"sha":"b8ca29c7707cd6b4338d0b9772d74dee676d49d2","producer":"PASS_152_SOURCE","physical_g0":"PENDING","native":"NOT_RUN"},
 "session_r2":{"pr":712,"sha":"bd8905ad549a7b26854b671be971f7d781308311","independent_707":"PASS_SCOPED_OLD_V2_ONLY","native":"NOT_RUN"},
 "store":{"pr":669,"sha":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121","external_rollback":"UNRESOLVED_680"},
 "game":{"pr":510,"sha":"d94400d95d13db7f27e1533df05c9292d5380431","private_port":"NOT_IMPLEMENTED"},
 "codec":{"pr":556,"sha":"33094f154c7b91119b36d58aab285e6e83c5de57","immutable":True},
 "cold_restart":{"pr":690,"sha":"cc7f73541517a18f4479bb155534bd4832d9985e","actual_generation":5,"asserted_generation":4,"fixture":"RED_718"},
 "runner":{"pr":711,"sha":"02272e8b0facc00d3943672770251b1d01182b85","native":"NOT_RUN"}
}
ALTERNATIVES=[719,695,693,682,652,644,604]
RECEIVERS={
 "one_lock":"B01-23/62",
 "native_private_port":"B01-23/20/62",
 "independent_security":"B01-61/41",
 "physical_source_g0":"B01-40/62",
 "native_g1":"B01-60+LOCAL_OPERATOR",
 "cold_restart_fixture":"B01-23",
 "trusted_rollback":"B01-62+OWNER",
 "eventual_integrator":"B01-00+OWNER+CODEX_LATER"
}
# Strict input intentionally remains historical even when other branches move.
RISK={
 "sole_selected_v3":729,"loser_v3":719,"loser_status":"ALT_HOLD",
 "frozen_host":690,"host_alt":693,
 "canonical_bridge_pr":None,"bridge_git_sha":None,
 "same_os_lock":"NOT_IMPLEMENTED",
 "constructor_v3_r2_compiles":"NOT_RUN",
 "lease_layout_legacy":[".owner",".lease"],
 "owned_host_world_binding":"NOT_IMPLEMENTED",
 "trusted_external_rollback":"UNRESOLVED",
 "cold_restart_corrected_new_pr":None,
 "independent_v3_qa":"NOT_RUN",
 "combined_source_g0":"HOLD",
 "unity_g1":"NOT_RUN","codex_ack":None,
 "integrator_feedback":"NOT_MEASURED","merged":False}
class Rejected(ValueError):pass
def need(v,msg):
 if not v:raise Rejected(msg)
def validate_receipt(d):
 need(type(d) is dict and d.get("schema")==SCHEMA,"schema")
 for k,reference in [("original_sources",SOURCES),("alternatives_hold",ALTERNATIVES),
                     ("receivers",RECEIVERS),("observed_risk",RISK)]:
  need(d.get(k)==reference,"W23 immutable Git receipt differs: "+k)
 need(d.get("source_only_handoff") is True,"not a release receipt")
 need(d.get("tests_are_reference_model_only") is True,"not physical C# or Unity")
 need(d.get("user_pc_access") is False,"user PC is not connected")
 need(d.get("native_nunit_xml") is None and d.get("native_editor_log") is None,
      "no fabricated native result")
 need(d.get("human_economy_trials")==0,"no invented playtest")
 return {"schema":SCHEMA,
  "verdict":"HOLD_V3_729_TO_SESSION_712_DUAL_LOCK_NOT_INTEGRATED",
  "selected_api":729,"alternative_v3":719,
  "generation_written_by_r5":5,"old_fixture_expected_generation":4,
  "cold_restart_fixture":"CORRECTION_REQUIRED_NEW_OWNER_SHA",
  "single_owner":"NOT_IMPLEMENTED","mixed_lock_paths":True,
  "session_constructor":"SOURCE_INCOMPATIBLE_PENDING_NEW_BRIDGE",
  "independent_new_v3_security":"NOT_RUN","combined_g0":"HOLD",
  "native_g1":"NOT_RUN","live_private_510_save":"HOLD",
  "external_valid_backup_rollback":"UNRESOLVED_680",
  "codex_ack":"NONE","g2":"HOLD","merged":False,
  "actual_integrator_latency":None,"integrator_feedback":"NOT_MEASURED",
  "stage":{"IMPLEMENTED":True,"TESTED":False,
           "QA_ACCEPTED":False,"READY_FOR_INTEGRATOR":False,
           "INTEGRATED":False,"RELEASED":False},
  "next_receivers":RECEIVERS}

class OneAuthorityModel:
 """In-memory single-lock reference state, NOT actual FileStream or Unity."""
 def __init__(self,run=52,slot="slot-A",lock_path=".authority"):
  need(type(run) is int and run>0,"positive run")
  need(isinstance(slot,str) and slot!="","nonempty slot")
  need(lock_path==".authority","one canonical lock path (no owner+lease)")
  self.run=run
  self.slot=slot
  self.lock_path=lock_path
  self.owner=None
  self.epoch=0
  self.checksum=None
  self.live=False
  self.partial=False
  self.quarantine=False
  self.ack_uncertain=False
  self.physical_retirement_proven=False

 def acquire(self,actor,run=None,slot=None,other_lock=None):
  need(other_lock is None,"parallel .lease/.owner forbidden")
  need(isinstance(actor,str) and bool(actor),"valid owner")
  need(self.run==(run if run is not None else self.run),"foreign run")
  need(self.slot==(slot if slot is not None else self.slot),"foreign slot")
  need(self.owner is None,"BUSY: owner retained")
  need(not (self.live or self.partial or self.quarantine or self.ack_uncertain),
       "unretired world or uncertain ACK")
  self.owner=actor
  self.physical_retirement_proven=False
  return True

 def stage(self,actor,partial=False):
  self._owns(actor)
  need(not self.quarantine and not self.ack_uncertain,"quarantined")
  need(not self.partial,"staged already")
  self.partial=True
  if not partial:self.live=True
  return True

 def publish(self,actor):
  self._owns(actor)
  need(self.partial and self.live and not self.quarantine,
       "publish only fully staged owned world")
  self.partial=False
  return True

 def cleanup(self,actor,retired):
  self._owns(actor)
  if retired is not True:
   self.quarantine=True
   return False
  self.partial=False
  self.live=False
  self.quarantine=False
  self.physical_retirement_proven=True
  return True

 def _owns(self,actor):
  need(self.owner==actor and actor is not None,"not unique authority")

 def commit(self,actor,generation,digest,ack_lost=False):
  self._owns(actor)
  need(not self.quarantine and not self.ack_uncertain,"quarantine/uncertain")
  need(self.live and not self.partial,"must have published scene, no staged root")
  need(type(generation) is int and generation==self.epoch+1,"monotonic epoch")
  need(isinstance(digest,str) and len(digest)==64 and
       all(ch in "0123456789abcdef" for ch in digest),
       "original payload digest shape")
  self.epoch=generation
  self.checksum=digest
  if ack_lost:
   self.ack_uncertain=True
   self.quarantine=True
  return "ACK_UNKNOWN" if ack_lost else "COMMITTED"

 def preview(self,actor,on_disk_epoch,on_disk_digest):
  self._owns(actor)
  if (self.quarantine or self.ack_uncertain or self.epoch!=on_disk_epoch
      or self.checksum!=on_disk_digest):
   self.quarantine=True
   raise Rejected("stale preview or uncertain durable authority")
  need(self.live,"no published world")
  return self.epoch

 def release(self,actor,physical_retired=False,readback_ok=True):
  self._owns(actor)
  if (physical_retired is not True or readback_ok is not True
      or self.live or self.partial or self.ack_uncertain):
   self.quarantine=True
   raise Rejected("cannot release unretired/uncertain authority")
  self.physical_retirement_proven=True
  self.quarantine=False
  self.owner=None
  return True

 def cold_restart(self,new_actor,on_disk_epoch,on_disk_digest):
  need(self.owner is None and not self.live and not self.partial
       and not self.quarantine and not self.ack_uncertain,
       "cold restart while owner or quarantine active")
  need(on_disk_epoch==self.epoch and on_disk_digest==self.checksum,
       "cold readback differs")
  return self.acquire(new_actor)

def main(argv=None):
 p=argparse.ArgumentParser(description=__doc__)
 p.add_argument("receipt",type=Path)
 p.add_argument("--require-ready",action="store_true")
 p.add_argument("--output",type=Path)
 a=p.parse_args(argv)
 try:
  b=a.receipt.read_bytes()
  result=validate_receipt(json.loads(b.decode("utf-8")))
  result["receipt_sha256"]=hashlib.sha256(b).hexdigest()
  msg=json.dumps(result,indent=2,sort_keys=True)+"\n"
  if a.output is not None:
   need(a.output.resolve()!=a.receipt.resolve() and not a.output.exists(),
        "write once only; never overwrite input/foreign evidence")
   with a.output.open("x",encoding="utf-8") as f:f.write(msg)
  else:print(msg,end="")
  return 3 if a.require_ready else 0
 except (ValueError,TypeError,OSError,UnicodeError) as e:
  print("W23_REJECT:"+str(e),file=sys.stderr)
  return 2
if __name__=="__main__":raise SystemExit(main())
