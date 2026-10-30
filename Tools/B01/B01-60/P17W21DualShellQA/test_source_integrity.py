#!/usr/bin/env python3
"""B01-60 W21 original source-locked native operator cross-shell gate.
No Windows shell or Unity is emulated here. Real shells run on Windows CI.
"""
from __future__ import annotations
import importlib.util
from pathlib import Path
import re
import sys

ROOT=Path(__file__).resolve().parents[4]
ORIGINAL=ROOT/"Tools/B01/B01-60/P17W12NativeSecureV2QA/runner.py"
BASE="c75cb6e52a0de11f27b570c34e08d32082af421f"
SHELL=ROOT/"Tools/B01/B01-60/P17W21DualShellQA/test_windows_shells.ps1"
HISTORICAL={
 "Tools/B01/B01-60/P17W20WindowsUnityOperator/Invoke-B0160WindowsUnity.ps1":"6e9b549ae36e9fbf6ba9260aeb1b4739d997fed0",
 "Tools/B01/B01-60/P17W20WindowsUnityOperator/base_native.py":"fa616317eb0cae955b31c360867022343fec5498",
}
W21={
 "Tools/B01/B01-60/P17W21DualShellQA/test_windows_shells.ps1",
 "Tools/B01/B01-60/P17W21DualShellQA/test_source_integrity.py",
 "Tools/B01/B01-60/P17W21DualShellQA/README.md",
 "Tools/B01/B01-60/P17W21DualShellQA/manifest.json",
 ".github/workflows/b01-60-p17w21-real-dual-windows-shell.yml",
}
R4="09c1809b73324a6d31c13e5d0143de62ac1e3ca6"
ALT="ae5dbfa5e9c4689d2bf80fcabf8438e9c673c419"
def need(ok,message):
 if not ok:raise AssertionError(message)
def original():
 spec=importlib.util.spec_from_file_location("b0160_original_frozen_v2_native",ORIGINAL)
 need(spec is not None and spec.loader is not None,"original secured native runner absent")
 m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 return m
def contract(text):
 required={
 "PS5_1":r'WindowsPowerShell51',
 "PS7":r'PowerShell7',
 "PS5_1_real_exe":r'"powershell[.]exe"',
 "PS7_real_exe":r'"pwsh[.]exe"',
 "exact_unmodified_W20":r"6e9b549ae36e9fbf6ba9260aeb1b4739d997fed0",
 "original_local_operator":r"Invoke-B0160WindowsUnity[.]ps1",
 "original_source_pinned":r"git[ ]@arg",
 "genuine_shell_calls":r"&[ ]\$exe[ ]@argsList",
 "original_receipt":r"VerifyEvidence",
 "sha256_each_original":r"Get-FileHash[ ]-LiteralPath[ ]\$file[ ]-Algorithm[ ]SHA256",
 "native_no_fake_g1":r'original-V2-native-only/native-verdict[.]json',
 "missing_editor_exit3":r'\$rc -eq 3',
 "original_modified_exit2":r'\$rc -eq 2.*Local operator script modified since Git HEAD',
 "own_game_root_unsafe":r'Scratch parent must be disjoint',
 "separate_temp_clone":r"clone --no-hardlinks --no-checkout",
 "manifest_unsigned":r'\$list[.]unsigned',
 }
 for label,pattern in required.items():
  need(re.search(pattern,text)!=None,"W21 actual dual-shell test source invariant omitted: "+label)
 forbidden=(r"(?im)^[ \t]*Remove-Item\b",r"(?im)^[ \t]*Invoke-WebRequest\b",
            r"(?im)^[ \t]*git push\b",r"(?im)^[ \t]*ssh\b")
 for pattern in forbidden:need(not re.search(pattern,text),"unsafe shell test operation")
 return True
def main():
 m=original()
 need(m.git("merge-base",BASE,"HEAD").decode().strip()==BASE,
      "W21 is not stacked on frozen W20")
 changes=set(m.git("diff","--name-only",BASE,"HEAD").decode().splitlines())
 need(changes==W21,"W21 modified any prior own producer/QA or has missing files: "+repr(changes^W21))
 for p,blob in HISTORICAL.items():
  need(m.git("rev-parse",BASE+":"+p).decode().strip()==blob,
       "frozen W20 Git blob mismatch "+p)
  need(m.git("rev-parse","HEAD:"+p).decode().strip()==blob,
       "W21 altered frozen W20 local Unity operator "+p)
 # Exact old #532 native source guard remains the authority for real frozen
 # selected game bytes/Unity .meta GUIDs and 2+14 original NUnit identities.
 m.OWN.update(HISTORICAL)
 m.OWN.update(W21)
 # Prior own W17-20 files as a historical SOURCE inventory, not edits.
 prior=m.git("diff","--name-only",m.SHA,BASE).decode().splitlines()
 m.OWN.update(prior)
 found=m.named_sources()
 need(len(found)>=25,"original native V2 source stage incomplete")
 need(len(m.expected_independent_native_cases("EditMode"))==2 and
      len(m.expected_independent_native_cases("PlayMode"))==14,
      "old original Unity independent native suite drift")
 need(m.SHA=="d94400d95d13db7f27e1533df05c9292d5380431",
      "original game source SHA altered")
 # Do not confuse metadata selection with an accepted safe R4 release.
 need(R4!=ALT,"owner selected R4 and losing ALT identical")
 test=SHELL.read_text("utf-8")
 contract(test)
 mutations=[
  ("PS5.1",'"powershell.exe"','"fake-shell.exe"'),
  ("PowerShell7",'"pwsh.exe"','"fake-shell.exe"'),
  ("Git blob pin","6e9b549ae36e9fbf6ba9260aeb1b4739d997fed0","0"*40),
  ("real process",'& $exe @argsList','Write-Output @argsList'),
  ("missing editor","$rc -eq 3","$rc -eq 0"),
  ("digest","Get-FileHash -LiteralPath $file -Algorithm SHA256","Get-Item -LiteralPath $file"),
  ("source cleanup guard","Scratch parent must be disjoint","UNSAFE_W21_SCRATCH_ROOT_ACCEPTED"),
  ("unsigned proof","$list.unsigned","$list.fake_signature"),
 ]
 negatives=0
 for desc,old,new in mutations:
  need(old in test,"test-only source mutation marker missing: "+desc)
  tampered=test.replace(old,new,1)
  try:contract(tampered)
  except AssertionError:negatives+=1
  else:raise AssertionError("W21 source test contract failed OPEN: "+desc)
 need(negatives==8,"not all eight W21 test-gate hostile mutations rejected")
 print("B01_60_W21_ORIGINAL_FROZEN_W20_RUNNER_GIT_BLOBS_UNCHANGED_2_14_EXACT_SOURCE_PASS")
 print("B01_60_W21_SOURCE_ONLY_W21_FIVE_OWN_PATHS_PIN_PASS")
 print("B01_60_W21_DUAL_WINDOWS_SHELL_ADVERSARIAL_8_8_SOURCE_MUTATIONS_DENIED")
 print("B01_60_W21_PYTHON_DOES_NOT_SIMULATE_REAL_WINDOWS_POWERSHELL_OR_UNITY")
 print("B01_60_W21_UNITY6000_3_14f1_G1_NOT_RUN_CANONICAL_SAVE_HOLD")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as err:
  print("B01_60_W21_SOURCE_ONLY_FAIL_CLOSED",repr(err),file=sys.stderr)
  sys.exit(2)
