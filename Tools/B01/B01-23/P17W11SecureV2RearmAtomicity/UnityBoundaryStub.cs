// QA-ONLY UnityEngine managed boundary, never imported into an Assets path.
// Compiles the genuine original Unity MonoBehaviour and W06 emitter / impulse ledger.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent:Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField:Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute:Attribute
    { public RangeAttribute(float lo,float hi){} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute:Attribute
    { public MinAttribute(float x){} }
    public class Object {}
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
    }
    public class MonoBehaviour:Component
    {
        public bool enabled=true;
        public static void Destroy(GameObject g){g.Destroyed=true;}
        public static void DestroyImmediate(GameObject g){g.Destroyed=true;}
    }
    public enum PrimitiveType{Cube}
    public class GameObject:Object
    {
        private readonly List<Component> components=new List<Component>();
        public string name;
        public int layer;
        public bool activeInHierarchy=true;
        public bool Destroyed;
        public Transform transform;
        public GameObject(string n=""){name=n;transform=new Transform{gameObject=this};}
        public T AddComponent<T>() where T:Component,new()
        { var c=new T{gameObject=this};components.Add(c);return c; }
        public T GetComponent<T>() where T:Component
        {foreach(var c in components)if(c is T typed)return typed;return null;}
        public static GameObject CreatePrimitive(PrimitiveType p)
        {var g=new GameObject("Cube");g.AddComponent<BoxCollider>();return g;}
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float px,float py,float pz){x=px;y=py;z=pz;}
        public static Vector3 zero=>new Vector3(0,0,0);
        public static Vector3 one=>new Vector3(1,1,1);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float s)=>new Vector3(a.x*s,a.y*s,a.z*s);
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
        public static float Distance(Vector3 a,Vector3 b)
        {var d=a-b;return (float)Math.Sqrt(d.x*d.x+d.y*d.y+d.z*d.z);}
        public static bool operator ==(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
        public static bool operator !=(Vector3 a,Vector3 b)=>!(a==b);
        public override bool Equals(object obj)=>obj is Vector3 v&&this==v;
        public override int GetHashCode()=>HashCode.Combine(x,y,z);
    }
    public struct Quaternion
    {
        public static Quaternion identity=>default;
        public static bool operator ==(Quaternion a,Quaternion b)=>true;
        public static bool operator !=(Quaternion a,Quaternion b)=>false;
        public override bool Equals(object obj)=>obj is Quaternion;
        public override int GetHashCode()=>0;
    }
    public class Transform:Component
    {
        private Transform _parent;
        private Vector3 _local;
        public Vector3 localScale=Vector3.one;
        public Vector3 localPosition{get=>_local;set=>_local=value;}
        public Vector3 position
        {
            get=>_parent==null?_local:_parent.position+_local;
            set=>_local=_parent==null?value:value-_parent.position;
        }
        public Quaternion rotation=>Quaternion.identity;
        public Vector3 lossyScale=>localScale;
        public Transform parent{get=>_parent;set=>SetParent(value,true);}
        public Vector3 TransformPoint(Vector3 p)=>position+p;
        public void SetParent(Transform next,bool worldPositionStays)
        {var old=position;_parent=next;if(worldPositionStays)position=old;}
    }
    public enum RigidbodyConstraints{FreezeRotation}
    public enum ForceMode{Impulse}
    public class Rigidbody:Component
    {
        public bool isKinematic,useGravity;
        public float mass=1,linearDamping,angularDamping;
        public Vector3 linearVelocity,angularVelocity;
        public RigidbodyConstraints constraints;
        public Vector3 position{get=>transform.position;set=>transform.position=value;}
        public Vector3 worldCenterOfMass=>transform.position;
        public int AddedImpulseCount;
        public void AddForce(Vector3 impulse,ForceMode mode){AddedImpulseCount++;}
    }
    public class Collider:Component
    {
        public bool enabled=true,isTrigger;
        public Rigidbody attachedRigidbody=>gameObject.GetComponent<Rigidbody>();
    }
    public class BoxCollider:Collider {public Vector3 size=Vector3.one,center=Vector3.zero;}
    public class SphereCollider:Collider {}
    public class CapsuleCollider:Collider {}
    public class MeshCollider:Collider {public bool convex;}
    public enum QueryTriggerInteraction{UseGlobal,Ignore,Collide}
    public static class Physics
    {
        public static readonly List<Collider> Hits=new List<Collider>();
        public static int Mask,Samples,SyncCalls;
        public static float Depth=.25f;
        public static bool Penetrating=true;
        public static Action BeforeOverlap;
        public static void Reset()
        {Hits.Clear();Mask=0;Samples=0;SyncCalls=0;Depth=.25f;Penetrating=true;BeforeOverlap=null;}
        public static void SyncTransforms(){SyncCalls++;}
        public static int OverlapBoxNonAlloc(Vector3 centre,Vector3 half,Collider[] dest,
            Quaternion rotation,int mask,QueryTriggerInteraction interaction)
        {
            Samples++;Mask=mask;BeforeOverlap?.Invoke();
            int count=Math.Min(Hits.Count,dest.Length);
            for(int i=0;i<count;i++)dest[i]=Hits[i];
            return count;
        }
        public static bool ComputePenetration(Collider a,Vector3 ap,Quaternion ar,
            Collider b,Vector3 bp,Quaternion br,out Vector3 direction,out float depth)
        {direction=new Vector3(1,0,0);depth=Depth;return Penetrating;}
    }
    public static class Application{public static bool isPlaying=true;}
    public struct Rect{public Rect(float a,float b,float c,float d){}}
    public class GUISkin{public object box=new object();}
    public static class GUI{public static GUISkin skin=new GUISkin();}
    public static class GUILayout
    {
        public static string NextButton;
        public static void BeginArea(Rect rect,object style){}
        public static void EndArea(){}
        public static void Label(string label){}
        public static void BeginHorizontal(){}
        public static void EndHorizontal(){}
        public static bool Button(string name)
        {if(NextButton==name){NextButton=null;return true;}return false;}
    }
}
