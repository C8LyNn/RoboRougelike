"""B01-00 W19 exact R3 candidate comparison + FAIL CLOSED source-only release gate.

Offline *receipt consistency*, NEVER Github owner authentication or native Unity QA.
A later selected R3 requires a new authenticated receipt and independent G0.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

SCHEMA="b01-w19-r3-exclusive-intake-v1"
PARENT={
 "game":{"pr":510,"sha":"d94400d95d13db7f27e1533df05c9292d5380431"},
 "save":{"pr":556,"sha":"33094f154c7b91119b36d58aab285e6e83c5de57"},
 "r2":{"pr":596,"sha":"efb2b06eafedab5469b57f163152d96aca3c5915"},
 "alt":{"pr":604,"status":"ALT_HOLD"}}
CANDIDATES={
 642: {"sha":"22cec9888047994ae523cbe7bf1b0a483d64cec4",
       "source_tests_each_os":101,"archive_tests_each_os":25,
       "zip_bytes":96318,"zip_members":23,
       "zip_sha256":"2fd97465358bddb7c9aeee91af2d4a0b75dd9b86525150e6bf708403291aa60d",
       "unity_guid_count":10,"asmdef_count":3,
       "producer_ci_run":41106902806,
       "changed_path_count":7,"new_type":"SAME_CORE_MODIFIED",
       "core_stale_export_guard":"SOURCE_IMPLEMENTED_NOT_INDEPENDENTLY_QA",
       "host_slot_lease":"SOURCE_IMPLEMENTED_NOT_INDEPENDENTLY_QA"},
 644: {"sha":"d9b95146e46a5d81d5a291d7887e456164f4d0c2",
       "source_tests_each_os":103,"archive_tests_each_os":27,
       "zip_bytes":103942,"zip_members":27,
       "zip_sha256":"7f10a7e4ef5052d7b83010c07fff4b8eb653e62caf314ed0751345c108547315",
       "unity_guid_count":11,"asmdef_count":3,
       "producer_ci_run":41105901634,
       "changed_path_count":10,"new_type":"ADDITIVE_LEASE_WITH_FROZEN_R2_CORE",
       "core_stale_export_guard":"R2_CORE_UNCHANGED_SEPARATE_INDEPENDENT_QA_REQUIRED",
       "host_slot_lease":"SOURCE_IMPLEMENTED_NOT_INDEPENDENTLY_QA"},
}
SECURITY={
 "629_stale_public_host":"R3_EXACT_642_SOURCE_GREEN_STALE_REPLAY",
 "637_durable_commit_then_throw":"R3_EXACT_642_INDEPENDENT_RELEASE_P0_RED",
 "631_native_checkpoint_fixture":"SOURCE_RISK_R3_NATIVE_NOT_RUN",
 "640_slot_ownership":"HISTORICAL_R2_RED_R3_INDEPENDENT_NOT_RUN",
 "original_556_consumed_light_heavy":"R3_INDEPENDENT_NOT_RUN",
 "whole_game_private_510_hydration":"NOT_AUTHORIZED",
 "crash_atomic_payload_and_checkpoint":"NOT_PROVEN",
}
GATES={
 "r3_independent_security_qa":"PARTIAL_629_GREEN_637_P0_RED",
 "r3_physical_g0":"HOLD_P0_RED",
 "native_unity_editmode":"NOT_RUN",
 "native_unity_playmode":"NOT_RUN",
 "native_unity_physx":"NOT_RUN",
 "canonical_game_save_load":"HOLD",
 "codex_offer":"BLOCKED",
 "codex_ack":"NONE",
 "g2":"HOLD",
 "integrated":False,
 "merged":False,
}
SIX={
 "IMPLEMENTED":True,"TESTED":True,"QA_ACCEPTED":False,
 "READY_FOR_INTEGRATOR":False,"INTEGRATED":False,"RELEASED":False
}

class Deny(ValueError):
    pass

def need(value, why):
    if not value: raise Deny(why)

def audit(document):
    need(type(document) is dict and document.get("schema")==SCHEMA,"W19 schema")
    need(document.get("frozen")==PARENT,"frozen #510/#556/#596 and #604 ALT source identity")
    need(document.get("security")==SECURITY,"do not fake R3 security independent QA")
    need(document.get("gates")==GATES,"do not fake G0/G1/live-game/ACK/G2/merge")
    need(document.get("owner_choice")=={
        "authority":"B01-23","status":"SELECTED_LINEAGE_SECURITY_HOLD","winner_pr":642,
        "loser_pr":644,"decision_comment_url":
        "https://github.com/C8LyNn/RoboRougelike/pull/642#issuecomment-6530905217"},
        "owner B01-23 has not explicitly frozen ONE R3; reject forged decision")
    need(document.get("competing_sources_installed") is False,
        "never install both R3 candidates")
    need(document.get("code_offer_to_codex") is False,
        "no #79 offer before real owner choice and independent G0")
    need(document.get("integrator_feedback") == {
        "status":"NOT_MEASURED","ack_url":None,"elapsed_seconds":None,
        "attempts":None,"regressions":None,"integration_sha":None},
        "integrator feedback unavailable, never invent results")
    candidates=document.get("candidates")
    need(type(candidates) is list and len(candidates)==2,"exactly two author original R3")
    observed=set()
    for record in candidates:
        need(type(record) is dict,"candidate record not object")
        pr=record.get("pr")
        need(type(pr) is int and pr in CANDIDATES and pr not in observed,
            "distinct candidate IDs #642 and #644 required")
        observed.add(pr)
        expected=CANDIDATES[pr]
        need(record.get("source")==expected,"original producer SHA/ZIP/test/GUID mismatch")
        need(record.get("owner")=="B01-23"
             and record.get("lineage_parent_pr")==596
             and record.get("lineage_parent_sha")==PARENT["r2"]["sha"]
             and record.get("branch_selection")==(
                 "SELECTED_BASE_SECURITY_HOLD" if pr==642 else "ALT_HOLD"),
             "candidate owner/lineage/selection conflict")
        need(record.get("ci_status")=="PRODUCER_SOURCE_PASS_ONLY",
             "producer source CI is not independent R3 acceptance")
        need(record.get("independent_qa")==(
                 "PARTIAL_629_GREEN_637_P0_RED" if pr==642 else "NOT_RUN")
             and record.get("formal_g0")==(
                 "HOLD_P0_RED" if pr==642 else "ALT_HOLD")
             and record.get("native_g1")=="NOT_RUN",
             "cannot promote producer tests into independent G0/G1")
        need(record.get("lifecycle")==SIX,"six distinct lifecycle stages; no promotion")
        need(record.get("producer_ci_url")==
             f"https://github.com/C8LyNn/RoboRougelike/actions/runs/{expected['producer_ci_run']}",
             "original producer run identity mismatch")
        need(record.get("pr_url")==f"https://github.com/C8LyNn/RoboRougelike/pull/{pr}",
             "candidate pull URL mismatch")
    need(observed==set(CANDIDATES),"must record both candidates")
    return {
        "schema":SCHEMA,
        "status":"HOLD_SELECTED_642_P0_UNCERTAIN_DURABLE_COMMIT",
        "selected_pr":642,"selected_sha":CANDIDATES[642]["sha"],
        "competing_prs":[642,644],
        "producer_source_ci_pass":True,
        "independent_r3_security_qa":"PARTIAL_629_GREEN_637_P0_RED",
        "r3_physical_g0":"HOLD_P0_RED",
        "native_unity_g1":"NOT_RUN",
        "canonical_game_save_load":"HOLD",
        "codex_offer":"BLOCKED","codex_ack":"NONE",
        "integrator_feedback":"NOT_MEASURED",
        "metrics":{"acceptance_rate":None,"elapsed_seconds":None,
                   "attempts":None,"regressions":None},
        "immutable_game_sha":PARENT["game"]["sha"],
        "immutable_save_sha":PARENT["save"]["sha"],
        "immutable_r2_sha":PARENT["r2"]["sha"],
        "alt604":"ALT_HOLD",
        "R3_historical_risks_not_independently_retested":list(SECURITY),
        "selection_receipt_is_offline_structural_only":True,
        "never_auto_pick_more_tests":True,
        "no_original_source_mutation":True,
        "ready_for_codex":False,
        "integrated":False,
        "released":False,
    }

def main(argv=None):
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument("receipt",type=Path)
    cli.add_argument("--output",type=Path)
    cli.add_argument("--require-ready",action="store_true")
    args=cli.parse_args(argv)
    try:
        raw=args.receipt.read_bytes()
        result=audit(json.loads(raw.decode("utf-8")))
        result["receipt_sha256"]=hashlib.sha256(raw).hexdigest()
        output=json.dumps(result,sort_keys=True,indent=2)+"\n"
        if args.output:
            need(args.output.resolve()!=args.receipt.resolve()
                 and not args.output.exists(),"do not overwrite original evidence")
            with args.output.open("x",encoding="utf-8") as fp:fp.write(output)
        else:
            print(output,end="")
        return 3 if args.require_ready else 0
    except (OSError,ValueError,TypeError,UnicodeError) as exc:
        print("W19_R3_HOLD: "+str(exc),file=sys.stderr)
        return 2

if __name__=="__main__":
    raise SystemExit(main())
