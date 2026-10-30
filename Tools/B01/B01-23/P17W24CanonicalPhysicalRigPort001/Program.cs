#nullable enable
using System;
using System.IO;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.CanonicalRigPortW24;
internal static class TestPhysicalLedger
{
    static int good,bad;
    static void Check(string name,Func<bool> f)
    {
        try{if(f()){good++;Console.WriteLine("PASS "+name);}
            else{bad++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){bad++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static byte[] Saved(long run,long gen,int mask)
    {
        var parts=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            SecureRigSpec s=SecureRigCatalog.At(i);
            bool detached=(mask&(1<<i))!=0;
            long owner=FrozenPartCatalog.Owner(i);
            parts[i]=new PersistedPart(s.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(i==0?-2f:4f,1.25f,0f),
                0,0,0,1,
                detached?new Vector3d(1f,0f,0f):new Vector3d(0,0,0),
                new Vector3d(0,0,0),s.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,gen,parts));
    }
    static int Main()
    {
        for(int mask=0;mask<4;mask++)
            for(int gen=1;gen<=3;gen++)
                for(int run=81;run<=82;run++)
                {
                    int m=mask,g=gen,r=run;
                    Check("actual #556/W06/#510 physical ledger "+r+"-"+g+"-"+m,()=>{
                        byte[] source=Saved(r,g,m);
                        if(!ColdRestore.TryStage(source,out ColdRestore? cold)||cold==null)
                            return false;
                        for(int i=0;i<2;i++)
                        {
                            PersistedPart p=cold.Snapshot.Parts[i];
                            if(!CanonicalRigLedgerHydratorW24.TryHydrate(p,i,cold,
                               out AtomicDockLedger ledger)||
                               ledger.Contains(p.Id)!=p.Consumed||
                               ledger.Count!=(p.Consumed?1:0)||
                               p.Id!=SecureRigCatalog.At(i).PartId)return false;
                        }
                        return true;
                    });
                }
        Check("real original #510 ledger rollback after physical callback error",()=>{
            if(!ColdRestore.TryStage(Saved(81,1,1),out ColdRestore? cold)||cold==null)
                return false;
            PersistedPart p=cold.Snapshot.Parts[0];
            if(!CanonicalRigLedgerHydratorW24.TryHydrate(p,0,cold,
                out AtomicDockLedger ledger))return false;
            bool succeeded=ledger.TryReattach(p.Id,
                ()=>throw new IOException("actual physical callback fails"),()=>{});
            return !succeeded&&ledger.Contains(p.Id)&&ledger.Count==1;
        });
        Check("wrong slot part ID is rejected before private ledger mutation",()=>{
            if(!ColdRestore.TryStage(Saved(81,1,1),out ColdRestore? cold)||cold==null)
                return false;
            PersistedPart p=cold.Snapshot.Parts[0];
            return !CanonicalRigLedgerHydratorW24.TryHydrate(p,1,cold,out var x)&&x.Count==0;
        });
        Check("corrupted original #556 wire never hydrates",()=>{
            byte[] bad=Saved(81,1,3);bad[125]^=4;
            return !ColdRestore.TryStage(bad,out ColdRestore? cold)&&cold==null;
        });
        Check("source catalog stable physical mass and two IDs",()=>
            SecureRigCatalog.Light.PartId==2310001&&
            SecureRigCatalog.Heavy.PartId==2310002&&
            SecureRigCatalog.Light.PartMassKg==FrozenPartCatalog.Mass(0)&&
            SecureRigCatalog.Heavy.PartMassKg==FrozenPartCatalog.Mass(1));
        Check("succeeded original private reattach rearms consumed ID",()=>{
            if(!ColdRestore.TryStage(Saved(81,2,2),out ColdRestore? cold)||cold==null)
                return false;
            PersistedPart p=cold.Snapshot.Parts[1];
            if(!CanonicalRigLedgerHydratorW24.TryHydrate(p,1,cold,
                out AtomicDockLedger ledger))return false;
            return ledger.TryReattach(p.Id,()=>{},()=>{})&&
                   !ledger.Contains(p.Id)&&ledger.Count==0;
        });
        Console.WriteLine("TOTAL "+(good+bad)+" PASS "+good+" FAIL "+bad);
        Console.WriteLine("ORIGINAL_510_ATOMIC_LEDGER_AND_556_WIRE=COMPILED; UNITY_PHYSX=NOT_RUN");
        return bad==0?0:1;
    }
}
