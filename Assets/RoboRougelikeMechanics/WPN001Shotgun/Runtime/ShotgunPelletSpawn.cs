using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.WPN001Shotgun
{
    public readonly struct ShotgunPelletSpawn
    {
        public readonly int PelletIndex;
        public readonly uint VolleyIndex;
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly Vector3 InheritedVelocity;
        public readonly float ProjectileSpeed;
        public readonly float Damage;
        public readonly float MaxDistance;
        public readonly float Lifetime;
        public readonly Rigidbody OwnerBody;
        public readonly GameObject OwnerRoot;

        public ShotgunPelletSpawn(int pelletIndex, uint volleyIndex, Vector3 origin,
            Vector3 direction, Vector3 inheritedVelocity, float projectileSpeed,
            float damage, float maxDistance, float lifetime, Rigidbody ownerBody, GameObject ownerRoot)
        {
            PelletIndex = pelletIndex;
            VolleyIndex = volleyIndex;
            Origin = origin;
            Direction = direction;
            InheritedVelocity = inheritedVelocity;
            ProjectileSpeed = projectileSpeed;
            Damage = damage;
            MaxDistance = maxDistance;
            Lifetime = lifetime;
            OwnerBody = ownerBody;
            OwnerRoot = ownerRoot;
        }
    }

    /// <summary>
    /// The game-specific bridge. Must spawn every pellet atomically or clean up all
    /// already-created objects and return false. Do NOT keep a reference to the list:
    /// the module reuses its array on later shots.
    /// Reuse the R6 physical projectile / hit / block damage pipeline here.
    /// </summary>
    public interface IShotgunVolleySink
    {
        bool TrySpawnVolley(IReadOnlyList<ShotgunPelletSpawn> pellets);
    }

    /// <summary>Optional one-shot resource transaction (ammo/energy).
    /// Refund only when the sink refuses a volley; integrations must implement
    /// an exact rollback of their own consumed resource.</summary>
    public interface IShotgunShotResourceBank
    {
        bool TryConsumeShot();
        void RefundShot();
    }
}
