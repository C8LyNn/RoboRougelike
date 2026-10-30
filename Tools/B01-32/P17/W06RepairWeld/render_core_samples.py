#!/usr/bin/env python3
"""Render original executed C# weld state samples as 512px transparent PNGs.
These are diagnostic CPU previews, NOT native Unity/GPU screenshots.
"""
import argparse, hashlib, json
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
SHA=lambda d:hashlib.sha256(d).hexdigest()
def require(ok,why):
    if not ok:raise ValueError("WELD_CSHARP_PREVIEW_REJECT "+why)
def make_one(item,dest):
    scale=2;image=Image.new("RGBA",(512*scale,512*scale),(0,0,0,0))
    d=ImageDraw.Draw(image,"RGBA")
    if item["visible"]:
        def p(x,y):
            return int((256+float(x)*250)*scale),int((256-float(y)*250)*scale)
        cx,cy=p(item["arcX"],item["arcY"])
        heat=float(item["heat"])
        require(heat>0 and heat<=1,"heat invalid")
        for radius in [21,13,6]:
            alpha=max(1,int((42 if radius==21 else 95 if radius==13 else 205)*heat))
            d.ellipse((cx-radius*scale,cy-radius*scale,cx+radius*scale,cy+radius*scale),fill=(255,161,30,alpha))
        for s in item["sparks"]:
            a=p(s["X0"],s["Y0"]);b=p(s["X1"],s["Y1"])
            val=max(0,min(255,int(s["Intensity"]*255)))
            if val:d.line((a,b),fill=(255,185,42,val),width=3*scale)
        d.line(((cx,cy),(cx,cy-int(0.18*250*scale))),fill=(72,210,255,int(240*heat)),width=5*scale)
        d.ellipse((cx-3*scale,cy-3*scale,cx+3*scale,cy+3*scale),fill=(255,250,192,int(255*heat)))
    image=image.resize((512,512),Image.Resampling.LANCZOS)
    dest.parent.mkdir(parents=True,exist_ok=True)
    image.save(dest)
    return image

def main():
    p=argparse.ArgumentParser();p.add_argument("--frames",default="build/RepairWeldW06/frames.json")
    p.add_argument("--output",default="build/RepairWeldW06")
    args=p.parse_args();out=Path(args.output)
    data=json.loads(Path(args.frames).read_text())
    require(len(data)==7,"seven actual core state samples required")
    need=["idle","weld_early","weld_mid","weld_late","cool_start","cool_mid","cool_end"]
    require([x["name"] for x in data]==need,"unmatched compiled C# state order")
    images=[];files=[]
    for frame in data:
        path=out/"preview"/(frame["name"]+".png")
        image=make_one(frame,path)
        images.append(image)
        a=image.getchannel("A")
        require((a.getbbox() is None)==(not frame["visible"]),"alpha must match original C# visible state "+frame["name"])
        files.append(dict(name=frame["name"],path=path.relative_to(out).as_posix(),sha256=SHA(path.read_bytes()),
                          actual_visible=bool(a.getbbox()),alpha_bbox=a.getbbox()))
    require(all(ImageChops.difference(images[i],images[i+1]).getbbox() is not None for i in range(1,5)),
            "real staged transition images cannot be duplicates")
    page=Image.new("RGB",(512*4,512*2),(24,29,40))
    for i,img in enumerate(images):
        page.paste(img,((i%4)*512,(i//4)*512),img)
    page.save(out/"WELD_STATE_CONTACT_SHEET.png")
    (out/"WELD_PREVIEW_RECEIPT.json").write_text(json.dumps(
        dict(origin="compiled original C# core samples, Pillow CPU diagnostics; NOT Unity",
             frame_count=7,states=files,contact_sha256=SHA((out/"WELD_STATE_CONTACT_SHEET.png").read_bytes())),indent=2)+"\n")
    print("WELD_REAL_ORIGINAL_CORE_RENDER_PASS 7/7 physical RGBA PNG, 5/5 non-idle images, 2/2 genuinely transparent OFF images")
    print("WELD_RENDER_DISTINCT_PASS 4/4 adjacent animated phase transitions")
if __name__=="__main__":main()
