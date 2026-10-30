using System;
using System.Collections.Generic;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    public class MonoBehaviour {}
    public class Transform { public Vector3 position; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float xx, float yy, float zz) { x=xx;y=yy;z=zz; }
    }
    public enum KeyCode { F7, W, S }
    public static class Time { public static double realtimeSinceStartupAsDouble; }
    public static class Input
    {
        public static readonly HashSet<KeyCode> KeysHeld = new HashSet<KeyCode>();
        public static readonly HashSet<KeyCode> KeysDown = new HashSet<KeyCode>();
        public static bool FailLegacy;
        public static bool GetKeyDown(KeyCode k)
        {
            if (FailLegacy) throw new InvalidOperationException("legacy input unavailable");
            return KeysDown.Contains(k);
        }
        public static bool GetKey(KeyCode k)
        {
            if (FailLegacy) throw new InvalidOperationException("legacy input unavailable");
            return KeysHeld.Contains(k);
        }
        public static void Reset() { KeysHeld.Clear();KeysDown.Clear();FailLegacy=false; }
    }
    public static class Mathf
    {
        public static float Max(float a,float b) { return Math.Max(a,b); }
        public static float Clamp(float a,float lo,float hi) { return Math.Max(lo,Math.Min(hi,a)); }
    }
    public struct Rect { public Rect(float x,float y,float w,float h){} }
    public static class GUI
    {
        public class Skin { public object box = new object(); }
        public static Skin skin = new Skin();
    }
    public static class GUILayout
    {
        public static readonly List<string> Labels = new List<string>();
        public static bool NextButton;
        public static void BeginArea(Rect rect,object style){}
        public static void EndArea(){}
        public static void Label(string s) { Labels.Add(s); }
        public static bool Button(string label)
        {
            bool v=NextButton;NextButton=false;return v;
        }
        public static void Reset(){ Labels.Clear();NextButton=false; }
    }
}
