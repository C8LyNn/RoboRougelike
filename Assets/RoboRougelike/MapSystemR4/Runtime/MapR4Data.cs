using System;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    [Serializable]
    public sealed class MapR4MapData
    {
        public string mapId;
        public int version;
        public MapR4NodeData[] nodes;
        public MapR4EdgeData[] edges;
        public MapR4AnchorData[] anchors;
        public MapR4StateData[] states;

        public static MapR4MapData Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("Map JSON must not be empty", nameof(json));
            var result = JsonUtility.FromJson<MapR4MapData>(json);
            if (result == null)
                throw new ArgumentException("Invalid map JSON", nameof(json));
            return result;
        }
    }

    [Serializable]
    public sealed class MapR4NodeData
    {
        public string id;
        public Vector3 position;
        public float padWidth = 12f;
    }

    [Serializable]
    public sealed class MapR4EdgeData
    {
        public string id;
        public string fromNode;
        public string toNode;
        public float width;
        public float clearHeight;
        public float availableTurnRadius;
        public float declaredSlopeDeg;
        public string[] requiredCapabilities;
        // R5: Optional cubic Bezier controls in world-local map coordinates (XZ).
        // Vertical profile is eased independently to avoid an abrupt grade at node pads.
        public bool useCurve;
        public Vector3 controlA;
        public Vector3 controlB;
        public int roadSegments = 20;
        public float roadThickness = 0.35f;
    }

    [Serializable]
    public sealed class MapR4AnchorData
    {
        public string id;
        public string kind;
        public string nodeId;
        public Vector3 offset;
    }

    [Serializable]
    public sealed class MapR4StateData
    {
        public string id;
        public string[] disabledEdgeIds;
    }

    [Serializable]
    public sealed class MapR4VehicleProfile
    {
        public string id;
        public float width;
        public float height;
        public float length = 3.5f;
        public float groundClearance = 0.1f;
        public float minTurnRadius;
        public float maxSlopeDeg;
        public float sideMargin = 0.35f;
        public float verticalMargin = 0.25f;
        public string[] capabilities = {"ground"};
    }

    public enum MapR4Mode { TimeAttack, Survival, Escape, Boss }
    public enum MapR4Status { Ready, Running, Completed, Failed }
}
