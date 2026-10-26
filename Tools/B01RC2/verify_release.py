"""Final evidence gate; no Unity execution or rewriting of results."""
import hashlib, json, pathlib, subprocess, xml.etree.ElementTree as ET, zipfile

r = pathlib.Path(__file__).resolve().parents[2]
d = r / 'Docs/B01_RC2'
source = '46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f'
qa_only={'Assets/Tests/Editor/R1PlayIntegrationTests.cs','Assets/Tests/Editor/RRLW34ReviewRegressionTests.cs'}
changed=set(subprocess.check_output(['git','-C',str(r),'diff','--name-only',source,'--','Assets','Packages','ProjectSettings'],text=True).splitlines())
assert changed<=qa_only, 'Production source differs from the actual release build'
assert not subprocess.check_output(['git','-C',str(r),'ls-files','--others','--exclude-standard','Assets','Packages','ProjectSettings']).strip()
summary = json.loads((d/'Native/native-summary.json').read_text())
final = [x for x in summary if x['stage']=='FinalRegression']
assert {x['phase'] for x in final} == {'EditMode','PlayMode'}, 'Full native cycle incomplete'
assert all(phase['source']==source for phase in final)
edit=next(x for x in final if x['phase']=='EditMode')
play=next(x for x in final if x['phase']=='PlayMode')
known_failures={
    'R1PlayIntegrationTests.KeyboardEnterStartsRunWithoutPrematureDefeat',
    'RRLW34ReviewRegressionTests.FullInventoryHasExplicitRecoveryForMandatoryRewardsIncludingFinalVictory'
}
assert edit['exitCode']==2 and edit['nunit']=={'total':1068,'passed':1066,'failed':2,'skipped':0}
assert {c['name'] for c in edit['cases'] if c['result']!='Passed'}==known_failures
assert play['exitCode']==0 and play['nunit']['total']==play['nunit']['passed']==424
assert play['nunit']['failed']==play['nunit']['skipped']==0
assert all(c['result']=='Passed' for c in play['cases'])
recheck=next(x for x in summary if x['stage']=='LegacyFixturesRecheck' and x['phase']=='EditMode')
assert recheck['source']=='f6184d2254c0ff4c59bcd1e9f21db6a134a54097' and recheck['exitCode']==0
assert recheck['nunit']=={'total':2,'passed':2,'failed':0,'skipped':0}
assert {c['name'] for c in recheck['cases']}==known_failures and all(c['result']=='Passed' for c in recheck['cases'])
for phase in ('Compile','Build'):
    receipt=json.loads((d/f'Native/RC2-ReleaseCandidate-20261026T115006Z/{phase}-process.json').read_text(encoding='utf-8-sig'))
    assert receipt['exitCode']==0 and receipt['source']==source
windows=json.loads((d/'Windows/windows-summary.json').read_text())
finite=[x for x in windows if x['mode']!='Retail']
assert len(finite)==7
for run in finite:
    assert run['source']==source and run['process']['exitCode']==0
    assert run['results'] and all(x['status']=='Passed' and not x['errors'] and not x['failedChecks'] for x in run['results'])
    assert not run['logErrorLines'] and run['screenshots'] and all(x['nonBlack'] for x in run['screenshots'])
    assert all((x['width'],x['height'])==(run['width'],run['height']) for x in run['screenshots'])
integrity=json.loads((d/'final-integrity.json').read_text())
assert not integrity['duplicateGUIDs'] and not integrity['missingMeta'] and not integrity['blockedAssetsPresent']
assert integrity['sharedParentPreserved'] and integrity['originalRewardPortIdentical']
for item in integrity['packages']:
    assert not item['mismatch']
    manifest=json.loads((d/f"Intake/{item['package']}-integrity.json").read_text())
    for f in manifest['files']:
        assert hashlib.sha256((r/f['path']).read_bytes()).hexdigest()==f['sha256'],f['path']
receipt=json.loads((d/'release-receipt.json').read_text())
archive=pathlib.Path(receipt['zip'])
assert receipt['source']==source and archive.stat().st_size==receipt['zipBytes']
assert hashlib.sha256(archive.read_bytes()).hexdigest()==receipt['zipSHA256']
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for f in receipt['files']:
        assert hashlib.sha256(z.read(f['path'])).hexdigest()==f['sha256']
        assert hashlib.sha256((pathlib.Path(receipt['extracted'])/f['path']).read_bytes()).hexdigest()==f['sha256']
assert any(pathlib.Path(x['executable']).resolve()==pathlib.Path(receipt['exe']).resolve() for x in finite), 'No actual extracted EXE run'
report=(d/'RELEASE_QA.md').read_text(encoding='utf-8')
assert 'IN_PROGRESS' not in report and 'NOT_RUN_PENDING' not in report, 'Report not finalized'
result={'status':'EVIDENCE_GATE_PASS_WITH_TWO_RECHECKED_EDITOR_FIXTURES','gameSource':source,
        'editorTestRevision':recheck['source'],'editorOnlyChangedFiles':sorted(changed),
        'native':[{k:x[k] for k in ('phase','exitCode','nunit')} for x in final],
        'targetedRecheck':{k:recheck[k] for k in ('phase','exitCode','nunit')},
        'finiteWindowsRuns':len(finite),'zipSHA256':receipt['zipSHA256'],'zipBytes':receipt['zipBytes'],
        'note':'Initial unfiltered Edit cycle remains1066/1068 with2FAIL. Those2Editor fixtures were corrected and rerun2/2, with production unchanged; no second full Edit run is claimed. Does not assert human UI, G1/G2, blocked package admission or a shutdown fix.'}
(d/'final-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result,indent=2))
