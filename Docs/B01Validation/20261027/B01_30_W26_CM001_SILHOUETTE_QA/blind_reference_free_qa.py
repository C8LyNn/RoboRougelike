#!/usr/bin/env python3
"""Independent W26 Pass3 actual reference-free B01-31 64/128 raster stress.

This is pixel-brightness under edge occlusion. NOT recognition, physics, CAD, or 3D.
SOURCE ART unchanged; prior full-card-stress PNG metrics are HISTORICAL.
"""
from __future__ import annotations
import argparse,hashlib,json,math
from pathlib import Path
from PIL import Image,ImageOps
NEW_PRODUCER_SHA='7a1416d3e10ee720d9d28931a3c268f95e2bba15'
PRODUCER_ARTIFACT=12739939008
KNOWN={
 'A-wheel':{64:'74285a185488c330c65e969db2bf5d3c2a232cef743cc00a094487d13196cf3f',
  128:'9c0403b36616ae1d5a503b38528a5c85cbdb150dec691fc69816c52aa09dbebe'},
 'B-track':{64:'e49e0938d169faa4d196a86548adc36127ee29604a9bc5aa47c3e73a94d9ef46',
  128:'e28f6a265fc887202f508093d322e86c1fb81cd6a9cbd3808c34ccda23305868'},
 'C-omni':{64:'1cbbbc96bfdf2d75cb3fcb4440f17b58dd5fbbb32911916bc4a9423f77bb44bf',
  128:'7bdef540d7c23b7a87fb036bfc3e67bb1b6a75e382ab7a36aedbf1b9f7a91cb0'}}
BG=(11,26,46)
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def sample(root,name,size):
 if name not in KNOWN or size not in (64,128):raise ValueError('UNKNOWN_SOURCE_ART_VARIANT')
 p=Path(root)/f'{name}-silhouette-{size}px.png'
 if sha(p)!=KNOWN[name][size]:raise ValueError('SOURCE_SHA_DRIFT '+name+' '+str(size))
 with Image.open(p) as im:
  if im.size!=(size,size):raise ValueError('SOURCE_PNG_DIMENSION_DRIFT')
  return ImageOps.grayscale(im.convert('RGB')).convert('RGB')
def count(im):
 return sum(max(px)>96 for px in im.getdata())
def cover(im,side):
 if side not in ('left','right','top','bottom'):raise ValueError('INVALID_COVER_DIRECTION')
 p=im.copy();w,h=p.size;n=round(w*.25)
 for y in range(h):
  for x in range(w):
   if ((side=='left' and x<n) or (side=='right' and x>=w-n) or
       (side=='top' and y<n) or (side=='bottom' and y>=h-n)):
    p.putpixel((x,y),BG)
 return p
def compute(root):
 rows=[]
 for name in KNOWN:
  for size in (64,128):
   im=sample(root,name,size)
   original=count(im)
   if original<20:raise ValueError('EMPTY_SOURCE_PIXEL_SUPPORT')
   results={s:round(count(cover(im,s))/original,6) for s in ('left','right','top','bottom')}
   rows.append({'variant':name,'sizePx':size,'referenceFreeSourceSha256':KNOWN[name][size],
    'brightPixelCount':original,'retainedFractionBy25pctEdge':results,
    'worst25pctEdge':min(results,key=results.get),'worstRetainedFraction':min(results.values())})
 return {'schema':'B01-30-CM001-PASS3-REFERENCE-FREE-PNG-PROXY-1',
    'inputProducerPR294Head':NEW_PRODUCER_SHA,'actualProducerGitHubArtifact':PRODUCER_ARTIFACT,
    'humanRecognition':'NOT_RUN','UnityNative':'NOT_RUN',
    'physicalSocketAndPivot':'UNKNOWN','decisionHQ':'PENDING',
    'notComparableWithPriorFullCardLegacyCounts':True,
    'pixelBrightnessHeuristic':'grayscale >96, canvas left/right/top/bottom quarter masking; not a perceptual score',
    'rows':rows}
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('--source',required=True);a.add_argument('--output',required=True)
 x=a.parse_args();r=compute(x.source)
 Path(x.output).write_text(json.dumps(r,sort_keys=True,indent=2)+'\n')
 print(json.dumps({f"{v['variant']}-{v['sizePx']}":v['worstRetainedFraction'] for v in r['rows']},sort_keys=True))
 print('REAL_REFERENCE_FREE_PNG_SOURCE_PASS / HUMAN_NOT_RUN / UNITY_NOT_RUN')
