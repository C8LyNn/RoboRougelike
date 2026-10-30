using UnityEngine;

namespace RoboRougelike.B0132P17.ScanSweep
{
    /// <summary>
    /// Visual-only original URP scanner mesh. No Raycast, AI, Physics, Collider, rewards,
    /// or authoritative world scanning. Another owner may opt in to StartVisualScan.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0132ScanSweepVisual : MonoBehaviour
    {
        [SerializeField] private bool visualsEnabled = true;
        private readonly ScanSweepCore core = new ScanSweepCore();
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material material;
        private readonly Vector3[] positions = new Vector3[ScanSweepCore.VertexCount];
        private readonly Color[] colors = new Color[ScanSweepCore.VertexCount];

        public ScanState State => core.State;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            ScanVertex[] v = new ScanVertex[ScanSweepCore.VertexCount];
            int[] triangles = new int[ScanSweepCore.IndexCount];
            ScanSweepCore.BuildSector(v, triangles);
            for (int i = 0; i < v.Length; i++)
            {
                positions[i] = new Vector3(v[i].X, v[i].Y, v[i].Z);
                colors[i] = new Color(0.18f, 0.85f, 1f, v[i].Alpha);
            }
            mesh = new Mesh { name = "B0132_Original_ScanSweepSector" };
            mesh.vertices = positions;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
            var shader = Shader.Find("B0132/ScanSweepTransparentURP");
            if (shader != null)
            {
                material = new Material(shader);
                meshRenderer.sharedMaterial = material;
            }
            core.SetVisualsEnabled(visualsEnabled);
            Redraw();
        }

        public bool StartVisualScan(float headingRadians = 0f)
        {
            bool started = core.Start(headingRadians);
            Redraw();
            return started;
        }
        public void SetVisualsEnabled(bool enabled)
        {
            visualsEnabled = enabled;
            core.SetVisualsEnabled(enabled);
            Redraw();
        }
        public void CancelVisualScan()
        {
            core.Cancel();
            Redraw();
        }
        private void Update()
        {
            core.Advance(Time.deltaTime);
            Redraw();
        }
        private void Redraw()
        {
            var pose = core.Sample();
            if (meshRenderer != null) meshRenderer.enabled = pose.Visible && material != null;
            if (material != null) material.SetFloat("_VisualOpacity", pose.Opacity);
            transform.localRotation = Quaternion.AngleAxis(
                pose.AngleRadians * Mathf.Rad2Deg, Vector3.up);
        }
        private void OnDisable()
        {
            core.Cancel();
            Redraw();
        }
        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (mesh != null) Destroy(mesh);
        }
    }
}
