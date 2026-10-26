"""One complete final ZIP, CRC and every extracted byte checked; no overwriting old RCs."""
import hashlib,json,pathlib,subprocess,zipfile
r=pathlib.Path(__file__).resolve().parents[2];build=r/'Builds/FULL-MERGE270'
receipts=sorted((r/'Artifacts/Local').glob('FM270-*/Build-process.json'),key=lambda p:p.stat().st_mtime)
receipt=json.loads(receipts[-1].read_text(encoding='utf-8-sig'));source=receipt['source'];assert receipt['exitCode']==0
assert not subprocess.check_output(['git','diff','--name-only',source,'--','Assets','Packages','ProjectSettings']).strip(),'Built source differs'
out=r.parent/'Releases'/'ROBOROUGELIKE_FULL_INTEGRATION_270_20261027';out.mkdir(parents=True,exist_ok=True)
name='RoboRougelike_FULL_INTEGRATION_270_Windows_x64';archive=out/(name+'.zip');extract=out/(name+'-VerifiedExtract')
assert not archive.exists() and not extract.exists(),'Refuse overwrite'
for name_required in ['RoboRougelike.exe','UnityPlayer.dll','RoboRougelike_Data','MonoBleedingEdge']:assert (build/name_required).exists()
readme=(r/'Docs/FullMerge270/START_HERE.txt').read_text(encoding='utf-8')+'\nGame source: '+source+'\n'
files=sorted(p for p in build.rglob('*') if p.is_file())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in files:z.write(p,p.relative_to(build).as_posix())
 z.writestr('START_HERE.txt',readme)
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 for i in z.infolist():assert (extract/i.filename).resolve().is_relative_to(extract.resolve())
 z.extractall(extract)
rows=[]
for p in files:
 rel=p.relative_to(build).as_posix();digest=sha(p);assert digest==sha(extract/rel),rel
 rows.append({'path':rel,'bytes':p.stat().st_size,'sha256':digest})
result={'source':source,'zip':str(archive),'zipBytes':archive.stat().st_size,'zipSHA256':sha(archive),'extracted':str(extract),'exe':str(extract/'RoboRougelike.exe'),'files':rows,'status':'CRC_AND_ALL_EXTRACTED_BYTES_PASS; actual launch separate'}
(out/'START_HERE.txt').write_text(readme,encoding='utf-8',newline='\n');(out/(name+'.sha256')).write_text(result['zipSHA256']+'  '+archive.name+'\n',encoding='utf-8',newline='\n')
(r/'Docs/FullMerge270/release-receipt.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({k:v for k,v in result.items() if k!='files'},indent=2))
