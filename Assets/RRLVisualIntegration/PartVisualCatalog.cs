using System;
using System.Collections.Generic;
using UnityEngine;
using RoboRougelike.R3;
using RoboRougelike.R4;

namespace RoboRougelike.VisualIntegration
{
    public enum PartVisualSide { Any, Left, Right }

    [CreateAssetMenu(menuName = "RoboRougelike/Part Visual Catalog", fileName = "RRLPartVisualCatalog")]
    public sealed class PartVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public R3PartKind kind;
            public bool filterWeapon;
            public R4WeaponKind weapon;
            public PartVisualSide side;
            public GameObject prefab;
            public Vector3 localOffset;
            public Vector3 localEuler;
            public Vector3 localScale = Vector3.one;
            [Tooltip("Follow existing logical wheel/weapon pivot, without inheriting its non-uniform scale.")]
            public bool followLogicalPivot = true;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Resolve(R3PartKind kind, R4WeaponKind weapon, Vector3Int cell)
        {
            var desiredSide = cell.x < 0 ? PartVisualSide.Left :
                              cell.x > 0 ? PartVisualSide.Right : PartVisualSide.Any;
            Entry generic = null;
            foreach (var entry in entries)
            {
                if (entry == null || entry.prefab == null || entry.kind != kind) continue;
                if (entry.filterWeapon && entry.weapon != weapon) continue;
                if (entry.side == desiredSide) return entry;
                if (entry.side == PartVisualSide.Any && generic == null) generic = entry;
            }
            return generic;
        }
    }
}
