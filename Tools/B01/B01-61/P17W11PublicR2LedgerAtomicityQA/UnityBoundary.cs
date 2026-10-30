// B01-61 W11 isolated managed UnityEngine API boundary (NOT Unity/PhysX).
// Derived from the original W10 test-only stub with ORIGINAL W06 emitter removed.
// This QA project compiles the ACTUAL original W06 ledger + emitter and ACTUAL W10 game class.
// TEST-ONLY managed Unity API substitute. This file is NEVER installed in Unity.
// The production DebrisRigPlayground.cs remains untouched by the QA stub.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float v) {} }
    public class Object {}
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component
    {
        public bool enabled=true;
        public static void Destroy(GameObject x) { x.Destroyed=true; }
        public static void DestroyImmediate(GameObject x) { x.Destroyed=true; }
    }
    public enum PrimitiveType { Cube }
    public sealed class GameObject : Object
    {
        private readonly List<Component> components=new List<Component>();
        public string name;
        public Transform transform;
        public bool activeInHierarchy=true;
        public bool Destroyed;
        public GameObject(string n="") {name=n;transform=new Transform{gameObject=this};}
        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var g=new GameObject("Cube");
            g.AddComponent<BoxCollider>();
            return g;
        }
        public T AddComponent<T>() where T:Component,new()
        {
            var c=new T{gameObject=this};components.Add(c);return c;
        }
        public T GetComponent<T>() where T:Component
        {
            foreach(var c in components)if(c is T t)return t;
            return null;
        }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero => new Vector3(0,0,0);
        public static Vector3 one => new Vector3(1,1,1);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float x)=>new Vector3(a.x*x,a.y*x,a.z*x);
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public static float Distance(Vector3 a,Vector3 b)
        { var d=a-b;return (float)Math.Sqrt(d.x*d.x+d.y*d.y+d.z*d.z); }
    }
    public struct Quaternion
    {
        public static Quaternion identity=>new Quaternion();
        public static Vector3 operator *(Quaternion q,Vector3 p)=>p;
    }
    public sealed class Transform : Component
    {
        private Transform _parent;
        private Vector3 local;
        public Vector3 localScale=Vector3.one;
        public Vector3 localPosition{get=>local;set=>local=value;}
        public Vector3 position
        {
            get=>_parent==null?local:_parent.position+local;
            set=>local=_parent==null?value:value-_parent.position;
        }
        public Transform parent { get=>_parent; set=>SetParent(value,true); }
        public Transform parentTransform=>_parent;
        public Quaternion rotation=>Quaternion.identity;
        public Vector3 lossyScale=>localScale;
        public Vector3 TransformPoint(Vector3 x)=>position+x;
        public void SetParent(Transform next,bool worldPositionStays)
        {
            var before=position;_parent=next;if(worldPositionStays)position=before;
        }
    }
    public enum ForceMode { Impulse }
    public enum RigidbodyConstraints{FreezeRotation}
    public class Rigidbody:Component
    {
        public bool isKinematic,useGravity;
        public float linearDamping,angularDamping,mass;
        public RigidbodyConstraints constraints;
        public Vector3 linearVelocity,angularVelocity;
        public Vector3 worldCenterOfMass => transform.position;
        public void AddForce(Vector3 impulse, ForceMode mode) {
            if (mode != ForceMode.Impulse) throw new InvalidOperationException("Expected impulse");
            if (!isKinematic && mass > 0f) linearVelocity += impulse * (1f / mass);
        }
    }
    public class Collider:Component
    {
        public bool enabled=true,isTrigger;
        public Rigidbody attachedRigidbody=>gameObject.GetComponent<Rigidbody>();
    }
    public class BoxCollider:Collider{public Vector3 size=Vector3.one,center=Vector3.zero;}
    public class SphereCollider:Collider{}
    public class CapsuleCollider:Collider{}
    public class MeshCollider:Collider{public bool convex;}
    public enum QueryTriggerInteraction{UseGlobal,Ignore,Collide}
    public static class Mathf { public static float Abs(float x)=>Math.Abs(x); }
    public static class Physics
    {
        public static readonly List<Collider> Hits=new List<Collider>();
        public static float Depth=.1f;
        public static bool Penetrating=true;
        public static float LastReach;
        public static int LastMask,Calls,SyncCalls,Capacity;
        public static QueryTriggerInteraction LastQuery;
        public static bool CorruptDirection;
        public static void Reset(){Hits.Clear();Depth=.1f;Penetrating=true;Calls=0;SyncCalls=0;Capacity=0;CorruptDirection=false;}
        public static void SyncTransforms(){SyncCalls++;}
        public static int OverlapBoxNonAlloc(Vector3 center,Vector3 half,Collider[] output,
            Quaternion rotation,int mask,QueryTriggerInteraction query)
        {
            Calls++;LastMask=mask;LastQuery=query;Capacity=output.Length;
            int n=Math.Min(Hits.Count,output.Length);
            for(int i=0;i<n;i++)output[i]=Hits[i];
            return n;
        }
        public static bool ComputePenetration(Collider source,Vector3 p,Quaternion r,
            Collider other,Vector3 op,Quaternion or,
            out Vector3 direction,out float distance)
        {
            direction=CorruptDirection?new Vector3(float.NaN,0,0):new Vector3(1,0,0);
            distance=Depth;
            return Penetrating;
        }
    }
    public static class Application {public static bool isPlaying=true;}
    public struct Rect {public Rect(float a,float b,float c,float d){}}
    public class GUISkin {public object box=new object();}
    public static class GUI {public static GUISkin skin=new GUISkin();}
    public static class GUILayout
    {
        public static string NextButton;
        public static void BeginArea(Rect r,object style){}
        public static void EndArea(){}
        public static void Label(string text){}
        public static void BeginHorizontal(){}
        public static void EndHorizontal(){}
        public static bool Button(string s)
        {
            if(s==NextButton){NextButton=null;return true;}return false;
        }
    }
}
