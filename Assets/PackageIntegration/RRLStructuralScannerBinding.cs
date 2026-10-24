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
            Scanner.Configure(new StructuralScanSettings{rangeMeters=60,fullConeDegrees=85});
            Scanner.Bind(transform,owner,owner.GetInstanceID(),RRLHitRouter.Team(owner),registry);
            Scanner.RestoreCooldown(state.DeviceReadyAt-Time.time);Device.Damageable.Died+=RefreshAvailability;RefreshAvailability();
        }
        public void RefreshAvailability()
        {
            if(Scanner==null||Device==null)return;
            Scanner.SetOperational(isActiveAndEnabled&&Device.HardwareAvailable&&RRLScanWorld.InEncounter(R1RunController.Instance));
        }
        public bool TryActivate()
        {
            RefreshAvailability();if(Device==null||!Device.Available||Scanner==null||!Scanner.IsReady)return false;
            RRLScanWorld.Register(registry,R1RunController.Instance);
            if(!Scanner.TryScan())return false;
            Device.State.DeviceReadyAt=Time.time+Scanner.CooldownLeft;return true;
        }
        void Update()=>RefreshAvailability();
        void OnTransformParentChanged()=>RefreshAvailability();
        void OnDisable(){if(Scanner!=null)Scanner.SetOperational(false);}
        void OnDestroy(){if(Device!=null&&Device.Damageable!=null)Device.Damageable.Died-=RefreshAvailability;}
    }
}
