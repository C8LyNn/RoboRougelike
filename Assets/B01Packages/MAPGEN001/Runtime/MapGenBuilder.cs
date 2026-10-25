using System;
using UnityEngine;

namespace B01.MAPGEN001
{
    /// <summary>Runtime-only opt-in Unity primitive builder. No dependency on R1/R5 or main scene.</summary>
    public sealed class MapGenBuilder : MonoBehaviour
    {
        public MapResult Current { get; private set; }

        // A runtime Shader.Find is Editor-only reliable: the URP Lit shader can
        // be stripped from an otherwise minimal Windows Player. The package
        // owns an URP-tagged shader under Resources so Unity includes it in
        // both Player and Editor, without modifying GraphicsSettings or Codex.
        // One material belongs to this builder; blocks share it.
        private const string RetainedShaderPath = "MAPGEN001_BlocksURP";
        private Material _generatedBlockMaterial;
        private Material GetGeneratedBlockMaterial()
        {
            if (_generatedBlockMaterial != null) return _generatedBlockMaterial;
            Shader shader = Resources.Load<Shader>(RetainedShaderPath);
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException(
                    "MAPGEN001_RETAINED_URP_SHADER_MISSING_OR_UNSUPPORTED: " +
                    RetainedShaderPath + " (import the complete MAPGEN001/Resources folder)");
            _generatedBlockMaterial = new Material(shader) {
                name = "MAPGEN001_Generated_URP_Blocks_Runtime"
            };
            return _generatedBlockMaterial;
        }

        public MapResult Regenerate(MapRecipe recipe, int seed, MapGenConfig config, MapVehicleProfile vehicle) =>
            Generate(recipe, seed, config, vehicle);

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
                        renderer.sharedMaterial = GetGeneratedBlockMaterial();
                        var block = new MaterialPropertyBlock();
                        Color c = spec.Kind == MapElementKind.Floor ? new Color(.28f, .32f, .34f) :
                            spec.Kind == MapElementKind.Boundary ? new Color(.24f, .24f, .27f) :
                            spec.Kind == MapElementKind.Ramp ? new Color(.75f, .54f, .23f) :
                            new Color(.48f, .38f, .30f);
                        block.SetColor("_BaseColor", c);
                        renderer.SetPropertyBlock(block);
                    }
                }
            // Public points are WORLD coordinates, even if the opt-in host is offset or rotated.
            Physics.SyncTransforms();
            var spawns = new Vector3[p.EnemySpawns.Count];
            var checkpoints = new Vector3[p.Checkpoints.Count];
            for (int i = 0; i < spawns.Length; i++) spawns[i] = root.transform.TransformPoint(p.EnemySpawns[i]);
            for (int i = 0; i < checkpoints.Length; i++) checkpoints[i] = root.transform.TransformPoint(p.Checkpoints[i]);
            var colliders = root.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) throw new InvalidOperationException("MAPGEN001_NO_COLLIDERS");
            Bounds worldBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++) worldBounds.Encapsulate(colliders[i].bounds);
            var next = new MapResult {
                GeneratedRoot = root, Recipe = p.Recipe, Seed = p.Seed,
                GenerationVersion = p.GenerationVersion,
                Start = root.transform.TransformPoint(p.Start),
                Finish = root.transform.TransformPoint(p.Finish),
                Spawns = spawns, CheckpointDescriptors = checkpoints,
                Bounds = worldBounds, GeometryHash = p.GeometryHash, Diagnostics = p.Diagnostics
            };
            // Fully deactivate old collision geometry before postponing Destroy until end of frame.
            // Finalize only after all geometry, bounds and metadata succeeded.
            // No engine calls after publishing Current: avoid a partial commit.
            Clear();
            Current = next;
            return Current;
            }
            catch
            {
                // Any failure before commit must preserve the previous scene.
                root.SetActive(false);
                Release(root);
                throw;
            }
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

        private void OnDestroy()
        {
            Clear();
            if (_generatedBlockMaterial == null) return;
            if (Application.isPlaying) Destroy(_generatedBlockMaterial);
            else DestroyImmediate(_generatedBlockMaterial);
            _generatedBlockMaterial = null;
        }
    }
}
