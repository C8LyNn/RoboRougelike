// B01-62 P17 W27 — real original #792 Rigidbody owner source compiled and run
// with real original #797 one-file world+V7, frozen #729/#556/#510 and #780.
// ONLY UnityEngine is substituted by exact source-only shim from #792.
// Unity PhysX/editor G1 is NOT_RUN; never install .source/ shim into Unity.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using B01.CanonicalRigPortW24;
using B01.CanonicalWholeGameAdapter002;
using B01.WholeGameEconomy002;
using B01.CanonicalWorldApi001;
using B01.DetachedSaveContract001;
using B01.SecureReattachV2;
using B01.DebrisRepulse001;
using RoboRougelike.B01.Economy.ApprovedV7;

internal static class CompositeActualSourceQA
{
    const long Run=6270062;
    static readonly Guid Slot=Guid.Parse("62700620-0000-4000-8000-000000000556");
    static int pass,fail;
    static void Check(string name,Func<bool> work)
    {
        try{if(work()){pass++;Console.WriteLine("PASS "+name);}
            else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static bool Scratch(Func<string,bool> work)
    {
        string path=Path.Combine(Path.GetTempPath(),"b0162-w27-composite-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try{return work(path);}
        finally{Directory.Delete(path,true);}
    }
    static string OwnedDir(string root)=>Path.Combine(root,
        "b0162_canonical_single_authority_v3","run_"+Run+"_slot_"+Slot.ToString("N"));
    static byte[] World(long generation,bool light,bool heavy)
    {
        var parts=new PersistedPart[FrozenPartCatalog.Slots];
        for(int i=0;i<parts.Length;i++)
        {
            bool detached=i==0?light:heavy;
            var spec=SecureRigCatalog.At(i);
            long owner=FrozenPartCatalog.Owner(i);
            // Unity stores actual physics fields as float, so a genuine
            // lossless original #792 import requires exact float wire values.
            double x=(double)(float)(spec.RootX+spec.PartOffsetX);
            double y=(double)(detached?2.2f:1.2f);
            var velocity=new Vector3d(detached?0.25:0,0,0);
            parts[i]=new PersistedPart(spec.PartId,owner,detached?0:owner,
                FrozenPartCatalog.Asset(i),FrozenPartCatalog.Socket(i),
                detached?PartDockState.Detached:PartDockState.Attached,
                detached,!detached,new Vector3d(x,y,0),
                0,0,0,1,velocity,new Vector3d(0,0,0),spec.PartMassKg);
        }
        return SnapshotCodec.Encode(new RunSnapshot(Run,generation,parts));
    }
    static ApprovedEconomySnapshotV7 Economy(long generation,int scrap)
    {
        var parts=new List<OwnedModuleV7>();
        for(int i=0;i<FrozenPartCatalog.Slots;i++)
            parts.Add(new OwnedModuleV7{
                Id=WholeGameEconomySlotV2.EconomicPartId(FrozenPartCatalog.Id(i)),
                Category=i==0?"Core":"Weapon",Hp=1,PurchaseScrap=100,ReservationId=""
            });
        return new ApprovedEconomySnapshotV7{
            Version=7,OwnerId=Slot.ToString("N"),RunId=Run.ToString(),
            Generation=generation,Scrap=scrap,Wins=0,FreeUsedForWin=0,
            Parts=parts,Pending=new List<RebuildTicketV7>(),
            ConsumedVictoryIds=new List<string>(),
            Loaner=new OneShotLoanerV7{Phase=LoanerPhaseV7.None,
                EverEntered=false,EncounterId="",PermitId=""}
        };
    }
    static CanonicalPhysicalRigPortW24 Physical()
    {
        var host=new GameObject("W27 original B01-23 physical #792 source owner");
        return host.AddComponent<CanonicalPhysicalRigPortW24>();
    }
    static bool PhysicalWorldAndV7SingleOwnerCold()
        =>Scratch(root=>{
            var port=Physical();
            if(!port.SetExternalWritersQuiesced(true))return false;
            if(CanonicalWholeGameSessionV2.TryOpen(root,Run,Slot,true,
                port,port,out var s)!=WholeGameSessionOpen.Accepted||s==null)
                return false;
            try
            {
                if(s.TryRestore(World(1,true,false),Economy(1,80),Run,1)!=
                    CanonicalGateResult.Accepted)return false;
                var light=port.VisiblePartBody(0);
                var heavy=port.VisiblePartBody(1);
                if(light==null||heavy==null||
                   light.transform.parent!=null||heavy.transform.parent==null||
                   !port.VisibleConsumedId(0,FrozenPartCatalog.Id(0))||
                   port.VisibleConsumedId(1,FrozenPartCatalog.Id(1)))return false;
                if(!s.TryCapture(out var live,out var wallet)||
                   wallet==null||wallet.Scrap!=80||
                   !ColdRestore.TryStage(live,out var coldSource)||coldSource==null)
                    return false;
                if(CanonicalWholeGameSessionV2.TryOpen(root,Run,Slot,false,
                    port,port,out var other)!=WholeGameSessionOpen.Busy||
                    other!=null)return false;
                var e2=wallet.Copy();
                if(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(e2,
                   "w27-unique-victory-1","w27-real-encounter-a",
                   out var won,out var why)||won==null||why!="OK")
                    return false;
                won.Scrap=35;
                if(s.TryRestore(World(2,false,true),won,Run,2)!=
                    CanonicalGateResult.Accepted)return false;
                if(!s.TryCapture(out live,out wallet)||
                   wallet==null||wallet.Wins!=1||wallet.Scrap!=35||
                   wallet.ConsumedVictoryIds.Count!=1||
                   port.VisibleConsumedId(0,FrozenPartCatalog.Id(0))||
                   !port.VisibleConsumedId(1,FrozenPartCatalog.Id(1)))
                    return false;
                if(!File.Exists(Path.Combine(OwnedDir(root),"save.envelope"))||
                   !File.Exists(Path.Combine(OwnedDir(root),"slot.canonical-game-v2"))||
                   Directory.GetFiles(OwnedDir(root),"*.lease").Length!=0)
                    return false;
                if(!s.TryCloseAfterPhysicalRetirement(
                    port.AllOwnedPhysicalRootsDestroyed))return false;
            }
            finally{s.Dispose();}
            var port2=Physical();
            if(!port2.SetExternalWritersQuiesced(true))return false;
            if(CanonicalWholeGameSessionV2.TryOpen(root,Run,Slot,false,
                port2,port2,out var reloaded)!=WholeGameSessionOpen.Accepted||
               reloaded==null)return false;
            try
            {
                var econ=Economy(2,35);
                if(!ApprovedEconomyPolicyV7.TryStageTrustedVictory(
                    Economy(1,80),"w27-unique-victory-1","w27-real-encounter-a",
                    out var won,out _)||won==null)return false;
                won.Scrap=35;
                if(reloaded.TryRestore(World(2,false,true),won,Run,2)!=
                   CanonicalGateResult.Accepted)return false;
                if(!reloaded.TryCapture(out var world,out var wallet)||
                   wallet==null||wallet.Scrap!=35||wallet.Wins!=1||
                   wallet.ConsumedVictoryIds.Single()!="w27-unique-victory-1"||
                   !port2.VisibleConsumedId(1,FrozenPartCatalog.Id(1)))
                    return false;
                if(ApprovedEconomyPolicyV7.TryStageTrustedVictory(wallet,
                    "w27-unique-victory-1","w27-real-encounter-a",out _,out _))
                    return false;
                Console.WriteLine("W27_REAL_ORIGINAL_792_PORT_PLUS_797_FULL_WORLD_V7_COLD_IDS=PASS_MANAGED_SOURCE");
                return reloaded.TryCloseAfterPhysicalRetirement(
                    port2.AllOwnedPhysicalRootsDestroyed);
            }
            finally{reloaded.Dispose();}
        });
    static bool NonQuiescedGatePreventsGameSave()
        =>Scratch(root=>{
            var port=Physical();
            if(CanonicalWholeGameSessionV2.TryOpen(root,Run,Slot,true,
                port,port,out var s)!=WholeGameSessionOpen.Accepted||s==null)
                return false;
            try
            {
                if(s.TryRestore(World(1,true,false),Economy(1,100),Run,1)!=
                    CanonicalGateResult.HostBusy)return false;
                if(s.HasWorld||port.VisiblePartBody(0)!=null||
                   File.Exists(Path.Combine(OwnedDir(root),"save.envelope")))
                    return false;
                Console.WriteLine("W27_REAL_792_NOT_QUIESCED_GATE_PREVENTS_797_SAVE_AND_PHYSICAL_STAGE=PASS");
                return s.TryCloseAfterPhysicalRetirement(
                    port.AllOwnedPhysicalRootsDestroyed);
            }
            finally{s.Dispose();}
        });
    public static int Main()
    {
        Check("Real original #792 physical Rig[] + one #797 world/economy session cold replay",
            PhysicalWorldAndV7SingleOwnerCold);
        Check("Real original #792 quiescence gate stops #797 commit and body stage",
            NonQuiescedGatePreventsGameSave);
        Console.WriteLine("B0162_W27_ORIGINAL_COMPOSITE_PHYSICAL_SOURCE_ABI_TOTAL "+
            (pass+fail)+" PASS "+pass+" FAIL "+fail);
        Console.WriteLine("B0162_W27_UNITY_PHYSX_NATIVE_G1=NOT_RUN");
        return fail==0?0:1;
    }
}
