// B01-60: disposable managed test doubles ONLY; never used inside Unity game.
// The geometry under test comes from uncompromised ORIGINAL Assets/R5/R5Arena.cs.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float xx,float yy,float zz){x=xx;y=yy;z=zz;}
    }
    public struct Color {
        public float r,g,b,a;
        public Color(float rr,float gg,float bb){r=rr;g=gg;b=bb;a=1;}
        public static Color operator *(Color c,float n)=>new Color(c.r*n,c.g*n,c.b*n);
    }
    public class Object { public static void Destroy(Object _unused){} }
    public class Component: Object {
        public GameObject gameObject {get;internal set;}
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();
        public T AddComponent<T>()where T:Component,new()=>gameObject.AddComponent<T>();
    }
    public class MonoBehaviour:Component {
        public MonoBehaviour(){gameObject=new GameObject("Original R5Arena managed harness host");}
    }
    public class Transform:Component {
        public Vector3 localPosition;
        public Vector3 localScale=new Vector3(1,1,1);
        public Transform parent;
        public void SetParent(Transform target,bool worldPositionStays){parent=target;}
    }
    public enum PrimitiveType {Cube}
    public sealed class GameObject:Object {
        public static readonly List<GameObject> All=new List<GameObject>();
        private readonly List<Component> components=new List<Component>();
        public string name;
        public bool activeSelf=true;
        public Transform transform {get;}
        public GameObject(string title) {
            name=title;
            transform=new Transform{gameObject=this};
            components.Add(transform);
            All.Add(this);
        }
        public static GameObject CreatePrimitive(PrimitiveType p) {
            if(p!=PrimitiveType.Cube)throw new Exception("Unexpected primitive");
            var go=new GameObject("Cube");
            go.AddComponent<Renderer>();
            return go;
        }
        public static GameObject Find(string title)=>All.LastOrDefault(g=>g.name==title && g.activeSelf);
        public void SetActive(bool active){activeSelf=active;}
        public T AddComponent<T>()where T:Component,new(){
            var component=new T{gameObject=this};
            components.Add(component);return component;
        }
        public T GetComponent<T>()where T:Component=>components.OfType<T>().FirstOrDefault();
    }
    public class Material:Object {
        public Material(){}
        public Material(Material m){}
        public void SetColor(string key,Color color){}
    }
    public class Renderer:Component {
        public Material sharedMaterial;
        public void SetPropertyBlock(MaterialPropertyBlock block){}
    }
    public class MaterialPropertyBlock {
        public void SetColor(string key,Color color){}
    }
    public static class Physics{public static void SyncTransforms(){}}
}
namespace RoboRougelike.PackageIntegration {
    public sealed class RRLCoverAdapter:UnityEngine.Component{}
    public sealed class RRLHazardAdapter:UnityEngine.Component{}
}
namespace RoboRougelike.R5 {
    // Exact enum member order pinned from unchanged ORIGINAL Assets/R5/R5RunPlan.cs.
    public enum R5ArenaKind {SalvageYard,Wasteland,Industrial}
}
