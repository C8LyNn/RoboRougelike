using System;
using B01.MAPGEN001;
using UnityEngine;

namespace B01.MAPGENWORLD001
{
    /// <summary>V1.1.0 opt-in host contract. Width MUST be measured from the actual assembled robot.</summary>
    [Serializable]
    public sealed class MapWorldRequest
    {
        public MapRecipe Recipe = MapRecipe.CombatArena;
        public int Seed = 7;
        public MapGenConfig Config = MapGenConfig.Default;
        public MapVehicleProfile Vehicle = MapVehicleProfile.Light;
        public bool EnvelopeMeasured;
        public float ClearancePerSideMeters = 0.25f;
    }

    public enum MapWorldFailure
    {
        None, MissingRequest, UnmeasuredEnvelope, InvalidClearance,
        RejectedGeneratorConfig, InsufficientMainRouteClearance,
        InvalidWorldEndpoints, WorldBuildFailed, InvalidWorldResult,
        InvalidReplayReceipt, ReplayGeometryMismatch
    }

    public sealed class MapWorldSnapshot
    {
        public readonly int Revision;
        public readonly MapRecipe Recipe;
        public readonly int Seed;
        public readonly string GeometryHash;
        public readonly string ReplayReceipt;
        public readonly Vector3 Start;
        public readonly Vector3 Finish;
        private readonly Vector3[] checkpoints;
        private readonly Vector3[] enemySpawns;
        public Vector3[] Checkpoints => (Vector3[])checkpoints.Clone();
        public Vector3[] EnemySpawns => (Vector3[])enemySpawns.Clone();
        public readonly GameObject GeneratedRoot;

        internal MapWorldSnapshot(MapResult result, int revision, string replayReceipt)
        {
            Revision = revision;
            Recipe = result.Recipe;
            Seed = result.Seed;
            GeometryHash = result.GeometryHash;
            ReplayReceipt = replayReceipt;
            Start = result.Start;
            Finish = result.Finish;
            checkpoints = (Vector3[])(result.CheckpointDescriptors ?? new Vector3[0]).Clone();
            enemySpawns = (Vector3[])(result.Spawns ?? new Vector3[0]).Clone();
            GeneratedRoot = result.GeneratedRoot;
        }
    }

    public interface IMapWorldActivityListener
    {
        void OnWorldReady(MapWorldSnapshot world);
        void OnWorldCleared(MapWorldSnapshot previous);
    }
}
