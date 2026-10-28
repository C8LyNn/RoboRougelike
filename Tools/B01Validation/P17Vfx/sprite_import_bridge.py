#!/usr/bin/env python3
"""B01-40 P17: physically rasterize immutable B01-32 art into Unity Sprite Single import ZIP.

No file in canonical Assets is written. The PNG textures and Unity 6-style .meta
are generated inside a disposable worktree/external artifact; only Codex can
decide to install. Sprite metadata are not a substitute for native Unity G1.
"""
import argparse
from hashlib import sha1, sha256
import io
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import zipfile

OWNER="829ac8da9db83a49865d24fa05a5143f59d4b83f"
BASE="5f06af81e2dd9d877e4300259bb3dd008cb9c9d1"
CODEX="be2c4b6ac63e04eff5b046e50641ec8d1da4837f"
OWNER_DIR="Research/B01-32/P17/PartDamageDecals"
RENDER="Tools/B01-32/P17/render_art.py"
TEMPLATE="Assets/TutorialInfo/Icons/URP.png.meta"
TEMPLATE_OID="0f2cab05ff467a6889a91c7fa17c485696f52ea0"
MANIFEST_OID="7f3e2cdbe06300cf4ee641659b6f9b8132a51ad5"
RENDER_OID="4a2b785fc2a460deea495aa38443d1cdd079cd97"
PACKAGE="B01-40-P17-DECAL-SPRITE-BRIDGE-001"
ROOT="Assets/B01ImportStaging/B0140DamageDecals"
FRAMES=ROOT+"/Frames"
STATES=("clean","abraded","fractured","critical")
ARCHIVE="B0140-PartDamageDecals-UnitySpriteSingles.zip"
SHA64=re.compile(r"^[a-f0-9]{64}$")
GUID_RE=re.compile(r"(?m)^guid: ([0-9a-f]{32})\r?$")

class Reject(ValueError):pass
def require(ok,why):
    if not ok:raise Reject(why)
def cmd(args,cwd=None,timeout=200,nonzero=False):
    p=subprocess.run(args,cwd=cwd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,
                     text=True,encoding="utf-8",errors="replace",timeout=timeout)
    if p.returncode and not nonzero:raise Reject("SUBPROCESS_FAILED "+repr(args)+": "+(p.stdout+p.stderr)[-800:])
    return p
def git(repo,*args):
    return cmd(["git","-C",str(repo),*args]).stdout.strip()
def original_blob(repo,commit,path,expected=None):
    oid=git(repo,"rev-parse",commit+":"+path)
    if expected:require(oid==expected,"ORIGINAL_BLOB_DRIFT:"+path)
    data=subprocess.check_output(["git","-C",str(repo),"show",commit+":"+path])
    require(sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()==oid,"GIT_BYTES_TAMPER:"+path)
    return data
def stage_guid(name):
    # Independent of operating system, machine, Unity installation or time.
    return sha256((PACKAGE+":v1:"+name).encode()).hexdigest()[:32]
def frame_names():
    return [f"{s}-{i}" for s in STATES for i in range(3)]
def canon(data):
    return (json.dumps(data,sort_keys=True,ensure_ascii=False,separators=(",",":"))+"\n").encode()
def artifact_sha(data):return sha256(data).hexdigest()

def pinned_original(repo):
    original_blob(repo,OWNER,OWNER_DIR+"/manifest.json",MANIFEST_OID)
    original_blob(repo,OWNER,RENDER,RENDER_OID)
    template=original_blob(repo,BASE,TEMPLATE,TEMPLATE_OID)
    manifest=json.loads(original_blob(repo,OWNER,OWNER_DIR+"/manifest.json"))
    require(manifest["package_id"]=="B01-32-DAMAGE-DECAL-001" and
            manifest["stage_order"]==list(STATES) and manifest["frame_order"]==[0,1,2] and
            manifest["frame_duration_ms"]==[160,160,160] and
            manifest["atlas"]["output_px"]==[768,1024] and
            manifest["atlas"]["cell_px"]==256,"ORIGINAL_ART_CONTRACT_CHANGED")
    for stage in STATES:
        for i in range(3):
            p=OWNER_DIR+"/frames/"+stage+"-"+str(i)+".svg"
            data=original_blob(repo,OWNER,p)
            require(b"<svg" in data and b"viewBox" in data,"OWNER_SVG_MISSING")
    return manifest,template

def generated_meta(template,guid):
    t=template.decode("utf-8")
    old=GUID_RE.search(t)
    require(old is not None,"TEMPLATE_GUID_MISSING")
    t=t[:old.start(1)]+guid+t[old.end(1):]
    mandatory={
        "spriteMode: 0":"spriteMode: 1",
        "textureType: 2":"textureType: 8",
        "spritePixelsToUnits: 100":"spritePixelsToUnits: 256",
        "spriteGenerateFallbackPhysicsShape: 1":"spriteGenerateFallbackPhysicsShape: 0",
        "filterMode: 0":"filterMode: 1",
    }
    for before,after in mandatory.items():
        require(t.count(before)==1,"UNITY_META_TEMPLATE_DRIFT:"+before)
        t=t.replace(before,after,1)
    require(t.count("enableMipMap: 0")==1 and t.count("alphaIsTransparency: 1")==1,
            "UNITY_SPRITE_ALPHA_MIP_TEMPLATE_DRIFT")
    return t.encode()
def folder_meta(guid):
    return ("fileFormatVersion: 2\n"
            "guid: "+guid+"\nfolderAsset: yes\nDefaultImporter:\n"
            "  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()

def existing_guids(repo,commit):
    # Deliberately reads Git source, never the user's live Unity Assets.
    p=cmd(["git","-C",str(repo),"grep","-I","-h","-E",r"^guid: [a-f0-9]{32}",commit,"--","Assets"],
          nonzero=True)
    require(p.returncode in (0,1),"EXISTING_GIT_GUID_SCAN_FAILED")
    return set(GUID_RE.findall(p.stdout))

def make_files(output,manifest,template):
    from PIL import Image,ImageChops
    output=Path(output)
    receipt=json.loads((output/"receipt.json").read_bytes())
    require(receipt["package_id"]=="B01-32-DAMAGE-DECAL-001" and
            receipt["png_atlas"]=="part-damage-atlas.png" and
            receipt["stage_order"]==list(STATES) and
            receipt["atlas_size"]==[768,1024] and
            len(receipt["entries"])==12,"ORIGINAL_RASTER_RECEIPT_DRIFT")
    atlas=Image.open(output/"part-damage-atlas.png").convert("RGBA")
    require(atlas.size==(768,1024),"ATLAS_SIZE_CHANGED")
    files={}
    folders=["Assets/B01ImportStaging","Assets/B01ImportStaging/B0140DamageDecals",FRAMES]
    for folder in folders:
        files[folder+".meta"]=folder_meta(stage_guid(folder))
    sprites=[];owner_entries={(x["state"],x["frame"]):x for x in receipt["entries"]}
    for idx,name in enumerate(frame_names()):
        stage,frame=name.rsplit("-",1)
        frame=int(frame)
        info=owner_entries[(stage,frame)]
        p=output/"frames"/(name+".png")
        require(p.is_file() and not p.is_symlink(),"ORIGINAL_RENDER_MISSING:"+name)
        raw=p.read_bytes()
        require(artifact_sha(raw)==info["png_sha256"],"ORIGINAL_PNG_SHA_CHANGED:"+name)
        rgba=Image.open(io.BytesIO(raw)).convert("RGBA")
        require(rgba.size==(256,256),"SPRITE_SIZE_CHANGED:"+name)
        x=frame*256;y=STATES.index(stage)*256
        require(atlas.crop((x,y,x+256,y+256)).tobytes()==rgba.tobytes(),
                "ATLAS_FRAME_PIXEL_PARITY_FAIL:"+name)
        bbox=rgba.getchannel("A").getbbox()
        require((bbox is None)==(stage=="clean"),"ALPHA_VISUAL_STATE_WRONG:"+name)
        rel=FRAMES+"/B0140_"+name+".png"
        files[rel]=raw
        guid=stage_guid(rel)
        files[rel+".meta"]=generated_meta(template,guid)
        sprites.append({"state":stage,"frame":frame,"duration_ms":160,
                        "sprite_asset_path":rel,"sprite_meta_guid":guid,
                        "png_sha256":artifact_sha(raw),"alpha_empty":bbox is None,
                        "atlas_rect_top_left":[x,y,256,256],
                        "atlas_rect_unity_bottom_left":[x,3*256-y,256,256]})
    # Atlas is an archival reference, not a second Unity Sprite to accidentally
    # trigger state duplication or wrong multiple-sprite indexing.
    files["Reference/part-damage-atlas.png"]=(output/"part-damage-atlas.png").read_bytes()
    files["Reference/producer-receipt.json"]=canon(receipt)
    files["Reference/codex-sprite-binding.json"]=canon({
        "package_id":PACKAGE,"owner_original_sha":OWNER,
        "owner_original_manifest_blob":MANIFEST_OID,
        "unity_import_mode":"12 separate Sprite (Single) PNG assets",
        "texture_stage_thresholds":"CODEX_POLICY_NOT_ASSUMED",
        "runtime_adapter":"CODEX_OWNED_NOT_INCLUDED",
        "frame_ms":160,"sprites":sprites,
        "native_unity":"NOT_RUN","codex_integrated":False,
        "install_root":ROOT,
        "rollback":"On approved disposable integration, remove ONLY these new paths and metas; never remove preexisting Assets."})
    require(len(files)==3+24+3,"IMPORT_MEMBER_COUNT_DRIFT")
    return files

def package_zip(files):
    buf=io.BytesIO()
    with zipfile.ZipFile(buf,"w",compression=zipfile.ZIP_STORED) as z:
        for path,data in sorted(files.items()):
            require(not path.startswith("/") and "\\" not in path and ".." not in Path(path).parts,
                    "ZIP_TRAVERSAL_MEMBER")
            m=zipfile.ZipInfo(path,(1980,1,1,0,0,0))
            m.create_system=3;m.external_attr=0o100644<<16
            m.compress_type=zipfile.ZIP_STORED
            z.writestr(m,data)
    return buf.getvalue()

def validate_zip(data,existing=None):
    from PIL import Image,ImageChops
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        names=z.namelist()
        require(names==sorted(names) and len(names)==30 and len(names)==len(set(names)),
                "ZIP_MEMBER_SET_DRIFT")
        require(all(not n.startswith("/") and "\\" not in n and ".." not in Path(n).parts for n in names),
                "ZIP_TRAVERSAL_MEMBER")
        files={n:z.read(n) for n in names}
        for info in z.infolist():
            require(info.create_system==3 and info.date_time==(1980,1,1,0,0,0),
                    "NONPORTABLE_ZIP_METADATA")
    index=json.loads(files["Reference/codex-sprite-binding.json"])
    require(index["package_id"]==PACKAGE and len(index["sprites"])==12 and
            index["runtime_adapter"]=="CODEX_OWNED_NOT_INCLUDED" and
            index["native_unity"]=="NOT_RUN" and not index["codex_integrated"],
            "FALSE_UNITY_INTEGRATION_CLAIM")
    guids=[]
    for n,b in files.items():
        if n.endswith(".meta"):
            m=GUID_RE.search(b.decode("utf-8"))
            require(m is not None,"UNREADABLE_META_GUID:"+n)
            g=m.group(1);guids.append(g)
            require(g==stage_guid(n[:-5]),"NONDETERMINISTIC_SPRITE_META_GUID:"+n)
            if n.endswith(".png.meta"):
                s=b.decode()
                require("spriteMode: 1" in s and "textureType: 8" in s and
                        "alphaIsTransparency: 1" in s and "enableMipMap: 0" in s and
                        "spriteGenerateFallbackPhysicsShape: 0" in s,
                        "WRONG_SPRITE_IMPORT_SETTINGS:"+n)
        elif n.endswith(".png") and n.startswith(FRAMES):
            require(Image.open(io.BytesIO(b)).size==(256,256),"PNG_SIZE_WRONG")
    require(len(guids)==15 and len(set(guids))==15,"DUPLICATE_UNITY_GUID")
    if existing is not None:require(not set(guids).intersection(existing),"CODEX_EXISTING_UNITY_GUID_COLLISION")
    atlas=Image.open(io.BytesIO(files["Reference/part-damage-atlas.png"])).convert("RGBA")
    require(atlas.size==(768,1024),"REFERENCE_ATLAS_SIZE_INVALID")
    for ordinal,entry in enumerate(index["sprites"]):
        require(entry["state"]==STATES[ordinal//3] and entry["frame"]==ordinal%3 and
                entry["duration_ms"]==160 and
                entry["atlas_rect_top_left"]==[(ordinal%3)*256,(ordinal//3)*256,256,256] and
                entry["atlas_rect_unity_bottom_left"]==[(ordinal%3)*256,(3-ordinal//3)*256,256,256],
                "ATLAS_INDEX_ORDER_MISMATCH")
        path=entry["sprite_asset_path"]
        require(path in files and entry["sprite_meta_guid"]==stage_guid(path) and
                entry["png_sha256"]==artifact_sha(files[path]),"SPRITE_INDEX_OR_BYTES_CHANGED")
        rgba=Image.open(io.BytesIO(files[path])).convert("RGBA")
        x,y,w,h=entry["atlas_rect_top_left"]
        require((w,h)==(256,256) and
                atlas.crop((x,y,x+w,y+h)).tobytes()==rgba.tobytes(),
                "SOURCE_ATLAS_SPRITE_PIXEL_MISMATCH")
        require((rgba.getchannel("A").getbbox() is None)==entry["alpha_empty"],
                "SPRITE_ALPHA_METADATA_MISMATCH")
    return {"package_id":PACKAGE,"sprites":12,"unity_guid_count":len(guids),
            "atlas_px":[768,1024],"source_commit":OWNER,
            "zip_sha256":artifact_sha(data),"native_unity":"NOT_RUN",
            "status":"REAL_PNG_AND_META_SOURCE_STAGING_PASS"}

def build(repo,out):
    repo=Path(repo).resolve(strict=True)
    out=Path(out).resolve()
    require(Path(git(repo,"rev-parse","--show-toplevel")).resolve()==repo,"REPO_ROOT_REQUIRED")
    require(not out.exists() and repo not in out.parents and out!=repo and out.parent.is_dir(),
            "EXTERNAL_NEW_OUTPUT_REQUIRED")
    manifest,template=pinned_original(repo)
    all_guids=existing_guids(repo,BASE)|existing_guids(repo,CODEX)|existing_guids(repo,OWNER)
    with tempfile.TemporaryDirectory(prefix="B0140_P17VFX_") as temp:
        temp=Path(temp);work=temp/"original-source";render=temp/"original-raster"
        cmd(["git","-C",str(repo),"worktree","add","--detach",str(work),OWNER])
        try:
            cmd([sys.executable,str(work/RENDER),str(render)],cwd=work,timeout=120)
            files=make_files(render,manifest,template)
            package=package_zip(files)
            result=validate_zip(package,all_guids)
            require(package==package_zip(files),"NONDETERMINISTIC_PAYLOAD")
        finally:
            cmd(["git","-C",str(repo),"worktree","remove","--force",str(work)])
        out.mkdir()
        (out/ARCHIVE).write_bytes(package)
        (out/"B0140-original-source-receipt.json").write_bytes(canon({
            **result,"art_producer_original_sha":OWNER,
            "codex_source_ref_only":CODEX,
            "output_zip_name":ARCHIVE,
            "file_sha256":{k:artifact_sha(v) for k,v in sorted(files.items())},
            "unity_import":"NOT_RUN","G1":"NOT_RUN","G2":"HOLD",
            "codex_ack":"PENDING",
        }))
        return result

def main():
    a=argparse.ArgumentParser()
    a.add_argument("--repo",help="QA worktree Git root")
    a.add_argument("--out",help="new external output directory")
    a.add_argument("--verify-zip",help="read-only validation of previously generated ZIP")
    args=a.parse_args()
    try:
        if args.verify_zip:
            existing=None
            if args.repo:
                repo=Path(args.repo).resolve(strict=True)
                pinned_original(repo)
                existing=existing_guids(repo,BASE)|existing_guids(repo,CODEX)|existing_guids(repo,OWNER)
            result=validate_zip(Path(args.verify_zip).read_bytes(),existing)
        else:
            require(args.repo and args.out,"BUILD_REQUIRES_REPO_AND_OUT")
            result=build(args.repo,args.out)
        print(json.dumps(result,sort_keys=True),flush=True)
        return 0
    except (Reject,OSError,ValueError,KeyError,zipfile.BadZipFile,subprocess.TimeoutExpired) as e:
        print("B01_40_P17_VFX_REJECT "+str(e)+" UNITY_NOT_RUN",file=sys.stderr,flush=True)
        return 2
if __name__=="__main__":sys.exit(main())
