"""Package the actual complete Windows Player and verify every extracted byte."""
import argparse, hashlib, json, pathlib, zipfile, subprocess
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
p=argparse.ArgumentParser();p.add_argument('--build',required=True);p.add_argument('--output',required=True);p.add_argument('--variant',required=True);p.add_argument('--source',required=True);a=p.parse_args()
build=pathlib.Path(a.build).resolve();out=pathlib.Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True)
exe=build/'RoboRougelike.exe'
for necessary in [exe,build/'RoboRougelike_Data',build/'UnityPlayer.dll',build/'MonoBleedingEdge']:
    if not necessary.exists(): raise ValueError('Incomplete real Player: '+str(necessary))
name='RoboRougelike_B01_RC1_'+a.variant+'_Windows_x64'
archive=out/(name+'.zip');extract=out/(name+'-VerifiedExtract')
if archive.exists() or extract.exists(): raise ValueError('Never overwrite a release artifact')
files=sorted(p for p in build.rglob('*') if p.is_file())
readme=('RoboRougelike B01 / RC1 '+a.variant+'\nUnity 6000.3.14f1 / Windows x64\nSource snapshot: '+a.source+'\n\nExtract the whole ZIP, then run RoboRougelike.exe. Keep all DLLs and the Data/MonoBleedingEdge folders together.\nControls: WASD drive, mouse aim, LMB fire, Esc pause, Alt+F4 exit.\nMenu: select Light/Standard/Heavy; six-stage Gauntlet, garage/test range, or three-wave Reactor Defense.\n'+('Experimental MAPGEN: choose MAPGEN arena in the main menu, then MAPGEN arena in the preparation garage. Seed uses the existing menu field. Normal Gauntlet and Reactor Defense remain available. Only generated CombatArena is connected to gameplay; course remains a separate source package. This is isolated experimental QA, not G2 or a production release.\n' if a.variant=='MAPGEN_EXPERIMENTAL' else 'BASE contains the existing OBJ210/PHY125 game without imported MAPGEN/CAMERA/BUILD/RAC/LEGENDARY packages.\n'))
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for file in files:z.write(file,file.relative_to(build).as_posix())
    z.writestr('RC1_README.txt',readme)
with zipfile.ZipFile(archive) as z:
    if z.testzip() is not None:raise ValueError('ZIP CRC failure')
    extract.mkdir()
    for info in z.infolist():
        dest=(extract/info.filename).resolve()
        if not dest.is_relative_to(extract): raise ValueError('Unsafe ZIP')
    z.extractall(extract)
rows=[]
for file in files:
    rel=file.relative_to(build).as_posix();expected=sha(file);actual=sha(extract/rel)
    if expected!=actual: raise ValueError('Extracted byte mismatch: '+rel)
    rows.append({'path':rel,'bytes':file.stat().st_size,'sha256':expected})
receipt={'variant':a.variant,'source':a.source,'zip':str(archive),'zipBytes':archive.stat().st_size,'zipSha256':sha(archive),'extracted':str(extract),'exe':str(extract/'RoboRougelike.exe'),'exeSha256':sha(exe),'files':rows,'verification':'PASS CRC and every extracted file byte/hash; launch recorded separately'}
(out/(name+'.sha256')).write_text(receipt['zipSha256']+'  '+archive.name+'\n')
(out/(name+'-receipt.json')).write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps({k:v for k,v in receipt.items() if k!='files'},indent=2))
