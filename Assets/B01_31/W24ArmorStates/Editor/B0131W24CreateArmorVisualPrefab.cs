#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoboRougelike.B0131.Art.Editor
{
    // Explicit opt-in. Never auto-executes, never edits canonical Assets/Pack or game state.
    public static class B0131W24CreateArmorVisualPrefab
    {
        private const string Root = "Assets/B01_31/W24ArmorStates";
        private const string Models = Root + "/Models";
        private const string Output = Root + "/Generated/B0131_W24_ArmorVisual_ONLY.prefab";
        private static readonly string[] States = { "OK", "DAMAGED", "DESTROYED" };
        private static readonly string[] ExpectedSha256 =
        {
            "bb5cef04d44497d2f2bb27c7f0bacc4acda8990e504200282352c3245c17c5d3",
            "6575a6eea08b68951b6ef57c86aaf9ccfdb45a0abc3ca5a229901877c1d171f7",
            "32950c0a822b9f063b0f8e4792fb31dc114463e6bc53d2a1daad46ae909d34f5"
        };

        [MenuItem("Tools/B01-31/W24/Create Isolated Armor Visual Prefab...")]
        public static void CreateWithExplicitConsent()
        {
            if (!EditorUtility.DisplayDialog("B01-31 — W24 isolated visual prefab",
                    "Creates only B0131_W24_ArmorVisual_ONLY.prefab inside the B01-31 Generated folder. " +
                    "Requires the three exact W23 OBJ files. No gameplay components, no Save/Load edits, " +
                    "no overwritten prefab. Continue?", "Create if safe", "Cancel"))
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Output) != null ||
                File.Exists(Output) || File.Exists(Output + ".meta"))
            {
                Debug.LogError("[B01-31 W24] Existing output/foreign prefab. Refusing overwrite: " + Output);
                return;
            }

            var imported = new GameObject[3];
            for (int i = 0; i < States.Length; ++i)
            {
                var path = Models + "/B0131ArmorModule1x2x2-" + States[i] + "-W23.obj";
                if (!File.Exists(path) || !MatchesSha256(path, ExpectedSha256[i]))
                {
                    Debug.LogError("[B01-31 W24] Original immutable OBJ missing or SHA mismatched: " + path);
                    return;
                }
                imported[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (imported[i] == null)
                {
                    Debug.LogError("[B01-31 W24] Native Unity ModelImporter did not return GameObject: " + path);
                    return;
                }
            }

            if (!AssetDatabase.IsValidFolder(Root))
            {
                Debug.LogError("[B01-31 W24] Package root not present; no workspace mutations performed.");
                return;
            }
            if (!AssetDatabase.IsValidFolder(Root + "/Generated"))
                AssetDatabase.CreateFolder(Root, "Generated");

            var parent = new GameObject("B0131_W24_ArmorVisual_ONLY");
            try
            {
                var views = new GameObject[3];
                for (int i = 0; i < 3; ++i)
                {
                    views[i] = (GameObject)Object.Instantiate(imported[i]);
                    views[i].name = States[i] + "_VISUAL_ONLY";
                    views[i].transform.SetParent(parent.transform, false);
                    views[i].transform.localPosition = Vector3.zero;
                    views[i].transform.localRotation = Quaternion.identity;
                    views[i].transform.localScale = Vector3.one;
                }

                var visual = parent.AddComponent<B0131W24ArmorDamageVisual>();
                if (!visual.ConfigureVisuals(views[0], views[1], views[2]) ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(Output) != null)
                    throw new InvalidOperationException("Invalid visual references or output collision");
                var prefab = PrefabUtility.SaveAsPrefabAsset(parent, Output);
                if (prefab == null)
                    throw new InvalidOperationException("Unity returned null Prefab on save");
                Debug.Log("[B01-31 W24] Isolated VISUAL ONLY prefab created: " + Output);
            }
            catch (Exception ex)
            {
                Debug.LogError("[B01-31 W24] No gameplay touched. Prefab operation failed: " + ex);
                // Never delete user/foreign files, including partially written output; human review.
                throw;
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static bool MatchesSha256(string assetPath, string expected)
        {
            using (var stream = File.OpenRead(assetPath))
            using (var hasher = SHA256.Create())
            {
                var hex = BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                return string.Equals(hex, expected, StringComparison.Ordinal);
            }
        }
    }
}
#endif
