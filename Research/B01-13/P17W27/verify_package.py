#!/usr/bin/env python3
"""Exact original-source Git blob/byte pin verification. Source G0 input, not native G1."""
import pathlib,json,hashlib,sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
BASE='Research/B01-13/P17W27/'
def check():
 m=json.loads((ROOT/BASE/'manifest.json').read_text('utf8'))
 assert m['package_id']=='B01-13-REAL-PLAYTEST-OBSERVATION-001/R1'
 assert m['parent_sha']=='b79b6e817b7d905d243fc1a02cee7cf90063c151'
 assert m['human_sessions']==0 and m['native_g1']=='NOT_RUN'
 assert len(m['files'])==6
 for name,record in sorted(m['files'].items()):
  assert name.startswith(BASE) or name=='.github/workflows/b01-13-w27-human-observation.yml'
  f=ROOT/name
  assert f.is_file() and not f.is_symlink(),('FILE',name)
  b=f.read_bytes();git=hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()
  assert record['git_blob_sha1']==git and record['bytes']==len(b),('DRIFT',name)
  print('SOURCE_PASS',name,'bytes',len(b),'git',git,'sha256',hashlib.sha256(b).hexdigest(),flush=True)
 print('MANIFEST_PASS',len(m['files']))
if __name__=='__main__':
 try:check()
 except Exception as e:print('MANIFEST_FAIL',repr(e),file=sys.stderr);sys.exit(2)
