"""Package the pinned native build, verify every extracted byte and SHA256."""
import hashlib,json,pathlib,shutil,zipfile
r=pathlib.Path(__file__).resolve().parents[2];build=r/'Builds/CAM275-Final'
receipt=json.loads((build/'receipt.json').read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(build/'RoboRougelike_Data/Managed/Assembly-CSharp.dll')==receipt['assemblySHA256']
release=r.parent/'Releases/ROBOROUGELIKE_CAMERA275_20261027';release.mkdir(parents=True,exist_ok=True)
archive=release/'RoboRougelike_CAMERA275_Windows_x64.zip';extracted=release/'RoboRougelike_CAMERA275_Windows_x64'
assert not archive.exists() and not extracted.exists(),'Refuse to overwrite an existing release'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in sorted(build.rglob('*')):
        if p.is_file():z.write(p,p.relative_to(build).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    z.extractall(extracted)
files=[]
for p in sorted(build.rglob('*')):
    if p.is_file():
        other=extracted/p.relative_to(build);assert other.is_file() and sha(p)==sha(other)
        files.append(dict(path=p.relative_to(build).as_posix(),bytes=p.stat().st_size,sha256=sha(p)))
result=dict(status='ARCHIVE_AND_EXTRACTION_PASS',source=receipt['source'],archive=str(archive),bytes=archive.stat().st_size,
    sha256=sha(archive),exe=str(extracted/'RoboRougelike.exe'),assemblySHA256=receipt['assemblySHA256'],files=files,
    qualification='Byte/hash validation only. Actual extracted Player launch is recorded separately, never inferred.')
out=r/'Docs/Camera275';out.mkdir(exist_ok=True,parents=True)
(out/'delivery-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
(release/'SHA256.txt').write_text(result['sha256']+'  '+archive.name+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='files'},indent=2))
