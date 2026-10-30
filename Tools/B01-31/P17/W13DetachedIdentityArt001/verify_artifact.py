#!/usr/bin/env python3
"""Independent read-only byte/pixel QA of B01-31 synthetic ID-art export.
Does NOT invoke producer, Unity Editor, real snapshots or W06/#510 gameplay.
"""
from __future__ import annotations
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import sys
import zipfile
from PIL import Image

ROOT="Assets/B0131DetachedIdentityMarkers"
PACKAGE="B01-31-DETACHED-IDENTITY-MARKERS-001"
H=lambda b:hashlib.sha256(b).hexdigest()

def check(ok,reason):
    if not ok:raise ValueError("B0131_ID_VISUAL_BINARY_RECEIVER_REJECT "+reason)

def inspect(original,output):
    fbytes=original.read_bytes()
    data=json.loads(fbytes)
    receipt=json.loads((output/"receipt.json").read_bytes())
    check(receipt["package_id"]==PACKAGE and receipt["version"]=="P17-W13-R1",
          "package ID/version")
    check(receipt["fixture_sha256"]==H(fbytes) and
          data["schema"]=="b01-31-visual-fixture-1",
          "authentic immutable producer fixture")
    check("NOT a Save/Load format" in data["warning"]
          and "not a real Save/Load round trip" in receipt["warning"],
          "non-authoritative synthetic Save/Load warning")
    before=data["pre_save"]
    after=data["post_load_simulated"]
    check(len(before)==16 and len(set(before))==16 and len(after)==16
          and before!=after and set(before)==set(after),
          "input phase identity and actual reordered set")
    check(receipt["pre_save_order"]==before and
          receipt["post_load_simulated_order"]==after,
          "output source/provenance expected ordering")
    originals=receipt["original_visual_marker_assets"]
    check(len(originals)==16 and [x["id"] for x in originals]==before,
          "16 original distinct art labels in fixture order")
    expected_sig=set()
    expected_paths=set()
    blobs={}
    for idx,entry in enumerate(originals):
        check(entry["file"]==f"marker_{idx:02d}.png",
              "no filename from input ID")
        check(entry["contrast_dark_ge18"]>2000 and
              entry["contrast_light_ge18"]>2000,
              "actually visible original sprite on both backgrounds")
        body=(output/"sprites"/entry["file"]).read_bytes()
        check(H(body)==entry["png_sha256"] and len(body)==entry["bytes"],
              "real source icon PNG hash/bytes mismatch")
        with Image.open(io.BytesIO(body)) as im:
            im.load()
            check(im.format=="PNG" and im.mode=="RGBA" and im.size==(256,256),
                  "actual RGBA 256 sprite pixel format")
            check(im.getpixel((0,0))[3]==0 and
                  im.getpixel((255,255))[3]==0 and
                  im.getchannel("A").getbbox() is not None,
                  "actual transparent corners and nonblank art")
            # Independently decode 5x3 visible cells, without calling
            # the producer's hash->glyph generator.
            pattern=[]
            for row in range(5):
                for col in range(3):
                    alpha=im.getpixel((87+col*17+7,82+row*17+7))[3]
                    pattern.append("1" if alpha>230 else "0")
            check("".join(pattern)==entry["pattern_signature"],
                  "real pixel glyph differs from machine receipt")
        blobs[entry["id"]]=body
        expected_paths.add(entry["file"])
        expected_sig.add(entry["pattern_signature"])
    check(len(blobs)==16 and len({H(x) for x in blobs.values()})==16
          and len(expected_sig)==16 and receipt["pattern_signatures_unique"]==16,
          "16 unique pixel-identifiable original signatures")
    check(receipt["stable_visual_id_bytes_pass"]==16 and
          receipt["dark_light_actual_contrast_cases"]==32 and
          receipt["negative_fixture_mutants_rejected"]==7,
          "generator source execution/negative controls")
    # Build a real receiver projection of both fixture-order phases onto the
    # same source art bytes (never a gameplay load assertion).
    before_projection={part_id:H(blobs[part_id]) for part_id in before}
    after_projection={part_id:H(blobs[part_id]) for part_id in after}
    check(before_projection==after_projection,
          "identity-keyed fixture order permutation changed sprite bytes")
    check(len(receipt["review_png_atlases"])==3,
          "exact original physical source research boards")
    for item in receipt["review_png_atlases"]:
        filename=item["name"]
        check(filename in {
          "BEFORE_SAVE_SYNTHETIC.png","AFTER_LOAD_SIMULATED.png",
          "AFTER_LOAD_SIMULATED_LIGHT.png"
        },"unexpected diagnostic board name")
        b=(output/filename).read_bytes()
        check(H(b)==item["sha256"] and len(b)==item["bytes"],
              "actual board PNG hashed bytes")
        with Image.open(io.BytesIO(b)) as image:
            image.load()
            check(image.format=="PNG" and image.mode=="RGB"
                  and image.size==(1400,1480)
                  and list(image.size)==item["dimensions"],
                  "actual 16-card visual board readback")
    inner=(output/"B0131-DetachedIdentityMarkers-UnitySprites.zip").read_bytes()
    check(H(inner)==receipt["actual_unity_sprite_zip_sha256"] and
          len(inner)==receipt["actual_unity_sprite_zip_bytes"],
          "actual inner Unity Sprite ZIP SHA/size")
    with zipfile.ZipFile(io.BytesIO(inner)) as z:
        names=z.namelist()
        check(z.testzip() is None and len(names)==34 and len(set(names))==34,
              "actual Unity ZIP CRC and 34 real members")
        check(len({x.casefold() for x in names})==34
              and all(x==ROOT+".meta" or x.startswith(ROOT+"/") for x in names),
              "Unity Windows path conflict or foreign owner")
        metas=[x for x in names if x.endswith(".meta")]
        check(len(metas)==18 and receipt["unique_unity_guids"]==18,
              "18 original Unity folder/sprite import metadata files")
        seen=set()
        for name in metas:
            txt=z.read(name).decode("utf8")
            m=re.search(r"(?m)^guid: ([a-f0-9]{32})\s*$",txt)
            check(m is not None and m.group(1) not in seen,
                  "missing/duplicate real Unity GUID")
            seen.add(m.group(1))
            if name.endswith(".png.meta"):
                check("textureType: 8" in txt and
                      "alphaIsTransparency: 1" in txt,
                      "real Unity Sprite alpha importer settings")
        for idx,entry in enumerate(originals):
            p=f"{ROOT}/Sprites/marker_{idx:02d}.png"
            check(p in names and p+".meta" in names
                  and z.read(p)==blobs[entry["id"]],
                  "actual Unity ZIP sprite byte equality to original PNG")
    check(H(inner+b"x")!=receipt["actual_unity_sprite_zip_sha256"],
          "tampered Unity Sprite ZIP must change digest")
    check(receipt["unity_editor_runtime"]=="NOT_RUN" and
          receipt["real_save_load_runtime"]=="NOT_RUN" and
          receipt["selected_reattach_pr"]==510 and
          receipt["selected_reattach_sha"]=="d94400d95d13db7f27e1533df05c9292d5380431",
          "must preserve actual gameplay and source QA gate distinction")
    print(json.dumps({
      "status":"PASS","actual_rgba_sprites":16,
      "actual_review_pngs":3,"pixel_decoded_unique_ID_shapes":16,
      "synthetic_order_independent_byte_parity":16,
      "real_unity_zip_members":34,"real_unique_unity_guid":18,
      "inner_unity_zip_sha256":H(inner),
      "native_unity":"NOT_RUN","real_save_load":"NOT_RUN"
    }))
    return 0

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("--fixture",required=True)
    p.add_argument("--source",required=True)
    args=p.parse_args()
    try:sys.exit(inspect(Path(args.fixture),Path(args.source)))
    except Exception as e:
        print("B0131_ID_RECEIVER_RED "+str(e),file=sys.stderr)
        raise
