using UnityEngine;

namespace RoboRougelike.VFX001
{
    public struct ImpactFeedbackSpec
    {
        public int particleCount;
        public float speed;
        public float lifeTime;
        public float size;
        public Color color;
        public bool emissiveFlash;

        public static ImpactFeedbackSpec Default => new ImpactFeedbackSpec
        {
            particleCount = 6,
            speed = 4f,
            lifeTime = 0.3f,
            size = 0.08f,
            color = new Color(1f, 0.8f, 0.3f),
            emissiveFlash = true
        };
    }
}
