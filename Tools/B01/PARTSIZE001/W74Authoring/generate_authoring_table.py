#!/usr/bin/env python3
"""Original-source PARTSIZE 11-kind x 4-yaw authoring envelopes, NOT Unity bounds.

Uses immutable R41 geometry and Codex R3 assembly + immutable PARTSIZE catalog.
These are analytical bounds of authored Unity primitive meshes at rest,
NOT physical Collider.bounds, PhysX, animated sweeps, or trusted sockets.
Do not use as a validation substitute for native G1.
"""
import argparse
import csv
import io
import math
from pathlib import Path
import re
import subprocess
import sys

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parents[2] / "W26Receipt"))
# Reuses the ACCEPTED seven-kind original-source primitive extraction, not a
# second approximate implementation or change to frozen QA #298.
from source_payload_measurements import source_parameters, classify, fitted_host_row, KINDS

PRODUCER_SHA="be2be0e4802bc3f450179901f9ae91c6d84bcb5b"
CODEX_SHA="231005c6d0351945a8ee6684f3856af118f0e94f"
SOURCE_FILES=(
 (PRODUCER_SHA,"Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs","6153d1758e800ec5c4210323610b061c06e8ea85"),
 (CODEX_SHA,"Assets/R4_1/R41PartGeometry.cs","76a14de4c347dd3e91b0a37487d804023eab5da1"),
 (CODEX_SHA,"Assets/R3/R3VehicleAssembly.cs","49fab51ac993128a9965df38ce32489f493b257a"),
)
KINDS_11=("Wheel","Gun","Suspension","Booster","JumpDrive","Blink",
          "Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack")
FIELDNAMES=("kind","yaw_deg","logical_xyz","authored_source_components",
 "analytical_static_aabb_min_xyz_m","analytical_static_aabb_max_xyz_m",
 "analytical_signed_cells_xyz","analytical_overshoot_xyz_m",
 "world_plus_z_axis_after_yaw","source_assumption",
 "physical_collider_bounds_native","renderer_bounds_native",
 "moving_sweep_native","trusted_socket_contact_native",
 "source_producer_sha","source_codex_sha")

def original(repo,sha,path,expected_blob):
    actual=subprocess.check_output(["git","-C",str(repo),"rev-parse",sha+":"+path],text=True).strip()
    if actual!=expected_blob:
        raise ValueError("SOURCE_GIT_BLOB_DRIFT:"+path+":"+actual)
    return subprocess.check_output(["git","-C",str(repo),"show",sha+":"+path]).decode("utf-8")

def unique_number(source,pattern):
    match=re.findall(pattern,source)
    if len(match)!=1:raise ValueError("ORIGINAL_SOURCE_NUMERIC_ANCHOR_MISMATCH:"+pattern)
    return tuple(float(s) for s in match[0]) if isinstance(match[0],tuple) else float(match[0])

def source_authored_four(geom,scale):
    # Four NOT INCLUDED by the previous seven-kind baseline; dimensions
    # come from actual original C# transform tokens, never visual guesses.
    if "if(solid){\n                    // A short CapsuleCollider" not in geom:
        raise ValueError("WHEEL_CONVEX_MESHCOLLIDER_SOURCE_ANCHOR_DRIFT")
    if "var tire=wheel.gameObject.AddComponent<MeshCollider>();" not in geom or "tire.convex=true;" not in geom:
        raise ValueError("WHEEL_MESH_COLLIDER_SOURCE_DRIFT")
    if 'Primitive(PrimitiveType.Cylinder,block,material,false,"Tłok aktywnego zawieszenia")' not in geom:
        raise ValueError("DECORATIVE_SUSPENSION_NOT_CONFIRMED")
    if "if(kind!=R3PartKind.Gun)return null;" not in geom or "bool heavy=weapon==R4WeaponKind.Heavy" not in geom:
        raise ValueError("BASIC_GUN_VARIANT_SOURCE_DRIFT")
    wpos=unique_number(geom,r"wheel\\.localPosition=new Vector3\\(0,(-?[\\d.]+)f,0\\)")
    wx,wy,wz=unique_number(geom,r"wheel\\.localScale=new Vector3\\(([\\d.]+)f,([\\d.]+)f,([\\d.]+)f\\)")
    tx,ty,tz=unique_number(geom,r"device\\.localScale=new Vector3\\(([\\d.]+)f,([\\d.]+)f,([\\d.]+)f\\);\\s*}\\s*if\\(kind==R3PartKind\\.OmniDrive\\)")
    tpos=unique_number(geom,r"if\\(kind==R3PartKind\\.CrawlerTrack\\)\\s*\\{device\\.localPosition=Vector3\\.down\\*([\\d.]+)f;")
    ppos=unique_number(geom,r"piston\\.localPosition=new Vector3\\(0,(-?[\\d.]+)f,0\\)")
    px,py,pz=unique_number(geom,r"piston\\.localScale=new Vector3\\(([\\d.]+)f,([\\d.]+)f,([\\d.]+)f\\)")
    gunpivot=unique_number(geom,r"pivot\\.localPosition=Vector3\\.up\\*([\\d.]+)f;")
    gunpos=unique_number(geom,r"barrel\\.localPosition=Vector3\\.forward\\*\\(heavy\\?[\\d.]+f:([\\d.]+)f\\);")
    gx,gy,gz=unique_number(geom,r"automatic\\?new Vector3\\([\\d.]+f,[\\d.]+f,[\\d.]+f\\):new Vector3\\(([\\d.]+)f,([\\d.]+)f,([\\d.]+)f\\);")
    # (local primitive center, local half extents), nominal Unity primitives.
    items={
      "Wheel":((0,wpos,0),(wy,wx/2,wz/2),"SOURCE_WHEEL_CYLINDER_MESH_CONVEX_NOT_PHYSX"),
      "CrawlerTrack":((0,-tpos,0),(tx/2,ty/2,tz/2),"SOURCE_CRAWLER_CUBE_NOT_PHYSX"),
      "Suspension":((0,ppos,0),(px/2,py,pz/2),"SOURCE_RENDER_DECORATIVE_PISTON_NONPHYSICAL"),
      "Gun":((0,gunpivot,gunpos),(gx/2,gy/2,gz/2),"SOURCE_BASIC_GUN_BARREL_ONLY_OTHER_VARIANTS_UNMEASURED"),
    }
    output={}
    for kind,(center,ext,note) in items.items():
        lo=tuple(round((center[i]-ext[i])*scale,6) for i in range(3))
        hi=tuple(round((center[i]+ext[i])*scale,6) for i in range(3))
        lo=tuple(min(-scale/2,lo[i]) for i in range(3))
        hi=tuple(max(scale/2,hi[i]) for i in range(3))
        output[kind]=(lo,hi,note)
    return output

def rotated_bounds(lo,hi,yaw):
    assert yaw in (0,90,180,270)
    pts=[]
    for x in (lo[0],hi[0]):
      for z in (lo[2],hi[2]):
        pts.append({0:(x,z),90:(z,-x),180:(-x,-z),270:(-z,x)}[yaw])
    return ([round(min(x for x,z in pts),6),round(lo[1],6),round(min(z for x,z in pts),6)],
            [round(max(x for x,z in pts),6),round(hi[1],6),round(max(z for x,z in pts),6)])

def fmt_xyz(seq):
    return "|".join(f"{v:.6f}" for v in seq)

def pose_cells(lo,hi):
    return "|".join(":".join(str(n) for n in range(math.floor((lo[i]+.275)/.55),
        math.floor((hi[i]+.275)/.55)+1)) for i in range(3))

def authoring_rows(catalog,geom,assembly):
    expected=['"'+k+'"' for k in KINDS_11]
    if any(k not in catalog for k in expected) or 'return PartSizeReferenceFixtures.Unmeasured(canonicalKind,' not in catalog:
        raise ValueError("PARTSIZE_11_UNMEASURED_OR_CANONICAL_SOURCE_CHANGED")
    required=("item.transform.localScale=Vector3.one*0.52f;",
      "item.transform.localRotation=R3MountingGrid.Rotation(state.RotationQuarterTurns);",
      "item.transform.localPosition=(Vector3)state.Cell*0.55f;",
      "item.AddComponent<R3BlockMountPoints>().Configure(state.Id,",
      "R3MountingGrid.WorldPortMask(state.RotationQuarterTurns,state.ContactMask));",
      "if(!wide)payload=R41PartGeometry.Payload(item.transform,state.Kind,state.Weapon,")
    if any(s not in assembly for s in required): raise ValueError("CODEX_HOST_OR_SOCKET_CONTRACT_DRIFT")
    if "public const float Grid=0.55f, BlockSize=0.52f;" not in geom:
        raise ValueError("SOURCE_GRID_SCALE_DRIFT")
    pars=source_parameters(geom)
    extra=source_authored_four(geom,.52)
    specs={}
    for kind in KINDS:
        row=fitted_host_row(classify(pars,kind),.52)
        specs[kind]=(row["assembled_root_plus_payload_aabb_min_m"],
                     row["assembled_root_plus_payload_aabb_max_m"],
                     "SOURCE_7KIND_PRIMITIVE_STATIC_YAW0_NO_SWEEP")
    specs.update(extra)
    if len(specs)!=11:raise ValueError("NOT_ELEVEN_AUTHORED_KINDS")
    rows=[]
    for kind in KINDS_11:
      lo,hi,note=specs[kind]
      for yaw,face in ((0,"+Z"),(90,"+X"),(180,"-Z"),(270,"-X")):
        a,b=rotated_bounds(lo,hi,yaw)
        over=[round(max(0.,-.275-a[i],b[i]-.275),6) for i in range(3)]
        rows.append(dict(kind=kind,yaw_deg=yaw,logical_xyz="1|1|1",
         authored_source_components=("ROOT_PLUS_RENDER_DECOR" if kind=="Suspension" else "ROOT_PLUS_STATIC_PAYLOAD"),
         analytical_static_aabb_min_xyz_m=fmt_xyz(a),
         analytical_static_aabb_max_xyz_m=fmt_xyz(b),
         analytical_signed_cells_xyz=pose_cells(a,b),
         analytical_overshoot_xyz_m=fmt_xyz(over),
         world_plus_z_axis_after_yaw=face,source_assumption=note,
         physical_collider_bounds_native="NOT_RUN",
         renderer_bounds_native="NOT_RUN",moving_sweep_native="NOT_RUN",
         trusted_socket_contact_native="UNVERIFIED",
         source_producer_sha=PRODUCER_SHA,source_codex_sha=CODEX_SHA))
    if len(rows)!=44 or {r["kind"] for r in rows}!=set(KINDS_11):
        raise ValueError("POSE_COVERAGE_NOT_44")
    return rows

def emit(rows):
    out=io.StringIO(newline="")
    writer=csv.DictWriter(out,fieldnames=FIELDNAMES,lineterminator="\n")
    writer.writeheader();writer.writerows(rows)
    return out.getvalue()

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--repo",default=".")
    ap.add_argument("--output",default="Docs/B01/PARTSIZE001/W74_11KIND_4YAW_AUTHORING.csv")
    ap.add_argument("--check",action="store_true")
    args=ap.parse_args()
    catalog,geom,assembly=(original(args.repo,sha,path,blob) for sha,path,blob in SOURCE_FILES)
    content=emit(authoring_rows(catalog,geom,assembly))
    dest=Path(args.output)
    if args.check:
        if not dest.exists() or dest.read_text(encoding="utf-8")!=content:
            raise SystemExit("SOURCE_DERIVED_44_ROW_TABLE_DRIFT")
    else:
        dest.parent.mkdir(parents=True,exist_ok=True)
        dest.write_text(content,encoding="utf-8")
    print("PARTSIZE_SOURCE_AUTHORING_44_ROWS_PASS; source Git blobs exact; "+
          "native Collider/Renderer/PhysX/socket NOT_RUN")

if __name__=="__main__":main()
