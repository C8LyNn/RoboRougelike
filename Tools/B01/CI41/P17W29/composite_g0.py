#!/usr/bin/env python3
"""B01-41 W29: physically verify and stage EXACT original #826 ONLY whole-game
Save + #792 / #780 / #750 into isolated QA scratch, never a Codex checkout.

The stage includes the REAL original #826 ABI netstandard2.1 compiler project
and original 9-scenario cold source runner, not a fake UnityEngine or a new writer.
"""
import argparse, hashlib, io, json, pathlib, re, shutil, subprocess, sys, zipfile
ROOT=pathlib.Path(__file__).resolve().parent
PLAN=json.loads((ROOT/"source_plan.json").read_text(encoding="utf-8"))
GUID=re.compile(rb"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$")
P=PLAN["producer_sha"]
PACKAGE="CI41-W29-826-FULL-GAME-COMPOSITE-ORIGINAL-G0-001/R1"
SELECTED={"game_824","save826","old_save797","r3_787","physical792","econ780",
          "cold750","rig772","host690","api729","frozen556","frozen510","w06_421"}
GOOD_826=["SHA256.Create()","ComputeHash(","File.Replace(staging,authoritative,null)",
          "File.Move(staging,authoritative)","file.Flush(true)","FileOptions.WriteThrough",
          "B0162G02","slot.canonical-game-v2","CanonicalEconomySectionV7.TryEncode",
          "CanonicalEconomySectionV7.TryDecode"]
BAD_826=["SHA256.HashData(","Convert.ToHexString(",
         "File.Move(staging,authoritative,true)","File.Delete(authoritative)"]

def call(*args):
    r=subprocess.run(("git",)+args,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if r.returncode:
        raise AssertionError("ORIGINAL_GIT_FAIL "+repr(args)+" "+r.stderr.decode("utf-8","replace")[-250:])
    return r.stdout

def blob(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()

def read(ref,path):
    assert ref in P and path and not path.startswith("/") and "\\" not in path
    assert all(s not in ("",".","..") for s in path.split("/"))
    return call("show",P[ref]+":"+path)

def files(ref,prefix):
    assert prefix.startswith("Assets/") and prefix.endswith("/")
    base=prefix[:-1]
    z=call("ls-tree","-r","--name-only",P[ref],"--",prefix,base+".meta").decode().splitlines()
    return sorted(x for x in z if x==base+".meta" or x.startswith(prefix))

def pin():
    assert set(P)==SELECTED,"INCORRECT_IMMUTABLE_PRODUCER_SET"
    assert P["game_824"]==PLAN["gameplay_base"]
    for name,sha in P.items():
        assert re.fullmatch("[0-9a-f]{40}",sha), "INVALID_SHA:"+name
        assert call("rev-parse","--verify",sha+"^{commit}").decode().strip()==sha, "SHA_NOT_IN_GIT:"+name
    call("merge-base","--is-ancestor",PLAN["gameplay_base"],"HEAD")

def select():
    selected={}
    for scope in PLAN["scopes"]:
        ref,prefix=scope["ref"],scope["prefix"]
        names=files(ref,prefix)
        assert names and prefix[:-1]+".meta" in names, "OMITTED_ROOT_FOLDER_META:"+prefix
        for path in names:
            assert path not in selected,"UNDECLARED_SOURCE_SHADOW:"+path
            selected[path]=(ref,read(ref,path))
    replaced=[]
    for spec in PLAN["selected_files"]:
        path,ref=spec["path"],spec["ref"]
        if path in selected:
            assert selected[path][0]==spec.get("replaces_from"),"UNAUTHORIZED_SOURCE_OVERWRITE:"+path
            replaced.append((path,selected[path][0],ref))
        selected[path]=(ref,read(ref,path))
    assert len(replaced)==1 and replaced[0][2]=="cold750","ONE_R5_COLD_FIXTURE_REQUIRED"
    assert len(selected)==PLAN["expected_selected_sources"],"INCOMPLETE_ACTUAL_SOURCE_TREE"
    assert len([x for x in selected.values() if x[0]=="save826"])==2,"NOT_EXACTLY_ONE_V3_WRITER"
    assert not any("P17W25WholeGameEconomyV7/" in x or
        "P17W25StrictCanonicalPathR3/" in x for x in selected),"COINSTALLED_OLD_WRITER"
    aud=[]
    for spec in PLAN["audit_only"]:
        path,ref=spec["path"],spec["ref"]
        assert path not in selected and ref in ("r3_787","old_save797"),"COINSTALLED_OLD_WRITER"
        x=read(ref,path)
        aud.append({"path":path,"role":ref,"source_git_blob":blob(x),
                    "sha256":hashlib.sha256(x).hexdigest(),"selected":False})
    assert len(aud)==4,"TWO_OLD_WRITERS_MUST_BE_EXCLUDED_AND_AUDITED"
    old= b"\n".join(selected[path][1] for path in selected if selected[path][0]=="save826")
    for word in GOOD_826:
        assert word.encode() in old,"NEW_826_DURABLE_ABI_REGRESSION:"+word
    for word in BAD_826:
        assert word.encode() not in old,"OLD_797_ABI_OR_DATA_LOSS_REGRESSION:"+word
    return selected,replaced,aud

def inspect_unity(selected):
    guids={}; asm={}; mscript=[]
    for path,(ref,data) in selected.items():
        if path.endswith(".meta"):
            m=GUID.findall(data)
            assert len(m)==1,"MALFORMED_OR_MISSING_META_GUID:"+path
            g=m[0].decode().lower()
            assert g not in guids,"DUPLICATE_GUID_INSIDE_PACKAGE:"+g
            guids[g]=path
        if path.endswith(".asmdef"):
            obj=json.loads(data)
            name=obj.get("name")
            assert isinstance(name,str) and name,"INVALID_ASMDEF:"+path
            assert name not in asm,"DUPLICATE_ASSEMBLY_NAME:"+name
            asm[name]={"path":path,"references":obj.get("references",[])}
        if path.startswith("Assets/") and not path.endswith((".meta",".md")):
            assert path+".meta" in selected,"MISSING_REAL_UNITY_ASSET_META:"+path
        if path.endswith((".prefab",".asset",".unity")):
            mscript.extend((path,g.decode()) for g in
                re.findall(rb"m_Script:.*?guid: ([a-f0-9]{32})",data))
    assert len(guids)==PLAN["expected_selected_unity_guid"],"MISSING_UNITY_GUIDS_NOT_53"
    assert not any(g not in guids for _,g in mscript),"BROKEN_INTERNAL_UNITY_MONO_REFERENCE"
    return guids,asm,mscript

def game_compare(selected,guids):
    # Exact 824 *current candidate* game paths; do not inspect local/other worktrees.
    game=P["game_824"]
    original=set(call("ls-tree","-r","--name-only",game).decode().splitlines())
    game_assets=sorted(x for x in original if x.startswith("Assets/") and x.endswith(".meta"))
    byfold={}
    for path in original:
        if path.startswith(("Assets/","Tools/")):
            byfold.setdefault(path.casefold(),[]).append(path)
    equal=[];mismatch=[];lower=[]
    for path,(_,data) in selected.items():
        if path in original:
            if read("game_824",path)==data:equal.append(path)
            else:mismatch.append(path)
        else:
            lower.extend([{"incoming":path,"existing":other}
                          for other in byfold.get(path.casefold(),[])])
    game_guids={}
    for path in game_assets:
        m=GUID.findall(read("game_824",path))
        if len(m)==1:game_guids.setdefault(m[0].decode().lower(),[]).append(path)
    foreign=[{"guid":g,"incoming":path,"existing":old}
             for g,path in guids.items() for old in game_guids.get(g,[])
             if old!=path]
    return {
      "exact_codex_game_sha":game,"current_game_meta_examined":len(game_assets),
      "selected_paths_already_byte_identical":len(equal),
      "selected_paths_absent_from_current_game":len(selected)-len(equal)-len(mismatch),
      "different_existing_paths":mismatch,
      "foreign_unity_guid_conflicts":foreign,
      "case_insensitive_windows_path_conflicts":lower,
      "safe_additive_without_current_game_overwrite":not (mismatch or foreign or lower)
    }

def unique_destination(path):
    p=pathlib.Path(path)
    assert p.is_absolute() and p.name.startswith("ci41-w29-owned-"),"FOREIGN_SCRATCH_NAME"
    assert not p.exists() and not p.is_symlink(),"FOREIGN_EVEN_EMPTY_DESTINATION"
    assert p.parent.is_dir() and not p.parent.is_symlink(),"SYMLINK_OR_ABSENT_PARENT"
    return p

def make_zip(d):
    output=io.BytesIO()
    with zipfile.ZipFile(output,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for p,b in sorted(d.items()):
            assert not p.startswith("/") and ".." not in p.split("/")
            zi=zipfile.ZipInfo(p,date_time=(1980,1,1,0,0,0))
            zi.compress_type=zipfile.ZIP_DEFLATED
            zi.create_system=3
            zi.external_attr=0o100644<<16
            z.writestr(zi,b,compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    return output.getvalue()

def abi_stage(out,selected):
    root=out/"ci41-w29-owned-original-abi-project"
    for path,(_,data) in selected.items():
        dest=root/path
        dest.parent.mkdir(parents=True,exist_ok=True)
        dest.write_bytes(data)
    for path in PLAN["abi_project"]:
        dest=root/path;dest.parent.mkdir(parents=True,exist_ok=True)
        dest.write_bytes(read("save826",path))
    prefix="Tools/B01/B01-62/P17W28UnityCompatibleWholeGameV3/.source/"
    for name in ("ApprovedEconomyPolicyV7.cs","CanonicalEconomySectionV7.cs"):
        raw=read("econ780","Assets/B01Packages/ECONAPPROVED007/Runtime/"+name)
        dest=root/(prefix+name);dest.parent.mkdir(parents=True,exist_ok=True)
        dest.write_bytes(raw)
    return root

def package(out):
    pin()
    selected,replaced,aud=select()
    guid,asm,mscript=inspect_unity(selected)
    game=game_compare(selected,guid)
    original=[{"path":path,"producer_role":role,"source_sha":P[role],
       "git_blob":blob(data),"sha256":hashlib.sha256(data).hexdigest(),"bytes":len(data)}
       for path,(role,data) in sorted(selected.items())]
    manifest={
      "package":PACKAGE,"source_provenance":"REAL_GIT_SHOW_ORIGINAL_BLOBS",
      "pins":P,"one_selected_writer":"#826_V3_ONLY",
      "excluded_historic_writer_audit":aud,
      "selected_source_files":original,
      "selected_source_file_count":len(original),
      "unique_unity_guid_count":len(guid),"unity_guid_path_map":guid,
      "asmdef_map":asm,"serialized_unity_script_refs":len(mscript),
      "one_replaced_native_test_fixture":replaced,
      "comparison_with_current_codex_game":game,
      "native_gates":{"SOURCE_BYTES":"PASS","G0_GAME_SAFETY":"PASS_SOURCE_ONLY"
          if game["safe_additive_without_current_game_overwrite"] else "RED_CONFLICT",
          "LIVE_SAVE_RIG_ECON_BINDER":"NOT_IMPLEMENTED","G1_UNITY":"NOT_RUN",
          "G2":"HOLD","CODEX_ACK":"NONE","INTEGRATED":False,"RELEASED":False},
      "migration":"NO_AUTOMATIC_MIGRATION_OR_PLAYER_FILE_TOUCH",
      "risk":"HIGH_NOT_PRODUCT_INSTALL_APPROVAL"
    }
    out=unique_destination(out)
    out.mkdir(mode=0o700)
    try:
        (out/"MANIFEST.json").write_text(json.dumps(manifest,sort_keys=True,indent=2)+"\n")
        guide={
          "README.md":"B01-41 W29 exact original #826 single writer + #792/#780/#750; review only.\n",
          "INSTALL_REVIEW.md":("NEVER extract to active Codex worktree or player Save.\n"
            "Only one writer #826, #797+raw #787 AUDIT ONLY.\n"
            "G0 source verifies Git bytes/Unity .meta/asmdef/case/conflicts, not a live game binder.\n"
            "B01-23/20/62 must wire actual #792 rig/ledger, authoritative victory and V7 wallet.\n"
            "B01-60 must execute real Unity 6000.3.14f1 Editor/NUnit/Player.\n"),
          "REQUIRED_NATIVE_G1.md":(
            "G1 NOT_RUN. Need native Unity: Load cold physical Rig[] and velocities/part IDs; "
            "same-epoch wallet fork, lost ACK, corrupt file, old Save backup-first, "
            "run victory/loaner IDs, current R3 combat/garage and Windows Player.log + ZIP SHA.\n"),
          "SOURCE_RECEIPT.json":json.dumps(manifest,sort_keys=True,indent=2)+"\n"
        }
        content={"SOURCE/"+path:data for path,(_,data) in selected.items()}
        content.update({path:value.encode() for path,value in guide.items()})
        content["MANIFEST.json"]=(json.dumps(manifest,sort_keys=True,indent=2)+"\n").encode()
        archive=make_zip(content)
        (out/"CI41_W29_SAVE826_SINGLE_WRITER_ORIGINAL_SOURCE.zip").write_bytes(archive)
        project=abi_stage(out,selected)
        result={"package":PACKAGE,"zip_sha256":hashlib.sha256(archive).hexdigest(),
          "zip_bytes":len(archive),"members":len(content),"sources":len(original),
          "guids":len(guid),"old_audit_only":len(aud),
          "game_additive":game["safe_additive_without_current_game_overwrite"],
          "game_sha":P["game_824"],"save_sha":P["save826"],
          "abi_project_root":str(project.relative_to(out)),
          "G1_UNITY":"NOT_RUN","CODEX_ACK":"NONE"}
        (out/"QA_RECEIPT.json").write_text(json.dumps(result,sort_keys=True,indent=2)+"\n")
        print("CI41_W29_REAL_ORIGINAL_SAVE826_COMPOSITE_SOURCE_PASS files="+str(len(original))+
              " guid="+str(len(guid))+" old_audit_only="+str(len(aud)))
        print("CI41_W29_CURRENT_CODEX_GAME_COLLISIONS "+json.dumps(game,sort_keys=True))
        print("CI41_W29_REAL_SOURCE_ZIP_SHA256 "+result["zip_sha256"])
        print("CI41_W29_ORIGINAL_V3_NETSTANDARD21_ABI_AND_NINE_COLD_TESTS_STAGED")
        print("CI41_W29_UNITY_G1_NOT_RUN_BINDER_MISSING_NO_CODEX_ACK")
        if not game["safe_additive_without_current_game_overwrite"]:
            raise AssertionError("GAME_SOURCE_G0_RED_NO_UNAUTHORIZED_OVERWRITE")
        return result
    except Exception:
        shutil.rmtree(out)
        raise

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--out",required=True)
    args=p.parse_args()
    package(pathlib.Path(args.out).resolve())
