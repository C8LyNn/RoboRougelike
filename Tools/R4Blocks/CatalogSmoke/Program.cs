// Minimal value-type stubs: this verifies real catalog C# logic, not Unity behavior.
using System;
namespace UnityEngine
{
    public struct Vector3Int { public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
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
        Console.WriteLine("Catalog smoke PASS: 30 entries, valid full ports, partial and quarantined ports, invalid input.");
        return 0;
        }
        catch(Exception error) { Console.Error.WriteLine("Catalog smoke FAIL: "+error.Message); return 1; }
    }
}
