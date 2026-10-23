using UnityEngine;

namespace RoboRougelike.Mechanics.BeamCutter
{
    /// <summary>Implement in R6 adapter. Shield interception MUST run before R6 block HP damage.</summary>
    public interface IBeamDamageSink
    {
        void ApplyBeamHit(in BeamHitRequest hit);
    }

    /// <summary>Optional shared R6 energy store. Spend only when a positive-duration fire tick is accepted.</summary>
    public interface IBeamEnergySource
    {
        bool TryConsume(float energy);
    }

    public readonly struct BeamHitRequest
    {
        public readonly Collider Collider;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly Vector3 Direction;
        public readonly float Damage;
        public readonly float ActiveSeconds;
        public readonly Rigidbody ShooterBody;
        public readonly GameObject Emitter;

        public BeamHitRequest(Collider collider, Vector3 point, Vector3 normal, Vector3 direction,
            float damage, float activeSeconds, Rigidbody shooterBody, GameObject emitter)
        {
            Collider = collider;
            Point = point;
            Normal = normal;
            Direction = direction;
            Damage = damage;
            ActiveSeconds = activeSeconds;
            ShooterBody = shooterBody;
            Emitter = emitter;
        }
    }

    public enum BeamStepStatus
    {
        Idle,
        Firing,
        Overheated,
        NoEnergy,
        Inoperable,
        MissingBindings,
        HitBufferOverflow,
        ObstructedAtMuzzle
    }

    public readonly struct BeamStepResult
    {
        public readonly BeamStepStatus Status;
        public readonly float ActiveSeconds;
        public readonly Collider FirstHit;
        public readonly float DamageRequested;
        public readonly Vector3 EndPoint;

        public BeamStepResult(BeamStepStatus status, float activeSeconds, Collider firstHit,
            float damageRequested, Vector3 endPoint)
        {
            Status = status;
            ActiveSeconds = activeSeconds;
            FirstHit = firstHit;
            DamageRequested = damageRequested;
            EndPoint = endPoint;
        }
    }
}
