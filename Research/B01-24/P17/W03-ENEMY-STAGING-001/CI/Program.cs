// Executable direct original C# builder validation under explicit UnityEditor API stubs.
// Unity-native .unity importer and R6 PhysX/Player remain NOT_RUN.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RoboRougelike.B0124.Staging;
internal static class Program {
  private static int passed,failed;
  private static void Check(string name,Action action){
    try {action();passed++;Console.WriteLine("PASS "+name);}
    catch(Exception e){failed++;Console.Error.WriteLine("FAIL "+name+": "+e.Message);}
  }
  private static void Must(bool ok,string detail){if(!ok)throw new Exception(detail);}
  private static string Blob(string file) {
    byte[] bytes=File.ReadAllBytes(file);
    byte[] pre=Encoding.ASCII.GetBytes("blob "+bytes.Length+"\0");
    byte[] all=new byte[pre.Length+bytes.Length];
    Buffer.BlockCopy(pre,0,all,0,pre.Length);
    Buffer.BlockCopy(bytes,0,all,pre.Length,bytes.Length);
    return Convert.ToHexString(SHA1.HashData(all)).ToLowerInvariant();
  }
  private static string Root="Research/B01-24/P17/W03-ENEMY-STAGING-001/";
  private static string Input="Research/B01-24/P17/ENEMY-FORMATIONS-002/formations.json";
  public static int Main() {
    Check("exact frozen producer #355 formations Git blob",()=>
      Must(Blob(Input)=="4c28b6289f23bcbd82c976bccb1567cbcb847371","source drift"));
    Check("exact original R5Arena static wall/obstacle geometry Git blob",()=>
      Must(Blob("Assets/R5/R5Arena.cs")=="fbe1537036910e0dcc2b2c7e502e66848fcd14a5","arena drift"));
    Check("Unity script tracked GUID unique and valid",()=>{
      var meta=File.ReadAllText(Root+"Unity/Editor/B0124EnemyStageBuilder.cs.meta");
      Must(meta.Contains("guid: 7a85dd4f01bb4e6ca241c78bdec78d24"),"tracked source guid");
    });
    Check("main source contains editor import menu and stable sandbox target",()=>{
      var code=File.ReadAllText(Root+"Unity/Editor/B0124EnemyStageBuilder.cs");
      Must(code.Contains("[MenuItem(") && code.Contains("NewSceneMode.Additive") &&
           code.Contains("Assets/B0124EnemyStagingW03") &&
           code.Contains("File.Exists(path)"),"safe Unity staging required");
    });
    Check("execute original Unity Editor generator under explicit .NET8 mocks",()=>{
      Must(B0124EnemyStageBuilder.StageCount==3,"not three arena families");
      Must(B0124EnemyStageBuilder.TotalLayouts==12,"not twelve scene variants");
      var receiverScene=new GameObject("UNRELATED RECEIVER SCENE CONTENT — MUST SURVIVE");
      var previous=SceneManager.GetActiveScene();
      B0124EnemyStageBuilder.GenerateAll();
      Must(EditorSceneManager.Saves.Count==12,"twelve distinct stage scenes not generated");
      Must(AssetDatabase.RefreshCalls==1,"expected single asset refresh");
      Must(EditorSceneManager.CloseCalls==12,"disposable additive preview scenes remained open");
      Must(SceneManager.GetActiveScene().Id==previous.Id,"receiver active scene changed");
      Must(GameObject.All.Count==1 && GameObject.All[0]==receiverScene,"receiver content was destroyed");
    });
    using var doc=JsonDocument.Parse(File.ReadAllText(Input));
    foreach(JsonElement formation in doc.RootElement.GetProperty("formations").EnumerateArray()){
      string id=formation.GetProperty("id").GetString()??"";
      int arena=formation.GetProperty("arena").GetInt32();
      int cubeCount=arena==1?7:11; // Floor, 4 R5 boundary walls, 2/6 source arena blockers
      for(int n=1;n<=4;n++){
        int count=n;
        Check(id+" original authored spawn count "+count+" in generated Unity staging scene",()=>{
          string path=B0124EnemyStageBuilder.OutputRoot+"/Scenes/"+id+"-S"+count+".unity";
          var snap=EditorSceneManager.Saves.Single(x=>x.Path==path);
          Must(File.Exists(path),"scene not physically written by API harness");
          Must(snap.Objects.Count(o=>o.primitive==PrimitiveType.Cube)==cubeCount,
               "actual matching R5 obstacle count and ground");
          Must(snap.Objects.Count(o=>o.primitive==PrimitiveType.Cylinder)==count+1,
               "original count player+enemies");
          Must(snap.Objects.Count(o=>o.primitive==PrimitiveType.Sphere)==2,
               "both R5 hazard locations shown");
          Must(snap.Objects.Where(o=>o.primitive==PrimitiveType.Sphere).All(o=>o.collider?.enabled==false),
               "preview markers should not add invisible collisions");
          Must(snap.Objects.Count(o=>o.camera!=null)==1 && snap.Objects.Count(o=>o.light!=null)==1,
               "scene camera+light missing");
          var player=snap.Objects.Single(x=>x.name.StartsWith("Player spawn",StringComparison.Ordinal));
          Must(player.transform.localPosition.x==0 && player.transform.localPosition.z==-22,
               "original R5 player spawn displaced");
          var enemies=snap.Objects.Where(x=>x.name.StartsWith("ENEMY_",StringComparison.Ordinal)).OrderBy(x=>x.name).ToArray();
          var positions=formation.GetProperty("options").GetProperty(count.ToString()).EnumerateArray().ToArray();
          Must(enemies.Length==positions.Length,"enemy vector list length mismatch");
          for(int i=0;i<count;i++){
            float x=positions[i][0].GetSingle(),z=positions[i][1].GetSingle();
            Must(enemies[i].transform.localPosition.x==x && enemies[i].transform.localPosition.z==z,
                 "producer spawn-coordinate mismatch index "+i);
            Must(enemies[i].transform.localScale.x==5 && enemies[i].transform.localScale.z==5,
                 "missing 2.5m radius collision proxy");
          }
          Must(snap.Objects.All(o=>o.transform.parent!=null || o.name.StartsWith("B01-24 W03 |")),
               "unparented scene actors");
        });
      }
    }
    Check("all 12 output names and scenes remain within dedicated new Assets subtree",()=>{
      Must(EditorSceneManager.Saves.Select(x=>x.Path).Distinct().Count()==12,"duplicate overwrite path");
      Must(EditorSceneManager.Saves.All(x=>x.Path.StartsWith("Assets/B0124EnemyStagingW03/Scenes/")),
           "would overwrite canonical game scene");
    });
    Check("fail-closed second run never overwrites existing inspector scenes",()=>{
      int before=EditorSceneManager.Saves.Count;
      try {B0124EnemyStageBuilder.GenerateAll();throw new Exception("Repeat run overwrote stage");}
      catch(IOException) {Must(EditorSceneManager.Saves.Count==before,"partial repeated output");}
    });
    Console.WriteLine("B01-24 W03 original Unity Editor CSharp builder "+passed+" PASS / "+failed+
      " FAIL; fake Editor serializer only; native Unity import/Physics NOT_RUN");
    return failed==0?0:1;
  }
}
