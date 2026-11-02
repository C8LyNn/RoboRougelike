#nullable enable
// Synthetic Editor/Scenes API ONLY for compilation of actual original W27 Editor source.
using System;
using UnityEngine;
namespace UnityEngine.SceneManagement
{
 public struct Scene {public bool isLoaded {get;set;}public bool IsValid()=>true;}
 public static class SceneManager
 {
   public static Scene GetActiveScene()=>new Scene{isLoaded=true};
   public static void SetActiveScene(Scene scene){}
   public static void MoveGameObjectToScene(GameObject go,Scene scene){}
 }
}
namespace UnityEditor
{
 [AttributeUsage(AttributeTargets.Method)]
 public sealed class MenuItem:Attribute {public MenuItem(string label){}}
 public class SceneAsset:UnityEngine.Object {}
 public static class EditorApplication {public static bool isPlayingOrWillChangePlaymode=>false;}
 public static class EditorUtility
 {public static bool DisplayDialog(string title,string message,string yes,string no)=>false;}
 public static class AssetDatabase
 {
  public static T? LoadAssetAtPath<T>(string path) where T:UnityEngine.Object=>null;
  public static bool IsValidFolder(string s)=>false;
  public static void CreateFolder(string path,string folder){}
 }
 public static class PrefabUtility
 {public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object prefab,UnityEngine.SceneManagement.Scene scene)=>new GameObject();}
}
namespace UnityEditor.SceneManagement
{
 public enum NewSceneSetup {EmptyScene}
 public enum NewSceneMode {Additive}
 public static class EditorSceneManager
 {
  public static bool SaveCurrentModifiedScenesIfUserWantsTo()=>false;
  public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup,NewSceneMode mode)=>new UnityEngine.SceneManagement.Scene{isLoaded=true};
  public static bool SaveScene(UnityEngine.SceneManagement.Scene scene,string output,bool saveAsCopy)=>false;
  public static void CloseScene(UnityEngine.SceneManagement.Scene scene,bool remove){}
 }
}
