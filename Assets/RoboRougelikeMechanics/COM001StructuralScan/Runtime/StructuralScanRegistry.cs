using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // Explicit lifecycle registration; no per-frame FindObjectsOfType.
    public sealed class StructuralScanRegistry : MonoBehaviour
    {
        private readonly List<IStructuralScanPart> parts = new List<IStructuralScanPart>();
        public IReadOnlyList<IStructuralScanPart> Parts { get { return parts; } }
        public int Count { get { return parts.Count; } }

        public bool Register(IStructuralScanPart part)
        {
            if (part == null || part.ScanPoint == null || parts.Contains(part)) return false;
            for (int i = 0; i < parts.Count; ++i)
                if (parts[i].OwnerKey == part.OwnerKey && parts[i].PartKey == part.PartKey) return false;
            parts.Add(part);
            return true;
        }
        public bool Unregister(IStructuralScanPart part) { return parts.Remove(part); }
        public void Clear() { parts.Clear(); }
    }
}
