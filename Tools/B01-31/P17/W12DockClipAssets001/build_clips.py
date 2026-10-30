#!/usr/bin/env python3
"""Four ORIGINAL Unity SpriteRenderer AnimationClip .anim source assets.
Actual deterministic Unity-YAML files + stable .meta in add-on 9-file ZIP.
Consumes only frozen B01-31 W11 original sprites/receipt; NO gameplay.
Unity native importer/PlayMode is NOT_RUN, never fabricate Unity G1.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import sys
import zipfile

ART_ROOT = "Assets/B0131DockDeniedVfx"
ADDON_FOLDER = ART_ROOT + "/Animations"
NAMES = ("collision-overlap","distance-limit","unguarded-route","query-saturated")
SOURCE_SHA="13632b19b8fee3c263aea62bc91535ba45b12168"
SOURCE_ZIP_SHA="7fc696ef80730192d41fffdb8736f1b6b81ce9611126c4ada7c40a4fc861aaef"
PKG="B01-31-DOCK-DENIED-UNITY-CLIPS-001"
VERSION="P17-W12-R1"
H=lambda raw:hashlib.sha256(raw).hexdigest()

def check(ok,why):
    if not ok:
        raise ValueError("B0131_W12_CLIP_FAIL_CLOSED "+why)

def stable_guid(asset_path):
    # Own metadata GUIDs; independent from immutable W11 sprite file identifiers.
    return H((PKG+":"+VERSION+":"+asset_path).encode("utf8"))[:32]

def folder_meta(folder):
    return (f"fileFormatVersion: 2\nguid: {stable_guid(folder)}\nfolderAsset: yes\n"
            "DefaultImporter:\n  externalObjects: {}\n  userData:\n"
            "  assetBundleName:\n  assetBundleVariant:\n").encode()

def clip_meta(path):
    return (f"fileFormatVersion: 2\nguid: {stable_guid(path)}\n"
            "NativeFormatImporter:\n  externalObjects: {}\n"
            "  mainObjectFileID: 7400000\n  userData:\n"
            "  assetBundleName:\n  assetBundleVariant:\n").encode()

def load_w11(root:Path):
    original=root/"B0131-DockDeniedVfx-UnitySpriteFrames.zip"
    check(original.is_file(),"missing real original W11 Sprite physical ZIP")
    original_bytes=original.read_bytes()
    check(H(original_bytes)==SOURCE_ZIP_SHA,
          "immutable original W11 Unity Sprite ZIP source SHA mismatch")
    rc=json.loads((root/"receipt.json").read_bytes())
    check(rc["package"]=="B01-31-DOCK-DENIED-ANIM-001"
          and rc["original_source_sha"]=="7944af98524b7eb5ceadb88eb056de1d486ac461"
          and rc["physical_unity_sprite_zip_sha256"]==SOURCE_ZIP_SHA
          and rc["zip_entry_count"]==70 and rc["stable_unique_meta_guids"]==38,
          "W11 original run immutable package/source/receipt mismatch")
    frames={(f["icon"],f["frame"]):f for f in rc["real_rgba_frame_pngs"]}
    check(len(frames)==32 and set(frames)=={(n,i) for n in NAMES for i in range(8)},
          "all 32 distinct original source frames not declared")
    guids={}
    original_pngs={}
    with zipfile.ZipFile(io.BytesIO(original_bytes)) as z:
        check(z.testzip() is None and len(z.namelist())==70,
              "real original sprite ZIP CRC or members")
        for name in NAMES:
            for i in range(8):
                p=f"{ART_ROOT}/Sprites/{name}/frame_{i:02d}.png"
                pm=p+".meta"
                raw=z.read(p)
                meta=z.read(pm).decode("utf8")
                m=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",meta)
                check(m is not None and "textureType: 8" in meta
                      and "spriteMode: 1" in meta,
                      "original real Unity Sprite importer missing/invalid")
                expected=frames[(name,i)]
                check(H(raw)==expected["sha256"] and len(raw)==expected["bytes"],
                      "original per-frame immutable real PNG payload drift")
                guids[(name,i)]=m.group(1)
                original_pngs[(name,i)]=H(raw)
    check(len(guids)==32 and len(set(guids.values()))==32,
          "W11 original 32 sprite GUID collision")
    return guids, original_pngs

def fmt_sprite_ref(guid):
    return "{fileID: 21300000, guid: "+guid+", type: 3}"

def render_clip(name, frame_guids):
    # Unity's serialized YAML format for SpriteRenderer PPtr object reference
    # curves, with explicit 0.8s loop closure to avoid 0.7s short-cycle bug.
    refs=[fmt_sprite_ref(frame_guids[(name,i)]) for i in range(8)]
    refs.append(refs[0])
    times=["0","0.1","0.2","0.3","0.4","0.5","0.6","0.7","0.8"]
    lines=["%YAML 1.1","%TAG !u! tag:unity3d.com,2011:","--- !u!74 &7400000",
    "AnimationClip:","  m_ObjectHideFlags: 0",
    "  m_CorrespondingSourceObject: {fileID: 0}",
    "  m_PrefabInstance: {fileID: 0}","  m_PrefabAsset: {fileID: 0}",
    "  m_Name: B0131_"+name.replace("-","_"),"  serializedVersion: 6",
    "  m_Legacy: 0","  m_Compressed: 0","  m_UseHighQualityCurve: 1",
    "  m_RotationCurves: []","  m_CompressedRotationCurves: []",
    "  m_EulerCurves: []","  m_PositionCurves: []","  m_ScaleCurves: []",
    "  m_FloatCurves: []","  m_PPtrCurves:","  - curve:"]
    for tm,ref in zip(times,refs):
        lines.extend([f"    - time: {tm}",f"      value: {ref}"])
    lines.extend(["    attribute: m_Sprite","    path:","    classID: 212",
                  "    script: {fileID: 0}",
                  "  m_SampleRate: 10","  m_WrapMode: 0",
                  "  m_Bounds:","    m_Center: {x: 0, y: 0, z: 0}",
                  "    m_Extent: {x: 0, y: 0, z: 0}",
                  "  m_ClipBindingConstant:","    genericBindings:",
                  "    - serializedVersion: 2","      path: 0",
                  "      attribute: 0","      script: {fileID: 0}",
                  "      typeID: 212","      customType: 23","      isPPtrCurve: 1",
                  "    pptrCurveMapping:"])
    for ref in refs:
        lines.append("    - "+ref)
    lines.extend([
    "  m_AnimationClipSettings:",
    "    serializedVersion: 2",
    "    m_AdditiveReferencePoseClip: {fileID: 0}",
    "    m_AdditiveReferencePoseTime: 0",
    "    m_StartTime: 0",
    "    m_StopTime: 0.8",
    "    m_OrientationOffsetY: 0",
    "    m_Level: 0",
    "    m_CycleOffset: 0",
    "    m_HasAdditiveReferencePose: 0",
    "    m_LoopTime: 1",
    "    m_LoopBlend: 0",
    "    m_LoopBlendOrientation: 0",
    "    m_LoopBlendPositionY: 0",
    "    m_LoopBlendPositionXZ: 0",
    "    m_KeepOriginalOrientation: 0",
    "    m_KeepOriginalPositionY: 1",
    "    m_KeepOriginalPositionXZ: 0",
    "    m_HeightFromFeet: 0",
    "    m_Mirror: 0",
    "  m_EditorCurves: []",
    "  m_EulerEditorCurves: []",
    "  m_HasGenericRootTransform: 0",
    "  m_HasMotionFloatCurves: 0",
    "  m_Events: []"])
    return ("\n".join(lines)+"\n").encode("utf8")

def produce_files(guids):
    files={ADDON_FOLDER+".meta":folder_meta(ADDON_FOLDER)}
    for name in NAMES:
        p=ADDON_FOLDER+"/B0131_"+name.replace("-","_")+".anim"
        files[p]=render_clip(name,guids)
        files[p+".meta"]=clip_meta(p)
    return files

def audit_files(files,original_guids):
    check(len(files)==9 and len(set(p.casefold() for p in files))==9,
          "only 9 own add-on paths and Windows casefold uniqueness")
    check(all(p==ADDON_FOLDER+".meta" or p.startswith(ADDON_FOLDER+"/")
              for p in files), "alien original asset namespace/file")
    check(len([p for p in files if p.endswith(".anim")])==4,
          "exactly four real authored AnimationClips")
    seen=[]
    expected={"0","0.1","0.2","0.3","0.4","0.5","0.6","0.7","0.8"}
    for name in NAMES:
        p=ADDON_FOLDER+"/B0131_"+name.replace("-","_")+".anim"
        body=files[p].decode("utf8")
        check(body.startswith("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!74 &7400000\n")
              and "  m_SampleRate: 10\n" in body
              and "  m_StopTime: 0.8\n" in body
              and "  m_LoopTime: 1\n" in body
              and "    attribute: m_Sprite\n    path:\n    classID: 212\n" in body,
              "original Unity native clip syntax, FPS/loop/SpriteRenderer route")
        block=body.split("  m_PPtrCurves:\n",1)[1].split("  m_SampleRate:",1)[0]
        pairs=re.findall(r"    - time: ([0-9.]+)\n      value: \{fileID: 21300000, guid: ([a-f0-9]{32}), type: 3\}",block)
        check(len(pairs)==9 and [x[0] for x in pairs]==["0","0.1","0.2","0.3","0.4","0.5","0.6","0.7","0.8"],
              "exact nine chronological keyframes needed for 800ms loop")
        check([x[1] for x in pairs]==[original_guids[(name,i)] for i in range(8)]+[original_guids[(name,0)]],
              "real 32 original source GUID sequence, correct loop closure")
        check(body.count("    - {fileID: 21300000, guid: ")==9,
              "actual native pptrCurveMapping nine references")
        check(body.count("  m_PPtrCurves:")==1 and body.count("  m_AnimationClipSettings:")==1,
              "unique native Unity serialized animation block")
    for p,data in files.items():
        check(".." not in p.split("/") and not p.startswith("/") and "\\" not in p,
              "ZIP path traversal")
        if p.endswith(".meta"):
            check(b"fileFormatVersion: 2\n" in data
                  and ("guid: "+stable_guid(p[:-5])).encode() in data,
                  "own actual correct deterministic GUID")
            seen.append(stable_guid(p[:-5]))
    check(len(seen)==5 and len(set(seen))==5
          and set(seen).isdisjoint(set(original_guids.values())),
          "5 unique owner-only original Clip/Folder GUIDs")
    return len(seen)

def build_zip(files,path):
    with zipfile.ZipFile(path,"w",compression=zipfile.ZIP_STORED) as z:
        for p,data in sorted(files.items()):
            info=zipfile.ZipInfo(p,(2020,1,1,0,0,0))
            info.create_system=3
            info.external_attr=0o100644<<16
            info.compress_type=zipfile.ZIP_STORED
            z.writestr(info,data)
    with zipfile.ZipFile(path) as z:
        check(z.testzip() is None and set(z.namelist())==set(files),
              "physical author-generated Unity ZIP CRC/path inventory")
        check(all(z.read(p)==bytes_ for p,bytes_ in files.items()),
              "original ZIP actual member byte parity")

def reject_mutants(files, guids):
    original_p=ADDON_FOLDER+"/B0131_collision_overlap.anim"
    orig=files[original_p]
    mutations=[
        orig.replace(b"m_StopTime: 0.8",b"m_StopTime: 0.7"),
        orig.replace(b"m_LoopTime: 1",b"m_LoopTime: 0"),
        orig.replace(b"classID: 212",b"classID: 1"),
        orig.replace(b"attribute: m_Sprite",b"attribute: m_Rotation"),
        orig.replace(guids[(NAMES[0],0)].encode(),b"0"*32,1),
        orig.replace(b"    - time: 0.8",b"    - time: 0.9"),
    ]
    for m in mutations:
        bad=dict(files)
        bad[original_p]=m
        try:audit_files(bad,guids)
        except ValueError:pass
        else:raise AssertionError("FALSE_GREEN tampered Unity AnimationClip accepted")
    bad=dict(files)
    bad.pop(original_p+".meta")
    try:audit_files(bad,guids)
    except ValueError:pass
    else:raise AssertionError("FALSE_GREEN missing meta accepted")
    return len(mutations)+1

def run(original,output):
    guids,source_hashes=load_w11(original)
    output.mkdir(parents=True,exist_ok=True)
    files=produce_files(guids)
    own_guids=audit_files(files,guids)
    negative=reject_mutants(files,guids)
    for p,raw in sorted(files.items()):
        target=output/"loose"/p
        target.parent.mkdir(parents=True,exist_ok=True)
        target.write_bytes(raw)
        check(target.read_bytes()==raw,"actual physical loose Unity source readback")
    zip_path=output/"B0131-DockDeniedVfx-UnityClips-ADDON.zip"
    build_zip(files,zip_path)
    receipt={
        "package_id":PKG,"version":VERSION,"source_pr":505,
        "source_exact_sha":SOURCE_SHA,"source_original_zip_sha256":SOURCE_ZIP_SHA,
        "source_original_rgba_sprite_sha256":[{"state":n,"frame":i,
                                                  "sha256":source_hashes[(n,i)],
                                                  "unity_sprite_guid":guids[(n,i)]}
                                                 for n in NAMES for i in range(8)],
        "owner":"B01-31","own_clip_count":4,"animation_frames_per_clip":8,
        "duration_seconds":0.8,"fps":10,
        "original_sprite_reference_fileID":21300000,
        "actual_clip_addon_files":[{"path":p,"sha256":H(data),"bytes":len(data)}
                                   for p,data in sorted(files.items())],
        "actual_unity_addon_zip_sha256":H(zip_path.read_bytes()),
        "actual_unity_addon_zip_bytes":zip_path.stat().st_size,
        "actual_unity_addon_zip_entries":len(files),
        "unique_own_meta_guids":own_guids,
        "reject_negative_mutants":negative,
        "no_gameplay_changes":True,"native_unity":"NOT_RUN",
        "formal_qa":"B01-40/B01-60 PENDING",
        "codex_ack":False,"integrated":False,"G2":"HOLD",
        "limitations":[
            "Real Unity .anim YAML files authored/verified offline only; Unity Editor import/Play Mode NOT_RUN",
            "No Unity AnimatorController, prefab, GameObject or scene supplied; host must authorize playback integration",
            "Requires exact original W11 Sprite ZIP installed first; GUIDs not regenerated",
            "No gameplay/public API/PhysX validation; #510 security not implemented here"
        ]
    }
    (output/"receipt.json").write_bytes((json.dumps(receipt,indent=2,sort_keys=True)+"\n").encode("utf8"))
    print(json.dumps({"status":"PASS","own_anim_clips":4,"original_sprite_guids":32,
                      "clip_keys_including_loop_closure":9,"actual_addon_files":len(files),
                      "own_unique_guid":own_guids,
                      "negative_mutants_rejected":negative,
                      "addon_zip_sha256":receipt["actual_unity_addon_zip_sha256"],
                      "native_unity":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True,help="Exact original W11 Actions output containing real sprite ZIP and receipt")
    p.add_argument("--output",required=True)
    a=p.parse_args()
    try:sys.exit(run(Path(a.original),Path(a.output)))
    except Exception as exc:
        print("W12_UNITY_ANIM_SOURCE_FAIL "+type(exc).__name__+": "+str(exc),file=sys.stderr)
        raise
