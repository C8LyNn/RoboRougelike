// B01-62 P17/W14 independent original-source executable defect RED -> sealed facade GREEN.
// Read actual selected #549 C# and actual original W06 C# directly in .NET8. NO Unity.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.B0162.SelectedSaveFacade;

internal static class Program
{
    private static int checks,attacks,positive,denials;
    private static readonly Vector3d Zero=new Vector3d(0,0,0);
    private static void Check(bool b,string text)
    {
        if(!b)throw new Exception("INDEPENDENT_QA_FAIL: "+text);
        checks++;
    }
    private static PersistedPart Part(int slot,bool detached)
    {
        var id=FrozenPartCatalog.Id(slot);
        return new PersistedPart(id,FrozenPartCatalog.Owner(slot),detached?0:FrozenPartCatalog.Owner(slot),
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,new Vector3d(slot+0.5,0,-0.5),
            0,0,0,1,detached?new Vector3d(0.3,0,0):Zero,Zero,FrozenPartCatalog.Mass(slot));
    }
    private static byte[] Wire(bool light,bool heavy,long run=77,long generation=3)
        =>SnapshotCodec.Encode(new RunSnapshot(run,generation,new []{Part(0,light),Part(1,heavy)}));
    private static void LegacyKnownRed(byte[] bytes)
    {
        // RED originally observed in B01-61 #550: source is unchanged and still unsafe if directly used.
        var raw=new ColdSessionHandle();
        Check(raw.TrySwap(bytes,77,1,()=>true)==RestoreVerdict.Accepted,"source baseline accepted");
        var exposed=raw.Current!;
        Check(exposed.GetLedger(0).Contains(2310001),"precondition consumed W06");
        exposed.Snapshot.Parts[0]=Part(1,false);
        Check(!SnapshotRules.Valid(raw.Current!.Snapshot),"ORIGINAL_549_RED exposed mutable validated array");
        Check(raw.Current.GetLedger(0).Contains(2310001),"ORIGINAL_549_RED ledger still consumed after array write");
        attacks++;
        var raw2=new ColdSessionHandle();
        Check(raw2.TrySwap(bytes,77,1,()=>true)==RestoreVerdict.Accepted,"second source baseline");
        Check(raw2.Current!.GetLedger(0).RearmAfterReattach(2310001),
              "ORIGINAL_549_RED public ledger can rearm without dock");
        Check(raw2.Current.Snapshot.Parts[0].Consumed&&!raw2.Current.GetLedger(0).Contains(2310001),
              "ORIGINAL_549_RED metadata and ledger diverged");
        attacks++;
        Console.WriteLine("ORIGINAL_549_DIRECT_API_RED=2_REPRODUCED");
    }
    private static void SealedBoundary()
    {
        var t=typeof(SecureColdSession);
        foreach(var f in t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.Static))
            Check(f.FieldType!=typeof(ColdSessionHandle) && f.FieldType!=typeof(ColdRestore) &&
                  f.FieldType!=typeof(RunSnapshot) && f.FieldType!=typeof(DebrisImpulseLedger) &&
                  !f.FieldType.IsArray,"no mutable original field "+f.Name);
        foreach(var member in t.GetMembers(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly))
        {
            if(member is MethodInfo m)
            {
                Type type=m.ReturnType;
                Check(type!=typeof(ColdSessionHandle) && type!=typeof(ColdRestore) &&
                      type!=typeof(RunSnapshot) && type!=typeof(DebrisImpulseLedger),
                      "no mutable original return "+m.Name);
                foreach(var par in m.GetParameters())
                    Check(par.ParameterType!=typeof(RunSnapshot) &&
                          par.ParameterType!=typeof(DebrisImpulseLedger) &&
                          par.ParameterType!=typeof(ColdRestore),"no mutable API param "+m.Name);
            }
        }
        Check(!t.GetProperties(BindingFlags.Instance|BindingFlags.Public).Any(),
              "no public pointer properties on facade");
        Check(typeof(PartView).IsValueType &&
              typeof(PartView).GetFields(BindingFlags.Instance|BindingFlags.Public).All(f=>f.IsInitOnly),
              "all exported PartView fields readonly scalars");
    }
    private static void RoundTrip(bool light,bool heavy,string path)
    {
        byte[] raw=Wire(light,heavy);
        File.WriteAllBytes(path,raw);
        var session=new SecureColdSession(); // BRAND NEW empty logical process state
        Check(session.TryColdLoad(File.ReadAllBytes(path),77,1,()=>true)==RestoreVerdict.Accepted,
            "cold disk save roundtrip");
        Check(session.TryGetHeader(out long run,out long gen)&&run==77&&gen==3,
            "original session epoch recovered");
        for(int slot=0;slot<2;slot++)
        {
            Check(session.TryGetPart(slot,out PartView part),"read-only accepted part");
            Check(part.Id==FrozenPartCatalog.Id(slot)&&part.AssetGuid==FrozenPartCatalog.Asset(slot),
                  "immutable stable id and asset guid");
            bool expected=slot==0?light:heavy;
            Check(part.Detached==expected && part.Consumed==expected && part.Kinematic!=expected,
                  "physical+ledger logical state consistent");
            Check(part.MassKg==FrozenPartCatalog.Mass(slot),"original two distinct masses restored");
        }
        Check(session.TryExport(out byte[]? export)&&export!=null,"copy-only re-export");
        Check(export!.SequenceEqual(raw),"source codec wire roundtrip byte-exact");
        export[0]^=0xff; // intentional corruption on caller copy
        Check(session.TryExport(out byte[]? again)&&again!.SequenceEqual(raw),
              "caller cannot corrupt published session via exported bytes");
        positive++;
    }
    private static void RejectsNoMutation()
    {
        var safe=new SecureColdSession();
        var initial=Wire(true,false);
        Check(safe.TryColdLoad(initial,77,1,()=>true)==RestoreVerdict.Accepted,"load initial");
        Check(safe.TryExport(out byte[]? first)&&first!=null,"initial export");

        byte[] tampered=(byte[])initial.Clone();tampered[tampered.Length/2]^=7;
        Check(safe.TryColdLoad(tampered,77,1,()=>true)==RestoreVerdict.InvalidSnapshot,"corrupt rejected");
        Check(safe.TryColdLoad(Wire(true,true,78),77,1,()=>true)==RestoreVerdict.WrongRun,"wrong run rejected");
        Check(safe.TryColdLoad(Wire(true,true,77,2),77,1,()=>true)==RestoreVerdict.StaleGeneration,"stale rejected");
        Check(safe.TryColdLoad(Wire(true,true),77,1,()=>false)==RestoreVerdict.TransactionBusy,"busy rejected");
        Check(safe.TryColdLoad(Wire(true,true),77,1,()=>throw new InvalidOperationException())==RestoreVerdict.Quarantine,"host exception quarantined");
        Check(safe.TryColdLoad(null,77,1,()=>true)==RestoreVerdict.InvalidSnapshot,"null rejected");
        Check(safe.TryColdLoad(new byte[5],77,1,()=>true)==RestoreVerdict.InvalidSnapshot,"short rejected");
        Check(safe.TryExport(out byte[]? current)&&current!.SequenceEqual(first!),"old session untouched by all rejections");
        Check(!safe.TryGetPart(-1,out _)&&!safe.TryGetPart(2,out _),"out of range returns false");
        denials+=7;
        var changing=Wire(true,true,77,4);
        byte[] pristine=(byte[])changing.Clone();
        Check(safe.TryColdLoad(changing,77,1,()=>{
            // Mutate caller buffer AFTER receiver has copied it but BEFORE producer validates.
            Array.Fill(changing,(byte)0);
            return true;
        })==RestoreVerdict.Accepted,"host callback cannot invalidate private saved bytes");
        Check(safe.TryExport(out byte[]? outData)&&outData!.SequenceEqual(pristine),
              "published session does not reference attacker input buffer");
        Check(safe.TryGetPart(1,out PartView heavy)&&heavy.Detached&&heavy.Consumed,
              "detached heavy retained in protected session");
    }
    public static int Main()
    {
        try
        {
            var scratch=Path.Combine(Path.GetTempPath(),"b01_62_w14_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                LegacyKnownRed(Wire(true,false));
                SealedBoundary();
                foreach(bool light in new[]{false,true})
                foreach(bool heavy in new[]{false,true})
                    RoundTrip(light,heavy,Path.Combine(scratch,$"{light}_{heavy}.save"));
                RejectsNoMutation();
            }
            finally{Directory.Delete(scratch,true);}
            Console.WriteLine($"B01_62_W14_SOURCE_QA_PASS_ASSERTIONS={checks} RED_BASELINE={attacks} FAC_ADE_COLD_ROUNDTRIPS={positive} NEGATIVE_DENIALS={denials}");
            Console.WriteLine("CORRECTED_RECEIVER_FACADE=SOURCE_GREEN ORIGINAL_PRODUCER_DIRECT_API=RED");
            Console.WriteLine("PRODUCTION_HOST_SAVELOAD=HOLD NATIVE_UNITY_G1=NOT_RUN G2=HOLD");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
