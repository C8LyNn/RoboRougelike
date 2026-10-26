using B01.MAPGEN001;
using UnityEngine;

namespace B01.MAPGENWORLD001
{
    /// <summary>Optional demo: attach to a new empty GameObject in a disposable URP scene.</summary>
    public sealed class MapWorldDemo : MonoBehaviour, IMapWorldActivityListener
    {
        public int Seed = 7;
        public bool Course;
        public bool GenerateOnStart = true;
        public bool Heavy;
        private MapGenWorldAdapter adapter;

        private void Start()
        {
            adapter = GetComponent<MapGenWorldAdapter>();
            if (adapter == null) adapter = gameObject.AddComponent<MapGenWorldAdapter>();
            adapter.Register(this);
            if (GenerateOnStart) CreateWorld();
        }
        [ContextMenu("Generate / Regenerate WORLD001")]
        public void CreateWorld()
        {
            if (adapter == null) adapter = GetComponent<MapGenWorldAdapter>();
            if (adapter == null) adapter = gameObject.AddComponent<MapGenWorldAdapter>();
            var vehicle = Heavy ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
            vehicle.Width = Heavy ? 5.180f : 2.980f; // demo example: real measured rig from W12
            var request = new MapWorldRequest {
                Recipe = Course ? MapRecipe.ObstacleCourse : MapRecipe.CombatArena,
                Seed = Seed, Config = MapGenConfig.Default,
                Vehicle = vehicle, EnvelopeMeasured = true,
                ClearancePerSideMeters = .25f
            };
            if (!adapter.TryRegenerate(request, out var world, out var failure))
                Debug.LogWarning("MAPGENWORLD001 rejected world: " + failure, this);
            else Debug.Log("MAPGENWORLD001 " + world.GeometryHash + " start=" +
                           world.Start + " finish=" + world.Finish, this);
        }
        [ContextMenu("Clear WORLD001")]
        public void ClearWorld() { if (adapter != null) adapter.ClearWorld(); }
        public void OnWorldReady(MapWorldSnapshot world)
        {
            // Example only. Host must spawn the authoritative R3/R6 robot using its own API.
            Debug.Log("WORLD001 activity ready revision=" + world.Revision, this);
        }
        public void OnWorldCleared(MapWorldSnapshot old)
        {
            Debug.Log("WORLD001 activity cleared revision=" + old.Revision, this);
        }
        private void OnDestroy() { if (adapter != null) adapter.Unregister(this); }
    }
}
