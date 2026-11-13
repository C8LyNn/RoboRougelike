using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using UnityEngine;

namespace RoboRougelike.R3
{
    // One activation per kind, shared by its mounted copies.
    public sealed class R3RechargePulse : MonoBehaviour
    {
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        public void Configure(R3VehicleAssembly owner){assembly=owner;energy=GetComponent<VehicleEnergyBank>();}
        bool Ready(R3MountedPart part)=>part!=null&&part.Kind==R3PartKind.RechargePulse&&part.Operational&&
            part.gameObject.activeInHierarchy&&part.transform.IsChildOf(transform)&&!RRLEmpPartState.IsBlocked(part.gameObject);
        public string Status
        {
            get
            {
                if(assembly==null)return "niedostępny";
                float remaining=assembly.Model.RechargePulseReadyAt-Time.time;
                if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
                int copies=0;float cost=0;
                foreach(var part in assembly.Parts)if(Ready(part))
                {copies++;cost+=assembly.Model.Economy.RechargePulseEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier);}
                if(copies==0)return "niedostępny";
                var emp=GetComponent<R3EmpDevices>();if(emp==null||!emp.CanRecharge())return "brak urządzeń do naładowania";
                return energy!=null&&energy.CanSpend(cost)?"gotowy":"brak energii";
            }
        }
        public bool TryActivate()
        {
            var run=R1RunController.Instance;
            if(assembly==null||energy==null||run==null||run.Player==null||run.Player.gameObject!=gameObject||
               !run.CanUseMovement||Time.time<assembly.Model.RechargePulseReadyAt)return false;
            var emp=GetComponent<R3EmpDevices>();if(emp==null)return false;
            var economy=assembly.Model.Economy;int units=0;float cost=0;
            foreach(var part in assembly.Parts)if(Ready(part))
            {units+=R3VehicleModel.TierEffectUnits(part.Tier);cost+=economy.RechargePulseEnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier);}
            if(units==0||float.IsNaN(cost)||float.IsInfinity(cost)||cost<0)return false;
            int amount=(int)System.Math.Min(int.MaxValue,(long)units*System.Math.Max(0,economy.RechargePulseChargesPerUnit));
            if(amount==0||!emp.CanRecharge()||!energy.TrySpend(cost))return false;
            emp.Recharge(amount);
            float cooldown=economy.RechargePulseCooldown;
            assembly.Model.RechargePulseReadyAt=Time.time+(float.IsNaN(cooldown)||float.IsInfinity(cooldown)?0:Mathf.Max(0,cooldown));
            return true;
        }
    }
}
