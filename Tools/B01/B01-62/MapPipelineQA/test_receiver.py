"""Independent producer-CLI receiver: physical file I/O and negative tamper tests."""
import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
import receiver

def sha(b):
    return hashlib.sha256(b).hexdigest()

class MapPipelineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sources = Path(os.environ["MAPPIPE_INPUTS"])

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="map-pipe-qa-")
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.out = self.root / "output"
        self.out.mkdir()

    def test_01_real_owner_clis_full_backup_diff_restore_undo(self):
        before = {p.name:sha(p.read_bytes()) for p in self.sources.iterdir() if p.is_file()}
        r = receiver.audit(self.sources, self.out)
        self.assertEqual("PASS", r["status"])
        self.assertEqual(360, r["input"]["items"])
        self.assertEqual(1, r["results"]["changedFootprintCells"])
        self.assertEqual("PASS", r["results"]["undoBytePreservation"])
        self.assertEqual("PASS", r["results"]["restoredByteIdentity"])
        self.assertEqual("PASS", r["results"]["restoredSemanticIdentity"])
        self.assertTrue((self.out / "controlled-change.svg").is_file())
        self.assertEqual(before, {p.name:sha(p.read_bytes()) for p in self.sources.iterdir() if p.is_file()})

    def test_02_no_existing_report_clobber(self):
        original = "IMPORTANT PREVIOUS RECEIPT"
        (self.out / "receipt.json").write_text(original, encoding="utf8")
        with self.assertRaisesRegex(receiver.ReceiverError, "Refusing to overwrite"):
            receiver.audit(self.sources, self.out)
        self.assertEqual(original, (self.out / "receipt.json").read_text("utf8"))

    def test_03_bytes_and_svg_replay_deterministic(self):
        a = receiver.audit(self.sources, self.out)
        second = self.root / "replay"
        second.mkdir()
        b = receiver.audit(self.sources, second)
        self.assertEqual(a, b)
        for filename in ("receipt.json", "controlled-change.svg"):
            self.assertEqual((self.out / filename).read_bytes(),
                             (second / filename).read_bytes())

    def test_04_bad_source_count_refused(self):
        source = self.root / "mutant"
        source.mkdir()
        for n in receiver.SOURCE_NAMES:
            (source/n).write_bytes((self.sources/n).read_bytes())
        data = json.loads((source/"arena.json").read_bytes())
        data["items"].pop()
        (source/"arena.json").write_text(json.dumps(data))
        with self.assertRaisesRegex(receiver.ReceiverError, "count mismatch"):
            receiver.audit(source, self.out)
        self.assertFalse(list(self.out.iterdir()))

    def test_05_wrong_barrier_refused(self):
        source = self.root / "mutant"
        source.mkdir()
        for n in receiver.SOURCE_NAMES:
            (source/n).write_bytes((self.sources/n).read_bytes())
        data = json.loads((source/"arena.json").read_bytes())
        next(v for v in data["items"] if v["kind"] == 8)["x"] = 5
        (source/"arena.json").write_text(json.dumps(data))
        with self.assertRaisesRegex(receiver.ReceiverError, "barrier was changed"):
            receiver.audit(source, self.out)

    def test_06_missing_source_tool_fails_closed(self):
        source = self.root / "empty"
        source.mkdir()
        with self.assertRaisesRegex(receiver.ReceiverError, "Missing source"):
            receiver.audit(source, self.out)

    def test_07_symlink_input_rejected_if_supported(self):
        source = self.root / "links"
        source.mkdir()
        for n in receiver.SOURCE_NAMES:
            if n != "arena.json":
                (source/n).write_bytes((self.sources/n).read_bytes())
        try:
            (source/"arena.json").symlink_to(self.sources/"arena.json")
        except (OSError,NotImplementedError):
            self.skipTest("Symlink privilege not granted")
        with self.assertRaisesRegex(receiver.ReceiverError, "Missing source"):
            receiver.audit(source, self.out)

    def test_08_controlled_mutation_preserves_non_targeted_items(self):
        raw = (self.sources/"arena.json").read_bytes()
        modified = receiver.controlled_change(raw)
        a = json.loads(raw)
        b = json.loads(modified)
        self.assertEqual(359,len(b["items"]))
        self.assertEqual(360,len(a["items"]))
        self.assertEqual(1,len(a["items"])-len(b["items"]))
        self.assertEqual(3,sum(i["kind"]==8 for i in b["items"]))
        self.assertEqual(4,sum(i["kind"]==8 for i in a["items"]))
        from collections import Counter
        encode = lambda items: Counter(json.dumps(i,sort_keys=True) for i in items)
        gone = list((encode(a["items"]) - encode(b["items"])).elements())
        added = list((encode(b["items"]) - encode(a["items"])).elements())
        self.assertEqual(1,len(gone))
        self.assertEqual((8,6,6),tuple(json.loads(gone[0])[k] for k in ("kind","x","z")))
        self.assertEqual([],added)
        self.assertEqual(raw,(self.sources/"arena.json").read_bytes())

if __name__ == "__main__":
    unittest.main()
