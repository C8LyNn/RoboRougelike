using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace B01.CHL003.Tests
{
    /// <summary>
    /// Real authored Unity Test Framework native acceptance; NEVER label
    /// PASS until Unity6000.3.14f1 actually executes and emits XML/exits.
    /// Same serialized demo runtime course, no synthetic checkpoint calls.
    /// </summary>
    public sealed class CHL003DemoAcceptancePlayModeTests
    {
        private GameObject bootstrapObject;
        private CHL003DemoBootstrap demo;

        [UnitySetUp]
        public IEnumerator SetupRealCourse()
        {
            bootstrapObject = new GameObject("CHL003_NATIVE_REAL_DEMO_TEST");
            demo = bootstrapObject.AddComponent<CHL003DemoBootstrap>();
            yield return null; // real MonoBehaviour.Start creates original primitives
            Assert.IsNotNull(demo.Course);
            Assert.IsNotNull(demo.ActiveVehicle);
            Assert.AreEqual(4,demo.Course.Core.CheckpointCount);
            demo.enabled = false; // freeze demo UI input; Rigidbody motor still runs
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator RemoveRealCourse()
        {
            if (bootstrapObject != null) Object.Destroy(bootstrapObject);
            yield return null;
        }

        private IEnumerator DriveUntilTerminal(int maxFixedSteps = 600)
        {
            demo.Motor.SetAxes(1f, 0f);
            for (int tick=0;tick<maxFixedSteps &&
                demo.Course.Core.State == CHL003CourseCore.Phase.Running;tick++)
                yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator P07_SameNativeCourseLightHeavyThreeRunsEachWithRealContact()
        {
            float lightMass=0,heavyMass=0;
            float lightWidth=0,heavyWidth=0;
            for(int variant=0;variant<2;variant++)
            {
                demo.SelectRigForTest(variant);
                if (variant==0)
                {
                    lightMass=demo.ActiveVehicle.mass;
                    lightWidth=demo.Course.Metrics.WidthMeters;
                }
                else
                {
                    heavyMass=demo.ActiveVehicle.mass;
                    heavyWidth=demo.Course.Metrics.WidthMeters;
                }
                for(int trial=0;trial<3;trial++)
                {
                    if (trial>0) demo.RetryForTest();
                    yield return DriveUntilTerminal();
                    var core=demo.Course.Core;
                    var measured=demo.Course.Metrics;
                    Assert.AreEqual(CHL003CourseCore.Phase.Completed,core.State,
                        measured.Profile+" physical attempt "+measured.Attempt+
                        " ended "+core.State+" checkpoint="+core.CheckpointIndex);
                    Assert.AreEqual(4,core.CheckpointIndex);
                    Assert.AreEqual(1,measured.Completions);
                    Assert.AreEqual(4,measured.Checkpoints);
                    Assert.Greater(measured.PhysicsSamples,4);
                    Assert.Greater(measured.DistanceMeters,35f);
                    Assert.Greater(measured.CollisionEntries,0);
                    Assert.Greater(measured.ContactPoints,0);
                    Assert.Greater(measured.RampContacts,0,
                        "Real ramp contact is required");
                    Assert.Greater(measured.BridgeContacts,0,
                        "Real bridge contact is required");
                    Debug.Log("CHL003_NATIVE_REAL_TRIAL "+
                        measured.SummaryCsv().TrimEnd());
                }
            }
            Assert.AreEqual(270f,lightMass);
            Assert.AreEqual(1150f,heavyMass);
            Assert.Less(lightWidth,heavyWidth);
            Assert.Greater(heavyWidth - lightWidth,.5f);
        }

        [UnityTest]
        public IEnumerator P08_FinalCheckpointAndFinishCoLocatedNeverCompleteSameFixedStep()
        {
            demo.FinishTransform.position = demo.FinalCheckpointTransform.position;
            Physics.SyncTransforms();
            int lastCpStep=-1,winStep=-1,wins=0;
            demo.Course.Core.CheckpointReached += n =>
            {
                if(n==4)lastCpStep=(int)demo.Course.PhysicsStep;
            };
            demo.Course.Core.Completed += _ =>
            {
                winStep=(int)demo.Course.PhysicsStep;wins++;
            };
            yield return DriveUntilTerminal(650);
            Assert.AreEqual(CHL003CourseCore.Phase.Completed,demo.Course.Core.State);
            Assert.GreaterOrEqual(lastCpStep,0);
            Assert.Greater(winStep,lastCpStep,
                "CP4 and Finish cannot complete on same Unity physics step");
            Assert.AreEqual(1,wins);
            for(int tick=0;tick<5;tick++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(1,wins,"Persistent collider overlap must not repeat win");
        }

        [UnityTest]
        public IEnumerator P09_NativePhysicsPauseFallRetryAndNoStaleWin()
        {
            demo.Motor.SetAxes(1f, 0f);
            for(int n=0;n<5;n++)yield return new WaitForFixedUpdate();
            demo.Course.SetPaused(true);
            var position=demo.ActiveVehicle.position;
            float before=demo.Course.Core.ElapsedSeconds;
            Assert.IsTrue(demo.ActiveVehicle.isKinematic,
                "True physics pause is mandatory; not merely stopping clock");
            for(int n=0;n<10;n++)yield return new WaitForFixedUpdate();
            Assert.Less(Vector3.Distance(position,demo.ActiveVehicle.position),.001f);
            Assert.AreEqual(before,demo.Course.Core.ElapsedSeconds,.0001f);
            demo.Course.SetPaused(false);
            Assert.IsFalse(demo.ActiveVehicle.isKinematic);
            demo.ActiveVehicle.position = new Vector3(0f,-9f,0f);
            Physics.SyncTransforms();
            for(int n=0;n<3;n++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(CHL003CourseCore.Phase.Failed,demo.Course.Core.State);
            Assert.AreEqual(1,demo.Course.Metrics.Falls);
            Assert.AreEqual(0,demo.Course.Metrics.Completions);
            demo.RetryForTest();
            Assert.AreEqual(CHL003CourseCore.Phase.Running,demo.Course.Core.State);
            Assert.AreEqual(0,demo.Course.Metrics.Falls);
            Assert.AreEqual(0,demo.Course.Core.CheckpointIndex);
            Assert.AreEqual(0,demo.Course.Metrics.ContactPoints);
            Assert.AreEqual(0f,demo.Course.Metrics.DistanceMeters);
            for(int n=0;n<3;n++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.Course.Metrics.Completions);
        }

        [UnityTest]
        public IEnumerator P10_AirborneMotorDoesNotAccelerateOrSteerWithoutRealTerrain()
        {
            // Actual Rigidbody, real four wheel-ray ground probe and gravity.
            // No pseudo-Physics substitutes or direct checkpoint completions.
            demo.ActiveVehicle.position = new Vector3(0f, 8f, 2f);
            demo.ActiveVehicle.linearVelocity = Vector3.zero;
            demo.ActiveVehicle.angularVelocity = Vector3.zero;
            demo.Motor.SetAxes(1f, 1f);
            Physics.SyncTransforms();
            for (int n=0;n<4;n++) yield return new WaitForFixedUpdate();
            Assert.GreaterOrEqual(demo.Motor.AirborneSteps, 3);
            Assert.IsFalse(demo.Motor.IsGrounded);
            Assert.AreEqual(0,demo.Motor.SupportRayCount);
            Assert.Less(new Vector2(demo.ActiveVehicle.linearVelocity.x,
                demo.ActiveVehicle.linearVelocity.z).magnitude,.02f,
                "No body horizontal acceleration by propulsive air control");
            Assert.Less(Mathf.Abs(demo.ActiveVehicle.angularVelocity.y),.02f);
        }

        [UnityTest]
        public IEnumerator P11_RealBackwardCrossAndWheelGrazeMustNotScore()
        {
            demo.Motor.SetAxes(0f,0f);
            var body=demo.ActiveVehicle;
            // Start BEYOND CP0 and pass back through its real trigger.
            body.position = new Vector3(0f,1.02f,6f);
            body.linearVelocity = Vector3.back*7f;
            Physics.SyncTransforms();
            for(int n=0;n<16;n++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.Course.Core.CheckpointIndex,
                "Reverse crossing must never earn CP0");
            demo.Course.Retry();
            demo.Course.Begin();
            // Physically touch gate with outside wheel while whole body
            // clearance falls outside aperture. Underlying floor stays valid.
            body.position=new Vector3(1.5f,1.02f,2.6f);
            body.linearVelocity=Vector3.forward*10f;
            body.angularVelocity=Vector3.zero;
            Physics.SyncTransforms();
            for(int n=0;n<18;n++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.Course.Core.CheckpointIndex,
                "One wheel grazing CP0 must never score entire vehicle");
        }

        [UnityTest]
        public IEnumerator P12_DrivingForceOccursOnlyWhenRealWheelGroundRaySupports()
        {
            demo.Motor.SetAxes(1f,0f);
            var body=demo.ActiveVehicle;
            body.linearVelocity=Vector3.zero;
            for(int n=0;n<24;n++)yield return new WaitForFixedUpdate();
            Assert.Greater(demo.Motor.GroundedSteps,3,
                "Four actual wheel rays should find the CHL ground");
            Assert.Greater(body.linearVelocity.z,.4f,
                "Real grounded Rigidbody must accelerate by AddForce");
            Assert.AreEqual(0,demo.Course.Core.CheckpointIndex,
                "The first gate should not be rewarded by time or ray cast alone");
        }

        [UnityTest]
        public IEnumerator P13_ElapsedTimeTracksPhysicsTicksNotRenderedFrames()
        {
            demo.Motor.SetAxes(0f,0f);
            yield return new WaitForFixedUpdate();
            long step0=demo.Course.PhysicsStep;
            float time0=demo.Course.Core.ElapsedSeconds;
            // A variable number of rendered frames cannot create phantom
            // elapsed duration if no fixed simulation step occurred.
            for (int i=0;i<90;i++) yield return null;
            long fixedTicks=demo.Course.PhysicsStep-step0;
            float time=demo.Course.Core.ElapsedSeconds-time0;
            Assert.Greater(fixedTicks,0);
            Assert.AreEqual(fixedTicks*Time.fixedDeltaTime,time,.005f,
                "Trial timing must use exactly physics fixed ticks");
        }

        [UnityTest]
        public IEnumerator P14_HeavyYaw8_RealGateMatchesBridgeRatherThanRejectingClearance()
        {
            // Upstream B01-21 PR #295 measured real CHL wheel spheres +
            // chassis: Heavy yaw8 projected width ~3.1205m, so the 3.7m
            // physical bridge admits >=.25m clearance, while an old 3.5m
            // checkpoint blocked equivalent alignment despite traversability.
            // Genuine PhysX negative -> Retry -> positive on SAME Rigidbody.
            demo.SelectRigForTest(1);
            demo.enabled = false;
            Rigidbody body=demo.ActiveVehicle;
            var trigger=demo.FirstCheckpointTransform.GetComponent<BoxCollider>();
            Assert.IsNotNull(trigger);
            Assert.IsTrue(trigger.isTrigger);
            Assert.AreEqual(3.70f,demo.FirstCheckpointTransform.lossyScale.x,.0001f);
            demo.Motor.SetAxes(0f,0f);
            demo.FirstCheckpointTransform.localScale = new Vector3(3.50f,4.5f,.16f);
            Physics.SyncTransforms();
            body.position=new Vector3(0f,1.02f,4.0f);
            body.rotation=Quaternion.Euler(0f,8f,0f);
            body.linearVelocity=Vector3.forward*8f;
            body.angularVelocity=Vector3.zero;
            Physics.SyncTransforms();
            for(int i=0;i<24;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(0,demo.Course.Core.CheckpointIndex,
                "OLD physically narrower gate cannot credit yaw8 Heavy full hull");

            demo.RetryForTest();
            demo.Motor.SetAxes(0f,0f);
            demo.FirstCheckpointTransform.localScale = new Vector3(3.70f,4.5f,.16f);
            body.position=new Vector3(0f,1.02f,4.0f);
            body.rotation=Quaternion.Euler(0f,8f,0f);
            body.linearVelocity=Vector3.forward*8f;
            body.angularVelocity=Vector3.zero;
            Physics.SyncTransforms();
            for(int i=0;i<24;i++)yield return new WaitForFixedUpdate();
            Assert.AreEqual(1,demo.Course.Core.CheckpointIndex,
                "W17 same-bridge-width 3.70m CP0 should credit actual physically" +
                " forward-cleared Heavy at yaw8 with conservative 0.25m margin");
            Assert.AreEqual(1,demo.Course.Metrics.Checkpoints);
            Assert.AreEqual(0,demo.Course.Metrics.Completions);
        }
    }
}
