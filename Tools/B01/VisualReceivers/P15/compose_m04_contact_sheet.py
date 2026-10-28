#!/usr/bin/env python3
"""Compose actual Blender-rendered M04 orthographic PNG frames into a contact sheet."""
import hashlib
import json
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat

def main():
    folder=Path(sys.argv[1])
    labels=("bottom","side","three_quarter")
    frames=(1,31)
    cellw,cellh=640,640
    receipt_path=folder/"M04-original-glb-lookdev.json"
    receipt=json.loads(receipt_path.read_text())
    assert receipt["actual_imported_pivots"]==4
    assert receipt["real_pose_change_in_imported_animation"] is True
    sheet=Image.new("RGB",(1920,1455),(10,19,32))
    draw=ImageDraw.Draw(sheet)
    font=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",24)
    smaller=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",18)
    title=ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",33)
    draw.text((33,13),"M04-024 / QUAD GIMBAL NOZZLES — ORIGINAL GLB IMPORT",(231,243,247),font=title)
    draw.text((33,57),"B01-30 independent orthographic contact sheet • Blender source render • NOT UNITY",
              fill=(164,197,212),font=smaller)
    outcomes={}
    for row,frame in enumerate(frames):
        for col,label in enumerate(labels):
            path=folder/f"M04-{label}-frame{frame:03}.png"
            im=Image.open(path).convert("RGB")
            assert im.size==(cellw,cellh),path
            stddev=ImageStat.Stat(im).stddev
            assert max(stddev)>8,(path,"blank render")
            x,y=col*cellw,83+row*cellh
            sheet.paste(im,(x,y))
            draw.rectangle((x+10,y+10,x+310,y+47),fill=(11,27,44))
            draw.text((x+21,y+16),f"{label.upper()} / FRAME {frame:03}",
                      fill=(224,238,245),font=font)
            outcomes[path.name]={"bytes":path.stat().st_size,
                "sha256":hashlib.sha256(path.read_bytes()).hexdigest(),
                "stddev":[round(v,2) for v in stddev]}
    for col,label in enumerate(labels):
        rest=Image.open(folder/f"M04-{label}-frame001.png").convert("RGB")
        active=Image.open(folder/f"M04-{label}-frame031.png").convert("RGB")
        bb=ImageChops.difference(rest,active).getbbox()
        outcomes[f"{label}_frame_difference_bbox"]=list(bb) if bb else None
    draw.text((26,1377),"SOURCE: M04 #196 @ eef02864 · 1 metre/unit · +Z UP · exhaust -Z · no actual collision/Unity acceptance",
              fill=(246,191,122),font=smaller)
    draw.text((26,1405),"3 views × 2 real imported animation poses. Mount/gimbal visuals only; NOT game physics.",
              fill=(156,192,208),font=smaller)
    path=folder/"M04-024-orthographic-contact-sheet.png"
    sheet.save(path)
    outcomes["contact_sheet"]={"bytes":path.stat().st_size,
       "sha256":hashlib.sha256(path.read_bytes()).hexdigest(),
       "dimensions":list(sheet.size)}
    receipt["render_file_proof"]=outcomes
    receipt_path.write_text(json.dumps(receipt,indent=2)+"\n")
    print("B01_30_M04_PHYSICAL_GLB_RENDER_CONTACT_SHEET_PASS",json.dumps(outcomes,sort_keys=True))
if __name__=="__main__":main()
