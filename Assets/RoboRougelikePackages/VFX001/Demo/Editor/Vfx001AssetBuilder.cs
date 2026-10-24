#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RoboRougelike.VFX001.Editor
{
    public static class Vfx001AssetBuilder
    {
        private const string Generated = "Assets/RoboRougelikePackages/VFX001/Resources";
        [MenuItem("Tools/RoboRougelike/VFX001/Create URP Materials")]
        public static void CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(Generated))
                AssetDatabase.CreateFolder("Assets/RoboRougelikePackages/VFX001", "Resources");
            Make("VFX001_Particle", "RoboRougelike/VFX/ProceduralParticleURP");
            Make("VFX001_Flame", "RoboRougelike/VFX/ProceduralFlameURP");
            Make("VFX001_Shockwave", "RoboRougelike/VFX/ShockwaveURP");
            Make("VFX001_SurfaceMark", "RoboRougelike/VFX/SurfaceMarkURP");
            Make("VFX001_ArmorScar", "RoboRougelike/VFX/ArmorScarURP");
            Make("VFX001_HeatGlow", "RoboRougelike/VFX/HeatGlowURP");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        private static void Make(string name, string shaderName)
        {
            string path = Generated + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Shader shader = Shader.Find(shaderName);
            if (shader == null) { Debug.LogError("VFX001: missing shader " + shaderName); return; }
            AssetDatabase.CreateAsset(new Material(shader), path);
            Debug.Log("VFX001: " + path);
        }
    }
}
#endif
