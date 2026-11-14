using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.StructuralScan;

namespace RoboRougelike.PackageIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLStructuralScannerBinding : MonoBehaviour,IRRLScanDevice,IRRLDeviceLifecycle
    {
        public const float BaseRange=60f,CooldownSeconds=8f,EnergyPerCopy=5f;
        public RRLDeviceBinding Device {get;private set;}
        public StructuralScanController Scanner {get;private set;}
        public RRLScanKind Kind=>RRLScanKind.Structural;
        public float CooldownLeft=>Scanner!=null?Scanner.CooldownLeft:0;
        StructuralScanRegistry registry;
        public void Bind(R3PartState state,Transform owner)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            registry=gameObject.AddComponent<StructuralScanRegistry>();Scanner=gameObject.AddComponent<StructuralScanController>();
            // Profile from the supplied PR4 production bridge, not the package24m/75deg default.
            var energy=owner.GetComponent<VehicleEnergyBank>();
            Scanner.Configure(new StructuralScanSettings{rangeMeters=BaseRange,fullConeDegrees=85,activationEnergy=energy!=null?EnergyPerCopy:0f});
            Scanner.Bind(transform,owner,owner.GetInstanceID(),RRLHitRouter.Team(owner),registry,energy);
            Scanner.RestoreCooldown(state.DeviceReadyAt-Time.time);Device.Damageable.Died+=RefreshAvailability;RefreshAvailability();
        }
        public void RefreshAvailability()
        {
            if(Scanner==null||Device==null)return;
            Scanner.SetOperational(isActiveAndEnabled&&Device.HardwareAvailable&&RRLScanWorld.InEncounter(R1RunController.Instance));
        }
        public bool TryActivate()
        {
            var controls=Device?.Owner!=null?Device.Owner.GetComponent<RRLScannerControls>():null;
            return controls!=null&&controls.Activate(RRLScanKind.Structural);
        }
        internal bool ActivateCombined(int units,float costMultiplier)
        {
            RefreshAvailability();if(Device==null||!Device.Available||Scanner==null||!Scanner.IsReady)return false;
            var energy=Device.Owner.GetComponent<VehicleEnergyBank>();
            Scanner.Configure(new StructuralScanSettings{rangeMeters=BaseRange*units,fullConeDegrees=85,activationEnergy=energy!=null?EnergyPerCopy*costMultiplier:0f});
            RRLScanWorld.Register(registry,R1RunController.Instance);
            if(!Scanner.TryScan())return false;
            Device.State.DeviceReadyAt=Time.time+Scanner.CooldownLeft;return true;
        }
        void OnEnable(){if(Scanner!=null&&Device?.State!=null)Scanner.RestoreCooldown(Device.State.DeviceReadyAt-Time.time);RefreshAvailability();}
        void Update()=>RefreshAvailability();
        void OnTransformParentChanged()=>RefreshAvailability();
        void OnDisable(){if(Scanner!=null)Scanner.SetOperational(false);}
        void OnDestroy(){if(Device!=null&&Device.Damageable!=null)Device.Damageable.Died-=RefreshAvailability;}
    }
}
