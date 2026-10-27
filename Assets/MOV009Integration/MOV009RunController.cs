using System;
using UnityEngine;
using RoboRougelike.MOV009Integration;
using RoboRougelike.Mechanics.MOV009;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        public bool ConfigureSuspension(bool enabled,ActiveSuspensionSupportMode mode,float offsetMeters)
        {
            if(Transitioning||VehicleModel==null||Progress==null||
                (Progress.State!=R1RunState.Garage&&Progress.State!=R1RunState.TestDrive)||
                !Enum.IsDefined(typeof(ActiveSuspensionSupportMode),mode))return false;
            VehicleModel.SuspensionEnabled=enabled;VehicleModel.SuspensionMode=mode;
            VehicleModel.SuspensionHeightOffset=Mathf.Clamp(SuspensionMath.Finite(offsetMeters),-.35f,.35f);
            Player?.GetComponent<MOV009VehicleAdapter>()?.ApplySettings();
            return true;
        }
    }
}
