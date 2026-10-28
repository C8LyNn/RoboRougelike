#!/usr/bin/env python3
"""Real original-source numeric checks and negative mutants; NOT Unity."""
import unittest
import source_payload_measurements as m

class OriginalR41StaticGeometryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.geom=m.git_bytes(".",m.CODEX_SHA,m.GEOM_PATH,m.GEOM_BLOB)
        cls.catalog=m.git_bytes(".",m.PART_SHA,m.CAT_PATH,m.CAT_BLOB)
        cls.host=m.git_bytes(".",m.CODEX_SHA,m.HOST_PATH,m.HOST_BLOB)
        cls.params=m.source_parameters(cls.geom)
        cls.items={r["kind"]:r for r in m.analyze(cls.geom,cls.catalog,cls.host)["rows"]}

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

    def test_09_unit_parent_counterfactual_only_all_seven_overhang(self):
        for k,row in self.items.items():
            with self.subTest(kind=k):
                self.assertTrue(any(x>0 for x in row["max_overshoot_vs_single_cell_xyz_m"]))
                self.assertEqual(row["payload_collider_enabled_in_host"],"UNVERIFIED")
                self.assertEqual(row["dynamic_sweep"],"UNMEASURED")
                self.assertEqual(row["typed_socket"],"UNVERIFIED")

    def test_10_negative_mutation_missing_primitive_collider_provenance(self):
        mutated=self.geom.replace("GameObject.CreatePrimitive(type)","MyFakePrimitive(type)")
        with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_MISSING"):
            m.analyze(mutated,self.catalog,self.host)

    def test_11_negative_mutation_fake_gyroscope_ring_as_physical(self):
        altered=self.geom.replace('material,false,"Pierścień żyroskopu"',
                                  'material,true,"Pierścień żyroskopu"')
        with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_MISSING"):
            m.analyze(altered,self.catalog,self.host)

    def test_12_negative_mutation_producer_unmeasured_policy_removed(self):
        changed=self.catalog.replace("PartSizeReferenceFixtures.Unmeasured(canonicalKind,",
                                     "PartSizeReferenceFixtures.LegacyCube(canonicalKind,")
        with self.assertRaisesRegex(ValueError,"PRODUCER_NO_LONGER_FAILS_CLOSED"):
            m.analyze(self.geom,changed,self.host)

    def test_13_source_broken_yaw_axis_anchor_denied(self):
        changed=self.geom.replace("Quaternion.Euler(0,0,90)","Quaternion.Euler(90,0,0)")
        with self.assertRaisesRegex(ValueError,"SOURCE_ANCHOR_MISSING"):
            m.analyze(changed,self.catalog,self.host)

    def test_14_original_git_blob_pins_narrow_sha(self):
        self.assertEqual(m.GEOM_BLOB,"76a14de4c347dd3e91b0a37487d804023eab5da1")
        self.assertEqual(m.CAT_BLOB,"6153d1758e800ec5c4210323610b061c06e8ea85")
        self.assertNotEqual(m.PART_SHA,m.CODEX_SHA)

    def test_15_actual_assembly_scale_and_solid_call_pinned(self):
        self.assertIn("item.transform.localScale=Vector3.one*0.52f;",self.host)
        self.assertEqual(m.host_scale_from_original_assembly(self.host),.52)
        self.assertIn("true,out partMuzzle);",self.host)
        self.assertEqual(m.HOST_BLOB,"1b49c7365388694ebffacc5a4f20b3572150577a")

    def test_16_omnidrive_real_assembly_is_y_minus1_not_minus2(self):
        r=self.items["OmniDrive"]
        self.assertEqual(r["signed_cells_intersected_xyz"][1],[-2,-1,0])
        self.assertEqual(r["assembled_payload_aabb_min_m"],[-.0936,-.442,-.234])
        self.assertEqual(r["assembled_payload_aabb_max_m"],[.0936,.026,.234])
        self.assertEqual(r["assembled_root_plus_payload_aabb_min_m"],[-.26,-.442,-.26])
        self.assertEqual(r["assembled_root_plus_payload_aabb_max_m"],[.26,.26,.26])
        self.assertEqual(r["assembled_signed_cells_xyz"][1],[-1,0])
        self.assertEqual(r["assembled_overshoot_vs_one_cell_xyz_m"],[0,.167,0])

    def test_17_booster_and_grapple_are_shorter_in_actual_parent(self):
        rear=self.items["Booster"]
        front=self.items["MagneticGrapple"]
        self.assertEqual(rear["assembled_payload_aabb_min_m"],[-.169,-.169,-.52])
        self.assertEqual(rear["assembled_root_plus_payload_aabb_max_m"],[.26,.26,.26])
        self.assertEqual(rear["assembled_signed_cells_xyz"][2],[-1,0])
        self.assertEqual(front["assembled_payload_aabb_max_m"],[.169,.169,.52])
        self.assertEqual(front["assembled_signed_cells_xyz"][2],[0,1])
        self.assertEqual(rear["assembled_overshoot_vs_one_cell_xyz_m"],[0,0,.245])
        self.assertEqual(front["assembled_overshoot_vs_one_cell_xyz_m"],[0,0,.245])

    def test_18_jump_and_hover_child_touches_y_minus1_only(self):
        for kind in ("JumpDrive","HoverEngine"):
            with self.subTest(kind=kind):
                r=self.items[kind]
                self.assertEqual(r["assembled_payload_aabb_min_m"],[-.26,-.3172,-.26])
                self.assertEqual(r["assembled_payload_aabb_max_m"],[.26,-.1508,.26])
                self.assertEqual(r["assembled_root_plus_payload_aabb_max_m"],[.26,.26,.26])
                self.assertEqual(r["assembled_signed_cells_xyz"][1],[-1,0])
                self.assertEqual(r["assembled_overshoot_vs_one_cell_xyz_m"],[0,.0422,0])

    def test_19_blink_and_gyro_sphere_fit_actual_root_physical(self):
        for kind in ("Blink","Gyroscope"):
            with self.subTest(kind=kind):
                r=self.items[kind]
                self.assertEqual(r["assembled_payload_aabb_min_m"],[-.221]*3)
                self.assertEqual(r["assembled_payload_aabb_max_m"],[.221]*3)
                self.assertEqual(r["assembled_root_plus_payload_aabb_min_m"],[-.26]*3)
                self.assertEqual(r["assembled_root_plus_payload_aabb_max_m"],[.26]*3)
                self.assertEqual(r["assembled_signed_cells_xyz"],[[0],[0],[0]])
                self.assertFalse(r["assembled_overhang_any"])
                self.assertEqual(r["assembled_overshoot_vs_one_cell_xyz_m"],[0,0,0])
        self.assertTrue(self.items["Gyroscope"]["gyro_decorative_ring_nonphysical"])

    def test_20_five_assembled_source_shapes_overhang_but_two_do_not(self):
        escaping={r["kind"] for r in self.items.values() if r["assembled_overhang_any"]}
        self.assertEqual(escaping,{"Booster","MagneticGrapple","JumpDrive","HoverEngine","OmniDrive"})
        for r in self.items.values():
            self.assertTrue(r["assembled_host_requests_solid_payload"])
            self.assertFalse(r["assembled_native_collider_verified"])
            self.assertEqual(r["native_physx"],"NOT_RUN")
            self.assertEqual(r["typed_socket"],"UNVERIFIED")

    def test_21_scale_mutant_090_rejected_in_actual_source(self):
        altered=self.host.replace("item.transform.localScale=Vector3.one*0.52f;",
                                  "item.transform.localScale=Vector3.one*0.90f;")
        with self.assertRaisesRegex(ValueError,"HOST_SCALE_SOURCE_CHANGED"):
            m.analyze(self.geom,self.catalog,altered)

    def test_22_false_host_solid_flag_rejected(self):
        altered=self.host.replace("true,out partMuzzle);","false,out partMuzzle);")
        with self.assertRaisesRegex(ValueError,"HOST_SOURCE_CONTRACT_CHANGED"):
            m.analyze(self.geom,self.catalog,altered)

    def test_23_missing_yaw_quarterturn_provenance_rejected(self):
        altered=self.host.replace("R3MountingGrid.Rotation(state.RotationQuarterTurns)",
                                  "Quaternion.identity")
        with self.assertRaisesRegex(ValueError,"HOST_SOURCE_CONTRACT_CHANGED"):
            m.analyze(self.geom,self.catalog,altered)

    def test_24_root_plus_payload_required_not_only_child(self):
        r=self.items["Blink"]
        self.assertNotEqual(r["assembled_payload_aabb_max_m"],
                            r["assembled_root_plus_payload_aabb_max_m"])
        self.assertEqual(r["assembled_root_plus_payload_aabb_max_m"],[.26]*3)


if __name__=="__main__":
    unittest.main(verbosity=2)
