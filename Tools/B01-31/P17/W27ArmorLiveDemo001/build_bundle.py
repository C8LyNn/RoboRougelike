#!/usr/bin/env python3
"""W27 actual real Unity native test/demo DELTA .unitypackage.
Exact original P12/W24/W25 Git blob ABI, 8 new-only assets, unique GUID,
20 physically serialized Unity tar entries. No asset writes to Codex/game.
"""
from __future__ import annotations
import argparse,gzip,hashlib,io,json,pathlib,tarfile,sys
ROOT="Assets/B01_31/W27ArmorLiveDemo"
PACKAGE="B01-31-W27-REAL-P12-ARMOR-PLAYMODE-DEMO-001"
BASES={
"Assets/Pack/P12_PipelineSafeDamageable.cs":"544bed274b616b91fd98c45d2b194b2f3a2d112a",
"Assets/B01_31/W24ArmorStates/B0131W24ArmorDamageVisual.cs":"8cf3664b9b6e5b0fd6623be7c3f4e72fd9164664",
"Assets/B01_31/W25P12DamageVisualBinding/B0131W25P12DamageVisualBinding.cs":"e1622abf22319e4448b53a0630d40387ade7a2b2"
}
ASSETS=(
ROOT,ROOT+"/Editor",ROOT+"/Tests",ROOT+"/Tests/PlayMode",
ROOT+"/B0131W27ArmorLiveDemoDriver.cs",
ROOT+"/Editor/B0131W27CreateArmorLiveDemoScene.cs",
ROOT+"/Tests/PlayMode/B0131W27ArmorRealP12PlayModeTests.cs",
ROOT+"/Tests/PlayMode/RoboRougelike.B0131.W27.NativePlayModeTests.asmdef",
)
sha=lambda b:hashlib.sha256(b).hexdigest()
def blob(b):return hashlib.sha1(b"blob "+str(len(b)).encode()+b"\0"+b).hexdigest()
def check(v,msg):
    if not v:raise ValueError("W27_PACKAGE_AUTHOR_RED: "+msg)
def taradd(tf,path,content):
    info=tarfile.TarInfo(path);info.size=len(content);info.uid=0;info.gid=0;info.mtime=0
    info.mode=0o644;tf.addfile(info,io.BytesIO(content))
def build(repo,out):
    for path,digest in BASES.items():
        check(blob((repo/path).read_bytes())==digest,
              "original P12 W24 W25 immutable source ABI changed "+path)
    assets={};used=set()
    for path in ASSETS:
        meta=(repo/(path+".meta")).read_bytes()
        keys=[x[6:].strip() for x in meta.decode().splitlines() if x.startswith("guid: ")]
        check(len(keys)==1 and len(keys[0])==32 and
              all(x in "0123456789abcdef" for x in keys[0]) and
              keys[0] not in used,"actual unique Unity GUID "+path)
        used.add(keys[0]);data=None if path in ASSETS[:4] else (repo/path).read_bytes()
        check(data is None or len(data)>120,"actual original source asset "+path)
        check((b"folderAsset: yes" in meta)==(data is None),"Unity folder .meta type")
        assets[path]={"meta":meta,"guid":keys[0],"bytes":data}
    check(len(assets)==8 and len(used)==8,"exact W27 own 8 paths, no overlap W24/25")
    raw=io.BytesIO()
    with gzip.GzipFile(fileobj=raw,mode="wb",filename="",mtime=0,compresslevel=9) as gz:
        with tarfile.open(fileobj=gz,mode="w",format=tarfile.USTAR_FORMAT) as tf:
            for path,a in sorted(assets.items()):
                guid=a["guid"];taradd(tf,guid+"/pathname",(path+"\n").encode())
                taradd(tf,guid+"/asset.meta",a["meta"])
                if a["bytes"] is not None:taradd(tf,guid+"/asset",a["bytes"])
    out.mkdir(parents=True,exist_ok=True)
    dest=out/"B0131W27-P12-Armor-LiveDemo-PlayMode-DELTA.unitypackage"
    data=raw.getvalue();dest.write_bytes(data)
    receipt={"package_id":PACKAGE,"version":"P17-W27-R1",
       "w25_parent":"dfff0aeaa7098a16621387557a04836bf873fd73",
       "exact_original_source_Git_blobs":BASES,
       "real_unitypackage_sha256":sha(data),"real_unitypackage_bytes":len(data),
       "new_only_unity_assets":8,"actual_tar_members":20,
       "native_unity_PlayMode_test_cases_written":5,"native_unity_G1":"NOT_RUN",
       "codex_ack":False,"formal_G0":"PENDING","integrated":False,
       "G2":"HOLD","no_original_game_source_mutation":True,
       "source_member_bytes_and_GUID":[
          {"path":p,"guid":a["guid"],"sha256":sha(a["bytes"]) if a["bytes"] else None,
           "bytes":len(a["bytes"]) if a["bytes"] else 0,
           "meta_sha256":sha(a["meta"])}
          for p,a in sorted(assets.items())]}
    (out/"receipt.json").write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n",encoding="utf-8")
    print(json.dumps({"status":"PASS","Unity_original_8_GUID_assets":len(assets),
       "actual_native_test_cases_source_only":5,"real_TAR_members":20,
       "real_unitypackage_SHA256":sha(data),"native_Unity_G1":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--repo",required=True);p.add_argument("--out",required=True)
    a=p.parse_args()
    try:build(pathlib.Path(a.repo),pathlib.Path(a.out))
    except Exception as e:print("W27_AUTHOR_RED "+str(e),file=sys.stderr);raise
