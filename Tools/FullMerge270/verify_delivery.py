"""Fail closed on missing actual native/Player/delivery evidence; never invent PASS."""
import hashlib,json,pathlib,subprocess,xml.etree.ElementTree as ET

r=pathlib.Path(__file__).resolve().parents[2]
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def git(*args): return subprocess.check_output(['git','-C',str(r),*args]).decode().strip()
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
folders=sorted(p for p in (r/'Artifacts/Local').glob('FM270-*') if p.is_dir())
receipts=sorted((r/'Artifacts/Local').glob('FM270-*/Build-process.json'),key=lambda p:p.stat().st_mtime)
assert receipts,'No actual build receipt'
build=read(receipts[-1]);source=build['source'];assert build['exitCode']==0
assert not git('diff','--name-only',source,'--','Assets','Packages','ProjectSettings'),'Delivered Unity source differs from the build'
full=next(p for p in reversed(folders) if p.name.startswith('FM270-FinalFullRelease-'))
native=[];full_failures=[]
for phase,minimum in [('EditMode',1100),('PlayMode',400)]:
 receipt=read(full/(phase+'-process.json'));n=ET.parse(full/(phase+'.xml')).getroot()
 counts={k:int(n.get(k)) for k in ('total','passed','failed','skipped')}
 assert counts['total']>=minimum and counts['passed']+counts['failed']==counts['total'] and counts['skipped']==0,(phase,counts)
 assert receipt['exitCode']==(2 if counts['failed'] else 0),(phase,receipt)
 failures=[c.get('fullname') for c in n.findall('.//test-case') if c.get('result')=='Failed'];assert len(failures)==counts['failed']
 full_failures.extend(failures)
 native.append({'phase':phase,'source':receipt['source'],'nativeExit':receipt['exitCode'],'counts':counts,'originalFailures':failures,'evidence':'Docs/FullMerge270/Evidence/'+full.name})
full_source=native[0]['source'];assert native[1]['source']==full_source
changed=git('diff','--name-only',full_source,source,'--','Assets','Packages','ProjectSettings').splitlines()
allowed={'Assets/B01Packages/CAMERA001/Runtime/Camera001Rig.cs','Assets/R1/R1CheckpointRace.cs','Assets/R1/FullMergeRuntimeQA.cs','Assets/Tests/Editor/FullMergeAdditionalRegression.cs','Assets/R1/R1ActivityHUD.cs',
 'Assets/B01Construction/BUILD001/Runtime/BuildVolumeValidator.cs','Assets/R1/R1RoadActivities.cs','Assets/R3/R3RestorationValidation.cs','Assets/Tests/Editor/MOVSeriesSpecialTests.cs','Assets/Tests/Editor/RRLW6PointDefenseTests.cs','Assets/Tests/Editor/RRLW6ShieldTests.cs','Assets/Pack/P12_PipelineSafeCamera.cs'}
assert set(changed)<=allowed,('Uncovered production delta after full regression',changed)
delta_native=[]
if changed or full_failures:
 delta=next(p for p in reversed(folders) if p.name.startswith('FM270-FinalDelta-'))
 delta_source=read(delta/'Compile-process.json')['source']
 for phase in ('Compile','EditMode','PlayMode'):
  q=read(delta/(phase+'-process.json'));assert q['source']==delta_source and q['exitCode']==0,(phase,q)
  if phase!='Compile':
   n=ET.parse(delta/(phase+'.xml')).getroot();assert int(n.get('failed'))==0 and int(n.get('skipped'))==0 and int(n.get('total'))>0
   delta_native.append({'phase':phase,'source':delta_source,'nativeExit':q['exitCode'],'counts':{k:int(n.get(k)) for k in ('total','passed','failed','skipped')},'evidence':'Docs/FullMerge270/Evidence/'+delta.name})
 n=ET.parse(delta/'EditMode.xml').getroot();names={c.get('name') for c in n.findall('.//test-case') if c.get('result')=='Passed'}
 assert {'HeavyHullFitsAtAllGameplayPitches','WideHullFitsAtQuarterOrbits','ActivityApisRejectMissingPhysicalPlayer','StaticWorldMeshAllowsCertifiedClearance','StaticWorldMeshPenetrationStillBlocksInstallation','RaisedJunctionDoesNotPutACurbAcrossItsIncomingGrade'}<=names,('Missing delta regression cases',names)
 passed_fullnames={c.get('fullname') for c in n.findall('.//test-case') if c.get('result')=='Passed'}
 assert set(full_failures)<=passed_fullnames,('Unclosed full-run failures',set(full_failures)-passed_fullnames)
 if delta_source!=source:
  last_delta=git('diff','--name-only',delta_source,source,'--','Assets','Packages','ProjectSettings').splitlines()
  assert set(last_delta)<={'Assets/R1/R1RoadActivities.cs','Assets/Tests/Editor/FullMergeAdditionalRegression.cs','Assets/B01Packages/CAMERA001/Runtime/Camera001Rig.cs','Assets/Pack/P12_PipelineSafeCamera.cs'},('Uncovered change after comprehensive targeted closure',last_delta)
  road=next(p for p in reversed(folders) if p.name.startswith('FM270-FinalCameraClosure-'))
  for phase in ('Compile','Build','EditMode'):
   q=read(road/(phase+'-process.json'));assert q['source']==source and q['exitCode']==0,(phase,q)
  n=ET.parse(road/'EditMode.xml').getroot();assert int(n.get('failed'))==0 and int(n.get('skipped'))==0 and int(n.get('total'))>0
  final_cases={c.get('name') for c in n.findall('.//test-case') if c.get('result')=='Passed'}
  assert {'RaisedJunctionDoesNotPutACurbAcrossItsIncomingGrade','RoadTimeAttackHasRealSourceWorldAndSharedVehicle','RoadResultUsesOneCanonicalRewardAndAllowsNextActivity','ArticulatingGunCannotFeedBackIntoTheCameraAimAnchor'}<=final_cases
  delta_native.append({'phase':'FinalCameraClosure/EditMode','source':source,'nativeExit':0,'counts':{k:int(n.get(k)) for k in ('total','passed','failed','skipped')},'evidence':'Docs/FullMerge270/Evidence/'+road.name})
  q=read(road/'PlayMode-process.json');n=ET.parse(road/'PlayMode.xml').getroot()
  assert q['source']==source and q['exitCode']==0 and int(n.get('failed'))==int(n.get('skipped'))==0 and int(n.get('total'))>0
  delta_native.append({'phase':'FinalCameraClosure/PlayMode','source':source,'nativeExit':0,'counts':{k:int(n.get(k)) for k in ('total','passed','failed','skipped')},'evidence':'Docs/FullMerge270/Evidence/'+road.name})
windows=[]
for p in folders:
 if not p.name.startswith('FM270-Windows-') or not (p/'process.json').exists():continue
 launch=read(p/'launch.json');proc=read(p/'process.json')
 if launch['source']!=source:continue
 for result in p.glob('*result.json'):
  q=read(result)
  windows.append({'mode':launch['mode'],'vehicle':launch['vehicle'],'width':launch['width'],'launch':launch,'process':proc,'result':q,'evidence':'Docs/FullMerge270/Evidence/'+p.name})
def passed(x):return x['process']['exitCode']==0 and x['result']['status']=='Passed' and not x['result']['errors']
selected=[]
for vehicle in ('Light','Heavy','Tall','Wide'):
 for width in (1280,1920):
  cases=[x for x in windows if x['mode']=='Camera' and x['vehicle']==vehicle and x['width']==width and passed(x)]
  assert cases,('Missing actual camera acceptance',vehicle,width)
  q=cases[-1]['result'];assert all(q[k]==0 for k in ('selfRayHits','worldOverlaps','offscreenHullFrames','continuousSpeedViolations'))
  assert q['settledAimObserved'] and q['settledAimError']<=.05 and q['shots']>0 and q['distance']>2
  selected.append(cases[-1])
for mode,vehicles in [('Loop',('Light','Heavy')),('Boss',('Heavy',)),('Mapgen',('Standard',)),('Input',('Standard',))]:
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
result={'status':'REQUIRED_AUTOMATED_DELIVERY_EVIDENCE_COMPLETE_WITH_TARGETED_CLOSURE','buildSource':source,'fullRegressionSource':full_source,'fullNative':native,'finalTargetedNative':delta_native,'fullRunFailuresClosedByExactName':full_failures,'targetedSourceDelta':changed,'requiredWindows':[{'mode':x['mode'],'vehicle':x['vehicle'],'width':x['width'],'evidence':x['evidence']} for x in selected],'zipSHA256':release['zipSHA256'],'limits':'Original full-run failures/exits remain FAIL in evidence, never rewritten to PASS. Matched targeted retests certify the final source delta. API-controlled outcomes are not human gameplay; no G1/G2 approval; manual UI/shutdown separate.'}
(r/'Docs/FullMerge270/delivery-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result,indent=2))
