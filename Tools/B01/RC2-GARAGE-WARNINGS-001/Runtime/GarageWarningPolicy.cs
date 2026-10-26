using System;
using System.Collections.Generic;
using System.Linq;
namespace B01.GameDesign.RC2GarageWarnings
{
    // Read-only presentation-policy adapter. NEVER mutates canonical R1/R3 state or grants deployment.
    public sealed class GarageSnapshot
    {
        public string SelectedDrive { get; set; } = "";
        public bool? ActivityRequiresWeapon { get; set; }
        public string ActivityId { get; set; } = "";
        public bool SuggestionsEnabled { get; set; } = true;
        public bool RiskyCenterOfMass { get; set; }
        public List<GaragePart> Parts { get; set; } = new();
    }

    public sealed class GaragePart
    {
        public string Id { get; set; } = "";
        // Canonical R3 part categories; ONLY physically mounted, never inventory/ghost.
        public string Kind { get; set; } = "";
        public float Health { get; set; } = 1;
        public int CellX { get; set; }
        public bool? ConnectedToTrustedCore { get; set; }
        // Calculated from actual host authoritative movement/weapon module state;
        // cooldowns are transient and should NOT mark lasting inability to fire.
        public bool? Operational { get; set; }
    }

    public sealed class WarningResult
    {
        public IReadOnlyList<string> Critical { get; }
        public IReadOnlyList<string> Suggestions { get; }
        public bool? WeaponRequired { get; }
        // THIS POLICY DOES NOT ALLOW/DENY START. Owner has left this decision open.
        public string LaunchDecision { get; } = "UNDECIDED_OWNER_APPROVAL_REQUIRED";
        public WarningResult(List<string> critical,List<string> suggestions,bool? weaponRequired)
        { Critical=critical;Suggestions=suggestions;WeaponRequired=weaponRequired; }
    }

    public static class GarageWarningPolicy
    {
        private static readonly HashSet<string> Mobility = new(StringComparer.Ordinal)
        {"Wheels","Tracks","Omni","Hover"};
        public static WarningResult Evaluate(GarageSnapshot? snapshot)
        {
            var errors = new List<string>();
            var tips = new List<string>();
            if (snapshot == null || snapshot.Parts == null ||
                !Mobility.Contains(snapshot.SelectedDrive) ||
                snapshot.Parts.Any(p=>p==null||string.IsNullOrWhiteSpace(p.Id)||
                    string.IsNullOrWhiteSpace(p.Kind)||float.IsNaN(p.Health)||
                    float.IsInfinity(p.Health)||p.Health<0||p.Health>1) ||
                snapshot.Parts.Select(p=>p.Id).Distinct(StringComparer.Ordinal).Count()!=snapshot.Parts.Count)
            {
                errors.Add("SNAPSHOT_UNTRUSTWORTHY");
                return new WarningResult(errors,tips,snapshot?.ActivityRequiresWeapon);
            }
            var parts=snapshot.Parts;
            static bool Available(GaragePart p) =>
                p.Health>0&&p.ConnectedToTrustedCore==true&&p.Operational==true;
            static bool Potential(GaragePart p) =>
                p.Health>0&&p.ConnectedToTrustedCore!=false&&p.Operational!=false;
            // Count every physically mounted Core, including 0HP and disabled entries.
            // BUILD rejects duplicate roots on load; a destroyed extra Core must not mask this.
            bool hasCore=parts.Count(p=>p.Kind=="Core")==1 &&
                         parts.Any(p=>p.Kind=="Core"&&Available(p));
            if (!hasCore) errors.Add("CORE_NONFUNCTIONAL_OR_AMBIGUOUS");
            // Mounted 0HP and inventory or disconnected parts cannot authenticate support.
            if(parts.Any(p=>p.Health>0&&p.ConnectedToTrustedCore==false))
                errors.Add("STRUCTURE_DISCONNECTED");
            bool drivesAvailable,drivesPossible;
            if(snapshot.SelectedDrive=="Wheels"||snapshot.SelectedDrive=="Tracks")
            {
                var needed=snapshot.SelectedDrive=="Wheels"?"Wheel":"CrawlerTrack";
                drivesAvailable=parts.Any(p=>p.Kind==needed&&p.CellX<0&&Available(p)) &&
                                parts.Any(p=>p.Kind==needed&&p.CellX>0&&Available(p));
                drivesPossible=parts.Any(p=>p.Kind==needed&&p.CellX<0&&Potential(p)) &&
                               parts.Any(p=>p.Kind==needed&&p.CellX>0&&Potential(p));
            }
            else
            {
                var needed=snapshot.SelectedDrive=="Omni"?"OmniDrive":"HoverEngine";
                drivesAvailable=parts.Any(p=>p.Kind==needed&&Available(p));
                drivesPossible=parts.Any(p=>p.Kind==needed&&Potential(p));
            }
            if(!drivesAvailable)errors.Add(drivesPossible?"MOBILITY_AUTHORITY_UNVERIFIED":"MOBILITY_NONFUNCTIONAL");
            if(snapshot.ActivityRequiresWeapon==null)
                errors.Add("ACTIVITY_WEAPON_REQUIREMENT_UNKNOWN");
            else if(snapshot.ActivityRequiresWeapon.Value)
            {
                bool hasGun=parts.Any(p=>p.Kind=="Gun"&&Available(p));
                if(!hasGun)
                    errors.Add(parts.Any(p=>p.Kind=="Gun"&&Potential(p))?
                        "WEAPON_AUTHORITY_UNVERIFIED":"WEAPON_NONFUNCTIONAL");
            }
            // Optimisation tips may be disabled; critical errors NEVER disappear.
            if(snapshot.SuggestionsEnabled&&snapshot.RiskyCenterOfMass)
                tips.Add("HIGH_CENTER_OF_MASS_TIP");
            return new WarningResult(errors,tips,snapshot.ActivityRequiresWeapon);
        }
    }
}
