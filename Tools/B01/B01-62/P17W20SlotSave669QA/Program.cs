// Independent B01-62 QA of ACTUAL owner #669 SLOTSAVE-001.
// Never substitute, port or modify the original producer source.
// This is a security witness under a local disk rollback/tamper threat model:
// SHA256 is corruption detection, not an authenticated nonrollback authority.
using System;
using System.IO;
using System.Linq;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.SingleFileSaveSlot001;

internal static class Program
{
    private const long Run=66920;
    private static int pass,fail;
    private static void Check(string name,Func<bool> attempt)
    {
        try{if(attempt()){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e.GetType().Name+" "+e.Message);}
    }
    private static bool Temp(Func<string,bool> test)
    {
        string root=Path.Combine(Path.GetTempPath(),"B0162-W20-INDEPENDENT-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{return test(root);}
        finally{Directory.Delete(root,true);} // strictly this test-created UUID-only path
    }
    private static byte[] Wire(long generation,bool light,bool heavy,long run=Run)
    {
        PersistedPart[] p=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            var s=SecureRigCatalog.At(i);
            bool det=i==0?light:heavy;
            var owner=FrozenPartCatalog.Owner(i);
            p[i]=new PersistedPart(s.PartId,owner,det?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                det?PartDockState.Detached:PartDockState.Attached,
                det,!det,
                new Vector3d(s.RootX+s.PartOffsetX,det?2.2:1.2,0),
                0,0,0,1,det?new Vector3d(.25,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,p));
    }
    private static bool State(byte[] raw,long g,bool light,bool heavy)
    {
        return ColdRestore.TryStage(raw,out var cold)&&cold!=null&&
            cold.Snapshot.Generation==g&&cold.Snapshot.RunId==Run&&
            cold.ContainsConsumedId(0,FrozenPartCatalog.Id(0))==light&&
            cold.ContainsConsumedId(1,FrozenPartCatalog.Id(1))==heavy;
    }
    private static bool OriginalColdRollbackWitness()
    {
        return Temp(root=>{
            var id=Guid.Parse("88441133-6666-4999-8111-010101010101");
            string dir=Path.Combine(root,"owned");
            if(SingleFileSaveSlot.TryCreateNew(dir,Run,id,out var owner)!=SlotStatus.Accepted||owner==null)return false;
            byte[] older;
            using(owner)
            {
                if(owner.TryCommit(0,Wire(1,true,false))!=SlotStatus.Accepted)return false;
                older=File.ReadAllBytes(Path.Combine(dir,"save.envelope"));
                if(owner.TryCommit(1,Wire(2,false,true))!=SlotStatus.Accepted)return false;
            }
            // An EXTERNAL old-backup restore/replacement of one VALID whole
            // envelope is indistinguishable from an intact historical checkpoint
            // when no trusted monotonic anchor exists outside this envelope.
            File.WriteAllBytes(Path.Combine(dir,"save.envelope"),older);
            if(SingleFileSaveSlot.TryOpenExisting(dir,Run,id,out var cold)!=SlotStatus.Accepted||cold==null)return false;
            using(cold)
            {
                if(cold.TryRead(out long epoch,out byte[] payload)!=SlotStatus.Accepted||
                   epoch!=1||!State(payload,1,true,false))return false;
                if(cold.TryCommit(1,Wire(2,true,true))!=SlotStatus.Accepted)return false;
                return cold.TryRead(out epoch,out payload)==SlotStatus.Accepted&&
                    State(payload,2,true,true);
            }
        });
    }
    private static bool OriginalSameEpochForkWitness()
    {
        return Temp(root=>{
            var id=Guid.Parse("88441133-6666-4999-8111-010101010102");
            string live=Path.Combine(root,"live");
            string branch=Path.Combine(root,"foreign-branch");
            if(SingleFileSaveSlot.TryCreateNew(live,Run,id,out var a)!=SlotStatus.Accepted||a==null)return false;
            if(SingleFileSaveSlot.TryCreateNew(branch,Run,id,out var b)!=SlotStatus.Accepted||b==null)
            {a.Dispose();return false;}
            using(a)
            using(b)
            {
                if(a.TryCommit(0,Wire(1,true,false))!=SlotStatus.Accepted||
                   b.TryCommit(0,Wire(1,false,true))!=SlotStatus.Accepted)return false;
                // External actor swaps a VALID alternative #556 payload with
                // SAME run, same slot, same generation, different consumed IDs.
                byte[] foreign=File.ReadAllBytes(Path.Combine(branch,"save.envelope"));
                File.WriteAllBytes(Path.Combine(live,"save.envelope"),foreign);
                if(a.TryRead(out long g,out byte[] now)!=SlotStatus.Accepted||
                   g!=1||!State(now,1,false,true))return false;
                // Original API requests ONLY generation; no expected record
                // fingerprint nor previously captured Light-ledger identity.
                if(a.TryCommit(1,Wire(2,true,true))!=SlotStatus.Accepted)return false;
                return a.TryRead(out g,out now)==SlotStatus.Accepted&&
                    State(now,2,true,true);
            }
        });
    }
    private static bool NoReplacementWithoutFile()
    {
        return Temp(root=>{
            var id=Guid.NewGuid();
            string dir=Path.Combine(root,"clean");
            if(SingleFileSaveSlot.TryCreateNew(dir,Run,id,out var owner)!=SlotStatus.Accepted||owner==null)return false;
            using(owner)
            {
                if(owner.TryCommit(0,Wire(1,true,false))!=SlotStatus.Accepted)return false;
                if(owner.TryCommit(0,Wire(1,false,true))!=SlotStatus.Stale)return false;
                return owner.TryRead(out long g,out byte[] raw)==SlotStatus.Accepted&&
                    g==1&&State(raw,1,true,false);
            }
        });
    }
    private static bool CorruptedForeignDenial()
    {
        return Temp(root=>{
            var id=Guid.NewGuid();
            string dir=Path.Combine(root,"dirty");
            if(SingleFileSaveSlot.TryCreateNew(dir,Run,id,out var owner)!=SlotStatus.Accepted||owner==null)return false;
            using(owner)
            {
                if(owner.TryCommit(0,Wire(1,true,false))!=SlotStatus.Accepted)return false;
                string file=Path.Combine(dir,"save.envelope");
                byte[] body=File.ReadAllBytes(file);body[body.Length-1]^=0x80;
                File.WriteAllBytes(file,body);
                return owner.TryRead(out _,out _)==SlotStatus.Corrupt&&
                    owner.TryCommit(1,Wire(2,false,true))==SlotStatus.Corrupt&&
                    File.ReadAllBytes(file).SequenceEqual(body);
            }
        });
    }
    private static bool ForeignRun()
    {
        return Temp(root=>{
            string dir=Path.Combine(root,"run");
            var id=Guid.NewGuid();
            if(SingleFileSaveSlot.TryCreateNew(dir,Run,id,out var owner)!=SlotStatus.Accepted||owner==null)return false;
            using(owner)
            {
                if(owner.TryCommit(0,Wire(1,true,false,Run+1))!=SlotStatus.Invalid)return false;
                if(owner.TryRead(out _,out _)!=SlotStatus.Empty)return false;
            }
            return SingleFileSaveSlot.TryOpenExisting(dir,Run+1,id,out _)==SlotStatus.WrongOwner;
        });
    }
    private static bool DirtyEmptyRoot()
    {
        return Temp(root=>{
            string dir=Path.Combine(root,"foreignEmpty");
            Directory.CreateDirectory(dir);
            bool denied=SingleFileSaveSlot.TryCreateNew(dir,Run,Guid.NewGuid(),out _)
                ==SlotStatus.WrongOwner;
            return denied&&Directory.Exists(dir)&&!Directory.EnumerateFileSystemEntries(dir).Any();
        });
    }
    private static bool RecordCannotServeOtherSlot()
    {
        return Temp(root=>{
            string live=Path.Combine(root,"live"),alternative=Path.Combine(root,"alt");
            var a=Guid.NewGuid();var b=Guid.NewGuid();
            if(SingleFileSaveSlot.TryCreateNew(live,Run,a,out var x)!=SlotStatus.Accepted||x==null)return false;
            if(SingleFileSaveSlot.TryCreateNew(alternative,Run,b,out var y)!=SlotStatus.Accepted||y==null)
            {x.Dispose();return false;}
            using(x)
            using(y)
            {
                if(x.TryCommit(0,Wire(1,true,false))!=SlotStatus.Accepted)return false;
                if(y.TryCommit(0,Wire(1,false,true))!=SlotStatus.Accepted)return false;
                File.WriteAllBytes(Path.Combine(alternative,"save.envelope"),
                    File.ReadAllBytes(Path.Combine(live,"save.envelope")));
                return y.TryRead(out _,out _)==SlotStatus.WrongOwner&&
                    y.TryCommit(1,Wire(2,true,true))==SlotStatus.WrongOwner;
            }
        });
    }
    public static int Main()
    {
        Check("01 untouched frozen #510 source SHA pinned by original #556 codec",()=>FrozenPartCatalog.SelectedSourceSha==
            "d94400d95d13db7f27e1533df05c9292d5380431");
        Check("02 actual #556 binary stage Light stable consumed ID",()=>State(Wire(1,true,false),1,true,false));
        Check("03 actual #556 binary stage Heavy stable consumed ID",()=>State(Wire(1,false,true),1,false,true));
        Check("04 genuine ORIGINAL #669 stale same-slot generation conflict denied",NoReplacementWithoutFile);
        Check("05 ORIGINAL #669 corrupted authority refuses and preserves bytes",CorruptedForeignDenial);
        Check("06 ORIGINAL #669 foreign run cannot enter existing slot",ForeignRun);
        Check("07 ORIGINAL #669 foreign EMPTY directory preserved",DirtyEmptyRoot);
        Check("08 ORIGINAL #669 foreign slot GUID valid record rejected",RecordCannotServeOtherSlot);
        Check("09 SOURCE TRUST RED witness: external whole-envelope rollback is accepted after cold restart",OriginalColdRollbackWitness);
        Check("10 SOURCE TRUST RED witness: identical run+slot+epoch fork can replace original ledger",OriginalSameEpochForkWitness);
        Console.WriteLine("B0162_W20_669_INDEPENDENT_QA_TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W20_669_ORIGINAL_CORRUPTION_AND_FOREIGN_ID_GREEN");
        Console.WriteLine("B0162_W20_669_VALID_OLD_ENVELOPE_EXTERNAL_ROLLBACK_RED_REPRODUCED");
        Console.WriteLine("B0162_W20_669_VALID_SAME_GENERATION_EXTERNAL_FORK_RED_REPRODUCED");
        Console.WriteLine("B0162_W20_669_THREAT_MODEL=EXTERNAL_AUTHORITATIVE_FILE_REPLACEMENT_NOT_NORMAL_API_WRITER");
        Console.WriteLine("B0162_W20_669_NATIVE_UNITY_NOT_RUN_PRIVATE_510_NOT_INTEGRATED");
        return fail==0?0:1;
    }
}
