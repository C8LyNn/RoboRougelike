#!/usr/bin/env python3
"""B01-30 QA of actual B01-31 700x866 PNG source, NOT human/Unity acceptance.
Reproduces grayscale and 25% four-sided occlusion at exactly 64/128 pixels.
Do not mistake colored pixel coverage for recognition or a real 3D silhouette.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw

INPUTS={
 'A-wheel':'a26c815efeec4196f1d621e47138d0ae81c12da7f309142921b564b31b0db221',
 'B-track':'624ffb8b8e98f9c6727821e6345eaa485d20eb9ac1ef67546d6c457f3bb388f2',
 'C-omni':'1b4035dd8432e6374843224793b322cca08af41833a1c1e17adf16c927a6ebab',
}
PRODUCER_HEAD='3efd76bb2eaa319282a51ad588f8f4176d0cc33b'
ORIGINAL_PRODUCER_ARTIFACT=12723984143
# NOW IDENTICAL TO CURRENT B01-31 W26 SUBJECT-ONLY crop, not outdated full-poster thumbs.
CROP=(142,264,620,529)
BG=(11,26,46)
SCALES=(64,128)

def sha(path):
 return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def load(p,name):
 if sha(p)!=INPUTS[name]:raise ValueError('PINNED_SOURCE_SHA256_MISMATCH '+name)
 with Image.open(p) as im:
  if im.size!=(700,866):raise ValueError('WRONG_SOURCE_CANVAS')
  return im.convert('RGB')

def view(im,pixels,gray=False):
 if pixels not in SCALES:raise ValueError('UNSUPPORTED_SIZE')
 c=im.crop(CROP)
 out=Image.new('RGB',(pixels,pixels),BG)
 fitted=ImageOps.contain(c,(pixels,pixels),Image.Resampling.LANCZOS)
 out.paste(fitted,((pixels-fitted.width)//2,(pixels-fitted.height)//2))
 if gray:return ImageOps.grayscale(out).convert('RGB')
 return out

def cover(im,side,fraction=.25):
 if side not in ('left','right','top','bottom'):raise ValueError('UNSUPPORTED_OCCLUSION_SIDE')
 result=im.copy();p=result.load();w,h=result.size
 k=round(w*fraction);j=round(h*fraction)
 for y in range(h):
  for x in range(w):
   hit=(x<k if side=='left' else x>=w-k if side=='right' else
        y<j if side=='top' else y>=h-j)
   if hit:p[x,y]=BG
 return result

def metric(img):
 # Bright painted material >96; excludes background and fine blue floor-grid.
 # NOT a precise filled silhouette nor perceptual or mechanical measurement.
 pixels=list(img.getdata())
 bright=[i for i,rgb in enumerate(pixels) if max(rgb)>96]
 n=len(bright)
 return {'brightPixels':n,'brightFraction':round(n/len(pixels),6),
   'uniqueRGB':len(set(pixels))}

def build(source,out):
 source=Path(source);out=Path(out);out.mkdir(parents=True,exist_ok=True)
 rows={};imgs={}
 for name in INPUTS:
  original=load(source/(name+'.png'),name)
  rows[name]={'sourcePNGsha256':INPUTS[name],'cropXYXY':list(CROP),
              'scales':{},'edge25pct':{}}
  for size in SCALES:
   a=view(original,size);b=view(original,size,True)
   for mode,im in [('color',a),('gray',b)]:
    fname=f'{name}-{size}-{mode}.png'
    im.save(out/fname,optimize=True)
    rows[name]['scales'][str(size)+'px-'+mode]={**metric(im),'file':fname,'sha256':sha(out/fname)}
    imgs[(name,size,mode)]=im
   baseline=metric(b)['brightPixels']
   cases={}
   for side in ('left','right','top','bottom'):
    c=cover(b,side)
    fname=f'{name}-{size}-gray-cover-{side}.png'
    c.save(out/fname,optimize=True)
    n=metric(c)['brightPixels']
    cases[side]={'file':fname,'brightPixelsRemaining':n,
                 'ratioRetained':round(n/max(baseline,1),6)}
   rows[name]['edge25pct'][str(size)+'px']={
    'worstSide':min(cases,key=lambda k:cases[k]['ratioRetained']),
    'worstRatio':min(v['ratioRetained'] for v in cases.values()),
    'bySide':cases}
 # Side-by-side crops and grayscale, worst-side occlusion. No 3D claim.
 sheet=Image.new('RGB',(882,650),(234,238,242))
 draw=ImageDraw.Draw(sheet)
 for j,name in enumerate(INPUTS):
  x=12+j*286
  draw.text((x,9),name,fill=(12,25,44))
  for size,y in [(128,38),(64,250)]:
   for mode,dx in [('color',0),('gray',142)]:
    sheet.paste(imgs[(name,size,mode)],(x+dx,y))
   draw.text((x,y+size+6),f'{size}px source / RGB and grayscale',fill=(12,25,44))
  poster=load(source/(name+'.png'),name).resize((64,79),Image.Resampling.LANCZOS)
  sheet.paste(poster,(x,355))
  draw.text((x,445),'64px whole card (bad test)',fill=(12,25,44))
  draw.text((x,493),'UNKNOWN: human recognition',fill=(12,25,44))
  draw.text((x,510),'UNKNOWN: physical envelope',fill=(12,25,44))
  side=rows[name]['edge25pct']['64px']['worstSide']
  with Image.open(out/f'{name}-64-gray-cover-{side}.png') as stress:
   sheet.paste(stress,(x,535))
  draw.text((x+72,545),'25% worst: '+side,fill=(12,25,44))
  draw.text((x+72,563),'retained bright '+str(rows[name]['edge25pct']['64px']['worstRatio']),fill=(12,25,44))
 sheet.save(out/'comparison_64_128.png',optimize=True)
 receipt={'schema':'B01-30-W26-CM001-READONLY-IMAGE-QA-1',
  'inputProducerPR294Head':PRODUCER_HEAD,
  'producerSvgFilesUnchangedSince':'62b9b6660819ff71ad007843802ac0e93d9fa362',
  'inputProducerPNGArtifactId':ORIGINAL_PRODUCER_ARTIFACT,
  'commonCropXYXY':list(CROP),'sizesPx':list(SCALES),
  'mode':'REAL_RENDER_RASTER_GRAYSCALE_AND_25PCT_EDGE_OCCLUSION_ONLY',
  'metricWarning':'Bright pigmented pixel proxy; NOT human recognition, mesh silhouette or physical bounds',
  'HQdecision':'PENDING','humanUserStudy':'NOT_RUN','Unity':'NOT_RUN','PhysX':'NOT_RUN',
  'allMountSocketsAndClearances':'UNKNOWN',
  'comparisonPngSha256':sha(out/'comparison_64_128.png'),'results':rows}
 (out/'receipt.json').write_text(json.dumps(receipt,sort_keys=True,indent=2)+'\n','utf8')
 return receipt

if __name__=='__main__':
 ap=argparse.ArgumentParser()
 ap.add_argument('--source',required=True);ap.add_argument('--output',required=True)
 x=ap.parse_args()
 receipt=build(x.source,x.output)
 print(json.dumps({k:{s:receipt['results'][k]['edge25pct'][s]['worstRatio'] for s in ('64px','128px')} for k in INPUTS},sort_keys=True))
 print('SOURCE_RASTER_QA_PASS / HUMAN_NOT_RUN / UNITY_NOT_RUN')
