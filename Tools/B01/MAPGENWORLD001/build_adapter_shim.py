#!/usr/bin/env python3
"""Write an explicitly NON-UNITY compile shim for actual MAPGENWORLD001 MonoBehaviour C#.
DO NOT ship this generated fake UnityEngine into a Unity project or count it as G1.
"""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[3]
src=root/"Tools/B01/MAPGEN001/Headless/UnitySurrogate.cs"
out=Path(sys.argv[1]) if len(sys.argv)>1 else root/"mapgenworld-evidence/adapter-shim/UnitySurrogate.cs"
content=src.read_text(encoding="utf-8")
target="    public class GameObject {}"
replacement="""    public class GameObject {
        public bool activeInHierarchy = true;
        private readonly System.Collections.Generic.Dictionary<System.Type, object> components
            = new System.Collections.Generic.Dictionary<System.Type, object>();
        public T AddComponent<T>() where T : new() {
            var component = new T(); components[typeof(T)]=component;
            if (component is MonoBehaviour b) b.gameObject=this;
            return component;
        }
        public T GetComponent<T>() where T: class =>
            components.TryGetValue(typeof(T),out var value) ? value as T : null;
    }
    [System.AttributeUsage(System.AttributeTargets.Class)] public sealed class DisallowMultipleComponent : System.Attribute {}
    [System.AttributeUsage(System.AttributeTargets.Class)] public sealed class RequireComponent : System.Attribute {
        public RequireComponent(System.Type type) {}
    }
    public class MonoBehaviour {
        public GameObject gameObject;
        public T GetComponent<T>() where T: class => gameObject.GetComponent<T>();
    }
    public static class Debug {
        public static int Exceptions;
        public static void LogException(System.Exception error, MonoBehaviour obj) { Exceptions++; }
    }"""
if content.count(target)!=1:raise SystemExit("STUB_API_CHANGED")
out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(content.replace(target,replacement),encoding="utf-8")
print("SIMULATED_UNITY_CSHARP_COMPILE_SHIM_GENERATED_NOT_NATIVE")
