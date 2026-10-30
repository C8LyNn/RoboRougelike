using UnityEngine;

namespace RoboRougelike.B0132P17.RepairWeld
{
    /// <summary>
    /// Cosmetic-only repair welding spark visualization. Another authorized system
    /// may call BeginWeld / SetVisualProgress / FinishWeld / CancelWeld.
    /// This component never repairs parts or spends resources.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0132RepairWeldVfx : MonoBehaviour
    {
        [SerializeField] private bool vfxEnabled = true;
        private readonly WeldVisualCore core = new WeldVisualCore();
        private readonly WeldFrame frame = new WeldFrame();
        private LineRenderer arc;
        private readonly LineRenderer[] sparks = new LineRenderer[8];
        private Material lineMaterial;

        public WeldPhase Phase => core.Phase;
        public bool IsVisualActive => vfxEnabled && isActiveAndEnabled &&
                                      core.Phase != WeldPhase.Idle;

        private void Awake()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) lineMaterial = new Material(shader);
            arc = NewLine("WeldArc", 0.023f);
            for (int i = 0; i < sparks.Length; i++)
                sparks[i] = NewLine("WeldSpark_" + i, 0.012f);
            Clear();
        }

        private LineRenderer NewLine(string label, float width)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = 0.003f;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            if (lineMaterial != null) line.sharedMaterial = lineMaterial;
            line.enabled = false;
            return line;
        }

        public void BeginWeld(uint seed)
        {
            core.Begin(seed);
            Redraw();
        }

        public void SetVisualProgress(float progress)
        {
            core.SetProgress(progress);
            Redraw();
        }

        public void FinishWeld()
        {
            core.Finish();
            Redraw();
        }

        public void CancelWeld()
        {
            core.Cancel();
            Clear();
        }

        public void SetVisualsEnabled(bool enabled)
        {
            vfxEnabled = enabled;
            if (!enabled) CancelWeld();
        }

        private void Update()
        {
            if (!vfxEnabled || !isActiveAndEnabled) { Clear(); return; }
            core.Tick(Time.deltaTime);
            Redraw();
        }

        private void Redraw()
        {
            if (arc == null || !vfxEnabled || !isActiveAndEnabled)
            {
                Clear();
                return;
            }
            core.Sample(frame);
            arc.enabled = frame.Visible;
            if (frame.Visible)
            {
                arc.startColor = new Color(1f, 0.89f, 0.29f, frame.Heat);
                arc.endColor = new Color(0.20f, 0.78f, 1f, frame.Heat);
                arc.SetPosition(0, new Vector3(frame.ArcX, -0.02f, 0));
                arc.SetPosition(1, new Vector3(frame.ArcX, frame.ArcY + 0.16f, 0));
            }
            for (int i = 0; i < sparks.Length; i++)
            {
                var line = sparks[i];
                if (line == null) continue;
                var sample = frame.Sparks[i];
                line.enabled = frame.Visible && sample.Intensity > 0.02f;
                if (!line.enabled) continue;
                line.startColor = new Color(1f, 0.73f, 0.28f, sample.Intensity);
                line.endColor = new Color(1f, 0.21f, 0.04f, 0f);
                line.SetPosition(0, new Vector3(sample.X0, sample.Y0, 0));
                line.SetPosition(1, new Vector3(sample.X1, sample.Y1, 0));
            }
        }

        private void Clear()
        {
            if (arc != null) arc.enabled = false;
            for (int i = 0; i < sparks.Length; i++)
                if (sparks[i] != null) sparks[i].enabled = false;
        }

        private void OnDisable()
        {
            core.Cancel();
            Clear();
        }

        private void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
