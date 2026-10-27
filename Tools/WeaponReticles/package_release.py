"""Package and byte-verify the actual Unity Windows build without launching it."""
import hashlib,json,pathlib,zipfile
r=pathlib.Path(__file__).resolve().parents[2];build=r/'Builds/FULL-MERGE270'
receipt=json.loads((r/'Artifacts/Local/FM270-RETICLE-Release-20261027T135818Z/Build-process.json').read_text(encoding='utf-8-sig'))
source='6cfdb55a20ac6d40c39cc2bcca69f74ec0232cfc'
assert receipt['source']==source and receipt['exitCode']==0,receipt
release=r.parent/'Releases/ROBOROUGELIKE_WEAPON_RETICLES_FINAL_20261027';release.mkdir(parents=True,exist_ok=True)
archive=release/'RoboRougelike_WeaponReticles_Windows_x64.zip';extracted=release/'RoboRougelike_WeaponReticles_Windows_x64'
assert not archive.exists() and not extracted.exists(),'Refusing to overwrite an existing release'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
files=sorted(p for p in build.rglob('*') if p.is_file())
assert (build/'RoboRougelike.exe').is_file() and (build/'UnityPlayer.dll').is_file() and (build/'RoboRougelike_Data/Managed/Assembly-CSharp.dll').is_file()
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in files:z.write(p,p.relative_to(build).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None;z.extractall(extracted)
entries=[]
for p in files:
    target=extracted/p.relative_to(build);assert target.is_file() and sha(p)==sha(target)
    entries.append(dict(path=p.relative_to(build).as_posix(),bytes=p.stat().st_size,sha256=sha(p)))
result=dict(source=source,archive=str(archive),archiveBytes=archive.stat().st_size,sha256=sha(archive),
    executable=str(extracted/'RoboRougelike.exe'),assemblySHA256=sha(extracted/'RoboRougelike_Data/Managed/Assembly-CSharp.dll'),
    files=entries,status='ARCHIVE_AND_EXTRACTED_BYTES_VERIFIED',windowsPlayer='NOT_RUN — user requested no PC or mouse control')
out=r/'Docs/WeaponReticles';out.mkdir(parents=True,exist_ok=True)
(out/'delivery.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
(release/'SHA256.txt').write_text(result['sha256']+'  '+archive.name+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='files'},indent=2))
