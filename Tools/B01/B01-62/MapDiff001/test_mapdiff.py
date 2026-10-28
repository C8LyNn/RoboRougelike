"""MAPDIFF-001: independent real-file positive/negative and deterministic tests."""
import json
from pathlib import Path
import tempfile
import unittest

import mapdiff as m


def doc(items=None, version=2, floor=False):
    return json.dumps({"version": version, "globalFloor": floor, "items": items or []},
                      separators=(",", ":")).encode("utf-8")


def item(k=0, x=0, z=0, rot=0, sx=1, sy=1, sz=1):
    return {"kind": k, "x": x, "z": z, "rotation": rot,
            "scaleX": sx, "scaleY": sy, "scaleZ": sz}


class PhysicalMapDiffTests(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory(prefix="b01-mapdiff-")
        self.addCleanup(tmp.cleanup)
        root = Path(tmp.name)
        self.a, self.b = root / "before.json", root / "after.json"
        self.out = root / "output"
        self.out.mkdir()

    def files(self, before, after):
        self.a.write_bytes(before)
        self.b.write_bytes(after)

    def compare(self, before, after):
        self.files(before, after)
        return m.run(self.a, self.b, self.out)

    def test_A01_original_bytes_remain_unmodified(self):
        before, after = doc([item()]), doc([item(1, 2)])
        report = self.compare(before, after)
        self.assertEqual(before, self.a.read_bytes())
        self.assertEqual(after, self.b.read_bytes())
        self.assertEqual(1, len(report["addedItems"]))
        self.assertEqual(0, len(report["removedItems"]))

    def test_A02_deterministic_empty_map(self):
        report = self.compare(doc(), doc())
        self.assertTrue(report["noChanges"])
        self.assertEqual([], report["changedCells"])
        self.assertIn('aria-label=', (self.out / "mapdiff.svg").read_text())

    def test_A03_semantic_identity_ignore_file_order(self):
        a = doc([item(1, 1), item(0, 0)])
        b = doc([item(0, 0), item(1, 1)])
        r = self.compare(a, b)
        self.assertTrue(r["noChanges"])
        self.assertNotEqual(r["before"]["sha256"], r["after"]["sha256"])
        self.assertEqual(r["before"]["semanticSHA256"], r["after"]["semanticSHA256"])

    def test_A04_one_added_wall(self):
        r = self.compare(doc([item(0)]), doc([item(0), item(1, 3, 4)]))
        self.assertEqual(1, len(r["addedItems"]))
        self.assertEqual(0, len(r["removedItems"]))
        self.assertEqual(1, r["cellSummary"]["objects"]["added"])
        self.assertFalse(r["noChanges"])

    def test_A05_rotated_footprint_distinguishes_cells(self):
        a = item(1, 0, 0, rot=0, sx=2)
        b = item(1, 0, 0, rot=1, sx=2)
        r = self.compare(doc([a]), doc([b]))
        self.assertEqual(3, len(r["changedCells"]))
        self.assertEqual({"added": 1, "removed": 1, "replaced": 1},
                         r["cellSummary"]["objects"])

    def test_A06_one_existing_object_kind_replaced(self):
        r = self.compare(doc([item(1)]), doc([item(7)]))
        self.assertEqual(1, r["cellSummary"]["objects"]["replaced"])
        self.assertEqual(1, len(r["addedItems"]))
        self.assertEqual(1, len(r["removedItems"]))

    def test_A07_ground_is_separate_from_object_layer(self):
        r = self.compare(doc([item(0, 1)]), doc([item(0, 1), item(7, 1)]))
        self.assertEqual(0, sum(r["cellSummary"]["ground"].values()))
        self.assertEqual(1, r["cellSummary"]["objects"]["added"])

    def test_A08_global_floor_only_change_visible(self):
        r = self.compare(doc([item(0)], floor=False), doc([item(0)], floor=True))
        self.assertTrue(r["globalFloorChanged"])
        self.assertFalse(r["noChanges"])
        self.assertEqual([], r["changedCells"])

    def test_A09_v1_ignores_scaled_fields(self):
        a = doc([item(1, sx=4)], version=1)
        b = doc([item(1, sx=1)], version=2)
        r = self.compare(a, b)
        self.assertTrue(r["noChanges"])
        self.assertEqual(r["before"]["semanticSHA256"], r["after"]["semanticSHA256"])

    def test_N01_reject_overlapping_object_footprints(self):
        with self.assertRaises(m.InvalidMap):
            m.mapdocument(doc([item(1, 0), item(7, 0)]))

    def test_N02_reject_out_of_bounds_rotated_footprint(self):
        with self.assertRaises(m.InvalidMap):
            m.mapdocument(doc([item(1, 50, 0, sx=2)]))

    def test_N03_reject_duplicate_spawn(self):
        with self.assertRaises(m.InvalidMap):
            m.mapdocument(doc([item(4, 0), item(4, 2)]))

    def test_N04_reject_boolean_as_kind(self):
        bad = doc([{"kind": True, "x": 0, "z": 0, "rotation": 0}])
        with self.assertRaises(m.InvalidMap):
            m.mapdocument(bad)

    def test_N05_reject_invalid_utf8_and_invalid_version(self):
        for data in (b"\xff\xfe", b'{"version":3,"items":[]}', b""):
            with self.subTest(data=data), self.assertRaises(m.InvalidMap):
                m.mapdocument(data)

    def test_N06_reject_invalid_file_size(self):
        self.a.write_bytes(b"x" * (m.MAX_FILE + 1))
        with self.assertRaises(m.InvalidMap):
            m.load_map(self.a)

    def test_N07_immutable_outputs_existing_no_clobber(self):
        self.files(doc(), doc())
        (self.out / "mapdiff.json").write_text("USER DATA")
        with self.assertRaises(m.InvalidMap):
            m.run(self.a, self.b, self.out)
        self.assertEqual("USER DATA", (self.out / "mapdiff.json").read_text())
        self.assertFalse((self.out / "mapdiff.svg").exists())

    def test_N08_symlink_input_rejected_if_os_permits(self):
        self.a.write_bytes(doc())
        self.b.write_bytes(doc())
        link = self.a.parent / "alias.json"
        try:
            link.symlink_to(self.a)
        except (OSError, NotImplementedError):
            self.skipTest("Symlink privilege unavailable")
        with self.assertRaises(m.InvalidMap):
            m.run(link, self.b, self.out)

    def test_N09_output_cannot_be_input_folder(self):
        self.files(doc(), doc())
        with self.assertRaises(m.InvalidMap):
            m.run(self.a, self.b, self.a.parent)

    def test_A10_report_matches_output_and_is_valid_xml(self):
        import xml.etree.ElementTree as ET
        r = self.compare(doc([item(1)]), doc([item(7)]))
        loaded = json.loads((self.out / "mapdiff.json").read_text("utf8"))
        self.assertEqual(r, loaded)
        xml = ET.parse(self.out / "mapdiff.svg")
        self.assertEqual("{http://www.w3.org/2000/svg}svg", xml.getroot().tag)
        self.assertIn("addedItems", loaded)

    def test_A11_no_missing_source_files_silently_created(self):
        self.a.write_bytes(doc())
        with self.assertRaises(m.InvalidMap):
            m.run(self.a, self.b, self.out)
        self.assertFalse(self.b.exists())
        self.assertFalse(list(self.out.iterdir()))


if __name__ == "__main__":
    unittest.main(verbosity=2)
