// B01-10 GHOST-001/R2: original Unity 6 URP opt-in, visual-only replay.
// No collisions, no physics bodies, no CHL/R1/R5 controller writes.
// Native Unity Editor/PlayMode is NOT_RUN until independently tested.
#if UNITY_5_3_OR_NEWER
using System;
using RoboRougelike.B0110.TrainingGhost;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoboRougelike.B0110.TrainingGhost
{
    [DisallowMultipleComponent]
    public sealed class GhostVisualProxy : MonoBehaviour
    {
        private GhostPlayback playback;
        private GhostPracticeLoop drill;
        private Mesh ownedMesh;
        private Material ownedMaterial;
        private MeshRenderer visual;
        private const float Height = 0.12f;
        // OFF by default. When explicitly enabled, this dedicated visual
        // proxy draws practice buttons and drives only its own ghost arrow.
        [SerializeField] private bool showPracticeControls = false;
        private GhostPracticeTransport practiceTransport;
        private bool explicitlyHidden;

        public bool IsBound => playback != null || drill != null;
        public float Duration => drill != null ? drill.SegmentDurationSeconds
            : playback == null ? 0f : playback.Duration;

        /// <summary>
        /// Call ONLY from an opt-in CHL host adapter AFTER it accepted
        /// an actual completed, player-verified reference recording.
        /// Install on a dedicated EMPTY root GameObject, not a real vehicle.
        /// </summary>
        private void ValidateVisualOnlyRoot()
        {
            if (transform.parent != null || GetComponent<Rigidbody>() != null
                || GetComponent<Collider>() != null
                || GetComponentInChildren<Collider>() != null
                || GetComponentInChildren<Rigidbody>() != null
                || GetComponent<MeshFilter>() != null && visual == null
                || GetComponent<MeshRenderer>() != null && visual == null)
                throw new InvalidOperationException(
                    "GHOST-001 requires its own empty non-colliding root, never a real robot");
        }

        public void Bind(GhostRun completedTrack)
        {
            ValidateVisualOnlyRoot();
            var validated = new GhostPlayback(completedTrack);
            EnsureVisual();
            drill = null;
            practiceTransport = null;
            playback = validated;
            explicitlyHidden = false;
            visual.enabled = false;
        }

        // Bind only a completed, meaningful, coach-issued practice loop to
        // this dedicated, collider-free visual root; no gameplay actor control.
        public void BindDrill(GhostPracticeLoop practiceLoop)
        {
            if (practiceLoop == null)
                throw new ArgumentException("No completed significant practice loop");
            ValidateVisualOnlyRoot();
            EnsureVisual();
            playback = null;
            drill = practiceLoop;
            practiceTransport = new GhostPracticeTransport(practiceLoop);
            explicitlyHidden = false;
            visual.enabled = false;
        }

        /// <summary>
        /// Supplied by the authoritative host playback clock. This method
        /// NEVER advances time, pushes physics forces or fires checkpoints.
        /// </summary>
        public void Seek(float elapsedSeconds)
        {
            if (playback == null) throw new InvalidOperationException(
                "Cannot display an unbound GHOST-001 reference");
            ApplyVisualPose(playback.Seek(elapsedSeconds));
        }

        public void SeekDrill(double visualLoopElapsedSeconds) =>
            SeekDrill(visualLoopElapsedSeconds, 1f);

        // The host selects the visual demonstration rate (0.25..2x).
        // Sampling does NOT adjust Unity Time.timeScale or physical velocity.
        public void SeekDrill(double visualLoopElapsedSeconds, float playbackSpeed)
        {
            if (drill == null)
                throw new InvalidOperationException(
                    "Cannot replay an unbound training segment");
            ApplyVisualPose(drill.PoseAt(visualLoopElapsedSeconds, playbackSpeed));
        }

        private void ApplyVisualPose(GhostPose p)
        {
            transform.SetPositionAndRotation(
                new Vector3(p.X, p.Y + Height, p.Z),
                Quaternion.Euler(0f, p.Yaw, 0f));
            if (visual != null) visual.enabled = !explicitlyHidden;
        }

        public void Hide()
        {
            explicitlyHidden = true;
            if (visual != null) visual.enabled = false;
        }

        public void Show()
        {
            explicitlyHidden = false;
            if (visual != null) visual.enabled = true;
        }

        // Mutually exclusive with a host-driven manual SeekDrill clock:
        // enable this built-in transport only for opt-in local visual review.
        public void EnablePracticeControls(bool enable)
        {
            if (enable && practiceTransport == null)
                throw new InvalidOperationException(
                    "Bind a completed coach practice loop before enabling controls");
            showPracticeControls = enable;
            if (!enable && practiceTransport != null)
                practiceTransport.Pause();
        }

        private void Update()
        {
            if (!showPracticeControls || practiceTransport == null || drill == null)
                return;
            practiceTransport.Advance(Time.unscaledDeltaTime);
            // Ghost pose only: no Time.timeScale/Rigidbody/PhysX or CHL state.
            ApplyVisualPose(practiceTransport.Pose());
        }

        private void OnGUI()
        {
            if (!showPracticeControls || practiceTransport == null || drill == null)
                return;
            float width = Mathf.Min(390f, Mathf.Max(240f, Screen.width - 20f));
            float x = Mathf.Max(10f, Screen.width - width - 10f);
            float y = Mathf.Max(10f, Screen.height - 155f);
            GUI.Box(new Rect(x, y, width, 145f),
                "CHL VISUAL DRILL / " + drill.Label + " (no physics)");
            GUI.Label(new Rect(x + 10f, y + 22f, width - 20f, 20f),
                "Visual segment: " + practiceTransport.ReferencePositionSeconds.ToString("0.00")
                + "/" + practiceTransport.ReferenceSegmentDuration.ToString("0.00")
                + "s  @ " + practiceTransport.PlaybackSpeed.ToString("0.##") + "x");

            if (GUI.Button(new Rect(x + 10f, y + 45f, 72f, 25f),
                practiceTransport.IsPlaying ? "Pause" : "Play"))
            {
                if (practiceTransport.IsPlaying) practiceTransport.Pause();
                else practiceTransport.Play();
            }
            if (GUI.Button(new Rect(x + 86f, y + 45f, 56f, 25f), "-0.1s"))
            {
                practiceTransport.Pause();
                practiceTransport.Step(-0.1d);
            }
            if (GUI.Button(new Rect(x + 146f, y + 45f, 56f, 25f), "+0.1s"))
            {
                practiceTransport.Pause();
                practiceTransport.Step(0.1d);
            }
            if (GUI.Button(new Rect(x + 206f, y + 45f, 68f, 25f), "Restart"))
                practiceTransport.Restart();

            float button = (width - 50f) / 4f;
            if (GUI.Button(new Rect(x + 10f, y + 78f, button, 24f), "0.25x"))
                practiceTransport.SetSpeed(.25f);
            if (GUI.Button(new Rect(x + 20f + button, y + 78f, button, 24f), "0.5x"))
                practiceTransport.SetSpeed(.5f);
            if (GUI.Button(new Rect(x + 30f + 2f * button, y + 78f, button, 24f), "1x"))
                practiceTransport.SetSpeed(1f);
            if (GUI.Button(new Rect(x + 40f + 3f * button, y + 78f, button, 24f), "2x"))
                practiceTransport.SetSpeed(2f);

            // Opt-in one-shot study: keep the actual final pose on screen
            // until Play or Restart. Loop is the backwards-compatible default.
            bool hold = practiceTransport.RepeatMode
                == GhostPracticeRepeatMode.StopAtEnd;
            if (GUI.Button(new Rect(x + 10f, y + 109f, width - 20f, 25f),
                hold ? "Repeat: Stop at end (click to loop)"
                     : "Repeat: Loop (click to stop at end)"))
                practiceTransport.SetRepeatMode(hold
                    ? GhostPracticeRepeatMode.Loop
                    : GhostPracticeRepeatMode.StopAtEnd);
        }

        private void EnsureVisual()
        {
            if (visual != null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException(
                    "URP Unlit shader not present; cannot display training ghost");
            // A small extruded directional arrow; not a primitive collider and
            // not an enemy/gameplay actor. Only MeshFilter + MeshRenderer.
            var filter = gameObject.AddComponent<MeshFilter>();
            visual = gameObject.AddComponent<MeshRenderer>();
            ownedMesh = new Mesh { name = "B01TrainingGhost_VisualArrow" };
            ownedMesh.vertices = new[]
            {
                new Vector3(-0.36f, 0f, -0.40f),
                new Vector3( 0.36f, 0f, -0.40f),
                new Vector3( 0.00f, 0f,  0.75f),
                new Vector3(-0.36f, 0.12f, -0.40f),
                new Vector3( 0.36f, 0.12f, -0.40f),
                new Vector3( 0.00f, 0.12f, 0.75f)
            };
            ownedMesh.triangles = new[]
            {
                0, 2, 1, 3, 4, 5,
                0, 1, 4, 0, 4, 3,
                1, 2, 5, 1, 5, 4,
                2, 0, 3, 2, 3, 5
            };
            ownedMesh.RecalculateNormals();
            ownedMesh.RecalculateBounds();
            filter.sharedMesh = ownedMesh;
            ownedMaterial = new Material(shader) { name = "B01TrainingGhost_URP_VisualOnly" };
            ownedMaterial.SetColor("_BaseColor", new Color(0.22f, 0.92f, 1f, 0.34f));
            ownedMaterial.SetFloat("_Surface", 1f);
            ownedMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            ownedMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            ownedMaterial.SetFloat("_ZWrite", 0f);
            ownedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ownedMaterial.renderQueue = (int)RenderQueue.Transparent;
            visual.sharedMaterial = ownedMaterial;
            visual.shadowCastingMode = ShadowCastingMode.Off;
            visual.receiveShadows = false;
            visual.enabled = false;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (ownedMaterial != null) Destroy(ownedMaterial);
                if (ownedMesh != null) Destroy(ownedMesh);
            }
            else
            {
                if (ownedMaterial != null) DestroyImmediate(ownedMaterial);
                if (ownedMesh != null) DestroyImmediate(ownedMesh);
            }
        }
    }
}
#endif
