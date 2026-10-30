"""Adversarial B01-62 contract validation, transactional restore and independent strict rollback QA."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

import save_contract as c
import rollback_witness as w

class SaveContractTests(unittest.TestCase):
    def setUp(self):
        self.state=c.fresh()
    def reject_state(self,mutate):
        clone=copy.deepcopy(self.state)
        mutate(clone)
        with self.assertRaises(c.Rejected):c.encode(clone)
    def reject_wire(self,mutate):
        clone=copy.deepcopy(self.state)
        mutate(clone)
        # independent checksum recalculated: tests schema rather than only checksum.
        raw=c.canonical({"payload":clone,"checksum_sha256":hashlib.sha256(c.canonical(clone)).hexdigest()})
        with self.assertRaises(c.Rejected):c.decode(raw)
    def test_01_attached_roundtrip(self):self.assertEqual(c.decode(c.encode(self.state)),self.state)
    def test_02_detached_roundtrip(self):
        s=c.sample_detached()
        self.assertEqual(c.decode(c.encode(s)),s)
    def test_03_both_detached_roundtrip(self):
        s=c.sample_detached()
        s["parts"][1].update({"detached":True,"consumed":True,"parent_guid":None,"kinematic":False})
        self.assertEqual(c.decode(c.encode(s)),s)
    def test_04_stable_original_catalog_ids(self):self.assertEqual([p["id"] for p in self.state["parts"]],[2310001,2310002])
    def test_05_deterministic_bytes(self):self.assertEqual(c.encode(self.state),c.encode(copy.deepcopy(self.state)))
    def test_06_tamper_hash(self):
        raw=c.encode(self.state)
        with self.assertRaises(c.Rejected):c.decode(raw.replace(b"2310001",b"2310009"))
    def test_07_wrong_source_even_with_matching_checksum(self):self.reject_wire(lambda s:s.update(selected_sha="baf65f2664e38f3b9c38815c3f07285f8107c1b6"))
    def test_08_wrong_schema(self):self.reject_state(lambda s:s.update(schema="v0"))
    def test_09_duplicate_id(self):self.reject_state(lambda s:s["parts"][1].update(id=s["parts"][0]["id"]))
    def test_10_duplicate_guid(self):self.reject_state(lambda s:s["parts"][1].update(part_guid=s["parts"][0]["part_guid"]))
    def test_11_unknown_id(self):self.reject_state(lambda s:s["parts"][0].update(id=99999999))
    def test_12_guid_mismatch(self):self.reject_state(lambda s:s["parts"][0].update(part_guid=s["parts"][1]["part_guid"]))
    def test_13_missing_rig(self):self.reject_state(lambda s:s["parts"].pop())
    def test_14_extra_rig(self):self.reject_state(lambda s:s["parts"].append(copy.deepcopy(s["parts"][0])))
    def test_15_detached_unconsumed(self):self.reject_state(lambda s:s["parts"][0].update(detached=True,kinematic=False,parent_guid=None))
    def test_16_attached_consumed(self):self.reject_state(lambda s:s["parts"][0].update(consumed=True))
    def test_17_detached_kinematic(self):self.reject_state(lambda s:s["parts"][0].update(detached=True,consumed=True,parent_guid=None))
    def test_18_attached_missing_parent(self):self.reject_state(lambda s:s["parts"][0].update(parent_guid=None))
    def test_19_active_tx(self):self.reject_state(lambda s:s.update(transaction_active=True))
    def test_20_loading_phase(self):self.reject_state(lambda s:s.update(phase="dock-commit"))
    def test_21_negative_generation(self):self.reject_state(lambda s:s.update(generation=-1))
    def test_22_boolean_id(self):self.reject_state(lambda s:s["parts"][0].update(id=True))
    def test_23_nonfinite_velocity(self):self.reject_state(lambda s:s["parts"][0].update(velocity=[float("nan"),0,0]))
    def test_24_invalid_rotation(self):self.reject_state(lambda s:s["parts"][0].update(rotation=[0,0,0,0]))
    def test_25_unexpected_member(self):self.reject_state(lambda s:s.update(codex_owned_field=1))
    def test_26_bad_utf8(self):
        with self.assertRaises(c.Rejected):c.decode(b"\xff")
    def test_27_truncated_json(self):
        with self.assertRaises(c.Rejected):c.decode(c.encode(self.state)[:15])
    def test_28_duplicate_json_keys(self):
        raw=b'{"payload":{},"payload":{},"checksum_sha256":"abc"}'
        with self.assertRaises(c.Rejected):c.decode(raw)
    def test_29_oversized(self):
        with self.assertRaises(c.Rejected):c.decode(b"X"*(c.MAX_BYTES+1))
    def test_30_failed_precommit_does_not_mutate_target(self):
        tgt=c.fresh(); before=copy.deepcopy(tgt)
        def failure(_):raise RuntimeError("injected")
        with self.assertRaises(RuntimeError):c.restore(tgt,c.encode(c.sample_detached()),failure)
        self.assertEqual(tgt,before)
    def test_31_corrupt_load_never_mutates_target(self):
        tgt=c.fresh();before=copy.deepcopy(tgt)
        with self.assertRaises(c.Rejected):c.restore(tgt,b"{}")
        self.assertEqual(tgt,before)
    def test_32_valid_restore_commits_after_preflight(self):
        tgt=c.fresh();src=c.sample_detached();events=[]
        c.restore(tgt,c.encode(src),lambda prepared:events.append(prepared["parts"][0]["consumed"]))
        self.assertEqual(events,[True]);self.assertEqual(tgt,src)
    def test_33_restore_has_no_aliases(self):
        tgt=c.fresh();src=c.sample_detached();c.restore(tgt,c.encode(src))
        src["parts"][0]["detached"]=False
        self.assertTrue(tgt["parts"][0]["detached"])
    def test_34_valid_quaternion_unit(self):
        c.encode(c.sample_detached())
    def test_35_invalid_type_for_boolean(self):self.reject_state(lambda s:s["parts"][0].update(consumed=0))
    def test_36_changed_run_guid_rejected(self):self.reject_state(lambda s:s.update(run_guid="bad"))
    def test_37_changed_parent_guid_rejected(self):self.reject_state(lambda s:s["parts"][1].update(parent_guid="a"*32))
    def test_38_root_full_state_can_be_refreshed(self):
        s=c.sample_detached();s["generation"]=42;self.assertEqual(c.decode(c.encode(s))["generation"],42)

class RollbackWrapperTests(unittest.TestCase):
    class Pkg:
        def __init__(self,expected):self.expected=expected;self.calls=0
        def verify(self,raw):return self.expected
        def rollback(self,raw,target):
            self.calls+=1
            import shutil
            shutil.rmtree(target)
    def seed(self,root):
        (root/"A"/"B").mkdir(parents=True)
        (root/"A"/"B"/"owned.txt").write_bytes(b"trusted")
        return self.Pkg({"A/B/owned.txt":b"trusted"})
    def test_39_normal_stage_clean(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            w.strict_rollback(pkg,b"",target)
            self.assertEqual(pkg.calls,1);self.assertFalse(target.exists())
    def test_40_extra_empty_root_dir_preserved(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            (target/"alien").mkdir()
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",target)
            self.assertTrue((target/"alien").is_dir());self.assertEqual(pkg.calls,0)
    def test_41_extra_empty_nested_dir_preserved(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            (target/"A"/"foreign").mkdir()
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",target)
            self.assertTrue((target/"A"/"foreign").exists());self.assertEqual(pkg.calls,0)
    def test_42_foreign_file_preserved(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            (target/"A"/"alien.txt").write_text("mine")
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",target)
            self.assertEqual((target/"A"/"alien.txt").read_text(),"mine")
    def test_43_changed_byte_preserved(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            (target/"A"/"B"/"owned.txt").write_bytes(b"tampered")
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",target)
            self.assertEqual(pkg.calls,0)
    def test_44_missing_byte_preserved(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            (target/"A"/"B"/"owned.txt").unlink()
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",target)
            self.assertEqual(pkg.calls,0)
    def test_45_symlink_dir_refused(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            (target/"A"/"external").symlink_to(Path(t),target_is_directory=True)
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",target)
            self.assertEqual(pkg.calls,0)
    def test_46_symlink_file_refused(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            (target/"A"/"B"/"owned.txt").unlink()
            (target/"A"/"B"/"owned.txt").symlink_to(Path(t))
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",target)
            self.assertEqual(pkg.calls,0)
    def test_47_symlink_root_refused(self):
        with tempfile.TemporaryDirectory() as t:
            target=Path(t)/"fresh";pkg=self.seed(target)
            link=Path(t)/"link";link.symlink_to(target,target_is_directory=True)
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",link)
            self.assertEqual(pkg.calls,0)
    def test_48_missing_stage_refused(self):
        with tempfile.TemporaryDirectory() as t:
            pkg=self.Pkg({})
            with self.assertRaises(w.Refuse):w.strict_rollback(pkg,b"",Path(t)/"absent")
            self.assertEqual(pkg.calls,0)

if __name__=="__main__":unittest.main()
