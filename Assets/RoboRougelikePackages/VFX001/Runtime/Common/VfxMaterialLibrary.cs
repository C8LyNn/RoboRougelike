using UnityEngine;
using UnityEngine.Rendering;

namespace RoboRougelike.VFX001
{
    /// <summary>Runtime-only texture and materials. No external textures required.</summary>
    public static class VfxMaterialLibrary
    {
        private static Material _particle;
        private static Material _flame;
        private static Material _shockwave;
        private static Material _surfaceMark;
        private static Material _armorScar;
        private static Material _heatGlow;
        private static Texture2D _radial;
        public static Material Particle
        {
            get
            {
                if (_particle != null) return _particle;
                Material asset = Resources.Load<Material>("VFX001_Particle");
                Shader shader = asset != null ? asset.shader : Shader.Find("RoboRougelike/VFX/ProceduralParticleURP");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) { Debug.LogWarning("VFX-001: no compatible particle shader found."); return null; }
                _particle = asset != null ? new Material(asset) : new Material(shader);
                _particle.name = "VFX001 Procedural Particle (Runtime)";
                _particle.hideFlags = HideFlags.DontSave;
                if (_particle.HasProperty("_Surface")) _particle.SetFloat("_Surface", 1f);
                if (_particle.HasProperty("_Blend")) _particle.SetFloat("_Blend", 0f);
                if (_particle.HasProperty("_SrcBlend")) _particle.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (_particle.HasProperty("_DstBlend")) _particle.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (_particle.HasProperty("_ZWrite")) _particle.SetFloat("_ZWrite", 0f);
                _particle.SetOverrideTag("RenderType", "Transparent");
                _particle.renderQueue = (int)RenderQueue.Transparent;
                if (_particle.HasProperty("_BaseMap")) _particle.SetTexture("_BaseMap", Radial);
                if (_particle.HasProperty("_MainTex")) _particle.SetTexture("_MainTex", Radial);
                if (_particle.HasProperty("_BaseColor")) _particle.SetColor("_BaseColor", Color.white);
                if (_particle.HasProperty("_Color")) _particle.SetColor("_Color", Color.white);
                return _particle;
            }
        }
        public static Material Flame
        {
            get
            {
                if (_flame != null) return _flame;
                Material asset = Resources.Load<Material>("VFX001_Flame");
                Shader shader = asset != null ? asset.shader : Shader.Find("RoboRougelike/VFX/ProceduralFlameURP");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) return Particle;
                _flame = asset != null ? new Material(asset) : new Material(shader);
                _flame.name = "VFX001 Flame (Runtime)";
                _flame.hideFlags = HideFlags.DontSave;
                if (_flame.HasProperty("_BaseColor")) _flame.SetColor("_BaseColor", new Color(1f, .56f, .1f, .7f));
                return _flame;
            }
        }
        public static Material Shockwave
        {
            get
            {
                if (_shockwave != null) return _shockwave;
                var asset = Resources.Load<Material>("VFX001_Shockwave");
                var shader = asset != null ? asset.shader : Shader.Find("RoboRougelike/VFX/ShockwaveURP");
                if (shader == null) { Debug.LogWarning("VFX001: missing ShockwaveURP material. Generate assets before build."); return null; }
                _shockwave = asset != null ? new Material(asset) : new Material(shader);
                _shockwave.name = "VFX001 Shockwave (Runtime)";
                _shockwave.hideFlags = HideFlags.DontSave;
                return _shockwave;
            }
        }
        public static Material SurfaceMark
        {
            get
            {
                if (_surfaceMark != null) return _surfaceMark;
                var asset = Resources.Load<Material>("VFX001_SurfaceMark");
                var shader = asset != null ? asset.shader : Shader.Find("RoboRougelike/VFX/SurfaceMarkURP");
                if (shader == null) { Debug.LogWarning("VFX001: missing SurfaceMarkURP material. Generate assets before build."); return null; }
                _surfaceMark = asset != null ? new Material(asset) : new Material(shader);
                _surfaceMark.name = "VFX001 Surface Mark (Runtime)";
                _surfaceMark.hideFlags = HideFlags.DontSave;
                _surfaceMark.SetTexture("_MainTex", Texture2D.whiteTexture);
                return _surfaceMark;
            }
        }
        public static Material ArmorScar
        {
            get
            {
                if (_armorScar != null) return _armorScar;
                var asset = Resources.Load<Material>("VFX001_ArmorScar");
                var shader = asset != null ? asset.shader : Shader.Find("RoboRougelike/VFX/ArmorScarURP");
                if (shader == null)
                {
                    Debug.LogWarning("VFX001: missing ArmorScarURP. Create URP materials before standalone build.");
                    return null;
                }
                _armorScar = asset != null ? new Material(asset) : new Material(shader);
                _armorScar.name = "VFX001 Armor Scar (Runtime)";
                _armorScar.hideFlags = HideFlags.DontSave;
                return _armorScar;
            }
        }
        public static Material HeatGlow
        {
            get
            {
                if (_heatGlow != null) return _heatGlow;
                var asset = Resources.Load<Material>("VFX001_HeatGlow");
                var shader = asset != null ? asset.shader : Shader.Find("RoboRougelike/VFX/HeatGlowURP");
                if (shader == null)
                {
                    Debug.LogWarning("VFX001: missing HeatGlowURP. Create URP materials before standalone build.");
                    return null;
                }
                _heatGlow = asset != null ? new Material(asset) : new Material(shader);
                _heatGlow.name = "VFX001 Heat Glow (Runtime)";
                _heatGlow.hideFlags = HideFlags.DontSave;
                return _heatGlow;
            }
        }
        private static Texture2D Radial
        {
            get
            {
                if (_radial != null) return _radial;
                const int size = 32;
                _radial = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
                { name = "VFX001 Radial Alpha (Runtime)", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float dx = ((x + .5f) / size - .5f) * 2f;
                    float dy = ((y + .5f) / size - .5f) * 2f;
                    float a = Mathf.Clamp01(1f - dx * dx - dy * dy);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
                _radial.SetPixels(pixels);
                _radial.Apply(false, true);
                return _radial;
            }
        }
    }
}
