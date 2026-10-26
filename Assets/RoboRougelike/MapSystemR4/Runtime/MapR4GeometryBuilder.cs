using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    public sealed class MapR4BuildResult
    {
        public readonly Dictionary<string, GameObject> Edges = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        public readonly Dictionary<string, Transform> Anchors = new Dictionary<string, Transform>(StringComparer.Ordinal);
        public readonly List<Mesh> OwnedMeshes = new List<Mesh>();
        public readonly List<Material> OwnedMaterials = new List<Material>();
        public GameObject Root;
    }

    // Editor/runtime blockout: static non-convex MeshColliders, no impact on vehicle Rigidbody.
    public static class MapR4GeometryBuilder
    {
        public static MapR4BuildResult Build(MapR4MapData map, Transform parent, Material roadMaterial = null, Material nodeMaterial = null)
        {
            var errors = MapR4GraphValidator.Validate(map);
            if (errors.Count > 0) throw new ArgumentException("Map invalid: " + string.Join("; ", errors));
            var result = new MapR4BuildResult();
            result.Root = new GameObject("MAP_R4_" + map.mapId);
            if (parent != null) result.Root.transform.SetParent(parent, false);
            if (roadMaterial == null)
            {
                roadMaterial = CreateDebugMaterial(new Color(0.57f, 0.61f, 0.68f));
                if (roadMaterial != null) result.OwnedMaterials.Add(roadMaterial);
            }
            if (nodeMaterial == null)
            {
                nodeMaterial = CreateDebugMaterial(new Color(0.36f, 0.46f, 0.51f));
                if (nodeMaterial != null) result.OwnedMaterials.Add(nodeMaterial);
            }
            var nodes = new Dictionary<string, MapR4NodeData>(StringComparer.Ordinal);
            foreach (var node in map.nodes)
            {
                nodes[node.id] = node;
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pad.name = "Node_" + node.id;
                pad.transform.SetParent(result.Root.transform, false);
                pad.transform.localPosition = node.position + Vector3.down * 0.25f;
                pad.transform.localScale = new Vector3(node.padWidth, 0.5f, node.padWidth);
                var renderer = pad.GetComponent<Renderer>();
                if (renderer != null && nodeMaterial != null) renderer.sharedMaterial = nodeMaterial;
            }

            foreach (var edge in map.edges)
            {
                var a = nodes[edge.fromNode].position;
                var b = nodes[edge.toNode].position;
                // R5: smooth grade and optional cubic centerline, preserving R4 input format.
                var mesh = MapR5RoadMesh.Build(a, b, edge);
                result.OwnedMeshes.Add(mesh);
                var road = new GameObject("Road_" + edge.id);
                road.transform.SetParent(result.Root.transform, false);
                road.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = road.AddComponent<MeshRenderer>();
                if (roadMaterial != null) renderer.sharedMaterial = roadMaterial;
                var collider = road.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                result.Edges.Add(edge.id, road);
            }

            foreach (var anchor in map.anchors)
            {
                var go = new GameObject("Anchor_" + anchor.id);
                go.transform.SetParent(result.Root.transform, false);
                go.transform.localPosition = nodes[anchor.nodeId].position + anchor.offset;
                result.Anchors.Add(anchor.id, go.transform);
            }
            return result;
        }

        private static Material CreateDebugMaterial(Color color)
        {
            // Sandbox fallback. For a production build assign shader-referenced Material assets.
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var material = new Material(shader) { name = "MAP_R4_DEBUG_MATERIAL" };
            material.color = color;
            return material;
        }

        public static void DisposeMeshes(MapR4BuildResult build)
        {
            if (build == null) return;
            foreach (var mesh in build.OwnedMeshes)
                if (mesh != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
                    else UnityEngine.Object.DestroyImmediate(mesh);
                }
            build.OwnedMeshes.Clear();
            foreach (var material in build.OwnedMaterials)
                if (material != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                    else UnityEngine.Object.DestroyImmediate(material);
                }
            build.OwnedMaterials.Clear();
        }
    }
}
