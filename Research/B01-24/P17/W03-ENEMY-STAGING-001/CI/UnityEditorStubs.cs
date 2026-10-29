// Test-only Unity/UnityEditor surrogate to EXECUTE unmodified original Editor scene builder
// under .NET8 on CI, without pretending this is Unity scene serialization or PhysX.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
  public enum PrimitiveType { Cube, Cylinder, Sphere }
  public enum LightType { Directional }
  public struct Vector2 {
    public float x,y;
    public Vector2(float x,float y){this.x=x;this.y=y;}
  }
  public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  }
  public struct Quaternion {
    public float x,y,z;
    public static Quaternion Euler(float x,float y,float z)=>new Quaternion{x=x,y=y,z=z};
  }
  public class Collider { public bool enabled=true; }
  public class Camera {
    public bool orthographic;
    public float orthographicSize,nearClipPlane,farClipPlane;
  }
  public class Light {public LightType type;public float intensity;}
  public class Transform {
    public GameObject gameObject;
    public Transform? parent;
    public Vector3 localPosition,localScale=new Vector3(1,1,1);
    public Quaternion rotation;
    public Vector3 position {get=>localPosition;set=>localPosition=value;}
    public Transform(GameObject owner){gameObject=owner;}
    public void SetParent(Transform owner,bool worldPositionStays){parent=owner;}
  }
  public class GameObject {
    public static readonly List<GameObject> All=new();
    public string name,tag="";
    public readonly Transform transform;
    public PrimitiveType? primitive;
    public Collider? collider;
    public Camera? camera;
    public Light? light;
    public GameObject(string name){this.name=name;transform=new Transform(this);All.Add(this);}
    public static GameObject CreatePrimitive(PrimitiveType kind){
      var go=new GameObject("Primitive "+kind);go.primitive=kind;go.collider=new Collider();return go;
    }
    public T AddComponent<T>() where T:class,new(){
      var component=new T();
      if(component is Camera cam)camera=cam;
      else if(component is Light l)light=l;
      else throw new InvalidOperationException("Unsupported stub API");
      return component;
    }
    public T GetComponent<T>() where T:class {
      if(typeof(T)==typeof(Collider) && collider!=null)return (T)(object)collider;
      throw new InvalidOperationException("Missing stub component "+typeof(T).Name);
    }
  }
  public static class Debug {public static readonly List<string> Messages=new(); public static void Log(object text)=>Messages.Add(text.ToString()??"");}
}
namespace UnityEditor {
  [AttributeUsage(AttributeTargets.Method)]
  public sealed class MenuItemAttribute:Attribute {
    public MenuItemAttribute(string menuPath){}
  }
  public static class AssetDatabase {public static int RefreshCalls;public static void Refresh(){RefreshCalls++;}}
}
namespace UnityEngine.SceneManagement {
  public struct Scene {
    public int Id;
    public Scene(int id){Id=id;}
  }
  public static class SceneManager {
    private static Scene active=new Scene(0);
    public static Scene GetActiveScene()=>active;
    public static bool SetActiveScene(Scene scene){active=scene;return true;}
  }
}
namespace UnityEditor.SceneManagement {
  using UnityEngine.SceneManagement;
  public enum NewSceneSetup {EmptyScene}
  public enum NewSceneMode {Single,Additive}
  public sealed class Snapshot {
    public string Path="";
    public List<UnityEngine.GameObject> Objects=new();
  }
  public static class EditorSceneManager {
    public static readonly List<Snapshot> Saves=new();
    private static readonly Dictionary<int,int> firstIndex=new();
    private static int nextId;
    public static int CloseCalls;
    public static Scene NewScene(NewSceneSetup setup,NewSceneMode mode) {
      if(setup!=NewSceneSetup.EmptyScene||mode!=NewSceneMode.Additive)
        throw new Exception("Refused unsafe overwrite of pre-existing scene");
      var scene=new Scene(++nextId);
      firstIndex.Add(scene.Id,UnityEngine.GameObject.All.Count);
      return scene;
    }
    public static bool SaveScene(Scene scene,string path) {
      // Mock output marker ONLY. Not valid Unity scene YAML.
      Directory.CreateDirectory(Path.GetDirectoryName(path)??".");
      File.WriteAllText(path,"MOCK_SCENE_NOT_NATIVE_UNITY\n");
      Saves.Add(new Snapshot{Path=path,Objects=UnityEngine.GameObject.All.Skip(firstIndex[scene.Id]).ToList()});
      return true;
    }
    public static bool CloseScene(Scene scene,bool removeScene) {
      if(!removeScene)throw new Exception("Expected dispose temporary scene");
      int first=firstIndex[scene.Id];
      UnityEngine.GameObject.All.RemoveRange(first,UnityEngine.GameObject.All.Count-first);
      firstIndex.Remove(scene.Id);CloseCalls++;
      return true;
    }
  }
}
