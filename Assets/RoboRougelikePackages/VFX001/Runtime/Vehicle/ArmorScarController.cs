using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>
    /// Bounded, cosmetic scorch decals made from world-aligned quads parented to ONE visual part.
    /// This script does not modify colliders, source meshes, gameplay damage or physics.
    /// A slight offset avoids z-fighting; placement assumes impact positions on the visible surface.
    /// </summary>
    public sealed class ArmorScarController : MonoBehaviour
    {
        [SerializeField] private Transform visualAnchor;
        [SerializeField, Range(1, 16)] private int maxScars = 5;
        [SerializeField, Min(.0005f)] private float surfaceOffset = .008f;
        private readonly List<MeshRenderer> _marks = new List<MeshRenderer>();
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private int _cursor;
        private static Mesh _quad;

        public int MarkCount => _marks.Count;
        public int Capacity => Mathf.Max(1, maxScars);

        private static Mesh Quad
        {
            get
            {
                if (_quad != null) return _quad;
                var m = new Mesh { name = "VFX001 Shared Scar Quad", hideFlags = HideFlags.DontSave };
                m.vertices = new[] { new Vector3(-.5f,-.5f,0), new Vector3(.5f,-.5f,0), new Vector3(-.5f,.5f,0), new Vector3(.5f,.5f,0) };
                m.uv = new[] { new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1) };
                m.triangles = new[] { 0,2,1, 2,3,1 };
                m.RecalculateNormals();
                m.RecalculateBounds();
                _quad=m;
                return m;
            }
        }

        public bool AddScar(Vector3 worldPoint, Vector3 worldNormal, ImpactMaterialType material, float intensity01, int visualSeed = 0)
        {
            if (worldNormal.sqrMagnitude < .00001f || !IsFinite(worldPoint) || !IsFinite(worldNormal)) return false;
            Material scarMaterial = VfxMaterialLibrary.ArmorScar;
            if (scarMaterial == null) return false;
            Transform root = visualAnchor != null ? visualAnchor : transform;
            MeshRenderer mark;
            if (_marks.Count < Capacity)
            {
                var go = new GameObject("VFX001 Armor Scorch (visual-only)", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                go.GetComponent<MeshFilter>().sharedMesh = Quad;
                mark = go.GetComponent<MeshRenderer>();
                mark.sharedMaterial = scarMaterial;
                mark.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mark.receiveShadows = false;
                _marks.Add(mark);
            }
            else
            {
                mark = _marks[_cursor];
                _cursor = (_cursor + 1) % Capacity;
            }
            if (mark == null) return false;
            var spec = VfxScarResolver.Resolve(material, intensity01);
            Vector3 normal = worldNormal.normalized;
            mark.transform.position = worldPoint + normal * surfaceOffset;
            mark.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
            mark.transform.localScale = new Vector3(spec.Diameter, spec.Diameter, 1f);
            _properties.Clear();
            var tint = spec.Tint;
            tint.a = spec.Opacity;
            _properties.SetColor("_BaseColor", tint);
            _properties.SetFloat("_CrackStrength", spec.CrackStrength);
            _properties.SetFloat("_Seed", (visualSeed & 1023) * .013f);
            mark.SetPropertyBlock(_properties); // Only small overlay renderers for batching safety.
            return true;
        }

        public void ClearScars()
        {
            foreach (var mark in _marks)
                if (mark != null)
                {
                    if (Application.isPlaying) Destroy(mark.gameObject);
                    else DestroyImmediate(mark.gameObject);
                }
            _marks.Clear();
            _cursor = 0;
        }

        private static bool IsFinite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
                   !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }
        private void OnDestroy() { ClearScars(); }
    }
}
