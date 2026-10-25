#!/usr/bin/env python3
"""B01-21 W11: deterministic two-pinned-revision C# execution, per-case diff and URP static inspection.
Uses real pinned C# MapGenPlanner/MapGenData/MapGenRouteValidator in two independent dotnet builds.
Only UnityEngine math is substituted by producer's explicitly synthetic surrogate. No Unity/PhysX.
"""
from __future__ import annotations
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
from xml.sax.saxutils import escape

R32="003bced54480d509ffc0d49d26daf87160a8d609"
R33="7334f3b1d8cd3a976098faa4e683f4de3d9a592f"
ROOT=Path(__file__).resolve().parents[3]  # repo root? checked/overridden in main below
SOURCE_PREFIX="Assets/B01Packages/MAPGEN001/Runtime/"
SOURCES=["MapGenData.cs","MapGenPlanner.cs","MapGenRouteValidator.cs"]
BLDR=SOURCE_PREFIX+"MapGenBuilder.cs"
NATIVE_TEST="Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPlayModeTests.cs"

def command(args, cwd=None, check=True):
    p=subprocess.run(args,cwd=cwd,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if check and p.returncode:
        raise RuntimeError(f"command failure {args}: exit {p.returncode}\n{p.stdout[-1000:]}\n{p.stderr[-2000:]}")
    return p

def git_bytes(rev,path):
    return subprocess.run(["git","show",f"{rev}:{path}"],cwd=ROOT,check=True,stdout=subprocess.PIPE).stdout

def make_tree(rev,path):
    path.mkdir(parents=True,exist_ok=True)
    for item in SOURCES:
        (path/item).write_bytes(git_bytes(rev,SOURCE_PREFIX+item))
    (path/"UnitySurrogate.cs").write_bytes(git_bytes(R32,"Tools/B01/MAPGEN001/Headless/UnitySurrogate.cs"))
    (path/"W11Cases.cs").write_bytes((ROOT/"Tools/B01/MAPGEN-DIAG-001/W11Cases.cs").read_bytes())
    project="""<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>enable</ImplicitUsings>
<Nullable>disable</Nullable></PropertyGroup>
<ItemGroup><Compile Include="W11Cases.cs"/><Compile Include="UnitySurrogate.cs"/>
<Compile Include="MapGenData.cs"/><Compile Include="MapGenPlanner.cs"/>
<Compile Include="MapGenRouteValidator.cs"/></ItemGroup></Project>"""
    (path/"W11.csproj").write_text(project,encoding="utf-8")

def static_urp():
    old=git_bytes(R32,BLDR).decode()
    new=git_bytes(R33,BLDR).decode()
    test=git_bytes(R33,NATIVE_TEST).decode()
    checks={
        "r32_no_explicit_urp_shader":'Shader.Find("Universal Render Pipeline/Lit")' not in old,
        "r33_explicit_urp_lit":'Shader.Find("Universal Render Pipeline/Lit")' in new,
        "r33_simple_lit_fallback":'Shader.Find("Universal Render Pipeline/Simple Lit")' in new,
        "r33_assigns_shared_material":'renderer.sharedMaterial = GetGeneratedBlockMaterial()' in new,
        "r33_uses_base_color_block":'block.SetColor("_BaseColor", c)' in new,
        "r33_owned_material_destroyed":'Destroy(_generatedBlockMaterial)' in new and 'DestroyImmediate(_generatedBlockMaterial)' in new,
        "r33_shader_absence_errors":'MAPGEN001_URP_SHADER_NOT_AVAILABLE' in new,
        "r33_native_shader_test_authored":'GeneratedBlocksUseUrpShaderInsteadOfErrorMaterial' in test,
    }
    return {
        "id":"URP_SOURCE_STRUCTURE","tier":"STATIC_SOURCE_ONLY_NOT_VISUAL",
        "checks":checks, "status":"STATIC_PASS_NEEDS_NATIVE" if all(checks.values()) else "CONFIRMED_STATIC_REGRESSION",
        "limitations":"Shader.Find in Editor need not imply shader included in Player; test checks shader name, not GPU pixels/screenshots. True URP 720p/1080p, player build, stripping, lifecycle require Unity."
    }

def main():
    global ROOT
    ROOT=Path.cwd()
    evidence=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else ROOT/"w11-mapgen-diff-evidence"
    evidence.mkdir(parents=True,exist_ok=True)
    snapshots={}
    blobs={}
    for name in SOURCES:
        a=git_bytes(R32,SOURCE_PREFIX+name)
        b=git_bytes(R33,SOURCE_PREFIX+name)
        blobs[name]={"r32_sha256":hashlib.sha256(a).hexdigest(),
                     "r33_sha256":hashlib.sha256(b).hexdigest(),"equal":a==b}
    for item in ["MapGenBuilder.cs"]:
        a=git_bytes(R32,SOURCE_PREFIX+item)
        b=git_bytes(R33,SOURCE_PREFIX+item)
        blobs[item]={"r32_sha256":hashlib.sha256(a).hexdigest(),
                     "r33_sha256":hashlib.sha256(b).hexdigest(),"equal":a==b}
    with tempfile.TemporaryDirectory(prefix="b01-mapgen-w11-") as work:
        work=Path(work)
        for label,sha in [("R3.2",R32),("R3.3",R33)]:
            dest=work/label
            make_tree(sha,dest)
            out=evidence/(label.replace(".","")+"_source_cases.json")
            p=command(["dotnet","run","--project",str(dest/"W11.csproj"),
                       "--configuration","Release","--",label,str(out)],cwd=ROOT)
            (evidence/(label.replace(".","")+"_dotnet_run.log")).write_text(p.stdout+"\nSTDERR\n"+p.stderr,encoding="utf-8")
            snapshots[label]=json.loads(out.read_text(encoding="utf-8"))
    lhs={x["Id"]:x for x in snapshots["R3.2"]["cases"]}
    rhs={x["Id"]:x for x in snapshots["R3.3"]["cases"]}
    all_ids=sorted(set(lhs)|set(rhs))
    results=[]
    for id in all_ids:
        a,b=lhs.get(id),rhs.get(id)
        d={"id":id,"tier":"EXECUTED_CSHARP_BOTH_REVISIONS_UNITY_MATH_SURROGATE",
           "r32":a,"r33":b,"classification":"CONFIRMED",
           "result":"MATCH","issue":""}
        if a is None or b is None:
            d["result"]="FAIL_MISSING_CASE"
        elif a["GeometryHash"]!=b["GeometryHash"] or a["Result"]!=b["Result"] or a["RouteCode"]!=b["RouteCode"]:
            d["result"]="FAIL_SOURCE_REGRESSION";d["issue"]="geometry or acceptance changed"
        elif (a["MinimumAxialFloorOverlapM"]!=b["MinimumAxialFloorOverlapM"]
              or a["MinimumRampBoundaryLateralGapM"]!=b["MinimumRampBoundaryLateralGapM"]
              or a["MaximumRampFloorIntrusionM"]!=b["MaximumRampFloorIntrusionM"]):
            d["result"]="FAIL_MEASURED_GEOMETRY_CHANGE"
        elif id.startswith("NARROW10_MEASURED/"):
            d["classification"]="CONFIRMED_PROFILE_CONSTRAINT"
            d["issue"]="Actual 2.980m Light exceeds 10m lane reservation; expected fail-closed. Does NOT mean all Light gameplay courses fail."
            if a["Result"]!="CONFIG_REJECTED" or "IMPOSSIBLE_COURSE_PROFILE" not in a["RouteCode"]:
                d["result"]="FAIL_EXPECTED_NARROW_REJECTION_MISSING"
            else: d["result"]="MATCH_EXPECTED_REJECTION"
        elif id.startswith("NARROW10_NOMINAL/"):
            if a["Result"]!="SOURCE_LAYOUT_PASS":
                d["result"]="FAIL_NOMINAL_LIGHT10"
        elif id.startswith(("BASE/","HEAVY_","PROFILE_CONTROL/")) and a["Result"]!="SOURCE_LAYOUT_PASS":
            d["result"]="FAIL_VALID_STANDARD_PROFILE"
        if a and a["RampCount"] and a["MinimumRampWidthMinusVehicleWidthM"] is not None and a["MinimumRampWidthMinusVehicleWidthM"]<0:
            d["optional_ramp_status"]="RAMP_NARROWER_THAN_HULL_NEEDS_NATIVE"
            d["optional_ramp_width_margin_m"]=a["MinimumRampWidthMinusVehicleWidthM"]
        if a and a["MaximumRampFloorIntrusionM"] is not None and a["MaximumRampFloorIntrusionM"]>0:
            d["ramp_floor_status"]="AABB_INTRUSION_EXISTS_NEEDS_PHYSX"
        if a and a["MinimumAxialFloorOverlapM"] is not None and a["MinimumAxialFloorOverlapM"]>0:
            d["floor_seam_status"]="POSITIVE_FLOOR_OVERLAP_NOT_A_GAP_NEEDS_PHYSX"
        results.append(d)
        (evidence/("CASE_"+re.sub(r"[^a-zA-Z0-9]","_",id)+".json")).write_text(
            json.dumps(d,indent=2,allow_nan=False),encoding="utf-8")
    urp=static_urp()
    failures=[r["id"] for r in results if r["result"].startswith("FAIL")]
    for path in SOURCES:
        if not blobs[path]["equal"]: failures.append("UNMODIFIED_RUNTIME_EXPECTED:"+path)
    if urp["status"]!="STATIC_PASS_NEEDS_NATIVE":failures.append("URP_STATIC")
    # explicit output includes independent overall result and verifiable reference SHAs
    receipt={"name":"B01-21 W11 MAPGEN R3.2-R3.3 differential",
             "source_r32_sha":R32,"source_r33_sha":R33,
             "source_sha256":blobs,"test_tier":"EXECUTED SOURCE C# + UNITYENGINE SURROGATE NOT UNITY",
             "cases_per_revision":len(lhs),"paired_cases":len(results),
             "same_geometry_hash_count":sum(1 for x in results if x["r32"] and x["r33"] and x["r32"]["GeometryHash"]==x["r33"]["GeometryHash"] and bool(x["r32"]["GeometryHash"])),
             "expected_narrow_rejections":sum(1 for x in results if x["result"]=="MATCH_EXPECTED_REJECTION"),
             "failures":failures,"urp_static":urp,"cases":results,
             "native_unity":"NOT_RUN","physx_r33":"NOT_RUN"}
    (evidence/"results.json").write_text(json.dumps(receipt,indent=2,allow_nan=False),encoding="utf-8")
    table=["| Case | R3.2 | R3.3 | Hash match | Classification | Note |","|---|---|---|---|---|---|"]
    for row in results:
        a,b=row["r32"],row["r33"]
        table.append("| "+row["id"]+" | "+(a["Result"] if a else "MISSING")+" | "+(b["Result"] if b else "MISSING")+
             " | "+str(a is not None and b is not None and a["GeometryHash"]==b["GeometryHash"])+
             " | "+row["result"]+" | "+row.get("issue","").replace("|","/")+" |")
    (evidence/"cases.md").write_text("\n".join(table)+"\n",encoding="utf-8")
    for row in results:
        print("W11_CASE",row["id"],row["result"],
              (row["r32"] or {}).get("GeometryHash",""),
              (row["r33"] or {}).get("GeometryHash",""))
    print("W11_FINAL paired=",len(results)," geometry_hash_matches=",receipt["same_geometry_hash_count"],
          " expected_narrow_rejections=",receipt["expected_narrow_rejections"]," failures=",len(failures))
    print("URP_STATIC=",urp["status"]," NATIVE_UNITY=NOT_RUN PHYSX_R33=NOT_RUN")
    return 2 if failures else 0

if __name__=="__main__":
    try:sys.exit(main())
    except Exception as e:
        import traceback
        traceback.print_exc()
        sys.exit(3)
