using System;
using System.Linq;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Mechanics.MOV005OmniDrive;
using RoboRougelike.Mechanics.MOV007;
using RoboRougelike.Mechanics.MOV008;

namespace RoboRougelike.MOVSeries
{
    public sealed partial class MOVSeriesVehicleAdapter
    {
        OmniDriveController omni;HoverVehicleController hover;CrawlerTrackController tracks;
        public float IndependentYaw {get;set;}
        public bool HasActiveDrive=>assembly!=null&&(Model.SelectedDrive switch {
            R3DriveKind.Wheels=>assembly.Parts.Any(p=>p.Kind==R3PartKind.Wheel&&Available(p)),
            R3DriveKind.Tracks=>assembly.Parts.Any(p=>p.Kind==R3PartKind.CrawlerTrack&&Available(p)),
            R3DriveKind.Omni=>assembly.Parts.Any(p=>p.Kind==R3PartKind.OmniDrive&&Available(p)),
            R3DriveKind.Hover=>assembly.Parts.Any(p=>p.Kind==R3PartKind.HoverEngine&&Available(p)),_=>false});
        void ConfigureDrives()
        {
            if(Model.Parts.Any(p=>p.Kind==R3PartKind.OmniDrive))
            {omni=gameObject.AddComponent<OmniDriveController>();omni.SetLimits(12,5);omni.SetCoasting(true);}
            if(Model.Parts.Any(p=>p.Kind==R3PartKind.CrawlerTrack))tracks=gameObject.AddComponent<CrawlerTrackController>();
            if(Model.Parts.Any(p=>p.Kind==R3PartKind.HoverEngine))
            {
                hover=gameObject.AddComponent<HoverVehicleController>();hover.forwardSpeedMetersPerSec=Model.Design.Speed;
                hover.strafeSpeedMetersPerSec=Model.Design.Speed*.8f;hover.yawDegreesPerSecond=Model.Design.TurnSpeed;
                hover.thrustNewtonsPerPoweredPod=700;hover.yawTorqueNmPerPoweredPod=450;
            }
            foreach(var part in assembly.Parts)
            {
                if(part.Kind!=R3PartKind.OmniDrive&&part.Kind!=R3PartKind.HoverEngine&&part.Kind!=R3PartKind.CrawlerTrack)continue;
                part.gameObject.AddComponent<MOVSeriesPartBinding>().Configure(this,part);
                bool tiltedMount=Vector3.Dot(part.transform.localRotation*Vector3.up,Vector3.up)<.999f;
                if(part.Kind==R3PartKind.OmniDrive)
                {
                    var m=part.gameObject.AddComponent<OmniDriveModule>();m.Configure(Model.Design.Speed,4,Model.Design.TurnSpeed,2);
                    m.SetAvailabilityFilter(()=>Available(part));m.GroundProbe.SetGroundFilter(IsTerrain);
                }
                else if(part.Kind==R3PartKind.HoverEngine)
                {
                    var m=part.gameObject.AddComponent<HoverLiftPod>();m.targetHeightMeters=Model.HoverHeightMeters;
                    m.SetUseMountAxes(tiltedMount);
                    m.nominalLoadKg=250;m.maximumLiftToNominalWeight=1.8f;m.springFrequencyHz=1.1f;m.dampingRatio=1;
                    m.SetAvailabilityFilter(()=>Available(part));m.SetGroundFilter(IsTerrain);
                }
                else
                {
                    var m=part.gameObject.AddComponent<CrawlerTrackModule>();m.SetSide(part.Cell.x<0?CrawlerTrackSide.Left:CrawlerTrackSide.Right);
                    m.SetUseMountAxes(tiltedMount);
                    m.ConfigureTraction(500,1200,1800);m.SetAvailabilityFilter(()=>Available(part));m.SetGroundFilter(IsTerrain);
                }
            }
            if(omni!=null)omni.RebuildModules();if(hover!=null)hover.RefreshModules();if(tracks!=null)tracks.RefreshModules();
            SetMotorLimits(tank.EffectiveForwardSpeed,tank.EffectiveReverseSpeed,tank.EffectiveTurnSpeed,false);
            GetComponent<RoboRougelike.MOV009Integration.MOV009VehicleAdapter>()?.ApplySettings();
        }
        void SyncDrives()
        {
            if(omni!=null)omni.SetOperational(CanControl&&Model.SelectedDrive==R3DriveKind.Omni);
            if(tracks!=null)tracks.SetDriveEnabled(CanControl&&Model.SelectedDrive==R3DriveKind.Tracks);
            if(hover!=null)hover.SetHoverMode(CanControl&&Model.SelectedDrive==R3DriveKind.Hover);
        }
        public bool SetDriveMode(R3DriveKind mode)
        {
            if(assembly==null||!Enum.IsDefined(typeof(R3DriveKind),mode))return false;
            structure.EvaluateStructure();
            var alive=assembly.Parts.Where(Available).Select(p=>State(p));if(!R3VehicleModel.HasMobility(alive,mode))return false;
            ClearDriveInput();Model.SelectedDrive=mode;SyncDrives();
            GetComponent<RoboRougelike.MOV009Integration.MOV009VehicleAdapter>()?.ApplySettings();return true;
        }
        static float Unit(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp(value,-1,1);
        public void SetDriveInput(Vector2 move,float yaw)
        {
            move=Vector2.ClampMagnitude(new Vector2(Unit(move.x),Unit(move.y)),1);yaw=Unit(yaw);
            if(!CanControl){ClearDriveInput();return;}
            if(Model.SelectedDrive==R3DriveKind.Omni&&omni!=null)omni.SetInput(move,yaw);
            if(Model.SelectedDrive==R3DriveKind.Tracks&&tracks!=null)tracks.SetInput(move.y,move.x);
            if(Model.SelectedDrive==R3DriveKind.Hover&&hover!=null){hover.SetMoveInput(move);hover.SetTurnInput(yaw);}
        }
        public bool TryDrive(float throttle,float steering,float forward,float reverse,float turn)
        {
            if(Model.SelectedDrive==R3DriveKind.Wheels)return false;
            SetMotorLimits(forward,reverse,turn,throttle<0);
            SetDriveInput(new Vector2(steering,throttle),IndependentYaw);return true;
        }
        static float PositiveLimit(float value,float maximum)=>float.IsNaN(value)||float.IsInfinity(value)?0:Mathf.Clamp(value,0,maximum);
        void SetMotorLimits(float forward,float reverse,float turn,bool backwards)
        {
            forward=PositiveLimit(forward,30);reverse=PositiveLimit(reverse,30);turn=PositiveLimit(turn,260);
            float speed=backwards?reverse:forward;
            foreach(var part in assembly.Parts)
                if(part!=null&&part.TryGetComponent<OmniDriveModule>(out var module))
                    module.Configure(speed,4,turn,2);
            if(hover!=null){hover.forwardSpeedMetersPerSec=speed;hover.strafeSpeedMetersPerSec=forward*.8f;hover.yawDegreesPerSecond=turn;}
            if(tracks!=null)tracks.SetDriveLimits(forward,reverse,turn);
        }
        public bool TryDriveWorld(Vector3 worldWish,float speed)
        {
            if(Model.SelectedDrive==R3DriveKind.Wheels)return false;
            SetMotorLimits(speed,speed,tank.EffectiveTurnSpeed,false);
            var local=transform.InverseTransformDirection(Vector3.ProjectOnPlane(worldWish,Vector3.up));
            var move=new Vector2(local.x,local.z);SetDriveInput(move,Unit(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg/45));return true;
        }
        void ClearDriveInput()
        {
            IndependentYaw=0;if(omni!=null)omni.SetInput(Vector2.zero,0);if(tracks!=null)tracks.SetInput(0,0);if(hover!=null)hover.ClearInput();
        }
    }
}
