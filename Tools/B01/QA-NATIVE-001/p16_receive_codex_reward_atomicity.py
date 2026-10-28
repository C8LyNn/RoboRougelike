#!/usr/bin/env python3
"""Independent original Codex P16Reward native-NUnit receipt intake (not Unity run).

ONE distinct new original source change: R1 victory on invalid 3-choice catalog
must stop before wallet/scrap mutation. Reads actual Codex RED+GREEN NUnit XML.
"""
import json
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

REPO=Path(__file__).resolve().parents[3]
BEFORE="589b60a912096d1e9917cb0bbac73a53e7543b4f"
AFTER="561c223f7f01c1e609548bf1c8c66b5132ec5485"
FOLDER="Docs/P16Reward/Evidence/"
BLOBS={
"red.xml":"e91f940d893e1cdb99d74a739e8140d02042db44",
"red-exit.txt":"78c6baefdd2d13b3080b53d794e90471df3c4bf2",
"regression.xml":"657f5cc9ba7a01e5dcb596d3c756c2c642cdc4a8",
"regression-exit.txt":"18748286e5b8b4de5db905f87cdfed1a7d48fe60",
}
CODE={"before":"a675950028ce01a9ef450ad57dd5b524ab838b2f",
      "after":"3db48a98faba36e124f13aef543f6a89e7c49147"}
NATIVE="VictoryRewardAtomicityTests.InvalidRewardCatalogCannotAwardScrapOrRepeatVictory"
POSITIVE={
"R1RunModelTests.RepeatedWinCannotAdvancePastUnchosenReward",
"RewardRC2IntegrationTests.LiveVictoryUsesOneKeep3WindowAndOriginalAtomicSettlement",
"InvalidWeaponTransactionTests.HealthyValidTransactionsPreserveGuidAndChargeOnce"}
class Bad(Exception):pass
def check(ok,reason):
    if not ok:raise Bad(reason)
def git(*args):
    return subprocess.run(["git","-C",str(REPO),*args],check=True,
                          stdout=subprocess.PIPE,stderr=subprocess.PIPE).stdout
def verified(ref,path,expected):
    data=git("show",f"{ref}:{path}")
    h=subprocess.run(["git","-C",str(REPO),"hash-object","--stdin"],
                     input=data,check=True,stdout=subprocess.PIPE).stdout.decode().strip()
    check(h==expected,"ORIGINAL_BLOB_MISMATCH:"+path)
    return data
def parse(raw,total,failed,result):
    root=ET.fromstring(raw)
    check(root.tag=="test-run" and root.get("result")==result,"ROOT_RESULT_WRONG")
    expected={"total":total,"passed":total-failed,"failed":failed,
              "skipped":0,"inconclusive":0}
    check(all(int(root.get(k,"-1"))==v for k,v in expected.items()),
          "NUNIT_COUNTS_OR_SKIP_WRONG")
    cases=root.findall(".//test-case")
    check(len(cases)==total,"MISSING_NUNIT_CASE")
    names={}
    for c in cases:
        name=c.get("name");cls=c.get("classname");full=c.get("fullname")
        check(name and cls and full==cls+"."+name,"INVALID_FULLNAME")
        check(full not in names,"DUPLICATE_CASE")
        check(c.get("result") in ("Passed","Failed"),"SKIPPED_CASE")
        names[full]=c.get("result")
    check(sum(v=="Failed" for v in names.values())==failed,
          "NUNIT_FAILED_COUNT_MISMATCH")
    check(NATIVE in names,"ORIGINAL_TARGET_MISSING")
    return names
def tweak_case(raw,field,value):
    root=ET.fromstring(raw)
    case=next(c for c in root.findall(".//test-case") if c.get("fullname")==NATIVE)
    case.set(field,value)
    return ET.tostring(root,encoding="utf-8")
def main():
    git("fetch","--no-tags","origin",BEFORE)
    git("fetch","--no-tags","origin",AFTER)
    check(subprocess.run(["git","-C",str(REPO),"merge-base","--is-ancestor",BEFORE,AFTER]).returncode==0,
          "NOT_A_NEW_GAME_SOURCE_COMMIT")
    for stage in ("before","after"):
        ref=BEFORE if stage=="before" else AFTER
        verified(ref,"Assets/R1/R1RunController.cs",CODE[stage])
    check(CODE["before"]!=CODE["after"],"SAME_GAME_SOURCE_NOT_A_FIX")
    raw={n:verified(AFTER,FOLDER+n,s) for n,s in BLOBS.items()}
    check(raw["red-exit.txt"].strip()==b"2","RED_EXIT_MISSING")
    check(raw["regression-exit.txt"].strip()==b"0","GREEN_EXIT_MISSING")
    red=parse(raw["red.xml"],1,1,"Failed(Child)")
    after=parse(raw["regression.xml"],32,0,"Passed")
    check(red=={NATIVE:"Failed"} and after[NATIVE]=="Passed",
          "ORIGINAL_REWARD_RED_NOT_GREEN")
    check(POSITIVE.issubset(after),"MISSING_REWARD_POSITIVE_CONTROLS")
    mutation=[]
    for name,alter in [
       ("truncated_xml",lambda raw:raw[:90]),
       ("fake_root_pass",lambda raw:raw.replace(b'passed="32"',b'passed="31"',1)),
       ("missing_repair_case",lambda raw:raw.replace(b'total="32"',b'total="31"',1)),
       ("retargeted_case",lambda raw:tweak_case(raw,"fullname","Other.Test")),
       ("skipped_case",lambda raw:tweak_case(raw,"result","Skipped")),
    ]:
        try:parse(alter(raw["regression.xml"]),32,0,"Passed")
        except (Bad,ET.ParseError):mutation.append(name)
        else:raise Bad("MUTATION_ACCEPTED:"+name)
    print("B0160_P16_REWARD_JSON="+json.dumps({
      "input_original_game_before":BEFORE,"input_original_game_after":AFTER,
      "game_controller_blobs":CODE,"receipt_blobs":BLOBS,
      "original_unity_native_red":{"test":NATIVE,"result":"Failed","exit":2},
      "original_unity_native_green":{"test":NATIVE,"total":32,"passed":32,"exit":0},
      "verified_positive_control_names":sorted(POSITIVE),
      "synthetic_receipt_mutations_denied":mutation,
      "native_executed_by":"Codex Windows Unity (recorded original XML)",
      "independent_unity_execution":"NOT_RUN","package_status":"QA_RECEIPT_ACCEPTANCE_ONLY"
    },sort_keys=True))
if __name__=="__main__":main()
