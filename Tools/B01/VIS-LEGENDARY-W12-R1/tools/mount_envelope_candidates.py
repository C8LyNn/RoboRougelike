#!/usr/bin/env python3
"""B01-30 W14: anchored proposals from pinned real animated GLB vertex envelopes.
Never creates BuildSocket, collider, mass, animation, import approval or gameplay permission.
"""
from __future__ import annotations
import argparse, hashlib, json, math
from pathlib import Path

GRID_M=.55
MARGIN_M=.05
ROOT_PAD_M=.10
MODELS={
 'M01-018':{'legend':'LEG-WPN-001','original':[3,2,2],'pivot':'MOUNT_TOP_SOCKET','vertical':'bottom','direction':[0,0,1]},
 'M04-024':{'legend':'LEG-MOV-001','original':[3,3,2],'pivot':'ROOT_UNDERSIDE_MOUNT_Z0','vertical':'top','direction':[0,-1,0]}
}

def measured(package,model):
 p=Path(package);expected=MODELS[model]
 spec=json.loads((p/'binding_spec.json').read_text('utf8'))
 if spec['upAxis']!='Y' or spec['units']!='meters' or spec['noSocketAuthority'] is not True:
  raise ValueError('unsafe source orientation or authority')
 matches=[v for v in spec['mounts'] if v.get('id')==model]
 if len(matches)!=1:raise ValueError('model not uniquely represented')
 a=matches[0]
 if a['legendaryId']!=expected['legend'] or a['footprintNominal']!=expected['original'] or a['pivot']!=expected['pivot']:
  raise ValueError('pinned footprint or pivot changed')
 if a['massKg'] is not None:raise ValueError('unapproved mass')
 if model=='M04-024' and a['exhaustAxis']!=[0,-1,0]:raise ValueError('hover exhaust axis changed')
 name=('M01-018_LEG-WPN-001_R1.glb' if model=='M01-018' else 'M04-024_LEG-MOV-001_R1.glb')
 raw=(p/'models'/name).read_bytes()
 sweep=json.loads((p/'qa'/(model+'_SWEEP.json')).read_text('utf8'))
 pinned=json.loads((p/'manifest.json').read_text('utf8'))
 binary_entries=[x for x in pinned['binaries'] if x['modelId']==model]
 if len(binary_entries)!=1 or binary_entries[0]['reworkedSha256']!=sweep['source_sha256']:
  raise ValueError('sweep is not pinned by manifest')
 if hashlib.sha256(raw).hexdigest()!=sweep['source_sha256']:
  raise ValueError('GLB HASH mismatch')
 count=sweep['samplesPerClip'];clips=sweep['animations']
 if count<121 or not clips:raise ValueError('insufficient animated geometry evidence')
 if any(c['samples']!=count or c.get('verticesPerSample',0)<=0 for c in clips):
  raise ValueError('invalid sampled geometry')
 if model=='M04-024':
  combined=[c for c in clips if c['clip']=='LEG-MOV-001_ALL_FOUR_GIMBALS']
  if len(combined)!=1 or combined[0]['channels']!=4:raise ValueError('4-gimbal combined clip absent')
 lo=[min(c['sampledSweptMin'][i] for c in clips) for i in range(3)]
 hi=[max(c['sampledSweptMax'][i] for c in clips) for i in range(3)]
 if any(not math.isfinite(x) for x in lo+hi) or any(lo[i]>=hi[i] for i in range(3)):
  raise ValueError('invalid vertex envelope')
 return {'id':model,'sha256':sweep['source_sha256'],'min':lo,'max':hi,
         'original':expected['original'],'vertical':expected['vertical'],
         'pivot':expected['pivot'],'direction':expected['direction'],'sampledTimesPerClip':count}

def candidate(info,cells,depth_offset=0.0):
 w,h,d=cells
 if any(type(v)!=int or v<1 for v in cells):raise ValueError('invalid cells')
 x=[-w*GRID_M/2,w*GRID_M/2]
 z=[-d*GRID_M/2+depth_offset,d*GRID_M/2+depth_offset]
 if info['vertical']=='bottom':y=[-ROOT_PAD_M,h*GRID_M-ROOT_PAD_M]
 elif info['vertical']=='top':y=[-h*GRID_M+ROOT_PAD_M,ROOT_PAD_M]
 else:raise ValueError('unsupported root plane policy')
 bounds=[x,y,z]
 six=[[round(info['min'][i]-bounds[i][0],6),round(bounds[i][1]-info['max'][i],6)] for i in range(3)]
 worst=min(v for face in six for v in face)
 return {'cellsWHD':list(cells),'nominalSizeM':[round(a*GRID_M,6) for a in cells],
   'depthCenterOffsetFromRootM':depth_offset,
   'boundingIntervalXYZ':bounds,'sixFaceClearancesM':six,
   'worstClearanceM':round(worst,6),'meetsReviewMargin':worst+1e-9>=MARGIN_M,
   'requiresDesignApprovalForOffset':depth_offset!=0,
   'approvedToMount':False,'authorizedSocket':False}

def recommend(info):
 valid=[]
 for w in range(1,11):
  for h in range(1,11):
   for d in range(1,13):
    for off in ([0.0,.10] if info['id']=='M01-018' else [0.0]):
     c=candidate(info,(w,h,d),off)
     if c['meetsReviewMargin']:valid.append(c)
 safe=[c for c in valid if not c['requiresDesignApprovalForOffset']]
 if not safe:raise ValueError('no unscaled anchored alternative')
 key=lambda c:(math.prod(c['cellsWHD']),max(c['cellsWHD']),c['cellsWHD'])
 fixed=min(safe,key=key)
 conditional=min(valid,key=lambda c:(math.prod(c['cellsWHD']),c['requiresDesignApprovalForOffset'],max(c['cellsWHD'])))
 original=candidate(info,info['original'])
 if original['meetsReviewMargin']:raise ValueError('source drift: previous invalid footprint now unexpectedly fits')
 return {'modelId':info['id'],'sha256':info['sha256'],'sampledTimesPerClip':info['sampledTimesPerClip'],
  'pivot':info['pivot'],'actualMinXYZ':info['min'],'actualMaxXYZ':info['max'],
  'axisDirection':info['direction'],'rootVerticalProposal':info['vertical'],
  'rootVerticalPaddingProposalM':ROOT_PAD_M,'marginForReviewOnlyM':MARGIN_M,
  'original':original,'minimumUnchangedRootDepthCenter':fixed,
  'minimumConditionalDepthCenterShift':conditional,
  'decision':'PREVIEW_PROPOSALS_ONLY','nativeUnity':'NOT_RUN','continuousTriangleCollision':'NOT_RUN',
  'neverAuthorizesBuildSocket':True}

def run(package,output):
 r={'schema':1,'revision':'W14_RC2_ANCHORED_VISUAL_FOOTPRINTS',
  'axes':{'X':'Width','Y':'Height','Z':'Depth'},'gridM':GRID_M,
  'samplesAreRealBinaryVertices':True,'approvedToMount':False,
  'models':[recommend(measured(package,name)) for name in MODELS],
  'G2':'HOLD','nativeUnity':'NOT_RUN','physxCollision':'NOT_RUN'}
 path=Path(output);path.parent.mkdir(parents=True,exist_ok=True)
 path.write_text(json.dumps(r,sort_keys=True,indent=2)+'\n','utf8')
 return r

if __name__=='__main__':
 p=argparse.ArgumentParser()
 p.add_argument('--package',required=True)
 p.add_argument('--output',required=True)
 args=p.parse_args()
 r=run(args.package,args.output)
 for x in r['models']:
  print(x['modelId'], 'unchanged-root',x['minimumUnchangedRootDepthCenter']['cellsWHD'],
        'conditional',x['minimumConditionalDepthCenterShift']['cellsWHD'])
