// B01-61 isolated TEST-ONLY managed UnityEngine surface, NEVER native Unity/PhysX.
// Compiles the REAL unchanged B01-23 IsolatedTwinRigSaveHost.cs directly.
// Reflection here emulates Unity's Awake lifecycle on THIS new test object only;
// it never reaches frozen #510 Rig[] or a private gameplay ledger.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute {}
    public enum PrimitiveType {Cube}
    public enum FindObjectsInactive {Include,Exclude}
    public enum FindObjectsSortMode {None}
    public enum RigidbodyConstraints {None,FreezeRotation}
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float xx,float yy,float zz){x=xx;y=yy;z=zz;}
        public static Vector3 zero=>new Vector3(0,0,0);
        public static Vector3 one=>new Vector3(1,1,1);
        public static bool operator ==(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
        public static bool operator !=(Vector3 a,Vector3 b)=>!(a==b);
        public override bool Equals(object obj)=>obj is Vector3 v&&this==v;
        public override int GetHashCode()=>HashCode.Combine(x,y,z);
    }
    public struct Quaternion
    {
        public float x,y,z,w;
        public Quaternion(float xx,float yy,float zz,float ww){x=xx;y=yy;z=zz;w=ww;}
        public static Quaternion identity=>new Quaternion(0,0,0,1);
        public static bool operator ==(Quaternion a,Quaternion b)=>
           a.x==b.x&&a.y==b.y&&a.z==b.z&&a.w==b.w;
        public static bool operator !=(Quaternion a,Quaternion b)=>!(a==b);
        public override bool Equals(object obj)=>obj is Quaternion q&&this==q;
        public override int GetHashCode()=>HashCode.Combine(x,y,z,w);
    }
    public class Object
    {
        public static bool operator ==(Object a,Object b)
        {
            bool x=ReferenceEquals(a,null)||(a is GameObject ga&&ga.Dead);
            bool y=ReferenceEquals(b,null)||(b is GameObject gb&&gb.Dead);
            return x&&y||(!x&&!y&&ReferenceEquals(a,b));
        }
        public static bool operator !=(Object a,Object b)=>!(a==b);
        public override bool Equals(object obj)=>this==obj as Object;
        public override int GetHashCode()=>base.GetHashCode();
        // Unity Destroy(GameObject) is end-of-frame deferred; disable body NOW.
        private static readonly List<GameObject> pending=new List<GameObject>();
        public static void Destroy(GameObject obj){obj.SetActive(false);pending.Add(obj);}
        public static void FlushEndOfFrame()
        {
            foreach(var obj in pending.ToArray())obj.DestroyRecursive();
            pending.Clear();
            foreach(var g in GameObject.Registry.ToArray())
                foreach(var c in g.AllComponents.OfType<MonoBehaviour>().ToArray())
                    c.GetType().GetMethod("LateUpdate",
                        BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(c,null);
            foreach(var obj in pending.ToArray())obj.DestroyRecursive();
            pending.Clear();
        }
        public static void DontDestroyOnLoad(GameObject obj){/* isolated shim */}

        // TEST-ONLY emulation: Unity Object.Destroy(MonoBehaviour) kills ONLY
        // the component. Owned child GameObjects/Rigidbodies are NOT destroyed.
        public static void Destroy(Component obj)
        {
            if(obj is MonoBehaviour b)
            {
                b.enabled=false;
                foreach(var method in new[]{"OnDisable","OnDestroy"})
                    obj.GetType().GetMethod(method,
                      BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(obj,null);
            }
            obj.gameObject.RemoveComponentOnly(obj);
        }
        public static void DestroyImmediate(GameObject obj)=>obj.DestroyRecursive();
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive,FindObjectsSortMode mode) where T:Component
        {
            return GameObject.Registry.Where(g=>!g.Dead &&
                (inactive==FindObjectsInactive.Include||g.activeInHierarchy))
                .SelectMany(g=>g.AllComponents).OfType<T>().ToArray();
        }
    }
    public class Component:Object
    {
        public GameObject gameObject {get;internal set;}=null!;
        public Transform transform=>gameObject.transform;
    }
    public class MonoBehaviour:Component
    {
        public bool enabled {get;set;}=true;
        public bool isActiveAndEnabled=>enabled && gameObject.activeInHierarchy;
    }
    public sealed class Transform:Component
    {
        private Transform _parent;
        internal List<Transform> Children {get;}=new List<Transform>();
        public Transform parent=>_parent;
        public Vector3 position {get;set;}=Vector3.zero;
        public Quaternion rotation {get;set;}=Quaternion.identity;
        public Vector3 localScale {get;set;}=Vector3.one;
        public Vector3 lossyScale=>localScale;
        public void SetParent(Transform parent,bool worldPositionStays)
        {
            if(_parent!=null)_parent.Children.Remove(this);
            _parent=parent;
            if(parent!=null)parent.Children.Add(this);
        }
    }
    public sealed class GameObject:Object
    {
        internal static List<GameObject> Registry {get;}=new List<GameObject>();
        private readonly List<Component> components=new List<Component>();
        internal IEnumerable<Component> AllComponents=>components;
        internal bool Dead {get;private set;}
        public string name {get;set;}
        public Transform transform {get;}
        public bool activeSelf {get;private set;}=true;
        public bool activeInHierarchy=>!Dead&&activeSelf &&
             (transform.parent==null||transform.parent.gameObject.activeInHierarchy);
        public GameObject(string name="GameObject")
        {
            this.name=name;transform=new Transform{gameObject=this};
            components.Add(transform);Registry.Add(this);
        }
        public static GameObject CreatePrimitive(PrimitiveType type)=>new GameObject(type.ToString());
        public T AddComponent<T>() where T:Component,new()
        {
            var obj=new T{gameObject=this};
            components.Add(obj);
            // Test harness handles ONLY MonoBehaviour lifecycle, not gameplay reflection.
            if(obj is MonoBehaviour)
                obj.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)?
                   .Invoke(obj,null);
            return obj;
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component
        {
            var found=new List<T>();
            void Visit(GameObject g)
            {
                if(g.Dead || (!includeInactive&&!g.activeInHierarchy))return;
                found.AddRange(g.components.OfType<T>());
                foreach(var ch in g.transform.Children)Visit(ch.gameObject);
            }
            Visit(this);return found.ToArray();
        }
        public void SetActive(bool value){activeSelf=value;}
        internal void RemoveComponentOnly(Component c) { components.Remove(c); }
        internal void DestroyRecursive()
        {
            if(Dead)return;
            Dead=true;activeSelf=false;
            foreach(var c in transform.Children.ToArray())c.gameObject.DestroyRecursive();
            if(transform.parent!=null)transform.SetParent(null,false);
        }
    }
    public sealed class Rigidbody:Component
    {
        public float mass {get;set;}=1;
        public bool isKinematic {get;set;}
        public bool useGravity {get;set;}
        public RigidbodyConstraints constraints {get;set;}
        public Vector3 linearVelocity {get;set;}=Vector3.zero;
        public Vector3 angularVelocity {get;set;}=Vector3.zero;
    }
    public static class Application
    {
        public static string persistentDataPath {get;set;}="";
        public static bool isPlaying {get;set;}=true;
    }
}
