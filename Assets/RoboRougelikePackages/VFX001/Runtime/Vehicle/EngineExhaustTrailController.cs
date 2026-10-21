using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>World-space exhaust particles; gameplay force must come from the real movement system.</summary>
    public sealed class EngineExhaustTrailController : MonoBehaviour
    {
        [SerializeField] private Transform exhaustSocket;
        [SerializeField, Min(0)] private float maximumRate = 30f;
        [SerializeField, Min(0)] private float particleSpeed = 4.5f;
        [SerializeField, Min(1)] private int particleLimit = 160;
        [SerializeField, Range(0f, 1f)] private float damageSmoke01;
        private ParticleSystem _trail;
        private float _thrust;
        public float Thrust01 => _thrust;
        public float EmissionRate { get; private set; }
        public bool HasEmitter => _trail != null;

        private void Awake()
        {
            if (exhaustSocket == null) exhaustSocket = transform;
            BuildEmitter();
            Apply();
        }
        public void Bind(Transform socket)
        {
            if (socket == null) return;
            exhaustSocket = socket;
            if (_trail != null)
            {
                _trail.transform.SetParent(socket, false);
                _trail.transform.localPosition = Vector3.zero;
            }
        }
        public void SetThrust01(float value) { _thrust = Mathf.Clamp01(value); Apply(); }
        public void SetDamage01(float value) { damageSmoke01 = Mathf.Clamp01(value); Apply(); }

        private void BuildEmitter()
        {
            if (_trail != null) return;
            var go = new GameObject("VFX001 Exhaust Trail (cosmetic)");
            go.transform.SetParent(exhaustSocket != null ? exhaustSocket : transform, false);
            // Local forward of the socket points OUT of the nozzle. Particles keep world-space trajectories.
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = .65f;
            main.startSpeed = particleSpeed;
            main.startSize = .17f;
            main.startColor = new Color(1f, .55f, .18f, .85f);
            main.maxParticles = Mathf.Max(1, particleLimit);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = .08f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, .1f));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = VfxMaterialLibrary.Particle;
            _trail = ps;
            _trail.Play();
        }
        private void Apply()
        {
            if (_trail == null) return;
            EmissionRate = _thrust * maximumRate;
            var em = _trail.emission;
            em.rateOverTime = EmissionRate;
            var main = _trail.main;
            main.startColor = Color.Lerp(new Color(1f, .55f, .17f, .85f), new Color(.33f, .36f, .39f, .48f), damageSmoke01);
        }
        private void OnDisable()
        {
            if (_trail != null) _trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnEnable()
        {
            if (_trail != null && !_trail.isPlaying) _trail.Play();
        }
    }
}
