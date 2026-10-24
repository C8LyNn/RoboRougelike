using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Reusable, self-contained four-layer burst: flash, sparks, smoke and debris.</summary>
    public sealed class VfxBurstActor : MonoBehaviour
    {
        private readonly ParticleSystem[] _layers = new ParticleSystem[4];
        private VfxPoolManager _owner;
        private float _expires;
        private bool _playing;
        private int _version;
        private VfxShockwaveRing _ring;
        private VfxLayerSpec _delayedSmoke;
        private VfxLayerSpec _delayedFragments;
        private bool _smokePending;
        private bool _fragmentsPending;
        private float _startedAt;
        public bool IsPlaying => _playing;
        public int PlayVersion => _version;

        public void Initialize(VfxPoolManager owner)
        {
            _owner = owner;
            CreateLayer(0, "flash", ParticleSystemRenderMode.Billboard);
            CreateLayer(1, "sparks", ParticleSystemRenderMode.Stretch);
            CreateLayer(2, "smoke", ParticleSystemRenderMode.Billboard);
            CreateLayer(3, "fragments", ParticleSystemRenderMode.Stretch);
            var ringObject = new GameObject("shockwave - cosmetic");
            ringObject.transform.SetParent(transform, false);
            _ring = ringObject.AddComponent<VfxShockwaveRing>();
            _ring.Initialize();
        }

        private void CreateLayer(int index, string name, ParticleSystemRenderMode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;
            main.duration = 3f;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = index == 2 ? 75f : 40f;
            shape.radius = 0.01f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = index == 3 ? ParticleSystemRenderMode.Mesh : mode;
            if (index == 3) renderer.mesh = VfxDebrisMeshLibrary.ForMaterial(ImpactMaterialType.Metal);
            renderer.sharedMaterial = VfxMaterialLibrary.Particle;
            if (mode == ParticleSystemRenderMode.Stretch)
            {
                renderer.velocityScale = index == 1 ? .1f : .02f;
                renderer.lengthScale = index == 1 ? 1.2f : .6f;
            }
            _layers[index] = ps;
        }

        public void Play(VfxEvent evt)
        {
            transform.position = evt.Position;
            transform.rotation = Quaternion.LookRotation(evt.Normal);
            gameObject.SetActive(true);
            var fragmentRenderer = _layers[3].GetComponent<ParticleSystemRenderer>();
            fragmentRenderer.mesh = VfxDebrisMeshLibrary.ForMaterial(evt.Material);
            var spec = VfxPresetResolver.Resolve(evt);
            SetLayer(0, spec.Flash);
            SetLayer(1, spec.Sparks);
            // Stagger smoke/debris so a large explosion does not look like one static puff.
            SetLayer(2, new VfxLayerSpec(0, 0f, .1f, .1f, Color.white));
            SetLayer(3, new VfxLayerSpec(0, 0f, .1f, .1f, Color.white));
            _delayedSmoke = spec.Smoke;
            _delayedFragments = spec.Fragments;
            _smokePending = spec.Smoke.Count > 0;
            _fragmentsPending = spec.Fragments.Count > 0;
            _startedAt = Time.time;
            if (_ring != null)
            {
                if (evt.Kind == VfxEventKind.Explosion || evt.Kind == VfxEventKind.CoreDestroyed)
                    _ring.Begin(evt.Intensity, evt.Kind == VfxEventKind.CoreDestroyed);
                else _ring.StopRing();
            }
            _expires = Time.time + spec.Duration;
            _playing = true;
            _version++;
        }

        private void SetLayer(int i, VfxLayerSpec s)
        {
            var ps = _layers[i];
            ps.Clear(true);
            if (s.Count == 0) { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); return; }
            var main = ps.main;
            main.startSpeed = s.Speed;
            main.startLifetime = s.Lifetime;
            main.startSize = s.Size;
            main.startColor = s.Color;
            ps.Emit(s.Count);
            ps.Play();
        }

        private void Update()
        {
            if (!_playing) return;
            float age = Time.time - _startedAt;
            if (_fragmentsPending && age >= .045f)
            {
                _fragmentsPending = false;
                SetLayer(3, _delayedFragments);
            }
            if (_smokePending && age >= .11f)
            {
                _smokePending = false;
                SetLayer(2, _delayedSmoke);
            }
            if (_ring != null) _ring.Tick(Time.deltaTime);
            // Scaled time keeps the burst alive while the game is paused.
            if (Time.time >= _expires && _owner != null) _owner.Return(this);
        }

        public void StopAndReset()
        {
            _playing = false;
            _smokePending = false;
            _fragmentsPending = false;
            if (_ring != null) _ring.StopRing();
            foreach (var system in _layers)
                if (system != null) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }
    }
}
