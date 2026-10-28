#!/usr/bin/env python3
"""B01-30 independent TECH-ART A1 selected-part SVG receiver, NOT a MODEL_JOB.

Runs on immutable original B01-31 source from git, creates visual overlays,
isolated component layers, transparent alpha masks and evidence JSON.
Image-space coordinates are NOT metres or verified game pivots.
"""
from __future__ import annotations

import copy
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

import cairosvg
from PIL import Image, ImageChops

SOURCE_COMMIT="8709cf27901c4edca1434cbc760dec5db130a3ef"
SOURCE_PATH="Docs/B01Concept/CONCEPT-MOBILITY-001/A1-single-wheel-arm.svg"
SOURCE_BLOB="96cc46a7c14f94a45c387e102d5940cb787d2d5c"
SVG="{http://www.w3.org/2000/svg}"
PARTS=("part-wheel","part-support-arm","part-damper-visual","part-mount-proposal")
OUT=Path(os.environ.get("B01_TECHART_OUT","build/B01-30/A1-techart"))

def git(*args):
    return subprocess.run(["git",*args],check=True,capture_output=True).stdout

def emit(name,raw,px=(1600,1000)):
    path=OUT/name
    path.parent.mkdir(parents=True,exist_ok=True)
    png=cairosvg.svg2png(bytestring=raw,output_width=px[0],output_height=px[1])
    path.write_bytes(png)
    im=Image.open(io.BytesIO(png)).convert("RGBA")
    assert im.size==px
    return im

def fresh_svg(defs,*content,width=1600,height=1000):
    root=ET.Element(SVG+"svg",{"width":str(width),"height":str(height),
        "viewBox":f"0 0 {width} {height}"})
    root.append(copy.deepcopy(defs))
    for item in content:root.append(copy.deepcopy(item))
    return ET.tostring(root,encoding="utf-8",xml_declaration=True)

def main():
    sha=git("rev-parse",f"{SOURCE_COMMIT}:{SOURCE_PATH}").decode().strip()
    assert sha==SOURCE_BLOB,("B01-31 source changed, FAIL CLOSED",sha)
    raw=git("cat-file","blob",sha)
    assert hashlib.sha1(f"blob {len(raw)}\0".encode()+raw).hexdigest()==sha
    root=ET.fromstring(raw); assert root.attrib["viewBox"]=="0 0 1600 1000"
    nodes={node.attrib["id"]:node for node in root.iter() if "id" in node.attrib}
    assert all(k in nodes for k in PARTS)
    assert nodes["part-mount-proposal"].attrib["data-status"]=="DESIGN_PROPOSAL_UNKNOWN"
    assert nodes["subject-root"].attrib.get("data-subject")=="single-detachable-part"
    defs=next(n for n in root if n.tag==SVG+"defs")
    OUT.mkdir(parents=True,exist_ok=True)
    original=emit("A1-original-full.png",raw)
    # Source-backed segmentation: isolated ORIGINAL semantic vector groups;
    # alpha-mask extents are screen-space only, not collision hulls.
    boxes={}; alpha_shapes={}
    for part in PARTS:
        one=fresh_svg(defs,nodes[part])
        partimg=emit(f"layers/{part}.png",one)
        alpha=partimg.getchannel("A")
        bb=alpha.getbbox()
        assert bb is not None,(part,"blank semantic part")
        boxes[part]=list(bb)
        alpha_shapes[part]=alpha
    wheelbox=boxes["part-wheel"]
    mountbox=boxes["part-mount-proposal"]
    armbox=boxes["part-support-arm"]
    # Illustration-derived proposals ONLY. Not validated physical measurements.
    proposed_axle=(1159,533)  # explicit original SVG wheel hub ellipse cx/cy
    proposed_mount=(310,532)  # centre of dashed proposal on illustration
    assert wheelbox[0]<=proposed_axle[0]<=wheelbox[2] and wheelbox[1]<=proposed_axle[1]<=wheelbox[3]
    assert mountbox[0]<=proposed_mount[0]<=mountbox[2] and mountbox[1]<=proposed_mount[1]<=mountbox[3]
    assert armbox[0]<wheelbox[2] and armbox[2]>mountbox[0]
    # Crop all subject groups to source-local illustration region.
    subject=fresh_svg(defs,nodes["subject-root"])
    emit("A1-source-subject.png",subject)
    annotate=ET.fromstring(raw)
    sheet=next(n for n in annotate if n.attrib.get("id")=="sheet-annotations")
    annotate.remove(sheet)  # replace author annotations with our receiver distinctions
    overlay=ET.SubElement(annotate,SVG+"g",{"id":"b01-30-receiver-overlay",
        "font-family":"DejaVu Sans,Arial,sans-serif"})
    points=(
        ("AXLE? 2D only",proposed_axle,"#6af0ee",(1025,190)),
        ("MOUNT? illustrative",proposed_mount,"#ffc677",(105,675)),
        ("ARM PIVOT? unknown",(433,505),"#a6beff",(495,750)),
    )
    for label,(x,y),color,(tx,ty) in points:
        ET.SubElement(overlay,SVG+"circle",{"cx":str(x),"cy":str(y),"r":"19",
            "stroke":color,"stroke-width":"5","fill":"none","stroke-dasharray":"8 5"})
        ET.SubElement(overlay,SVG+"path",{"d":f"M{x} {y}L{tx} {ty-18}",
            "stroke":color,"stroke-width":"3","fill":"none"})
        ET.SubElement(overlay,SVG+"rect",{"x":str(tx-7),"y":str(ty-43),
            "width":"385","height":"42","rx":"6","fill":"#081220","stroke":color})
        text=ET.SubElement(overlay,SVG+"text",{"x":str(tx),"y":str(ty-14),
            "font-size":"23","font-weight":"700","fill":color})
        text.text=label
    subtitle=ET.SubElement(overlay,SVG+"text",{"x":"120","y":"926",
        "font-size":"20","fill":"#ffe1a8"})
    subtitle.text="TECH ART / PROPOSALS ONLY — NO XY→XYZ, NO SOCKET, MASS, COLLIDER, TRAVEL OR G1"
    annotated=ET.tostring(annotate,encoding="utf-8",xml_declaration=True)
    (OUT/"A1-annotated-receiver.svg").write_bytes(annotated)
    rgba=emit("A1-annotated-receiver.png",annotated)
    assert ImageChops.difference(original.convert('RGB'),rgba.convert('RGB')).getbbox() is not None
    # One actual mounting visual overlap risk test, measured in illustration pixels:
    # overlap of silhouette layers, NOT an actual collision or clearance.
    overlaps={}
    for a,b in (("part-wheel","part-support-arm"),
                ("part-mount-proposal","part-support-arm"),
                ("part-wheel","part-damper-visual")):
        mask_a=alpha_shapes[a].point(lambda x: 255 if x>10 else 0)
        mask_b=alpha_shapes[b].point(lambda x: 255 if x>10 else 0)
        overlap=ImageChops.multiply(mask_a,mask_b)
        overlaps[a+"__"+b]=overlap.getbbox()
    report={
        "task":"B01-30-P15-A1-SINGLE-PART-TECHART-RECEIVER",
        "original_git_commit":SOURCE_COMMIT,"original_svg_git_blob":sha,
        "original_svg_bytes":len(raw),
        "source_semantic_groups":list(PARTS),
        "individual_layer_nonblank":4,
        "layer_canvas_bboxes_pixels":boxes,
        "layer_pair_overlap_pixel_bounds":{k:(list(v) if v else None) for k,v in overlaps.items()},
        "proposed_2d_axle_only":list(proposed_axle),
        "proposed_2d_mount_only":list(proposed_mount),
        "proposed_arm_hinge_2d_only":[433,505],
        "hard_gate":"PHYSICAL_3D_PIVOTS_SOCKET_MASS_CLEARANCE_TRAVEL_COLLIDERS_UNKNOWN",
        "outcome":"VISUAL_TECHART_RESEARCH_ONLY",
        "actual_image_execution":"CairoSVG+Pillow raster of original immutable SVG and layers",
        "unity_native":"NOT_RUN","model_job":"NOT_AUTHORIZED"}
    (OUT/"A1-techart-receipt.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("B01_30_SELECTED_ART_ACTUAL_PASS",json.dumps(report,sort_keys=True))
if __name__=="__main__":main()
