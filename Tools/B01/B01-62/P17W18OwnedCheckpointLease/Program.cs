using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using B01.B0162.W18;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;

internal static class Program
{
    private static int asserts, rejected, originalCas, crossProcess;
    private static void Assert(bool yes,string label)
    {
        if(!yes)throw new Exception("B01_62_W18_ASSERT_FAIL:"+label);
        asserts++;
    }
    private static bool TryAcquire(string dir)
    {
        try {using var gate=new OwnedCheckpointSlotLease(dir);return true;}
        catch(IOException){return false;}
    }
    private static void Reject(Action action,string label)
    {
        bool denied=false;
        try{action();}
        catch(IOException){denied=true;}
        Assert(denied,label);rejected++;
    }
    private static int Child(string[] args)
    {
        string mode=args[1],dir=args[2];
        bool got=TryAcquire(dir);
        if(mode=="blocked"&&!got){Console.WriteLine("CHILD_CROSS_PROCESS_LEASE_DENIED");return 0;}
        if(mode=="granted"&&got){Console.WriteLine("CHILD_CROSS_PROCESS_LEASE_GRANTED");return 0;}
        Console.Error.WriteLine("WRONG_CHILD_LEASE_RESULT:"+mode+":"+got);
        return 3;
    }
    private static void VerifyChild(string directory,string mode)
    {
        var p=new ProcessStartInfo("dotnet")
        {
            UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true
        };
        p.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        p.ArgumentList.Add("--probe");p.ArgumentList.Add(mode);p.ArgumentList.Add(directory);
        using var child=Process.Start(p)??throw new IOException("No child process");
        string stdout=child.StandardOutput.ReadToEnd();
        string stderr=child.StandardError.ReadToEnd();
        child.WaitForExit();
        Assert(child.ExitCode==0&&stdout.Contains(mode=="blocked"?"CHILD_CROSS_PROCESS_LEASE_DENIED":
            "CHILD_CROSS_PROCESS_LEASE_GRANTED"),"real NEW .NET process "+mode+" "+stderr);
        crossProcess++;
    }
    private static byte[] Original556Fixture()
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=i==0;
            long id=FrozenPartCatalog.Id(i),owner=FrozenPartCatalog.Owner(i);
            var zero=new Vector3d(0,0,0);
            parts[i]=new PersistedPart(id,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(i+1,detached?2:1,0),
                0,0,0,1,detached?new Vector3d(.25,0,0):zero,zero,
                FrozenPartCatalog.Mass(i));
        }
        byte[] bytes=SnapshotCodec.Encode(new RunSnapshot(77,1,parts));
        Assert(ColdRestore.TryStage(bytes,out ColdRestore? stage)&&stage!=null,
            "actual #556 original binary codec and W06 ColdRestore");
        Assert(stage!.ContainsConsumedId(0,parts[0].Id)&&
               !stage.ContainsConsumedId(1,parts[1].Id),
               "genuine #556 original consumed ID persists");
        return bytes;
    }
    private static void Run()
    {
        string scratch=Path.Combine(Path.GetTempPath(),"b01-62-w18-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try
        {
            string own=Path.Combine(scratch,"unique-slot-A");
            OwnedCheckpointSlotLease.CreateOwnedSlot(own);
            Reject(()=>OwnedCheckpointSlotLease.CreateOwnedSlot(own),"occupied slot cannot be recreated");
            byte[] original=Original556Fixture();
            string h=Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant();
            Assert(h.Length==64,"original SHA256 source Save fingerprint 32 bytes");

            LeaseBoundOriginal596Store old;
            using(var lease=new OwnedCheckpointSlotLease(own))
            {
                old=new LeaseBoundOriginal596Store(lease);
                Assert(lease.IsHeld,"owns exclusive OS file lock");
                Assert(old.Read(77,out long first,out string empty)&&first==0&&empty=="",
                    "real original #596 FileCheckpointStore initial state");
                Assert(old.Commit(77,0,"",1,h),"real original #596 physical sidecar durable commit");
                originalCas++;
                Assert(old.Read(77,out long gen,out string digest)&&gen==1&&digest==h,
                    "real #596 original checkpoint generation/hash persisted to file");
                Assert(!old.Commit(77,0,"",2,h),"original #596 CAS detects stale writer");
                originalCas++;
                Assert(!TryAcquire(own),"second active OS host same directory denied");
                rejected++;
                VerifyChild(own,"blocked");
                string other=Path.Combine(scratch,"unique-slot-B");
                OwnedCheckpointSlotLease.CreateOwnedSlot(other);
                using(var leaseB=new OwnedCheckpointSlotLease(other))
                {
                    var storeB=new LeaseBoundOriginal596Store(leaseB);
                    Assert(storeB.Read(99,out long otherGen,out string otherHash)&&otherGen==0&&otherHash=="",
                        "different owner-created slot has independent generation authority");
                    Assert(storeB.Commit(99,0,"",1,new string('b',64)),
                        "parallel different slot allowed and independent");
                    originalCas++;
                }
            }
            // In-memory stale actor retains object, but must not read/commit
            // even if a separate new process or live actor acquired the lease.
            Assert(!old.Read(77,out _,out _),"released old host cannot read through wrapper");
            Assert(!old.Commit(77,1,h,2,new string('c',64)),
                "released old host cannot publish higher checkpoint over new owner");
            rejected+=2;
            VerifyChild(own,"granted");
            using(var fresh=new OwnedCheckpointSlotLease(own))
            {
                var store=new LeaseBoundOriginal596Store(fresh);
                Assert(store.Read(77,out long gen,out string got)&&gen==1&&got==h,
                    "fresh host reads the exact real original checkpoint after child exit");
                Assert(store.Commit(77,1,h,2,new string('d',64)),
                    "new legit owner can advance generation from fresh sealed state");
                originalCas++;
                Assert(!old.Commit(77,1,h,3,new string('e',64)),
                    "stale old host cannot overwrite after new owner");
                rejected++;
            }

            // Foreign/dirty physical state must remain byte-for-byte untouched.
            string foreign=Path.Combine(scratch,"foreign-directory");
            OwnedCheckpointSlotLease.CreateOwnedSlot(foreign);
            string foreignEmpty=Path.Combine(foreign,"UNOWNED_EMPTY");
            Directory.CreateDirectory(foreignEmpty);
            Reject(()=>{using var unused=new OwnedCheckpointSlotLease(foreign);},
                "foreign empty directory fails closed");
            Assert(Directory.Exists(foreignEmpty),"foreign EMPTY folder preserved");
            Directory.Delete(foreignEmpty);
            string marker=Path.Combine(foreign,"owner.marker");
            byte[] markerBytes=File.ReadAllBytes(marker);
            File.WriteAllText(marker,"foreign!");
            Reject(()=>{using var unused=new OwnedCheckpointSlotLease(foreign);},
                "modified marker does not claim foreign slot");
            Assert(File.ReadAllText(marker)=="foreign!","foreign modified marker preserved");
            File.WriteAllBytes(marker,markerBytes);
            using(var restored=new OwnedCheckpointSlotLease(foreign))
                Assert(restored.IsHeld,"repaired ONLY own fixture marker is openable");
            string alien=Path.Combine(foreign,"user-settings.txt");
            File.WriteAllText(alien,"User data");
            Reject(()=>{using var unused=new OwnedCheckpointSlotLease(foreign);},
                "foreign file no deletion");
            Assert(File.ReadAllText(alien)=="User data","foreign user bytes preserved");

            // Corrupt original selected checkpoint must fail closed, even with
            // a valid lease. No pretending it is an initialized/valid new slot.
            File.WriteAllText(Path.Combine(own,"checkpoint.state"),"BAD-ORIGINAL-CHECKPOINT");
            using(var corrupted=new OwnedCheckpointSlotLease(own))
            {
                var originalStore=new LeaseBoundOriginal596Store(corrupted);
                Assert(!originalStore.Read(77,out _,out _),
                    "real original #596 FileCheckpointStore corrupt disk refuses");
                rejected++;
            }
            Console.WriteLine("B0162_W18_ORIGINAL_596_OS_SLOT_LEASE_ASSERTIONS_PASS="+asserts);
            Console.WriteLine($"B0162_W18_CROSS_PROCESS={crossProcess} ACTUAL_ORIGINAL_596_CAS={originalCas} NEGATIVES={rejected}");
            Console.WriteLine("SELECTED_596_SOURCE_ONLY LIVE_UNITY_HOST_NOT_BOUND G1_NOT_RUN G2_HOLD");
        }
        finally
        {
            // Scratch is a newly generated UUID-owned directory created above.
            // Never invoke this against an existing Codex/player/save directory.
            Directory.Delete(scratch,recursive:true);
        }
    }
    private static int Main(string[] args)
    {
        try
        {
            if(args.Length==3&&args[0]=="--probe")return Child(args);
            if(args.Length>0)throw new ArgumentException("Unknown mode");
            Run();return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
