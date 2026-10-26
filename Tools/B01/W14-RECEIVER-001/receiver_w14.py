#!/usr/bin/env python3
"""B01-40 W14 original-byte G0. No live game writes, no Unity-native inference."""
import argparse, hashlib, io, json, re, stat, zipfile
from pathlib import Path, PurePosixPath

class GateError(ValueError):
    pass

PINS = {
 'BUILD_R14': {'outer':'B01_BUILD_R14_actions.zip','inner':'B01-BUILD001-R14.zip','sha':'62321794e8f91f1660c32597632b40b9bccdbab1becd3c7e750632f06f8714aa','bytes':155320,'count':60,'source':'95707c68c8071a949b5d9fec7ff57926e92f8198','root':'Assets/B01Construction/BUILD001','meta':'Assets/B01Construction/BUILD001.meta','manifest':'PACKAGE_MANIFEST_R13.json','records':59},
 'WORLD_R11': {'outer':'B01_WORLD_R11_actions.zip','inner':'mapgenworld-evidence/package/MAPGENWORLD001_SOURCE_FULL.zip','sha':'a27891c3b9420a3060190c613104121154ef1385c6d15354d2d68aa22f22802a','bytes':46467,'count':40,'source':'5ce70920478043210edbd78713f746b8d8ff431d','root':'Assets/B01Packages/MAPGENWORLD001','meta':'Assets/B01Packages/MAPGENWORLD001.meta','manifest':'META-INF/MAPGENWORLD001_MANIFEST.json','records':39},
 'CAMERA_R122': {'outer':'B01_CAMERA_R122_actions.zip','inner':'CAMERA001_R1_2_2_FULL_SOURCE_QA.zip','sha':'ecf2211e317a35af7a726afe1b484e83e1b7a326faeb5aed3057fe6153b338ee','bytes':76032,'count':60,'source':'063e755c50c9728f0a9627d25d3677280625a73d','root':'Assets/B01Packages/CAMERA001','meta':'Assets/B01Packages/CAMERA001.meta','manifest':'Docs/B01/CAMERA001/manifest.json','records':59},
 'ECONLEG_GUARD': {'outer':'B01_ECONLEG_GUARD_actions.zip','inner':'ECONLEGENDARY-GUARD-001.zip','sha':'7f826d0148d0dacf612b256a21e32fe3eaaf341651c600ca742dd36e5fdcf09c','bytes':7908,'count':13,'source':'01ee22a3a2dbd0c2ced3140032042894f88d2ebb','root':'Assets/B01Packages/ECONLEGENDARY001','meta':'Assets/B01Packages/ECONLEGENDARY001.meta','manifest':None,'records':13}
}
SHA_RE=re.compile(r'^[0-9a-f]{64}$'); GUID_RE=re.compile(rb'(?m)^guid: ([0-9a-fA-F]{32})\s*$')
EXTS=('.cs','.asmdef','.shader','.unity','.prefab','.asset','.mat','.controller','.anim','.uxml','.uss')

def digest(v):return hashlib.sha256(v).hexdigest()
def blob(v):return hashlib.sha1(b'blob '+str(len(v)).encode()+b'\0'+v).hexdigest()
def safe_path(p):
    if not isinstance(p,str) or p != PurePosixPath(p).as_posix() or any(s in ('','.','..') for s in p.split('/')) or p.startswith('/') or any(c in p for c in ('\\',':','\0')) or p.endswith('/'):
        raise GateError(f'UNSAFE_PATH:{p!r}')
    if any(c in set('<>"|?*') for c in p):raise GateError('WINDOWS_RESERVED:'+p)
    for s in p.split('/'):
        if s.rstrip(' .')!=s or s.split('.')[0].upper() in {'CON','PRN','AUX','NUL',*(f'COM{i}' for i in range(1,10)),*(f'LPT{i}' for i in range(1,10))}:
            raise GateError('WINDOWS_NAME:'+p)
    return p

def read_zip(blob_bytes):
    try:
        with zipfile.ZipFile(io.BytesIO(blob_bytes)) as z:
            infos=z.infolist(); names=[safe_path(i.filename) for i in infos]
            if len(set(names)) !=len(names) or len(set(p.casefold() for p in names)) !=len(names):raise GateError('DUPLICATE_PATH_CASEFOLD')
            if any(stat.S_IFMT(i.external_attr>>16)==stat.S_IFLNK for i in infos):raise GateError('SYMLINK')
            if any(i.file_size>8_000_000 for i in infos) or sum(i.file_size for i in infos)>60_000_000:raise GateError('ZIP_BOMB_LIMIT')
            if z.testzip() is not None:raise GateError('CRC_FAIL')
            return {i.filename:z.read(i.filename) for i in infos}
    except (OSError,zipfile.BadZipFile,EOFError,zlib_error()) as e:
        raise GateError('ZIP_INVALID:'+str(e)) from e

def zlib_error():
    import zlib
    return zlib.error

def records_for(kind,contents,outer):
    pin=PINS[kind]
    if kind=='ECONLEG_GUARD':
        try:m=json.loads(outer['source-manifest.json'])
        except (KeyError,ValueError) as e:raise GateError('EXTERNAL_MANIFEST_REQUIRED') from e
        if m.get('producer_head')!=pin['source']:raise GateError('SOURCE_SHA_MISMATCH')
        try:bridge_receipt=json.loads(outer['receiver-bridge-receipt.json'])
        except (ValueError,KeyError) as e:raise GateError('RECEIVER_BRIDGE_RECEIPT_MISSING') from e
        require_guard_bridge_receipt(bridge_receipt)
        entries={x['path']:(x['sha256'],x['bytes'],None) for x in m['files'] if x['path'] in contents}
        if len(entries)!=pin['records']:raise GateError('MANIFEST_SCOPE')
        return entries
    try:m=json.loads(contents[pin['manifest']])
    except (KeyError,ValueError) as e:raise GateError('INNER_MANIFEST_REQUIRED') from e
    if kind=='BUILD_R14':
        if m.get('source_head_sha')!=pin['source']:raise GateError('SOURCE_SHA_MISMATCH')
        return {r['path']:(r['sha256'],r['bytes'],None) for r in m['files']}
    if kind=='WORLD_R11':
        if m.get('version') != '1.1.0': raise GateError('BAD_WORLD_VERSION')
        return {r['path']:(r['sha256'],r['size'],None) for r in m['files']}
    if kind=='CAMERA_R122':
        sha=m['r122_source_sha256_by_path']; sizes=m['r122_source_bytes_by_path']
        if set(sha)!=set(sizes):raise GateError('MANIFEST_LENGTH_SCOPE')
        return {p:(h,sizes[p],m['files'][p].get('git_blob_sha1')) for p,h in sha.items()}
    raise GateError('UNKNOWN_KIND')

def verify(kind,outer_bytes):
    p=PINS[kind]
    outer=read_zip(outer_bytes)
    if p['inner'] not in outer:raise GateError('INNER_ZIP_MISSING')
    inner_bytes=outer[p['inner']]
    if digest(inner_bytes)!=p['sha'] or len(inner_bytes)!=p['bytes']:raise GateError('INNER_ZIP_SHA_OR_SIZE')
    contents=read_zip(inner_bytes)
    if len(contents)!=p['count']:raise GateError('INNER_ENTRY_COUNT')
    records=records_for(kind,contents,outer)
    if len(records)!=p['records'] or set(contents) != set(records) | ({p['manifest']} if p['manifest'] else set()):raise GateError('MANIFEST_SCOPE')
    for path,(sha,size,gitblob) in records.items():
        if path not in contents or not SHA_RE.fullmatch(sha):raise GateError('MISSING_OR_INVALID_SHA:'+path)
        d=contents[path]
        if digest(d)!=sha or len(d)!=size:raise GateError('FILE_SHA_OR_SIZE:'+path)
        if gitblob is not None and blob(d)!=gitblob:raise GateError('BLOB_MISMATCH:'+path)
    assets={k:v for k,v in contents.items() if k.startswith('Assets/')}
    root=p['root']; owner=p['meta']
    for path in assets:
        if not (path==owner or path.startswith(root+'/') or path in ('Assets/B01Packages.meta','Assets/B01Construction.meta')):raise GateError('OUT_OF_SCOPE:'+path)
    # Each file and each nested Unity directory must have its own meta file; the shared parent may live in existing target.
    meta_paths=set(x for x in assets if x.endswith('.meta'))
    for path in assets:
        if path.endswith('.meta'):continue
        if path.endswith(EXTS) and path+'.meta' not in meta_paths:raise GateError('MISSING_FILE_META:'+path)
        parts=path.split('/')
        for index in range(2,len(parts)):
            folder='/'.join(parts[:index])+'.meta'
            if folder not in meta_paths and folder not in ('Assets/B01Packages.meta',):raise GateError('MISSING_DIRECTORY_META:'+folder)
    # Do not accept unparseable or duplicate GUID metadata.
    guid_to_path={}; asmnames={}; asmrefs={}
    for path in sorted(meta_paths):
        v=GUID_RE.search(assets[path]);
        if not v:raise GateError('MISSING_GUID:'+path)
        guid=v.group(1).decode().lower()
        if guid in guid_to_path:raise GateError('GUID_COLLISION:'+path+' vs '+guid_to_path[guid])
        guid_to_path[guid]=path
    if kind=='ECONLEG_GUARD':
        require_guard_owner_identity(assets)
    for path,d in assets.items():
        if path.endswith('.asmdef'):
            try:a=json.loads(d)
            except ValueError as e:raise GateError('BAD_ASMDEF:'+path) from e
            name=a.get('name')
            if not name or name in asmnames:raise GateError('BAD_OR_DUP_ASMDEF:'+path)
            asmnames[name]=path;asmrefs[name]=a.get('references',[])
    return {'package':kind,'source':p['source'],'zip_sha256':digest(inner_bytes),'zip_bytes':len(inner_bytes),'zip_entries':len(contents),'file_sha256_pass':len(records),'guid_count':len(guid_to_path),'asmdefs':asmrefs,'status':'G0_SOURCE_PASS_NOT_NATIVE'},contents,guid_to_path

def require_guard_owner_identity(assets):
    """W15 P0: fail closed on a stale permissive cross-R3 ownership guard.

    This only verifies actual pinned source text. The separate native C# behavior
    and receiver adapters remain NOT_RUN / NOT_PROVEN by this static gate.
    """
    path='Assets/B01Packages/ECONLEGENDARY001/Runtime/GuardedLegendaryPort.cs'
    blob=assets.get(path)
    if blob is None:raise GateError('GUARD_SOURCE_MISSING')
    source=blob.decode('utf-8')
    required=(
        'interface ISharedR3PhysicalOwnerIdentity',
        'object AuthoritativeR3Model { get; }',
        'inner as ISharedR3PhysicalOwnerIdentity',
        'economyHost as ISharedR3PhysicalOwnerIdentity',
        'OWNER_IDENTITY_UNAVAILABLE',
        'OWNER_IDENTITY_MISMATCH',
        'object.ReferenceEquals(legendOwner.AuthoritativeR3Model,econOwner.AuthoritativeR3Model)',
        'if(!CheckSharedOwnership(out reason))return false;',
    )
    for snippet in required:
        if source.count(snippet)!=1:
            raise GateError('OWNER_IDENTITY_CONTRACT_ABSENT_OR_DUPLICATED:'+snippet)
    return 'STATIC_OWNER_REFERENCE_EQUALITY_PRESENT_NOT_NATIVE_PROOF'

def require_guard_bridge_receipt(receipt):
    """Enforce the W15 bridge receipt contract; does not verify generated bridge
    source bytes, which are not shipped in this Unity-only source artifact.
    """
    if (receipt.get('purpose')!='COMPILE_ONLY_RECEIVER_REFERENCE_NOT_INSTALLED' or
        receipt.get('upstream_legend')!='dffabbc6960304fdb33322b5a23c884feac804c6' or
        receipt.get('upstream_econ')!='12572117a3dce369257feabc57021cde58023369' or
        receipt.get('native_unity')!='NOT_RUN' or receipt.get('g2')!='HOLD'):
        raise GateError('RECEIVER_BRIDGE_STATUS_OR_UPSTREAM_MISMATCH')
    expect={
      'R3LegendaryPort.install.cs':('d1ea529d2acc657bc51039d2022b4839ab6fb19a25ba799630030b08d2803981','5bb862dd082c5ad1ab237fadde086985dbb2a829da3e889db9b9234f4d7dce02'),
      'R3EconRebuildPort.install.cs':('225db15dd242516e21cf38d794dbbb069f4d5512f8257e82101c138b7f488027','6a852fe94a2627f41d07291a657b9db36967cc7ee88bca5fcee10932a43b4932')}
    seen=set()
    for entry in receipt.get('files',[]):
        name=entry.get('file')
        if name not in expect or name in seen:raise GateError('RECEIVER_BRIDGE_FILE_SCOPE')
        seen.add(name)
        if (entry.get('upstream_sha256'),entry.get('receiver_sha256'))!=expect[name] or entry.get('changed_lines')!=2:
            raise GateError('RECEIVER_BRIDGE_SHA_RECEIPT:'+name)
    if seen!=set(expect):raise GateError('RECEIVER_BRIDGE_INCOMPLETE')
    return 'BRIDGE_RECEIPT_PINNED_ONLY_NO_GENERATED_BYTES'

def cross_check(results):
    paths={};guids={};duplicate=[];incompat=[];missing_references=[]; asmnames=set()
    for kind,(receipt,contents,guidmap) in results.items():
        for name in receipt['asmdefs']: asmnames.add(name)
        for path,d in contents.items():
            if not path.startswith('Assets/'):continue
            if path in paths and paths[path][1]!=d:
                if path in ('Assets/B01Packages.meta','Assets/B01Construction.meta'): duplicate.append({'path':path,'resolution':'PRESERVE_TARGET_PARENT_META_ONLY'})
                else:incompat.append({'path':path,'first':paths[path][0],'second':kind})
            paths.setdefault(path,(kind,d))
        for guid,path in guidmap.items():
            if guid in guids and guids[guid]!=path:incompat.append({'guid':guid,'first':guids[guid],'second':path})
            guids.setdefault(guid,path)
    for kind,(receipt,_,_) in results.items():
        for src, refs in receipt['asmdefs'].items():
            for ref in refs:
                if not ref.startswith('GUID:') and ref not in asmnames:missing_references.append({'package':kind,'assembly':src,'external':ref})
    if incompat:raise GateError('CROSS_PACKAGE_COLLISION:'+str(incompat[:3]))
    return {'source_collisions':len(incompat),'preserve_only_parent_meta':duplicate,'external_assembly_refs':missing_references,
        'asset_unique_paths':len(paths),'unique_guids':len(guids),'G0_TARGET':'NOT_RUN_LIVE','G2':'HOLD'}

def main():
    a=argparse.ArgumentParser();a.add_argument('--artifacts-dir',type=Path,required=True);a.add_argument('--report',type=Path);args=a.parse_args()
    result={}
    for name,p in PINS.items():
        path=args.artifacts_dir/p['outer']
        result[name]=verify(name,path.read_bytes())
    output={'packages':{name:v[0] for name,v in result.items()},'cross':cross_check(result),'G1_NATIVE':'NOT_RUN_BY_B01_40','G2':'HOLD'}
    if args.report:args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(output,indent=2)+'\n')
    print(json.dumps(output,indent=2));return 0
if __name__=='__main__':raise SystemExit(main())
