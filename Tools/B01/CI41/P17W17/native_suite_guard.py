#!/usr/bin/env python3
"""B01-41/W17: exact original #604 overlay scope + native PlayMode testcase inventory.

This tool never executes Unity. Pass means SOURCE/AUTHORED-NATIVE-READINESS ONLY.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys

BASE="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
ROOT="Assets/B0141SaveBridgeNativeQA"
NATIVE=ROOT+"/Tests/SaveBridgeFailureNativeTests.cs"
ASM=ROOT+"/Tests/B0141.SaveBridge.NativeQA.asmdef"
OWNED={
    ROOT+".meta",ROOT+"/Tests.meta",
    ASM,ASM+".meta",NATIVE,NATIVE+".meta",
    "Tools/B01/CI41/P17W17/native_suite_guard.py",
    "Tools/B01/CI41/P17W17/test_native_suite_guard.py",
    "Tools/B01/CI41/P17W17/README.md",
    ".github/workflows/b01-41-p17w17-native-negative-readiness.yml",
}
UPSTREAM={
    "Assets/B0123UnitySaveBridge001/Tests/IsolatedSaveWorldNativePlayModeTests.cs":
       "9b836abacaa4d3a995f765e0c964e19173da1528",
    "Assets/B0123UnitySaveBridge001/Tests/B0123.UnitySaveBridge.PlayModeTests.asmdef":
       "0013e3b5c5c39e24b00dea8e51e0507daaa856f8",
    "Assets/B0123UnitySaveBridge001/Unity/IsolatedTwinRigSaveHost.cs":
       "5012e98bba786c6b9702992c317586a0015678c1",
    "Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs":
       "399d3cb2074a2659af8f4e941edb28a3d2dd2832",
}
EXPECTED={
    "CorruptPersistentFence_QuarantinesFreshHostBeforePhysicalPublication":(
        "File.ReadAllBytes(fence)", "File.WriteAllBytes(fence,physical)",
        "Assert.AreEqual(88,physical.Length)", "Assert.IsTrue(fresh.Quarantined)",
        "Assert.AreEqual(0,freshGo.GetComponentsInChildren<Rigidbody>(true).Length)"),
    "StaleGenerationOnFreshHost_DeniedWithoutPublishingNewRigidbodies":(
        "Assert.AreEqual(WorldSwapVerdict.StaleGeneration,fresh.LastRestoreVerdict)",
        "Assert.IsFalse(fresh.HasWorld)", "Assert.IsTrue(fresh.TryRestoreBytes(newer,862,12))",
        "Assert.AreEqual(12,fresh.Generation)"),
    "ForeignRunSameFence_DeniedWithPriorWorldAndConsumedIdsIntact":(
        "Assert.AreEqual(WorldSwapVerdict.StaleGeneration,foreign.LastRestoreVerdict)",
        "Assert.IsTrue(first.HasWorld)", "Assert.IsTrue(first.IsConsumed(1,2310002))",
        "Assert.AreEqual(0,foreignGo.GetComponentsInChildren<Rigidbody>(true).Length)"),
    "InvalidSlotName_RejectsTraversalAndNeverEnablesPersistentFence":(
        "Assert.IsFalse(host.UsePersistentCheckpointSlot(\"../foreign\"))",
        "Assert.IsFalse(host.PersistentCheckpointEnabled)",
        "Assert.IsTrue(host.UsePersistentCheckpointSlot(slot))",
        "Assert.IsTrue(File.Exists(fence))"),
}
class Refuse(Exception):pass

def require(ok,code):
    if not ok: raise Refuse(code)

def git(repo,*args):
    z=subprocess.run(["git","-C",str(repo),*args],stdout=subprocess.PIPE,
                     stderr=subprocess.PIPE,check=False)
    require(z.returncode==0,"GIT_SOURCE_UNAVAILABLE:"+args[0])
    return z.stdout.decode("utf-8","replace").strip()

def source_methods(source):
    require("namespace B01.CI41.NativeSaveQA" in source,"WRONG_NATIVE_NAMESPACE")
    require("using UnityEngine.TestTools;" in source and
            "using NUnit.Framework;" in source,"MISSING_REAL_UNITY_TEST_IMPORT")
    require("[Ignore" not in source and "[Explicit" not in source and
            "#if" not in source,"DISABLED_NATIVE_TEST")
    pattern=r"\[UnityTest\]\s*public IEnumerator\s+([A-Za-z][A-Za-z_0-9]*)\s*\(\s*\)\s*\{"
    found=list(re.finditer(pattern,source))
    require(len(found)==len(EXPECTED),"NATIVE_CASE_INVENTORY_WRONG_COUNT")
    cases={}
    for i,match in enumerate(found):
        name=match.group(1)
        require(name not in cases,"DUPLICATE_NATIVE_CASE")
        start=match.end()
        depth=1
        end=start
        # Case bodies authored here have no braces inside quoted strings.
        while end<len(source) and depth:
            if source[end]=="{":depth+=1
            if source[end]=="}":depth-=1
            end+=1
        require(depth==0,"UNCLOSED_NATIVE_TEST_METHOD")
        cases[name]=source[start:end-1]
    require(set(cases)==set(EXPECTED),"NATIVE_CASES_MISMATCH")
    for name,body in cases.items():
        for token in EXPECTED[name]:
            require(token in body,"MISSING_NEGATIVE_ASSERTION:"+name)
        require("yield return null;" in body and "finally" in body and
                "Object.Destroy" in body and "File.Delete(fence)" in body,
                "MISSING_NATIVE_LIFECYCLE_OR_OWNED_CLEANUP:"+name)
        require("AddComponent<IsolatedTwinRigSaveHost>" in body,
                "NO_REAL_UNITY_HOST_IN_NATIVE_TEST:"+name)
        require("Assert.IsFalse" in body and "Assert.IsTrue" in body,
                "MISSING_POSITIVE_OR_NEGATIVE_ASSERTIONS:"+name)
    require("GetField(" not in source and "BindingFlags" not in source and
            "SecureDebrisPlaygroundV2" not in source,"UNAUTHORIZED_PRIVATE_GAME_HOOK")
    return sorted(cases)

def assembly_ok(data):
    require(type(data)==dict and data.get("name")=="B0141.SaveBridge.NativeQA" and
            data.get("rootNamespace")=="B01.CI41.NativeSaveQA",
            "WRONG_TEST_ASSEMBLY_IDENTITY")
    require(data.get("references")==[
        "B0123.UnitySaveBridge.Core",
        "B0123.UnitySaveBridge.Unity",
        "B0123.DetachedSaveContract.Runtime"],
        "WRONG_NATIVE_TEST_ASSEMBLY_DEPENDENCIES")
    require(data.get("includePlatforms")==["Editor"] and
            data.get("defineConstraints")==["UNITY_INCLUDE_TESTS"] and
            data.get("optionalUnityReferences")==["TestAssemblies"] and
            data.get("noEngineReferences") is False and
            data.get("overrideReferences") is True and
            data.get("precompiledReferences")==["nunit.framework.dll"],
            "NATIVE_ASMDEF_NOT_TEST_RUNNER_IMPORTABLE")

def validate_guid_entries(entries):
    found={}
    new=[]
    for path,raw in entries.items():
        match=re.findall(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",raw)
        require(len(match)==1,"MISSING_DUPLICATE_META_GUID:"+path)
        guid=match[0]
        require(guid not in found,"COLLIDING_UNITY_GUID:"+path)
        found[guid]=path
        if path.startswith(ROOT):new.append(guid)
    require(len(new)==4,"OWN_NEW_META_GUID_COUNT")
    return len(found)

def audit(repo):
    require(git(repo,"rev-parse","HEAD")!=BASE,"NO_NEW_INDEPENDENT_QA_FILES")
    for sha in (GAME,SAVE,BASE):
        require(git(repo,"rev-parse","--verify",sha+"^{commit}")==sha,
                "MISSING_IMMUTABLE_PARENT")
    git(repo,"merge-base","--is-ancestor",GAME,BASE)
    git(repo,"merge-base","--is-ancestor",SAVE,BASE)
    git(repo,"merge-base","--is-ancestor",BASE,"HEAD")
    actual=set(git(repo,"diff","--name-only",BASE,"HEAD").splitlines())
    require(actual==OWNED,"OUT_OF_SCOPE_OR_MISSING_QA_FILES")
    for file,sha in UPSTREAM.items():
        require(git(repo,"rev-parse",BASE+":"+file)==sha,"ORIGINAL_PRODUCER_BLOB_DRIFT")
        require(git(repo,"rev-parse","HEAD:"+file)==sha,"FROZEN_PRODUCER_FILE_MUTATED")
    cases=source_methods((repo/NATIVE).read_text(encoding="utf-8"))
    assembly_ok(json.loads((repo/ASM).read_text(encoding="utf-8")))
    paths=git(repo,"ls-tree","-r","--name-only","HEAD").splitlines()
    assets=[x for x in paths if x.startswith("Assets/") and x.endswith(".meta")]
    metas={x:(repo/x).read_text(encoding="utf-8") for x in assets if (repo/x).is_file()}
    # Checkout has every current HEAD path at the time this is invoked.
    require(len(metas)==len(assets),"UNREADABLE_META_FILES")
    guid_count=validate_guid_entries(metas)
    for path in (NATIVE,ASM,ROOT,ROOT+"/Tests"):
        require(path+".meta" in metas,"MISSING_NEW_QA_META:"+path)
    existing=(repo/UPSTREAM[next(iter(UPSTREAM))]).read_text(encoding="utf-8")
    producer_cases=re.findall(r"\[UnityTest\]\s*public IEnumerator\s+(\w+)\s*\(",existing)
    require(len(producer_cases)==5 and len(set(producer_cases))==5,
            "ORIGINAL_PRODUCER_NATIVE_CASES_CHANGED")
    require(set(cases).isdisjoint(producer_cases),"DUPLICATE_PRODUCER_NATIVE_CASE")
    return {
        "package":"CI41-NATIVE-SAVE-NEGATIVE-G1-001","version":"R1",
        "source_readiness":"SOURCE_CHECK_PASS",
        "frozen_510":GAME,"frozen_556":SAVE,
        "conditional_bridge_604":BASE,
        "conditional_selection_only":True,
        "new_native_tests_authored":cases,
        "original_producer_native_tests_authored":producer_cases,
        "total_authored_native_cases":len(cases)+len(producer_cases),
        "unity_meta_guid_count":guid_count,
        "qa_only_changed_paths":sorted(OWNED),
        "real_unity_editmode":"NOT_RUN","real_unity_playmode":"NOT_RUN",
        "real_physx":"NOT_RUN","source_g0_for_bridge":"NOT_GRANTED_BY_B01_41",
        "selected_adapter":"NONE_BY_B01_41",
        "canonical_510_save":"HOLD","codex_ack":False,"g2":"HOLD",
        "release_gate":"HOLD"}
def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--output",required=True)
    opt=p.parse_args()
    try:
        out=audit(Path(opt.repo))
        code=0
    except (Refuse,ValueError,UnicodeError,OSError) as exc:
        out={"package":"CI41-NATIVE-SAVE-NEGATIVE-G1-001",
             "source_readiness":"FAIL_CLOSED","reason":str(exc),
             "real_unity_playmode":"NOT_RUN","release_gate":"HOLD"}
        code=2
    s=json.dumps(out,indent=2,sort_keys=True)+"\n"
    Path(opt.output).write_text(s,encoding="utf-8")
    print(s,end="")
    return code
if __name__=="__main__":
    sys.exit(main())
