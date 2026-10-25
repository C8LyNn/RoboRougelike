"""Collect one limited R3.3 native cycle; never run or modify producer code."""
import json,re,shutil
import xml.etree.ElementTree as ET
from pathlib import Path

root=Path(__file__).resolve().parents[2]
out=root/'Docs/MAPGEN001R33NativeQA'
runs={'Compile':'MAPGEN-R33-compile-*','OriginalUnity':'MAPGEN-R33-original-*','MaterialBuild':'MAPGEN-R33-material-build-*','MaterialPlayer':'MAPGEN-R33-material-player-*'}
for label,pattern in runs.items():
    sources=list((root/'Artifacts/Local').glob(pattern))
    assert len(sources)==1,(label,sources)
    dest=out/label;dest.mkdir(parents=True,exist_ok=True)
    for file in sources[0].iterdir():
        if file.suffix not in ('.json','.xml','.log','.png'):continue
        if file.suffix=='.log':
            lines=file.read_text(encoding='utf-8-sig',errors='replace').splitlines()
            lines=['[connection identifier omitted]' if line.startswith('Player connection [') else '  Machine Id: [omitted]' if line.lstrip().startswith('Machine Id:') else line for line in lines]
            (dest/file.name).write_text('\n'.join(line.rstrip() for line in lines)+'\n',encoding='utf-8')
        else:shutil.copyfile(file,dest/file.name)
def read(path):return json.loads((out/path).read_text(encoding='utf-8-sig'))
native={}
for mode in ('EditMode','PlayMode'):
    xml=ET.parse(out/'OriginalUnity'/f'{mode}.xml').getroot()
    native[mode]={k:int(xml.get(k)) for k in ('total','passed','failed','skipped')}
    native[mode]['process']=read(f'OriginalUnity/{mode}-process.json')
cube=[]
for case in ET.parse(out/'OriginalUnity/PlayMode.xml').iter('test-case'):
    if not case.get('name','').endswith('_CubePhysics'):continue
    message=(case.findtext('failure/message') or '').strip()
    cube.append({'name':case.get('name'),'result':case.get('result'),'message':message})
assert len(cube)==12 and len({c['name'] for c in cube})==12
lines=['# R3.3 independently executed cube cases','', '| Case | Result | Actual failure message |','|---|---|---|']
for case in cube:lines.append('| '+case['name']+' | '+case['result']+' | '+case['message'].split('\n')[0].replace('|','/')+' |')
(out/'CUBE_CASES.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
summary={'source':'7334f3b1d8cd3a976098faa4e683f4de3d9a592f','target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','compile':read('Compile/process.json'),'native':native,'cubeCases':cube,'materialBuild':read('MaterialBuild/process.json'),'materialPlayerProcess':read('MaterialPlayer/process.json'),'materialPlayer':read('MaterialPlayer/material-results.json'),'physicalR6':'NOT_RUN: original native cube blocker; limited task condition C','screenshots':'NOT_RUN: both Generate calls throw missing URP shader before capture','materialLifecycle':'NOT_RUN in Player: no generated map/material; raw default booleans are not measured PASS'}
(out/'native-summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'compile':summary['compile']['exitCode'],'EditMode':native['EditMode'],'PlayMode':native['PlayMode'],'cubeCases':len(cube),'cubeFailed':sum(c['result']=='Failed' for c in cube),'materialBuild':summary['materialBuild']['exitCode'],'materialPlayer':summary['materialPlayerProcess']['exitCode']}))
