using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering.Tests
{
    public sealed class SteeringProfileTests
    {
        private SteeringProfile P => SteeringProfile.Default;
        private const float E=0.0001f;
        [Test] public void DefaultProfilePositiveAndFinite()
        {
            var p=P; Assert.Greater(p.maxCenterAngleDegrees,0); Assert.Greater(p.steeringRateDegreesPerSecond,0);
            Assert.Greater(p.halfAngleAtSpeedMetersPerSecond,0);
        }
        [Test] public void InputClamped()
        {
            SteeringMath.CenterTargets(SteeringMode.FrontOnly,50f,0,P,1,1,out var f,out _);
            Assert.AreEqual(P.maxCenterAngleDegrees,f,E);
        }
        [Test] public void SpeedHalfAngleAtConfiguredSpeed()
        {
            Assert.AreEqual(.5f,SteeringMath.SpeedFactor(P.halfAngleAtSpeedMetersPerSecond,P),E);
        }
        [Test] public void SpeedLowerLimitRespected()
        {
            Assert.AreEqual(P.minimumSpeedFactor,SteeringMath.SpeedFactor(100000,P),E);
        }
        [Test] public void NegativeSpeedEquivalentToPositive()
        {
            Assert.AreEqual(SteeringMath.SpeedFactor(-14,P),SteeringMath.SpeedFactor(14,P),E);
        }
        [Test] public void NanSpeedNeverPoisonsSolution()
        {
            Assert.AreEqual(1f,SteeringMath.SpeedFactor(float.NaN,P),E);
        }
        [Test] public void FrontModeCommandsOnlyFront()
        {
            SteeringMath.CenterTargets(SteeringMode.FrontOnly,1,0,P,1,1,out var f,out var r);
            Assert.Greater(f,0); Assert.AreEqual(0,r,E);
        }
        [Test] public void RearModeCommandsOnlyRear()
        {
            SteeringMath.CenterTargets(SteeringMode.RearOnly,1,0,P,1,1,out var f,out var r);
            Assert.AreEqual(0,f,E); Assert.Less(r,0);
        }
        [Test] public void CounterPhaseRearOpposite()
        {
            SteeringMath.CenterTargets(SteeringMode.CounterPhase,1,0,P,1,1,out var f,out var r);
            Assert.Greater(f,0); Assert.Less(r,0);
        }
        [Test] public void CrabSameSteerFrontRear()
        {
            SteeringMath.CenterTargets(SteeringMode.Crab,.8f,0,P,1,1,out var f,out var r);
            Assert.AreEqual(f,r,E);
        }
        [Test] public void SteeringHealthMissingFrontDisablesFront()
        {
            SteeringMath.CenterTargets(SteeringMode.FrontOnly,1,0,P,0,1,out var f,out _);
            Assert.AreEqual(0,f,E);
        }
        [Test] public void SteeringHealthHalfFrontHalvesAngle()
        {
            SteeringMath.CenterTargets(SteeringMode.FrontOnly,1,0,P,.5f,1,out var f,out _);
            Assert.AreEqual(P.maxCenterAngleDegrees/2f,f,E);
        }
        [Test] public void SteeringHealthMissingRearDisablesRear()
        {
            SteeringMath.CenterTargets(SteeringMode.CounterPhase,1,0,P,1,0,out _,out var r);
            Assert.AreEqual(0,r,E);
        }
        [Test] public void SteeringInputNanIsNeutral()
        {
            SteeringMath.CenterTargets(SteeringMode.Crab,float.NaN,0,P,1,1,out var f,out var r);
            Assert.AreEqual(0,f,E); Assert.AreEqual(0,r,E);
        }
        [Test] public void ApproachLimitedByDegreesPerSecond()
        {
            Assert.AreEqual(13.5f,SteeringMath.Approach(0,40,135,.1f),E);
        }
        [Test] public void ApproachNeverOvershoots()
        {
            Assert.AreEqual(4,SteeringMath.Approach(0,4,135,1f),E);
        }
        [Test] public void ApproachWithNegativeDeltaDoesNothing()
        {
            Assert.AreEqual(11,SteeringMath.Approach(11,40,135,-1),E);
        }
        [Test] public void ApproachWithNanDeltaDoesNothing()
        {
            Assert.AreEqual(11,SteeringMath.Approach(11,40,135,float.NaN),E);
        }
        [Test] public void SanitizationFixesZeroProfileFields()
        {
            var p=new SteeringProfile().Sanitized();
            Assert.Greater(p.maxCenterAngleDegrees,0); Assert.Greater(p.halfAngleAtSpeedMetersPerSecond,0);
        }
        [Test] public void SanitizationFixesBadGeometry()
        {
            var g=new SteeringGeometry{wheelbaseMeters=float.NaN,trackMeters=float.PositiveInfinity}.Sanitized();
            Assert.AreEqual(2.8f,g.wheelbaseMeters,E); Assert.AreEqual(2f,g.trackMeters,E);
        }
        [Test] public void InvalidModeFallsBackToFront()
        {
            SteeringMath.CenterTargets((SteeringMode)99,1,0,P,1,1,out var f,out var r);
            Assert.Greater(f,0);Assert.AreEqual(0,r,E);
        }
    }
}
