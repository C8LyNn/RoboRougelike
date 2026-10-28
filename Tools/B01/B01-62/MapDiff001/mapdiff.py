#!/usr/bin/env python3
"""MAPDIFF-001: read-only MapDocument v1/v2 footprint-aware visual comparison."""
import argparse
from collections import Counter
import hashlib
from html import escape
import json
from pathlib import Path
import sys

MAX_FILE = 1024 * 1024
MAX_COORD = 50
MAX_SCALE = 4
KIND_NAMES = ("Ground", "Wall", "Ramp", "Obstacle", "Spawn",
              "Finish", "Platform", "Pillar", "LowBarrier", "Steps")
PALETTE = ("#a5abb1", "#374e74", "#a48957", "#c17848", "#27a56c",
           "#e7bf53", "#7c88a0", "#a36b97", "#a78644", "#8d8f99")


class InvalidMap(Exception):
    pass


def check(ok, message):
    if not ok:
        raise InvalidMap(message)


def guard(path):
    path = path.absolute()
    for component in (path, *path.parents):
        check(not component.is_symlink(), "Symbolic links not allowed")


def footprint(item):
    kind, x, z, rot, sx, sy, sz = item
    for dx in range(sx):
        for dz in range(sz):
            rx, rz = ((dx, dz), (dz, -dx), (-dx, -dz), (-dz, dx))[rot]
            yield x + rx, z + rz


def mapdocument(raw):
    try:
        data = json.loads(raw.decode("utf-8-sig"))
    except (UnicodeError, ValueError) as exc:
        raise InvalidMap("Malformed UTF-8 / JSON: " + str(exc))
    check(type(data) is dict and type(data.get("version")) is int and
          data["version"] in (1, 2), "Only MapDocument v1/v2 supported")
    version = data["version"]
    global_floor = data.get("globalFloor", False) if version == 2 else False
    check(type(global_floor) is bool, "Invalid globalFloor")
    entries = data.get("items")
    check(type(entries) is list and len(entries) <= 4000, "Invalid item list")
    items = []
    cells = {"ground": {}, "objects": {}}
    counted = [0, 0]
    unique = [0, 0]
    for index, item in enumerate(entries):
        check(type(item) is dict and all(type(item.get(t)) is int for t in
                                         ("kind", "x", "z", "rotation")),
              "Invalid kind/position/rotation at index " + str(index))
        kind, x, z, rot = (item[t] for t in ("kind", "x", "z", "rotation"))
        sx, sy, sz = ((1, 1, 1) if version == 1 else
                      tuple(item.get(k, 1) for k in ("scaleX", "scaleY", "scaleZ")))
        check(0 <= kind < len(KIND_NAMES) and -MAX_COORD <= x <= MAX_COORD and
              -MAX_COORD <= z <= MAX_COORD and 0 <= rot <= 3 and
              all(type(v) is int and 1 <= v <= MAX_SCALE for v in (sx, sy, sz)),
              "Out-of-range item/scale at index " + str(index))
        normalized = (kind, x, z, rot, sx, sy, sz)
        layer = "ground" if kind == 0 else "objects"
        counted[kind != 0] += 1
        check(counted[kind != 0] <= 2000, "More than 2000 items on a layer")
        if kind in (4, 5):
            unique[kind - 4] += 1
            check(unique[kind - 4] <= 1, "Duplicate spawn/finish")
        for cell in footprint(normalized):
            check(all(-MAX_COORD <= v <= MAX_COORD for v in cell),
                  "Rotated footprint out of bounds")
            check(cell not in cells[layer], "Overlapping " + layer + " footprint")
            cells[layer][cell] = normalized
        items.append(normalized)
    return {"version": version, "globalFloor": global_floor, "items": items,
            "cells": cells, "sourceSHA256": hashlib.sha256(raw).hexdigest()}


def load_map(path):
    path = Path(path)
    guard(path)
    check(path.is_file() and path.stat().st_size <= MAX_FILE,
          "Map must exist and be no larger than 1MiB")
    raw = path.read_bytes()
    check(len(raw) <= MAX_FILE, "Map exceeded 1MiB during read")
    parsed = mapdocument(raw)
    check(path.read_bytes() == raw, "Map changed during read")
    return parsed


def display_item(item):
    kind, x, z, rot, sx, sy, sz = item
    return {"kind": kind, "name": KIND_NAMES[kind], "x": x, "z": z,
            "rotation": rot, "scaleX": sx, "scaleY": sy, "scaleZ": sz}


def semantic_sha(document):
    canonical = {"globalFloor": document["globalFloor"],
                 "items": [list(i) for i in sorted(document["items"])]}
    return hashlib.sha256(json.dumps(canonical, separators=(",", ":"),
                                     sort_keys=True).encode()).hexdigest()


def compare(before, after):
    old = Counter(before["items"])
    new = Counter(after["items"])
    removed = sorted((old - new).elements())
    added = sorted((new - old).elements())
    changes = []
    counters = {}
    for layer in ("ground", "objects"):
        a, b = before["cells"][layer], after["cells"][layer]
        counts = {"added": 0, "removed": 0, "replaced": 0}
        for x, z in sorted(a.keys() | b.keys()):
            left, right = a.get((x, z)), b.get((x, z))
            if left == right:
                continue
            state = "added" if left is None else ("removed" if right is None else "replaced")
            counts[state] += 1
            changes.append({"layer": layer, "x": x, "z": z, "status": state,
                            "before": None if left is None else display_item(left),
                            "after": None if right is None else display_item(right)})
        counters[layer] = counts
    return {
        "schema": "B01-62-MAPDIFF-001/v1",
        "before": {"version": before["version"], "items": len(before["items"]),
                   "globalFloor": before["globalFloor"],
                   "sha256": before["sourceSHA256"], "semanticSHA256": semantic_sha(before)},
        "after": {"version": after["version"], "items": len(after["items"]),
                  "globalFloor": after["globalFloor"],
                  "sha256": after["sourceSHA256"], "semanticSHA256": semantic_sha(after)},
        "unchangedItems": sum((old & new).values()),
        "addedItems": [display_item(v) for v in added],
        "removedItems": [display_item(v) for v in removed],
        "globalFloorChanged": before["globalFloor"] != after["globalFloor"],
        "changedCells": changes,
        "cellSummary": counters,
        "noChanges": not removed and not added and before["globalFloor"] == after["globalFloor"]
    }


def svg(before, after, report):
    universe = set()
    for stage in (before, after):
        for layer in ("ground", "objects"):
            universe.update(stage["cells"][layer].keys())
    universe.update((r["x"], r["z"]) for r in report["changedCells"])
    if not universe:
        universe = {(0, 0)}
    xs, zs = zip(*universe)
    xmin, xmax, zmin, zmax = min(xs) - 1, max(xs) + 1, min(zs) - 1, max(zs) + 1
    width, height = xmax - xmin + 1, zmax - zmin + 1
    unit = min(380 / width, 360 / height, 26)
    margin, top = 22, 94
    panel_w = width * unit + 2 * margin
    panel_h = height * unit + top + 36
    canvas_w, canvas_h = panel_w * 3 + 24 * 2, panel_h + 30
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{canvas_w:.1f}" '
           f'height="{canvas_h:.1f}" viewBox="0 0 {canvas_w:.1f} {canvas_h:.1f}" '
           f'role="img" aria-label="Map changes: before, after and changed cells">',
           '<rect width="100%" height="100%" fill="#111a28"/>',
           '<style>text{font-family:Arial,sans-serif} .heading{fill:#eaf0fb;font-size:16px;font-weight:bold}'
           '.sub{fill:#9fb4d2;font-size:11px}.cell{stroke:#243042;stroke-width:.4}</style>']
    statuses = {(r["layer"], r["x"], r["z"]): r["status"]
                for r in report["changedCells"]}
    for p, (label, doc) in enumerate((("BEFORE", before), ("AFTER", after),
                                      ("CHANGED CELLS", None))):
        off_x = p * (panel_w + 24)
        out.append(f'<g transform="translate({off_x:.1f},0)">')
        out.append(f'<rect x="0" y="0" width="{panel_w:.1f}" height="{panel_h:.1f}" '
                   'rx="12" fill="#1c283b" stroke="#435572"/>')
        out.append(f'<text x="{margin}" y="32" class="heading">{label}</text>')
        if doc:
            subtitle = f'{len(doc["items"])} objects/tiles | v{doc["version"]} | globalFloor={doc["globalFloor"]}'
        else:
            subtitle = (f'{len(report["changedCells"])} changed footprint cells | '
                        f'+{len(report["addedItems"])} / -{len(report["removedItems"])} items')
        out.append(f'<text x="{margin}" y="53" class="sub">{escape(subtitle)}</text>')
        if doc:
            for layer in ("ground", "objects"):
                for (x, z), item in sorted(doc["cells"][layer].items()):
                    px, py = margin + (x - xmin) * unit, top + (zmax - z) * unit
                    kind = item[0]
                    stroke = "#2d3b56" if layer == "ground" else "#0e1522"
                    out.append(f'<rect x="{px:.2f}" y="{py:.2f}" '
                               f'width="{unit:.2f}" height="{unit:.2f}" '
                               f'fill="{PALETTE[kind]}" stroke="{stroke}" '
                               f'stroke-width="{0.6 if layer == "ground" else 1.2}">'
                               f'<title>{escape(KIND_NAMES[kind])} @ {x},{z}</title></rect>')
        else:
            grouped = {}
            for (layer, x, z), status in statuses.items():
                grouped.setdefault((x, z), set()).add(status)
            for (x, z), states in sorted(grouped.items()):
                px, py = margin + (x - xmin) * unit, top + (zmax - z) * unit
                state = sorted(states)[0] if len(states) == 1 else "mixed"
                fill = {"added": "#2bcfa2", "removed": "#e87777",
                        "replaced": "#f3be5a", "mixed": "#cf87f1"}[state]
                out.append(f'<rect x="{px:.2f}" y="{py:.2f}" width="{unit:.2f}" '
                           f'height="{unit:.2f}" fill="{fill}" class="cell">'
                           f'<title>{escape(",".join(sorted(states)))} @ {x},{z}</title></rect>')
        for x in range(xmin, xmax + 2):
            xx = margin + (x - xmin) * unit
            out.append(f'<line x1="{xx:.2f}" y1="{top}" x2="{xx:.2f}" '
                       f'y2="{top + height * unit:.2f}" stroke="#6c7890" stroke-opacity=".15"/>')
        for z in range(zmin, zmax + 2):
            yy = top + z * unit - zmin * unit
            out.append(f'<line x1="{margin}" y1="{yy:.2f}" '
                       f'x2="{margin + width * unit:.2f}" y2="{yy:.2f}" '
                       'stroke="#6c7890" stroke-opacity=".15"/>')
        out.append('</g>')
    out.append(f'<text x="14" y="{canvas_h - 8:.1f}" class="sub">'
               'Legend: green=added, red=removed, amber=replaced, purple=mixed | '
               'Grid origin uses game x/z; north at top</text></svg>')
    return "\n".join(out) + "\n"


def run(before_path, after_path, output_dir):
    before_path, after_path = Path(before_path), Path(after_path)
    before, after = load_map(before_path), load_map(after_path)
    report = compare(before, after)
    output = Path(output_dir)
    guard(output)
    check(output.is_dir(), "Output directory must exist")
    for source in (before_path, after_path):
        check(output.resolve() != source.resolve().parent,
              "Output must be separate from input map directory")
    json_file, svg_file = output / "mapdiff.json", output / "mapdiff.svg"
    for file in (json_file, svg_file):
        guard(file)
        check(not file.exists(), "Refusing to overwrite prior comparison output")
    json_body = json.dumps(report, sort_keys=True, indent=2, ensure_ascii=False) + "\n"
    svg_body = svg(before, after, report)
    try:
        # Binary creation prevents Windows text-mode CRLF rewriting the
        # output. Identical source inputs must yield identical byte hashes
        # on Windows and POSIX (not merely semantically equal SVG/JSON).
        with json_file.open("xb") as f:
            f.write(json_body.encode("utf-8"))
        with svg_file.open("xb") as f:
            f.write(svg_body.encode("utf-8"))
    except OSError:
        if json_file.exists() and not svg_file.exists():
            json_file.unlink()
        raise
    return report


def main():
    p = argparse.ArgumentParser(description="Read-only map geometry diff and SVG preview")
    p.add_argument("--before", required=True)
    p.add_argument("--after", required=True)
    p.add_argument("--out-dir", required=True)
    args = p.parse_args()
    try:
        r = run(args.before, args.after, args.out_dir)
        print(json.dumps({"status": "PASS", "added": len(r["addedItems"]),
                          "removed": len(r["removedItems"]),
                          "changedCells": len(r["changedCells"]),
                          "noChanges": r["noChanges"]}))
        return 0
    except (InvalidMap, OSError) as e:
        print(json.dumps({"status": "REJECTED", "error": str(e)}), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
