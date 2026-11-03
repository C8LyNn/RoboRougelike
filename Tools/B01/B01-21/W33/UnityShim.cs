// Deterministic C# source-method harness only; NOT UnityEngine or PhysX.
using System;
namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static bool operator !(Object value) => value is null || value.Destroyed;
        public static implicit operator bool(Object value) => value is not null && !value.Destroyed;
    }
    public sealed class GameObject : Object
    {
        public int layer;
        public bool activeInHierarchy = true;
    }
    public class Component : Object
    {
        public GameObject gameObject = new GameObject();
        public Transform transform = new Transform();
        public T GetComponent<T>() where T : Component => null;
    }
    public class MonoBehaviour : Component { public bool isActiveAndEnabled = true; }
    public class Transform
    {
        public Vector3 up = Vector3.up;
        public Vector3 forward = Vector3.forward;
        public Matrix4x4 localToWorldMatrix;
    }
    public struct Vector2
    {
        public float x, y;
        public static Vector2 one => new Vector2 { x = 1, y = 1 };
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => new Vector3(0,0,0);
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 forward => new Vector3(0,0,1);
        public Vector3 normalized
        {
            get
            {
                var m = MathF.Sqrt(x*x+y*y+z*z);
                return m > 0 ? new Vector3(x/m,y/m,z/m) : zero;
            }
        }
        public static Vector3 operator *(Vector3 a, float n) => new Vector3(a.x*n,a.y*n,a.z*n);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public struct Matrix4x4 {}
    public class Collider : Component
    {
        public bool enabled = true;
        public bool isTrigger;
        public Rigidbody attachedRigidbody;
    }
    public class BoxCollider : Collider
    {
        public Vector3 size, center;
    }
    public enum ForceMode { VelocityChange }
    public class Rigidbody : Component
    {
        public bool isKinematic;
        public Vector3 linearVelocity = Vector3.zero;
        public void AddForce(Vector3 delta, ForceMode mode)
        {
            if (mode != ForceMode.VelocityChange) throw new InvalidOperationException("force mode");
            linearVelocity += delta;
        }
    }
    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int n) => new LayerMask { value=n };
    }
    public static class Time { public static double fixedTimeAsDouble; }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a,b); }
    public static class Debug { public static void LogError(string message, Object context) {} }
    public struct Color { public static Color cyan => new Color(); }
    public static class Gizmos
    {
        public static Color color;
        public static Matrix4x4 matrix;
        public static void DrawWireCube(Vector3 center, Vector3 size) {}
        public static void DrawRay(Vector3 from, Vector3 direction) {}
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute
    { public RequireComponent(Type type) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute
    { public MinAttribute(float value) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute
    { public HeaderAttribute(string label) {} }
}
