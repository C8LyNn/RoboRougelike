#!/usr/bin/env python3
"""B01-40 P17 actual original-C# MAPGEN arena receiver; no Unity install or merge.

Run pinned B01-20 #326 in ONE temporary detached Git worktree. Re-validate all
18 C#-generated blueprints, then build a portable, immutable delivery ZIP
containing *data*, preview SVGs and exact MapGenPlanner call parameters.
Original MapBlueprint JSON is diagnostic, NOT MapDocument v2.
"""
import argparse
from collections import defaultdict
import hashlib
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import zipfile

OWNER_SHA="66c8988aa06a63036c6abede4c7181c0c7bfd904"
MAPGEN_SHA="5be31039411f89f17654b1c332f8692883fa03cc"
PACKAGE="B01-40-P17-MAPGEN-ARENA-RECEIVER-001"
ROOT="Research/B01-20/P16/MAPGEN-ARENA"
MAPGEN_ORIGINAL="Assets/B01Packages/MAPGEN001/Runtime"
ORIGINAL=[
    (ROOT+"/presets.json","5a0c8f0ce1f7226261352e9200cd2b5a64ce19c3"),
    (ROOT+"/ArenaProgram.cs","0e71b9c3be6c608d78924caa20b58c6c76d585fe"),
    (ROOT+"/MapgenArenaP16.csproj","138d1a8d4654f6d7b3907cb152ebab643385ebeb"),
    (ROOT+"/validate_blueprints.py","51e378637336efaff05a26995c7ba23136388af6"),
    (ROOT+"/render_arenas.py","73daece38c1bdfc9eb0143dff7af9ff45568b2c1"),
    (ROOT+"/README.md","ac25034b017fb9d401ef038f182832e274160e7b"),
    (ROOT+"/manifest.json","194c0faa40b89c1ed8411a95ad3bde4319957d77"),
    (MAPGEN_ORIGINAL+"/MapGenData.cs","b586a5bfc697bc37edbf98bb936459aeebe694c6"),
    (MAPGEN_ORIGINAL+"/MapGenPlanner.cs","89ff64e12f278f2c9393ad542161a9387e1f4117"),
    (MAPGEN_ORIGINAL+"/MapGenRouteValidator.cs","4e4bee443a60abb3b284e8c546203096095e12bd"),
    ("Tools/B01/MAPGEN001/Headless/UnitySurrogate.cs","68f30fc4c49db21d90b143cf9bc13d230ad06184"),
]
SEEDS=[7,31,83,109,202,407]
NAMES=("P16-ARENA-A-OPEN","P16-ARENA-B-COVER","P16-ARENA-C-DENSE")
HASH64=re.compile(r"^[a-f0-9]{64}$")

class Reject(ValueError):
    pass

def require(value, message):
    if not value:raise Reject(message)

def run(cmd, *, cwd=None, timeout=240):
    p=subprocess.run(cmd,cwd=cwd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
                     text=True,encoding="utf-8",errors="replace",timeout=timeout)
    if p.returncode:
        raise Reject("COMMAND_FAILED:"+repr(cmd)+" EXIT="+str(p.returncode)+" "+
                     (p.stdout+"\n"+p.stderr)[-900:])
    return p.stdout

def blob_sha(raw):
    return hashlib.sha1(b"blob "+str(len(raw)).encode()+b"\0"+raw).hexdigest()

def git(repo,*args):
    return run(["git","-C",str(repo),*args]).strip()

def verify_pinned_source(repo):
    manifest_oid=git(repo,"rev-parse",OWNER_SHA+":"+ROOT+"/manifest.json")
    require(manifest_oid=="194c0faa40b89c1ed8411a95ad3bde4319957d77",
            "B01_20_MANIFEST_CHANGED")
    manifest=json.loads(subprocess.check_output(
        ["git","-C",str(repo),"show",OWNER_SHA+":"+ROOT+"/manifest.json"]))
    require(manifest["original_mapgen_producer"]["immutable_sha"]==MAPGEN_SHA,
            "PRODUCER_SHA_CHANGED")
    for path,oid in ORIGINAL:
        actual=git(repo,"rev-parse",OWNER_SHA+":"+path)
        require(actual==oid,"PINNED_SOURCE_DRIFT:"+path)
        raw=subprocess.check_output(["git","-C",str(repo),"show",OWNER_SHA+":"+path])
        require(blob_sha(raw)==oid,"SOURCE_BLOB_BYTES_INVALID:"+path)
        # Check pristine producer Git tree as a SECOND original source, not
        # merely that B01-20's stacked branch happened to contain source files.
        if path.startswith(MAPGEN_ORIGINAL+"/") or path.endswith("/UnitySurrogate.cs"):
            require(git(repo,"rev-parse",MAPGEN_SHA+":"+path)==oid,
                    "ORIGINAL_MAPGEN_PRODUCER_DIFFERS:"+path)
    return manifest

def canonical_json(data):
    # JSON means the same on Windows/Unix; normalized canonical UTF-8 gives
    # byte-exact reproducible ZIPs without modifying the author's source.
    return (json.dumps(data,sort_keys=True,ensure_ascii=False,
                       separators=(",",":"),allow_nan=False)+"\n").encode("utf-8")

def parsed(path):
    return json.loads(path.read_text(encoding="utf-8"))

def check_generated(root):
    root=Path(root)
    presets=parsed(root/"source-presets.json")
    generated=root/"owner-output"
    result=parsed(generated/"results.json")
    require(presets["packageId"]=="MAPGEN-ARENA-P16" and
            presets["originalMapgenOwnerCommit"]==MAPGEN_SHA and
            presets["originalMapgenRecipe"]=="CombatArena" and
            presets["schemaVersion"]==1 and
            presets["profiles"]==["Light","Heavy"],
            "SOURCE_PRESETS_CONTRACT_INVALID")
    variants=presets["variants"]
    require(len(variants)==3 and [v["id"] for v in variants]==list(NAMES),
            "SOURCE_VARIANT_IDS_CHANGED")
    require(all(v["seeds"]==SEEDS for v in variants),"SOURCE_SEEDS_CHANGED")
    require(result["status"]=="SOURCE_ORIGINAL_CSHARP_PASS" and
            result["sourceSha"]==MAPGEN_SHA and
            result["totalVariants"]==3 and
            result["uniqueGeneratedMaps"]==18 and
            result["actualOriginalCsharpPlans"]==36 and
            result["originalPlannerNegativeRejections"]==6 and
            result["actualUnityNative"]=="NOT_RUN" and
            result["actualPhysxDrives"]==0 and
            len(result["generatedBlueprints"])==18,"ORIGINAL_CSHARP_RECEIPT_INVALID")
    expected={(v["id"],seed) for v in variants for seed in SEEDS}
    visited=set();hashes=set();covers=defaultdict(list)
    files={}
    calls=[]
    for row in result["generatedBlueprints"]:
        vid=row["variant"];seed=row["seed"];filename=row["file"]
        require((vid,seed) in expected and (vid,seed) not in visited,
                "DUPLICATE_OR_UNKNOWN_PRESET_SEED")
        require(filename==vid+"-seed-"+str(seed)+".json" and
                filename==Path(filename).name and not (generated/filename).is_symlink(),
                "UNSAFE_BLUEPRINT_PATH")
        visited.add((vid,seed))
        blueprint=parsed(generated/filename)
        preset=next(v for v in variants if v["id"]==vid)
        require(blueprint["variantId"]==vid and blueprint["seed"]==seed and
                blueprint["packageId"]==presets["packageId"] and
                blueprint["sourceOwnerSha"]==MAPGEN_SHA and
                blueprint["sourceRecipe"]=="CombatArena" and
                blueprint["config"]==preset["config"] and
                blueprint["geometryHash"]==row["geometryHash"] and
                blueprint["nativeUnityPhysics"]=="NOT_RUN",
                "BLUEPRINT_PROVENANCE_CHANGED")
        require(isinstance(row["geometryHash"],str) and HASH64.fullmatch(row["geometryHash"]) and
                row["geometryHash"] not in hashes,
                "DUPLICATE_OR_INVALID_GEOMETRY_HASH")
        hashes.add(row["geometryHash"])
        require(len(blueprint["enemySpawns"])==3 and
                {x["profile"] for x in blueprint["sourceValidation"]}=={"Light","Heavy"} and
                all(x["sourceValidatorPassed"] is True and
                    x["sourceRouteStatus"]=="BFS_SPINE_CLEARANCE_OK"
                    for x in blueprint["sourceValidation"]) and
                blueprint["guaranteedClearanceWidthM"]>=12 and
                blueprint["bounds"]["size"]=={"x":64,"y":8,"z":64},
                "ORIGINAL_ROUTE_AND_GEOMETRY_CONTRACT_CHANGED")
        elements=blueprint["elements"]
        require(any(x["kind"]=="Floor" for x in elements) and
                sum(x["kind"]=="Boundary" for x in elements)==4 and
                len(elements)==row["sourceElementCount"],
                "ORIGINAL_ELEMENT_COUNT_INVALID")
        covers[vid].append(sum(x["kind"]=="Cover" for x in elements))
        files["blueprints/"+filename]=canonical_json(blueprint)
        calls.append({"id":vid+"-seed-"+str(seed),"packageId":PACKAGE,
                      "recipe":"CombatArena","seed":seed,"config":preset["config"],
                      "sourceMapgenSha":MAPGEN_SHA,"sourceB0120Sha":OWNER_SHA,
                      "originalGeometryHash":row["geometryHash"],
                      "originalSourceRoutesValidated":["Light","Heavy"],
                      "runtimeUnityStatus":"NOT_RUN",
                      "diagnosticBlueprint":"blueprints/"+filename})
    require(visited==expected and len(hashes)==18,"INCOMPLETE_18_ARENA_SOURCE_OUTPUT")
    means=[sum(covers[n])/len(covers[n]) for n in NAMES]
    require(means[0]<means[1]<means[2],"NO_INCREASING_COVER_PROFILES")
    preview_dir=generated/"previews"
    require(len(list(preview_dir.glob("*.svg")))==18,"EXPECTED_18_SVG_PREVIEWS")
    for vid,seed in sorted(visited):
        stem=vid+"-seed-"+str(seed)
        raw=(preview_dir/(stem+".svg")).read_bytes()
        require(b"<svg" in raw and b"NOT NATIVE UNITY/PHYSX" in raw,
                "MISSING_SOURCE_PREVIEW_MARKER")
        files["previews/"+stem+".svg"]=raw.replace(b"\r\n",b"\n")
    gallery=(generated/"gallery.html").read_bytes()
    require(b"18 wygenerowanych aren" in gallery and
            b"NOT_RUN" in gallery and len(gallery)>1000,
            "ORIGINAL_HTML_GALLERY_UNAVAILABLE")
    files["gallery.html"]=gallery.replace(b"\r\n",b"\n")
    files["results.json"]=canonical_json(result)
    files["presets.json"]=canonical_json(presets)
    files["codex-generate-calls.json"]=canonical_json({
        "type":"MAPGEN_ORIGINAL_CSHARP_DIAGNOSTIC_RECIPE_CALLS",
        "notes":"Use MapGenPlanner/MapGenBuilder with exact CombatArena inputs only after Codex approval. MapBlueprint is NOT MapDocument v2.",
        "calls":sorted(calls,key=lambda x:x["id"]),
        "integratedIntoGame":False,"unityNative":"NOT_RUN"})
    report={"package_id":PACKAGE,"owner":"B01-40","producer_original_commit":OWNER_SHA,
            "mapgen_original_commit":MAPGEN_SHA,"type":"GENERATED_GAME_ARENA_DATA_HANDOFF_NOT_UNITY_IMPORT",
            "variant_count":3,"seed_count":6,"geometry_blueprints":18,
            "svg_previews":18,"original_csharp_plan_route_tests":36,
            "original_rejected_invalid_inputs":6,
            "cover_means_by_variant":{n:round(m,3) for n,m in zip(NAMES,means)},
            "contents_sha256":{name:hashlib.sha256(data).hexdigest()
                               for name,data in sorted(files.items())},
            "unity_native":"NOT_RUN","independent_G1":"NOT_RUN","G2":"HOLD",
            "codex_install":"Do not install diagnostic MapBlueprint as MapDocument v2; consume codex-generate-calls.json with canonical MAPGEN after authorized game intake.",
            "rollback":"No project writes: discard standalone ZIP and temporary receiver output; no main/develop merge."}
    files["receipt.json"]=canonical_json(report)
    require(len(files)==41,"UNEXPECTED_CONTENT_COUNT")
    return files,report

def zip_payload(files):
    buffer=io.BytesIO()
    with zipfile.ZipFile(buffer,"w",compression=zipfile.ZIP_STORED) as z:
        for name,content in sorted(files.items()):
            require(not name.startswith("/") and ".." not in Path(name).parts and
                    "\\" not in name,"UNSAFE_ZIP_MEMBER")
            info=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
            info.create_system=3
            info.compress_type=zipfile.ZIP_STORED
            info.external_attr=(0o100644 << 16)
            z.writestr(info,content)
    return buffer.getvalue()

def receive(repo,out):
    repo=Path(repo).resolve(strict=True)
    out=Path(out).resolve(strict=False)
    original_repo=Path(git(repo,"rev-parse","--show-toplevel")).resolve(strict=True)
    require(original_repo==repo,"REPO_MUST_BE_GIT_ROOT")
    require(out!=repo and repo not in out.parents and
            out!=repo.parent and out.parent.exists() and
            not out.exists() and not out.is_symlink(),
            "OUTPUT_MUST_BE_NEW_EXTERNAL_DIRECTORY")
    verify_pinned_source(repo)
    with tempfile.TemporaryDirectory(prefix="b0140_p17_original_worktree_") as scratch:
        tmp=Path(scratch)
        work=tmp/"original-owner"
        added=False
        try:
            run(["git","-C",str(repo),"worktree","add","--detach",str(work),OWNER_SHA])
            added=True
            # Original source files are never patched; all .NET bin/obj and
            # generated previews live in this disposable worktree/temp root.
            origin=work/ROOT
            target=tmp/"owner-output"
            target.mkdir()
            import shutil
            shutil.copyfile(origin/"presets.json",tmp/"source-presets.json")
            output=run(["dotnet","run","--project",str(origin/"MapgenArenaP16.csproj"),
                        "--configuration","Release","--",str(origin/"presets.json"),str(target)],
                       cwd=work,timeout=210)
            require("MAPGEN_P16_SOURCE_PASS" in output,"C_SHARP_SOURCE_GATE_DID_NOT_PASS")
            run([sys.executable,str(origin/"validate_blueprints.py"),str(target)],cwd=work)
            run([sys.executable,str(origin/"render_arenas.py"),str(target)],cwd=work)
            files,report=check_generated(tmp)
            zip_bytes=zip_payload(files)
            require(zip_bytes==zip_payload(files),"NON_DETERMINISTIC_ZIP_BYTES")
            # The external output path is never used for compilation, rendering
            # or partially-complete data. Only write after every gate passes.
            out.mkdir()
            archive=out/"B01-40-P17-MAPGEN-ARENAS-CODEX.zip"
            with archive.open("xb") as handle:handle.write(zip_bytes)
            with (out/"receiver-receipt.json").open("xb") as handle:
                data=dict(report)
                data["archive_sha256"]=hashlib.sha256(zip_bytes).hexdigest()
                handle.write(canonical_json(data))
            return data
        finally:
            if added:
                run(["git","-C",str(repo),"worktree","remove","--force",str(work)])
                run(["git","-C",str(repo),"worktree","prune"])

def main():
    a=argparse.ArgumentParser()
    a.add_argument("--repo",required=True,help="Current QA Git source root")
    a.add_argument("--out",required=True,help="A NEW EXTERNAL directory for source-only ZIP")
    args=a.parse_args()
    try:
        report=receive(args.repo,args.out)
        print(json.dumps(report,sort_keys=True,indent=2),flush=True)
        return 0
    except (Reject,OSError,ValueError,KeyError,subprocess.TimeoutExpired) as e:
        print("B01_40_P17_REJECT "+str(e)+" UNITY_NATIVE_NOT_RUN",file=sys.stderr,flush=True)
        return 2

if __name__=="__main__":sys.exit(main())
