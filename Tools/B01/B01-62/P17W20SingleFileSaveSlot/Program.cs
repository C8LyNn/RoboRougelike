#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.SingleFileSaveSlot001;

internal static class Program
{
    private static int pass;
    private const long Run=62020;
    static void Check(bool yes,string name)
    {
        if(!yes)throw new Exception("B0162_W20_FAIL: "+name);
        pass++;
        Console.WriteLine("PASS "+name);
    }
    static PersistedPart Part(int slot,bool detached)
    {
        var p=SecureRigCatalog.At(slot);
        long owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(p.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(p.RootX+p.PartOffsetX,detached?2.2:1.2,0),
            0,0,0,1,
            detached?new Vector3d(.25,0,0):new Vector3d(0,0,0),
            new Vector3d(0,0,0),p.PartMassKg);
    }
    static byte[] Original556(long gen,bool light,bool heavy,long run=Run) =>
        SnapshotCodec.Encode(new RunSnapshot(run,gen,
            new[]{Part(0,light),Part(1,heavy)}));

    sealed class FaultyPublisher : IAtomicEnvelopePublisher
    {
        public bool ThrowBefore,ThrowAfter;
        public void Publish(string stage,string final)
        {
            if(ThrowBefore)throw new IOException("injected power cut before atomic rename");
            new OsAtomicEnvelopePublisher().Publish(stage,final);
            if(ThrowAfter)throw new IOException("lost ACK after real durable rename");
        }
    }

    static bool CheckCold(byte[] raw,long gen,bool light,bool heavy)
    {
        if(!ColdRestore.TryStage(raw,out ColdRestore? cold)||
            cold==null||cold.Snapshot.Generation!=gen||
            cold.Snapshot.RunId!=Run)return false;
        return cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light &&
            cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy &&
            cold.Snapshot.Parts[0].Id==FrozenPartCatalog.Id(0) &&
            cold.Snapshot.Parts[1].Id==FrozenPartCatalog.Id(1);
    }

    // Actually launches another CLR process to contest the OS FileShare.None lease.
    static int Child(string root,Guid slot)
    {
        var psi=new ProcessStartInfo("dotnet")
        {
            UseShellExecute=false,
            RedirectStandardOutput=true,
            RedirectStandardError=true
        };
        psi.ArgumentList.Add(typeof(Program).Assembly.Location);
        psi.ArgumentList.Add("--probe");
        psi.ArgumentList.Add(root);
        psi.ArgumentList.Add(Run.ToString(System.Globalization.CultureInfo.InvariantCulture));
        psi.ArgumentList.Add(slot.ToString("D"));
        using var p=Process.Start(psi)??throw new Exception("child did not start");
        if(!p.WaitForExit(20000))
        {
            try{p.Kill(true);}catch{}
            throw new Exception("child process timed out");
        }
        string stdout=p.StandardOutput.ReadToEnd();
        string stderr=p.StandardError.ReadToEnd();
        if(p.ExitCode!=0&&p.ExitCode!=3)
            throw new Exception("child failed "+p.ExitCode+" "+stdout+" "+stderr);
        return p.ExitCode;
    }

    static void Test()
    {
        string root=Path.Combine(Path.GetTempPath(),"b0162-w20-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Guid slot=Guid.NewGuid();
            string dir=Path.Combine(root,"current-slot");
            var created=SingleFileSaveSlot.TryCreateNew(dir,Run,slot,out var owner);
            Check(created==SlotStatus.Accepted&&owner!=null,"create new owned namespace");
            using(owner!)
            {
                Check(owner.TryRead(out long emptyGen,out _)==SlotStatus.Empty&&
                    emptyGen==0,"new slot has no canonical Save");
                Check(owner.TryCommit(0,Original556(1,true,false))==SlotStatus.Accepted,
                    "write full original #556 Light snapshot+revision as ONE OS file");
                Check(owner.TryRead(out long epoch,out byte[] payload)==SlotStatus.Accepted&&
                    epoch==1&&CheckCold(payload,1,true,false),
                    "read original frozen #556 Light consumed ID and generation together");
                Check(Child(dir,slot)==3,"another CLR process cannot own active checkpoint slot");
                Check(SingleFileSaveSlot.TryOpenExisting(dir,Run,slot,out _)==SlotStatus.Busy,
                    "same process double owner denied");
                Check(owner.TryCommit(0,Original556(1,false,true))==SlotStatus.Stale,
                    "no stale generation fork/overwrite");
                Check(owner.TryCommit(1,Original556(2,false,true,Run+1))==SlotStatus.Invalid,
                    "foreign run ID rejected before writing anything");
                Check(owner.TryCommit(1,Original556(3,false,true))==SlotStatus.Invalid,
                    "nonconsecutive generation cannot skip epoch");
                Check(owner.TryCommit(1,Original556(2,false,true))==SlotStatus.Accepted,
                    "atomic replacement with authentic Heavy consumed snapshot");
                Check(owner.TryRead(out epoch,out payload)==SlotStatus.Accepted&&
                    epoch==2&&CheckCold(payload,2,false,true),
                    "original W06 Heavy ID moved, Light ID not resurrected");
            }
            Check(Child(dir,slot)==0,"separate CLR process reopens SAME slot after Dispose");
            Check(SingleFileSaveSlot.TryOpenExisting(dir,Run,slot,out var cold)==SlotStatus.Accepted&&cold!=null,
                "cold restart binds original run+GUID persisted owner identity");
            using(cold!)
            {
                Check(cold.TryRead(out long e,out byte[] p)==SlotStatus.Accepted&&
                    e==2&&CheckCold(p,2,false,true),
                    "cold process reads durable version2 Heavy ledger, no sidecar");
                Check(cold.TryCommit(2,Original556(3,true,true))==SlotStatus.Accepted,
                    "new owner accepts monotonic generation3 after restart");
                Check(cold.TryRead(out e,out p)==SlotStatus.Accepted&&
                    CheckCold(p,3,true,true),"cold #556 both IDs coherent");
            }
            // ACK after real OS rename is lost. Old same owner MUST quarantine,
            // next freshly leased owner must recover authoritative latest bytes.
            {
                string crash=Path.Combine(root,"after-rename");
                Guid id=Guid.NewGuid();
                var hook=new FaultyPublisher();
                Check(SingleFileSaveSlot.TryCreateNew(crash,Run,id,out var session,hook)
                    ==SlotStatus.Accepted&&session!=null,
                    "create separate fault-injection save slot");
                using(session!)
                {
                    Check(session.TryCommit(0,Original556(1,true,false))
                        ==SlotStatus.Accepted,"initial persistent g1 for lost ACK scenario");
                    hook.ThrowAfter=true;
                    Check(session.TryCommit(1,Original556(2,false,true))
                        ==SlotStatus.Quarantined&&session.Quarantined,
                        "renamed full g2 then ACK lost => sticky quarantine");
                    Check(session.TryRead(out _,out _)==SlotStatus.Quarantined,
                        "uncertain owner cannot read+reissue stale save");
                    Check(session.TryCommit(2,Original556(3,true,false))
                        ==SlotStatus.Quarantined,
                        "uncertain owner cannot overwrite durable g2 with stale g3");
                }
                Check(SingleFileSaveSlot.TryOpenExisting(crash,Run,id,out var recovered)
                    ==SlotStatus.Accepted&&recovered!=null,
                    "fresh exclusive owner acquires after ambiguous commit crash");
                using(recovered!)
                {
                    Check(recovered.TryRead(out long g,out byte[] p)
                        ==SlotStatus.Accepted&&g==2&&CheckCold(p,2,false,true),
                        "real atomic file g2 Heavy recovered, not stale g1 Light");
                    Check(recovered.TryCommit(2,Original556(3,true,true))
                        ==SlotStatus.Accepted,
                        "new owner resolves ambiguous ACK from authoritative file only");
                }
            }
            // Before-rename fault leaves a staged .stage file; NEVER promote it.
            {
                string crash=Path.Combine(root,"before-rename");
                Guid id=Guid.NewGuid();
                var hook=new FaultyPublisher();
                Check(SingleFileSaveSlot.TryCreateNew(crash,Run,id,out var s,hook)
                    ==SlotStatus.Accepted&&s!=null,"create abort-before-rename slot");
                using(s!)
                {
                    Check(s.TryCommit(0,Original556(1,true,false))
                        ==SlotStatus.Accepted,"commit initial g1 before interrupted stage");
                    hook.ThrowBefore=true;
                    Check(s.TryCommit(1,Original556(2,false,true))
                        ==SlotStatus.Quarantined&&s.Quarantined,
                        "pre-rename callback error conservatively quarantines");
                }
                Check(Directory.EnumerateFiles(crash,"*.stage").Any(),
                    "incomplete own staged write physically exists, left untouched");
                Check(SingleFileSaveSlot.TryOpenExisting(crash,Run,id,out var recovered)
                    ==SlotStatus.Accepted&&recovered!=null,
                    "new owner ignores orphan, holds fresh exclusive lease");
                using(recovered!)
                {
                    Check(recovered.TryRead(out long g,out byte[] p)
                        ==SlotStatus.Accepted&&g==1&&CheckCold(p,1,true,false),
                        "interrupted pending g2 NEVER considered committed");
                    Check(recovered.TryCommit(1,Original556(2,false,true))
                        ==SlotStatus.Accepted,
                        "recover can monotonically commit correct version after orphan");
                }
            }
            // Intentionally corrupted authoritative file must FAIL CLOSED, no
            // stale backup/previous epoch fallback and no destructive cleanup.
            {
                string corrupt=Path.Combine(root,"corrupt");
                Guid id=Guid.NewGuid();
                Check(SingleFileSaveSlot.TryCreateNew(corrupt,Run,id,out var s)
                    ==SlotStatus.Accepted&&s!=null,"create corrupt-negative slot");
                using(s!)
                    Check(s.TryCommit(0,Original556(1,true,false))
                        ==SlotStatus.Accepted,"write original #556 for tamper negative");
                string path=Path.Combine(corrupt,"save.envelope");
                byte[] raw=File.ReadAllBytes(path);raw[22]^=0x30;
                File.WriteAllBytes(path,raw);
                Check(SingleFileSaveSlot.TryOpenExisting(corrupt,Run,id,out var reopened)
                    ==SlotStatus.Accepted&&reopened!=null,
                    "lease can be acquired after malicious checksum tamper");
                using(reopened!)
                {
                    Check(reopened.TryRead(out _,out _)==SlotStatus.Corrupt,
                        "corrupt canonical envelope fails closed without rollback");
                    Check(reopened.TryCommit(0,Original556(1,false,true))
                        ==SlotStatus.Corrupt,
                        "corrupt canonical file cannot be silently overwritten");
                }
                Check(File.ReadAllBytes(path).SequenceEqual(raw),
                    "original corrupt evidence byte-preserved for forensic receiver");
            }
            {
                string foreign=Path.Combine(root,"foreign-empty");
                Directory.CreateDirectory(foreign);
                Check(SingleFileSaveSlot.TryCreateNew(foreign,Run,Guid.NewGuid(),out _)
                    ==SlotStatus.WrongOwner,"foreign EMPTY folder preserved");
                Check(Directory.Exists(foreign)&&!Directory.EnumerateFiles(foreign).Any(),
                    "foreign EMPTY folder never deleted or modified");
                Check(SingleFileSaveSlot.TryOpenExisting(dir,Run+1,slot,out _)
                    ==SlotStatus.WrongOwner,"foreign run cannot reopen another owner's slot");
                Check(SingleFileSaveSlot.TryOpenExisting(dir,Run,Guid.NewGuid(),out _)
                    ==SlotStatus.WrongOwner,"wrong original slot GUID cannot own durable Save");
            }
            Console.WriteLine("B0162_W20_ORIGINAL_556_SINGLEFILE_SOURCE_ASSERTIONS_PASS="+pass);
            Console.WriteLine("B0162_W20_ORIGINAL_GIT_BINARY=UNCHANGED SINGLE_ATOMIC_FILE=TRUE");
            Console.WriteLine("B0162_W20_POSTRENAME_ACK_LOSS_QUARANTINED=TRUE");
            Console.WriteLine("B0162_W20_PRERENAME_ORPHAN_NEVER_PROMOTED=TRUE");
            Console.WriteLine("B0162_W20_SEPARATE_CLR_LEASE_DENY_AND_TAKEOVER=TRUE");
            Console.WriteLine("B0162_W20_HARDWARE_POWERLOSS_DIR_FSYNC=NOT_PROVEN");
            Console.WriteLine("B0162_W20_UNITY_G1_NOT_RUN CANONICAL_510_BINDING_NOT_IMPLEMENTED G2_HOLD");
        }
        finally {Directory.Delete(root,true);} // This process created this UUID scratch ONLY.
    }

    public static int Main(string[] args)
    {
        if(args.Length==4&&args[0]=="--probe")
        {
            if(!long.TryParse(args[2],out long run)||
                !Guid.TryParse(args[3],out Guid slot))return 4;
            SlotStatus st=SingleFileSaveSlot.TryOpenExisting(args[1],run,slot,out var lease);
            if(st==SlotStatus.Busy)return 3;
            if(st!=SlotStatus.Accepted||lease==null)return 4;
            using(lease){return 0;}
        }
        try {Test();return 0;}
        catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
    }
}
