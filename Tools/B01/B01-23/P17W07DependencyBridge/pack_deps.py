#!/usr/bin/env python3
"""B01-23 W07: exact immutable 16-source-P17-dependency physical ZIP; no canonical writes."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import zipfile

SOURCE="5666a39eabd4cea91b2b605a9eb99ea2f1e226e8"
TARGET="203a0805251ed28293888e43f2d49f0df63767c6"
PINS={
  "Assets/PARTSIZE001.meta": "613b401213ace8316407558ed75346d733214c31",
  "Assets/PARTSIZE001/Runtime.meta": "cf23f523dac5bf44c3524f3f3cbf4408bde061d1",
  "Assets/PARTSIZE001/Runtime/B01.PARTSIZE001.Runtime.asmdef": "90b80cf81b42da927285448c8ad12c5f7cbfe4e4",
  "Assets/PARTSIZE001/Runtime/B01.PARTSIZE001.Runtime.asmdef.meta": "2d919c2d770f32684933298238adc3b284d1bacf",
  "Assets/PARTSIZE001/Runtime/PartSizeContract.cs": "07455919a5544875fee5a78ecbbce36dc6cb49c2",
  "Assets/PARTSIZE001/Runtime/PartSizeContract.cs.meta": "1babe7e5c2db0a317f9777de0a5304a40bdbe78d",
  "Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs": "6153d1758e800ec5c4210323610b061c06e8ea85",
  "Assets/PARTSIZE001/Runtime/PartSizeBaselineCatalog.cs.meta": "83a3f62f5882b7cab537b0bc133cc29a57c565ad",
  "Assets/PARTSIZE001/Blueprints.meta": "277f557c51f4348908e64e14b9fc9885b7fc72c2",
  "Assets/PARTSIZE001/Blueprints/P17.meta": "00d066a7138a709c91c2fc27846164291e925b27",
  "Assets/PARTSIZE001/Blueprints/P17/P17_Bulwark_Tracks.json": "2fe5b441d081ad3590ccbeabf1cbb1f266ec662a",
  "Assets/PARTSIZE001/Blueprints/P17/P17_Bulwark_Tracks.json.meta": "d7e2e336eed0f52f161844a48f598ad0ec34be43",
  "Assets/PARTSIZE001/Blueprints/P17/P17_Flanker_Wheels.json": "c4778df1db431ed1b51e8fb1e607ae1ede3e1bef",
  "Assets/PARTSIZE001/Blueprints/P17/P17_Flanker_Wheels.json.meta": "49942677da06daabc059a11e550a9e37f37b292a",
  "Assets/PARTSIZE001/Blueprints/P17/P17_Skirmisher_Hover.json": "4d7b4ab0adfb71fa2c20479eaa7c266687d28fa1",
  "Assets/PARTSIZE001/Blueprints/P17/P17_Skirmisher_Hover.json.meta": "1fe1b221dd5b72deb5eed94bec172c1a573ac89f"
}
RECEIPT="ORIGINAL_SOURCE_MANIFEST.json"
MARKER=".b01-p17-dependency-owned"
GUID=re.compile(rb"(?m)^guid: ([a-fA-F0-9]{32})\s*$")

class Hold(Exception): pass

def run_git(*args,input_bytes=None):
    p=subprocess.run(["git",*args],input=input_bytes,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    if p.returncode: raise Hold("MISSING_GIT_OBJECT "+str(args)+" "+p.stderr.decode(errors="replace")[:200])
    return p.stdout

def blob(data):
    return hashlib.sha1(b"blob "+str(len(data)).encode()+b"\0"+data).hexdigest()

def digest(data):
    return hashlib.sha256(data).hexdigest()

def source():
    result={}
    for path,pin in PINS.items():
        content=run_git("show",SOURCE+":"+path)
        if blob(content)!=pin: raise Hold("ORIGINAL_BLOB_MISMATCH "+path)
        result[path]=content
    unique_guids(result)
    return result

def unique_guids(data):
    seen={}
    for path,contents in data.items():
        if not path.endswith(".meta"): continue
        match=GUID.findall(contents)
        if len(match)!=1: raise Hold("INVALID_META "+path)
        g=match[0].lower().decode()
        if g in seen: raise Hold("DUPLICATE_META_GUID "+g)
        seen[g]=path
    return seen

def inventory(data):
    return dict(schema="b01-p17-dependency-bridge-v1",
                source_sha=SOURCE, target_example_sha=TARGET,
                native_unity="NOT_RUN", codex_ack=False,
                paths=[dict(path=p,bytes=len(data[p]),sha256=digest(data[p]),git_blob_sha1=PINS[p])
                       for p in sorted(PINS)])

def zipinfo(path):
    info=zipfile.ZipInfo(path,(1980,1,1,0,0,0))
    info.create_system=3
    info.compress_type=zipfile.ZIP_STORED
    info.external_attr=0o100644<<16
    return info

def make(data):
    if set(data)!=set(PINS): raise Hold("SOURCE_PATH_SET")
    unique_guids(data)
    out=io.BytesIO()
    with zipfile.ZipFile(out,"w") as z:
        for path in sorted(PINS):
            if blob(data[path])!=PINS[path]: raise Hold("TAMPERED_SOURCE "+path)
            z.writestr(zipinfo(path),data[path])
        z.writestr(zipinfo(RECEIPT),(json.dumps(inventory(data),sort_keys=True,indent=2)+"\n").encode())
    return out.getvalue()

def verify(raw):
    try:
        with zipfile.ZipFile(io.BytesIO(raw)) as z:
            names=z.namelist()
            if len(names)!=17 or len(set(names))!=17 or set(names)!=set(PINS)|{RECEIPT}:
                raise Hold("ZIP_MEMBER_SET")
            for member in z.infolist():
                if member.is_dir() or member.file_size>200000 or member.filename.startswith("/") or "\\" in member.filename or ".." in member.filename.split("/"):
                    raise Hold("ZIP_UNSAFE_MEMBER")
            data={path:z.read(path) for path in PINS}
            for path in PINS:
                if blob(data[path])!=PINS[path]: raise Hold("ZIP_ORIGINAL_GIT_BLOB "+path)
            unique_guids(data)
            if json.loads(z.read(RECEIPT))!=inventory(data): raise Hold("EMBEDDED_RECEIPT_TAMPER")
            return data
    except (zipfile.BadZipFile,KeyError,ValueError,json.JSONDecodeError) as e:
        raise Hold("BAD_ARCHIVE "+str(e)) from e

def tree_at(sha):
    content=run_git("ls-tree","-r","-z",sha,"--","Assets")
    entries={}
    for line in content.split(b"\0"):
        if line:
            left,path=line.split(b"\t",1)
            entries[path.decode()]=left.decode().split()[2]
    return entries

def batch_blobs(shas):
    if not shas: return []
    raw=run_git("cat-file","--batch",input_bytes="".join(sha+"\n" for sha in shas).encode())
    results=[]; pos=0
    for sha in shas:
        end=raw.find(b"\n",pos)
        if end<0: raise Hold("BAD_CATFILE_HEADER")
        fields=raw[pos:end].decode().split()
        if len(fields)!=3 or fields[:2]!=[sha,"blob"]: raise Hold("BAD_CATFILE_OBJECT")
        size=int(fields[2]);start=end+1
        results.append(raw[start:start+size])
        if len(results[-1])!=size or raw[start+size:start+size+1]!=b"\n": raise Hold("BAD_CATFILE_SIZE")
        pos=start+size+1
    return results

def preflight(raw,sha):
    data=verify(raw)
    tree=tree_at(sha)
    fold={}
    for p in tree:
        if p.casefold() in fold and fold[p.casefold()]!=p: raise Hold("TARGET_CASEFOLD_DUP")
        fold[p.casefold()]=p
    metas=[(p,v) for p,v in tree.items() if p.endswith(".meta")]
    known={}
    for (path,_),content in zip(metas,batch_blobs([v for _,v in metas])):
        parsed=GUID.findall(content)
        if len(parsed)!=1: raise Hold("TARGET_BAD_META "+path)
        key=parsed[0].lower().decode()
        if key in known: raise Hold("TARGET_GUID_DUP "+key)
        known[key]=path
    existing=[]; missing=[]
    for path in PINS:
        other=fold.get(path.casefold())
        if other:
            if other!=path or tree[other]!=PINS[path]: raise Hold("TARGET_PATH_BYTES_COLLISION "+path)
            existing.append(path)
        else: missing.append(path)
    for guid,path in unique_guids(data).items():
        if guid in known and (known[guid]!=path or path not in existing):
            raise Hold("TARGET_GUID_COLLISION "+path)
    four=("Assets/PARTSIZE001/Runtime/PartSizeContract.cs",
          "Assets/PARTSIZE001/Blueprints/P17/P17_Bulwark_Tracks.json",
          "Assets/PARTSIZE001/Blueprints/P17/P17_Flanker_Wheels.json",
          "Assets/PARTSIZE001/Blueprints/P17/P17_Skirmisher_Hover.json")
    return dict(source_sha=SOURCE,target_sha=sha,target_assets=len(tree),
                target_metas=len(metas),missing_source_files=missing,
                already_identical=existing,missing_four=sum(p in missing for p in four),
                path_guid_collision="NONE_AT_EXACT_SHA", native_unity="NOT_RUN",
                verdict="ORIGINAL_SOURCE_PREFLIGHT_ONLY")

def staging_allowed(path):
    p=path.resolve()
    if path.exists() or path.is_symlink(): raise Hold("DESTINATION_ALREADY_EXISTS")
    if not p.parent.is_dir(): raise Hold("DESTINATION_PARENT_MISSING")
    status=subprocess.run(["git","-C",str(p.parent),"rev-parse","--show-toplevel"],
                          stdout=subprocess.PIPE,stderr=subprocess.DEVNULL)
    if status.returncode==0: raise Hold("REFUSE_WRITE_INSIDE_GIT_WORKTREE")
    return p

def stage(raw,path):
    files=verify(raw)
    target=staging_allowed(path)
    target.mkdir()
    try:
        for p,data in files.items():
            dest=target/p
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(data)
        (target/MARKER).write_text(digest(raw)+"\n")
    except BaseException:
        shutil.rmtree(target)
        raise
    return target

def rollback(raw,path):
    files=verify(raw); target=path.resolve()
    if not target.is_dir() or path.is_symlink(): raise Hold("NOT_OWNED_STAGE")
    marker=target/MARKER
    if not marker.is_file() or marker.read_text()!=digest(raw)+"\n": raise Hold("BAD_STAGE_MARKER")
    found=set()
    for item in target.rglob("*"):
        if item.is_symlink(): raise Hold("FOREIGN_SYMLINK")
        if item.is_file():
            rel=item.relative_to(target).as_posix()
            found.add(rel)
            if rel not in set(PINS)|{MARKER} or (rel!=MARKER and item.read_bytes()!=files[rel]):
                raise Hold("REFUSE_CHANGED_OR_FOREIGN_FILE")
    if found!=set(PINS)|{MARKER}: raise Hold("STAGE_MISSING_FILE")
    shutil.rmtree(target)
    return "ROLLBACK_OWNED_FILES_PASS"

def main():
    p=argparse.ArgumentParser()
    p.add_argument("action",choices=("build","verify","preflight","stage","rollback"))
    p.add_argument("--zip",required=True)
    p.add_argument("--target",default=TARGET)
    p.add_argument("--dest")
    p.add_argument("--receipt")
    a=p.parse_args()
    path=Path(a.zip)
    if a.action=="build":
        if path.exists(): raise Hold("REFUSE_OVERWRITE_ZIP")
        raw=make(source());path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(raw)
        print("ORIGINAL_ZIP_SIZE",len(raw),"ORIGINAL_ZIP_SHA256",digest(raw))
    else:
        raw=path.read_bytes()
        if a.action=="verify":
            verify(raw);print("VERIFIED_16_BLOBS_17_ZIP_MEMBERS SHA256",digest(raw))
        elif a.action=="preflight":
            result=preflight(raw,a.target)
            if a.receipt:
                out=Path(a.receipt)
                if out.exists(): raise Hold("RECEIPT_EXISTS")
                out.write_text(json.dumps(result,sort_keys=True,indent=2)+"\n")
            print(json.dumps(result,sort_keys=True))
        elif a.action=="stage":
            if not a.dest: raise Hold("DEST_REQUIRED")
            print("STAGED_NON_GAME",stage(raw,Path(a.dest)))
        elif a.action=="rollback":
            if not a.dest: raise Hold("DEST_REQUIRED")
            print(rollback(raw,Path(a.dest)))
if __name__=="__main__":
    try: main()
    except (Hold,OSError) as e: print("HOLD",e,file=sys.stderr);sys.exit(2)
