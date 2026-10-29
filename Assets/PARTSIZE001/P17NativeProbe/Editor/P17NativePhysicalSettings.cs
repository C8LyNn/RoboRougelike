// Dedicated Unity MonoScript with a filename matching ScriptableObject type.
// Necessary for reliable CreateAssetMenu script association and editor GUID binding.
// This is a settings asset only; real prefab references are assigned by the native QA receiver.
using UnityEngine;

namespace B01.PARTSIZE001.P17Probe
{
    [CreateAssetMenu(fileName="P17NativePhysicalSettings",menuName="B01/P17 native physical probe settings")]
    public sealed class P17NativePhysicalSettings : ScriptableObject
    {
        public GameObject armorPrefab;
        public GameObject corePrefab;
        public GameObject crawlerTrackPrefab;
        public GameObject gunPrefab;
        public GameObject hoverEnginePrefab;
        public GameObject wheelPrefab;

        public GameObject ForKind(string kind)
        {
            switch(kind)
            {
                case "Armor":return armorPrefab;
                case "Core":return corePrefab;
                case "CrawlerTrack":return crawlerTrackPrefab;
                case "Gun":return gunPrefab;
                case "HoverEngine":return hoverEnginePrefab;
                case "Wheel":return wheelPrefab;
                default:return null;
            }
        }
    }

}
