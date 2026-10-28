#!/usr/bin/env python3
"""B01-30 PASS16 dimensionless motion study from signed original B01-31 A1 SVG.

These are 2D illustration transformations, NOT mechanical travel acceptance,
pivot measurements, Unity physics, or a new 3D MODEL_JOB.
"""
import copy
import hashlib
import io
import json
import math
from pathlib import Path
import xml.etree.ElementTree as ET

import cairosvg
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat

import selected_wheel_techart as origin

NS="{http://www.w3.org/2000/svg}"
OUT=Path("build/B01-30/P16-A1-motion-proposal")
PIVOT=(433,505)  # only an illustrative, 2D original drawing circle center
HUB=(1159,533)   # exact wheel hub 2D SVG ellipse center
ANGLES=(-12,0,12)  # explicitly unapproved visualization angles, not suspension spec

def draw_frame(defs,nodes,angle):
    # Original B01-31 geometry is never modified, only independently referenced.
    page=ET.Element(NS+"svg",{"width":"1600","height":"1000",
        "viewBox":"190 185 1220 660","aria-label":"Proposed 2D wheel sweep, not physical engineering"})
    page.append(copy.deepcopy(defs))
    ET.SubElement(page,NS+"rect",{"x":"190","y":"185","width":"1220","height":"660","fill":"#0b1628"})
    for x in range(190,1420,80):
        ET.SubElement(page,NS+"path",{"d":f"M{x} 185V845","stroke":"#335064",
            "stroke-opacity":"0.3","stroke-width":"1"})
    for y in range(185,850,80):
        ET.SubElement(page,NS+"path",{"d":f"M190 {y}H1410","stroke":"#335064",
            "stroke-opacity":"0.3","stroke-width":"1"})
    # Only the owner-drawn mounting bracket is held fixed; no chassis imagined.
    page.append(copy.deepcopy(nodes["part-mount-proposal"]))
    rot=ET.SubElement(page,NS+"g",{"id":"proposed-rigid-arm-wheel-only",
        "transform":f"rotate({angle} {PIVOT[0]} {PIVOT[1]})",
        "data-motion":"ILLUSTRATIVE_2D_ONLY"})
    rot.append(copy.deepcopy(nodes["part-support-arm"]))
    rot.append(copy.deepcopy(nodes["part-wheel"]))
    # Do NOT animate the owner-drawn damper: linkage geometry isn't specified.
    # A vector proposal cannot assert compression, wheel grip or collision.
    ET.SubElement(page,NS+"circle",{"cx":str(PIVOT[0]),"cy":str(PIVOT[1]),
        "r":"17","fill":"none","stroke":"#ffd08b","stroke-width":"4"})
    ET.SubElement(page,NS+"path",{"d":f"M{PIVOT[0]-23} {PIVOT[1]}H{PIVOT[0]+23} M{PIVOT[0]} {PIVOT[1]-23}V{PIVOT[1]+23}",
        "stroke":"#ffd08b","stroke-width":"2"})
    text=ET.SubElement(page,NS+"text",{"x":"245","y":"242","font-size":"27",
        "font-family":"DejaVu Sans,sans-serif","font-weight":"700","fill":"#f7e1bf"})
    text.text=f"ILLUSTRATIVE ARM SWEEP {angle:+d} deg / NOT APPROVED"
    note=ET.SubElement(page,NS+"text",{"x":"235","y":"809","font-size":"20",
        "font-family":"DejaVu Sans,sans-serif","fill":"#c4e2e8"})
    note.text="DAMPER OMITTED: NO KINEMATIC LINKAGE / NO REAL METRES OR COLLIDERS"
    return ET.tostring(page,encoding="utf-8",xml_declaration=True)

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    sha=origin.git("rev-parse",f"{origin.SOURCE_COMMIT}:{origin.SOURCE_PATH}").decode().strip()
    assert sha==origin.SOURCE_BLOB,(sha,"source not pinned")
    raw=origin.git("cat-file","blob",sha)
    assert hashlib.sha1(f"blob {len(raw)}\0".encode()+raw).hexdigest()==sha
    svg=ET.fromstring(raw)
    defs=next(x for x in svg if x.tag==NS+"defs")
    groups={x.attrib["id"]:x for x in svg.iter() if "id" in x.attrib}
    assert all(x in groups for x in ("part-wheel","part-support-arm","part-mount-proposal"))
    assert groups["part-mount-proposal"].attrib["data-status"]=="DESIGN_PROPOSAL_UNKNOWN"
    frames=[]
    screen_positions={}
    for angle in ANGLES:
        source=draw_frame(defs,groups,angle)
        (OUT/f"A1-kinematic-proposal-{angle:+03d}.svg").write_bytes(source)
        data=cairosvg.svg2png(bytestring=source,output_width=640,output_height=360)
        im=Image.open(io.BytesIO(data)).convert("RGB")
        assert im.size==(640,360)
        assert max(ImageStat.Stat(im).stddev)>12
        (OUT/f"A1-kinematic-proposal-{angle:+03d}.png").write_bytes(data)
        frames.append((angle,im))
        theta=math.radians(angle)
        dx,dy=HUB[0]-PIVOT[0],HUB[1]-PIVOT[1]
        screen_positions[str(angle)]=[round(PIVOT[0]+dx*math.cos(theta)-dy*math.sin(theta),2),
                                      round(PIVOT[1]+dx*math.sin(theta)+dy*math.cos(theta),2)]
    # All 3 candidate illustrations must demonstrably differ, without
    # attributing their pixel changes to a real physical simulation.
    for i in range(len(frames)-1):
        assert ImageChops.difference(frames[i][1],frames[i+1][1]).getbbox()
    contact=Image.new("RGB",(1920,470),(11,22,39))
    draw=ImageDraw.Draw(contact)
    for i,(angle,im) in enumerate(frames):
        contact.paste(im,(640*i,42))
        draw.text((640*i+15,9),f"A1 approved CONCEPT / rotation {angle:+d} deg (PROPOSED 2D)",
                  fill=(240,226,202))
    draw.text((26,420),"Original B01-31 SVG only. 2D pivot hypothesis: (433,505)px. Damper linkage excluded.",
              fill=(241,194,137))
    draw.text((26,443),"No MODEL_JOB. No physical footprint, socket, steering arc, mass, or PhysX proof.",
              fill=(187,211,222))
    pic=OUT/"A1-wheel-arm-motion-proposal-contact.png"
    contact.save(pic)
    assert pic.stat().st_size>20000
    result={
        "kind":"B01-30-P16-GDESIGN-2D-ILLUSTRATIVE-MOTION-NOT_PHYSICS",
        "input_exact_commit":origin.SOURCE_COMMIT,
        "input_git_blob_sha1":sha,
        "angles_degrees_visual_only":list(ANGLES),
        "fixed_original_group":"part-mount-proposal",
        "rotated_original_groups":["part-support-arm","part-wheel"],
        "excluded_unknown_kinematic_group":"part-damper-visual",
        "screen_space_pivot_candidate_px":list(PIVOT),
        "screen_space_hub_xy_px":list(HUB),
        "transformed_hub_screen_points_px":screen_positions,
        "rendered_from_original_source":True,
        "actual_images_written":4,
        "all_pose_pngs_distinct":True,
        "contact_png_sha256":hashlib.sha256(pic.read_bytes()).hexdigest(),
        "gates":["MODEL_JOB_NOT_AUTHORIZED","MOUNT_SOCKET_MASS_CLEARANCE_UNMEASURED",
                 "UNREAL_PHYSX_NOT_RUN","Unity_NOT_RUN"],
        "result":"ORIGINAL_SVG_PIVOT_PROPOSAL_RENDER_PASS"
    }
    (OUT/"A1-motion-study-receipt.json").write_text(json.dumps(result,indent=2)+"\n")
    print("B01_30_P16_ORIGINAL_A1_MOTION_PREVIEW_PASS",json.dumps(result,sort_keys=True))

if __name__=="__main__":main()
