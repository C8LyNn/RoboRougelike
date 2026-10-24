using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoboRougelike.VFX001
{
    /// <summary>
    /// Bounded transient glowing surface overlays. Independent from gameplay heat/damage.
    /// Intended for important impacts; projected quads assume a locally flat visible surface.
    /// </summary>
    public sealed class VfxHeatImpactController : MonoBehaviour
    {
        private sealed class Glow
        {
            public MeshRenderer Renderer;
            public float Remaining;
            public float Duration;
            public float Strength;
            public Color Tint;
        }
        [SerializeField] private Transform visualAnchor;
        [SerializeField, Range(1, 8)] private int maximumGlows = 3;
        [SerializeField, Min(.05f)] private float glowLifetime = .9f;
        [SerializeField, Min(.01f)] private float defaultDiameter = .42f;
        [SerializeField, Min(.001f)] private float surfaceOffset = .012f;
        private readonly List<Glow> _pool = new List<Glow>();
        private MaterialPropertyBlock _properties;
        private static Mesh _quad;
        private int _cursor;
        private int _live;
        public int CreatedOverlays => _pool.Count;
        public int ActiveOverlays => _live;
        public int Capacity => Mathf.Max(1, maximumGlows);

        private static Mesh Quad
        {
            get
            {
                if (_quad != null) return _quad;
                _quad = new Mesh { name = "VFX001 Shared Heat Quad", hideFlags = HideFlags.DontSave };
                _quad.vertices = new [] { new Vector3(-.5f,-.5f,0f),new Vector3(.5f,-.5f,0f),new Vector3(-.5f,.5f,0f),new Vector3(.5f,.5f,0f) };
                _quad.uv = new [] { Vector2.zero,Vector2.right,Vector2.up,Vector2.one };
                _quad.triangles = new [] { 0,2,1,2,3,1 };
                _quad.RecalculateNormals();
                _quad.RecalculateBounds();
                return _quad;
            }
        }

        public bool PlayHeat(Vector3 worldPosition, Vector3 worldNormal, float intensity01,
            ImpactMaterialType surfaceType = ImpactMaterialType.Metal)
        {
            if (!VfxImpulseEnvelope.Finite(worldPosition) || !VfxImpulseEnvelope.Finite(worldNormal) ||
                worldNormal.sqrMagnitude < .00001f || float.IsNaN(intensity01)) return false;
            var material = VfxMaterialLibrary.HeatGlow;
            if (material == null) return false;
            float strength = Mathf.Clamp01(intensity01);
            if (strength <= .001f) return false;
            var glow = GetOrCreate(material);
            if (glow == null) return false;
            var normal = worldNormal.normalized;
            var t = glow.Renderer.transform;
            t.position = worldPosition + normal * surfaceOffset;
            t.rotation = Quaternion.LookRotation(normal, Vector3.up);
            float size = defaultDiameter * Mathf.Lerp(.6f, 1.6f, strength);
            t.localScale = new Vector3(size, size, 1f);
            glow.Duration = Mathf.Max(.05f, glowLifetime);
            glow.Remaining = glow.Duration;
            glow.Strength = strength;
            glow.Tint = surfaceType == ImpactMaterialType.Energy ? new Color(.25f, .77f, 1f, .85f)
                : surfaceType == ImpactMaterialType.Core ? new Color(1f, .25f, .18f, .9f)
                : new Color(1f, .54f, .15f, .9f);
            if (!glow.Renderer.enabled) { glow.Renderer.enabled = true; _live++; }
            Apply(glow, strength);
            return true;
        }

        private Glow GetOrCreate(Material mat)
        {
            if (_pool.Count < Capacity)
            {
                var go = new GameObject("VFX001 Transient Heat", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(visualAnchor != null ? visualAnchor : transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = Quad;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enabled = false;
                var glow = new Glow { Renderer = renderer };
                _pool.Add(glow);
                return glow;
            }
            var item = _pool[_cursor];
            _cursor = (_cursor + 1) % _pool.Count;
            return item;
        }

        private void Update() { Tick(Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            for (int i=0;i<_pool.Count;i++)
            {
                var glow = _pool[i];
                if (glow.Renderer == null || !glow.Renderer.enabled) continue;
                glow.Remaining = Mathf.Max(0f, glow.Remaining - dt);
                if (glow.Remaining <= 0f)
                {
                    glow.Renderer.enabled = false;
                    _live = Mathf.Max(0, _live - 1);
                    continue;
                }
                float u = glow.Remaining / glow.Duration;
                Apply(glow, glow.Strength * u * u);
            }
        }

        private void Apply(Glow glow, float strength)
        {
            if (_properties == null) _properties = new MaterialPropertyBlock();
            _properties.Clear();
            _properties.SetColor("_Tint", glow.Tint);
            _properties.SetFloat("_Intensity", strength);
            glow.Renderer.SetPropertyBlock(_properties);
        }
        public void ClearHeat()
        {
            for (int i=0;i<_pool.Count;i++)
            {
                if (_pool[i].Renderer != null) _pool[i].Renderer.enabled = false;
                _pool[i].Remaining = 0f;
            }
            _live = 0;
        }
        private void OnDisable() { ClearHeat(); }
        private void OnDestroy()
        {
            // The child objects are destroyed automatically with this GameObject.
            _pool.Clear();
            _live = 0;
        }
    }
}
