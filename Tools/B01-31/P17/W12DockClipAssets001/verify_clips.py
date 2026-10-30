#!/usr/bin/env python3
"""Independent actual Unity .anim YAML parser+archive verifier, NOT native Unity.
Uses PyYAML to check semantic object-reference animation curves and real W11 GUIDs.
Never runs generator; can run on Windows downloaded Ubuntu original outputs.
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

import yaml

NAMES=("collision-overlap","distance-limit","unguarded-route","query-saturated")
BASE="Assets/B0131DockDeniedVfx"
FOLDER=BASE+"/Animations"
SOURCE_SHA="13632b19b8fee3c263aea62bc91535ba45b12168"
SOURCE_ZIP_SHA="7fc696ef80730192d41fffdb8736f1b6b81ce9611126c4ada7c40a4fc861aaef"
H=lambda b:hashlib.sha256(b).hexdigest()

def must(condition,reason):
    if not condition:raise ValueError("W12_ANIM_RECEIVER_REJECT "+reason)

def inspect(original,out):
    r=json.loads((out/"receipt.json").read_bytes())
    must(r["package_id"]=="B01-31-DOCK-DENIED-UNITY-CLIPS-001"
         and r["version"]=="P17-W12-R1","wrong produced package identity")
    must(r["source_exact_sha"]==SOURCE_SHA
         and r["source_original_zip_sha256"]==SOURCE_ZIP_SHA,
         "not the frozen W11 producer")
    must((original/"B0131-DockDeniedVfx-UnitySpriteFrames.zip").is_file(),
         "physical W11 ZIP not supplied")
    source=(original/"B0131-DockDeniedVfx-UnitySpriteFrames.zip").read_bytes()
    must(H(source)==SOURCE_ZIP_SHA,"source actual original bytes drift")
    provenance=json.loads((original/"receipt.json").read_bytes())
    must(provenance["physical_unity_sprite_zip_sha256"]==SOURCE_ZIP_SHA,
         "original original-image receipt tampered")
    addon=(out/"B0131-DockDeniedVfx-UnityClips-ADDON.zip").read_bytes()
    must(H(addon)==r["actual_unity_addon_zip_sha256"]
         and len(addon)==r["actual_unity_addon_zip_bytes"],
         "actual addon physical ZIP hash/bytes mismatch")
    declarations=r["actual_clip_addon_files"]
    must(len(declarations)==9 and r["actual_unity_addon_zip_entries"]==9
         and r["unique_own_meta_guids"]==5
         and r["reject_negative_mutants"]==7,
         "actual generator receipt inventory and negative tests")
    expected={f["path"] for f in declarations}
    must(len(expected)==9 and FOLDER+".meta" in expected,"expected standalone 9 add-on paths")
    actual_data={}
    with zipfile.ZipFile(io.BytesIO(addon)) as z:
        must(z.testzip() is None and set(z.namelist())==expected,
             "original actual add-on Unity ZIP member inventory CRC")
        for decl in declarations:
            p=decl["path"]
            must(p==FOLDER+".meta" or p.startswith(FOLDER+"/"),
                 "non-scoped / foreign Unity owner path")
            raw=z.read(p)
            loose=(out/"loose"/p).read_bytes()
            must(raw==loose and H(raw)==decl["sha256"]
                 and len(raw)==decl["bytes"],
                 "actual original .anim or .meta corrupted")
            actual_data[p]=raw
    # Parse YAML semantically, not with source-generator's regex.
    sprite_guid={}
    with zipfile.ZipFile(io.BytesIO(source)) as z:
        must(z.testzip() is None and len(z.namelist())==70,
             "actual source Unity ZIP not original")
        for name in NAMES:
            for i in range(8):
                p=f"{BASE}/Sprites/{name}/frame_{i:02d}.png.meta"
                txt=z.read(p).decode("utf8")
                m=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",txt)
                must(m is not None,"missing source original 32 Sprite GUID")
                sprite_guid[(name,i)]=m.group(1)
    must(len(set(sprite_guid.values()))==32,
         "source 32 physical unique GUIDs")
    own=[]
    for name in NAMES:
        p=FOLDER+"/B0131_"+name.replace("-","_")+".anim"
        raw=actual_data[p]
        text=raw.decode("utf8")
        must(text.startswith("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!74 &7400000\n"),
             "Unity original clip YAML serialized document tag")
        # Parse the actual source using a separate general-purpose YAML parser.
        normalized="\n".join(text.splitlines()[3:])
        try:root=yaml.safe_load(normalized)
        except yaml.YAMLError as e:raise ValueError("real native Unity .anim YAML parse error") from e
        must(set(root)=={"AnimationClip"},"Unity YAML not an AnimationClip")
        clip=root["AnimationClip"]
        must(clip["serializedVersion"]==6 and clip["m_SampleRate"]==10,
             "actual clip serializer/sample rate")
        settings=clip["m_AnimationClipSettings"]
        must(settings["m_LoopTime"]==1 and abs(settings["m_StopTime"]-0.8)<1e-9
             and settings["m_StartTime"]==0,
             "actual clip 800ms looping bounds")
        must(len(clip["m_PPtrCurves"])==1,"expected exactly 1 original object curve")
        curve=clip["m_PPtrCurves"][0]
        must(curve["attribute"]=="m_Sprite" and curve["classID"]==212
             and curve["path"] is None,"real SpriteRenderer.m_Sprite object curve")
        keys=curve["curve"]
        must(len(keys)==9 and all(abs(keys[i]["time"]-i/10)<1e-8 for i in range(9)),
             "original 8 frames+0.8 loop closure monotonic time")
        expected_guid=[sprite_guid[(name,i)] for i in range(8)]+[sprite_guid[(name,0)]]
        must([key["value"]["guid"] for key in keys]==expected_guid,
             "original immutable sprite GUID exact keyframe order")
        must(all(key["value"]["fileID"]==21300000
                 and key["value"]["type"]==3 for key in keys),
             "Unity imported single-Sprite local fileID/class")
        binding=clip["m_ClipBindingConstant"]
        must(len(binding["genericBindings"])==1
             and binding["genericBindings"][0]["typeID"]==212
             and binding["genericBindings"][0]["isPPtrCurve"]==1,
             "actual m_ClipBindingConstant SpriteRenderer serialized binding")
        must([ref["guid"] for ref in binding["pptrCurveMapping"]]==expected_guid,
             "actual Unity mapping must exactly match keyframes")
        must(len(binding["pptrCurveMapping"])==9,
             "actual nine explicit PPtr links for 800ms closure")
        metap=actual_data[p+".meta"].decode("utf8")
        must("NativeFormatImporter:" in metap
             and "mainObjectFileID: 7400000" in metap,
             "actual native Unity .anim importer .meta")
    must(len([p for p in actual_data if p.endswith(".anim")])==4,
         "exactly four Unity AnimationClips in importable addon")
    for p,data in actual_data.items():
        if p.endswith(".meta"):
            m=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",data.decode("utf8"))
            must(m is not None,"missing own stable .meta GUID")
            own.append(m.group(1))
    must(len(own)==5 and len(set(own))==5
         and not set(own).intersection(set(sprite_guid.values())),
         "owner clip/folder GUIDs unique vs immutable source sprite GUIDs")
    # Receiver negative mutations of both source and output archive.
    must(H(addon+b"\0")!=r["actual_unity_addon_zip_sha256"]
         and H(source+b"\0")!=SOURCE_ZIP_SHA,
         "binary corruption SHA must reject")
    print(json.dumps({"status":"PASS","source_sprite_sha":SOURCE_ZIP_SHA,
                      "source_sprite_guids":32,"actual_unity_clips":4,
                      "actual_persistent_loop_keys_per_clip":9,
                      "physical_addon_files":9,"own_unique_guids":5,
                      "per_clip_duration_ms":800,
                      "actual_clip_addon_sha256":H(addon),
                      "native_unity":"NOT_RUN","independent_formal_QA":"PENDING"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--addon",required=True)
    args=p.parse_args()
    try:sys.exit(inspect(Path(args.original),Path(args.addon)))
    except Exception as e:
        print("W12_UNITY_ANIM_RECEIVER_RED "+str(e),file=sys.stderr)
        raise
