"""Collect measured BUILD native results; distinguish source tests, receiver regressions and demo observations."""
import hashlib,json,shutil,struct
import xml.etree.ElementTree as ET
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Docs/BUILD001R12NativeQA'
for label,pattern in {'Compile':'BUILD-R12-compile-*','NativeUnity':'BUILD-R12-native-*','DemoBuild':'BUILD-R12-demo-build-*','DemoPlayer':'BUILD-R12-demo-player-*'}.items():
 sources=list((root/'Artifacts/Local').glob(pattern));assert len(sources)==1,(label,sources)
 dest=out/label;dest.mkdir(parents=True,exist_ok=True)
 for f in sources[0].iterdir():
  if f.suffix not in ('.json','.xml','.log','.png'):continue
  if f.suffix=='.log':
   lines=f.read_text(encoding='utf-8-sig',errors='replace').splitlines()
   lines=['[connection identifier omitted]' if s.startswith('Player connection [') else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in lines]
   (dest/f.name).write_text('\n'.join(s.rstrip() for s in lines)+'\n',encoding='utf-8')
  else:shutil.copyfile(f,dest/f.name)
def read(p):return json.loads((out/p).read_text(encoding='utf-8-sig'))
native={};cases=[]
for mode in ('EditMode','PlayMode'):
 xml=ET.parse(out/'NativeUnity'/f'{mode}.xml').getroot()
 native[mode]={k:int(xml.get(k)) for k in ('total','passed','failed','skipped')}
 native[mode]['process']=read(f'NativeUnity/{mode}-process.json')
 for c in xml.iter('test-case'):
  cases.append({'mode':mode,'name':c.get('name'),'fullname':c.get('fullname'),'owner':'Receiver' if '.ReceiverQA.' in c.get('fullname','') else 'Producer','result':c.get('result'),'output':c.findtext('output') or '', 'failure':c.findtext('failure/message') or ''})
assert len(cases)==38 and sum(c['result']=='Passed' for c in cases)==34
assert sum(c['owner']=='Producer' for c in cases)==13 and all(c['result']=='Passed' for c in cases if c['owner']=='Producer')
bugs=[c for c in cases if 'BUILD_W9_' in c['name']];assert len(bugs)==4 and all(c['result']=='Failed' for c in bugs)
for c in bugs:assert 'attempt=1' in c['output'] and 'attempt=2' in c['output']
demo=read('DemoPlayer/demo-results.json');assert len(demo['rows'])==10
shots=[]
for p in sorted((out/'DemoPlayer').glob('*.png')):
 data=p.read_bytes();w,h=struct.unpack('>II',data[16:24]);assert (w,h)==((1280,720) if '1280' in p.name else (1920,1080))
 shots.append({'path':'DemoPlayer/'+p.name,'width':w,'height':h,'sha256':hashlib.sha256(data).hexdigest()})
assert len(shots)==10
summary={'producerSource':'489676414a3eb45fe70124098e3778a81d0aaff2','target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','compile':read('Compile/process.json'),'native':native,'NUnit':{'executed':38,'passed':34,'failed':4,'skipped':0,'producer':{'executed':13,'passed':13},'receiver':{'executed':25,'passed':21,'failed':4}},'cases':cases,'demoBuild':read('DemoBuild/process.json'),'demoPlayerProcess':read('DemoPlayer/process.json'),'demoLaunch':read('DemoPlayer/launch.json'),'demoObservations':{'count':10,'logicPassed':sum(r['logicStatus']=='PASS' for r in demo['rows']),'errorShader':sum(r['errorShader'] for r in demo['rows'])},'screenshots':shots,'realR3R6':'NOT_RUN: four required placement invariants fail; no integration adapter installed','overall':'FAIL / integration HOLD'}
(out/'native-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
lines=['# All actual NUnit results','', '| Mode | Origin | Case | Result |','|---|---|---|---|']
for c in cases:lines.append('| '+' | '.join([c['mode'],c['owner'],c['name'].replace('|','/'),c['result']])+' |')
(out/'NUNIT_CASES.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
lines=['# Native W9 reproductions','', 'Four failing NUnit cases; each collected two independent fixture observations before asserting the required rejection. These eight sub-observations are not eight additional NUnit cases.','']
for c in bugs:lines+=['## '+c['name'],'','```text',c['output'].strip(),'```','','```text',c['failure'].strip(),'```','']
(out/'W9_REPRODUCTIONS.md').write_text('\n'.join(lines).rstrip()+'\n',encoding='utf-8')
tree=ET.Element('native-demo-observations',format='Receiver observation XML, NOT NUnit results',source=summary['producerSource'],target=summary['target'],nativeExit=str(summary['demoPlayerProcess']['exitCode']))
for r in demo['rows']:ET.SubElement(tree,'observation',choice=str(r['choice']),width=str(r['width']),height=str(r['height']),logic=r['logicStatus'],actual=r['actual'],shader=r['shader'],errorShader=str(r['errorShader']))
ET.ElementTree(tree).write(out/'DemoPlayer/demo-observations.xml',encoding='utf-8',xml_declaration=True)
print(json.dumps({'NUnit':summary['NUnit'],'EditMode':{k:v for k,v in native['EditMode'].items() if k!='process'},'PlayMode':{k:v for k,v in native['PlayMode'].items() if k!='process'},'demo':summary['demoObservations'],'PNG':len(shots)}))
