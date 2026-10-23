using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Defenses.DirectionalShield;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLShieldBinding : MonoBehaviour
    {
        public RRLDeviceBinding Device {get;private set;}
        public DirectionalShieldGenerator Generator {get;private set;}
        RRLShieldDamageFilter filter;
        public void Bind(R3PartState state,Transform owner)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();
            Device.Bind(state,owner);
            Generator=gameObject.AddComponent<DirectionalShieldGenerator>();
            Generator.Bind(Device.Body,transform);Generator.SetAutomaticSimulation(false);
            if(state.DefenseCharge>=0)Generator.RestoreState(state.DefenseCharge,state.DefenseRechargeAge);
            filter=owner.GetComponent<RRLShieldDamageFilter>();
            if(filter==null)filter=owner.gameObject.AddComponent<RRLShieldDamageFilter>();
            filter.Register(this);Generator.OnAbsorbed+=OnAbsorbed;Device.Damageable.Died+=OnLost;
            RefreshOperational();CaptureState();
        }
        public void RefreshOperational()
        {if(Generator!=null)Generator.SetOperational(isActiveAndEnabled&&Device!=null&&Device.Available);}
        public void CaptureState()
        {
            if(Device?.State==null||Generator==null)return;
            Device.State.DefenseCharge=Generator.Charge;
            Device.State.DefenseRechargeAge=Generator.SecondsSinceAbsorption;
        }
        void Update()
        {
            RefreshOperational();if(Generator!=null)Generator.Advance(Time.deltaTime);CaptureState();
        }
        void OnAbsorbed(float amount)=>CaptureState();
        void OnLost(){if(Generator!=null)Generator.SetOperational(false);CaptureState();}
        void OnDisable(){if(Generator!=null)Generator.SetOperational(false);}
        void OnDestroy()
        {
            if(Generator!=null)Generator.OnAbsorbed-=OnAbsorbed;
            if(Device!=null&&Device.Damageable!=null)Device.Damageable.Died-=OnLost;
            if(filter!=null)filter.Unregister(this);
        }
    }
}
