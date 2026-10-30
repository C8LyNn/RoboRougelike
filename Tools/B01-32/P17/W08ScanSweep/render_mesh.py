#!/usr/bin/env python3
"""Render real C#-compiled OBJ geometry in 8 source timeline frames to transparent
CPU diagnostic PNGs. NOT a Unity rendering screenshot or GPU evidence.
"""
import argparse,hashlib,json,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageChops
SHA=lambda b:hashlib.sha256(b).hexdigest()
def require(v,msg):
    if not v:raise ValueError("SCAN_ORIGINAL_MESH_RENDER_RED "+msg)
def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--output",default="build/ScanSweepW08")
    args=ap.parse_args();out=Path(args.output)
    data=json.loads((out/"frames.json").read_text())
    names=["off","start","quarter","half","three_quarters","fade_start","fade_half","fade_end"]
    require(len(data)==8 and [x["name"] for x in data]==names,"expected 8 exact compiled core snapshots")
    lines=(out/"scan-sector.obj").read_text().splitlines()
    vertices=[list(map(float,l[2:].split())) for l in lines if l.startswith("v ")]
    triangles=[list(map(int,l[2:].split())) for l in lines if l.startswith("f ")]
    require(len(vertices)==128 and len(triangles)==64,"original physical 3D mesh geometry wrong")
    require(all(len(t)==3 and all(1<=p<=128 for p in t) for t in triangles),"invalid original OBJ index")
    require(all(abs(p[1]-.015)<.0001 for p in vertices),"original mesh y plane mismatch")
    images=[]
    for snap in data:
        canvas=Image.new("RGBA",(512,512),(0,0,0,0))
        d=ImageDraw.Draw(canvas,"RGBA")
        angle=float(snap["angle"])
        opacity=float(snap["opacity"])
        require(0<=opacity<=1,"opacity finite/bounded")
        if snap["visible"]:
            cos=math.cos(angle);sin=math.sin(angle)
            def project(p):
                x,y,z=vertices[p-1]
                xx=x*cos+z*sin
                zz=-x*sin+z*cos
                return (int(256+xx*105),int(256-zz*105))
            for t in triangles:
                fade=int(145*opacity)
                d.polygon([project(p) for p in t],fill=(37,214,247,fade))
            for v in range(0,128,4):
                d.line([project(v+1),project(v+2)],fill=(107,241,255,int(230*opacity)),width=1)
        images.append(canvas)
        target=out/"preview"/(snap["name"]+".png")
        target.parent.mkdir(parents=True,exist_ok=True)
        canvas.save(target)
        bbox=canvas.getchannel("A").getbbox()
        require((bbox is None)==(not snap["visible"]),"actual source OFF/ON alpha mismatch "+snap["name"])
    require(all(ImageChops.difference(images[i],images[i+1]).getbbox() is not None for i in range(1,6)),
            "moving original 3D scan geometry must produce five different rendered frames")
    contact=Image.new("RGB",(2048,1024),(26,33,44))
    for i,p in enumerate(images):contact.paste(p,((i%4)*512,(i//4)*512),p)
    contact.save(out/"SCAN_SWEEP_CONTACT_SHEET.png")
    receipt={"source":"physical 3D OBJ exported from compiled original C#9, CPU rasterized via Pillow",
      "native_unity":"NOT_RUN","vertex_count":len(vertices),"face_count":len(triangles),
      "obj_sha256":SHA((out/"scan-sector.obj").read_bytes()),
      "states":[{"name":x["name"],"png_sha256":SHA((out/"preview"/(x["name"]+".png")).read_bytes())} for x in data]}
    (out/"RENDER_RECEIPT.json").write_text(json.dumps(receipt,indent=2)+"\n")
    print("SCAN_ORIGINAL_OBJ_GEOMETRY_PASS 128 original compiled C# vertices, 64 real OBJ triangles")
    print("SCAN_PHYSICAL_RENDER_PASS 8/8 actual original mesh-derived RGBA PNGs, 5/5 different motion samples")
if __name__=="__main__":main()
