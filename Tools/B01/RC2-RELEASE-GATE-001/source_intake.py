#!/usr/bin/env python3
"""B01-40 reproducible *physical producer artifact* intake for W13 only.
The source ZIP is never installed; this gate checks exact byte provenance,
manifest completeness, scope, paired Unity .meta and duplicate GUID.
"""
import argparse
import hashlib
import io
import json
import re
import zipfile
from pathlib import Path
from rc2_release_gate import GateError, ensure, valid_name

PINS={
 'BUILD_R13':{'zip_name':'B01-BUILD001-R13.zip','sha':'bbd604f497539451bee3ed54094132fd830e68901d11ba6fa7c5943babbebc75','bytes':138799,'source':'be54b4a33360cbd1923ae5dacbc92dff8eaf586b','manifest':'PACKAGE_MANIFEST_R13.json','count':56,'guid_count':23,'paths':('Assets/B01Construction.meta','Assets/B01Construction/BUILD001.meta','Assets/B01Construction/BUILD001/','Docs/B01/BUILD001/','Tools/B01/BUILD001/')},
 'MAPGENWORLD_R2':{'zip_name':'MAPGENWORLD001_SOURCE_FULL.zip','sha':'28b6422e7c358f379a47b49a0a671f631cee6d7f627daf3f6bf4807d7c3b8a41','bytes':38067,'source':'5ce9d806d7fc7eb0ab2ef7b586626404c346174f','manifest':'META-INF/MAPGENWORLD001_MANIFEST.json','count':37,'guid_count':14,'paths':('Assets/B01Packages/MAPGENWORLD001.meta','Assets/B01Packages/MAPGENWORLD001/','Docs/B01/MAPGENWORLD001/','Tools/B01/MAPGENWORLD001/')},
}
DOMAIN_PINS={
 'REWARDKEEP3':{'zip':'REWARDKEEP3001_Unity_Source.zip','source':'6bb6796ee3ad88a015fb6c8d033023c0ee3b5e94','sha':'27df176be1ca28a307dc1b61f92710b9f8b0d7e37b420c033cdb61d020266fe4','prefix':'Assets/B01Packages/REWARDKEEP3001'},
 'ECONREBUILD':{'zip':'ECONREBUILD001_Unity_Source.zip','source':'12572117a3dce369257feabc57021cde58023369','sha':'fdcb15e6b394715f450771cac798b1c02b8efe1f33acb815e9e0957c46bd2d87','prefix':'Assets/B01Packages/ECONREBUILD001'}
}

def validate_inner(data,kind):
 pin=PINS[kind]
 ensure(len(data)==pin['bytes'],'INNER_ZIP_SIZE_MISMATCH')
 ensure(hashlib.sha256(data).hexdigest()==pin['sha'],'INNER_ZIP_SHA256_MISMATCH')
 try:
  with zipfile.ZipFile(io.BytesIO(data)) as z:
   names=z.namelist(); ensure(len(names)==pin['count'],'INNER_ENTRY_COUNT_MISMATCH')
   ensure(len(names)==len(set(valid_name(n) for n in names)),'INNER_DUPLICATE_WINDOWS_PATH')
   ensure(pin['manifest'] in names,'INNER_MANIFEST_MISSING')
   m=json.loads(z.read(pin['manifest']))
   if kind == 'BUILD_R13': ensure(m.get('source_head_sha') == pin['source'], 'EMBEDDED_SOURCE_SHA_MISMATCH')
   if kind == 'MAPGENWORLD_R2': ensure(m.get('upstreamMAPGEN001') == '5be31039411f89f17654b1c332f8692883fa03cc', 'UPSTREAM_SHA_MISMATCH')
   rows=m['files']; rec={f['path']:f for f in rows}
   ensure(len(rows)==pin['count']-1 and len(rec)==len(rows),'MANIFEST_ENTRY_MISMATCH')
   ensure(set(names)==set(rec)|{pin['manifest']},'MANIFEST_ZIP_COMPLETENESS')
   ensure(all(p==pin['manifest'] or any(p==prefix or p.startswith(prefix) for prefix in pin['paths']) for p in names),'PACKAGE_SCOPE_ESCAPE')
   ensure(all('/obj/' not in p and '/bin/' not in p and '__pycache__' not in p for p in names),'TRANSIENT_BUILD_OUTPUT')
   for path,row in rec.items():
    dat=z.read(path)
    ensure(len(dat)==row.get('bytes',row.get('size')),'FILE_BYTES_MISMATCH:'+path)
    ensure(hashlib.sha256(dat).hexdigest()==row['sha256'],'FILE_SHA256_MISMATCH:'+path)
   asset={p:z.read(p) for p in names if p.startswith('Assets/')}
   for p in asset:
    if p.endswith('.meta'):continue
    ensure(p+'.meta' in asset,'UNITY_META_MISSING:'+p)
   guid={}
   for p,b in asset.items():
    if not p.endswith('.meta'):continue
    mm=re.search(rb'(?m)^guid: ([0-9a-f]{32})\s*$',b)
    ensure(mm is not None,'INVALID_UNITY_GUID:'+p)
    g=mm.group(1).decode()
    ensure(g not in guid,'UNITY_GUID_COLLISION:'+p+' AND '+guid.get(g,''))
    guid[g]=p
   ensure(len(guid)==pin['guid_count'],'GUID_COUNT_MISMATCH')
   asm=[]
   for p,b in asset.items():
    if p.endswith('.asmdef'):
     a=json.loads(b.decode());ensure(a.get('name'),'ASMDEF_NO_NAME:'+p);asm.append(a['name'])
   ensure(len(set(asm))==len(asm),'DUPLICATE_ASMDEF_NAME')
   if kind=='MAPGENWORLD_R2':
    ensure('B01.MAPGENWORLD001.Runtime' in asm,'WORLD_RUNTIME_ASMDEF_MISSING')
    ensure(any('B01.MAPGEN001.Runtime' in str(json.loads(b.decode()).get('references',[])) for p,b in asset.items() if p.endswith('.asmdef')),'UPSTREAM_MAPGEN_ASMDEF_MISSING')
   if kind=='BUILD_R13':
    ensure('Assets/B01Construction/BUILD001/Resources/BUILD001_PreviewURP.shader' in asset,'BUILD_URP_SHADER_MISSING')
   return {'kind':kind,'status':'PASS_PHYSICAL_PRODUCER_SOURCE','source':pin['source'],'inner_sha256':pin['sha'],'inner_bytes':len(data),'entries':len(names),'file_hashes_pass':len(rec),'guid_unique':len(guid),'asmdef_names':asm}
 except (zipfile.BadZipFile,EOFError,OSError,ValueError,KeyError,RuntimeError) as exc:
  raise GateError('MALFORMED_SOURCE_ZIP:'+type(exc).__name__) from exc

def audit_wrapper(path,kind):
 with zipfile.ZipFile(path) as z:
  if PINS[kind]['manifest'] in z.namelist():
   return validate_inner(Path(path).read_bytes(),kind)
  candidates=[n for n in z.namelist() if n.endswith('/'+PINS[kind]['zip_name']) or n==PINS[kind]['zip_name']]
  ensure(len(candidates)==1,'EXPECTED_ONE_NESTED_PRODUCER_ZIP')
  return validate_inner(z.read(candidates[0]),kind)

def audit_domain_artifact(path, kind):
 """Actual Unity ZIP with *external* manifest in producer Actions wrapper."""
 pin=DOMAIN_PINS[kind]
 with zipfile.ZipFile(path) as outer:
  ensure(pin['zip'] in outer.namelist() and 'source-manifest.json' in outer.namelist(), 'DOMAIN_EXTERNAL_MANIFEST_OR_ZIP_MISSING')
  m=json.loads(outer.read('source-manifest.json'))
  ensure(m.get('commit_sha') == pin['source'], 'DOMAIN_STALE_SOURCE_SHA')
  entries={row['path']:row for row in m['entries'] if row['path'].startswith('Assets/')}
  inner=outer.read(pin['zip'])
 ensure(hashlib.sha256(inner).hexdigest()==pin['sha'],'DOMAIN_ZIP_SHA_MISMATCH')
 with zipfile.ZipFile(io.BytesIO(inner)) as z:
  paths=z.namelist(); ensure(len(paths)==16 and len(set(valid_name(p) for p in paths))==16,'DOMAIN_ZIP_STRUCTURE')
  ensure(set(paths)==set(entries),'DOMAIN_EXTERNAL_MANIFEST_SET_MISMATCH')
  ensure(all(p=='Assets/B01Packages.meta' or p==pin['prefix']+'.meta' or p.startswith(pin['prefix']+'/') for p in paths),'DOMAIN_SCOPE_ESCAPE')
  guids={};assemblies=[]
  for p in paths:
   b=z.read(p);row=entries[p]
   ensure(row['bytes']==len(b) and row['sha256']==hashlib.sha256(b).hexdigest(), 'DOMAIN_FILE_SHA_MISMATCH:'+p)
   if p.endswith('.meta'):
    mm=re.search(rb'(?m)^guid: ([0-9a-f]{32})\s*$',b)
    ensure(mm is not None,'DOMAIN_INVALID_META:'+p)
    g=mm.group(1).decode();ensure(g not in guids,'DOMAIN_GUID_COLLISION');guids[g]=p
   else:
    ensure(p+'.meta' in paths,'DOMAIN_MISSING_META:'+p)
   if p.endswith('.asmdef'):
    a=json.loads(b);ensure(a.get('name'),'DOMAIN_ASMDEF_NO_NAME');assemblies.append(a['name'])
  ensure(len(guids)==11 and len(set(assemblies))==2,'DOMAIN_META_OR_ASMDEF_COUNT')
 return {'kind':kind,'source':pin['source'],'status':'PASS_PHYSICAL_PRODUCER_SOURCE','entries':len(paths),'manifest_file_hashes_pass':len(entries),'guid_unique':len(guids),'asmdef_names':assemblies,'inner_sha256':pin['sha'],'inner_bytes':len(inner)}

def main():
 p=argparse.ArgumentParser();p.add_argument('kind',choices=tuple(PINS)+tuple(DOMAIN_PINS));p.add_argument('artifact');p.add_argument('--report');a=p.parse_args()
 try:r=audit_wrapper(a.artifact,a.kind) if a.kind in PINS else audit_domain_artifact(a.artifact,a.kind)
 except (GateError, OSError,zipfile.BadZipFile) as e:r={'kind':a.kind,'status':'FAIL','reason':str(e)}
 txt=json.dumps(r,indent=2,sort_keys=True)
 if a.report:Path(a.report).write_text(txt+'\n')
 print(txt);return 0 if r['status'].startswith('PASS') else 2
if __name__=='__main__':raise SystemExit(main())
