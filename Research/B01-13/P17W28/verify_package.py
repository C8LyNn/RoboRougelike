#!/usr/bin/env python3
"""Physical Git blob / byte manifest verifier, source-only, not Unity G1."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def main():
 m=json.loads(pathlib.Path(__file__).with_name('manifest.json').read_text('utf8'))
 assert m['package_id']=='B01-13-W34-PLAYTEST-EVIDENCE-INTAKE-001/R5'
 assert m['parent_pr']==905 and m['parent_sha']=='7309b8b080041f984cae3adcdd8b2d08bb2f3ea0'
 assert len(m['files'])==6
 for name,expected in sorted(m['files'].items()):
  assert (name.startswith('Research/B01-13/P17W28/') or name=='Research/B01-13/P17W32/test_zip_canonical.py' or name=='.github/workflows/b01-13-w28-evidence-intake.yml'),('OUT_OF_SCOPE',name)
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),('FILE',name)
  b=f.read_bytes();sha=hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()
  assert sha==expected['git_blob_sha1'] and len(b)==expected['bytes'],('ORIGINAL_SOURCE_MISMATCH',name)
  print('W34_SOURCE_PASS',name,sha,'bytes',len(b),'sha256',hashlib.sha256(b).hexdigest(),flush=True)
 print('W34_MANIFEST_PASS',len(m['files']),flush=True)
if __name__=='__main__':
 try:main()
 except Exception as e:print('W34_MANIFEST_FAIL',str(e),file=sys.stderr);sys.exit(2)
