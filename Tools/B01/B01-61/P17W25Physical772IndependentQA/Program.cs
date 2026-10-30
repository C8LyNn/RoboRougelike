#nullable enable
using System;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.CanonicalRigPortW24;

internal static class Independent772
{
    private static int pass,fail;
    private static readonly Vector3d Zero=new Vector3d(0,0,0);
    private static void Check(string label,Func<bool> test)
    {
        try{if(test()){pass++;Console.WriteLine("PASS "+label);}
            else{fail++;Console.WriteLine("FAIL "+label);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+label+" "+e);}
    }
    private static PersistedPart BuildPart(int slot,bool detached,int whichRun=81)
    {
        var s=SecureRigCatalog.At(slot);
        long owner=FrozenPartCatalog.Owner(slot);
        return new PersistedPart(s.PartId,owner,detached?0:owner,
            FrozenPartCatalog.Asset(slot),FrozenPartCatalog.Socket(slot),
            detached?PartDockState.Detached:PartDockState.Attached,
            detached,!detached,
            new Vector3d(s.RootX+s.PartOffsetX,detached?2.2:1.2,0),
            0,0,0,1,detached?new Vector3d(.25,0,0):Zero,Zero,s.PartMassKg);
    }
    private static byte[] Snapshot(int mask,long generation=1,long run=81)
    {
        var p=new[]{BuildPart(0,(mask&1)!=0),BuildPart(1,(mask&2)!=0)};
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,p));
    }
    private static bool Stage(int mask,out ColdRestore? cold,long generation=1)
        =>ColdRestore.TryStage(Snapshot(mask,generation),out cold)&&cold!=null;
    private static bool Mask(int mask)
    {
        if(!Stage(mask,out var cold)||cold==null)return false;
        for(int i=0;i<2;i++)
        {
            bool det=(mask&(1<<i))!=0;
            PersistedPart p=cold.Snapshot.Parts[i];
            if(!CanonicalRigLedgerHydratorW24.TryHydrate(p,i,cold,
                 out AtomicDockLedger ledger)||
               p.Id!=SecureRigCatalog.At(i).PartId||
               ledger.Contains(p.Id)!=det||
               ledger.Count!=(det?1:0)||ledger.Busy||
               cold.ContainsConsumedId(i,p.Id)!=det)
                return false;
        }
        return true;
    }
    private static bool IdentityMismatch()
    {
        if(!Stage(1,out var cold)||cold==null)return false;
        var p=cold.Snapshot.Parts[0];
        return !CanonicalRigLedgerHydratorW24.TryHydrate(p,1,cold,
             out var wrong)&&wrong.Count==0;
    }
    private static PersistedPart Copy(PersistedPart p,long owner,bool consumed)
        =>new PersistedPart(p.Id,owner,consumed?0:owner,p.AssetGuid,p.SocketGuid,
            consumed?PartDockState.Detached:PartDockState.Attached,consumed,!consumed,
            p.Position,p.Qx,p.Qy,p.Qz,p.Qw,p.LinearVelocity,p.AngularVelocity,p.MassKg);
    private static bool ForgedOwner()
    {
        if(!Stage(1,out var cold)||cold==null)return false;
        PersistedPart p=cold.Snapshot.Parts[0];
        var forged=Copy(p,p.OwnerId+200,p.Consumed);
        return !CanonicalRigLedgerHydratorW24.TryHydrate(forged,0,cold,
            out var ledger)&&ledger.Count==0;
    }
    private static bool ContradictoryConsumed()
    {
        if(!Stage(1,out var cold)||cold==null)return false;
        PersistedPart p=cold.Snapshot.Parts[0];
        var forged=Copy(p,p.OwnerId,false);
        return !CanonicalRigLedgerHydratorW24.TryHydrate(forged,0,cold,
            out var ledger)&&ledger.Count==0;
    }
    private static bool ReattachThrowRollsBack()
    {
        if(!Stage(1,out var cold)||cold==null)return false;
        PersistedPart p=cold.Snapshot.Parts[0];
        if(!CanonicalRigLedgerHydratorW24.TryHydrate(p,0,cold,
             out var ledger))return false;
        bool ok=ledger.TryReattach(p.Id,
            ()=>throw new InvalidOperationException("original physical reattach callback failed"),
            ()=>{});
        return !ok&&ledger.Contains(p.Id)&&ledger.Count==1&&!ledger.Busy;
    }
    private static bool SuccessfulReattachThenNoDouble()
    {
        if(!Stage(2,out var cold)||cold==null)return false;
        PersistedPart p=cold.Snapshot.Parts[1];
        if(!CanonicalRigLedgerHydratorW24.TryHydrate(p,1,cold,
            out var ledger))return false;
        return ledger.TryReattach(p.Id,()=>{},()=>{})&&
            !ledger.Contains(p.Id)&&ledger.Count==0&&
            !ledger.TryReattach(p.Id,()=>{},()=>{})&&ledger.Count==0;
    }
    private static bool DuplicateDetachedIdNotConsumedTwice()
    {
        var l=new AtomicDockLedger();
        long id=FrozenPartCatalog.Id(0);
        if(!l.TryDetach(id,Zero,Zero,Zero,FrozenPartCatalog.Mass(0),0,0,
            out var _))return false;
        return !l.TryDetach(id,Zero,Zero,Zero,FrozenPartCatalog.Mass(0),0,0,
            out var _)&&l.Contains(id)&&l.Count==1&&!l.Busy;
    }
    private static bool OriginalCorruptPayloadDenied()
    {
        byte[] data=Snapshot(3,2);
        data[data.Length-2]^=1;
        return !ColdRestore.TryStage(data,out ColdRestore? cold)&&cold==null;
    }
    private static bool StableIdsAcrossGeneration()
    {
        if(!Stage(1,out var before,1)||before==null||
           !Stage(2,out var after,2)||after==null)return false;
        return before.Snapshot.Parts[0].Id==after.Snapshot.Parts[0].Id&&
            before.Snapshot.Parts[1].Id==after.Snapshot.Parts[1].Id&&
            before.ContainsConsumedId(0,FrozenPartCatalog.Id(0))&&
            after.ContainsConsumedId(1,FrozenPartCatalog.Id(1));
    }
    private static bool ByteExactSnapshotRoundtrip()
    {
        byte[] wire=Snapshot(3,17);
        if(!SnapshotCodec.TryDecode(wire,out RunSnapshot? s)||s==null||
           !ColdRestore.TryStage(wire,out ColdRestore? plan)||plan==null||
           !CanonicalRigLedgerHydratorW24.TryHydrate(s.Parts[0],0,plan,out var l)||
           !CanonicalRigLedgerHydratorW24.TryHydrate(s.Parts[1],1,plan,out var h))
            return false;
        return s.Generation==17&&l.Count==1&&h.Count==1&&
               s.Parts[0].Position.Y==2.2&&s.Parts[1].Position.Y==2.2;
    }
    public static int Main()
    {
        for(int i=0;i<4;i++)
        {
            int mask=i;
            Check("ORIGINAL_510_556_W06_RIG_ID_MASK_"+mask,()=>Mask(mask));
        }
        Check("ORIGINAL_510_LEDGER_FOREIGN_SLOT_ID_REJECTED",IdentityMismatch);
        Check("ORIGINAL_510_LEDGER_FOREIGN_OWNER_REJECTED",ForgedOwner);
        Check("ORIGINAL_510_LEDGER_CONSUMED_FORK_REJECTED",ContradictoryConsumed);
        Check("ORIGINAL_510_LEDGER_REATTACH_CALLBACK_THROW_ROLLBACK",ReattachThrowRollsBack);
        Check("ORIGINAL_510_LEDGER_REATTACH_SUCCESS_IDEMPOTENT",SuccessfulReattachThenNoDouble);
        Check("ORIGINAL_510_LEDGER_DUPLICATE_DETACH_DENIED",DuplicateDetachedIdNotConsumedTwice);
        Check("ORIGINAL_556_CORRUPTION_PRESTAGE_DENIED",OriginalCorruptPayloadDenied);
        Check("ORIGINAL_510_W06_ID_STABLE_GENERATION_1_2",StableIdsAcrossGeneration);
        Check("ORIGINAL_772_HYDRATOR_FROZEN_556_BYTE_EXACT_WIRE",ByteExactSnapshotRoundtrip);
        Console.WriteLine("B0161_W25_772_ORIGINAL_510_ATOMIC_DOCK_LEDGER_AND_556_SOURCE");
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("UNITY_PHYSICS_G1=NOT_RUN ORIGINAL_FROZEN_PRIVATE_RIG_NOT_CONNECTED GAME_SAVELOAD_HOLD");
        return fail==0?0:1;
    }
}
