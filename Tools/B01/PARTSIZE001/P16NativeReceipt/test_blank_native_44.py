#!/usr/bin/env python3
"""Real B01-23 original Git-blob native intake template tests (NOT Unity)."""
import copy
import json
from pathlib import Path
import sys
import unittest
import importlib.util

p=Path(__file__).with_name("generate_blank_native_44.py")
spec=importlib.util.spec_from_file_location("native44",p)
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

class NativeTemplate(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original=mod.load_original(".")
        cls.body=mod.build_template(cls.original)

    def test_exact_source_catalog_and_44(self):
        self.assertEqual(len(mod.kinds_from_actual_source(self.original)),11)
        self.assertEqual(len(self.body["measurements"]),44)
        self.assertTrue(mod.validate_blank(self.body))
    def test_all_pose_names_are_exact_and_distinct(self):
        ids=[x["case_id"] for x in self.body["measurements"]]
        self.assertEqual(len(set(ids)),44)
        self.assertEqual(ids[0],"PART_WHEEL_Y000")
        self.assertEqual(ids[-1],"PART_CRAWLERTRACK_Y270")
    def test_every_metric_is_unmeasured(self):
        self.assertTrue(all(x["native_status"]=="NOT_RUN" and
            x["actual_collider_bounds_min_m"] is None and
            x["actual_renderer_bounds_max_m"] is None and
            x["unity_exit_code"] is None for x in self.body["measurements"]))
    def reject_change(self,change):
        bad=copy.deepcopy(self.body);change(bad)
        with self.assertRaises(ValueError):
            mod.validate_blank(bad)
    def test_cannot_forge_native_pass(self):
        self.reject_change(lambda a:a["measurements"][0].__setitem__("native_status","PASS"))
    def test_cannot_inject_fake_zero_physics(self):
        self.reject_change(lambda a:a["measurements"][0].__setitem__("actual_overlap_depth_m",0.0))
    def test_cannot_inject_fake_xml_hash(self):
        self.reject_change(lambda a:a["measurements"][0].__setitem__("original_nunit_xml_sha256","abc"))
    def test_cannot_inject_fake_exit_code(self):
        self.reject_change(lambda a:a["measurements"][0].__setitem__("unity_exit_code",0))
    def test_cannot_omit_pose(self):
        self.reject_change(lambda a:a["measurements"].pop())
    def test_cannot_duplicate_pose(self):
        self.reject_change(lambda a:a["measurements"].__setitem__(5,a["measurements"][4]))
    def test_cannot_swap_pose(self):
        self.reject_change(lambda a:a["measurements"].__setitem__(0,a["measurements"][1]))
    def test_cannot_change_kind(self):
        self.reject_change(lambda a:a["measurements"][0].__setitem__("kind","Core"))
    def test_cannot_fake_source_sha(self):
        self.reject_change(lambda a:a.__setitem__("producer_source_sha","f"*40))
    def test_cannot_fake_g1(self):
        self.reject_change(lambda a:a["gates"].__setitem__("G1","PASS"))
    def test_cannot_fake_unity_version(self):
        self.reject_change(lambda a:a.__setitem__("unity_version_required","6.0"))
    def test_cannot_add_unknown_measurement(self):
        self.reject_change(lambda a:a["measurements"][0].__setitem__("fake_pass",True))
    def test_catalog_renamed_kind_rejected(self):
        altered=self.original.replace('"Wheel"','"FakeWheel"',1)
        with self.assertRaises(ValueError):mod.kinds_from_actual_source(altered)
    def test_catalog_unmeasured_contract_removed_rejected(self):
        altered=self.original.replace("new Cell3(1,1,1));","new Cell3(2,1,1));")
        with self.assertRaises(ValueError):mod.kinds_from_actual_source(altered)

if __name__=="__main__":
    unittest.main(verbosity=2)
