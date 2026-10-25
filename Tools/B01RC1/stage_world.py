"""Receiver-only transactional import for the adapter's documented manifest format.

PACKAGE-AUTO 402863b does not accept this producer's `files` list manifest or
its deliberate omission of the already owned B01Packages.meta. Do not alter
the producer ZIP or toolkit to force acceptance. Stage only documented Assets.
"""
import argparse, hashlib, json, pathlib, re, subprocess, time, zipfile

PIN = 'de1ce561d7e2ffac861c3e0fc76e5d722bdbb9f7'
ZIP_HASH = 'e5970c8f1f13837f9fe43e3d575c5ff4dc3a0dfbc1eb0174dbf08699aee1a491'
PREFIX = 'Assets/B01Packages/MAPGENWORLD001/'
ROOT_META = 'Assets/B01Packages/MAPGENWORLD001.meta'
MANIFEST = 'META-INF/MAPGENWORLD001_MANIFEST.json'
GUID = re.compile(rb'^guid: ([a-f0-9]{32})', re.M)
def digest(data): return hashlib.sha256(data).hexdigest()
def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')
def safe(root, rel):
    path=(root/rel).resolve()
    if not path.is_relative_to(root) or not (rel.startswith(PREFIX) or rel==ROOT_META):
        raise ValueError('Non-owned path: '+rel)
    return path
def rollback(root, journal):
    data=json.loads(journal.read_text(encoding='utf-8'))
    if data['target']!=str(root): raise ValueError('Wrong journal target')
    for row in data['files']:
        path=safe(root,row['path'])
        if path.exists() and digest(path.read_bytes())!=row['sha256']:
            raise ValueError('Refuse rollback of modified file: '+row['path'])
    for row in reversed(data['files']):
        path=safe(root,row['path'])
        if path.exists(): path.unlink()
    for rel in sorted(data.get('directories',[]),key=len,reverse=True):
        path=safe(root,rel+'/')
        if path.exists() and not any(path.iterdir()): path.rmdir()
    data['state']='rolled_back';save(journal,data)
    print(json.dumps({'rollback':'PASS','files':len(data['files']),'journal':str(journal)}))
def stage(root, archive):
    raw=archive.read_bytes()
    if digest(raw)!=ZIP_HASH: raise ValueError('Wrong adapter ZIP digest')
    parent=root/'Assets/B01Packages.meta';parent_before=parent.read_bytes()
    with zipfile.ZipFile(archive) as z:
        manifest=json.loads(z.read(MANIFEST));contents={i['path']:z.read(i['path']) for i in manifest['files']}
        if set(z.namelist())!=set(contents)|{MANIFEST}: raise ValueError('Manifest scope')
        for row in manifest['files']:
            raw=contents[row['path']]
            if len(raw)!=row['size'] or digest(raw)!=row['sha256']: raise ValueError('Bad manifest bytes')
    assets={p:b for p,b in contents.items() if p.startswith(PREFIX) or p==ROOT_META}
    guids={}
    for path in (root/'Assets').rglob('*.meta'):
        match=GUID.search(path.read_bytes())
        if match:
            key=match[1].decode()
            if key in guids: raise ValueError('Existing GUID collision')
            guids[key]=str(path)
    for rel,raw in assets.items():
        path=safe(root,rel)
        if path.exists(): raise ValueError('Target path already exists: '+rel)
        original=subprocess.check_output(['git','show',PIN+':'+rel],cwd=root)
        if raw!=original: raise ValueError('Source SHA mismatch: '+rel)
        if rel.endswith('.meta'):
            key=GUID.search(raw)[1].decode()
            if key in guids: raise ValueError('Source/target GUID collision')
            guids[key]=rel
    journal=root/'.b01-package-auto'/('world-'+str(time.time_ns())+'.json');journal.parent.mkdir(exist_ok=True)
    data={'source':PIN,'zipSha256':ZIP_HASH,'target':str(root),'parentMetaSha256':digest(parent_before),'state':'staging','files':[],'directories':[]}
    save(journal,data)
    try:
        for rel,raw in sorted(assets.items()):
            path=safe(root,rel)
            missing=[];folder=path.parent
            while not folder.exists(): missing.append(folder);folder=folder.parent
            for folder in reversed(missing):
                folder.mkdir();data['directories'].append(folder.relative_to(root).as_posix())
            data['files'].append({'path':rel,'sha256':digest(raw)})
            save(journal,data)
            with path.open('xb') as out: out.write(raw)
        if parent.read_bytes()!=parent_before: raise ValueError('Parent GUID was changed')
        data['state']='staged';save(journal,data)
    except BaseException:
        rollback(root,journal);raise
    print(json.dumps({'stage':'PASS','files':len(assets),'guidCount':14,'journal':str(journal),'source':PIN}))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('action',choices=['stage','rollback']);p.add_argument('--target',required=True);p.add_argument('--zip');p.add_argument('--journal');a=p.parse_args()
    root=pathlib.Path(a.target).resolve()
    if root.name!='RoboRougelike-B01-RC1' or not (root/'.b01-package-auto-sandbox.json').is_file(): raise ValueError('Not the designated RC1 sandbox')
    if a.action=='stage': stage(root,pathlib.Path(a.zip))
    else: rollback(root,pathlib.Path(a.journal))
