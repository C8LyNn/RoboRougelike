"""Test actual newly fixed B01-31 blind PNG files, never simulated optical recognition."""
import importlib.util,os,tempfile,unittest
from pathlib import Path
S=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('blind',S/'blind_reference_free_qa.py')
q=importlib.util.module_from_spec(spec);spec.loader.exec_module(q)
SOURCE=Path(os.environ.get('B01_CM001_BLIND_DIR',S/'NO_REAL_PRODUCER_ART'))
class B01SourceAfterReferenceRemoval(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not (SOURCE/'C-omni-silhouette-64px.png').is_file():
   raise unittest.SkipTest('new corrected original B01-31 PNG artifact not available')
  cls.r=q.compute(SOURCE)
 def test_01_exact_real_B0131_head(self):
  self.assertEqual(self.r['inputProducerPR294Head'],'7a1416d3e10ee720d9d28931a3c268f95e2bba15')
 def test_02_all_six_true_raster_hashes(self):
  self.assertEqual(len(self.r['rows']),6)
  for row in self.r['rows']:
   self.assertEqual(q.sha(SOURCE/f"{row['variant']}-silhouette-{row['sizePx']}px.png"),row['referenceFreeSourceSha256'])
 def test_03_source_B0131_reference_free_not_historical(self):
  self.assertTrue(self.r['notComparableWithPriorFullCardLegacyCounts'])
 def test_04_all_nonempty_art_pixels(self):
  self.assertTrue(all(x['brightPixelCount']>20 for x in self.r['rows']))
 def test_05_all_four_edges_tested(self):
  self.assertTrue(all(set(v['retainedFractionBy25pctEdge'])=={'left','right','top','bottom'} for v in self.r['rows']))
 def test_06_fraction_bounds(self):
  self.assertTrue(all(0<x['worstRetainedFraction']<=1 for x in self.r['rows']))
 def test_07_forgery_of_SVG_thumbnail_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/'A-wheel-silhouette-64px.png'
   p.write_bytes((SOURCE/p.name).read_bytes()+b'forged-tail')
   with self.assertRaisesRegex(ValueError,'SOURCE_SHA_DRIFT'):
    q.sample(d,'A-wheel',64)
 def test_08_unrecognized_direction_rejected(self):
  with self.assertRaisesRegex(ValueError,'INVALID_COVER_DIRECTION'):
   q.cover(q.sample(SOURCE,'B-track',64),'diagonal')
 def test_09_no_human_inference(self):
  self.assertEqual(self.r['humanRecognition'],'NOT_RUN')
 def test_10_no_engine_or_socket_authorization(self):
  self.assertEqual(self.r['UnityNative'],'NOT_RUN')
  self.assertEqual(self.r['physicalSocketAndPivot'],'UNKNOWN')
 def test_11_not_forged_HQ_choice(self):
  self.assertEqual(self.r['decisionHQ'],'PENDING')
 def test_12_source_fixture_does_not_prove_hover(self):
  self.assertIn('C-omni',{x['variant'] for x in self.r['rows']})
  self.assertTrue(self.r['notComparableWithPriorFullCardLegacyCounts'])
if __name__=='__main__':unittest.main(verbosity=2)
