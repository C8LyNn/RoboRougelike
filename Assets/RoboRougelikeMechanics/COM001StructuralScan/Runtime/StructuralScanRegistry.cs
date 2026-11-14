using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.Mechanics.StructuralScan
{
    // Explicit lifecycle registration; no per-frame FindObjectsOfType.
    public sealed class StructuralScanRegistry : MonoBehaviour
    {
        private readonly List<IStructuralScanPart> parts = new List<IStructuralScanPart>();
        private readonly HashSet<long> identities = new HashSet<long>();
        private readonly Dictionary<IStructuralScanPart,long> registrations = new Dictionary<IStructuralScanPart,long>();
        public IReadOnlyList<IStructuralScanPart> Parts { get { return parts; } }
        public int Count { get { return parts.Count; } }

        public bool Register(IStructuralScanPart part)
        {
            if (part == null || part.ScanPoint == null || registrations.ContainsKey(part)) return false;
            long identity=((long)part.OwnerKey<<32)|(uint)part.PartKey;
            if(!identities.Add(identity))return false;
            registrations.Add(part,identity);
            parts.Add(part);
            return true;
        }
        public bool Unregister(IStructuralScanPart part)
        {
            if(part==null||!registrations.TryGetValue(part,out long identity))return false;
            registrations.Remove(part);identities.Remove(identity);return parts.Remove(part);
        }
        public void Clear() { parts.Clear();identities.Clear();registrations.Clear(); }
    }
}
