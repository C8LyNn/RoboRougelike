// This is a CLEARLY SYNTHETIC minimal managed UnityEngine API for .NET8 QA.
// The actual original P12, W24 and W25 C# files are linked/compiled verbatim.
// THIS IS NOT NATIVE UNITY G1, PHYSX, IMPORTER OR PLAYER PLAYMODE.
using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute
    { public RequireComponent(Type type) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute
    { public RangeAttribute(float min,float max) {} }
    public class Object
    {
        protected static void Destroy(Object item,float after=0) { }
    }
    public class Component : Object
    {
        public GameObject gameObject { get; internal set; } = new GameObject();
        public Transform transform => gameObject.transform;
        public T? GetComponent<T>() where T:Component => gameObject.GetComponent<T>();
        public T? GetComponentInParent<T>() where T:Component => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component
    {
        public bool isActiveAndEnabled { get; set; } = true;
        public Coroutine StartCoroutine(IEnumerator e) => new Coroutine();
        public void StopCoroutine(Coroutine c) {}
    }
    public sealed class Coroutine {}
    public class GameObject : Object
    {
        private readonly Dictionary<Type,Component> comps=new();
        public bool activeSelf { get; private set; }
        public Transform transform { get; } = new Transform();
        public GameObject(bool initial=false) {activeSelf=initial;}
        public void SetActive(bool state) {activeSelf=state;}
        public T? GetComponent<T>() where T:Component => comps.TryGetValue(typeof(T),out var c)?(T)c:null;
        public T AddComponent<T>() where T:Component,new()
        {
            var obj=new T{gameObject=this};
            comps[typeof(T)]=obj;
            return obj;
        }
    }
    public class Transform
    {
        public Vector3 position { get; set; }
        public void SetParent(Transform? parent,bool worldPositionStays) {}
    }
    public readonly struct Vector3
    {
        public readonly float x,y,z;
        public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero => new(0,0,0);
        public static Vector3 up => new(0,1,0);
        public Vector3 normalized { get
        {
            var mag=MathF.Sqrt(x*x+y*y+z*z);
            return mag<=0 ? zero:new(x/mag,y/mag,z/mag);
        }}
        public static Vector3 operator *(Vector3 v,float x)=>new(v.x*x,v.y*x,v.z*x);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public readonly struct Color
    {
        public readonly float r,g,b,a;
        public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white=>new(1,1,1);
        public static Color red=>new(1,0,0);
        public static Color Lerp(Color a,Color b,float t)
            => new(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t);
    }
    public static class Mathf
    {
        public static float Max(float a,float b)=>MathF.Max(a,b);
        public static float Clamp01(float x)=>MathF.Max(0,MathF.Min(1,x));
        public static float Clamp(float x,float lo,float hi)=>MathF.Max(lo,MathF.Min(hi,x));
    }
    public class Renderer : Component
    {
        public Material? sharedMaterial {get;set;}
        public void GetPropertyBlock(MaterialPropertyBlock b){}
        public void SetPropertyBlock(MaterialPropertyBlock b){}
    }
    public class Material
    {
        public bool HasProperty(string name)=>false;
        public Color GetColor(string name)=>Color.white;
    }
    public class MaterialPropertyBlock
    {public void SetColor(string name,Color value){}}
    public sealed class WaitForSeconds
    {public WaitForSeconds(float secs){}}
    public enum CollisionDetectionMode {Discrete,Continuous}
    public enum ForceMode {Impulse}
    public class Rigidbody : Component
    {
        public Vector3 angularVelocity {get;set;}
        public CollisionDetectionMode collisionDetectionMode {get;set;}
        public Vector3 GetPointVelocity(Vector3 pos)=>Vector3.zero;
        public void AddForceAtPosition(Vector3 force,Vector3 position,ForceMode mode){}
    }
}
namespace RoboRougelike.R3
{
    public sealed class R3Debris : UnityEngine.Component {}
}
namespace RoboRougelike.R6
{
    public static class R6CombatFeedback
    {
        public static void Hit(object d,UnityEngine.Vector3 hit,bool destroyed){}
    }
    public static class R6VehicleDrive
    {
        public static float PartMass(object d)=>1f;
    }
    public static class R6DebrisRelease
    {
        public static void Release(UnityEngine.Rigidbody d,UnityEngine.Rigidbody? owner,
           UnityEngine.Vector3 v,UnityEngine.Vector3 spin,float mass){}
    }
}
