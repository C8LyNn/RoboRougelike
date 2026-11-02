#nullable enable
// Explicit SYNTHETIC standalone .NET8 UI stubs. The game source P12 and
// actual W24 W25 W27 scripts are VERBATIM, none are replaced.
namespace UnityEngine
{
    public readonly struct Rect
    { public Rect(float a,float b,float c,float d) {} }
    public static class GUI
    { public static GUIStyleState skin { get; } = new GUIStyleState(); }
    public sealed class GUIStyleState
    { public object box { get; } = new object(); }
    public static class GUILayout
    {
        public static void BeginArea(Rect r,object style) {}
        public static void EndArea() {}
        public static void Label(string text) {}
        public static bool Button(string text) => false;
    }
}
