using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute {}
    public class MonoBehaviour {}
    public enum KeyCode { F9 }
    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) { return false; }
    }
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
    }
    public struct Rect
    {
        public Rect(float x, float y, float w, float h) {}
    }
    public static class GUI
    {
        public sealed class Skin { public object box = new object(); }
        public static Skin skin = new Skin();
    }
    public static class GUILayout
    {
        public static void BeginArea(Rect bounds, object style) {}
        public static void EndArea() {}
        public static void Label(string text) {}
        public static bool Button(string text) { return false; }
    }
}
