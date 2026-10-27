"""Verify preservation from PR277; source intake deviations stay explicit."""
import hashlib,json,pathlib,re,subprocess
r=pathlib.Path(__file__).resolve().parents[2]
base='0c7d0ba3d6b5d8ba31f728c3fb650ea79414c4cd'
def git(*args):return subprocess.check_output(['git','-C',str(r),*args])
def sha(data):return hashlib.sha256(data).hexdigest()
guid=re.compile(rb'^guid: ([a-f0-9]{32})',re.M);guids={};missing=[]
for p in (r/'Assets').rglob('*'):
    if p.name.endswith('.meta'):
        m=guid.search(p.read_bytes())
        if m:guids.setdefault(m.group(1).decode(),[]).append(p.relative_to(r).as_posix())
    elif not pathlib.Path(str(p)+'.meta').is_file():missing.append(p.relative_to(r).as_posix())
changes=git('diff','--name-status',base,'--','Assets','Packages','ProjectSettings').decode().splitlines()
deleted=[line for line in changes if line.startswith('D\t')]
modified_meta=[line for line in changes if line.startswith('M\t') and line.endswith('.meta')]
duplicates={key:paths for key,paths in guids.items() if len(paths)>1}
preserved={name:(r/name).read_bytes()==git('show',base+':'+name) for name in
    ['Assets/B01Packages.meta','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt']}
modified=[line.split('\t',1)[1] for line in changes if line.startswith('M\t')]
receipts=[]
for name in modified:
    receipts.append(dict(path=name,baseSHA256=sha(git('show',base+':'+name)),currentSHA256=sha((r/name).read_bytes())))
result=dict(source=git('rev-parse','HEAD').decode().strip(),baseline=base,GUIDs=len(guids),missingMeta=missing,
    duplicateGUIDs=duplicates,deletedAssets=deleted,modifiedExistingMeta=modified_meta,preserved=preserved,modifiedFiles=receipts)
assert not missing and not duplicates and not deleted and not modified_meta and all(preserved.values()),result
out=r/'Docs/Camera275';out.mkdir(parents=True,exist_ok=True)
(out/'integrity.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(status='PASS',source=result['source'],GUIDs=len(guids),modified=len(receipts),preserved=preserved),indent=2))
