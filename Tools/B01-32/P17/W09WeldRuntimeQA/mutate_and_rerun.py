#!/usr/bin/env python3
"""Four live behavior mutants against unmodified original MonoBehaviour;
each recompiles original C# against test-only UnityEngine mock, and MUST RED.
"""
import json,os,subprocess
from pathlib import Path
ROOT=Path("build/W09WeldReceiver/Original")
SRC=ROOT/"B0132RepairWeldVfx.cs"
assert SRC.is_file(),"first run immutable physical Git/ZIP intake"
unchanged=SRC.read_bytes()
source=unchanged.decode()
patterns=[
  ("SKIP_CANCEL_ON_VISUAL_OFF","if (!enabled) CancelWeld();","if (false) CancelWeld();"),
  ("SKIP_CORE_CANCEL_AT_CANCEL","public void CancelWeld()\n        {\n            core.Cancel();",
       "public void CancelWeld()\n        {\n            // core.Cancel();"),
  ("SKIP_CORE_CANCEL_ON_DISABLE","private void OnDisable()\n        {\n            core.Cancel();",
       "private void OnDisable()\n        {\n            // core.Cancel();"),
  ("LEAK_ENABLED_SPARKS_AT_CANCEL","if (sparks[i] != null) sparks[i].enabled = false;",
       "if (sparks[i] != null) sparks[i].enabled = true;")
]
results=[]
try:
 for label,old,new in patterns:
    assert source.count(old)==1,"mutant match unsafe/nonunique: "+label
    SRC.write_text(source.replace(old,new,1))
    p=subprocess.run(["dotnet","run","-c","Release","--project",
         "Tools/B01-32/P17/W09WeldRuntimeQA/Runner/Runner.csproj"],
         stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=90)
    failword="W09_UNCHANGED_WELD_RUNTIME_RED" in p.stdout or "AUTHORED_ASSERT_" in p.stdout
    if p.returncode==0 or not failword:
       raise AssertionError("W09_MUTANT_FALSE_GREEN "+label+" exit="+str(p.returncode)+
                            "\n"+p.stdout[-3500:])
    results.append({"mutation":label,"rejected":True,"exit":p.returncode})
    print("W09_REAL_ORIGINAL_MONOBEHAVIOUR_MUTANT_REJECT_PASS",label)
finally:
    SRC.write_bytes(unchanged)
assert SRC.read_bytes()==unchanged,"mutations must not alter the frozen original source"
p=subprocess.run(["dotnet","run","-c","Release","--project",
 "Tools/B01-32/P17/W09WeldRuntimeQA/Runner/Runner.csproj"],
 stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=90)
assert p.returncode==0,"restored original must GREEN:\n"+p.stdout[-3500:]
Path("build/W09WeldReceiver/BEHAVIOUR_MUTANT_RECEIPT.json").write_text(
 json.dumps({"original_restored_pass":True,"native_unity":"NOT_RUN",
             "mutation_tests":results},indent=2)+"\n")
print("W09_CSHARP_RUNTIME_MUTATIONS_RED_TO_GREEN_PASS 4/4 rejected, original exact source restored and rerun GREEN")
