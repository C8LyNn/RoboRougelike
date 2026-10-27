using System;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.MOVSeries;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        MOVSeriesVehicleAdapter Movement=>Player?.GetComponent<MOVSeriesVehicleAdapter>();
        public bool CanUseMovement=>!Transitioning&&!Paused&&Progress!=null&&Player!=null&&Player.Controller.GameplayInputEnabled&&
            (Progress.State==R1RunState.Combat||Progress.State==R1RunState.TestDrive);
        bool CanConfigureMovement=>!Transitioning&&VehicleModel!=null&&Progress!=null&&
            (Progress.State==R1RunState.Garage||(Paused&&(Progress.State==R1RunState.TestDrive||Progress.State==R1RunState.Combat)));
        public bool UseBooster()=>CanUseMovement&&Movement?.TryBoost()==true;
        public bool UseJump()=>CanUseMovement&&Movement?.TryJump()==true;
        public bool UseBlink()=>CanUseMovement&&Movement?.TryBlink(Player.transform.forward)==true;
        public bool ToggleGyro()=>CanUseMovement&&Movement?.ToggleGyro()==true;
        public bool UseGrapple()
        {
            if(!CanUseMovement||Movement==null)return false;
            return Movement.TryGrapple(Player.Controller.ModuleAimPoint()-Movement.GrappleAimOrigin);
        }
        public bool ConfigureDrive(R3DriveKind mode)
        {
            if(!CanConfigureMovement||!Enum.IsDefined(typeof(R3DriveKind),mode))return false;
            if(Movement!=null)return Movement.SetDriveMode(mode);
            if(mode!=R3DriveKind.Wheels||!R3VehicleModel.HasMobility(VehicleModel.Parts,mode))return false;
            VehicleModel.SelectedDrive=mode;return true;
        }
        static float FiniteSetting(float value,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:value;
        public bool ConfigureMovement(float thrust,float duration,bool gyro,float hoverHeight)
        {
            if(!CanConfigureMovement)return false;
            VehicleModel.BoosterThrustNewtons=Mathf.Clamp(FiniteSetting(thrust,3000),500,6000);
            VehicleModel.BoosterDurationSeconds=Mathf.Clamp(FiniteSetting(duration,1),.2f,2);
            VehicleModel.GyroEnabled=gyro;VehicleModel.HoverHeightMeters=Mathf.Clamp(FiniteSetting(hoverHeight,1),.7f,1.6f);
            Movement?.ApplyDeviceSettings();return true;
        }
    }
}
