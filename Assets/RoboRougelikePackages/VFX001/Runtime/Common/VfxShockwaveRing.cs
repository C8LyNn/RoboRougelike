using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Optional purely graphical expanding ring, pooled with a VfxBurstActor.</summary>
    public sealed class VfxShockwaveRing : MonoBehaviour
    {
        private static Mesh _sharedMesh;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Color _color;
        private float _elapsed;
        private float _duration;
        private float _radius;
        private bool _running;
        public bool IsRunning => _running;

        public void Initialize()
        {
            if (_renderer != null) return;
            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = SharedRing;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = VfxMaterialLibrary.Shockwave;
            if (_renderer.sharedMaterial == null)
                Debug.LogWarning("VFX001: missing shockwave shader/material. Generate VFX001 materials before build.", this);
            _block = new MaterialPropertyBlock();
            _renderer.enabled = false;
        }

        public void Begin(float intensity01, bool core)
        {
            Initialize();
            _elapsed = 0f;
            _duration = core ? .65f : .38f;
            _radius = (core ? 5.5f : 2.25f) * Mathf.Lerp(.6f, 1.5f, Mathf.Clamp01(intensity01));
            _color = core ? new Color(1f, .38f, .15f, .55f) : new Color(1f, .72f, .3f, .4f);
            _running = true;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * .02f;
            _renderer.enabled = true;
            Tick(0f);
        }

        public void Tick(float deltaTime)
        {
            if (!_running) return;
            _elapsed += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(.001f, _duration));
            float radius = Mathf.Lerp(.02f, _radius, 1f - (1f - t) * (1f - t));
            transform.localScale = Vector3.one * radius;
            var c = _color;
            c.a *= (1f - t) * (1f - t);
            _block.SetColor("_Tint", c);
            _renderer.SetPropertyBlock(_block);
            if (t >= 1f) StopRing();
        }

        public void StopRing()
        {
            _running = false;
            if (_renderer != null) _renderer.enabled = false;
        }

        private static Mesh SharedRing
        {
            get
            {
                if (_sharedMesh != null) return _sharedMesh;
                const int segments = 48;
                var vertices = new Vector3[segments * 2];
                var uv = new Vector2[segments * 2];
                var triangles = new int[segments * 6];
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2f / segments;
                    Vector3 n = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    vertices[i * 2] = n * .72f;
                    vertices[i * 2 + 1] = n;
                    uv[i * 2] = new Vector2(0f, i / (float)segments);
                    uv[i * 2 + 1] = new Vector2(1f, i / (float)segments);
                    int next = ((i + 1) % segments) * 2;
                    int j = i * 6;
                    triangles[j] = i * 2;
                    triangles[j + 1] = next;
                    triangles[j + 2] = i * 2 + 1;
                    triangles[j + 3] = i * 2 + 1;
                    triangles[j + 4] = next;
                    triangles[j + 5] = next + 1;
                }
                _sharedMesh = new Mesh { name = "VFX001 Shared Radial Shockwave", hideFlags = HideFlags.DontSave };
                _sharedMesh.vertices = vertices;
                _sharedMesh.uv = uv;
                _sharedMesh.triangles = triangles;
                _sharedMesh.RecalculateBounds();
                return _sharedMesh;
            }
        }
    }
}
