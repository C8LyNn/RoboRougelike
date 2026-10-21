using UnityEngine;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R3
{
    public sealed class R3MountedPart : MonoBehaviour
    {
        public Vector3Int Cell;
        public R3PartKind Kind;
        public RoboRougelike.R4.R4WeaponKind Weapon;
        public string Id;
        public float NextFireTime;
        public P12PipelineSafeDamageable Damageable;
        public P12_PipelineSafePlayerBlock Block;
        public Transform GunPivot, Muzzle;
        public bool Operational => Damageable != null && !Damageable.IsDead && Block != null && !Block.DetachedFromVehicle;
    }
}
