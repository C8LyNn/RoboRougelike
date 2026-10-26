"""Source ancestry/GUID preservation and exact imported-source deviations, no Unity mutation."""
import hashlib,json,pathlib,re,subprocess
r=pathlib.Path(__file__).resolve().parents[2];base='056911a8284f7d61f2a8d13ed2669e453c3d9915'
def git(*args):return subprocess.check_output(['git','-C',str(r),*args])
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
guid_re=re.compile(rb'^guid: ([a-f0-9]{32})',re.M);guids={};missing=[];changed_old=[];deleted=[]
for p in (r/'Assets').rglob('*'):
 if p.name.endswith('.meta'):
  m=guid_re.search(p.read_bytes())
  if m:guids.setdefault(m.group(1).decode(),[]).append(p.relative_to(r).as_posix())
 elif not pathlib.Path(str(p)+'.meta').is_file():missing.append(p.relative_to(r).as_posix())
old_paths=git('ls-tree','-r','--name-only',base,'--','Assets').decode().splitlines()
for name in old_paths:
 p=r/name
 if not p.exists():deleted.append(name);continue
 if name.endswith('.meta'):
  before=guid_re.search(git('show',base+':'+name));after=guid_re.search(p.read_bytes())
  if (before.group(1) if before else None)!=(after.group(1) if after else None):changed_old.append(name)
packages=[]
for name in ('BUILD','WORLD','CAMERA','RAC','MAPR6'):
 receipt=json.loads((r/f'Docs/FullMerge270/Intake/{name}-integrity.json').read_text(encoding='utf-8-sig'))
 rows=receipt.get('files',receipt.get('sourceFiles',[]));deviations=[];unavailable=[]
 if not isinstance(rows,list):raise ValueError(name+' missing file manifest')
 for row in rows:
  p=r/row['path']
  if not p.exists():unavailable.append(row['path'])
  elif sha(p)!=row['sha256']:deviations.append({'path':row['path'],'sourceSHA256':row['sha256'],'receiverSHA256':sha(p)})
 packages.append({'name':name,'source':receipt['source'],'manifestFiles':len(rows),'localPatches':deviations,'missing':unavailable})
result={'sha':git('rev-parse','HEAD').decode().strip(),'baseline':base,'originalAssetFiles':len(old_paths),'deletedOriginalAssets':deleted,'changedOriginalGUIDs':changed_old,
 'guidCount':len(guids),'duplicateGUIDs':{k:v for k,v in guids.items() if len(v)>1},'missingMeta':missing,
 'sharedParentPreserved':(r/'Assets/B01Packages.meta').read_bytes()==git('show',base+':Assets/B01Packages.meta'),'packages':packages}
assert not deleted and not changed_old and not result['duplicateGUIDs'] and not missing and result['sharedParentPreserved']
assert all(not x['missing'] for x in packages)
(r/'Docs/FullMerge270/final-integrity.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'status':'GUID_META_AND_BASELINE_PRESERVATION_PASS','source':result['sha'],'GUIDs':len(guids),'sourcePackages':[{'name':p['name'],'files':p['manifestFiles'],'localPatches':[x['path'] for x in p['localPatches']]} for p in packages]},indent=2))
