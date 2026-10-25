"""Preserve one native CAMERA cycle, including actual-pose failures and screenshots."""
import csv,hashlib,json,shutil,struct
import xml.etree.ElementTree as ET
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Docs/CAMERA001R11NativeQA'
for label,pattern in {'Compile':'CAMERA-R11-compile-*','OriginalUnity':'CAMERA-R11-original-*','Build':'CAMERA-R11-build-*','Functional':'CAMERA-R11-functional-*'}.items():
 sources=list((root/'Artifacts/Local').glob(pattern));assert len(sources)==1,(label,sources)
 dest=out/label;dest.mkdir(parents=True,exist_ok=True)
 for f in sources[0].iterdir():
  if f.suffix not in ('.json','.xml','.log','.png','.csv'):continue
  if f.suffix=='.log':
   lines=f.read_text(encoding='utf-8-sig',errors='replace').splitlines()
   lines=['[connection identifier omitted]' if s.startswith('Player connection [') else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in lines]
   (dest/f.name).write_text('\n'.join(s.rstrip() for s in lines)+'\n',encoding='utf-8')
  else:shutil.copyfile(f,dest/f.name)
def read(p):return json.loads((out/p).read_text(encoding='utf-8-sig'))
native={}
for mode in ('EditMode','PlayMode'):
 xml=ET.parse(out/'OriginalUnity'/f'{mode}.xml').getroot()
 native[mode]={k:int(xml.get(k)) for k in ('total','passed','failed','skipped')}
 native[mode]['process']=read(f'OriginalUnity/{mode}-process.json')
functional=read('Functional/functional-results.json');assert len(functional['rows'])==8
diagnostic=ET.Element('native-camera-diagnostic',source=functional['source'],target=functional['target'],method=functional['method'],nativeExit=str(read('Functional/process.json')['exitCode']))
for r in functional['rows']:ET.SubElement(diagnostic,'case',id=r['id'],status=r['status'],ownRayIntersections=str(r['ownReticleHits']),worldClearanceFlags=str(r['worldPenetrationSamples']))
ET.ElementTree(diagnostic).write(out/'Functional/functional-diagnostic.xml',encoding='utf-8',xml_declaration=True)
samples=list(csv.DictReader((out/'Functional/camera-trace.csv').read_text().splitlines()))
phases={}
for phase in sorted({r['phase'] for r in samples}):
 rows=[r for r in samples if r['phase']==phase]
 peak=max(rows,key=lambda r:float(r['step']))
 phases[phase]={'samples':len(rows),'actualOwnIntersections':sum(r['actualOwnBlocked']=='True' for r in rows),'maxPositionStep':float(peak['step']),'peakSample':peak}
shots=[]
for f in sorted((out/'Functional').glob('*.png')):
 b=f.read_bytes();w,h=struct.unpack('>II',b[16:24]);shots.append({'path':'Functional/'+f.name,'width':w,'height':h,'sha256':hashlib.sha256(b).hexdigest()})
assert len(shots)==34
for s in shots:assert (s['width'],s['height'])==((1280,720) if '-1280-' in s['path'] else (1920,1080))
summary={'source':'d48108f56d3fe0790ad715683e77b2de9219e4b1','target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','compile':read('Compile/process.json'),'native':native,'build':read('Build/process.json'),'player':read('Functional/process.json'),'launch':read('Functional/launch.json'),'phases':phases,'firstActualOwnIntersection':next(r for r in samples if r['actualOwnBlocked']=='True'),'screenshots':shots,'overall':'FAIL / source camera repair needed; no G2'}
(out/'native-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
lines=['# Functional observations, actual Windows Player','', '| Rig/resolution | Parts | Hull W×H×L m | Actual own-ray hits | World clearance flags | Pivot-blocked samples | Damage parts | Move m | Physical shells observed | Muzzle error deg |','|---|---|---|---|---|---|---|---|---|---|']
for r in functional['rows']:
 size='×'.join(f"{r['hullSize'][k]:.3f}" for k in ('x','y','z'))
 values=[r['id'],r['parts'],size,r['ownReticleHits'],r['worldPenetrationSamples'],r['pivotOverlapSamples'],f"{r['parts']}→{r['partsAfterDamage']}",f"{r['distanceMoved']:.3f}" if r['id'].startswith('Light') else 'NOT_RUN',r['newFastBodies'] if r['id'].startswith('Light') else 'NOT_RUN',f"{r['maxMuzzleAimError']:.4f}" if r['id'].startswith('Light') else 'NOT_RUN']
 lines.append('| '+' | '.join(map(str,values))+' |')
lines+=['','Own intersections occur only at framing→orbit pitch transition, not the subsequent constant-pitch yaw sweep. World clearance flags mean camera centre within0.1m of an axis-aligned wall AABB; not a full camera near-plane penetration test. Damage uses original HP/detachment. Physical shell identification uses recorded P17PhysicalShell/R4ProjectileOrigin components; muzzle error is not a measured ballistic trajectory error. Static Tall/Wide models are QA fixtures, not garage-validity certification.']
(out/'FUNCTIONAL_TABLE.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(json.dumps({'native':native,'playerExit':summary['player']['exitCode'],'screenshots':len(shots),'phases':{k:{a:b for a,b in v.items() if a!='peakSample'} for k,v in phases.items()},'corridorPeak':phases['corridor']['peakSample']}))
