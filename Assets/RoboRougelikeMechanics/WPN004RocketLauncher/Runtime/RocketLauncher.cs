using UnityEngine;

namespace RoboRougelike.Mechanics.Rockets
{
    [DisallowMultipleComponent]
    public sealed class RocketLauncher : MonoBehaviour
    {
        [SerializeField] private RocketProjectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Rigidbody vehicleBody;
        [SerializeField] private Transform ownerRoot;
        [SerializeField] private RocketConfig config = new RocketConfig();
        [SerializeField] private bool operational = true;
        [SerializeField] private bool requireAttachedToOwner = true;

        private readonly RocketFireGate gate = new RocketFireGate();
        private IRocketAmmoSource ammoSource;
        private IRocketEnergySource energySource;
        private Transform guidanceTarget;

        public float NextReadyTime => gate.NextReadyTime;
        public RocketSpec CurrentSpec => (config ?? new RocketConfig()).Snapshot();
        public bool Operational => operational && isActiveAndEnabled &&
            (!requireAttachedToOwner || (ownerRoot != null && transform.IsChildOf(ownerRoot)));

        public void Bind(Rigidbody body, Transform owner, Transform exit)
        {
            vehicleBody = body;
            ownerRoot = owner;
            muzzle = exit;
        }

        public void SetPrefab(RocketProjectile prefab) => projectilePrefab = prefab;
        public void SetConfig(RocketConfig value) => config = value ?? new RocketConfig();
        public void SetAmmoSource(IRocketAmmoSource value) => ammoSource = value;
        public void SetEnergySource(IRocketEnergySource value) => energySource = value;
        public void SetGuidanceTarget(Transform value) => guidanceTarget = value;
        public void SetOperational(bool value) => operational = value;
        public void SetRequireAttachedToOwner(bool value) => requireAttachedToOwner = value;

        public RocketFireResult TryFire()
        {
            if (!Operational) return RocketFireResult.Disabled;
            if (vehicleBody == null || ownerRoot == null) return RocketFireResult.MissingOwner;
            if (muzzle == null) return RocketFireResult.MissingMuzzle;
            if (projectilePrefab == null) return RocketFireResult.MissingPrefab;
            if (!gate.Ready(Time.time)) return RocketFireResult.Cooldown;

            RocketSpec spec = (config ?? new RocketConfig()).Snapshot();
            bool spentAmmo = false;
            bool spentEnergy = false;
            if (spec.AmmoCost > 0 && ammoSource != null)
            {
                if (!ammoSource.TrySpendAmmo(spec.AmmoCost)) return RocketFireResult.OutOfAmmo;
                spentAmmo = true;
            }
            if (spec.EnergyCost > 0f && energySource != null)
            {
                if (!energySource.TrySpendEnergy(spec.EnergyCost))
                {
                    if (spentAmmo) ammoSource.RefundAmmo(spec.AmmoCost);
                    return RocketFireResult.OutOfEnergy;
                }
                spentEnergy = true;
            }

            RocketProjectile spawned = null;
            try
            {
                spawned = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
                if (spawned == null || !spawned.TryInitialize(spec, vehicleBody, ownerRoot,
                        gameObject, guidanceTarget, muzzle.forward))
                {
                    if (spawned != null) Destroy(spawned.gameObject);
                    if (spentEnergy) energySource.RefundEnergy(spec.EnergyCost);
                    if (spentAmmo) ammoSource.RefundAmmo(spec.AmmoCost);
                    return RocketFireResult.SpawnFailed;
                }
            }
            catch (System.Exception exception)
            {
                if (spawned != null) Destroy(spawned.gameObject);
                if (spentEnergy) energySource.RefundEnergy(spec.EnergyCost);
                if (spentAmmo) ammoSource.RefundAmmo(spec.AmmoCost);
                Debug.LogException(exception, this);
                return RocketFireResult.SpawnFailed;
            }

            if (!spawned.gameObject.activeSelf) spawned.gameObject.SetActive(true);
            gate.Commit(Time.time, spec.Cooldown);
            if (spec.RecoilImpulse > 0f)
                vehicleBody.AddForceAtPosition(-muzzle.forward.normalized * spec.RecoilImpulse,
                    muzzle.position, ForceMode.Impulse);
            return RocketFireResult.Fired;
        }
    }
}
