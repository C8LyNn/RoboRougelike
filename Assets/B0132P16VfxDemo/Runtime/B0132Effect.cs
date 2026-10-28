using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.B0132P16
{
    /// <summary>
    /// Standalone, cosmetic-only visual asset. Never changes HP, damage, inventory,
    /// Rigidbody state, target acquisition or canonical VFX001 ownership.
    /// Drag any of the three supplied prefabs into an isolated scene to preview.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0132Effect : MonoBehaviour
    {
        public enum EffectKind { ProjectileHit = 0, PartDetached = 1, CriticalWarning = 2 }

        [SerializeField] private EffectKind effectKind;
        [SerializeField] private bool autoPlay = true;
        [SerializeField, Range(0.1f, 4f)] private float scale = 1f;

        private sealed class Piece
        {
            public Transform Transform;
            public Vector3 Start;
            public Vector3 Velocity;
            public Vector3 BaseScale;
            public float RotationSpeed;
        }

        private readonly List<Piece> pieces = new List<Piece>();
        private readonly List<Material> materials = new List<Material>();
        private GameObject ringRoot;
        private float age;
        private float duration;
        private bool playing;
        private bool started;
        private Vector3 normal = Vector3.up;

        public bool IsPlaying => playing;
        public int ActiveVisualCount => pieces.Count + (ringRoot != null ? 1 : 0);
        public EffectKind Kind => effectKind;

        private void Start()
        {
            if (autoPlay && !started) Play(effectKind, transform.position, Vector3.up);
        }

        public void Play(EffectKind kind, Vector3 position, Vector3 outward)
        {
            StopVisuals();
            started = true;
            effectKind = kind;
            transform.position = position;
            normal = outward.sqrMagnitude < 0.0001f ? Vector3.up : outward.normalized;
            age = 0f;
            duration = kind == EffectKind.ProjectileHit ? 0.55f :
                       kind == EffectKind.PartDetached ? 1.35f : 2.1f;
            playing = true;
            switch (kind)
            {
                case EffectKind.ProjectileHit: CreateHit(); break;
                case EffectKind.PartDetached: CreateDetached(); break;
                case EffectKind.CriticalWarning: CreateWarning(); break;
            }
        }

        private void CreateHit()
        {
            AddPiece("impact flash", PrimitiveType.Sphere, Vector3.zero,
                Vector3.zero, Vector3.one * 0.37f * scale,
                new Color(0.88f, 0.98f, 1f), 0f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var radial = new Vector3(Mathf.Cos(a), 0.25f + 0.35f * Mathf.Sin(a * 2f), Mathf.Sin(a));
                Vector3 direction = (radial + normal * 0.5f).normalized;
                AddPiece("impact spark " + i, PrimitiveType.Cube,
                    Vector3.zero, direction * (1.8f + (i % 3) * .38f) * scale,
                    Vector3.one * (.047f + (i % 2) * .026f) * scale,
                    i % 2 == 0 ? new Color(0.28f, .9f, 1f) : new Color(1f, .79f, .29f),
                    80f + i * 11f);
            }
        }

        private void CreateDetached()
        {
            AddPiece("damaged module", PrimitiveType.Cube, Vector3.zero,
                (normal * 2.0f + Vector3.up * 1.1f) * scale,
                Vector3.one * .67f * scale, new Color(.69f, .26f, .14f), 150f);
            AddPiece("damage flash", PrimitiveType.Sphere, Vector3.zero,
                Vector3.zero, Vector3.one * .3f * scale,
                new Color(1f, .42f, .13f), 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 velocity = (new Vector3(Mathf.Cos(a), .55f, Mathf.Sin(a))
                    + normal * .3f) * (1.2f + i * .09f) * scale;
                AddPiece("detached fragment " + i, PrimitiveType.Cube,
                    Vector3.zero, velocity, Vector3.one * .11f * scale,
                    new Color(.88f, .51f, .25f), 110f + i * 17f);
            }
        }

        private void CreateWarning()
        {
            AddPiece("critical warning core", PrimitiveType.Cube,
                Vector3.up * .43f * scale, Vector3.zero,
                new Vector3(.3f, .42f, .13f) * scale,
                new Color(1f, .14f, .08f), 0f);
            ringRoot = new GameObject("critical warning ring");
            ringRoot.transform.SetParent(transform, false);
            ringRoot.transform.localPosition = Vector3.up * .42f * scale;
            var line = ringRoot.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = .055f * scale;
            line.numCornerVertices = 2;
            line.material = MakeMaterial(new Color(1f, .29f, .08f));
            line.startColor = new Color(1f, .32f, .13f);
            line.endColor = line.startColor;
            for (int i = 0; i < 48; i++)
            {
                float a = Mathf.PI * 2f * i / 48f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * .7f,
                    Mathf.Sin(a) * .7f, 0f) * scale);
            }
        }

        private Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            materials.Add(material);
            return material;
        }

        private void AddPiece(string title, PrimitiveType primitive, Vector3 local,
            Vector3 velocity, Vector3 size, Color tint, float rotationSpeed)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = title;
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = local;
            obj.transform.localScale = size;
            var collider = obj.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = MakeMaterial(tint);
            pieces.Add(new Piece
            {
                Transform = obj.transform,
                Start = local,
                Velocity = velocity,
                BaseScale = size,
                RotationSpeed = rotationSpeed
            });
        }

        private void Update()
        {
            if (!playing) return;
            age += Time.deltaTime;
            float f = Mathf.Clamp01(age / duration);
            if (f >= 1f) { StopVisuals(); return; }

            float fade = Mathf.Max(.025f, 1f - f);
            foreach (var piece in pieces)
            {
                if (piece.Transform == null) continue;
                float travel = age;
                if (effectKind == EffectKind.PartDetached)
                    piece.Transform.localPosition = piece.Start + piece.Velocity * travel
                        + Vector3.down * (1.4f * travel * travel);
                else
                    piece.Transform.localPosition = piece.Start + piece.Velocity * travel;
                piece.Transform.localScale = piece.BaseScale * fade;
                if (piece.RotationSpeed > 0f)
                    piece.Transform.Rotate(Vector3.one, piece.RotationSpeed * Time.deltaTime);
            }
            if (effectKind == EffectKind.CriticalWarning)
            {
                float pulse = 1f + .12f * Mathf.Sin(age * 13f);
                if (ringRoot != null) ringRoot.transform.localScale = Vector3.one * pulse;
                if (pieces.Count > 0 && pieces[0].Transform != null)
                    pieces[0].Transform.localScale = pieces[0].BaseScale * pulse * fade;
            }
        }

        public void StopVisuals()
        {
            playing = false;
            foreach (var p in pieces)
            {
                if (p.Transform == null) continue;
                p.Transform.gameObject.SetActive(false);
                Destroy(p.Transform.gameObject);
            }
            pieces.Clear();
            if (ringRoot != null) { ringRoot.SetActive(false); Destroy(ringRoot); ringRoot = null; }
            foreach (var material in materials)
                if (material != null) Destroy(material);
            materials.Clear();
        }

        private void OnDisable() => StopVisuals();
        private void OnDestroy() => StopVisuals();
    }
}
