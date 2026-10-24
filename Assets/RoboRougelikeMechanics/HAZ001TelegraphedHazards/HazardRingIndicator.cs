using UnityEngine;

namespace RoboRougelike.Mechanics.HAZ001
{
    /// <summary>Optional visible warning ring. Creates only its own transient child.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TelegraphedHazardZone))]
    public sealed class HazardRingIndicator : MonoBehaviour
    {
        [SerializeField, Range(16, 128)] private int segments = 64;
        [SerializeField, Min(0.01f)] private float width = 0.08f;
        private TelegraphedHazardZone zone;
        private LineRenderer ring;
        private GameObject ringObject;
        private Material ownedMaterial;

        private void Awake()
        {
            zone = GetComponent<TelegraphedHazardZone>();
            ringObject = new GameObject("HAZ001 Warning Ring (runtime)");
            ringObject.transform.SetParent(transform, false);
            ring = ringObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.widthMultiplier = width;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                ownedMaterial = new Material(shader);
                ring.sharedMaterial = ownedMaterial;
            }
            ring.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                ring.SetPosition(i, new Vector3(
                    Mathf.Cos(angle) * zone.Radius, 0.12f,
                    Mathf.Sin(angle) * zone.Radius));
            }
            ApplyPhase(zone.Phase);
        }

        private void OnEnable()
        {
            if (zone == null) zone = GetComponent<TelegraphedHazardZone>();
            zone.PhaseChanged += ApplyPhase;
            if (ring != null) ApplyPhase(zone.Phase);
        }

        private void OnDisable()
        {
            if (zone != null) zone.PhaseChanged -= ApplyPhase;
            if (ring != null) ring.enabled = false;
        }

        private void ApplyPhase(HazardPhase phase)
        {
            if (ring == null) return;
            ring.enabled = phase == HazardPhase.Telegraph || phase == HazardPhase.Active;
            Color color = phase == HazardPhase.Active ? Color.red : Color.yellow;
            ring.startColor = color;
            ring.endColor = color;
            if (ownedMaterial != null)
            {
                if (ownedMaterial.HasProperty("_BaseColor"))
                    ownedMaterial.SetColor("_BaseColor", color);
                if (ownedMaterial.HasProperty("_Color"))
                    ownedMaterial.SetColor("_Color", color);
            }
        }

        private void OnDestroy()
        {
            if (ringObject != null) Destroy(ringObject);
            if (ownedMaterial != null) Destroy(ownedMaterial);
        }
    }
}
