"""B01-30 pass3 technical-art image-annotation controls; not CAD/Unity tests."""
import importlib.util,os,tempfile,unittest,copy
from pathlib import Path
from PIL import Image
P=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('tech',P/'tech_art_hypotheses.py')
t=importlib.util.module_from_spec(s);s.loader.exec_module(t)
SOURCE=Path(os.environ.get('B01_CM001_RENDER_DIR',P/'SOURCE_UNMOUNTED'))
class PreselectionTechArt(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not all((SOURCE/(name+'.png')).is_file() for name in t.INPUTS):
   raise unittest.SkipTest('Real producer original PNG archive missing')
  cls.tmp=tempfile.TemporaryDirectory()
  cls.r=t.sheet(SOURCE,Path(cls.tmp.name))
 @classmethod
 def tearDownClass(cls):
  if hasattr(cls,'tmp'):cls.tmp.cleanup()
 def test_01_3_actual_art_source_sentinels(self):
  self.assertEqual(len(t.INPUTS),3)
  self.assertTrue(t.validate())
 def test_02_current_producer_source_sha(self):
  self.assertEqual(self.r['producerPR294Head'],'3efd76bb2eaa319282a51ad588f8f4176d0cc33b')
 def test_03_all_unknowns_not_measured(self):
  self.assertTrue(all(v in ('UNKNOWN_NOT_MEASURED','NOT_RUN','PENDING','SOURCE_2D_ONLY',False) for v in t.STATE.values()))
 def test_04_no_model_or_tech_card_approved(self):
  self.assertFalse(self.r['engineeringStates']['realModelAuthorized'])
  self.assertFalse(self.r['engineeringStates']['technicalArtCardApproved'])
 def test_05_hq_choice_pending(self):
  self.assertEqual(self.r['engineeringStates']['HQSelected'],'PENDING')
 def test_06_no_typed_socket(self):
  self.assertEqual(self.r['engineeringStates']['allTypedSockets'],'UNKNOWN_NOT_MEASURED')
 def test_07_no_real_axisXYZ(self):
  self.assertEqual(self.r['engineeringStates']['allUnityAxisDirections'],'UNKNOWN_NOT_MEASURED')
 def test_08_all_2d_only_markers_count(self):
  self.assertTrue(all(len(r['imageMarkers'])==4 for r in self.r['variants']))
 def test_09_all_markers_no_world_pivot(self):
  self.assertTrue(all(m['worldPivotXYZ']=='UNKNOWN_NOT_MEASURED'
  for v in self.r['variants'] for m in v['imageMarkers']))
 def test_10_divide_assemblies_per_option(self):
  self.assertTrue(all(len(x['candidateBreakdown'])>=3 for x in self.r['variants']))
 def test_11_every_option_has_3_animation_pivot_risks(self):
  self.assertTrue(all(len(x['axisQuestions'])>=3 for x in self.r['variants']))
 def test_12_2d_pixel_marker_in_frame(self):
  self.assertTrue(all(0<=m['screenPxOnSheet'][0]<=1600 and 0<=m['screenPxOnSheet'][1]<=1320
   for x in self.r['variants'] for m in x['imageMarkers']))
 def test_13_output_true_1600x1320(self):
  with Image.open(Path(self.tmp.name)/'CM001_TECH_ART_PRESELECTION_PIVOTS_UNKNOWN.png') as im:
   self.assertEqual(im.size,(1600,1320))
 def test_14_output_receipt_sha_actual(self):
  from hashlib import sha256
  self.assertEqual(self.r['imageSha256'],sha256((Path(self.tmp.name)/'CM001_TECH_ART_PRESELECTION_PIVOTS_UNKNOWN.png').read_bytes()).hexdigest())
 def test_15_mutated_art_binary_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   f=Path(d)/'A-wheel.png';f.write_bytes((SOURCE/'A-wheel.png').read_bytes()+b'not-approved')
   with self.assertRaisesRegex(ValueError,'PINNED_SOURCE_SHA256_MISMATCH'):
    t.load(f,'A-wheel')
 def test_16_forged_authority_rejected(self):
  a=t.STATE['realModelAuthorized'];t.STATE['realModelAuthorized']=True
  try:
   with self.assertRaisesRegex(ValueError,'AUTHORITY_FORGERY'):t.validate()
  finally:t.STATE['realModelAuthorized']=a
 def test_17_forged_3d_world_coordinate_rejected(self):
  a=t.STATE['allTypedSockets'];t.STATE['allTypedSockets']=[0,1,2]
  try:
   with self.assertRaisesRegex(ValueError,'PHYSICAL_GROUND_TRUTH_FORGERY'):t.validate()
  finally:t.STATE['allTypedSockets']=a
 def test_18_forged_world_3d_pivot_not_used(self):
  a=t.OPTIONS['A-wheel']['markers'][0]
  t.OPTIONS['A-wheel']['markers'][0]=(-2,.2,a[2])
  try:
   with self.assertRaisesRegex(ValueError,'NOT_A_SAFE_SCREEN_POINT'):t.validate()
  finally:t.OPTIONS['A-wheel']['markers'][0]=a
 def test_19_real_user_study_not_run(self):
  self.assertEqual(self.r['engineeringStates']['allRealUsers'],'NOT_RUN')
 def test_20_native_engine_not_run(self):
  self.assertEqual(self.r['engineeringStates']['unityNative'],'NOT_RUN')
if __name__=='__main__':unittest.main(verbosity=2)
