"""Collect actual R3.4.1 runs; keep NUnit, material observations and real wheels separate."""
import csv,hashlib,json,shutil,struct
import xml.etree.ElementTree as ET
from PIL import Image
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Docs/MAPGEN001R341NativeQA'
runs={'Compile':'MAPGEN-R341-compile-*','OriginalUnity':'MAPGEN-R341-original-*','MaterialBuild':'MAPGEN-R341-material-build-*','Materials':'MAPGEN-R341-material-player-*','WheelBuild':'MAPGEN-R341-wheel-build-*','WheelsFirst4':'MAPGEN-R341-wheels-first4-*','WheelsRemaining8':'MAPGEN-R341-wheels-remaining8-*'}
for label,pattern in runs.items():
 sources=list((root/'Artifacts/Local').glob(pattern));assert len(sources)==1,(label,sources)
 dest=out/label;dest.mkdir(parents=True,exist_ok=True)
 for file in sources[0].iterdir():
  if file.suffix not in ('.json','.xml','.log','.csv','.png'):continue
  if file.suffix=='.log':
   lines=file.read_text(encoding='utf-8-sig',errors='replace').splitlines()
   lines=['[connection identifier omitted]' if s.startswith('Player connection [') else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in lines]
   (dest/file.name).write_text('\n'.join(s.rstrip() for s in lines)+'\n',encoding='utf-8')
  else:shutil.copyfile(file,dest/file.name)
def read(path):return json.loads((out/path).read_text(encoding='utf-8-sig'))
native={};cases=[]
for mode in ('EditMode','PlayMode'):
 tree=ET.parse(out/'OriginalUnity'/f'{mode}.xml').getroot();native[mode]={k:int(tree.get(k)) for k in ('total','passed','failed','skipped')};native[mode]['process']=read(f'OriginalUnity/{mode}-process.json')
 cases += [{'name':c.get('name'),'mode':mode,'result':c.get('result'),'output':c.findtext('output') or ''} for c in tree.iter('test-case')]
cube=[c for c in cases if c['name'].endswith('_CubePhysics')];assert len(cube)==12
flat=[c for c in cases if c['name']=='CubeForceActuatorMovesOnFlatControlSurface'];assert len(flat)==1
material=read('Materials/material-results.json');assert len(material['rows'])==4
mt=ET.Element('native-material-observations',format='Standalone diagnostics, NOT NUnit',source=material['source'],nativeExit=str(read('Materials/process.json')['exitCode']))
for r in material['rows']:ET.SubElement(mt,'case',recipe=r['recipe'],width=str(r['width']),height=str(r['height']),status=r['status'],resourceMatches=str(r['resourceMatches']),lifecycleRan=str(r['lifecycleRan']))
ET.ElementTree(mt).write(out/'Materials/material-observations.xml',encoding='utf-8',xml_declaration=True)
wheel=[];wheel_runs={}
for label in ('WheelsFirst4','WheelsRemaining8'):
 data=read(label+'/wheel-trials.json');process=read(label+'/process.json');wheel+=data['trials']
 assert data['source']=='5be31039411f89f17654b1c332f8692883fa03cc'
 assert not data['diagnosticMaterialOverride']
 wheel_runs[label]={'nativeExit':process['exitCode'],'rows':len(data['trials']),'passed':sum(t['status']=='PASS' for t in data['trials']),'errors':data['errors'],'launch':read(label+'/launch.json')}
 xml=ET.Element('native-wheel-observations',format='Real standalone R3/R6 diagnostics, NOT NUnit',source=data['source'],target=data['vehicleSource'],nativeExit=str(process['exitCode']))
 for t in data['trials']:ET.SubElement(xml,'case',id=t['id'],status=t['status'],finish=str(t['reachedFinish']),distance=str(t['distance']),minimumContacts=str(t['minimumContacts']))
 ET.ElementTree(xml).write(out/label/'wheel-observations.xml',encoding='utf-8',xml_declaration=True)
assert len(wheel)==12 and len({(t['recipe'],t['vehicle'],t['seed']) for t in wheel})==12
assert {t['seed'] for t in read('WheelsFirst4/wheel-trials.json')['trials']}=={7}
assert {t['seed'] for t in read('WheelsRemaining8/wheel-trials.json')['trials']}=={31,83}
assert wheel_runs['WheelsFirst4']['launch']['assemblySha256']==wheel_runs['WheelsRemaining8']['launch']['assemblySha256']
shots=[]
for label in ('Materials','WheelsFirst4','WheelsRemaining8'):
 for file in sorted((out/label).glob('*.png')):
  data=file.read_bytes();w,h=struct.unpack('>II',data[16:24]);shot={'path':label+'/'+file.name,'width':w,'height':h,'sha256':hashlib.sha256(data).hexdigest()}
  if label=='Materials':
   rgb=Image.open(file).convert('RGB').tobytes();shot['diagnosticMagentaPixels']=sum(r>200 and g<70 and b>200 for r,g,b in zip(rgb[0::3],rgb[1::3],rgb[2::3]))
  shots.append(shot)
assert len(shots)==40
summary={'producerSource':'5be31039411f89f17654b1c332f8692883fa03cc','target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','compile':read('Compile/process.json'),'native':native,'NUnitCases':cases,'cubeCases':cube,'flatControl':flat[0],'materialBuild':read('MaterialBuild/process.json'),'materialPlayer':read('Materials/process.json'),'materialLaunch':read('Materials/launch.json'),'materialResult':material,'wheelBuild':read('WheelBuild/process.json'),'wheelRuns':wheel_runs,'wheelTrials':wheel,'screenshots':shots,'note':'Separate actual native NUnit counts and standalone physical/graphics observations; no G2 approval'}
(out/'native-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
lines=['# Actual R3.4.1 real R3/R6 matrix','', '4first seed7 attempts +8new seed31/83 attempts;12distinct configurations, no duplicate seed7 retest. API-steered canonical force/contact solver, not manual player input.','', '| Recipe | Rig | Seed | Status | Distance m | Seconds | Contacts min/max | Zero queries | Query episode estimate s | Seams | Parts | Final XYZ | Finish |','|---|---|---|---|---|---|---|---|---|---|---|---|']
for t in wheel:
 pos=t['lastPosition'];values=[t['recipe'],t['vehicle'],t['seed'],t['status'],f"{t['distance']:.3f}",f"{t['seconds']:.3f}",f"{t['minimumContacts']}/{t['maximumContacts']}",f"{t['samples']-t['groundedSamples']}/{t['samples']}",f"{t['longestNoContact']:.3f}",t['seamTransitions'],f"{t['partsBefore']}→{t['partsAfter']}",'/'.join(f'{pos[k]:.3f}' for k in ('x','y','z')),t['reachedFinish']]
 lines.append('| '+' | '.join(map(str,values))+' |')
lines+=['','Distance is accumulated3D body movement including initial settling; it is not a pure horizontal odometer. Contacts are original R6WheelContacts fixed-step queries. The corrected own QA episode counter accumulates0.02s per unsupported query; that estimate is not continuous airborne duration. Finite65s case cap and2.4m horizontal waypoint/finish tolerance are unchanged from the prior verified R6 harness. Optional side ramps were not traversed in this protected-route matrix.']
(out/'REAL_R6_MATRIX.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
lines=['# Independently executed original cube/flat tests','', '| Native NUnit case | Actual result |','|---|---|']
for c in cube+flat:lines.append('| '+c['name']+' | '+c['result']+' |')
lines+=['','Original assertions and limits unchanged. Detailed positions/velocities/support-ray counts are emitted by producer code on failure; successful cases do not emit those numeric values. Native PASS proves their actual unchanged assertions, not fabricated per-case telemetry or real-wheel drive.']
(out/'CUBE_AND_FLAT_CASES.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(json.dumps({'native':{k:{a:b for a,b in v.items() if a!='process'} for k,v in native.items()},'cubePassed':sum(c['result']=='Passed' for c in cube),'flat':flat[0]['result'],'materialStatus':material['status'],'wheelRuns':{k:{a:b for a,b in v.items() if a!='launch'} for k,v in wheel_runs.items()},'PNG':len(shots)}))
