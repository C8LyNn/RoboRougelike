using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering.Tests
{
    public sealed class SteeringComponentEditModeTests
    {
        private GameObject root;
        private IndependentSteeringController c;
        [SetUp] public void Setup()
        {
            root=new GameObject("TestVehicle");
            var rb=root.AddComponent<Rigidbody>();rb.useGravity=false;
            c=root.AddComponent<IndependentSteeringController>();c.SetAutomaticFixedUpdate(false);c.Bind(rb);
            c.Configure(SteeringGeometry.Default,new SteeringProfile{
                maxCenterAngleDegrees=30, rearSteerRatio=.7f,
                steeringRateDegreesPerSecond=1000,halfAngleAtSpeedMetersPerSecond=10000,minimumSpeedFactor=.35f});
        }
        [TearDown] public void Teardown() { if(root!=null) Object.DestroyImmediate(root); }
        private SteeringWheelModule Wheel(SteeringWheelPosition p)
        {
            var go=new GameObject(p.ToString());go.transform.SetParent(root.transform,false);
            var m=go.AddComponent<SteeringWheelModule>();m.Configure(p);
            Assert.IsTrue(c.RegisterWheel(m));return m;
        }
        private void AddAll() { for(int i=0;i<4;i++) Wheel((SteeringWheelPosition)i); }

        [Test] public void WithNoWheel_NoSteering()
        {
            c.SetSteeringInput(1);c.Step(.1f);
            Assert.AreEqual(0,c.CurrentSolution.frontCenterDegrees,.0001f);
        }
        [Test] public void FrontOnlyTurnsFrontWheels()
        {
            AddAll();c.SetSteeringInput(1);c.Step(.1f);
            Assert.Greater(c.GetWheel(SteeringWheelPosition.FrontLeft).CurrentAngleDegrees,0);
            Assert.AreEqual(0,c.GetWheel(SteeringWheelPosition.RearLeft).CurrentAngleDegrees,.001f);
        }
        [Test] public void UnregisterRemovesWheel()
        {
            var w=Wheel(SteeringWheelPosition.FrontLeft);c.UnregisterWheel(w);
            Assert.IsNull(c.GetWheel(SteeringWheelPosition.FrontLeft));
        }
        [Test] public void DuplicatePositionRejected()
        {
            var first=Wheel(SteeringWheelPosition.FrontLeft);
            var second=new GameObject("duplicate").AddComponent<SteeringWheelModule>();
            second.transform.SetParent(root.transform,false);second.Configure(SteeringWheelPosition.FrontLeft);
            Assert.IsFalse(c.RegisterWheel(second));Assert.AreSame(first,c.GetWheel(SteeringWheelPosition.FrontLeft));
        }
        [Test] public void DestroyedWheelUnavailableNextStep()
        {
            AddAll();var w=c.GetWheel(SteeringWheelPosition.FrontLeft);
            Object.DestroyImmediate(w.gameObject);
            c.SetSteeringInput(1);Assert.DoesNotThrow(()=>c.Step(.1f));
        }
        [Test] public void MissingLeftWheelReducesFrontAuthority()
        {
            AddAll();c.SetSteeringInput(1);c.Step(.1f);
            float intact=c.CurrentSolution.frontCenterDegrees;
            c.GetWheel(SteeringWheelPosition.FrontLeft).SetOperational(false);
            c.Step(.1f);
            Assert.AreEqual(intact/2,c.CurrentSolution.frontCenterDegrees,.01f);
        }
        [Test] public void DetachedWheelZeroAngle()
        {
            AddAll();c.SetSteeringInput(1);c.Step(.1f);
            var w=c.GetWheel(SteeringWheelPosition.FrontLeft);w.SetStructurallyAttached(false);c.Step(.1f);
            Assert.AreEqual(0,w.CurrentAngleDegrees,.001f);
        }
        [Test] public void ResetClearsInputAndAngles()
        {
            AddAll();c.SetSteeringInput(1);c.Step(.1f);c.ResetSteering();
            Assert.AreEqual(0,c.CurrentInput,.001f);Assert.AreEqual(0,c.CurrentSolution.curvatureInverseMeters,.001f);
        }
        [Test] public void PivotRestoredOnDamage()
        {
            var m=Wheel(SteeringWheelPosition.FrontLeft);
            var pivot=new GameObject("Pivot").transform;
            pivot.SetParent(m.transform,false);pivot.localRotation=Quaternion.Euler(0,16,0);
            m.Configure(SteeringWheelPosition.FrontLeft,pivot);
            Wheel(SteeringWheelPosition.FrontRight);
            c.SetSteeringInput(1);c.Step(.1f);
            Assert.Greater(Quaternion.Angle(pivot.localRotation,Quaternion.Euler(0,16,0)),1);
            m.SetOperational(false);
            Assert.Less(Quaternion.Angle(pivot.localRotation,Quaternion.Euler(0,16,0)),.01f);
        }
        [Test] public void ModeSwitchChangesRearAngle()
        {
            AddAll();c.SetSteeringInput(1);c.SetMode(SteeringMode.Crab);c.Step(.1f);
            Assert.Greater(c.CurrentSolution.rearLeftDegrees,0);
            c.SetMode(SteeringMode.CounterPhase);c.Step(.1f);
            Assert.Less(c.CurrentSolution.rearLeftDegrees,0);
        }
    }
}
