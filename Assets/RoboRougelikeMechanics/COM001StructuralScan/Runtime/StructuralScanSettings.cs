using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    [Serializable]
    public sealed class StructuralScanSettings
    {
        [Min(0.1f)] public float rangeMeters = 24f;
        [Range(1f, 180f)] public float fullConeDegrees = 75f;
        [Min(0f)] public float cooldownSeconds = 8f;
        [Min(0.01f)] public float displaySeconds = 5f;
        [Min(0f)] public float activationEnergy = 0f;
        [Range(1, 128)] public int maxRevealedParts = 16;
        [Range(1, 4096)] public int maxRegisteredParts = 512;
        public LayerMask obstructionMask = ~0;

        public void Validate()
        {
            rangeMeters = Mathf.Clamp(Safe(rangeMeters, 24f), 0.1f, 1000f);
            fullConeDegrees = Mathf.Clamp(Safe(fullConeDegrees, 75f), 1f, 180f);
            cooldownSeconds = Mathf.Clamp(Safe(cooldownSeconds, 8f), 0f, 3600f);
            displaySeconds = Mathf.Clamp(Safe(displaySeconds, 5f), 0.01f, 3600f);
            activationEnergy = Mathf.Clamp(Safe(activationEnergy, 0f), 0f, 1000000f);
            maxRevealedParts = Mathf.Clamp(maxRevealedParts, 1, 128);
            maxRegisteredParts = Mathf.Clamp(maxRegisteredParts, 1, 4096);
        }

        private static float Safe(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }
    }
}
