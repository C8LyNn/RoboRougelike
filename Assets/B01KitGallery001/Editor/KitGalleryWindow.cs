#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        public const string SourceSha = "b1d4e341d846c9397e236809a1d7e4c4fff9c935";
        // Fingerprint of all 207 semantic fields in accepted-source #411:
        // schema/provenance, all 10 kit IDs/names/descriptions, part order/counts,
        // reference unit scrap and computed totals. Length prefixes avoid ambiguity.
        // This is NOT a cryptographic authorization or a live wallet transaction.
        private const string FrozenCatalogDigest = "d748152edbc0c3837f870c9b8bdb3fb9e4b8dab147633d7abe1028b19356450a";
        private static void AppendToken(StringBuilder buffer, string value)
        {
            string safe = value ?? String.Empty;
            buffer.Append(safe.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(safe);
        }
        private static void AppendNumber(StringBuilder buffer, int value)
        {
            AppendToken(buffer, value.ToString(CultureInfo.InvariantCulture));
        }
        public static string ComputeCatalogDigest(KitCatalog d)
        {
            if (d == null || d.offers == null)
                throw new InvalidOperationException("MISSING_CATALOG_FOR_DIGEST");
            var buffer = new StringBuilder();
            AppendToken(buffer, d.schema);
            AppendToken(buffer, d.source_sha);
            AppendToken(buffer, d.source_package);
            AppendToken(buffer, d.reference_only ? "1" : "0");
            AppendNumber(buffer, d.offers.Length);
            foreach (KitOffer offer in d.offers)
            {
                if (offer == null || offer.parts == null)
                    throw new InvalidOperationException("INVALID_DIGEST_OFFER");
                AppendToken(buffer, offer.id);
                AppendToken(buffer, offer.title);
                AppendToken(buffer, offer.role);
                AppendToken(buffer, offer.description);
                AppendNumber(buffer, offer.price_scrap);
                AppendNumber(buffer, offer.pieces);
                AppendNumber(buffer, offer.parts.Length);
                foreach (KitLine line in offer.parts)
                {
                    if (line == null) throw new InvalidOperationException("INVALID_DIGEST_LINE");
                    AppendToken(buffer, line.kind);
                    AppendToken(buffer, line.weapon);
                    AppendNumber(buffer, line.count);
                    AppendNumber(buffer, line.unit_scrap);
                }
            }
            using (SHA256 hasher = SHA256.Create())
            {
                byte[] digest = hasher.ComputeHash(Encoding.UTF8.GetBytes(buffer.ToString()));
                return BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
            }
        }
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
            if (ComputeCatalogDigest(d) != FrozenCatalogDigest)
                throw new InvalidOperationException("FROZEN_KIT_CATALOG_CONTENT_MISMATCH");
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
        private int selected, compared = 7;
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
                compared = 7;
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
                GUILayout.Label(offer.id + " | " + offer.title + " | " + offer.price_scrap + " scrap", EditorStyles.boldLabel);
                if (GUILayout.Button("Set LEFT: " + offer.id)) selected = i;
                if (GUILayout.Button("Set RIGHT: " + offer.id)) compared = i;
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
            GUILayout.Label("Side-by-side reference comparison", EditorStyles.boldLabel);
            if (selected == compared) {
                EditorGUILayout.HelpBox("Select two different kits for a comparison.", MessageType.Info);
            } else {
                try {
                    KitComparison difference = KitComparisonRules.Compare(
                        catalog.offers[selected], catalog.offers[compared]);
                    EditorGUILayout.LabelField(difference.leftId + " → " + difference.rightId);
                    EditorGUILayout.LabelField("Reference scrap: " + difference.leftScrap +
                        " → " + difference.rightScrap + " (delta " + difference.deltaScrap + ")");
                    EditorGUILayout.LabelField("Physical pieces: " + difference.leftPieces +
                        " → " + difference.rightPieces + " (delta " + difference.deltaPieces + ")");
                    foreach (var row in difference.parts)
                        EditorGUILayout.LabelField(row.key + ": " + row.leftCount +
                            " → " + row.rightCount + " (" + row.deltaCount + ")");
                    if (GUILayout.Button("Copy COMPARISON (TSV)"))
                        EditorGUIUtility.systemCopyBuffer = KitComparisonRules.ToTsv(difference);
                } catch (Exception errorCompare) {
                    EditorGUILayout.HelpBox("Comparison refused: " + errorCompare.Message, MessageType.Error);
                }
            }
            EditorGUILayout.LabelField("Pinned source SHA: " + catalog.source_sha);
        }
    }
}
#endif
