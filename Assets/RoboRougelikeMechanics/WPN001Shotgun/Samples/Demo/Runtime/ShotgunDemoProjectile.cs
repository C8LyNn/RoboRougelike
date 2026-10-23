using UnityEngine;

namespace RoboRougelike.WPN001Shotgun.Demo
{
    /// <summary>Minimal standalone projectile. Demo ONLY: production uses R6 projectile & damage.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class ShotgunDemoProjectile : MonoBehaviour
    {
        private Rigidbody body;
        private GameObject ownerRoot;
        private Vector3 origin;
        private float damage;
        private float maxDistance;
        private float lifetime;
        private float elapsed;
        private bool armed;

        public void Initialize(ShotgunPelletSpawn spawn)
        {
            body = GetComponent<Rigidbody>();
            ownerRoot = spawn.OwnerRoot;
            origin = spawn.Origin;
            damage = spawn.Damage;
            maxDistance = spawn.MaxDistance;
            lifetime = spawn.Lifetime;
            transform.position = spawn.Origin;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.None;
            body.linearVelocity = spawn.Direction * spawn.ProjectileSpeed + spawn.InheritedVelocity;
            armed = true;
        }

        private void FixedUpdate()
        {
            if (!armed) return;
            elapsed += Time.fixedDeltaTime;
            if (elapsed >= lifetime || (transform.position - origin).sqrMagnitude >= maxDistance * maxDistance)
                Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!armed) return;
            if (ownerRoot != null && collision.collider.transform.IsChildOf(ownerRoot.transform))
                return;
            var target = collision.collider.GetComponentInParent<ShotgunDemoTargetHealth>();
            if (target != null) target.ApplyDamage(damage);
            armed = false;
            Destroy(gameObject);
        }
    }
}
