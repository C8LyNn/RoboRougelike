import csv,gzip,hashlib,json,pathlib,shutil,xml.etree.ElementTree as ET
from PIL import Image,ImageStat
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/CAMERA001R12NativeQA'
finalPlayers=sorted((r/'Artifacts/Local').glob('CameraR12-Player-*'))
assert len(finalPlayers)==3,'Collect only after final bounded follow-up exists'
sources={'Native':'CameraR12-Tests-20261026T000603Z','BuildInitial':'CameraR12-Build-20261026T000907Z','BuildDiagnostic':'CameraR12-Build-20261026T001430Z','BuildFinal':'CameraR12-Build-20261026T002026Z','WindowsInitial':'CameraR12-Player-20261026T001057Z','WindowsDiagnostic':'CameraR12-Player-20261026T001510Z','WindowsFinal':finalPlayers[-1].name}
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
for name,folder in sources.items():
 src=r/'Artifacts/Local'/folder;dest=out/name;dest.mkdir(exist_ok=True)
 for f in src.iterdir():
  if f.suffix not in ('.json','.xml','.log','.csv','.png','.jsonl'):continue
  data=f.read_bytes()
  if f.suffix=='.log':
   lines=data.decode('utf-8-sig',errors='replace').splitlines()
   lines=['[connection identifiers omitted]' if s.startswith('Player connection [') else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in lines]
   data=('\n'.join(s.rstrip() for s in lines)+'\n').encode()
  if f.suffix in ('.log','.jsonl'):
   (dest/(f.name+'.gz')).write_bytes(gzip.compress(data,mtime=0))
  else:(dest/f.name).write_bytes(data)
native={}
for mode,expected in [('EditMode',5),('PlayMode',13)]:
 node=ET.parse(out/'Native'/f'{mode}.xml').getroot();p=read(out/'Native'/f'{mode}-process.json')
 native[mode]={'counts':{k:int(node.get(k)) for k in ('total','passed','failed','skipped')},'process':p,'cases':[{'name':x.get('fullname'),'result':x.get('result')} for x in node.findall('.//test-case')]}
 assert native[mode]['counts']=={'total':expected,'passed':expected,'failed':0,'skipped':0} and p['exitCode']==0
windows={};shots=[]
for name in ('WindowsInitial','WindowsDiagnostic','WindowsFinal'):
 dest=out/name;d=read(dest/'functional-results.json');p=read(dest/'process.json')
 assert len(d['rows'])==8 and d['status']!='Running'
 traces=list(csv.DictReader((dest/'camera-trace.csv').read_text().splitlines()))
 phases={}
 for phase in sorted({s['phase'] for s in traces}):
  rows=[s for s in traces if s['phase']==phase]
  peak=max(rows,key=lambda x:float(x['step']))
  phases[phase]={'samples':len(rows),'originOwnHits':sum(x['actualOriginOwnHit']=='True' for x in rows),'nearClipOwnHits':sum(x['actualOwnBlocked']=='True' for x in rows),'worldOverlaps':sum(x['worldSphereOverlap']=='True' for x in rows),'maxStep':float(peak['step']),'maxStepExcess':max(float(x['stepExcess']) for x in rows),'maxAimError':max(float(x['actualAimError']) for x in rows),'peak':peak}
 screenshots=[]
 for f in sorted(dest.glob('*.png')):
  with Image.open(f) as im:
   expected=(1280,720) if '-1280-' in f.name else (1920,1080)
   assert im.size==expected,(f,im.size)
   extrema=im.convert('RGB').getextrema();assert any(a!=b for a,b in extrema),(f,'uniform screenshot')
   screenshots.append({'path':name+'/'+f.name,'width':im.width,'height':im.height,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
 assert len(screenshots)==48
 diagnostic=ET.Element('windows-camera-diagnostic',source=d['source'],receiver=p['source'],nativeExit=str(p['exitCode']),method=d['method'])
 for row in d['rows']:
  ET.SubElement(diagnostic,'case',id=row['id'],status=row['status'],ownOriginHits=str(row['finalOriginOwnHits']),ownNearClipHits=str(row['ownReticleHits']),worldOverlaps=str(row['worldSphereOverlapSamples']),speedViolations=str(row['speedCapViolations']),aimViolations=str(row['aimErrorViolations']))
 ET.ElementTree(diagnostic).write(dest/'functional-diagnostic.xml',encoding='utf-8',xml_declaration=True)
 windows[name]={'process':p,'launch':read(dest/'launch.json'),'overall':d['status'],'rows':d['rows'],'errors':d['errors'],'samples':len(traces),'phases':phases,'screenshots':screenshots}
 if name in ('WindowsDiagnostic','WindowsFinal'):
  aim=[json.loads(s) for s in gzip.decompress((dest/'aim-trace.jsonl.gz').read_bytes()).decode().splitlines()]
  shot=[json.loads(s) for s in gzip.decompress((dest/'shots.jsonl.gz').read_bytes()).decode().splitlines()]
  percase=[]
  for row in d['rows']:
   a=[x for x in aim if x['id']==row['id'] and x['phase']=='inputLMBDrive' and x['sincePhase']>1.5];peak=max(a,key=lambda x:x['consumedError'])
   allcase=[x for x in aim if x['id']==row['id']]
   shotscase=[x for x in shot if x['id']==row['id']]
   paired=[]
   for sh in shotscase:
    aa=[x for x in allcase if x['gun']==sh['part'] and abs(x['time']-sh['time'])<.0001]
    assert len(aa)==1,(sh,len(aa))
    paired.append({'shot':sh,'aim':aa[0]})
   percase.append({'id':row['id'],'peak':peak,'gunTrackingViolationsAfter1_5s':sum(x['consumedError']>.05 for x in a),'identifiedShots':len(shotscase),'maximumFiringFrameAimError':max(x['aim']['consumedError'] for x in paired),'firingFrameViolations':sum(x['aim']['consumedError']>.05 for x in paired),'shots':paired})
  (out/('aim-diagnostics.json' if name=='WindowsFinal' else 'aim-diagnostics-initial.json')).write_text(json.dumps(percase,indent=2)+'\n')
  windows[name]['aimSummary']=[{k:v for k,v in x.items() if k!='shots'} for x in percase]
 if name=='WindowsFinal':
  final=list(csv.DictReader((dest/'final-rendered-pose.csv').read_text().splitlines()))
  windows[name]['endOfFrame']={'samples':len(final),'ownOriginHits':sum(x['ownOrigin']=='True' for x in final),'ownNearClipHits':sum(x['ownNearClip']=='True' for x in final),'insideOwn':sum(x['insideOwn']=='True' for x in final),'worldOverlaps':sum(x['worldOverlap']=='True' for x in final)}
  windows[name]['flightAimMaximum']=max(x['flightAimError'] for x in shot)
  windows[name]['flightAimViolations']=sum(x['flightAimError']>.05 for x in shot)
coreFail=any(x[k] for x in windows['WindowsFinal']['rows'] for k in ('ownReticleHits','finalOriginOwnHits','originInsideOwnSamples','speedCapViolations','worldSphereOverlapSamples','aimErrorViolations','finalRenderedOwnHits','finalRenderedWorldOverlaps'))
summary={'producer':'48581e49a878eca376674551017532bf17fd35b2','base':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','compile':read(out/'Native/Compile-process.json'),'native':native,'builds':{k:read(out/k/'Build-process.json') for k in ('BuildInitial','BuildDiagnostic','BuildFinal')},'windows':windows,'cameraStatus':'FAIL' if coreFail else 'PASS_OBSERVED','overallFunctional':windows['WindowsFinal']['overall'],'G2':'NOT_GRANTED'}
(out/'native-summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'native':{k:v['counts'] for k,v in native.items()},'windows':{k:{'exit':v['process']['exitCode'],'samples':v['samples'],'rows':[(x['id'],x['status'],x.get('maxMuzzleConsumedAfterSettle',x.get('maxMuzzleAfterSettle'))) for x in v['rows']]} for k,v in windows.items()},'endOfFrame':windows['WindowsFinal']['endOfFrame'],'flightAimMaximum':windows['WindowsFinal']['flightAimMaximum'],'cameraStatus':summary['cameraStatus']}))
