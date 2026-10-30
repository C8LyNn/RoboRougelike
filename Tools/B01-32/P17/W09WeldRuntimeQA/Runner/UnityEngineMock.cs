// TEST-ONLY SURROGATE: absolutely NOT a real UnityEngine implementation.
// The exact unmodified producer MonoBehaviour is compiled/executed against it.
// Every fabricated API below is explicitly in test-only code.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    public class Object
    {
        public bool WasDestroyed { get; protected set; }
        public static T Instantiate<T>(T source) where T : Object
        {
            if (source is GameObject go) {
                var cloned = new GameObject(go.name + "(Clone)");
                if (go.GetComponent<RoboRougelike.B0132P17.RepairWeld.B0132RepairWeldVfx>() != null)
                    cloned.AddComponent<RoboRougelike.B0132P17.RepairWeld.B0132RepairWeldVfx>();
                return (T)(Object)cloned;
            }
            throw new InvalidOperationException("Test-only resources support GameObject only");
        }
        public static void Destroy(Object target)
        {
            if (target == null) return;
            target.WasDestroyed = true;
            if (target is GameObject go) {
                foreach (var c in go.GetAllComponents()) {
                    c.InvokeLifecycle("OnDisable");
                    c.InvokeLifecycle("OnDestroy");
                }
                foreach (var child in go.transform.Children.ToArray()) Destroy(child.gameObject);
            }
        }
    }
    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T[] GetComponentsInChildren<T>() where T : Component => gameObject.GetComponentsInChildren<T>();
        public void InvokeLifecycle(string name)
        {
            var m=GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
            m?.Invoke(this,null);
        }
    }
    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled && gameObject != null && gameObject.activeSelf;
    }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public string name;
        public bool activeSelf=true;
        public Transform transform {get;}
        public GameObject(string label="New GameObject")
        {
            name=label;
            transform=new Transform{gameObject=this};
            components.Add(transform);
        }
        public T AddComponent<T>() where T : Component,new()
        {
            var t=new T{gameObject=this};
            components.Add(t);
            t.InvokeLifecycle("Awake");
            return t;
        }
        public T GetComponent<T>() where T:Component => components.OfType<T>().FirstOrDefault();
        public T[] GetComponentsInChildren<T>() where T:Component =>
            components.OfType<T>().Concat(transform.Children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>())).ToArray();
        public IEnumerable<Component> GetAllComponents() => components.ToArray();
        public void SetActive(bool active)
        {
            if (activeSelf && !active) foreach(var c in components)c.InvokeLifecycle("OnDisable");
            activeSelf=active;
        }
    }
    public class Transform : Component
    {
        public Transform parent;
        public List<Transform> Children {get;}=new List<Transform>();
        public void SetParent(Transform next,bool worldPositionStays=false)
        {
            parent?.Children.Remove(this);
            parent=next;
            next?.Children.Add(this);
        }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float a,float b,float c){x=a;y=b;z=c;}
    }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float x,float y,float z,float w){r=x;g=y;b=z;a=w;}
    }
    public class Shader : Object
    {
        public string name;
        public static bool PrimaryAvailable=true;
        public static bool FallbackAvailable=true;
        public static Shader Find(string path)
        {
            if (path=="Sprites/Default"&&PrimaryAvailable) return new Shader{name=path};
            if (path=="Universal Render Pipeline/Unlit"&&FallbackAvailable) return new Shader{name=path};
            return null;
        }
    }
    public class Material : Object
    {
        public Shader shader;
        public Material(Shader value){shader=value;}
    }
    public class LineRenderer : Behaviour
    {
        public bool useWorldSpace;
        public int positionCount, numCapVertices,numCornerVertices;
        public float startWidth,endWidth;
        public Material sharedMaterial;
        public Color startColor,endColor;
        public Vector3[] Positions=new Vector3[2];
        public void SetPosition(int index,Vector3 value)
        {
            if(index<0||index>=2)throw new ArgumentOutOfRangeException(nameof(index));
            Positions[index]=value;
        }
    }
    public static class Time {public static float deltaTime=1f/60f;}
    public static class Resources
    {
        public static string VerifiedPrefabricatedPath;
        public static T Load<T>(string name) where T:Object
        {
            if(name!="B0132_RepairWeld"||!File.Exists(VerifiedPrefabricatedPath))return null;
            var mock=new GameObject("MOCKONLY_B0132_RepairWeld");
            mock.AddComponent<RoboRougelike.B0132P17.RepairWeld.B0132RepairWeldVfx>();
            return mock as T;
        }
    }
}
namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class UnityTestAttribute:Attribute{}
}
namespace NUnit.Framework
{
    public static class Assert
    {
        public static int Calls;
        public static void IsTrue(bool x,string message=""){Calls++;if(!x)throw new Exception("AUTHORED_ASSERT_TRUE_FAIL "+message);}
        public static void IsFalse(bool x,string message=""){Calls++;if(x)throw new Exception("AUTHORED_ASSERT_FALSE_FAIL "+message);}
        public static void IsNotNull(object x,string message=""){Calls++;if(x==null)throw new Exception("AUTHORED_ASSERT_NOT_NULL_FAIL "+message);}
        public static void AreEqual<T>(T expected,T actual,string message=""){Calls++;if(!object.Equals(expected,actual))throw new Exception("AUTHORED_ASSERT_EQUAL_FAIL "+expected+" vs "+actual+" "+message);}
    }
}
