using System;
using System.Collections.Generic;

namespace B01.SecureReattachV2
{
    public enum DockResult
    {
        Allowed, NotDetached, InvalidInput, OutOfReach, QuerySaturated, Obstructed, TransactionRejected
    }

    public readonly struct DockHit
    {
        public readonly bool IsOwnBody;
        public readonly bool IsTrigger;
        public readonly bool SupportedShape;
        public readonly double DepthMetres;
        public DockHit(bool own, bool trigger, bool supported, double depthMetres)
        {
            IsOwnBody = own;
            IsTrigger = trigger;
            SupportedShape = supported;
            DepthMetres = depthMetres;
        }
    }

    // Source-owned, fail-closed gate. Runtime supplies real Physics overlap + penetration.
    public static class SecureDockPolicy
    {
        public const int AllLayers = -1;
        public const int Capacity = 128;

        public static DockResult Check(
            bool detached, bool dynamicBody, double distance, double maximumReach,
            int physicsLayerMask, int count, int capacity, double maxPenetration,
            IReadOnlyList<DockHit> hits)
        {
            if (!detached) return DockResult.NotDetached;
            if (!dynamicBody || physicsLayerMask != AllLayers ||
                !Finite(distance) || distance < 0 ||
                !Finite(maximumReach) || maximumReach <= 0 || maximumReach > 50 ||
                !Finite(maxPenetration) || maxPenetration < 0 || maxPenetration > .05 ||
                capacity < 4 || capacity > 1024 ||
                count < 0 || count > capacity ||
                hits == null || hits.Count != count)
                return DockResult.InvalidInput;
            if (count >= capacity) return DockResult.QuerySaturated;
            if (distance > maximumReach) return DockResult.OutOfReach;
            for (int i = 0; i < hits.Count; i++)
            {
                DockHit hit = hits[i];
                if (hit.IsOwnBody || hit.IsTrigger) continue;
                if (!hit.SupportedShape || !Finite(hit.DepthMetres) ||
                    hit.DepthMetres < 0 || hit.DepthMetres > maxPenetration)
                    return DockResult.Obstructed;
            }
            return DockResult.Allowed;
        }
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    }

    // Every supported PUBLIC reattach entrypoint must use the same inspector.
    // The mutating commit callback is PRIVATE to the concrete Unity host.
    public sealed class GuardedDockCommand
    {
        private readonly Func<int,DockResult> inspect;
        private readonly Func<int,bool> commit;
        private bool executing;
        public DockResult LastResult { get; private set; } = DockResult.InvalidInput;
        public GuardedDockCommand(Func<int,DockResult> inspector, Func<int,bool> commitAction)
        {
            inspect = inspector ?? throw new ArgumentNullException(nameof(inspector));
            commit = commitAction ?? throw new ArgumentNullException(nameof(commitAction));
        }
        public bool Reattach(int index) => Execute(index);
        public bool TrySafeReattach(int index) => Execute(index);
        public bool ReattachFromGui(int index) => Execute(index);
        private bool Execute(int index)
        {
            if (executing || index < 0 || index > 1)
            {
                LastResult = DockResult.InvalidInput;
                return false;
            }
            executing = true;
            try
            {
                LastResult = inspect(index);
                if (LastResult != DockResult.Allowed) return false;
                try
                {
                    if (!commit(index))
                    {
                        LastResult = DockResult.TransactionRejected;
                        return false;
                    }
                    return true;
                }
                catch
                {
                    LastResult = DockResult.TransactionRejected;
                    throw;
                }
            }
            finally { executing = false; }
        }
    }
}
