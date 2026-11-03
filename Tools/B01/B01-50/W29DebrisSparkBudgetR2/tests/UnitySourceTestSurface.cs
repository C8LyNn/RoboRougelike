// Explicit TEST-ONLY minimal Unity API. This NEVER replaces an actual Unity Editor/PhysX test.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class AddComponentMenu : Attribute {
        public AddComponentMenu(string _) {}
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute {
        public RangeAttribute(float _, float __) {}
        public RangeAttribute(int _, int __) {}
    }

    public class MonoBehaviour
    {
        public GameObject gameObject { get; internal set; }
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled && gameObject != null && gameObject.activeInHierarchy;
        public T GetComponent<T>() where T : MonoBehaviour => gameObject.GetComponent<T>();
        public T GetComponentInChildren<T>(bool includeInactive) where T : MonoBehaviour =>
            gameObject.GetComponentInChildren<T>(includeInactive);
    }

    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float a,float b,float c) { x=a;y=b;z=c; }
        public float sqrMagnitude => x*x+y*y+z*z;
    }

    public class Rigidbody : MonoBehaviour
    {
        public bool isKinematic;
        private Vector3 value;
        public int VelocityWrites { get; private set; }
        public Vector3 linearVelocity {
            get { return value; }
            set { this.value = value; ++VelocityWrites; }
        }
    }

    public class GameObject
    {
        private readonly Dictionary<Type,MonoBehaviour> components = new Dictionary<Type,MonoBehaviour>();
        public bool activeInHierarchy = true;
        public int BodyComponentLookups { get; private set; }
        public int NewlyAllocatedVfxCount { get; private set; }
        public T AddComponent<T>() where T : MonoBehaviour,new()
        {
            T component = new T();
            component.gameObject = this;
            components[typeof(T)] = component;
            if (typeof(T) == typeof(RoboRougelike.VFX001.DetachedModuleVfxController))
                ++NewlyAllocatedVfxCount;
            return component;
        }
        public T GetComponent<T>() where T : MonoBehaviour
        {
            if (typeof(T) == typeof(Rigidbody)) ++BodyComponentLookups;
            return components.TryGetValue(typeof(T), out MonoBehaviour c) ? (T)c : null;
        }
        public T GetComponentInChildren<T>(bool _) where T : MonoBehaviour => GetComponent<T>();
    }

    public static class Time { public static float unscaledTime; }
    public static class Debug {
        public static int WarningCount;
        public static void LogWarning(string msg, object context) { ++WarningCount; }
    }

    public static class Mathf
    {
        public static float Sqrt(float value) => MathF.Sqrt(value);
        public static int Clamp(int v,int lo,int hi) => Math.Max(lo,Math.Min(hi,v));
        public static float Clamp(float v,float lo,float hi) => Math.Max(lo,Math.Min(hi,v));
    }
}

namespace RoboRougelike.R3
{
    // Original R3 event signature is independently checked byte-for-byte by CI.
    public class R3Debris : UnityEngine.MonoBehaviour
    {
        public static event Action<R3Debris> Separated;
        public static void SimulateSeparated(R3Debris value) => Separated?.Invoke(value);
    }
}

namespace RoboRougelike.VFX001
{
    // Explicit synthetic VFX method counter, NOT actual Unity ParticleSystem.
    public class DetachedModuleVfxController : UnityEngine.MonoBehaviour
    {
        public int SetCalls { get; private set; }
        public bool Detached { get; private set; }
        public float EmissionRate { get; private set; }
        public void SetDetachedMotion(bool detached,float speedMetersPerSecond)
        {
            ++SetCalls;
            Detached = detached;
            EmissionRate = detached && speedMetersPerSecond > 2.2f ? speedMetersPerSecond : 0f;
        }
    }
}
