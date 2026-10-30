// Test-only Unity API surrogate; NOT real Unity/URP/PhysX execution.
using System;
namespace UnityEngine {
 [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
 [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
 [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a,float b) {} }
 [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float min) {} }
 public class Object {}
 public class MonoBehaviour : Object { public bool enabled=true; }
 public sealed class GameObject { public bool activeInHierarchy=true; }
 public sealed class Transform { public Vector3 position; }
 public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public float sqrMagnitude=>x*x+y*y+z*z; public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
 public sealed class Camera : Object { public static Camera main; public Transform transform=new Transform(); }
 public sealed class Light : Object { public bool enabled=true; public GameObject gameObject=new GameObject(); public Transform transform=new Transform(); }
 public static class Debug { public static void LogError(string m,Object context){} public static void LogWarning(string m,Object context){} }
}
