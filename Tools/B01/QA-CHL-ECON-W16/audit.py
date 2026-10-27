#!/usr/bin/env python3
"""B01-22 W16 independent source QA gate. Not a CHL reward adapter or Unity test."""
import argparse
import hashlib
import json
import pathlib
import re

R1="Assets/R1/R1RunController.cs"
R3="Assets/R3/R3VehicleModel.cs"
CHL="Assets/RoboRougelikeChallenges/CHL003"
class QAError(ValueError):
    pass

def require(ok,reason):
    if not ok:
        raise QAError(reason)

def uncomment(code):
    return re.sub(r"/\*[\s\S]*?\*/|//[^\r\n]*","",code)

def method(code,decl):
    idx=code.find(decl)
    require(idx>=0 and code.find(decl,idx+len(decl))<0,"MISSING_OR_DUPLICATE_HOST_METHOD "+decl)
    opening=code.find("{",idx+len(decl))
    require(opening>=0,"METHOD_OPEN_BRACE_MISSING")
    depth=0
    for end in range(opening,len(code)):
        if code[end]=="{":depth+=1
        if code[end]=="}":
            depth-=1
            if depth==0:return code[opening:end+1]
    raise QAError("METHOD_UNBALANCED")

def inspect_host(run,model):
    run,model=uncomment(run),uncomment(model)
    update=method(run,"private void Update()")
    confirm=method(run,"public bool ConfirmRewards(")
    repair=method(model,"public int RepairCost(R3PartState part)")
    gate="Progress.State != R1RunState.Combat || Paused) return;"
    require(gate in update,"HOST_COMBAT_GATE_REMOVED")
    require(update.count("VehicleModel.Award(LastScrapReward);")==1,
            "HOST_REWARD_MISSING_OR_DUPLICATED")
    require(update.count("Progress.WinEncounter(")==1,
            "HOST_WIN_TRANSITION_MISSING_OR_DUPLICATED")
    require("Progress.OpenSalvage();" in update,"HOST_SALVAGE_STAGE_MISSING")
    require("CheckpointRace?.Won==true" in update and
      "RoadChallenge?.Status==RoboRougelike.MapSystemR4.MapR4Status.Completed" in update,
      "EXISTING_HOST_COURSE_WIN_PREDICATES_CHANGED")
    first=update.find(gate)
    loss=update.find("if (defeat != null)")
    victory=update.find("else if (Encounter.Cleared")
    payout=update.find("VehicleModel.Award(LastScrapReward);")
    require(0<=first<loss<victory<payout,"HOST_LOSS_FIRST_ORDER_DRIFT")
    require("Progress?.State!=R1RunState.Salvage" in confirm and
       "CommitRecovery(ids,bonuses)" in confirm,"HOST_SALVAGE_CONFIRM_UNGUARDED")
    require("part.Health <= 0 ? Price(part.Kind,part.Weapon)" in repair,
       "PINNED_0HP_REPAIR_PRICE_SOURCE_CHANGED")
    return {"host_win_award_calls":1,
      "host_eligibility":"Combat state; not paused/transitioning; loss first",
      "existing_host_winners":["Encounter.Cleared","CheckpointRace.Won","RoadChallenge.Completed"],
      "host_victory_event_id_journal_in_method":False,
      "baseline_0hp_equals_buy_price":True,
      "note":"Original frozen C# static observation; not CHL integration or runtime PASS"}

CALLS=[
 ("DIRECT_REWARD_WRITE",r"\b(?:Award|ReceiveSalvage|ReceiveBonuses|Dismantle)\s*\("),
 ("RUN_REWARD_MUTATION",r"\b(?:ConfirmSalvage|ConfirmRewards|WinEncounter|OpenSalvage)\s*\("),
 ("DIRECT_CURRENCY_WRITE",r"\b(?:Scrap|EarnedScrap|LastScrapReward)\s*(?:\+\+|--|[+\-]?=)")
]
CORE_IMPORT=re.compile(r"\busing\s+RoboRougelike\.(?:R1|R3|R4)\s*;")
def inspect_chl_files(sources):
    checked=0
    for filename,source in sources:
        path=filename.replace("\\","/")
        if not path.endswith(".cs"):continue
        checked+=1
        code=uncomment(source)
        for reason,pattern in CALLS:
            require(not re.search(pattern,code),"CHL_ECON_LEAK "+reason+" "+path)
        is_core=("/Runtime/" in path or "/Core/" in path)
        is_noncore=any(s in path for s in ("/Adapters/","/Integration/","/Editor/","/Tests/"))
        if is_core and not is_noncore:
            require(not CORE_IMPORT.search(code),"CHL_HOST_IMPORT_IN_CORE "+path)
    return checked

def inspect_chl_directory(root):
    if not root.is_dir():
        return {"state":"SOURCE_NOT_PRESENT","count":0,"native":"NOT_RUN"}
    files=[(p.as_posix(),p.read_text(encoding="utf8")) for p in sorted(root.rglob("*.cs"))]
    count=inspect_chl_files(files)
    if count==0:return {"state":"SOURCE_NOT_PRESENT","count":0,"native":"NOT_RUN"}
    return {"state":"STATIC_BOUNDARY_CHECK_PASS","count":count,"native":"NOT_RUN",
       "limits":"No proof of indirect callback, real reward or Unity runtime behavior"}

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--host",required=True)
    ap.add_argument("--chl",default=CHL)
    ap.add_argument("--out",default="artifacts/chl-econ/receipt.json")
    args=ap.parse_args()
    base=pathlib.Path(args.host)
    receipt=json.loads((base/"receipt.json").read_text(encoding="utf8"))
    for entry in receipt["actual_pinned_host_files"]:
        raw=(base/entry["path"]).read_bytes()
        require(hashlib.sha256(raw).hexdigest()==entry["sha256"],
          "FROZEN_HOST_SOURCE_BYTES_CHANGED")
    h=inspect_host((base/R1).read_text(encoding="utf8"),(base/R3).read_text(encoding="utf8"))
    c=inspect_chl_directory(pathlib.Path(args.chl))
    result={"schema":"b01-22-w16-chl-economy-boundary-v1",
      "real_code_source_commit":receipt["commit"],
      "real_code_source_blobs":receipt["actual_pinned_host_files"],
      "host_source_audit":"STATIC_PASS",
      "host":h,"chl":c,
      "native_unity":"NOT_RUN","runtime_chl_reward_gates":"NOT_RUN","g2":"HOLD"}
    out=pathlib.Path(args.out)
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(result,indent=2,sort_keys=True)+"\n",encoding="utf8")
    print("PINNED_R1_R3_HOST_REWARD_INGRESS_STATIC_PASS")
    print("CHL_SOURCE_STATE",c["state"])
    print("CHL_NATIVE_REWARD_AND_PHYSICS_NOT_RUN")

if __name__=="__main__":
    main()
