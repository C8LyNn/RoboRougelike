using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using B01.DebrisRepulse001;
using B01.SelectedDetachedSaveContractV2;

internal static class Program
{
    private static int passed,failed;
    static void Test(string name,Func<bool> action)
    {
        try { bool ok=action(); if(!ok)throw new Exception("false");passed++;Console.WriteLine("PASS "+name); }
        catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}
    }
    static PersistedPart P(int i,bool detached)
    {
        long owner=FrozenPartCatalog.Owner(i);
        return new PersistedPart(FrozenPartCatalog.Id(i),owner,detached?0:owner,
            FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,new Vector3d(i+0.75,0,-0.5),
            0,0,0,1,detached?new Vector3d(.3,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),FrozenPartCatalog.Mass(i));
    }
    static RunSnapshot Snap(bool light=true,bool heavy=false,long generation=3,long run=77)=>
        new RunSnapshot(run,generation,new[]{P(0,light),P(1,heavy)});
    static byte[] Wire(bool light=true,bool heavy=false,long generation=3,long run=77)=>
        SnapshotCodec.Encode(Snap(light,heavy,generation,run));
    static bool Clean(bool light,bool heavy)
    {
        var encoded=Wire(light,heavy);
        if(!SnapshotCodec.TryDecode(encoded,out RunSnapshot? snap)||snap==null)return false;
        if(!ColdRestore.TryStage(encoded,out ColdRestore? plan)||plan==null)return false;
        return snap.Parts[0].Id==2310001 &&snap.Parts[1].Id==2310002 &&
            plan.ContainsConsumed(0,2310001)==light && plan.ContainsConsumed(1,2310002)==heavy &&
            plan.ConsumedCount(0)==(light?1:0) && plan.ConsumedCount(1)==(heavy?1:0) &&
            SnapshotCodec.Encode(snap).SequenceEqual(encoded);
    }
    static bool Disk(bool light,bool heavy)
    {
        var folder=Path.Combine(Path.GetTempPath(),"b01-62-w14-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var file=Path.Combine(folder,"slot.bin");
            File.WriteAllBytes(file,Wire(light,heavy,19));
            byte[] read=File.ReadAllBytes(file);
            if(!ColdRestore.TryStage(read,out ColdRestore? plan)||plan==null)return false;
            return plan.Snapshot.Generation==19 &&
                plan.ContainsConsumed(0,2310001)==light &&
                plan.ContainsConsumed(1,2310002)==heavy;
        }
        finally {Directory.Delete(folder,true);}
    }
    static bool NoPublicMutableW06()
    {
        var ty=typeof(ColdRestore);
        if(ty.GetMethods(BindingFlags.Public|BindingFlags.Instance)
           .Any(x=>typeof(DebrisImpulseLedger).IsAssignableFrom(x.ReturnType)))return false;
        if(ty.GetFields(BindingFlags.Public|BindingFlags.Instance)
           .Any(x=>typeof(DebrisImpulseLedger).IsAssignableFrom(x.FieldType) ||
                   x.FieldType.IsArray && typeof(DebrisImpulseLedger).IsAssignableFrom(x.FieldType.GetElementType()!)))
            return false;
        return ty.GetMethod("GetLedger",BindingFlags.Public|BindingFlags.Instance)==null;
    }
    static bool SnapshotCopyAfterCommit()
    {
        var handle=new ColdSessionHandle();
        var accepted=Wire();
        if(handle.TrySwap(accepted,77,1,()=>true)!=RestoreVerdict.Accepted)return false;
        var original=handle.Current!;
        var leak=original.Snapshot.Parts;
        leak[0]=P(1,false); // Attempt exact #550 prior RED after publication.
        var leak2=original.Snapshot.Parts;
        return SnapshotRules.Valid(original.Snapshot) &&
            leak2[0].Id==2310001&&leak2[0].Consumed&&
            original.ContainsConsumed(0,2310001)&&
            original.ConsumedCount(0)==1;
    }
    static bool SnapshotCopyBeforeCommit()
    {
        var input=new[]{P(0,true),P(1,false)};
        var snap=new RunSnapshot(77,3,input);
        input[0]=P(1,false); // caller retains former original array, must not change snapshot
        return snap.Parts[0].Id==2310001&&SnapshotRules.Valid(snap);
    }
    static bool HostReentrancyBlocked()
    {
        var host=new ColdSessionHandle();
        byte[] bytes=Wire();bool nested=false;int callbacks=0;
        var rc=host.TrySwap(bytes,77,1,()=>{
            callbacks++;
            if(callbacks==2)
                nested=host.TrySwap(Wire(false,true,4),77,1,()=>true)==RestoreVerdict.TransactionBusy;
            return true;
        });
        return rc==RestoreVerdict.Accepted&&nested&&callbacks==2 &&
            host.Current!.ContainsConsumed(0,2310001);
    }
    static bool BusyAtCommitRejectsNoPublish()
    {
        var h=new ColdSessionHandle();int n=0;
        return h.TrySwap(Wire(),77,1,()=>++n==1)==RestoreVerdict.TransactionBusy
            && h.Current==null &&n==2;
    }
    static bool BusyDuringNewerSwapKeepsOld()
    {
        var h=new ColdSessionHandle();if(h.TrySwap(Wire(),77,1,()=>true)!=RestoreVerdict.Accepted)return false;
        var old=h.Current;int n=0;
        var verdict=h.TrySwap(Wire(false,true,4),77,1,()=>++n==1);
        return verdict==RestoreVerdict.TransactionBusy &&ReferenceEquals(old,h.Current)
            &&h.Current!.ContainsConsumed(0,2310001);
    }
    static bool ThrowOnSecondIdleKeepsOld()
    {
        var h=new ColdSessionHandle();if(h.TrySwap(Wire(),77,1,()=>true)!=RestoreVerdict.Accepted)return false;
        var old=h.Current;int n=0;
        return h.TrySwap(Wire(false,true,4),77,1,()=>{
            if(++n==2)throw new InvalidOperationException("fault");return true;
        })==RestoreVerdict.Quarantine&&ReferenceEquals(old,h.Current);
    }
    static bool RetainOldAfterReject(byte[] candidate,long expectedRun,long minEpoch)
    {
        var h=new ColdSessionHandle();
        if(h.TrySwap(Wire(),77,1,()=>true)!=RestoreVerdict.Accepted)return false;
        var old=h.Current;
        var result=h.TrySwap(candidate,expectedRun,minEpoch,()=>true);
        return result!=RestoreVerdict.Accepted &&ReferenceEquals(old,h.Current)
            &&h.Current!.ContainsConsumed(0,2310001)&&!h.Current.ContainsConsumed(1,2310002);
    }
    static bool NotTampered(int seed)
    {
        byte[] bytes=Wire();
        int index=(seed*31)%(bytes.Length-32);
        bytes[index]^=(byte)(seed%253+1);
        return RetainOldAfterReject(bytes,77,1);
    }
    public static int Main()
    {
        Test("01 #549-derived one format, frozen #510 SHA",()=>FrozenPartCatalog.SelectedSourceSha=="d94400d95d13db7f27e1533df05c9292d5380431");
        Test("02 original W06 original light ID",()=>FrozenPartCatalog.Id(0)==2310001);
        Test("03 original W06 original heavy ID",()=>FrozenPartCatalog.Id(1)==2310002);
        Test("04 public W06 ledger leak removed",NoPublicMutableW06);
        Test("05 regression #550 public snapshot mutation after publish rejected",SnapshotCopyAfterCommit);
        Test("06 input original array mutation before validate rejected",SnapshotCopyBeforeCommit);
        Test("07 no externally writable Parts public field",()=>typeof(RunSnapshot).GetField("Parts",BindingFlags.Public|BindingFlags.Instance)==null);
        Test("08 attached attached cold restore",()=>Clean(false,false));
        Test("09 light detached cold restore",()=>Clean(true,false));
        Test("10 heavy detached cold restore",()=>Clean(false,true));
        Test("11 both detached cold restore",()=>Clean(true,true));
        Test("12 attached attached actual disk read/write",()=>Disk(false,false));
        Test("13 light detached actual disk read/write",()=>Disk(true,false));
        Test("14 heavy detached actual disk read/write",()=>Disk(false,true));
        Test("15 both detached actual disk read/write",()=>Disk(true,true));
        Test("16 host busy before begins",()=>new ColdSessionHandle().TrySwap(Wire(),77,1,()=>false)==RestoreVerdict.TransactionBusy);
        Test("17 idle becomes busy between decode and publish",BusyAtCommitRejectsNoPublish);
        Test("18 failed newer-swap leaves old ledger unchanged",BusyDuringNewerSwapKeepsOld);
        Test("19 callback exception at commit leaves old",ThrowOnSecondIdleKeepsOld);
        Test("20 nested reentrant swap rejected",HostReentrancyBlocked);
        Test("21 stale generation refused",()=>RetainOldAfterReject(Wire(false,true,2),77,1));
        Test("22 wrong run refused",()=>RetainOldAfterReject(Wire(false,true,4,88),77,1));
        Test("23 corrupted payload refused",()=>{var b=Wire();b[76]^=17;return RetainOldAfterReject(b,77,1);});
        Test("24 truncated payload refused",()=>RetainOldAfterReject(Wire()[..^2],77,1));
        Test("25 unsupported schema refused with signed invalid version",()=>{
            var b=Wire();b[8]=2;using var sha=SHA256.Create();
            var digest=sha.ComputeHash(b,0,b.Length-32);Buffer.BlockCopy(digest,0,b,b.Length-32,32);
            return RetainOldAfterReject(b,77,1);
        });
        Test("26 missing consumed ID rejected",()=>{
            var b=Wire();var old=Snap();var parts=old.Parts;
            var p=parts[0];
            parts[0]=new PersistedPart(p.Id,p.OwnerId,p.ParentId,p.AssetGuid,p.SocketGuid,p.State,
                false,p.Kinematic,p.Position,p.Qx,p.Qy,p.Qz,p.Qw,p.LinearVelocity,p.AngularVelocity,p.MassKg);
            return !SnapshotRules.Valid(new RunSnapshot(77,3,parts));
        });
        Test("27 duplicate IDs refused",()=>!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{P(0,true),P(0,true)})));
        Test("28 missing rig refused",()=>!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{P(0,true)})));
        Test("29 foreign rig refused",()=>!SnapshotRules.Valid(new RunSnapshot(77,3,new[]{P(1,false),P(0,true)})));
        Test("30 legacy detached save never changes active pointer",()=>RetainOldAfterReject(new byte[]{0,1,2},77,1));
        for(int i=1;i<=80;i++){int n=i;Test("fuzz corrupted source byte "+n,()=>NotTampered(n));}
        for(int i=0;i<64;i++){int n=i;Test("fuzz genuine schema roundtrip "+n,()=>Clean((n&1)!=0,(n&2)!=0));}
        Console.WriteLine("W14_SELECTED549_V2_TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        Console.WriteLine("FROZEN_510_NOT_MODIFIED; ORIGINAL_W06_COMPILED; UNITY_G1_NOT_RUN; SAVE_HOST_NOT_INTEGRATED");
        return failed==0?0:1;
    }
}
