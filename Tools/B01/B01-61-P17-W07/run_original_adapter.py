#!/usr/bin/env python3
"""B01-61 independent executable original B01-23 #421 source adapter QA.
DOES NOT execute Unity/PhysX; an isolated strict managed-only Rigidbody call stub
is substituted, while the producer's full original actual C# files are compiled.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

REPO_SOURCE="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
CORE="Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs"
EMITTER="Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs"
ASM="Assets/B0123DebrisRepulse001/Runtime/B0123.DebrisRepulse.Runtime.asmdef"
SOURCES={
 CORE:"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
 EMITTER:"2b3a8465c550bd49a5c98be5a8064a4ce0947f4b",
 ASM:"25a49dc836b62d791c0bf40c214c1c6cf09a9fea",
 "Assets/B0123DebrisRepulse001.meta":"4900cc8af114ab7fd40fe65429a4c3d8dd4be5cf",
 "Assets/B0123DebrisRepulse001/Runtime.meta":"5e4ad70a12ace046c7712feccb9fc0cf11cc4e61",
 ASM+".meta":"5576a828cb49cec88c79049f70c40d81dd13c4a9",
 CORE+".meta":"ab3b0e3d291942bf90ca64583ab54e6511828dff",
 EMITTER+".meta":"e99a0e675058abcd60dc28d1191c02962af75c51",
}
ROOT=Path(__file__).resolve().parent
MUTANTS={
 "remove_kinematic_guard":(EMITTER,"body == null || body.isKinematic","body == null"),
 "remove_one_shot_ledger_guard":(CORE,"seen.Contains(id) ||",""),
 "force_mode_not_impulse":(EMITTER,"ForceMode.Impulse","ForceMode.Force"),
 "zero_impulse_always_added":(EMITTER,"if (impulse.MagnitudeSquared > 0)","if (true)"),
 "ignore_actual_rigidbody_velocity":(EMITTER,"Vector3 velocity = body.linearVelocity;","Vector3 velocity = new Vector3(0,0,0);"),
}
GUID_RE=re.compile(rb"(?m)^guid: ([a-fA-F0-9]{32})\s*$")
PASS_RE=re.compile(r"B0161_W07_ORIGINAL_EMITTER_TESTS=(\d+) PASS / (\d+) FAIL")

def check(condition,message):
    if not condition:
        raise AssertionError(message)

def git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],capture_output=True)
    check(p.returncode==0,"git "+str(args)+": "+p.stderr.decode(errors="replace")[-400:])
    return p.stdout

def get_original(repo):
    check(git(repo,"rev-parse",REPO_SOURCE+"^{commit}").decode().strip()==REPO_SOURCE,
          "original producer immutable commit not fetched")
    content={}
    for path,sha in SOURCES.items():
        real=git(repo,"rev-parse",REPO_SOURCE+":"+path).decode().strip()
        check(real==sha,"producer original Git blob SHA mismatch "+path+" "+real)
        content[path]=git(repo,"show",REPO_SOURCE+":"+path)
        gitblob=hashlib.sha1(b"blob "+str(len(content[path])).encode()+b"\0"+content[path]).hexdigest()
        check(gitblob==sha,"corrupt original data "+path)
    defs=json.loads(content[ASM])
    check(defs["name"]=="B0123.DebrisRepulse.Runtime","Unexpected original Unity assembly name")
    check(defs["autoReferenced"] is False,"Original Unity asmdef auto-referenced contract changed")
    check(defs["noEngineReferences"] is False,"Original Unity adapter needs engine references")
    guids={}
    for path,payload in content.items():
        if path.endswith(".meta"):
            ids=GUID_RE.findall(payload)
            check(len(ids)==1,"bad Unity .meta guid "+path)
            guid=ids[0].lower()
            check(guid not in guids,"duplicate original producer Unity GUID "+path)
            guids[guid]=path
    check(len(guids)==5,"Expected exactly five original producer Unity GUIDs")
    print("ORIGINAL_B01_23_UNMODIFIED_GIT_BLOBS=8/8 PASS | ASSEMBLY=ISOLATED_AUTO_REFERENCED_FALSE | GUIDS=5/5 UNIQUE")
    return content

def compile_execute(temp,originals,label):
    project=Path(temp)
    project.mkdir(parents=True,exist_ok=True)
    for name in ("Program.cs","UnityEngineBoundaryStub.cs","DebrisEmitterQA.csproj"):
        (project/name).write_bytes((ROOT/name).read_bytes())
    (project/"DebrisImpulseCore.cs").write_bytes(originals[CORE])
    (project/"DebrisRepulsionEmitter.cs").write_bytes(originals[EMITTER])
    cmd=["dotnet","run","--project",str(project/"DebrisEmitterQA.csproj"),"--configuration","Release"]
    try:
        p=subprocess.run(cmd,capture_output=True,text=True,timeout=120,check=False)
    except subprocess.TimeoutExpired:
        raise AssertionError("managed receiver timeout "+label)
    out=p.stdout+"\n"+p.stderr
    summary=PASS_RE.findall(out)
    check(len(summary)==1,"test runner did not execute: "+label+" "+out[-1200:])
    good,bad=map(int,summary[0])
    return {"label":label,"exit":p.returncode,"pass":good,"fail":bad,
            "stdout_sha256":hashlib.sha256(out.encode()).hexdigest(),
            "detected_failures":[line for line in out.splitlines() if line.startswith("FAIL ")]},out

def run(repo,output):
    original=get_original(repo)
    base=ROOT/"Program.cs"
    test_count=len(re.findall(r'\bTest\("',base.read_text(encoding="utf8")))
    check(test_count==47,"Independent tests disappeared or silently changed")
    reports=[]
    with tempfile.TemporaryDirectory(prefix="b01_61_original_emitter_qa_") as root:
        good,log=compile_execute(Path(root)/"original",original,"genuine_original_source")
        check(good["exit"]==0 and good["pass"]==47 and good["fail"]==0,
              "UNMODIFIED ORIGINAL RUNTIME ADAPTER SOURCE QA FAILED "+log[-1600:])
        print("ORIGINAL_CSHARP_ADAPTER_COMPILED_AND_EXECUTED=PASS | MANAGED_BOUNDARY_47/47 PASS")
        reports.append(good)
        for name,(file,old,new) in MUTANTS.items():
            text=original[file].decode("utf8")
            check(text.count(old)==1,"mutation anchor wrong source count "+name)
            poisoned=dict(original)
            poisoned[file]=text.replace(old,new,1).encode("utf8")
            result,stdout=compile_execute(Path(root)/name,poisoned,name)
            # Compile errors or skipped/missing tests are NOT valid mutant kills.
            check(result["exit"]!=0 and result["fail"]>=1 and result["pass"]+result["fail"]==47
                  and result["detected_failures"],
                  "FALSE_GREEN_MUTATION / NOT_ACTUAL_TEST_FAILURE "+name+" "+stdout[-1000:])
            print("DELIBERATE_SOURCE_MUTANT_REJECTED="+name+" | FAILS="+str(result["fail"]))
            reports.append(result)
    check(not output.exists(),"refusing to overwrite existing original QA evidence")
    report={
      "schema":"B01-61-P17-W07-DEBRIS-EMITTER-ORIGINAL-ADAPTER-SOURCE-001",
      "producerPR":421,"producerSha":REPO_SOURCE,
      "originalBlobSha1":SOURCES,
      "originalManagedAdapterTests":47,"mutantKills":len(MUTANTS),
      "runs":reports,
      "type":"ACTUAL_ORIGINAL_CSHARP_CORE_AND_UNITY_ADAPTER_WITH_MANAGED_RIGIDBODY_STUB",
      "trueUnityEngineCompiled":False,"truePhysXExecuted":False,
      "unityNative":"NOT_RUN","playerGameIntegrated":False,
      "codexAck":"NOT_OBSERVED","g0":"INDEPENDENT_STUB_SOURCE_QA_PASS_ONLY",
      "nativeG1":"NOT_RUN"}
    raw=(json.dumps(report,sort_keys=True,indent=2)+"\n").encode("utf8")
    output.write_bytes(raw)
    print("ORIGINAL_RECEIPT_SHA256="+hashlib.sha256(raw).hexdigest())
    print("B0161_W07_ORIGINAL_EMITTER_QA=PASS | ORIGINAL_TESTS=47/47 | MUTANTS=5/5_KILLED_BY_ASSERTIONS | UNITY_NATIVE=NOT_RUN | CODEX_ACK=NO")
    return report

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--repo",default=".")
    parser.add_argument("--output",default="debris_original_adapter_qa.json")
    args=parser.parse_args()
    check(shutil.which("dotnet") is not None,"dotnet SDK 8 required")
    run(Path(args.repo).resolve(),Path(args.output))
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except (AssertionError,OSError,ValueError,KeyError,subprocess.CalledProcessError) as error:
        print("B0161_W07_ADAPTER_INDEPENDENT_QA_RED="+repr(error),file=sys.stderr)
        sys.exit(2)
