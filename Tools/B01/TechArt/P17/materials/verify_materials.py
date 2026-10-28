#!/usr/bin/env python3
"""Independent original PNG-level QA for B01-30 MAT-A1-001.

Inspects genuine files, PNG decoded pixels, repeat seams, URP channel contract,
generator determinism and a synthetic invalid-metallic negative case.
Not a Unity renderer/physically approved PBR validation.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import sys

from PIL import Image, ImageStat

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_materials as gm


def check_profile(profile: str, images: dict[str, Image.Image], size: int) -> list[dict]:
    proof = []
    def require(label: str, condition: bool) -> None:
        proof.append({"name":f"{profile}/{label}", "result":"PASS" if condition else "FAIL"})
        if not condition:
            raise AssertionError(f"{profile}/{label}")

    require("four-real-RGBA-maps",
            set(images)==set(gm.MAP_NAMES) and all(im.mode=="RGBA" and im.size==(size,size) for im in images.values()))
    base=images["basecolor"]
    norm=images["normal"]
    metal=images["metallic_smoothness"]
    ao=images["occlusion"]
    # Pixel content, no fake surrogate JSON-only mask checks.
    bases=list(base.getdata())
    normals=list(norm.getdata())
    metals=list(metal.getdata())
    occlusions=list(ao.getdata())
    metal_value=0 if profile=="tire_rubber" else 219
    require("URP-metallic-R-and-GB0",
            all(c[0]==metal_value and c[1]==0 and c[2]==0 for c in metals))
    smooth=[c[3] for c in metals]
    require("URP-smoothness-alpha-valid",
            (25<=min(smooth)<=max(smooth)<=65) if profile=="tire_rubber" else
            (147<=min(smooth)<=max(smooth)<=207))
    require("URP-AO-G-valid",
            all(220<=c[1]<=255 and c[0]==c[1]==c[2] and c[3]==255 for c in occlusions))
    require("opaque-RGB-albedo", all(p[3]==255 and 15<=min(p[:3]) and max(p[:3])<=180 for p in bases))
    require("nonflat-procedural-source-color", max(c[0] for c in bases)-min(c[0] for c in bases)>=7)
    require("nonflat-normal-and-smoothness",
            max(c[0] for c in normals)-min(c[0] for c in normals)>6
            and max(c[1] for c in normals)-min(c[1] for c in normals)>6
            and max(smooth)-min(smooth)>=5)
    # Encoded tangent-space normals must remain unit length (quantization epsilon).
    lengths=[]
    for red,green,blue,_ in normals[::max(1,size//16)]:
        xyz=[(v/127.5)-1.0 for v in (red,green,blue)]
        lengths.append(sum(v*v for v in xyz)**0.5)
    require("normal-length-unit-decoded", all(abs(n-1.0)<0.025 for n in lengths))
    require("normal-Z-points-outward", min(p[2] for p in normals)>170)
    # One-sided seam on a tile must join its opposite side closely.
    # This measures *actual* adjacent texels under Repeat, not matching
    # first/last pixels (which would duplicate a scanline incorrectly).
    seam = {}
    for kind,im in images.items():
        px=im.load()
        diffs=[]
        for n in range(size):
            left,right=px[0,n],px[size-1,n]
            top,bottom=px[n,0],px[n,size-1]
            channels=(0,1,2) if kind!="metallic_smoothness" else (0,3)
            diffs.extend(abs(left[c]-right[c]) for c in channels)
            diffs.extend(abs(top[c]-bottom[c]) for c in channels)
        mean=sum(diffs)/len(diffs)
        seam[kind]=round(mean,4)
        require(f"repeat-neighbor-seam:{kind}", mean<=13.0 if kind!="normal" else mean<=24.0)
    proof.append({"name":f"{profile}/measured-seam-mean", "result":"PASS", "values":seam})
    return proof


def main() -> int:
    p=argparse.ArgumentParser()
    p.add_argument("--input", default="build/B01-30/MAT-A1-001")
    args=p.parse_args()
    root=Path(args.input)
    manifest=json.loads((root/"material-manifest.json").read_text())
    size=manifest["size"]
    assert manifest["package_id"]=="B01-30-MAT-A1-001" and manifest["version"]=="R1"
    assert set(manifest["profile_names"])==set(gm.PROFILE_NAMES)
    receipts=[]
    sha_set=set()
    for entry in manifest["textures"]:
        path=root/entry["path"]
        assert path.is_file() and path.stat().st_size==entry["bytes"]
        actual_hash=hashlib.sha256(path.read_bytes()).hexdigest()
        assert actual_hash==entry["sha256"], ("MANIFEST_SHA_MISMATCH",str(path))
        assert actual_hash not in sha_set,("DUPLICATE_TEXTUReS",str(path))
        sha_set.add(actual_hash)
    for profile in gm.PROFILE_NAMES:
        source=gm.produce(profile,size)  # full independent regeneration, not just header.
        images={}
        for kind in gm.MAP_NAMES:
            target=root/profile/f"{profile}_{kind}.png"
            assert target.read_bytes()==source[kind],("SOURCE_NONDETERMINISM",str(target))
            with Image.open(target) as img:
                images[kind]=img.convert("RGBA")
        receipts.extend(check_profile(profile,images,size))
        # Deliberate negative control: mutate one REAL decoded pixel to non-URP
        # metallic code, and prove the very same verification rejects it.
        bad={name:im.copy() for name,im in images.items()}
        bad["metallic_smoothness"].putpixel((size//2,size//2),(127,99,5,255))
        negative_rejected=False
        try:
            check_profile(profile,bad,size)
        except AssertionError:
            negative_rejected=True
        assert negative_rejected,("FALSE_GREEN_SURVIVED_MUTATION",profile)
        receipts.append({"name":f"{profile}/synthetic-bad-packed-metallic-expected-REJECTION","result":"PASS"})
    with Image.open(root/"MAT-A1-001-map-contact-sheet.png") as sheet:
        assert sheet.size[0]>1000 and sheet.size[1]>600 and max(ImageStat.Stat(sheet.convert("RGB")).stddev)>25
    receipts.append({"name":"actual-nonflat-material-map-contact","result":"PASS"})
    report={"package_id":"B01-30-MAT-A1-001", "result":"PASS",
            "checks_passed":sum(p["result"]=="PASS" for p in receipts),
            "checks_failed":sum(p["result"]=="FAIL" for p in receipts),
            "verified_original_PNG_count":len(gm.PROFILE_NAMES)*len(gm.MAP_NAMES),
            "proof":receipts,"manifest_sha256":hashlib.sha256((root/"material-manifest.json").read_bytes()).hexdigest(),
            "not_run":["Unity 6 URP material visualization","Mesh UV and collider","G1","Independent B01-60 QA","Approved MODEL_JOB"]}
    (root/"verification-receipt.json").write_text(json.dumps(report,indent=2)+"\n")
    print("B01_30_MAT_A1_ORIGINAL_PNG_QA_PASS",json.dumps({
        "profiles":len(gm.PROFILE_NAMES),"original_pngs":report["verified_original_PNG_count"],
        "checks":report["checks_passed"],"negative_tests_rejected":len(gm.PROFILE_NAMES),
        "receipt":str(root/"verification-receipt.json")},sort_keys=True))
    return 0


if __name__=="__main__":
    raise SystemExit(main())
