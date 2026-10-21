using NUnit.Framework;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV4CoreTimelineTests
    {
        [Test]
        public void CoreBeatsHaveMonotonicTimes()
        {
            float previous = -1f;
            for (int i=0; i<VfxCoreDestructionTimeline.BeatCount; i++)
            {
                var beat = VfxCoreDestructionTimeline.GetBeat(i);
                Assert.Greater(beat.AtSeconds, previous);
                previous=beat.AtSeconds;
            }
            Assert.AreEqual(VfxCoreDestructionTimeline.LastBeatSeconds, previous, .0001f);
        }
        [Test]
        public void CoreHasExactlyOneMainDetonation()
        {
            int detonations=0;
            for (int i=0;i<VfxCoreDestructionTimeline.BeatCount;i++)
                if (VfxCoreDestructionTimeline.GetBeat(i).Kind == VfxEventKind.CoreDestroyed) detonations++;
            Assert.AreEqual(1, detonations);
        }
        [Test]
        public void CoreTimelineStartsWithFlashAndEndsWithDebris()
        {
            Assert.AreEqual(VfxEventKind.Hit,VfxCoreDestructionTimeline.GetBeat(0).Kind);
            Assert.AreEqual(VfxEventKind.Detached,VfxCoreDestructionTimeline.GetBeat(VfxCoreDestructionTimeline.BeatCount-1).Kind);
        }
        [Test]
        public void NegativeCoreBeatThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>VfxCoreDestructionTimeline.GetBeat(-1));
        }
    }
}
