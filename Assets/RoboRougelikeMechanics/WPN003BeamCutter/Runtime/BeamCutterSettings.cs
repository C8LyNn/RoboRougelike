using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.BeamCutter
{
    /// <summary>Runtime-tunable values; no asset/Resources dependency.</summary>
    [Serializable]
    public sealed class BeamCutterSettings
    {
        [Min(0.1f)] public float rangeMeters = 16f;
        [Min(0f)] public float damagePerSecond = 35f;
        [Min(0.01f)] public float heatCapacity = 100f;
        [Min(0f)] public float heatPerSecond = 32f;
        [Min(0f)] public float coolPerSecond = 24f;
        [Range(0f, 1f)] public float restartHeatFraction = 0.4f;
        [Min(0f)] public float energyPerSecond = 0f;
        [Min(0f)] public float recoilNewtons = 0f;
        public LayerMask hitLayers = ~0;
        public bool enableLineRenderer = true;

        public BeamCutterSettings Sanitized()
        {
            return new BeamCutterSettings
            {
                rangeMeters = PositiveOr(rangeMeters, 16f, 0.1f),
                damagePerSecond = NonNegative(damagePerSecond),
                heatCapacity = PositiveOr(heatCapacity, 100f, 0.01f),
                heatPerSecond = NonNegative(heatPerSecond),
                coolPerSecond = NonNegative(coolPerSecond),
                restartHeatFraction = Mathf.Clamp01(IsFinite(restartHeatFraction) ? restartHeatFraction : 0.4f),
                energyPerSecond = NonNegative(energyPerSecond),
                recoilNewtons = NonNegative(recoilNewtons),
                hitLayers = hitLayers,
                enableLineRenderer = enableLineRenderer
            };
        }

        private static float NonNegative(float value) => IsFinite(value) ? Mathf.Max(0f, value) : 0f;
        private static float PositiveOr(float value, float fallback, float minimum) => IsFinite(value) && value >= minimum ? value : fallback;
        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
