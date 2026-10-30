using System;

namespace UnityEngine
{
    // B01-61 QA-only surface stub. This is NOT UnityEngine or PhysX and never
    // asserts that actual Rigidbody simulation was tested.
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute
    {
        public MinAttribute(float value) { }
    }
    public class MonoBehaviour { }
    public enum ForceMode { Force, Impulse, VelocityChange, Acceleration }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public override string ToString() => $"({x},{y},{z})";
    }
    public class Rigidbody
    {
        public bool isKinematic;
        public float mass = 1f;
        public Vector3 worldCenterOfMass, linearVelocity;
        public int AddForceCalls { get; private set; }
        public Vector3 LastForce { get; private set; }
        public ForceMode LastMode { get; private set; }
        public void AddForce(Vector3 impulse, ForceMode mode)
        {
            AddForceCalls++;
            LastForce = impulse;
            LastMode = mode;
        }
    }
}
