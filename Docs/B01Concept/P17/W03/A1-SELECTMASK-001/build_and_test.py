#!/usr/bin/env python3
"""Real source A1 UI selection-mask and focus-ring art builder.

Reads the three original immutable approved-perspective SVGs from PR #377
through git-show; does NOT duplicate or alter producer-owned source files.
Outputs working 2D pixel-hit masks and separate focus-ring PNGs.
NOT physical colliders, Unity scene behavior, gameplay or human playtesting.
"""
from __future__ import annotations

import copy
import hashlib
import io
import json
import subprocess
import tempfile
from pathlib import Path
import xml.etree.ElementTree as ET
import zlib

import cairosvg
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont, ImageStat

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered"
M = json.loads((HERE / "manifest.json").read_text(encoding="utf-8"))
VIEW_IDS = {"face": "part-wheel", "side": "part-wheel", "top": "part-wheel"}
PARTS = {"part-mount": "mount-proposal",
         "part-arm": "support-arm",
         "part-damper": "damper-proposal",
         "part-wheel": "wheel"}
NS = "{http://www.w3.org/2000/svg}"


def git_blob(raw: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()


def git_file(commit: str, path: str) -> bytes:
    return subprocess.run(
        ["git", "show", f"{commit}:{path}"],
        check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    ).stdout


def find_id(svg: ET.Element, name: str) -> ET.Element:
    hits = [e for e in svg.iter() if e.get("id") == name]
    if len(hits) != 1:
        raise ValueError(f"Bad SVG id={name}, count={len(hits)}")
    return hits[0]


def alpha_mask(original: Image.Image, threshold: int) -> Image.Image:
    return original.getchannel("A").point(
        lambda alpha: 255 if alpha >= threshold else 0
    )


def opaque_area(mask: Image.Image) -> int:
    return sum(1 for value in mask.getdata() if value > 0)


def hit(mask: Image.Image, x: int, y: int) -> bool:
    return (0 <= x < mask.width and 0 <= y < mask.height
            and mask.getpixel((x, y)) != 0)


def acceptable(mask: Image.Image) -> bool:
    bounds = mask.getbbox()
    if bounds is None:
        return False
    area = opaque_area(mask)
    return (mask.getpixel((0, 0)) == 0
            and bounds[0] >= 1 and bounds[1] >= 1
            and bounds[2] < mask.width and bounds[3] < mask.height
            and mask.width * mask.height * .055 < area
            < mask.width * mask.height * .82)


def make_ring(mask: Image.Image, radius_px: int) -> Image.Image:
    dilated = mask.filter(ImageFilter.MaxFilter(radius_px * 2 + 1))
    return ImageChops.subtract(dilated, mask)


def focus_touches_canvas_edge(ring: Image.Image) -> bool:
    """Observable warning: a focus ring at the raster edge may be clipped."""
    bounds = ring.getbbox()
    return (bounds is not None
            and (bounds[0] == 0 or bounds[1] == 0
                 or bounds[2] == ring.width or bounds[3] == ring.height))


def sprite_from_mask(mask: Image.Image, color: tuple[int, int, int]) -> Image.Image:
    image = Image.new("RGBA", mask.size, color + (0,))
    image.putalpha(mask)
    return image


def clear_previous_export(folder: Path) -> None:
    """Remove only previous A1 generated PNGs and old success/failure receipt."""
    folder.mkdir(parents=True, exist_ok=True)
    for old in folder.glob("A1-*.png"):
        if old.is_file():
            old.unlink()
    (folder / "receipt.json").unlink(missing_ok=True)


def cleanup_negative_regression() -> bool:
    """A failed rerun must never preserve an earlier PASS receipt or stale A1 art."""
    with tempfile.TemporaryDirectory(prefix="b01-31-art-cleanup-") as tmp:
        directory = Path(tmp)
        (directory / "A1-obsolete.png").write_bytes(b"OLD")
        (directory / "receipt.json").write_text('{"status":"PASS"}', encoding="utf-8")
        (directory / "unrelated.txt").write_text("PRESERVE", encoding="utf-8")
        clear_previous_export(directory)
        return (not (directory / "A1-obsolete.png").exists()
                and not (directory / "receipt.json").exists()
                and (directory / "unrelated.txt").read_text(encoding="utf-8")
                == "PRESERVE")


def main() -> int:
    # Do not leave a prior PASS receipt discoverable after failed or interrupted runs.
    clear_previous_export(OUT)
    check_results: list[dict] = []
    output_records: list[dict] = []
    unique_masks = {}
    card = Image.new("RGB", (3 * 256 + 48, 320), "#15273a")
    draw = ImageDraw.Draw(card)

    def check(name: str, condition: bool):
        check_results.append({"name": name, "status": "PASS" if condition else "FAIL"})

    def save(name: str, image: Image.Image):
        dest = OUT / name
        image.save(dest, "PNG")
        raw = dest.read_bytes()
        loaded = Image.open(io.BytesIO(raw))
        check(f"{name} real PNG signature", raw[:8] == b"\x89PNG\r\n\x1a\n")
        check(f"{name} real PNG size", loaded.size == image.size)
        check(f"{name} real bytes nonempty", len(raw) > 100)
        output_records.append({
            "name": name, "size_bytes": len(raw),
            "sha256": hashlib.sha256(raw).hexdigest(),
            "crc32": f"{zlib.crc32(raw):08x}", "size_px": list(image.size),
        })

    check("negative stale export and prior PASS receipt invalidated",
          cleanup_negative_regression())
    source = M["source"]
    check("one authorized A1 unit, not a MODEL_JOB",
          M["authorization"]["unit"] == "SINGLE_ATTACHABLE_PART"
          and M["authorization"]["MODEL_JOB"] == "NOT_AUTHORIZED")
    check("three source-only views and three scales",
          M["render"]["views"] == ["face", "side", "top"]
          and M["render"]["sizes"] == [48, 96, 192])
    check("source locked to previous immutable PR377 HEAD",
          source["pr"] == 377
          and source["immutable_commit"] ==
          "759c3471aab998b8ae1f153a9d4921401dddc749")

    for view in M["render"]["views"]:
        path = source["base_path"] + f"A1-orientation-{view}.svg"
        raw = git_file(source["immutable_commit"], path)
        correct_blob = source["expected_blob_sha1"][view]
        verified = git_blob(raw) == correct_blob
        check(f"{view}: same real original Git object", verified)
        if not verified:
            raise RuntimeError(f"{view}: immutable original SVG Git blob drift; export refused")
        check(f"{view}: negative corrupted Git object must fail",
              git_blob(raw + b"x") != correct_blob)
        root = ET.fromstring(raw)
        semantic = {
            e.get("id"): e.get("data-part")
            for e in root.iter() if e.get("data-part")
        }
        check(f"{view}: all four source semantic parts", semantic == PARTS)
        check(f"{view}: single original attachable part",
              sum(e.get("data-authoring-unit") == "SINGLE_ATTACHABLE_PART"
                  for e in root.iter()) == 1)
        check(f"{view}: every concept geometry state unmeasured",
              all(find_id(root, key).get("data-physical-status") ==
                  "UNMEASURED" for key in PARTS))
        mutated_semantic = copy.deepcopy(root)
        find_id(mutated_semantic, VIEW_IDS[view]).set("data-part", "not-wheel")
        check(f"{view}: negative semantic mutation rejected",
              {e.get("id"): e.get("data-part")
               for e in mutated_semantic.iter() if e.get("data-part")} != PARTS)

        for size in M["render"]["sizes"]:
            src_png = cairosvg.svg2png(
                bytestring=raw, output_width=size, output_height=size)
            sprite = Image.open(io.BytesIO(src_png)).convert("RGBA")
            sprite.load()
            mask = alpha_mask(sprite, M["render"]["alpha_threshold"])
            check(f"{view}/{size}: actual full-size 2D alpha mask",
                  mask.size == (size, size) and acceptable(mask))
            check(f"{view}/{size}: 2D hit works and transparent corner misses",
                  hit(mask, *mask.getbbox()[:2]) or (
                      opaque_area(mask) > 0 and
                      any(hit(mask, x, y) for y in range(size)
                          for x in range(size) if mask.getpixel((x, y))))
                  and not hit(mask, 0, 0) and not hit(mask, -1, 2)
                  and not hit(mask, size, size))
            ring = make_ring(mask, M["render"]["outline_thickness_pixels"][str(size)])
            check(f"{view}/{size}: real outer focus pixels",
                  opaque_area(ring) > max(3, size // 8))
            check(f"{view}/{size}: focus never covers selectable interior",
                  ImageChops.multiply(ring, mask).getbbox() is None)
            check(f"{view}/{size}: focus has declared image dimensions",
                  ring.size == mask.size and ring.mode == "L")
            mask_img = sprite_from_mask(mask, (255, 255, 255))
            ring_img = sprite_from_mask(ring, (91, 234, 217))
            save(f"A1-{view}-mask-{size}.png", mask_img)
            save(f"A1-{view}-focus-{size}.png", ring_img)
            output_records[-1]["focus_touches_canvas_edge"] = (
                focus_touches_canvas_edge(ring)
            )
            if size == 96:
                unique_masks[view] = hashlib.sha256(mask.tobytes()).hexdigest()
                bg = Image.new("RGBA", (96, 96), (17, 31, 47, 255))
                bg = Image.alpha_composite(bg, sprite)
                bg = Image.alpha_composite(bg, ring_img)
                save(f"A1-{view}-preview-96.png", bg)
                tile = bg.resize((192, 192), Image.Resampling.NEAREST)
                card.paste(tile.convert("RGB"), (32 + 256 * len(unique_masks) - 256, 52))
                draw.text((32 + 256 * len(unique_masks) - 256, 25),
                          f"{view.upper()} / SELECTABLE PIXELS", fill="#d2edec",
                          font=ImageFont.load_default())

    check("all three perspectives genuinely differ in alpha geometry",
          len(set(unique_masks.values())) == 3)
    a = Image.new("L", (48, 48), 0)
    check("negative all-transparent mask rejected", not acceptable(a))
    a.paste(255, (0, 0, 48, 48))
    check("negative opaque-background mask rejected", not acceptable(a))
    a = Image.new("L", (48, 48), 0)
    a.paste(255, (12, 12, 35, 35))
    mutant = a.copy()
    mutant.putpixel((0, 0), 255)
    check("negative single-pixel alpha source mutation is detectable",
          a.tobytes() != mutant.tobytes() and acceptable(a)
          and not acceptable(mutant))

    edge_probe = Image.new("L", (8, 8), 0)
    edge_probe.putpixel((0, 4), 255)
    check("negative focus touching canvas edge is detected",
          focus_touches_canvas_edge(edge_probe))
    edge_probe.putpixel((0, 4), 0)
    edge_probe.putpixel((4, 4), 255)
    check("interior focus does not trigger edge warning",
          not focus_touches_canvas_edge(edge_probe))

    save("A1-mask-contact.png", card)
    check("exact real PNG production count",
          len(output_records) == M["render"]["output_count"])
    expected_pngs = {
        f"A1-{view}-{kind}-{size}.png"
        for view in M["render"]["views"]
        for kind in ("mask", "focus")
        for size in M["render"]["sizes"]
    } | {f"A1-{view}-preview-96.png" for view in M["render"]["views"]} | {
        "A1-mask-contact.png"
    }
    reported_pngs = [item["name"] for item in output_records]
    disk_pngs = [p.name for p in OUT.iterdir()
                 if p.is_file() and p.suffix.lower() == ".png"]
    check("exact exported PNG inventory matches expected names and receipt",
          len(disk_pngs) == len(expected_pngs)
          and set(disk_pngs) == expected_pngs
          and set(reported_pngs) == expected_pngs
          and len(reported_pngs) == len(expected_pngs))

    result = {
        "package_id": M["package_id"], "version": M["version"],
        "producer": M["owner"],
        "status": "PASS" if all(x["status"] == "PASS" for x in check_results) else "FAIL",
        "passed": sum(x["status"] == "PASS" for x in check_results),
        "failed": sum(x["status"] == "FAIL" for x in check_results),
        "source_pr_sha": source["immutable_commit"],
        "tests": check_results, "outputs": output_records,
        "scope": "Original visual alpha selection and focus PNGs, NOT colliders",
        "focus_edge_touch_count": sum(bool(x.get("focus_touches_canvas_edge"))
                                      for x in output_records),
        "not_run": ["Unity Editor", "Unity PlayMode", "Windows Player",
                    "Human UI legibility", "Physical collider", "Blender"],
    }
    # A failed export is not a deliverable: remove partial PNGs and never publish
    # a stale PASS receipt from a previous run.
    if result["status"] != "PASS":
        clear_previous_export(OUT)
    else:
        (OUT / "receipt.json").write_text(
            json.dumps(result, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8")
    for item in check_results:
        if item["status"] == "FAIL":
            print("FAILED_CHECK: " + item["name"])
    print(json.dumps({"status": result["status"], "pass": result["passed"],
                      "fail": result["failed"], "png_files": len(output_records)}))
    return int(result["failed"] > 0)


if __name__ == "__main__":
    raise SystemExit(main())
