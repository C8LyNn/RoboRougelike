using System;
using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    [Serializable] public struct R4MovementArtEntry
    {
        public R3PartKind Kind;
        public GameObject Model;
    }
    public sealed class R4MovementArtCatalog : ScriptableObject
    {
        public R4MovementArtEntry[] Models=Array.Empty<R4MovementArtEntry>();
        public AnimationClip CrawlerTravel;
        public GameObject SideWheel;
        public GameObject SideWheelSlick;
        public GameObject SideWheelLarge;
        public GameObject ModelFor(R3PartKind kind)
        {
            if(Models==null)return null;
            foreach(var entry in Models)if(entry.Kind==kind)return entry.Model;
            return null;
        }
    }
}
