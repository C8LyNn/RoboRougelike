"""B01-50 PERFSCALING001 schema tests. ALL numeric rows are synthetic, NOT PhysX traces."""
import csv
import json
import pathlib
import sys
import tempfile
import unittest
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]))
from validate_receipt import audit, EXPECTED, quantile, ReceiptError
SOURCE="a2ccd055070a963805cba0779b79f18489f4794f"
FIXTURE="1234567890123456789012345678901234567890"  # test-only sentinel

def csvout(path, records):
    with path.open("w",newline="") as f:
        writer=csv.DictWriter(f,fieldnames=list(records[0]))
        writer.writeheader();writer.writerows(records)

def generate(path):
    timing=[];stats=[]
    for case,(p,pro,e) in EXPECTED.items():
        for rep in range(1,4):
            times=[]
            for step in range(200):
                ms=1+step*.001+rep*.01
                timing.append(dict(case_id=case,repetition=rep,step=step,simulate_ms=ms))
                times.append(ms)
            stats.append(dict(case_id=case,repetition=rep,parts=p,projectiles=pro,
                enemy_proxies=e,compound_colliders=1+p+6*e+pro,rigidbodies=1+e+pro,
                p50_ms=quantile(times,.5),p95_ms=quantile(times,.95),
                p99_ms=quantile(times,.99),mean_ms=sum(times)/len(times),
                managed_alloc_bytes=0,gc0_collections=0))
    receipt=dict(classification="NATIVE_PHYSICS_PROXY_CAPTURE_REVIEW_REQUIRED_NOT_GAME_FPS",
        source_sha=SOURCE,fixture_sha=FIXTURE,unity_version="6000.3.14f1",
        os="SYNTHETIC_NOT_UNITY",cpu="TEST_ONLY",graphics="TEST_ONLY",
        cases=8,repetitions=3,measured_steps=200,warmup_steps=30,fixed_delta_s=.02,
        native_player_fps="NOT_MEASURED",real_enemy_ai="NOT_MEASURED",
        real_r4_projectiles="NOT_MEASURED",gpu_main_render="NOT_MEASURED")
    csvout(path/"simulations.csv",timing)
    csvout(path/"summary.csv",stats)
    (path/"receipt.json").write_text(json.dumps(receipt))
    return timing,stats,receipt

class Intake(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.dir=pathlib.Path(self.tmp.name)
        self.samples,self.stats,self.receipt=generate(self.dir)
    def tearDown(self): self.tmp.cleanup()
    def receiptwrite(self): (self.dir/"receipt.json").write_text(json.dumps(self.receipt))
    def test_correct_structure_is_not_native_pass(self):
        o=audit(self.dir,SOURCE,FIXTURE)
        self.assertEqual(o["status"],"STRUCTURAL_REVIEW_REQUIRED_NOT_NATIVE_ACCEPTANCE")
        self.assertEqual(o["claimed_proxy_physics_steps"],4800)
    def test_wrong_game_commit(self):
        with self.assertRaisesRegex(ReceiptError,"source SHA mismatch"):audit(self.dir,"f"*40,FIXTURE)
    def test_wrong_fixture_commit(self):
        with self.assertRaisesRegex(ReceiptError,"fixture SHA mismatch"):audit(self.dir,SOURCE,"f"*40)
    def test_fake_game_fps(self):
        self.receipt["native_player_fps"]="60";self.receiptwrite()
        with self.assertRaisesRegex(ReceiptError,"false game metric"):audit(self.dir,SOURCE,FIXTURE)
    def test_missing_step(self):
        csvout(self.dir/"simulations.csv",self.samples[1:])
        with self.assertRaisesRegex(ReceiptError,"missing step"):audit(self.dir,SOURCE,FIXTURE)
    def test_duplicate_step(self):
        csvout(self.dir/"simulations.csv",self.samples+[self.samples[0]])
        with self.assertRaisesRegex(ReceiptError,"duplicate step"):audit(self.dir,SOURCE,FIXTURE)
    def test_wrong_99th_percentile(self):
        self.stats[0]["p99_ms"]+=5
        csvout(self.dir/"summary.csv",self.stats)
        with self.assertRaisesRegex(ReceiptError,"wrong calculated p99"):audit(self.dir,SOURCE,FIXTURE)
    def test_wrong_count(self):
        self.stats[0]["rigidbodies"]+=1
        csvout(self.dir/"summary.csv",self.stats)
        with self.assertRaisesRegex(ReceiptError,"wrong object count"):audit(self.dir,SOURCE,FIXTURE)
    def test_extra_csv_field(self):
        with (self.dir/"summary.csv").open("a") as f:f.write("wrong,1,1,2,3,4,5,6,7,8,9,0,0,EXTRA\n")
        with self.assertRaisesRegex(ReceiptError,"extra or missing CSV cell"):audit(self.dir,SOURCE,FIXTURE)
    def test_missing_host(self):
        self.receipt["cpu"]="";self.receiptwrite()
        with self.assertRaisesRegex(ReceiptError,"no host evidence"):audit(self.dir,SOURCE,FIXTURE)
    def test_absent_summary(self):
        csvout(self.dir/"summary.csv",self.stats[:-1])
        with self.assertRaisesRegex(ReceiptError,"missing summary"):audit(self.dir,SOURCE,FIXTURE)
    def test_quantile_interpolation(self):
        self.assertEqual(quantile([1,2,3,4,5],.5),3)
        self.assertAlmostEqual(quantile([1,2,3,4,5],.95),4.8)

if __name__=="__main__": unittest.main()
