using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>
    /// Fire and smoke fed by the actual game state. Does NOT apply damage or spread fire.
    /// Existing health and detachment controllers remain authoritative.
    /// Attach only to significant modules to avoid emitter explosions at scale.
    /// </summary>
    public sealed class BurningModuleFxController : MonoBehaviour
    {
        [SerializeField] private Transform visualSocket;
        [SerializeField] private Transform lodCamera;
        [SerializeField, Min(0f)] private float maximumFlameRate = 20f;
        [SerializeField, Min(0f)] private float maximumSmokeRate = 14f;
        [SerializeField, Range(0f,1f)] private float startSmokeAtDamage = .55f;
        [SerializeField] private bool stopWhenDetached;
        private ParticleSystem _flames;
        private ParticleSystem _smoke;
        private float _damage01;
        private bool _burning;
        private bool _detached;
        public float FlameRate { get; private set; }
        public float SmokeRate { get; private set; }
        public bool HasEmitters => _flames != null && _smoke != null;

        private void Awake()
        {
            if (visualSocket == null) visualSocket = transform;
            _flames = Create("VFX001 Cosmetic Flames", new Color(1f,.47f,.1f,.8f), .18f, .55f, 28);
            _smoke = Create("VFX001 Cosmetic Burn Smoke", new Color(.19f,.21f,.23f,.46f), .35f, 1.5f, 32);
            Apply();
        }

        private ParticleSystem Create(string name, Color tint, float size, float lifetime, int particleCap)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(visualSocket, false);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSize = size;
            main.startSpeed = name.Contains("Smoke") ? .48f : .35f;
            main.startLifetime = lifetime;
            main.startColor = tint;
            main.maxParticles = particleCap;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .07f;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = VfxMaterialLibrary.Particle;
            ps.Play();
            return ps;
        }

        public void SetVisualState(float damage01, bool burning, bool detached)
        {
            _damage01 = Mathf.Clamp01(damage01);
            _burning = burning;
            _detached = detached;
            Apply();
        }

        private void LateUpdate() { if (_burning || _damage01 >= startSmokeAtDamage) Apply(); }

        private void Apply()
        {
            if (!HasEmitters) return;
            bool allowed = !(stopWhenDetached && _detached);
            float lod = 1f;
            if (lodCamera != null) lod = VfxDistanceLod.RateFactor(Vector3.Distance(transform.position, lodCamera.position));
            float fire = allowed && _burning ? Mathf.Max(.15f, _damage01) : 0f;
            FlameRate = maximumFlameRate * fire * lod;
            float smoke = allowed ? Mathf.Max(fire * .8f, Mathf.InverseLerp(startSmokeAtDamage, 1f, _damage01) * .6f) : 0f;
            SmokeRate = maximumSmokeRate * smoke * lod;
            var emFlame = _flames.emission;
            emFlame.rateOverTime = FlameRate;
            var emSmoke = _smoke.emission;
            emSmoke.rateOverTime = SmokeRate;
        }
        private void OnDisable()
        {
            if (_flames != null) _flames.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_smoke != null) _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnEnable()
        {
            if (_flames != null && !_flames.isPlaying) _flames.Play();
            if (_smoke != null && !_smoke.isPlaying) _smoke.Play();
        }
    }
}
