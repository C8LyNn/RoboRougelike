"""Real 3-source raster stress: run with B01_CM001_RENDER_DIR set to original producer artifact."""
import hashlib,importlib.util,os,tempfile,unittest
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('qa',ROOT/'silhouette_stress.py')
q=importlib.util.module_from_spec(spec);spec.loader.exec_module(q)
INPUT=Path(os.environ.get('B01_CM001_RENDER_DIR',ROOT/'NO_SOURCE'))
class OriginalCM001Stress(unittest.TestCase):
 @classmethod
 def setUpClass(cls):
  if not all((INPUT/(n+'.png')).is_file() for n in q.INPUTS):
   raise unittest.SkipTest('actual published original B01-31 PNGs not mounted')
  cls.tmp=tempfile.TemporaryDirectory()
  cls.path=Path(cls.tmp.name)
  cls.r=q.build(INPUT,cls.path)
 @classmethod
 def tearDownClass(cls):
  if hasattr(cls,'tmp'):cls.tmp.cleanup()
 def test01_original_per_file_pins(self):
  for n,s in q.INPUTS.items():self.assertEqual(q.sha(INPUT/(n+'.png')),s)
 def test02_current_source_concept_head(self):
  self.assertEqual(self.r['inputProducerPR294Head'],'3efd76bb2eaa319282a51ad588f8f4176d0cc33b')
 def test03_all_source_png_700_866(self):
  for n in q.INPUTS:
   with Image.open(INPUT/(n+'.png')) as im:self.assertEqual(im.size,(700,866))
 def test04_current_producer_crop_equal(self):self.assertEqual(q.CROP,(142,264,620,529))
 def test05_all_twelve_64_128_both_modes(self):
  for n in q.INPUTS:
   for px in q.SCALES:
    for mode in ('gray','color'):
     with Image.open(self.path/f'{n}-{px}-{mode}.png') as im:self.assertEqual(im.size,(px,px))
 def test06_all_grayscale_pixels_equal_channels(self):
  for n in q.INPUTS:
   for px in q.SCALES:
    with Image.open(self.path/f'{n}-{px}-gray.png') as im:
     self.assertTrue(all(r==g==b for r,g,b in im.getdata()))
 def test07_each_real_art_option_not_blank(self):
  for n in q.INPUTS:self.assertGreater(self.r['results'][n]['scales']['64px-color']['brightPixels'],80)
 def test08_same_crop_all(self):
  self.assertTrue(all(x['cropXYXY']==list(q.CROP) for x in self.r['results'].values()))
 def test09_distinct_all_three_colors(self):
  self.assertEqual(len({self.r['results'][n]['scales']['64px-color']['sha256'] for n in q.INPUTS}),3)
 def test10_stress_four_edges_at_two_sizes(self):
  for n in q.INPUTS:
   for px in (64,128):
    case=self.r['results'][n]['edge25pct'][str(px)+'px']
    self.assertEqual(set(case['bySide']),{'left','right','top','bottom'})
    self.assertGreater(case['worstRatio'],0)
    self.assertLess(case['worstRatio'],1)
 def test11_right_occlusion_exact_25pct_pixels(self):
  im=q.view(q.load(INPUT/'A-wheel.png','A-wheel'),64,True)
  masked=q.cover(im,'right')
  for y in range(64):
   for x in range(48,64):self.assertEqual(masked.getpixel((x,y)),q.BG)
 def test12_real_image_mosaic_size(self):
  with Image.open(self.path/'comparison_64_128.png') as im:self.assertEqual(im.size,(882,650))
 def test13_no_false_user_test(self):self.assertEqual(self.r['humanUserStudy'],'NOT_RUN')
 def test14_no_false_native_unity(self):self.assertEqual(self.r['Unity'],'NOT_RUN')
 def test15_physical_geometry_unknown(self):self.assertEqual(self.r['allMountSocketsAndClearances'],'UNKNOWN')
 def test16_hq_decision_pending(self):self.assertEqual(self.r['HQdecision'],'PENDING')
 def test17_forged_published_png_fails_closed(self):
  with tempfile.TemporaryDirectory() as d:
   p=Path(d)/'A-wheel.png'
   p.write_bytes((INPUT/'A-wheel.png').read_bytes()+b'bad')
   with self.assertRaisesRegex(ValueError,'PINNED_SOURCE_SHA256_MISMATCH'):q.load(p,'A-wheel')
 def test18_no_arbitrary_256px_pass(self):
  with self.assertRaisesRegex(ValueError,'UNSUPPORTED_SIZE'):q.view(q.load(INPUT/'A-wheel.png','A-wheel'),256)
 def test19_no_diagonal_occlusion_accepted(self):
  with self.assertRaisesRegex(ValueError,'UNSUPPORTED_OCCLUSION_SIDE'):q.cover(Image.new('RGB',(64,64),q.BG),'diagonal')
 def test20_deterministic_second_run(self):
  with tempfile.TemporaryDirectory() as d:
   other=Path(d);q.build(INPUT,other)
   for filename in self.path.glob('*.png'):
    self.assertEqual(q.sha(filename),q.sha(other/filename.name),filename.name)
if __name__=='__main__':unittest.main(verbosity=2)
