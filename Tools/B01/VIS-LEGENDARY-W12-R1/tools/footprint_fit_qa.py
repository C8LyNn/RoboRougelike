#!/usr/bin/env python3
"""B01-30 W13: fail-closed real GLB swept-vertex vs typed W/H/D footprint gate.

Axis contract GLB X=Width, Y=Height, Z=Depth. Neither a sampled AABB
nor a visual GLB node authorizes BuildSocket, Unity PhysX or game mounting.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

GRID_M = 0.55
MARGIN_M = 0.05  # diagnostic reviewer margin ONLY, not approved Build threshold
MODELS = {'M01-018': 'LEG-WPN-001', 'M04-024': 'LEG-MOV-001'}

def evaluate(mount, sweep, binary, margin_m=MARGIN_M):
    if mount.get('id') not in MODELS or mount.get('legendaryId') != MODELS[mount['id']]:
        raise ValueError('unknown or mismatched model mapping')
    if mount.get('massKg') is not None or mount.get('socketKind') not in ('WeaponBase', 'MobilityHub'):
        raise ValueError('unapproved mass or socket contract')
    if not mount.get('visualNode', '').startswith('VIS_LEG_'):
        raise ValueError('missing visual-only node')
    cells = mount.get('footprintNominal', [])
    if len(cells) != 3 or any(type(x) is not int or x <= 0 for x in cells):
        raise ValueError('invalid footprint cell vector W/H/D')
    if not isinstance(margin_m, (float,int)) or not math.isfinite(margin_m) or margin_m < 0:
        raise ValueError('invalid nonnegative margin')
    if hashlib.sha256(binary).hexdigest() != sweep['source_sha256']:
        raise ValueError('stale or tampered source GLB binary')
    samples=sweep.get('samplesPerClip',0)
    if samples < 121:
        raise ValueError('insufficient sampled animation times')
    animations=sweep.get('animations')
    if not animations:
        raise ValueError('no actual animation envelopes')
    low=[float('inf')]*3
    high=[float('-inf')]*3
    clips=[]
    for clip in animations:
        if clip.get('samples') != samples:
            raise ValueError('inconsistent clip samples')
        clips.append(clip['clip'])
        if clip.get('verticesPerSample',0) <= 0:
            raise ValueError('clip has no real vertices')
        a,b=clip['sampledSweptMin'],clip['sampledSweptMax']
        if len(a)!=3 or len(b)!=3 or any(not math.isfinite(v) for v in a+b) or any(a[i]>=b[i] for i in range(3)):
            raise ValueError('invalid sampled vertex bounds')
        for i in range(3):
            low[i]=min(low[i],a[i])
            high[i]=max(high[i],b[i])
    if mount['id']=='M04-024':
        combined=[a for a in animations if a['clip']=='LEG-MOV-001_ALL_FOUR_GIMBALS']
        if len(combined)!=1 or combined[0]['channels']!=4:
            raise ValueError('missing verified four-gimbal combined clip')
        if mount.get('exhaustAxis') != [0,-1,0]:
            raise ValueError('hover exhaust must stay downward: GLB -Y')
    extent=[round(high[i]-low[i],6) for i in range(3)]
    nominal=[round(x*GRID_M,6) for x in cells]
    clearance=[round(nominal[i]-extent[i],6) for i in range(3)]
    zero=all(x>=0 for x in clearance)
    safe=all(x>=2*margin_m for x in clearance)
    reason=('ENVELOPE_EXCEEDS_TYPED_FOOTPRINT' if not zero else
            'MARGIN_INSUFFICIENT' if not safe else 'SIZE_ONLY_NOT_COLLISION_CERTIFIED')
    return {'modelId':mount['id'],'legendaryId':mount['legendaryId'],
            'sourceSha256':sweep['source_sha256'],'clips':clips,'sampledFramesPerClip':samples,
            'axisMapping':{'X':'Width','Y':'Height','Z':'Depth'},
            'footprintCellsWHD':cells,'footprintM_WHD':nominal,
            'sampledVertexMinXYZ':low,'sampledVertexMaxXYZ':high,
            'sampledVertexExtentXYZ':extent,'extentRemainderM':clearance,
            'perFaceMarginTargetM':margin_m,'sizeFitsZeroMargin':zero,
            'sizeFitsMargin':safe,'approvedToMount':False,
            'status':'BLOCKED' if not safe else 'REVIEW_PHYSICAL_FIT',
            'reason':reason,
            'proofLimit':'121 sampled genuine vertices per clip; NOT triangle BVH, PhysX, socket authority or native Unity.'}

def assess_package(package, output):
    root=Path(package)
    contract=json.loads((root/'binding_spec.json').read_text('utf8'))
    if contract.get('upAxis')!='Y' or contract.get('units')!='meters' or contract.get('noSocketAuthority') is not True:
        raise ValueError('untrusted coordinate/socket contract')
    mounts=contract['mounts']
    if len(mounts)!=2 or {x['id'] for x in mounts} != set(MODELS):
        raise ValueError('both expected models are required')
    reports=[]
    for mount in mounts:
        model=mount['id']
        basename='M01-018_LEG-WPN-001_R1.glb' if model=='M01-018' else 'M04-024_LEG-MOV-001_R1.glb'
        binary=(root/'models'/basename).read_bytes()
        sweep=json.loads((root/'qa'/(model+'_SWEEP.json')).read_text('utf8'))
        reports.append(evaluate(mount,sweep,binary))
    result={'status':'BLOCKED_FOR_PHYSICAL_MOUNT','reports':reports,
            'zeroAuthorizedSockets':True,'nativeUnity':'NOT_RUN','physicalTriangleIntersection':'NOT_RUN'}
    output=Path(output)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(result,sort_keys=True,indent=2)+'\n','utf8')
    return result

def main():
    p=argparse.ArgumentParser()
    p.add_argument('--package',required=True)
    p.add_argument('--output',required=True)
    args=p.parse_args()
    res=assess_package(args.package,args.output)
    for r in res['reports']:
        print(r['modelId'],r['status'],r['reason'],r['sampledVertexExtentXYZ'],'remaining',r['extentRemainderM'])

if __name__=='__main__':
    main()
