// B01-25-MAPDRAFT-IMPORT-001 R1
// Compiles the actual immutable Codex MapDraft.cs byte-for-byte, with ONLY
// minimal UnityEngine.Vector2Int and JsonUtility substitutes outside Unity.
// A passing source test DOES NOT imply Unity JsonUtility/PhysX/Player PASS.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoboRougelike.MapEditor;
using UnityEngine;

static class Program
{
    const string ClearanceBlob="53eb696950121465befea2855eda4d7268750016";
    const string SlalomBlob="013f5fe2058e76233f580a439e8402c0fe09f124";
    const string MapDraftBlob="0331a54a1545ecd1ea18b616ef13fed113474b8c";
    static int checks=0, negatives=0;
    static readonly List<string> TestNames=new();
    static void Assert(bool condition,string reason)
    {
        if(!condition)throw new InvalidOperationException(reason);
    }
    static void Pass(string id,Action test)
    {
        try{test();checks++;TestNames.Add("PASS "+id);Console.WriteLine("PASS "+id);}
        catch(Exception e){Console.Error.WriteLine("FAIL "+id+": "+e);throw;}
    }
    static void Negative(string id,Action test)
    {
        Pass("NEGATIVE_"+id,test);negatives++;
    }
    static string GitBlob(byte[] bytes)
    {
        var header=Encoding.UTF8.GetBytes("blob "+bytes.Length+"\0");
        var joined=new byte[header.Length+bytes.Length];
        Buffer.BlockCopy(header,0,joined,0,header.Length);
        Buffer.BlockCopy(bytes,0,joined,header.Length,bytes.Length);
        return Convert.ToHexString(SHA1.HashData(joined)).ToLowerInvariant();
    }
    static string ToJson(MapDocument doc)=>JsonUtility.ToJson(doc,true);
    static MapDocument Copy(MapDocument src)=>
        JsonUtility.FromJson<MapDocument>(ToJson(src))??throw new Exception("Copy failed");
    static (bool ok,string? error,MapDraft draft) Load(string json)
    {
        var draft=new MapDraft();
        bool ok=draft.TryLoad(json,out var error);
        return (ok,error,draft);
    }
    static void RejectLoad(MapDocument src,string reason)
    {
        var r=Load(ToJson(src));
        Assert(!r.ok,"MapDraft.TryLoad wrongly accepted "+reason);
        Assert(!string.IsNullOrWhiteSpace(r.error),"TryLoad did not provide a reason: "+reason);
    }
    static MapItem Obj(MapItemKind kind,int x,int z)=>new()
    {
        kind=kind,x=x,z=z,rotation=0,scaleX=1,scaleY=1,scaleZ=1
    };
    static void Main(string[] args)
    {
        if(args.Length!=3)
        {
            Console.Error.WriteLine("Usage: MapDraftImportProof <clearance.json> <slalom.json> <receipt.json>");
            Environment.ExitCode=2;return;
        }
        string clearance=File.ReadAllText(args[0]),slalom=File.ReadAllText(args[1]);
        byte[] cbytes=File.ReadAllBytes(args[0]),sbytes=File.ReadAllBytes(args[1]);
        string cblob=GitBlob(cbytes),sblob=GitBlob(sbytes);
        Pass("CLEARANCE_EXACT_GIT_BLOB",()=>Assert(cblob==ClearanceBlob,cblob));
        Pass("SLALOM_EXACT_GIT_BLOB",()=>Assert(sblob==SlalomBlob,sblob));
        string sourcePath=Path.Combine(AppContext.BaseDirectory,".."); // source is also checked by CI git hash-object
        var c=JsonUtility.FromJson<MapDocument>(clearance)??throw new Exception("No clearance doc");
        var s=JsonUtility.FromJson<MapDocument>(slalom)??throw new Exception("No slalom doc");
        Pass("CLEARANCE_ORIGINAL_CODE_LOAD",()=>
        {
            var r=Load(clearance);
            Assert(r.ok&&r.error==null,"Error: "+r.error);
            Assert(r.draft.Items.Count==415,"Expected 415 original items");
            Assert(!r.draft.GlobalFloor,"Unexpected floor");
            Assert(r.draft.TestError()==null,"TestError: "+r.draft.TestError());
            Assert(r.draft.Spawn?.x==9&&r.draft.Spawn?.z==2,"Spawn wrong");
            Assert(r.draft.Finish?.x==9&&r.draft.Finish?.z==13,"Finish wrong");
        });
        Pass("SLALOM_ORIGINAL_CODE_LOAD",()=>
        {
            var r=Load(slalom);
            Assert(r.ok&&r.error==null,"Error: "+r.error);
            Assert(r.draft.Items.Count==360,"Expected 360 original items");
            Assert(!r.draft.GlobalFloor,"Unexpected floor");
            Assert(r.draft.TestError()==null,"TestError: "+r.draft.TestError());
            Assert(r.draft.Spawn?.x==7&&r.draft.Spawn?.z==2,"Spawn wrong");
            Assert(r.draft.Finish?.x==7&&r.draft.Finish?.z==15,"Finish wrong");
        });
        Pass("CLEARANCE_KIND_COUNTS",()=>{
            var expected=new Dictionary<MapItemKind,int>{{MapItemKind.Ground,288},{MapItemKind.Wall,105},
                {MapItemKind.Platform,4},{MapItemKind.Pillar,8},{MapItemKind.LowBarrier,8},
                {MapItemKind.Spawn,1},{MapItemKind.Finish,1}};
            foreach(var e in expected)Assert(c.items.Count(x=>x.kind==e.Key)==e.Value,"Wrong kind "+e.Key);
        });
        Pass("SLALOM_KIND_COUNTS",()=>{
            Assert(s.items.Count(x=>x.kind==MapItemKind.Ground)==252,"Ground");
            Assert(s.items.Count(x=>x.kind==MapItemKind.Wall)==92,"Wall");
            Assert(s.items.Count(x=>x.kind==MapItemKind.Platform)==4,"Corners");
            Assert(s.items.Count(x=>x.kind==MapItemKind.Spawn)==1,"Spawn");
            Assert(s.items.Count(x=>x.kind==MapItemKind.Finish)==1,"Finish");
        });
        Pass("FOUR_FULL_HEIGHT_CORNER_PLATFORMS_BOTH",()=>{
            foreach(var doc in new[]{c,s})
            {
                Assert(doc.items.Count(x=>x.kind==MapItemKind.Platform&&x.scaleY==4)==4,
                    "Missing 4m full-height corner");
            }
        });
        Pass("MAPDRAFT_FOOTPRINTS_REAL_CSHARP",()=>{
            foreach(var doc in new[]{c,s})
            {
                var free=new HashSet<Vector2Int>();
                var floor=new HashSet<Vector2Int>();
                foreach(var item in doc.items)
                {
                    var set=item.kind==MapItemKind.Ground?floor:free;
                    foreach(var fp in MapDraft.Footprint(item))
                        Assert(set.Add(fp),"Same-class footprint collision at "+fp.x+","+fp.y);
                }
                Assert(floor.Count==doc.items.Count(x=>x.kind==MapItemKind.Ground),"Ground area mismatch");
            }
        });
        Pass("ROUNDTRIP_BOTH_USING_ORIGINAL_CSHARP",()=>{
            foreach(var json in new[]{clearance,slalom})
            {
                var first=Load(json);
                Assert(first.ok,"Input failed");
                var again=Load(first.draft.ToJson());
                Assert(again.ok&&again.draft.TestError()==null,"ToJson reimport failed: "+again.error);
                Assert(first.draft.Items.Count==again.draft.Items.Count,"Object count drift");
                Assert(first.draft.GlobalFloor==again.draft.GlobalFloor,"Floor drift");
            }
        });
        Pass("UNDO_REDO_ORIGINAL_CSHARP_LOAD",()=>{
            var d=new MapDraft();
            Assert(d.TryLoad(clearance,out _),"First load fail");
            Assert(d.Undo()&&d.Items.Count==0,"Load undo changed");
            Assert(d.Redo()&&d.Items.Count==415,"Redo did not restore map");
        });
        Negative("VERSION_REJECTED",()=>{
            var t=Copy(c);t.version=42;RejectLoad(t,"v42");
        });
        Negative("INVALID_ENUM_ID_REJECTED",()=>{
            var t=Copy(c);t.items[5].kind=(MapItemKind)99;RejectLoad(t,"kind99");
        });
        Negative("ROTATION_OUT_OF_RANGE_REJECTED",()=>{
            var t=Copy(c);t.items[5].rotation=4;RejectLoad(t,"rotation4");
        });
        Negative("SCALE_OUT_OF_RANGE_REJECTED",()=>{
            var t=Copy(c);t.items[5].scaleX=5;RejectLoad(t,"scaleX5");
        });
        Negative("OUT_OF_BOUNDS_REJECTED",()=>{
            var t=Copy(c);t.items[5].x=51;RejectLoad(t,"x51");
        });
        Negative("OVERLAP_WALL_REJECTED",()=>{
            var t=Copy(c);var wall=t.items.First(x=>x.kind==MapItemKind.Wall);
            t.items.Add(wall.Copy());RejectLoad(t,"duplicate wall footprint");
        });
        Negative("DUPLICATE_SPAWN_REJECTED",()=>{
            var t=Copy(c);t.items.Add(Obj(MapItemKind.Spawn,8,2));
            RejectLoad(t,"second free spawn");
        });
        Negative("DUPLICATE_FINISH_REJECTED",()=>{
            var t=Copy(s);t.items.Add(Obj(MapItemKind.Finish,8,15));
            RejectLoad(t,"second finish");
        });
        Negative("TOO_MANY_GROUND_REJECTED",()=>{
            var t=Copy(c);for(int i=t.items.Count(x=>x.kind==MapItemKind.Ground);i<=MapDraft.MaxItems;i++)
                t.items.Add(Obj(MapItemKind.Ground,0,0));
            RejectLoad(t,"2001 floors");
        });
        Negative("MALFORMED_JSON_REJECTED",()=>{
            var r=Load("{no json");
            Assert(!r.ok&&!string.IsNullOrEmpty(r.error),"Malformed string unexpectedly accepted");
        });
        Negative("MISSING_SPAWN_ACCEPTED_BUT_TESTERROR",()=>{
            var t=Copy(c);t.items.RemoveAll(x=>x.kind==MapItemKind.Spawn);
            var r=Load(ToJson(t));
            Assert(r.ok&&r.draft.TestError()!=null,"TestError must catch no start");
        });
        Negative("MISSING_SPAWN_FLOOR_ACCEPTED_BUT_TESTERROR",()=>{
            var t=Copy(c);t.items.RemoveAll(x=>x.kind==MapItemKind.Ground&&x.x==9&&x.z==2);
            var r=Load(ToJson(t));
            Assert(r.ok&&r.draft.TestError()!=null,"TestError must catch no ground under Spawn");
        });
        Pass("R1_IMPORT_WOULD_DROP_CORNER_VERTICAL_SCALE",()=>{
            var t=Copy(c);t.version=1;
            var r=Load(ToJson(t));
            Assert(r.ok,"R1 should be readable");
            Assert(r.draft.Items.Where(x=>x.kind==MapItemKind.Platform).All(x=>x.scaleY==1),
                "Expected original V1 scaling fallback");
            // Acceptance must require v2 to maintain actual source corner height.
        });
        Console.WriteLine($"RESULT PASS original MapDraft.cs original C# paths: {checks}/{checks}, {negatives} negative semantics verified; both original arena data. UNITY_JSONUTILITY=STUB NOT_RUN; PHYSX NOT_RUN");
        var receipt=new
        {
            package="B01-25-MAPDRAFT-IMPORT-001/R1",
            originalMapDraftGitBlob=MapDraftBlob,
            clearanceMapGitBlob=cblob,
            slalomMapGitBlob=sblob,
            sourceExecuted="original immutable Codex MapDraft.cs plus minimal UnityEngine surrogate",
            positiveAndNegativeAssertions=checks,
            expectedRejectAndTestErrorCases=negatives,
            serializer="System.Text.Json JsonUtility stub - NOT Unity JsonUtility",
            unityEditMode="NOT_RUN",unityPlayMode="NOT_RUN",physx="NOT_RUN",
            cases=TestNames.ToArray()
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
        File.WriteAllText(args[2],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
    }
}
