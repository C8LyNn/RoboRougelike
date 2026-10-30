#!/usr/bin/env python3
"""Independent static safety contract test for W20 Windows local operator.

Runs unchanged original #532 Python source gate and ten adversarial PowerShell
text mutations. These DO NOT emulate Unity, Windows PowerShell or PhysX.
Actual Windows PowerShell dry run and negative are in the Windows CI job.
"""
import importlib.util
import json
from pathlib import Path
import re
import sys

ROOT=Path(__file__).resolve().parents[4]
HERE=ROOT/"Tools/B01/B01-60/P17W20WindowsUnityOperator"
PS=HERE/"Invoke-B0160WindowsUnity.ps1"
PY=HERE/"base_native.py"
def need(cond,msg):
 if not cond:raise AssertionError(msg)
def module():
 spec=importlib.util.spec_from_file_location("b0160_w20_base_native_original",PY)
 need(spec is not None and spec.loader is not None,"new source wrapper absent")
 m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 return m
def safety_gate(s):
 requirements={
  "one_time_native_switch":r"\[switch\]\$Run",
  "typed_operator_confirmation":r'Read-Host\s+"To authorize ONE local native run',
  "exact_confirmation_phrase":r'\$answer\s+-ceq\s+"B0160 RUN NATIVE"',
  "no_native_without_switch":r"if\(-not \$Run\)",
  "no_native_without_editor":r'Need \$ready "Unity Hub 6000\.3\.14f1 missing',
  "local_repo_git_clone_only":r"git -c core\.hooksPath=NUL clone --no-hardlinks --no-checkout -- \$repo \$co",
  "immutable_checkout":r'checkout","--detach",\$SourceSha',
  "disjoint_scratch_from_game":r'ChildOf \$parent \$repo',
  "refuse_existing_scratch":r'Need \(-not\(Test-Path -LiteralPath \$scratch\)\)',
  "original_operator_script_blob":r'HEAD:\$me',
  "exact_unity_version":r'6000\\\.3\\\.14f1',
  "exact_unity_test_framework":r"com\.unity\.test-framework",
  "real_v2_only_verdict":r"PASS_V2_ONLY_R4_STILL_NOT_RUN",
  "scope_save_hOLD":r"canonical_SaveLoad",
  "sha256_manifest":r"Get-FileHash -LiteralPath \$f\.FullName -Algorithm SHA256",
  "not_supposed_to_sign":r"NOT a cryptographic signature",
  "no_cleanup_proof":r"NO_CLEANUP_NO_UPLOAD_NO_MERGE",
  "no_license_bypass":r"Verify legitimate Unity license via your own Unity Hub",
 }
 for label,pattern in requirements.items():
  need(re.search(pattern,s,re.I)!=None,"PowerShell operator missing required gate "+label)
 banned=(r"(?m)^\s*Remove-Item\b",
         r"(?m)^\s*Invoke-WebRequest\b",
         r"(?m)^\s*Invoke-RestMethod\b",
         r"(?m)^\s*Enable-PSRemoting\b",
         r"(?m)^\s*Register-ScheduledTask\b",
         r"(?m)^\s*git\s+push\b",
         r"(?m)^\s*ssh\b")
 for x in banned:need(re.search(x,s,re.I)==None,"Forbidden privileged/network/deletion action "+x)
 need("function Git(" not in s and "function Invoke-LocalGit(" in s,
      "PowerShell function shadows git.exe")
 return True
def main():
 m=module()
 o=m.original_module() # actual original frozen #532 and strict owned-only Git gates
 need(o.SHA==m.GAME_SHA,"wrong original selected V2 SHA")
 need(len(o.PLAY_CASES)==14 and len(o.EDIT_CASES)==2,
      "original native suites unexpectedly changed")
 source=PS.read_text("utf-8")
 safety_gate(source)
 probes=[
  ("missing operator prompt",'Read-Host "To authorize ONE local native run','Read-Suppressed "To authorize ONE local native run'),
  ("weaken typed consent",'$answer -ceq "B0160 RUN NATIVE"','$answer -eq $answer'),
  ("remove native switch",'[switch]$Run','[string]$Run'),
  ("disable missing unity guard",'Need $ready "Unity Hub 6000.3.14f1 missing','Need $true "Unity Hub 6000.3.14f1 missing'),
  ("unsafe remote clone",'git -c core.hooksPath=NUL clone --no-hardlinks --no-checkout -- $repo $co','git clone https://example.invalid/arbitrary/runner $co'),
  ("skip disjoint workspace",'ChildOf $parent $repo','DisabledChild $parent $repo'),
  ("allow existing scratch",'Need (-not(Test-Path -LiteralPath $scratch))','Need ($true)'),
  ("skip script pin",'HEAD:$me','HEAD:$unknown'),
  ("remove source hash",'Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256','Get-Length -LiteralPath $f.FullName'),
  ("disable no-delete boundary",'NO_CLEANUP_NO_UPLOAD_NO_MERGE','DELETE_ALL_EXTERNAL_TREE'),
 ]
 denied=0
 for label,old,new in probes:
  need(old in source,"unrecognized original source contract "+label)
  modified=source.replace(old,new,1)
  try:safety_gate(modified)
  except AssertionError:denied+=1
  else:raise AssertionError("unsafe PowerShell change not denied: "+label)
 need(denied==10,"exact ten source tamper controls not rejected")
 print("B01_60_W20_ORIGINAL_FROZEN_V2_SUITE_2_EDIT_14_PLAY_PIN_PASS")
 print("B01_60_W20_POWERSHELL_SOURCE_SAFETY_CONTRACT_PASS")
 print("B01_60_W20_ADVERSARIAL_SOURCE_SAFETY_NEGATIVES_PASS",denied)
 print("B01_60_W20_REAL_WINDOWS_POWERSHELL_AND_UNITY_NOT_SIMULATED_BY_PYTHON")
 out=ROOT/"qa-w20-windows-operator-static-source-evidence"
 need(not out.exists(),"no overwriting old receipt")
 out.mkdir()
 (out/"receipt.json").write_text(json.dumps({
  "package":"B01-60-P17W20-WINDOWS-LOCAL-UNITY",
  "original_game_sha":m.GAME_SHA,"original_prior_QA":m.W19_SHA,
  "original_native_cases_authored":{"EditMode":2,"PlayMode":14},
  "adversarial_source_safety_denials":denied,
  "actual_unity":"NOT_RUN","actual_windows_powershell":"NOT_EXECUTED_BY_THIS_PYTHON_TEST",
  "native_G1":"NOT_RUN","selected_r4":"HOLD",
  "canonical_save":"HOLD","Codex_ack":"NONE","merged":False,
 },sort_keys=True,indent=2)+"\n",encoding="utf-8")
 return 0
if __name__=="__main__":
 try:sys.exit(main())
 except Exception as e:
  print("B01_60_W20_STATIC_SOURCE_GATE_FAIL",repr(e),file=sys.stderr)
  sys.exit(2)
