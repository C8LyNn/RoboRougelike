// HEADLESS TEST SHIMS — only Unity class signatures needed to execute
// ORIGINAL canonical R5RunPlan/R1RunDefinition/R3EnemyArchetype source unchanged.
// System.Text.Json is NOT Unity JsonUtility and cannot certify native import parity.
using System;
using System.Text.Json;
namespace UnityEngine
{
    public class ScriptableObject {}
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    { public string menuName { get; set; } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute : Attribute
    { public RangeAttribute(float minimum, float maximum) {} }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute
    { public MinAttribute(float minimum) {} }
    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) =>
            Math.Min(max, Math.Max(min, value));
        public static float Max(float a, float b) => Math.Max(a, b);
    }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson<T>(T value) =>
            JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string source) =>
            JsonSerializer.Deserialize<T>(source, Options);
    }
}
namespace RoboRougelike.R3 { public sealed class R3GarageEconomy {} }
namespace RoboRougelike.R1 { public sealed class R1UpgradeDefinition {} }
