"""Collect actual RC1 evidence. Standalone observations never inflate NUnit counts."""
import argparse,datetime,gzip,hashlib,json,math,pathlib,shutil,xml.etree.ElementTree as ET
from PIL import Image
p=argparse.ArgumentParser();p.add_argument('--require-complete',action='store_true');args=p.parse_args()
root=pathlib.Path.cwd();local=root/'Artifacts/Local';out=root/'Docs/B01_RC1';out.mkdir(parents=True,exist_ok=True)
intake=root.parent/'B01-RC1-Intake';release=root.parent/'Releases/B01_RC1_20261025'
runs={'BaseInterrupted':'RC1-Base-20261025T184651Z','BaseNative':'RC1-Base-20261025T191245Z',
 'HostRed':'RC1-Targeted-20261025T215132Z','HostGreen':'RC1-Targeted-20261025T215432Z',
 'ExperimentalBuild':'RC1-Experimental-20261025T220542Z','PreGuardNative':'RC1-Experimental-20261025T221008Z',
 'GuardRed':'RC1-Targeted-20261025T224911Z','FinalNative':'RC1-Experimental-20261025T225108Z',
 'BaseUI720':'RC1-Base-Interactive720','BaseUI1080':'RC1-Base-Interactive1080',
 'ExperimentalUI720':'RC1-EXP-Interactive720','ExperimentalUI1080':'RC1-EXP-Interactive1080',
 'MapgenHidden720':'RC1-MAPGEN-API720','MapgenVisible720':'RC1-MAPGEN-API720-Visible',
 'MapgenVisible1080':'RC1-MAPGEN-API1080-Visible','ReactorWindows':'RC1-OBJ-API720-Visible',
 'FinalMapgen720':'RC1-FINAL-MAPGEN-API720','FinalMapgen1080':'RC1-FINAL-MAPGEN-API1080','FinalReactor':'RC1-FINAL-OBJ-API720',
 'FinalRetailLaunch':'RC1-FINAL-Retail-Launch'}
def sha(data):return hashlib.sha256(data).hexdigest()
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def save(path,value):path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
summary={'baseSource':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','gameSource':'86eb7bb77e6bfc7787f781ea1ccda547b8a4f643','preGuardFullRegressionSource':'8ac3a814b692a380033323b188747fe106314054','native':{},'windows':{},'screenshots':[],'evidence':[],'artifacts':{},'independentG1':'PENDING','G2':'HOLD'}
for label,folder in runs.items():
    source=local/folder;dest=out/label;dest.mkdir(exist_ok=True)
    for file in source.iterdir():
        if file.suffix not in ('.json','.xml','.log','.png','.csv'):continue
        raw=file.read_bytes();target=dest/file.name
        if file.suffix=='.log':
            lines=raw.decode('utf-8-sig',errors='replace').splitlines()
            lines=['[connection identifier omitted]' if s.startswith('Player connection [') else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in lines]
            clean=('\n'.join(s.rstrip() for s in lines)+'\n').encode()
            target=dest/(file.name+'.gz');target.write_bytes(gzip.compress(clean,mtime=0))
        else:target.write_bytes(raw)
        summary['evidence'].append({'path':target.relative_to(out).as_posix(),'sourceSha256':sha(raw),'publicSha256':sha(target.read_bytes()),'bytes':target.stat().st_size})
        if file.suffix=='.png':
            with Image.open(file) as img:
                rgb=img.convert('RGB');black=rgb.getbbox() is None
                summary['screenshots'].append({'path':target.relative_to(out).as_posix(),'width':img.width,'height':img.height,'allBlack':black,'sha256':sha(raw),'visualEvidenceValid':not black})
    if label in ('BaseNative','HostRed','HostGreen','PreGuardNative','GuardRed','FinalNative'):
        modes={}
        for mode in ('EditMode','PlayMode'):
            xml=source/(mode+'.xml')
            if not xml.exists():continue
            tree=ET.parse(xml).getroot();proc=read(source/(mode+'-process.json'))
            cases=[{'name':c.get('fullname') or c.get('name'),'result':c.get('result')} for c in tree.iter('test-case')]
            modes[mode]={k:int(tree.get(k,'0')) for k in ('total','passed','failed','skipped')}
            modes[mode].update({'result':tree.get('result'),'process':proc,'cases':cases})
            if label=='HostGreen':
                modes[mode]['sourceStatus']='Working-tree diagnostic: recorded HEAD bc46c8a plus uncommitted host bridge; not immutable final acceptance. Original process metadata preserved.'
        summary['native'][label]=modes
    resultFile=next(iter(source.glob('*native-result.json')),None)
    if resultFile is not None:
        r=read(resultFile);processFile=source/'process.json'
        item={'status':r['status'],'rawChecks':len(r['checks']),'distinctCheckNames':len({c['name'] for c in r['checks']}),'failedChecks':[c for c in r['checks'] if not c['passed']],'errors':r['errors'],'width':r['width'],'height':r['height'],'method':r['method'],'nativeExit':read(processFile)['exitCode'] if processFile.exists() else None}
        if 'distance' in r:
            a,b=r['start'],r['last'];item.update({'distance3DIncludingSettling':r['distance'],'netHorizontalMeters':math.hypot(b['x']-a['x'],b['z']-a['z']),'start':a,'last':b,'playerHpLost':r['playerHpLost'],'enemyHpLost':r['enemyHpLost'],'contactSamples':r['contactSamples'],'supportedSamples':r['supportedSamples'],'maximumContacts':r['maxContacts'],'measuredEnvelope':r['measuredEnvelope'],'firstHash':r['firstHash'],'secondHash':r['secondHash']})
        summary['windows'][label]=item
    launch=source/'launch.json'
    if launch.exists():
        launched=read(launch);exe=pathlib.Path(launched['exe']);managed=exe.parent/(exe.stem+'_Data')/'Managed'
        hashes={}
        for name in ('Assembly-CSharp.dll','B01.MAPGEN001.Runtime.dll','B01.MAPGENWORLD001.Runtime.dll'):
            dll=managed/name
            if dll.exists():hashes[name]={'sha256':sha(dll.read_bytes()),'bytes':dll.stat().st_size,'lastWriteUtc':datetime.datetime.fromtimestamp(dll.stat().st_mtime,datetime.timezone.utc).isoformat()}
        audit={'kind':'post-run binary audit; files were not rebuilt between runs','recordedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'launchSource':launched['source'],'exeSha256':sha(exe.read_bytes()),'managed':hashes}
        # Preserve audits captured before the later guarded source rebuilt the QA folder.
        if not (dest/'binary-audit.json').exists():save(dest/'binary-audit.json',audit)
for name in ('mapgen-source-verification.json','mapgen-target-preflight.json','mapgen-first-stage.json','mapgen-final-stage.json','world-first-stage.json','world-final-stage.json','initial-worktrees.json','rollback-proof.txt','rollback-proof-final.txt'):
    shutil.copyfile(intake/name,out/name)
shutil.copyfile(local/'RC1-source-integrity.json',out/'source-integrity.json')
for variant in ('BASE','MAPGEN_EXPERIMENTAL'):
    prefix='RoboRougelike_B01_RC1_'+variant+'_Windows_x64'
    receipt=read((release if variant=='BASE' else release/'Final')/(prefix+'-receipt.json'))
    assert sha(pathlib.Path(receipt['zip']).read_bytes())==receipt['zipSha256']
    save(out/(prefix+'-receipt.json'),receipt)
    summary['artifacts'][variant]={k:v for k,v in receipt.items() if k!='files'}
    dll=next(r for r in receipt['files'] if r['path']=='RoboRougelike_Data/Managed/Assembly-CSharp.dll')
    summary['artifacts'][variant]['gameAssemblySha256']=dll['sha256']
    assert b'B01RC1RuntimeQA' not in (pathlib.Path(receipt['extracted'])/'RoboRougelike_Data/Managed/Assembly-CSharp.dll').read_bytes(),'QA fixture leaked into retail'
summary['notes']=['Native NUnit counts are separate from standalone checks and UI observations.','Hidden720 screenshots are black and INVALID visual evidence; failed native exit retained.','UI screenshots are DPI-scaled whole-window captures; visible API captures are native1280x720/1920x1080.','Original raw logs remain local; public gzip logs omit machine/connection identifiers and trailing whitespace only.','Full pre-guard regression1055/423 at8ac3a81; final source86eb7bb differs only by null-player guard and its new test. Final35Edit/423Play revalidated actual final code, no unchanged1021legacy Edit repetition.']
if args.require_complete:
    final=summary['native']['FinalNative'];assert set(final)=={'EditMode','PlayMode'},'Final native run incomplete'
    for r in final.values():assert r['failed']==0 and r['passed']==r['total'] and r['process']['exitCode']==0
    for label in ('FinalMapgen720','FinalMapgen1080','FinalReactor'):
        r=summary['windows'][label];assert r['nativeExit']==0 and r['status']=='Passed' and not r['errors'] and not r['failedChecks']
save(out/'native-summary.json',summary)
print(json.dumps({'native':{label:{mode:{k:r[k] for k in ('total','passed','failed','skipped')} for mode,r in modes.items()} for label,modes in summary['native'].items()},'windows':{label:{k:r[k] for k in ('status','nativeExit','rawChecks','distinctCheckNames')} for label,r in summary['windows'].items()},'validPNG':sum(s['visualEvidenceValid'] for s in summary['screenshots']),'blackInvalidPNG':sum(s['allBlack'] for s in summary['screenshots']),'zipBytes':{k:r['zipBytes'] for k,r in summary['artifacts'].items()}}))
