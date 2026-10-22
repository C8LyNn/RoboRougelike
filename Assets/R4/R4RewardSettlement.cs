using System;
using System.Collections.Generic;
using System.Linq;
using RoboRougelike.R1;
using RoboRougelike.R3;

namespace RoboRougelike.R4
{
    // One transaction boundary shared by the live reward UI and model tests.
    public static class R4RewardSettlement
    {
        public static bool TryClaim(R1RunProgress progress,R4SalvageLedger ledger,R3VehicleModel model,
            IEnumerable<string> selectedParts,IEnumerable<int> selectedBonuses)
        {
            if(progress==null||ledger==null||model==null||selectedParts==null||selectedBonuses==null)return false;
            var parts=selectedParts.ToArray();var bonuses=selectedBonuses.ToArray();
            if(parts.Length>3||bonuses.Length!=1||parts.Distinct().Count()!=parts.Length||
                !progress.CanCompleteUnifiedRewards(bonuses))return false;
            var bonus=R3VehicleModel.CreateBonusReward(progress.Offers[bonuses[0]]);
            if(bonus==null)return false;
            // One inventory batch contains the independent bonus and all selected enemy parts.
            // No progress/scrap/ledger mutation happens if any incoming item is rejected.
            if(!ledger.Claim(model,parts,bonus))return false;
            return progress.CompleteUnifiedRewards(bonuses);
        }
    }
}
