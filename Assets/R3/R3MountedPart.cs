using UnityEngine;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R3
{
    public sealed class R3MountedPart : MonoBehaviour
    {
        public Vector3Int Cell;
        public R3PartKind Kind;
        public int Tier=1;
        public R3WheelStyle WheelStyle;
        public RoboRougelike.R4.R4WeaponKind Weapon;
        public string Id;
        // One physical reward module carries its own independently switchable stat modifier.
        public RoboRougelike.R1.R1UpgradeEffect UpgradeEffect;
        public RoboRougelike.R1.R1ModifierOperation UpgradeOperation;
        public RoboRougelike.R1.R1BonusScope UpgradeScope;
        public float UpgradeAmount;
        public float NextFireTime;
        // Guards a zero or sub-clock interval without changing normal cooldowns.
        internal float LastConventionalShotTime=float.NegativeInfinity;
        DeviceFireSchedule basicCadence;
        internal DeviceFireSchedule BasicCadence=>basicCadence??=new DeviceFireSchedule();
        internal void StopBasicCadence()
        {if(basicCadence!=null){basicCadence.Observe(Time.time,0,false);basicCadence.TakeAll();}}
        public P12PipelineSafeDamageable Damageable;
        public P12_PipelineSafePlayerBlock Block;
        public Transform GunPivot, Muzzle;
        public bool Operational => Damageable != null && !Damageable.IsDead && Block != null && !Block.DetachedFromVehicle;
        internal event System.Action EligibilityChanged;
        void OnEnable()=>EligibilityChanged?.Invoke();
        void OnDisable(){StopBasicCadence();EligibilityChanged?.Invoke();}
        void OnTransformParentChanged()=>EligibilityChanged?.Invoke();
    }
}
