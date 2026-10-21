import sys, unittest, tempfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import check_results

class ResultsTests(unittest.TestCase):
    def check(self,content,code=0):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'results.xml'; p.write_text(content)
            return check_results.validate(p,code)
    def test_executed_pass(self):
        self.assertEqual(self.check('<test-run result="Passed" total="1" passed="1" failed="0" skipped="0"><test-case result="Passed"/></test-run>')['passed'],1)
    def test_empty_suite_rejected(self):
        with self.assertRaises(ValueError): self.check('<test-run result="Passed" total="0" passed="0" failed="0"/>')
    def test_nonzero_native_exit_rejected(self):
        with self.assertRaises(ValueError): self.check('<test-run result="Passed"><test-case result="Passed"/></test-run>',3)
    def test_failure_cannot_hide_in_aggregate(self):
        with self.assertRaises(ValueError): self.check('<test-run result="Passed"><test-case result="Failed"/></test-run>')
    def test_skipped_is_not_pass(self):
        with self.assertRaises(ValueError): self.check('<test-run result="Passed"><test-case result="Skipped"/></test-run>')
    def test_count_mismatch_rejected(self):
        with self.assertRaises(ValueError): self.check('<test-run result="Passed" total="2" passed="2"><test-case result="Passed"/></test-run>')

if __name__=='__main__': unittest.main()
