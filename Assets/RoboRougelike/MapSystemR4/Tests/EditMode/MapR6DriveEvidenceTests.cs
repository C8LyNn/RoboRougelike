using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.MapSystemR4.Tests
{
    public sealed class MapR6DriveEvidenceTests
    {
        [Test]
        public void VehicleStationaryWithoutDemand_NeverCountsAsStuck()
        {
            var data = new MapR6DriveEvidence { StuckAfterSeconds = 1f };
            for (int i=0;i<100;i++) data.Sample(Vector3.zero, Vector3.up, .05f, 0f);
            Assert.AreEqual(0, data.StuckEvents);
        }

        [Test]
        public void NoProgressWithDemand_TriggersOnceUntilReset()
        {
            var data = new MapR6DriveEvidence { StuckAfterSeconds = 1f };
            for (int i=0;i<80;i++) data.Sample(Vector3.zero, Vector3.up, .05f, 1f);
            Assert.AreEqual(1, data.StuckEvents);
            data.Sample(new Vector3(10f,0,0), Vector3.up, .05f, 1f);
            for (int i=0;i<30;i++) data.Sample(new Vector3(10f,0,0), Vector3.up, .05f, 1f);
            Assert.AreEqual(2, data.StuckEvents);
        }

        [Test]
        public void TracksDistanceAndTilt()
        {
            var data = new MapR6DriveEvidence();
            data.Sample(Vector3.zero, Vector3.up, .02f, 0f);
            data.Sample(new Vector3(3f,0,4f), Vector3.right, .02f, 1f);
            Assert.AreEqual(5f,data.DrivenDistanceMeters,.001f);
            Assert.AreEqual(90f,data.MaxTiltDegrees,.01f);
        }
    }
}
