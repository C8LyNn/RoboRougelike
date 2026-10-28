// Contract-only substitutes for Unity API used by the ORIGINAL compiled source.
// NOT an engine implementation: no particles, transforms, physics, scene lifecycle or Unity renderer.
// VfxPoolManager.cs, VfxEvent.cs and VfxBudgetPolicy.cs are compiled unchanged.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute
    {
        public MinAttribute(int value) {}
    }

    public static class Mathf
    {
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Clamp01(float x) => Math.Clamp(x, 0f, 1f);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude => x*x + y*y + z*z;
        public Vector3 normalized
        {
            get
            {
                float length = MathF.Sqrt(sqrMagnitude);
                return length > 0f ? new Vector3(x/length, y/length, z/length) : new Vector3(0f,0f,0f);
            }
        }
        public static Vector3 zero => new(0f,0f,0f);
        public static Vector3 up => new(0f,1f,0f);
    }

    public static class Time { public static int frameCount = 1; }

    public sealed class Transform
    {
        public void SetParent(Transform parent, bool worldPositionStays) {}
    }

    public class MonoBehaviour
    {
        private bool _enabled = true;
        public GameObject gameObject { get; internal set; } = null!;
        public Transform transform => gameObject.transform;
        public bool enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                bool wasEnabled = _enabled;
                _enabled = value;
                if (wasEnabled && !value && gameObject.activeSelf)
                    InvokeLifecycle("OnDisable");
            }
        }
        internal void InvokeLifecycle(string name)
            => GetType().GetMethod(name, BindingFlags.Instance|BindingFlags.NonPublic)
                        ?.Invoke(this, null);
    }

    public sealed class GameObject
    {
        private readonly List<MonoBehaviour> _components = new();
        public string name { get; }
        public bool activeSelf { get; private set; } = true;
        public Transform transform { get; } = new();
        public GameObject(string name) { this.name = name; }
        public T AddComponent<T>() where T : MonoBehaviour, new()
        {
            var component = new T { gameObject = this };
            _components.Add(component);
            component.InvokeLifecycle("Awake");
            return component;
        }
        public void SetActive(bool active)
        {
            if (activeSelf == active) return;
            bool wasActive = activeSelf;
            activeSelf = active;
            if (wasActive && !active)
                foreach (var component in _components)
                    if (component.enabled) component.InvokeLifecycle("OnDisable");
        }
    }
}

namespace RoboRougelike.VFX001
{
    // Only the actor boundary is substituted. The original pool, budget, and event are unchanged.
    // It records lifecycle calls; cannot prove actual Unity particle reset / emission.
    public sealed class VfxBurstActor : UnityEngine.MonoBehaviour
    {
        public VfxPoolManager? Owner { get; private set; }
        public int PlayCalls { get; private set; }
        public void Initialize(VfxPoolManager owner) => Owner = owner;
        public void StopAndReset() => gameObject.SetActive(false);
        public void Play(VfxEvent evt)
        {
            PlayCalls++;
            gameObject.SetActive(true);
        }
    }
}
