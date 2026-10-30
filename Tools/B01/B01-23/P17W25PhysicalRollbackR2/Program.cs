#nullable enable
using System;
using B01.DebrisRepulse001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.CanonicalWorldApi001;
using B01.CanonicalRigPortW24;
using UnityEngine;
internal static class RealPhysicalUnitySourceW25
{
    static int ok,bad;
    static void Test(string name,Func<bool> fn)
    {try{if(fn()){ok++;Console.WriteLine("PASS "+name);}
         else{bad++;Console.WriteLine("FAIL "+name);}}
     catch(Exception e){bad++;Console.WriteLine("FAIL "+name+" "+e);}}
    static byte[] Bytes(long run,long generation,int mask)
    {
        var items=new PersistedPart[2];
        for(int i=0;i<2;i++)
        {
            bool detached=(mask&(1<<i))!=0;
            var spec=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            items[i]=new PersistedPart(spec.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(i==0?-2f:4f,1.25f,0),
                0,0,0,1,detached?new Vector3d(1,0,0):new Vector3d(0,0,0),
                new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(run,generation,items));
    }
    static bool OriginalActualRuntimeCase(long run,long gen,int mask)
    {
        byte[] b=Bytes(run,gen,mask);
        if(!ColdRestore.TryStage(b,out ColdRestore? plan)||plan==null)return false;
        var host=new GameObject("W25 actual physical code in managed fake Unity scene");
        var port=host.AddComponent<CanonicalPhysicalRigPortW24>();
        if(!port.SetExternalWritersQuiesced(true))return false;
        ICanonicalStageOwnership stage=port.BeginOwnedStage(plan.Snapshot,plan);
        ICanonicalStagedWorld world=stage.PrepareInactive();
        if(!SnapshotCodec.Encode(world.Capture()).AsSpan().SequenceEqual(b))
            return false;
        world.Activate();
        for(int i=0;i<2;i++)
        {
            bool detached=(mask&(1<<i))!=0;
            var body=port.VisiblePartBody(i);
            if(body==null||body.mass!=(float)FrozenPartCatalog.Mass(i)||
               body.isKinematic==detached||
               port.VisibleConsumedId(i,FrozenPartCatalog.Id(i))!=detached||
               (body.transform.parent==null)!=detached)return false;
        }
        if(!SnapshotCodec.Encode(world.Capture()).AsSpan().SequenceEqual(b))
            return false;
        // Live body moves after original g1 immutable restore.
        var moving=port.VisiblePartBody(0);
        if(moving==null)return false;
        if((mask&1)!=0)
        {
            moving.linearVelocity=new Vector3(2.5f,0,0);
            moving.angularVelocity=new Vector3(0,0.5f,0);
            world.Deactivate();
            world.Activate();
            if(moving.linearVelocity.x!=2.5f||moving.angularVelocity.y!=0.5f)
                return false; // old W24 source BUG resets velocity 1, angular 0
        }
        world.Retire();
        return true;
    }
    static int Main()
    {
        for(int mask=0;mask<4;mask++)
            for(int gen=1;gen<=3;gen++)
            {
                int m=mask,g=gen;
                Test("ACTUAL W25 physical Unity C# source direct compile / ORIGINAL ledger "+m+"-g"+g,
                    ()=>OriginalActualRuntimeCase(81,g,m));
            }
        Test("physical session requires external writers quiesced before any root allocation",()=>
        {
            var go=new GameObject("unquiesced");var port=go.AddComponent<CanonicalPhysicalRigPortW24>();
            if(!ColdRestore.TryStage(Bytes(81,1,1),out ColdRestore? cold)||cold==null)
                return false;
            try{port.BeginOwnedStage(cold.Snapshot,cold);return false;}
            catch(InvalidOperationException){return true;}
        });
        Test("original #556 corrupt physical import cannot hydrate",()=>
        {
            byte[] broken=Bytes(81,1,1);broken[121]^=7;
            return !ColdRestore.TryStage(broken,out ColdRestore? plan)&&plan==null;
        });
        Console.WriteLine("W25_ACTUAL_PHYSICAL_SOURCE_SHIM_TOTAL "+(ok+bad)+
            " PASS "+ok+" FAIL "+bad);
        Console.WriteLine("UNITYENGINE=MANAGED_FAKE_SHIM_ONLY; NATIVE_UNITY_PHYSX_G1=NOT_RUN");
        return bad==0?0:1;
    }
}
