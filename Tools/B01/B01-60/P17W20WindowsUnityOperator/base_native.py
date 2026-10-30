#!/usr/bin/env python3
"""Own W20 adapter of the FROZEN B01-60 #532+#601 native V2 runner.

No new tests or copied gameplay source. The original runner still enforces
original 510+W06 exact source, exact 2 EditMode +14 PlayMode case fullnames,
NUnit XML, Editor.log Unity6000.3.14f1 and real process result. Its raw G1
verdict is scoped here to secure-reattach V2 ONLY, not R4 or canonical Save.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parents[4]
ORIGINAL=ROOT/"Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
W19_SHA="680e3e8127d076842ceec006efb95d8763681587"
GAME_SHA="d94400d95d13db7f27e1533df05c9292d5380431"
SRC_FILES={
 "Tools/B01/B01-60/P17W17NativeAdapterCoverage/audit_native_adapter_coverage.py",
 "Tools/B01/B01-60/P17W17NativeAdapterCoverage/README.md",
 "Tools/B01/B01-60/P17W17NativeAdapterCoverage/manifest.json",
 ".github/workflows/b01-60-p17w17-native-adapter-coverage.yml",
 "Tools/B01/B01-60/P17W18Selected596Native/run.py",
 "Tools/B01/B01-60/P17W18Selected596Native/test_preflight.py",
 "Tools/B01/B01-60/P17W18Selected596Native/README.md",
 "Tools/B01/B01-60/P17W18Selected596Native/manifest.json",
 ".github/workflows/b01-60-p17w18-selected596-native-isolated.yml",
 "Tools/B01/B01-60/P17W19Selected642Native/run.py",
 "Tools/B01/B01-60/P17W19Selected642Native/test_source_fixture.py",
 "Tools/B01/B01-60/P17W19Selected642Native/README.md",
 "Tools/B01/B01-60/P17W19Selected642Native/manifest.json",
 ".github/workflows/b01-60-p17w19-selected642-native-fixture.yml",
 "Tools/B01/B01-60/P17W20WindowsUnityOperator/Invoke-B0160WindowsUnity.ps1",
 "Tools/B01/B01-60/P17W20WindowsUnityOperator/base_native.py",
 "Tools/B01/B01-60/P17W20WindowsUnityOperator/test_contract.py",
 "Tools/B01/B01-60/P17W20WindowsUnityOperator/README.md",
 "Tools/B01/B01-60/P17W20WindowsUnityOperator/manifest.json",
 ".github/workflows/b01-60-p17w20-windows-local-operator.yml",
}
def need(ok,why):
 if not ok:raise AssertionError(why)

def original_module():
 spec=importlib.util.spec_from_file_location("b0160_w20_original_v2_native",ORIGINAL)
 need(spec is not None and spec.loader is not None,"frozen B01-60 original native runner missing")
 m=importlib.util.module_from_spec(spec)
 spec.loader.exec_module(m)
 need(m.SHA==GAME_SHA,"original selected #510 game SHA changed")
 need(m.git("rev-parse",W19_SHA+":Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py").decode().strip()
      =="99003aa6a1a07f3bfde0aaa98b91a06159efae18",
      "hardened W19 native runner Git blob unexpectedly changed")
 need(m.git("merge-base",W19_SHA,"HEAD").decode().strip()==W19_SHA,
      "B01-60 W20 not descendant of exact original W19")
 require_new=set(m.git("diff","--name-only",W19_SHA,"HEAD").decode().splitlines())
 need(require_new==SRC_FILES-set(x for x in SRC_FILES if "/P17W20" not in x and "w20-" not in x),
      "new W20 source changes not exactly 6 own test/operator paths")
 m.OWN.update(SRC_FILES)
 return m

def main():
 ap=argparse.ArgumentParser()
 ap.add_argument("--mode",choices=("preflight","native"),default="preflight")
 ap.add_argument("--unity",default="")
 ap.add_argument("--out",required=True)
 args=ap.parse_args()
 m=original_module()
 out=Path(args.out).expanduser().resolve()
 need(not out.exists(),"evidence path already exists: refuse overwrite")
 native=(args.mode=="native")
 need(not native or bool(args.unity),"native selected without explicit Unity.exe")
 old=sys.argv
 sys.argv=[str(ORIGINAL),"--mode",args.mode,"--out",str(out)]
 if native:sys.argv.extend(["--unity",args.unity])
 try:ret=m.main()
 finally:sys.argv=old
 need(ret in (0,3,4),"unexpected original source/native runner return")
 report={"package":"B01-60-W20-WINDOWS-MANUAL-UNITY-OPERATOR",
         "original_game_SHA":GAME_SHA,"original_QA_prior_W19_SHA":W19_SHA,
         "real_Editor_invoked":native,"original_return_code":ret,
         "only_v2_16_case_native":ret==0 and native,
         "selected_r4_host_native":"NOT_RUN",
         "canonical_live_game_save":"HOLD",
         "full_unity_save_G1":"NOT_VERIFIED_BY_V2_ONLY",
         "release":"HOLD","merged":False}
 # Create scoped status only after runner has generated its real source receipt;
 # do not overwrite any original XML, log, ZIP or original verdict.
 if out.is_dir():
  (out/"b0160-w20-v2-only-scope.json").write_text(
   json.dumps(report,sort_keys=True,indent=2)+"\n",encoding="utf-8")
 if ret==0:
  print("B01_60_W20_SOURCE_V2_PREFLIGHT_PASS" if not native else
        "B01_60_W20_GENUINE_NATIVE_V2_16_ONLY_NOT_SELECTED_R4_SAVE")
 elif ret==3:print("B01_60_W20_NATIVE_G1_NOT_RUN_MISSING_EDITOR")
 else:print("B01_60_W20_NATIVE_G1_HOLD_OR_FAIL")
 return ret
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W20_FAIL_CLOSED",repr(e),file=sys.stderr)
  sys.exit(2)
