#nullable enable
using System;
using System.IO;
using System.Linq;
using B01.ExternalReceiptFence001;
using B01.DetachedSaveContract001;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
using B01.SingleFileSaveSlot001;

internal static class Program
{
    private static int assertions;
    private const long Run=62121;
    private static void Check(bool ok,string reason)
    {
        if(!ok)throw new InvalidOperationException("B0162_W21_WITNESS_FAIL "+reason);
        assertions++;
        Console.WriteLine("PASS "+reason);
    }

    private static PersistedPart Part(int i,bool detached)
    {
        var s=SecureRigCatalog.At(i);
        long owner=FrozenPartCatalog.Owner(i);
        return new PersistedPart(s.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(s.RootX+s.PartOffsetX,detached?2.2:1.2,0),
            0,0,0,1,new Vector3d(detached?0.25:0,0,0),
            new Vector3d(0,0,0),s.PartMassKg);
    }
    private static byte[] Original556(int gen,bool light,bool heavy,long run=Run)
        =>SnapshotCodec.Encode(new RunSnapshot(run,gen,
            new[]{Part(0,light),Part(1,heavy)}));

    private sealed class Hook:IReceiptPublisher
    {
        public bool ThrowBefore,ThrowAfter;
        public void Publish(string source,string dest)
        {
            if(ThrowBefore)throw new IOException("witness update interrupted BEFORE rename");
            new AtomicReceiptPublisher().Publish(source,dest);
            if(ThrowAfter)throw new IOException("lost witness ACK AFTER rename");
        }
    }

    private static (SingleFileSaveSlot slot,ExternalReceiptAuthority authority,
        WitnessBoundSave guard) New(string root,string name,Guid id,Hook? hook=null)
    {
        string sd=Path.Combine(root,name+"-save");
        string wd=Path.Combine(root,name+"-trusted");
        Check(SingleFileSaveSlot.TryCreateNew(sd,Run,id,out var slot)
              ==SlotStatus.Accepted&&slot!=null,name+":new owned Save root");
        Check(ExternalReceiptAuthority.CreateOwned(wd,Run,id,out var trust,hook)
              && trust!=null,name+":separate trust root created");
        return(slot!,trust!,new WitnessBoundSave(slot!,trust!));
    }

    private static (SingleFileSaveSlot slot,ExternalReceiptAuthority authority,
        WitnessBoundSave guard) Open(string root,string name,Guid id)
    {
        Check(SingleFileSaveSlot.TryOpenExisting(Path.Combine(root,name+"-save"),
            Run,id,out var slot)==SlotStatus.Accepted&&slot!=null,
            name+":cold original #669 physical slot opened");
        Check(ExternalReceiptAuthority.OpenOwned(Path.Combine(root,name+"-trusted"),
            Run,id,out var trust)&&trust!=null,
            name+":cold external authority opened");
        return(slot!,trust!,new WitnessBoundSave(slot!,trust!));
    }

    private static void RunTests()
    {
        string root=Path.Combine(Path.GetTempPath(),"b01-62-w21-external-receipt-"+
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Guid id=Guid.NewGuid();
            byte[] oldEnvelope;
            byte[] newEnvelope;
            {
                var(slot,trust,guard)=New(root,"rollback",id);
                using(slot)using(trust)
                {
                    Check(guard.Read(out long gen,out _)==FenceResult.Empty&&gen==0,
                        "initial trusted highwater g0 matches empty original #669");
                    Check(guard.Commit(0,Original556(1,true,false))==FenceResult.Accepted,
                        "original #556 Light g1 committed with external trusted receipt");
                    oldEnvelope=File.ReadAllBytes(Path.Combine(root,"rollback-save","save.envelope"));
                    Check(guard.Read(out gen,out byte[] light)==FenceResult.Accepted&&
                        gen==1&&ColdRestore.TryStage(light,out var a)&&
                        a!.ContainsConsumedId(0,FrozenPartCatalog.Id(0))&&
                        !a.ContainsConsumedId(1,FrozenPartCatalog.Id(1)),
                        "trust g1 contains original frozen W06 Light consumed ID only");
                    Check(guard.Commit(1,Original556(2,false,true))==FenceResult.Accepted,
                        "full original Heavy g2 Save and EXTERNAL highwater commit");
                    newEnvelope=File.ReadAllBytes(Path.Combine(root,"rollback-save","save.envelope"));
                    Check(guard.Read(out gen,out byte[] heavy)==FenceResult.Accepted&&
                        gen==2&&ColdRestore.TryStage(heavy,out var b)&&
                        !b!.ContainsConsumedId(0,FrozenPartCatalog.Id(0))&&
                        b.ContainsConsumedId(1,FrozenPartCatalog.Id(1)),
                        "g2 trusted highwater preserves Heavy not stale Light");
                    Check(guard.Commit(1,Original556(2,true,false))==FenceResult.Stale,
                        "old g1 writer rejected by receipt CAS");
                }
            }
            // The valid original g1 envelope has its OWN correct digest.
            // Restore its raw bytes while independent trust witness retains g2.
            {
                string target=Path.Combine(root,"rollback-save","save.envelope");
                File.WriteAllBytes(target,oldEnvelope);
                var(slot,trust,guard)=Open(root,"rollback",id);
                using(slot)using(trust)
                {
                    Check(guard.Read(out _,out _)==FenceResult.Stale,
                        "EXTERNAL VALID BACKUP g1 rejected across cold restart (#680)");
                    Check(guard.Quarantined,"valid g1 external rollback seals cold owner");
                    Check(guard.Commit(1,Original556(2,false,true))
                        ==FenceResult.Quarantined,
                        "old valid Light cannot mint another g2 after rollback");
                }
                File.WriteAllBytes(target,newEnvelope); // deliberate test-owned recovery
                var(slot2,trust2,guard2)=Open(root,"rollback",id);
                using(slot2)using(trust2)
                {
                    Check(guard2.Read(out long g,out _)==FenceResult.Accepted&&g==2,
                        "same actual trusted receipt permits correct original Heavy g2");
                }
            }
            // Construct a second independent legitimate same-ID envelope
            // at SAME generation but with different original consumed ledger.
            {
                var(s,t,g)=New(root,"alternate",id);
                using(s)using(t)
                {
                    Check(g.Commit(0,Original556(1,true,false))==FenceResult.Accepted,
                        "alternate fixture original g1");
                    Check(g.Commit(1,Original556(2,true,false))==FenceResult.Accepted,
                        "alternate g2 valid but DIFFERENT consumed Light ledger");
                }
                string alt=Path.Combine(root,"alternate-save","save.envelope");
                string dest=Path.Combine(root,"rollback-save","save.envelope");
                File.Copy(alt,dest,true);
                var(s,t,g)=Open(root,"rollback",id);
                using(s)using(t)
                {
                    Check(g.Read(out _,out _)==FenceResult.Fork,
                        "EXTERNAL SAME-GENERATION valid #556 LEDGER FORK denied (#680)");
                    Check(g.Commit(2,Original556(3,true,true))
                        ==FenceResult.Quarantined,
                        "forked old-world g2 cannot silently write later epoch");
                }
                File.WriteAllBytes(dest,newEnvelope);
            }
            // Fault occurs AFTER original single-envelope persisted but BEFORE
            // independent trust witness advances. Neither old nor new state
            // can be auto-adopted; old physical payload is not destroyed.
            {
                var hook=new Hook();
                Guid crashId=Guid.NewGuid();
                var(s,t,g)=New(root,"gap",crashId,hook);
                using(s)using(t)
                {
                    Check(g.Commit(0,Original556(1,true,false))==FenceResult.Accepted,
                        "gap setup g1");
                    hook.ThrowBefore=true;
                    Check(g.Commit(1,Original556(2,false,true))
                        ==FenceResult.Quarantined&&g.Quarantined,
                        "Save g2 real rename then trusted receipt UPDATE failure quarantines");
                    Check(g.Read(out _,out _)==FenceResult.Quarantined,
                        "unsafe active owner refuses stale read after split publication");
                }
                var(s2,t2,g2)=Open(root,"gap",crashId);
                using(s2)using(t2)
                {
                    Check(g2.Read(out _,out _)==FenceResult.Quarantined,
                        "cold restart detects Save AHEAD of witness, no false success");
                    Check(g2.Commit(1,Original556(2,true,false))
                        ==FenceResult.Quarantined,
                        "no unauthorized automatic downgrade on uncertain cross-file state");
                    Check(File.Exists(Path.Combine(root,"gap-save","save.envelope")),
                        "forensic original Save payload remains preserved on gap");
                }
            }
            // WITNESS RECORD CAN BE REPLACED TOO if attacker has access to
            // both physical directories. Explicit test proves OUT-OF-SCOPE.
            {
                Guid bothId=Guid.NewGuid();
                var(s,t,g)=New(root,"both-rollback",bothId);
                byte[] save1,proof1;
                using(s)using(t)
                {
                    Check(g.Commit(0,Original556(1,true,false))==FenceResult.Accepted,
                        "two-root threat setup g1");
                    save1=File.ReadAllBytes(Path.Combine(root,"both-rollback-save","save.envelope"));
                    proof1=File.ReadAllBytes(Path.Combine(root,"both-rollback-trusted","witness.record"));
                    Check(g.Commit(1,Original556(2,false,true))==FenceResult.Accepted,
                        "two-root threat setup g2");
                }
                File.WriteAllBytes(Path.Combine(root,"both-rollback-save","save.envelope"),save1);
                File.WriteAllBytes(Path.Combine(root,"both-rollback-trusted","witness.record"),proof1);
                var(s2,t2,g2)=Open(root,"both-rollback",bothId);
                using(s2)using(t2)
                {
                    Check(g2.Read(out long gen,out _)==FenceResult.Accepted&&gen==1,
                        "EXPLICIT OUT OF SCOPE: attacker replacing BOTH roots defeats trust");
                }
            }
            {
                string empty=Path.Combine(root,"foreign-empty");
                Directory.CreateDirectory(empty);
                Check(!ExternalReceiptAuthority.CreateOwned(empty,Run,Guid.NewGuid(),out _),
                    "refuse foreign EMPTY trusted root without deleting it");
                Check(Directory.Exists(empty)&&
                      !Directory.EnumerateFileSystemEntries(empty).Any(),
                    "foreign EMPTY trusted folder is preserved");
                Check(!ExternalReceiptAuthority.OpenOwned(Path.Combine(root,"rollback-trusted"),
                    Run+1,id,out _),"cold wrong RunId cannot claim trusted record");
                Check(!ExternalReceiptAuthority.OpenOwned(Path.Combine(root,"rollback-trusted"),
                    Run,Guid.NewGuid(),out _),"cold wrong slot Guid cannot claim trusted record");
            }
            Console.WriteLine("B0162_W21_EXTERNAL_RECEIPT_SOURCE_ASSERTIONS_PASS="+assertions);
            Console.WriteLine("B0162_W21_VALID_BACKUP_OLDER_GENERATION=REJECTED_WITH_TRUSTED_WITNESS");
            Console.WriteLine("B0162_W21_VALID_SAME_GENERATION_DIFFERENT_LEDGER=REJECTED_WITH_TRUSTED_WITNESS");
            Console.WriteLine("B0162_W21_2FILE_PARTIAL_COMMIT=QUARANTINE_NOT_AUTO_RECOVERED");
            Console.WriteLine("B0162_W21_BOTH_ROOT_ROLLBACK=OUT_OF_SCOPE_PROVEN");
            Console.WriteLine("B0162_W21_HARDWARE_POWERLOSS_AND_UNTRUSTED_WITNESS=NOT_PROVEN");
            Console.WriteLine("B0162_W21_NATIVE_UNITY_G1=NOT_RUN LIVE_510=HOLD G2=HOLD");
        }
        finally {Directory.Delete(root,true);} // solely this UUID-created scratch
    }
    public static int Main()
    {
        try {RunTests();return 0;}
        catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
    }
}
