"""Fail closed on missing actual native/Player/delivery evidence; never invent PASS."""
import hashlib,json,pathlib,subprocess,xml.etree.ElementTree as ET

r=pathlib.Path(__file__).resolve().parents[2]
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def git(*args): return subprocess.check_output(['git','-C',str(r),*args]).decode().strip()
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
folders=sorted((r/'Artifacts/Local').glob('FM270-*'))
receipts=sorted((r/'Artifacts/Local').glob('FM270-*/Build-process.json'),key=lambda p:p.stat().st_mtime)
assert receipts,'No actual build receipt'
build=read(receipts[-1]);source=build['source'];assert build['exitCode']==0
assert not git('diff','--name-only',source,'--','Assets','Packages','ProjectSettings'),'Delivered Unity source differs from the build'
full=next(p for p in reversed(folders) if p.name.startswith('FM270-FinalFullRelease-'))
native=[]
for phase,minimum in [('EditMode',1100),('PlayMode',400)]:
 receipt=read(full/(phase+'-process.json'));n=ET.parse(full/(phase+'.xml')).getroot()
 counts={k:int(n.get(k)) for k in ('total','passed','failed','skipped')}
 assert receipt['exitCode']==0 and counts['total']>=minimum and counts['passed']==counts['total'] and counts['failed']==counts['skipped']==0,(phase,counts)
 native.append({'phase':phase,'source':receipt['source'],'counts':counts,'evidence':full.relative_to(r).as_posix()})
full_source=native[0]['source'];assert native[1]['source']==full_source
changed=git('diff','--name-only',full_source,source,'--','Assets','Packages','ProjectSettings').splitlines()
allowed={'Assets/B01Packages/CAMERA001/Runtime/Camera001Rig.cs','Assets/R1/R1CheckpointRace.cs','Assets/R1/FullMergeRuntimeQA.cs','Assets/Tests/Editor/FullMergeAdditionalRegression.cs'}
assert set(changed)<=allowed,('Uncovered production delta after full regression',changed)
if changed:
 delta=next(p for p in reversed(folders) if p.name.startswith('FM270-FinalDelta-'))
 for phase in ('Compile','EditMode','PlayMode'):
  q=read(delta/(phase+'-process.json'));assert q['source']==source and q['exitCode']==0,(phase,q)
  if phase!='Compile':
   n=ET.parse(delta/(phase+'.xml')).getroot();assert int(n.get('failed'))==0 and int(n.get('skipped'))==0 and int(n.get('total'))>0
 n=ET.parse(delta/'EditMode.xml').getroot();names={c.get('name') for c in n.findall('.//test-case') if c.get('result')=='Passed'}
 assert {'HeavyHullFitsAtMinimumPitch','WideHullFitsAtQuarterOrbits','ActivityApisRejectMissingPhysicalPlayer'}<=names,('Missing delta regression cases',names)
windows=[]
for p in folders:
 if not p.name.startswith('FM270-Windows-') or not (p/'process.json').exists():continue
 launch=read(p/'launch.json');proc=read(p/'process.json')
 if launch['source']!=source:continue
 for result in p.glob('*result.json'):
  q=read(result)
  windows.append({'mode':launch['mode'],'vehicle':launch['vehicle'],'width':launch['width'],'launch':launch,'process':proc,'result':q,'evidence':p.relative_to(r).as_posix()})
def passed(x):return x['process']['exitCode']==0 and x['result']['status']=='Passed' and not x['result']['errors']
selected=[]
for vehicle in ('Light','Heavy','Tall','Wide'):
 for width in (1280,1920):
  cases=[x for x in windows if x['mode']=='Camera' and x['vehicle']==vehicle and x['width']==width and passed(x)]
  assert cases,('Missing actual camera acceptance',vehicle,width)
  q=cases[-1]['result'];assert all(q[k]==0 for k in ('selfRayHits','worldOverlaps','offscreenHullFrames','continuousSpeedViolations'))
  assert q['settledAimObserved'] and q['settledAimError']<=.05 and q['shots']>0 and q['distance']>2
  selected.append(cases[-1])
for mode,vehicles in [('Loop',('Light','Heavy')),('Boss',('Heavy',)),('Mapgen',('Light',)),('Input',('Light',))]:
 for vehicle in vehicles:
  cases=[x for x in windows if x['mode']==mode and x['vehicle']==vehicle and passed(x)]
  assert cases,('Missing actual activity/control acceptance',mode,vehicle)
  if mode=='Loop':assert cases[-1]['result']['completed']==6
  selected.append(cases[-1])
for width in (1280,1920):assert any(x['mode']=='Input' and x['width']==width and passed(x) for x in windows)
release=read(r/'Docs/FullMerge270/release-receipt.json');assert release['source']==source
archive=pathlib.Path(release['zip']);assert archive.stat().st_size==release['zipBytes'] and digest(archive)==release['zipSHA256']
extract=pathlib.Path(release['extracted'])
for row in release['files']:assert digest(extract/row['path'])==row['sha256'],row['path']
assert any(passed(x) and pathlib.Path(x['launch']['executable'])==pathlib.Path(release['exe']) for x in windows),'No successful actual extracted Player launch'
integrity=read(r/'Docs/FullMerge270/final-integrity.json')
assert not integrity['deletedOriginalAssets'] and not integrity['changedOriginalGUIDs'] and not integrity['duplicateGUIDs'] and not integrity['missingMeta'] and integrity['sharedParentPreserved']
result={'status':'REQUIRED_AUTOMATED_DELIVERY_EVIDENCE_PASS','buildSource':source,'fullRegressionSource':full_source,'fullNative':native,'targetedSourceDelta':changed,'requiredWindows':[{'mode':x['mode'],'vehicle':x['vehicle'],'width':x['width'],'evidence':x['evidence']} for x in selected],'zipSHA256':release['zipSHA256'],'limits':'API-controlled outcomes are not human gameplay; no G1/G2 approval; manual UI/shutdown separate.'}
(r/'Docs/FullMerge270/delivery-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result,indent=2))
