#!/usr/bin/env python3
"""18 adversarial controls of original selected #596 immutable source CAS proof."""
import copy
from pathlib import Path
import sys
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parent))
import source_guard as g

REPO=Path(__file__).resolve().parents[4]
SOURCE=(REPO/(g.BASE+"Program.cs")).read_text()
CSP=(REPO/(g.BASE+"Original596ParallelCheckpointQA.csproj")).read_text()

class SourceOnlyRealityCheck(unittest.TestCase):
    def fails(self,source,why):
        with self.assertRaisesRegex(g.Refuse,why):g.check_runner_source(source)
    def test_01_real_original_concurrency_source_complete(self):
        self.assertEqual(g.check_runner_source(SOURCE),13)
        print("ACTUAL_596_SOURCE_DIRECTLY_COMPILED_MULTI_PROCESS_CAS_READY_G1_NOT_RUN")
    def test_02_original_producer_imports_and_compile_strict(self):
        g.check_csproj(CSP)
    def test_03_exact_selected_and_frozen_shas(self):
        self.assertEqual(g.SELECTED,"efb2b06eafedab5469b57f163152d96aca3c5915")
        self.assertEqual(g.GAME,"d94400d95d13db7f27e1533df05c9292d5380431")
        self.assertEqual(g.SAVE,"33094f154c7b91119b36d58aab285e6e83c5de57")
    def test_04_all_changes_additive_ci41_only(self):
        self.assertEqual(len(g.OWNED),7)
        self.assertTrue(all(p.startswith(g.BASE) or
                            p==".github/workflows/b01-41-p17w18-596-real-concurrent-cas.yml"
                            for p in g.OWNED))
    def test_05_reject_fake_task_only_concurrency(self):
        self.fails(SOURCE.replace('Process.Start(info)','FakeTask.Start(info)'),
                   "MISSING_REAL_CONCURRENT")
    def test_06_reject_fake_or_removed_real_disk_commit(self):
        self.fails(SOURCE.replace('new FileCheckpointStore(file).Commit(run,0,"",1,hash)',
                                  'true'),"MISSING_REAL_CONCURRENT")
    def test_07_reject_one_process_sequential_workflow(self):
        self.fails(SOURCE.replace('File.WriteAllText(start,"GO")','Thread.Sleep(100)'),
                   "MISSING_REAL_CONCURRENT")
    def test_08_reject_serial_process_waits(self):
        self.fails(SOURCE.replace("a.WaitForExit(25000)&&b.WaitForExit(25000)",
                                  "a.WaitForExit(25000)"),"MISSING_REAL_CONCURRENT")
    def test_09_reject_two_winners_acceptable(self):
        self.fails(SOURCE.replace('(ra=="COMMITTED") != (rb=="COMMITTED")',
                                  '(ra=="COMMITTED") || (rb=="COMMITTED")'),
                   "MISSING_REAL_CONCURRENT")
    def test_10_reject_no_real_24_children(self):
        self.fails(SOURCE.replace("childProcesses==24","childProcesses==0"),
                   "MISSING_REAL_CONCURRENT")
    def test_11_reject_smaller_stress_set(self):
        self.fails(SOURCE.replace("i<12","i<2"),"MISSING_REAL_CONCURRENT")
    def test_12_reject_nonstrict_lock(self):
        self.fails(SOURCE.replace("FileShare.None","FileShare.ReadWrite"),
                   "MISSING_REAL_CONCURRENT")
    def test_13_reject_shared_user_data_access(self):
        self.fails(SOURCE.replace('Path.GetTempPath()',
                       'Application.persistentDataPath'),"UNAUTHORIZED_SHARED_CHECKPOINT")
    def test_14_reject_copied_source_alternative(self):
        self.fails(SOURCE+"\nclass FileCheckpointStore {}", "FORBIDDEN_COPIED_SOURCE")
    def test_15_reject_unsafe_global_cleanup(self):
        self.fails(SOURCE.replace("Directory.Delete(root,true)",
                                  "Directory.Delete(Path.GetTempPath(),true)"),
                   "MISSING_REAL_CONCURRENT")
    def test_16_reject_omitted_warnings_as_errors(self):
        with self.assertRaisesRegex(g.Refuse,"ORIGINAL_COMPILER_WARNINGS_NOT_STRICT"):
            g.check_csproj(CSP.replace("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",
                                      "<TreatWarningsAsErrors>false</TreatWarningsAsErrors>"))
    def test_17_reject_foreign_unselected_source_replacement(self):
        with self.assertRaisesRegex(g.Refuse,"MISSING_ORIGINAL_COMPILE_REFERENCE"):
            g.check_csproj(CSP.replace("Assets/B0123UnitySaveBridge001/Core/AtomicWorldSwap.cs",
                                      "Assets/B0123UnitySaveBridge001/Core/CheckpointFence.cs"))
    def test_18_reject_copied_codec_instead_of_frozen_save(self):
        with self.assertRaisesRegex(g.Refuse,"MISSING_ORIGINAL_COMPILE_REFERENCE"):
            g.check_csproj(CSP.replace("Assets/B0123DetachedSaveContract001/Runtime/DetachedSnapshotContract.cs",
                                      "Assets/B0123UnitySaveBridge001/Runtime/FakeCodec.cs"))

if __name__=="__main__":unittest.main(verbosity=2)
