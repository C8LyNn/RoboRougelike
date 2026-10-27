"""Check delivered bytes and actual receipts; never infer execution from test source."""
import hashlib
import json
import pathlib
import subprocess
import xml.etree.ElementTree as ET
import zipfile

root=pathlib.Path(__file__).resolve().parents[2]
source='8a294e6638d4a82619089c0ab5ad02110847c233'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def digest(path):return hashlib.file_digest(path.open('rb'),'sha256').hexdigest()
assert not subprocess.check_output(['git','-C',str(root),'diff','--name-only',source,'--','Assets','Packages','ProjectSettings']).strip(), 'Delivered Unity source changed'
release=root.parent/'Releases/ROBOROUGELIKE_EXIT273_FINAL_20261027'
receipt=read(release/'release-receipt.json')
assert receipt['source']==source and digest(pathlib.Path(receipt['archive']))==receipt['sha256']
with zipfile.ZipFile(receipt['archive']) as archive:assert archive.testzip() is None
for item in receipt['files']:assert digest(pathlib.Path(receipt['extract'])/item['path'])==item['sha256'],item['path']
assert digest(pathlib.Path(receipt['extract'])/'RoboRougelike_Data/Managed/Assembly-CSharp.dll')==digest(root/'Builds/FULL-MERGE270/RoboRougelike_Data/Managed/Assembly-CSharp.dll')
native=[];case_results={}
for mode in ['EditMode','PlayMode']:
    folders=list((root/'Artifacts/Local').glob('FM270-EXIT273-ReleaseFull-*'))
    assert len(folders)==1
    process=read(folders[0]/(mode+'-process.json'))
    assert process['source']==source and process['exitCode']==0
    result=ET.parse(folders[0]/(mode+'.xml')).getroot()
    counts={k:int(result.get(k)) for k in ['total','passed','failed','skipped']}
    assert counts['total']>0 and counts['total']==counts['passed'] and counts['failed']==counts['skipped']==0
    native.append(dict(mode=mode,source=source,exitCode=0,**counts))
    case_results.update({c.get('fullname'):c.get('result') for c in result.findall('.//test-case')})
old=read(root/'Docs/FullMerge270/Evidence/native-summary.json')
old_failures=[c['name'] for phase in old for c in phase.get('cases',[]) if phase.get('nunit',{}).get('total')==1197 and c['result']=='Failed']
assert len(set(old_failures))==4
for name in set(old_failures):assert case_results.get(name)=='Passed',name
windows=[]
for folder in (root/'Artifacts/Local').glob('*Windows*'):
    if not (folder/'launch.json').exists():continue
    launch=read(folder/'launch.json')
    if launch['source']!=source:continue
    process=read(folder/'process.json')
    assert process['source']==source and process['exitCode']==0
    results=[read(path) for path in folder.glob('*result.json')]
    for result in results:
        assert result['status']=='Passed' and not result.get('error') and not result.get('errors')
        if 'applicationQuitReached' in result:assert result['applicationQuitReached']
    windows.append(dict(mode=launch['mode'],label=launch.get('label'),width=launch['width'],exitCode=0,evidence=str(folder.relative_to(root))))
assert len([row for row in windows if row['mode']!='Manual'])==9
assert len([row for row in windows if row['mode']=='Manual'])==2
operator=read(root/'Artifacts/Local/EXIT273-Final-Operator.json')
assert operator['source']==source and operator['mainMenuQuit']=='PASS' and operator['pauseQuit']=='PASS'
integrity=read(root/'Docs/Exit273/final-integrity.json')
assert integrity['sha']==source and not integrity['missingMeta'] and not integrity['duplicateGUIDs'] and not integrity['changedOriginalGUIDs'] and not integrity['deletedOriginalAssets']
result=dict(status='READY_WITH_DOCUMENTED_ENGINE_LIMITATION',source=source,native=native,previousFullFailuresNowPassed=sorted(set(old_failures)),finalWindows=windows,manual=operator,zipSHA256=receipt['sha256'],zipBytes=receipt['bytes'],guidCount=integrity['guidCount'],historicalUIACrash='NOT_PROVEN_FIXED',physicalGamepad='NOT_RUN')
(root/'Docs/Exit273/delivery-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['manual','previousFullFailuresNowPassed','finalWindows']},indent=2))
