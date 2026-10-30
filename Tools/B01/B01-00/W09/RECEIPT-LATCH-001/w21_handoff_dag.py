"""B01-00 W21 cross-subsystem integration DAG with fail-closed source receipts.

This calculates critical handoffs and inherited blockers. An offline snapshot
is NOT authenticated GitHub evidence and cannot grant source G0 or native G1.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

VERSION="b01-w21-cross-subsystem-handoff-v1"
SHAS={
 "r4_frozen":"09c1809b73324a6d31c13e5d0143de62ac1e3ca6",
 "r4_alt":"ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419",
 "api_selected_base":"eed2e39636edfd8b8db754c426db452164cbf9d7",
 "api_alt":"5387e829a5f6d38b713086e6fb906a9a755189ff",
 "host_v5":"cc7f73541517a18f4479bb155534bd4832d9985e",
 "api_v2":"809f82575c638e1b4d2806b5f417bddab6c0871a",
 "storage_v1":"d6ec0c82e388e65c1e02ddfd920bb50a1f6ef121",
 "runner_v1":"c75cb6e52a0de11f27b570c34e08d32082af421f",
 "game510":"d94400d95d13db7f27e1533df05c9292d5380431",
 "save556":"33094f154c7b91119b36d58aab285e6e83c5de57",
}
PRODUCER={
 "host_r5":{"pr":690,"sha_key":"host_v5","evidence_run":41120562896,
            "depends_on":[],"receiver":"B01-61/41+40/62",
            "required":["INDEPENDENT_R5_COMPONENT_DESTROY_QA",
                        "FORMAL_NEW_R5_PHYSICAL_G0"]},
 "api_v2":{"pr":691,"sha_key":"api_v2","evidence_run":41121167581,
           "depends_on":["host_r5"],"receiver":"B01-23/62+61/41+40",
           "required":["API_681_MULTI_OWNER_EXCLUSIVE_LEASE_688",
                       "INDEPENDENT_V2_PREPARE_ABORT_QA_685",
                       "FORMAL_NEW_API_G0"]},
 "storage":{"pr":669,"sha_key":"storage_v1","evidence_run":41115085734,
            "depends_on":[],"receiver":"B01-62+61/41+40",
            "required":["EXTERNAL_VALID_BACKUP_ROLLBACK_POLICY_680",
                        "TRUSTED_MONOTONIC_WATERMARK_OR_SCOPED_THREAT_MODEL",
                        "STORAGE_NEW_G0"]},
 "native_runner":{"pr":684,"sha_key":"runner_v1","evidence_run":41116686300,
            "depends_on":[],"receiver":"B01-60+41+40+LOCAL_OPERATOR",
            "required":["OWNER_EXECUTES_ACTIVATED_UNITY_6000_3_14F1",
                        "ORIGINAL_NUNIT_EDITORLOG_SHA_MANIFEST"]},
 "canonical_adapter":{"pr":None,"sha_key":None,"evidence_run":None,
            "depends_on":["host_r5","api_v2","storage"],
            "receiver":"B01-23/20/62+61/41+40",
            "required":["REAL_FROZEN_510_PRIVATE_RIG_LEDGER_PORT",
                        "WHOLE_SNAPSHOT_ATOMIC_COLD_RESTORE"]},
 "integrator":{"pr":None,"sha_key":None,"evidence_run":None,
            "depends_on":["canonical_adapter","native_runner"],
            "receiver":"B01-00+OWNER+CODEX_LATER",
            "required":["SINGLE_G0_SIGNED_PACK",
                        "CODEX_REVIEW_ACK_AND_EXPLICIT_INSTALL_PERMISSION"]}
}
KNOWN_FLAGS={
 "host_r5":{"SOURCE_IMPL":"PRODUCER_TESTED","INDEPENDENT":"PENDING",
            "FORMAL_G0":"PENDING","NATIVE_G1":"NOT_RUN"},
 "api_v2":{"SOURCE_IMPL":"PRODUCER_TESTED","INDEPENDENT":"PENDING",
           "FORMAL_G0":"PENDING","NATIVE_G1":"NOT_RUN"},
 "storage":{"SOURCE_IMPL":"PRODUCER_TESTED","INDEPENDENT":"TRUST_BOUNDARY_RED_680",
            "FORMAL_G0":"PENDING","NATIVE_G1":"NOT_RUN"},
 "native_runner":{"SOURCE_IMPL":"PRODUCER_TESTED","INDEPENDENT":"SOURCE_REVIEWED_ONLY",
            "FORMAL_G0":"NOT_APPLICABLE","NATIVE_G1":"NOT_RUN"},
 "canonical_adapter":{"SOURCE_IMPL":"NOT_IMPLEMENTED","INDEPENDENT":"NOT_RUN",
            "FORMAL_G0":"NOT_RUN","NATIVE_G1":"NOT_RUN"},
 "integrator":{"SOURCE_IMPL":"NOT_IMPLEMENTED","INDEPENDENT":"NOT_RUN",
            "FORMAL_G0":"NOT_RUN","NATIVE_G1":"NOT_RUN"},
}
AUTHORITIES={"r4":{"winner_pr":647,"loser_pr":652,"loser":"ALT_HOLD"},
             "api":{"winner_pr":681,"loser_pr":682,"loser":"ALT_HOLD"},
             "codex":"UNAVAILABLE","merge":False,"releases":False}
CROSS={"r4_647_security_670":"REPRODUCED_SOURCE_RED_ON_FROZEN_R4",
       "r5_690_security":"PRODUCER_GREEN_INDEPENDENT_PENDING",
       "api_682_security_685":"REPRODUCED_SOURCE_RED_ALT_HOLD",
       "api_681_security_688":"REPRODUCED_SOURCE_RED_ON_SELECTED_BASE",
       "api_v2_691_688_fix":"PENDING_NEW_OWNER_EXCLUSIVE_LEASE",
       "storage_669_680":"EXTERNAL_VALID_FILE_REPLACEMENT_TRUST_RED",
       "unity_pc_native":"NOT_RUN",
       "original_510_private_port":"NOT_IMPLEMENTED",
       "integrator_feedback":"NOT_MEASURED"}
class Blocked(ValueError): pass
def demand(ok,msg):
    if not ok: raise Blocked(msg)
def evaluate(receipt):
    demand(type(receipt) is dict and receipt.get("schema")==VERSION,"schema")
    for k,val in (("source_sha",SHAS),("authorities",AUTHORITIES),
                  ("security_evidence",CROSS),("stage_status",KNOWN_FLAGS)):
        demand(receipt.get(k)==val,k+" differs from reviewed W21 HOLD")
    observed=receipt.get("packages")
    demand(type(observed) is dict and set(observed)==set(PRODUCER),"stage set")
    for name,spec in PRODUCER.items():
        demand(observed.get(name)==spec,"original PR/SHA/dependency/receiver changed: "+name)
    outcome=receipt.get("integrator_outcome")
    demand(outcome=={"status":"NOT_MEASURED","ack":None,"elapsed_s":None,
                     "retries":None,"regressions":None,"commit_sha":None},
           "never fake integrator action or zero metrics")
    demand(receipt.get("operator_access")=={"owner_pc_access":False,
            "native_unity_executed":False,"ssh":False,"credentials":False},
           "do not fake operator access")
    # Topological order deterministic, refuses missing parents and cycles.
    done=set()
    remaining=set(PRODUCER)
    order=[]
    while remaining:
        ready=sorted(n for n in remaining if set(PRODUCER[n]["depends_on"]) <= done)
        demand(ready,"dependency cycle / missing owner")
        for n in ready:
            order.append(n);done.add(n);remaining.remove(n)
    stages=[]
    propagated={}
    for name in order:
        node=PRODUCER[name]
        own=list(node["required"])
        upstream=sorted(set(v for d in node["depends_on"] for v in propagated[d]))
        combined=sorted(set(own+upstream))
        propagated[name]=combined
        stages.append({
            "id":name,"owner_pr":node["pr"],
            "owner_sha":SHAS[node["sha_key"]] if node["sha_key"] else None,
            "source_producer_ci_run":node["evidence_run"],
            "source_status":KNOWN_FLAGS[name]["SOURCE_IMPL"],
            "independent_status":KNOWN_FLAGS[name]["INDEPENDENT"],
            "source_g0":KNOWN_FLAGS[name]["FORMAL_G0"],
            "native_g1":KNOWN_FLAGS[name]["NATIVE_G1"],
            "immediate_blockers":own,
            "inherited_blockers":upstream,
            "all_blockers":combined,
            "receiver":node["receiver"],
            "ready_for_integrator":False,
            "integrated":False,"released":False})
    return {"schema":VERSION,
            "verdict":"HOLD_W21_INTEGRATION_DEPENDENCY_GATES",
            "owner_selected":{"r4":647,"api":681},
            "alternatives_hold":[652,682,644,604],
            "dependency_order":order,"stages":stages,
            "critical_integrator_blockers":propagated["integrator"],
            "unresolved_count":len(propagated["integrator"]),
            "native_unity":"NOT_RUN","live_game_saveload":"HOLD",
            "codex_ack":"NONE","g2":"HOLD","merged":False,
            "acceptance_latency_seconds":None,"regression_rate":None,
            "metrics":"NOT_MEASURED",
            "six_state":{"IMPLEMENTED":True,"TESTED":False,
                         "QA_ACCEPTED":False,"READY_FOR_INTEGRATOR":False,
                         "INTEGRATED":False,"RELEASED":False}}
def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("receipt",type=Path)
    p.add_argument("--require-release",action="store_true")
    p.add_argument("--output",type=Path)
    args=p.parse_args(argv)
    try:
        raw=args.receipt.read_bytes()
        result=evaluate(json.loads(raw.decode("utf-8")))
        result["input_sha256"]=hashlib.sha256(raw).hexdigest()
        text=json.dumps(result,indent=2,sort_keys=True)+"\n"
        if args.output is None:print(text,end="")
        else:
            demand(args.output.resolve()!=args.receipt.resolve()
                   and not args.output.exists(),"refuse overwrite")
            with args.output.open("x",encoding="utf-8") as f:f.write(text)
        return 3 if args.require_release else 0
    except (OSError,TypeError,UnicodeError,ValueError) as e:
        print("B01_W21_REJECT:"+str(e),file=sys.stderr)
        return 2
if __name__=="__main__":
    raise SystemExit(main())
