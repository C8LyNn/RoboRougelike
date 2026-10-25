"""Read-only immutable source, target GUID and open-PR path inventory; not a release gate."""
import hashlib,json,re,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[2];out=root/'Docs/B01Validation/20261025'
def git(*a,check=True):return subprocess.run(['git',*a],cwd=root,capture_output=True,check=check)
prs=json.loads((out/'open-prs.json').read_text(encoding='utf-8-sig'));inventory=[]
for p in prs:
 head='origin/'+p['headRefName'];base='origin/'+p['baseRefName'];mb=git('merge-base',base,head,check=False)
 paths=git('diff','--name-only',mb.stdout.decode().strip(),head).stdout.decode().splitlines() if mb.returncode==0 else None
 inventory.append({**p,'changedPaths':paths,'reservationEvidence':'branch diff, not an ownership approval'})
(out/'open-pr-paths.json').write_text(json.dumps(inventory,indent=2)+'\n',encoding='utf-8')
pin='894d3488dedb69e2b8d89683d750bbdbfedf4e55'
manifest=json.loads(git('show',pin+':Docs/B01/CAMERA001/manifest.json').stdout)
records=[]
for path,v in manifest['files'].items():
 data=git('show',pin+':'+path).stdout
 blob=hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
 assert len(data)==v['bytes'] and blob==v['git_blob_sha1'],path
 assert (root/path).read_bytes()==data,path
 records.append({'path':path,'bytes':len(data),'gitBlobSha1':blob,'sha256':hashlib.sha256(data).hexdigest(),'sourceAndCheckoutIdentical':True})
guids={};collisions=[]
for p in (root/'Assets').rglob('*.meta'):
 m=re.search(r'(?m)^guid: ([0-9a-f]{32})\s*$',p.read_text(encoding='utf-8-sig'))
 if not m:continue
 name=p.relative_to(root).as_posix()
 if m[1] in guids:collisions.append([guids[m[1]],name,m[1]])
 guids[m[1]]=name
summary={'source':pin,'target':'4c9236a4cdc27814ea2e7679872ec4a524150cc8','files':records,'guidCount':len(guids),'guidCollisions':collisions,'gate':'Local integrity audit only; independent B01-40/B01-60 and G2 remain pending'}
(out/'CAMERA001-source-audit.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
shared=('Assets/Pack/P12_PipelineSafeCamera.cs','Assets/R3/','Assets/R6/','Assets/scene 1.unity')
conflicts=[{'pr':p['number'],'head':p['headRefName'],'paths':[x for x in p['changedPaths'] or [] if any(x==s or x.startswith(s) for s in shared)]} for p in inventory]
conflicts=[p for p in conflicts if p['paths']]
print(json.dumps({'openPRs':len(inventory),'unresolvedBranchDiffs':[p['number'] for p in inventory if p['changedPaths'] is None],'sourceFilesVerified':len(records),'guidCollisions':collisions,'hostPathPRs':conflicts},indent=2))
