#!/usr/bin/env python3
"""Exact-Git-source composite SAFE receiver; never writes to a Unity project.

Reads source Git blobs with git show, audits all .meta GUIDs and asmdefs,
rebuilds a deterministic namespaced SOURCE zip only in newly owned scratch.
A valid packet NEVER constitutes a playable-game G0/G1 or Codex ACK.
"""
import argparse, hashlib, io, json, os, pathlib, re, shutil, stat, subprocess, sys, tempfile, zipfile
from collections import Counter

HERE=pathlib.Path(__file__).resolve().parent
PLAN=json.loads((HERE/"source_plan.json").read_text(encoding="utf-8"))
BASE=pathlib.Path.cwd()
GUID_RE=re.compile(rb"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$")
SELECTED={"whole_game_797", "physical_rig_792", "econ_v7_780",
          "r5_fixture_750", "selected_strict_path_787"}
REQUIRED=SELECTED|{"main","physical_parent_772","r5_parent_690",
          "canonical_api_729","frozen_save_556","frozen_game_510","w06_debris_421"}

def git(*args):
    r=subprocess.run(["git",*args],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if r.returncode: raise RuntimeError("GIT_FAIL "+repr(args)+" "+r.stderr[:250].decode("utf-8","replace"))
    return r.stdout

def ensure_git():
    assert set(PLAN["producers"])==REQUIRED, "CANNOT CHANGE ORIGINAL SOURCE AUTHORITY SET"
    for role,sha in PLAN["producers"].items():
        assert re.fullmatch(r"[0-9a-f]{40}",sha)
        assert git("rev-parse","--verify",sha+"^{commit}").strip().decode()==sha,"MISSING_PINNED_SHA:"+role
    git("merge-base","--is-ancestor",PLAN["source_base_sha"],"HEAD")

def read_source(role,path):
    assert role in PLAN["producers"] and path and "\\" not in path
    assert not path.startswith("/") and not any(p in (".","..") for p in path.split("/"))
    sha=PLAN["producers"][role]
    return git("show",sha+":"+path)

def list_source(role,prefix):
    sha=PLAN["producers"][role]
    names=git("ls-tree","-r","--name-only",sha,"--",prefix).decode().splitlines()
    return sorted(x for x in names if x.startswith(prefix) and not x.endswith("/"))

def hash_git(b):
    return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()

def select():
    """An explicit only-once source provenance map with intentional R5 fixture overlay."""
    entries={}
    replaced=[]
    for scope in PLAN["asset_sources"]:
        role,prefix=scope["ref"],scope["prefix"]
        paths=list_source(role,prefix)
        if not paths:raise AssertionError("SOURCE_PREFIX_EMPTY:"+role+":"+prefix)
        for path in paths:
            if path in entries:raise AssertionError("UNAUTHORIZED_SOURCE_PATH_COLLISION:"+path)
            entries[path]=(role,read_source(role,path))
    for spec in PLAN["explicit_files"]:
        role,path=spec["ref"],spec["path"]
        if path in entries:
            if entries[path][0]!=spec.get("replaces_from"):
                raise AssertionError("UNAUTHORIZED_SELECTED_PATH_OVERWRITE:"+path)
            replaced.append({"path":path,"previous_source_role":entries[path][0],
                             "chosen_source_role":role})
        entries[path]=(role,read_source(role,path))
    assert len(replaced)==1 and replaced[0]["chosen_source_role"]=="r5_fixture_750"
    audits=[]
    for spec in PLAN["audit_only"]:
        path=spec["path"];role=spec["ref"];b=read_source(role,path)
        assert path not in entries, "DUAL_SAVE_WRITER_NOT_ALLOWED"
        audits.append({"role":role,"path":path,"git_blob":hash_git(b),
                       "sha256":hashlib.sha256(b).hexdigest(),"bytes":len(b),
                       "selected_runtime":False})
    assert sum(1 for p in entries if p.endswith(".cs") and "P17W25WholeGameEconomyV7/" in p)==2
    assert not any("P17W25StrictCanonicalPathR3/" in p for p in entries)
    return entries,audits,replaced

def inspect(entries):
    # GUID uniqueness must span the full physical composite, not per producer.
    guids={}
    asmdefs={}
    yaml_scripts=[]
    for path,(role,b) in entries.items():
        if path.endswith(".meta"):
            matches=GUID_RE.findall(b)
            if len(matches)!=1:raise AssertionError("MISSING_OR_AMBIGUOUS_UNITY_GUID:"+path)
            value=matches[0].decode().lower()
            if value in guids:raise AssertionError("DUPLICATE_UNITY_GUID:"+value+" "+path+" "+guids[value])
            guids[value]=path
        if path.endswith(".asmdef"):
            cfg=json.loads(b)
            name=cfg.get("name")
            if not isinstance(name,str) or not name:raise AssertionError("ASMDEF_MISSING_NAME:"+path)
            if name in asmdefs:raise AssertionError("DUPLICATE_ASMDEF_NAME:"+name)
            asmdefs[name]={"path":path,"references":cfg.get("references",[])}
        if path.endswith((".unity",".prefab",".asset")):
            yaml_scripts.extend((path,x.decode()) for x in re.findall(rb"m_Script:.*?guid: ([a-f0-9]{32})",b))
        if path.startswith("Assets/") and not path.endswith(".meta") and not path.endswith(".md"):
            if path+".meta" not in entries:
                raise AssertionError("UNPAIRED_UNITY_ASSET_META:"+path)
    missing_refs=sorted({(name,ref) for name,data in asmdefs.items()
                         for ref in data["references"] if isinstance(ref,str)
                         and not ref.startswith("GUID:") and ref not in asmdefs})
    missing_scripts=sorted((path,g) for path,g in yaml_scripts if g not in guids)
    # Distinguish known Unity-internal/global package refs from own GUIDs.
    if missing_scripts:raise AssertionError("MISSING_LOCAL_SERIALIZED_SCRIPT_GUID:"+str(missing_scripts[:4]))
    return {"unique_guid_count":len(guids),"guid_map":guids,
            "asmdefs":asmdefs,"unresolved_asmdef_names":missing_refs,
            "yaml_m_script_refs_checked":len(yaml_scripts)}

def scan_dir_for_foreign(path):
    """Never accept an existing directory, even if EMPTY. No recursive wipe."""
    p=pathlib.Path(path)
    assert p.is_absolute() and p.name.startswith("ci41-w27-owned-"),"DISPOSABLE_UNIQUE_OUTPUT_REQUIRED"
    assert not p.exists() and not p.is_symlink(),"EXISTING_OR_FOREIGN_DESTINATION_REFUSED"
    parent=p.parent
    assert parent.is_dir() and not parent.is_symlink(),"FOREIGN_OR_SYMLINK_PARENT_REFUSED"
    return p

def deterministic_zip(files):
    output=io.BytesIO()
    with zipfile.ZipFile(output,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9,allowZip64=True) as z:
        for name,b in sorted(files.items()):
            assert not name.startswith("/") and not any(x==".." for x in name.split("/"))
            i=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
            i.compress_type=zipfile.ZIP_DEFLATED
            i.create_system=3
            i.external_attr=(0o100644<<16)
            z.writestr(i,b,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    return output.getvalue()

def package(out):
    ensure_git()
    entries,audits,replaced=select()
    audit=inspect(entries)
    selected_files=[{"path":path,"producer":role,"source_sha":PLAN["producers"][role],
                     "git_blob":hash_git(b),"sha256":hashlib.sha256(b).hexdigest(),"bytes":len(b)}
                    for path,(role,b) in sorted(entries.items())]
    assert any(x["path"].startswith("Assets/B0123CanonicalRigPortW24/") for x in selected_files)
    assert any(x["path"].startswith("Assets/B01Packages/ECONAPPROVED007/") for x in selected_files)
    assert all(x["path"].startswith(("Assets/","Tools/")) for x in selected_files)
    assert any(x["path"].endswith("CanonicalWholeGameSessionV2.cs") for x in selected_files)
    assert any(x["path"].endswith("WholeGameEconomySlotV2.cs") for x in selected_files)
    # Only source files listed, no hidden branch-history coinstall.
    receipt={
     "package_id":PLAN["package_id"],"revision":PLAN["revision"],"origin":"EXACT_GIT_ORIGINAL_BLOBS",
     "tested_base_sha":PLAN["source_base_sha"],"source_heads":PLAN["producers"],
     "selected_runtime":"WHOLE_GAME_797_ONLY","raw_r3_787":"QA_AUDIT_ONLY_NOT_INSTALLED",
     "parent_772":"INHERITED_ASSETS_VIA_792","r5_fixture_750":"EXACT_ONE_TEST_OVERLAY",
     "selected_files":selected_files,"replaced_original_fixture":replaced,
     "noninstalled_R3_audit_blobs":audits,
     "source_closure":{"files":len(selected_files),"unique_meta_guids":audit["unique_guid_count"],
                        "asmdefs":audit["asmdefs"],"unresolved_asmdef_names":audit["unresolved_asmdef_names"],
                        "unity_yaml_m_script_refs":audit["yaml_m_script_refs_checked"]},
     "risk":{"level":"HIGH","why":"Save/world+economy can lose progress; no native Unity binder"},
     "gates":{"G0_PROVENANCE":"SOURCE_TREE_AUTHENTICATED",
              "G0_UNITY_ABI":"HOLD_MISSING_GAME_BINDER_AND_NATIVE",
              "G1_NATIVE_UNITY":"NOT_RUN","G2_CODEX_INTEGRATION":"HOLD",
              "CODEX_ACK":"NONE","INTEGRATED":False},
     "only_safe_path":"REVIEW_OR_DISPOSABLE_SOURCE_STAGING_NO_LIVE_GAME_INSTALL",
     "rollback":"REMOVE_ONLY_FRESH_UUID_OWNED_STAGING; never rollback foreign or active Codex files"}
    out=scan_dir_for_foreign(out)
    out.mkdir(mode=0o700)
    try:
        (out/"MANIFEST.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n",encoding="utf-8")
        support={
         "README.md":("CI41 W27 exact original Git source transport. NO Unity install, no merge.\n"
                      "One whole-game #797 writer; #787 audit-only; physical #792 extends #772; #750 corrects one R5 fixture.\n"),
         "INSTALL_FOR_CODEX.md":(
                      "REVIEW ONLY. Codex owns active W26 worktree; DO NOT EXTRACT INTO IT.\n"
                      "1) Compare this immutable receipt with exact PR heads.\n"
                      "2) B01-40/60 independently review GUID/asmdefs, selected Unity source paths, package SHA, active Codex branch reserved paths.\n"
                      "3) B01-00 authorizes ONE new clean disposable Unity sandbox only after G0 signoff; never copy both #787 R3 and #797 V2 writers.\n"
                      "4) #797 Tool C# sources are not Unity .asmdef/runtime assets: missing game-facing binder + asmdefs must be authored by B01-23/62/Codex without shared worktree edits.\n"
                      "5) Old Save migration requires verified user-controlled backup and real cold restore; NO auto-migration, NO changes to persistentDataPath.\n"
                      "6) Real native Unity 6000.3.14f1 Editor.log/NUnit3/XML+SHA required for G1. Await Codex explicit ACK; no release.\n"
                      "Rollback: do not touch user workspace; discard only freshly created disposable scratch owned by this receipt.\n"),
         "TEST_PLAN.md":("G0 original Git SHA/blob/bytes, unique Unity .meta GUID, .asmdef references, collisions, case path and scratch rollback.\n"
                         "G1 pending: actual Rigidbody old world velocity rollback, stage quarantine, Run/Light/Heavy consumed IDs, whole game+V7 wallet and loaner cold restore, same-epoch fork, lost ACK and native R5 ColdRestart.\n"),
         "KNOWN_LIMITATIONS.md":("Source closure is NOT fully compiled as Unity assemblies. #797 source only in Tools; missing real runtime assembly binder. #792 authored Unity tests NOT_RUN; #750 native PlayMode NOT_RUN; no licensed Unity/G1.\n"
                                 "Selected #787 R3 store is excluded from runtime to prevent dual Save writer; alternative #749/#773/#793/#764 NOT_INCLUDED.\n"
                                 "No claims about current Codex active worktree or unpushed files.\n"),
         "CHANGELOG.md":"R1 W27 new independent exact-source closure receipt only; no gameplay source modifications.\n",
         "QA_B01_60.md":"Independent native QA: NOT_RUN; no Unity Editor/NUnit3 log. B01-60 signoff: PENDING.\n",
         "TEST_EVIDENCE/G0_SOURCE_RECEIPT.json":json.dumps(receipt,sort_keys=True,indent=2)+"\n"}
        zfiles={"SOURCE/"+path:b for path,(_,b) in entries.items()}
        zfiles.update({path:b.encode() for path,b in support.items()})
        zfiles["MANIFEST.json"]=(json.dumps(receipt,sort_keys=True,indent=2)+"\n").encode()
        blob=deterministic_zip(zfiles)
        (out/"CI41_W27_EXACT_SELECTED_SOURCE_CLOSURE.zip").write_bytes(blob)
        result={"package_id":PLAN["package_id"],"sha256_zip":hashlib.sha256(blob).hexdigest(),
                "zip_bytes":len(blob),"zip_members":len(zfiles),"authored_files":len(selected_files),
                "meta_guids":audit["unique_guid_count"],"asmdefs":len(audit["asmdefs"]),
                "unresolved_asmdef_names":audit["unresolved_asmdef_names"],
                "G0_SOURCE_PROVENANCE":"PASS","G0_INSTALL":"HOLD",
                "G1_NATIVE":"NOT_RUN","CODEX_ACK":"NONE"}
        (out/"QA_RECEIPT.json").write_text(json.dumps(result,sort_keys=True,indent=2)+"\n")
        print("CI41_W27_EXACT_ORIGINAL_GIT_BLOBS_AND_GUIDS_PASS files="+str(len(selected_files))+
              " unique_guids="+str(audit["unique_guid_count"])+" zip_sha256="+result["sha256_zip"])
        print("CI41_W27_ONE_WHOLE_GAME_797_ONLY_R3_787_AUDIT_NOT_INSTALLED")
        print("CI41_W27_SAFE_FOREIGN_SCRATCH_AND_CODEX_WORKTREE_UNTOUCHED")
        print("CI41_W27_G0_SOURCE_PROVENANCE_PASS_G0_GAME_BINDER_HOLD_G1_NATIVE_NOT_RUN")
        return result
    except Exception:
        # Never delete foreign paths; this exact output was newly created here.
        shutil.rmtree(out)
        raise

def main():
    a=argparse.ArgumentParser()
    a.add_argument("--out",required=True)
    v=a.parse_args()
    package(pathlib.Path(v.out).resolve())
if __name__=="__main__":main()
