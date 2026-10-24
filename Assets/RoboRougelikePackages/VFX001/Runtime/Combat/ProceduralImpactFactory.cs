using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Legacy v1 adapter. Prefer VfxPoolManager.Emit for long-lived integration.</summary>
    public static class ProceduralImpactFactory
    {
        public static GameObject CreateImpact(string name, Vector3 position, Vector3 normal, ImpactFeedbackSpec spec, Transform parent = null)
        {
            var root = new GameObject(name ?? "ImpactVFX");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal.sqrMagnitude > .00001f ? normal.normalized : Vector3.up));
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = Mathf.Max(.02f, spec.lifeTime);
            main.startSpeed = Mathf.Max(0f, spec.speed);
            main.startSize = Mathf.Max(.01f, spec.size);
            main.startColor = spec.color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Max(1, spec.particleCount);
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = .01f;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.sharedMaterial = VfxMaterialLibrary.Particle;
            renderer.velocityScale = .1f;
            var cleanup = root.AddComponent<TransientAutoDestroy>();
            cleanup.SetLifetime(main.startLifetime.constantMax + .3f);
            ps.Emit(Mathf.Max(0, spec.particleCount));
            ps.Play();
            return root;
        }
    }
}
