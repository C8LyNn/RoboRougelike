using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using UnityEngine;

namespace RoboRougelike.R3
{
    // Each type has one shared activation/cooldown; copies supply the strength and cost.
    [DisallowMultipleComponent]
    public sealed class R3OffensiveDevices : MonoBehaviour
    {
        sealed class ActiveDevice
        {
            public readonly R3PartKind Kind;
            public readonly List<R3MountedPart> Copies=new List<R3MountedPart>();
            public float EndsAt;
            public ActiveDevice(R3PartKind kind){Kind=kind;}
        }
        readonly ActiveDevice overdrive=new ActiveDevice(R3PartKind.Overdrive);
        readonly ActiveDevice rapidFire=new ActiveDevice(R3PartKind.RapidFire);
        readonly ActiveDevice projectileBoost=new ActiveDevice(R3PartKind.DoubleShotOverdrive);
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        R3MountedBonusEffects effects;
        public string OverdriveStatus=>Status(overdrive);
        public string RapidFireStatus=>Status(rapidFire);
        public string DoubleShotOverdriveStatus=>Status(projectileBoost);
        public int AdditionalProjectileUnits
        {
            get
            {
                if(assembly==null||projectileBoost.Copies.Count==0||Time.time>=projectileBoost.EndsAt)return 0;
                long units=0;foreach(var part in projectileBoost.Copies)if(Ready(part,projectileBoost.Kind))units+=R3VehicleModel.TierEffectUnits(part.Tier);
                return (int)System.Math.Min(int.MaxValue,units*System.Math.Max(0,assembly.Model.Economy.DoubleShotOverdriveExtraPerUnit));
            }
        }

        public void Configure(R3VehicleAssembly owner)
        {assembly=owner;energy=GetComponent<VehicleEnergyBank>();effects=GetComponent<R3MountedBonusEffects>();}
        static float NonNegative(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Max(0,value);
        bool Ready(R3MountedPart part,R3PartKind kind)=>part!=null&&part.Kind==kind&&part.Operational&&
            part.gameObject.activeInHierarchy&&part.transform.IsChildOf(transform)&&!RRLEmpPartState.IsBlocked(part.gameObject);
        float ReadyAt(ActiveDevice device)=>device.Kind==R3PartKind.Overdrive?assembly.Model.OverdriveReadyAt:
            device.Kind==R3PartKind.RapidFire?assembly.Model.RapidFireReadyAt:assembly.Model.DoubleShotOverdriveReadyAt;
        void SetReadyAt(ActiveDevice device,float at)
        {if(device.Kind==R3PartKind.Overdrive)assembly.Model.OverdriveReadyAt=at;else if(device.Kind==R3PartKind.RapidFire)assembly.Model.RapidFireReadyAt=at;else assembly.Model.DoubleShotOverdriveReadyAt=at;}
        float Cost(ActiveDevice device)=>NonNegative(device.Kind==R3PartKind.Overdrive?assembly.Model.Economy.OverdriveEnergyPerCopy:device.Kind==R3PartKind.RapidFire?assembly.Model.Economy.RapidFireEnergyPerCopy:assembly.Model.Economy.DoubleShotOverdriveEnergyPerCopy);
        float Duration(ActiveDevice device)=>NonNegative(device.Kind==R3PartKind.Overdrive?assembly.Model.Economy.OverdriveDuration:device.Kind==R3PartKind.RapidFire?assembly.Model.Economy.RapidFireDuration:assembly.Model.Economy.DoubleShotOverdriveDuration);
        float Cooldown(ActiveDevice device)=>NonNegative(device.Kind==R3PartKind.Overdrive?assembly.Model.Economy.OverdriveCooldown:device.Kind==R3PartKind.RapidFire?assembly.Model.Economy.RapidFireCooldown:assembly.Model.Economy.DoubleShotOverdriveCooldown);
        float Bonus(ActiveDevice device)=>NonNegative(device.Kind==R3PartKind.Overdrive?assembly.Model.Economy.OverdriveBonusPerUnit:assembly.Model.Economy.RapidFireReductionPerUnit);
        void SetContribution(ActiveDevice device,float value)
        {if(device.Kind==R3PartKind.Overdrive)effects.SetOverdriveIncrease(value);else effects.SetRapidFireReduction(value);}
        string Status(ActiveDevice device)
        {
            if(assembly==null||energy==null)return "niedostępny";
            if(device.Copies.Count>0&&Time.time<device.EndsAt)return "aktywny";
            float remaining=ReadyAt(device)-Time.time;
            if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
            float cost=0;int count=0;
            foreach(var part in assembly.Parts)if(Ready(part,device.Kind))
            {count++;cost+=Cost(device)*R3VehicleModel.TierActivationCost(part.Tier);}
            if(count==0)return "niedostępny";
            return energy.CanSpend(cost)?"gotowy":"brak energii";
        }

        public int TryActivate()=>Activate(overdrive);
        public int TryActivateRapidFire()=>Activate(rapidFire);
        public int TryActivateDoubleShotOverdrive()=>Activate(projectileBoost);
        int Activate(ActiveDevice device)
        {
            var run=R1RunController.Instance;
            if(assembly==null||energy==null||effects==null||run==null||run.Player==null||
               run.Player.gameObject!=gameObject||!run.CanUseMovement||Time.time<device.EndsAt)return 0;
            if(device.Copies.Count>0)Clear(device);
            if(Time.time<ReadyAt(device))return 0;
            float duration=Duration(device),cost=0;
            if(duration<=0)return 0;
            foreach(var part in assembly.Parts)
                if(Ready(part,device.Kind)){device.Copies.Add(part);cost+=Cost(device)*R3VehicleModel.TierActivationCost(part.Tier);}
            if(device.Copies.Count==0||!energy.TrySpend(cost)){device.Copies.Clear();return 0;}
            device.EndsAt=Time.time+duration;SetReadyAt(device,Time.time+Cooldown(device));
            ApplyContribution(device);return device.Copies.Count;
        }
        void Update(){Tick(overdrive);Tick(rapidFire);Tick(projectileBoost);}
        void Tick(ActiveDevice device)
        {
            if(device.Copies.Count==0)return;
            if(Time.time>=device.EndsAt){Clear(device);return;}
            ApplyContribution(device);
        }
        void ApplyContribution(ActiveDevice device)
        {
            int units=0;
            foreach(var part in device.Copies)if(Ready(part,device.Kind))units+=R3VehicleModel.TierEffectUnits(part.Tier);
            if(units==0){Clear(device);return;}
            if(device.Kind==R3PartKind.DoubleShotOverdrive)
                return;
            SetContribution(device,Bonus(device)*units);
        }
        void Clear(ActiveDevice device)
        {device.Copies.Clear();device.EndsAt=0;if(device.Kind!=R3PartKind.DoubleShotOverdrive&&effects!=null)SetContribution(device,0);}
        void OnDisable(){Clear(overdrive);Clear(rapidFire);Clear(projectileBoost);}
    }
}
