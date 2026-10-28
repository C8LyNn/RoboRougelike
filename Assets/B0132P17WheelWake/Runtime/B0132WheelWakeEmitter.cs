using UnityEngine;

namespace RoboRougelike.B0132P17
{
    /// <summary>
    /// Two-wheel visual dust/spark trails. Explicit inputs only: the game owner
    /// selects a cosmetic mode and sends normalized slip/contact. No Rigidbody,
    /// wheel controller, canonical VFX001, weapon or damage references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0132WheelWakeEmitter : MonoBehaviour
    {
        [SerializeField] private Vector3 leftWheelOffset = new Vector3(-.8f, -.24f, -.58f);
        [SerializeField] private Vector3 rightWheelOffset = new Vector3(.8f, -.24f, -.58f);

        private ParticleSystem[] systems;
        private Material ownedMaterial;
        private WheelWakeSample lastSample;

        public int SystemCount => systems == null ? 0 : systems.Length;
        public bool IsEmitting => lastSample.Visible && isActiveAndEnabled;
        public WheelWakeSample LastSample => lastSample;

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (systems != null) return;
            systems = new ParticleSystem[2];
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) ownedMaterial = new Material(shader);
            systems[0] = MakeOne("left contact wake", leftWheelOffset);
            systems[1] = MakeOne("right contact wake", rightWheelOffset);
        }

        private ParticleSystem MakeOne(string title, Vector3 offset)
        {
            var go = new GameObject(title);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            // Local +Z is turned toward the vehicle's rear.
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 2f;
            main.maxParticles = 128;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = .4f;
            main.startSpeed = .55f;
            main.startSize = .18f;
            main.startColor = Color.clear;
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = .13f;
            shape.angle = 18f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && ownedMaterial != null)
                renderer.sharedMaterial = ownedMaterial;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return system;
        }

        // Called by a future Codex-approved adapter or the isolated demo.
        // A VFX OFF or loss-of-ground-contact immediately clears all live particles.
        public void SetVisualInput(WheelWakeMode mode, float speedMetersPerSecond,
            float slip01, bool grounded, bool vfxEnabled)
        {
            if (!isActiveAndEnabled)
            {
                lastSample = default;
                return;
            }
            EnsureInitialized();
            var value = WheelWakeProfile.Evaluate(mode, speedMetersPerSecond,
                slip01, grounded, vfxEnabled);
            lastSample = value;
            if (!value.Visible)
            {
                ClearAll();
                return;
            }

            for (int i = 0; i < systems.Length; i++)
            {
                var system = systems[i];
                if (system == null) continue;
                var main = system.main;
                main.startLifetime = value.ParticleLifetime;
                main.startSpeed = value.ParticleSpeed;
                main.startSize = value.ParticleSize;
                main.startColor = new Color(value.Red, value.Green,
                    value.Blue, value.Alpha);
                var emission = system.emission;
                emission.rateOverTime = value.RatePerSecond * .5f;
                if (!system.isPlaying) system.Play(true);
            }
        }

        public void ClearAll()
        {
            lastSample = default;
            if (systems == null) return;
            foreach (var system in systems)
            {
                if (system == null) continue;
                var emission = system.emission;
                emission.rateOverTime = 0f;
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnDisable() => ClearAll();

        private void OnDestroy()
        {
            ClearAll();
            if (ownedMaterial != null) Destroy(ownedMaterial);
            ownedMaterial = null;
        }
    }
}
