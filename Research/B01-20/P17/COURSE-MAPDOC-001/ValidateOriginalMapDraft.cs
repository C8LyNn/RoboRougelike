// COURSE-MAPDOC-001: TEST the original Codex MapDraft.TryLoad C# implementation.
// MapDraft.cs is fetched read-only at immutable commit, NEVER duplicated in this PR.
// UnityEngine JsonUtility is replaced with an explicit source-only stub (not Unity runtime).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using RoboRougelike.MapEditor;
using UnityEngine;

static class VerifyOriginalCodex
{
 const string OriginalMapDraftBlob="0331a54a1545ecd1ea18b616ef13fed113474b8c";
 static void Check(bool c,string e){if(!c)throw new Exception("MAPDOC_CSHARP_FAIL:"+e);}
 static MapDraft Load(string input) {
    var d=new MapDraft();
    Check(d.TryLoad(input,out var err),"ORIGINAL_TRYLOAD:"+err);
    Check(d.TestError()==null,"ORIGINAL_TESTERROR:"+d.TestError());
    return d;
 }
 static void Reject(MapDocument doc,string why) {
    var d=new MapDraft();
    Check(!d.TryLoad(JsonUtility.ToJson(doc),out var error),"NEGATIVE_ACCEPTED_"+why);
    Check(!string.IsNullOrWhiteSpace(error),"NEGATIVE_NO_ERROR_"+why);
 }
 static int Main(string[] args) {
    try {
      if(args.Length!=2)throw new Exception("Usage: VerifyOriginalCodex <native-mapdir> <receipt>");
      var directory=args[0];
      var results=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"receipt.json"))).RootElement;
      Check(results.GetProperty("maps").GetInt32()==18,"RECEIPT_COUNT");
      var entries=results.GetProperty("converted").EnumerateArray().ToArray();
      Check(entries.Length==18,"ENTRY_COUNT");
      var hashes=new HashSet<string>();
      var counts=new Dictionary<int,int>();
      int cases=0,neg=0;
      foreach(var entry in entries) {
        string name=entry.GetProperty("map").GetString()!;
        string path=Path.Combine(directory,name);
        byte[] bytes=File.ReadAllBytes(path);
        string sha=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Check(sha==entry.GetProperty("mapSha256").GetString(),"GIT_ARTIFACT_HASH_"+name);
        Check(hashes.Add(sha),"DUPLICATE_NATIVE_MAP");
        var json=System.Text.Encoding.UTF8.GetString(bytes);
        var d=Load(json);
        var doc=JsonUtility.FromJson<MapDocument>(json)!;
        Check(doc.version==2 && !doc.globalFloor,"NATIVE_MAPDOC_FORMAT");
        Check(doc.items.All(i=>i.scaleX==1&&i.scaleY==1&&i.scaleZ==1&&i.rotation==0),"GRID_ITEM_SHAPE");
        Check(doc.items.Count(i=>i.kind==MapItemKind.Spawn)==1 &&
              doc.items.Count(i=>i.kind==MapItemKind.Finish)==1,"SPAWN_FINISH");
        int floors=doc.items.Count(i=>i.kind==MapItemKind.Ground);
        int solids=doc.items.Count(i=>i.kind!=MapItemKind.Ground);
        Check(floors<=MapDraft.MaxItems && solids<=MapDraft.MaxItems,"NATIVE_BUDGET");
        Check(floors==entry.GetProperty("counts").GetProperty("ground").GetInt32(),"FLOOR_COUNT_MATCH");
        Check(solids==entry.GetProperty("counts").GetProperty("solid").GetInt32()+2,"OBJECT_COUNT_MATCH");
        Check(doc.items.Count==d.Items.Count,"NATIVE_LOADER_DROPPED_ITEMS");
        var again=Load(d.ToJson());
        Check(d.Items.Count==again.Items.Count,"NATIVE_ROUNDTRIP_COUNT");
        var side=File.ReadAllBytes(Path.Combine(directory,entry.GetProperty("sidecar").GetString()!));
        Check(Convert.ToHexString(SHA256.HashData(side)).ToLowerInvariant() ==entry.GetProperty("sidecarSha256").GetString(),"SIDECAR_SHA");
        var metadata=JsonDocument.Parse(side).RootElement;
        Check(metadata.GetProperty("sourceGeometryHash").GetString()==entry.GetProperty("sourceGeometryHash").GetString(),"GEOMETRY_PIN");
        int cp=metadata.GetProperty("checkpointMarkersNonNative").GetArrayLength();
        Check(cp>=3&&cp<=5,"CHECKPOINT_SIDECAR");
        counts[cp]=counts.GetValueOrDefault(cp)+1;
        cases++;
      }
      Check(cases==18 && counts.GetValueOrDefault(3)==6&&counts.GetValueOrDefault(4)==6&&counts.GetValueOrDefault(5)==6,"COVERAGE");
      // Actual ORIGINAL TryLoad rejection pathways, not a parallel hand-coded assertion only.
      var first=JsonUtility.FromJson<MapDocument>(File.ReadAllText(Path.Combine(directory,entries[0].GetProperty("map").GetString()!)))!;
      var wrongVersion=JsonUtility.FromJson<MapDocument>(JsonUtility.ToJson(first))!;
      wrongVersion.version=3;Reject(wrongVersion,"VERSION");neg++;
      var dupSpawn=JsonUtility.FromJson<MapDocument>(JsonUtility.ToJson(first))!;
      dupSpawn.items.Add(dupSpawn.items.First(x=>x.kind==MapItemKind.Spawn).Copy());Reject(dupSpawn,"DUP_SPAWN");neg++;
      var colliding=JsonUtility.FromJson<MapDocument>(JsonUtility.ToJson(first))!;
      colliding.items.Add(colliding.items.First(x=>x.kind!=MapItemKind.Ground).Copy());Reject(colliding,"DUP_OBJECT");neg++;
      var invalidCoord=JsonUtility.FromJson<MapDocument>(JsonUtility.ToJson(first))!;
      invalidCoord.items.First().x=51;Reject(invalidCoord,"OUT_OF_BOUNDS");neg++;
      var badScale=JsonUtility.FromJson<MapDocument>(JsonUtility.ToJson(first))!;
      badScale.items.First().scaleX=5;Reject(badScale,"BAD_SCALE");neg++;
      var noSpawn=JsonUtility.FromJson<MapDocument>(JsonUtility.ToJson(first))!;
      noSpawn.items.RemoveAll(x=>x.kind==MapItemKind.Spawn);
      var d0=new MapDraft();Check(d0.TryLoad(JsonUtility.ToJson(noSpawn),out _) &&
         d0.TestError()!=null,"MISSING_SPAWN_TESTERROR");neg++;
      var receipt=new {
        packageId="COURSE-MAPDOC-001",originalMapDraftGitBlob=OriginalMapDraftBlob,
        mapsAcceptedByActualOriginalCodexMapDraft=cases,
        originalCsharpRejectAndTestErrorCases=neg,
        checkpointMarkerCounts=counts,
        unityJsonUtility="SYSTEM_TEXT_JSON_STUB_NOT_UNITY",
        unityEditMode="NOT_RUN",unityPlayMode="NOT_RUN",physx="NOT_RUN",
        readyForCodexIndependentQA=true
      };
      Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
      File.WriteAllText(args[1],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
      Console.WriteLine($"COURSE_MAPDOC_ORIGINAL_CODEX_MAPDRAFT_CSHARP_PASS={cases} NEGATIVES={neg} UNITY_NOT_RUN");
      return 0;
    } catch(Exception e) {Console.Error.WriteLine(e);return 2;}
 }
}
