using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using B01.PARTSIZE001;
internal static class Smoke
{
  static int asserts;
  static void Check(bool yes,string why) { if(!yes) throw new Exception("FAIL "+why); asserts++; }
  static Cell3 Point(JsonElement e) => new Cell3(e.GetProperty("x").GetInt32(),e.GetProperty("y").GetInt32(),e.GetProperty("z").GetInt32());
  static void Main(string[] paths)
  {
    Check(paths.Length==3,"exact original trio");
    foreach(var path in paths)
    {
      using var doc=JsonDocument.Parse(File.ReadAllText(path));
      var rig=doc.RootElement;
      Check(rig.GetProperty("schema").GetString()=="B01-PARTSIZE-P17-RIG-BLUEPRINT-v1","schema");
      Check(rig.GetProperty("nativePhysicalStatus").GetString()=="UNMEASURED_COLLIDER_AND_SOCKET","unmeasured");
      Check(rig.GetProperty("sourcePartSizeCommit").GetString()=="be2be0e4802bc3f450179901f9ae91c6d84bcb5b","source");
      int cores=0,guns=0;
      var occupied=new HashSet<Cell3>();
      var ids=new HashSet<string>();
      var parts=rig.GetProperty("parts");
      foreach(var part in parts.EnumerateArray())
      {
        string kind=part.GetProperty("kind").GetString();
        Check(ids.Add(part.GetProperty("localId").GetString()),"partid");
        if(kind=="Core") cores++;
        if(kind=="Gun") guns++;
        var def=PartSizeBaselineCatalog.ForSourceKind(kind);
        foreach(var cell in PartSizeGrid.Occupied(def,Point(part.GetProperty("cell")),Point(part.GetProperty("rotationQuarterTurns"))))
          Check(occupied.Add(cell),"no overlap");
      }
      Check(cores==1 && guns>=1,"core guns");
      Check(parts.GetArrayLength()>=10,"part count");
      Console.WriteLine("ORIGINAL_RIG_PASS "+rig.GetProperty("blueprintId").GetString());
    }
    Check(PartSizeGrid.Occupied(PartSizeBaselineCatalog.ForSourceKind("BonusModule"),new Cell3(0,0,0),new Cell3(0,1,0)).Length==4,"rotated multi-cell");
    Check(PartSizeGrid.DefinitionError(PartSizeBaselineCatalog.ForSourceKind("Wheel"))=="UNMEASURED_PHYSICAL_ENVELOPE","no false physical approval");
    Console.WriteLine("ORIGINAL_CSHARP_RIGS_PASS=3 ASSERTIONS="+asserts+" NATIVE_UNITY=NOT_RUN");
  }
}
