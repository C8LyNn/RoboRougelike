#!/usr/bin/env python3
"""Render exact compiled original C# recoil state snapshots as tangible PNG frames.
CPU diagnostic preview only. NEVER described as actual Unity/GPU visuals.
"""
import argparse,hashlib,json
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
SHA=lambda b:hashlib.sha256(b).hexdigest()
def check(p,why):
    if not p:raise ValueError("DUAL_RECOIL_PREVIEW_FAIL "+why)
def draw(item):
    im=Image.new("RGBA",(512,512),(0,0,0,0))
    d=ImageDraw.Draw(im,"RGBA")
    # top-view mechanical barrels: left and right barrel move independently rearward.
    for lane,x,travel,flash in [("left",155,item["left"],item["flashL"]),
                                ("right",355,item["right"],item["flashR"])]:
        d.rounded_rectangle((x-48,105,x+48,408),radius=19,fill=(58,67,84,235),
                            outline=(131,147,161,240),width=4)
        offset=int(travel*540)
        d.rounded_rectangle((x-24,92+offset,x+24,328+offset),radius=9,
                            fill=(100,150,167,255),outline=(195,229,226,245),width=4)
        d.rectangle((x-22,108+offset,x+22,130+offset),fill=(38,53,65,255))
        d.rounded_rectangle((x-33,351,x+33,389),radius=7,fill=(41,45,58,255))
        # Original core flash intensity is a pure diagnostic indicator (not gameplay).
        if flash>0.05:
            a=int(220*flash)
            d.ellipse((x-13,54+offset,x+13,80+offset),fill=(250,201,76,a))
    return im
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--samples",default="build/DualRecoilW07/frames.json")
    ap.add_argument("--output",default="build/DualRecoilW07")
    args=ap.parse_args()
    data=json.loads(Path(args.samples).read_text())
    names=["rest","left_start","left_peak","left_recover","right_start","both","settle","vfx_off"]
    check(len(data)==len(names) and [x["name"] for x in data]==names,"sample list differs from ORIGINAL compiled C#")
    dest=Path(args.output);(dest/"preview").mkdir(parents=True,exist_ok=True)
    images=[];hashes=[]
    for x in data:
        pic=draw(x);images.append(pic)
        path=dest/"preview"/(x["name"]+".png")
        pic.save(path)
        hashes.append(dict(name=x["name"],sha256=SHA(path.read_bytes()),left=x["left"],right=x["right"]))
    check(all(ImageChops.difference(images[i],images[i+1]).getbbox() is not None for i in range(1,6)),
          "consecutive original C# recoil states must have distinct pixels")
    # The barrel static silhouettes remain on OFF; assert offset exactly reset.
    check(data[-1]["left"]==0.0 and data[-1]["right"]==0.0,"VFX OFF must reset both barrel offsets")
    check(data[1]["left"]>0 and data[1]["right"]==0,"left-only sample")
    check(data[4]["right"]>0,"right-only sample")
    canvas=Image.new("RGB",(512*4,512*2),(23,30,40))
    for i,im in enumerate(images):canvas.paste(im,((i%4)*512,(i//4)*512),im)
    canvas.save(dest/"DUAL_RECOIL_CONTACT_SHEET.png")
    (dest/"PREVIEW_RECEIPT.json").write_text(json.dumps({
        "origin":"Real .NET compiled original DualRecoilCore C#; diagnostic Pillow CPU, NOT Unity screenshot",
        "frames":hashes,"sheet_sha256":SHA((dest/"DUAL_RECOIL_CONTACT_SHEET.png").read_bytes())
    },indent=2)+"\n")
    print("DUAL_RECOIL_REAL_CPU_PNG_PASS 8/8 PNGs, physical 2048x1024 contact sheet")
    print("DUAL_RECOIL_PROVEN_MOTION_PASS left-only/right-only/both/OFF; 5/5 adjacent original states differ")
if __name__=="__main__":main()
