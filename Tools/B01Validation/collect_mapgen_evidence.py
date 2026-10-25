"""Collect measured native evidence; preserve failures and distinguish custom physical XML."""
import csv,hashlib,json,shutil,struct,zipfile
import xml.etree.ElementTree as ET
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Docs/MAPGEN001NativeQA'
runs={'Calibration':'MAPGEN-R32-calibration-20261025T101345Z','Matrix':'MAPGEN-R32-matrix-20261025T101455Z','RampProbes':'MAPGEN-R32-ramp-probes-20261025T102656Z'}
summary={};shots=[]
for label,run in runs.items():
 src=root/'Artifacts/Local'/run;dest=out/label;dest.mkdir(exist_ok=True)
 data=json.loads((src/'wheel-trials.json').read_text());process=json.loads((src/'process.json').read_text(encoding='utf-8-sig'))
 for name in ['wheel-trials.json','wheel-trace.csv','launch.json','process.json']:shutil.copyfile(src/name,dest/name)
 # These XML rows describe custom standalone native telemetry, NOT NUnit test execution.
 tree=ET.Element('native-physics-diagnostic',method=data['method'],source=data['source'],target=data['vehicleSource'],nativeExit=str(process['exitCode']))
 for row in data['trials']:
  ET.SubElement(tree,'trial',id=row['id'],status=row['status'],reachedGeneratedFinish=str(row['reachedFinish']),distance=str(row['distance']),seconds=str(row['seconds']),minimumContacts=str(row['minimumContacts']),maximumContacts=str(row['maximumContacts']))
 ET.ElementTree(tree).write(dest/'physical-trials-diagnostic.xml',encoding='utf-8',xml_declaration=True)
 names=list(src.glob('*.png')) if label!='Calibration' else []
 for p in names:
  shutil.copyfile(p,dest/p.name);b=p.read_bytes();w,h=struct.unpack('>II',b[16:24]);shots.append({'path':str((dest/p.name).relative_to(out)).replace('\\','/'),'width':w,'height':h,'sha256':hashlib.sha256(b).hexdigest()})
 summary[label]={'nativeExit':process['exitCode'],'status':data['status'],'rows':len(data['trials']),'passed':sum(r['status']=='PASS' for r in data['trials']),'errors':data['errors']}
 if label=='Matrix':matrix=data
assert len(matrix['trials'])==19 and sum(r['group']=='BASE' for r in matrix['trials'])==12
header=['Group','Recipe','Rig','Seed','Difficulty','Result','Distance m','Seconds','Contacts min/max','Zero queries','Seams','Max tilt deg','Parts before/after','Finish']
lines=['# Actual R3/R6 traversal table','', '19 independent physical attempts;17 distinct recipe/rig/seed/difficulty configurations. Historical normal7/31 repeat two base controls. API force driver, not human play.','', '| '+' | '.join(header)+' |','|'+'|'.join(['---']*len(header))+'|']
for r in matrix['trials']:
 values=[r['group'],r['recipe'],r['vehicle'],r['seed'],r['difficulty'],r['status'],f"{r['distance']:.2f}",f"{r['seconds']:.2f}",f"{r['minimumContacts']}/{r['maximumContacts']}",f"{r['samples']-r['groundedSamples']}/{r['samples']}",r['seamTransitions'],f"{r['maxTilt']:.2f}",f"{r['partsBefore']}/{r['partsAfter']}",r['reachedFinish']]
 lines.append('| '+' | '.join(map(str,values))+' |')
lines+=['','Contacts are actual R6WheelContacts queries at configured0.02s fixed cadence, after warmup. Three Light course rows have one zero query each. Raw longestNoContact=0 undercounts a single-query episode; do not read it as continuous contact. Use zero-query counts above; substep contact duration was not measured.','', 'A PASS means the real chassis reached all generated waypoints and finished within2.4m horizontally, without loss of parts, fall, flip or unresolved stall under the65s limit. Optional side ramps were avoided in this protected-spine matrix; see separate RampProbes data.']
(out/'PHYSICAL_TRIALS.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
(out/'native-summary.json').write_text(json.dumps({'runs':summary,'screenshots':shots,'distinctMatrixConfigurations':len({(r['recipe'],r['vehicle'],r['seed'],r['difficulty']) for r in matrix['trials']})},indent=2)+'\n',encoding='utf-8')
def scrub(text):
 return '\n'.join('[connection identifier omitted]' if line.startswith('Player connection [') else '  Machine Id: [omitted]' if line.lstrip().startswith('Machine Id:') else line.rstrip() for line in text.splitlines())+'\n'
with zipfile.ZipFile(out/'native-build-and-player-logs.zip','x',compression=zipfile.ZIP_DEFLATED) as z:
 for name in ['MAPGEN-R32-QA-build','MAPGEN-R32-QA-build-2','MAPGEN-R32-QA-build-3',*runs.values()]:
  folder=root/'Artifacts/Local'/name
  for p in folder.iterdir():
   if p.name in ('Editor.log','Player.log','process.json'):z.writestr(name+'/'+p.name,scrub(p.read_text(encoding='utf-8-sig',errors='replace')))
print(json.dumps(summary,indent=2))
