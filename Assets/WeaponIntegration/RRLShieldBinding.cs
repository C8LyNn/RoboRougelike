using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.Defenses.DirectionalShield;

namespace RoboRougelike.WeaponIntegration
{
    [DisallowMultipleComponent]
    public sealed class RRLShieldBinding : MonoBehaviour,IRRLDeviceLifecycle
    {
        public const float BaseCapacity=100f,BaseRegenerationPerSecond=12f;
        public void RefreshAvailability(){RefreshOperational();}
        public RRLDeviceBinding Device {get;private set;}
        public DirectionalShieldGenerator Generator {get;private set;}
        RRLShieldDamageFilter filter;
        R3MountedBonusEffects bonuses;
        float appliedMultiplier;
        int tierUnits;
        public void Bind(R3PartState state,Transform owner)
        {
            Device=GetComponent<RRLDeviceBinding>();if(Device==null)Device=gameObject.AddComponent<RRLDeviceBinding>();
            Device.Bind(state,owner);
            Generator=gameObject.AddComponent<DirectionalShieldGenerator>();
            Generator.Bind(Device.Body,transform);Generator.SetAutomaticSimulation(false);
            tierUnits=R3VehicleModel.TierEffectUnits(state.Tier);
            bonuses=owner.GetComponent<R3MountedBonusEffects>();
            ConfigureCapacity(bonuses!=null?bonuses.ShieldDurabilityMultiplier:1f);
            if(state.DefenseCharge>=0)
            {
                float previousCapacity=state.DefenseCapacity>0?state.DefenseCapacity:BaseCapacity*tierUnits;
                double fraction=System.Math.Min(1d,state.DefenseCharge/(double)previousCapacity);
                Generator.RestoreState((float)(Generator.Capacity*fraction),state.DefenseRechargeAge);
            }
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
            Device.State.DefenseCapacity=Generator.Capacity;
            Device.State.DefenseRechargeAge=Generator.SecondsSinceAbsorption;
        }
        void Update()
        {
            RefreshOperational();RefreshCapacity();if(Generator!=null)Generator.Advance(Time.deltaTime);CaptureState();
        }
        void ConfigureCapacity(float multiplier)
        {
            appliedMultiplier=multiplier;
            float capacity=(float)System.Math.Min(float.MaxValue,(double)BaseCapacity*tierUnits*multiplier);
            Generator.Configure(capacity,capacity,BaseRegenerationPerSecond*tierUnits,3,60,9);
        }
        void RefreshCapacity()
        {
            if(Generator==null)return;
            float multiplier=bonuses!=null?bonuses.ShieldDurabilityMultiplier:1f;
            if(multiplier==appliedMultiplier)return;
            float previousCapacity=Generator.Capacity;
            double fraction=previousCapacity>0?Generator.Charge/(double)previousCapacity:0;
            float age=Generator.SecondsSinceAbsorption;
            ConfigureCapacity(multiplier);
            Generator.RestoreState((float)(Generator.Capacity*fraction),age);
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
