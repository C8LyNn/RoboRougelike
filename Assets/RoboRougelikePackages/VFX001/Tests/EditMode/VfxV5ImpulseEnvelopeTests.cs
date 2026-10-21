using NUnit.Framework;
using UnityEngine;

namespace RoboRougelike.VFX001.Tests
{
    public sealed class VfxV5ImpulseEnvelopeTests
    {
        [TestCase(0f, 1f)]
        [TestCase(5f, 1f)]
        [TestCase(30f, 0f)]
        [TestCase(100f, 0f)]
        public void Attenuation_ClampsDistance(float distance, float expected)
        {
            Assert.AreEqual(expected, VfxImpulseEnvelope.Attenuation(distance, 5f, 30f), 0.0001f);
        }
        [Test] public void Attenuation_IsMonotonic()
        {
            float previous = 1f;
            for (int distance=0;distance<40;distance++)
            {
                float next = VfxImpulseEnvelope.Attenuation(distance, 5f, 30f);
                Assert.LessOrEqual(next, previous + .0001f);
                previous = next;
            }
        }
        [Test] public void Attenuation_NanDoesNotPoisonEffect() =>
            Assert.AreEqual(0f, VfxImpulseEnvelope.Attenuation(float.NaN, 3f, 20f));
        [Test] public void Decay_StartAndEnd()
        {
            Assert.AreEqual(1f, VfxImpulseEnvelope.Decay(0f, .4f), .0001f);
            Assert.AreEqual(.25f, VfxImpulseEnvelope.Decay(.2f, .4f), .0001f);
            Assert.AreEqual(0f, VfxImpulseEnvelope.Decay(.4f, .4f), .0001f);
        }
        [Test] public void Bounded_RestrictsTranslation()
        {
            Assert.AreEqual(2f, VfxImpulseEnvelope.Bounded(new Vector3(8f,0f,0f),2f).magnitude, .0001f);
            Assert.AreEqual(Vector3.zero, VfxImpulseEnvelope.Bounded(new Vector3(float.NaN,0f,0f),1f));
        }
    }
}
