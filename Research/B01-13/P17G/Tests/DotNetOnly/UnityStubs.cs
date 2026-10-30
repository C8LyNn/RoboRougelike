using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    public class MonoBehaviour {}
    public enum KeyCode { F8, W, A, S, D, Space }
    public static class Time { public static double realtimeSinceStartupAsDouble; }
    public static class Input
    {
        public static readonly HashSet<KeyCode> Keys = new HashSet<KeyCode>();
        public static readonly HashSet<int> Mouse = new HashSet<int>();
        public static bool FailLegacy;
        public static bool GetKeyDown(KeyCode key)
        {
            if (FailLegacy) throw new InvalidOperationException("New Input System only");
            return Keys.Contains(key);
        }
        public static bool GetMouseButtonDown(int index)
        {
            if (FailLegacy) throw new InvalidOperationException("New Input System only");
            return Mouse.Contains(index);
        }
        public static void Clear()
        {
            Keys.Clear();
            Mouse.Clear();
            FailLegacy = false;
        }
    }
    public static class Mathf
    {
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static float Clamp(float a, float low, float high) { return Math.Max(low, Math.Min(a, high)); }
    }
    public struct Rect { public Rect(float x, float y, float w, float h) {} }
    public static class GUI
    {
        public class GuiSkin { public object box = new object(); }
        public static GuiSkin skin = new GuiSkin();
    }
    public static class GUILayout
    {
        public static readonly List<string> Labels = new List<string>();
        public static bool NextButtonPress;
        public static void BeginArea(Rect bounds, object skin) {}
        public static void EndArea() {}
        public static void Label(string s) { Labels.Add(s); }
        public static bool Button(string s)
        {
            bool pressed = NextButtonPress;
            NextButtonPress = false;
            return pressed;
        }
        public static void Clear() { Labels.Clear(); NextButtonPress = false; }
    }
}
