using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.R1
{
    // Isolated prototype. The asset is read-only; existing profiles are never changed.
    [DefaultExecutionOrder(1100)]
    public sealed class LightStylePrototype : MonoBehaviour
    {
        public static LightStylePrototype Instance { get; private set; }
        public bool Enabled { get; private set; }
        public float Strength { get; private set; } = .6f;
        public bool ShowPanel { get; private set; }
        public string Status { get; private set; }

        private Volume volume;
        private VolumeProfile preset;
        private Camera boundCamera;
        private UniversalAdditionalCameraData cameraData;
        private bool originalPostProcessing, createdCameraData;
        private LayerMask originalVolumeMask;
        private int volumeLayer = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            preset = Resources.Load<VolumeProfile>("VisualPrototype/LekkaStylizacja");
            if (preset == null) Status = "Brak profilu prototypu.";
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            bool toggle = keyboard != null && keyboard.f8Key.wasPressedThisFrame;
            bool panel = keyboard != null && keyboard.f10Key.wasPressedThisFrame;
#else
            bool toggle = Input.GetKeyDown(KeyCode.F8);
            bool panel = Input.GetKeyDown(KeyCode.F10);
#endif
            var run = R1RunController.Instance;
            if (toggle) SetEnabled(!Enabled);
            if (panel && run != null && run.Paused) ShowPanel = !ShowPanel;
            if (run == null || !run.Paused) ShowPanel = false;
        }

        private void LateUpdate()
        {
            if (!Enabled || Strength <= 0) return;
            if (boundCamera != Camera.main) Refresh();
        }

        public void SetEnabled(bool enabled) { Enabled = enabled; Refresh(); }
        public void SetStrength(float strength)
        {
            Strength = float.IsNaN(strength) || float.IsInfinity(strength) ? 0 : Mathf.Clamp01(strength);
            Refresh();
        }

        private void Refresh()
        {
            if (!Enabled || Strength <= 0) { RestoreCamera(); return; }
            if (preset == null) { Status = "Brak profilu prototypu."; RestoreCamera(); return; }
            var main = Camera.main;
            if (main == null) { Status = "Brak kamery głównej."; RestoreCamera(); return; }
            if (boundCamera != main || cameraData == null)
            {
                RestoreCamera();
                if (!TryCreateVolume()) return;
                boundCamera = main;
                cameraData = main.GetComponent<UniversalAdditionalCameraData>();
                createdCameraData = cameraData == null;
                if (createdCameraData) cameraData = main.gameObject.AddComponent<UniversalAdditionalCameraData>();
                originalPostProcessing = cameraData.renderPostProcessing;
                originalVolumeMask = cameraData.volumeLayerMask;
                // If post-processing was off, do not awaken unrelated scene volumes.
                cameraData.volumeLayerMask = originalPostProcessing
                    ? originalVolumeMask.value | (1 << volumeLayer) : 1 << volumeLayer;
                cameraData.renderPostProcessing = true;
            }
            volume.weight = Strength;
            volume.enabled = true;
            Status = null;
        }

        private bool TryCreateVolume()
        {
            if (volume != null) return true;
            int used = 0;
            foreach (var other in FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                used |= 1 << other.gameObject.layer;
            for (int layer = 31; layer >= 8; layer--)
                if ((used & (1 << layer)) == 0) { volumeLayer = layer; break; }
            if (volumeLayer < 0) { Status = "Brak wolnej warstwy dla filtra."; return false; }
            var holder = new GameObject("Prototyp - lekka stylizacja") { layer = volumeLayer, hideFlags = HideFlags.DontSave };
            holder.transform.SetParent(transform, false);
            volume = holder.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000;
            volume.sharedProfile = preset;
            volume.weight = 0;
            volume.enabled = false;
            return true;
        }

        private void RestoreCamera()
        {
            if (volume != null) { volume.weight = 0; volume.enabled = false; }
            if (cameraData != null)
            {
                cameraData.volumeLayerMask = originalVolumeMask;
                cameraData.renderPostProcessing = originalPostProcessing;
                if (createdCameraData) Destroy(cameraData);
            }
            boundCamera = null;
            cameraData = null;
            createdCameraData = false;
        }

        internal void DrawPausePanel()
        {
            if (!ShowPanel) return;
            GUI.Box(new Rect(10, 130, 210, 160), "Lekka stylizacja");
            bool enabled = GUI.Toggle(new Rect(22, 165, 180, 24), Enabled, " Włącz filtr");
            if (enabled != Enabled) SetEnabled(enabled);
            GUI.Label(new Rect(22, 197, 180, 24), $"Siła: {Mathf.RoundToInt(Strength * 100)}%");
            float strength = GUI.HorizontalSlider(new Rect(22, 231, 180, 24), Strength, 0, 1);
            if (!Mathf.Approximately(strength, Strength)) SetStrength(strength);
            GUI.Label(new Rect(22, 255, 185, 28), Status ?? "F8: szybki OFF / ON");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            RestoreCamera();
        }
    }
}
