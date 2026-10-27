"""Verify release bytes and record actual QA, including failures and missing results."""
import collections,hashlib,json,pathlib,subprocess,xml.etree.ElementTree as ET,zipfile
r=pathlib.Path(__file__).resolve().parents[2];out=r/'Docs/Camera275';source='c61894a6389e43727db42a6dd58f153a496923ee'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
native=r/'Artifacts/Local/FM270-CAM275-ReleaseFull-20261027T124058Z';suites={}
for mode,expected in [('EditMode',1223),('PlayMode',474)]:
    processFile=native/(mode+'-process.json');xmlFile=native/(mode+'.xml')
    if not processFile.exists() or not xmlFile.exists():
        suites[mode]=dict(status='INTERRUPTED_NO_RESULT',processReceipt=processFile.exists(),xml=xmlFile.exists(),log=(native/(mode+'.log')).exists())
        continue
    process=read(processFile);assert process['source']==source
    root=ET.parse(xmlFile).getroot();counts={key:int(root.get(key)) for key in ('total','passed','failed','skipped')}
    previous=ET.parse(r/f'Docs/Exit273/Evidence/FM270-EXIT273-ReleaseFull-20261027T081029Z/{mode}.xml').getroot()
    def methods(xml):return collections.Counter((c.get('classname'),c.get('methodname')) if c.get('classname') and c.get('methodname') else c.get('fullname','').split('(')[0] for c in xml.findall('.//test-case'))
    old,new=methods(previous),methods(root);missing=old-new;assert not missing,missing
    failures=[dict(name=c.get('fullname'),message=c.findtext('failure/message')) for c in root.findall('.//test-case') if c.get('result')=='Failed']
    suites[mode]=dict(status='PASS' if process['exitCode']==0 and counts['failed']==0 and counts['skipped']==0 else 'FAIL',counts=counts,process=process,failures=failures,baselineMethodCaseCountsPreserved=True)
assert not subprocess.check_output(['git','diff','--name-only',source,'--','Assets','Packages','ProjectSettings'],cwd=r).strip(),'Game source changed after freeze'
delivery=read(out/'delivery-verification.json');archive=pathlib.Path(delivery['archive']);exe=pathlib.Path(delivery['exe']);base=exe.parent
assert delivery['source']==source and sha(archive)==delivery['sha256']
with zipfile.ZipFile(archive) as z:assert z.testzip() is None
for f in delivery['files']:assert sha(base/f['path'])==f['sha256'],f['path']
assert sha(base/'RoboRougelike_Data/Managed/Assembly-CSharp.dll')==delivery['assemblySHA256']
shots=sorted((r/'Artifacts/Local').glob('CAM275-Aim-Final-*/aim-result.json'));assert len(shots)==1
shot=read(shots[0]);assert shot['status']=='Passed' and len(shot['shots'])==28 and not shot['errors']
assert all(s['status']=='Passed' for s in shot['shots'])
assert read(shots[0].parent/'process.json')['exitCode']==0
trials=[]
for folder in sorted((r/'Artifacts/Local').glob('CAM275-Windows-Final-*')):
    status=read(folder/'camera-result.json');process=read(folder/'process.json');assert process['source']==source
    assert status['ownRayHits']==status['worldOverlaps']==0,status
    assert process['exitCode'] in (0,1),process
    trials.append(dict(vehicle=status['vehicle'],fps=status['fps'],status=status['status'],exitCode=process['exitCode'],distance=status['distance']))
assert len(trials)==15 and len({(t['vehicle'],t['fps']) for t in trials})==15
extracted=[]
for folder in sorted((r/'Artifacts/Local').glob('FM270-Windows-*')):
    if not (folder/'process.json').exists():continue
    launch=read(folder/'launch.json');process=read(folder/'process.json')
    if str(base).lower() not in str(launch.get('executable','')).lower():continue
    assert process['exitCode']==0 and process['source']==source,process
    extracted.append(dict(stage=folder.name,mode=launch['mode'],vehicle=launch.get('vehicle'),width=launch['width'],exitCode=process['exitCode']))
assert len(extracted)==4,extracted
untracked=subprocess.check_output(['git','ls-files','--others','--exclude-standard','Assets'],cwd=r,text=True).splitlines()
result=dict(status='BUILD_VERIFIED_WITH_OPEN_QA_FAILURES',source=source,generatedUntrackedAssets=untracked,
    reportHead=subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip(),suites=suites,
    archiveSHA256=delivery['sha256'],extractedFilesVerified=len(delivery['files']),shotsPassed=28,driveTrials=trials,
    drivePassed=sum(t['status']=='Passed' for t in trials),driveFailed=sum(t['status']!='Passed' for t in trials),extractedPlayerChecks=extracted,
    qualification='Delivery bytes are verified; failed/incomplete native tests and drive trials remain visible. No moving-target, physical gamepad, final human zoom, or historical UIA-crash claim is inferred from this gate.')
(out/'final-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ('suites','driveTrials','extractedPlayerChecks')},indent=2))
