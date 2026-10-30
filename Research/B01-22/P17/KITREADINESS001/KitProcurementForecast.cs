// B01-22 PASS17 W11: consolidated physical-part shortfall for ALL requested
// EK01-EK10 recipes. Informational, not a shop/cart/transaction or reservation.
// This .NET8/C#9 host is Research-only; Unity source KitReadinessPlanner unchanged.
using System;
using System.Collections.Generic;
using System.Linq;
using B01KitReadiness001;

internal sealed class ProcurementLine
{
    public string Kind, Weapon;
    public int Required, Owned, Covered, Missing, UnitReferenceScrap, ReferenceMissingScrap;
}

internal sealed class ProcurementForecast
{
    public bool AllRequestedKitsPossibleFromCurrentStock;
    public int TotalRequestedKits, TotalRequiredPieces, TotalCoveredPieces;
    public int TotalMissingPieces, TotalReferenceMissingScrap;
    public ProcurementLine[] Lines;
}

internal static class KitProcurementForecast
{
    // Run ONLY AFTER the original, immutable 154-field recipe + stock validations
    // succeeded in KitReadinessPlanner.Preview. Re-run this checked original
    // validator here rather than trusting callers or copying a sourceSha header.
    internal static ProcurementForecast Calculate(string sourceSha, KitRecipe[] approved,
                                                    OwnedPart[] stock, string[] requests)
    {
        KitReadinessPlanner.Preview(sourceSha, approved, stock, requests);

        var catalog = approved.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var needed = new SortedDictionary<string, ProcurementLine>(StringComparer.Ordinal);
        foreach (string id in requests)
        {
            foreach (PartNeed part in catalog[id].Parts)
            {
                string key = part.Kind + "\u001f" + part.Weapon;
                if (!needed.TryGetValue(key, out ProcurementLine line))
                {
                    line = new ProcurementLine {
                        Kind = part.Kind, Weapon = part.Weapon,
                        UnitReferenceScrap = part.UnitScrap
                    };
                    needed.Add(key, line);
                }
                if (line.UnitReferenceScrap != part.UnitScrap)
                    throw new ArgumentException("INCONSISTENT_REFERENCE_UNIT_PRICE");
                line.Required = checked(line.Required + part.Quantity);
            }
        }

        var owned = stock.ToDictionary(x => x.Kind + "\u001f" + x.Weapon,
            x => x.Quantity, StringComparer.Ordinal);
        int req = 0, covered = 0, missing = 0, scrap = 0;
        foreach (ProcurementLine line in needed.Values)
        {
            int n;
            owned.TryGetValue(line.Kind + "\u001f" + line.Weapon, out n);
            line.Owned = n;
            line.Covered = Math.Min(line.Required, n);
            line.Missing = checked(line.Required - line.Covered);
            line.ReferenceMissingScrap = checked(line.Missing * line.UnitReferenceScrap);
            req = checked(req + line.Required);
            covered = checked(covered + line.Covered);
            missing = checked(missing + line.Missing);
            scrap = checked(scrap + line.ReferenceMissingScrap);
        }
        return new ProcurementForecast {
            AllRequestedKitsPossibleFromCurrentStock = missing == 0,
            TotalRequestedKits = requests.Length,
            TotalRequiredPieces = req,
            TotalCoveredPieces = covered,
            TotalMissingPieces = missing,
            TotalReferenceMissingScrap = scrap,
            Lines = needed.Values.ToArray()
        };
    }
}
