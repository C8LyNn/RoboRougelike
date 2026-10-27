using System;
using System.Collections.Generic;

namespace B01.PARTSIZE001
{
    /// <summary>
    /// Source-audited 19 canonical R3PartKind names from Codex integration
    /// 88b8eed. Only the *logical occupied cells* are inferred for every kind.
    /// Structural cube/physical wide base-box geometry is SOURCE-DERIVED;
    /// visual payloads and moving envelopes are explicitly UNMEASURED.
    /// No R3 enum reference, adapter, migration, inventory or host mutation.
    /// </summary>
    public static class PartSizeBaselineCatalog
    {
        public static readonly string[] CanonicalNames = {
            "Armor","Wheel","Gun","Core","Suspension","Booster",
            "JumpDrive","Blink","Gyroscope","OmniDrive",
            "MagneticGrapple","HoverEngine","CrawlerTrack","BonusModule",
            "DirectionalShield","PointDefense","StructuralScanner",
            "ScannerPulse","IndependentSteering"
        };
        private static readonly HashSet<string> Wide = new HashSet<string>(
            new[]{"BonusModule","DirectionalShield","PointDefense",
                "StructuralScanner","ScannerPulse","IndependentSteering"},
            StringComparer.Ordinal);

        public static PartSizeDefinition ForSourceKind(string canonicalKind)
        {
            if(Array.IndexOf(CanonicalNames,canonicalKind)<0)
                throw new ArgumentException("UNKNOWN_CANONICAL_KIND",nameof(canonicalKind));
            if(Wide.Contains(canonicalKind))
                return PartSizeReferenceFixtures.LegacyWide(canonicalKind);
            if(canonicalKind=="Armor"||canonicalKind=="Core")
                return PartSizeReferenceFixtures.LegacyCube(canonicalKind);
            // 0.52m is only the common *root cube*; wheel/gun/moving
            // payloads can exceed it. Never treat base cube as full hull.
            return PartSizeReferenceFixtures.Unmeasured(canonicalKind,
                new Cell3(1,1,1));
        }
    }
}
