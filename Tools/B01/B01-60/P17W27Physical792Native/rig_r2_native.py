#!/usr/bin/env python3
"""B01-60 W27 exact original #792 R2 native PhysX runner: source-only by default.

Reuses immutable #783 B01-60 operator and frozen #718 Unity NUnit harness, but
loads the *author's original* updated physics C# and fourth PlayMode regression.
No branch overlay or modifications to B01-23, Codex, runtime Save, or user PC.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import secrets
import sys
import tempfile

ROOT=Path(__file__).resolve().parents[4]
OLD=ROOT/"Tools/B01/B01-60/P17W25Physical772Native/rig_native.py"
BASE="494e1056366c9004deefd7e4a73ad07b0a62a137"
PARENT="ce46a5e0cc5d26df6c4ece723e8efe20d70fc4b1"
R2="16cb2a7b5158605b15191b6074f9e392e0d1ebd3"
R2_BLOBS={
 "Assets/B0123CanonicalRigPortW24/Runtime/CanonicalPhysicalRigPortW24.cs":
 "7623f698b6e644b8d8247a011b453bf40adc2227",
 "Assets/B0123CanonicalRigPortW24/Tests/PlayMode/CanonicalPhysicalRigPortW24NativeTests.cs":
 "358ef67f8f0f795d94a646e8e11c27158e2c8f0d",
}
ALLOWED_R2_DIFF={
 *R2_BLOBS,
 ".github/workflows/b01-23-p17w25-physical-rollback-r2.yml",
 "Tools/B01/B01-23/P17W25PhysicalRollbackR2/ManagedUnitySourceOnlyShim.cs",
 "Tools/B01/B01-23/P17W25PhysicalRollbackR2/PhysicalSourceOnlyShimTests.csproj",
 "Tools/B01/B01-23/P17W25PhysicalRollbackR2/Program.cs",
 "Tools/B01/B01-23/P17W25PhysicalRollbackR2/README.md",
 "Tools/B01/B01-23/P17W25PhysicalRollbackR2/manifest.json",
 "Tools/B01/B01-23/P17W25PhysicalRollbackR2/package.py",
 "Tools/B01/B01-23/P17W25PhysicalRollbackR2/test_package.py",
}
METHODS=(
 "Original510AtomicLedger_RealRigidbodyDetachedAndAttachedParentAndVelocity",
 "AbortedCompetingWorld_PreservesActualLiveVelocityAndAngularVelocity",
 "InvalidOriginal556Binary_CannotCreateAnyUnityBodyOrConsumeId",
 "QuiescenceRequired_BeforeRealPhysicalStage",
)
PROBE="Assets/B0123CanonicalRigPortW24/Tests/PlayMode/B0160W27ScopeProbe.cs"
PROBE_FQCN="B01.CanonicalRigPortW24.Tests.B0160W27ScopeProbe.OnlyPrivateSaveScope"
SUITE="B01.CanonicalRigPortW24.Tests.CanonicalPhysicalRigPortW24NativeTests."

def need(ok,why):
 if not ok:raise AssertionError("B0160_W27_HOLD: "+why)
def old():
 spec=importlib.util.spec_from_file_location("b0160_w27_original_783",OLD)
 need(spec is not None and spec.loader is not None,"frozen #783 runner missing")
 m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 need(m.RIG==PARENT and len(m.METHODS)==3 and m.BASE.exists(),
      "old physical receiver source changed")
 need(m.git("rev-parse",BASE+":"+OLD.relative_to(ROOT).as_posix())==
      m.git("rev-parse","HEAD:"+OLD.relative_to(ROOT).as_posix()),
      "original B01-60 W25 SHA drift")
 return m

def selected(m):
 need(m.git("merge-base",BASE,"HEAD")==BASE,"B01-60 W25 not immutable ancestor")
 need(m.git("cat-file","-t",R2)=="commit","original owner #792 object not available locally")
 need(m.git("merge-base",PARENT,R2)==PARENT,"#792 not immutable #772 lineage")
 changed=set(m.git("diff","--name-only",PARENT,R2).splitlines())
 need(changed==ALLOWED_R2_DIFF,
      "original #792 changed outside own 2 physics C# plus 8 own tests/CI")
 for path,sha in R2_BLOBS.items():
  need(m.git("rev-parse",R2+":"+path)==sha,
       "original physics or 4-test fixture Git blob mismatch "+path)
 source=m.selected_source(m.original_module())  # exact original #772 closure
 for path,sha in R2_BLOBS.items():
  source[path]=m.git_bytes(R2,path)
 need(tuple(x.decode() for x in re.findall(
    rb"\[UnityTest\]\s*public\s+IEnumerator\s+(\w+)\s*\(",
    source[m.NATIVE_FIXTURE]))==METHODS,"four original Unity [UnityTest] ABI changed")
 # Strong proof: no other #772 Unity source file changed at #792.
 for path in source:
  actual=m.git("rev-parse",R2+":"+path)
  if path in R2_BLOBS:need(actual==R2_BLOBS[path],"R2 updated source inconsistent")
  else:
   need(actual==m.git("rev-parse",PARENT+":"+path),
        "R2 unexpected Unity source mutation "+path)
 # Verify new regression asserts BOTH live linear/angular velocity.
 testcase=source[m.NATIVE_FIXTURE].decode("utf-8")
 pos=testcase.index("public IEnumerator AbortedCompetingWorld_")
 after=testcase[pos:testcase.index("[UnityTest]",pos)]
 for required in ("linearVelocity","angularVelocity","Reactivate",
                  "Assert.AreEqual","yield return null"):
  need(required in after,"R2 original native test missing "+required)
 runtime=source[m.PHYSICAL].decode("utf-8")
 need("Old Rigidbody lost before rollback snapshot" in runtime and
      "linearVelocity" in runtime and "angularVelocity" in runtime,
      "R2 product physics rollback was not selected")
 return source

def private_case(m,source,index,nonce,helper):
 need(0<=index<4 and re.fullmatch("[0-9a-f]{12}",nonce) is not None,
      "invalid R2 per-case identity")
 product=f"B0160W27Rig{index}{nonce}"
 ps=source[m.PROJECT].decode("utf-8")
 for field,value in (("companyName",f"B0160W27{nonce}"),("productName",product)):
  pattern=r"(?m)^([ \t]*"+field+r": )[^\r\n]+$"
  need(len(re.findall(pattern,ps))==1,"ambiguous PlayerSettings "+field)
  ps=re.sub(pattern,lambda q:q.group(1)+value,ps,count=1)
 probe=("using System;\nusing NUnit.Framework;\nusing UnityEngine;\n"
        "namespace B01.CanonicalRigPortW24.Tests {\n"
        " public sealed class B0160W27ScopeProbe {\n"
        "  [Test] public void OnlyPrivateSaveScope() {\n"
       f'   Assert.IsTrue(Application.persistentDataPath.IndexOf("{product}",'
        'StringComparison.OrdinalIgnoreCase)>=0,"B0160_W27_WRONG_PRIVATE_SAVE");\n'
        "  }\n }\n}\n").encode()
 out=dict(source)
 need(PROBE not in out and PROBE+".meta" not in out,
      "QA-only test collided with original test source")
 out[m.PROJECT]=ps.encode()
 out[PROBE]=probe
 out[PROBE+".meta"]=helper.meta(PROBE)
 need(helper.guid_check(out)==helper.guid_check(source)+1,
      "W27 unique private probe GUID failed")
 return out,product

def main(argv=None):
 p=argparse.ArgumentParser()
 p.add_argument("--mode",choices=("preflight","native"),default="preflight")
 p.add_argument("--scratch-parent",default=os.environ.get("TEMP") or tempfile.gettempdir())
 p.add_argument("--unity",default="")
 p.add_argument("--source-g0-approved",action="store_true")
 p.add_argument("--operator-consent",default="")
 args=p.parse_args(argv)
 m=old();source=selected(m)
 n=m.original_module();helper=n.runner()
 need(helper.SHA==n.R5,"frozen original #718 ZIP verifier changed")
 helper.SHA=R2  # only in-memory inherited module, never modifies original ZIP source
 if args.mode=="native":
  need(os.name=="nt","native Unity G1 requires owner Windows")
  need(args.source_g0_approved,"independent B01-40/41 source-G0 for #792 required")
  need(args.operator_consent=="B0160 W27 PHYSICS NATIVE",
       "owner did not give explicit typed authorization")
  unity=Path(args.unity)
  need(unity.is_file() and not unity.is_symlink() and
       re.search(r"[/\\]6000\.3\.14f1[/\\]Editor[/\\]Unity\.exe$",str(unity),re.I),
       "Unity Hub Editor 6000.3.14f1 missing")
 else:unity=None
 base=n.safe_root(args.scratch_parent)
 receipt={"package":"B01-60-P17W27-RIG-R2-NATIVE-001",
          "original_physical_R2_sha":R2,"original_physical_parent_sha":PARENT,
          "original_750_R5":"60c73a6f9ed2585cbb9bf7aeaad8c62eef0629cb",
          "single_selected_787":"f5db40bf293fe9582ec7c6fcb3515aeff6fcf937",
          "whole_save_797":"2b7983f0f9e09dd1d56906b379a3f7af7b02f21a",
          "economy_780":"b4df10a7ad7e6c758f8331013f6b216b3a3689a3",
          "native_G1":"NOT_RUN","integration_G1":"NOT_RUN",
          "source_G0":"INDEPENDENT_PENDING","codex_scope":"RESERVED_NOT_WRITTEN",
          "has_live_game_binding":False,"integrated":False,"merged":False,
          "uploaded":False,"codex_ack":False,"test_methods":list(METHODS)}
 receipt_path=base/"P27-receipt.json"
 receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
 try:
  for i,method in enumerate(METHODS):
   scope,product=private_case(m,source,i,secrets.token_hex(6),helper)
   qa_receipt={"original_source_sha":R2,"native_unity":"NOT_RUN",
               "source_only_preflight":True,"integrated":False,"codex_ack":False,
               "native_meta_guids":helper.guid_check(scope),
               "file_sha256":{p:n.blake(v) for p,v in sorted(scope.items())}}
   archive=helper.zip_data(scope,qa_receipt)
   dest=base/("P"+str(i))
   helper.stage(archive,scope,dest)
   need((dest/m.PROJECT).read_bytes()==scope[m.PROJECT],
        "private PlayerSettings changed during package staging")
   if args.mode=="native":
    n.run_editor(unity,dest,base,"guard"+str(i),PROBE_FQCN,scope,helper)
    n.run_editor(unity,dest,base,"rigcase"+str(i),SUITE+method,scope,helper)
   else:helper.rollback(archive,scope,dest)
  if args.mode=="preflight":
   receipt["source_preflight"]="PASS_4_PRIVATE_ORIGINAL_792_STAGES_ROLLED_BACK"
   print("B0160_W27_FOUR_ORIGINAL_R2_PROJECTS_STAGE_ROLLBACK_PASS")
   print("B0160_W27_NATIVE_G1_NOT_RUN")
  else:
   receipt["native_G1"]="FOUR_R2_CASES_EXECUTED_INDEPENDENT_RAW_QA_PENDING"
   receipt["operator_consent"]=True
   print("B0160_W27_NATIVE_LOGS_CREATED_UNATTESTED_INDEPENDENT_G1_PENDING")
  receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  (base/"P27-sha256-manifest.json").write_text(json.dumps(
      n.manifest_files(base,receipt),sort_keys=True,indent=2)+"\n")
  print("B0160_W27_LOCAL_EVIDENCE_NO_AUTO_UPLOAD="+str(base))
  return 0
 except Exception as exc:
  receipt["source_or_native_error"]=str(exc)
  receipt_path.write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
  print("B0160_W27_FAIL_CLOSED="+str(exc),file=sys.stderr)
  print("B0160_W27_LOCAL_EVIDENCE_NO_AUTO_UPLOAD="+str(base))
  return 4 if args.mode=="native" else 2

if __name__=="__main__":
 try:sys.exit(main())
 except Exception as exc:
  print("B0160_W27_PRECHECK_HOLD="+repr(exc),file=sys.stderr)
  sys.exit(3 if "native" in sys.argv else 2)
