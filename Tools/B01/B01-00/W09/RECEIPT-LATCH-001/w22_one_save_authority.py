#!/usr/bin/env python3
"""W22 B01-00 original executable *read-only* release/receiver decision gate.

A source-only consistency receipt is not a GitHub signature, a native Unity test,
or authorization to merge. Never authorize release on producer CI alone.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

SCHEMA="b01-w22-unique-save-release-ledger-r1"
SHA={
"game510":"d94400d95d13db7f27e1533df05c9292d5380431",
"save556":"33094f154c7b91119b36d58aab285e6e83c5de57",
"r4_647":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
"r5_selected_690":"cc7f73541517a18f4479bb155534bd4832d9985e",
"r5_alt_693":"f30f45200896a012e8b059e8d031b731d6f25778",
"api_v1_681":"eed2e39636edfd8b8db754c426db452164cbf9d7",
"api_v2_selected_691":"809f82575c638e1b4d2806b5f417bddab6c0871a",
"api_alt_682":"5387e829a5f6d38b713086e6fb906a9a755189ff",
"api_alt_695":"e190fac795c2613d33d48ec58a60b5df1b827141",
"storage_669":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
"session_old_697":"ab62b041e50f8aa0defada2aadc0987d6c63f9d9",
"session_new_712":"bd8905ad549a7b26854b671be971f7d781308311",
"external_anchor_709":"1dd60f622217889b0d4cc7dc315cf6331c12545c",
"windows_711":"02272e8b0facc00d3943672770251b1d01182b85"
}
ALTS={"r5":[693],"api":[682,695],"r4":[652],"r3":[644],"r2":[604]}
OWNER={"r5":690,"api":691,"session":712,"storage":669,"runner":711}
# Distinct source facts deliberately cannot grant full release or native approval.
FACTS={
"r5_690":{
"producer_run":41120562896,"producer_source":"PASS_BOTH_OS_270_COMBINED",
"independent_pr":705,"independent_sha":"0d07173f38ac4b332d6c826e434de1f53205107f",
"independent_run":41125719017,"independent":"GREEN_SOURCE_SYNTHETIC_UNITY_25_EACH_OS",
"physical_pr":699,"physical_sha":"3ff32424092a003484b8f05cc39d49390eee3195",
"physical":"G0_ACCEPTED_SOURCE_ONLY","full_g0":"HOLD_NOT_LIVE_SCENE",
"native":"NOT_RUN","zip_sha256":"a37e942d6a0cc5236570edaf7cba49384a84d0553f88fb29988a855d692a1496",
"zip_bytes":129088,"zip_members":27,
"receiver":"B01-23/61/41/40/62",
"next":"IND_INTEGRATED_HOST_SCENE_RELEASE_AND_REAL_UNITY"},
"api_691":{
"producer_run":41121167581,"producer_source":"PASS_SOURCE_BOTH_OS",
"independent_pr":710,"independent_sha":"cf6718ddd04683f0e633c3580818431245b2c105",
"independent_run":41126448602,"independent":"P0_RED_TWO_ACTIVE_SAME_SLOT_AND_STALE_PREVIEW_9_EACH_OS",
"physical_pr":704,"physical_sha":"3cd990c1fabe7c01d226f40f3b8faa6de59e9819",
"physical":"G0_ACCEPTED_SOURCE_ONLY","full_g0":"HOLD_P0_RED",
"native":"NOT_RUN","zip_sha256":"90198e6429d912a40e4e2df3935281627282f14799c4f34479439af150f5bccd",
"zip_bytes":61736,"zip_members":15,
"receiver":"B01-23/20/62+61/41",
"next":"NEW_VERSIONED_EXCLUSIVE_LIFETIME_SLOT_EPOCH_FENCE_FOR_RESTORE_PREVIEW"},
"session_712":{
"producer_run":41127040969,"producer_source":"PASS_36_EACH_OS",
"independent_pr":707,"independent_sha":"b3c5b1628ef017147f8138c7049f5dd5f2cc730b",
"independent_run":None,
"independent":"HISTORIC_697_P0_RED_NEW_712_EXACT_QA_NOT_RUN",
"physical_pr":None,"physical_sha":None,
"physical":"PENDING_NEW_SHA","full_g0":"PENDING_INDEPENDENT",
"native":"NOT_RUN","zip_sha256":None,"zip_bytes":None,"zip_members":None,
"receiver":"B01-62+61/41+40",
"next":"INDEPENDENT_EXACT_712_707_CROSS_PROCESS_REPLAY+PHYSICAL_RETIREMENT_PROOF"},
"storage_669":{
"producer_run":None,"producer_source":"PASS_ORIGINAL_OS_SOURCE",
"independent_pr":680,"independent_sha":"a8e7b68e94f3454d998e732c52ae8eb42c850042",
"independent_run":None,
"independent":"TRUST_RED_VALID_OLD_ENVELOPE_AND_EQUAL_GEN_LEDGER_FORK",
"physical_pr":None,"physical_sha":None,
"physical":"SOURCE_ONLY","full_g0":"HOLD_TRUST_POLICY",
"native":"NOT_RUN","zip_sha256":None,"zip_bytes":None,"zip_members":None,
"receiver":"B01-62+61/41+40",
"next":"TRUSTED_MONOTONIC_EXTERNAL_ROOT_OR_EXPLICIT_BACKUP_THREAT_BOUNDARY_709"},
"windows_711":{
"producer_run":41127568270,"producer_source":"PASS_REAL_WINDOWS_POWERSHELL_SOURCE_ONLY",
"independent_pr":None,"independent_sha":None,"independent_run":None,
"independent":"SOURCE_HARNESS_ONLY",
"physical_pr":None,"physical_sha":None,
"physical":"DISPOSABLE_SCRATCH_ONLY","full_g0":"NOT_APPLICABLE",
"native":"NOT_RUN","zip_sha256":None,"zip_bytes":None,"zip_members":None,
"receiver":"B01-60+LOCAL_OPERATOR+41",
"next":"OPERATOR_MANUALLY_RUN_LICENSED_UNITY_6000_3_14F1_AND_REDACT_XML_LOG"}
}
DEPENDENCIES={
"r5_690":[],
"api_691":["r5_690"],
"storage_669":[],
"session_712":["api_691","storage_669"],
"windows_711":[],
"real_game_adapter":["r5_690","api_691","session_712"],
"codex_integrator":["real_game_adapter","windows_711"]
}
NEEDS={
"r5_690":["NATIVE_R5_GAME_SCENE_QA","FULL_HOST_OWNER_G0_APPROVAL"],
"api_691":["NEW_EXCLUSIVE_SESSION_FENCE_710","STALE_PREVIEW_REJECT","NEW_SHA_INDEPENDENT_710_QA"],
"storage_669":["EXTERNAL_BACKUP_VALID_ROLLBACK_POLICY_680","TRUSTED_ANCHOR_709_REVIEW_OR_SCOPE"],
"session_712":["RETEST_707_ON_EXACT_712_SHA","PHYSICAL_PARTIAL_OWNER_RETIREMENT_CONFIRM","NEW_SHA_FORMAL_G0"],
"windows_711":["OWNER_OPERATOR_OPT_IN_ACTIVATED_UNITY","GENUINE_NUNIT_EDITOR_LOG_SHA"],
"real_game_adapter":["NEW_VERSIONED_REAL_510_PRIVATE_RIG_LEDGER_WORLD_PORT","END_TO_END_CRASH_COLD_RESTORE_AND_STABLE_IDS"],
"codex_integrator":["OWNER_AUTHORIZED_RECEIPT_AND_ACK","NO_UNRESOLVED_P0_AND_SIGNED_G0_G1"]}
LIFECYCLE={"IMPLEMENTED":True,"TESTED":True,"QA_ACCEPTED":False,
"READY_FOR_INTEGRATOR":False,"INTEGRATED":False,"RELEASED":False}
ACTUAL={"codex_ack":None,"codex_outcome":"NOT_MEASURED","elapsed_seconds":None,
"attempts":None,"regressions":None,"native_unity":"NOT_RUN",
"live_game_save":"HOLD","g2":"HOLD","merged":False,
"operator_pc_access":False,"credential_access":False}
class Unsafe(ValueError):pass
def insist(condition,reason):
 if not condition:raise Unsafe(reason)
def validate(data):
 insist(type(data) is dict and data.get("schema")==SCHEMA,"schema")
 for key,expected in [("git_sha",SHA),("alternatives_hold",ALTS),
    ("selected_owners",OWNER),("source_evidence",FACTS),
    ("dependencies",DEPENDENCIES),("requirements",NEEDS),
    ("gates",ACTUAL),("lifecycle",LIFECYCLE)]:
  insist(data.get(key)==expected,"W22 reviewed source snapshot mismatch: "+key)
 insist(data.get("owner_intake_permission")=="SOURCE_ONLY_NOT_INSTALL","owner permission")
 insist(data.get("external_anchor_709")=="OPTIONAL_NOT_TRUST_CERTIFIED","witness authenticity")
 insist(data.get("economy")=="PROVISIONAL_NOT_HUMAN_TESTED","no fabricated playtesting")
 # DAG is explicitly validated and transitive blockers propagated.
 pending=set(DEPENDENCIES)
 ordered=[]
 while pending:
  ready=sorted(k for k in pending if set(DEPENDENCIES[k]).issubset(set(ordered)))
  insist(bool(ready),"dependency cycle/missing node")
  ordered.extend(ready)
  pending.difference_update(ready)
 inherited={}
 stages=[]
 for k in ordered:
  blockers=set(NEEDS[k])
  for parent in DEPENDENCIES[k]:blockers.update(inherited[parent])
  inherited[k]=sorted(blockers)
  evidence=FACTS.get(k)
  stages.append({"stage":k,"pr":({"r5_690":690,"api_691":691,"storage_669":669,
        "session_712":712,"windows_711":711}).get(k),
    "independent_source":evidence["independent"] if evidence else "NOT_IMPLEMENTED",
    "physical_g0":evidence["physical"] if evidence else "NOT_RUN",
    "native_g1":"NOT_RUN","recipient":evidence["receiver"] if evidence else
      ("B01-23/20/62+40/61" if k=="real_game_adapter" else "B01-00+OWNER+CODEX_LATER"),
    "all_blockers":sorted(blockers),"ready":False})
 return {"schema":SCHEMA,"verdict":"HOLD_W22_ONE_SAVE_AUTHORITY_NOT_RELEASED",
  "selected":{"host":690,"api":691,"session":712},
  "alternative_prs":[693,695,682,652,644,604],
  "source_g0_host":"ACCEPTED_SOURCE_ONLY",
  "source_g0_api":"ACCEPTED_SOURCE_ONLY",
  "session_712_independent":"NOT_RUN",
  "api_691_release":"P0_RED_MULTI_OWNER",
  "native_unity_g1":"NOT_RUN","game_save":"HOLD","g2":"HOLD",
  "codex_ack":"NONE","integrator_metrics":"NOT_MEASURED",
  "acceptance_ratio":None,"elapsed_s":None,
  "dependency_order":ordered,"stages":stages,
  "critical_blockers":inherited["codex_integrator"],
  "critical_blockers_count":len(inherited["codex_integrator"]),
  "no_merge":True}
def main(argv=None):
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument("input",type=Path)
 parser.add_argument("--strict-release",action="store_true")
 parser.add_argument("--output",type=Path)
 arg=parser.parse_args(argv)
 try:
  raw=arg.input.read_bytes()
  ans=validate(json.loads(raw.decode("utf-8")))
  ans["input_sha256"]=hashlib.sha256(raw).hexdigest()
  text=json.dumps(ans,indent=2,sort_keys=True)+"\n"
  if arg.output is None:print(text,end="")
  else:
   insist(arg.output.resolve()!=arg.input.resolve() and not arg.output.exists(),
          "refuse existing file / foreign evidence overwrite")
   with arg.output.open("x",encoding="utf-8") as f:f.write(text)
  return 3 if arg.strict_release else 0
 except (ValueError,TypeError,OSError,UnicodeError) as exc:
  print("W22_REFUSED:"+str(exc),file=sys.stderr)
  return 2
if __name__=="__main__":
 sys.exit(main())
