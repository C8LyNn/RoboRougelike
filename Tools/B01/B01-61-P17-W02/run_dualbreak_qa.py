#!/usr/bin/env python3
"""B01-61 WAVE02: independent B01-23 receiver for *two* simultaneous part losses.

1. Pins the exact original committed B01-23 PARTSIZE C# and three game blueprints.
2. Compiles original frozen geometry, original rig validator and own NEW C# driver.
3. Executes 38 historical source-case comparisons + 227 NEW 2-hit scenarios.
4. Crosschecks all 227 against an independent Python logical graph oracle.
5. Tamper tests fake physical status, owner identity and connected gun count.
The Python/C# stubs are NOT native Unity, PhysX, typed sockets or combat.
"""
import argparse
import copy
import hashlib
import json
import pathlib
import shutil
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parent
RIG_SHA="0978497561c8b6400490235d3e5a401ac32bea2d"
WEAK_SHA="098ccdbe8f129253fa4ada52318436b7cc2b4dc4"
PARTSIZE_SHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
INPUTS = [
    ("PartSizeContract.cs",RIG_SHA,"Assets/PARTSIZE001/Runtime/PartSizeContract.cs","07455919a5544875fee5a78ecbbce36dc6cb49c2"),
    ("PartSizeBaselineCatalog.cs",RIG_SHA,"Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs","6153d1758e800ec5c4210323610b061c06e8ea85"),
    ("RigValidator.cs",RIG_SHA,"Tools/B01/PARTSIZE001/P17Rigs/RigValidator.cs","988fc31d8633e5429fc4ba377856187e5ee7601c"),
    ("P17_Flanker_Wheels.json",RIG_SHA,"Assets/PARTSIZE001/Blueprints/P17/P17_Flanker_Wheels.json","c4778df1db431ed1b51e8fb1e607ae1ede3e1bef"),
    ("P17_Bulwark_Tracks.json",RIG_SHA,"Assets/PARTSIZE001/Blueprints/P17/P17_Bulwark_Tracks.json","2fe5b441d081ad3590ccbeabf1cbb1f266ec662a"),
    ("P17_Skirmisher_Hover.json",RIG_SHA,"Assets/PARTSIZE001/Blueprints/P17/P17_Skirmisher_Hover.json","4d7b4ab0adfb71fa2c20479eaa7c266687d28fa1"),
    ("P17_SingleBreakOutcomes.json",WEAK_SHA,"Assets/PARTSIZE001/Weakpoints/P17/P17_SingleBreakOutcomes.json","eb4ab2e2cd6d4c34b6a014b00b6c86efc472cfda"),
]
CS_PROJ="""<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup>
  <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors><Deterministic>true</Deterministic>
  <NoWarn>$(NoWarn);CS8765;CS8629;CS8603</NoWarn>
</PropertyGroup>
</Project>
"""
FACES=((1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1))
def git(repo,*args):
    p=subprocess.run(["git","-C",str(repo),*args],capture_output=True,check=False)
    if p.returncode:raise RuntimeError("GIT_COMMAND_FAIL "+repr(args)+" "+p.stderr.decode(errors="replace")[:230])
    return p.stdout

def assert_unchanged_git_bytes(repo,target,name,ref,filepath,sha):
    actual=git(repo,"rev-parse",ref+":"+filepath).decode().strip()
    if len(sha)!=40 or actual!=sha:
        raise ValueError("ORIGINAL_B01_23_GIT_BLOB_DRIFT "+filepath+" "+actual+" != "+sha)
    data=git(repo,"show",ref+":"+filepath)
    if b"\x00" in data:raise ValueError("NON_UTF8_ORIGINAL_SOURCE")
    (target/name).write_bytes(data)
    return data

def coords(part):
    # Frozen PARTSIZE original source pins define all P17 stock part kinds as
    # one logical cell. Refuse new wide part kinds or non-zero yaw until reviewed.
    rot=part["rotationQuarterTurns"]
    if rot["x"] or rot["y"] or rot["z"]:
        raise ValueError("NEW_ROTATED_SOURCE_PART_NEEDS_ORACLE_REVISION")
    p=part["cell"]
    return (p["x"],p["y"],p["z"])

def oracle(rig,removed):
    parts=rig["parts"]
    owner={part["localId"]:part for part in parts}
    removed=set(removed)
    if len(removed)!=2 or not removed.issubset(owner):
        raise ValueError("PAIR_NOT_DISTINCT_PART_OWNERS")
    cell_owner={}
    for part in parts:
        if part["localId"] not in removed:
            c=coords(part)
            if c in cell_owner:raise ValueError("TWO_PARTS_IN_ONE_SOURCE_CELL")
            cell_owner[c]=part["localId"]
    core=next(p["localId"] for p in parts if p["kind"]=="Core")
    reached=set()
    todo=[]
    if core not in removed:
        reached.add(core);todo=[core]
    while todo:
        part_id=todo.pop()
        px,py,pz=coords(owner[part_id])
        for dx,dy,dz in FACES:
            neighbor=cell_owner.get((px+dx,py+dy,pz+dz))
            if neighbor and neighbor not in reached:
                reached.add(neighbor);todo.append(neighbor)
    unmounted=sorted(set(owner)-removed-reached)
    selected=rig["selectedDrive"]
    connected=[owner[id] for id in reached]
    if selected=="Hover":
        drive=any(p["kind"]=="HoverEngine" for p in connected)
    else:
        kind="Wheel" if selected=="Wheels" else "CrawlerTrack"
        drive=(any(p["kind"]==kind and p["cell"]["x"]<0 for p in connected)
               and any(p["kind"]==kind and p["cell"]["x"]>0 for p in connected))
    return dict(
        blueprintId=rig["blueprintId"],
        removedPartIds=sorted(removed),
        coreLost=core in removed,
        detachedPartIds=unmounted,
        connectedPartIds=sorted(reached),
        selectedDriveOperable=drive,
        connectedGunCount=sum(p["kind"]=="Gun" for p in connected),
        nativePhysicalStatus="NOT_RUN"
    )

def inspect(rigs,raw):
    if len(raw)!=227:raise AssertionError("PAIR_COUNT_MISMATCH")
    expected=[]
    for rig in rigs:
        ps=rig["parts"]
        for i in range(len(ps)):
            for j in range(i+1,len(ps)):
                expected.append(oracle(rig,(ps[i]["localId"],ps[j]["localId"])))
    if len(expected)!=227:raise AssertionError("INPUT_RIG_OWNER_COUNT_DRIFT")
    for i,(want,actual) in enumerate(zip(expected,raw)):
        if want!=actual:raise AssertionError(
            "INDEPENDENT_ORACLE_MISMATCH_AT_"+str(i)+" "+json.dumps({"expected":want,"actual":actual},ensure_ascii=False)[:550])
    if len({(x["blueprintId"],tuple(x["removedPartIds"])) for x in raw})!=227:
        raise AssertionError("DUPLICATE_PAIR_IDS")
    return expected

def main():
    p=argparse.ArgumentParser()
    p.add_argument("--repo",default=".")
    p.add_argument("--output",default="dualbreak_output.json")
    opt=p.parse_args()
    repo=pathlib.Path(opt.repo).resolve()
    output=pathlib.Path(opt.output).resolve()
    for sha in (RIG_SHA,WEAK_SHA,PARTSIZE_SHA):
        if git(repo,"rev-parse",sha+"^{commit}").decode().strip()!=sha:
            raise ValueError("MISSING_IMMUTABLE_ORIGINAL_SOURCE_COMMIT "+sha)
    manifest=json.loads((ROOT/"manifest.json").read_text(encoding="utf8"))
    if manifest["source_blob"]!=git(repo,"hash-object",str(ROOT/"dualbreak"/"Program.cs")).decode().strip():
        raise ValueError("MUTATED_QA_CSHARP_SOURCE_SHA")
    with tempfile.TemporaryDirectory(prefix="b0161_w02_doublebreak_") as tmp:
        work=pathlib.Path(tmp)
        original=work/"original"
        original.mkdir()
        owner_bytes={}
        for name,sha,path,blob in INPUTS:
            owner_bytes[name]=assert_unchanged_git_bytes(repo,original,name,sha,path,blob)
        # Unlike the producer's single removal tests, this is a separate source pin.
        catalog=owner_bytes["PartSizeBaselineCatalog.cs"].decode("utf8")
        contract=owner_bytes["PartSizeContract.cs"].decode("utf8")
        if not all(k in catalog for k in ('"Armor"','"Core"','new Cell3(1,1,1)')):
            raise ValueError("SOURCE_LOGICAL_PART_DIMENSIONS_CHANGED")
        if "new Cell3(1,1,1)" not in contract:
            raise ValueError("SOURCE_CUBE_CELL_DIMENSIONS_CHANGED")
        rigs=[json.loads(owner_bytes[n]) for n in ("P17_Flanker_Wheels.json",
              "P17_Bulwark_Tracks.json","P17_Skirmisher_Hover.json")]
        if [len(r["parts"]) for r in rigs]!=[14,14,10]:
            raise ValueError("ORIGINAL_RIG_PART_COUNT_CHANGED")
        if any(any(p["kind"] not in ("Core","Armor","Wheel","CrawlerTrack","HoverEngine","Gun")
                   for p in rig["parts"]) for rig in rigs):
            raise ValueError("ORIGINAL_SOURCE_PART_KINDS_CHANGED")
        if any(p["rotationQuarterTurns"]!={"x":0,"y":0,"z":0}
                   for rig in rigs for p in rig["parts"]):
            raise ValueError("ROTATED_PART_ORACLE_NOT_VERIFIED")
        shutil.copyfile(ROOT/"dualbreak"/"Program.cs",work/"Program.cs")
        (work/"DoubleBreak.csproj").write_text(CS_PROJ,encoding="utf8")
        process=subprocess.run(
            ["dotnet","run","--project",str(work/"DoubleBreak.csproj"),"--configuration","Release",
             "--no-launch-profile"],cwd=work,capture_output=True,text=True)
        print("DOTNET_ORIGINAL_SOURCE_EXECUTED_EXIT="+str(process.returncode))
        print(process.stdout[-5000:])
        if process.stderr:print("DOTNET_DIAGNOSTICS="+process.stderr[-1800:])
        if process.returncode:raise RuntimeError("ORIGINAL_PARTSIZE_CSHARP_PAIR_QA_FAILED")
        raw=json.loads((work/"dualbreak_output.json").read_text(encoding="utf8"))
        expected=inspect(rigs,raw)
        print("INDEPENDENT_PYTHON_GRAPH_ORACLE=227/227_PASS | SOURCE_ORIGINAL_SINGLE_BREAK_COMPARE=38/38_PASS")
        mutants=(
            ("wrong_connected_guns",lambda a:a.__setitem__("connectedGunCount",-5)),
            ("fabricated_native_validation",lambda a:a.__setitem__("nativePhysicalStatus","PASS")),
            ("wrong_owner_pair",lambda a:a["removedPartIds"].__setitem__(0,"FRAUDULENT-PART")),
            ("falsified_detach_set",lambda a:a.__setitem__("detachedPartIds",["FRAUDULENT-PART"]))
        )
        for label,change in mutants:
            broken=copy.deepcopy(raw)
            change(broken[0])
            try:inspect(rigs,broken)
            except AssertionError:print("IN_MEMORY_MUTATION_REJECTED="+label)
            else:raise RuntimeError("MUTATION_FAIL_OPEN:"+label)
        output.parent.mkdir(parents=True,exist_ok=True)
        encoded=json.dumps({"package":"B01-61-P17-W02-DUALBREAK","source":RIG_SHA,
          "weakpointSource":WEAK_SHA,"nativeUnity":"NOT_RUN",
          "sourceLogicalPairCases":raw},ensure_ascii=False,indent=2)+"\n"
        output.write_text(encoded,encoding="utf8")
        print("B0161_W02_PAIR_DATA_FILE="+str(output))
        print("B0161_W02_PAIR_DATA_SHA256="+hashlib.sha256(encoded.encode("utf8")).hexdigest())
        print("RECEIVER_STATUS=ORIGINAL_CSHARP_PASS | LOGICAL_TWO_OWNER_LOSS=227/227 | INDEPENDENT_PYTHON=227/227 | IN_MEMORY_MUTATIONS=4/4 | UNITY_NATIVE=NOT_RUN | GAME_CHANGED=NO")
    return 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as exc:
        print("B0161_W02_QA_FAIL="+repr(exc),file=sys.stderr)
        sys.exit(2)
