#!/usr/bin/env python3
"""Rasterize W17 concept research SVGs and verify basic source/visible-image contracts.

This is a research-only visual artifact check. It neither loads Unity nor validates
physical attachment, camera occlusion, collisions, suspension, movement or playability.
"""
from __future__ import annotations
from pathlib import Path
import shutil
from io import BytesIO
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
import cairosvg
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageChops

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered"
SOURCE = ["comparison.svg", "A-wheel.svg", "B-track.svg", "C-omni.svg"]
EXPECTED = {"comparison.svg": (2100,974), "A-wheel.svg": (700,866),
            "B-track.svg": (700,866), "C-omni.svg": (700,866)}
SVGNS = "{http://www.w3.org/2000/svg}"
# Actual raw illustration area from the artist-authored 700x866 cards.
# Avoid: header/title above y260; cube reference below-left x130; footer > y520.
# This is a standardized *comparison frame*, not a verified in-game camera view.
SILHOUETTE_CROP = (142, 264, 620, 529)
SILHOUETTE_FILES = ["A-wheel.svg", "B-track.svg", "C-omni.svg"]
SILHOUETTE_COLORS = ("#43cddc", "#efb66a", "#b6a0fa")
# Published B01-11 study protocol #292 comment 6447607722. Do not show
# the private mapping key or source filenames to the participant.
BLIND_ORDERS = (
    ("P01",  64, "ABC"),
    ("P02",  64, "BCA"),
    ("P03",  64, "CAB"),
    ("P04",  64, "ACB"),
    ("P05", 128, "BAC"),
    ("P06", 128, "CBA"),
    ("P07", 128, "ABC"),
    ("P08", 128, "BCA"),
)
BLIND_CONCEPT_FILE = {"A": "A-wheel.svg", "B": "B-track.svg", "C": "C-omni.svg"}
checks = []
files = []

def strip_reference_overlay(source_svg: bytes) -> bytes:
    """Remove ONLY the decorative reference-cube group from in-memory SVG for blind tests.

    The original SVG and full technical cards remain untouched. The reference cube
    and scale labels must still be present on original full annotated art boards.
    Fail closed if exactly one group containing both two expected direct labels
    cannot be found. This protects against accidentally stripping a vehicle group.
    """
    root = ET.fromstring(source_svg)
    matched = []
    for parent in root.iter():
        for child in list(parent):
            if child.tag != SVGNS+"g":
                continue
            labels = [(node.text or "").strip()
                      for node in list(child) if node.tag == SVGNS+"text"]
            if labels == ["0.55 m", "GRID REFERENCE ONLY"]:
                matched.append((parent,child))
    if len(matched) != 1:
        raise ValueError(f"expected exactly one reference overlay, found {len(matched)}")
    matched[0][0].remove(matched[0][1])
    remaining = "".join(root.itertext())
    if "GRID REFERENCE ONLY" in remaining or "0.55 m" in remaining:
        raise ValueError("reference label remains after exact overlay removal")
    return ET.tostring(root,encoding="utf-8",xml_declaration=True)


def check(name: str, ok: bool, detail: str):
    checks.append({"name":name,"status":"PASS" if ok else "FAIL","detail":detail})
    print(f"{'PASS' if ok else 'FAIL'}: {name}: {detail}")

def record(path: Path):
    data = path.read_bytes()
    files.append({"path": str(path.relative_to(HERE)), "bytes":len(data),
                  "sha256":hashlib.sha256(data).hexdigest()})

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    tile_samples = []
    for file in SOURCE:
        raw = (HERE / file).read_bytes()
        try:
            tree = ET.fromstring(raw)
            valid = tree.tag == SVGNS+"svg"
            dims = (int(tree.attrib["width"]),int(tree.attrib["height"]))
            text = "".join(tree.itertext())
            shapes = sum(1 for elem in tree.iter() if elem.tag in
                         (SVGNS+"polygon",SVGNS+"path",SVGNS+"circle",SVGNS+"ellipse"))
            check(file+"-xml", valid, f"parsed root SVG; {shapes} vector primitives")
            check(file+"-reference-and-unknown",
                  ("0.55 m" in text and "SOCKET UNKNOWN" in text and
                   "HITBOX UNKNOWN" in text and "MASS UNKNOWN" in text and
                   "CLEARANCE UNKNOWN" in text), "0.55 m and all four UNKNOWN disclosures")
            check(file+"-dimensions",dims==EXPECTED[file],str(dims))
            check(file+"-original-shape-density",shapes>=25,f"{shapes} vector primitives")
        except Exception as err:
            check(file+"-xml",False,str(err))
            continue
        try:
            dest = OUT / file.replace(".svg",".png")
            cairosvg.svg2png(bytestring=raw,write_to=str(dest))
            with Image.open(dest) as im:
                im.load()
                px = im.convert("RGB")
                sample = [px.getpixel((x,y)) for x,y in
                          ((im.width//2,im.height//2),
                           (im.width//4,im.height//2),
                           (3*im.width//4,im.height//2),
                           (im.width//2,im.height//4))]
                check(file+"-raster-encoded",
                      im.format=="PNG" and im.size==EXPECTED[file],
                      f"real CairoSVG->PNG {im.size}; sampled pixel colors {sample}")
                # Distinct dimensions and output are real raster results, not readability pass.
                for sz in (128,64):
                    thumb=OUT / file.replace(".svg",f"-{sz}px.png")
                    newh=max(1,round(im.height*sz/im.width))
                    px.resize((sz,newh), Image.Resampling.LANCZOS).save(thumb)
                    record(thumb)
            record(dest)
            if file in SILHOUETTE_FILES:
                # W26 PASS3 defect: the old subject-only crop still contained
                # the "0.55 m / GRID REFERENCE ONLY" label at global x167/y507
                # (the SVG card itself is translated by +27,+53).
                # For blind recognition, render a reference-free ephemeral SVG.
                clean_svg = strip_reference_overlay(raw)
                check(file+"-source-reference-detached",
                      b"GRID REFERENCE ONLY" not in clean_svg and b"0.55 m" not in clean_svg,
                      "decorative scale legend removed from in-memory blind-test SVG only")
                subject_render = Image.open(BytesIO(cairosvg.svg2png(bytestring=clean_svg)))
                subject_render.load()
                reference_free = subject_render.convert("RGB")
                # A real pixel regression: prove the label area changed while a
                # distant representative portion of the vehicle remains exact.
                legend_region = (155,495,340,532)
                vehicle_region = (330,280,610,480)
                legend_changed = ImageChops.difference(
                    px.crop(legend_region),reference_free.crop(legend_region)).getbbox() is not None
                same_vehicle = ImageChops.difference(
                    px.crop(vehicle_region),reference_free.crop(vehicle_region)).getbbox() is None
                check(file+"-legend-pixels-removed",legend_changed,
                      f"source-vs-blind PNG nonidentical within label region {legend_region}")
                check(file+"-vehicle-pixels-identical",same_vehicle,
                      f"source-vs-blind PNG byte-exact RGB in unaffected vehicle ROI {vehicle_region}")
                x0, y0, x1, y1 = SILHOUETTE_CROP
                check(file+"-subject-framing-bounds",
                      (0 <= x0 < x1 <= im.width and 0 <= y0 < y1 <= im.height
                       and x0 > 125 and y0 > 252 and y1 < 550),
                      f"subject-only crop {SILHOUETTE_CROP}; excludes reference cube/header/footer")
                cropped=reference_free.crop(SILHOUETTE_CROP)
                # Proxy for nonempty image data, not shape quality or user recognition.
                bright=sum(1 for pixel in cropped.getdata() if max(pixel)>=140)
                check(file+"-subject-not-flat",bright>100,
                      f"{bright} pixels with any channel >=140; nonempty subject crop only")
                previews={}
                for side in (128,64):
                    content=ImageOps.contain(cropped,(side-8,side-8),Image.Resampling.LANCZOS)
                    tile=Image.new("RGB",(side,side),(11,26,46))
                    tile.paste(content,((side-content.width)//2,(side-content.height)//2))
                    out=OUT/file.replace(".svg",f"-silhouette-{side}px.png")
                    tile.save(out)
                    record(out)
                    previews[side]=tile
                tile_samples.append((file,previews))
        except Exception as err:
            check(file+"-raster-encoded",False,str(err))
    # Real blinded human-study preparation: exact published B01-11 eight-person
    # counterbalance; NO human participants recruited or tested here.
    # Only publish participant-safe folders to a projected viewer. The key is
    # intentionally OUTSIDE this folder and must never be opened during study.
    blind_dir=OUT/"blind-study"
    blind_dir.mkdir(parents=True,exist_ok=True)
    by_concept={file.split("-")[0]:previews for file,previews in tile_samples}
    private_key=[]
    for participant, side, sequence in BLIND_ORDERS:
        destdir=blind_dir/participant
        destdir.mkdir(parents=True,exist_ok=True)
        session_valid=(len(sequence)==3 and set(sequence)==set(BLIND_CONCEPT_FILE)
                       and side in (64,128) and set(by_concept)==set(BLIND_CONCEPT_FILE))
        if not session_valid:
            check("blind-"+participant+"-balanced",False,"invalid session labels/order/size")
            continue
        entries=[]
        copied_correctly=True
        names_neutral=True
        for i,letter in enumerate(sequence,1):
            source=OUT/BLIND_CONCEPT_FILE[letter].replace(".svg",f"-silhouette-{side}px.png")
            neutral=destdir/f"stimulus-{i:02}.png"
            shutil.copyfile(source,neutral)
            # Identity byte check against the genuine, previously rasterized
            # corrected stimulus; not a re-illustration or a different variant.
            copied_correctly &= neutral.read_bytes()==source.read_bytes()
            names_neutral &= (
                all(secret not in str(neutral).lower() for secret in ("wheel","track","omni"))
                and not any(secret in neutral.read_bytes()
                            for secret in (b"A-wheel",b"B-track",b"C-omni",b"0.55 m")))
            record(neutral)
            entries.append({
                "token":neutral.name,
                "concept_key":letter,
                "original_source_file":BLIND_CONCEPT_FILE[letter],
                "image_sha256":hashlib.sha256(neutral.read_bytes()).hexdigest(),
                "px_size":side
            })
        check("blind-"+participant+"-pixel-identity",copied_correctly,
              "3 neutral PNGs copied byte-identically from reference-free actual renderer")
        check("blind-"+participant+"-no-answer-leak",names_neutral,
              "no wheel/track/omni label in subject folder path, PNG filename or basic byte scan")
        private_key.append({"participant":participant,"sequence":sequence,
                            "pixels":side,"stimuli":entries})
    check("blind-eight-sessions",len(private_key)==8 and
          sum(item["pixels"]==64 for item in private_key)==4 and
          sum(item["pixels"]==128 for item in private_key)==4 and
          sum(len(item["stimuli"]) for item in private_key)==24,
          "8 disjoint three-image sessions, 4 at 64px + 4 at 128px, 24 files")
    keyfile=OUT/"facilitator-KEEP_PRIVATE.json"
    keyfile.write_text(json.dumps({
        "instruction":"NEVER show this key, parent output directory, or original file names to participants; share only ONE blind-study/Pxx folder at a time; hide app title/path; user study NOT_RUN.",
        "source":"B01-11 protocol issue #292 comment 6447607722",
        "concept_art_approval":"HQ_NOT_SELECTED",
        "responses_recorded":0,
        "sessions":private_key
    },indent=2)+"\\n",encoding="utf-8")
    record(keyfile)
    check("blind-private-key-separated",keyfile.parent==OUT and
          keyfile.parent!=blind_dir and keyfile.is_file() and
          not any(x.name.endswith(".json") for x in blind_dir.rglob("*")),
          "mapping held outside participant folders; facilitator must protect access")
    # Side-by-side equal-size artwork; separate from full specification-sheet cards.
    if len(tile_samples)==3:
        board=Image.new("RGB",(3*196,260),(8,20,33))
        draw=ImageDraw.Draw(board)
        for i,(name,previews) in enumerate(tile_samples):
            col=i*196
            draw.rounded_rectangle((col+7,7,col+189,252),radius=10,
                                   fill=(19,34,55),outline=(49,72,94),width=2)
            draw.text((col+15,16),name.split("-")[0]+" — 128 px",
                      fill=(217,230,242),font=ImageFont.load_default())
            board.paste(previews[128],(col+34,38))
            draw.text((col+15,178),"64 px",fill=(169,186,204),
                      font=ImageFont.load_default())
            board.paste(previews[64],(col+67,177))
        contact=OUT/"silhouette-review-128-64.png"
        board.save(contact)
        record(contact)
        check("artwork-only-contact-sheet",contact.exists(),
              "three 128px and 64px subject-only renders assembled (NOT an evaluated UX PASS)")
    else:
        check("artwork-only-contact-sheet",False,f"{len(tile_samples)}/3 silhouette source variants")
    for name in SOURCE:
        record(HERE/name)
    summary={"purpose":"source SVG parse and real raster PNG existence; no visual gameplay proof",
             "status":"PASS" if all(x["status"]=="PASS" for x in checks) else "FAIL",
             "pass_count":sum(x["status"]=="PASS" for x in checks),
             "fail_count":sum(x["status"]=="FAIL" for x in checks),
             "checks":checks,"outputs":files,
             "silhouette_crop_xyxy":SILHOUETTE_CROP,
             "silhouette_frame_claim":"Original image-only subject crop; 64/128px pixels delivered, human readability NOT_RUN.",
             "not_run":["Unity EditMode","Unity PlayMode","PhysX collision","camera read tests",
                        "manual 64/128 px readability evaluation","PartSize socket/measured clearance"]}
    (OUT/"test-receipt.json").write_text(json.dumps(summary,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"status":summary["status"],"pass":summary["pass_count"],
                      "fail":summary["fail_count"],"outputs":len(files)}))
    return 0 if summary["fail_count"]==0 else 1

if __name__=="__main__":
    sys.exit(main())
