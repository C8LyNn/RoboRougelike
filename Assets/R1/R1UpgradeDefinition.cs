using UnityEngine;

namespace RoboRougelike.R1
{
    public enum R1UpgradeEffect { Hull, Damage, Reload, Mobility, Repair, Core }
    // Zero is the historical additive bonus in existing assets and saves.
    public enum R1ModifierOperation { Increased, Reduced, More, Less }

    [CreateAssetMenu(menuName = "RoboRougelike/Ulepszenie")]
    public sealed class R1UpgradeDefinition : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public R1UpgradeEffect Effect;
        public R1ModifierOperation Operation;
        [Range(0, 1)] public float Amount = 0.2f;
    }
}
