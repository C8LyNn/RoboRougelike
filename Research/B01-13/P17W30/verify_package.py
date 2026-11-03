#!/usr/bin/env python3
"""Fail-closed byte-exact owner scope verifier; no native game assertions."""
import hashlib, json, pathlib, sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
def main():
 m=json.loads(pathlib.Path(__file__).with_name('manifest.json').read_text('utf8'))
 assert m['package_id']=='B01-13-W30-INTERACTIVE-WINDOWS-PLAYER-SESSION-001/R1'
 assert m['parent_sha']=='6affe78371a4c9c0bcb90835ebb9bffee6976da9'
 assert len(m['files'])==5
 for name,v in sorted(m['files'].items()):
  assert name.startswith('Research/B01-13/P17W30/') or name=='.github/workflows/b01-13-w30-real-player-session.yml',name
  p=ROOT/name
  assert p.is_file() and not p.is_symlink(),('MISSING_OR_SYMLINK',name)
  b=p.read_bytes()
  git=hashlib.sha1(b'blob '+str(len(b)).encode()+b'\0'+b).hexdigest()
  assert git==v['git_blob_sha1'] and len(b)==v['bytes'],('FILE_MISMATCH',name)
  print('W30_GIT_BLOB_PASS',name,len(b),git,'SHA256',hashlib.sha256(b).hexdigest(),flush=True)
 print('W30_MANIFEST_PASS',len(m['files']))
if __name__=='__main__':
 try:main()
 except Exception as e:print('W30_MANIFEST_FAIL',str(e),file=sys.stderr);sys.exit(2)
