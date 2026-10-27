// W16 B01-30. Actual B01-23 production code is COMPILED directly, not mirrored.
// Input bounds are analytically replayed from pinned real R41 transform constants;
// PhysX/Unity Collider.bounds, scene, and rendering are NOT_RUN.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using B01.PARTSIZE001;

static class QA
{
    static void Require(bool valid,string message)
    {
        if(!valid)throw new Exception("PR286_REPLAY_FAIL "+message);
    }
    static double[] Three(JsonElement el)
    {
        return el.EnumerateArray().Select(e=>e.GetDouble()).ToArray();
    }
    static int[] Cells(JsonElement el)
    {
        return el.EnumerateArray().Select(e=>e.GetInt32()).ToArray();
    }
    static Meters3 Meters(double[] p)=>new(p[0],p[1],p[2]);
    static void Main(string[] args)
    {
        if(args.Length!=1)throw new ArgumentException("requires exact independently generated JSON");
        using var doc=JsonDocument.Parse(File.ReadAllText(args[0]));
        var root=doc.RootElement;
        var records=root.GetProperty("records").EnumerateArray().ToArray();
        Require(records.Length==32,"expected 32 yaw cases from real R41 geometry");
        int observed=0,blocked=0,permitted=0;
        foreach(var group in records.GroupBy(r=>r.GetProperty("legacyVariant").GetString()))
        {
            var zero=group.Single(r=>r.GetProperty("yawQuarterTurns").GetInt32()==0);
            double[] lo=Three(zero.GetProperty("sourcePredictedPhysicalAABBMinXYZ"));
            double[] hi=Three(zero.GetProperty("sourcePredictedPhysicalAABBMaxXYZ"));
            var center=lo.Zip(hi,(a,b)=>(a+b)*.5).ToArray();
            var size=lo.Zip(hi,(a,b)=>b-a).ToArray();
            var cells=Cells(zero.GetProperty("declaredCells"));
            var partDef=new PartSizeDefinition(group.Key,new Cell3(cells[0],cells[1],cells[2]),
                Meters(center),Meters(size),"R41_SOURCE_PREDICTED_QA_ONLY");
            foreach(var row in group)
            {
                int yaw=row.GetProperty("yawQuarterTurns").GetInt32();
                var err=PartSizeGrid.EnvelopeError(partDef,new Cell3(0,0,0),new Cell3(0,yaw,0));
                bool expectFit=Three(row.GetProperty("sourcePredictedColliderOverrunXYZ")).All(v=>v==0);
                Require((err==null)==expectFit,group.Key+"/q="+yaw+
                    " expectedFit="+expectFit+" actualError="+err);
                if(expectFit)permitted++;else blocked++;
                observed++;
            }
        }
        Require(observed==32,"not all physical source AABBs checked");
        Require(blocked>0 && permitted>0,"expected both positive and negative fixtures");
        // The catalog must REFUSE to approve unmeasured imported model types.
        int sourceUnmeasured=0;
        foreach(var name in PartSizeBaselineCatalog.CanonicalNames)
        {
            var def=PartSizeBaselineCatalog.ForSourceKind(name);
            var error=PartSizeGrid.EnvelopeError(def,new Cell3(0,0,0),new Cell3(0,0,0));
            bool expectedUnmeasured=new[]{"Wheel","Gun","Suspension","Booster","JumpDrive",
                "Blink","Gyroscope","OmniDrive","MagneticGrapple","HoverEngine","CrawlerTrack"}.Contains(name);
            Require((error=="UNMEASURED_PHYSICAL_ENVELOPE")==expectedUnmeasured,name+" source catalog status "+error);
            if(expectedUnmeasured)sourceUnmeasured++;
        }
        Require(sourceUnmeasured==11,"unmeasured source types must stay 11");
        Console.WriteLine($"B01-30 ACTUAL_COMPILED_PR286_CONTRACT_PASS cases={observed} blocked={blocked} permitted={permitted} catalog_unmeasured={sourceUnmeasured} UNITY_NOT_RUN");
    }
}
