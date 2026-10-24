using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Cosmetic dust and skid trails, driven by speed/slip/ground contact from vehicle physics.</summary>
    public sealed class GroundContactVfxController : MonoBehaviour
    {
        [SerializeField] private Transform leftContact;
        [SerializeField] private Transform rightContact;
        [SerializeField, Min(0f)] private float minSlipForMarks = .45f;
        [SerializeField, Min(0f)] private float minSpeedForMarks = 1.5f;
        [SerializeField, Min(0f)] private float maxDustRate = 20f;
        [SerializeField, Min(.01f)] private float markWidth = .12f;
        [SerializeField, Min(.1f)] private float markLifetime = 3f;
        private ParticleSystem _dust;
        private TrailRenderer _left;
        private TrailRenderer _right;
        private float _speed;
        private float _slip;
        private bool _grounded;
        public bool MarksEmitting => _left != null && _left.emitting;
        public float DustEmissionRate { get; private set; }

        private void Awake()
        {
            if (leftContact == null) leftContact = transform;
            if (rightContact == null) rightContact = transform;
            _dust = CreateDust();
            _left = CreateMark(leftContact, "Left Skid Mark");
            _right = CreateMark(rightContact, "Right Skid Mark");
            Apply();
        }
        public void SetMotion(float speedMetersPerSecond, float slip01, bool grounded)
        {
            _speed = Mathf.Max(0f, speedMetersPerSecond);
            _slip = Mathf.Clamp01(slip01);
            _grounded = grounded;
            Apply();
        }
        public void Bind(Transform left, Transform right)
        {
            if (left == null || right == null) return;
            leftContact = left;
            rightContact = right;
            if (_left != null) { _left.transform.SetParent(left, false); _left.transform.localPosition = Vector3.zero; _left.Clear(); }
            if (_right != null) { _right.transform.SetParent(right, false); _right.transform.localPosition = Vector3.zero; _right.Clear(); }
        }
        private ParticleSystem CreateDust()
        {
            var go = new GameObject("VFX001 Ground Dust");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = .7f;
            main.startSpeed = 1f;
            main.startSize = .14f;
            main.startColor = new Color(.55f, .51f, .44f, .4f);
            main.maxParticles = 80;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .12f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = VfxMaterialLibrary.Particle;
            ps.Play();
            return ps;
        }
        private TrailRenderer CreateMark(Transform socket, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(socket, false);
            var mark = go.AddComponent<TrailRenderer>();
            mark.sharedMaterial = VfxMaterialLibrary.SurfaceMark;
            mark.time = markLifetime;
            mark.minVertexDistance = .16f;
            mark.startWidth = markWidth;
            mark.endWidth = markWidth;
            mark.alignment = LineAlignment.TransformZ;
            mark.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mark.receiveShadows = false;
            mark.emitting = false;
            mark.startColor = new Color(.05f, .05f, .05f, .36f);
            mark.endColor = new Color(.05f, .05f, .05f, 0f);
            return mark;
        }
        private void Apply()
        {
            bool skid = _grounded && _speed >= minSpeedForMarks && _slip >= minSlipForMarks;
            if (_left != null) _left.emitting = skid;
            if (_right != null) _right.emitting = skid;
            DustEmissionRate = _grounded ? _slip * Mathf.Clamp01(_speed / 12f) * maxDustRate : 0f;
            if (_dust != null)
            {
                var em = _dust.emission;
                em.rateOverTime = DustEmissionRate;
            }
        }
        private void OnDisable()
        {
            if (_left != null) { _left.emitting = false; _left.Clear(); }
            if (_right != null) { _right.emitting = false; _right.Clear(); }
            if (_dust != null) _dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnEnable() { if (_dust != null && !_dust.isPlaying) _dust.Play(); }
    }
}
