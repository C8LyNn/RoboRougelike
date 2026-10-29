// Compiles and executes the ACTUAL original Unity Editor import check.
// Explicit shims are NOT a substitute for native Unity 6000.3.14f1.
using System;
using System.Collections.Generic;
using System.Linq;
using B0130.M04UnityIntake;
using UnityEditor;
using UnityEngine;

namespace UnityEngine
{
    public class Object { public string name; }
    public class GameObject : Object
    {
        public Transform[] Nodes = new Transform[0];
        public MeshFilter[] Meshes = new MeshFilter[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            if (typeof(T) == typeof(Transform)) return Nodes as T[];
            if (typeof(T) == typeof(MeshFilter)) return Meshes as T[];
            throw new Exception("unknown Unity API shim: " + typeof(T));
        }
    }
    public class Transform : Object {}
    public class MeshFilter : Object {}
    public class AnimationClip : Object {}
    public static class Debug
    {
        public static readonly List<string> Errors = new List<string>();
        public static readonly List<string> Messages = new List<string>();
        public static void LogError(string s) { Errors.Add(s); }
        public static void Log(string s) { Messages.Add(s); }
    }
}
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute { public MenuItem(string _) {} }
    public enum ModelImporterAnimationType { None, Legacy, Generic, Human }
    public class AssetImporter
    {
        public static ModelImporter Current;
        public static AssetImporter GetAtPath(string _) { return Current; }
    }
    public class ModelImporter : AssetImporter
    {
        public ModelImporterAnimationType animationType;
        public bool importAnimation;
        public float globalScale;
        public static int Reimports;
        public void SaveAndReimport() { Reimports++; }
    }
    public struct EditorCurveBinding
    {
        public string path;
        public string propertyName;
    }
    public static class AssetDatabase
    {
        public static GameObject Model;
        public static UnityEngine.Object[] Subassets = new UnityEngine.Object[0];
        public static T LoadAssetAtPath<T>(string path) where T : class
        {
            if (path != "Assets/B0130M04Intake001/M04-024-4GIMBALS-ORIGINAL.fbx")
                throw new Exception("outside own model scope");
            return Model as T;
        }
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string path)
        {
            if (path != "Assets/B0130M04Intake001/M04-024-4GIMBALS-ORIGINAL.fbx")
                throw new Exception("outside own model scope");
            return Subassets;
        }
    }
    public static class AnimationUtility
    {
        public static readonly Dictionary<string,EditorCurveBinding[]> Bindings =
            new Dictionary<string,EditorCurveBinding[]>();
        public static EditorCurveBinding[] GetCurveBindings(AnimationClip clip)
        {
            return Bindings.ContainsKey(clip.name) ? Bindings[clip.name] : new EditorCurveBinding[0];
        }
    }
}

public static class ImporterTests
{
    static readonly string[] Names = {"N01_GIMBAL_PIVOT","N02_GIMBAL_PIVOT","N03_GIMBAL_PIVOT","N04_GIMBAL_PIVOT"};
    static int assertions=0;
    static void Check(bool yes,string message)
    {
        if(!yes) throw new Exception("B01-30 MOCK SOURCE RED: "+message);
        assertions++;
    }
    static void Reset()
    {
        UnityEngine.Debug.Errors.Clear();UnityEngine.Debug.Messages.Clear();
        AssetImporter.Current=new ModelImporter{animationType=ModelImporterAnimationType.None,
            globalScale=2f,importAnimation=false};
        ModelImporter.Reimports=0;
        AssetDatabase.Model=new GameObject{
            Nodes=Names.Select(n=>new Transform{name=n}).ToArray(),
            Meshes=Enumerable.Range(0,105).Select(i=>new MeshFilter{name="Mesh"+i}).ToArray()
        };
        var clip=new AnimationClip{name="M04_4GimbalTake"};
        AssetDatabase.Subassets=new UnityEngine.Object[]{clip};
        AnimationUtility.Bindings.Clear();
        AnimationUtility.Bindings["M04_4GimbalTake"]=Names.Select(n=>new EditorCurveBinding{
            path="ROOT_UNDERSIDE_MOUNT_Z0/"+n,propertyName="m_LocalRotation.x"}).ToArray();
    }
    public static int Main()
    {
        Reset();
        M04CandidateImportCheck.PrepareAndCheck();
        Check(ModelImporter.Reimports==1,"opt-in importer settings must be applied exactly once");
        var importer=(ModelImporter)AssetImporter.Current;
        Check(importer.animationType==ModelImporterAnimationType.Generic,"generic animation type");
        Check(importer.importAnimation,"import animation");
        Check(importer.globalScale==1f,"unity import scale exactly 1");
        Check(UnityEngine.Debug.Errors.Count==0,"valid 4 joint common-clip");
        Check(UnityEngine.Debug.Messages.Count==1,"valid acceptance log");
        M04CandidateImportCheck.PrepareAndCheck();
        Check(ModelImporter.Reimports==1,"idempotent second run");
        Check(M04CandidateImportCheck.VerifyImported(),"read-only source positive");
        Reset();AssetImporter.Current=null;
        M04CandidateImportCheck.PrepareAndCheck();
        Check(ModelImporter.Reimports==0 && UnityEngine.Debug.Errors.Count==1,
              "missing importer fails closed before writing");
        Reset();AssetDatabase.Model=null;
        Check(!M04CandidateImportCheck.VerifyImported(),"missing object fails closed");
        Reset();AssetDatabase.Model.Meshes=AssetDatabase.Model.Meshes.Take(104).ToArray();
        Check(!M04CandidateImportCheck.VerifyImported(),"104/105 mesh loss rejected");
        Reset();AssetDatabase.Model.Nodes=AssetDatabase.Model.Nodes.Take(3).ToArray();
        Check(!M04CandidateImportCheck.VerifyImported(),"missing fourth pivot rejected");
        Reset();AssetDatabase.Model.Nodes=AssetDatabase.Model.Nodes.Concat(new[]{new Transform{name=Names[0]}}).ToArray();
        Check(!M04CandidateImportCheck.VerifyImported(),"duplicate pivot rejected");
        Reset();AnimationUtility.Bindings["M04_4GimbalTake"]=Names.Take(3).Select(n=>new EditorCurveBinding{
            path=n,propertyName="m_LocalRotation.w"}).ToArray();
        Check(!M04CandidateImportCheck.VerifyImported(),"one of four missing rotation keys rejected");
        Reset();AnimationUtility.Bindings["M04_4GimbalTake"]=Names.Select(n=>new EditorCurveBinding{
            path=n,propertyName="m_LocalPosition.x"}).ToArray();
        Check(!M04CandidateImportCheck.VerifyImported(),"translation-only not accepted as gimbal tilt");
        Reset();
        AssetDatabase.Subassets=new UnityEngine.Object[]{new AnimationClip{name="A"},new AnimationClip{name="B"}};
        AnimationUtility.Bindings["A"]=Names.Take(2).Select(n=>new EditorCurveBinding{
            path=n,propertyName="localEulerAnglesRaw.x"}).ToArray();
        AnimationUtility.Bindings["B"]=Names.Skip(2).Select(n=>new EditorCurveBinding{
            path=n,propertyName="localEulerAnglesRaw.y"}).ToArray();
        Check(!M04CandidateImportCheck.VerifyImported(),"split animations across clips not accepted as one");
        Reset();
        AssetDatabase.Subassets=new UnityEngine.Object[]{new AnimationClip{name="A"},new AnimationClip{name="M04_4GimbalTake"}};
        AnimationUtility.Bindings["A"]=new EditorCurveBinding[0];
        Check(M04CandidateImportCheck.VerifyImported(),"empty extra clip does not negate complete take");
        Console.WriteLine("B01_30_M04_UNITY_IMPORT_ACTUAL_CSHARP_SHIMS_PASS "+assertions+" assertions");
        return 0;
    }
}
