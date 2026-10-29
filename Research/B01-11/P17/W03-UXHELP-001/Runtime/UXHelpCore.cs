using System;
using System.Collections.Generic;

namespace B01.UXHelp
{
    // This is an isolated game-content candidate. The Codex-owned game supplies authoritative state.
    // No UnityEngine, no local player state mutation, no time/network/analytics dependencies.
    public enum HelpStage { VehicleSelect, Garage, Combat, Reward, Results }
    public enum HelpDrive { Wheels, Tracks, Omni, Hover }
    public enum HelpSeverity { Critical, Information }

    public sealed class HelpSnapshot
    {
        public HelpStage Stage;
        public HelpDrive Drive;
        public bool IsFirstCombat;
        public bool OptionalTipsEnabled = true;
        public bool HasCheckpoint;
        public bool HasSelectedDriveMobility = true;
        public int MountedWeapons = 1;
        public int ActiveWeapons = 1;
        public int AssignedWeaponGroups = 1;
        public int CoreHp = 100;
        public int CoreMaxHp = 100;
    }

    public sealed class HelpMessage
    {
        public readonly string Id;
        public readonly HelpSeverity Severity;
        public readonly string Polish;
        public readonly string English;
        public HelpMessage(string id, HelpSeverity severity, string polish, string english)
        {
            Id = id; Severity = severity; Polish = polish; English = english;
        }
        public string Text(string locale) { return locale == "en" ? English : Polish; }
    }

    public static class UXHelpCore
    {
        // These are safe UI descriptions, NOT instructions to modify game economy/physics.
        // Optional hints can be dismissed; simultaneous critical reasons are never hidden.
        public static readonly HelpMessage DriveMissing = new HelpMessage(
            "GARAGE_DRIVE_MISSING", HelpSeverity.Critical,
            "Wybrany napęd jest niesprawny. Sprawdź odpowiednie moduły w garażu.",
            "The selected drive is inoperable. Check its modules in the garage.");
        public static readonly HelpMessage WeaponMissing = new HelpMessage(
            "GARAGE_WEAPON_MISSING", HelpSeverity.Critical,
            "Brak zamontowanej broni. Zamontuj sprawny moduł broni przed wyprawą.",
            "No weapon is mounted. Install a working weapon module before deployment.");
        public static readonly HelpMessage WeaponBroken = new HelpMessage(
            "GARAGE_WEAPON_BROKEN", HelpSeverity.Critical,
            "Zamontowana broń nie działa. Sprawdź jej stan i dostępne naprawy.",
            "The mounted weapon is inoperable. Check its condition and available repairs.");
        public static readonly HelpMessage CoreCritical = new HelpMessage(
            "COMBAT_CORE_CRITICAL", HelpSeverity.Critical,
            "Rdzeń jest poważnie uszkodzony. Unikaj dalszych trafień.",
            "Your core is critically damaged. Avoid further hits.");
        public static readonly HelpMessage CombatWeaponLost = new HelpMessage(
            "COMBAT_WEAPON_LOST", HelpSeverity.Critical,
            "Żadna zamontowana broń nie jest aktywna. Zmień taktykę i unikaj ostrzału.",
            "No mounted weapon is active. Adapt your tactics and avoid incoming fire.");
        public static readonly HelpMessage BasicControls = new HelpMessage(
            "COMBAT_WHEELS_CONTROLS", HelpSeverity.Information,
            "W/S — jazda, A/D — skręt, mysz — celowanie, LPM/PPM — ogień.",
            "W/S — drive, A/D — steer, mouse — aim, LMB/RMB — fire.");
        public static readonly HelpMessage OmniControls = new HelpMessage(
            "COMBAT_OMNI_CONTROLS", HelpSeverity.Information,
            "W/S — jazda, A/D — ruch boczny, Z/C — obrót, LPM/PPM — ogień.",
            "W/S — drive, A/D — strafe, Z/C — rotate, LMB/RMB — fire.");
        public static readonly HelpMessage WeaponGroups = new HelpMessage(
            "COMBAT_DUAL_GROUPS", HelpSeverity.Information,
            "Masz dwie grupy broni. LPM i PPM sterują osobnymi grupami.",
            "Two weapon groups are assigned. LMB and RMB control different groups.");
        public static readonly HelpMessage Checkpoint = new HelpMessage(
            "SELECT_RESUME_CHECKPOINT", HelpSeverity.Information,
            "Dostępna jest zapisana wyprawa. Możesz wybrać jej wczytanie.",
            "An expedition checkpoint is available. You can choose to resume it.");
        public static readonly HelpMessage RewardChoice = new HelpMessage(
            "REWARD_SELECT_MODULE", HelpSeverity.Information,
            "Wybierz nagrodę. Sprawdź jej efekt przed następnym starciem.",
            "Choose a reward. Check its effect before the next encounter.");

        public static HelpMessage[] Select(HelpSnapshot state, ISet<string> dismissed = null)
        {
            if (state == null) throw new ArgumentNullException("state");
            if (!Enum.IsDefined(typeof(HelpStage), state.Stage) ||
                !Enum.IsDefined(typeof(HelpDrive), state.Drive) ||
                state.MountedWeapons < 0 || state.ActiveWeapons < 0 ||
                state.ActiveWeapons > state.MountedWeapons ||
                state.AssignedWeaponGroups < 0 || state.AssignedWeaponGroups > 2 ||
                state.CoreMaxHp <= 0 || state.CoreHp < 0 || state.CoreHp > state.CoreMaxHp)
                throw new ArgumentException("Invalid authoritative state", "state");

            var result = new List<HelpMessage>();
            switch (state.Stage)
            {
                case HelpStage.Garage:
                    if (!state.HasSelectedDriveMobility) result.Add(DriveMissing);
                    if (state.MountedWeapons == 0) result.Add(WeaponMissing);
                    else if (state.ActiveWeapons == 0) result.Add(WeaponBroken);
                    break;
                case HelpStage.Combat:
                    // Avoid floating point: 0<HP<=25% qualifies. Actual 0HP result belongs to game.
                    if (state.CoreHp > 0 && (long)state.CoreHp * 4 <= state.CoreMaxHp)
                        result.Add(CoreCritical);
                    if (state.MountedWeapons > 0 && state.ActiveWeapons == 0)
                        result.Add(CombatWeaponLost);
                    break;
            }

            // Optional, lower-priority guidance: exactly one message per call.
            if (!state.OptionalTipsEnabled) return result.ToArray();
            HelpMessage optional = null;
            if (state.Stage == HelpStage.VehicleSelect && state.HasCheckpoint)
                optional = Checkpoint;
            else if (state.Stage == HelpStage.Reward)
                optional = RewardChoice;
            else if (state.Stage == HelpStage.Combat && state.ActiveWeapons > 0)
            {
                // Both control cards claim LMB/RMB can fire: never suggest firing with
                // zero operational weapons. Two groups require two active weapons.
                if (state.IsFirstCombat)
                {
                    var controls = (state.Drive == HelpDrive.Omni || state.Drive == HelpDrive.Hover)
                        ? OmniControls : BasicControls;
                    if (dismissed == null || !dismissed.Contains(controls.Id))
                        optional = controls;
                }
                // An already dismissed first-combat card must not starve a second
                // valid hint. Return at most one non-dismissed optional card.
                if (optional == null && state.AssignedWeaponGroups == 2 && state.ActiveWeapons >= 2)
                    optional = WeaponGroups;
            }
            if (optional != null && (dismissed == null || !dismissed.Contains(optional.Id)))
                result.Add(optional);
            return result.ToArray();
        }

        // Unity UI may choose a different rendering policy; content always remains decoupled.
        public static bool HasBothGarageCritical(HelpSnapshot s)
        {
            var messages = Select(s);
            bool drive = false, weapon = false;
            foreach (var m in messages)
            {
                if (m.Id == DriveMissing.Id) drive = true;
                if (m.Id == WeaponMissing.Id || m.Id == WeaponBroken.Id) weapon = true;
            }
            return drive && weapon;
        }
    }
}
