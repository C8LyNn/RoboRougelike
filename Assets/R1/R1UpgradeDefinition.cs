using UnityEngine;

namespace RoboRougelike.R1
{
    public enum R1UpgradeEffect { Hull, Damage, Reload, Mobility, Repair, Core }

    [CreateAssetMenu(menuName = "RoboRougelike/Ulepszenie")]
    public sealed class R1UpgradeDefinition : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public R1UpgradeEffect Effect;
        [Range(0, 1)] public float Amount = 0.2f;
    }
}
