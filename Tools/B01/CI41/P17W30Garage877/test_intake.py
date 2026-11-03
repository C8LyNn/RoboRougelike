#!/usr/bin/env python3
"""Fail-closed independent QA; mutations are ephemeral strings, not gameplay edits."""
import unittest
from unittest.mock import patch
import intake

class W30RealGameGarageSourceQA(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        names=set(intake.REQUIRED)|{
          "Assets/R3/R3BuildValidation.cs",
          "Assets/Tests/Editor/GarageEditFreedomTests.cs",
        }
        cls.original={p:intake.show(intake.GARAGE,p).decode("utf-8-sig","replace") for p in names}

    def test_01_exact_original_three_file_scope_and_blob(self):
        self.assertEqual(len(intake.test_origins()),3)

    def test_02_exact_original_runtime_canonical_checks(self):
        p=intake.source_contract(self.original)
        self.assertEqual(len(p),14)
        self.assertTrue(all(p.values()))

    def test_03_changed_original_producer_byte_rejected(self):
        original_fetch=intake.show
        def mutant(ref,path):
            b=original_fetch(ref,path)
            return b+b"x" if path=="Assets/R3/R3BuildValidation.cs" else b
        with self.assertRaisesRegex(intake.GateError,"Git blob changed"):
            intake.test_origins(mutant)

    def bad(self,pathname,old,new=""):
        mutated=dict(self.original)
        self.assertIn(old,mutated[pathname])
        mutated[pathname]=mutated[pathname].replace(old,new)  # delete all references, not only the enum declaration
        with self.assertRaisesRegex(intake.GateError,"policy RED"):
            intake.source_contract(mutated)

    def test_04_remove_original_real_installation_gate(self):
        self.bad("Assets/R3/R3BuildValidation.cs",
                 "canonical.SetInstallationGate(candidate=>gate!=null?gate.Validate(candidate)")

    def test_05_remove_preview_placement_authority(self):
        self.bad("Assets/R3/R3GarageView.cs","OwnedPlacementError(Owned.Id,Cell,SelectedRotation)")

    def test_06_remove_buy_safety_before_scrap(self):
        self.bad("Assets/R3/R3VehicleModel.cs",
                 "PlacementError(cell,kind,false,weapon,rotation)!=null")

    def test_07_remove_mount_safety_before_id_mutation(self):
        self.bad("Assets/R3/R3VehicleModel.cs",
                 "OwnedPlacementError(part.Id,cell,rotation)!=null")

    def test_08_remove_physical_BUILD001_before_hard_sweep(self):
        self.bad("Assets/R3/R3BuildValidation.cs",
                 "BuildPlacementRules.CheckForInstallation(")

    def test_09_remove_saw_disc_physics(self):
        self.bad("Assets/R3/R3BuildValidation.cs",
                 "RotarySawContactMath.IsOnCuttingDisc(")

    def test_10_remove_emp_area_exemption(self):
        self.bad("Assets/R3/R3BuildValidation.cs",
                 "candidate.Weapon==R4WeaponKind.EmpEmitter)return null;")

    def test_11_remove_garage_visual_color(self):
        self.bad("Assets/B01UserExperience/B0112GarageSpatialW28/B0112GarageSpatialOverlay.cs",
                 "MechanismOrange")

    def test_12_remove_R5_real_JumpPad(self):
        self.bad("Assets/R5/R5Arena.cs",
                 "AddComponent<B01.MapJumpPad001.MapJumpPad>()")

    def test_13_remove_source_saw_safe_wallet_fixture(self):
        self.bad("Assets/Tests/Editor/GarageEditFreedomTests.cs",
                 "run.GarageMount(id,oldCell,default),Is.False")

    def test_14_tainted_file_reference_denied(self):
        for name in ("main", "a"*40):
            with self.subTest(ref=name):
                with self.assertRaises(intake.GateError):
                    intake.show(name,"../Assets/R3/R3BuildValidation.cs")

if __name__=="__main__": unittest.main()
