#!/usr/bin/env python3
"""W24 full seven authored R5 V6 Unity test operator, manual opt-in only.

Reuses frozen #718 original source packager, native runner, per-case guards.
W24 only selects immutable corrected author fixture #750 instead of old #690.
No Unity execution unless --mode native AND consent AND source G0 authorization.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
import tempfile

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]
OLDER=ROOT/"Tools/B01/B01-60/P17W22R5PartialNative/r5_native.py"
R5="cc7f73541517a18f4479bb155534bd4832d9985e"
V6="60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb"
CORRECTED_BLOB="2508a04f57b04447112d2777ce4e3cd7478ed40e"
QA_BASE="c782eeec12b46e4a142af79d6e641ab4c9e2e004"
ONLY_AUTHOR_CHANGES={
"Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs",
"Tools/B01/B01-23/P17W23R5ColdRestart/README.md",
"Tools/B01/B01-23/P17W23R5ColdRestart/test_fixture.py",
".github/workflows/b01-23-p17w23-r5-coldrestart-fixture.yml",
}

def need(ok,why):
    if not ok:raise AssertionError("B0160_W24_HOLD: "+why)
def git(*args):
    return subprocess.check_output(["git","-C",str(ROOT),*args],stderr=subprocess.PIPE).decode().strip()
def original():
    s=importlib.util.spec_from_file_location("b0160_w24_frozen_original_r5",OLDER)
    need(s is not None and s.loader is not None, "missing frozen #718 runner")
    m=importlib.util.module_from_spec(s)
    s.loader.exec_module(m)
    need(m.R5==R5 and len(m.METHODS)==7 and
         m.HOLD_METHOD in m.METHODS,"wrong frozen #718 source identity")
    need(git("rev-parse",QA_BASE+":"+str(OLDER.relative_to(ROOT)).replace("\\","/"))
         =="40ca11d886fc567a5a79f6abfeabdf2b37f4c36e",
         "changed original frozen runner blob")
    return m
def selected_source(n):
    need(git("merge-base",QA_BASE,"HEAD")==QA_BASE,"W24 not own W23 successor")
    need(git("cat-file","-t",V6)=="commit","original #750 historical Git object missing locally")
    need(git("merge-base",R5,V6)==R5,"#750 not true original R5 descendant")
    changed=set(git("diff","--name-only",R5,V6).splitlines())
    need(changed==ONLY_AUTHOR_CHANGES,"#750 modifies unexpected original R5 scope")
    original_blobs=dict(n.BLOBS)
    original_blobs[n.FIXTURE]=CORRECTED_BLOB
    for path,blob in original_blobs.items():
        need(git("rev-parse",V6+":"+path)==blob,
             "wrong exact original R5 V6 source blob: "+path)
    orig=n.selected_source() # pinned original R5 + 7 authored identities + GUID/Unity closure
    names=git("ls-tree","-r","--name-only",V6).splitlines()
    selected={}
    for p in names:
        if p in n.OTHER or any(p==prefix+".meta" or p.startswith(prefix+"/")
                               for prefix in n.SCOPE):
            selected[p]=subprocess.check_output(
                ["git","-C",str(ROOT),"show",V6+":"+p],stderr=subprocess.PIPE)
    need(set(selected)==set(orig), "corrected V6 changed Unity project scope unexpectedly")
    need(all(selected[p]==orig[p] for p in orig if p!=n.FIXTURE),
         "corrected #750 changes nonfixture original Unity source")
    corrected=selected[n.FIXTURE].decode("utf-8")
    old=orig[n.FIXTURE].decode("utf-8")
    begin="public IEnumerator "+n.HOLD_METHOD+"()"
    stop="public IEnumerator WrongGeneration_"
    body=corrected.split(begin,1)[1].split(stop,1)[0]
    oldbody=old.split(begin,1)[1].split(stop,1)[0]
    need("Assert.AreEqual(4,restored.Generation)" in oldbody and
         "Assert.AreEqual(4,restored.Generation)" not in body,
         "former ColdRestart expected-red not repaired")
    for needle in (
      "SnapshotCodec.TryDecode(saved,out RunSnapshot savedSnapshot)",
      "Assert.AreEqual(5,savedSnapshot.Generation)",
      "restored.TryRestoreBytes(saved,41,5)",
      "Assert.AreEqual(5,restored.Generation)",
      "IsConsumed(0,2310001)","IsConsumed(1,2310002)"
    ):
        need(needle in body,"corrected ColdRestart lost required behavior: "+needle)
    import re as _re
    identities=tuple(_re.findall(
        r"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",corrected))
    need(identities==n.METHODS,"corrected V6 changed seven R5 NUnit test identities")
    need(n.runner().guid_check(selected)==n.runner().guid_check(orig),
         "fixture-only correction changed Unity GUID closure")
    return selected

def main(argv=None):
    args=argparse.ArgumentParser()
    args.add_argument("--mode",choices=("preflight","native"),default="preflight")
    args.add_argument("--unity",default="")
    args.add_argument("--scratch-parent",default=os.environ.get("TEMP") or tempfile.gettempdir())
    args.add_argument("--operator-consent",default="")
    args.add_argument("--approved-source-g0",action="store_true")
    opts=args.parse_args(argv)
    n=original()
    src=selected_source(n)
    m=n.runner()
    # Frozen #718 intentionally rebinds its private W12 verifier SHA to old R5.
    # This NEW, run-local W24 module is pinned to the owner-authored V6 source;
    # never mutate #718, #750 or W12 disk files to retarget evidence checks.
    need(m.SHA==R5,"frozen original W12 test verifier unexpected source pin")
    m.SHA=V6
    if opts.mode=="native":
        need(os.name=="nt","only Windows operator may invoke Unity")
        need(opts.operator_consent=="B0160 R5V6 SEVEN NATIVE",
             "typed owner manual authorization absent")
        need(opts.approved_source_g0,"corrected #750 source G0 not authorized")
        u=Path(opts.unity)
        need(u.is_file() and
             re.search(r"[/\\]6000\.3\.14f1[/\\]Editor[/\\]Unity\.exe$",
                       str(u),flags=re.I),"missing exact Unity Hub 6000.3.14f1 executable")
        need(not u.is_symlink(),"Unity editor symlink denied")
    else:u=None
    root=n.safe_root(opts.scratch_parent)
    receipt={
       "package":"B01-60-P17W24-R5V6-SEVEN-NATIVE",
       "frozen_runner_sha":QA_BASE,"selected_original_R5_SHA":R5,
       "corrected_owner_fixture_SHA":V6,
       "corrected_fixture_git_blob":CORRECTED_BLOB,
       "seven_native_methods":list(n.METHODS),"original_unity_version":"6000.3.14f1",
       "native_G1":"NOT_RUN","source_G0":"SEPARATE_INDEPENDENT_REVIEW_REQUIRED",
       "canonical_749_native_G1":"NOT_RUN","canonical_753":"ALT_HOLD",
       "live_game_rig_ledger_save":"HOLD","full_release":"HOLD",
       "integrated":False,"uploaded":False,"merged":False,"codex_ack":"NONE"}
    receipt_path=root/"P24-receipt.json"
    receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    try:
        for i,method in enumerate(n.METHODS):
            scope,product=n.private_source(src,i,secrets.token_hex(6),m)
            qa={"original_source_sha":V6,"native_unity":"NOT_RUN",
                "source_only_preflight":True,"codex_ack":False,"integrated":False,
                "native_meta_guids":m.guid_check(scope),
                "file_sha256":{p:n.blake(v) for p,v in sorted(scope.items())}}
            archive=m.zip_data(scope,qa)
            project=root/("P"+str(i))
            m.stage(archive,scope,project)
            need((project/n.PS).read_bytes()==scope[n.PS],
                 "private per-case PlayerSettings did not stage byte-exact")
            if opts.mode=="native":
                n.run_editor(u,project,root,"guard"+str(i),n.PROBE,scope,m)
                n.run_editor(u,project,root,"r5v6case"+str(i),n.SUITE+method,scope,m)
            else:
                m.rollback(archive,scope,project)
        if opts.mode=="preflight":
            receipt["source_preflight"]="PASS_7_PRIVATE_R5V6_STAGES_ROLLED_BACK_NO_EDITOR"
            print("B0160_W24_R5V6_SOURCE_ONLY_7_PROJECTS_STAGE_ROLLBACK_PASS")
            print("B0160_W24_R5V6_ALL_7_CORRECTED_ORIGINAL_NUNIT_IDENTITIES_PINNED")
            print("B0160_W24_NATIVE_G1_NOT_RUN")
        else:
            receipt["native_G1"]="SEVEN_R5V6_CASES_STRUCTURALLY_PASS_PENDING_INDEPENDENT_QA"
            receipt["operator_consent"]=True
            print("B0160_W24_REAL_SEVEN_R5V6_XML_EDITOR_LOGS_CAPTURED_INDEPENDENT_QA_PENDING")
        receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
        (root/"P24-sha256-manifest.json").write_text(json.dumps(
            n.manifest_files(root,receipt),sort_keys=True,indent=2)+"\n")
        print("B0160_W24_LOCAL_PRIVATE_EVIDENCE_NO_UPLOAD="+str(root))
        return 0
    except Exception as ex:
        receipt["failed_closed"]=str(ex)
        receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
        print("B0160_W24_R5V6_HOLD="+str(ex),file=sys.stderr)
        print("B0160_W24_LOCAL_PRIVATE_EVIDENCE_NO_UPLOAD="+str(root))
        return 4 if opts.mode=="native" else 2

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as ex:
        print("B0160_W24_PRECHECK_HOLD="+repr(ex),file=sys.stderr)
        sys.exit(3 if "--mode" in sys.argv and "native" in sys.argv else 2)
