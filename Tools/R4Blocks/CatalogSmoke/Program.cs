// Minimal value-type stubs: this verifies real catalog C# logic, not Unity behavior.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RoboRougelike.R4_1;
namespace UnityEngine
{
    public struct Vector3Int { public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
    public class Object
    {
        public static int Destroyed;
        public static void Destroy(Object value){if(value!=null)Destroyed++;}
        public static void DestroyImmediate(Object value){if(value!=null)Destroyed++;}
    }
    public sealed class Mesh : Object
    {
        public static int FailAfter=-1;
        public string name;public Vector3[] vertices;public int[] triangles;
        public void RecalculateBounds(){if(FailAfter==0)throw new InvalidOperationException("Simulated mesh error");if(FailAfter>0)FailAfter--;}
    }
    public static class Application { public static bool isPlaying; }
    public static class JsonUtility { public static T FromJson<T>(string json)=>JsonSerializer.Deserialize<T>(json,new JsonSerializerOptions{IncludeFields=true}); }
}
namespace B01.BUILD001
{
    public enum BuildSocketKind { Structural }
    public struct BuildSocket
    {
        public BuildSocket(UnityEngine.Vector3Int cell,UnityEngine.Vector3Int face,BuildSocketKind kind,
            UnityEngine.Vector2 halfSize,UnityEngine.Vector2 offset=default) { }
    }
}
internal static class Program
{
    static void Expect(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static int Main()
    {
        try
        {
        Expect(R4BlockCatalog.All.Length==30,"Expected 30 entries");
        Expect(R4BlockCatalog.CanMountModule("R4B_Cube",0),"Cube full port must remain allowed");
        Expect(R4BlockCatalog.CanMountModule("R4B_Wedge",0),"Wedge full port must remain allowed");
        Expect(!R4BlockCatalog.CanMountModule("R4B_Wedge",2),"Partial port must be rejected");
        Expect(!R4BlockCatalog.CanMountModule("R4B_Tetra",0),"Forbidden-module shape must be rejected");
        Expect(!R4BlockCatalog.CanMountModule("R4B_OctColumn",0),"Unsafe octagonal top port must be quarantined");
        Expect(!R4BlockCatalog.CanMountModule("R4B_OctColumn",1),"Unsafe octagonal bottom port must be quarantined");
        Expect(!R4BlockCatalog.CanMountModule("R4B_OctColumn",2),"Partial octagonal side port must be rejected");
        Expect(R4BlockCatalog.CanMountModule("R4B_Wedge",1),"Other full wedge port must remain allowed");
        Expect(!R4BlockCatalog.CanMountModule("missing",0),"Unknown shape must fail closed");
        Expect(!R4BlockCatalog.CanMountModule("R4B_Cube",99),"Invalid socket index must fail closed");
        string json=File.ReadAllText("Tools/R4Blocks/generated/R4BlockColliderMeshes.json");
        Expect(R4ColliderBundle.IsTrustedSource(json),"Pinned raw bundle hash must match");
        Expect(!R4ColliderBundle.IsTrustedSource(json+" "),"Changed raw bundle must fail its hash");
        var bundle=JsonSerializer.Deserialize<R4ColliderBundle>(json,new JsonSerializerOptions{IncludeFields=true});
        Expect(bundle!=null&&bundle.Validate(out _),"Verified 30-shape bundle must pass structural validation");
        var piece=bundle.shapes[0].pieces[0];float position=piece.positions[0];piece.positions[0]=float.NaN;
        Expect(!bundle.Validate(out _),"Non-finite position must fail");piece.positions[0]=position;
        int index=piece.indices[0];piece.indices[0]=int.MaxValue;Expect(!bundle.Validate(out _),"Out-of-range triangle index must fail");piece.indices[0]=index;
        piece.indices[0]=piece.indices[1];Expect(!bundle.Validate(out _),"Degenerate triangle must fail");piece.indices[0]=index;
        string id=bundle.shapes[0].id;bundle.shapes[0].id=bundle.shapes[1].id;Expect(!bundle.Validate(out _),"Duplicate shape must fail");bundle.shapes[0].id=id;
        string pieceName=piece.name;piece.name="wrong";Expect(!bundle.Validate(out _),"Foreign piece name must fail");piece.name=pieceName;
        string revision=bundle.sourceRevision;bundle.sourceRevision="wrong";Expect(!bundle.Validate(out _),"Wrong source revision must fail");bundle.sourceRevision=revision;
        Expect(bundle.Validate(out _),"Restored bundle must pass");
        Expect(R4ColliderMeshFactory.TryCreate(json,"R4B_Arch",out var meshes,out _),"Trusted arch mesh factory call must pass");
        Expect(meshes.Length==3,"Arch must create three separate meshes");
        var arch=Array.Find(bundle.shapes,s=>s.id=="R4B_Arch");
        Expect(meshes[0].name==arch.pieces[0].name && meshes[0].vertices.Length*3==arch.pieces[0].positions.Length &&
            meshes[0].triangles.Length==arch.pieces[0].indices.Length && meshes[0].vertices[0].x==arch.pieces[0].positions[0],"Factory must preserve names and mesh data");
        Expect(!R4ColliderMeshFactory.TryCreate(json+" ","R4B_Arch",out _,out _),"Changed raw bundle must not create meshes");
        Expect(!R4ColliderMeshFactory.TryCreate(json,"missing",out _,out _),"Unknown shape must not create meshes");
        UnityEngine.Object.Destroyed=0;UnityEngine.Mesh.FailAfter=1;
        Expect(!R4ColliderMeshFactory.TryCreate(json,"R4B_Arch",out _,out _),"Failed mesh creation must return false");
        Expect(UnityEngine.Object.Destroyed==2,"Every partially created mesh must be released");UnityEngine.Mesh.FailAfter=-1;
        UnityEngine.Object.Destroyed=0;
        Expect(R4ColliderMeshCache.TryOpen(json,out var cache,out _),"Trusted bundle must open a cache");
        Expect(cache.TryGet("R4B_Arch",out var cachedFirst,out _),"Arch must build once");
        Expect(cache.TryGet("R4B_Arch",out var cachedSecond,out _) && ReferenceEquals(cachedFirst,cachedSecond),"Repeated shape must share cached meshes");
        Expect(cachedFirst.Count==3 && ((IList<UnityEngine.Mesh>)cachedFirst).IsReadOnly && !cache.TryGet("missing",out _,out _),"Cache must expose a read-only collection and reject unknown shape");
        Expect(cache.TryGet("R4B_Cube",out var cubeMeshes,out _) && cubeMeshes.Count==1 && !ReferenceEquals(cachedFirst,cubeMeshes),"Different shapes must keep separate meshes");
        cache.Dispose();Expect(UnityEngine.Object.Destroyed==4,"Dispose must release each cached mesh once");
        cache.Dispose();Expect(UnityEngine.Object.Destroyed==4 && !cache.TryGet("R4B_Arch",out _,out _),"Disposed cache must stay closed and idempotent");
        Expect(!R4ColliderMeshCache.TryOpen(json+" ",out _,out _),"Changed bundle must not open a cache");
        UnityEngine.Object.Destroyed=0;UnityEngine.Mesh.FailAfter=1;
        Expect(R4ColliderMeshCache.TryOpen(json,out var failureCache,out _),"Trusted bundle must open a fresh cache");
        Expect(!failureCache.TryGet("R4B_Arch",out _,out _) && UnityEngine.Object.Destroyed==2,"Failed lazy creation must release partial meshes");
        UnityEngine.Mesh.FailAfter=-1;Expect(failureCache.TryGet("R4B_Arch",out _,out _),"Failed shape creation must be retryable");
        failureCache.Dispose();Expect(UnityEngine.Object.Destroyed==5,"Retry meshes must be released on dispose");
        Console.WriteLine("Catalog smoke PASS: 30 entries, valid full ports, partial and quarantined ports, invalid input.");
        Console.WriteLine("Bundle smoke PASS: pinned source, 30 shapes, geometry structure and six corruption cases.");
        Console.WriteLine("Mesh factory smoke PASS: arch pieces, data, fail-closed inputs and partial cleanup.");
        Console.WriteLine("Mesh cache smoke PASS: reuse, fail-closed inputs, retry and idempotent disposal.");
        return 0;
        }
        catch(Exception error) { Console.Error.WriteLine("Catalog smoke FAIL: "+error.Message); return 1; }
    }
}
