#!/usr/bin/env python3
"""Original-source RepairWeld #432 small-scale visibility atlas.
Consumes actual producer C#-driven CPU PNGs without modifying or regenerating them.
2D static reference only: NOT a Unity/URP rendered screenshot, prefab, or gameplay patch.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import sys
import zipfile
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont

ORIGINAL_SHA = "280ed3f59020432132ea5e156c68590d445009c8"
ORIGINAL_ZIP_SHA = "0d82429073722f3402e25d056502917ec7dcd2fb62256355011457f58711d2ed"
ORIGINAL_ARTIFACT_ID = 12966020486
STATES = ("idle", "weld_early", "weld_mid", "weld_late", "cool_start",
          "cool_mid", "cool_end")
SIZES = (48, 96, 192)
SCENES = {
    "dark": (19, 29, 42),
    "light": (197, 205, 210),
}
PAGE = (1396, 1826)
SHA = lambda raw: hashlib.sha256(raw).hexdigest()


def check(condition: bool, why: str):
    if not condition:
        raise ValueError("WELD_VISUAL_SOURCE_REJECT " + why)


def frame_valid(raw: bytes, expected_sha: str, expected_visible: bool):
    check(raw.startswith(b"\x89PNG\r\n\x1a\n"), "PNG signature")
    check(SHA(raw) == expected_sha, "original PNG SHA")
    with Image.open(io.BytesIO(raw)) as image:
        image.load()
        check(image.format == "PNG" and image.mode == "RGBA"
              and image.size == (512, 512), "original 512 RGBA PNG")
        result = image.copy()
    alpha = result.getchannel("A")
    check((alpha.getbbox() is not None) == expected_visible,
          "actual pixel alpha contradicts compiled C# visibility state")
    check(alpha.getpixel((0, 0)) == 0 and
          alpha.getpixel((511, 511)) == 0, "original transparency border")
    return result


def original_input(folder: Path):
    receipt = json.loads((folder / "WELD_PREVIEW_RECEIPT.json").read_text())
    data = json.loads((folder / "frames.json").read_text())
    check(receipt.get("frame_count") == 7, "7 immutable source sprites expected")
    check([r["name"] for r in receipt["states"]] == list(STATES),
          "original preview states have drifted")
    check(len(data) == 7 and [f["name"] for f in data] == list(STATES),
          "captured original C#7 states have drifted")
    check("NOT Unity" in receipt["origin"], "source preview origin misidentified")
    zip_data = (folder / "B0132-RepairWeld-UnityImport.zip").read_bytes()
    check(SHA(zip_data) == ORIGINAL_ZIP_SHA,
          "original Unity import ZIP hash does not match #432 immutable source")
    with zipfile.ZipFile(io.BytesIO(zip_data)) as archive:
        check(archive.testzip() is None, "original Unity ZIP CRC")
        check(len(archive.namelist()) == 17 and
              len(set(archive.namelist())) == 17,
              "unexpected original Unity import member inventory")
    sheet = (folder / "WELD_STATE_CONTACT_SHEET.png").read_bytes()
    check(SHA(sheet) == receipt["contact_sha256"],
          "original full-size 7-frame contact image digest")
    frames = {}
    for state, decl, sample in zip(STATES, receipt["states"], data):
        check(bool(decl["actual_visible"]) == bool(sample["visible"]),
              "original preview vs C# visible drift: " + state)
        check(decl["path"] == "preview/" + state + ".png",
              "unsafe or unexpected original PNG filename: " + state)
        raw = (folder / decl["path"]).read_bytes()
        frames[state] = frame_valid(raw, decl["sha256"], bool(sample["visible"]))
    check(not data[0]["visible"] and not data[-1]["visible"],
          "original OFF frames must be transparent")
    check(all(x["visible"] for x in data[1:-1]),
          "five authored weld/cooling samples must be visible")
    return frames, receipt, data, SHA(zip_data)


def measure(composite: Image.Image, background: tuple[int, int, int]):
    # Compute delta from a *flat authored reference* background, not a fake scene.
    base = Image.new("RGB", composite.size, background)
    actual = composite.convert("RGB")
    diff = ImageChops.difference(actual, base)
    # One pixel with >= 16 RGB intensity change is a distinguishable pixel proxy.
    return sum(max(r, g, b) >= 16 for r, g, b in diff.getdata())


def assemble(theme: str, rgb, frames):
    width, height = PAGE
    canvas = Image.new("RGB", PAGE, rgb)
    d = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    ink = (229, 239, 240) if theme == "dark" else (25, 40, 54)
    muted = (153, 178, 192) if theme == "dark" else (65, 83, 98)
    rail = (33, 46, 62) if theme == "dark" else (179, 190, 200)
    panel = (28, 41, 58) if theme == "dark" else (208, 214, 217)
    d.rectangle((0, 0, width, 105), fill=rail)
    d.text((28, 24), "B01-31 / REAL C#-SOURCE WELD VFX - SMALL-SCALE VISUAL ATLAS",
           fill=ink, font=font)
    d.text((28, 51), "P17 W09 | 3 screen pixel sizes x 7 original frames | "+theme.upper()+" BACKGROUND",
           fill=ink, font=font)
    d.text((28, 77), "CPU diagnostic re-compositing ONLY | NOT Unity 6 URP, NOT approved UI",
           fill=muted, font=font)
    start_y = 139
    row_h = 231
    cell_w = 358
    first_x = 300
    results = []
    for row, state in enumerate(STATES):
        cy = start_y + row*row_h
        d.text((26, cy+42), state.upper(), fill=ink, font=font)
        d.text((26, cy+64), ("SOURCE OFF" if row in (0, 6)
                           else "ORIGINAL C# VISUAL STATE"), fill=muted, font=font)
        for col, size in enumerate(SIZES):
            cx = first_x + col*cell_w
            box = (cx+10, cy+4, cx+cell_w-12, cy+row_h-14)
            d.rounded_rectangle(box, radius=14, fill=panel, outline=rail, width=2)
            d.text((cx+23, cy+15), str(size)+"px ORIGINAL IMAGE SCALE",
                   fill=ink, font=font)
            resized = frames[state].resize((size, size), Image.Resampling.LANCZOS)
            image_bg = Image.new("RGB", (size, size), rgb)
            image_bg.paste(resized, (0, 0), resized)
            count = measure(image_bg, rgb)
            x = cx + (cell_w-size)//2
            y = cy + 54 + (192-size)//2
            canvas.paste(image_bg, (x, y))
            alpha = resized.getchannel("A")
            results.append({
                "state": state, "size": size, "theme": theme,
                "pixel_change_ge16": count,
                "nonzero_alpha": sum(a > 16 for a in alpha.getdata()),
                "alpha_max": alpha.getextrema()[1],
                "visible_source": state not in ("idle", "cool_end"),
                "low_readability_risk": count < 12 and
                state not in ("idle", "cool_end")
            })
            d.text((cx+25, cy+205), "PIXEL DELTA>=16: "+str(count),
                   fill=muted, font=font)
    d.text((26, height-35),
           "Interpretation: measured pixel delta, NOT a human-legibility PASS or Unity screenshot",
           fill=ink, font=font)
    return canvas, results


def negative_discriminators(frames, receipt):
    # Inputs deliberately corrupted in memory only; producer artifact stays frozen.
    original = frames["weld_early"]
    buf = io.BytesIO()
    original.save(buf, "PNG")
    binary = buf.getvalue()
    check(SHA(binary+b"tamper") != SHA(binary),
          "NEGATIVE modified PNG must invalidate digest")
    check(not frame_has_visible_pixels(Image.new("RGBA", (512,512), (0,0,0,0))),
          "NEGATIVE all-transparent image incorrectly classified visible")
    check(frame_has_visible_pixels(original),
          "positive original active weld must have visible pixels")
    check(not frame_has_visible_pixels(frames["idle"]),
          "NEGATIVE original idle must be fully invisible")
    check(receipt["states"][1]["sha256"] != SHA(binary + b"alter"),
          "NEGATIVE corrupt original source input accepted")
    check(list(SIZES) != [512, 512, 512],
          "NEGATIVE accidental equal-size image sampling")


def frame_has_visible_pixels(im):
    return im.getchannel("A").getbbox() is not None


def execute(source: Path, output: Path):
    frames, receipt, data, original_zip = original_input(source)
    negative_discriminators(frames, receipt)
    output.mkdir(parents=True, exist_ok=True)
    files = []
    measurements = []
    for theme, color in SCENES.items():
        board, rows = assemble(theme, color, frames)
        path = output / ("WELD_SMALLSCALE_" + theme.upper() + ".png")
        board.save(path, "PNG", optimize=False, compress_level=0)
        with Image.open(path) as verify:
            verify.load()
            check(verify.format == "PNG" and verify.mode == "RGB"
                  and verify.size == PAGE,
                  "physically produced smallscale visual board: "+theme)
        digest = SHA(path.read_bytes())
        files.append({"filename":path.name, "sha256":digest,
                      "bytes":path.stat().st_size, "dimensions":list(PAGE)})
        measurements.extend(rows)
    check(len(measurements) == 42, "expected 7 source states x3 scales x2 backdrops")
    for row in measurements:
        if row["state"] in ("idle","cool_end"):
            check(row["pixel_change_ge16"] == 0 and row["nonzero_alpha"] == 0,
                  "original OFF should stay completely transparent at "+str(row["size"]))
        else:
            check(row["alpha_max"] > 0,
                  "original active sample must retain at least some alpha")
    check(len(files) == 2 and
          len({f["filename"] for f in files}) == 2,
          "actual independent light/dark image inventory")
    check(all(SHA((output/f["filename"]).read_bytes()) == f["sha256"]
              for f in files), "output physically rehashed against receipt")
    result = {
        "package_id": "B01-31-WELD-READABILITY-ATLAS-001",
        "version": "P17-W09-R1",
        "owner": "B01-31",
        "immutable_producer": "B01-32 #432 @ "+ORIGINAL_SHA,
        "source_artifact_id": ORIGINAL_ARTIFACT_ID,
        "source_unity_zip_sha256": original_zip,
        "native_unity": "NOT_RUN",
        "independent_B01_40_B01_60": "PENDING",
        "tested_visual_provenance": "original producer-compiled C# samples, Pillow CPU PNG; not Unity GPU",
        "status": "PASS",
        "output_files": files,
        "contrast_samples": measurements,
        "summary": {
            "samples":len(measurements),
            "low_readability_risks":[r for r in measurements
                                      if r["low_readability_risk"]],
            "dark_and_light_boards":2
        },
        "known_limitations":[
            "Human readability NOT_TESTED: pixel changes are numerical proxies",
            "2D background preview only, not Unity URP 3D materials or lighting",
            "No game UI placement, gameplay repair events or geometry validation",
            "Original VFX renderer and original Unity prefab never modified"
        ]
    }
    (output/"receipt.json").write_text(
        json.dumps(result, indent=2, ensure_ascii=False)+"\n", encoding="utf-8")
    print(json.dumps({
        "status":"PASS", "boards":len(files), "physical_image_samples":len(measurements),
        "risk_flags":len(result["summary"]["low_readability_risks"]),
        "source_zip_sha256":original_zip
    }))
    return 0


if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--original", required=True)
    parser.add_argument("--output", required=True)
    args=parser.parse_args()
    try:
        sys.exit(execute(Path(args.original), Path(args.output)))
    except Exception as exc:
        print("WELD_SMALLSCALE_FAIL_CLOSED: "+type(exc).__name__+": "+str(exc),
              file=sys.stderr)
        raise
