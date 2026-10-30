#!/usr/bin/env python3
"""Original B01-31 UNITY .anim SOURCE ART add-on for immutable W14 Cable Snap.
Four real Unity AnimationClip YAMLs: 3 separate phases + ONE 36-frame one-shot.
Consumes exact immutable W14 original sprite ZIP bytes, never modifies sprites.
This is NOT native Unity Editor, AnimatorController, gameplay, Save/Load or PhysX.
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

PKG="B01-31-CABLE-SNAP-ONE-SHOT-UNITY-CLIPS-001"
VERSION="P17-W15-R1"
SOURCE_PR=557
SOURCE_SHA="d6eb3406120bef2f921079dffe01d898985d7a25"
SOURCE_ZIP_SHA="b164a1a5ac305d1c0147b131eb16f104d64685ea9267b3d8d20340028ba9e902"
NS="Assets/B0131CableSnapVfx"
FOLDER=NS+"/Animations"
STATES=("fracture","discharge","cooldown")
CLIPS=STATES+("full_sequence",)
H=lambda b:hashlib.sha256(b).hexdigest()

def require(cond,why):
    if not cond:raise ValueError("W15_CABLE_CLIP_FAIL_CLOSED "+why)

def guid(path):
    return H((PKG+"/"+VERSION+"/"+path).encode("utf-8"))[:32]

def meta_for_folder(path):
    return (f"fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\n"
            "DefaultImporter:\n  externalObjects: {}\n  userData:\n"
            "  assetBundleName:\n  assetBundleVariant:\n").encode()

def meta_for_clip(path):
    return (f"fileFormatVersion: 2\nguid: {guid(path)}\n"
            "NativeFormatImporter:\n  externalObjects: {}\n"
            "  mainObjectFileID: 7400000\n  userData:\n"
            "  assetBundleName:\n  assetBundleVariant:\n").encode()

def read_source(path:Path):
    zip_path=path/"B0131-CableSnap-UnitySpriteImport.zip"
    require(zip_path.is_file(),"missing original W14 physical Unity Sprite ZIP")
    blob=zip_path.read_bytes()
    require(H(blob)==SOURCE_ZIP_SHA,"original W14 immutable SHA256 ZIP is wrong")
    source=json.loads((path/"receipt.json").read_bytes())
    require(source["package_id"]=="B01-31-CABLE-SNAP-VFX-001"
            and source["version"]=="P17-W14-R1"
            and source["unity_sprite_zip_sha256"]==SOURCE_ZIP_SHA
            and source["unity_sprite_zip_members"]==77
            and source["unique_unity_guids"]==41
            and source["selected_reattach_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431",
            "source receipt identity, producer version or source provenance mismatch")
    frames={(x["state"],x["frame"]):x for x in source["sprites"]}
    require(len(frames)==36
            and set(frames)=={(s,i) for s in STATES for i in range(12)},
            "real original full 3x12 PNG inventory missing")
    actual_guids={}
    actual_png_sha={}
    with zipfile.ZipFile(io.BytesIO(blob)) as z:
        require(z.testzip() is None and len(z.namelist())==77,
                "actual original physical ZIP 77 members CRC")
        for state in STATES:
            for i in range(12):
                p=f"{NS}/Sprites/{state}/frame_{i:02d}.png"
                data=z.read(p)
                metadata=z.read(p+".meta").decode("utf8")
                match=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",metadata)
                require(match is not None
                        and "textureType: 8" in metadata
                        and "spriteMode: 1" in metadata,
                        "real immutable Unity Sprite .meta GUID/texture format")
                r=frames[(state,i)]
                require(H(data)==r["sha256"] and len(data)==r["bytes"],
                        "source actual PNG byte digest receipt mismatch")
                actual_guids[(state,i)]=match.group(1)
                actual_png_sha[(state,i)]=H(data)
    require(len(set(actual_guids.values()))==36,
            "source 36 independent immutable sprite GUIDs")
    return actual_guids,actual_png_sha

def frame_plan(which):
    if which=="full_sequence":
        return [(s,i) for s in STATES for i in range(12)]
    require(which in STATES,"not an original phase")
    return [(which,i) for i in range(12)]

def clock_time(index):
    # Exact 80ms authored original GIF/source cadence, not 10fps.
    ticks=index*8
    return str(ticks//100) if ticks%100==0 else f"{ticks/100:.2f}".rstrip("0").rstrip(".")

def pptr(id):
    return "{fileID: 21300000, guid: "+id+", type: 3}"

def render_clip(which,original_guids):
    plan=frame_plan(which)
    # A distinct final HOLD key at the EXACT end prevents a 12-frame
    # 960ms phase from truncating at last frame's t=0.88.
    keys=[(clock_time(i),pptr(original_guids[k])) for i,k in enumerate(plan)]
    keys.append((clock_time(len(plan)),pptr(original_guids[plan[-1]])))
    name="B0131_CableSnap_"+which
    tstop=clock_time(len(plan))
    lines=["%YAML 1.1","%TAG !u! tag:unity3d.com,2011:",
        "--- !u!74 &7400000","AnimationClip:",
        "  m_ObjectHideFlags: 0","  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}","  m_PrefabAsset: {fileID: 0}",
        "  m_Name: "+name,"  serializedVersion: 6",
        "  m_Legacy: 0","  m_Compressed: 0","  m_UseHighQualityCurve: 1",
        "  m_RotationCurves: []","  m_CompressedRotationCurves: []",
        "  m_EulerCurves: []","  m_PositionCurves: []",
        "  m_ScaleCurves: []","  m_FloatCurves: []",
        "  m_PPtrCurves:","  - curve:"]
    for time,ref in keys:
        lines.extend(["    - time: "+time,"      value: "+ref])
    lines.extend([
        "    attribute: m_Sprite",
        "    path:",
        "    classID: 212",
        "    script: {fileID: 0}",
        "  m_SampleRate: 12.5",
        "  m_WrapMode: 0",
        "  m_Bounds:",
        "    m_Center: {x: 0, y: 0, z: 0}",
        "    m_Extent: {x: 0, y: 0, z: 0}",
        "  m_ClipBindingConstant:",
        "    genericBindings:",
        "    - serializedVersion: 2",
        "      path: 0",
        "      attribute: 0",
        "      script: {fileID: 0}",
        "      typeID: 212",
        "      customType: 23",
        "      isPPtrCurve: 1",
        "    pptrCurveMapping:"])
    for _,ref in keys:
        lines.append("    - "+ref)
    lines.extend([
        "  m_AnimationClipSettings:",
        "    serializedVersion: 2",
        "    m_AdditiveReferencePoseClip: {fileID: 0}",
        "    m_AdditiveReferencePoseTime: 0",
        "    m_StartTime: 0",
        "    m_StopTime: "+tstop,
        "    m_OrientationOffsetY: 0",
        "    m_Level: 0",
        "    m_CycleOffset: 0",
        "    m_HasAdditiveReferencePose: 0",
        "    m_LoopTime: 0",
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

def clip_path(which):
    return FOLDER+"/B0131_CableSnap_"+which+".anim"

def produce(guids):
    files={FOLDER+".meta":meta_for_folder(FOLDER)}
    for name in CLIPS:
        p=clip_path(name)
        files[p]=render_clip(name,guids)
        files[p+".meta"]=meta_for_clip(p)
    return files

def audit(files,guids):
    require(len(files)==9
            and len({x.lower() for x in files})==9
            and all(x==FOLDER+".meta" or x.startswith(FOLDER+"/") for x in files),
            "exclusive nine W15 add-on asset paths only")
    seen=[]
    for p,b in files.items():
        require(p.startswith(NS+"/") and ".." not in p.split("/")
                and "\\" not in p,"non-traversable Unity import path")
        if p.endswith(".meta"):
            m=re.search(r"(?m)^guid: ([0-9a-f]{32})\s*$",b.decode())
            require(m and m.group(1)==guid(p[:-5]),
                    "all original metadata own deterministic GUID")
            seen.append(m.group(1))
    require(len(seen)==5 and len(set(seen))==5 and
            set(seen).isdisjoint(set(guids.values())),
            "five new original GUIDs disjoint from 36 source sprite meta")
    for phase in CLIPS:
        raw=files[clip_path(phase)].decode()
        plan=frame_plan(phase)
        expected=[guids[x] for x in plan]+[guids[plan[-1]]]
        times=[clock_time(i) for i in range(len(plan)+1)]
        curve=raw.split("  m_PPtrCurves:\n",1)[1].split("  m_SampleRate:",1)[0]
        pairs=re.findall(r"    - time: ([0-9.]+)\n      value: \{fileID: 21300000, guid: ([0-9a-f]{32}), type: 3\}",curve)
        require([x[0] for x in pairs]==times and
                [x[1] for x in pairs]==expected,
                "Unity native SpriteRenderer curve correct ordered source GUID and exact 80ms timing")
        require(raw.count("    - {fileID: 21300000, guid: ")==len(plan)+1
                and "    attribute: m_Sprite\n    path:\n    classID: 212\n" in raw
                and "  m_SampleRate: 12.5\n" in raw
                and "    m_LoopTime: 0\n" in raw
                and "    m_StopTime: "+clock_time(len(plan))+"\n" in raw,
                "nonlooping source native AnimationClip YAML serializer/binding/hold")
        require("  m_ClipBindingConstant:" in raw
                and "      typeID: 212\n" in raw
                and "      isPPtrCurve: 1\n" in raw,
                "actual serialized PPtr mapping")
    return len(seen)

def negative_tests(files,original_guids):
    name=clip_path("fracture")
    content=files[name]
    first=original_guids[("fracture",0)].encode()
    variants=[
      content.replace(b"m_LoopTime: 0",b"m_LoopTime: 1",1),
      content.replace(b"m_StopTime: 0.96",b"m_StopTime: 0.88",1),
      content.replace(b"m_SampleRate: 12.5",b"m_SampleRate: 10",1),
      content.replace(b"attribute: m_Sprite",b"attribute: m_Transform",1),
      content.replace(first,b"0"*32,1),
      content.replace(b"    - time: 0.96",b"    - time: 1.04",1),
      content.replace(b"typeID: 212",b"typeID: 1",1),
    ]
    for variant in variants:
        bad=dict(files);bad[name]=variant
        try:audit(bad,original_guids)
        except ValueError:pass
        else:raise AssertionError("FALSE GREEN authored animation mutation accepted")
    bad=dict(files);bad.pop(name+".meta")
    try:audit(bad,original_guids)
    except ValueError:pass
    else:raise AssertionError("FALSE GREEN missing clip meta accepted")
    return 8

def physical_zip(files,target):
    with zipfile.ZipFile(target,"w",compression=zipfile.ZIP_STORED) as z:
        for p,data in sorted(files.items()):
            info=zipfile.ZipInfo(p,(2020,1,1,0,0,0))
            info.create_system=3
            info.external_attr=0o100644<<16
            info.compress_type=zipfile.ZIP_STORED
            z.writestr(info,data)
    with zipfile.ZipFile(target) as z:
        require(z.testzip() is None and set(z.namelist())==set(files)
                and len(z.namelist())==9,"actual original ZIP nine assets/CRC")
        require(all(z.read(p)==b for p,b in files.items()),
                "real source authored Unity assets byte readback")

def run(original:Path,output:Path):
    guids,original_pngs=read_source(original)
    files=produce(guids)
    meta_count=audit(files,guids)
    negatives=negative_tests(files,guids)
    output.mkdir(parents=True,exist_ok=True)
    for p,data in sorted(files.items()):
        file=output/"loose"/p
        file.parent.mkdir(parents=True,exist_ok=True)
        file.write_bytes(data)
        require(file.read_bytes()==data,"physical authored loose .anim/.meta not writable")
    archive=output/"B0131-CableSnap-OneShot-UnityClips-ADDON.zip"
    physical_zip(files,archive)
    clip_specs=[]
    for which in CLIPS:
        frame_list=frame_plan(which)
        clip_specs.append({
            "clip":which,"name":clip_path(which),
            "physical_asset_sha256":H(files[clip_path(which)]),
            "frames":len(frame_list),"keyframes_with_terminal_hold":len(frame_list)+1,
            "sample_fps":12.5,"frame_interval_ms":80,
            "duration_ms":len(frame_list)*80,
            "loop":False,
            "all_original_sprite_guid_refs":
                [guids[x] for x in frame_list]+[guids[frame_list[-1]]]
        })
    manifest={
        "package_id":PKG,"version":VERSION,"owner":"B01-31",
        "input_original_source_pr":SOURCE_PR,
        "input_original_source_exact_sha":SOURCE_SHA,
        "input_original_sprite_zip_sha256":SOURCE_ZIP_SHA,
        "input_actual_original_sprite_count":len(guids),
        "input_original_sprite_sha256":[{"state":s,"frame":i,
                                        "sha256":original_pngs[(s,i)],
                                        "sprite_guid":guids[(s,i)]}
                                       for s in STATES for i in range(12)],
        "clip_specs":clip_specs,
        "actual_new_Unity_asset_files":[{"path":p,"sha256":H(b),"bytes":len(b)}
                                        for p,b in sorted(files.items())],
        "actual_addon_Unity_zip_sha256":H(archive.read_bytes()),
        "actual_addon_Unity_zip_bytes":archive.stat().st_size,
        "actual_addon_Unity_zip_members":len(files),
        "actual_own_unique_GUIDs":meta_count,
        "negative_mutants_REJECTED":negatives,
        "original_art_source_read_only":True,
        "native_unity":"NOT_RUN",
        "game_save_load":"NOT_RUN",
        "gameplay_script":"NONE",
        "AnimatorController":"NOT_IMPLEMENTED",
        "B01_40_formal_G0":"PENDING",
        "B01_60_native_G1":"NOT_RUN",
        "G2":"HOLD",
        "codex_offer":False,"codex_ack":False,"integrated":False,
        "frozen_reattach_510":"d94400d95d13db7f27e1533df05c9292d5380431",
        "frozen_save_contract_556":"33094f154c7b91119b36d58aab285e6e83c5de57"
    }
    (output/"receipt.json").write_bytes((json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode())
    print(json.dumps({"status":"PASS","actual_Unity_AnimationClips":len(CLIPS),
                      "phase_clip_keys":13,"full_one_shot_keys":37,
                      "full_one_shot_duration_ms":2880,
                      "source_sprite_GUID":len(guids),
                      "physical_addon_zip_members":len(files),
                      "own_unique_GUID":meta_count,
                      "negative_mutations_rejected":negatives,
                      "inner_addon_zip_sha256":manifest["actual_addon_Unity_zip_sha256"],
                      "native_Unity":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    ap=argparse.ArgumentParser()
    ap.add_argument("--original",required=True)
    ap.add_argument("--output",required=True)
    args=ap.parse_args()
    try:sys.exit(run(Path(args.original),Path(args.output)))
    except Exception as e:
        print("W15_ORIGINAL_ANIMCLIP_SOURCE_RED "+str(e),file=sys.stderr)
        raise
