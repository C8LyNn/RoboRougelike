"""Stage the exact B01-60 #245 recorder and prepare a pinned, isolated comparison."""
import hashlib
import importlib.util
import json
import pathlib
import subprocess

root=pathlib.Path(__file__).resolve().parents[2]
tool_sha='aa5eedf2c1406b04ad9a496eed82242521d1325a'
relative='Tools/B01/QA-NATIVE-001/rc1_shutdown_ab.py'
staged=root/'Tools/Exit273/Staged';staged.mkdir(exist_ok=True)
raw=subprocess.check_output(['git','-C',str(root),'show',tool_sha+':'+relative])
tool=staged/'rc1_shutdown_ab.py';tool.write_bytes(raw)
(staged/'provenance.json').write_text(json.dumps(dict(source=tool_sha,path=relative,sha256=hashlib.sha256(raw).hexdigest()),indent=2))
spec=importlib.util.spec_from_file_location('b01_shutdown',tool)
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
releases=root.parent/'Releases'
base=releases/'ROBOROUGELIKE_FULL_INTEGRATION_270_20261027/RoboRougelike_FULL_INTEGRATION_270_Windows_x64.zip'
patched=releases/'ROBOROUGELIKE_EXIT273_20261027/RoboRougelike_EXIT273_Windows_x64.zip'
rc2=releases/'B01_UNIFIED_RC2_20261026/RoboRougelike_B01_UNIFIED_RC2_Windows_x64.zip'
builds={}
for label,path,sha in [('BASE',base,'88b8eed60345c49481648833e1fde6dc1a400258'),('EXP',patched,'2e4bff3dc49dd578422af136992040806ccbe9fc'),('RC2',rc2,'46b42f62d591ad4aeb9dbfdeda9997044c6b5e1f')]:
    builds[label]=dict(archive=str(path),sha256=module.digest(path),source_sha=sha,exe='RoboRougelike.exe')
plan=dict(schema=module.SCHEMA,unity_version='6000.3.14f1',builds=builds,order=['BASE','EXP','EXP','BASE','RC2','RC2'],manual_instructions='Manual operator: compare native Alt+F4 and X on BASE (#272) and EXP (#273); menu Quit is additionally tested on EXP. Historical RC2 is optional, not silently declared executed.')
plan_path=root/'Artifacts/Local/EXIT273-AB-plan.json';plan_path.write_text(json.dumps(plan,indent=2))
print(json.dumps(module.prepare(plan_path,root/'Artifacts/Local/EXIT273-AB'),indent=2))
