"""Synthetic-only schema negatives for B01-50 LIVE frame recorder. Not real FPS."""
import csv,json,pathlib,sys,tempfile,unittest
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]))
from validate_live import CaptureError,audit
GAME="a"*40
BUILD="b"*64
CASE="SYNTHETIC_ONLY"
def csvwrite(path,data):
    with path.open("w",newline="") as f:
        w=csv.DictWriter(f,fieldnames=list(data[0]))
        w.writeheader();w.writerows(data)
class LiveIntake(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.folder=pathlib.Path(self.tmp.name)
        self.frames=[dict(frame_index=i,elapsed_ms=i*100,delta_ms=100) for i in range(1200)]
        self.scans=[dict(elapsed_ms=i*1000,scene="SyntheticTest",active_parts=24,
            active_projectiles=1,active_enemies=2,rigidbodies=8,scan_ms=.1) for i in range(120)]
        self.meta=dict(
            classification="GAME_FRAME_TIME_REVIEW_REQUIRED_NOT_DISPLAY_PRESENTMON_OR_NATIVE_PERF_PASS",
            game_sha=GAME,build_zip_sha256=BUILD,scenario_id=CASE,unity_version="6000.3.14f1",
            observed_capture_seconds=120,capture_complete=True,frame_rows=1200,
            object_scans=120,profiler_main_render_gpu_gc="NOT_MEASURED")
        self.save()
    def tearDown(self):self.tmp.cleanup()
    def save(self):
        csvwrite(self.folder/"frames.csv",self.frames)
        csvwrite(self.folder/"objects.csv",self.scans)
        (self.folder/"receipt.json").write_text(json.dumps(self.meta))
    def runaudit(self):return audit(self.folder,GAME,BUILD,CASE)
    def test_positive_structural_not_native_pass(self):
        self.assertEqual(self.runaudit()["status"],"LIVE_RAW_REVIEW_REQUIRED_NOT_PERFORMANCE_ACCEPTANCE")
    def test_sha(self):
        with self.assertRaisesRegex(CaptureError,"game SHA"):audit(self.folder,"c"*40,BUILD,CASE)
    def test_incomplete(self):
        self.meta["capture_complete"]=False;self.save()
        with self.assertRaisesRegex(CaptureError,"incomplete"):self.runaudit()
    def test_duration(self):
        self.meta["observed_capture_seconds"]=119;self.save()
        with self.assertRaisesRegex(CaptureError,"duration"):self.runaudit()
    def test_frame_missing(self):
        self.frames.pop();self.save()
        with self.assertRaisesRegex(CaptureError,"frame row"):self.runaudit()
    def test_time_duplicate(self):
        self.frames[40]["elapsed_ms"]=self.frames[39]["elapsed_ms"];self.save()
        with self.assertRaisesRegex(CaptureError,"nonmonotonic"):self.runaudit()
    def test_negative_parts(self):
        self.scans[10]["active_parts"]=-5;self.save()
        with self.assertRaisesRegex(CaptureError,"invalid object count"):self.runaudit()
    def test_fake_gpu_marker(self):
        self.meta["profiler_main_render_gpu_gc"]="12";self.save()
        with self.assertRaisesRegex(CaptureError,"fake Unity"):self.runaudit()
    def test_scan_count(self):
        self.scans=self.scans[:89];self.meta["object_scans"]=89;self.save()
        with self.assertRaisesRegex(CaptureError,"insufficient object scans"):self.runaudit()
    def test_extra_csv_column(self):
        with (self.folder/"frames.csv").open("a") as f:f.write("x,10,2,EXTRA\n")
        with self.assertRaisesRegex(CaptureError,"malformed CSV"):self.runaudit()
if __name__=="__main__":unittest.main()
