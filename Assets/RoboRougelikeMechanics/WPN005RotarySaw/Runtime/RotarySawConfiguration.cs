using System;
using UnityEngine;

namespace RoboRougelike.Weapons.RotarySaw
{
    public enum SawAxleAxis { Right, Up, Forward }

    [Serializable]
    public sealed class RotarySawConfiguration
    {
        [Header("Contact geometry (metres)")]
        [Min(0.05f)] public float bladeRadius = 0.9f;
        [Min(0.01f)] public float bladeHalfThickness = 0.14f;
        [Min(0f)] public float hubDeadZone = 0.06f;
        public SawAxleAxis axleAxis = SawAxleAxis.Right;
        public LayerMask contactLayers = ~0;
        [Range(4, 256)] public int maxOverlapHits = 64;

        [Header("Rotary motor")]
        [Min(1f)] public float maximumRpm = 2800f;
        [Min(1f)] public float minimumCutRpm = 450f;
        [Min(1f)] public float accelerationRpmPerSecond = 3500f;
        [Min(1f)] public float decelerationRpmPerSecond = 2200f;
        [Min(0f)] public float damagePerSecondAtFullRpm = 52f;
        [Min(0.01f)] public float maxCuttableResistance = 3f;

        [Header("Thermal / jam protection")]
        [Min(0.1f)] public float heatCapacity = 100f;
        [Min(0f)] public float heatPerSecondAtFullRpm = 25f;
        [Min(0f)] public float coolingPerSecond = 24f;
        [Range(0f, 1f)] public float resumeHeatFraction = 0.4f;
        [Min(0f)] public float jamSeconds = 1.2f;
        [Min(0f)] public float jamHeatPenalty = 20f;

        [Header("Optional resources and reaction force")]
        [Min(0f)] public float energyPerSecond = 0f;
        [Min(0f)] public float reactionForceNewtonsPerContact = 0f;
        [Min(0f)] public float maximumTotalReactionForceNewtons = 180f;

        public void ClampToSafeValues()
        {
            bladeRadius = ClampFinite(bladeRadius, 0.05f, 20f, 0.9f);
            bladeHalfThickness = ClampFinite(bladeHalfThickness, 0.01f, bladeRadius, 0.14f);
            hubDeadZone = ClampFinite(hubDeadZone, 0f, bladeRadius * 0.95f, 0.06f);
            maxOverlapHits = Mathf.Clamp(maxOverlapHits, 4, 256);
            maximumRpm = ClampFinite(maximumRpm, 1f, 100000f, 2800f);
            minimumCutRpm = ClampFinite(minimumCutRpm, 1f, maximumRpm, 450f);
            accelerationRpmPerSecond = ClampFinite(accelerationRpmPerSecond, 1f, 100000f, 3500f);
            decelerationRpmPerSecond = ClampFinite(decelerationRpmPerSecond, 1f, 100000f, 2200f);
            damagePerSecondAtFullRpm = ClampFinite(damagePerSecondAtFullRpm, 0f, 100000f, 52f);
            maxCuttableResistance = ClampFinite(maxCuttableResistance, 0.01f, 1000f, 3f);
            heatCapacity = ClampFinite(heatCapacity, 0.1f, 100000f, 100f);
            heatPerSecondAtFullRpm = ClampFinite(heatPerSecondAtFullRpm, 0f, 100000f, 25f);
            coolingPerSecond = ClampFinite(coolingPerSecond, 0f, 100000f, 24f);
            resumeHeatFraction = ClampFinite(resumeHeatFraction, 0f, 1f, 0.4f);
            jamSeconds = ClampFinite(jamSeconds, 0f, 10000f, 1.2f);
            jamHeatPenalty = ClampFinite(jamHeatPenalty, 0f, 100000f, 20f);
            energyPerSecond = ClampFinite(energyPerSecond, 0f, 100000f, 0f);
            reactionForceNewtonsPerContact = ClampFinite(reactionForceNewtonsPerContact, 0f, 100000f, 0f);
            maximumTotalReactionForceNewtons = ClampFinite(maximumTotalReactionForceNewtons, 0f, 100000f, 180f);
        }

        public RotarySawConfiguration Copy()
        {
            return (RotarySawConfiguration)MemberwiseClone();
        }

        public static float ClampFinite(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = fallback;
            return Mathf.Clamp(value, min, max);
        }
    }
}
