import json, struct, tempfile, unittest, zlib
from pathlib import Path
import qa_native as q
S="a"*40;T="b"*40
def xml(names=("A",), failed=0):
 cases="".join(f'<test-case name="{n}" result="{"Failed" if i<failed else "Passed"}"/>' for i,n in enumerate(names))
 return f'<test-run result="{"Failed(Child)" if failed else "Passed"}" total="{len(names)}" passed="{len(names)-failed}" failed="{failed}" skipped="0">{cases}</test-run>'
def png(w=1280,h=720):
 def chunk(k,d):return struct.pack(">I",len(d))+k+d+struct.pack(">I",zlib.crc32(k+d)&0xffffffff)
 raw=(b"\x00"+b"\x01\x02\x03"*w)*h
 return b"\x89PNG\r\n\x1a\n"+chunk(b"IHDR",struct.pack(">IIBBBBB",w,h,8,2,0,0,0))+chunk(b"IDAT",zlib.compress(raw))+chunk(b"IEND",b"")
class QA(unittest.TestCase):
 def setUp(self):
  self.t=tempfile.TemporaryDirectory();self.root=Path(self.t.name)
  self.plan=dict(source_sha=S,target_sha=T,required_stages=["Compile","EditMode"],required_nunit={"EditMode":["A"]})
 def tearDown(self):self.t.cleanup()
 def stage(self,name="EditMode",code=0,log=True,xmlcontent=None):
  d=self.root/name;d.mkdir(exist_ok=True)
  (d/"process.json").write_text(json.dumps(dict(exit_code=code,source_sha=S,target_sha=T,unity_version="6000.3.14f1")))
  if log:(d/("Player.log" if name.startswith("Player") else "Editor.log")).write_text("real log fixture")
  if name in ("EditMode","PlayMode"):(d/"results.xml").write_text(xml() if xmlcontent is None else xmlcontent)
  return d
 def result(self,name="EditMode"):return next(x for x in q.check(self.plan,self.root)["stages"] if x["stage"]==name)
 def test_no_native_data_is_not_run(self):self.assertEqual(q.check(self.plan,self.root)["status"],"NOT_RUN")
 def test_native_zero_and_log(self):self.stage("Compile");self.assertEqual(self.result("Compile")["status"],"PASS")
 def test_nonzero_native_exit_fails(self):self.stage("Compile",1);self.assertEqual(self.result("Compile")["status"],"FAIL")
 def test_missing_log_fails(self):self.stage("Compile",log=False);self.assertEqual(self.result("Compile")["status"],"FAIL")
 def test_valid_nunit_passes(self):self.stage();self.assertEqual(self.result()["status"],"PASS")
 def test_wrong_nunit_case_fails(self):self.stage(xmlcontent=xml(("Other",)));self.assertEqual(self.result()["status"],"FAIL")
 def test_one_of_twelve_missing_fails(self):self.plan["required_nunit"]["EditMode"]=["A"]+[f"Cube{i}" for i in range(11)];self.stage();self.assertEqual(self.result()["status"],"FAIL")
 def test_forged_nunit_count_fails(self):self.stage(xmlcontent=xml().replace('total="1"','total="12"'));self.assertEqual(self.result()["status"],"FAIL")
 def test_wrong_sha_fails(self):
  d=self.stage();p=d/"process.json";a=json.loads(p.read_text());a["source_sha"]="c"*40;p.write_text(json.dumps(a));self.assertEqual(self.result()["status"],"FAIL")
 def test_png_crc_and_resolution(self):
  p=self.root/"a.png";p.write_bytes(png());self.assertTrue(q.png(p,1280,720));self.assertFalse(q.png(p,1920,1080))
  b=bytearray(p.read_bytes());b[-6]^=1;p.write_bytes(b);self.assertFalse(q.png(p,1280,720))
 def test_missing_camera_functional_case_fails(self):
  self.plan.update(required_stages=["Player720"],screenshots={"Player720":["a.png"]},required_functional={"Player720":["CAM-PITCH"]})
  d=self.stage("Player720");(d/"a.png").write_bytes(png());(d/"telemetry.json").write_text(json.dumps({"source_sha":S,"cases":[]}))
  self.assertEqual(self.result("Player720")["status"],"FAIL")
 def test_camera_own_hits_fail(self):
  self.plan.update(required_stages=["Player720"],screenshots={"Player720":["a.png"]},required_functional={"Player720":["CAM-PITCH"]})
  d=self.stage("Player720");(d/"a.png").write_bytes(png())
  (d/"telemetry.json").write_text(json.dumps({"source_sha":S,"cases":[{"id":"CAM-PITCH","status":"PASS","actualOwnRayHits":1,"worldPenetrationCount":0,"maxContinuousSpeedMps":3}]}))
  self.assertEqual(self.result("Player720")["status"],"FAIL")
 def test_r6_no_wheel_contact_fails(self):
  self.plan.update(required_stages=["Player720"],screenshots={"Player720":["a.png"]},required_functional={"Player720":["R6-Arena-7-Light"]})
  d=self.stage("Player720");(d/"a.png").write_bytes(png())
  (d/"telemetry.json").write_text(json.dumps({"source_sha":S,"cases":[{"id":"R6-Arena-7-Light","status":"PASS","wheelContactSamples":0,"finishReached":True}]}))
  self.assertEqual(self.result("Player720")["status"],"FAIL")
if __name__=="__main__":unittest.main()
