#!/usr/bin/env python3
"""Independent CPython math and actual exported glTF 2.0 validator: M02-020."""
import json,math,struct,sys
from pathlib import Path
ID="M02-020"
OUT=Path(__file__).resolve().parent/"generated"

def linkage(side,stroke):
    px,py=side*.600,.100
    ax,ay=-side*.080,.080
    qx,qy=side*.355+stroke,.045
    L=math.hypot(px+ax-side*.355,py+ay-.045)
    A=math.hypot(ax,ay)
    dx,dy=qx-px,qy-py
    d=math.hypot(dx,dy)
    c=(A*A+d*d-L*L)/(2*A*d)
    assert -1<=c<=1,(side,stroke,c)
    phi=math.atan2(dy,dx)
    neutral=math.atan2(ay,ax)
    wrap=lambda v:(v+math.pi)%(2*math.pi)-math.pi
    a=min((wrap(phi+math.acos(c)-neutral),
           wrap(phi-math.acos(c)-neutral)),key=abs)
    x=px+ax*math.cos(a)-ay*math.sin(a)
    y=py+ax*math.sin(a)+ay*math.cos(a)
    return a,abs(math.hypot(x-qx,y-qy)-L)

def math_qa():
    error=0.
    angle=0.
    spacing=1.
    for i in range(121):
        stroke=.065*math.sin(2*math.pi*i/120)
        for side in (-1,1):
            a,e=linkage(side,stroke)
            error=max(error,e)
            angle=max(angle,abs(a))
            spacing=min(spacing,abs(side*.345+stroke-side*.185)/8)
    gap=.139-.066-(.045+.026)
    assert error<1e-9 and angle<math.radians(40)
    assert spacing>.008 and gap>.0019
    assert abs(2*math.pi*.060/20-math.pi*.006)<1e-10
    return dict(frames=121,rod_samples=242,max_rod_error_m=error,
                max_angle_deg=math.degrees(angle),
                min_bellows_pitch_m=spacing,pinion_rack_tip_gap_m=gap)

def glb_qa():
    blob=(OUT/(ID+"_animated.glb")).read_bytes()
    magic,version,size=struct.unpack_from("<4sII",blob)
    assert magic==b"glTF" and version==2 and size==len(blob)
    n,typ=struct.unpack_from("<I4s",blob,12)
    assert typ==b"JSON"
    doc=json.loads(blob[20:20+n].decode("utf-8"))
    assert len(doc.get("materials",[]))>=3
    assert len(doc.get("meshes",[]))>=65
    names=[x.get("name","") for x in doc["nodes"]]
    required=["RACK_SLIDE_X","PINION_ROTATE_Z",
              "LEFT_KNUCKLE_ROTATE_Z","RIGHT_KNUCKLE_ROTATE_Z",
              "LEFT_TIE_ROD_RIG","RIGHT_TIE_ROD_RIG"]
    for name in required:assert name in names,name
    animations=doc.get("animations",[])
    assert animations
    targets={(names[ch["target"]["node"]],ch["target"]["path"])
             for a in animations for ch in a["channels"]}
    for target in [("RACK_SLIDE_X","translation"),
                   ("PINION_ROTATE_Z","rotation"),
                   ("LEFT_KNUCKLE_ROTATE_Z","rotation"),
                   ("RIGHT_KNUCKLE_ROTATE_Z","rotation"),
                   ("LEFT_TIE_ROD_RIG","translation"),
                   ("LEFT_TIE_ROD_RIG","rotation"),
                   ("RIGHT_TIE_ROD_RIG","translation"),
                   ("RIGHT_TIE_ROD_RIG","rotation")]:
        assert target in targets,target
    return dict(bytes=len(blob),materials=len(doc["materials"]),
                meshes=len(doc["meshes"]),nodes=len(names),
                animations=len(animations),animated_targets=len(targets))

if __name__=="__main__":
    report={"id":ID,"math":math_qa()}
    if "--math-only" not in sys.argv:
        blender=json.loads((OUT/(ID+"_qa.json")).read_text())
        assert blender["id"]==ID
        assert blender["reentrant"]=="PASS" and blender["manifold"]=="PASS"
        assert blender["geometry"]["materials"]>=3
        assert (OUT/(ID+".blend")).stat().st_size>10000
        report["blender"]=blender["blender"]
        report["glb"]=glb_qa()
    print(json.dumps(report,indent=2))
