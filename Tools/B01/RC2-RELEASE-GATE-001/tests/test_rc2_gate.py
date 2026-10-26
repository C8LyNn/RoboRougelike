import contextlib
import io
import json
import os
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import rc2_release_gate as gate
import source_intake

BUILD=os.environ.get('B01_BUILD_R13_ARTIFACT','/mnt/data/BUILD_R13_PRODUCER_ARTIFACT.zip')
WORLD=os.environ.get('B01_WORLD_R2_ARTIFACT','/mnt/data/MAPGENWORLD_R2_PRODUCER_ARTIFACT.zip')

class GateTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
  self.root=Path(self.tmp.name)
  self.blobs={'RoboRougelike.exe':b'MZ-EXAMPLE','RoboRougelike_Data/Managed/Assembly-CSharp.dll':b'fake-assembly','RoboRougelike_Data/boot.config':b'boot'}
  self.receipt={'source':gate.PINS['production'],'zipBytes':0,'zipSHA256':'','files':[{'path':p,'bytes':len(b),'sha256':gate.sha256(b)} for p,b in self.blobs.items()]}
  self.zip_path=self.root/'RC2.zip'
  self.repack()
  self.patches=[patch.object(gate,'EXPECTED_ZIP_SHA',gate.file_sha256(self.zip_path)),patch.object(gate,'EXPECTED_ZIP_BYTES',self.zip_path.stat().st_size),patch.object(gate,'EXPECTED_ASSEMBLY_SHA',gate.sha256(self.blobs['RoboRougelike_Data/Managed/Assembly-CSharp.dll']))]
  for x in self.patches:x.start();self.addCleanup(x.stop)
  self.receipt['zipBytes']=gate.EXPECTED_ZIP_BYTES;self.receipt['zipSHA256']=gate.EXPECTED_ZIP_SHA
 def repack(self, entries=None):
  with zipfile.ZipFile(self.zip_path,'w',compression=zipfile.ZIP_STORED) as z:
   for p,b in (entries or self.blobs).items():z.writestr(p,b)
 def test_01_valid_release(self):
  self.assertEqual(gate.inspect_release(self.zip_path,self.receipt)['files'],3)
 def test_02_missing_local_archive_is_pending(self):
  with self.assertRaisesRegex(gate.GateError,'PENDING_BINARY_ACCESS'):gate.inspect_release(self.root/'missing.zip',self.receipt)
 def test_03_modified_zip_hash_rejected(self):
  with open(self.zip_path,'ab') as f:f.write(b'evil')
  with self.assertRaisesRegex(gate.GateError,'RC2_ZIP_BYTES_MISMATCH'):gate.inspect_release(self.zip_path,self.receipt)
 def test_04_forged_receipt_hash_rejected(self):
  r=json.loads(json.dumps(self.receipt));r['zipSHA256']='0'*64
  with self.assertRaisesRegex(gate.GateError,'RECEIPT_PIN_MISMATCH'):gate.inspect_release(self.zip_path,r)
 def test_05_forged_receipt_file_hash_rejected(self):
  r=json.loads(json.dumps(self.receipt));r['files'][0]['sha256']='0'*64
  with self.assertRaisesRegex(gate.GateError,'ZIP_FILE_SHA256_MISMATCH'):gate.inspect_release(self.zip_path,r)
 def test_06_missing_archive_entry_rejected(self):
  self.repack({'RoboRougelike.exe':b'MZ-EXAMPLE', 'RoboRougelike_Data/Managed/Assembly-CSharp.dll':b'fake-assembly'})
  with patch.object(gate,'EXPECTED_ZIP_BYTES',self.zip_path.stat().st_size),patch.object(gate,'EXPECTED_ZIP_SHA',gate.file_sha256(self.zip_path)):
   r=json.loads(json.dumps(self.receipt));r['zipBytes']=gate.EXPECTED_ZIP_BYTES;r['zipSHA256']=gate.EXPECTED_ZIP_SHA
   with self.assertRaisesRegex(gate.GateError,'ZIP_MISSING_DECLARED_FILES'):gate.inspect_release(self.zip_path,r)
 def test_07_zip_slip_rejected(self):
  self.repack({**self.blobs,'../out.txt':b'danger'})
  with patch.object(gate,'EXPECTED_ZIP_BYTES',self.zip_path.stat().st_size),patch.object(gate,'EXPECTED_ZIP_SHA',gate.file_sha256(self.zip_path)):
   r=json.loads(json.dumps(self.receipt));r['zipBytes']=gate.EXPECTED_ZIP_BYTES;r['zipSHA256']=gate.EXPECTED_ZIP_SHA
   with self.assertRaisesRegex(gate.GateError,'UNSAFE_ZIP_TRAVERSAL'):gate.inspect_release(self.zip_path,r)
 def test_08_duplicate_windows_names_rejected(self):
  self.repack({**self.blobs,'ROBOROUGELIKE.EXE':b'fake'})
  with patch.object(gate,'EXPECTED_ZIP_BYTES',self.zip_path.stat().st_size),patch.object(gate,'EXPECTED_ZIP_SHA',gate.file_sha256(self.zip_path)):
   r=json.loads(json.dumps(self.receipt));r['zipBytes']=gate.EXPECTED_ZIP_BYTES;r['zipSHA256']=gate.EXPECTED_ZIP_SHA
   with self.assertRaisesRegex(gate.GateError,'ZIP_DUPLICATE_WINDOWS_PATH'):gate.inspect_release(self.zip_path,r)
 def test_09_wrong_assembly_rejected(self):
  with patch.object(gate,'EXPECTED_ASSEMBLY_SHA','f'*64):
   with self.assertRaisesRegex(gate.GateError,'RC2_ASSEMBLY_PIN_MISMATCH'):gate.inspect_release(self.zip_path,self.receipt)
 def test_10_wrong_producer_sha_rejected(self):
  r=json.loads(json.dumps(self.receipt));r['source']='a'*40
  with self.assertRaisesRegex(gate.GateError,'RECEIPT_SOURCE_MISMATCH'):gate.inspect_release(self.zip_path,r)
 def test_11_traversal_path_helper(self):
  for s in ['C:/file','../a','/path','a\\b','a/./b','a/../b','a:bad','a?.exe']:
   with self.subTest(s=s),self.assertRaises(gate.GateError):gate.valid_name(s)
 def test_12_original_targeted_not_full_edit(self):
  receipts=[{'phase':'EditMode','exitCode':2,'nunit':{'total':1068,'passed':1066,'failed':2}}, {'phase':'EditMode','source':gate.PINS['editor_fix'],'exitCode':0,'nunit':{'total':2,'passed':2,'failed':0}}, {'phase':'PlayMode','exitCode':0,'nunit':{'total':424,'passed':424,'failed':0}}]
  eq={'productionBuildSHA':gate.PINS['production'],'editorTestRevision':gate.PINS['editor_fix'],'productionChangesExcludingEditorTests':[], 'assemblySHA256':gate.EXPECTED_ASSEMBLY_SHA,'archiveSHA256':gate.EXPECTED_ZIP_SHA}
  result=gate.inspect_evidence(receipts,eq)
  self.assertEqual(result['full_edit_new_revision'],'NOT_RUN')
  self.assertEqual(result['full_edit'],'FAIL_1066_OF_1068_OLD_REVISION')
 def test_13_forged_green_initial_native_receipt_rejected(self):
  receipts=[{'phase':'EditMode','exitCode':0,'nunit':{'total':1068,'passed':1068,'failed':0}}, {'phase':'EditMode','source':gate.PINS['editor_fix'],'exitCode':0,'nunit':{'total':2,'passed':2,'failed':0}}, {'phase':'PlayMode','exitCode':0,'nunit':{'total':424,'passed':424,'failed':0}}]
  eq={'productionBuildSHA':gate.PINS['production'],'editorTestRevision':gate.PINS['editor_fix'],'productionChangesExcludingEditorTests':[], 'assemblySHA256':gate.EXPECTED_ASSEMBLY_SHA,'archiveSHA256':gate.EXPECTED_ZIP_SHA}
  with self.assertRaisesRegex(gate.GateError,'FULL_EDIT_RESULT_CONTRADICTS_PIN'):gate.inspect_evidence(receipts,eq)
 def test_14_prod_edit_changes_rejected(self):
  receipts=[{'phase':'EditMode','exitCode':2,'nunit':{'total':1068,'passed':1066,'failed':2}}, {'phase':'EditMode','source':gate.PINS['editor_fix'],'exitCode':0,'nunit':{'total':2,'passed':2,'failed':0}}, {'phase':'PlayMode','exitCode':0,'nunit':{'total':424,'passed':424,'failed':0}}]
  eq={'productionBuildSHA':gate.PINS['production'],'editorTestRevision':gate.PINS['editor_fix'],'productionChangesExcludingEditorTests':['Assets/R3/evil.cs'],'assemblySHA256':gate.EXPECTED_ASSEMBLY_SHA,'archiveSHA256':gate.EXPECTED_ZIP_SHA}
  with self.assertRaisesRegex(gate.GateError,'PRODUCTION_CHANGED_AFTER_BUILD'):gate.inspect_evidence(receipts,eq)
 def test_15_valid_path_casefold_collision(self):
  with self.assertRaisesRegex(gate.GateError,'DUPLICATE_RECEIPT_PATH'):
   r=json.loads(json.dumps(self.receipt));r['files'].append({'path':'ROBOROUGELIKE.EXE','bytes':1,'sha256':'0'*64});gate.inspect_release(self.zip_path,r)
 def test_16_guid_and_manifest_real_build(self):
  if not Path(BUILD).exists():self.skipTest('requires real Actions producer wrapper')
  self.assertEqual(source_intake.audit_wrapper(BUILD,'BUILD_R13')['guid_unique'],23)
 def test_17_guid_and_manifest_real_world(self):
  if not Path(WORLD).exists():self.skipTest('requires real Actions producer wrapper')
  self.assertEqual(source_intake.audit_wrapper(WORLD,'MAPGENWORLD_R2')['file_hashes_pass'],36)
 def test_18_corrupted_real_build_gate(self):
  if not Path(BUILD).exists():self.skipTest('requires real Actions producer wrapper')
  with zipfile.ZipFile(BUILD) as z:binary=z.read('B01-BUILD001-R13.zip') if 'B01-BUILD001-R13.zip' in z.namelist() else Path(BUILD).read_bytes()
  data=bytearray(binary);data[-20]^=1
  with self.assertRaisesRegex(gate.GateError,'INNER_ZIP_SHA256_MISMATCH'):source_intake.validate_inner(bytes(data),'BUILD_R13')
 def test_19_corrupted_world_gate(self):
  if not Path(WORLD).exists():self.skipTest('requires real Actions producer wrapper')
  with zipfile.ZipFile(WORLD) as z:binary=z.read(next(n for n in z.namelist() if n.endswith('MAPGENWORLD001_SOURCE_FULL.zip'))) if any(n.endswith('MAPGENWORLD001_SOURCE_FULL.zip') for n in z.namelist()) else Path(WORLD).read_bytes()
  data=bytearray(binary);data[51]^=1
  with self.assertRaisesRegex(gate.GateError,'INNER_ZIP_SHA256_MISMATCH'):source_intake.validate_inner(bytes(data),'MAPGENWORLD_R2')
 def _git_fixture(self, bad=None):
  base={'Assets/R3/core.cs':'a'*40,'Assets/R6/drive.cs':'b'*40,'Assets/Pack/P12_PipelineSafeEnemyTurret.cs':'c'*40,'Packages/manifest.json':'d'*40,'ProjectSettings/ProjectVersion.txt':'e'*40,'Assets/B01Packages.meta':'f'*40}
  source=dict(base);source['Assets/B01Packages/MAPGENWORLD001.meta']='1'*40
  source['Assets/B01Packages/REWARDKEEP3001.meta']='2'*40
  for key,prefix in gate.PKG_PREFIXES.items():
   for i in range(10):source[prefix+'/Runtime/test%d.cs'%i]=(str(i)+'b')*20
  source['Assets/Pack/P12_PipelineSafeEnemyTurret.cs']='3'*40
  editor=dict(source);report=dict(source)
  for name in gate.EDITOR_FIX_PATHS:
   source[name]='4'*40;editor[name]='5'*40;report[name]='5'*40
  data={'production':source,'editor_fix':editor,'report':report,'rc1':base,'world':{k:v for k,v in source.items() if 'MAPGENWORLD001' in k},'keep3':{k:v for k,v in source.items() if 'REWARDKEEP3001' in k}}
  if bad=='world':data['production']['Assets/B01Packages/MAPGENWORLD001.meta']='0'*40
  if bad=='pack':
   for v in ('production','editor_fix','report'):data[v]['Assets/Pack/other.cs']='2'*40
  if bad=='runtime':data['editor_fix']['Assets/R6/drive.cs']='9'*40
  return data
 def test_21_git_source_positive(self):
  d=self._git_fixture()
  with patch.object(gate,'git_tree',side_effect=lambda _repo,ref:d[ref]):
   r=gate.inspect_git_source(self.root,{k:k for k in gate.PINS})
  self.assertEqual(r['status'],'PASS_PINNED_GIT_SOURCE')
  self.assertEqual(r['producer_files'],{'world':11,'keep3':11})
 def test_22_git_source_wrong_producer_rejected(self):
  d=self._git_fixture('world')
  with patch.object(gate,'git_tree',side_effect=lambda _repo,ref:d[ref]):
   with self.assertRaisesRegex(gate.GateError,'PRODUCER_PACKAGE_CHANGED'):gate.inspect_git_source(self.root,{k:k for k in gate.PINS})
 def test_23_git_unapproved_runtime_change_rejected(self):
  d=self._git_fixture('runtime')
  with patch.object(gate,'git_tree',side_effect=lambda _repo,ref:d[ref]):
   with self.assertRaisesRegex(gate.GateError,'UNAUTHORIZED_EDITOR_ASSET_CHANGE'):gate.inspect_git_source(self.root,{k:k for k in gate.PINS})
 def test_24_git_pack_unreviewed_change_rejected(self):
  d=self._git_fixture('pack')
  with patch.object(gate,'git_tree',side_effect=lambda _repo,ref:d[ref]):
   with self.assertRaisesRegex(gate.GateError,'UNREVIEWED_PACK_CHANGE'):gate.inspect_git_source(self.root,{k:k for k in gate.PINS})
 def test_20_no_release_artifact_claimed_without_file(self):
  result=gate.main(['--report',str(self.root/'result.json')])
  self.assertEqual(result,0)
  saved=json.loads((self.root/'result.json').read_text())
  self.assertEqual(saved['binary']['status'],'PENDING_BINARY_ACCESS')
  self.assertEqual(saved['G2'],'HOLD')

if __name__=='__main__':unittest.main()
