using System;
using System.Collections.Generic;
using RoboRougelike.R1;

// Existing physics/garage scenarios keep one chosen bonus, now in the single reward transaction.
public static class RRLW2TestFlow
{
    public static bool Claim(R1RunController run,IEnumerable<string> parts) => run.ConfirmRewards(parts,
        run.Progress.Offers.Count>0?new[]{0}:Array.Empty<int>());
    public static void DefeatAll(R1RunController run)
    {
        foreach(var foe in run.Encounter.Enemies)
            if(foe!=null&&!foe.IsDefeated)foe.Core.ApplyDamage(100000,foe.Core.transform.position,UnityEngine.Vector3.up);
    }
}
