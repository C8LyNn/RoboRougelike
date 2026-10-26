"""W14 RC2: real GLB+sample evidence, anchored footprint candidates and negative tests."""
import copy, importlib.util, json, os, tempfile, unittest, shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
PACKAGE=Path(os.environ.get('CI_PACKAGE_DIR',str(ROOT/'_not_generated_yet')))
spec=importlib.util.spec_from_file_location('mount',ROOT/'tools/mount_envelope_candidates.py')
mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)

class OptionsTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not (PACKAGE/'qa/M04-024_SWEEP.json').exists():
   raise unittest.SkipTest('actual Codex ZIP not staged yet')
  cls.gun=mod.measured(PACKAGE,'M01-018')
  cls.hover=mod.measured(PACKAGE,'M04-024')

 def test_original_gun_blocked(self):self.assertFalse(mod.candidate(self.gun,[3,2,2])['meetsReviewMargin'])
 def test_original_hover_blocked(self):self.assertFalse(mod.candidate(self.hover,[3,3,2])['meetsReviewMargin'])
 def test_gun_fixed_proposal(self):
  a=mod.recommend(self.gun)['minimumUnchangedRootDepthCenter']
  self.assertEqual(a['cellsWHD'],[3,2,8]);self.assertTrue(a['meetsReviewMargin'])
  self.assertFalse(a['approvedToMount'])
 def test_gun_7_depth_requires_offset(self):
  a=mod.recommend(self.gun)['minimumConditionalDepthCenterShift']
  self.assertEqual(a['cellsWHD'],[3,2,7]);self.assertEqual(a['depthCenterOffsetFromRootM'],.10)
  self.assertTrue(a['requiresDesignApprovalForOffset'])
 def test_gun_7_without_offset_fails(self):self.assertFalse(mod.candidate(self.gun,[3,2,7])['meetsReviewMargin'])
 def test_hover_fixed_proposal(self):
  a=mod.recommend(self.hover)['minimumUnchangedRootDepthCenter']
  self.assertEqual(a['cellsWHD'],[4,2,4]);self.assertTrue(a['meetsReviewMargin'])
 def test_hover_nominal_depth_fails(self):self.assertFalse(mod.candidate(self.hover,[4,2,2])['meetsReviewMargin'])
 def test_hover_3_2_3_fails(self):self.assertFalse(mod.candidate(self.hover,[3,2,3])['meetsReviewMargin'])
 def test_hover_4_2_3_fails(self):self.assertFalse(mod.candidate(self.hover,[4,2,3])['meetsReviewMargin'])
 def test_hover_3_2_4_fails(self):self.assertFalse(mod.candidate(self.hover,[3,2,4])['meetsReviewMargin'])
 def test_hover_vertical_mount_root(self):self.assertEqual(self.hover['vertical'],'top')
 def test_weapon_vertical_mount_root(self):self.assertEqual(self.gun['vertical'],'bottom')
 def test_real_glb_pin_m04(self):
  self.assertEqual(self.hover['sha256'],'c40ce2c090b232833340e3135d75184d01eb7f81c072ab07c80d46508f2af0d8')
 def test_real_glb_pin_m01(self):
  self.assertEqual(self.gun['sha256'],'795124613e26cf2f4d7d3d59990542d0d2f4b3a65fef911132318748bae589dd')
 def test_never_authoritative_even_large(self):
  for x in (self.hover,self.gun):
   r=mod.candidate(x,[10,10,12])
   self.assertFalse(r['approvedToMount']);self.assertFalse(r['authorizedSocket'])
 def test_zero_scale_not_accepted(self):
  with self.assertRaisesRegex(ValueError,'invalid cells'):mod.candidate(self.hover,[0,2,4])
 def test_invalid_frame_counts_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/'source';shutil.copytree(PACKAGE,path)
   q=path/'qa/M04-024_SWEEP.json';v=json.loads(q.read_text());v['samplesPerClip']=3
   q.write_text(json.dumps(v))
   with self.assertRaisesRegex(ValueError,'insufficient'):mod.measured(path,'M04-024')
 def test_swapped_look_axis_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/'source';shutil.copytree(PACKAGE,path)
   q=path/'binding_spec.json';v=json.loads(q.read_text());v['upAxis']='Z'
   q.write_text(json.dumps(v))
   with self.assertRaisesRegex(ValueError,'unsafe source orientation'):mod.measured(path,'M04-024')
 def test_modified_actual_binary_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/'source';shutil.copytree(PACKAGE,path)
   q=path/'models/M01-018_LEG-WPN-001_R1.glb';q.write_bytes(q.read_bytes()+b'bad')
   with self.assertRaisesRegex(ValueError,'HASH mismatch'):mod.measured(path,'M01-018')
 def test_changed_manifest_sha_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/'source';shutil.copytree(PACKAGE,path)
   q=path/'manifest.json';v=json.loads(q.read_text());v['binaries'][0]['reworkedSha256']='0'*64
   q.write_text(json.dumps(v))
   with self.assertRaisesRegex(ValueError,'not pinned'):mod.measured(path,'M01-018')
 def test_unsafe_hover_exhaust_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   path=Path(d)/'source';shutil.copytree(PACKAGE,path)
   q=path/'binding_spec.json';v=json.loads(q.read_text());v['mounts'][1]['exhaustAxis']=[0,1,0]
   q.write_text(json.dumps(v))
   with self.assertRaisesRegex(ValueError,'exhaust axis'):mod.measured(path,'M04-024')
 def test_machine_json_is_not_approval(self):
  with tempfile.TemporaryDirectory() as d:
   file=Path(d)/'options.json';r=mod.run(PACKAGE,file)
   self.assertTrue(file.exists());self.assertFalse(r['approvedToMount'])
   self.assertEqual(len(r['models']),2)
   self.assertTrue(all(m['decision']=='PREVIEW_PROPOSALS_ONLY' for m in r['models']))
if __name__=='__main__':unittest.main(verbosity=2)
