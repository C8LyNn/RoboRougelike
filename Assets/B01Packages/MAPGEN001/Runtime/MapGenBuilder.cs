using System;
using UnityEngine;

namespace B01.MAPGEN001
{
    /// <summary>Runtime-only opt-in Unity primitive builder. No dependency on R1/R5 or main scene.</summary>
    public sealed class MapGenBuilder : MonoBehaviour
    {
        public MapResult Current { get; private set; }

        public MapResult Generate(MapRecipe recipe, int seed, MapGenConfig config, MapVehicleProfile vehicle)
        {
            // Validate and plan BEFORE changing an existing generated level.
            MapBlueprint p = MapGenPlanner.Plan(recipe, seed, config, vehicle);
            var root = new GameObject("MAPGEN001_" + recipe + "_" + seed);
            root.transform.SetParent(transform, false);
            try
            {
                for (int i = 0; i < p.Elements.Count; i++)
                {
                    MapElement spec = p.Elements[i];
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = spec.Kind + "_" + i;
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = spec.Position;
                    go.transform.localRotation = spec.Rotation;
                    go.transform.localScale = spec.Size;
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var block = new MaterialPropertyBlock();
                        Color c = spec.Kind == MapElementKind.Floor ? new Color(.28f, .32f, .34f) :
                            spec.Kind == MapElementKind.Boundary ? new Color(.24f, .24f, .27f) :
                            spec.Kind == MapElementKind.Ramp ? new Color(.75f, .54f, .23f) :
                            new Color(.48f, .38f, .30f);
                        block.SetColor("_BaseColor", c);
                        renderer.SetPropertyBlock(block);
                    }
                }
            }
            catch
            {
                root.SetActive(false);
                Release(root);
                throw;
            }
            var next = new MapResult {
                GeneratedRoot = root, Recipe = p.Recipe, Seed = p.Seed,
                GenerationVersion = p.GenerationVersion, Start = p.Start, Finish = p.Finish,
                Spawns = p.EnemySpawns.ToArray(), CheckpointDescriptors = p.Checkpoints.ToArray(),
                Bounds = p.Bounds, GeometryHash = p.GeometryHash, Diagnostics = p.Diagnostics
            };
            // Fully deactivate old collision geometry before postponing Destroy until end of frame.
            Clear();
            Current = next;
            Physics.SyncTransforms();
            return Current;
        }

        public void Clear()
        {
            if (Current == null) return;
            if (Current.GeneratedRoot != null)
            {
                Current.GeneratedRoot.SetActive(false);
                Release(Current.GeneratedRoot);
            }
            Current = null;
        }

        private static void Release(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        private void OnDestroy() { Clear(); }
    }
}
