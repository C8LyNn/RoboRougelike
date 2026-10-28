// MAT-IMPORT-001: standalone optional receiver for the source-pinned MAT-A1-001 PNGs.
// Editor-only, opt-in, isolated candidate folder. No canonical gameplay code.
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace B0130.MaterialImport
{
    public static class MatA1Installer
    {
        private const string Root = "Assets/B0130MATPack01";
        private const string ShaderName = "Universal Render Pipeline/Lit";
        private static readonly string[] Families = { "tire_rubber", "brushed_arm_alloy" };
        private static readonly string[] Maps = { "basecolor", "normal", "metallic_smoothness", "occlusion" };

        [MenuItem("Tools/B01/MAT-A1-001/Create candidate URP materials")]
        public static void Install()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("MAT-IMPORT-001: URP/Lit shader unavailable. No material written.");
                return;
            }

            // Preflight every input BEFORE creating or changing any materials.
            foreach (string family in Families)
            {
                foreach (string map in Maps)
                {
                    string source = PathFor(family, map);
                    if (!(AssetImporter.GetAtPath(source) is TextureImporter))
                    {
                        Debug.LogError("MAT-IMPORT-001: Missing importable PNG: " + source);
                        return;
                    }
                }
            }

            // Change texture import settings only for our own package assets.
            foreach (string family in Families)
            {
                foreach (string map in Maps)
                {
                    string source = PathFor(family, map);
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(source);
                    bool color = map == "basecolor";
                    bool normal = map == "normal";
                    TextureImporterType wantedType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    bool dirty =
                        importer.textureType != wantedType ||
                        importer.sRGBTexture != color ||
                        !importer.mipmapEnabled ||
                        importer.wrapMode != TextureWrapMode.Repeat;
                    if (!dirty)
                        continue;
                    importer.textureType = wantedType;
                    importer.sRGBTexture = color;
                    importer.mipmapEnabled = true;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.SaveAndReimport();
                }
            }

            string targetFolder = Root + "/Materials";
            if (!AssetDatabase.IsValidFolder(targetFolder))
                AssetDatabase.CreateFolder(Root, "Materials");

            foreach (string family in Families)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(targetFolder + "/" + family + ".mat");
                bool newlyCreated = material == null;
                if (newlyCreated)
                    material = new Material(shader) { name = "B0130_" + family };

                material.shader = shader;
                material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BaseMap", RequireTexture(family, "basecolor"));
                material.SetTexture("_BumpMap", RequireTexture(family, "normal"));
                material.SetTexture("_MetallicGlossMap", RequireTexture(family, "metallic_smoothness"));
                material.SetTexture("_OcclusionMap", RequireTexture(family, "occlusion"));
                material.SetFloat("_BumpScale", 1f);
                material.SetFloat("_Metallic", 1f); // R channel still encodes tire=0 and alloy=219/255.
                material.SetFloat("_Smoothness", 1f); // A channel controls smoothness.
                material.SetFloat("_SmoothnessTextureChannel", 0f); // metallic alpha, not albedo alpha.
                material.SetFloat("_OcclusionStrength", 1f);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.EnableKeyword("_OCCLUSIONMAP");

                if (newlyCreated)
                    AssetDatabase.CreateAsset(material, targetFolder + "/" + family + ".mat");
                else
                    EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MAT-IMPORT-001: Created/updated two isolated candidate URP Lit materials in " + targetFolder
                      + ". No existing project materials were modified.");
        }

        private static string PathFor(string family, string map)
        {
            return Root + "/Textures/" + family + "/" + family + "_" + map + ".png";
        }

        private static Texture2D RequireTexture(string family, string map)
        {
            string path = PathFor(family, map);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                throw new InvalidOperationException("MAT-IMPORT-001 import failed: " + path);
            return tex;
        }
    }
}
#endif
