"""Synthetic/archive/receipt adversarial tests only. Never starts Unity or a game."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

import rc1_shutdown_ab as qa


class ShutdownABTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.plan = {"schema": qa.SCHEMA, "unity_version": "6000.3.14f1",
                     "manual_instructions": "Start the menu, open Quit, confirm UI exit.",
                     "order": ["BASE", "EXP", "EXP", "BASE", "RC2", "RC2"], "builds": {}}
        for num, label in enumerate(("BASE", "EXP", "RC2"), 1):
            path = self.root / f"{label}.zip"
            with zipfile.ZipFile(path, "w") as z:
                z.writestr("Game.exe", f"fixture {num}")
                z.writestr("Game_Data/data.unity3d", b"Unity synthetic fixture")
            self.plan["builds"][label] = {"archive": str(path), "sha256": qa.digest(path),
                                           "source_sha": f"{num}" * 40, "exe": "Game.exe"}
        self.plan_file = self.root / "plan.json"
        self.write_plan()
        self.evidence = self.root / "independent-evidence"

    def write_plan(self):
        self.plan_file.write_text(json.dumps(self.plan), encoding="utf-8")

    def prepared(self):
        qa.prepare(self.plan_file, self.evidence)
        return qa.load(self.evidence / "prepared.json")

    def fake_run(self, index, native_exit=0, log="Player finished", timed_out=False):
        prep = qa.load(self.evidence / "prepared.json")
        label = self.plan["order"][index]
        folder = self.evidence / "runs" / f"{index+1:02d}-{label}"
        folder.mkdir(parents=True)
        if log is not None:
            (folder / "Player.log").write_text(log)
        x = None if timed_out else (native_exit & 0xFFFFFFFF)
        category = ("TIMEOUT_KILLED" if timed_out else
                    "ACCESS_VIOLATION_C0000005" if x == qa.AV_CODE else
                    "CLEAN_EXIT_ZERO" if x == 0 else "NONZERO_OTHER")
        rec = {"schema": qa.SCHEMA, "index": index, "label": label,
               "plan_sha256": prep["plan_sha256"],
               "exe_sha256": prep["exe_sha256"][label],
               "native_exit_signed": None if timed_out else native_exit,
               "native_exit_unsigned": x,
               "native_exit_hex": None if x is None else f"0x{x:08X}",
               "timed_out": timed_out, "classification": category,
               "player_log_sha256": qa.digest(folder / "Player.log") if log is not None and log else None,
               "player_log_bytes": len(log) if log is not None else 0,
               "operator_note": "I clicked the Quit button in GUI"}
        (folder / "receipt.json").write_text(json.dumps(rec))
        return folder / "receipt.json"

    def test_plan_accepts_immutable_three_way_comparison(self):
        self.assertEqual(qa.verify_plan(self.plan)["order"][0], "BASE")

    def test_bad_order_rejected(self):
        self.plan["order"].reverse()
        with self.assertRaisesRegex(ValueError, "ORDER"):
            qa.verify_plan(self.plan)

    def test_missing_build_rejected(self):
        del self.plan["builds"]["EXP"]
        with self.assertRaisesRegex(ValueError, "THREE_PINNED"):
            qa.verify_plan(self.plan)

    def test_duplicate_sha_rejected(self):
        self.plan["builds"]["RC2"]["sha256"] = self.plan["builds"]["BASE"]["sha256"]
        with self.assertRaisesRegex(ValueError, "SAME_ARCHIVE"):
            qa.verify_plan(self.plan)

    def test_invalid_pin_rejected(self):
        self.plan["builds"]["BASE"]["source_sha"] = "unfrozen"
        with self.assertRaisesRegex(ValueError, "BAD_SOURCE_SHA"):
            qa.verify_plan(self.plan)

    def test_missing_manual_instructions_rejected(self):
        self.plan["manual_instructions"] = "quit"
        with self.assertRaisesRegex(ValueError, "MANUAL_INSTRUCTIONS"):
            qa.verify_plan(self.plan)

    def test_valid_archive_extraction_and_sha(self):
        prep = self.prepared()
        self.assertEqual(len(prep["exe_sha256"]), 3)
        self.assertTrue((self.evidence / "extracted" / "RC2" / "Game.exe").exists())

    def test_archive_hash_mismatch_no_side_effects(self):
        self.plan["builds"]["EXP"]["sha256"] = "0" * 64
        self.write_plan()
        with self.assertRaisesRegex(ValueError, "ARCHIVE_SHA_MISMATCH"):
            qa.prepare(self.plan_file, self.evidence)
        self.assertFalse(self.evidence.exists())

    def test_path_traversal_zip_rejected(self):
        self.replace_zip("BASE", [("../escape.txt", b"danger"), ("Game.exe", b"fixture")])
        with self.assertRaisesRegex(ValueError, "UNSAFE_ZIP_PATH"):
            qa.prepare(self.plan_file, self.evidence)
        self.assertFalse((self.root / "escape.txt").exists())

    def test_dot_segment_zip_rejected(self):
        self.replace_zip("BASE", [("a/./b.txt", b"danger"), ("Game.exe", b"fixture")])
        with self.assertRaisesRegex(ValueError, "UNSAFE_ZIP_PATH"):
            qa.prepare(self.plan_file, self.evidence)

    def test_windows_backslash_zip_rejected(self):
        self.replace_zip("BASE", [("sub\\Game.exe", b"fixture"), ("Game.exe", b"fixture")])
        with self.assertRaisesRegex(ValueError, "UNSAFE_ZIP_PATH"):
            qa.prepare(self.plan_file, self.evidence)

    def test_duplicate_windows_case_zip_rejected(self):
        self.replace_zip("BASE", [("Game.exe", b"a"), ("GAME.EXE", b"b")])
        with self.assertRaisesRegex(ValueError, "ZIP_CASE_DUPLICATE"):
            qa.prepare(self.plan_file, self.evidence)

    def test_symlink_zip_rejected(self):
        path = self.root / "BASE.zip"
        with zipfile.ZipFile(path, "w") as z:
            x = zipfile.ZipInfo("Game.exe")
            x.create_system = 3
            x.external_attr = 0o120777 << 16
            z.writestr(x, b"elsewhere")
        self.plan["builds"]["BASE"]["sha256"] = qa.digest(path)
        self.write_plan()
        with self.assertRaisesRegex(ValueError, "ZIP_SYMLINK"):
            qa.prepare(self.plan_file, self.evidence)

    def replace_zip(self, label, rows):
        path = self.root / f"{label}.zip"
        with zipfile.ZipFile(path, "w") as z:
            for name, data in rows:
                z.writestr(name, data)
        self.plan["builds"][label]["sha256"] = qa.digest(path)
        self.write_plan()

    def test_missing_exe_rejected(self):
        self.replace_zip("EXP", [("Other.exe", b"wrong")])
        with self.assertRaisesRegex(ValueError, "EXE_NOT_IN_ARCHIVE"):
            qa.prepare(self.plan_file, self.evidence)

    def test_prevent_overwriting_evidence(self):
        self.prepared()
        with self.assertRaisesRegex(ValueError, "ALREADY_EXISTS"):
            qa.prepare(self.plan_file, self.evidence)

    def test_unexecuted_not_run(self):
        self.prepared()
        result = qa.verify(self.evidence)
        self.assertEqual(result["status"], "NOT_RUN")
        self.assertEqual(sum(r["status"] == "NOT_RUN" for r in result["cases"]), 6)

    def test_partial_matrix_not_green(self):
        self.prepared()
        self.fake_run(0)
        result = qa.verify(self.evidence)
        self.assertEqual(result["status"], "NOT_RUN")
        self.assertEqual(result["cases"][0]["status"], "PASS")

    def test_all_six_clean_only_observational(self):
        self.prepared()
        for i in range(6):
            self.fake_run(i)
        r = qa.verify(self.evidence)
        self.assertEqual(r["status"], "PASS")
        self.assertEqual(r["conclusion"], "NO_CRASH_IN_SAMPLED_RUNS_NOT_A_FIX_PROOF")
        self.assertIn("EXTERNAL_OPERATOR", r["attribution"])

    def test_access_violation_signed_is_fail(self):
        self.prepared()
        for i in range(6):
            self.fake_run(i, -1073741819 if i == 0 else 0)
        r = qa.verify(self.evidence)
        self.assertEqual(r["status"], "FAIL")
        self.assertEqual(r["cases"][0]["exit_hex"], "0xC0000005")
        self.assertEqual(r["conclusion"], "RC2_NOT_OBSERVED_IN_SAMPLED_RUNS_NOT_A_FIX_PROOF")

    def test_rc2_access_violation(self):
        self.prepared()
        for i in range(6):
            self.fake_run(i, 0xC0000005 if i == 5 else 0)
        self.assertEqual(qa.verify(self.evidence)["conclusion"], "RC2_ACCESS_VIOLATION_OBSERVED")

    def test_timeout_not_crash_or_success(self):
        self.prepared()
        self.fake_run(0, timed_out=True)
        r = qa.verify(self.evidence)
        self.assertEqual(r["cases"][0]["status"], "BLOCKED")
        self.assertEqual(r["status"], "BLOCKED")

    def test_missing_or_modified_log_fails(self):
        self.prepared()
        self.fake_run(0)
        (self.evidence / "runs" / "01-BASE" / "Player.log").write_text("altered")
        self.assertEqual(qa.verify(self.evidence)["cases"][0]["status"], "FAIL")

    def test_native_exit_mismatch_fails(self):
        self.prepared()
        p = self.fake_run(0)
        j = json.loads(p.read_text())
        j["native_exit_signed"] = 0xC0000005
        p.write_text(json.dumps(j))
        self.assertEqual(qa.verify(self.evidence)["cases"][0]["status"], "FAIL")

    def test_contradictory_timeout_zero_fails(self):
        self.prepared()
        p = self.fake_run(0)
        j = json.loads(p.read_text())
        j["timed_out"] = True
        p.write_text(json.dumps(j))
        self.assertEqual(qa.verify(self.evidence)["cases"][0]["status"], "FAIL")

    def test_forged_label_fails(self):
        self.prepared()
        p = self.fake_run(0)
        j = json.loads(p.read_text())
        j["label"] = "RC2"
        p.write_text(json.dumps(j))
        self.assertEqual(qa.verify(self.evidence)["cases"][0]["status"], "FAIL")

    def test_missing_operator_note_fails(self):
        self.prepared()
        p = self.fake_run(0)
        j = json.loads(p.read_text())
        j["operator_note"] = ""
        p.write_text(json.dumps(j))
        self.assertEqual(qa.verify(self.evidence)["cases"][0]["status"], "FAIL")

    def test_changed_plan_fails(self):
        self.prepared()
        p = self.evidence / "plan.json"
        j = json.loads(p.read_text())
        j["manual_instructions"] += " altered"
        p.write_text(json.dumps(j))
        self.assertEqual(qa.verify(self.evidence)["status"], "FAIL")

    def test_unrecognized_run_folder_fails(self):
        self.prepared()
        (self.evidence / "runs" / "99-INJECTED").mkdir(parents=True)
        self.assertEqual(qa.verify(self.evidence)["status"], "FAIL")

    def test_nonwindows_execution_blocked(self):
        self.prepared()
        if qa.sys.platform != "win32":
            with self.assertRaisesRegex(RuntimeError, "WINDOWS_GUI_ONLY"):
                qa.launch(self.evidence, 0, 60, "I clicked Quit in GUI")


if __name__ == "__main__":
    unittest.main(verbosity=2)