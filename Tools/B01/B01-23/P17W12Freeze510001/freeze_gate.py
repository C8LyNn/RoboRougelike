#!/usr/bin/env python3
"""Read-only exclusive #510 frozen source and recipient scan. NOT B01-40 G0."""
import hashlib, io, json, re, subprocess, sys, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SOURCE="d94400d95d13db7f27e1533df05c9292d5380431"
OLD="33a32b8bd9ab40467ab1a17b5684926a37dbd4d3"
W06="cb2b3a76e84266e2730fffc4ab048c1039f0652f"
ALT={"511":"baf65f2664e38f3b9c38815c3f07285f8107c1b6","494":"6efe45961ed0ffdf183e8016e350978b9f3dc51c"}
DIGEST="d4ddfa3528ef0416aefffca72d884000f3b0c9959fb8dff53096487ef95c9387"
SIZE=99666
ASSET="Assets/B0123SecureReattachV2"
T10="Tools/B01/B01-23/P17W10SecureReattachV2/"
T11="Tools/B01/B01-23/P17W11DockAtomicR2/"
WORKFLOWS=[".github/workflows/b01-23-p17w10-secure-reattach-v2.yml",".github/workflows/b01-23-p17w11-atomic-dock-r2.yml"]
PINS={
"Assets/B0123DebrisRepulse001/Runtime/DebrisRepulsionEmitter.cs":"2b3a8465c550bd49a5c98be5a8064a4ce0947f4b",
"Assets/B0123DebrisRepulse001/Runtime/DebrisImpulseCore.cs":"fa1aa5e531927cbe73cf5fdfe161b4a981316db5",
"Assets/B0123DebrisRepulse001/Runtime/B0123.DebrisRepulse.Runtime.asmdef":"25a49dc836b62d791c0bf40c214c1c6cf09a9fea"}
FORBIDDEN=("Assets/B0123DebrisPlayground001/","Assets/B0123ReattachClearance001/","Assets/B0123DockHardening001/","Tools/B01/B01-23/P17W11SecureV2RearmAtomicity/","Tools/B01/B01-23/P17W10PublicDockGuardR2/")
LEGACY_GUIDS=("b017d070000000000000000000000008","b018d080000000000000000000000008","b019d090000000000000000000000008")
LEGACY_CLASSES=("DebrisRigPlayground","DebrisSafeReattachGate","HardenedDockDemo")
def git(*args,binary=False):
    b=subprocess.check_output(["git",*args],cwd=ROOT)
    return b if binary else b.decode().strip()
def original(path,ref=SOURCE):return git("show",ref+":"+path,binary=True)
def files():
    arr=git("ls-tree","-r","--name-only",SOURCE,ASSET+"/").splitlines()
    arr+=git("ls-tree","-r","--name-only",SOURCE,T10).splitlines()
    arr+=git("ls-tree","-r","--name-only",SOURCE,T11).splitlines()
    arr += [ASSET+".meta",*WORKFLOWS]
    arr.sort()
    if len(arr)!=34 or len(set(arr))!=34:raise ValueError("SELECTED_34_SOURCE_FILES_DRIFT")
    if not all(x.startswith((ASSET+"/",T10,T11)) or x in [ASSET+".meta",*WORKFLOWS] for x in arr):
        raise ValueError("UNOWNED_SELECTED_SOURCE_PATH")
    return arr
def source_check():
    if git("rev-parse",SOURCE)!=SOURCE or SOURCE in ALT.values():raise ValueError("SOURCE_IDENTITY_CHANGED")
    for path,blob in PINS.items():
        if git("rev-parse",SOURCE+":"+path)!=blob:raise ValueError("W06_FROZEN_DEPENDENCY_DRIFT "+path)
    names=files();ids=set();asm={}
    for p in names:
        b=original(p)
        if p.endswith(".meta"):
            m=re.findall(r"^guid: ([0-9a-f]{32})\s*$",b.decode(),re.M)
            if len(m)!=1 or m[0] in ids:raise ValueError("UNITY_META_GUID_ERROR "+p)
            ids.add(m[0])
        if p.endswith(".asmdef"):
            j=json.loads(b);asm[j["name"]]=sorted(j["references"])
    expected={"B0123.SecureReattachV2.Runtime":["B0123.DebrisRepulse.Runtime"],
              "B0123.SecureReattachV2.Editor":["B0123.SecureReattachV2.Runtime"],
              "B0123.SecureReattachV2.PlayMode":["B0123.SecureReattachV2.Runtime"]}
    if len(ids)!=14 or asm!=expected:raise ValueError("GUID_OR_ASMDEF_DRIFT")
    return names
def manifest(names):
    return {"package":"B01-23-SECURE-REATTACH-V2","version":"2.0.0","source_sha":SOURCE,
            "parent_w06_sha":W06,"supersedes_source_sha":OLD,
            "frozen_source_blobs":PINS,"owned_meta_guids":14,"native_unity":"NOT_RUN","codex_ack":False,
            "files":[{"path":p,"bytes":len(original(p)),"sha256":hashlib.sha256(original(p)).hexdigest()} for p in names]}
def zinfo(p):
    info=zipfile.ZipInfo(p,(1980,1,1,0,0,0))
    info.compress_type=zipfile.ZIP_STORED;info.create_system=3;info.external_attr=0o100644<<16
    return info
def reproduce():
    paths=source_check()
    parts={p:original(p) for p in paths}
    parts["PACKAGE_MANIFEST.json"]=(json.dumps(manifest(paths),sort_keys=True,separators=(",",":"))+"\n").encode()
    out=io.BytesIO()
    with zipfile.ZipFile(out,"w") as z:
        for p in sorted(parts):z.writestr(zinfo(p),parts[p])
    raw=out.getvalue()
    verify_archive(raw)
    return raw
def verify_archive(raw):
    if len(raw)!=SIZE or hashlib.sha256(raw).hexdigest()!=DIGEST:
        raise ValueError("NOT_FROZEN_510_SOURCE_INNER_ZIP")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        names=z.namelist();paths=files()
        if len(names)!=35 or len(set(names))!=35 or set(names)!=set(paths)|{"PACKAGE_MANIFEST.json"}:
            raise ValueError("FROZEN_ZIP_MEMBERS_CHANGED")
        if json.loads(z.read("PACKAGE_MANIFEST.json"))!=manifest(paths):
            raise ValueError("FROZEN_ZIP_MANIFEST_CHANGED")
        for p in paths:
            if z.read(p)!=original(p):raise ValueError("FROZEN_ZIP_GIT_BLOB_CHANGED "+p)
    return True
def receiver_scan(folder):
    """Never writes/deletes files. Checks fully STAGED SOURCE, not an assumed Codex worktree."""
    root=Path(folder)
    if not root.is_dir() or root.is_symlink():raise ValueError("NO_REAL_RECEIVER_DIRECTORY")
    paths=files()
    for p in paths:
        f=root/p
        if not f.is_file() or f.is_symlink():raise ValueError("SELECTED_SOURCE_MISSING "+p)
        if f.read_bytes()!=original(p):raise ValueError("ALTERNATIVE_OR_MUTATED_SOURCE "+p)
    checked=0
    for f in root.rglob("*"):
        if f.is_symlink():raise ValueError("UNSAFE_RECEIVER_SYMLINK")
        if not f.is_file():continue
        rel=f.relative_to(root).as_posix()
        if rel.startswith(FORBIDDEN):raise ValueError("LEGACY_OR_ALTERNATIVE_INSTALLED "+rel)
        if rel.startswith(ASSET+"/") and rel not in paths:
            raise ValueError("UNOWNED_FILE_INSIDE_SELECTED_PACKAGE "+rel)
        if f.suffix.lower() in (".unity",".prefab",".asset"):
            s=f.read_text(encoding="utf-8",errors="replace").lower()
            if any(re.search(r"guid:\s*"+x+r"\b",s) for x in LEGACY_GUIDS):
                raise ValueError("LEGACY_SCRIPT_GUID_IN_SCENE_OR_PREFAB "+rel)
        if rel.startswith("Assets/") and rel.endswith(".cs") and not rel.startswith(ASSET+"/"):
            s=f.read_text(encoding="utf-8",errors="replace")
            if any(re.search(r"\b"+x+r"\b",s) for x in LEGACY_CLASSES):
                raise ValueError("LEGACY_HOST_CSHARP_ROUTE "+rel)
        checked+=1
    return checked
def main():
    import argparse
    a=argparse.ArgumentParser()
    a.add_argument("command",choices=("freeze","verify-artifact","scan-receiver"))
    a.add_argument("--zip",type=Path);a.add_argument("--receiver",type=Path)
    x=a.parse_args()
    try:
        if x.command=="freeze":
            raw=reproduce()
            print(json.dumps({"freeze":"PRODUCER_IDENTITY_PASS_NOT_INDEPENDENT_G0","source_sha":SOURCE,
                "selected_pr":510,"version":"2.0.0","inner_bytes":len(raw),"members":35,
                "inner_sha256":hashlib.sha256(raw).hexdigest(),"G0":"PENDING","G1":"NOT_RUN",
                "Codex_ACK":False,"511":"ALT_HOLD","494":"ALT_HOLD"},sort_keys=True))
        elif x.command=="verify-artifact":
            if x.zip is None:raise ValueError("INNER_ZIP_REQUIRED")
            verify_archive(x.zip.read_bytes());print("ORIGINAL_SELECTED_INNER_ZIP_VERIFIED")
        else:
            if x.receiver is None:raise ValueError("RECEIVER_DIRECTORY_REQUIRED")
            print("EXCLUSIVE_510_READ_ONLY_RECEIVER_PASS files="+str(receiver_scan(x.receiver)))
    except (ValueError,OSError,subprocess.CalledProcessError,zipfile.BadZipFile) as e:
        print("W12_EXCLUSIVE_510_GATE_RED "+str(e),file=sys.stderr);sys.exit(3)
if __name__=="__main__":main()
