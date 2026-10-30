// QA-only UnityEngine managed API stand-ins. NOT actual Unity/PhysX.
using System;
using System.Threading;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Method)] public sealed class ContextMenu : Attribute
    {
        public ContextMenu(string name) {}
    }
    public class Object {}
    public class GameObject { public bool activeInHierarchy=true; }
    public class Component : Object
    {
        private static int nextId;
        private readonly int id=Interlocked.Increment(ref nextId);
        public readonly GameObject gameObject=new GameObject();
        public int GetInstanceID() { return id; }
    }
    public class MonoBehaviour : Component {}
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float ax,float ay,float az) { x=ax;y=ay;z=az; }
    }
    public class Rigidbody : Component
    {
        public float mass=1;
        public bool isKinematic;
        public Vector3 worldCenterOfMass;
    }
    public struct WheelHit { public Vector3 point; }
    public class WheelCollider : Component
    {
        public bool enabled=true;
        public bool IsGrounded=true;
        public WheelHit Hit;
        public int Calls;
        public bool GetGroundHit(out WheelHit hit)
        {
            Calls++; hit=Hit; return IsGrounded;
        }
    }
    public static class Debug
    {
        public static void Log(object message, Object context) {}
        public static void LogWarning(object message, Object context) {}
    }
}
