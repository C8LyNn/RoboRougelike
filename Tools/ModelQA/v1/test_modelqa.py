#!/usr/bin/env python3
"""Independent QA for the auditor itself, standard library only."""
import json
import pathlib
import tempfile
import unittest

from modelqa import (
    primitive_vertices, bounds, overlap_volume, sweep_json, inspect_obj,
    source_status, catalog_inventory, make_html
)

class AuditorTests(unittest.TestCase):
    @staticmethod
    def box(name,center,parent):
        return {"name":name,"shape":"box","center":center,
                "size":[.2,.2,.2],"parent":parent}
    def test_sampled_collision_is_found_between_keys(self):
        model={"hinge_pivot":[0,0,0],"tilt_degrees":[-1,1],
               "parts":[self.box("static",[0,0,0],"base"),
                        self.box("moving",[0,0,0],"tilt")]}
        out=sweep_json(model,.5)
        self.assertEqual(out["sample_count"],5)
        self.assertEqual(out["pairs_checked"],5)
        self.assertEqual(out["positive_aabb_pairs"],5)
        self.assertEqual(out["narrowphase"],"NOT_RUN")
    def test_actual_m05_four_angle_key_format_including_return(self):
        model={"hinge_pivot":[0,0,0],"tilt_degrees":[-22,0,22,0],
               "parts":[self.box("static",[0,0,0],"base"),
                        self.box("moving",[3,0,0],"tilt")]}
        result=sweep_json(model,.5)
        self.assertEqual(result["sample_count"],89)
        self.assertEqual(result["pairs_checked"],89)

    def test_sampled_clear_separate_parts(self):
        m={"hinge_pivot":[0,0,0],"tilt_degrees":[-1,1],
           "parts":[self.box("static",[0,0,0],"base"),
                    self.box("moving",[3,0,0],"tilt")]}
        r=sweep_json(m,.5)
        self.assertEqual(r["positive_aabb_pairs"],0)
        self.assertEqual(r["status"],"BROADPHASE_CLEAR_SAMPLED")
        self.assertEqual(r["continuity_between_samples"],"NOT_PROVEN")
    def test_primitive_box_and_cylinder_have_finite_vertices(self):
        self.assertEqual(len(primitive_vertices(self.box("test",[0,0,0],"base"))),8)
        c={"shape":"cylinder","name":"c","center":[0,0,0],"axis":"X",
           "radius":.03,"depth":.3,"segments":16}
        self.assertEqual(len(primitive_vertices(c)),32)
        self.assertLess(overlap_volume(([0,0,0],[1,1,1]),([2,2,2],[3,3,3])),1e-12)
        self.assertEqual(len(bounds(primitive_vertices(c),False,[0,0,0],0)),2)
    def test_obj_parser_actual_indices_and_nonzero_area(self):
        raw=b"o triangle\nusemtl Mat\nv 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n"
        r=inspect_obj(raw)
        self.assertEqual(r["triangles"],1)
        self.assertEqual(r["degenerate_triangles"],0)
        self.assertEqual(r["status"],"STATIC_MESH_BASIC_PASS")
    def test_obj_triple_degenerate_detected(self):
        r=inspect_obj(b"o broken\nv 0 0 0\nv 1 0 0\nv 2 0 0\nf 1 2 3\n")
        self.assertEqual(r["degenerate_triangles"],1)
        self.assertEqual(r["status"],"STATIC_MESH_BASIC_FAIL")
    def test_obj_out_of_range_rejected(self):
        with self.assertRaises(ValueError):
            inspect_obj(b"v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 9\n")
    def test_stale_export_diff_does_not_award_pass(self):
        old={"generator":{"git_blob_sha":"old"},"exports":{"mesh.glb":{"git_blob_sha":"same"}}}
        new={"generator":{"git_blob_sha":"fixed"},"exports":{"mesh.glb":{"git_blob_sha":"same"}}}
        self.assertEqual(source_status(old,new,["mesh.glb"])["mesh.glb"],
                         "UNCHANGED_AFTER_SOURCE_FIX_STALE_RISK")
    def test_real_regeneration_requires_source_parity_validation(self):
        old={"generator":{"git_blob_sha":"old"},"exports":{"mesh.obj":{"git_blob_sha":"A"}}}
        new={"generator":{"git_blob_sha":"fixed"},"exports":{"mesh.obj":{"git_blob_sha":"B"}}}
        self.assertEqual(source_status(old,new,["mesh.obj"])["mesh.obj"],
                         "CHANGED_EXPORT_PARITY_NOT_YET_PROVEN")
    def test_catalog_must_be_seventy_distinct(self):
        models=[{"id":f"M{s:02d}-{j:03d}","fix_pr":None}
                for s in range(1,6) for j in range(1,15)]
        res=catalog_inventory({"models":models})
        self.assertEqual(res["models_declared"],70)
        models[-1]["id"]=models[0]["id"]
        with self.assertRaises(AssertionError):
            catalog_inventory({"models":models})
    def test_report_html_is_escaped(self):
        data={"pilot":[{"model":"<script>","original_sha":"a"*40,
                        "fix_sha":"b"*40,"status":"FAIL<script>"}]}
        html=make_html(data)
        self.assertNotIn("<script>",html)
        self.assertIn("&lt;script&gt;",html)
    def test_missing_binary_is_not_success(self):
        old={"generator":{"git_blob_sha":"old"},"exports":{}}
        new={"generator":{"git_blob_sha":"new"},"exports":{}}
        self.assertEqual(source_status(old,new,["missing.fbx"])["missing.fbx"],
                         "MISSING_IN_FIX")

if __name__=="__main__":
    unittest.main(verbosity=2)
