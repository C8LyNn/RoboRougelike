"""Check original Codex LD02 change scope from actual generated MAPDIFF JSON + SVG."""
import argparse
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET


def main():
    cli = argparse.ArgumentParser()
    cli.add_argument("--output-dir", type=Path, required=True)
    options = cli.parse_args()
    json_file = options.output_dir / "mapdiff.json"
    svg_file = options.output_dir / "mapdiff.svg"
    info = json.loads(json_file.read_text(encoding="utf-8"))
    assert info["schema"] == "B01-62-MAPDIFF-001/v1"
    assert info["before"]["version"] == info["after"]["version"] == 2
    assert info["before"]["items"] == 168
    assert info["after"]["items"] == 173
    assert info["before"]["sha256"] == "50e5b7e84bc51425080e91199abab96266a5615b84aaa919caa9cfff2fa3c115"
    assert info["after"]["sha256"] == "2c9947884b957fab5ba5eabdd6f8cc3054f8683413893618b0a18da53cfa5ce5"
    assert len(info["addedItems"]) == 11, "Real original LD02 additions changed unexpectedly"
    assert len(info["removedItems"]) == 6, "Real original LD02 removals changed unexpectedly"
    assert info["unchangedItems"] == 162
    assert len(info["changedCells"]) >= 11
    assert info["globalFloorChanged"] is False
    assert info["noChanges"] is False
    added_walls = sorted((i["x"], i["z"]) for i in info["addedItems"] if i["kind"] == 1)
    assert added_walls == [(4, 4), (5, 4), (6, 4), (7, 4), (8, 4)]
    assert sum(i["kind"] == 7 for i in info["addedItems"]) == 4
    assert sum(i["kind"] == 8 for i in info["addedItems"]) == 2
    assert sum(i["kind"] == 7 for i in info["removedItems"]) == 4
    assert sum(i["kind"] == 8 for i in info["removedItems"]) == 2
    svg = svg_file.read_bytes()
    assert b'aria-label="Map changes:' in svg
    root = ET.fromstring(svg)
    assert root.tag == "{http://www.w3.org/2000/svg}svg"
    evidence = {"result": "PASS", "two_original_source_maps": [168, 173],
                "new_objects": 11, "removed_objects": 6,
                "unchanged_objects": 162, "real_new_wall_cells": 5,
                "changed_footprint_cells": len(info["changedCells"]),
                "original_svg_sha256": hashlib.sha256(svg).hexdigest(),
                "native_unity": "NOT_RUN"}
    print("MAPDIFF_ORIGINAL_LD02_PASS=" + json.dumps(evidence, sort_keys=True))


if __name__ == "__main__":
    main()
