using System;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R4_1
{
    [Serializable] public struct R4WeaponArtEntry
    {
        public R4WeaponKind Kind;
        public GameObject Model;
    }
    // The catalog is a reference table only; game code decides when to use it.
    public sealed class R4WeaponArtCatalog : ScriptableObject
    {
        public R4WeaponArtEntry[] Models=Array.Empty<R4WeaponArtEntry>();
        public GameObject ModelFor(R4WeaponKind weapon)
        {
            if(Models==null)return null;
            foreach(var entry in Models)if(entry.Kind==weapon)return entry.Model;
            return null;
        }
    }
}
