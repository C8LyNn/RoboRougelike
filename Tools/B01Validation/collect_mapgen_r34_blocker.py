"""Preserve the actual R3.4 compiler failure; never invent NUnit/Player execution."""
import json,re,shutil
import xml.etree.ElementTree as ET
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Docs/MAPGEN001R34NativeQA'
runs=list((root/'Artifacts/Local').glob('MAPGEN-R34-compile-*'));assert len(runs)==1
src=runs[0];dest=out/'Compile';dest.mkdir(parents=True,exist_ok=True)
process=json.loads((src/'process.json').read_text(encoding='utf-8-sig'));assert process['exitCode']==1
raw=(src/'Editor.log').read_text(encoding='utf-8-sig',errors='replace')
assert "MapGenPlayModeTests.cs(37,56): error CS1061: 'Shader' does not contain a definition for 'GetTag'" in raw
lines=['[connection identifier omitted]' if s.startswith('Player connection [') else 'Machine Id: [omitted]' if s.lstrip().startswith('Machine Id:') else s for s in raw.splitlines()]
(dest/'Editor.log').write_text('\n'.join(s.rstrip() for s in lines)+'\n',encoding='utf-8');shutil.copyfile(src/'process.json',dest/'process.json')
assert not list((root/'Artifacts/Local').glob('MAPGEN-R34-original-*')),'Unexpected later test run'
cube_source=(root/'Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPhysicalTraversalTests.cs').read_text()
methods=re.findall(r'\[UnityTest\]\s+public\s+IEnumerator\s+(\w+)',cube_source)
cube=[{'name':n,'status':'NOT_RUN','reason':'Project compilation failed before NUnit execution'} for n in methods if n.endswith('_CubePhysics')]
assert len(cube)==12 and 'CubeForceActuatorMovesOnFlatControlSurface' in methods
vehicles=[{'recipe':recipe,'seed':seed,'rig':rig,'status':'NOT_RUN','initialFour':seed==7,'reason':'Phase A compilation blocker'} for seed in [7,31,83] for recipe in ['CombatArena','ObstacleCourse'] for rig in ['Light','Heavy']]
api_path=Path('C:/Program Files/Unity/Hub/Editor/6000.3.14f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.xml')
api=api_path.read_text(encoding='utf-8-sig')
proof={'installedEngineApi':str(api_path),'materialGetTagDeclared':'M:UnityEngine.Material.GetTag(System.String,System.Boolean)' in api,'shaderGetTagDeclared':'M:UnityEngine.Shader.GetTag(' in api,'officialReference':'https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Material.GetTag.html'}
assert proof['materialGetTagDeclared'] and not proof['shaderGetTagDeclared']
(out/'api-evidence.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
status={'producer':'0024731e1de0c0814968bd91d860a6ac9bb45385','target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','compile':{'result':'FAIL','process':process,'error':'CS1061','file':'Assets/B01Packages/MAPGEN001/Tests/PlayMode/MapGenPlayModeTests.cs','line':37,'column':56},'EditMode':'NOT_RUN','PlayMode':'NOT_RUN','cubeCases':cube,'flatControl':'NOT_RUN','WindowsBuild':'NOT_RUN','URPPlayer':'NOT_RUN','materialLifecycle':'NOT_RUN','screenshots':'NOT_RUN: no compiled R3.4 Player','realR6':vehicles,'nunitXmlProduced':False,'scope':'One original compilation attempt; no source repair, exclusion, runtime-only bypass or later native tests','overall':'FAIL / BLOCKED pending producer API repair'}
(out/'native-status.json').write_text(json.dumps(status,indent=2)+'\n',encoding='utf-8')
tree=ET.Element('qa-compilation-status',producer=status['producer'],target=status['target'],format='Receiver status record, NOT NUnit execution XML')
ET.SubElement(tree,'stage',id='Compile',result='FAIL',nativeExit='1',error='CS1061')
for name in ['EditMode','PlayMode','FlatControl','WindowsBuild','URPPlayer','MaterialLifecycle','RealR6']:ET.SubElement(tree,'stage',id=name,result='NOT_RUN',reason='Compile blocker')
for c in cube:ET.SubElement(tree,'cube-case',name=c['name'],result='NOT_RUN')
ET.ElementTree(tree).write(out/'native-status.xml',encoding='utf-8',xml_declaration=True)
table=['# R3.4 cube cases — not executed','', '| Authored case | Actual execution result | Reason |','|---|---|---|']
for c in cube:table.append('| '+c['name']+' | NOT_RUN | C# compiler failure before NUnit |')
table+=['','Flat control CubeForceActuatorMovesOnFlatControlSurface: NOT_RUN. No R3.4 positions, velocities, contacts, displacements or progress values were measured.']
(out/'CUBE_CASES.md').write_text('\n'.join(table)+'\n',encoding='utf-8')
print(json.dumps({'nativeCompileExit':process['exitCode'],'error':'CS1061 Shader.GetTag at37:56','cubeNotRun':len(cube),'laterNativeRuns':0,'apiProof':proof}))
