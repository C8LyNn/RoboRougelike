#!/usr/bin/env python3
"""B01-00 W20: owner-only R4 selection / canonical API / Windows G1 admission.

Offline original-source evidence *schema* verifier. This does NOT authenticate
GitHub signatures, execute producer binaries, or prove native Unity/PhysX.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

SCHEMA="b01-w20-r4-api-windows-intake-v1"
PARENTS={
  "game510":"d94400d95d13db7f27e1533df05c9292d5380431",
  "save556":"33094f154c7b91119b36d58aab285e6e83c5de57",
  "host596":"efb2b06eafedab5469b57f163152d96aca3c5915",
  "r3_642":"22cec9888047994ae523cbe7bf1b0a483d64cec4",
  "r3_644":"ALT_HOLD","r2_604":"ALT_HOLD"}
R4={
647:{"sha":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
 "source_ci":41110483164,"physical_zip_sha256":"9ae2919e9cfdea1bf75f785b1cdfc2d3465a6a90c7d18ff22def69cb53406d30",
 "physical_zip_bytes":105099,"physical_zip_members":23,
 "independent_security":{"pr":664,"sha":"398b160cec2918be21ed29dbb1dfcef833198b81",
 "run":41112337448,"cases_each_os":46,"scope":"DIRECT_ORIGINAL_CORE_PLUS_SYNTHETIC_UNITY_HOST"},
 "independent_receiver":{"pr":662,"sha":"ccc1d047ead4d9dfe578dba89d4b4ef96f680efd",
 "run":41112235790,"cases_each_os":97,"scope":"PHYSICAL_GIT_ZIP_AND_DIRECT_DISK_CORE"},
 "lease_lifecycle":"RETAIN_UNTIL_ONDESTROY_SOURCE_GREEN"},
652:{"sha":"ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419",
 "source_ci":41110778985,"physical_zip_sha256":"cf9412e2f34fadeb75e028387eb5869c4444c1b9e8a3b88e7c6f40bba7f37f9e",
 "physical_zip_bytes":111519,"physical_zip_members":25,
 "independent_security":None,"independent_receiver":None,
 "lease_lifecycle":"ONDISABLE_RELEASE_SOURCE_RISK_NOT_NATIVE_PROVEN"}
}
OWNER={"authority":"B01-23","decision":"PENDING_FINAL_EXCLUSIVE_R4",
       "winner_pr":None,"loser_pr":None,"comment_url":None}
API={"issue":666,"authority":"OWNER_APPROVED_NEW_VERSIONED_IMPLEMENTATION",
     "source_pr":None,"source_sha":None,"implemented":False,
     "independent_qa":"NOT_RUN","captured_private_rig_ledger":False,
     "atomic_single_slot_recovery":"NOT_IMPLEMENTED","canonical_game_integrated":False}
WINDOWS={"issue":667,"lead":"B01-60","local_operator_opt_in":False,
         "runner_pr":None,"runner_sha":None,"source_review":"PENDING",
         "exact_editor_version":"6000.3.14f1",
         "private_disposable_checkout":"NOT_VERIFIED",
         "credentials_disclosed":False,"remote_ssh_used":False,
         "unity_editor_executed":False,"editmode":"NOT_RUN",
         "playmode":"NOT_RUN","physx":"NOT_RUN","nunit_xml":None,
         "editor_log":None,"source_manifest":None}
ECON={"issue":668,"policy":"PROVISIONAL_PER_UNIQUE_VICTORY_EPOCH",
      "global_free_reservations_per_victory":1,
      "victory_event_id_one_use":True,
      "stale_restore_replay_denied":"NOT_IMPLEMENTED",
      "source252":"FROZEN","unit_price":"TUNABLE_UNDECIDED",
      "player_balancing_trials":0,"human_preference_measured":False}
GATES={"selected_r4_g0":"PENDING_OWNER_CHOICE",
       "new_r4_native_g1":"NOT_RUN","canonical_game_save":"HOLD",
       "codex_available":False,"codex_offer":"BLOCKED","codex_ack":"NONE",
       "g2":"HOLD","merged":False,"integrated":False,"released":False}
LIFE={"IMPLEMENTED":True,"TESTED":True,"QA_ACCEPTED":False,
      "READY_FOR_INTEGRATOR":False,"INTEGRATED":False,"RELEASED":False}
FEEDBACK={"outcome":"NOT_MEASURED","review_url":None,"elapsed_seconds":None,
          "attempts":None,"conflicts":None,"regressions":None}
class IntakeError(ValueError): pass
def req(ok,msg):
    if not ok: raise IntakeError(msg)

def check(d):
    req(type(d) is dict and d.get("schema")==SCHEMA,"W20 schema")
    for k,v in [("frozen",PARENTS),("owner_choice",OWNER),("canonical_api",API),
                ("local_unity",WINDOWS),("economy",ECON),("gates",GATES),
                ("lifecycle",LIFE),("integrator_feedback",FEEDBACK)]:
        req(d.get(k)==v,k+" changed or fabricated")
    req(d.get("authoritative_selection_source")==
        "B01-23_EXPLICIT_COMMENT_REQUIRED","owner decision provenance")
    req(d.get("co_install_alternatives") is False,"no R4 co-install or mixed files")
    req(d.get("authorized_protected_merge") is False,"no protected merge")
    r=d.get("r4_candidates")
    req(type(r) is list and len(r)==2,"exactly two original R4 candidates")
    seen=set()
    for row in r:
        req(type(row) is dict,"candidate object")
        pr=row.get("pr")
        req(type(pr) is int and pr in R4 and pr not in seen,"distinct R4 PR")
        seen.add(pr)
        req(row.get("source")==R4[pr],"R4 original SHA/package/QA scope mismatch")
        req(row.get("owner")=="B01-23" and row.get("parent")=="r3_642"
            and row.get("admission")=="UNSELECTED_ORIGIN_ONLY",
            "R4 candidacy cannot become a release")
        req(row.get("original_pr_url")==
            f"https://github.com/C8LyNn/RoboRougelike/pull/{pr}","R4 PR URL")
        req(row.get("tested_source_only") is True,"not producer source evidence")
        req(row.get("independent_formal_g0")=="PENDING","G0 needs new owner+receiver")
        req(row.get("native_unity")=="NOT_RUN","native never auto passes")
    req(seen==set(R4),"both R4 original records required")
    return {"schema":SCHEMA,
     "verdict":"HOLD_R4_EXCLUSIVE_OWNER_DECISION_AND_NATIVE_API",
     "r4_final_pr":None,"r4_final_sha":None,
     "r4_candidates":[647,652],"alternative_mixing_forbidden":True,
     "r4_647_independent_source_qa":"GREEN_ONLY_FOR_ORIGINAL_647",
     "r4_652_lease_lifecycle":"SOURCE_RISK_NOT_EXACT_INDEPENDENT_QA",
     "formal_selected_r4_g0":"PENDING","native_unity_g1":"NOT_RUN",
     "canonical_api":"AUTHORIZED_TO_DEVELOP_NOT_IMPLEMENTED",
     "canonical_game_save":"HOLD","codex_ack":"NONE",
     "codex_offer":"BLOCKED","g2":"HOLD",
     "economy":"PLAYTEST_HYPOTHESIS_NO_HUMAN_DATA",
     "integrator_feedback":"NOT_MEASURED",
     "acceptance_rate":None,"elapsed_seconds":None,"regression_rate":None,
     "six_lifecycle":dict(LIFE),
     "no_protected_merge":True}

def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("receipt",type=Path)
    p.add_argument("--output",type=Path)
    p.add_argument("--require-release",action="store_true")
    a=p.parse_args(argv)
    try:
        raw=a.receipt.read_bytes()
        out=check(json.loads(raw.decode("utf-8")))
        out["input_sha256"]=hashlib.sha256(raw).hexdigest()
        serialized=json.dumps(out,indent=2,sort_keys=True)+"\n"
        if a.output is None:print(serialized,end="")
        else:
            req(a.output.resolve()!=a.receipt.resolve()
                and not a.output.exists(),"refuse overwriting existing evidence")
            with a.output.open("x",encoding="utf-8") as f:f.write(serialized)
        return 3 if a.require_release else 0
    except (OSError,ValueError,TypeError,UnicodeError) as exc:
        print("W20_HOLD: "+str(exc),file=sys.stderr)
        return 2

if __name__=="__main__":
    raise SystemExit(main())
