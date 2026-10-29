using System;
using System.IO;
using System.Text.Json;

namespace UnityEngine
{
    // Test-only .NET 8 simulation, NOT actual UnityEngine.JsonUtility.
    public static class Application
    {
        public static bool isBatchMode => false;
        public static string persistentDataPath => Path.GetTempPath();
    }
    public static class Mathf
    {
        public static float Max(float a, float b) => MathF.Max(a,b);
    }
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Settings = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false
        };
        public static string ToJson(object obj) =>
            JsonSerializer.Serialize(obj, obj.GetType(), Settings);
        public static T FromJson<T>(string content) =>
            JsonSerializer.Deserialize<T>(content, Settings);
    }
}
namespace RoboRougelike.R5
{
    public sealed class FakeNamespaceOnly { }
}
namespace RoboRougelike.R1
{
    [Serializable] public sealed class R1ProgressCheckpoint
    {
        public int stage;
        public bool garage;
    }
}
namespace RoboRougelike.R3
{
    [Serializable] public sealed class R3ModelCheckpoint
    {
        public string tag;
        public string[] ownedIds;
    }
    public sealed class R3PartState
    {
        public float DeviceReadyAt;
        public float ReloadReadyAt;
        public float DefenseRechargeAge;
        public int Ammo;
        public R3PartState Copy() => (R3PartState)MemberwiseClone();
    }
}
