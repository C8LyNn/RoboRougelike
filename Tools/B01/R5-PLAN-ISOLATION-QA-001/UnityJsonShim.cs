// QA test-double only, NEVER shipped to Unity.
// System.Text.Json IncludeFields approximates JsonUtility field cloning.
// Validation occurs on the ORIGINAL unchanged Codex C# R5RunPlan/R1/R3 files.
using System;
using System.Text.Json;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute:Attribute
    { public string menuName=""; }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RangeAttribute:Attribute
    { public RangeAttribute(float min,float max){} }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute:Attribute
    { public MinAttribute(float min){} }
    public class ScriptableObject {}
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Clamp(int n,int lo,int hi)=>Math.Clamp(n,lo,hi);
    }
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options=new()
        { IncludeFields=true,PropertyNameCaseInsensitive=true };
        public static string ToJson(object instance)=>JsonSerializer.Serialize(instance,instance.GetType(),Options);
        public static T FromJson<T>(string json)=>JsonSerializer.Deserialize<T>(json,Options)
            ?? throw new InvalidOperationException("Missing JsonUtility payload");
    }
}
namespace RoboRougelike.R1 {public sealed class R1UpgradeDefinition {}}
namespace RoboRougelike.R3 {public sealed class R3GarageEconomy {}}
