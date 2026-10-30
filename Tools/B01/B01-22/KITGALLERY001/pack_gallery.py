"""Deterministic physical Unity Editor-only package for B01-22 KITCOMPARE.
Never touches gameplay; creates a new output directory and verifies rollback.
"""
import argparse,hashlib,io,json,pathlib,subprocess,sys,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[4]
MEMBERS=tuple(sorted(("Assets/B01KitGallery001.meta","Assets/B01KitGallery001/Data.meta",
"Assets/B01KitGallery001/Editor.meta","Assets/B01KitGallery001/Data/KitGallery.json",
"Assets/B01KitGallery001/Data/KitGallery.json.meta",
"Assets/B01KitGallery001/Editor/KitGalleryWindow.cs",
"Assets/B01KitGallery001/Editor/KitGalleryWindow.cs.meta",
"Assets/B01KitGallery001/Editor/KitComparisonRules.cs",
"Assets/B01KitGallery001/Editor/KitComparisonRules.cs.meta")))
PIN="b1d4e341d846c9397e236809a1d7e4c4fff9c935"
class PackageError(ValueError):pass
def hash_bytes(raw):return hashlib.sha256(raw).hexdigest()
def source_files(root=ROOT):
    data={}
    for name in MEMBERS:
        p=root/name
        if p.is_symlink() or not p.is_file():raise PackageError("MISSING_OR_SYMLINK:"+name)
        data[name]=p.read_bytes()
    return data
def make_zip(data):
    if set(data)!=set(MEMBERS):raise PackageError("FOREIGN_OR_MISSING_ASSET")
    out=io.BytesIO()
    with zipfile.ZipFile(out,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
        for name in MEMBERS:
            item=zipfile.ZipInfo(name,(2020,1,1,0,0,0))
            item.create_system=3;item.external_attr=0o100644<<16;item.compress_type=zipfile.ZIP_DEFLATED
            z.writestr(item,data[name],compress_type=zipfile.ZIP_DEFLATED,compresslevel=9)
    return out.getvalue()
def receipt_for(raw,data):
    return {"schema":"b01-kitcompare-unity-source-v1","source_sha":PIN,"no_game_writes":True,
            "zip_sha256":hash_bytes(raw),"zip_bytes":len(raw),
            "files":{n:{"bytes":len(data[n]),"sha256":hash_bytes(data[n])} for n in MEMBERS}}
def verify_zip(raw,receipt,data):
    if receipt.get("schema")!="b01-kitcompare-unity-source-v1" or receipt.get("source_sha")!=PIN or receipt.get("no_game_writes") is not True:raise PackageError("BAD_RECEIPT_SCHEMA")
    if receipt.get("zip_sha256")!=hash_bytes(raw) or receipt.get("zip_bytes")!=len(raw) or set(receipt.get("files",{}))!=set(MEMBERS):raise PackageError("BAD_RECEIPT_HASHES")
    if set(data)!=set(MEMBERS):raise PackageError("SOURCE_FILE_SET")
    with zipfile.ZipFile(io.BytesIO(raw)) as z:
        if z.namelist()!=list(MEMBERS) or z.testzip() is not None:raise PackageError("ZIP_MEMBER_LIST_OR_CRC")
        for n in MEMBERS:
            content=z.read(n)
            if content!=data[n] or receipt["files"][n]!={"bytes":len(content),"sha256":hash_bytes(content)}:raise PackageError("BAD_SOURCE_OR_FILE:"+n)
    return len(MEMBERS)
def no_links(path):
    if any(p.is_symlink() for p in (path,*path.parents)):raise PackageError("SYMLINK_OUTPUT")
def stage(dest,source_check=True):
    dest=pathlib.Path(dest);no_links(dest)
    if dest.exists() or not dest.parent.is_dir():raise PackageError("DEST_EXISTS_OR_PARENT_MISSING")
    if source_check:
        subprocess.run([sys.executable,str(pathlib.Path(__file__).with_name("verify_gallery.py"))],cwd=ROOT,check=True,capture_output=True,encoding="utf-8")
    data=source_files();raw=make_zip(data);receipt=receipt_for(raw,data);verify_zip(raw,receipt,data)
    dest.mkdir(mode=0o700)
    try:
        with (dest/"kitcompare-unity.zip").open("xb") as f:f.write(raw)
        with (dest/"receipt.json").open("x",encoding="utf-8") as f:f.write(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    except BaseException:
        # Do not recursively remove user data, even on partial failure.
        raise
    return receipt
def rollback(dest):
    dest=pathlib.Path(dest);no_links(dest)
    if not dest.is_dir() or {p.name for p in dest.iterdir()}!={"kitcompare-unity.zip","receipt.json"}:raise PackageError("ROLLBACK_FOREIGN_FILES")
    if any(p.is_symlink() or not p.is_file() for p in dest.iterdir()):raise PackageError("ROLLBACK_SYMLINK")
    rec=json.loads((dest/"receipt.json").read_text(encoding="utf-8"))
    verify_zip((dest/"kitcompare-unity.zip").read_bytes(),rec,source_files())
    (dest/"kitcompare-unity.zip").unlink();(dest/"receipt.json").unlink();dest.rmdir()
    return "ROLLED_BACK_NO_GAME_CHANGES"
def main():
    parser=argparse.ArgumentParser();group=parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--out");group.add_argument("--rollback");args=parser.parse_args()
    try:
        if args.out:print(json.dumps({"status":"PHYSICAL_SOURCE_ZIP_CREATED","receipt":stage(args.out)},sort_keys=True))
        else:print(json.dumps({"status":rollback(args.rollback)}))
    except (OSError,ValueError,subprocess.CalledProcessError,zipfile.BadZipFile) as e:
        print("FAIL_CLOSED:"+str(e),file=sys.stderr);raise SystemExit(2)
if __name__=="__main__":main()
