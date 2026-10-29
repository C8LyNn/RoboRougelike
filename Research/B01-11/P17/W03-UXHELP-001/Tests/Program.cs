using System;
using System.Collections.Generic;
using B01.UXHelp;

class Program
{
    static int pass, fail;
    static HelpSnapshot S(HelpStage stage)
    {
        return new HelpSnapshot { Stage = stage };
    }
    static void Check(string name, Func<bool> predicate)
    {
        try
        {
            if (!predicate()) throw new Exception("assert false");
            Console.WriteLine("PASS " + name); pass++;
        }
        catch (Exception e) { Console.WriteLine("FAIL " + name + ": " + e.Message); fail++; }
    }
    static bool Reject(Action act)
    {
        try { act(); return false; } catch (ArgumentException) { return true; }
    }
    static string IDs(HelpSnapshot s, ISet<string> dismissed = null)
    {
        var a=UXHelpCore.Select(s,dismissed);var text=new List<string>();
        foreach(var msg in a) text.Add(msg.Id);
        return string.Join("|",text.ToArray());
    }
    static bool Same(string actual,string expected) {return actual == expected;}
    static void Main()
    {
        Check("No false hint in ordinary garage",()=>Same(IDs(S(HelpStage.Garage)),""));
        Check("No false hint in ordinary combat",()=>Same(IDs(S(HelpStage.Combat)),""));
        Check("No false hint in results",()=>Same(IDs(S(HelpStage.Results)),""));
        var noDrive=S(HelpStage.Garage);noDrive.HasSelectedDriveMobility=false;
        Check("Missing drive critical",()=>Same(IDs(noDrive),"GARAGE_DRIVE_MISSING"));
        var noGun=S(HelpStage.Garage);noGun.MountedWeapons=0;noGun.ActiveWeapons=0;
        Check("No gun critical",()=>Same(IDs(noGun),"GARAGE_WEAPON_MISSING"));
        var broken=S(HelpStage.Garage);broken.ActiveWeapons=0;
        Check("Broken gun critical",()=>Same(IDs(broken),"GARAGE_WEAPON_BROKEN"));
        var both=S(HelpStage.Garage);both.MountedWeapons=0;both.ActiveWeapons=0;both.HasSelectedDriveMobility=false;
        Check("Simultaneous drive and gun critical",()=>Same(IDs(both),"GARAGE_DRIVE_MISSING|GARAGE_WEAPON_MISSING"));
        Check("Both critical explicit API",()=>UXHelpCore.HasBothGarageCritical(both));
        Check("Both critical tips OFF",()=>{both.OptionalTipsEnabled=false;return Same(IDs(both),"GARAGE_DRIVE_MISSING|GARAGE_WEAPON_MISSING");});
        Check("Critical dismissal ignored",()=>Same(IDs(both,new HashSet<string>{"GARAGE_DRIVE_MISSING","GARAGE_WEAPON_MISSING"}),"GARAGE_DRIVE_MISSING|GARAGE_WEAPON_MISSING"));
        var bothBroken=S(HelpStage.Garage);bothBroken.ActiveWeapons=0;bothBroken.HasSelectedDriveMobility=false;
        Check("Double broken precise reasons",()=>Same(IDs(bothBroken),"GARAGE_DRIVE_MISSING|GARAGE_WEAPON_BROKEN"));
        var dangerous=S(HelpStage.Combat);dangerous.CoreHp=25;
        Check("Core 25% inclusive",()=>Same(IDs(dangerous),"COMBAT_CORE_CRITICAL"));
        dangerous.CoreHp=26;
        Check("Core 26% noncritical",()=>Same(IDs(dangerous),""));
        dangerous.CoreHp=1;dangerous.CoreMaxHp=4;
        Check("Small core exact threshold",()=>Same(IDs(dangerous),"COMBAT_CORE_CRITICAL"));
        dangerous.CoreHp=0;dangerous.CoreMaxHp=100;
        Check("0HP no fake avoid-hits prompt",()=>Same(IDs(dangerous),""));
        var unarmed=S(HelpStage.Combat);unarmed.ActiveWeapons=0;
        Check("Combat weapon lost",()=>Same(IDs(unarmed),"COMBAT_WEAPON_LOST"));
        unarmed.CoreHp=10;
        Check("Combat double critical",()=>Same(IDs(unarmed),"COMBAT_CORE_CRITICAL|COMBAT_WEAPON_LOST"));
        unarmed.OptionalTipsEnabled=false;
        Check("Combat critical unaffected by tips OFF",()=>Same(IDs(unarmed),"COMBAT_CORE_CRITICAL|COMBAT_WEAPON_LOST"));
        var initial=S(HelpStage.Combat);initial.IsFirstCombat=true;
        Check("Wheels first combat controls",()=>Same(IDs(initial),"COMBAT_WHEELS_CONTROLS"));
        initial.Drive=HelpDrive.Tracks;
        Check("Tracks first combat controls",()=>Same(IDs(initial),"COMBAT_WHEELS_CONTROLS"));
        initial.Drive=HelpDrive.Omni;
        Check("Omni first combat controls",()=>Same(IDs(initial),"COMBAT_OMNI_CONTROLS"));
        initial.Drive=HelpDrive.Hover;
        Check("Hover first combat controls",()=>Same(IDs(initial),"COMBAT_OMNI_CONTROLS"));
        initial.AssignedWeaponGroups=2;initial.Drive=HelpDrive.Wheels;
        Check("First combat instructions prioritized over dual group hint",()=>Same(IDs(initial),"COMBAT_WHEELS_CONTROLS"));
        initial.IsFirstCombat=false;
        Check("Dual weapon group instruction",()=>Same(IDs(initial),"COMBAT_DUAL_GROUPS"));
        var dismissed=new HashSet<string>{"COMBAT_DUAL_GROUPS"};
        Check("Dismiss optional hint",()=>Same(IDs(initial,dismissed),""));
        initial.OptionalTipsEnabled=false;
        Check("Optional tips switch OFF",()=>Same(IDs(initial),""));
        var checkpoint=S(HelpStage.VehicleSelect);checkpoint.HasCheckpoint=true;
        Check("Checkpoint only if authoritative flag",()=>Same(IDs(checkpoint),"SELECT_RESUME_CHECKPOINT"));
        checkpoint.HasCheckpoint=false;
        Check("No phantom checkpoint",()=>Same(IDs(checkpoint),""));
        checkpoint.HasCheckpoint=true;checkpoint.OptionalTipsEnabled=false;
        Check("Suppress checkpoint with tips OFF",()=>Same(IDs(checkpoint),""));
        var reward=S(HelpStage.Reward);
        Check("Reward hint",()=>Same(IDs(reward),"REWARD_SELECT_MODULE"));
        reward.OptionalTipsEnabled=false;
        Check("Reward hints optional",()=>Same(IDs(reward),""));
        Check("Null state rejects",()=>Reject(()=>UXHelpCore.Select(null)));
        var invalid=S(HelpStage.Garage);invalid.ActiveWeapons=2;
        Check("Active gun > mounted rejects",()=>Reject(()=>UXHelpCore.Select(invalid)));
        invalid=S(HelpStage.Combat);invalid.CoreMaxHp=0;
        Check("Zero max HP rejects",()=>Reject(()=>UXHelpCore.Select(invalid)));
        invalid=S(HelpStage.Combat);invalid.CoreHp=101;
        Check("Over max HP rejects",()=>Reject(()=>UXHelpCore.Select(invalid)));
        invalid=S(HelpStage.Garage);invalid.MountedWeapons=-1;
        Check("Negative gun count rejects",()=>Reject(()=>UXHelpCore.Select(invalid)));
        invalid=S(HelpStage.Garage);invalid.AssignedWeaponGroups=3;
        Check("Three groups reject",()=>Reject(()=>UXHelpCore.Select(invalid)));
        invalid=S(HelpStage.VehicleSelect);invalid.Stage=(HelpStage)99;
        Check("Unknown state rejects",()=>Reject(()=>UXHelpCore.Select(invalid)));
        invalid=S(HelpStage.Combat);invalid.Drive=(HelpDrive)10;
        Check("Unknown drive rejects",()=>Reject(()=>UXHelpCore.Select(invalid)));
        Check("Polish copy nonempty",()=>UXHelpCore.BasicControls.Text("pl").Contains("LPM/PPM"));
        Check("English copy nonempty",()=>UXHelpCore.BasicControls.Text("en").Contains("LMB/RMB"));
        Check("No accidental English fallback for pl",()=>UXHelpCore.WeaponMissing.Text("pl").Contains("Brak"));
        Check("No accidental Polish in en",()=>UXHelpCore.WeaponMissing.Text("en").Contains("weapon"));
        Check("Critical message severity",()=>UXHelpCore.WeaponBroken.Severity==HelpSeverity.Critical);
        Check("Info message severity",()=>UXHelpCore.OmniControls.Severity==HelpSeverity.Information);
        Check("IDs do not duplicate",()=>{
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var msg in new [] {UXHelpCore.DriveMissing,UXHelpCore.WeaponMissing,UXHelpCore.WeaponBroken,UXHelpCore.CoreCritical,UXHelpCore.CombatWeaponLost,UXHelpCore.BasicControls,UXHelpCore.OmniControls,UXHelpCore.WeaponGroups,UXHelpCore.Checkpoint,UXHelpCore.RewardChoice})
                if(!ids.Add(msg.Id))return false;
            return ids.Count==10;
        });
        Console.WriteLine("RESULT " + pass + " PASS, " + fail + " FAIL");
        if(fail>0)Environment.Exit(1);
    }
}
