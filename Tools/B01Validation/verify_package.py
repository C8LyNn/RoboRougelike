"""Verify exact immutable bytes and GUIDs without modifying source or gates."""
import argparse,hashlib,json,re,subprocess
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('package',choices=['BUILD001','RAC001']);p.add_argument('checkout',type=Path);a=p.parse_args()
spec={'BUILD001':('d575b79e262ec9b65702608d6b6e28972da0d21c','Docs/B01/BUILD001/manifest.json'),'RAC001':('6c1aa7c2351a29695e4288dddad2565cdec3682a','Docs/ParallelMechanics/RAC-001/manifest.r3.json')}
pin,manifest=spec[a.package];root=a.checkout.resolve()
def git(*args):return subprocess.check_output(['git',*args],cwd=root)
m=json.loads(git('show',pin+':'+manifest));records=[]
entries=m.get('files',m.get('blob_sha1_and_bytes',m.get('git_blob_sha1_and_bytes')))
if not isinstance(entries,dict):raise ValueError('Unrecognized manifest file map')
for path,v in entries.items():
 data=git('show',pin+':'+path);blob=hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
 assert blob==v.get('git_blob_sha1',v.get('blob_sha1')) and len(data)==v['bytes'],path
 assert (root/path).read_bytes()==data,path
 records.append({'path':path,'bytes':len(data),'gitBlobSha1':blob,'sha256':hashlib.sha256(data).hexdigest(),'sourceAndCheckoutIdentical':True})
seen={};collisions=[]
for path in (root/'Assets').rglob('*.meta'):
 match=re.search(r'(?m)^guid: ([0-9a-f]{32})\s*$',path.read_text(encoding='utf-8-sig'))
 if not match:continue
 name=path.relative_to(root).as_posix()
 if match[1] in seen:collisions.append([seen[match[1]],name])
 seen[match[1]]=name
result={'package':a.package,'source':pin,'target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','files':records,'guidCount':len(seen),'guidCollisions':collisions,'gate':'Receiver integrity only; not independent G1/G2 approval'}
out=Path(__file__).resolve().parents[2]/'Docs/B01Validation/20261025'/(a.package+'-source-audit.json');out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'package':a.package,'verifiedFiles':len(records),'guidCollisions':collisions}))
