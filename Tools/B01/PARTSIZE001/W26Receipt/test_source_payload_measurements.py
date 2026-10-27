#!/usr/bin/env python3
"""Real original-source numeric checks and negative mutants; NOT Unity."""
import unittest
import source_payload_measurements as m

class OriginalR41StaticGeometryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.geom=m.git_bytes(".",m.CODEX_SHA,m.GEOM_PATH,m.GEOM_BLOB)
        cls.catalog=m.git_bytes(".",m.PART_SHA,m.CAT_PATH,m.CAT_BLOB)
        cls.params=m.source_parameters(cls.geom)
        cls.items={r["kind"]:r for r in m.analyze(cls.geom,cls.catalog)["rows"]}

    def test_01_actual_codex_geometry_git_blob_and_catalog(self):
        self.assertEqual(len(self.items),7)
        self.assertEqual(m.GRID,.55)
        self.assertIn("return PartSizeReferenceFixtures.Unmeasured(",self.catalog)

    def test_02_booster_source_z_rear_static_aabb(self):
        row=self.items["Booster"]
        self.assertEqual(row["payload_aabb_min_m"],[-.325,-.325,-1.0])
        self.assertEqual(row["payload_aabb_max_m"],[.325,.325,-.2])
        self.assertEqual(row["signed_cells_intersected_xyz"][2],[-2,-1,0])

    def test_03_grapple_source_forward_static_aabb(self):
        row=self.items["MagneticGrapple"]
        self.assertEqual(row["payload_aabb_min_m"],[-.325,-.325,.2])
        self.assertEqual(row["payload_aabb_max_m"],[.325,.325,1.0])
        self.assertEqual(row["signed_cells_intersected_xyz"][2],[0,1,2])

    def test_04_jumpdrive_downward_short_cylinder(self):
        self.assertEqual(self.items["JumpDrive"]["payload_aabb_min_m"],[-.5,-.61,-.5])
        self.assertEqual(self.items["JumpDrive"]["payload_aabb_max_m"],[.5,-.29,.5])

    def test_05_hover_engine_matches_jumpdrive_static_transform_only(self):
        self.assertEqual(self.items["HoverEngine"]["payload_aabb_min_m"],
                         self.items["JumpDrive"]["payload_aabb_min_m"])
        self.assertEqual(self.items["HoverEngine"]["native_physx"],"NOT_RUN")

    def test_06_blink_static_sphere_is_bigger_than_one_cell(self):
        row=self.items["Blink"]
        self.assertEqual(row["payload_aabb_min_m"],[-.425,-.425,-.425])
        self.assertEqual(row["max_overshoot_vs_single_cell_xyz_m"],[.15,.15,.15])

    def test_07_gyroscope_visual_ring_not_physical_proof(self):
        row=self.items["Gyroscope"]
        self.assertTrue(row["gyro_decorative_ring_nonphysical"])
        self.assertEqual(row["payload_aabb_max_m"],[.425,.425,.425])
        self.assertEqual(row["typed_socket"],"UNVERIFIED")

    def test_08_omnidrive_z90_cylinder_static_axes(self):
        row=self.items["OmniDrive"]
        self.assertEqual(row["payload_aabb_min_m"],[-.18,-.85,-.45])
        self.assertEqual(row["payload_aabb_max_m"],[.18,.05,.45])
        # y=-0.85 is 0.025m below signed cell -1 minimum -0.825.
        # The source axis-aligned proxy intersects THREE y layers, not two.
        self.assertEqual(row["signed_cells_intersected_xyz"][1],[-2,-1,0])

    def test_09_all_seven_outgrow_nominal_single_055_cell(self):
        for k,row in self.items.items():
            with self.subTest(kind=k):
                self.assertTrue(any(x>0 for x in row["max_overshoot_vs_single_cell_xyz_m"]))
                self.assertEqual(row["payload_collider_enabled_in_host"],"UNVERIFIED")
                self.assertEqual(row["dynamic_sweep"],"UNMEASURED")
                self.assertEqual(row["typed_socket"],"UNVERIFIED")

    def test_10_negative_mutation_missing_primitive_collider_provenance(self):
        mutated=self.geom.replace("GameObject.CreatePrimitive(type)","MyFakePrimitive(type)")
        with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_MISSING"):
            m.analyze(mutated,self.catalog)

    def test_11_negative_mutation_fake_gyroscope_ring_as_physical(self):
        altered=self.geom.replace('material,false,"Pierścień żyroskopu"',
                                  'material,true,"Pierścień żyroskopu"')
        with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_MISSING"):
            m.analyze(altered,self.catalog)

    def test_12_negative_mutation_producer_unmeasured_policy_removed(self):
        changed=self.catalog.replace("PartSizeReferenceFixtures.Unmeasured(canonicalKind,",
                                     "PartSizeReferenceFixtures.LegacyCube(canonicalKind,")
        with self.assertRaisesRegex(ValueError,"PRODUCER_NO_LONGER_FAILS_CLOSED"):
            m.analyze(self.geom,changed)

    def test_13_source_broken_yaw_axis_anchor_denied(self):
        changed=self.geom.replace("Quaternion.Euler(0,0,90)","Quaternion.Euler(90,0,0)")
        with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_MISSING"):
            m.analyze(changed,self.catalog)

    def test_14_original_git_blob_pins_narrow_sha(self):
        self.assertEqual(m.GEOM_BLOB,"76a14de4c347dd3e91b0a37487d804023eab5da1")
        self.assertEqual(m.CAT_BLOB,"6153d1758e800ec5c4210323610b061c06e8ea85")
        self.assertNotEqual(m.PART_SHA,m.CODEX_SHA)

if __name__=="__main__":
    unittest.main(verbosity=2)
