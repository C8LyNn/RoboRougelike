#!/usr/bin/env python3
"""Exact physical original W24 source only. Not real Unity, participant, or gameplay QA."""
import hashlib,json,pathlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def main():
 m=json.loads(pathlib.Path(__file__).with_name('manifest.json').read_text(encoding='utf8'))
 assert m['package']=='B01-13-APPROVED-GARAGE-HUMAN-STUDY-001/R2'
 assert m['policy']=='OWNER_APPROVED_2026_10_30_ISSUE_731'
 assert len(m['files'])==6
 for name, expected in sorted(m['files'].items()):
  assert name.startswith('Research/B01-13/P17V/') or name=='.github/workflows/b01-13-w24-approved-garage-study.yml',('SCOPE',name)
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),('FILE',name)
  b=f.read_bytes()
  digest=hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()
  assert digest==expected['git_blob_sha1'] and len(b)==expected['bytes'],('DRIFT',name)
  print('SOURCE_PASS',name,'git',digest,'bytes',len(b),'sha256',hashlib.sha256(b).hexdigest(),flush=True)
 print('MANIFEST_PASS',len(m['files']),flush=True)
if __name__=='__main__':
 try:main()
 except Exception as e:print('MANIFEST_FAIL',repr(e),file=sys.stderr);sys.exit(2)
