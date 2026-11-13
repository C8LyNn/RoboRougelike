using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using UnityEngine;

namespace RoboRougelike.R3
{
    // One shared activation and cooldown per device type; all values are provisional.
    [DisallowMultipleComponent]
    public sealed class R3OffensiveOverdrive : MonoBehaviour
    {
        readonly List<R3MountedPart> active=new List<R3MountedPart>();
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        R3MountedBonusEffects effects;
        float endsAt;
        public string Status
        {
            get
            {
                if(assembly==null||energy==null)return "niedostępny";
                if(active.Count>0&&Time.time<endsAt)return "aktywny";
                float remaining=assembly.Model.OverdriveReadyAt-Time.time;
                if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
                float cost=0;int count=0;
                foreach(var part in assembly.Parts)if(Ready(part))
                {count++;cost+=NonNegative(assembly.Model.Economy.OverdriveEnergyPerCopy)*R3VehicleModel.TierActivationCost(part.Tier);}
                if(count==0)return "niedostępny";
                return energy.CanSpend(cost)?"gotowy":"brak energii";
            }
        }

        public void Configure(R3VehicleAssembly owner)
        {assembly=owner;energy=GetComponent<VehicleEnergyBank>();effects=GetComponent<R3MountedBonusEffects>();}

        static float NonNegative(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Max(0,value);
        bool Ready(R3MountedPart part)=>part!=null&&part.Kind==R3PartKind.Overdrive&&part.Operational&&
            part.gameObject.activeInHierarchy&&part.transform.IsChildOf(transform)&&!RRLEmpPartState.IsBlocked(part.gameObject);

        public int TryActivate()
        {
            var run=R1RunController.Instance;
            if(assembly==null||energy==null||effects==null||run==null||run.Player==null||
               run.Player.gameObject!=gameObject||!run.CanUseMovement||Time.time<endsAt)return 0;
            // A key press can arrive on the expiry frame before Update clears the effect.
            if(active.Count>0){active.Clear();endsAt=0;effects.SetOverdriveIncrease(0);}
            if(Time.time<assembly.Model.OverdriveReadyAt)return 0;
            var tuning=assembly.Model.Economy;
            float duration=NonNegative(tuning.OverdriveDuration),cost=0;
            if(duration<=0)return 0;
            active.Clear();
            foreach(var part in assembly.Parts)
                if(Ready(part)){active.Add(part);cost+=NonNegative(tuning.OverdriveEnergyPerCopy)*R3VehicleModel.TierActivationCost(part.Tier);}
            if(active.Count==0||!energy.TrySpend(cost)){active.Clear();return 0;}
            endsAt=Time.time+duration;
            assembly.Model.OverdriveReadyAt=Time.time+NonNegative(tuning.OverdriveCooldown);
            ApplyContribution();return active.Count;
        }

        void Update()
        {
            if(active.Count==0)return;
            if(Time.time>=endsAt){active.Clear();effects?.SetOverdriveIncrease(0);return;}
            ApplyContribution();
        }

        void ApplyContribution()
        {
            int units=0;
            foreach(var part in active)if(Ready(part))units+=R3VehicleModel.TierEffectUnits(part.Tier);
            if(units==0){active.Clear();endsAt=0;effects.SetOverdriveIncrease(0);return;}
            effects.SetOverdriveIncrease(NonNegative(assembly.Model.Economy.OverdriveBonusPerUnit)*units);
        }

        void OnDisable(){active.Clear();endsAt=0;effects?.SetOverdriveIncrease(0);}
    }
}
