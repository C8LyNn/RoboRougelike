using UnityEngine;

namespace RoboRougelike.WPN002Railgun.Demo
{
    public sealed class RailgunDemoShotSink : MonoBehaviour, IRailgunShotSink
    {
        [SerializeField] private RailgunDemoProjectile projectilePrefab;
        public RailgunDemoProjectile LastSpawned { get; private set; }
        public void SetPrefab(RailgunDemoProjectile prefab) => projectilePrefab = prefab;
        public bool TrySpawnShot(RailgunShot shot)
        {
            if (projectilePrefab == null || shot.InitialVelocity.sqrMagnitude < 0.001f) return false;
            RailgunDemoProjectile instance = Instantiate(projectilePrefab, shot.Origin,
                Quaternion.LookRotation(shot.InitialVelocity.normalized));
            instance.Initialize(shot);
            instance.gameObject.SetActive(true);
            LastSpawned = instance;
            return true;
        }
    }
}
