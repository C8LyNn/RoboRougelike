#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;

namespace B01KitGallery001
{
    // B01-22 W06: pure editor-only comparison, no wallet, no upgrade, no install commands.
    public sealed class KitPartDifference
    {
        public string key;
        public int leftCount, rightCount, deltaCount, leftScrap, rightScrap;
    }
    public sealed class KitComparison
    {
        public string leftId, rightId;
        public int leftScrap, rightScrap, deltaScrap, leftPieces, rightPieces, deltaPieces;
        public KitPartDifference[] parts;
    }
    public static class KitComparisonRules
    {
        private sealed class Entry
        {
            public int count, scrap, unit;
        }
        private static Dictionary<string, Entry> Index(KitOffer kit)
        {
            if (kit == null || kit.parts == null || kit.parts.Length == 0)
                throw new InvalidOperationException("INVALID_COMPARISON_KIT");
            var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
            long total = 0, pieces = 0;
            foreach (var line in kit.parts)
            {
                if (line == null || String.IsNullOrEmpty(line.kind) || line.kind == "Core" ||
                    line.count < 1 || line.count > 8 || line.unit_scrap < 0 ||
                    (line.kind == "Gun" && String.IsNullOrWhiteSpace(line.weapon)) ||
                    (line.kind != "Gun" && !String.IsNullOrEmpty(line.weapon)))
                    throw new InvalidOperationException("INVALID_COMPARISON_LINE");
                var key = line.kind == "Gun" ? "Gun:" + line.weapon : line.kind;
                if (result.ContainsKey(key)) throw new InvalidOperationException("DUPLICATE_COMPARISON_LINE");
                var subtotal = checked(line.count * line.unit_scrap);
                result.Add(key, new Entry { count = line.count, scrap = subtotal, unit = line.unit_scrap });
                total += subtotal; pieces += line.count;
            }
            if (total != kit.price_scrap || pieces != kit.pieces || pieces > 12)
                throw new InvalidOperationException("COMPARISON_TOTAL_MISMATCH");
            return result;
        }

        public static KitComparison Compare(KitOffer left, KitOffer right)
        {
            if (left == null || right == null || left.id == right.id ||
                String.IsNullOrWhiteSpace(left.id) || String.IsNullOrWhiteSpace(right.id))
                throw new InvalidOperationException("COMPARISON_REQUIRES_TWO_DISTINCT_KITS");
            var a = Index(left);
            var b = Index(right);
            var keys = new SortedSet<string>(a.Keys, StringComparer.Ordinal);
            keys.UnionWith(b.Keys);
            var lines = new List<KitPartDifference>();
            foreach (var key in keys)
            {
                Entry before, after;
                a.TryGetValue(key, out before);
                b.TryGetValue(key, out after);
                if (before != null && after != null && before.unit != after.unit)
                    throw new InvalidOperationException("CONFLICTING_PART_REFERENCE_PRICE");
                int ac = before == null ? 0 : before.count;
                int bc = after == null ? 0 : after.count;
                lines.Add(new KitPartDifference {
                    key = key, leftCount = ac, rightCount = bc, deltaCount = bc - ac,
                    leftScrap = before == null ? 0 : before.scrap,
                    rightScrap = after == null ? 0 : after.scrap
                });
            }
            return new KitComparison {
                leftId = left.id, rightId = right.id, leftScrap = left.price_scrap,
                rightScrap = right.price_scrap, deltaScrap = checked(right.price_scrap - left.price_scrap),
                leftPieces = left.pieces, rightPieces = right.pieces,
                deltaPieces = right.pieces - left.pieces, parts = lines.ToArray()
            };
        }

        // Stable TSV for review, not a transaction or a part-install recipe.
        public static string ToTsv(KitComparison result)
        {
            if (result == null || result.parts == null)
                throw new InvalidOperationException("MISSING_COMPARISON");
            var text = new StringBuilder("Part\tLeftCount\tRightCount\tDeltaCount\tLeftScrap\tRightScrap\n");
            foreach (var row in result.parts)
                text.Append(row.key).Append('\t').Append(row.leftCount).Append('\t')
                    .Append(row.rightCount).Append('\t').Append(row.deltaCount).Append('\t')
                    .Append(row.leftScrap).Append('\t').Append(row.rightScrap).Append('\n');
            return text.ToString();
        }
    }
}
#endif
