using UnityEngine;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R1
{
    // Serialized component references are remapped by Unity when the intact vehicle is cloned.
    public sealed class R1VehicleState : MonoBehaviour
    {
        [SerializeField] private P12PipelineSafeTankController controller;
        [SerializeField] private P12PipelineSafeVehicleStructure structure;
        [SerializeField] private P12PipelineSafeVehicleModuleSystem modules;
        [SerializeField] private Transform cameraPivot;
        public P12PipelineSafeTankController Controller => controller;
        public P12PipelineSafeVehicleStructure Structure => structure;
        public P12PipelineSafeVehicleModuleSystem Modules => modules;
        public Transform CameraPivot => cameraPivot;
        public P12PipelineSafeDamageable Core
        {
            get
            {
                foreach (var block in GetComponentsInChildren<P12_PipelineSafePlayerBlock>())
                    if (block.IsCore) return block.GetComponent<P12PipelineSafeDamageable>();
                return null;
            }
        }
        public float HealthFraction
        {
            get
            {
                float hp = 0, max = 0;
                foreach (var part in GetComponentsInChildren<P12PipelineSafeDamageable>())
                { if (!part.IsDead) { hp += part.CurrentHp; max += part.MaxHp; } }
                return max > 0 ? hp / max : 0;
            }
        }
        public string DefeatReason
        {
            get
            {
                if (structure == null || !structure.CoreAlive) return "Rdzeń pojazdu został zniszczony.";
                if (modules == null || !modules.WeaponOnline) return "Utracono uzbrojenie.";
                var movement=GetComponent<RoboRougelike.MOVSeries.MOVSeriesVehicleAdapter>();
                if(movement!=null&&movement.Model.SelectedDrive!=RoboRougelike.R3.R3DriveKind.Wheels)
                {if(!movement.HasActiveDrive)return "Utracono wybrany napęd.";}
                else if (modules.ActiveWheelCount == 0) return "Utracono wszystkie koła.";
                return null;
            }
        }
        public void Bind(P12PipelineSafeTankController tank, Transform pivot)
        {
            controller = tank; cameraPivot = pivot;
            structure = tank.GetComponent<P12PipelineSafeVehicleStructure>();
            modules = tank.GetComponent<P12PipelineSafeVehicleModuleSystem>();
        }
        public void Apply(R1RunStats stats)
        {
            controller.SetRunModifiers(stats.Damage, stats.FireInterval, stats.Mobility);
            foreach (var part in GetComponentsInChildren<P12PipelineSafeDamageable>())
            {
                var block = part.GetComponent<P12_PipelineSafePlayerBlock>();
                float factor = stats.Hull * (block != null && block.IsCore ? stats.Core : 1);
                part.SetDurabilityMultiplier(factor);
            }
        }
        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            controller.SetGameplayInputEnabled(false);
            var body = GetComponent<Rigidbody>();
            body.position = position; body.rotation = rotation;
            Physics.SyncTransforms();
        }
    }
}
