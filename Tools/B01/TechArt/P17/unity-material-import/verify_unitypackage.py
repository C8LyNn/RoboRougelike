#!/usr/bin/env python3
"""Verify actual MAT-IMPORT-001 UnityPackage bytes against the frozen original
MAT-A1-001 source and an independent tar/manifest reader. No Unity G1 claims."""
import argparse
import copy
import hashlib
import io
import json
from pathlib import Path
import tarfile

from build_unitypackage import (
    PACKAGE_ID, ROOT, PROFILES, MAPS, build_unitypackage,
    package_assets, stable_guid
)


def check_package(raw, expected):
    with tarfile.open(fileobj=io.BytesIO(raw), mode="r:gz") as tar:
        members=tar.getmembers()
        if len(members)!=len(expected)*3:
            raise AssertionError("MISSING_OR_EXTRA_UNITY_PACKAGE_ENTRIES")
        names=[m.name for m in members]
        if len(set(names)) != len(names):
            raise AssertionError("DUPLICATE_TAR_ENTRY")
        if not all(m.isfile() and not m.name.startswith("/") and ".." not in m.name.split("/") for m in members):
            raise AssertionError("UNSAFE_TAR_PATH")
        d={m.name:tar.extractfile(m).read() for m in members}
    paths=set()
    guids=set()
    for item in expected:
        path=item["path"]
        guid=stable_guid(path)
        if guid in guids or path in paths:
            raise AssertionError("DUPLICATE_GUID_OR_PATH")
        guids.add(guid)
        paths.add(path)
        keys={guid+"/asset",guid+"/asset.meta",guid+"/pathname"}
        if not keys.issubset(d):
            raise AssertionError("MISSING_IMPORTABLE_UNITY_ASSET")
        if d[guid+"/asset"]!=item["bytes"]:
            raise AssertionError("CHANGED_ORIGINAL_ASSET_BYTES")
        if d[guid+"/asset.meta"]!=item["meta"]:
            raise AssertionError("WRONG_IMPORTER_META_OR_GUID")
        if d[guid+"/pathname"]!=(path+"\n").encode():
            raise AssertionError("BROKEN_UNITY_PATHNAME")
        if ("guid: "+guid).encode() not in d[guid+"/asset.meta"]:
            raise AssertionError("MISSING_STABLE_GUID")
        if path.endswith(".png"):
            if b"TextureImporter:" not in item["meta"]:
                raise AssertionError("TEXTURE_META_MISSING")
            if "normal.png" in path and b"textureType: 1" not in item["meta"]:
                raise AssertionError("BAD_NORMAL_IMPORT")
            if "basecolor.png" in path and b"sRGBTexture: 1" not in item["meta"]:
                raise AssertionError("BAD_COLORSPACE")
            if ("normal.png" not in path and "basecolor.png" not in path
                    and b"sRGBTexture: 0" not in item["meta"]):
                raise AssertionError("BAD_LINEAR_DATA_TEXTURE")
    if len(guids)!=10 or len(d)!=30:
        raise AssertionError("WRONG_ASSET_COUNT")
    if len([x for x in paths if x.endswith(".png")]) != 8:
        raise AssertionError("WRONG_PBR_TEXTURE_COUNT")
    if not all(s.startswith(ROOT+"/") for s in paths):
        raise AssertionError("OUTSIDE_OWNED_ASSETS_DIRECTORY")
    return {"asset_paths":sorted(paths),"stable_guids":sorted(guids),"asset_count":len(paths)}


def must_reject(case, raw, originals):
    try:
        check_package(raw, originals)
    except (AssertionError, tarfile.TarError):
        return {"case":case,"result":"EXPECTED_REJECTION_PASS"}
    raise AssertionError("FALSE_GREEN_ON_MUTATED_UNITYPACKAGE:"+case)


def main():
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--built",required=True)
    args=p.parse_args()
    out=Path(args.built)
    receipt=json.loads((out/"MAT-IMPORT-001-receipt.json").read_text())
    path=out/receipt["file"]
    raw=path.read_bytes()
    source=Path(__file__).with_name("MatA1Installer.cs")
    assets=package_assets(Path(args.original),source)
    assert receipt["package_id"]==PACKAGE_ID
    assert receipt["assets_count"]==10
    assert hashlib.sha256(raw).hexdigest()==receipt["package_sha256"]
    proof=check_package(raw,assets)
    assert build_unitypackage(assets)==raw, "NONDETERMINISTIC_PACKAGE"
    # One actual texel-data corruption
    damaged=copy.deepcopy(assets)
    first=next(x for x in damaged if x["path"].endswith("_basecolor.png"))
    mut=bytearray(first["bytes"])
    mut[75] ^= 1
    first["bytes"]=bytes(mut)
    # Archive a corrupted asset but compare against the ORIGINAL canonical source
    negatives=[must_reject("actual-original-PNG-payload-corruption",
                           build_unitypackage(damaged),assets)]
    # Fake .meta GUID must not remain silently accepted
    damaged=copy.deepcopy(assets)
    damaged[0]["meta"]=damaged[0]["meta"].replace(b"guid: ",b"guid: f")
    negatives.append(must_reject("wrong-Unity-meta-guid",
                                 build_unitypackage(damaged),assets))
    negatives.append(must_reject("missing-real-PNG-entry",
                                 build_unitypackage(assets[1:]),assets))
    report={
        "package_id":PACKAGE_ID,"result":"PASS",
        "actual_importable_tar_entries":len(assets)*3,
        "actually_included_PNG":8,
        "original_authoritative_source_png_byte_match":True,
        "deterministic_package_bitidentical_replay":True,
        "negative_controls":negatives,
        "sha256":receipt["package_sha256"],
        "paths":proof["asset_paths"],
        "not_run":["Native Unity importer","Unity URP Lit shader render",
                   "Importer C# compiled by Unity","B01-60 independent QA","G2 integration"]
    }
    (out/"MAT-IMPORT-001-verified.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("B01_30_MAT_IMPORT_REAL_BINARY_TAR_QA_PASS",
          json.dumps({"pngs":8,"assets":len(assets),"tar_entries":len(assets)*3,
                      "expected_negative_rejections":len(negatives),
                      "package_sha256":receipt["package_sha256"]},sort_keys=True))


if __name__=="__main__":
    main()
