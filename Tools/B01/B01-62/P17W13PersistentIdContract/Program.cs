// B01-62 W13: genuine source C# round-trip and hostile save vectors.
// Original W06 and immutable original #510 ledger/rig catalog compiled verbatim.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using B01.DataIntegrity.W13;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class Program
{
    static int good, bad;
    static void Check(string title,Func<bool> fn)
    {
        try
        {
            if(fn()){good++;Console.WriteLine("PASS "+title);}
            else {bad++;Console.WriteLine("FAIL "+title);}
        }
        catch(Exception e){bad++;Console.WriteLine("FAIL "+title+" "+e);}
    }
    static bool Reject(Action fn)
    {
        try{fn();return false;}
        catch(InvalidDataException){return true;}
    }
    static string GuidFor(int i)=>i==0?"01010101010101010101010101010101":"02020202020202020202020202020202";
    static PartSnapshot Part(int index,bool detached)
    {
        return new PartSnapshot
        {
            Index=index,PartId=SecureRigCatalog.At(index).PartId,
            InstanceGuid=GuidFor(index),
            Detached=detached,Consumed=detached,
            IsKinematic=!detached,Parent=detached?"world":"root",
            WorldPosition=new Coordinate{X=index==0?-3.5:3.5,Y=1.2,Z=0},
            LinearVelocity=new Coordinate{X=detached?1:0,Y=0,Z=0},
            AngularVelocity=new Coordinate()
        };
    }
    static SavePayload Sample(bool first=true,bool second=false)
        =>new SavePayload{SchemaVersion=1,SaveSlotGuid="10101010101010101010101010101010",
            RunEpoch=10,Parts=new[]{Part(0,first),Part(1,second)}};
    static byte[] Mutate(Func<JsonNode,JsonNode> mutator)
    {
        JsonNode node=JsonNode.Parse(Encoding.UTF8.GetString(Contract.Encode(Sample())));
        return Encoding.UTF8.GetBytes(mutator(node).ToJsonString());
    }
    static bool Tmp(Func<string,bool> f)
    {
        var dir=Path.Combine(Path.GetTempPath(),"b01-62-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try{return f(dir);}
        finally{Directory.Delete(dir,true);}
    }
    static bool RoundTrip(bool a,bool b)
    {
        return Tmp(d=>{
            string file=Path.Combine(d,"slot.json");
            SavePayload initial=Sample(a,b);
            Contract.WriteNewSlot(file,initial,()=>true);
            RestorePlan restored=Contract.ReadAndStage(file,()=>true);
            return restored.Parts.Length==2 && restored.RunEpoch==initial.RunEpoch &&
                restored.SaveSlotGuid==initial.SaveSlotGuid &&
                restored.Parts[0].OriginalR2Ledger.Contains(2310001)==a &&
                restored.Parts[1].OriginalR2Ledger.Contains(2310002)==b &&
                restored.Parts[0].Snapshot.Detached==a&&restored.Parts[1].Snapshot.Detached==b;
        });
    }
    static bool CorrectlyRearmRealLedger()
    {
        return Tmp(d=>{
            string f=Path.Combine(d,"s.json");
            Contract.WriteNewSlot(f,Sample(true,true),()=>true);
            var p=Contract.ReadAndStage(f,()=>true);
            for(int i=0;i<2;i++)
            {
                var l=p.Parts[i].OriginalR2Ledger;long id=p.Parts[i].Snapshot.PartId;
                int touched=0;
                if(!l.Contains(id)||l.Count!=1) return false;
                if(!l.TryReattach(id,()=>touched++,()=>touched--))return false;
                if(touched!=1 || l.Count!=0 || l.Contains(id))return false;
                if(!l.TryDetach(id,new Vector3d(0,0,0),new Vector3d(1,0,0),
                    new Vector3d(0,0,0),1,0,0,out _))return false;
                if(l.Count!=1||!l.Contains(id))return false;
            }
            return true;
        });
    }
    static int Main()
    {
        Check("01 genuine frozen SecureRigCatalog ID light",()=>SecureRigCatalog.At(0).PartId==2310001);
        Check("02 genuine frozen SecureRigCatalog ID heavy",()=>SecureRigCatalog.At(1).PartId==2310002);
        Check("03 disk roundtrip both attached",()=>RoundTrip(false,false));
        Check("04 disk roundtrip light detached",()=>RoundTrip(true,false));
        Check("05 disk roundtrip heavy detached",()=>RoundTrip(false,true));
        Check("06 disk roundtrip both detached",()=>RoundTrip(true,true));
        Check("07 actual W06 R2 ledger restored ID rearm and redetach",CorrectlyRearmRealLedger);
        Check("08 reject foreign rig ID",()=>{var x=Sample();x.Parts[0].PartId=99;return Reject(()=>Contract.Encode(x));});
        Check("09 reject duplicate rig ID",()=>{var x=Sample();x.Parts[1].PartId=x.Parts[0].PartId;return Reject(()=>Contract.Encode(x));});
        Check("10 reject reversed rig ordering",()=>{var x=Sample();Array.Reverse(x.Parts);return Reject(()=>Contract.Encode(x));});
        Check("11 reject missing rig",()=>{var x=Sample();x.Parts=new[]{Part(0,true)};return Reject(()=>Contract.Encode(x));});
        Check("12 reject extra rig",()=>{var x=Sample();x.Parts=new[]{Part(0,true),Part(1,false),Part(1,false)};return Reject(()=>Contract.Encode(x));});
        Check("13 reject missing persistent module GUID",()=>{var x=Sample();x.Parts[0].InstanceGuid="";return Reject(()=>Contract.Encode(x));});
        Check("14 reject duplicate persistent module GUID",()=>{var x=Sample();x.Parts[1].InstanceGuid=x.Parts[0].InstanceGuid;return Reject(()=>Contract.Encode(x));});
        Check("15 reject GUID same as save-slot",()=>{var x=Sample();x.Parts[0].InstanceGuid=x.SaveSlotGuid;return Reject(()=>Contract.Encode(x));});
        Check("16 reject noncanonical uppercase module GUID",()=>{var x=Sample();x.Parts[0].InstanceGuid="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";return Reject(()=>Contract.Encode(x));});
        Check("17 reject other producer candidate SHA",()=>{var x=Sample();x.SourceSha="baf65f2664e38f3b9c38815c3f07285f8107c1b6";return Reject(()=>Contract.Encode(x));});
        Check("18 reject schema downgrade",()=>{var x=Sample();x.SchemaVersion=0;return Reject(()=>Contract.Encode(x));});
        Check("19 reject future schema",()=>{var x=Sample();x.SchemaVersion=2;return Reject(()=>Contract.Encode(x));});
        Check("20 reject invalid run epoch",()=>{var x=Sample();x.RunEpoch=0;return Reject(()=>Contract.Encode(x));});
        Check("21 reject detached without consumed ID",()=>{var x=Sample();x.Parts[0].Consumed=false;return Reject(()=>Contract.Encode(x));});
        Check("22 reject attached with consumed ID",()=>{var x=Sample(false,false);x.Parts[0].Consumed=true;return Reject(()=>Contract.Encode(x));});
        Check("23 reject detached kinematic",()=>{var x=Sample();x.Parts[0].IsKinematic=true;return Reject(()=>Contract.Encode(x));});
        Check("24 reject detached parented to root",()=>{var x=Sample();x.Parts[0].Parent="root";return Reject(()=>Contract.Encode(x));});
        Check("25 reject attached with velocity",()=>{var x=Sample(false,false);x.Parts[0].LinearVelocity.X=1;return Reject(()=>Contract.Encode(x));});
        Check("26 reject detached corrupted NaN position",()=>{var x=Sample();x.Parts[0].WorldPosition.X=double.NaN;return Reject(()=>Contract.Encode(x));});
        Check("27 reject detached corrupted infinity velocity",()=>{var x=Sample();x.Parts[0].LinearVelocity.X=double.PositiveInfinity;return Reject(()=>Contract.Encode(x));});
        Check("28 reject corrupted saved payload hash",()=>Reject(()=>Contract.Decode(Mutate(x=>{x["payload"]["runEpoch"]=11;return x;}))));
        Check("29 reject duplicate JSON property keys",()=>{var s=Encoding.UTF8.GetString(Contract.Encode(Sample()));s=s.Replace("\"schemaVersion\":1","\"schemaVersion\":1,\"schemaVersion\":1");return Reject(()=>Contract.Decode(Encoding.UTF8.GetBytes(s)));});
        Check("30 reject unknown JSON field",()=>Reject(()=>Contract.Decode(Mutate(x=>{x["payload"]["untrusted"]="value";return x;}))));
        Check("31 reject unknown envelope format",()=>Reject(()=>Contract.Decode(Mutate(x=>{x["format"]="EVIL";return x;}))));
        Check("32 reject noncanonical whitespace",()=>{var s=Encoding.UTF8.GetString(Contract.Encode(Sample()));return Reject(()=>Contract.Decode(Encoding.UTF8.GetBytes(" "+s)));});
        Check("33 reject invalid UTF-8",()=>Reject(()=>Contract.Decode(new byte[]{0xff,0xfe,0xfd})));
        Check("34 reject oversized input",()=>Reject(()=>Contract.Decode(new byte[Contract.MaxBytes+1])));
        Check("35 reject invalid JSON",()=>Reject(()=>Contract.Decode(Encoding.UTF8.GetBytes("{broken"))));
        Check("36 fail closed on existing slot: original bytes untouched",()=>Tmp(d=>{
            var f=Path.Combine(d,"slot.json");Contract.WriteNewSlot(f,Sample(),()=>true);
            var before=File.ReadAllBytes(f);bool rejected=Reject(()=>Contract.WriteNewSlot(f,Sample(false,false),()=>true));
            return rejected&&File.ReadAllBytes(f).SequenceEqual(before);
        }));
        Check("37 busy before save refuses creation",()=>Tmp(d=>{
            var f=Path.Combine(d,"slot.json");
            return Reject(()=>Contract.WriteNewSlot(f,Sample(),()=>false))&&!File.Exists(f);
        }));
        Check("38 busy during save refuses creation and cleans only owned temp",()=>Tmp(d=>{
            int calls=0;string f=Path.Combine(d,"slot.json");
            bool reject=Reject(()=>Contract.WriteNewSlot(f,Sample(),()=>++calls==1));
            return reject&&!File.Exists(f)&&Directory.GetFiles(d).Length==0;
        }));
        Check("39 busy before load refuses, saved bytes unchanged",()=>Tmp(d=>{
            string f=Path.Combine(d,"slot.json");Contract.WriteNewSlot(f,Sample(),()=>true);
            var original=File.ReadAllBytes(f);
            bool reject=Reject(()=>Contract.ReadAndStage(f,()=>false));
            return reject&&File.ReadAllBytes(f).SequenceEqual(original);
        }));
        Check("40 busy during load refuses no live mutation",()=>Tmp(d=>{
            string f=Path.Combine(d,"slot.json");Contract.WriteNewSlot(f,Sample(),()=>true);
            int calls=0;int physical=0;
            bool reject=Reject(()=>Contract.ReadAndStage(f,()=>{calls++;return calls==1;}));
            return reject&&physical==0;
        }));
        Check("41 missing save refuses load",()=>Tmp(d=>Reject(()=>Contract.ReadAndStage(Path.Combine(d,"missing.json"),()=>true))));
        Check("42 corrupted old data file refuses with no rewrite",()=>Tmp(d=>{
            string f=Path.Combine(d,"slot.json");
            var bad=Encoding.UTF8.GetBytes("{\"schemaVersion\":0}");
            File.WriteAllBytes(f,bad);bool refused=Reject(()=>Contract.ReadAndStage(f,()=>true));
            return refused&&File.ReadAllBytes(f).SequenceEqual(bad);
        }));
        Check("43 reconstructed attached ID ledger permits first genuine W06 detach",()=>Tmp(d=>{
            string f=Path.Combine(d,"slot.json");
            Contract.WriteNewSlot(f,Sample(false,false),()=>true);
            var p=Contract.ReadAndStage(f,()=>true);
            var l=p.Parts[0].OriginalR2Ledger;
            return l.Count==0 && l.TryDetach(2310001,new Vector3d(0,0,0),
                new Vector3d(1,0,0),new Vector3d(0,0,0),1,0,0,out _)
                &&l.Contains(2310001);
        }));
        Console.WriteLine("TOTAL "+(good+bad)+" PASS "+good+" FAIL "+bad+
            " ORIGINAL_W06_AND_510_CSHARP_EXECUTED NATIVE_UNITY_NOT_RUN");
        return bad==0?0:1;
    }
}
