// Minimal non-Unity adapter for executing the UNMODIFIED original MapDraft.cs in .NET 8.
// Unity's actual JsonUtility, Unity 6000.3.14f1/PhysX and player are NOT run here.
using System;
using System.Text.Json;
namespace UnityEngine
{
    public readonly struct Vector2Int : IEquatable<Vector2Int>
    {
        public readonly int x, y;
        public Vector2Int(int x, int y) { this.x=x; this.y=y; }
        public bool Equals(Vector2Int other) => x==other.x && y==other.y;
        public override bool Equals(object obj) => obj is Vector2Int other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x,y);
    }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Compact = new JsonSerializerOptions { IncludeFields=true };
        private static readonly JsonSerializerOptions Pretty = new JsonSerializerOptions { IncludeFields=true, WriteIndented=true };
        public static string ToJson(object obj, bool prettyPrint=false)
            => JsonSerializer.Serialize(obj, prettyPrint ? Pretty : Compact);
        public static T FromJson<T>(string json)
            => JsonSerializer.Deserialize<T>(json, Compact);
    }
}
