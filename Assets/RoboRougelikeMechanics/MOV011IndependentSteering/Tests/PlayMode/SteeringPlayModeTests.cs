using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoboRougelike.Mechanics.IndependentSteering.Demo;

namespace RoboRougelike.Mechanics.IndependentSteering.Tests
{
    public sealed class SteeringPlayModeTests
    {
        private GameObject root;
        private Rigidbody rb;
        private IndependentSteeringController c;
        private SteeringDemoDrive demo;
        private SteeringWheelModule[] wheels;

        [SetUp] public void SetUp()
        {
            root=new GameObject("MOV011 PlayMode test vehicle");
            rb=root.AddComponent<Rigidbody>();
            rb.mass=250f;rb.useGravity=false;
            // Match the supplied demo hull. A colliderless body's default yaw inertia of1
            // makes this250kg per-wheel grip fixture numerically unstable.
            root.AddComponent<BoxCollider>().size=new Vector3(1.5f,.4f,2.2f);rb.ResetInertiaTensor();
            rb.constraints=RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping=0f;rb.angularDamping=0.3f;
            c=root.AddComponent<IndependentSteeringController>();c.Bind(rb);c.SetAutomaticFixedUpdate(false);
            var profile=SteeringProfile.Default;
            profile.steeringRateDegreesPerSecond=1000f;profile.halfAngleAtSpeedMetersPerSecond=1000f;
            c.Configure(SteeringGeometry.Default,profile);
            wheels=new SteeringWheelModule[4];
            Vector3[] locations={new Vector3(-1,0,1.4f),new Vector3(1,0,1.4f),new Vector3(-1,0,-1.4f),new Vector3(1,0,-1.4f)};
            for(int i=0;i<4;i++)
            {
                var o=new GameObject("Wheel "+i);
                o.transform.SetParent(root.transform,false);o.transform.localPosition=locations[i];
                wheels[i]=o.AddComponent<SteeringWheelModule>();
                wheels[i].Configure((SteeringWheelPosition)i);
                Assert.IsTrue(c.RegisterWheel(wheels[i]));
            }
            demo=root.AddComponent<SteeringDemoDrive>();demo.Bind(c,rb);
            demo.SetAutomaticFixedUpdate(false);demo.SetDemoForces(600f,2f,900f,20f);
        }
        [TearDown] public void TearDown() { if(root!=null) Object.Destroy(root); }
        private IEnumerator RunFor(int frames)
        {
            for(int i=0;i<frames;i++)
            {
                c.Step(Time.fixedDeltaTime);
                demo.StepDrive();
                yield return new WaitForFixedUpdate();
            }
        }
        [UnityTest] public IEnumerator ForwardThrottle_MovesVehicleForward()
        {
            demo.SetThrottle(1f);yield return RunFor(20);
            Assert.Greater(root.transform.position.z,.1f);
        }
        [UnityTest] public IEnumerator ReverseThrottle_MovesVehicleBackward()
        {
            demo.SetThrottle(-1f);yield return RunFor(20);
            Assert.Less(root.transform.position.z,-.1f);
        }
        [UnityTest] public IEnumerator NeutralThrottle_NoPropulsionFromRest()
        {
            demo.SetThrottle(0f);yield return RunFor(10);
            Assert.Less(root.transform.position.magnitude,.01f);
        }
        [UnityTest] public IEnumerator Crab_ProducesSidewaysTranslation()
        {
            c.SetMode(SteeringMode.Crab);c.SetSteeringInput(1f);demo.SetThrottle(1f);
            yield return RunFor(25);
            Assert.Greater(root.transform.position.x,.1f);
            Assert.Greater(root.transform.position.z,.1f);
        }
        [UnityTest] public IEnumerator Crab_ShouldNotCauseStrongYawFromSymmetricWheels()
        {
            c.SetMode(SteeringMode.Crab);c.SetSteeringInput(1f);demo.SetThrottle(1f);
            yield return RunFor(25);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f,root.transform.eulerAngles.y)),3f);
        }
        [UnityTest] public IEnumerator FrontSteering_DrivesAndTurns()
        {
            c.SetMode(SteeringMode.FrontOnly);c.SetSteeringInput(1f);demo.SetThrottle(1f);
            yield return RunFor(40);
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0,root.transform.eulerAngles.y)),.5f);
        }
        [UnityTest] public IEnumerator CounterPhase_BothAxlesHaveOppositeAngles()
        {
            c.SetMode(SteeringMode.CounterPhase);c.SetSteeringInput(.75f);
            yield return RunFor(5);
            Assert.Greater(wheels[0].CurrentAngleDegrees,0);
            Assert.Less(wheels[2].CurrentAngleDegrees,0);
        }
        [UnityTest] public IEnumerator DetachingFrontLeft_StopsItsSteering()
        {
            c.SetMode(SteeringMode.FrontOnly);c.SetSteeringInput(1f);
            yield return RunFor(5);wheels[0].SetStructurallyAttached(false);
            yield return RunFor(5);
            Assert.AreEqual(0f,wheels[0].CurrentAngleDegrees,.001f);
            Assert.Greater(wheels[1].CurrentAngleDegrees,0f);
        }
        [UnityTest] public IEnumerator DestroyedWheel_NoNullReferenceInSimulation()
        {
            c.SetMode(SteeringMode.CounterPhase);c.SetSteeringInput(1f);demo.SetThrottle(1f);
            Object.Destroy(wheels[1].gameObject);
            yield return RunFor(8);
            Assert.IsNotNull(c);
        }
        [UnityTest] public IEnumerator DamagedAllWheels_NoDrive()
        {
            for(int i=0;i<4;i++) wheels[i].SetOperational(false);
            c.SetSteeringInput(1f);demo.SetThrottle(1f);
            yield return RunFor(12);
            Assert.Less(rb.linearVelocity.magnitude,.01f);
        }
        [UnityTest] public IEnumerator InputRelease_DoesNotForceStopExistingMomentum()
        {
            demo.SetThrottle(1f);yield return RunFor(18);
            demo.SetThrottle(0f);c.SetSteeringInput(0f);
            float before=rb.linearVelocity.magnitude;
            yield return RunFor(1);
            Assert.Greater(rb.linearVelocity.magnitude,0f);
            Assert.LessOrEqual(rb.linearVelocity.magnitude,before+.15f);
        }
        [UnityTest] public IEnumerator ExternalBoostVelocity_IsNotActivelyBrakedByThrottleLimiter()
        {
            demo.SetDemoForces(600f,0f,0f,3f);
            rb.linearVelocity=new Vector3(0f,0f,6f);
            demo.SetThrottle(1f);yield return RunFor(5);
            Assert.Greater(rb.linearVelocity.z,5.5f);
        }
        [UnityTest] public IEnumerator MassInfluencesDemoAcceleration()
        {
            rb.mass=600f;demo.SetThrottle(1f);yield return RunFor(12);
            float heavySpeed=rb.linearVelocity.z;
            rb.linearVelocity=Vector3.zero;
            rb.mass=150f;yield return RunFor(12);
            Assert.Greater(rb.linearVelocity.z,heavySpeed*1.5f);
        }
        [UnityTest] public IEnumerator WheelColliderSteerAngleGetsUpdated()
        {
            var o=new GameObject("Collider pivot");o.transform.SetParent(root.transform,false);
            var wc=o.AddComponent<WheelCollider>();
            wheels[0].Configure(SteeringWheelPosition.FrontLeft,null,wc);
            c.SetSteeringInput(.5f);
            yield return RunFor(5);
            Assert.AreEqual(wheels[0].CurrentAngleDegrees,wc.steerAngle,.001f);
        }
    }
}
