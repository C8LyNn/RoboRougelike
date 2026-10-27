#!/usr/bin/env python3
"""B01-61 source integrity only. Does not compile or execute C# or Unity."""
import argparse, json, pathlib, re, subprocess, sys
DIR=pathlib.Path(__file__).resolve().parent
def git(*args):
    return subprocess.check_output(["git",*args],text=True,stderr=subprocess.STDOUT).strip()
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--fixture-only",action="store_true")
    opt=ap.parse_args()
    m=json.loads((DIR/"manifest.json").read_text(encoding="utf-8"))
    f=DIR/m["fixture"]
    s=f.read_text(encoding="utf-8")
    checks=[]
    def ck(n,v): checks.append((n,bool(v)))
    ck("QA-only source owned",DIR.name=="B01-61-PASS12")
    ck("fixture original Git SHA",git("hash-object",str(f))==m["fixture_git_blob"])
    ck("exact 1 [Test] and 1 [UnityTest]",s.count("[Test]")==1 and s.count("[UnityTest]")==1)
    for name in m["expected_tests"]:
        ck("authored test "+name,bool(re.search(r"\b"+re.escape(name)+r"\s*\(",s)))
    ck("batch isolation", "Application.isBatchMode" in s and "Assets/scene 1.unity" in s)
    ck("bounded waits", s.count("Time.realtimeSinceStartup+30")>=2)
    ck("safe Unity teardown", "[UnityTearDown]" in s and "new ExitPlayMode()" in s)
    ck("original vs test copy", "Is.SameAs(garageModel)" in s and "Is.Not.SameAs(garageModel)" in s)
    ck("weapon A/B acceptance", "WeaponGroupA" in s and "WeaponGroupB" in s)
    ck("Assist vs Full acceptance", "AssistOnly" in s and "FullSupport" in s)
    ck("identity and Scrap", "partIds" in s and "gunId" in s and "Is.EqualTo(scrap)" in s)
    ck("map and pause return", "Is.EqualTo(mapBefore)" in s and "ReturnFromEditedMapTest()" in s)
    ck("native unrun declared", all(m[k]=="NOT_RUN" for k in ("Unity_EditMode","Unity_PlayMode","Windows_Player")))
    if not opt.fixture_only:
        sha=m["codex_sha"]
        ck("pinned Codex commit exists",git("-C",opt.repo,"rev-parse",sha+"^{commit}")==sha)
        for path,expected in m["original_git_blobs"].items():
            ck("original Git C# blob "+path,git("-C",opt.repo,"rev-parse",sha+":"+path)==expected)
        model=git("-C",opt.repo,"show",sha+":Assets/R3/R3VehicleModel.cs")
        editor=git("-C",opt.repo,"show",sha+":Assets/MapEditor/R1MapEditor.cs")
        susp=git("-C",opt.repo,"show",sha+":Assets/MOV009Integration/MOV009RunController.cs")
        ck("original group clone", "copy.weaponGroupA=weaponGroupA;copy.weaponGroupB=weaponGroupB" in model)
        ck("original suspension clone", "copy.SuspensionMode=SuspensionMode;copy.SuspensionHeightOffset=SuspensionHeightOffset" in model)
        ck("original map test snapshot", "workshopModel=VehicleModel" in editor)
        ck("original map test copy", "VehicleModel=workshopModel.DeepCopy()" in editor)
        ck("original map test restore", "VehicleModel=workshopModel;workshopModel=null" in editor)
        ck("original test suspension API", "Progress.State!=R1RunState.TestDrive" in susp)
    for name,ok in checks: print(("PASS" if ok else "FAIL")+" | "+name)
    print("STATIC_PASS=%d/%d | UNITY_NATIVE=NOT_RUN | CODEX_SHA=%s"%
          (sum(ok for _,ok in checks),len(checks),m["codex_sha"]))
    return int(not all(v for _,v in checks))
if __name__=="__main__":sys.exit(main())
