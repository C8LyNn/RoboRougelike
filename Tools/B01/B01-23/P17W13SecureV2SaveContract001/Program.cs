// Executes original selected #510 AtomicDockLedger and original W06 DebrisImpulseLedger.
// No native Unity Engine is present; no claim to capture/restore private Rig or physics.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using B01.SecureV2SaveContract001;

internal static class Program
{
    static int passed;
    static void Check(bool condition,string label)
    {
        if(!condition)throw new Exception("SAVE_CONTRACT_TEST_RED "+label);
        passed++;
        Console.WriteLine("SAVE_CONTRACT_PASS "+label);
    }
    static void Reject(Action action,string label)
    {
        try{action();}
        catch(InvalidDataException){Check(true,"reject "+label);return;}
        catch(InvalidOperationException){Check(true,"reject "+label);return;}
        throw new Exception("FALSE_GREEN "+label);
    }
    static readonly TrustedManifest Authority=TrustedManifest.SelectedFixture();
    static readonly Guid Run=Guid.Parse("4a3df6d1-0b57-4e92-a8d9-1c1c6bd2a888");
    static PartRecord Part(int index,bool detached)
    {
        var p=Authority.Parts[index];
        V3 mount=p.Mount;
        V3 pos=detached?new V3(p.RootX+mount.X+2,index+2,3):
            new V3(p.RootX+mount.X,mount.Y,mount.Z);
        return new PartRecord(p.Id,p.ModuleGuid,detached,detached,!detached,
            pos,Q4.Identity,
            detached?new V3(.5,2,-.25):V3.Zero,
            detached?new V3(0,.1,0):V3.Zero,mount);
    }
    static RunSnapshot State(bool a,bool b,long gen=7) =>
        new RunSnapshot(TrustedManifest.SelectedSourceSha,Run,gen,false,
            new[]{Part(0,a),Part(1,b)});
    static RunSnapshot Replace(RunSnapshot snapshot,PartRecord a,PartRecord b)
        =>new RunSnapshot(snapshot.SourceSha,snapshot.RunId,snapshot.Generation,snapshot.TransactionPending,
            new[]{a,b});
    static PartRecord Rewrite(PartRecord p,long? id=null,Guid? guid=null,
        bool? detached=null,bool? consumed=null,bool? kinematic=null,
        V3? pos=null,Q4? rot=null,V3? linear=null,V3? angular=null,V3? mount=null)
        =>new PartRecord(id??p.Id,guid??p.ModuleGuid,detached??p.Detached,
            consumed??p.Consumed,kinematic??p.IsKinematic,pos??p.WorldPosition,
            rot??p.WorldRotation,linear??p.LinearVelocity,angular??p.AngularVelocity,mount??p.LocalMount);
    static void Stage(bool a,bool b)
    {
        var original=State(a,b);
        var bytes=StableIdSnapshotCodec.Encode(original,Authority);
        Check(bytes.Length==StableIdSnapshotCodec.TotalBytes,"binary schema fixed size");
        var parsed=StableIdSnapshotCodec.Decode(bytes,Authority,Run,7);
        Check(parsed.Generation==7 && parsed.RunId==Run,"session + generation roundtrip");
        for(int i=0;i<2;i++)
        {
            Check(parsed.Parts[i].Id==original.Parts[i].Id,"stable ID roundtrip "+i);
            Check(parsed.Parts[i].ModuleGuid==original.Parts[i].ModuleGuid,"module GUID roundtrip "+i);
            Check(parsed.Parts[i].Detached==original.Parts[i].Detached,"detached state roundtrip "+i);
            Check(parsed.Parts[i].Consumed==original.Parts[i].Consumed,"consumed ledger flag roundtrip "+i);
            Check(parsed.Parts[i].WorldPosition.X==original.Parts[i].WorldPosition.X,"world physical pose roundtrip "+i);
        }
        var prepared=StableIdSnapshotCodec.Prepare(bytes,Authority,Run,7,false);
        Check(prepared.Snapshot.Generation==7,"prepare no mutation snapshot");
        for(int i=0;i<2;i++)
        {
            bool detached=original.Parts[i].Detached;
            var ledger=prepared.StagedLedger(i);
            Check(ledger.Count==(detached?1:0),"original R2 AtomicDockLedger count "+i);
            Check(ledger.Contains(original.Parts[i].Id)==detached,"original R2/W06 consumed membership "+i);
            if(detached)
            {
                Check(!ledger.TryDetach(original.Parts[i].Id,new B01.DebrisRepulse001.Vector3d(0,0,0),
                    new B01.DebrisRepulse001.Vector3d(0,0,0),
                    new B01.DebrisRepulse001.Vector3d(0,0,0),1,0,0,
                    out B01.DebrisRepulse001.Vector3d _),"no repeat detach after load "+i);
                Check(ledger.TryReattach(original.Parts[i].Id,()=>{},()=>{}),
                    "once-only original W06 real rearm "+i);
                Check(!ledger.Contains(original.Parts[i].Id),"ID rearmed before dock "+i);
                Check(!ledger.TryReattach(original.Parts[i].Id,()=>{},()=>{}),
                    "repeated reattach rejected "+i);
            }
            else Check(!ledger.TryReattach(original.Parts[i].Id,()=>{},()=>{}),
                "attached ID cannot be rearmed "+i);
        }
        Check(Convert.ToHexString(SHA256.HashData(bytes)).Length==64,"canonical encoded full record digest");
    }
    static byte[] Alter(byte[] raw,int offset,byte value)
    {
        var copy=(byte[])raw.Clone();copy[offset]=value;
        using(var sha=SHA256.Create())
        {
            byte[] sum=sha.ComputeHash(copy,0,copy.Length-32);
            Array.Copy(sum,0,copy,copy.Length-32,32);
        }
        return copy;
    }
    static void Invalid()
    {
        RunSnapshot snapshot=State(true,false);
        byte[] raw=StableIdSnapshotCodec.Encode(snapshot,Authority);
        var p0=snapshot.Parts[0];var p1=snapshot.Parts[1];
        Reject(()=>StableIdSnapshotCodec.Decode(null,Authority,Run,7),"null");
        Reject(()=>StableIdSnapshotCodec.Decode(raw.Take(raw.Length-1).ToArray(),Authority,Run,7),"truncated");
        var corrupted=(byte[])raw.Clone();corrupted[123]^=1;
        Reject(()=>StableIdSnapshotCodec.Decode(corrupted,Authority,Run,7),"broken checksum");
        Reject(()=>StableIdSnapshotCodec.Decode(Alter(raw,0,0),Authority,Run,7),"forged legacy magic with matching checksum");
        Reject(()=>StableIdSnapshotCodec.Decode(Alter(raw,8,2),Authority,Run,7),"unknown v2 schema");
        Reject(()=>StableIdSnapshotCodec.Decode(Alter(raw,10,(byte)'f'),Authority,Run,7),"wrong producer SHA");
        Reject(()=>StableIdSnapshotCodec.Decode(Alter(raw,75,1),Authority,Run,7),"missing part count");
        Reject(()=>StableIdSnapshotCodec.Decode(Alter(raw,100,2),Authority,Run,7),"invalid bool representation");
        Reject(()=>StableIdSnapshotCodec.Decode(Alter(raw,74,1),Authority,Run,7),"in-progress transaction persisted");
        Reject(()=>StableIdSnapshotCodec.Decode(raw,Authority,Guid.NewGuid(),7),"foreign run");
        Reject(()=>StableIdSnapshotCodec.Decode(raw,Authority,Run,8),"stale save generation");
        Reject(()=>StableIdSnapshotCodec.Prepare(raw,Authority,Run,7,true),"cannot load inside live detach dock reset");
        Reject(()=>StableIdSnapshotCodec.Encode(new RunSnapshot(snapshot.SourceSha,Guid.Empty,7,false,
            snapshot.Parts),Authority),"empty run UUID");
        Reject(()=>StableIdSnapshotCodec.Encode(new RunSnapshot(snapshot.SourceSha,Run,-1,false,
            snapshot.Parts),Authority),"negative generation");
        Reject(()=>StableIdSnapshotCodec.Encode(new RunSnapshot(snapshot.SourceSha,Run,7,true,
            snapshot.Parts),Authority),"snapshot capture while busy");
        Reject(()=>StableIdSnapshotCodec.Encode(new RunSnapshot("baf65f2664e38f3b9c38815c3f07285f8107c1b6",
            Run,7,false,snapshot.Parts),Authority),"alternative #511 SHA");
        Reject(()=>StableIdSnapshotCodec.Encode(new RunSnapshot("6efe45961ed0ffdf183e8016e350978b9f3dc51c",
            Run,7,false,snapshot.Parts),Authority),"alternative #494 SHA");
        Reject(()=>StableIdSnapshotCodec.Encode(new RunSnapshot(snapshot.SourceSha,Run,7,false,
            new[]{p0}),Authority),"missing second part");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,p1,p0),Authority),"reordered parts");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,p0,p0),Authority),"duplicate stable ID+GUID");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,id:999999),p1),Authority),"unknown foreign part ID");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,guid:Guid.NewGuid()),p1),Authority),"foreign module GUID");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,consumed:false),p1),Authority),"detached+unconsumed contradictory state");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p1,consumed:true),p1),Authority),"attached+consumed contradictory state");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,kinematic:true),p1),Authority),"detached+kinematic contradictory state");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p1,kinematic:false),p1),Authority),"attached+dynamic contradictory state");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,mount:new V3(100,1,0)),p1),Authority),"unauthorized local mount change");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p1,pos:new V3(100,1,0)),p1),Authority),"attached body outside canonical mount");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p1,linear:new V3(1,0,0)),p1),Authority),"attached body has velocity");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,pos:new V3(double.NaN,0,0)),p1),Authority),"NaN physical position");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,linear:new V3(double.PositiveInfinity,0,0)),p1),Authority),"infinite linear velocity");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,rot:new Q4(0,0,0,0)),p1),Authority),"unnormalized quaternion");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p0,pos:new V3(200000,0,0)),p1),Authority),"unbounded physical coordinate");
        Reject(()=>StableIdSnapshotCodec.Encode(Replace(snapshot,Rewrite(p1,angular:new V3(0,.5,0)),p1),Authority),"attached angular velocity");
        Check(Authority.Parts[0].Id==2310001 && Authority.Parts[1].Id==2310002,
            "original exact selected SecureRigCatalog stable IDs");
    }
    public static void Main()
    {
        Stage(false,false);Stage(true,false);Stage(false,true);Stage(true,true);
        Invalid();
        var raw=StableIdSnapshotCodec.Encode(State(true,false),Authority);
        Console.WriteLine("CANONICAL_SAVE_BYTES="+raw.Length+
            " SHA256="+Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant());
        Console.WriteLine("SOURCE_SAVELOAD_ASSERTIONS_PASS="+passed+
            " ACTUAL_ORIGINAL_W06_LEDGER=YES R2_ATOMIC_LEDGER=YES UNITY_NATIVE=NOT_RUN");
    }
}
