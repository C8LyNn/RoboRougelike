using UnityEngine;

namespace RoboRougelike.B0132P17.DualRecoil
{
    /// <summary>
    /// Original dual-barrel animated visual with zero physical colliders.
    /// A real weapon owner may call PlayLeftShot/PlayRightShot after HQ integration.
    /// Never alters ammo, weapon cooldown, projectile or Rigidbody.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0132DualRecoilVisual : MonoBehaviour
    {
        [SerializeField] private bool visualsEnabled = true;
        private readonly DualRecoilCore core = new DualRecoilCore();
        private readonly Transform[] barrel = new Transform[2];
        private readonly Vector3[] rest = new Vector3[2];
        private Mesh createdMesh;
        private Material createdMaterial;

        public RecoilPose CurrentPose => core.Sample();
        public bool VisualsEnabled => core.VisualsEnabled;

        private void Awake()
        {
            // Pooled render-only meshes: no Unity primitive collider side effects.
            createdMesh = BuildBoxMesh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null) createdMaterial = new Material(shader);
            for (int i = 0; i < 2; i++)
            {
                var part = new GameObject(i == 0 ? "WeaponLeft_Barrel" : "WeaponRight_Barrel");
                part.transform.SetParent(transform, false);
                rest[i] = new Vector3(i == 0 ? -0.45f : 0.45f, 0f, 0f);
                part.transform.localPosition = rest[i];
                part.transform.localScale = new Vector3(0.28f, 0.28f, 1.35f);
                var filter = part.AddComponent<MeshFilter>();
                filter.sharedMesh = createdMesh;
                var visual = part.AddComponent<MeshRenderer>();
                if (createdMaterial != null) visual.sharedMaterial = createdMaterial;
                barrel[i] = part.transform;
            }
            core.SetVisualsEnabled(visualsEnabled);
            RenderPose();
        }

        public bool PlayLeftShot(float visualIntensity = 1f)
        {
            bool result = core.Fire(RecoilLane.Left, visualIntensity);
            RenderPose();
            return result;
        }

        public bool PlayRightShot(float visualIntensity = 1f)
        {
            bool result = core.Fire(RecoilLane.Right, visualIntensity);
            RenderPose();
            return result;
        }

        public void SetVisualsEnabled(bool enabled)
        {
            visualsEnabled = enabled;
            core.SetVisualsEnabled(enabled);
            RenderPose();
        }

        private void Update()
        {
            core.Advance(Time.deltaTime);
            RenderPose();
        }

        private void RenderPose()
        {
            var pose = core.Sample();
            if (barrel[0] != null)
                barrel[0].localPosition = rest[0] - Vector3.forward * pose.LeftTravel;
            if (barrel[1] != null)
                barrel[1].localPosition = rest[1] - Vector3.forward * pose.RightTravel;
        }

        private void OnDisable()
        {
            core.Clear(); // disabled objects must not retain invisible recoil
            RenderPose();
        }

        private void OnDestroy()
        {
            if (createdMesh != null) Destroy(createdMesh);
            if (createdMaterial != null) Destroy(createdMaterial);
        }

        private static Mesh BuildBoxMesh()
        {
            // 24 face-specific vertices and 36 triangles, Unity-compatible normals;
            // no new materials or third-party prefab/model dependencies.
            var v = new Vector3[24];
            var n = new Vector3[24];
            var t = new int[36];
            Vector3[] basis = {
                Vector3.forward, Vector3.back, Vector3.right,
                Vector3.left, Vector3.up, Vector3.down
            };
            for (int face = 0; face < 6; face++)
            {
                Vector3 forward = basis[face];
                Vector3 u = Vector3.Cross(forward, Vector3.up);
                if (u.sqrMagnitude < 0.01f) u = Vector3.right;
                u.Normalize();
                Vector3 w = Vector3.Cross(forward, u).normalized;
                int first = face * 4;
                v[first] = forward * 0.5f + (-u - w) * 0.5f;
                v[first + 1] = forward * 0.5f + (u - w) * 0.5f;
                v[first + 2] = forward * 0.5f + (u + w) * 0.5f;
                v[first + 3] = forward * 0.5f + (-u + w) * 0.5f;
                for (int j = 0; j < 4; j++) n[first + j] = forward;
                int k = face * 6;
                t[k] = first; t[k + 1] = first + 1; t[k + 2] = first + 2;
                t[k + 3] = first; t[k + 4] = first + 2; t[k + 5] = first + 3;
            }
            var mesh = new Mesh { name = "B0132_DualRecoilVisualOnlyBox" };
            mesh.vertices = v;
            mesh.normals = n;
            mesh.triangles = t;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
