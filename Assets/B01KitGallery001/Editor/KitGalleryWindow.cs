#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace B01KitGallery001
{
    [Serializable] public class KitLine { public string kind, weapon; public int count, unit_scrap; }
    [Serializable] public class KitOffer {
        public string id, title, role, description;
        public int price_scrap, pieces;
        public KitLine[] parts;
    }
    [Serializable] public class KitCatalog {
        public string schema, source_sha, source_package;
        public bool reference_only;
        public KitOffer[] offers;
    }
    public static class KitGalleryRules
    {
        public const string SourceSha = "0d212a706c1e633b773d2e03b7b40c28bb63521b";
        public static int Validate(KitCatalog d)
        {
            if (d == null || d.schema != "b01-22-unity-gallery-v1" ||
                d.source_sha != SourceSha || d.source_package != "B01-22-EQUIPMENT-KITS-001" ||
                !d.reference_only || d.offers == null || d.offers.Length != 10)
                throw new InvalidOperationException("INVALID_SOURCE_OR_SCHEMA");
            var seen = new HashSet<string>();
            for (int i = 0; i < d.offers.Length; i++)
            {
                KitOffer k = d.offers[i];
                if (k == null || k.id != "EK" + (i+1).ToString("D2") || !seen.Add(k.id) ||
                    String.IsNullOrEmpty(k.title) || String.IsNullOrEmpty(k.role) ||
                    k.parts == null || k.parts.Length == 0) throw new InvalidOperationException("INVALID_KIT");
                var types = new HashSet<string>();
                var rows = new HashSet<string>();
                int pieces = 0;
                long price = 0;
                foreach (var p in k.parts)
                {
                    if (p == null || p.kind == "Core" || String.IsNullOrEmpty(p.kind) ||
                        p.count < 1 || p.count > 8 || p.unit_scrap < 0)
                        throw new InvalidOperationException("INVALID_PART");
                    if (p.kind == "Gun")
                    {
                        if (String.IsNullOrEmpty(p.weapon)) throw new InvalidOperationException("GUN_TYPE_MISSING");
                        types.Add(p.weapon);
                    }
                    else if (!String.IsNullOrEmpty(p.weapon)) throw new InvalidOperationException("WEAPON_ON_NONGUN");
                    if (!rows.Add(p.kind + ":" + p.weapon)) throw new InvalidOperationException("DUPLICATE_PART");
                    pieces += p.count; price += (long)p.unit_scrap * p.count;
                }
                if (types.Count > 2 || pieces > 12 || pieces != k.pieces ||
                    price != k.price_scrap || price > Int32.MaxValue)
                    throw new InvalidOperationException("TOTAL_OR_FAMILY_MISMATCH");
            }
            return d.offers.Length;
        }
        public static bool Matches(KitOffer k, string term)
        {
            if (String.IsNullOrWhiteSpace(term)) return true;
            return (k.id + " " + k.title + " " + k.role + " " + k.description)
                .IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public static string ToTsv(KitOffer kit)
        {
            var rows = new System.Text.StringBuilder("Kind\tWeapon\tCount\tUnitScrap\n");
            foreach (var p in kit.parts) rows.Append(p.kind).Append('\t').Append(p.weapon)
                .Append('\t').Append(p.count).Append('\t').Append(p.unit_scrap).Append('\n');
            return rows.ToString();
        }
    }

    public sealed class KitGalleryWindow : EditorWindow
    {
        public const string AssetPath = "Assets/B01KitGallery001/Data/KitGallery.json";
        private KitCatalog catalog;
        private string error, search = "";
        private int selected;
        private Vector2 scroll;

        [MenuItem("B01/Equipment/Kit Gallery (Read Only)")]
        public static void Open()
        {
            KitGalleryWindow window = GetWindow<KitGalleryWindow>("Equipment Kit Gallery");
            window.minSize = new Vector2(650, 450);
            window.Reload();
        }
        private void OnEnable() { Reload(); }
        private void Reload()
        {
            catalog = null;
            error = null;
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetPath);
            if (asset == null) { error = "Missing KitGallery.json"; return; }
            try {
                var loaded = JsonUtility.FromJson<KitCatalog>(asset.text);
                KitGalleryRules.Validate(loaded);
                catalog = loaded;
                selected = 0;
            } catch (Exception exception) { error = exception.Message; }
            Repaint();
        }
        private void OnGUI()
        {
            GUILayout.Label("B01-22 / Equipment Gallery", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Editor-only source snapshot. NO purchases, mounting, save changes or reward grants.", MessageType.Info);
            if (GUILayout.Button("Reload and validate")) Reload();
            if (catalog == null) { EditorGUILayout.HelpBox(error ?? "No data", MessageType.Error); return; }
            search = EditorGUILayout.TextField("Search kit / role", search);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < catalog.offers.Length; i++) {
                KitOffer offer = catalog.offers[i];
                if (!KitGalleryRules.Matches(offer, search)) continue;
                if (GUILayout.Button(offer.id + " | " + offer.title + " | " + offer.price_scrap + " scrap")) selected = i;
            }
            EditorGUILayout.EndScrollView();
            KitOffer active = catalog.offers[selected];
            GUILayout.Label(active.title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(active.role + " | " + active.pieces + " pieces | " + active.price_scrap + " scrap");
            EditorGUILayout.LabelField(active.description);
            foreach (var line in active.parts)
                EditorGUILayout.LabelField(line.count + "x " + line.kind +
                  (line.kind == "Gun" ? " (" + line.weapon + ")" : "") + " = " +
                  (line.count * line.unit_scrap) + " scrap");
            if (GUILayout.Button("Copy part list (TSV)"))
                EditorGUIUtility.systemCopyBuffer = KitGalleryRules.ToTsv(active);
            EditorGUILayout.LabelField("Pinned source SHA: " + catalog.source_sha);
        }
    }
}
#endif
