// Test-only stand-ins to compile the actual unchanged Unity Editor source; never imported into Assets.
using System;
using System.Text.Json;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public class TextAsset { public string text=""; }
 public static class JsonUtility { public static T FromJson<T>(string value) =>
   JsonSerializer.Deserialize<T>(value,new JsonSerializerOptions{IncludeFields=true})!; }
}
namespace UnityEditor {
 using UnityEngine;
 [AttributeUsage(AttributeTargets.Method)] public sealed class MenuItem:Attribute {
   public MenuItem(string label) {} }
 public enum MessageType { Info,Error }
 public sealed class GUIStyle {}
 public static class EditorStyles { public static GUIStyle boldLabel=new GUIStyle(); }
 public class EditorWindow {
   public Vector2 minSize;
   public static T GetWindow<T>(string title) where T:EditorWindow,new()=>new T();
   protected void Repaint() {}
 }
 public static class AssetDatabase { public static T LoadAssetAtPath<T>(string path) where T:class =>
    (System.IO.File.Exists(path) ? new TextAsset { text=System.IO.File.ReadAllText(path) } : null) as T; }
 public static class GUILayout {
   public static void Label(string text,GUIStyle style) {}
   public static bool Button(string text)=>false;
 }
 public static class EditorGUILayout {
   public static void HelpBox(string text,MessageType mode) {}
   public static string TextField(string name,string value)=>value;
   public static Vector2 BeginScrollView(Vector2 value)=>value;
   public static void EndScrollView(){}
   public static void LabelField(string text){}
 }
 public static class EditorGUIUtility { public static string systemCopyBuffer=""; }
}
