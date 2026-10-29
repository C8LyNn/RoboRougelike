// B01-21-MAP-BREAK-001 W03: execute REAL portable C# source against ORIGINAL
// frozen MAPGEN001 33 Python-computed damage states, no UnityEngine fakes needed.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using B01.MAPBREAK001;

internal static class Program
{
    private static int assertions;
    private const string Root = "Research/B01-21/P17/BREAK001/generated";
    private static readonly string[] Names = {
        "AR16_01_OPEN_FLANK", "AR16_02_SPLIT_DECISION", "AR16_03_DENSE_PRESSURE"
    };

    private static void Require(bool b, string reason)
    {
        assertions++;
        if (!b) throw new Exception("SOURCE_ORIGINAL_CSHARP_CROSSCHECK_FAIL: " + reason);
    }

    private static int Main()
    {
        try
        {
            foreach (string name in Names) TestOriginalLayout(name);
            TestInvalidInputRejection();
            Console.WriteLine("MAP_BREAK_W03_ORIGINAL_CSHARP_3_ARENAS_33_DAMAGE_STATES_PASS | Assertions=" +
                              assertions.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("MAP_BREAK_W03_NATVE_UNITY_PHYSX=NOT_RUN; NO_CANONICAL_GAME_MODIFICATIONS");
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("MAP_BREAK_W03_FAIL " + e);
            return 1;
        }
    }

    private static SourceCoverLayout ReadLayout(JsonElement data)
    {
        var l = new SourceCoverLayout();
        l.SourceLayoutId = data.GetProperty("arenaId").GetString()!;
        l.OriginalMapGeometryHash = data.GetProperty("originalGeometryHash").GetString()!;
        l.EyeY = data.GetProperty("staticEyeHeightYMetres").GetDouble();
        l.OriginalCoverCount = data.GetProperty("originalCoverCount").GetInt32();
        var markers = new List<SourceMarker>();
        foreach (var mark in data.GetProperty("sourceEnemyDescriptors").EnumerateArray())
            markers.Add(new SourceMarker {
                X = mark.GetProperty("x").GetDouble(),
                Z = mark.GetProperty("z").GetDouble()
            });
        l.EnemySpawnDescriptors = markers.ToArray();
        var boxes = new List<SourceBox>();
        int coverOrdinal = 0;
        foreach (var element in data.GetProperty("originalFloorBoundariesAndCovers").EnumerateArray())
        {
            string kind = element.GetProperty("kind").GetString()!;
            var pos = element.GetProperty("position");
            var size = element.GetProperty("size");
            if (kind != "Boundary" && kind != "Cover") throw new Exception("UNKNOWN_SOURCE_SOLID");
            boxes.Add(new SourceBox {
                SourceElementIndex = element.GetProperty("sourceElementIndex").GetInt32(),
                CoverOrdinal = kind == "Cover" ? coverOrdinal++ : -1,
                X = pos.GetProperty("x").GetDouble(),
                Y = pos.GetProperty("y").GetDouble(),
                Z = pos.GetProperty("z").GetDouble(),
                Width = size.GetProperty("x").GetDouble(),
                Height = size.GetProperty("y").GetDouble(),
                Depth = size.GetProperty("z").GetDouble(),
            });
        }
        Require(coverOrdinal == l.OriginalCoverCount, l.SourceLayoutId + "/ORIGINAL_BOX_COUNT");
        l.Boxes = boxes.ToArray();
        return l;
    }

    private static Dictionary<string, SourceCell> AsDict(SourceCell[] cells)
    {
        return cells.ToDictionary(x => Key(x.X,x.Z),x => x);
    }
    private static string Key(int x,int z) => x.ToString(CultureInfo.InvariantCulture)+","+
                                                z.ToString(CultureInfo.InvariantCulture);
    private static string Key(JsonElement p) => Key(p.GetProperty("x").GetInt32(),
                                                   p.GetProperty("z").GetInt32());
    private static int[] JsonIndices(JsonElement a) => a.EnumerateArray().Select(x=>x.GetInt32()).ToArray();
    private static void SameInts(int[] actual, int[] expected, string reason)
    {
        Require(actual.SequenceEqual(expected), reason+" ["+string.Join(",",actual)+"] != ["+
                                               string.Join(",",expected)+"]");
    }
    private static void CheckSnapshot(JsonElement pythonCells, SourceCell[] csharp, string desc)
    {
        var map = AsDict(csharp);
        Require(pythonCells.GetArrayLength()==map.Count,desc+"/CELL_COUNT");
        foreach(var sample in pythonCells.EnumerateArray())
        {
            string key=Key(sample);
            Require(map.ContainsKey(key),desc+"/MISSING "+key);
            SameInts(map[key].VisibleEnemyIndices,
              JsonIndices(sample.GetProperty("visibleEnemyDescriptors")),desc+"/VISIBLE "+key);
            Require(map[key].VisibleCount==sample.GetProperty("visibleEnemyCount").GetInt32(),
                    desc+"/EXPOSURE "+key);
        }
    }

    private static void TestOriginalLayout(string name)
    {
        string file=Path.Combine(Root,name+".break.json");
        using JsonDocument document=JsonDocument.Parse(File.ReadAllText(file,Encoding.UTF8));
        JsonElement input=document.RootElement;
        SourceCoverLayout layout=ReadLayout(input);
        string gameMapGeometryHash=layout.OriginalMapGeometryHash;
        var world = new CoverWorld(layout,gameMapGeometryHash);
        Require(world.CoverCount==layout.OriginalCoverCount,name+"/COVER_COUNT");
        Require(world.DestroyedCount==0,name+"/START_PRISTINE");
        string pristineHash=world.SourceOverlayStateHash;
        Require(pristineHash==input.GetProperty("intactBaseline").GetProperty("stateHash").GetString(),
                name+"/PYTHON_ORIGINAL_STATE_SHA256");
        CheckSnapshot(input.GetProperty("intactBaseline").GetProperty("visibilityGrid"),
                      world.ExposureGrid4m(),name+"/ORIGINAL_GRID");

        var expectedCases=input.GetProperty("coverDestructionCases");
        Require(expectedCases.GetArrayLength()==layout.OriginalCoverCount,name+"/CASE_LENGTH");
        int scenarios=0;
        foreach(var item in expectedCases.EnumerateArray())
        {
            int ordinal=item.GetProperty("destroyedCoverOrdinal").GetInt32();
            world.RestoreAll();
            Require(world.Destroy(ordinal),name+"/DESTROY");
            Require(world.IsDestroyed(ordinal),name+"/DESTROYED_FLAG");
            Require(!world.Destroy(ordinal),name+"/REPEATED_DESTROY_NO_CHANGE");
            Require(world.DestroyedCount==1,name+"/EXACTLY_ONE");
            SameInts(world.RemainingOriginalCoverOrdinals(),
                     JsonIndices(item.GetProperty("remainingSourceCoverOrdinals")),
                     name+"/REMAINING_SOURCE_ORDINALS");
            Require(world.SourceOverlayStateHash==item.GetProperty("overlayStateHash").GetString(),
                    name+"/"+ordinal+"/PYTHON_SOURCE_STATE_HASH");

            var after=AsDict(world.ExposureGrid4m());
            JsonElement beforeCells=input.GetProperty("intactBaseline").GetProperty("visibilityGrid");
            Require(after.Count==item.GetProperty("sourceClearGridCellsAfter").GetInt32(),
                    name+"/"+ordinal+"/AFTER_CLEAR_COUNT");
            Require(beforeCells.GetArrayLength()==item.GetProperty("sourceClearGridCellsBefore").GetInt32(),
                    name+"/"+ordinal+"/BEFORE_CLEAR_COUNT");
            var beforeKeys=new HashSet<string>();
            int increased=0;
            foreach(var sample in beforeCells.EnumerateArray())
            {
                string key=Key(sample);beforeKeys.Add(key);
                Require(after.ContainsKey(key),name+"/"+ordinal+"/DAMAGE_REDUCED_FREE_FLOOR "+key);
                int old=sample.GetProperty("visibleEnemyCount").GetInt32();
                int current=after[key].VisibleCount;
                Require(current>=old,name+"/"+ordinal+"/COVER_REMOVAL_HID_ENEMY "+key);
                increased+=current-old;
            }
            Require(increased==item.GetProperty("additionalVisibleEnemyPairCount").GetInt32(),
                    name+"/"+ordinal+"/PYTHON_VISIBLE_PAIRS");
            var newCells=item.GetProperty("newlyClearCells");
            Require(after.Count-beforeKeys.Count==newCells.GetArrayLength(),
                    name+"/"+ordinal+"/NEW_SOURCE_CELLS");
            foreach(var sample in newCells.EnumerateArray())
            {
                string key=Key(sample);
                Require(!beforeKeys.Contains(key),name+"/"+ordinal+"/ALREADY_CLEAR "+key);
                Require(after.ContainsKey(key),name+"/"+ordinal+"/MISSING_NEW_CELL "+key);
                SameInts(after[key].VisibleEnemyIndices,
                         JsonIndices(sample.GetProperty("visibleEnemyDescriptors")),
                         name+"/"+ordinal+"/NEW_SOURCE_LOS "+key);
            }
            foreach(var p in item.GetProperty("priorClearCellsWithIncreasedEnemyVisibility").EnumerateArray())
            {
                string key=Key(p);
                Require(after[key].VisibleCount==p.GetProperty("visibleAfter").GetInt32(),
                        name+"/"+ordinal+"/VISIBILITY_DELTA "+key);
            }
            string receipt=world.ExportReceipt();
            world.RestoreAll();
            Require(!world.IsDestroyed(ordinal),name+"/RESTORE_ALL");
            string err;
            Require(world.TryImportReceipt(receipt,out err),
                    name+"/"+ordinal+"/IMPORT_RECEIPT "+err);
            Require(world.DestroyedCount==1&&world.IsDestroyed(ordinal),
                    name+"/"+ordinal+"/IMPORT_STATE");
            Require(world.Restore(ordinal),name+"/RESTORE_ONE");
            Require(!world.Restore(ordinal),name+"/REPEATED_RESTORE_NO_CHANGE");
            Require(world.SourceOverlayStateHash==pristineHash,
                    name+"/"+ordinal+"/RESTORED_ORIGINAL_SHA");
            scenarios++;
        }
        Require(scenarios==layout.OriginalCoverCount,name+"/LOOP_CASES");
        Console.WriteLine("MAP_BREAK_W03_PYTHON_ORIGINAL_VS_REAL_CSHARP_"+name+"="+scenarios+"_DAMAGE_AND_RESET_PASS");

        // Independent new behavior beyond the previous 33 isolated Python states:
        // TWO destroyed covers, order-independent, deterministic and transactional.
        world.Destroy(1);world.Destroy(0);
        string bothHash=world.SourceOverlayStateHash;
        string combinedReceipt=world.ExportReceipt();
        SourceCell[] both=world.ExposureGrid4m();
        world.RestoreAll();
        world.Destroy(0);world.Destroy(1);
        Require(world.SourceOverlayStateHash==bothHash,name+"/TWO_DAMAGE_ORDER_INDEPENDENCE");
        CheckSnapshotAgainstSelf(both,world.ExposureGrid4m(),name+"/MULTI_DAMAGE_CANONICAL");
        Require(world.ExportReceipt()==combinedReceipt,name+"/TWO_DAMAGE_RECEIPT_STABILITY");

        string tampered=combinedReceipt.Substring(0,combinedReceipt.Length-1)+"0";
        // Last checksum char might already be zero; force a guaranteed mismatch.
        if (tampered==combinedReceipt) tampered=combinedReceipt.Substring(0,combinedReceipt.Length-1)+"1";
        string reason;
        string beforeBad=world.SourceOverlayStateHash;
        Require(!world.TryImportReceipt(tampered,out reason)&&reason=="CHECKSUM",
                name+"/CORRUPTED_RECEIPT_FAIL_CLOSED");
        Require(world.SourceOverlayStateHash==beforeBad,name+"/CORRUPTED_RECEIPT_NO_STATE_MUTATION");
        Require(!world.TryImportReceipt(combinedReceipt.Replace(name,"WRONG_MAP"),out reason)
                && reason=="WRONG_MAP",name+"/FOREIGN_MAP_RECEIPT_FAIL_CLOSED");
        Require(world.SourceOverlayStateHash==beforeBad,name+"/FOREIGN_RECEIPT_NO_STATE_MUTATION");

        world.RestoreAll();
        Require(world.TryImportReceipt(combinedReceipt,out reason),
                name+"/TWO_DAMAGE_REPLAY");
        Require(world.DestroyedCount==2 && world.SourceOverlayStateHash==bothHash,
                name+"/TWO_DAMAGE_REPLAY_EXACT");
        world.RestoreAll();
    }

    private static void CheckSnapshotAgainstSelf(SourceCell[] a,SourceCell[] b,string reason)
    {
        var left=AsDict(a);var right=AsDict(b);
        Require(left.Count==right.Count,reason+"/GRID_SIZE");
        foreach(var pair in left)
        {
            Require(right.ContainsKey(pair.Key),reason+"/MISSING");
            SameInts(pair.Value.VisibleEnemyIndices,right[pair.Key].VisibleEnemyIndices,
                     reason+"/"+pair.Key);
        }
    }

    private static void TestInvalidInputRejection()
    {
        using JsonDocument d=JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Root,Names[0]+".break.json"),Encoding.UTF8));
        SourceCoverLayout original=ReadLayout(d.RootElement);
        bool mismatch=false;
        try { new CoverWorld(original,new string('a',64)); }
        catch(ArgumentException e) { mismatch=e.Message.Contains("STALE_OR_WRONG_MAP_GEOMETRY_HASH"); }
        Require(mismatch,"WRONG_ACTUAL_MAP_GEOMETRY_REJECTED");
        var copy=ReadLayout(d.RootElement);
        copy.Boxes[5].CoverOrdinal=copy.Boxes[4].CoverOrdinal;
        bool collision=false;
        try { new CoverWorld(copy,copy.OriginalMapGeometryHash); }
        catch(ArgumentException e) { collision=e.Message.Contains("COVER_ORDINAL_COLLISION_OR_RANGE"); }
        Require(collision,"SOURCE_ORDINAL_COLLISION_REJECTED");
        var world=new CoverWorld(original,original.OriginalMapGeometryHash);
        string prior=world.SourceOverlayStateHash;
        string err;
        Require(!world.TryImportReceipt("",out err)&&err=="EMPTY","EMPTY_RECEIPT_REJECTED");
        Require(world.SourceOverlayStateHash==prior,"EMPTY_RECEIPT_IMMUTABLE");
        bool badOrdinal=false;
        try{world.Destroy(-1);}catch(ArgumentOutOfRangeException){badOrdinal=true;}
        Require(badOrdinal,"NEGATIVE_COVER_ORDINAL_REJECTED");
        var mutableAuthoringSource=ReadLayout(d.RootElement);
        var frozenRuntime=new CoverWorld(mutableAuthoringSource,
                                         mutableAuthoringSource.OriginalMapGeometryHash);
        var originalVisibility=frozenRuntime.VisibleEnemyDescriptors(-20,-20);
        string frozenHash=frozenRuntime.SourceOverlayStateHash;
        string frozenReceipt=frozenRuntime.ExportReceipt();
        // Host-owned source objects are mutable, but cannot silently mutate
        // an initialized runtime after a source hash was validated.
        mutableAuthoringSource.Boxes[4].X+=2000;
        mutableAuthoringSource.Boxes[4].Width*=100;
        mutableAuthoringSource.EnemySpawnDescriptors[0].X+=9999;
        mutableAuthoringSource.OriginalMapGeometryHash=new string('b',64);
        mutableAuthoringSource.SourceLayoutId="FAKE_LAYOUT";
        Require(frozenRuntime.SourceOverlayStateHash==frozenHash,
                "POST_INIT_AUTHORING_SOURCE_MUTATION_HASH_FROZEN");
        Require(frozenRuntime.ExportReceipt()==frozenReceipt,
                "POST_INIT_AUTHORING_SOURCE_MUTATION_RECEIPT_FROZEN");
        SameInts(frozenRuntime.VisibleEnemyDescriptors(-20,-20),
                 originalVisibility,"POST_INIT_AUTHORING_SOURCE_MUTATION_LOS_FROZEN");
        var invalidMarker=ReadLayout(d.RootElement);
        invalidMarker.EnemySpawnDescriptors[0].X=double.NaN;
        bool rejectedNaN=false;
        try { new CoverWorld(invalidMarker,invalidMarker.OriginalMapGeometryHash); }
        catch(ArgumentException e) { rejectedNaN=e.Message.Contains("INVALID_ENEMY_SOURCE_MARKER"); }
        Require(rejectedNaN,"NON_FINITE_SOURCE_MARKER_REJECTED");
        Console.WriteLine("MAP_BREAK_W03_PROTECTED_NEGATIVE_SOURCES_AND_REPLAY_PASS");
    }
}
