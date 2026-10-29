#!/usr/bin/env python3
"""Source G0 validation: actual UnityPackage from immutable original FBX,
including independent tar decode, GUID/meta check, byte replay and negatives.
Native Unity ModelImporter and G1 deliberately NOT_RUN.
"""
import argparse
import copy
import hashlib
import io
import json
from pathlib import Path
import struct
import tarfile
import tempfile

from build_intake import assets_from_fbx,digest,guid,pack,ROOT,SOURCE_FBX_SHA256

def inspect(raw:bytes,original_assets:list[dict]):
    with tarfile.open(fileobj=io.BytesIO(raw),mode="r:gz") as tar:
        members=tar.getmembers()
        names=[m.name for m in members]
        assert len(names)==9 and len(set(names))==9,"WRONG_UNITYPACKAGE_TAR_ENTRY_COUNT"
        assert all(m.isfile() and not m.issym() and ".." not in m.name.split("/")
                   and not m.name.startswith("/") for m in members),"UNSAFE_TAR"
        contents={x.name:tar.extractfile(x).read() for x in members}
    paths=[]
    ids=[]
    for entry in original_assets:
        path=entry["path"]
        key=guid(path)
        assert key==entry["guid"]
        assert path.startswith(ROOT+"/"),"OUTSIDE_OWN_SCOPE"
        assert key not in ids
        ids.append(key);paths.append(path)
        for suffix,want in (("asset",entry["asset"]),
                            ("asset.meta",entry["meta"]),
                            ("pathname",(path+"\n").encode())):
            actual=contents[key+"/"+suffix]
            assert actual==want,("CORRUPTED_ASSET_MEMBER",path,suffix)
        assert ("guid: "+key+"\n").encode() in contents[key+"/asset.meta"]
    assert len(paths)==3 and len(set(paths))==3
    assert sum(x["kind"]=="fbx" for x in original_assets)==1
    return {"unity_assets":len(paths),"tar_members":len(names),"asset_paths":paths}

def reject(tag,raw,original):
    try:inspect(raw,original)
    except (AssertionError,KeyError,tarfile.TarError):
        return {"name":tag,"result":"EXPECTED_FAIL_CLOSED_PASS"}
    raise AssertionError("FALSE_GREEN_FOR_CORRUPT_ARCHIVE:"+tag)

def main():
    p=argparse.ArgumentParser()
    for name in ("fbx","source-receipt","reimport-receipt","built"):
        p.add_argument("--"+name,required=True)
    a=p.parse_args()
    root=Path(a.built)
    receipt=json.loads((root/"m04-unity-intake-receipt.json").read_text())
    assert receipt["producer_exact_fbx_sha256"]==SOURCE_FBX_SHA256
    original=assets_from_fbx(Path(a.fbx),Path(a.source_receipt),Path(a.reimport_receipt))
    file=root/receipt["unitypackage"]
    raw=file.read_bytes()
    assert digest(raw)==receipt["unitypackage_sha256"]
    assert file.stat().st_size==receipt["unitypackage_bytes"]
    result=inspect(raw,original)
    assert raw==pack(original),"NOT_BIT_IDENTICAL_TWO_PASSES"
    negative=[]
    corrupt=copy.deepcopy(original)
    i=next(x for x in corrupt if x["kind"]=="fbx")
    data=bytearray(i["asset"]);data[350]^=1;i["asset"]=bytes(data)
    negative.append(reject("bad-FBX-data-member",pack(corrupt),original))
    corrupt=copy.deepcopy(original)
    corrupt[0]["meta"]=corrupt[0]["meta"].replace(b"guid: ",b"guid: changed")
    negative.append(reject("changed-meta-guid",pack(corrupt),original))
    negative.append(reject("missing-editor-script",pack(original[:2]),original))
    with tempfile.TemporaryDirectory() as temp:
        bad=Path(temp)/"corrupt.fbx"
        contents=bytearray(Path(a.fbx).read_bytes());contents[350]^=1
        bad.write_bytes(contents)
        try:
            assets_from_fbx(bad,Path(a.source_receipt),Path(a.reimport_receipt))
        except AssertionError:
            negative.append({"name":"wrong-input-FBX-source-SHA",
                             "result":"EXPECTED_FAIL_CLOSED_PASS"})
        else:
            raise AssertionError("BAD_SOURCE_FBX_BYTES_NOT_REJECTED")
    report={
        "package_id":"B01-30-M04-UNITY-INTAKE-001",
        "source_fbxdigest":SOURCE_FBX_SHA256,
        "result":"REAL_UNITYPACKAGE_SOURCE_G0_PASS",
        **result,
        "bit_exact_repack":True,
        "negative_controls":negative,
        "archive_sha256":digest(raw),
        "not_run":["Unity native ModelImporter","Unity Editor actual runtime",
                   "real Unity animation curve bindings","B01-60 G1","Codex install"]
    }
    (root/"m04-unity-intake-verified.json").write_text(json.dumps(report,indent=2)+"\n")
    print("B01_30_M04_SOURCE_LOCKED_UNITYPACKAGE_TAR_QA_PASS",
          json.dumps({"members":result["tar_members"],"original_fbx":SOURCE_FBX_SHA256,
                      "negative_tests":len(negative),"stable_replay":True},sort_keys=True))
if __name__=="__main__":main()
