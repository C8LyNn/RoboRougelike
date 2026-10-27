using System;
using NUnit.Framework;

namespace B01.CHL003.Tests
{
    public sealed class CHL003FinishAndTelemetryEditModeTests
    {
        [Test] public void E11_FinishContactBeforeFinalCheckpointCannotWinSameFixedStep()
        {
            var a=new CHL003FinishArbiter();
            Assert.IsTrue(a.ObserveFinishContact(17,true));
            a.LastCheckpointAccepted(17);
            Assert.IsFalse(a.TryCommit(17,true,true,false));
            Assert.IsTrue(a.TryCommit(18,true,true,false));
            Assert.IsFalse(a.TryCommit(19,true,true,false));
        }

        [Test] public void E12_FinalCheckpointBeforeFinishAlsoDeferred()
        {
            var a=new CHL003FinishArbiter();
            a.LastCheckpointAccepted(18);
            Assert.IsTrue(a.ObserveFinishContact(18,true));
            Assert.IsFalse(a.TryCommit(18,true,true,false));
            Assert.IsTrue(a.TryCommit(19,true,true,false));
        }

        [Test] public void E13_ExpiredContactPausedAndRetryNeverAutoWin()
        {
            var a=new CHL003FinishArbiter();
            Assert.IsFalse(a.ObserveFinishContact(1,false));
            Assert.IsFalse(a.TryCommit(3,true,true,false));
            a.LastCheckpointAccepted(3);
            a.ObserveFinishContact(3,true);
            Assert.IsFalse(a.TryCommit(4,true,false,false));
            Assert.IsFalse(a.TryCommit(5,true,true,false));
            a.ObserveFinishContact(6,true);
            Assert.IsFalse(a.TryCommit(7,true,true,true));
            a.Reset();
            Assert.IsFalse(a.TryCommit(8,true,true,false));
            Assert.Throws<ArgumentOutOfRangeException>(()=>
                a.ObserveFinishContact(-1,true));
        }

        [Test] public void E14_ActualContactFramesGenerateInspectableReproducibleCounters()
        {
            var m=new CHL003TrialMetrics();
            m.Reset("LIGHT",1,270,2.0f,2.30f);
            m.Begin();
            m.RecordPhysics(0,1,0,0,0);
            m.RecordPhysics(0,1.5f,5,8f,.2f);
            m.RecordCollision("CHL003_REAL_LOW_BARRIER",4,true);
            m.RecordCollision("CHL003_SOLID_RAMP",2,true);
            m.RecordCollision("CHL003_NARROW_SOLID_BRIDGE",3,true);
            m.RecordCheckpoint(1,.2f);
            m.RecordCompleted(.3f);
            Assert.AreEqual(3,m.CollisionEntries);
            Assert.AreEqual(9,m.ContactPoints);
            Assert.AreEqual(4,m.BarrierContacts);
            Assert.AreEqual(2,m.RampContacts);
            Assert.AreEqual(3,m.BridgeContacts);
            Assert.AreEqual(2,m.PhysicsSamples);
            Assert.Greater(m.DistanceMeters,5f);
            Assert.AreEqual(1,m.Completions);
            Assert.IsTrue(m.ToCsv().Contains("COLLISION"));
            StringAssert.Contains("Completed",m.SummaryCsv());
        }

        [Test] public void E15_RetryHeavyKeepsRigDifferenceClearsFallAndTime()
        {
            var m=new CHL003TrialMetrics();
            m.Reset("HEAVY",2,1150,2.82f,3.40f);
            m.Begin();
            m.RecordPhysics(0,1,0,0,0);
            m.RecordFailed("FellOffCourse",3f);
            Assert.AreEqual(1,m.Falls);
            m.Reset("HEAVY",3,1150,2.82f,3.40f);
            Assert.AreEqual(0,m.Falls);
            Assert.AreEqual(0,m.Completions);
            Assert.AreEqual(0,m.PhysicsSamples);
            Assert.AreEqual(0,m.ContactPoints);
            Assert.AreEqual(0f,m.ElapsedSeconds);
            Assert.AreEqual(1150,m.MassKg);
            Assert.AreEqual(2.82f,m.WidthMeters);
            Assert.Throws<ArgumentException>(()=>
                m.Reset("FAKE",1,1,1,1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>
                m.RecordCollision("BARRIER",-1,true));
        }

        [Test] public void E16_ActualForwardFullHullCrossesGateWithClearance()
        {
            Assert.IsTrue(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1.41f,1.41f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsTrue(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
        }

        [Test] public void E17_ReverseAndWheelOnlyHitDoNotCreditGate()
        {
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                .15f,-.15f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1.0f,1.65f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,-.01f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.12f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,-1f));
        }

        [Test] public void E18_NonFiniteAndApertureVerticalOverflowFailClosed()
        {
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                float.NaN,.1f,-1f,1f,-2.20f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.1f,-1f,1f,-2.50f,-.5f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.1f,.1f,-1f,1f,-2.20f,2.7f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
        }

        [Test] public void E19_UpstreamB0121HeavyYaw8OldGateFailsBridgeAlignedGatePasses()
        {
            // Exact B01-21 source math of 4 physical sphere wheels
            // (radius .410, center X±1.0 Z±1.15) and Heavy 2.5x3.4 hull.
            // This is EditMode analytic contract, not an actual PhysX drive.
            float r = 8f * UnityEngine.Mathf.Deg2Rad;
            float c = UnityEngine.Mathf.Abs(UnityEngine.Mathf.Cos(r));
            float s = UnityEngine.Mathf.Abs(UnityEngine.Mathf.Sin(r));
            float halfWheel = c*1.0f + s*1.15f + .410f;
            float halfChassis = c*1.25f + s*1.70f;
            float h = UnityEngine.Mathf.Max(halfWheel, halfChassis);
            Assert.Greater((3.70f-2*h)*.5f, .25f);
            Assert.Less((3.50f-2*h)*.5f, .25f);
            Assert.IsTrue(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.10f,.10f,-h,h,-2.16f,-.45f,
                -1.85f,1.85f,-2.25f,2.25f,1f));
            Assert.IsFalse(CHL003GateCrossing.IsForwardFullBodyCrossing(
                -.10f,.10f,-h,h,-2.16f,-.45f,
                -1.75f,1.75f,-2.25f,2.25f,1f));
        }
  
        [Test] public void E20_B0160NearGroundFalseSupportRejectsHoveringWheel()
        {
            // Original Light spheres 0.82m diameter, Y root -.52m,
            // floor top Y=0. This checks original SOURCE C# reach policy,
            // not a licensed Unity physics contact.
            const float radius = .41f;
            float hover35 = 1.28f-.52f+
                CHL003WheelSupportWindow.OriginLiftMeters;
            float hover41 = 1.34f-.52f+
                CHL003WheelSupportWindow.OriginLiftMeters;
            Assert.Less(hover35,.95f, "Old long ray falsely allowed thrust");
            Assert.IsFalse(CHL003WheelSupportWindow.WithinWheelReach(hover35,radius));
            Assert.IsFalse(CHL003WheelSupportWindow.WithinWheelReach(hover41,radius));
            Assert.IsTrue(CHL003WheelSupportWindow.WithinWheelReach(
                .93f-.52f+CHL003WheelSupportWindow.OriginLiftMeters,radius));
            Assert.IsFalse(CHL003WheelSupportWindow.WithinWheelReach(
                .97f-.52f+CHL003WheelSupportWindow.OriginLiftMeters,radius));
            Assert.AreEqual(.58f,
                CHL003WheelSupportWindow.MaximumCastMeters(radius),.00001f);
            Assert.AreEqual(0f,
                CHL003WheelSupportWindow.MaximumCastMeters(float.NaN));

            // W58: the original motor must reject a near-floor wheel when
            // it is a trigger, disabled or owned by another Rigidbody.
            // This exercises the actual motor's private physics query in
            // Unity EditMode rather than repeating cast-distance arithmetic.
            var floor = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
            var rig = new UnityEngine.GameObject("CHL003_E20_WHEEL_OWNER");
            var foreign = new UnityEngine.GameObject("CHL003_E20_FOREIGN_WHEEL");
            try
            {
                floor.transform.position = new UnityEngine.Vector3(0f, -.5f, 0f);
                floor.transform.localScale = new UnityEngine.Vector3(8f, 1f, 8f);
                rig.transform.position = new UnityEngine.Vector3(0f, .93f, 0f);
                var body = rig.AddComponent<UnityEngine.Rigidbody>();
                body.isKinematic = true;
                var wheelObject = new UnityEngine.GameObject("PHYSICAL_SPHERE_WHEEL");
                wheelObject.transform.SetParent(rig.transform, false);
                wheelObject.transform.localPosition = new UnityEngine.Vector3(0f, -.52f, 0f);
                var wheel = wheelObject.AddComponent<UnityEngine.SphereCollider>();
                wheel.radius = radius;
                var motor = rig.AddComponent<CHL003VehicleMotor>();
                motor.Configure(null, 4200f, 700f,
                    new UnityEngine.Transform[] { wheelObject.transform });
                var realContact = typeof(CHL003VehicleMotor).GetMethod(
                    "HasRealGroundContact",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(realContact, "No native wheel support authority");
                UnityEngine.Physics.SyncTransforms();
                Assert.IsTrue((bool)realContact.Invoke(motor, null),
                    "Positive: solid sphere attached to owner near ground");

                wheel.isTrigger = true;
                UnityEngine.Physics.SyncTransforms();
                Assert.IsFalse((bool)realContact.Invoke(motor, null),
                    "Negative: trigger wheel cannot grant motor support");

                wheel.isTrigger = false;
                wheel.enabled = false;
                UnityEngine.Physics.SyncTransforms();
                Assert.IsFalse((bool)realContact.Invoke(motor, null),
                    "Negative: disabled wheel cannot grant motor support");

                wheel.enabled = true;
                foreign.transform.position = new UnityEngine.Vector3(0f, .41f, 0f);
                var foreignBody = foreign.AddComponent<UnityEngine.Rigidbody>();
                foreignBody.isKinematic = true;
                var foreignSphere = foreign.AddComponent<UnityEngine.SphereCollider>();
                foreignSphere.radius = radius;
                Assert.AreSame(foreignBody, foreignSphere.attachedRigidbody,
                    "Negative fixture must have a different actual Rigidbody");
                motor.Configure(null, 4200f, 700f,
                    new UnityEngine.Transform[] { foreign.transform });
                UnityEngine.Physics.SyncTransforms();
                Assert.IsFalse((bool)realContact.Invoke(motor, null),
                    "Negative: foreign sphere is not attached to owner Rigidbody");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(foreign);
                UnityEngine.Object.DestroyImmediate(rig);
                UnityEngine.Object.DestroyImmediate(floor);
            }

        }
    }
}
