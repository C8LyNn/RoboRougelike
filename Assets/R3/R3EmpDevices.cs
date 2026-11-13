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
        R3VehicleAssembly assembly;
        VehicleEnergyBank energy;
        EmpEmitter area;
        public void Configure(R3VehicleAssembly owner)
        {
            assembly=owner;energy=GetComponent<VehicleEnergyBank>();
            area=gameObject.AddComponent<EmpEmitter>();area.SetAutomaticSimulation(false);
            area.BindVehicle(transform,transform.GetInstanceID(),RRLHitRouter.Team(transform));area.SetOrigin(transform);
            area.SetEnergySource(energy);area.ConfigurePhysics(~0,~0,8192,512);
        }
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
            float remaining=(kind==R3PartKind.TargetedEmp?assembly.Model.TargetedEmpReadyAt:assembly.Model.AreaEmpReadyAt)-Time.time;
            if(remaining>0)return $"odnowienie {Mathf.CeilToInt(remaining)} s";
            int units=Units(kind,out float cost);return units==0?"niedostępny":energy.CanSpend(cost)?"gotowy":"brak energii";
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
            var enemy=hit.GetComponentInParent<P12PipelineSafeEnemyTurret>();
            if(enemy==null||enemy.IsDefeated||Vector3.Distance(transform.position,hit.ClosestPoint(transform.position))>TargetRange)return false;
            int units=Units(R3PartKind.TargetedEmp,out float cost);if(units==0)return false;
            RRLEmpPartState.RefreshEligibility();
            var receivers=enemy.GetComponentsInChildren<EmpReceiver>();
            var targets=new List<EmpReceiver>();
            foreach(var receiver in receivers)if(receiver.CanBeDisrupted&&receiver.VehicleRoot==enemy.transform&&receiver.Resistance<1f)targets.Add(receiver);
            if(targets.Count==0||!energy.TrySpend(cost))return false;
            assembly.Model.TargetedEmpReadyAt=Time.time+Cooldown;
            foreach(var receiver in targets)if(receiver!=null)receiver.ApplyEmp(DurationPerUnit*units*(1-receiver.Resistance));
            return true;
        }
        public bool TryArea()
        {
            if(!PlayerCanUse()||Time.time<assembly.Model.AreaEmpReadyAt)return false;
            int units=Units(R3PartKind.AreaEmp,out float cost);if(units==0)return false;
            RRLEmpPartState.RefreshEligibility();
            area.Configure(AreaRadius*Mathf.Sqrt(units),DurationPerUnit*units,0,cost);
            if(!area.TryPulse(out _))return false;
            assembly.Model.AreaEmpReadyAt=Time.time+Cooldown;return true;
        }
    }
}
