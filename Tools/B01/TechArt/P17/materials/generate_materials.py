#!/usr/bin/env python3
"""B01-30 MAT-A1-001: reproducible tileable candidate materials for A1 wheel+arm.

All textures are generated from original analytic functions. NO B01-31 asset
modification, MODEL_JOB, Unity project mutation, or claim of approved PBR.
Exports 8 real RGBA PNG maps compatible with Unity URP Lit plus a visual sheet.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import zlib

PROFILE_NAMES = ("tire_rubber", "brushed_arm_alloy")
MAP_NAMES = ("basecolor", "normal", "metallic_smoothness", "occlusion")
TAU = math.tau


def clamp_byte(value: float) -> int:
    return max(0, min(255, round(value)))


def height(profile: str, x: int, y: int, size: int) -> float:
    u, v = x / size, y / size
    if profile == "tire_rubber":
        # Two diagonal chevron-like interleaved bands, periodic in BOTH axes.
        # No camera lighting baked into this procedural normal/height basis.
        a = math.sin(TAU * (10 * u + 6 * v))
        b = math.sin(TAU * (10 * u - 6 * v))
        return 0.48 + 0.23 * max(a, b) + 0.055 * math.sin(TAU * 7 * u) * math.sin(TAU * 9 * v)
    if profile == "brushed_arm_alloy":
        # Fine, entirely periodic material microstructure, not a UV-specific part.
        return (0.50 + 0.11 * math.sin(TAU * 37 * v)
                + 0.045 * math.sin(TAU * 51 * v)
                + 0.025 * math.sin(TAU * 5 * u))
    raise ValueError("Unknown authored material: " + profile)


def png_bytes(size: int, rgba: bytes) -> bytes:
    """Small, lossless, fully deterministic PNG RGBA writer (no random seed)."""
    assert len(rgba) == size * size * 4
    def chunk(tag: bytes, data: bytes) -> bytes:
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xffffffff))
    ihdr = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
    stride = size * 4
    raw = b"".join(b"\0" + rgba[y * stride:(y + 1) * stride] for y in range(size))
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr)
            + chunk(b"IDAT", zlib.compress(raw, level=9)) + chunk(b"IEND", b""))


def produce(profile: str, size: int) -> dict[str, bytes]:
    assert profile in PROFILE_NAMES
    # All derived maps sample one physical-looking periodic micro-height source.
    # Normals use periodic central differences. OpenGL-style tangent +Y:
    # Unity must import as Texture Type Normal map; not SRGB color data.
    surface = [height(profile, x, y, size) for y in range(size) for x in range(size)]
    layers = {name: bytearray(size * size * 4) for name in MAP_NAMES}
    for y in range(size):
        for x in range(size):
            idx = y * size + x
            h = surface[idx]
            du = (surface[y * size + ((x + 1) % size)]
                  - surface[y * size + ((x - 1) % size)]) * 0.5
            dv = (surface[((y + 1) % size) * size + x]
                  - surface[((y - 1) % size) * size + x]) * 0.5
            scale = 5.0 if profile == "tire_rubber" else 2.8
            nx, ny, nz = -scale * du, -scale * dv, 1.0
            inv = 1.0 / math.sqrt(nx * nx + ny * ny + nz * nz)
            nrgb = tuple(clamp_byte(127.5 * (1.0 + component * inv))
                         for component in (nx, ny, nz))
            k = idx * 4
            layers["normal"][k:k + 4] = bytes((*nrgb, 255))
            if profile == "tire_rubber":
                tint = 15.0 * (h - 0.5)
                base = (30 + tint, 41 + tint, 53 + tint)
                metallic = 0
                smoothness = clamp_byte(43 + 21 * (h - 0.5))
                ao = clamp_byte(238 - 20 * max(0, 0.52 - h))
            else:
                tint = 33.0 * (h - 0.5)
                base = (91 + tint, 116 + tint, 135 + tint)
                metallic = 219
                smoothness = clamp_byte(175 + 34 * (h - 0.5))
                ao = clamp_byte(247 - 12 * max(0, 0.50 - h))
            layers["basecolor"][k:k + 4] = bytes((*map(clamp_byte, base), 255))
            # URP Lit Metallic map: RED=metallic, ALPHA=smoothness.
            # Green and blue intentionally zero, NOT packed HDRP MaskMap.
            layers["metallic_smoothness"][k:k + 4] = bytes((metallic, 0, 0, smoothness))
            # URP Lit separate occlusion map: GREEN channel is AO strength.
            # Set RGB equal G for standalone grayscale inspection convenience.
            layers["occlusion"][k:k + 4] = bytes((ao, ao, ao, 255))
    return {name:png_bytes(size, bytes(layer)) for name, layer in layers.items()}


def preview_sheet(root: Path, size: int) -> Path:
    """Actual generated PNG swatches, not mocked image references."""
    from PIL import Image, ImageDraw, ImageFont
    col = 256
    pad = 22
    head = 84
    sw = col * 4 + pad * 5
    sh = head + (col + 65) * 2 + 34
    sheet = Image.new("RGB", (sw, sh), (12, 22, 35))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.text((pad, 19), "MAT-A1-001 | ORIGINAL TILEABLE URP LIT MAPS | TECH ART CANDIDATE", fill=(231, 242, 248), font=font)
    draw.text((pad, 39), "Wheel rubber / arm alloy - 2 source independent materials, NOT an approved 3D part", fill=(171, 204, 215), font=font)
    for row, profile in enumerate(PROFILE_NAMES):
        top = head + row * (col + 65)
        for column, kind in enumerate(MAP_NAMES):
            x = pad + column * (col + pad)
            png = root / profile / f"{profile}_{kind}.png"
            with Image.open(png) as opened:
                patch = opened.convert("RGB").resize((col, col), Image.Resampling.NEAREST)
            sheet.paste(patch, (x, top))
            draw.text((x + 1, top + col + 9), profile, fill=(218, 231, 240), font=font)
            draw.text((x + 1, top + col + 25), kind, fill=(138, 190, 203), font=font)
    dest = root / "MAT-A1-001-map-contact-sheet.png"
    sheet.save(dest, format="PNG")
    return dest


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="build/B01-30/MAT-A1-001")
    parser.add_argument("--size", type=int, default=512)
    args = parser.parse_args()
    size = args.size
    if size < 64 or size > 1024 or size & (size - 1):
        parser.error("size must be a power of two from 64 to 1024")
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    entries = []
    for profile in PROFILE_NAMES:
        folder = out / profile
        folder.mkdir(parents=True, exist_ok=True)
        for kind, png in produce(profile, size).items():
            path = folder / f"{profile}_{kind}.png"
            path.write_bytes(png)
            entries.append({"path":path.relative_to(out).as_posix(),
                            "bytes":len(png), "sha256":hashlib.sha256(png).hexdigest(),
                            "width":size, "height":size, "png_mode":"RGBA"})
    swatch = preview_sheet(out, size)
    entries.append({"path":swatch.relative_to(out).as_posix(),
                    "bytes":swatch.stat().st_size,
                    "sha256":hashlib.sha256(swatch.read_bytes()).hexdigest()})
    manifest = {
        "package_id":"B01-30-MAT-A1-001",
        "version":"R1",
        "source":"original deterministic mathematical functions, no external texture use",
        "style_reference":"B01-31 CONCEPT-MOBILITY-001 REVISE_A_ONLY wheel+support-arm",
        "status":"CANDIDATE_TECH_ART_NO_MODEL_JOB_NO_GAME_INTEGRATION",
        "target":"Unity 6000.3.14f1 URP Lit, texture sources only",
        "size":size, "tile_mode":"Repeat", "profile_names":list(PROFILE_NAMES),
        "channel_contract":{
            "basecolor":"RGBA sRGB, alpha=255",
            "normal":"RGB tangent-space +Y, texture type Normal Map, sRGB disabled",
            "metallic_smoothness":"R=metallic, A=smoothness (URP Lit _MetallicGlossMap); G/B=0, sRGB disabled",
            "occlusion":"G=ambient occlusion, RGB repeated for preview; sRGB disabled"},
        "textures":entries, "test_state":"MUST_RUN_INDEPENDENT_VERIFY",
        "not_run":["Unity renderer", "3D mesh UV/texel density",
                   "physical material approval", "URP visual receiver", "PhysX"]
    }
    (out / "material-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print("B01_30_MAT_A1_GENERATED", json.dumps({
        "profiles":len(PROFILE_NAMES), "textures":len(entries)-1,
        "size":size,"preview":str(swatch),"manifest":str(out / "material-manifest.json")
    },sort_keys=True))


if __name__ == "__main__":
    main()
