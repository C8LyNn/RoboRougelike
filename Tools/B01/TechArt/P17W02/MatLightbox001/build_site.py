#!/usr/bin/env python3
"""B01-30 MAT-LIGHTBOX-001: physically build an offline WebGL preview site
using eight REAL, SHA-pinned original MAT-A1-001 generated PNG images.
This does not modify the original producer, Unity scenes, models or prefabs.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import shutil

FAMILIES=("tire_rubber","brushed_arm_alloy")
MAPS=("basecolor","normal","metallic_smoothness","occlusion")
INPUT_SHA="171983f85ff41df6ef33b95939cc877ba68e03ca"
INPUT_GENERATOR_BLOB="171d53bc7d5a4840a067af5c934a8fd1c7ad774e"
ROOT=Path(__file__).resolve().parent

def sha(data:bytes)->str:
    return hashlib.sha256(data).hexdigest()

def package(original:Path,output:Path)->dict:
    manifest=json.loads((original/"material-manifest.json").read_text(encoding="utf-8"))
    assert manifest["package_id"]=="B01-30-MAT-A1-001"
    assert manifest["version"]=="R1" and manifest["size"]==512
    pinned={x["path"]:x for x in manifest["textures"]}
    assert len(pinned)==9
    output.mkdir(parents=True,exist_ok=True)
    files=[]
    for family in FAMILIES:
        for kind in MAPS:
            rel=f"{family}/{family}_{kind}.png"
            raw=(original/rel).read_bytes()
            assert raw[:8]==bytes.fromhex("89504e470d0a1a0a")
            assert sha(raw)==pinned[rel]["sha256"]
            assert len(raw)==pinned[rel]["bytes"]
            dest=output/"textures"/rel
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(raw)
            files.append({"path":str(dest.relative_to(output)).replace("\\","/"),
                          "sha256":sha(raw),"bytes":len(raw),"source_path":rel})
    for name in ("index.html","viewer.js"):
        raw=(ROOT/name).read_bytes()
        (output/name).write_bytes(raw)
        files.append({"path":name,"sha256":sha(raw),"bytes":len(raw),"source_path":name})
    receipt={
        "package_id":"B01-30-MAT-LIGHTBOX-001","version":"R1",
        "source_owner_package":"B01-30-MAT-A1-001/R1",
        "source_exact_commit":INPUT_SHA,"source_generator_git_blob":INPUT_GENERATOR_BLOB,
        "independent_material_profiles":list(FAMILIES),
        "texture_files_count":8,"preview":"index.html",
        "render_requirement":"WebGL2 true GPU/SwiftShader browser, HTTP local origin",
        "actual_png_source_pinned":True,"files":files,
        "status":"BUILT_SOURCE_NOT_NATIVE_UNITY",
        "not_run":["Unity6000.3.14f1 URP Lit","real module UV and texel density",
                   "native Unity material import","B01-60 independent QA","G1/G2"]
    }
    (output/"site-receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
    print("MAT_LIGHTBOX_REAL_SITE_BUILT",
          json.dumps({"files":len(files),"textures":len(files)-2,
                      "receipt":str(output/"site-receipt.json")},sort_keys=True))
    return receipt

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--original",required=True)
    p.add_argument("--out",required=True)
    a=p.parse_args()
    package(Path(a.original),Path(a.out))
