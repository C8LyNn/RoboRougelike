#nullable enable
// EXPLICITLY SYNTHETIC .NET8 ONLY; no native Unity, no rendering or PhysX.
// All FIVE original P12/W24/W25/W27/W28 project sources compile verbatim.
// This UnityEngine shim exists solely for rigorous original-method logical QA.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
 [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
 [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute {public RequireComponent(Type t){}}
 [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
 [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute {public RangeAttribute(float a,float b){}}
 public class Object
 {
   public static void DestroyImmediate(Object? target)
   {
     if(target is Component comp)comp.gameObject.Detach(comp);
     if(target is GameObject go)go.MarkDestroyed();
   }
   protected static void Destroy(Object? obj,float delay=0) {}
 }
 public class Component : Object
 {
   public GameObject gameObject {get; internal set;}=null!;
   public Transform transform=>gameObject.transform;
   public T? GetComponent<T>() where T:Component =>gameObject.GetComponent<T>();
   public T? GetComponentInParent<T>() where T:Component =>gameObject.GetComponent<T>();
   public T[] GetComponentsInChildren<T>(bool incl) where T:Component=>gameObject.GetComponentsInChildren<T>(incl);
 }
 public class MonoBehaviour:Component
 {
   public bool isActiveAndEnabled {get;set;}=true;
   public Coroutine StartCoroutine(IEnumerator routine)=>new Coroutine();
   public void StopCoroutine(Coroutine routine){}
 }
 public sealed class Coroutine {}
 public enum PrimitiveType {Cube}
 public class GameObject : Object
 {
   private readonly List<Component> components=new();
   private readonly List<GameObject> children=new();
   public string name {get;set;}
   public bool activeSelf {get;private set;}=true;
   public bool dead {get;private set;}
   public Transform transform {get;}
   public GameObject(string n="synthetic",bool active=true)
   {name=n;activeSelf=active;transform=new Transform(this);components.Add(transform);}
   public static GameObject CreatePrimitive(PrimitiveType type)
   {var go=new GameObject(type.ToString());go.AddComponent<MeshRenderer>();go.AddComponent<BoxCollider>();return go;}
   public void SetActive(bool active)=>activeSelf=active;
   public void Detach(Component comp)=>components.Remove(comp);
   public void MarkDestroyed()=>dead=true;
   internal void AddChild(GameObject child){if(!children.Contains(child))children.Add(child);}
   internal void RemoveChild(GameObject child){children.Remove(child);}
   public T AddComponent<T>() where T:Component,new()=> (T)AddComponent(typeof(T));
   public Component AddComponent(Type type)
   {
     if(!typeof(Component).IsAssignableFrom(type))throw new ArgumentException(type.ToString());
     var obj=(Component)Activator.CreateInstance(type)!;
     obj.gameObject=this;components.Add(obj);return obj;
   }
   public T? GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
   public T[] GetComponents<T>() where T:Component=>components.OfType<T>().ToArray();
   public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component
   {
     var result=new List<T>();
     void Visit(GameObject node)
     {
       if(!includeInactive&&!node.activeSelf)return;
       result.AddRange(node.components.OfType<T>());
       foreach(var child in node.children)Visit(child);
     }
     Visit(this);return result.ToArray();
   }
 }
 public sealed class Transform:Component
 {
   private Transform? parent;
   internal Transform(GameObject owner){gameObject=owner;}
   public Vector3 position {get;set;}
   public Vector3 localPosition {get;set;}
   public Vector3 localScale {get;set;}=new Vector3(1,1,1);
   public Quaternion rotation {get;set;}
   public void LookAt(Vector3 target){}
   public void SetParent(Transform? next,bool keepWorld)
   {
     parent?.gameObject.RemoveChild(gameObject);
     parent=next;
     next?.gameObject.AddChild(gameObject);
   }
 }
 public readonly struct Vector3
 {
   public readonly float x,y,z;
   public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
   public static Vector3 zero=>new(0,0,0);
   public static Vector3 up=>new(0,1,0);
   public static Vector3 one=>new(1,1,1);
   public Vector3 normalized {get {var length=MathF.Sqrt(x*x+y*y+z*z);return length<=0?zero:new(x/length,y/length,z/length);}}
   public static Vector3 operator *(Vector3 v,float x)=>new(v.x*x,v.y*x,v.z*x);
   public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
 }
 public readonly struct Quaternion
 {public static Quaternion Euler(float a,float b,float c)=>new();}
 public readonly struct Color
 {
   public readonly float r,g,b,a;
   public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
   public static Color white=>new(1,1,1);
   public static Color red=>new(1,0,0);
   public static Color Lerp(Color a,Color b,float t)=>new(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t);
 }
 public sealed class MaterialPropertyBlock {public void SetColor(string key,Color val){}}
 public class Material
 {public bool HasProperty(string s)=>false;public Color GetColor(string s)=>Color.white;}
 public class Renderer:Component
 {
   public Material? sharedMaterial {get;set;}
   public void GetPropertyBlock(MaterialPropertyBlock block){}
   public void SetPropertyBlock(MaterialPropertyBlock block){}
 }
 public class MeshRenderer:Renderer {}
 public class Collider:Component {public bool isTrigger {get;set;}}
 public class BoxCollider:Collider {}
 public class Joint:Component {}
 public class FixedJoint:Joint {}
 public enum CollisionDetectionMode {Discrete,Continuous}
 public enum ForceMode {Impulse}
 public class Rigidbody:Component
 {
   public Vector3 angularVelocity {get;set;}
   public CollisionDetectionMode collisionDetectionMode {get;set;}
   public Vector3 GetPointVelocity(Vector3 at)=>Vector3.zero;
   public void AddForceAtPosition(Vector3 force,Vector3 position,ForceMode mode){}
 }
 public static class Mathf
 {
   public static float Max(float x,float y)=>MathF.Max(x,y);
   public static float Clamp(float x,float min,float max)=>MathF.Max(min,MathF.Min(x,max));
   public static float Clamp01(float x)=>Clamp(x,0,1);
 }
 public class WaitForSeconds {public WaitForSeconds(float s){}}
 public class Camera:Component
 {
   public bool orthographic {get;set;}
   public float orthographicSize {get;set;}
   public CameraClearFlags clearFlags {get;set;}
   public Color backgroundColor {get;set;}
 }
 public enum CameraClearFlags {SolidColor}
 public class Light:Component {public LightType type {get;set;}public float intensity {get;set;}}
 public enum LightType {Directional}
 public readonly struct Rect {public Rect(float x,float y,float w,float h){}}
 public sealed class GUIStyleState {public object box {get;}=new();}
 public static class GUI {public static GUIStyleState skin {get;}=new();}
 public static class GUILayout
 {
   public static void BeginArea(Rect r,object box){}
   public static void Label(string text){}
   public static bool Button(string label)=>false;
   public static void EndArea(){}
 }
 public static class Debug
 {
   public static void Log(string message){}
   public static void LogError(string message){}
 }
}
namespace RoboRougelike.R3 {public sealed class R3Debris:UnityEngine.Component {}}
namespace RoboRougelike.R6
{
 public static class R6CombatFeedback {public static void Hit(object source,UnityEngine.Vector3 pos,bool destroyed){}}
 public static class R6VehicleDrive {public static float PartMass(object source)=>1f;}
 public static class R6DebrisRelease
 {
  public static void Release(UnityEngine.Rigidbody item,UnityEngine.Rigidbody? owner,UnityEngine.Vector3 inherited,UnityEngine.Vector3 spin,float mass){}
 }
}
