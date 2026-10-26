import hashlib, io, json, pathlib, re, subprocess, zipfile
r=pathlib.Path(__file__).resolve().parents[2]
pin='48581e49a878eca376674551017532bf17fd35b2'
base='4c9236a4cdc27814ea2e7679872ec4a524150cc8'
def git(*a):return subprocess.check_output(['git','-C',str(r),*a])
m=json.loads((r/'Docs/B01/CAMERA001/manifest.json').read_text())
outer=zipfile.ZipFile(r/'Artifacts/Intake/CAMERA_R12_source_artifact.zip')
receipt=json.loads(outer.read('SHA256_REPORT.json'))
b=outer.read('CAMERA001_R1_2_FULL_SOURCE_QA.zip')
assert hashlib.sha256(b).hexdigest()==receipt['sha256']=='d00e3a3f4aea9ce15a8ecc4d95838fcc5a42329060568da63d0e2a72817cbd9d'
assert len(b)==receipt['bytes']==46544
z=zipfile.ZipFile(io.BytesIO(b));assert z.testzip() is None
assert len(z.namelist())==receipt['entries']==41
records=[]
for p in z.namelist():
 data=(r/p).read_bytes();assert data==z.read(p)==git('show',pin+':'+p),p
 if p.endswith('manifest.json'):continue
 h=hashlib.sha256(data).hexdigest()
 assert h==m['r12_source_sha256_by_path'][p],p
 assert len(data)==m['r12_source_bytes_by_path'][p],p
 records.append({'path':p,'sha256':h,'bytes':len(data)})
assert len(records)==40
seen={};collisions=[];invalid=[];missing=[]
for p in (r/'Assets').rglob('*.meta'):
 match=re.search(r'(?m)^guid: ([0-9a-f]{32})\s*$',p.read_text(encoding='utf-8-sig'))
 if not match:invalid.append(str(p.relative_to(r)));continue
 if match[1] in seen:collisions.append([seen[match[1]],str(p.relative_to(r))])
 seen[match[1]]=str(p.relative_to(r))
for p in (r/'Assets/B01Packages/CAMERA001').rglob('*'):
 if p.suffix in ('.cs','.asmdef','.unity') and not pathlib.Path(str(p)+'.meta').exists():missing.append(str(p))
for parent in ['d48108f56d3fe0790ad715683e77b2de9219e4b1']:
 subprocess.run(['git','-C',str(r),'merge-base','--is-ancestor',parent,pin],check=True)
assert not collisions and not invalid and not missing
assert not git('diff',base,'--','Packages','ProjectSettings','Assets/R1','Assets/R3','Assets/R6','Assets/Pack')
out=r/'Docs/CAMERA001R12NativeQA';out.mkdir(exist_ok=True)
result={'source':pin,'gameBase':base,'archive':receipt,'fullFiles':41,'manifestFiles':40,'records':records,'GUIDcollisions':collisions,'invalidMeta':invalid,'missingMeta':missing,'sharedParent':'Target absent; original CAMERA parent imported. No existing GUID overwritten.','status':'PASS'}
(out/'source-integrity.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k!='records'}))
