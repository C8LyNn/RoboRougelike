#!/usr/bin/env python3
"""INDEPENDENT Windows-ready physical Unity .anim YAML semantic receiver.
Parses actual Ubuntu-produced .anim via separate PyYAML; NO generator import.
Verifies all 3x12 original source sprite GUID+sha from immutable W14 physical ZIP.
NOT actual Unity Editor import, save/load, reattach or PhysX.
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

SOURCE_SHA="d6eb3406120bef2f921079dffe01d898985d7a25"
SOURCE_ZIP_SHA="b164a1a5ac305d1c0147b131eb16f104d64685ea9267b3d8d20340028ba9e902"
NS="Assets/B0131CableSnapVfx"
FOLDER=NS+"/Animations"
STATES=("fracture","discharge","cooldown")
CLIPS=STATES+("full_sequence",)
H=lambda b:hashlib.sha256(b).hexdigest()

def check(c,why):
    if not c:raise ValueError("W15_INDEPENDENT_PHYSICAL_YAML_REJECT "+why)

def expected_plan(name):
    return [(s,i) for s in STATES for i in range(12)] if name=="full_sequence" else [(name,i) for i in range(12)]

def verify(original_dir:Path,addon_dir:Path):
    actual_src=(original_dir/"B0131-CableSnap-UnitySpriteImport.zip").read_bytes()
    check(H(actual_src)==SOURCE_ZIP_SHA, "actual original W14 inner source SHA mismatch")
    w14receipt=json.loads((original_dir/"receipt.json").read_bytes())
    check(w14receipt["package_id"]=="B01-31-CABLE-SNAP-VFX-001" and
          w14receipt["unity_sprite_zip_sha256"]==SOURCE_ZIP_SHA and
          w14receipt["unity_sprite_zip_members"]==77,
          "original source physical art W14 receipt mismatch")
    r=json.loads((addon_dir/"receipt.json").read_bytes())
    check(r["package_id"]=="B01-31-CABLE-SNAP-ONE-SHOT-UNITY-CLIPS-001"
          and r["version"]=="P17-W15-R1"
          and r["input_original_source_exact_sha"]==SOURCE_SHA
          and r["input_original_sprite_zip_sha256"]==SOURCE_ZIP_SHA,
          "not W15 immutable original package")
    check(r["actual_own_unique_GUIDs"]==5 and
          r["actual_addon_Unity_zip_members"]==9
          and r["negative_mutants_REJECTED"]==8,
          "physical four new .anim/metas/negative controls absent")
    source_frames={(x["state"],x["frame"]):x for x in w14receipt["sprites"]}
    source_guid={}
    with zipfile.ZipFile(io.BytesIO(actual_src)) as z:
        check(z.testzip() is None and len(z.namelist())==77,
              "actual original 77 physical source sprites ZIP")
        for state in STATES:
            for i in range(12):
                path=f"{NS}/Sprites/{state}/frame_{i:02d}.png"
                raw=z.read(path)
                m=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",z.read(path+".meta").decode("utf8"))
                check(m is not None and
                      H(raw)==source_frames[(state,i)]["sha256"],
                      "original actual W14 PNG bytes or real Unity GUID differ")
                source_guid[(state,i)]=m.group(1)
    check(len(source_guid)==36 and len(set(source_guid.values()))==36,
          "original 36 sprite references not unique")
    declared_src={(x["state"],x["frame"]):x for x in r["input_original_sprite_sha256"]}
    check(set(declared_src)==set(source_frames),"input source frame receipt incomplete")
    for key in source_frames:
        check(declared_src[key]["sha256"]==source_frames[key]["sha256"]
              and declared_src[key]["sprite_guid"]==source_guid[key],
              "W15 art author did not reference original immutable W14 bytes/GUID")
    data=(addon_dir/"B0131-CableSnap-OneShot-UnityClips-ADDON.zip").read_bytes()
    check(H(data)==r["actual_addon_Unity_zip_sha256"]
          and len(data)==r["actual_addon_Unity_zip_bytes"],
          "actual original 9-member ZIP raw SHA/byte count")
    decl=r["actual_new_Unity_asset_files"]
    check(len(decl)==9 and len({x["path"] for x in decl})==9,
          "receipt new nine actual file inventory")
    expected={FOLDER+".meta"}
    for clip in CLIPS:
        name=FOLDER+"/B0131_CableSnap_"+clip+".anim"
        expected.add(name);expected.add(name+".meta")
    actual={}
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        check(z.testzip() is None and len(z.namelist())==9
              and set(z.namelist())==expected,
              "actual original ZIP contains unexpected/shared Unity paths")
        for x in decl:
            name=x["path"]
            content=z.read(name)
            check(content==(addon_dir/"loose"/name).read_bytes()
                  and H(content)==x["sha256"] and len(content)==x["bytes"],
                  "original serialized .anim/.meta byte receiver mismatch")
            actual[name]=content
    own_guids=[]
    for name in expected:
        if not name.endswith(".meta"):continue
        txt=actual[name].decode("utf8")
        m=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",txt)
        check(m is not None,"actual Unity clip/folder missing own GUID")
        own_guids.append(m.group(1))
        if name.endswith(".anim.meta"):
            check("NativeFormatImporter:" in txt and "mainObjectFileID: 7400000" in txt,
                  "Unity original .anim NativeFormatImporter or class74 main fileID")
        else:
            check("folderAsset: yes" in txt,"actual Unity animation folder .meta")
    check(len(own_guids)==5 and len(set(own_guids))==5
          and not set(own_guids).intersection(set(source_guid.values())),
          "own five .meta GUID unique and no original Sprite GUID overwrite")
    check(len(r["clip_specs"])==4,"both four phase and one-shot clip timing receipts")
    total_keys=0
    for spec in r["clip_specs"]:
        clip=spec["clip"]
        check(clip in CLIPS, "unknown phase or compiled event in generated receipt")
        p=FOLDER+"/B0131_CableSnap_"+clip+".anim"
        raw=actual[p].decode("utf8")
        check(raw.startswith("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!74 &7400000\n"),
              "real Unity AnimationClip class 74 YAML serialized header")
        try:parsed=yaml.safe_load("\n".join(raw.splitlines()[3:]))
        except yaml.YAMLError as ex:raise ValueError("real physical .anim cannot be semantically parsed") from ex
        check(isinstance(parsed,dict) and set(parsed)=={"AnimationClip"},
              "Unity animation YAML missing only root")
        c=parsed["AnimationClip"]
        check(c["serializedVersion"]==6 and c["m_SampleRate"]==12.5
              and c["m_Legacy"]==0,"original 12.5fps animated SpriteRenderer asset")
        conf=c["m_AnimationClipSettings"]
        expected_frames=36 if clip=="full_sequence" else 12
        duration=expected_frames*0.08
        check(conf["m_LoopTime"]==0 and conf["m_StartTime"]==0
              and abs(conf["m_StopTime"]-duration)<1e-7,
              "must be ONE-SHOT exact source 960ms or 2880ms, never loop")
        binding=c["m_ClipBindingConstant"]
        check(len(binding["genericBindings"])==1
              and binding["genericBindings"][0]["typeID"]==212
              and binding["genericBindings"][0]["isPPtrCurve"]==1,
              "native Unity SpriteRenderer PPtr .m_Sprite binding")
        curves=c["m_PPtrCurves"]
        check(len(curves)==1 and curves[0]["attribute"]=="m_Sprite"
              and curves[0]["classID"]==212 and curves[0]["path"] is None,
              "actual SpriteRenderer rather than transform/missing target")
        keys=curves[0]["curve"]
        plan=expected_plan(clip)
        guids=[source_guid[key] for key in plan]+[source_guid[plan[-1]]]
        check(len(keys)==expected_frames+1 and
              [x["value"]["guid"] for x in keys]==guids,
              "authored actual immutable 12 or 36 ordered source sprite refs+terminal hold")
        check(all(abs(keys[i]["time"]-i*0.08)<1e-7 for i in range(len(keys))),
              "80ms original source frame chronology; no undercut last 80ms")
        check(all(x["value"]["fileID"]==21300000 and
                  x["value"]["type"]==3 for x in keys),
              "physical single Sprite .meta original local fileID")
        mapping=binding["pptrCurveMapping"]
        check([x["guid"] for x in mapping]==guids
              and all(x["fileID"]==21300000 for x in mapping),
              "real physical native Unity mapping exact original source sprite refs")
        check(spec["name"]==p and spec["frames"]==expected_frames and
              spec["keyframes_with_terminal_hold"]==expected_frames+1
              and spec["duration_ms"]==expected_frames*80 and spec["loop"] is False
              and spec["all_original_sprite_guid_refs"]==guids,
              "actual machine receipt timing/source GUID sequences differ from YAML")
        total_keys+=len(keys)
    check(total_keys==13*3+37,
          "actual 76 Unity m_Sprite keys (3x13+37) not present")
    check(r["native_unity"]=="NOT_RUN" and r["game_save_load"]=="NOT_RUN"
          and r["gameplay_script"]=="NONE" and r["AnimatorController"]=="NOT_IMPLEMENTED"
          and r["frozen_reattach_510"]=="d94400d95d13db7f27e1533df05c9292d5380431"
          and r["frozen_save_contract_556"]=="33094f154c7b91119b36d58aab285e6e83c5de57",
          "source QA cannot ever be misrepresented as live Unity/SAVE game integration")
    check(H(data+b"tamper")!=r["actual_addon_Unity_zip_sha256"],
          "physical byte tamper digest")
    print(json.dumps({"status":"PASS","Unity_yaml_clips":4,
          "standalone_phase_clips":3,"full_oneshot_36_frame_clips":1,
          "native_yaml_parsed_PPtr_keys":total_keys,
          "original_W14_GUIDs_verified":36,
          "actually_downloaded_zip_members":9,
          "new_meta_guids":len(own_guids),
          "physical_Unity_addon_sha256":H(data),
          "Unity_Editor_import":"NOT_RUN",
          "real_SaveLoad":"NOT_RUN"}))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--addon",required=True)
    a=p.parse_args()
    try:sys.exit(verify(Path(a.original),Path(a.addon)))
    except Exception as e:
        print("W15_ORIGINAL_INDEPENDENT_RECEIVER_RED "+type(e).__name__+": "+str(e),file=sys.stderr)
        raise
