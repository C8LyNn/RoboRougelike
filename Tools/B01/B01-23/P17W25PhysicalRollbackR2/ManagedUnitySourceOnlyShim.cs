// W25 managed-only fake UnityEngine used ONLY by dotnet source compiler.
// NEVER install this in Unity; PhysX, pending Destroy, frame scheduling NOT simulated.
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent:Attribute{}
    public class Object
    {
        public bool Destroyed {get;private set;}
        internal virtual void MarkDestroyed(){Destroyed=true;}
        public static bool operator ==(Object? a,Object? b)
        {
            bool x=ReferenceEquals(a,null)||(!ReferenceEquals(a,null)&&a.Destroyed);
            bool y=ReferenceEquals(b,null)||(!ReferenceEquals(b,null)&&b.Destroyed);
            return x==y;
        }
        public static bool operator !=(Object? a,Object? b)=>!(a==b);
        public override bool Equals(object? obj)=>ReferenceEquals(this,obj);
        public override int GetHashCode()=>base.GetHashCode();
        public static void Destroy(GameObject? x){if(!ReferenceEquals(x,null))x.MarkDestroyed();}
        public static void DestroyImmediate(GameObject? x)=>Destroy(x);
    }
    public readonly struct Vector3
    {
        public readonly float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    }
    public readonly struct Quaternion
    {
        public readonly float x,y,z,w;
        public Quaternion(float x,float y,float z,float w)
        {this.x=x;this.y=y;this.z=z;this.w=w;}
    }
    public class Component:Object
    {
        public GameObject gameObject=null!;
        public Transform transform=>gameObject.transform;
        internal override void MarkDestroyed(){base.MarkDestroyed();}
    }
    public class MonoBehaviour:Component{}
    public class Transform:Component
    {
        private Transform? current;
        public Transform? parent=>current;
        public Vector3 position,localPosition,localScale;
        public Quaternion rotation;
        public void SetParent(Transform? parent,bool worldPositionStays=false)
        { current=parent; }
    }
    public class GameObject:Object
    {
        readonly List<Component> comps=new List<Component>();
        public string name;
        public readonly Transform transform;
        bool active=true;
        public GameObject(string name)
        {
            this.name=name;
            transform=new Transform{gameObject=this};
            comps.Add(transform);
        }
        public void SetActive(bool active){this.active=active;}
        public bool Active=>active;
        public T AddComponent<T>() where T:Component,new()
        {
            var comp=new T{gameObject=this};comps.Add(comp);
            typeof(T).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic|
                BindingFlags.Public)?.Invoke(comp,null);
            return comp;
        }
        internal override void MarkDestroyed()
        {
            base.MarkDestroyed();
            foreach(var c in comps)c.MarkDestroyed();
        }
    }
    public sealed class BoxCollider:Component
    {
        public bool isTrigger;
    }
    public sealed class Rigidbody:Component
    {
        public float mass=1f,linearDamping,angularDamping;
        public bool isKinematic,useGravity;
        public Vector3 linearVelocity,angularVelocity;
    }
    public static class Application {public static bool isPlaying=true;}
}
