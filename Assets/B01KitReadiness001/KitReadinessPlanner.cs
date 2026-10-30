// B01-22 / KITREADINESS-001 R1 — independent, deterministic preview only.
// Unity 6 C#9 compatible. NO purchase, credit mutation, inventory write, mount or save.
using System;
using System.Collections.Generic;
using System.Linq;

namespace B01KitReadiness001
{
    public sealed class PartNeed
    {
        public string Kind, Weapon;
        public int Quantity, UnitScrap;
    }
    public sealed class KitRecipe
    {
        public string Id;
        public PartNeed[] Parts;
    }
    public sealed class OwnedPart
    {
        public string Kind, Weapon;
        public int Quantity;
    }
    public sealed class NeedResult
    {
        public string Kind, Weapon;
        public int Required, Available, Missing, ReferenceMissingScrap;
    }
    public sealed class KitReadiness
    {
        public string Id;
        public bool Ready;
        public int RequiredPieces, MissingPieces, ReferenceMissingScrap;
        public NeedResult[] Needs;
    }
    public sealed class ReadinessPlan
    {
        public KitReadiness[] Requests;
        public OwnedPart[] RemainingVirtualStock;
        public int ReadyCount, BlockedCount;
    }

    public static class KitReadinessPlanner
    {
        public const string SourceSha = "b1d4e341d846c9397e236809a1d7e4c4fff9c935";
        private const int MaxRequests = 100;
        private const int MaxStock = 1000000;
        private const char Separator = '\u001f';

        private static string Key(string kind, string weapon)
        {
            if (String.IsNullOrWhiteSpace(kind) || kind.IndexOf(Separator) >= 0 ||
                kind.IndexOf('|') >= 0 || kind == "Core" || weapon == null ||
                weapon.IndexOf(Separator) >= 0 || weapon.IndexOf('|') >= 0 ||
                (kind == "Gun" ? String.IsNullOrWhiteSpace(weapon) :
                 !String.IsNullOrEmpty(weapon)))
                throw new ArgumentException("INVALID_PHYSICAL_PART");
            return kind + Separator + weapon;
        }

        // Invariant: no mutation of caller's recipe, stock or game state.
        // One request is reserved ATOMICALLY in a private virtual ledger:
        // an insufficient kit never consumes partial pieces before the next request.
        public static ReadinessPlan Preview(string sourceSha, KitRecipe[] catalog,
                                             OwnedPart[] inventory, string[] orderedRequests)
        {
            if (sourceSha != SourceSha) throw new ArgumentException("WRONG_SOURCE");
            if (catalog == null || catalog.Length != 10 || inventory == null ||
                orderedRequests == null || orderedRequests.Length > MaxRequests)
                throw new ArgumentException("INVALID_INPUT");
            var recipes = new Dictionary<string, KitRecipe>(StringComparer.Ordinal);
            var knownParts = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < catalog.Length; ++i)
            {
                KitRecipe kit = catalog[i];
                if (kit == null || kit.Id != "EK" + (i + 1).ToString("D2") ||
                    kit.Parts == null || kit.Parts.Length == 0 ||
                    !recipes.TryAdd(kit.Id, kit))
                    throw new ArgumentException("INVALID_CATALOG_ID");
                int pieces = 0;
                var unique = new HashSet<string>(StringComparer.Ordinal);
                var families = new HashSet<string>(StringComparer.Ordinal);
                foreach (PartNeed row in kit.Parts)
                {
                    if (row == null || row.Quantity < 1 || row.Quantity > 8 ||
                        row.UnitScrap < 0 || row.UnitScrap > 100000 ||
                        !unique.Add(Key(row.Kind, row.Weapon)))
                        throw new ArgumentException("INVALID_CATALOG_PART");
                    knownParts.Add(Key(row.Kind, row.Weapon));
                    if (row.Kind == "Gun") families.Add(row.Weapon);
                    pieces += row.Quantity;
                }
                if (pieces > 12 || families.Count > 2)
                    throw new ArgumentException("INVALID_CATALOG_LIMIT");
            }
            var stock = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (OwnedPart part in inventory)
            {
                if (part == null || part.Quantity < 0 || part.Quantity > MaxStock)
                    throw new ArgumentException("INVALID_STOCK_COUNT");
                string key = Key(part.Kind, part.Weapon);
                if (!knownParts.Contains(key) || !stock.TryAdd(key, part.Quantity))
                    throw new ArgumentException("UNKNOWN_OR_DUPLICATE_STOCK");
            }
            var decisions = new List<KitReadiness>();
            int ready = 0;
            foreach (string request in orderedRequests)
            {
                KitRecipe kit;
                if (request == null || !recipes.TryGetValue(request, out kit))
                    throw new ArgumentException("UNKNOWN_REQUEST_ID");
                var lines = new List<NeedResult>();
                int missingTotal = 0, quoteTotal = 0, pieceTotal = 0;
                foreach (PartNeed part in kit.Parts)
                {
                    string key = Key(part.Kind, part.Weapon);
                    int owned;
                    stock.TryGetValue(key, out owned);
                    int shortfall = Math.Max(0, part.Quantity - owned);
                    int quoted = checked(shortfall * part.UnitScrap);
                    missingTotal = checked(missingTotal + shortfall);
                    quoteTotal = checked(quoteTotal + quoted);
                    pieceTotal = checked(pieceTotal + part.Quantity);
                    lines.Add(new NeedResult {
                        Kind = part.Kind, Weapon = part.Weapon,
                        Required = part.Quantity, Available = owned,
                        Missing = shortfall, ReferenceMissingScrap = quoted
                    });
                }
                bool canBuild = missingTotal == 0;
                if (canBuild)
                {
                    // Atomic second pass; no partial reservation on failure.
                    foreach (PartNeed part in kit.Parts)
                    {
                        string key = Key(part.Kind, part.Weapon);
                        stock[key] -= part.Quantity;
                    }
                    ++ready;
                }
                decisions.Add(new KitReadiness {
                    Id = kit.Id, Ready = canBuild, RequiredPieces = pieceTotal,
                    MissingPieces = missingTotal, ReferenceMissingScrap = quoteTotal,
                    Needs = lines.ToArray()
                });
            }
            // Sorted, detached virtual snapshot. Never written into real inventory.
            OwnedPart[] remaining = stock.OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new OwnedPart {
                    Kind = x.Key.Substring(0, x.Key.IndexOf(Separator)),
                    Weapon = x.Key.Substring(x.Key.IndexOf(Separator) + 1),
                    Quantity = x.Value
                }).ToArray();
            return new ReadinessPlan {
                Requests = decisions.ToArray(), RemainingVirtualStock = remaining,
                ReadyCount = ready, BlockedCount = decisions.Count - ready
            };
        }
    }
}
