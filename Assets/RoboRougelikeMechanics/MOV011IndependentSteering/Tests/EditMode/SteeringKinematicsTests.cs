using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.Mechanics.IndependentSteering.Tests
{
    public sealed class SteeringKinematicsTests
    {
        private static readonly SteeringGeometry G = SteeringGeometry.Default;
        private const float EPS=0.001f;

        [Test] public void ZeroInput_ZeroAllAnglesAndCurvature()
        {
            var s=SteeringMath.Solve(0,0,G);
            Assert.AreEqual(0,s.frontLeftDegrees,EPS); Assert.AreEqual(0,s.frontRightDegrees,EPS);
            Assert.AreEqual(0,s.rearLeftDegrees,EPS); Assert.AreEqual(0,s.rearRightDegrees,EPS);
            Assert.AreEqual(0,s.curvatureInverseMeters,EPS); Assert.AreEqual(0,s.midpointSlipDegrees,EPS);
        }
        [Test] public void FrontSteering_RearWheelsStraight()
        {
            var s=SteeringMath.Solve(25,0,G);
            Assert.Greater(s.frontLeftDegrees,0); Assert.Greater(s.frontRightDegrees,0);
            Assert.AreEqual(0,s.rearLeftDegrees,EPS); Assert.AreEqual(0,s.rearRightDegrees,EPS);
        }
        [Test] public void FrontRightTurn_RightInsideWheelSteersMore()
        {
            var s=SteeringMath.Solve(25,0,G);
            Assert.Greater(s.frontRightDegrees,s.frontLeftDegrees);
        }
        [Test] public void FrontLeftTurn_LeftInsideWheelSteersMoreMagnitude()
        {
            var s=SteeringMath.Solve(-25,0,G);
            Assert.Greater(Mathf.Abs(s.frontLeftDegrees),Mathf.Abs(s.frontRightDegrees));
        }
        [Test] public void CounterPhase_IncreasesCurvature()
        {
            var front=SteeringMath.Solve(25,0,G);
            var both=SteeringMath.Solve(25,-15,G);
            Assert.Greater(both.curvatureInverseMeters,front.curvatureInverseMeters);
        }
        [Test] public void Crab_HasZeroCurvature()
        {
            var s=SteeringMath.Solve(25,25,G);
            Assert.AreEqual(0,s.curvatureInverseMeters,EPS);
        }
        [Test] public void Crab_AllWheelsEqualSameAngle()
        {
            var s=SteeringMath.Solve(22,22,G);
            Assert.AreEqual(22,s.frontLeftDegrees,EPS); Assert.AreEqual(22,s.frontRightDegrees,EPS);
            Assert.AreEqual(22,s.rearLeftDegrees,EPS); Assert.AreEqual(22,s.rearRightDegrees,EPS);
        }
        [Test] public void PositiveRearAngle_FrontWheelsStraight()
        {
            var s=SteeringMath.Solve(0,22,G);
            Assert.AreEqual(0,s.frontLeftDegrees,EPS); Assert.AreEqual(0,s.frontRightDegrees,EPS);
            // Solve accepts axle angles, not a signed driver command. RearOnly reverses the command in CenterTargets.
            Assert.Greater(s.rearLeftDegrees,0);Assert.Greater(s.rearRightDegrees,0);
        }
        [Test] public void RearSteering_PositiveAngle_NegativeCurvature()
        {
            Assert.Less(SteeringMath.Solve(0,15,G).curvatureInverseMeters,0);
        }
        [Test] public void LeftAndRight_AreAntisymmetric()
        {
            var a=SteeringMath.Solve(20,-12,G);
            var b=SteeringMath.Solve(-20,12,G);
            Assert.AreEqual(a.frontLeftDegrees,-b.frontRightDegrees,EPS);
            Assert.AreEqual(a.rearLeftDegrees,-b.rearRightDegrees,EPS);
        }
        [Test] public void WideningTrack_IncreasesInsideOutsideDifference()
        {
            var small=SteeringMath.Solve(25,0,new SteeringGeometry{wheelbaseMeters=3,trackMeters=1});
            var large=SteeringMath.Solve(25,0,new SteeringGeometry{wheelbaseMeters=3,trackMeters=2.5f});
            Assert.Greater(large.frontRightDegrees-large.frontLeftDegrees, small.frontRightDegrees-small.frontLeftDegrees);
        }
        [Test] public void WheelbaseIncrease_ReducesCurvature()
        {
            var a=SteeringMath.Solve(20,0,new SteeringGeometry{wheelbaseMeters=2,trackMeters=1.5f});
            var b=SteeringMath.Solve(20,0,new SteeringGeometry{wheelbaseMeters=4,trackMeters=1.5f});
            Assert.Greater(a.curvatureInverseMeters,b.curvatureInverseMeters);
        }
        [Test] public void SmallWheelbase_DoesNotProduceNan()
        {
            var s=SteeringMath.Solve(50,-50,new SteeringGeometry{wheelbaseMeters=0,trackMeters=0});
            Assert.IsFalse(float.IsNaN(s.frontLeftDegrees)); Assert.IsFalse(float.IsInfinity(s.rearRightDegrees));
        }
        [Test] public void ExcessiveInput_WheelAngleIsCapped()
        {
            var s=SteeringMath.Solve(1000,-1000,new SteeringGeometry{wheelbaseMeters=.1f,trackMeters=12f});
            Assert.LessOrEqual(Mathf.Abs(s.frontLeftDegrees),80f);
            Assert.LessOrEqual(Mathf.Abs(s.rearRightDegrees),80f);
        }
        [Test] public void NanAndInfinityInputs_Sanitized()
        {
            var s=SteeringMath.Solve(float.NaN,float.PositiveInfinity,G);
            Assert.AreEqual(0f,s.curvatureInverseMeters,EPS);
        }
        [Test] public void PositionLookupMatchesFields()
        {
            var s=SteeringMath.Solve(18,-8,G);
            Assert.AreEqual(s.frontLeftDegrees,s.GetAngle(SteeringWheelPosition.FrontLeft),EPS);
            Assert.AreEqual(s.frontRightDegrees,s.GetAngle(SteeringWheelPosition.FrontRight),EPS);
            Assert.AreEqual(s.rearLeftDegrees,s.GetAngle(SteeringWheelPosition.RearLeft),EPS);
            Assert.AreEqual(s.rearRightDegrees,s.GetAngle(SteeringWheelPosition.RearRight),EPS);
        }
        [Test] public void InvalidPositionReturnsZero() => Assert.AreEqual(0f,SteeringMath.Solve(10,0,G).GetAngle((SteeringWheelPosition)99),EPS);
    }
}
