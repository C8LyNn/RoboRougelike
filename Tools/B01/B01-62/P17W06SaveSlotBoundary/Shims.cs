using System;
using System.IO;
using System.Text.Json;

namespace UnityEngine
{
    // Explicit .NET 8 test doubles. Never call this native Unity serialization.
    public static class Application
    {
        public static bool isBatchMode { get; set; }
        public static string persistentDataPath { get; set; } = "";
    }
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson(object value) =>
            JsonSerializer.Serialize(value, value.GetType(), Options);
        public static T FromJson<T>(string value) =>
            JsonSerializer.Deserialize<T>(value, Options);
    }
}
namespace RoboRougelike.R5
{
    public sealed class SourceNamespacePlaceholder { }
}
namespace RoboRougelike.R1
{
    [Serializable] public sealed class R1ProgressCheckpoint
    {
        public int completed;
        public bool inGarage;
    }
}
namespace RoboRougelike.R3
{
    [Serializable] public sealed class R3ModelCheckpoint
    {
        public string[] ids;
        public float[] condition;
    }
}
