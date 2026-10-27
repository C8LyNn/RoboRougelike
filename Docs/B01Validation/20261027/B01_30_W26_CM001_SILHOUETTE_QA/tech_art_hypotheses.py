#!/usr/bin/env python3
"""B01-30/pass3: marked image-space WHAT-IF technical art annotations.
Every marker is an illustrator-plane approximate locus, not a Unity XYZ pivot/socket.
Reads only hash-pinned original B01-31 full-card renders.
"""
from __future__ import annotations
import argparse,json,hashlib,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from silhouette_stress import load,CROP,INPUTS,PRODUCER_HEAD
UNKNOWN="UNKNOWN_NOT_MEASURED"
# Points are FRACTIONS OF SOURCE ILLUSTRATION CROP (not meters, not XYZ).
OPTIONS={
 "A-wheel":{
  "unitBoundary":"multi-wheel vehicle illustration; not a verified single mountable game part",
  "candidateBreakdown":["frame reference (illustrated vehicle, not build socket)","wheel hub x4",
        "suspension wishbone/strut x4","steering carrier x4 (if steering chosen)"],
  "axisQuestions":["wheel spin: axle normal to illustrated wheel plane (Unity signed axis UNKNOWN)",
                   "steering yaw: local vertical? UNKNOWN","suspension travel and limits: UNKNOWN"],
  "noMountAssumption":"frame-to-strut and strut-to-wheel visual joins do not imply typed socket",
  "markers":[(0.48,.41,"visual frame reference"),(.16,.46,"left wheel hub (image)"),
     (.365,.74,"near wheel hub (image)"),(.78,.52,"right wheel hub (image)")]},
 "B-track":{
  "unitBoundary":"two-track vehicle illustration; no isolated single track-module interface shown",
  "candidateBreakdown":["frame reference (illustration only)","left belt loop as visual rig",
      "right belt loop as visual rig","sprockets/idlers/belt pieces per track"],
  "axisQuestions":["each idler/sprocket spin axis: face-normal in illustration; signed XYZ UNKNOWN",
                   "belt scrolling/UV or skinned path: implementation UNKNOWN",
                   "individual tread dynamic collision vs static hull: design decision"],
  "noMountAssumption":"central-body side plates are not verified typed frame sockets",
  "markers":[(.48,.35,"central reference frame"),(.22,.58,"left track belt loop"),
     (.68,.41,"right track belt loop"),(.30,.64,"left idler/sprocket candidate")]},
 "C-omni":{
  "unitBoundary":"four-pod vehicle illustration; no approved single mobility module or drive type",
  "candidateBreakdown":["frame/hub reference (art only)","radial arms x4",
                        "ring pods x4","emissive/decorative rings (if any)"],
  "axisQuestions":["pod swivel/gimbal around arm tip? axis UNKNOWN",
                   "rings rotate mechanically or glow only? UNKNOWN",
                   "true hover vs omni-wheel ground contact: HQ GAME DESIGN DECISION"],
  "noMountAssumption":"arm-root and pod-join graphics are not authoritative mounting sockets",
  "markers":[(.47,.33,"central illustrated frame"),(.12,.48,"left ring pod"),
     (.36,.76,"near ring pod"),(.81,.48,"right ring pod")]}
}
STATE={"artReference":"SOURCE_2D_ONLY","allMechanicPivots":UNKNOWN,
       "allUnityAxisDirections":UNKNOWN,"allTypedSockets":UNKNOWN,
       "allColliderAABB":UNKNOWN,"allAnimationSweeps":UNKNOWN,
       "allFootprintCells":UNKNOWN,"allMass":UNKNOWN,"allRealUsers":"NOT_RUN",
       "unityNative":"NOT_RUN","HQSelected":"PENDING",
       "realModelAuthorized":False,"technicalArtCardApproved":False}
def validate():
 if set(OPTIONS)!=set(INPUTS):raise ValueError('INPUT_OPTION_DRIFT')
 for name,v in OPTIONS.items():
  if len(v['markers'])!=4:raise ValueError('MISSING_2D_ANNOTATION '+name)
  if len(v['candidateBreakdown'])<3:raise ValueError('INSUFFICIENT_ASSEMBLY_BREAKDOWN '+name)
  if len(v['axisQuestions'])<3:raise ValueError('MISSING_ANIMATION_RISK '+name)
  for x,y,label in v['markers']:
   if not (math.isfinite(x) and math.isfinite(y) and .05<=x<=.94 and .05<=y<=.92):
    raise ValueError('NOT_A_SAFE_SCREEN_POINT '+name)
   if not label:raise ValueError('LABEL_MISSING')
 for k,v in STATE.items():
  if k in ('realModelAuthorized','technicalArtCardApproved'):
   if v is not False:raise ValueError('AUTHORITY_FORGERY')
  elif k in ('allRealUsers','unityNative'):
   if v!='NOT_RUN':raise ValueError('NATIVE_FORGERY')
  elif k=='HQSelected':
   if v!='PENDING':raise ValueError('HQ_DECISION_FORGERY')
  elif k=='artReference':
   if v!='SOURCE_2D_ONLY':raise ValueError('CAD_FORGERY')
  elif v!=UNKNOWN:raise ValueError('PHYSICAL_GROUND_TRUTH_FORGERY')
 return True
def font(size):
 for p in ('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',
          '/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf'):
  if Path(p).exists():return ImageFont.truetype(p,size=size)
 return ImageFont.load_default()
def sheet(source,target):
 validate()
 source=Path(source);target=Path(target);target.mkdir(parents=True,exist_ok=True)
 W,H=1600,1320
 bg='#081421';fg='#e9f1f7';accent='#5ad6c9';warn='#f0bd76'
 canvas=Image.new('RGB',(W,H),bg)
 d=ImageDraw.Draw(canvas)
 d.text((35,16),'B01-30  /  CM001  /  TECH-ART PRE-SELECTION RISK MAP',font=font(25),fill=fg)
 d.text((35,48),'IMAGE-SPACE HYPOTHESES ONLY  |  SOURCE ART 2D  |  PIVOT XYZ / COLLIDER / SOCKET: UNKNOWN',font=font(16),fill=warn)
 evidence=[]
 for index,(name,tech) in enumerate(OPTIONS.items()):
  full=load(source/(name+'.png'),name)
  img=full.crop(CROP)
  img=img.resize((646,358),Image.Resampling.LANCZOS)
  y=91+index*397
  canvas.paste(img,(36,y+29))
  d.rectangle((28,y+22,690,y+396),outline='#304c60',width=2)
  d.text((40,y),f'{index+1} / {name.upper()}   [DRAWING, NOT A MODEL]',font=font(21),fill=fg)
  points=[]
  for k,(x,yrel,label) in enumerate(tech['markers'],1):
   cx=36+round(646*x);cy=y+29+round(358*yrel)
   r=17
   d.ellipse((cx-r,cy-r,cx+r,cy+r),fill='#11273e',outline='#e8c078',width=3)
   d.text((cx-6,cy-9),str(k),font=font(17),fill='#fff4d8')
   points.append({'id':k,'screenFrac':[x,yrel],'screenPxOnSheet':[cx,cy],
       'hypothesisLabel':label,'worldPivotXYZ':UNKNOWN})
  d.text((735,y+7),'ASSEMBLY UNIT?  NOT RESOLVED',font=font(21),fill=warn)
  d.text((735,y+39),'Illustrated subject is an entire multi-element assembly.',font=font(16),fill=fg)
  d.text((735,y+69),'Numbered image-space loci (never Unity pivots):',font=font(17),fill=accent)
  for k,p in enumerate(points,1):
   d.text((743,y+98+(k-1)*30),f'{k}. {p["hypothesisLabel"]}',font=font(17),fill=fg)
  d.text((735,y+227),'Before a 3D blockout, decide:',font=font(18),fill=accent)
  for n,q in enumerate(tech['axisQuestions']):
   # short display summary; detailed text is in report JSON / README
   d.text((743,y+257+26*n),f'- {q[:89]}',font=font(14),fill=fg)
  d.text((735,y+349),'NO SOCKET PROVEN  /  NO PHYSICAL SIZE CLAIM',font=font(17),fill=warn)
  evidence.append({'concept':name,'producerPNGsha256':INPUTS[name],
    'candidateBreakdown':tech['candidateBreakdown'],
    'unitBoundaryQuestion':tech['unitBoundary'],
    'axisQuestions':tech['axisQuestions'],
    'noMountAssumption':tech['noMountAssumption'],'imageMarkers':points})
 out=target/'CM001_TECH_ART_PRESELECTION_PIVOTS_UNKNOWN.png'
 canvas.save(out,optimize=True)
 result={'schema':'B01-30-W26PASS3-TECH-ART-PRESELECTION-IMAGE-ANNOTATIONS',
      'producerPR294Head':PRODUCER_HEAD,'currentSourceCrop':list(CROP),
      'sourceStrictly2D':True,'engineeringStates':STATE,
      'imageSha256':hashlib.sha256(out.read_bytes()).hexdigest(),
      'imageSizePx':[W,H],'variants':evidence}
 (target/'tech_art_receipt.json').write_text(json.dumps(result,indent=2,ensure_ascii=False,sort_keys=True)+'\n')
 return result
if __name__=='__main__':
 p=argparse.ArgumentParser()
 p.add_argument('--source',required=True);p.add_argument('--output',required=True)
 a=p.parse_args()
 print(json.dumps({'image':sheet(a.source,a.output)['imageSha256'],
   'source':'2D_ONLY','realUsers':'NOT_RUN','Unity':'NOT_RUN','G_DESIGN':'PENDING'}))
