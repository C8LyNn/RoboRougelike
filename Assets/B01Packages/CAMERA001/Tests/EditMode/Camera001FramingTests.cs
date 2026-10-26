using System;
using NUnit.Framework;
using UnityEngine;

namespace B01.CAMERA001.Tests
{
    public sealed class Camera001FramingTests
    {
        private static Camera001Framing.Request Input(Bounds hull, float yaw, float pitch,
            float zoom, float aspect)
        {
            return new Camera001Framing.Request {
                HullBounds = hull, YawDegrees = yaw, PitchDegrees = pitch,
                RequestedDistance = zoom, MinimumDistance = 3f, MaximumDistance = 18f,
                VerticalFovDegrees = 62f, Aspect = aspect, SafetyHeadroom = .65f, AimLead = 1.7f
            };
        }

        [Test] public void EightHundredFortyGeometryAndZoomProposalsDoNotAimThroughOwnVehicle()
        {
            // 2 bodies x 3 aspect ratios x 7 pitches x 20 yaw samples = 840
            foreach (Bounds hull in new[] {
                new Bounds(Vector3.up * 1.4f, new Vector3(2.1f, 4f, 3.2f)),
                new Bounds(Vector3.up * 3f, new Vector3(4.5f, 10f, 5.8f))
            })
            foreach (float aspect in new[] { 16f / 9f, 4f / 3f, 21f / 9f })
            foreach (float pitch in new[] { -8f, -2f, 0f, 18f, 35f, 55f, 70f })
            for (int step = 0; step < 20; step++)
            {
                float yaw = 18f * step;
                var solution = Camera001Framing.Solve(Input(hull, yaw, pitch, 3f + step * .5f, aspect));
                Assert.IsFalse(solution.OwnHullIntersectsCrosshair,
                    "yaw=" + yaw + " pitch=" + pitch + " aspect=" + aspect);
                Assert.GreaterOrEqual(solution.DesiredCameraWorld.y, hull.max.y + .60f);
                Assert.GreaterOrEqual(solution.Distance, 3f);
                Assert.LessOrEqual(solution.Distance, 18f);
                Assert.IsFalse(float.IsNaN(solution.Distance));
            }
        }

        [Test] public void BiggerAndTallerRobotsDemandMoreFramingDistance()
        {
            var small = Camera001Framing.Solve(Input(
                new Bounds(Vector3.up, new Vector3(2, 2, 3)), 15, 18, 3, 16f/9f));
            var huge = Camera001Framing.Solve(Input(
                new Bounds(Vector3.up * 3, new Vector3(5, 11, 7)), 15, 18, 3, 16f/9f));
            Assert.Greater(huge.FitDistance, small.FitDistance);
            Assert.Greater(huge.Distance, small.Distance);
        }

        [Test] public void SafeMaximumZoomNeverExceedsConfiguredLimit()
        {
            var giant = Camera001Framing.Solve(Input(
                new Bounds(Vector3.up * 10, new Vector3(20, 30, 30)), 180, 0, 3, 1f));
            Assert.AreEqual(18f, giant.Distance, .001f);
            Assert.IsTrue(giant.FitSaturated, "Large hull must declare inability to fit at maximum zoom");
        }

        [Test] public void DifferentResolutionWithSameAspectKeepsGeometry()
        {
            var hull = new Bounds(new Vector3(9, 1, 19), new Vector3(2, 5, 4));
            var a = Camera001Framing.Solve(Input(hull, 210, 20, 8, 1280f/720f));
            var b = Camera001Framing.Solve(Input(hull, 210, 20, 8, 1920f/1080f));
            Assert.Less(Vector3.Distance(a.DesiredCameraWorld, b.DesiredCameraWorld), .001f);
            Assert.Less(Vector3.Distance(a.AimWorld, b.AimWorld), .001f);
        }

        [Test] public void InvalidGeometryAndNonFiniteRequestFailClosed()
        {
            var r = Input(new Bounds(Vector3.zero, Vector3.one), 0, 20, 8, 16f/9f);
            r.YawDegrees = float.NaN;
            Assert.Throws<ArgumentException>(() => Camera001Framing.Solve(r));
            r.YawDegrees = 0f; r.Aspect = 0f;
            Assert.Throws<ArgumentException>(() => Camera001Framing.Solve(r));
            r.Aspect = 1f; r.HullBounds = new Bounds(Vector3.zero, Vector3.zero);
            Assert.Throws<ArgumentException>(() => Camera001Framing.Solve(r));
        }
    }
}
