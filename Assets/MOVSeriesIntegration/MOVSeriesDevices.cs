using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.Booster;
using RoboRougelike.Mechanics.MOV002JumpDrive;
using RoboRougelike.Mechanics.MOV004GyroscopicStabilizer;

namespace RoboRougelike.MOVSeries
{
    public sealed partial class MOVSeriesVehicleAdapter
    {
        readonly List<VehicleBooster> boosters=new List<VehicleBooster>();
        readonly List<JumpDrive> jumps=new List<JumpDrive>();
        readonly List<GyroStabilizer> gyros=new List<GyroStabilizer>();
        Transform jumpProbe;
        void ConfigureDevices()
        {
            jumpProbe=new GameObject("Jump ground probe").transform;jumpProbe.SetParent(transform,false);
            foreach(var part in assembly.Parts)
            {
                if(part.Kind<R3PartKind.Booster||part.Kind>R3PartKind.Gyroscope)continue;
                var binding=part.gameObject.AddComponent<MOVSeriesPartBinding>();binding.Configure(this,part);
                switch(part.Kind)
                {
                    case R3PartKind.Booster:
                        var booster=part.gameObject.AddComponent<VehicleBooster>();booster.Bind(body,part.transform);
                        ConfigureBooster(booster);
                        booster.SetEnergySource(GetComponent<VehicleEnergyBank>());
                        booster.SetAvailabilityFilter(()=>Available(part));boosters.Add(booster);break;
                    case R3PartKind.JumpDrive:
                        var jump=part.gameObject.AddComponent<JumpDrive>();jump.BindToVehicle(body);jump.SetGroundProbe(jumpProbe);
                        jump.Configure(new JumpDriveSettings{TargetJumpHeightMeters=1.2f,SupportedMassKg=500,MaxUpwardSpeedMps=5.2f,
                            CooldownSeconds=6,GroundProbeDistanceMeters=.3f,EnergyCost=VehicleEnergyCosts.Jump});
                        jump.SetEnergySource(GetComponent<VehicleEnergyBank>());
                        jump.SetAvailabilityFilter(()=>Available(part));jump.SetGroundFilter(IsTerrain);jumps.Add(jump);break;
                    case R3PartKind.Gyroscope:
                        var gyro=part.gameObject.AddComponent<GyroStabilizer>();gyro.BindToVehicle(body);
                        int gyroUnits=R3VehicleModel.TierEffectUnits(part.Tier);
                        gyro.Configure(15f*gyroUnits,6.5f*gyroUnits,8f*gyroUnits);gyro.SetAvailabilityFilter(()=>Available(part));gyros.Add(gyro);break;
                }
            }
        }
        void SyncDevices()
        {
            foreach(var g in gyros)if(g!=null)g.SetRequestedActive(Model.GyroEnabled&&CanControl);
        }
        static int BoosterTier(VehicleBooster booster)=>booster.GetComponent<R3MountedPart>()?.Tier??1;
        static float BoosterEnergy(VehicleBooster booster)=>VehicleEnergyCosts.BoosterActivation*R3VehicleModel.TierActivationCost(BoosterTier(booster));
        void ConfigureBooster(VehicleBooster booster)
        {
            float thrust=Mathf.Clamp(Model.BoosterThrustNewtons,500,6000)*R3VehicleModel.TierEffectUnits(BoosterTier(booster));
            booster.Configure(thrust,Mathf.Clamp(Model.BoosterDurationSeconds,.2f,2),5,0,0,BoosterEnergy(booster));
        }
        public bool TryBoost()
        {
            if(!PrepareAction())return false;
            if(Model.BoosterReadyAt>Time.time||Model.Parts.Any(p=>p.Kind==R3PartKind.Booster&&p.DeviceReadyAt>Time.time))
            {Feedback="Booster: trwa odnowienie";return false;}
            var eligible=boosters.Where(b=>b!=null&&b.isActiveAndEnabled&&b.IsOperational).ToArray();
            var energy=GetComponent<VehicleEnergyBank>();float cost=eligible.Sum(BoosterEnergy);
            if(eligible.Length==0||eligible.Any(b=>!b.IsReady)||energy==null||!energy.CanSpend(cost))
            {Feedback="Booster niedostępny / brak energii";return false;}
            var started=new List<VehicleBooster>();
            foreach(var b in eligible)
            {
                if(!b.TryActivate())
                {
                    foreach(var previous in started)
                        ConfigureBooster(previous);
                    energy.Refund(started.Sum(BoosterEnergy));
                    Feedback="Booster niedostępny";return false;
                }
                started.Add(b);
            }
            Model.BoosterReadyAt=Time.time+started.Max(b=>b.RemainingBoostSeconds)+5;
            foreach(var state in Model.Parts)if(state.Kind==R3PartKind.Booster)state.DeviceReadyAt=Model.BoosterReadyAt;
            Feedback="Booster aktywny";return true;
        }
        public bool TryJump()
        {
            if(!PrepareAction()||jumps.Count==0)return false;
            // One common gate prevents sequential airborne jumps through different mounts.
            if(Model.JumpReadyAt>Time.time||Model.Parts.Any(p=>p.Kind==R3PartKind.JumpDrive&&p.DeviceReadyAt>Time.time))return false;
            var eligible=jumps.Where(j=>j!=null&&j.isActiveAndEnabled&&j.IsOperational).ToArray();
            var energy=GetComponent<VehicleEnergyBank>();
            float cost=eligible.Sum(j=>VehicleEnergyCosts.Jump*R3VehicleModel.TierActivationCost(j.GetComponent<R3MountedPart>().Tier));
            if(eligible.Length==0||energy==null||!energy.CanSpend(cost)){Feedback="Skok niedostępny / brak energii";return false;}
            float bottom=float.PositiveInfinity;
            foreach(var c in GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger&&c.attachedRigidbody==body)bottom=Mathf.Min(bottom,c.bounds.min.y);
            if(float.IsInfinity(bottom))return false;
            jumpProbe.position=new Vector3(body.worldCenterOfMass.x,bottom+.05f,body.worldCenterOfMass.z);
            // One physical impulse and one atomic payment for the working group.
            // Height grows linearly per copy; upward speed follows its square root.
            var primary=eligible[0];float units=eligible.Sum(j=>R3VehicleModel.TierEffectUnits(j.GetComponent<R3MountedPart>().Tier));
            primary.Configure(new JumpDriveSettings{TargetJumpHeightMeters=1.2f*units,SupportedMassKg=500,
                MaxUpwardSpeedMps=5.2f*Mathf.Sqrt(units),CooldownSeconds=6,GroundProbeDistanceMeters=.3f,EnergyCost=cost});
            bool success=primary.TryActivate()==JumpDriveResult.Activated;
            if(success)foreach(var jump in eligible.Skip(1))jump.GetComponent<RoboRougelike.R4_1.R4JumpArt>()?.NotifySharedActivation();
            if(success){Model.JumpReadyAt=Time.time+6;foreach(var p in Model.Parts.Where(p=>p.Kind==R3PartKind.JumpDrive))p.DeviceReadyAt=Model.JumpReadyAt;}
            Feedback=success?"Skok":"Skok wymaga podłoża i sprawnego modułu";return success;
        }
        public bool ToggleGyro()
        {
            if(!PrepareAction()||!gyros.Any(g=>g!=null&&g.IsOperational))return false;
            Model.GyroEnabled=!Model.GyroEnabled;SyncDevices();return true;
        }
        public void ApplyDeviceSettings()
        {
            foreach(var b in boosters)if(b!=null)ConfigureBooster(b);
            foreach(var pod in GetComponentsInChildren<RoboRougelike.Mechanics.MOV007.HoverLiftPod>())pod.targetHeightMeters=Model.HoverHeightMeters;
            SyncDevices();
        }
        public bool Boosting=>boosters.Any(b=>b!=null&&b.IsBoosting);
        public int OperationalCount(R3PartKind kind)=>assembly.Parts.Count(p=>p.Kind==kind&&Available(p));
        public float ReadyIn(R3PartKind kind)
        {
            var parts=assembly.Parts.Where(p=>p.Kind==kind&&Available(p)).Select(State).ToArray();
            if(parts.Length==0)return float.PositiveInfinity;
            float gate=kind==R3PartKind.Booster?Mathf.Max(Model.BoosterReadyAt,Model.Parts.Where(p=>p.Kind==kind).Max(p=>p.DeviceReadyAt)):
                kind==R3PartKind.JumpDrive?Mathf.Max(Model.JumpReadyAt,Model.Parts.Where(p=>p.Kind==kind).Max(p=>p.DeviceReadyAt)):
                kind==R3PartKind.Blink?Mathf.Max(Model.BlinkReadyAt,Model.Parts.Where(p=>p.Kind==kind).Max(p=>p.DeviceReadyAt)):parts.Min(p=>p.DeviceReadyAt);
            return Mathf.Max(0,gate-Time.time);
        }
    }
}
