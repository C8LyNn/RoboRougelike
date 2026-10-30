#!/usr/bin/env python3
"""Physically create the incremental 3-asset W25 Unity package, no W24/P12 copies.
Only own new folder, W25 original C# and README. Source ABI hashes pin genuine
P12 Damageable and W24 visual C#; read-only original source, no Unity invocation.
"""
import argparse,gzip,hashlib,io,json,pathlib,tarfile,sys
ID="B01-31-P17W25-REAL-P12-ARMOR-VISUAL-BINDING-001"
VERSION="P17-W25-R1"
ROOT="Assets/B01_31/W25P12DamageVisualBinding"
P12="Assets/Pack/P12_PipelineSafeDamageable.cs"
W24="Assets/B01_31/W24ArmorStates/B0131W24ArmorDamageVisual.cs"
P12_BLOB="544bed274b616b91fd98c45d2b194b2f3a2d112a"
W24_BLOB="8cf3664b9b6e5b0fd6623be7c3f4e72fd9164664"
sha=lambda x:hashlib.sha256(x).hexdigest()
def git_blob(content):
    return hashlib.sha1(b"blob "+str(len(content)).encode()+b"\0"+content).hexdigest()
def check(cond,msg):
    if not cond:raise ValueError("W25_DELTA_ORIGINAL_SOURCE_RED "+msg)
def file_for_tar(tar,name,data):
    info=tarfile.TarInfo(name);info.size=len(data);info.mode=0o644;info.mtime=0
    info.uid=info.gid=0
    tar.addfile(info,io.BytesIO(data))
def build(repo,out):
    check(git_blob((repo/P12).read_bytes())==P12_BLOB,
          "original unchanged gameplay P12 SHA source ABI")
    check(git_blob((repo/W24).read_bytes())==W24_BLOB,
          "original unchanged W24 visual SHA source ABI")
    assets={}
    paths=(ROOT,ROOT+"/B0131W25P12DamageVisualBinding.cs",ROOT+"/README.md")
    for path in paths:
        meta=(repo/(path+".meta")).read_bytes()
        guids=[s[6:] for s in meta.decode().splitlines() if s.startswith("guid: ")]
        check(len(guids)==1 and len(guids[0])==32
              and all(x in "0123456789abcdef" for x in guids[0]),
              "original GUID and safe exact .meta source")
        payload=None if path==ROOT else (repo/path).read_bytes()
        if payload is not None:check(len(payload)>250,"original W25 source size")
        assets[path]={"guid":guids[0],"meta":meta,"content":payload}
    check(len(set(v["guid"] for v in assets.values()))==3,
          "three unique original W25 Unity GUIDs")
    out.mkdir(parents=True,exist_ok=True)
    raw=io.BytesIO()
    with gzip.GzipFile(fileobj=raw,mode="wb",filename="",mtime=0,compresslevel=9) as gz:
        with tarfile.open(fileobj=gz,mode="w",format=tarfile.USTAR_FORMAT) as t:
            for path,item in sorted(assets.items()):
                g=item["guid"]
                file_for_tar(t,g+"/pathname",(path+"\n").encode())
                file_for_tar(t,g+"/asset.meta",item["meta"])
                if item["content"] is not None:
                    file_for_tar(t,g+"/asset",item["content"])
    data=raw.getvalue()
    dest=out/"B0131W25-P12-DamageToArmorVisual-DELTA.unitypackage"
    dest.write_bytes(data)
    check(dest.read_bytes()==data,"real physical delta Unity TAR/GZIP bytes")
    receipt={"package_id":ID,"version":VERSION,
      "P12_original_Git_blob_SHA":P12_BLOB,"W24_original_Git_blob_SHA":W24_BLOB,
      "unitypackage_SHA256":sha(data),"unitypackage_size":len(data),
      "owned_three_GUID_path_entries":{k:v["guid"] for k,v in sorted(assets.items())},
      "source_files":[{"path":k,"sha256":sha(v["content"]) if v["content"] is not None else None,
         "bytes":len(v["content"]) if v["content"] is not None else 0}
         for k,v in sorted(assets.items())],
      "no_authoritative_Game_HP_Save_or_economy_writes":True,
      "source_managed_DotNet8_QA":"SEPARATELY_RUN",
      "native_Unity_G1":"NOT_RUN","formal_G0":"PENDING","G2":"HOLD",
      "Codex_ACK":False,"integrated":False}
    (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    print(json.dumps({"status":"PASS","actual_delta_assets":3,"real_TAR_members":8,
      "actual_UNITYPACKAGE_SHA256":sha(data),"source_P12_Git_blob":P12_BLOB,
      "original_W24_Git_blob":W24_BLOB,"Unity_native":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True);p.add_argument("--output",required=True)
    a=p.parse_args()
    try:build(pathlib.Path(a.repo),pathlib.Path(a.output))
    except Exception as exc:
        print("W25_DELTA_AUTHOR_RED "+str(exc),file=sys.stderr)
        raise
