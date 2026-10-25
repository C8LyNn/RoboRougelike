using System;
using System.Collections.Generic;
using B01.MAPGEN001;
using UnityEngine;

namespace B01.MAPGENWORLD001
{
    /// <summary>
    /// Opt-in lifecycle adapter. Producer MAPGEN001 alone builds and validates map geometry.
    /// Receiver owns actual R3/R6 spawn, movement, race/boss activity and further PhysX checks.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapGenBuilder))]
    public sealed class MapGenWorldAdapter : MonoBehaviour
    {
        private MapGenBuilder builder;
        private readonly List<IMapWorldActivityListener> listeners = new List<IMapWorldActivityListener>();
        private int revision;
        public MapWorldSnapshot Current { get; private set; }
        public MapWorldFailure LastFailure { get; private set; }
        public event Action<MapWorldSnapshot> WorldReady;
        public event Action<MapWorldSnapshot> WorldCleared;

        private void Awake() { builder = GetComponent<MapGenBuilder>(); }

        public void Register(IMapWorldActivityListener listener)
        {
            if (listener != null && !listeners.Contains(listener)) listeners.Add(listener);
        }
        public void Unregister(IMapWorldActivityListener listener) { listeners.Remove(listener); }

        public bool TryGenerate(MapWorldRequest request, out MapWorldSnapshot snapshot,
            out MapWorldFailure failure)
        {
            snapshot = null;
            if (!MapWorldPolicy.TryPlan(request, out MapBlueprint plan, out failure))
            {
                LastFailure = failure;
                return false; // Atomic negative input: the previous live map stays intact.
            }
            if (builder == null) builder = GetComponent<MapGenBuilder>();
            if (builder == null) { failure = LastFailure = MapWorldFailure.WorldBuildFailed; return false; }
            MapResult built;
            try { built = builder.Generate(request.Recipe, request.Seed, request.Config, request.Vehicle); }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
                failure = LastFailure = MapWorldFailure.WorldBuildFailed;
                return false;
            }
            // Check a generated result without recalculating MAPGEN's geometry or mutating the vehicle.
            var previous = Current;
            if (built == null || built.GeneratedRoot == null ||
                !built.GeneratedRoot.activeInHierarchy ||
                built.GeometryHash != plan.GeometryHash ||
                !ValidPoint(built.Start) || !ValidPoint(built.Finish) ||
                (built.CheckpointDescriptors == null) || (built.Spawns == null))
            {
                builder.Clear(); // Reject unsafe/unknown live state; never return a false SUCCESS.
                Current = null;
                if (previous != null) Notify(previous, false);
                failure = LastFailure = MapWorldFailure.InvalidWorldResult;
                return false;
            }
            var old = previous;
            snapshot = new MapWorldSnapshot(built, ++revision);
            Current = snapshot;
            LastFailure = failure = MapWorldFailure.None;
            if (old != null) Notify(old, false);
            Notify(snapshot, true);
            return true;
        }

        public bool TryRegenerate(MapWorldRequest request, out MapWorldSnapshot snapshot,
            out MapWorldFailure failure) => TryGenerate(request, out snapshot, out failure);

        public void ClearWorld()
        {
            if (builder == null) builder = GetComponent<MapGenBuilder>();
            var old = Current;
            if (builder != null) builder.Clear();
            Current = null;
            LastFailure = MapWorldFailure.None;
            if (old != null) Notify(old, false);
        }

        private static bool ValidPoint(Vector3 p) =>
            !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) &&
            !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);

        private void Notify(MapWorldSnapshot world, bool ready)
        {
            // One broken optional game-activity consumer cannot revert a map already committed
            // by the producer. Exception isolation is explicit; no counterfeit atomic rollback.
            var callbacks = ready ? WorldReady : WorldCleared;
            if (callbacks != null)
                foreach (Delegate callback in callbacks.GetInvocationList())
                    try { ((Action<MapWorldSnapshot>)callback)(world); }
                    catch (Exception ex) { Debug.LogException(ex, this); }
            foreach (var listener in listeners.ToArray())
                try { if (ready) listener.OnWorldReady(world); else listener.OnWorldCleared(world); }
                catch (Exception ex) { Debug.LogException(ex, this); }
        }

        private void OnDestroy() { ClearWorld(); }
    }
}
