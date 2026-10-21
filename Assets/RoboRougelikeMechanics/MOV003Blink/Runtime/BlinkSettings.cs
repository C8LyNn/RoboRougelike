using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV003Blink
{
    [Serializable]
    public sealed class BlinkSettings
    {
        [Min(0.1f)] public float DistanceMeters = 8f;
        [Min(0f)] public float CooldownSeconds = 20f;
        [Min(0f)] public float EnergyCost = 0f;
        [Tooltip("If enabled, the swept bounding volume cannot cross a solid obstacle. Disable only for an intentional wall-phasing ability.")]
        public bool RequireClearPath = true;
        [Tooltip("Checks ground support at the destination. Disable only for vehicles designed to fly or hover independently.")]
        public bool RequireGroundSupport = true;
        [Tooltip("Layers treated as solid obstacles, including other vehicles. For complex terrain use a separate terrain layer.")]
        public LayerMask BlockingLayers = ~0;
        [Tooltip("Layers queried for supporting ground. The same ground can also be in BlockingLayers; flat support surfaces are filtered.")]
        public LayerMask SupportLayers = ~0;
        [Tooltip("How many of the five footprint probes (center plus four inset corners) must have ground support.")]
        [Range(1, 5)] public int RequiredSupportSamples = 3;
        [Range(0f, 1f)] public float MinimumGroundNormalY = 0.6f;
        [Min(0f)] public float MaximumDropMeters = 0.65f;
        [Min(0f)] public float AllowedRiseMeters = 0.08f;
        [Tooltip("Additional safety volume around the vehicle, measured in world units.")]
        [Min(0f)] public float HullPaddingMeters = 0.03f;
        [Tooltip("Keep linear and angular velocities instead of instantly stopping after Blink.")]
        public bool PreserveVelocity = true;
        public bool RequireModuleUnderVehicleTransform = true;
    }
}
