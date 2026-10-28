// Tests ORIGINAL Unity Editor receiver C# against minimal explicit API shims.
// This is NOT a licensed Unity Editor execution.
using System;
using System.Collections.Generic;
using System.Linq;
using B0130.MaterialImport;
using UnityEngine;
using UnityEditor;

namespace UnityEngine
{
    public class Texture2D
    {
        public string path;
        public Texture2D(string p) { path = p; }
    }
    public class Shader
    {
        public string name;
        public static bool Missing;
        public static Shader Find(string n)
        {
            return Missing ? null : new Shader { name = n };
        }
    }
    public class Material
    {
        public Shader shader;
        public string name;
        public Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public Dictionary<string, float> floats = new Dictionary<string, float>();
        public HashSet<string> keywords = new HashSet<string>();
        public Material(Shader s) { shader = s; }
        public void SetColor(string property, Color c) {}
        public void SetTexture(string property, Texture2D value) { textures[property] = value; }
        public void SetFloat(string property, float value) { floats[property] = value; }
        public void EnableKeyword(string keyword) { keywords.Add(keyword); }
    }
    public struct Color
    {
        public static Color white { get { return new Color(); } }
    }
    public enum TextureWrapMode { Repeat, Clamp }
    public static class Debug
    {
        public static List<string> Errors = new List<string>();
        public static void LogError(string s) { Errors.Add(s); }
        public static void Log(string s) {}
    }
}

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute
    {
        public MenuItem(string path) {}
    }
    public enum TextureImporterType { Default, NormalMap }
    public class AssetImporter
    {
        public static Dictionary<string, TextureImporter> Importers = new Dictionary<string, TextureImporter>();
        public static AssetImporter GetAtPath(string path)
        {
            return Importers.ContainsKey(path) ? Importers[path] : null;
        }
    }
    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType = TextureImporterType.Default;
        public bool sRGBTexture = true;
        public bool mipmapEnabled = false;
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        public static int ReimportCount;
        public void SaveAndReimport() { ReimportCount++; }
    }
    public static class AssetDatabase
    {
        public static Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public static int SaveCount;
        public static int CreateAssetCount;
        public static bool FolderCreated;
        public static T LoadAssetAtPath<T>(string path) where T : class
        {
            if (typeof(T) == typeof(Material))
                return Materials.ContainsKey(path) ? Materials[path] as T : null;
            if (typeof(T) == typeof(Texture2D))
                return Textures.ContainsKey(path) ? Textures[path] as T : null;
            return null;
        }
        public static bool IsValidFolder(string p) { return FolderCreated; }
        public static void CreateFolder(string parent, string child)
        {
            if (parent != "Assets/B0130MATPack01" || child != "Materials")
                throw new Exception("Unapproved material directory");
            FolderCreated = true;
        }
        public static void CreateAsset(Material asset, string path)
        {
            if (!path.StartsWith("Assets/B0130MATPack01/Materials/"))
                throw new Exception("Installer mutated another owner's files");
            if (Materials.ContainsKey(path))
                throw new Exception("Replaced pre-existing material instead of idempotent update");
            Materials[path] = asset;
            CreateAssetCount++;
        }
        public static void SaveAssets() { SaveCount++; }
    }
    public static class EditorUtility
    {
        public static int DirtyCount;
        public static void SetDirty(Material material) { DirtyCount++; }
    }
}

public static class TestInstaller
{
    const string Root = "Assets/B0130MATPack01";
    static string[] Families = { "tire_rubber", "brushed_arm_alloy" };
    static string[] Kinds = { "basecolor", "normal", "metallic_smoothness", "occlusion" };
    static int Passed;

    static void Assert(bool actual, string message)
    {
        if (!actual) throw new Exception("TEST_FAILED: " + message);
        Passed++;
    }
    static void Reset()
    {
        AssetDatabase.Textures.Clear();
        AssetDatabase.Materials.Clear();
        AssetImporter.Importers.Clear();
        AssetDatabase.SaveCount = 0;
        AssetDatabase.CreateAssetCount = 0;
        AssetDatabase.FolderCreated = false;
        EditorUtility.DirtyCount = 0;
        TextureImporter.ReimportCount = 0;
        Debug.Errors.Clear();
        Shader.Missing = false;
    }
    static string PathFor(string family, string kind)
    {
        return Root + "/Textures/" + family + "/" + family + "_" + kind + ".png";
    }
    static void Inputs()
    {
        foreach (string f in Families)
        foreach (string k in Kinds)
        {
            string p = PathFor(f, k);
            AssetDatabase.Textures[p] = new Texture2D(p);
            AssetImporter.Importers[p] = new TextureImporter();
        }
    }
    static void VerifyValid()
    {
        Reset(); Inputs(); MatA1Installer.Install();
        Assert(Debug.Errors.Count == 0,"valid original 8 PNG inputs");
        Assert(AssetDatabase.Materials.Count == 2, "two material assets created");
        Assert(AssetDatabase.CreateAssetCount == 2,"only 2 material assets written");
        Assert(TextureImporter.ReimportCount >= 6,"all 8 PNGs evaluated for settings and corrected");
        foreach (string f in Families)
        {
            var mat = AssetDatabase.Materials[Root + "/Materials/" + f + ".mat"];
            Assert(mat.shader.name == "Universal Render Pipeline/Lit", "real URP shader resolved");
            Assert(mat.floats["_SmoothnessTextureChannel"] == 0f,"metallic alpha smoothness channel");
            Assert(mat.floats["_Metallic"] == 1f,"metallic map multipler set");
            Assert(mat.keywords.Contains("_NORMALMAP") && mat.keywords.Contains("_METALLICSPECGLOSSMAP"),"normal and metallic keywords");
            foreach (string map in Kinds)
            {
                string key = map == "basecolor" ? "_BaseMap" : map == "normal" ? "_BumpMap" :
                             map == "metallic_smoothness" ? "_MetallicGlossMap" : "_OcclusionMap";
                Assert(mat.textures[key].path == PathFor(f,map),"original PNG material bind " + f + "/" + map);
                TextureImporter imp = AssetImporter.Importers[PathFor(f,map)];
                Assert(imp.wrapMode == TextureWrapMode.Repeat && imp.mipmapEnabled,"repeat and mipmaps");
                Assert(imp.sRGBTexture == (map == "basecolor"),"correct linear/colour channel import");
                Assert(imp.textureType == (map == "normal" ? TextureImporterType.NormalMap : TextureImporterType.Default),"normal map texture type");
            }
        }
        int oldReimports = TextureImporter.ReimportCount;
        Material prior = AssetDatabase.Materials[Root + "/Materials/tire_rubber.mat"];
        MatA1Installer.Install();
        Assert(TextureImporter.ReimportCount == oldReimports,"second call import changes none");
        Assert(AssetDatabase.Materials[Root + "/Materials/tire_rubber.mat"] == prior,"idempotent material reference");
        Assert(AssetDatabase.CreateAssetCount == 2,"second call creates no duplicate assets");
    }
    static void VerifyFailurePaths()
    {
        Reset(); Inputs();
        AssetImporter.Importers.Remove(PathFor("tire_rubber","normal"));
        MatA1Installer.Install();
        Assert(AssetDatabase.Materials.Count == 0,"missing texture stops before partial create");
        Assert(Debug.Errors.Count == 1,"missing texture reports failure");
        Reset(); Inputs();
        Shader.Missing = true;
        MatA1Installer.Install();
        Assert(AssetDatabase.Materials.Count == 0,"missing shader stops without writing");
        Assert(Debug.Errors.Count == 1,"missing URP shader reports failure");
    }
    public static int Main()
    {
        VerifyValid(); VerifyFailurePaths();
        Console.WriteLine("B01_30_MAT_IMPORT_CSHARP_ORIGINAL_SOURCE_STUBS_PASS " + Passed + " checks");
        return 0;
    }
}
