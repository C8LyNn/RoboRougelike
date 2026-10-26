#!/usr/bin/env python3
"""B01-40 read-only RC1 source/binary provenance and CAMERA R1.2 package receiver.

Python 3.10+ standard library, no Unity activation, no installation/merge/approval.
All inputs pinned; missing Windows binaries MUST remain PENDING, never PASS.
"""
from __future__ import annotations
import argparse
import base64
import hashlib
import io
import json
import os
from pathlib import Path
import re
import stat
import sys
import urllib.request
import zipfile
import zlib

REPO = 'C8LyNn/RoboRougelike'
RC1_REPORT = '5c29e8e28e7b34fedf0c992fca49451828b02314'
RC1_GAME = '86eb7bb77e6bfc7787f781ea1ccda547b8a4f643'
RC1_BASE = '4c9236a4cdc27814ea2e7679872ec4a524150cc8'
MAPGEN = '5be31039411f89f17654b1c332f8692883fa03cc'
WORLD = 'de1ce561d7e2ffac861c3e0fc76e5d722bdbb9f7'
CAMERA = '48581e49a878eca376674551017532bf17fd35b2'
CAMERA_QA = '08037f0b831381dfda111edc6b9810a1adfd2ffa'
CAMERA_SHA = 'd00e3a3f4aea9ce15a8ecc4d95838fcc5a42329060568da63d0e2a72817cbd9d'
CAMERA_LENGTH = 46544
BASE_ZIP = ('457ec2020375e18c4599d832dc028eed7900f8cf5d603765c9e417fd80a8ca90',38481635)
EXP_ZIP = ('722be56b98db0abe1517ec1d018b6f4d6ecbf0a46ea172d198f8294a7edc8cca',38502444)
PRESERVE = ['Assets/Pack','Assets/R3','Assets/R6','Assets/scene 1.unity','Packages','ProjectSettings']
SHA_RE = re.compile(r'[0-9a-f]{64}\Z')
GUID_RE = re.compile(rb'(?m)^guid:\s*([0-9a-fA-F]{32})\s*$')

class GateError(ValueError):
    pass

def require(condition, message):
    if not condition:
        raise GateError(message)

def sha256(data): return hashlib.sha256(data).hexdigest()
def git_blob(data): return hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()

def safe_entries(z):
    paths = {}
    for info in z.infolist():
        path = info.filename
        require(not info.is_dir(), f'unexpected directory ZIP entry {path}')
        require(not path.startswith(('/', '\\')), f'absolute path {path}')
        parts = path.replace('\\','/').split('/')
        require('\\' not in path and all(p not in ('','..','.') for p in parts), f'unsafe path {path}')
        require(not re.match(r'^[a-zA-Z]:', path), f'Windows drive path {path}')
        require(path not in paths, f'duplicate ZIP path {path}')
        require(info.file_size < 10_000_000, f'oversize single source file {path}')
        require((info.external_attr >> 16) & 0o170000 != stat.S_IFLNK, f'symlink {path}')
        paths[path] = info
    return paths

def camera_inner(archive, expected_sha=CAMERA_SHA, expected_bytes=CAMERA_LENGTH):
    with zipfile.ZipFile(archive) as outer:
        names=safe_entries(outer)
        require('CAMERA001_R1_2_FULL_SOURCE_QA.zip' in names, 'missing exact inner source ZIP')
        data=outer.read('CAMERA001_R1_2_FULL_SOURCE_QA.zip')
        report=json.loads(outer.read('SHA256_REPORT.json'))
    require(len(data)==expected_bytes and sha256(data)==expected_sha, 'camera full ZIP SHA/size mismatch')
    require(report.get('sha256')==expected_sha and report.get('bytes')==expected_bytes, 'outer producer report mismatch')
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        paths=safe_entries(z)
        require(len(paths)==41 and report.get('entries')==41, 'CAMERA entry count mismatch')
        manifest_path='Docs/B01/CAMERA001/manifest.json'
        require(manifest_path in paths, 'missing camera manifest')
        m=json.loads(z.read(manifest_path))
        hashes=m.get('r12_source_sha256_by_path',{})
        lengths=m.get('r12_source_bytes_by_path',{})
        blobs=m.get('files',{})
        expected=set(paths)-{manifest_path}
        require(len(expected)==40 and set(hashes)==expected and set(lengths)==expected and set(blobs)==expected,'not exact 40-file manifest coverage')
        require(m.get('revision')=='R1.2', 'wrong revision')
        require(m.get('unity_version')=='6000.3.14f1','wrong Unity version')
        guid_to_path={}; asmdefs={}; files=0; shader_count=0
        for path in sorted(expected):
            require(path=='Assets/B01Packages.meta' or path.startswith('Assets/B01Packages/CAMERA001') or path.startswith('Docs/B01/CAMERA001/') or path.startswith('Tools/B01/CAMERA001/'),f'out of owner scope {path}')
            raw=z.read(path)
            require(sha256(raw)==hashes[path],f'mismatched SHA256 {path}')
            require(len(raw)==lengths[path],f'mismatched bytes {path}')
            require(git_blob(raw)==blobs[path]['git_blob_sha1'],f'mismatched Git blob {path}')
            if path.endswith('.meta'):
                match=GUID_RE.search(raw)
                require(match is not None,f'missing valid guid {path}')
                guid=match.group(1).decode().lower()
                require(guid not in guid_to_path,f'duplicate GUID {guid} {path} {guid_to_path.get(guid)}')
                guid_to_path[guid]=path
            elif path.startswith('Assets/'):
                require(path+'.meta' in expected, f'missing Unity sidecar {path}')
            if path.endswith('.asmdef'):
                obj=json.loads(raw); name=obj.get('name');require(name and name not in asmdefs,'invalid duplicate asmdef name')
                asmdefs[name]=obj
            files+=1
        for asm in asmdefs.values():
            for r in asm.get('references',[]):
                require(r in asmdefs, f'unresolved local asmdef ref {r}')
        require(len(guid_to_path)==16 and len(asmdefs)==3,'unexpected GUID/assembly inventory')
        require('Assets/B01Packages/CAMERA001/Runtime/Camera001Rig.cs' in expected,'missing actual camera implementation')
        require('Assets/B01Packages/CAMERA001/Tests/PlayMode/Camera001WorldCollisionTests.cs' in expected,'missing native regression suite')
        parent=z.read('Assets/B01Packages.meta'); match=GUID_RE.search(parent)
        require(match is not None,'missing parent GUID')
        parent_guid=match.group(1).decode()
    return {'status':'PASS_SOURCE_STATIC_NOT_NATIVE','source':CAMERA,'files_sha256':files,'entries':41,'bytes':len(data),'zip_sha256':sha256(data),'unity_guids':len(guid_to_path),'asmdefs':sorted(asmdefs),'source_parent_guid':parent_guid,'parent_meta_policy':'PRESERVE_TARGET_IF_PRESENT','native_qa':'EXTERNAL_FAIL_AIM_TRACKING_RC1_INTEGRATION_HOLD'}

def index_tree(tree):
    require(not tree.get('truncated'), 'GitHub tree truncated')
    indexed={}
    for x in tree.get('tree',[]):
        if x.get('type')=='blob':
            require(x['path'] not in indexed, 'duplicate tree path')
            indexed[x['path']]=x['sha']
    require(indexed, 'empty GitHub tree')
    return indexed

def audit_rc1(receipt, trees):
    require(receipt.get('gameSource')==RC1_GAME and receipt.get('base')==RC1_BASE and receipt.get('phy'), 'RC1 pin mismatch')
    t={name:index_tree(blob) for name,blob in trees.items()}
    require(set(t)=={'game','base','mapgen','world'},'missing pinned git tree')
    packages={'MAPGEN001':('mapgen',MAPGEN),'MAPGENWORLD001':('world',WORLD)}
    report={};allpaths=set()
    for key,(source,expected_sha) in packages.items():
        pkg=receipt['packages'][key]
        require(pkg['source']==expected_sha,'producer source SHA substituted')
        files=pkg.get('files',[])
        require(pkg['byteIdenticalFiles']==len(files) and len(files)==(35 if key=='MAPGEN001' else 23),'source receipt count mismatch')
        for f in files:
            path=f['path']
            require(path not in allpaths, f'overlap with different owner {path}')
            allpaths.add(path)
            require(path.startswith('Assets/B01Packages/'+key),'out-of-scope import')
            require(SHA_RE.fullmatch(f['sha256']) and isinstance(f['bytes'],int) and f['bytes']>0,'malformed RC1 source receipt')
            require(path in t['game'] and path in t[source],f'absent in actual git trees {path}')
            require(t['game'][path]==t[source][path],f'actual Git blob mismatch {path}')
        report[key]={'source':expected_sha,'git_blob_equal':len(files),'status':'PASS_TRACKED_SOURCE_BYTES'}
    for path in PRESERVE:
        a={p:s for p,s in t['base'].items() if p==path or p.startswith(path+'/')}
        b={p:s for p,s in t['game'].items() if p==path or p.startswith(path+'/')}
        require(a and a==b,f'protected game path changed {path}')
    root='Assets/B01Packages.meta'
    require(t['game'].get(root)==t['mapgen'].get(root)==t['world'].get(root),'RC1 shared parent .meta changed')
    require(receipt.get('guidCollisions')==0,'RC1 producer reported GUID collision')
    require(receipt.get('protectedPathsUnchanged')==PRESERVE,'RC1 protected path scope changed')
    return {'status':'PASS_GIT_PROVENANCE_NOT_NATIVE_OR_BINARY','game':RC1_GAME,'base':RC1_BASE,'tracked_files':len(allpaths),'packages':report,'protected_areas':len(PRESERVE),'target_parent_git_blob':t['game'][root],'windows_distributable_integrity':'PENDING_ACTUAL_BYTES'}

def audit_native(native, camera):
    require(native['gameSource']==RC1_GAME and native['baseSource']==RC1_BASE, 'RC1 native wrong game source')
    require(native.get('G2')=='HOLD' and native.get('independentG1')=='PENDING', 'RC1 claim exceeds authorization')
    for field in ['FinalMapgen720','FinalMapgen1080','FinalReactor']:
        v=native['windows'][field]
        require(v.get('status')=='Passed' and v.get('nativeExit')==0 and not v.get('failedChecks'),f'final RC1 runtime failed {field}')
    hidden=native['windows'].get('MapgenHidden720')
    require(hidden is not None and hidden.get('nativeExit')!=0, 'known shutdown failure omitted or incorrectly marked clean')
    require(camera['producer']==CAMERA and camera.get('G2') in ('HOLD','NOT_GRANTED'),'camera QA pins/gate mismatch')
    require(camera['compile'].get('exitCode')==0,'camera original compilation failure')
    for phase,n in [('EditMode',5),('PlayMode',13)]:
        d=camera['native'][phase]
        require(d['counts']=={'total':n,'passed':n,'failed':0,'skipped':0} and d['process']['exitCode']==0, f'camera {phase} xml/process fail')
    final=camera['windows']['WindowsFinal']
    require(final['process']['exitCode']!=0,'camera failing functional run incorrectly claimed success')
    return {'status':'PASS_EVIDENCE_CLASSIFICATION_WITH_OPEN_FAILURES','rc1_visible_native':'PASS_EXTERNAL_CONTROLLED_ONLY','rc1_hidden_shutdown':'FAIL_OBSERVED_C0000005','camera_native_tests':'5_EDIT_13_PLAY_EXTERNAL_PASS','camera_functional':'FAIL_OBSERVED_2_OF_8_LIGHT_AIM_TRACKING','independent_G1':'PENDING','G2':'HOLD'}

def verify_release_zip(path,expected):
    if not path: return {'status':'PENDING_BINARY_ACCESS','reason':'owner Windows ZIP not supplied'}
    data=Path(path).read_bytes()
    require(len(data)==expected[1] and sha256(data)==expected[0],f'Windows ZIP SHA/bytes mismatch {path}')
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        # Windows retail ZIPs legitimately contain directory entries. Validate their
        # paths, reject duplicate and traversal, then CRC-check every file.
        names=set()
        for i in z.infolist():
            path=i.filename.rstrip('/')
            require(path and path not in names, f'duplicate/invalid Windows ZIP entry {path}')
            names.add(path)
            parts=path.replace('\\','/').split('/')
            require(not path.startswith(('/', '\\')) and '\\' not in path and all(p not in ('','..','.') for p in parts) and not re.match(r'^[a-zA-Z]:',path),f'unsafe retail path {path}')
            require(((i.external_attr >> 16)&0o170000)!=stat.S_IFLNK,f'Windows retail symlink {path}')
            if not i.is_dir():z.read(i.filename)
    return {'status':'PASS_ACTUAL_ZIP_SHA_CRC','bytes':len(data),'sha256':sha256(data)}

def github_json(path,sha):
    url=f'https://api.github.com/repos/{REPO}/contents/{path}?ref={sha}'
    headers={'Accept':'application/vnd.github+json','User-Agent':'B01-40-ReceiverGate/1'}
    if os.getenv('GITHUB_TOKEN'):headers['Authorization']='Bearer '+os.environ['GITHUB_TOKEN']
    with urllib.request.urlopen(urllib.request.Request(url,headers=headers),timeout=35) as r:obj=json.load(r)
    require(obj.get('encoding')=='base64','unexpected GitHub encoding')
    return json.loads(base64.b64decode(obj['content']))

def github_tree(sha):
    url=f'https://api.github.com/repos/{REPO}/git/trees/{sha}?recursive=1'
    headers={'Accept':'application/vnd.github+json','User-Agent':'B01-40-ReceiverGate/1'}
    if os.getenv('GITHUB_TOKEN'):headers['Authorization']='Bearer '+os.environ['GITHUB_TOKEN']
    with urllib.request.urlopen(urllib.request.Request(url,headers=headers),timeout=35) as r:return json.load(r)

def cli(argv=None):
    parser=argparse.ArgumentParser(description='READ-ONLY B01-40 RC1+CAMERA source/evidence gates')
    parser.add_argument('--camera-artifact',type=Path,required=True,help='REAL downloaded GitHub Actions outer ZIP #12610554389')
    parser.add_argument('--live',action='store_true',help='pin and independently compare real GitHub Git trees and evidence; requires GitHub network')
    parser.add_argument('--base-zip',type=Path,help='optional actual Windows BASE ZIP from owner')
    parser.add_argument('--experimental-zip',type=Path,help='optional actual Windows FINAL experimental ZIP from owner')
    parser.add_argument('--json-out',type=Path)
    a=parser.parse_args(argv)
    result={'package':'B01-40 RC1-RECEIVER-001','G2':'HOLD'}
    try:
        result['camera_source']=camera_inner(a.camera_artifact)
        if a.live:
            source=github_json('Docs/B01_RC1/source-integrity.json',RC1_REPORT)
            native=github_json('Docs/B01_RC1/native-summary.json',RC1_REPORT)
            cam=github_json('Docs/CAMERA001R12NativeQA/native-summary.json',CAMERA_QA)
            trees={name:github_tree(sha) for name,sha in {'game':RC1_GAME,'base':RC1_BASE,'mapgen':MAPGEN,'world':WORLD}.items()}
            result['rc1_source']=audit_rc1(source,trees)
            result['external_native']=audit_native(native,cam)
        else:
            result['rc1_source']={'status':'PENDING_LIVE_GIT_TREE_RECEIPT','reason':'use --live for independent pinned 58-file comparison'}
            result['external_native']={'status':'PENDING_LIVE_RAW_QA_RECEIPT'}
        result['windows_zip_base']=verify_release_zip(a.base_zip,BASE_ZIP)
        result['windows_zip_experimental']=verify_release_zip(a.experimental_zip,EXP_ZIP)
        result['status']='PASS_PERFORMED_CHECKS_G2_HOLD'
    except (GateError,zipfile.BadZipFile,zlib.error,KeyError,ValueError,OSError) as e:
        result['status']='FAIL_CLOSED'
        result['error']=str(e)
    print(json.dumps(result,indent=2,ensure_ascii=False))
    if a.json_out:
        a.json_out.parent.mkdir(parents=True,exist_ok=True)
        a.json_out.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
    return 0 if result['status']=='PASS_PERFORMED_CHECKS_G2_HOLD' else 2

if __name__=='__main__':sys.exit(cli())
