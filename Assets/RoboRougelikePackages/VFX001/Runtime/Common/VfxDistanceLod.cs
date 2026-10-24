using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Pure visual LOD response. Applies to particle emission only; never culls gameplay events.</summary>
    public static class VfxDistanceLod
    {
        public static float RateFactor(float meters, float nearMeters = 16f, float farMeters = 44f)
        {
            if (float.IsNaN(meters) || float.IsInfinity(meters)) return 0f;
            float near = Mathf.Max(0f, nearMeters);
            float far = Mathf.Max(near + .001f, farMeters);
            return 1f - Mathf.InverseLerp(near, far, Mathf.Max(0f, meters));
        }
    }
}
