using UnityEngine;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RoboRougelike.WeaponIntegration;
using RoboRougelike.Mechanics.SEN001;

namespace RoboRougelike.PackageIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLPulseScannerBinding : MonoBehaviour,IRRLScanDevice,IRRLDeviceLifecycle
    {
        public RRLDeviceBinding Device {get;private set;}
        public ScannerPulse Pulse {get;private set;}
        public RRLScanKind Kind=>RRLScanKind.Pulse;
        public float CooldownLeft=>Pulse!=null?Pulse.RemainingCooldown:0;
        public void Bind(R3PartState state,Transform owner)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();Device.Bind(state,owner);
            Pulse=gameObject.AddComponent<ScannerPulse>();Pulse.Configure(20,5,3,1<<RRLPulseSignal.SignalLayer);
            Pulse.RestoreCooldown(state.DeviceReadyAt-Time.time);Device.Damageable.Died+=RefreshAvailability;RefreshAvailability();
        }
        public void RefreshAvailability()
        {if(Pulse!=null&&Device!=null)Pulse.SetOperational(isActiveAndEnabled&&Device.HardwareAvailable&&RRLScanWorld.InEncounter(R1RunController.Instance));}
        public bool TryActivate()
        {
            RefreshAvailability();if(Device==null||!Device.Available||Pulse==null||!Pulse.Ready)return false;
            RRLPulseSignal.Prepare(R1RunController.Instance);Physics.SyncTransforms();
            if(Pulse.TryPulse(transform.position)<0)return false;
            Device.State.DeviceReadyAt=Time.time+Pulse.RemainingCooldown;return true;
        }
        void OnEnable(){if(Pulse!=null&&Device?.State!=null)Pulse.RestoreCooldown(Device.State.DeviceReadyAt-Time.time);RefreshAvailability();}
        void Update()=>RefreshAvailability();
        void OnTransformParentChanged()=>RefreshAvailability();
        void OnDisable(){if(Pulse!=null)Pulse.SetOperational(false);}
        void OnDestroy(){if(Device!=null&&Device.Damageable!=null)Device.Damageable.Died-=RefreshAvailability;}
    }
}
