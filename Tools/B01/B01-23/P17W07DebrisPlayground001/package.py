#!/usr/bin/env python3
"""Source-only, Git-object verified, deterministic W07 DELTA ZIP (not native Unity QA)."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[4]
SUBTREE = "Assets/B0123DebrisPlayground001/"
TOOLS = "Tools/B01/B01-23/P17W07DebrisPlayground001/"
WORKFLOW = ".github/workflows/b01-23-p17w07-debris-playground.yml"
PARENT_SHA = "cb2b3a76e84266e2730fffc4ab048c1039f0652f"
FROZEN_CORE = "fa1aa5e531927cbe73cf5fdfe161b4a981316db5"
FROZEN_ADAPTER = "2b3a8465c550bd49a5c98be5a8064a4ce0947f4b"
PACKAGE_ID = "B01-23-DEBRIS-PHYSICS-PLAYGROUND-001"
MANIFEST = "PACKAGE_MANIFEST.json"

def git(*arguments):
    return subprocess.check_output(("git",)+arguments,cwd=ROOT).decode("utf-8").strip()

def data_for(path):
    return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)

def digest(data):
    return hashlib.sha256(data).hexdigest()

def head():
    return git("rev-parse","HEAD")

def check_parent():
    subprocess.run(["git","merge-base","--is-ancestor",PARENT_SHA,"HEAD"],
                   cwd=ROOT,check=True,stdout=subprocess.DEVNULL)
    checks = {
        "Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs": FROZEN_CORE,
        "Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs": FROZEN_ADAPTER,
    }
    for path,blob in checks.items():
        if git("rev-parse","HEAD:"+path) != blob:
            raise ValueError("frozen #421 parent dependency changed: "+path)

def names():
    result=git("ls-tree","-r","--name-only","HEAD",SUBTREE,TOOLS,WORKFLOW)
    files=result.splitlines()
    if not files or len(files)!=len(set(files)):
        raise ValueError("empty or duplicate payload")
    if not all(p.startswith((SUBTREE,TOOLS)) or p==WORKFLOW for p in files):
        raise ValueError("file outside W07 exclusive ownership")
    for required in (
        SUBTREE+"Runtime/RigDemoDefinition.cs",
        SUBTREE+"Runtime/DebrisRigPlayground.cs",
        SUBTREE+"Editor/DebrisPlaygroundSceneBuilder.cs",
        SUBTREE+"Tests/PlayMode/DebrisRigPlayModeTests.cs",
        TOOLS+"Program.cs", TOOLS+"RuntimeSourceTests.csproj",
        TOOLS+"package.py", TOOLS+"test_package.py",
        WORKFLOW):
        if required not in files:
            raise ValueError("missing source file "+required)
    return sorted(files)

def source_gate():
    check_parent()
    files=names()
    guids=set()
    metas=0
    for path in files:
        if path.endswith(".meta"):
            metas+=1
            raw=data_for(path).decode("utf-8")
            lines=[line.split(":",1)[1].strip() for line in raw.splitlines()
                   if line.startswith("guid:")]
            if len(lines)!=1 or len(lines[0])!=32 or any(ch not in "0123456789abcdef" for ch in lines[0]):
                raise ValueError("invalid Unity .meta "+path)
            if lines[0] in guids:
                raise ValueError("duplicate GUID "+path)
            guids.add(lines[0])
    if metas!=12:
        raise ValueError("expected 12 unique Unity .meta records; got "+str(metas))
    asmdefs={}
    for path in files:
        if path.endswith(".asmdef"):
            obj=json.loads(data_for(path))
            asmdefs[obj["name"]]=obj
    expected={
        "B0123.DebrisPlayground.Runtime":["B0123.DebrisRepulse.Runtime"],
        "B0123.DebrisPlayground.Editor":["B0123.DebrisPlayground.Runtime","B0123.DebrisRepulse.Runtime"],
        "B0123.DebrisPlayground.PlayMode":["B0123.DebrisPlayground.Runtime","B0123.DebrisRepulse.Runtime"],
    }
    if {name:sorted(info.get("references",[])) for name,info in asmdefs.items()} != {name:sorted(refs) for name,refs in expected.items()}:
        raise ValueError("asmdef references do not match original frozen package")
    if asmdefs["B0123.DebrisPlayground.Editor"].get("includePlatforms")!=["Editor"]:
        raise ValueError("Editor assembly is not Editor-only")
    play=asmdefs["B0123.DebrisPlayground.PlayMode"]
    if "UNITY_INCLUDE_TESTS" not in play.get("defineConstraints",[]) or "nunit.framework.dll" not in play.get("precompiledReferences",[]):
        raise ValueError("PlayMode test assembly not configured for NUnit/Unity Test Framework")
    return files,metas

def zipinfo(name):
    obj=zipfile.ZipInfo(name,date_time=(1980,1,1,0,0,0))
    obj.create_system=3
    obj.compress_type=zipfile.ZIP_STORED
    obj.external_attr=0o100644<<16
    return obj

def produce():
    files,metas=source_gate()
    contents={path:data_for(path) for path in files}
    manifest={
        "schema":"B01-DEBRIS-PLAYGROUND-DELTA-1",
        "package_id":PACKAGE_ID,
        "version":"1.0.0",
        "producer":"B01-23",
        "source_sha":head(),
        "requires_pr":421,
        "requires_source_sha":PARENT_SHA,
        "requires_core_git_blob":FROZEN_CORE,
        "requires_adapter_git_blob":FROZEN_ADAPTER,
        "native_unity":"NOT_RUN",
        "independent_qa":"PENDING",
        "codeX_claimed":False,
        "owned_delta":"W07 new paths ONLY",
        "unity_meta_count":metas,
        "files":[{"path":path,"bytes":len(contents[path]),"sha256":digest(contents[path])} for path in files],
    }
    contents[MANIFEST]=(json.dumps(manifest,sort_keys=True,separators=(",",":"))+"\n").encode()
    buffer=io.BytesIO()
    with zipfile.ZipFile(buffer,"w",compression=zipfile.ZIP_STORED) as archive:
        for path in sorted(contents):
            archive.writestr(zipinfo(path),contents[path])
    return buffer.getvalue(),manifest

def verified(data):
    with zipfile.ZipFile(io.BytesIO(data),"r") as archive:
        info=archive.infolist()
        names_received=[item.filename for item in info]
        if len(names_received)!=len(set(names_received)) or set(names_received)!=(set(names())|{MANIFEST}):
            raise ValueError("ZIP unexpected/duplicate/missing entries")
        if any(x.startswith("/") or ".." in x.split("/") or "\\" in x for x in names_received):
            raise ValueError("ZIP traversal")
        contents={name:archive.read(name) for name in names_received}
    manifest=json.loads(contents[MANIFEST])
    if manifest.get("package_id")!=PACKAGE_ID or manifest.get("source_sha")!=head() or \
       manifest.get("requires_source_sha")!=PARENT_SHA or manifest.get("native_unity")!="NOT_RUN" or \
       manifest.get("codeX_claimed") is not False:
        raise ValueError("manifest provenance or false-native/ACK claim")
    files,_=source_gate()
    records=manifest.get("files")
    if not isinstance(records,list) or [r.get("path") for r in records]!=files:
        raise ValueError("wrong file-list receipt")
    for record in records:
        path=record["path"]
        if record["bytes"]!=len(contents[path]) or record["sha256"]!=digest(contents[path]) or \
           contents[path]!=data_for(path):
            raise ValueError("original source byte hash mismatch "+path)
    return contents

def check_external(destination):
    resolved=destination.resolve()
    if resolved==ROOT.resolve() or ROOT.resolve() in resolved.parents:
        raise ValueError("refuse stage/delete inside repo")

def stage(data,destination):
    check_external(destination)
    if destination.exists():
        raise ValueError("destination already exists")
    contents=verified(data)
    destination.mkdir()
    try:
        for path,content in contents.items():
            target=destination/path
            target.parent.mkdir(parents=True,exist_ok=True)
            target.write_bytes(content)
    except Exception:
        shutil.rmtree(destination)
        raise

def rollback(data,destination):
    check_external(destination)
    contents=verified(data)
    if not destination.is_dir() or destination.is_symlink():
        raise ValueError("stage missing or unsafe")
    actual=set()
    for path in destination.rglob("*"):
        if path.is_symlink():
            raise ValueError("symlink in destination; refuse deletion")
        if path.is_file():
            relative=path.relative_to(destination).as_posix()
            if relative not in contents or path.read_bytes()!=contents[relative]:
                raise ValueError("foreign/edited file; refuse deletion")
            actual.add(relative)
    if actual!=set(contents):
        raise ValueError("stage incomplete")
    shutil.rmtree(destination)

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("action",choices=["build","verify","stage","rollback","source-gate"])
    parser.add_argument("--archive")
    parser.add_argument("--destination")
    a=parser.parse_args()
    if a.action=="source-gate":
        paths,metas=source_gate()
        print("PASS REAL GIT W07 source paths",len(paths),"stable .meta",metas,"frozen #421 dependency intact")
        return
    if not a.archive: raise ValueError("--archive required")
    archive=Path(a.archive)
    if a.action=="build":
        if archive.exists(): raise ValueError("archive exists")
        data,_=produce()
        archive.parent.mkdir(parents=True,exist_ok=True)
        archive.write_bytes(data)
        verified(data)
        print("PASS ZIP",len(data),"SHA256",digest(data),"HEAD",head(),"members",len(names())+1)
    else:
        data=archive.read_bytes()
        if a.action=="verify":
            verified(data)
            print("PASS ZIP integrity",digest(data))
        elif a.action=="stage":
            if not a.destination: raise ValueError("--destination required")
            stage(data,Path(a.destination))
            print("PASS isolated staged W07 DELTA ONLY")
        else:
            if not a.destination: raise ValueError("--destination required")
            rollback(data,Path(a.destination))
            print("PASS rollback W07 DELTA ONLY")
if __name__=="__main__":
    try:main()
    except (OSError,ValueError,KeyError,AssertionError,subprocess.CalledProcessError,zipfile.BadZipFile) as e:
        print("FAIL "+str(e),file=sys.stderr)
        sys.exit(1)
