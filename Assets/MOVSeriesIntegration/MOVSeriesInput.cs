using UnityEngine;
using UnityEngine.InputSystem;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.PackageIntegration;

namespace RoboRougelike.MOVSeries
{
    public sealed class MOVSeriesInput : MonoBehaviour
    {
        MOVSeriesVehicleAdapter owner;
        public void Bind(MOVSeriesVehicleAdapter adapter)=>owner=adapter;
        void Update()
        {
            if(owner!=null)owner.IndependentYaw=0;
            var run=R1RunController.Instance;
            if(run==null||run.Player==null||run.Player.gameObject!=gameObject||!run.CanUseMovement)return;
            var keys=Keyboard.current;if(keys==null)return;
            // Z/X/C/V are reserved for the shared deployable slots.
            if(owner!=null)owner.IndependentYaw=(keys.periodKey.isPressed?1:0)-(keys.commaKey.isPressed?1:0);
            var slots=run.VehicleModel.GetActiveSlots();
            var pressed=new[]{keys.digit1Key.wasPressedThisFrame,keys.digit2Key.wasPressedThisFrame,
                keys.digit3Key.wasPressedThisFrame,keys.digit4Key.wasPressedThisFrame};
            for(int i=0;i<4;i++)if(pressed[i]&&slots[i].HasValue)Activate(run,slots[i].Value);
            var deployables=run.VehicleModel.GetDeployableSlots();
            var deployPressed=new[]{keys.zKey.wasPressedThisFrame,keys.xKey.wasPressedThisFrame,keys.cKey.wasPressedThisFrame,keys.vKey.wasPressedThisFrame};
            for(int i=0;i<4;i++)if(deployPressed[i]&&deployables[i].HasValue)GetComponent<R3DeployableService>()?.TryDeploy(deployables[i].Value);
            if(keys.gKey.wasPressedThisFrame)run.ToggleGyro();
        }
        void Activate(R1RunController run,R3PartKind kind)
        {
            switch(kind)
            {
                case R3PartKind.Booster:run.UseBooster();break;
                case R3PartKind.JumpDrive:run.UseJump();break;
                case R3PartKind.Blink:run.UseBlink();break;
                case R3PartKind.MagneticGrapple:run.UseGrapple();break;
                case R3PartKind.RepairPulse:GetComponent<R3RepairPulse>()?.TryActivate();break;
                case R3PartKind.Overdrive:GetComponent<R3OffensiveDevices>()?.TryActivate();break;
                case R3PartKind.RapidFire:GetComponent<R3OffensiveDevices>()?.TryActivateRapidFire();break;
                case R3PartKind.DoubleShotOverdrive:GetComponent<R3OffensiveDevices>()?.TryActivateDoubleShotOverdrive();break;
                case R3PartKind.TargetedEmp:GetComponent<R3EmpDevices>()?.TryTargeted();break;
                case R3PartKind.AreaEmp:GetComponent<R3EmpDevices>()?.TryArea();break;
                case R3PartKind.RechargePulse:GetComponent<R3RechargePulse>()?.TryActivate();break;
                case R3PartKind.StructuralScanner:GetComponent<RRLScannerControls>()?.Activate(RRLScanKind.Structural);break;
                case R3PartKind.ScannerPulse:GetComponent<RRLScannerControls>()?.Activate(RRLScanKind.Pulse);break;
            }
        }
    }
}
