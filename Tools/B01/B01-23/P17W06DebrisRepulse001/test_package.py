"""Real archive source, corruption, staging and rollback tests. Not Unity."""
import io
from pathlib import Path
import tempfile
import unittest
import zipfile
import package as p

def mutate_zip(original, updates=None, extra=None):
    updates = updates or {}
    with zipfile.ZipFile(io.BytesIO(original), "r") as reader:
        content = {n: reader.read(n) for n in reader.namelist()}
    content.update(updates)
    if extra:
        content.update(extra)
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, "w", compression=zipfile.ZIP_STORED) as writer:
        for name in sorted(content):
            writer.writestr(p.canonical_info(name), content[name])
    return stream.getvalue()

class PhysicalPackageTest(unittest.TestCase):
    def test_original_git_source_and_manifest(self):
        b, meta = p.produce()
        parts, verified = p.verified_members(b)
        self.assertEqual(verified["source_sha"], p.source_sha())
        self.assertEqual(len(parts), len(p.FILES)+1)
        self.assertEqual(len(meta["files"]),len(p.FILES))
    def test_reproducible_bytes_and_sha(self):
        a, _=p.produce()
        b, _=p.produce()
        self.assertEqual(a,b)
        self.assertEqual(p.sha(a),p.sha(b))
    def test_corrupt_original_source_rejected(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verified_members,mutate_zip(data,{p.FILES[0]:b"changed"}))
    def test_unexpected_member_rejected(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verified_members,mutate_zip(data,extra={"Assets/R3/Injected.cs":b"x"}))
    def test_zip_slip_rejected(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.verified_members,mutate_zip(data,extra={"../outside":b"x"}))
    def test_false_codex_ack_rejected(self):
        import json
        data,_=p.produce()
        parts,_=p.verified_members(data)
        meta=json.loads(parts[p.MANIFEST])
        meta["codex_ack"]=True
        bogus=mutate_zip(data,{p.MANIFEST:json.dumps(meta).encode()})
        self.assertRaises(ValueError,p.verified_members,bogus)
    def test_stage_and_rollback_exact(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as tmp:
            target=Path(tmp)/"isolated"
            p.stage(data,target)
            self.assertTrue(target.exists())
            p.rollback(data,target)
            self.assertFalse(target.exists())
    def test_stage_refuses_existing_destination(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as tmp:
            self.assertRaises(ValueError,p.stage,data,Path(tmp))
    def test_rollback_refuses_changed_source(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"isolated"
            p.stage(data,dest)
            (dest/p.FILES[0]).write_bytes(b"unowned mutation")
            self.assertRaises(ValueError,p.rollback,data,dest)
            self.assertTrue(dest.exists())
    def test_rollback_refuses_extra_file(self):
        data,_=p.produce()
        with tempfile.TemporaryDirectory() as tmp:
            dest=Path(tmp)/"isolated"
            p.stage(data,dest)
            (dest/"foreign.txt").write_text("do not delete")
            self.assertRaises(ValueError,p.rollback,data,dest)
            self.assertTrue((dest/"foreign.txt").exists())
    def test_stage_inside_repo_refused(self):
        data,_=p.produce()
        self.assertRaises(ValueError,p.stage,data,p.ROOT/"Artifacts"/"test-do-not-create")
    def test_reject_duplicate_member(self):
        data,_=p.produce()
        with zipfile.ZipFile(io.BytesIO(data),"r") as source:
            entries={name:source.read(name) for name in source.namelist()}
        stream=io.BytesIO()
        with zipfile.ZipFile(stream,"w",compression=zipfile.ZIP_STORED) as target:
            for name in sorted(entries):
                target.writestr(p.canonical_info(name),entries[name])
            import warnings
            with warnings.catch_warnings():
                warnings.simplefilter("ignore",UserWarning)
                target.writestr(p.canonical_info(p.FILES[0]),entries[p.FILES[0]])
        self.assertRaises(ValueError,p.verified_members,stream.getvalue())

if __name__=="__main__":
    unittest.main(verbosity=2)
