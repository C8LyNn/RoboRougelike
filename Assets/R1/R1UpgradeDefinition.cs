using UnityEngine;

namespace RoboRougelike.R1
{
    public enum R1UpgradeEffect { Hull, Damage, Reload, Mobility, Repair, Core }
    // Zero is the historical additive bonus in existing assets and saves.
    public enum R1ModifierOperation { Increased, Reduced, More, Less }
    // Older rewards and checkpoints have no scope and remain global.
    public enum R1BonusScope { Global, BasicCannon, Drones, Turrets, Traps, BlastDamage, PiercingDamage }

    [CreateAssetMenu(menuName = "RoboRougelike/Ulepszenie")]
    public sealed class R1UpgradeDefinition : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public R1UpgradeEffect Effect;
        public R1ModifierOperation Operation;
        public R1BonusScope Scope;
        [Range(0, 1)] public float Amount = 0.2f;
    }
}
