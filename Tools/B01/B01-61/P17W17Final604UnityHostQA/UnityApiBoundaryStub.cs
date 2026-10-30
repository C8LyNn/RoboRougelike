// MANAGED non-native Unity API SHIM only. PhysX and Unity runtime NOT RUN.
using System;using System.Collections.Generic;using System.Linq;
namespace UnityEngine {
[AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent:Attribute{}
[AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField:Attribute{}
public enum PrimitiveType{Cube}
public enum FindObjectsInactive{Include}
public enum FindObjectsSortMode{None}
[Flags] public enum RigidbodyConstraints{None=0,FreezeRotation=1}
public struct Vector3{
 public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
 public static Vector3 zero=>new Vector3(0,0,0);public static Vector3 one=>new Vector3(1,1,1);
 public static bool operator==(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
 public static bool operator!=(Vector3 a,Vector3 b)=>!(a==b);
 public override bool Equals(object? obj)=>obj is Vector3 v&&this==v;
 public override int GetHashCode()=>HashCode.Combine(x,y,z);
}
public struct Quaternion{
 public float x,y,z,w;public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
 public static Quaternion identity=>new Quaternion(0,0,0,1);
 public static bool operator==(Quaternion a,Quaternion b)=>a.x==b.x&&a.y==b.y&&a.z==b.z&&a.w==b.w;
 public static bool operator!=(Quaternion a,Quaternion b)=>!(a==b);
 public override bool Equals(object? o)=>o is Quaternion q&&this==q;
 public override int GetHashCode()=>HashCode.Combine(x,y,z,w);
}
public class Object{
 public static void Destroy(GameObject g){g.SetActive(false);g.Destroyed=true;}
 public static void DestroyImmediate(GameObject g){Destroy(g);}
}
public sealed class Transform{
 public readonly GameObject? gameObject;
 public Transform? parent;public Vector3 position=Vector3.zero,localScale=Vector3.one;
 public Quaternion rotation=Quaternion.identity;public Vector3 lossyScale=>localScale;
 public Transform(GameObject? owner=null){gameObject=owner;}
 public void SetParent(Transform? p,bool worldPositionStays){parent=p;}
}
public class MonoBehaviour:Object{
 public Transform transform=new Transform();public bool isActiveAndEnabled=true;
 private static readonly List<MonoBehaviour> Registry=new List<MonoBehaviour>();
 public static void Register(MonoBehaviour m){if(!Registry.Contains(m))Registry.Add(m);}
 public static void Unregister(MonoBehaviour m){Registry.Remove(m);}
 public static void ResetRegistry(){Registry.Clear();}
 public static T[] FindObjectsByType<T>(FindObjectsInactive i,FindObjectsSortMode s)where T:MonoBehaviour=>Registry.OfType<T>().ToArray();
}
public sealed class Rigidbody{
 public GameObject? gameObject;public float mass=1;public bool isKinematic,useGravity;
 public RigidbodyConstraints constraints;public Vector3 linearVelocity=Vector3.zero,angularVelocity=Vector3.zero;
}
public sealed class GameObject:Object{
 public static readonly List<GameObject> All=new List<GameObject>();
 public string name;public readonly Transform transform;public bool activeSelf=true,Destroyed;public Rigidbody? Body;
 public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject==null||transform.parent.gameObject.activeInHierarchy);
 public GameObject(string n){name=n;transform=new Transform(this);All.Add(this);}
 public void SetActive(bool b){activeSelf=b;}
 public static GameObject CreatePrimitive(PrimitiveType p)=>new GameObject("Cube");
 public T AddComponent<T>()where T:class,new(){T o=new T();if(o is Rigidbody r){Body=r;r.gameObject=this;}return o;}
}
public static class Application{
 public static bool isPlaying=true;public static string persistentDataPath=System.IO.Path.GetTempPath();
}
}