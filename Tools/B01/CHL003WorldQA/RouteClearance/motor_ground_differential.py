#!/usr/bin/env python3
"""B01-21 independent SOURCE DIFFERENTIAL: real motor ray vs authored wheel/collider geometry.
No engine, real raycast, PhysX, contact, six trials or G1 is executed.
"""
import argparse,json,math,os,re,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
PRODUCER="077e692785aff2990a2d72a80fd1db62ac57b0e8"
DEFAULT_DEMO=Path(os.environ.get("B01_21_CURRENT_CHL_DEMO",ROOT/"Assets/RoboRougelikeChallenges/CHL003/Demo/CHL003DemoBootstrap.cs"))
DEFAULT_MOTOR=Path(os.environ.get("B01_21_CURRENT_CHL_MOTOR",ROOT/"Assets/RoboRougelikeChallenges/CHL003/Runtime/CHL003VehicleMotor.cs"))

def check(p,reason):
    if not p:raise ValueError(reason)

def source_contract(demo,motor):
    for token in ("spawn.position = new Vector3(0f, 1.02f, 0f)",
            "new Vector3(.82f, .82f, .82f)",
            "new Vector3(side * width,", "-.52f, end * (heavy ? 1.15f : .75f)",
            "float width = heavy ? 1.0f : .59f;"):
        check(token in demo,"DEMO_DRIFT:"+token)
    ground=motor.split("private bool HasRealGroundContact()",1)
    check(len(ground)==2,"MOTOR_METHOD_MISSING")
    body=ground[1].split("private void FixedUpdate()",1)[0]
    for token in ("probe.position + Vector3.up * .14f",
            "Physics.RaycastNonAlloc(", "groundHits, .95f",
            "QueryTriggerInteraction.Ignore",
            "c.attachedRigidbody == body",
            "groundHits[i].normal.y < .5f","SupportRayCount++",
            "return SupportRayCount > 0"):
        check(token in body,"MOTOR_SOURCE_DRIFT:"+token)
    check(".distance" not in body,"NEW_HIT_DISTANCE_GUARD_RECHECK")
    check("if (!IsGrounded) return;" in motor and "body.AddForce(" in motor
          and "body.AddTorque(" in motor,"TRACTIVE_GUARD_DRIFT")
    surfaces={}
    for name in ("APPROACH_GROUND","NARROW_SOLID_BRIDGE","REAL_LOW_BARRIER"):
        pattern=r'Box\("'+name+r'",\s*new Vector3\(([^)]+)\),\s*new Vector3\(([^)]+)\)'
        m=re.search(pattern,demo)
        check(m is not None,"SURFACE_MISSING:"+name)
        c=[float(t.strip().removesuffix("f")) for t in m[1].split(",")]
        v=[float(t.strip().removesuffix("f")) for t in m[2].split(",")]
        check(len(c)==len(v)==3 and all(t>0 for t in v),"INVALID_BOX_DIM")
        surfaces[name]={"top":c[1]+v[1]/2,
                        "xmin":c[0]-v[0]/2,"xmax":c[0]+v[0]/2,
                        "zmin":c[2]-v[2]/2,"zmax":c[2]+v[2]/2}
    return surfaces

def measured(rig,surface,label,root_y,center_z,surfaces):
    box=surfaces[surface]
    x=1 if rig=="Heavy" else .59
    z=1.15 if rig=="Heavy" else .75
    wheel_xy=[(a,center_z+b) for a in (-x,x) for b in (-z,z)]
    aligned=sum(box["xmin"]<=a<=box["xmax"] and box["zmin"]<=b<=box["zmax"]
                for a,b in wheel_xy)
    # Original SphereCollider scale .82 => radius .41, center local Y=-.52;
    # original ray origin .14 above wheel center, length .95. Ignore tricks.
    bottom=root_y-.52-.41
    distance=root_y-.52+.14-box["top"]
    ray=aligned>0 and 0<=distance<=.95
    contact_gap=bottom-box["top"]
    return {"rig":rig,"surface":surface,"scenario":label,"rootY_M":root_y,
            "rootZ_M":center_z,"supportTopY_M":box["top"],
            "wheelsAboveSourceBoxXZ":aligned,"wheelBottomGapM":round(contact_gap,6),
            "sourceRayToSurfaceTopM":round(distance,6),
            "sourceRayCanReach":ray,"wheelSurfaceGeometricallySeparated":contact_gap>0.0001,
            "conditionalMotorEnablesIfPhysicsRayHits":ray,
            "observedRaycast":"NOT_RUN","observedPhysXContact":"NOT_RUN"}

def calculate(demo,motor):
    boxes=source_contract(demo,motor);data=[]
    for rig in ("Light","Heavy"):
        for name,y in (("REAL_SPAWN",1.02),("HOVER_035",1.28),("OUTSIDE_RAY_041",1.34)):
            data.append(measured(rig,"APPROACH_GROUND",name,y,0,boxes))
        for name,y in (("IDEAL_TOUCH",2.61),("HOVER_035",2.96)):
            data.append(measured(rig,"NARROW_SOLID_BRIDGE",name,y,21,boxes))
        # Narrow obstacle supports one axle; remaining axle can be airborne too.
        back=1.15 if rig=="Heavy" else .75
        data.append(measured(rig,"REAL_LOW_BARRIER","HOVER_009_ONE_AXLE",
                             1.28,8.7+back,boxes))
    check(len(data)==12,"CASE_COUNT")
    for row in data:
        if row["scenario"]=="OUTSIDE_RAY_041":
            check(not row["sourceRayCanReach"],"RAY_RANGE_MUTATION")
        elif row["scenario"]=="IDEAL_TOUCH":
            check(not row["wheelSurfaceGeometricallySeparated"] and
                  row["sourceRayCanReach"],"IDEAL_TOUCH_MUTATION")
        else:
            check(row["sourceRayCanReach"] and row["wheelSurfaceGeometricallySeparated"],
                  "PHANTOM_GROUND_DIFFERENTIAL_NOT_PROVEN")
    return {"taskId":"B01-21-PASS3-MAP-VS-MOTOR-20261027",
            "producerInputSHA":PRODUCER,
            "originalDemoGitBlobSHA1":"9308ceb9eab89d1ab483b560ff3ed58fa6d2d32e",
            "originalMotorGitBlobSHA1":"2ec5334539cfe3b1108ad2c868e04be7a9b18129",
            "originalRayLengthM":.95,"originalWheelRadiusM":.41,
            "originalRayOriginAboveWheelCenterM":.14,
            "originalWheelCenterRelativeY_M":-.52,
            "originalSpawnWheelBottomGapM":.09,
            "maxFlatWheelGapGeometricallyRayReachableM":.40,
            "conclusion":"SOURCE_RAY_REACH_CAN_COEXIST_WITH_NO_SPHERE_FLOOR_CONTACT",
            "notAnObservedEngineFailure":True,"nativeUnity":"NOT_RUN",
            "physicalContact":"NOT_RUN","rows":data}

class ContractTests(unittest.TestCase):
    def setUp(self):
        self.demo=DEFAULT_DEMO.read_text(encoding="utf-8")
        self.motor=DEFAULT_MOTOR.read_text(encoding="utf-8")
        self.r=calculate(self.demo,self.motor)
    def row(self,rig,zone,label):
        return next(x for x in self.r["rows"] if x["rig"]==rig and
                    x["surface"]==zone and x["scenario"]==label)
    def reject(self,demo=None,motor=None):
        with self.assertRaises(ValueError):calculate(demo or self.demo,motor or self.motor)
    def test_01_actual_source_case_count(self):self.assertEqual(len(self.r["rows"]),12)
    def test_02_both_real_spawn_nine_centimeters_air(self):
        for rig in ("Light","Heavy"):
            a=self.row(rig,"APPROACH_GROUND","REAL_SPAWN")
            self.assertEqual(a["wheelBottomGapM"],.09)
            self.assertTrue(a["sourceRayCanReach"] and a["wheelSurfaceGeometricallySeparated"])
    def test_03_both_ground_hover_035_ray_still_reaches(self):
        for rig in ("Light","Heavy"):
            a=self.row(rig,"APPROACH_GROUND","HOVER_035")
            self.assertEqual(a["wheelBottomGapM"],.35);self.assertTrue(a["sourceRayCanReach"])
    def test_04_both_ground_hover_041_outside_ray(self):
        for rig in ("Light","Heavy"):
            self.assertFalse(self.row(rig,"APPROACH_GROUND","OUTSIDE_RAY_041")["sourceRayCanReach"])
    def test_05_bridge_support_top_actual_height(self):
        for rig in ("Light","Heavy"):
            a=self.row(rig,"NARROW_SOLID_BRIDGE","IDEAL_TOUCH")
            self.assertEqual(a["supportTopY_M"],1.68)
            self.assertEqual(a["wheelBottomGapM"],0)
    def test_06_bridge_035_hover_misidentified_if_hit(self):
        for rig in ("Light","Heavy"):
            a=self.row(rig,"NARROW_SOLID_BRIDGE","HOVER_035")
            self.assertEqual(a["wheelBottomGapM"],.35);self.assertTrue(a["sourceRayCanReach"])
    def test_07_obstacle_top_one_axle_is_eligible_even_with_gap(self):
        for rig in ("Light","Heavy"):
            a=self.row(rig,"REAL_LOW_BARRIER","HOVER_009_ONE_AXLE")
            self.assertEqual(a["supportTopY_M"],.26)
            self.assertEqual(a["wheelsAboveSourceBoxXZ"],2)
            self.assertEqual(a["wheelBottomGapM"],.09)
            self.assertTrue(a["sourceRayCanReach"])
    def test_08_motor_policy_does_not_measure_collision_distance(self):
        self.assertTrue(self.r["notAnObservedEngineFailure"])
        self.assertAlmostEqual(self.r["maxFlatWheelGapGeometricallyRayReachableM"],.40)
    def test_09_not_native_pass(self):
        self.assertEqual(self.r["nativeUnity"],"NOT_RUN")
        self.assertTrue(all(x["observedPhysXContact"]=="NOT_RUN" for x in self.r["rows"]))
    def test_10_mutant_raycast_length_rejected(self):
        self.reject(motor=self.motor.replace("groundHits, .95f","groundHits, .45f",1))
    def test_11_mutant_correct_distance_guard_needs_new_review(self):
        self.reject(motor=self.motor.replace("SupportRayCount++;",
              "if (groundHits[i].distance > .55f) continue; SupportRayCount++;",1))
    def test_12_mutant_spawn_height_rejected(self):
        self.reject(demo=self.demo.replace("new Vector3(0f, 1.02f, 0f)",
                   "new Vector3(0f, .93f, 0f)",1))
    def test_13_mutant_wheel_radius_rejected(self):
        self.reject(demo=self.demo.replace("new Vector3(.82f, .82f, .82f)",
                   "new Vector3(.62f, .62f, .62f)",1))
    def test_14_mutant_normal_filter_rejected(self):
        self.reject(motor=self.motor.replace("groundHits[i].normal.y < .5f",
                   "groundHits[i].normal.y < -.5f",1))
    def test_15_mutant_real_bridge_height_rejected(self):
        self.reject(demo=self.demo.replace('Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.38f, 24.5f)',
                  'Box("NARROW_SOLID_BRIDGE", new Vector3(0f, 1.58f, 24.5f)',1))
    def test_16_mutant_barrier_removed_rejected(self):
        self.reject(demo=self.demo.replace('Box("REAL_LOW_BARRIER",',
                   'Box("NOT_AN_OBSTACLE",',1))

def main():
    p=argparse.ArgumentParser();p.add_argument("--output",type=Path,required=True)
    p.add_argument("--demo",type=Path,default=DEFAULT_DEMO)
    p.add_argument("--motor",type=Path,default=DEFAULT_MOTOR)
    p.add_argument("--selftest",action="store_true")
    a=p.parse_args()
    report=calculate(a.demo.read_text(encoding="utf-8"),a.motor.read_text(encoding="utf-8"))
    a.output.parent.mkdir(parents=True,exist_ok=True)
    a.output.write_text(json.dumps(report,sort_keys=True,indent=2)+"\n",encoding="utf-8")
    if a.selftest:
        # Use explicitly pinned producer files for actual test inputs in CI.
        os.environ["B01_21_CURRENT_CHL_DEMO"]=str(a.demo)
        os.environ["B01_21_CURRENT_CHL_MOTOR"]=str(a.motor)
        globals()["DEFAULT_DEMO"]=a.demo;globals()["DEFAULT_MOTOR"]=a.motor
        result=unittest.TextTestRunner(verbosity=2).run(
            unittest.defaultTestLoader.loadTestsFromTestCase(ContractTests))
        if not result.wasSuccessful() or result.testsRun!=16:
            raise SystemExit(1)
    print("B01_21_PASS3_MOTOR_MAP_SOURCE_DIFFERENTIAL_PASS cases=12 tests="+
          ("16" if a.selftest else "NOT_RUN")+" native=NOT_RUN")
if __name__=="__main__":main()
