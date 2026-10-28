#!/usr/bin/env python3
"""P16 release-safe ORIGINAL CHL QA operator CLI preflight receipt. NEVER invokes Unity."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

OWNER_SHA="c7b2a234a286089383503da41883d624fe650551"
QA_SOURCE_SHA="9a6acca417524545fbce7ac5962fe0944c5237c6"
OPERATOR_BLOB="96aec2f870ccc510727048a7208292f781e80c36"
MANIFEST_BLOB="0d1483b114fba786b1d96242d8d20527b945c5b7"
OPERATOR="Tools/B01/QA-CHL003/W66Receipt/chl003_w66_operator.py"

def run(args,check=True):
    p=subprocess.run([str(x) for x in args],text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if check and p.returncode!=0:
        raise RuntimeError(f"COMMAND_ERROR: {args}: {p.returncode}: {p.stderr[-1000:]}")
    return p

def git(repo,*args):
    return run(["git","-C",str(repo),*args]).stdout.strip()

def guard(condition,name):
    if not condition:raise RuntimeError("P16_DRYRUN_FAIL_"+name)

def check(actual,expected,code):
    guard(actual==expected,code+": "+repr(actual))

def cli(operator,project,evidence):
    return run([sys.executable,operator,"--preflight-only","--project",project,"--evidence",evidence],check=False)

def main():
    if len(sys.argv)!=2:raise RuntimeError("USAGE: dryrun_chl_release.py /outside-worktree/receipt.json")
    output=Path(sys.argv[1]).resolve()
    repo=Path(__file__).resolve().parents[4]
    guard((repo/OPERATOR).is_file(),"MISSING_OPERATOR")
    check(git(repo,"hash-object",OPERATOR),OPERATOR_BLOB,"WRONG_OPERATOR_BLOB")
    check(git(repo,"rev-parse",OWNER_SHA+"^{commit}"),OWNER_SHA,"PRODUCER_COMMIT_UNAVAILABLE")
    check(git(repo,"rev-parse",QA_SOURCE_SHA+"^{commit}"),QA_SOURCE_SHA,"QA_OPERATOR_BASE_UNAVAILABLE")
    guard(not (repo==output or repo in output.parents or output in repo.parents),"OUTPUT_MUST_BE_EXTERNAL")
    operator=str(repo/OPERATOR)
    cases=[]
    with tempfile.TemporaryDirectory(prefix="b01_chl_p16_native_not_run_") as temp:
        sandbox=Path(temp)
        roots=[sandbox/"producer",sandbox/"sibling",sandbox/"wrong-qa-sha"]
        try:
            git(repo,"worktree","add","--detach",str(roots[0]),OWNER_SHA)
            git(repo,"worktree","add","--detach",str(roots[1]),OWNER_SHA)
            git(repo,"worktree","add","--detach",str(roots[2]),QA_SOURCE_SHA)
            check(git(roots[0],"rev-parse","HEAD"),OWNER_SHA,"PRODUCER_SHA_CHANGED")
            check(git(roots[0],"hash-object","Docs/B01/CHL003/manifest.json"),MANIFEST_BLOB,"SOURCE_MANIFEST_CHANGED")
            external=sandbox/"safe-external-evidence"/"native"
            ok=cli(operator,roots[0],external)
            guard(ok.returncode==0,"GOOD_SOURCE_PREFLIGHT_EXIT:"+ok.stderr)
            payload=json.loads(ok.stdout)
            guard(payload.get("source_sha")==OWNER_SHA and
                  payload.get("source_and_worktree_guard")=="PASS" and
                  payload.get("native_unity")=="NOT_RUN","FALSE_GREEN_RECEIPT")
            cases.append({"id":"P16-CHL-01_VALID_SOURCE_DETACHED_WORKTREE","expected":"ACCEPT","actual":"ACCEPT","exit":ok.returncode})
            for name,project,evidence,want in [
                ("P16-CHL-02_SIBLING_WORKTREE_ROOT",roots[0],roots[1],"EVIDENCE_IN_REGISTERED_WORKTREE"),
                ("P16-CHL-03_SIBLING_ASSETS",roots[0],roots[1]/"Assets"/"Evidence","EVIDENCE_IN_REGISTERED_WORKTREE"),
                ("P16-CHL-04_OUTPUT_ANCESTOR",roots[0],sandbox,"EVIDENCE_ANCESTOR_OF_PROJECT"),
                ("P16-CHL-05_WRONG_QA_SHA",roots[2],external,"WRONG_SOURCE_SHA"),
            ]:
                res=cli(operator,project,evidence)
                guard(res.returncode==2 and want in res.stderr,"NEGATIVE_BYPASS_"+name+":"+res.stderr)
                cases.append({"id":name,"expected":"REJECT","actual":"REJECT","exit":res.returncode,"reason":want})
            check(git(roots[0],"status","--porcelain"),"","PRODUCER_DIRTY_AFTER_PREFLIGHT")
            check(git(roots[1],"status","--porcelain"),"","SIBLING_DIRTY_AFTER_PREFLIGHT")
            guard(not external.exists(),"DRYRUN_WROTE_NATIVE_OUTPUT")
        finally:
            for root in reversed(roots):
                if root.exists():
                    run(["git","-C",str(repo),"worktree","remove","--force",str(root)])
    guard(len(cases)==5,"CASE_COUNT")
    output.parent.mkdir(parents=True,exist_ok=True)
    receipt={
        "package_id":"CHL003-P16-RELEASE-SAFE-PREFLIGHT",
        "source_operator_pr":319,
        "operator_base_sha":QA_SOURCE_SHA,
        "operator_git_blob":OPERATOR_BLOB,
        "producer_pr":285,
        "producer_sha":OWNER_SHA,
        "producer_manifest_git_blob":MANIFEST_BLOB,
        "tests_executed":len(cases),
        "tests_passed":len(cases),
        "tests":cases,
        "native_unity":"NOT_RUN",
        "physx_drives":0,
        "g1":"NOT_RUN",
        "g2":"HOLD",
        "independent_receiver":"B01-40/B01-60 source safety accepted historical same operator blob; this is author-run CLI dry-run, not a native receiver acceptance"
    }
    output.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    print("CHL_P16_ORIGINAL_OPERATOR_CLI_DRYRUN_PASS=5 SOURCE_SHA="+OWNER_SHA+" NATIVE_UNITY=NOT_RUN")
    print("RECEIPT_SHA256="+hashlib.sha256(output.read_bytes()).hexdigest())
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except (RuntimeError,ValueError,OSError,subprocess.SubprocessError) as e:
        print("CHL_P16_DRYRUN_FAIL_CLOSED "+str(e),file=sys.stderr)
        sys.exit(2)
