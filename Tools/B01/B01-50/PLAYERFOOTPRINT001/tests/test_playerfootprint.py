import copy
import json
from pathlib import Path
import stat
import sys
import tempfile
import unittest
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import playerfootprint as m


class PlayerFootprintTests(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.folder = Path(tmp.name)
        self.zip_path = self.folder / "player.zip"

    def build(self, extra=None, prefix="", compression=zipfile.ZIP_DEFLATED):
        files = {
            "RoboRougelike.exe": b"MZ-test-exe",
            "UnityPlayer.dll": b"MZ-test-dll",
            "RoboRougelike_Data/globalgamemanagers": b"global-manager",
            "RoboRougelike_Data/resources.assets": b"raw-asset",
        }
        if extra:
            files.update(extra)
        with zipfile.ZipFile(self.zip_path, "w", compression=compression) as z:
            for path, data in files.items():
                z.writestr(prefix + path, data)
        return self.zip_path

    def test_valid_build_snapshot(self):
        self.build()
        report = m.inspect(self.zip_path)
        self.assertEqual(report["summary"]["files"], 4)
        self.assertEqual(report["archive_sha256"], m.digest(self.zip_path))
        self.assertEqual(report["summary"]["by_extension"][".exe"]["files"], 1)
        self.assertEqual(m.verify_report(report), report)

    def test_wrapper_directory(self):
        self.build(prefix="windows-x64/")
        r = m.inspect(self.zip_path)
        self.assertEqual(r["entries"][0]["path"], "RoboRougelike.exe")
        self.assertFalse(any(x["path"].startswith("windows") for x in r["entries"]))

    def test_byte_permutation_determinism(self):
        self.build(extra={"Data/two.bin": b"123", "Data/one.bin": b"456"})
        self.assertEqual(m.inspect(self.zip_path), m.inspect(self.zip_path))

    def test_source_bytes_not_compressed_size(self):
        self.build(extra={"more.dat": b"00000000" * 1000})
        r = m.inspect(self.zip_path)
        self.assertGreater(r["summary"]["player_uncompressed_bytes"], r["archive_bytes"])

    def test_missing_exe(self):
        self.build(extra={"RoboRougelike.exe": b""})
        with self.assertRaisesRegex(ValueError, "valid PE"):
            m.inspect(self.zip_path)

    def test_missing_unityplayer(self):
        self.build(extra={"UnityPlayer.dll": b""})
        with self.assertRaisesRegex(ValueError, "valid PE"):
            m.inspect(self.zip_path)

    def test_missing_data_layout(self):
        self.build(extra={"RoboRougelike_Data/globalgamemanagers": b""})
        # Empty manager is a valid file for inventory purposes, not a runnable native acceptance.
        self.assertEqual(m.inspect(self.zip_path)["summary"]["files"], 4)
        with zipfile.ZipFile(self.zip_path, "w") as z:
            z.writestr("RoboRougelike.exe", b"MZ")
            z.writestr("UnityPlayer.dll", b"MZ")
        with self.assertRaisesRegex(ValueError, "layout"):
            m.inspect(self.zip_path)

    def test_invalid_build_name(self):
        self.build()
        for name in ("../evil", "", "RoboRougelike.exe", "two words"):
            with self.subTest(name=name), self.assertRaises(ValueError):
                m.inspect(self.zip_path, build_name=name)

    def test_zip_slip_rejected(self):
        for bad in ("../escape", "/absolute", "bad\\windows", "nested//empty", "folder/./value", "C:/drive"):
            with self.subTest(bad=bad):
                self.build(extra={bad: b"bad"})
                with self.assertRaises(ValueError):
                    m.inspect(self.zip_path)

    def test_windows_reserved_and_ambiguous_rejected(self):
        for bad in ("con.txt", "LPT1", "foo./file", "hello /file", "foo:bar", "nul", "a?b"):
            with self.subTest(bad=bad):
                self.build(extra={bad: b"bad"})
                with self.assertRaises(ValueError):
                    m.inspect(self.zip_path)

    def test_case_collision_rejected(self):
        self.build(extra={"A.bin": b"x", "a.BIN": b"y"})
        with self.assertRaisesRegex(ValueError, "case-colliding"):
            m.inspect(self.zip_path)

    def test_duplicate_same_spelling_rejected(self):
        self.build()
        with zipfile.ZipFile(self.zip_path, "a") as z:
            z.writestr("RoboRougelike.exe", b"MZ-new")
        with self.assertRaisesRegex(ValueError, "case-colliding"):
            m.inspect(self.zip_path)

    def test_symlink_rejected(self):
        self.build()
        info = zipfile.ZipInfo("sneaky-link")
        info.create_system = 3
        info.external_attr = (stat.S_IFLNK | 0o777) << 16
        with zipfile.ZipFile(self.zip_path, "a") as z:
            z.writestr(info, "UnityPlayer.dll")
        with self.assertRaisesRegex(ValueError, "link or special"):
            m.inspect(self.zip_path)

    def test_file_parent_collision(self):
        self.build(extra={"outer": b"x", "outer/child": b"y"})
        with self.assertRaisesRegex(ValueError, "parent"):
            m.inspect(self.zip_path)

    def test_bomb_ratio(self):
        self.build(extra={"zeros.bin": b"0" * 50000})
        with self.assertRaisesRegex(ValueError, "compression ratio"):
            m.inspect(self.zip_path, max_ratio=5)

    def test_resource_budgets(self):
        self.build()
        with self.assertRaisesRegex(ValueError, "total budget"):
            m.inspect(self.zip_path, max_total=2)
        with self.assertRaisesRegex(ValueError, "member count"):
            m.inspect(self.zip_path, max_files=1)
        with self.assertRaisesRegex(ValueError, "oversized"):
            m.inspect(self.zip_path, max_file=1)

    def test_corrupted_member_crc(self):
        self.build(extra={"secret.txt": b"UNIQUEPAYLOADHERE"}, compression=zipfile.ZIP_STORED)
        raw = self.zip_path.read_bytes()
        self.assertIn(b"UNIQUEPAYLOADHERE", raw)
        self.zip_path.write_bytes(raw.replace(b"UNIQUEPAYLOADHERE", b"ALTEREDPAYLOADXX", 1))
        with self.assertRaises(zipfile.BadZipFile):
            m.inspect(self.zip_path)

    def test_bad_file_type(self):
        self.build()
        p = zipfile.ZipInfo("block")
        p.create_system = 3
        p.external_attr = (stat.S_IFIFO | 0o600) << 16
        with zipfile.ZipFile(self.zip_path, "a") as z:
            z.writestr(p, b"fifo")
        with self.assertRaisesRegex(ValueError, "special"):
            m.inspect(self.zip_path)

    def test_input_symlink_refused(self):
        self.build()
        link = self.folder / "alias.zip"
        link.symlink_to(self.zip_path)
        with self.assertRaisesRegex(ValueError, "non-symlink"):
            m.inspect(link)

    def test_bad_report_provenance(self):
        self.build()
        doc = m.inspect(self.zip_path)
        for value in ("UNITY_NATIVE_PASS", "PLAYER_EXECUTED", ""):
            d = copy.deepcopy(doc)
            d["provenance"] = value
            with self.subTest(value=value), self.assertRaises(ValueError):
                m.verify_report(d)

    def test_poisoned_report_and_extra_fields(self):
        self.build()
        doc = m.inspect(self.zip_path)
        d = copy.deepcopy(doc)
        d["summary"]["files"] += 1
        with self.assertRaisesRegex(ValueError, "summary"):
            m.verify_report(d)
        d = copy.deepcopy(doc)
        d["secret"] = True
        with self.assertRaisesRegex(ValueError, "structure"):
            m.verify_report(d)
        d = copy.deepcopy(doc)
        d["entries"][0]["bytes"] = True
        with self.assertRaises(ValueError):
            m.verify_report(d)

    def test_report_duplicate_and_path_injection(self):
        self.build()
        doc = m.inspect(self.zip_path)
        d = copy.deepcopy(doc)
        d["entries"][1]["path"] = d["entries"][0]["path"].upper()
        with self.assertRaisesRegex(ValueError, "duplicate"):
            m.verify_report(d)
        d = copy.deepcopy(doc)
        d["entries"][0]["path"] = "../evil"
        with self.assertRaises(ValueError):
            m.verify_report(d)

    def test_delta_same_size_changed_hash(self):
        self.build()
        before = m.inspect(self.zip_path)
        self.build(extra={"RoboRougelike_Data/resources.assets": b"raw-assEt"})
        after = m.inspect(self.zip_path)
        d = m.compare(before, after)
        self.assertEqual(d["uncompressed_delta_bytes"], 0)
        self.assertEqual(d["modified"], ["RoboRougelike_Data/resources.assets"])
        self.assertNotEqual(d["before_archive_sha256"], d["after_archive_sha256"])

    def test_delta_added_removed_and_mismatch_name(self):
        self.build(extra={"old": b"x"})
        before = m.inspect(self.zip_path)
        self.build(extra={"new": b"x"})
        after = m.inspect(self.zip_path)
        d = m.compare(before, after)
        self.assertEqual(d["added"], ["new"])
        self.assertEqual(d["removed"], ["old"])
        after["build_name"] = "Different"
        with self.assertRaisesRegex(ValueError, "incompatible"):
            m.compare(before, after)

    def test_exclusive_report(self):
        self.build()
        out = self.folder / "a.json"
        m.exclusive_write(out, m.inspect(self.zip_path))
        with self.assertRaises(FileExistsError):
            m.exclusive_write(out, {})
        self.assertEqual(m.main(["inspect", "--zip", str(self.zip_path), "--out", str(self.zip_path)]), 1)

    def test_cli_compare_budget_and_valid_outputs(self):
        self.build()
        a = self.folder / "a.json"
        b = self.folder / "b.json"
        result = self.folder / "delta.json"
        self.assertEqual(m.main(["inspect", "--zip", str(self.zip_path), "--out", str(a)]), 0)
        self.build(extra={"extra.bin": b"new"})
        self.assertEqual(m.main(["inspect", "--zip", str(self.zip_path), "--out", str(b)]), 0)
        self.assertEqual(m.main(["compare", "--before", str(a), "--after", str(b),
                                 "--out", str(result), "--max-uncompressed-growth", "2"]), 2)
        self.assertEqual(json.loads(result.read_text())["uncompressed_delta_bytes"], 3)

    def test_cli_invalid_budget_no_outputs(self):
        self.build()
        out = self.folder / "a.json"
        self.assertEqual(m.main(["inspect", "--zip", str(self.zip_path), "--out", str(out),
                                 "--max-entries", "0"]), 1)
        self.assertFalse(out.exists())
        self.assertEqual(m.main(["compare", "--before", out.as_posix(), "--after", out.as_posix(),
                                 "--out", out.as_posix(), "--max-uncompressed-growth", "-1"]), 1)


if __name__ == "__main__":
    unittest.main()
