// MANAGED-ONLY contract shim for executing the EXACT original Unity MapJumpPad.cs.
// It is not Unity, not real PhysX, not a Unity Player and not G1 evidence.
using System;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute
    { public RequireComponent(Type type) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute
    { public HeaderAttribute(string title) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute
    { public MinAttribute(float minimum) {} }

    public class GameObject
    {
        public int layer;
        public bool activeInHierarchy = true;
    }
    public class Component
    {
        public GameObject gameObject = new GameObject();
        public bool Destroyed;
        public static bool operator !(Component component) =>
            ReferenceEquals(component, null) || component.Destroyed;
        public static bool operator true(Component component) =>
            !ReferenceEquals(component, null) && !component.Destroyed;
        public static bool operator false(Component component) =>
            ReferenceEquals(component, null) || component.Destroyed;
    }
    public class MonoBehaviour : Component
    {
        public bool isActiveAndEnabled = true;
        public Transform transform = new Transform();
        public T GetComponent<T>() where T : Component =>
            typeof(T) == typeof(BoxCollider)
               ? (T)(Component)new BoxCollider { isTrigger = true }
               : null;
    }
    public class Collider : Component
    {
        public bool isTrigger;
        public bool enabled = true;
        public Rigidbody attachedRigidbody;
    }
    public sealed class BoxCollider : Collider
    {
        public Vector3 center;
        public Vector3 size;
    }
    public class Rigidbody : Component
    {
        public bool isKinematic;
        public Vector3 linearVelocity;
        public int AppliedVelocityChangeCount;
        public void AddForce(Vector3 delta, ForceMode mode)
        {
            if (mode != ForceMode.VelocityChange)
                throw new InvalidOperationException("Original impulse mode changed");
            AppliedVelocityChangeCount++;
            linearVelocity += delta;
        }
    }
    public enum ForceMode { VelocityChange }
    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int mask) => new LayerMask { value = mask };
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float a, float b) { x = a; y = b; }
        public static Vector2 one => new Vector2(1, 1);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x = a; y = b; z = c; }
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public Vector3 normalized {
            get {
                float length = MathF.Sqrt(x*x + y*y + z*z);
                return length <= 0.00001f ? new Vector3(0,0,0) :
                    new Vector3(x / length, y / length, z / length);
            }
        }
        public static Vector3 operator +(Vector3 a, Vector3 b) =>
            new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a, float b) =>
            new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public struct Matrix4x4 { public static Matrix4x4 identity => new Matrix4x4(); }
    public class Transform
    {
        public Vector3 up = Vector3.up;
        public Vector3 forward = Vector3.forward;
        public Matrix4x4 localToWorldMatrix = Matrix4x4.identity;
    }
    public struct Color { public static Color cyan => new Color(); }
    public static class Gizmos
    {
        public static Color color;
        public static Matrix4x4 matrix;
        public static void DrawWireCube(Vector3 center, Vector3 extents) {}
        public static void DrawRay(Vector3 origin, Vector3 delta) {}
    }
    public static class Debug { public static void LogError(string message, object context) {} }
    public static class Time { public static double fixedTimeAsDouble; }
    public static class Mathf
    {
        public static float Max(float a, float b) => MathF.Max(a, b);
    }
}
