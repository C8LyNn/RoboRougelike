using System;
using UnityEngine;

namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    [Serializable]
    public sealed class GrappleTuning
    {
        [Header("Target and line")]
        [Min(1f)] public float maxRange = 24f;
        [Min(2f)] public float maxStretchDistance = 40f;
        public LayerMask targetAndOccluderLayers = ~0;
        public bool allowDynamicTargets = true;
        public bool requireMagneticSurface = false;
        public bool breakWhenLineObstructed = true;

        [Header("Winch physics")]
        [Min(0.1f)] public float minRopeLength = 2f;
        [Min(0f)] public float reelSpeed = 7f;
        [Min(0f)] public float springStiffness = 2200f;
        [Min(0f)] public float damping = 340f;
        [Min(0f)] public float maximumTensionNewtons = 15000f;
        [Min(0f)] public float maximumVehicleAcceleration = 12f;
        [Min(0f)] public float maximumTargetAcceleration = 8f;
        public bool reelAutomatically = true;

        [Header("Ability")]
        [Min(0f)] public float cooldownSeconds = 6f;
        [Min(0f)] public float maximumAttachmentSeconds = 8f;
        [Min(0f)] public float activationEnergy = 20f;
        public bool useEnergy = false;

        public void Sanitize()
        {
            maxRange = Mathf.Max(1f, maxRange);
            maxStretchDistance = Mathf.Max(maxRange, maxStretchDistance);
            minRopeLength = Mathf.Clamp(minRopeLength, 0.1f, maxRange);
            reelSpeed = Mathf.Max(0f, reelSpeed);
            springStiffness = Mathf.Max(0f, springStiffness);
            damping = Mathf.Max(0f, damping);
            maximumTensionNewtons = Mathf.Max(0f, maximumTensionNewtons);
            maximumVehicleAcceleration = Mathf.Max(0f, maximumVehicleAcceleration);
            maximumTargetAcceleration = Mathf.Max(0f, maximumTargetAcceleration);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
            maximumAttachmentSeconds = Mathf.Max(0f, maximumAttachmentSeconds);
            activationEnergy = Mathf.Max(0f, activationEnergy);
        }
    }
}
