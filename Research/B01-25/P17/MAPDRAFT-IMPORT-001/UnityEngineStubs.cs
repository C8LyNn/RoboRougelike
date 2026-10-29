// B01-25 standalone .NET 8 surrogate ONLY. This code is NOT Unity and not installed
// in Assets. Linked original MapDraft.cs is fetched by SHA at CI execution time.
using System;
using System.Text.Json;

namespace UnityEngine
{
    public readonly struct Vector2Int : IEquatable<Vector2Int>
    {
        public readonly int x,y;
        public Vector2Int(int x,int y) { this.x=x; this.y=y; }
        public bool Equals(Vector2Int v)=>x==v.x&&y==v.y;
        public override bool Equals(object? obj)=>obj is Vector2Int v&&Equals(v);
        public override int GetHashCode()=>HashCode.Combine(x,y);
        public static bool operator ==(Vector2Int a,Vector2Int b)=>a.Equals(b);
        public static bool operator !=(Vector2Int a,Vector2Int b)=>!a.Equals(b);
    }
    public static class JsonUtility
    {
        // The two fully field-specified MapDocument-v2 inputs used here are
        // deserialized with a NON-UNITY surrogate. This does not validate the
        // behavior of Unity's production JsonUtility implementation.
        public static readonly JsonSerializerOptions Options=new()
        {
            IncludeFields=true,
            PropertyNameCaseInsensitive=false,
            WriteIndented=true
        };
        public static T? FromJson<T>(string json)=>JsonSerializer.Deserialize<T>(json,Options);
        public static string ToJson(object value,bool prettyPrint=false)=>
            JsonSerializer.Serialize(value,new JsonSerializerOptions {IncludeFields=true,WriteIndented=prettyPrint});
    }
}
