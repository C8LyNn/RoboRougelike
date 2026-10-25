"""Read-only exact CAMERA R1.1 Git/manifest/GUID receipt; no gate approval."""
import hashlib,json,re,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[2];pin='d48108f56d3fe0790ad715683e77b2de9219e4b1'
def git(*args):return subprocess.check_output(['git',*args],cwd=root)
manifest=json.loads((root/'Docs/B01/CAMERA001/manifest.json').read_text())
records=[];manifest_errors=[]
for path,entry in manifest['files'].items():
 data=(root/path).read_bytes();source=git('show',pin+':'+path)
 assert data==source,path
 blob=hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
 if blob!=entry['git_blob_sha1'] or len(data)!=entry['bytes']:manifest_errors.append(path)
 records.append({'path':path,'sha256':hashlib.sha256(data).hexdigest(),'gitBlobSha1':blob,'bytes':len(data)})
paths=git('ls-tree','-r','--name-only',pin,'--','Assets/B01Packages.meta','Assets/B01Packages/CAMERA001.meta','Assets/B01Packages/CAMERA001','Docs/B01/CAMERA001','Tools/B01/CAMERA001').decode().splitlines()
for path in paths:assert (root/path).read_bytes()==git('show',pin+':'+path),path
seen={};collisions=[];bad=[]
for p in (root/'Assets').rglob('*.meta'):
 m=re.search(r'(?m)^guid: ([0-9a-f]{32})\s*$',p.read_text(encoding='utf-8-sig'))
 if not m:bad.append(str(p.relative_to(root)));continue
 if m[1] in seen:collisions.append([seen[m[1]],str(p.relative_to(root))])
 seen[m[1]]=str(p.relative_to(root))
out=root/'Docs/CAMERA001R11NativeQA';out.mkdir(exist_ok=True)
result={'source':pin,'target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','entireFilesMatched':len(paths),'manifestEntries':len(records),'manifestErrors':manifest_errors,'records':records,'guidCollisions':collisions,'invalidMetas':bad,'note':'Producer SHA256 fields null; verified declared Git blob SHA1+bytes and computed independent SHA256. Not B01-60/G2.'}
(out/'source-integrity.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['records','note']}))
assert not manifest_errors and not collisions and not bad
