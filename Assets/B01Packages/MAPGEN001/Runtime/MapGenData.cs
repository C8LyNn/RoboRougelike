using System;
using System.Collections.Generic;
using UnityEngine;

namespace B01.MAPGEN001
{
    public enum MapRecipe { CombatArena, ObstacleCourse }
    public enum MapElementKind { Floor, Boundary, Cover, Obstacle, Ramp }

    [Serializable]
    public struct MapGenConfig
    {
        public int Segments; // 3..5, course only
        public int ObstacleDensity; // 0..100
        public float Difficulty; // 0..1
        public float LaneWidth; // metres, course only
        public static MapGenConfig Default => new MapGenConfig
        { Segments = 4, ObstacleDensity = 50, Difficulty = .5f, LaneWidth = 16f };
    }

    [Serializable]
    public struct MapVehicleProfile
    {
        public float Width;
        public float Length;
        public float MinTurnRadius;
        public float MaxSlopeDegrees; // 0 = legacy default (12 degrees)
        public static MapVehicleProfile Light => new MapVehicleProfile { Width = 2.1f, Length = 3.2f, MinTurnRadius = 3f, MaxSlopeDegrees = 14f };
        public static MapVehicleProfile Heavy => new MapVehicleProfile { Width = 4.5f, Length = 5.8f, MinTurnRadius = 6f, MaxSlopeDegrees = 10f };
    }

    public struct MapElement
    {
        public MapElementKind Kind;
        public Vector3 Position;
        public Vector3 Size;
        public Quaternion Rotation;
        public MapElement(MapElementKind kind, Vector3 position, Vector3 size, Quaternion rotation)
        { Kind = kind; Position = position; Size = size; Rotation = rotation; }
    }

    public sealed class MapBlueprint
    {
        public MapRecipe Recipe;
        public int Seed;
        public int GenerationVersion = 1;
        public readonly List<MapElement> Elements = new List<MapElement>();
        public readonly List<Vector3> Checkpoints = new List<Vector3>();
        public readonly List<Vector3> EnemySpawns = new List<Vector3>();
        public Vector3 Start;
        public Vector3 Finish;
        public Bounds Bounds;
        public float GuaranteedClearanceWidth;
        public string GeometryHash;
        public string Diagnostics;
    }

    public sealed class MapResult
    {
        public GameObject GeneratedRoot;
        public MapRecipe Recipe;
        public int Seed;
        public int GenerationVersion;
        public Vector3 Start;
        public Vector3 Finish;
        public Vector3[] Spawns;
        public Vector3[] CheckpointDescriptors;
        public Bounds Bounds;
        public string GeometryHash;
        public string Diagnostics;
    }
}
