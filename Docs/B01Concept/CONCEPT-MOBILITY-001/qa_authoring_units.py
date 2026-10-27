#!/usr/bin/env python3
"""Auditable research SVG authoring-unit gate. No Unity, 3D, physics or HQ selection claims.

Source art is allowed to be an unsegmented, whole-vehicle illustration while
HQ has not selected a detachable part. This tool documents that limitation,
tests the semantic-group detector using synthetic controls, and fails closed
if the research snapshot unexpectedly changes without a new review.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
OUT = HERE / "rendered" / "authoring-unit-source-qa.json"
SVG = "{http://www.w3.org/2000/svg}"
SOURCES = ("A-wheel.svg", "B-track.svg", "C-omni.svg")
DRAWABLE = {"path", "rect", "circle", "ellipse", "line", "polygon", "polyline"}
STATUS = "SOURCE_RESEARCH_ONLY_NOT_PART_MODEL_READY"


def localname(tag: str) -> str:
    return tag.split("}", 1)[-1]


def semantic_groups(root: ET.Element) -> list[ET.Element]:
    return [
        el for el in root.iter()
        if localname(el.tag) == "g" and (
            bool(el.attrib.get("id"))
            or any(key.startswith("data-part") or key.startswith("data-component")
                   or key.startswith("data-module") for key in el.attrib)
        )
    ]


def main() -> int:
    checks: list[dict[str, object]] = []
    sources: list[dict[str, object]] = []

    def verify(name: str, ok: bool) -> None:
        checks.append({"name": name, "status": "PASS" if ok else "FAIL"})

    for name in SOURCES:
        original_bytes = (HERE / name).read_bytes()
        root = ET.fromstring(original_bytes)
        groups = [node for node in root.iter() if localname(node.tag) == "g"]
        drawables = [node for node in root.iter() if localname(node.tag) in DRAWABLE]
        ids = [node.attrib["id"] for node in root.iter() if "id" in node.attrib]
        explicit_parts = semantic_groups(root)

        # This expectation applies ONLY to the current pre-selection research.
        verify(f"{name}:canvas", root.attrib.get("width") == "700"
               and root.attrib.get("height") == "866")
        verify(f"{name}:generic-groups", len(groups) == 2)
        verify(f"{name}:no-component-ids", not ids)
        verify(f"{name}:no-detachable-groups", not explicit_parts)
        verify(f"{name}:nonempty-vector", len(drawables) >= 20)

        # Positive control: detector must see an added semantically named group.
        positive = ET.fromstring(original_bytes)
        positive_groups = [node for node in positive.iter() if localname(node.tag) == "g"]
        if positive_groups:
            positive_groups[-1].set("id", "PART_SYNTHETIC_TEST")
        verify(f"{name}:semantic-detector-positive-control",
               len(semantic_groups(positive)) == 1)
        # Negative control: unmodified original must not be detected as a part.
        verify(f"{name}:semantic-detector-negative-control",
               len(semantic_groups(root)) == 0)

        sources.append({
            "name": name,
            "sha256": hashlib.sha256(original_bytes).hexdigest(),
            "drawables": len(drawables),
            "group_count": len(groups),
            "semantic_detachable_groups": len(explicit_parts),
            "id_count": len(ids),
            "interpretation": "WHOLE_ILLUSTRATION_NOT_A_PART_EXPORT",
        })

    meta = json.loads((HERE / "metadata.json").read_text(encoding="utf-8"))
    verify("metadata:three-research-options", len(meta.get("variants", [])) == 3)
    verify("metadata:all-await-hq", all(
        row.get("hqs_decision") == "PENDING" for row in meta.get("variants", [])))
    verify("metadata:no-part-export-map", not any(
        "export_groups" in k or "component_map" in k or "part_map" in k
        for k in meta.keys()))
    verify("metadata:mounting-unverified",
           "module_mount_compatibility" in meta.get("status_unknown", []))

    failed = [c["name"] for c in checks if c["status"] == "FAIL"]
    result = {
        "purpose": "Detachable-part semantic authoring audit of current research art",
        "test_class": "ORIGINAL_SVG_SOURCE_ONLY_WITH_SYNTHETIC_DETECTOR_CONTROLS",
        "status": "PASS" if not failed else "FAIL",
        "pass_count": len(checks) - len(failed),
        "fail_count": len(failed),
        "research_interpretation": STATUS,
        "sources": sources,
        "checks": checks,
        "not_run": [
            "HQ art selection", "modeling", "Unity", "PhysX", "socket validation",
            "human recognition and visual gameplay evaluation",
        ],
        "handoff": "Require HQ selection and one-part redraw/semantic authoring before Model Lab",
    }
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n",
                   encoding="utf-8")
    print(json.dumps({"status": result["status"], "checks_pass": result["pass_count"],
                      "checks_fail": result["fail_count"],
                      "semantic_part_groups": [s["semantic_detachable_groups"]
                                               for s in sources]}))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
