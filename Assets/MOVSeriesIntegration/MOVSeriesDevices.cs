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
            int boostCount=Mathf.Max(1,Model.Parts.Count(p=>p.Kind==R3PartKind.Booster));
            int gyroCount=Mathf.Max(1,Model.Parts.Count(p=>p.Kind==R3PartKind.Gyroscope));
            jumpProbe=new GameObject("Jump ground probe").transform;jumpProbe.SetParent(transform,false);
            foreach(var part in assembly.Parts)
            {
                if(part.Kind<R3PartKind.Booster||part.Kind>R3PartKind.Gyroscope)continue;
                var binding=part.gameObject.AddComponent<MOVSeriesPartBinding>();binding.Configure(this,part);
                switch(part.Kind)
                {
                    case R3PartKind.Booster:
                        var booster=part.gameObject.AddComponent<VehicleBooster>();booster.Bind(body,transform);
                        booster.Configure(Mathf.Clamp(Model.BoosterThrustNewtons,500,6000),Mathf.Clamp(Model.BoosterDurationSeconds,.2f,2),5,10f/boostCount,20);
                        booster.SetAvailabilityFilter(()=>Available(part));boosters.Add(booster);break;
                    case R3PartKind.JumpDrive:
                        var jump=part.gameObject.AddComponent<JumpDrive>();jump.BindToVehicle(body);jump.SetGroundProbe(jumpProbe);
                        jump.Configure(new JumpDriveSettings{TargetJumpHeightMeters=1.2f,SupportedMassKg=500,MaxUpwardSpeedMps=5.2f,
                            CooldownSeconds=6,GroundProbeDistanceMeters=.3f});
                        jump.SetAvailabilityFilter(()=>Available(part));jump.SetGroundFilter(IsTerrain);jumps.Add(jump);break;
                    case R3PartKind.Gyroscope:
                        var gyro=part.gameObject.AddComponent<GyroStabilizer>();gyro.BindToVehicle(body);
                        gyro.Configure(15f/gyroCount,6.5f/gyroCount,8f/gyroCount);gyro.SetAvailabilityFilter(()=>Available(part));gyros.Add(gyro);break;
                }
            }
        }
        void SyncDevices()
        {
            foreach(var g in gyros)if(g!=null)g.SetRequestedActive(Model.GyroEnabled&&CanControl);
        }
        public bool TryBoost()
        {
            if(!PrepareAction())return false;bool success=false;
            foreach(var b in boosters)
            {
                if(b==null)continue;var state=State(b.GetComponent<R3MountedPart>());
                if(Time.time<state.DeviceReadyAt||!b.TryActivate())continue;
                state.DeviceReadyAt=Time.time+b.RemainingBoostSeconds+5;success=true;
            }
            Feedback=success?"Booster aktywny":"Booster niedostępny / cooldown";return success;
        }
        public bool TryJump()
        {
            if(!PrepareAction()||jumps.Count==0)return false;
            // One common gate prevents sequential airborne jumps through different mounts.
            if(Model.Parts.Any(p=>p.Kind==R3PartKind.JumpDrive&&p.DeviceReadyAt>Time.time))return false;
            float bottom=float.PositiveInfinity;
            foreach(var c in GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger&&c.attachedRigidbody==body)bottom=Mathf.Min(bottom,c.bounds.min.y);
            jumpProbe.position=new Vector3(body.worldCenterOfMass.x,bottom+.05f,body.worldCenterOfMass.z);
            bool success=false;
            foreach(var jump in jumps)if(jump!=null&&jump.TryActivate()==JumpDriveResult.Activated)success=true;
            if(success)foreach(var p in Model.Parts.Where(p=>p.Kind==R3PartKind.JumpDrive))p.DeviceReadyAt=Time.time+6;
            Feedback=success?"Skok":"Skok wymaga podłoża i sprawnego modułu";return success;
        }
        public bool ToggleGyro()
        {
            if(!PrepareAction()||!gyros.Any(g=>g!=null&&g.IsOperational))return false;
            Model.GyroEnabled=!Model.GyroEnabled;SyncDevices();return true;
        }
        public void ApplyDeviceSettings()
        {
            int count=Mathf.Max(1,Model.Parts.Count(p=>p.Kind==R3PartKind.Booster));
            foreach(var b in boosters)if(b!=null)b.Configure(Model.BoosterThrustNewtons,Model.BoosterDurationSeconds,5,10f/count,20);
            foreach(var pod in GetComponentsInChildren<RoboRougelike.Mechanics.MOV007.HoverLiftPod>())pod.targetHeightMeters=Model.HoverHeightMeters;
            SyncDevices();
        }
        public bool Boosting=>boosters.Any(b=>b!=null&&b.IsBoosting);
        public int OperationalCount(R3PartKind kind)=>assembly.Parts.Count(p=>p.Kind==kind&&Available(p));
        public float ReadyIn(R3PartKind kind)
        {
            var parts=assembly.Parts.Where(p=>p.Kind==kind&&Available(p)).Select(State).ToArray();
            if(parts.Length==0)return float.PositiveInfinity;
            float gate=kind==R3PartKind.JumpDrive?Model.Parts.Where(p=>p.Kind==kind).Max(p=>p.DeviceReadyAt):parts.Min(p=>p.DeviceReadyAt);
            return Mathf.Max(0,gate-Time.time);
        }
    }
}
