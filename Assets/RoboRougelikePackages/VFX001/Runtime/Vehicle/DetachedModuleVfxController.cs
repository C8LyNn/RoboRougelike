using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Optional bounded trailing sparks on fast, already-detached pieces; no Rigidbody writes.</summary>
    public sealed class DetachedModuleVfxController : MonoBehaviour
    {
        [SerializeField] private Transform sparksSocket;
        [SerializeField, Min(.01f)] private float minimumSpeed = 2.2f;
        [SerializeField, Min(0f)] private float maximumEmission = 9f;
        private ParticleSystem _sparks;
        private bool _detached;
        private float _speed;
        public float EmissionRate { get; private set; }
        public bool IsDetached => _detached;
        public bool HasEmitter => _sparks != null;

        public void SetDetachedMotion(bool detached, float speedMetersPerSecond)
        {
            _detached = detached;
            _speed = Mathf.Max(0f, speedMetersPerSecond);
            if (_detached && _sparks == null) CreateEmitter();
            Apply();
        }
        private void CreateEmitter()
        {
            var go = new GameObject("VFX001 Detached Sparks", typeof(ParticleSystem));
            go.transform.SetParent(sparksSocket != null ? sparksSocket : transform, false);
            _sparks = go.GetComponent<ParticleSystem>();
            var main = _sparks.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = .8f;
            main.startLifetime = .27f;
            main.startSize = .045f;
            main.startColor = new Color(1f, .71f, .2f, .9f);
            main.maxParticles = 16;
            var emission = _sparks.emission;
            emission.rateOverTime = 0f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = VfxMaterialLibrary.Particle;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.4f;
            _sparks.Play();
        }
        private void Apply()
        {
            EmissionRate = _detached ? Mathf.Clamp01((_speed - minimumSpeed) / 8f) * maximumEmission : 0f;
            if (_sparks == null) return;
            var e = _sparks.emission;
            e.rateOverTime = EmissionRate;
        }
        private void OnDisable()
        {
            if (_sparks != null) _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnEnable() { if (_sparks != null && !_sparks.isPlaying) _sparks.Play(); }
    }
}
