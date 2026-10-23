using UnityEngine;

namespace RoboRougelike.WPN002Railgun
{
    // A production sink MUST spawn through the game's existing projectile/damage system.
    public readonly struct RailgunShot
    {
        public readonly uint ShotSequence;
        public readonly Vector3 Origin, Direction, InitialVelocity;
        public readonly RailgunSpec Spec;
        public readonly Rigidbody OwnerBody;
        public readonly GameObject OwnerRoot;
        public RailgunShot(uint sequence, Vector3 origin, Vector3 direction, Vector3 velocity,
            RailgunSpec spec, Rigidbody ownerBody, GameObject ownerRoot)
        {
            ShotSequence = sequence;
            Origin = origin; Direction = direction.normalized;
            InitialVelocity = velocity; Spec = spec; OwnerBody = ownerBody; OwnerRoot = ownerRoot;
        }
    }
    public interface IRailgunShotSink { bool TrySpawnShot(RailgunShot shot); }
    public interface IRailgunShotResourceBank
    {
        bool TryConsumeShot();
        void RefundShot();
    }
}
