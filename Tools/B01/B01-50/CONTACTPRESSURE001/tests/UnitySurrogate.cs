// Explicit test-only UnityEngine surface. NEVER represents an actual Unity engine execution.
using System;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class AddComponentMenu : Attribute
    {
        public AddComponentMenu(string path) {}
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(int lo, int hi) {}
    }
    public class Object {}
    public class MonoBehaviour : Object { public bool enabled=true; }
    public sealed class Collision { public int contactCount; }
    public static class Debug { public static void LogError(string message, Object owner) {} }
}
