using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Continuous cosmetic smoke on a dedicated socket; never edits HP or physics.</summary>
    public sealed class PersistentDamageFxController : MonoBehaviour
    {
        [SerializeField] private Transform smokeSocket;
        [SerializeField, Range(0f, 1f)] private float startSmokeAtDamage = .45f;
        [SerializeField, Min(0)] private float maximumParticlesPerSecond = 10f;
        [SerializeField, Min(0)] private int maxParticles = 40;
        private ParticleSystem _smoke;
        private float _damage;
        private bool _detached;
        public float Damage01 => _damage;
        public bool Detached => _detached;
        public float CurrentEmissionRate { get; private set; }
        public bool HasEmitter => _smoke != null;

        private void Awake()
        {
            if (smokeSocket == null) smokeSocket = transform;
            BuildEmitter();
            Apply();
        }

        public void SetDamage01(float damage01)
        {
            _damage = Mathf.Clamp01(damage01);
            Apply();
        }

        public void SetDetached(bool detached)
        {
            _detached = detached;
            Apply();
        }

        public void Bind(Transform socket)
        {
            if (socket == null) return;
            smokeSocket = socket;
            if (_smoke != null)
            {
                _smoke.transform.SetParent(socket, false);
                _smoke.transform.localPosition = Vector3.zero;
                _smoke.transform.localRotation = Quaternion.identity;
            }
        }

        private void BuildEmitter()
        {
            if (_smoke != null) return;
            var go = new GameObject("VFX001 Damage Smoke (cosmetic)");
            go.transform.SetParent(smokeSocket != null ? smokeSocket : transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.5f;
            main.startSpeed = .65f;
            main.startSize = .32f;
            main.startColor = new Color(.28f, .31f, .34f, .4f);
            main.maxParticles = Mathf.Max(1, maxParticles);
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .1f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = VfxMaterialLibrary.Particle;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .3f, 1f, 1.5f));
            _smoke = ps;
            _smoke.Play();
        }

        private void Apply()
        {
            if (_smoke == null) return;
            float amount = _detached ? 0f : Mathf.InverseLerp(startSmokeAtDamage, 1f, _damage);
            CurrentEmissionRate = amount * maximumParticlesPerSecond;
            var emission = _smoke.emission;
            emission.rateOverTime = CurrentEmissionRate;
        }

        private void OnDisable()
        {
            if (_smoke != null) _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnEnable()
        {
            if (_smoke != null && !_smoke.isPlaying) _smoke.Play();
        }
    }
}
