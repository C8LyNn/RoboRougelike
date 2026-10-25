#!/usr/bin/env python3
"""Ten independent orthographic SVG diagnostics from actual model primitive data.
They are wireframe projected geometry, NOT Blender materials/lighting/GLB renders.
"""
from __future__ import annotations
import argparse
import json
import math
from pathlib import Path
from xml.sax.saxutils import escape
from modelqa import primitive_vertices

VIEWS = {
    "front":(0,1,1,1),  "back":(0,-1,1,1),
    "left":(2,1,1,1),   "right":(2,-1,1,1),
    "top":(0,1,2,1),    "bottom":(0,1,2,-1)
}

def segments(part, angle, pivot):
    verts=primitive_vertices(part)
    c,s=math.cos(math.radians(angle)),math.sin(math.radians(angle))
    if part["parent"]=="tilt":
        verts=[(x+pivot[0],y*c-z*s+pivot[1],y*s+z*c+pivot[2]) for x,y,z in verts]
    n=len(verts); pairs=[]
    if part["shape"]=="box":
        # vertices are ordered by itertools.product(x,y,z), neighbours differ in one bit
        for a in range(8):
            for k in (1,2,4):
                b=a^k
                if b>a:pairs.append((a,b))
    elif part["shape"]=="cylinder":
        length=n//2
        for k in range(length):
            pairs.extend(((k,(k+1)%length),
                          (k+length,(k+1)%length+length),(k,k+length)))
    return [(verts[a],verts[b]) for a,b in pairs]

def make_svg(spec,view="front",angle=0,width=640,height=640):
    if view not in VIEWS: raise ValueError("Unknown camera view")
    ax,signx,ay,signy=VIEWS[view]
    seg=[]
    for part in spec["parts"]:
        tint="#4b8ad1" if part["parent"]=="tilt" else "#555d67"
        for a,b in segments(part,angle,spec["hinge_pivot"]):
            seg.append((a,b,tint))
    projected=[((a[ax]*signx,a[ay]*signy),(b[ax]*signx,b[ay]*signy),color)
               for a,b,color in seg]
    points=[p for a,b,_ in projected for p in (a,b)]
    xs=[p[0] for p in points]; ys=[p[1] for p in points]
    xmin,xmax=min(xs),max(xs);ymin,ymax=min(ys),max(ys)
    scale=min((width-80)/max(.01,xmax-xmin),(height-130)/max(.01,ymax-ymin))
    cx=(xmin+xmax)/2;cy=(ymin+ymax)/2
    def p(v): return (width/2+(v[0]-cx)*scale,height/2-(v[1]-cy)*scale)
    out=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
         '<rect width="100%" height="100%" fill="#fff"/>',
         f'<text x="20" y="32" font-size="17" fill="#20242a">{escape(spec["id"])} — {escape(view)} — tilt {angle:g}°</text>',
         '<text x="20" y="55" font-size="12" fill="#666">MODEL-QA / orthographic mesh projection • blue=moving • gray=fixed • not Blender</text>']
    for a,b,color in projected:
        x1,y1=p(a);x2,y2=p(b)
        out.append(f'<line x1="{x1:.2f}" y1="{y1:.2f}" x2="{x2:.2f}" y2="{y2:.2f}" stroke="{color}" stroke-width=".55" opacity=".68"/>')
    out.append('</svg>')
    return "\n".join(out)+"\n"

def render_pack(spec,output):
    output=Path(output);output.mkdir(parents=True,exist_ok=True)
    names=[]
    for view in VIEWS:
        p=output/f"{spec['id']}_{view}_neutral.svg"
        p.write_text(make_svg(spec,view,0),encoding="utf-8")
        names.append(p.name)
    for angle in (-22,-11,0,11,22):
        p=output/f"{spec['id']}_side_{angle:+03d}deg.svg"
        p.write_text(make_svg(spec,"left",angle),encoding="utf-8")
        names.append(p.name)
    return names

def main():
    p=argparse.ArgumentParser()
    p.add_argument("geometry_json",type=Path)
    p.add_argument("--output",type=Path,default=Path("modelqa_report/renders"))
    args=p.parse_args()
    spec=json.loads(args.geometry_json.read_text(encoding="utf8"))
    print(json.dumps({"view_files":render_pack(spec,args.output),
                      "classification":"ACTUAL_PRIMITIVE_PROJECTION_NOT_BLENDER_RENDER"},indent=2))

if __name__=="__main__":main()
