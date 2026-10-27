using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV002JumpDrive
{
    [Serializable]
    public sealed class JumpDriveSettings
    {
        [Min(0.01f)] public float TargetJumpHeightMeters = 1.8f;
        [Tooltip("Mass supported at full nominal jump height. Heavier vehicles jump lower without multiple drives.")]
        [Min(0.01f)] public float SupportedMassKg = 100f;
        [Min(0.01f)] public float MaxUpwardSpeedMps = 9f;
        [Min(0f)] public float CooldownSeconds = 9f;
        [Min(0f)] public float EnergyCost = 0f;
        [Tooltip("Probe origin should be near the underside of the vehicle; if empty, uses Rigidbody world center of mass.")]
        [Min(0.01f)] public float GroundProbeDistanceMeters = 2.5f;
        [Range(0f, 1f)] public float MinimumGroundNormalDot = 0.55f;
        public LayerMask GroundLayers = ~0;
        public bool RequireGroundContact = true;
        [Tooltip("Fails safely when a detached module is no longer under the vehicle Rigidbody transform.")]
        public bool RequireModuleUnderVehicleTransform = true;
    }
}
