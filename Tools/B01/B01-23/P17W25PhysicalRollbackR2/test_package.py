import io,json,unittest,zipfile
import package as p
def tamper(raw,edit=None):
 with zipfile.ZipFile(io.BytesIO(raw)) as z:members={n:z.read(n) for n in z.namelist()}
 members.update(edit or {});out=io.BytesIO()
 with zipfile.ZipFile(out,"w") as z:
  for n in sorted(members):z.writestr(p.info(n),members[n])
 return out.getvalue()
class StrictSource(unittest.TestCase):
 def test_exact_parent_and_three_frozen_original_blobs(self):p.check()
 def test_real_4_authored_Unity_PhysX_tests_still_NOT_RUN(self):
  p.check();self.assertEqual(p.verify(p.build())["native_unity"],"NOT_RUN")
 def test_9_unique_original_W24_meta_not_replaced(self):
  self.assertEqual(len([x for x in p.paths() if x.endswith(".meta")]),9)
 def test_deterministic_git_original_source_zip(self):
  self.assertEqual(p.build(),p.build())
 def test_dirty_original_source_rejected(self):
  with self.assertRaises(ValueError):
   p.verify(tamper(p.build(),{p.paths()[0]:b"ATTACK"}))
 def test_foreign_member_rejected(self):
  with self.assertRaises(ValueError):p.verify(tamper(p.build(),{"../../foreign":b"a"}))
 def test_fake_native_Editor_result_is_rejected(self):
  with zipfile.ZipFile(io.BytesIO(p.build())) as z:d=json.loads(z.read(p.MANIFEST))
  d["native_unity"]="PASS"
  with self.assertRaises(ValueError):
   p.verify(tamper(p.build(),{p.MANIFEST:json.dumps(d).encode()}))
if __name__=="__main__":unittest.main(verbosity=2)
