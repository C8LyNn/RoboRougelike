"""Read-only frozen source/GUID/host-scope check; no gameplay certification."""
import hashlib,json,pathlib,re,subprocess
root=pathlib.Path.cwd().resolve();out=root/'Artifacts/Local/RC1-source-integrity.json'
base='4c9236a4cdc27814ea2e7679872ec4a524150cc8';phy='916654634d1bfdc2bbc14e64b9eeac856ede1fa8'
packages={'MAPGEN001':'5be31039411f89f17654b1c332f8692883fa03cc','MAPGENWORLD001':'de1ce561d7e2ffac861c3e0fc76e5d722bdbb9f7'}
def git(*args):return subprocess.check_output(['git',*args],cwd=root)
def sha(data):return hashlib.sha256(data).hexdigest()
assert subprocess.run(['git','merge-base','--is-ancestor',phy,base],cwd=root).returncode==0
assert subprocess.run(['git','merge-base','--is-ancestor',packages['MAPGEN001'],packages['MAPGENWORLD001']],cwd=root).returncode==0
receipt={'gameSource':git('rev-parse','HEAD').decode().strip(),'base':base,'phy':phy,'packages':{},'nativeCertification':False}
for name,pin in packages.items():
    prefix='Assets/B01Packages/'+name
    files=git('ls-tree','-r','--name-only',pin,prefix,prefix+'.meta').decode().splitlines()
    rows=[]
    for rel in files:
        actual=(root/rel).read_bytes();original=git('show',pin+':'+rel)
        assert actual==original,rel
        rows.append({'path':rel,'sha256':sha(actual),'bytes':len(actual)})
    receipt['packages'][name]={'source':pin,'byteIdenticalFiles':len(rows),'files':rows}
parent='Assets/B01Packages.meta'
assert (root/parent).read_bytes()==git('show',packages['MAPGEN001']+':'+parent)
receipt['parentMetaSha256']=sha((root/parent).read_bytes())
guids={}
for file in (root/'Assets').rglob('*.meta'):
    m=re.search(rb'^guid: ([a-f0-9]{32})',file.read_bytes(),re.M)
    assert m is not None,str(file)
    key=m[1].decode();assert key not in guids,(key,guids.get(key),str(file));guids[key]=file.relative_to(root).as_posix()
receipt['totalMetaGuids']=len(guids);receipt['guidCollisions']=0
protected=['Assets/Pack','Assets/R3','Assets/R6','Assets/scene 1.unity','Packages','ProjectSettings']
assert not git('diff','--name-only',base,'--',*protected).strip(),'Forbidden authoritative source/settings change'
receipt['protectedPathsUnchanged']=protected
receipt['modifiedExistingGameFiles']=git('diff','--name-only','--diff-filter=M',base,'--','Assets').decode().splitlines()
assert set(receipt['modifiedExistingGameFiles'])=={'Assets/R1/R1RunController.cs','Assets/R1/R1RunHUD.cs','Assets/R1/R1EncounterManager.cs'}
receipt['result']='PASS_SOURCE_BYTES_GUID_AND_SCOPE_NOT_GAMEPLAY'
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps({'result':receipt['result'],'packageFiles':{n:r['byteIdenticalFiles'] for n,r in receipt['packages'].items()},'GUID':len(guids),'modifiedExisting':receipt['modifiedExistingGameFiles']}))
