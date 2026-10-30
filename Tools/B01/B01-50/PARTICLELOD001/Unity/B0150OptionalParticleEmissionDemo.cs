// Manual PlayMode-only decorative ParticleSystem sample; no saved scene modifications.
// Does not enable physics, collision, triggers or gameplay scripts.
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using B01.Performance.ParticleEmission.UnityAdapter;

namespace B01.Performance.ParticleEmission.UnityAdapter
{
    public sealed class B0150OptionalParticleEmissionDemo : MonoBehaviour
    {
        [SerializeField] private B0150OptionalParticleEmissionLod emissionController;
        [SerializeField] private Transform sceneCamera;
        [SerializeField, Range(1,128)] private int count = 24;

        [ContextMenu("PARTICLELOD-001 / Create optional particle demonstration")]
        public void CreateOptionalParticleSamples()
        {
            if (!Application.isPlaying || emissionController == null || sceneCamera == null) return;
            var systems = new ParticleSystem[Mathf.Clamp(count,1,128)];
            for (int i = 0; i < systems.Length; i++)
            {
                var sample = new GameObject("B0150 runtime-only optional dust " + i);
                sample.transform.SetParent(transform, false);
                sample.transform.localPosition = new Vector3((i % 8) * 4f, 0, (i / 8) * 12f);
                ParticleSystem ps = sample.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 0.5f;
                main.startSpeed = 0.1f;
                main.maxParticles = 30;
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = 4f;
                var collision = ps.collision;
                collision.enabled = false;
                var trigger = ps.trigger;
                trigger.enabled = false;
                systems[i] = ps;
            }
            emissionController.SetOptionalDecorations(systems, sceneCamera);
        }
    }
}
#endif
