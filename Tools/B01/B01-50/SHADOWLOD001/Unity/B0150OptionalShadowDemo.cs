// Manual, play-mode-only sandbox. Generates runtime decorative cubes with disabled
// colliders, never edits the scene or Codex prefabs. Invoke via context menu.
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using B01.Performance.ShadowBudget.UnityAdapter;

namespace B01.Performance.ShadowBudget.UnityAdapter
{
    public sealed class B0150OptionalShadowDemo : MonoBehaviour
    {
        [SerializeField] private B0150OptionalShadowDistancePacer pacer;
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Range(1,128)] private int sampleDecorationCount = 48;
        [ContextMenu("SHADOWLOD001 / Create runtime-only shadow demo")]
        public void CreateSandboxDecoration()
        {
            if (!Application.isPlaying || pacer == null || cameraTransform == null) return;
            var demo = new Renderer[Mathf.Clamp(sampleDecorationCount, 1, 128)];
            for (int i = 0; i < demo.Length; i++)
            {
                var sample = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sample.name = "B0150 runtime-only optional shadow " + i;
                sample.transform.SetParent(transform, false);
                sample.transform.localPosition = new Vector3((i % 8) * 3, 0, (i / 8) * 8);
                Collider collider = sample.GetComponent<Collider>();
                if (collider != null) { collider.enabled = false; Destroy(collider); }
                demo[i] = sample.GetComponent<Renderer>();
                demo[i].shadowCastingMode = ShadowCastingMode.On;
            }
            pacer.SetOptionalDecoration(demo, cameraTransform);
        }
    }
}
#endif
