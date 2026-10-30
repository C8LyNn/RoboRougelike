// TEST-ONLY MINIMAL UnityEngine SURROGATE. No native Unity, no PhysX is executed.
using System;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute
    {
        public RequireComponent(Type t) {}
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class AddComponentMenu : Attribute
    {
        public AddComponentMenu(string name) {}
    }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(int a,int b) {}
        public RangeAttribute(float a,float b) {}
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public float sqrMagnitude { get { return x*x + y*y + z*z; } }
    }
    public class Collision {}
    public class Joint {}
    public class Rigidbody
    {
        public bool isKinematic;
        public bool detectCollisions=true;
        public bool sleeping;
        public bool acceptSleep=true;
        public int SleepCalls;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public bool IsSleeping() { return sleeping; }
        public void Sleep() { ++SleepCalls; if(acceptSleep) sleeping=true; }
    }
    public class MonoBehaviour
    {
        public bool enabled=true;
        public Rigidbody SimulatedBody; // test controlled; not an original Unity property
        public Joint SimulatedJoint;
        protected T GetComponent<T>() where T:class
        { return typeof(T)==typeof(Joint) ? SimulatedJoint as T : SimulatedBody as T; }
    }
}
