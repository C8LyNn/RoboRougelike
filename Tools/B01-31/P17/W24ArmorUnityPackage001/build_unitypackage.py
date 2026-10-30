#!/usr/bin/env python3
"""B01-31 W24 deterministic real Unity .unitypackage TAR/GZIP builder.
Only imports pinned actual W23 original OBJ/MTL physical source, two C# files
and their actual Git-tracked Unity meta GUIDs. Never installs on user PC.
"""
import argparse,gzip,hashlib,io,json,pathlib,tarfile,sys
ID="B01-31-W24-NATIVE-ARMOR-UNITYPACKAGE-001"
VER="P17-W24-R1"
BASE="B0131ArmorModule1x2x2"
ROOT="Assets/B01_31/W24ArmorStates"
MODELS=ROOT+"/Models"
STATES=("OK","DAMAGED","DESTROYED")
HASHES={"OK":"bb5cef04d44497d2f2bb27c7f0bacc4acda8990e504200282352c3245c17c5d3",
"DAMAGED":"6575a6eea08b68951b6ef57c86aaf9ccfdb45a0abc3ca5a229901877c1d171f7",
"DESTROYED":"32950c0a822b9f063b0f8e4792fb31dc114463e6bc53d2a1daad46ae909d34f5"}
MTL_HASH="5b72c141dac3efd9bd56c2e1d6db3933d244c6608533b04693b8d87985cadc27"
COUNTS={"OK":204,"DAMAGED":264,"DESTROYED":288}
SHA=lambda x:hashlib.sha256(x).hexdigest()
def assert_ok(x,s):
    if not x:raise ValueError("W24_UNITYPACKAGE_SOURCE_RED: "+s)
def meta_guid(path):
    return hashlib.sha256((ID+"|"+path).encode()).hexdigest()[:32]
def generated_meta(path):
    return ("fileFormatVersion: 2\nguid: "+meta_guid(path)+"\n").encode()
def original_assets(original):
    o=original/"B0131W23"
    rec=json.loads((o/"receipt.json").read_text())
    assert_ok(rec["package_id"]=="B01-31-W23-ARMOR-OBJ-IMPORT-BRIDGE-001"
              and rec["version"]=="P17-W23-R1"
              and rec["real_distribution_zip_members"]==8,
              "exact original W23 original receipt")
    listed={f["name"]:f["sha256"] for f in rec["files"]}
    out={}
    for state in STATES:
        filename=BASE+"-"+state+"-W23.obj"
        data=(o/filename).read_bytes()
        assert_ok(SHA(data)==HASHES[state]==listed[filename],"real W23 OBJ SHA "+state)
        assert_ok(sum(row.startswith(b"f ") for row in data.splitlines())==COUNTS[state],
                  "real original W23 indexed triangular geometry")
        out[MODELS+"/"+filename]=data
    filename=BASE+"-W23-shared.mtl";data=(o/filename).read_bytes()
    assert_ok(SHA(data)==MTL_HASH==listed[filename] and data.count(b"newmtl ")==6,
              "original six physical material source MTL")
    out[MODELS+"/"+filename]=data
    for filename in ("original-pbr-materials.json",
                     BASE+"-W23-iso-three-states.png",
                     BASE+"-W23-face-three-states.png"):
        data=(o/filename).read_bytes()
        assert_ok(SHA(data)==listed[filename],"source original sidecar/PNG SHA "+filename)
        out[MODELS+"/"+filename]=data
    return out
def source_assets(repo,original):
    assets=original_assets(original)
    scriptpaths=(ROOT+"/B0131W24ArmorDamageVisual.cs",
                 ROOT+"/Editor/B0131W24CreateArmorVisualPrefab.cs")
    for p in scriptpaths:
        data=(repo/p).read_bytes()
        assert_ok(len(data)>1000 and b"namespace RoboRougelike.B0131.Art" in data,
                  "original real native C# source missing "+p)
        assets[p]=data
    folders=("Assets/B01_31",ROOT,MODELS,ROOT+"/Editor")
    for p in folders:
        raw=(repo/(p+".meta")).read_bytes()
        assert_ok(b"folderAsset: yes" in raw,"real Unity folder meta missing "+p)
        assets[p]=None
    guids={}
    for p in assets:
        f=repo/(p+".meta")
        meta=f.read_bytes() if f.is_file() else generated_meta(p)
        g=[line[6:].strip() for line in meta.decode().splitlines()
           if line.startswith("guid: ")]
        assert_ok(len(g)==1 and len(g[0])==32 and
                  all(x in "0123456789abcdef" for x in g[0]),
                  "invalid Unity GUID "+p)
        assert_ok(g[0] not in guids.values(),"duplicate original source Unity GUID")
        guids[p]=g[0]
    assert_ok(len(assets)==13,"3 OBJ +1 MTL +1 PBR +2 PNG +2 C# +4 folders")
    return assets,guids
def add(tf,path,data):
    info=tarfile.TarInfo(path);info.size=len(data);info.mode=0o644
    info.uid=0;info.gid=0;info.mtime=0
    tf.addfile(info,io.BytesIO(data))
def build(repo,source,out):
    assets,guids=source_assets(repo,source)
    payload=io.BytesIO()
    with gzip.GzipFile(fileobj=payload,mode="wb",filename="",mtime=0,compresslevel=9) as gz:
        with tarfile.open(fileobj=gz,mode="w",format=tarfile.USTAR_FORMAT) as t:
            for path in sorted(assets):
                g=guids[path]
                add(t,g+"/pathname",(path+"\n").encode())
                file=repo/(path+".meta")
                add(t,g+"/asset.meta",file.read_bytes() if file.exists() else generated_meta(path))
                if assets[path] is not None:add(t,g+"/asset",assets[path])
    out.mkdir(parents=True,exist_ok=True)
    packaged=payload.getvalue()
    path=out/"B0131W24-Armor-ThreeStates-NATIVE-OBJ-Visual.unitypackage"
    path.write_bytes(packaged)
    assert_ok(path.read_bytes()==packaged and packaged.startswith(b"\x1f\x8b"),
              "actual physical .unitypackage gzip/tar")
    receipt={"package_id":ID,"version":VER,
       "actual_unitypackage_SHA256":SHA(packaged),"actual_unitypackage_bytes":len(packaged),
       "original_immutable_W23_OBJ_SHA256":HASHES,
       "original_immutable_W23_MTL_SHA256":MTL_HASH,
       "real_armor_triangles":COUNTS,
       "asset_counts":{"folders":4,"obj":3,"mtl":1,"pbr_json":1,
                       "png_review_boards":2,"native_CSharp":2},
       "real_Unity_asset_paths_to_GUIDs":dict(sorted(guids.items())),
       "real_asset_members":[{"path":p,"sha256":SHA(data) if data is not None else None,
                              "bytes":len(data) if data is not None else 0}
                             for p,data in sorted(assets.items())],
       "visual_only":True,"no_gameplay_authority":True,
       "native_Unity_G1":"NOT_RUN","formal_G0":"PENDING",
       "G2":"HOLD","codex_ACK":False,"integrated":False}
    (out/"receipt.json").write_text(json.dumps(receipt,sort_keys=True,indent=2)+"\n")
    print(json.dumps({"status":"PASS","actual_Unity_assets":len(assets),
      "real_origin_OBJ_triangles":COUNTS,"native_Unity_CSharp_sources":2,
      "real_unique_GUIDs":len(set(guids.values())),
      "real_UNITYPACKAGE_SHA256":SHA(packaged),"Unity_native":"NOT_RUN"}))
if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--repo",required=True);p.add_argument("--original",required=True)
    p.add_argument("--output",required=True);a=p.parse_args()
    try:build(pathlib.Path(a.repo),pathlib.Path(a.original),pathlib.Path(a.output))
    except Exception as e:
        print("W24_UNITYPACKAGE_BUILD_RED "+type(e).__name__+": "+str(e),file=sys.stderr)
        raise
