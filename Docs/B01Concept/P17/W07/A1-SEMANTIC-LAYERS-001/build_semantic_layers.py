#!/usr/bin/env python3
"""B01-31: real per-component 2D art from immutable A1 original SVGs.
No Unity, collider, socket, physical dimension, gameplay or MODEL_JOB claims.
"""
from __future__ import annotations
import copy
import hashlib
import io
import json
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

import cairosvg
from PIL import Image, ImageChops, ImageDraw

DIR = Path(__file__).resolve().parent
OUT = DIR / "rendered"
M = json.loads((DIR / "manifest.json").read_text(encoding="utf-8"))
GROUPS = [
    ("mount", "mount-proposal", "part-mount"),
    ("arm", "support-arm", "part-arm"),
    ("damper", "damper-proposal", "part-damper"),
    ("wheel", "wheel", "part-wheel"),
]


def git_blob(raw):
    return hashlib.sha1(b"blob " + str(len(raw)).encode() + b"\0" + raw).hexdigest()


def source(view):
    p = M["upstream"]["path"] + "A1-orientation-" + view + ".svg"
    raw = subprocess.run(
        ["git", "show", M["upstream"]["commit"] + ":" + p],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True
    ).stdout
    if git_blob(raw) != M["upstream"]["blobs"][view]:
        raise ValueError("Original Git SVG blob changed: " + view)
    return raw


def unit(root):
    matches = [x for x in root.iter()
               if x.get("data-authoring-unit") == "SINGLE_ATTACHABLE_PART"]
    if len(matches) != 1:
        raise ValueError("Expected exactly one semantic A1 unit")
    return matches[0]


def validate(raw):
    root = ET.fromstring(raw)
    parts = [x for x in unit(root) if x.get("data-part")]
    expected = {(semantic, ident) for _, semantic, ident in GROUPS}
    if (len(parts) != 4 or
            {(x.get("data-part"), x.get("id")) for x in parts} != expected or
            any(x.get("data-physical-status") != "UNMEASURED"
                for x in parts)):
        raise ValueError("Missing, duplicated or unauthorized semantic group")
    ids = [x.get("id") for x in root.iter() if x.get("id")]
    if len(ids) != len(set(ids)):
        raise ValueError("Duplicate SVG id")
    return root


def isolate(original, semantic):
    clone = copy.deepcopy(original)
    parent = unit(clone)
    for node in list(parent):
        if node.get("data-part") != semantic:
            parent.remove(node)
    if len([x for x in parent if x.get("data-part")]) != 1:
        raise ValueError("Invalid component isolation")
    return ET.tostring(clone, encoding="utf-8")


def render(src, size):
    payload = cairosvg.svg2png(bytestring=src, output_width=size,
                               output_height=size)
    with Image.open(io.BytesIO(payload)) as img:
        return img.convert("RGBA")


def transparent_layer(image, size):
    alpha = image.getchannel("A")
    box = alpha.getbbox()
    populated = sum(v > 15 for v in alpha.getdata())
    return (image.size == (size, size) and box is not None
            and 10 < populated < size * size * .8
            and alpha.getpixel((0, 0)) == 0
            and box[0] > 0 and box[1] > 0
            and box[2] < size and box[3] < size)


def overlap(a, b):
    a = a.getchannel("A").point(lambda v: 255 if v >= 16 else 0)
    b = b.getchannel("A").point(lambda v: 255 if v >= 16 else 0)
    union = ImageChops.lighter(a, b).histogram()[255]
    both = ImageChops.multiply(a, b).histogram()[255]
    return (both / union) if union else 0.0


def negative_tests():
    original = validate(source("face"))
    removed = copy.deepcopy(original)
    parent = unit(removed)
    parent.remove(next(x for x in parent if x.get("data-part") == "wheel"))
    duplicated = copy.deepcopy(original)
    parent = unit(duplicated)
    parent.append(copy.deepcopy(next(
        x for x in parent if x.get("data-part") == "support-arm")))
    checks = []
    for name, bad in (("missing wheel", removed),
                      ("duplicate arm", duplicated)):
        try:
            validate(ET.tostring(bad, encoding="utf-8"))
            checks.append((name, False))
        except ValueError:
            checks.append((name, True))
    checks.append(("opaque background rejected", not transparent_layer(
        Image.new("RGBA", (128, 128), (255, 0, 0, 255)), 128)))
    checks.append(("mutated upstream blob is not trusted",
                   git_blob(source("face") + b"x") != M["upstream"]["blobs"]["face"]))
    return checks


def clear():
    OUT.mkdir(parents=True, exist_ok=True)
    for path in OUT.glob("A1-*.png"):
        path.unlink()
    (OUT / "receipt.json").unlink(missing_ok=True)


def build():
    clear()
    tests = []
    files = []
    montage = Image.new("RGB", (4 * 220 + 24, 3 * 240 + 24), "#14293a")
    draw = ImageDraw.Draw(montage)

    def check(name, valid):
        tests.append({"name": name, "passed": bool(valid)})

    def save(name, img):
        dest = OUT / name
        img.save(dest, "PNG")
        raw = dest.read_bytes()
        with Image.open(io.BytesIO(raw)) as loaded:
            same = ImageChops.difference(img.convert("RGBA"),
                                         loaded.convert("RGBA")).getbbox() is None
        check(name + " actual PNG byte-pixel roundtrip",
              raw.startswith(b"\x89PNG\r\n\x1a\n") and same)
        files.append({"path": name, "sha256": hashlib.sha256(raw).hexdigest(),
                      "bytes": len(raw), "px": list(img.size)})

    for row, view in enumerate(M["views"]):
        raw = source(view)
        root = validate(raw)
        check(view + " immutable original SVG verified",
              git_blob(raw) == M["upstream"]["blobs"][view])
        for size in M["sizes"]:
            rebuilt = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            for col, (slug, semantic, _) in enumerate(GROUPS):
                img = render(isolate(root, semantic), size)
                check(view + "/" + slug + "/" + str(size) + " layer alpha",
                      transparent_layer(img, size))
                save(f"A1-{view}-{slug}-{size}.png", img)
                rebuilt = Image.alpha_composite(rebuilt, img)
                if size == 256:
                    preview = Image.new("RGBA", (192, 192), (17, 38, 54, 255))
                    preview.alpha_composite(
                        img.resize((192, 192), Image.Resampling.LANCZOS))
                    x, y = 16 + col * 220, 16 + row * 240
                    montage.paste(preview.convert("RGB"), (x, y + 20))
                    draw.text((x, y), (view + " / " + slug).upper(),
                              fill="#def6e8")
            ref = render(raw, size)
            iou = overlap(ref, rebuilt)
            check(view + "/" + str(size) + " assembly alpha coverage >= 98.5%",
                  iou >= .985)
            if size == 256:
                save(f"A1-{view}-assembly-256.png", ref)
                files[-1]["recomposed_alpha_iou"] = round(iou, 7)
    save("A1-semantic-layer-contact.png", montage)

    for name, valid in negative_tests():
        check("negative " + name, valid)
    wanted = {f"A1-{v}-{part}-{s}.png" for v in M["views"]
              for part, _, _ in GROUPS for s in M["sizes"]}
    wanted |= {f"A1-{v}-assembly-256.png" for v in M["views"]}
    wanted.add("A1-semantic-layer-contact.png")
    names = [x["path"] for x in files]
    check("exact 28 unique expected physical PNG files",
          set(names) == wanted and len(names) == len(wanted)
          and {p.name for p in OUT.glob("*.png")} == wanted
          and len(names) == M["png_count"])
    check("all saved output hashes match actual PNG bytes",
          all(hashlib.sha256((OUT / rec["path"]).read_bytes()).hexdigest()
              == rec["sha256"] for rec in files))
    check("tampered PNG rejected by SHA gate",
          hashlib.sha256(b"tamper" + (OUT / names[0]).read_bytes()).hexdigest()
          != files[0]["sha256"])

    fails = [t["name"] for t in tests if not t["passed"]]
    result = {"id": M["package_id"], "version": M["version"],
              "source_sha": M["upstream"]["commit"],
              "status": "FAIL" if fails else "PASS",
              "pass": len(tests) - len(fails), "fail": len(fails),
              "png_count": len(files), "tests": tests, "outputs": files,
              "native_unity": "NOT_RUN", "independent_qa": "PENDING",
              "game_integrated": False}
    if fails:
        clear()
        for name in fails:
            print("FAILED_CHECK: " + name, file=sys.stderr)
    else:
        (OUT / "receipt.json").write_text(
            json.dumps(result, indent=2, ensure_ascii=False) + "\n",
            encoding="utf-8")
    print(json.dumps({k: result[k] for k in ("status", "pass", "fail", "png_count")}))
    return int(bool(fails))


if __name__ == "__main__":
    try:
        sys.exit(build())
    except Exception as error:
        clear()
        print("EXPORT_FAILED_CLOSED:", type(error).__name__, error, file=sys.stderr)
        raise
