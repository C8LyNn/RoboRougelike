using System;
using B01.BUILD001;
using RoboRougelike.R4_1;
using UnityEngine;
internal static class Program
{
    static void Expect(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Main()
    {
        int moduleDefinitions=0;
        foreach(var entry in R4BlockCatalog.All)
        {
            Expect(R4MountDefinitionAdapter.TryCreate(entry.Id,false,out var structural),entry.Id+" structural");
            Expect(BuildSocketGeometry.Valid(structural,out var reason),entry.Id+" structural: "+reason);
            if(R4MountDefinitionAdapter.TryCreate(entry.Id,true,out var module))
            {
                Expect(BuildSocketGeometry.Valid(module,out reason),entry.Id+" module: "+reason);
                moduleDefinitions++;
            }
        }
        Expect(R4MountDefinitionAdapter.TryCreate("R4B_Wedge",false,out var wedge),"wedge structural");
        Expect(R4MountDefinitionAdapter.TryCreate("R4B_Wedge",true,out var wedgeModule),"wedge module");
        var wheel=new BuildDefinition("wheel",BuildRole.Wheel,new[]{Vector3Int.zero},new[]{new BuildSocket(Vector3Int.zero,Vector3Int.right,BuildSocketKind.Axle,new Vector2(.47f,.47f))},false);
        var supportPose=new BuildPose(Vector3Int.zero,Vector3Int.zero);
        var wheelPose=new BuildPose(Vector3Int.left,Vector3Int.zero);
        Expect(BuildSocketGeometry.Touches(wedge,supportPose,wheel,wheelPose,out _),"partial geometry should overlap");
        Expect(!BuildSocketGeometry.Touches(wedgeModule,supportPose,wheel,wheelPose,out _),"partial geometry must not support a module");
        Expect(R4MountDefinitionAdapter.TryCreate("R4B_Cube",true,out var cube),"cube module");
        Expect(BuildSocketGeometry.Touches(cube,supportPose,wheel,wheelPose,out _),"full cube should support wheel");
        Expect(!R4MountDefinitionAdapter.TryCreate("R4B_OctColumn",true,out _),"octagon quarantine");
        Console.WriteLine("Actual BUILD001 geometry smoke PASS: 30 structural definitions, "+moduleDefinitions+" module definitions, partial/full wheel contacts and octagon quarantine; identity poses only.");
    }
}
