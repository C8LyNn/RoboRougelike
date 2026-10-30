#!/usr/bin/env python3
"""B01-40 P17 W18: read-only Unity YAML m_Script GUID closure preflight.

Uses original #625/G0 source tests and immutable #575 safe staging without
modification; adds GUID-only legacy/unknown serialized MonoBehaviour detection.
Never mutate canonical/game/Codex or frozen #510/#556/#596.
"""
from __future__ import annotations
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

SOURCE_W17="180849791c4b9594b12f26ef8d9dc0cf90adb61c"
GAME="d94400d95d13db7f27e1533df05c9292d5380431"
SAVE="33094f154c7b91119b36d58aab285e6e83c5de57"
BRIDGE="efb2b06eafedab5469b57f163152d96aca3c5915"
LOSER="1aa8a59ce1bb4fcd998f0b2b1ce25dfaea333f0a"
RECEIVER="3b743acc52bb44ae6a50af56ed9aca46d22821a7"
QA="86f3292c928e52609db6c7afdb2010971d6946f1"
SCRIPT_REF_RE=re.compile(r"\bm_Script\s*:\s*\{([^}]*)\}",re.S)
SCRIPT_TOKEN_RE=re.compile(r"\bm_Script\s*:")
GUID_RE=re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b")
FID_RE=re.compile(r"\bfileID:\s*(-?\d+)\b")
TYPE_RE=re.compile(r"\btype:\s*(\d+)\b")
META_GUID_RE=re.compile(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
YAML_EXT=(".unity",".prefab",".asset",".controller",".overridecontroller")
class Reject(Exception):pass
def check(ok,why):
    if not ok: raise Reject(why)
def module(path,name):
    spec=importlib.util.spec_from_file_location(name,str(path))
    m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def git(root,*args):
    p=subprocess.run(["git","-C",str(root),*args],capture_output=True)
    check(p.returncode==0,"GIT_READ_FAILED:"+repr(args)+p.stderr.decode(errors="replace"))
    return p.stdout
def meta_guids(files):
    guids={}
    for path,data in files.items():
        if not path.endswith(".meta"):continue
        m=META_GUID_RE.search(data.decode("utf-8"))
        check(m is not None,"INVALID_META_GUID:"+path)
        guid=m.group(1).lower()
        check(guid not in guids,"DUPLICATE_GUID:"+guid)
        guids[guid]=path
    return guids
def validate_script_refs(files, whitelisted_meta):
    """Reject unresolved, non-.cs GUID, foreign script and invalid YAML shape.

    Fail closed: external package MonoScripts may need explicit OWNER inventory
    and a new versioned allowlist. No blanket exceptions for arbitrary YAML.
    """
    source_guids=meta_guids(files)
    check(set(source_guids)==set(whitelisted_meta),
          "SCRIPT_GUID_INVENTORY_DIFFERENT_FROM_EXACT_SOURCE")
    refs={}
    for path,data in sorted(files.items()):
        if not path.lower().endswith(YAML_EXT):continue
        try:t=data.decode("utf-8",errors="strict")
        except UnicodeDecodeError:raise Reject("NON_UTF8_UNITY_SERIALIZATION:"+path)
        found=SCRIPT_REF_RE.findall(t)
        check(len(SCRIPT_TOKEN_RE.findall(t))==len(found),
              "MALFORMED_OR_UNRECOGNIZED_m_Script:"+path)
        for value in found:
            g=GUID_RE.search(value);fid=FID_RE.search(value);typ=TYPE_RE.search(value)
            check(g is not None and fid is not None and typ is not None,
                  "INCOMPLETE_m_Script_REFERENCE:"+path)
            guid=g.group(1).lower()
            check(fid.group(1)=="11500000" and typ.group(1)=="3",
                  "WRONG_OR_ABSENT_UNITY_SCRIPT_REFERENCE_FIELDS:"+path)
            owner=whitelisted_meta.get(guid)
            check(owner is not None,"UNRESOLVED_OR_LEGACY_SCRIPT_GUID:"+path+":"+guid)
            check(owner.endswith(".cs.meta"),
                  "SCRIPT_GUID_RESOLVES_TO_NON_CSHARP_ASSET:"+path+":"+owner)
            check(owner[:-5] in files,"MISSING_SCRIPT_SOURCE_FOR_META:"+owner)
            refs.setdefault(path,[]).append({"guid":guid,"owner":owner})
    return refs

def witness_one(roots,original_w17,old,combined):
    """Native Unity-style YAML GUID-only adversarial file that W15 path scanner misses."""
    marker=next(p for p in combined if p.endswith(".cs.meta") and
                p.startswith("Assets/B0123UnitySaveBridge001/"))
    approved_guid=META_GUID_RE.search(combined[marker].decode()).group(1)
    allmeta=meta_guids(combined)
    check(approved_guid.lower() in allmeta,"OWN_CSHARP_GUID_MISSING")
    folder_guid=next(g for g,p in allmeta.items() if not p.endswith(".cs.meta"))
    rogue=next(g for g in
               ("c0ffee00000000000000000000decade",
                "fedcba9876543210fedcba9876543210")
               if g not in allmeta)
    good=(f"%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour:\n"
          f"  m_Script: {{fileID: 11500000, guid: {approved_guid}, type: 3}}\n"
          f"  m_Name: AllowedSelectedOriginal\n").encode()
    wrong=(f"%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour:\n"
           f"  m_Script: {{fileID: 11500000, guid: {rogue}, type: 3}}\n"
           f"  m_Name: LegacyControllerWhoseTypeIsOnlyAGuid\n").encode()
    with tempfile.TemporaryDirectory(prefix="b0140-w18-yaml-") as temp:
        root=Path(temp);host=root/"fake-authorized-host";host.mkdir()
        source_host={
          "ProjectSettings/ProjectVersion.txt":
              git(roots["game"],"show","HEAD:ProjectSettings/ProjectVersion.txt"),
          "Packages/manifest.json":
              git(roots["game"],"show","HEAD:Packages/manifest.json"),
          "Assets/SOMEONE_ELSE/scenes/Room.unity":good,
        }
        for p,v in source_host.items():
            f=host/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(v)
        before,_=old.preflight_host(host,combined)
        approved={**combined,**before}
        positive=validate_script_refs(approved,meta_guids(approved))
        check("Assets/SOMEONE_ELSE/scenes/Room.unity" in positive,
              "POSITIVE_SELECTED_SCENE_IGNORED")
        checks=[]
        def reject_case(data,label,ext=".unity",old_accept=False):
            h=root/("mutated_"+label)
            shutil.copytree(host,h)
            p=h/("Assets/SOMEONE_ELSE/scenes/HIDDEN"+ext)
            p.write_bytes(data)
            original_bytes,_=old.crawl(h)
            # Existing original #575 preflight checks only name/path tokens.
            if old_accept:
                old.preflight_host(h,combined)
                checks.append("EXISTING_W15_NAME_SCAN_BYPASS_WITNESSED")
            try:validate_script_refs({**combined,**original_bytes},
                                     meta_guids({**combined,**original_bytes}))
            except Reject as e:
                checks.append(label+":"+str(e).split(":")[0])
            else:raise AssertionError("FALSE_ACCEPT_OF_"+label)
            after,_=old.crawl(h)
            check(original_bytes==after,"GUARD_MODIFIED_FOREIGN_PROJECT")
        reject_case(wrong,"GUID_ONLY_UNKNOWN_SCRIPT",old_accept=True)
        reject_case(wrong,"GUID_ONLY_IN_PREFAB",".prefab")
        reject_case(wrong,"GUID_ONLY_IN_ASSET",".asset")
        reject_case(good+wrong,"MULTIPLE_MIXED_REFERENCES")
        reject_case(good.replace(b"fileID: 11500000",b"fileID: 987654"),
                    "INVALID_SCRIPT_FILEID")
        reject_case(good.replace(b"type: 3",b"type: 0"),"WRONG_REFERENCE_TYPE")
        reject_case(good.replace(b"guid:",b"absent:"),"NO_SCRIPT_GUID")
        reject_case(good.replace(b"fileID:",b"absent:"),"NO_SCRIPT_FILEID")
        reject_case(good.replace(b"}",b""),"MALFORMED_UNCLOSED_YAML")
        reject_case(good.replace(approved_guid.encode(),folder_guid.encode()),
                    "NON_SCRIPT_META_GUID_REFERENCE")
        reject_case(good.replace(approved_guid.encode(),rogue.upper().encode()),
                    "UPPERCASE_UNRESOLVED_GUID")
        reject_case(good.replace(b"m_Script:",b"m_Script:\n  #dummy"),
                    "BROKEN_MULTILINE_OR_COMMENT")
        # Positive genuine Unity style multi-reference YAML payload.
        multi={**combined,**before}
        scene="Assets/SOMEONE_ELSE/scenes/Room.unity"
        multi[scene]=good+good
        valid=validate_script_refs(multi,meta_guids(multi))
        check(len(valid[scene])==2,"MULTIPLE_VALID_SELECTED_SCRIPT_REFS_FAILED")
        # A script from #604 CheckpointFence is never importable. By separate
        # source checkout it is a real original Git GUID, not made-up.
        losing_guid_path="Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs.meta"
        loss=git(roots["loser"],"show","HEAD:"+losing_guid_path)
        lose_g=META_GUID_RE.search(loss.decode()).group(1).lower()
        check(lose_g not in allmeta,"ALT_604_GUID_COLLIDES_WITH_SELECTED_596")
        reject_case(good.replace(approved_guid.encode(),lose_g.encode()),
                    "REAL_ALT_604_CHECKPOINT_FENCE_GUID")
        # Actual legacy source trees from original Git can supply a genuine
        # W07 GUID even when the old source is excluded from the new host.
        oldmeta=git(roots["game"],"ls-tree","-r","--name-only","HEAD",
                    "Assets/B0123DebrisPlayground001").decode().splitlines()
        legacy=[p for p in oldmeta if p.endswith(".cs.meta")]
        legacy_provenance="NOT_PRESENT_IN_SELECTED_GAME_SHA"
        if legacy:
            lg=META_GUID_RE.search(git(roots["game"],"show","HEAD:"+legacy[0]).decode())
            check(lg is not None,"INVALID_HISTORICAL_W07_META")
            legacy_guid=lg.group(1).lower()
            check(legacy_guid not in allmeta,"LEGACY_W07_INSTALLED_WITH_SELECTED")
            reject_case(good.replace(approved_guid.encode(),legacy_guid.encode()),
                        "ORIGINAL_W07_GUID_ONLY")
            legacy_provenance=legacy[0]
        check(old.crawl(host)[0]==source_host,"W18_WITNESS_CHANGED_HOST")
    return {"new_negative_rejections":len(checks)-1,
            "negative_checks":checks,
            "positive_script_ref_checks":3,
            "W15_name_scan_guid_only_bypass":"ACTUALLY_WITNESSED",
            "original_losing_604_script_meta":losing_guid_path,
            "old_W07_guid_source":legacy_provenance,
            "selected_legitimate_unity_host_meta":marker,
            "source_guid_count":len(allmeta)}

def main():
    p=argparse.ArgumentParser()
    for k in ("game","save","bridge","receiver","w17","loser","qa","output"):
        p.add_argument("--"+k,required=True,type=Path)
    args=p.parse_args()
    roots={k:getattr(args,k).resolve() for k in
           ("game","save","bridge","receiver","w17","loser","qa")}
    for k,ref in (("game",GAME),("save",SAVE),("bridge",BRIDGE),
                  ("receiver",RECEIVER),("w17",SOURCE_W17),("loser",LOSER),
                  ("qa",QA)):
        check(git(roots[k],"rev-parse","HEAD").decode().strip()==ref,
              "PINNED_SOURCE_SHA_MISMATCH:"+k)
    owner=module(roots["w17"]/ "Tools/B01/B01-40/P17W17Selected596G0/intake.py",
                 "independent_original_w17_G0")
    safe=module(roots["receiver"]/
       "Tools/B01/B01-40/P17W15AtomicReceiverMigration/migration.py",
       "independent_original_w15_safe_receiver")
    # No author/source modifications: execute all ORIGINAL W17 physical source
    # intake+W15 file-stage validation FIRST, including original author Git ZIP.
    owner.source_pr_pins(roots)
    selected,original=owner.original_bridge_bytes(roots,safe)
    owner.selected_596_release_source_review(selected)
    prior=owner.actual_tests(roots,safe,selected,original)
    base,_=safe.source_files(roots["game"],roots["save"])
    check(len(base)==37 and len(selected)==16,"WRONG_ONLY_596_SOURCE_COUNTS")
    combined={**base,**selected}
    check(len(combined)==53 and len(safe.validate_guids(combined))==33,
          "FROZEN_510_556_596_META_GUID_COLLISION")
    check(prior["negative_count"]==18 and len(prior["positive_runs"])==2,
          "PREVIOUS_SOURCE_G0_NOT_EXECUTED_AS_REQUIRED")
    extra=witness_one(roots,owner,safe,combined)
    result={
       "package":"B01-40-W18-SERIALIZED-GUID-SOURCE-RECEIVER-FENCE",
       "source_exact":BRIDGE,"alternative":LOSER,
       "original_author_source_zip":original,
       "combined_original_unity_files":53,"unique_unity_meta_guids":33,
       "original_w17_safe_migration":{"negative_checks":prior["negative_count"],
         "physical_stage_rollback_cycles":len(prior["positive_runs"]),
         "native_unity":"NOT_RUN"},
       "yaml_script_guid_gate":extra,
       "source_g0":"ACCEPTED_SOURCE_ONLY_ORIGINAL_596_PHYSICAL",
       "host_security":"HOLD_PENDING_INDEPENDENT_SELECTED_PUBLIC_HOST_QA",
       "ready_for_integrator":False,"actual_codex_installation":False,
       "native_unity":"NOT_RUN","canonical_live_save":"HOLD",
       "g2":"HOLD","ack":False,"merge":False,
       "status":{"IMPLEMENTED":"YES",
                 "TESTED":"YES_SOURCE_PHYSICAL_NOT_NATIVE",
                 "QA_ACCEPTED":"SOURCE_G0_ONLY",
                 "READY_FOR_INTEGRATOR":"BLOCKED",
                 "INTEGRATED":"NO","RELEASED":"NO"}}
    args.output.mkdir(parents=True,exist_ok=True)
    (args.output/"w18-one596-source-receipt.json").write_text(
         json.dumps(result,sort_keys=True,indent=2)+"\n")
    print("B0140_W18_AUTHOR_SELECTED_596_ORIGINAL_PHYSICAL",
          json.dumps(original,sort_keys=True))
    print("B0140_W18_SERIALIZED_GUID_FENCE",
          json.dumps(extra,sort_keys=True))
    print("B0140_W18_OLD_RECEIVER_BYPASS_PROVEN_NEW_GUID_FENCE_REJECTS")
    print("B0140_W18_PHYSICAL_G0_ACCEPTED_SOURCE_ONLY_NATIVE_G1_NOT_RUN_PUBLIC_HOST_HOLD_G2_HOLD_NO_MERGE")
if __name__=="__main__":
    try:main()
    except (Reject,ValueError,AssertionError,OSError,subprocess.SubprocessError) as err:
        print("B0140_W18_SOURCE_FENCE_FAIL",str(err));raise SystemExit(1)
