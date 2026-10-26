"""Pinned full-source intake into this authorized disposable RC2 worktree only."""
import argparse,base64,hashlib,io,json,pathlib,re,subprocess,zipfile
R=pathlib.Path(__file__).resolve().parents[2]
P={
 'MAPGENWORLD':('5ce9d806d7fc7eb0ab2ef7b586626404c346174f','28b6422e7c358f379a47b49a0a671f631cee6d7f627daf3f6bf4807d7c3b8a41',['Assets/B01Packages/MAPGENWORLD001.meta','Assets/B01Packages/MAPGENWORLD001/','Docs/B01/MAPGENWORLD001/','Tools/B01/MAPGENWORLD001/']),
 'BUILD':('be54b4a33360cbd1923ae5dacbc92dff8eaf586b','bbd604f497539451bee3ed54094132fd830e68901d11ba6fa7c5943babbebc75',['Assets/B01Construction.meta','Assets/B01Construction/BUILD001.meta','Assets/B01Construction/BUILD001/','Docs/B01/BUILD001/','Tools/B01/BUILD001/']),
 'RAC':('fd9a3f4dda08eaba56d9f74007bc35dc0809b67e','78ec4592ee4144504d6fb259addbba8cc5fd68685d9fc057f603d6a9cfec1a7a',['Assets/RoboRougelikeMechanics.meta','Assets/RoboRougelikeMechanics/RAC001CheckpointRace.meta','Assets/RoboRougelikeMechanics/RAC001CheckpointRace/','Docs/ParallelMechanics/RAC-001/']),
 'KEEP3':('6bb6796ee3ad88a015fb6c8d033023c0ee3b5e94','27df176be1ca28a307dc1b61f92710b9f8b0d7e37b420c033cdb61d020266fe4',['Assets/B01Packages/REWARDKEEP3001.meta','Assets/B01Packages/REWARDKEEP3001/','Tools/B01/REWARDKEEP3001/']),
 'CAMERA':('52b5930a04a6e5d9f2fc28fa5b616036b61f0e44','b5d74b724d4479a39c8dd8f19414c50b62ad3418782c6f7d67dfeedcbb1e1099',['Assets/B01Packages/CAMERA001.meta','Assets/B01Packages/CAMERA001/','Docs/B01/CAMERA001/','Tools/B01/CAMERA001/']),
 'LEGENDARY':('dffabbc6960304fdb33322b5a23c884feac804c6','14eccb49f69b2e67ecb34008fe19689b1b66e629ed94491224fb327216fc1fb2',['Assets/B01Packages/LEGENDARY001.meta','Assets/B01Packages/LEGENDARY001/','Tools/B01/LEGENDARY-UNITY-001/']),
 'ECON':('12572117a3dce369257feabc57021cde58023369','fdcb15e6b394715f450771cac798b1c02b8efe1f33acb815e9e0957c46bd2d87',['Assets/B01Packages/ECONREBUILD001.meta','Assets/B01Packages/ECONREBUILD001/','Tools/B01/ECONREBUILD001/']),
}
SHARED={'Assets/B01Packages.meta','Assets/B01Construction.meta','Assets/RoboRougelikeMechanics.meta'}
def git(*args):return subprocess.check_output(['git','-C',str(R),*args])
def h(b):return hashlib.sha256(b).hexdigest()
def owned(p,roots):return any(p.startswith(x) if x.endswith('/') else p==x for x in roots)
def guid(data):
 m=re.search(rb'(?m)^guid: ([0-9a-f]{32})\s*$',data);assert m,'Invalid meta';return m[1].decode()
def run(key,apply=False):
 assert R.name=='RoboRougelike-B01-Unified-RC2' and git('branch','--show-current').decode().strip()=='integration/b01-unified-rc2'
 pin,digest,roots=P[key];allpaths=git('ls-tree','-r','--name-only',pin).decode().splitlines()
 paths=[p for p in allpaths if owned(p,roots)]
 assert paths and git('rev-parse',pin).decode().strip()==pin
 src={p:git('show',pin+':'+p) for p in paths}
 physical=R/'Artifacts/Intake'/(key+'-source-artifact.zip')
 wrapper=zipfile.ZipFile(physical);assert wrapper.testzip() is None
 candidates=[(n,wrapper.read(n)) for n in wrapper.namelist() if n.endswith('.zip') and not n.endswith('/')]
 nested=[(n,b) for n,b in candidates if h(b)==digest];assert len(nested)==1,(key,'No exact inner ZIP',[(n,h(b)) for n,b in candidates])
 name,b=nested[0];inner_bytes=len(b);z=zipfile.ZipFile(io.BytesIO(b));assert z.testzip() is None
 matches=[];generated=[]
 for n in z.namelist():
  if n.endswith('/'):continue
  q=pathlib.PurePosixPath(n);assert not q.is_absolute() and '..' not in q.parts and '\\' not in n and ':' not in n
  if n in allpaths:
   assert z.read(n)==git('show',pin+':'+n),(key,n,'archive/source mismatch');matches.append(n)
  else:
   assert n.lower().endswith(('.json','.txt')) and ('manifest' in n.lower() or 'sha256' in n.lower()),n
   generated.append(n)
 skipped=[]
 for p in SHARED:
  data=src.get(p,z.read(p) if p in z.namelist() else None)
  if data is not None and (R/p).exists():
   skipped.append({'path':p,'preservedSHA256':h((R/p).read_bytes()),'sourceSHA256':h(data)});src.pop(p,None)
 existing=git('ls-files').decode().splitlines();obsolete=[p for p in existing if owned(p,roots) and p not in paths and p not in SHARED]
 for p in list(src)+obsolete:
  path=R/p;assert path.resolve().is_relative_to(R.resolve()) and not path.is_symlink()
  if path.exists():
   assert path.read_bytes()==git('show','HEAD:'+p),(p,'local modification')
   if p.endswith('.meta') and p in src:assert guid(path.read_bytes())==guid(src[p]),(p,'GUID replacement blocked')
 prospective={p.relative_to(R).as_posix():p.read_bytes() for p in (R/'Assets').rglob('*.meta')}
 prospective.update({p:b for p,b in src.items() if p.endswith('.meta')})
 for p in obsolete:prospective.pop(p,None)
 seen={}
 for p,b in prospective.items():
  g=guid(b);assert g not in seen,(p,seen.get(g),'GUID collision');seen[g]=p
 for p in src:
  if p.startswith('Assets/') and pathlib.Path(p).suffix in ('.cs','.asmdef','.unity','.shader','.mat','.prefab'):
   assert p+'.meta' in src or (R/(p+'.meta')).exists(),(p,'missing meta')
 receipt={'package':key,'source':pin,'targetBefore':git('rev-parse','HEAD').decode().strip(),'zip':{'name':name,'sha256':digest,'bytes':inner_bytes,'entries':len(z.namelist()),'gitMatched':len(matches),'generatedManifests':generated},'preservedShared':skipped,'sourceFiles':len(paths),'GUIDCountAfter':len(seen),'obsolete':obsolete,'files':[{'path':p,'sha256':h(v),'bytes':len(v)} for p,v in src.items()],'status':'VERIFIED_NOT_STAGED'}
 if apply:
  before={p:base64.b64encode((R/p).read_bytes()).decode() if (R/p).exists() else None for p in list(src)+obsolete}
  journal=R/'Artifacts/Intake'/(key+'-stage-journal.json');journal.write_text(json.dumps({'before':before,'receipt':receipt},indent=2))
  touched=[]
  try:
   for p,v in src.items():
    f=R/p;f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes(v);touched.append(p)
   for p in obsolete:(R/p).unlink();touched.append(p)
  except BaseException:
   for p in reversed(touched):
    old=before[p]
    if old is None:(R/p).unlink(missing_ok=True)
    else:(R/p).write_bytes(base64.b64decode(old))
   raise
  for p,v in src.items():assert (R/p).read_bytes()==v
  receipt['status']='STAGED_FULL_PINNED_SOURCE'
 out=R/'Docs/B01_RC2/Intake'/(key+'-integrity.json');out.parent.mkdir(exist_ok=True,parents=True);out.write_text(json.dumps(receipt,indent=2)+'\n')
 print(json.dumps({k:v for k,v in receipt.items() if k!='files'}))
if __name__=='__main__':
 a=argparse.ArgumentParser();a.add_argument('package',choices=P);a.add_argument('--apply',action='store_true');args=a.parse_args();run(args.package,args.apply)
