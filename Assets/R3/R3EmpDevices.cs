using System.Collections.Generic;
using RoboRougelike.Mechanics.EMP;
using RoboRougelike.R1;
using RoboRougelike.WeaponIntegration;
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.R3
{
    // Both devices reuse the existing disruption receivers. Values are prototype tuning.
    public sealed class R3EmpDevices : MonoBehaviour
    {
        public const float EnergyPerCopy=15f, DurationPerUnit=3f, Cooldown=12f, TargetRange=30f, AreaRadius=9f;
        public const string TargetPool="emp-targeted",AreaPool="emp-area";
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        EmpEmitter area;
        bool poolsInitialized;
        public void Configure(R3VehicleAssembly owner)
        {
            assembly=owner;energy=GetComponent<VehicleEnergyBank>();
            energy?.Refresh();
            area=gameObject.AddComponent<EmpEmitter>();area.SetAutomaticSimulation(false);
            area.BindVehicle(transform,transform.GetInstanceID(),RRLHitRouter.Team(transform));area.SetOrigin(transform);
            area.SetEnergySource(energy);area.ConfigurePhysics(~0,~0,8192,512);
        }
        void EnsureInstalledPools()
        {
            if(assembly==null||poolsInitialized)return;
            foreach(var part in assembly.Parts)
            {
                if(part.Kind==R3PartKind.TargetedEmp)assembly.Model.Charges.EnsurePool(TargetPool,System.Math.Max(0,assembly.Model.Economy.DeviceBaseCharges));
                if(part.Kind==R3PartKind.AreaEmp)assembly.Model.Charges.EnsurePool(AreaPool,System.Math.Max(0,assembly.Model.Economy.DeviceBaseCharges));
            }
            poolsInitialized=true;
        }
        void Update(){if(PlayerCanUse())EnsureInstalledPools();}
        bool Ready(R3MountedPart p,R3PartKind kind)=>p!=null&&p.Kind==kind&&p.Operational&&
            p.gameObject.activeInHierarchy&&p.transform.IsChildOf(transform)&&!RRLEmpPartState.IsBlocked(p.gameObject);
        bool PlayerCanUse()
        {
            var run=R1RunController.Instance;
            return assembly!=null&&energy!=null&&run!=null&&run.Player!=null&&run.Player.gameObject==gameObject&&run.CanUseMovement;
        }
        int Units(R3PartKind kind,out float cost)
        {
            int units=0;cost=0;
            foreach(var part in assembly.Parts)if(Ready(part,kind))
            {units+=R3VehicleModel.TierEffectUnits(part.Tier);cost+=EnergyPerCopy*R3VehicleModel.TierActivationCost(part.Tier);}
            return units;
        }
        public string Status(R3PartKind kind)
        {
            if(assembly==null||energy==null)return "niedostępny";
            if(PlayerCanUse())EnsureInstalledPools();
            float remaining=(kind==R3PartKind.TargetedEmp?assembly.Model.TargetedEmpReadyAt:assembly.Model.AreaEmpReadyAt)-Time.time;
            if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
            int units=Units(kind,out float cost);if(units==0)return "niedostępny";
            string pool=kind==R3PartKind.TargetedEmp?TargetPool:AreaPool;
            int current=assembly.Model.Charges.Current(pool),maximum=assembly.Model.Charges.Maximum(pool);
            return current==0?"brak ładunków":!energy.CanSpend(cost)?"brak energii":$"gotowy • {current}/{maximum}";
        }
        bool HasDevice(R3PartKind kind){foreach(var part in assembly.Parts)if(Ready(part,kind))return true;return false;}
        bool CanRecharge(string pool,R3PartKind kind)=>HasDevice(kind)&&assembly.Model.Charges.Current(pool)<assembly.Model.Charges.Maximum(pool);
        public bool CanRecharge()=>assembly!=null&&(CanRecharge(TargetPool,R3PartKind.TargetedEmp)||CanRecharge(AreaPool,R3PartKind.AreaEmp));
        public void Recharge(int amount)
        {
            if(assembly==null)return;
            if(HasDevice(R3PartKind.TargetedEmp))assembly.Model.Charges.Recharge(TargetPool,amount);
            if(HasDevice(R3PartKind.AreaEmp))assembly.Model.Charges.Recharge(AreaPool,amount);
        }
        public bool TryTargeted()
        {
            var controller=GetComponent<P12PipelineSafeTankController>();
            if(controller==null)return false;
            return TryTargeted(controller.ModuleAimCollider());
        }
        public bool TryTargeted(Collider hit)
        {
            if(!PlayerCanUse()||Time.time<assembly.Model.TargetedEmpReadyAt||!RRLHitRouter.CanHit(hit,transform))return false;
            EnsureInstalledPools();
            var enemy=hit.GetComponentInParent<P12PipelineSafeEnemyTurret>();
            if(enemy==null||enemy.IsDefeated||Vector3.Distance(transform.position,hit.ClosestPoint(transform.position))>TargetRange)return false;
            int units=Units(R3PartKind.TargetedEmp,out float cost);if(units==0||assembly.Model.Charges.Current(TargetPool)<1)return false;
            RRLEmpPartState.RefreshEligibility();
            var receivers=enemy.GetComponentsInChildren<EmpReceiver>();
            var targets=new List<EmpReceiver>();
            foreach(var receiver in receivers)if(receiver.CanBeDisrupted&&receiver.VehicleRoot==enemy.transform&&receiver.Resistance<1f)targets.Add(receiver);
            if(targets.Count==0||!energy.TrySpend(cost))return false;
            assembly.Model.Charges.TrySpend(TargetPool,1);assembly.Model.TargetedEmpReadyAt=Time.time+Cooldown;
            foreach(var receiver in targets)if(receiver!=null)receiver.ApplyEmp(DurationPerUnit*units*(1-receiver.Resistance));
            return true;
        }
        public bool TryArea()
        {
            if(!PlayerCanUse()||Time.time<assembly.Model.AreaEmpReadyAt)return false;
            EnsureInstalledPools();
            int units=Units(R3PartKind.AreaEmp,out float cost);if(units==0||assembly.Model.Charges.Current(AreaPool)<1)return false;
            RRLEmpPartState.RefreshEligibility();
            area.Configure(AreaRadius*Mathf.Sqrt(units),DurationPerUnit*units,0,cost);
            if(!area.TryPulse(out _))return false;
            assembly.Model.Charges.TrySpend(AreaPool,1);assembly.Model.AreaEmpReadyAt=Time.time+Cooldown;return true;
        }
    }
}
