using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using RoboRougelike.B01.Legendary;
using RoboRougelike.B01.Economy.Rebuild;
using RoboRougelike.B01.Economy.LegendaryGuard;

sealed class SharedHost : IAuthoritativeR3Port, IAuthoritativeRebuildPort, IRebuildReservationProof, ISharedR3PhysicalOwnerIdentity
{
    public sealed class Item {public string Id,Kind,Family;public float Health;public bool IsMounted;}
    public string RobotId {get;}
    public readonly List<Item> Parts=new();
    public readonly Dictionary<string,string> Reservations=new(StringComparer.Ordinal);
    public readonly HashSet<string> Completed=new(StringComparer.Ordinal);
    public int Scrap=1000,RebuildQuote=25,BuyPrice=100,UnsafeRepairCalls,Debits,Completions,Salvages;
    public bool ProofAvailable=true,RejectReservation=false,RejectComplete=false,RejectMounted=false,SnapshotMismatch=false;
    public bool HideOwnershipIdentity;
    public object AuthoritativeR3Model => HideOwnershipIdentity ? null : this;
    public SharedHost(string id){RobotId=id;}
    public IReadOnlyList<PartObservation> Mounted=>Parts.Where(x=>x.IsMounted).Select(x=>new PartObservation(x.Id,x.Kind,x.Family,x.Health)).ToArray();
    public IReadOnlyList<PartObservation> Inventory=>Parts.Where(x=>!x.IsMounted).Select(x=>new PartObservation(x.Id,x.Kind,x.Family,x.Health)).ToArray();
    public IReadOnlyList<RebuildPartView> OwnedParts=>Parts.Select(x=>new RebuildPartView(x.Id,SnapshotMismatch?"Armor":x.Kind,x.Health)).ToArray();
    public int PurchasePrice(string id)=>BuyPrice;
    public int PaidRebuildQuote(string id)=>RebuildQuote;
    public bool HasReservation(string id,string key)=>Reservations.TryGetValue(id,out var k)&&k==key;
    public bool TryIsPending(string id,out bool pending){pending=Reservations.ContainsKey(id);return ProofAvailable;}
    public bool TryReserveAndDebitAtomically(string id,int charge,string key,out string reason)
    {
        var p=Parts.FirstOrDefault(x=>x.Id==id);
        if(RejectReservation||p==null||p.Health!=0||Reservations.ContainsKey(id)||charge>Scrap)
        {reason="RESERVE_DENIED";return false;}
        Scrap-=charge;Debits++;Reservations.Add(id,key);reason="OK";return true;
    }
    public bool TryCompleteReservedBatchAtomically(string eventId,IReadOnlyList<RebuildTicket> tickets,out string reason)
    {
        if(RejectComplete||Completed.Contains(eventId)||
           tickets.Any(t=>!HasReservation(t.InstanceId,t.ReservationKey)||Parts.Count(p=>p.Id==t.InstanceId&&p.Health==0)!=1))
        {reason="BATCH_DENIED";return false;}
        Completed.Add(eventId);Completions++;
        foreach(var t in tickets)
        {
            Reservations.Remove(t.InstanceId);
            Parts.Single(x=>x.Id==t.InstanceId).Health=1;
        }
        reason="OK";return true;
    }
    public bool HasCertifiedSocket(string kind,string token,LegendaryPlacement placement)=>
        kind=="WeaponBase"&&token=="weapon" || kind=="MobilityHub"&&token=="hub";
    public bool CanMountExisting(string id,LegendaryPlacement p,out string error)
    {
        if(RejectMounted||!Parts.Any(x=>x.Id==id&&!x.IsMounted)){error="MOUNT_DENIED";return false;}
        error="OK";return true;
    }
    public bool MountExisting(string id,LegendaryPlacement p,out string error)
    {
        if(!CanMountExisting(id,p,out error))return false;
        Parts.Single(x=>x.Id==id).IsMounted=true;error="OK";return true;
    }
    public bool CanUnmountSafely(string id,out string error)
    {
        var p=Parts.FirstOrDefault(x=>x.Id==id&&x.IsMounted);
        if(p==null||p.Health<=0){error="REMOVE_DENIED";return false;}
        error="OK";return true;
    }
    public bool UnmountExisting(string id,out string error)
    {
        if(!CanUnmountSafely(id,out error))return false;
        Parts.Single(x=>x.Id==id).IsMounted=false;error="OK";return true;
    }
    public bool RepairExisting(string id,out string error)
    {
        UnsafeRepairCalls++;
        var p=Parts.FirstOrDefault(x=>x.Id==id);
        if(p==null){error="NOT_OWNED";return false;}
        p.Health=1;error="OK";return true;
    }
    public bool ReceiveSalvage(PartObservation newPart,out string error)
    {
        if(Parts.Any(p=>p.Id==newPart.InstanceId)){error="ALREADY_OWNED";return false;}
        Parts.Add(new Item{Id=newPart.InstanceId,Kind=newPart.BasePartKind,Family=newPart.WeaponFamily,Health=newPart.Health});
        Salvages++;error="OK";return true;
    }
    public bool TransferInventoryAtomically(IAuthoritativeR3Port target,string id,out string error)
    {
        if(target is not SharedHost b || !Parts.Any(x=>x.Id==id&&!x.IsMounted)||b.Parts.Any(x=>x.Id==id))
        {error="TRANSFER_DENIED";return false;}
        var p=Parts.Single(x=>x.Id==id);Parts.Remove(p);b.Parts.Add(p);error="OK";return true;
    }
}
static class Program
{
    static readonly List<(string Name,bool Pass,string Error)> Results=new();
    static int next=1;
    static string NewId()=>(next++).ToString("x32");
    static void Ensure(bool ok,string why="assertion failed"){if(!ok)throw new Exception(why);}
    static void Same(object actual,object expected){Ensure(Equals(actual,expected),$"expected {expected}, got {actual}");}
    static void Test(string name,Action action){try{action();Results.Add((name,true,""));Console.WriteLine("PASS "+name);}
       catch(Exception e){Results.Add((name,false,e.Message));Console.Error.WriteLine("FAIL "+name+" "+e.Message);}}
    static (SharedHost,RebuildQueueService,LegendaryUnityService,GuardedLegendaryPort) Start(string id="robot-A")
    {
        var h=new SharedHost(id);var econ=new RebuildQueueService(h);
        var gate=new GuardedLegendaryPort(h,h,econ,h);
        var legendary=new LegendaryUnityService(new IAuthoritativeR3Port[]{gate},LegendaryCatalog.CreatePilotDefinitions());
        return(h,econ,legendary,gate);
    }
    static string Grant(SharedHost host,LegendaryUnityService legend,string kind="Gun")
    {
        var id=NewId();var fam=kind=="Gun"?"Railgun":null;
        Ensure(legend.TryGrant(host.RobotId,"grant-"+id,new PartObservation(id,kind,fam,1),kind=="Gun"?"LEG-WPN-001":"LEG-MOV-001",out var err),err);
        return id;
    }
    static void Destroy(SharedHost h,string id){h.Parts.Single(x=>x.Id==id).Health=0;}
    static string Save(RebuildQueueService queue){Ensure(queue.TrySave(out var data,out var e),e);return data;}
    static string Save(LegendaryUnityService legend){Ensure(legend.TrySaveMetadata(out var data,out var e),e);return data;}
    static void MainTests()
    {
        Test("01_guard_no_pending_proof_rejects_mount",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);h.ProofAvailable=false;
            Ensure(!g.CanMountExisting(id,new LegendaryPlacement(0,0,0,"weapon"),out var reason));
            Same(reason,"RESERVATION_PROOF_UNAVAILABLE");Same(h.Parts.Count,1);
        });
        Test("02_zero_hp_unqueued_repair_never_immediate",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(!l.TryRepair(h.RobotId,id,out var reason));
            Same(reason,"REBUILD_REQUIRED");Same(h.UnsafeRepairCalls,0);Same(h.Parts.Single().Health,0f);
        });
        Test("03_queue_legend_free_and_blocks_repair_until_win",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            Same(h.Scrap,1000);Same(h.Debits,1);
            Ensure(!l.TryRepair(h.RobotId,id,out reason));Same(reason,"REBUILD_PENDING");
            Same(h.UnsafeRepairCalls,0);Same(e.PendingCount,1);Same(h.Parts.Single().Health,0f);
            Ensure(!g.CanMountExisting(id,new LegendaryPlacement(0,0,0,"weapon"),out reason));Same(reason,"REBUILD_PENDING");
        });
        Test("04_loss_does_not_release_or_reset_free_cycle",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            Ensure(!e.TryFinishVictory("loss-1",false,out reason));Same(reason,"NO_VICTORY_NO_PROGRESS");
            Same(e.WonChallenges,0);Same(e.PendingCount,1);Ensure(!e.FreeSlotAvailable);
            Ensure(!l.TryRepair(h.RobotId,id,out reason));Same(reason,"REBUILD_PENDING");
        });
        Test("05_next_real_win_completes_and_preserves_legend_id",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            Ensure(e.TryFinishVictory("real-win-001",true,out reason),reason);
            Same(e.PendingCount,0);Same(e.WonChallenges,1);Ensure(e.FreeSlotAvailable);
            Same(h.Parts.Single().Id,id);Same(h.Parts.Single().Health,1f);
            Ensure(l.TryGetDefinition(id,out var def));Same(def.Id,"LEG-WPN-001");
            Ensure(l.TryEquip(h.RobotId,id,new LegendaryPlacement(0,0,0,"weapon"),out reason),reason);
            Same(h.Parts.Single().Id,id);
        });
        Test("06_two_zero_hp_copies_first_free_second_paid",()=>{
            var (h,e,l,g)=Start();string a=Grant(h,l),b=Grant(h,l);Destroy(h,a);Destroy(h,b);
            Ensure(e.TryQueueDestroyed(a,out var reason),reason);
            Ensure(e.TryQueueDestroyed(b,out reason),reason);
            Same(h.Scrap,975);Same(h.Debits,2);
            Same(e.PendingCount,2);
            Ensure(e.TryFinishVictory("v2",true,out reason),reason);
            Same(h.Parts.Count,2);Ensure(h.Parts.All(p=>p.Health==1));
            Same(h.Scrap,975);Same(h.Completions,1);
        });
        Test("07_paid_order_insufficient_money_leaves_all_unchanged",()=>{
            var (h,e,l,g)=Start();string a=Grant(h,l),b=Grant(h,l);Destroy(h,a);Destroy(h,b);
            Ensure(e.TryQueueDestroyed(a,out var reason),reason);
            h.Scrap=10;var before=Save(e);
            Ensure(!e.TryQueueDestroyed(b,out reason));Same(reason,"RESERVE_DENIED");
            Same(h.Scrap,10);Same(e.PendingCount,1);Same(Save(e),before);
            Same(h.Parts.Count,2);
        });
        Test("08_save_load_rebuild_and_legend_same_owned_physical",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            var econSave=Save(e);var legendarySave=Save(l);
            var afterE=new RebuildQueueService(h);
            var afterG=new GuardedLegendaryPort(h,h,afterE,h);
            var afterL=new LegendaryUnityService(new IAuthoritativeR3Port[]{afterG},LegendaryCatalog.CreatePilotDefinitions());
            Ensure(afterE.TryRestore(econSave,out reason),reason);
            Ensure(afterL.TryRestoreMetadata(legendarySave,out reason),reason);
            Ensure(!afterL.TryRepair(h.RobotId,id,out reason));Same(reason,"REBUILD_PENDING");
            Ensure(afterE.TryFinishVictory("v3",true,out reason),reason);
            Same(h.Parts.Single().Id,id);
            Ensure(afterL.CheckInvariants(out reason),reason);
        });
        Test("09_save_load_victory_replay_cannot_refund_or_reforge",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            Ensure(e.TryFinishVictory("v-replay",true,out reason),reason);
            var econSave=Save(e);var legendarySave=Save(l);var initialMoney=h.Scrap;
            var e2=new RebuildQueueService(h);var g2=new GuardedLegendaryPort(h,h,e2,h);
            var l2=new LegendaryUnityService(new IAuthoritativeR3Port[]{g2},LegendaryCatalog.CreatePilotDefinitions());
            Ensure(e2.TryRestore(econSave,out reason),reason);
            Ensure(l2.TryRestoreMetadata(legendarySave,out reason),reason);
            Ensure(!e2.TryFinishVictory("v-replay",true,out reason));Same(reason,"VICTORY_EVENT_REPLAY");
            Same(h.Scrap,initialMoney);Same(h.Parts.Count,1);Same(h.Completions,1);
        });
        Test("10_corrupt_save_does_not_mutate_existing_ledger",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            var good=Save(e);Ensure(!e.TryRestore("{",out reason));Same(reason,"CORRUPT_REBUILD_SAVE");
            Same(Save(e),good);Same(h.Parts.Count,1);
        });
        Test("11_cross_package_host_ownership_mismatch_blocks_mutator",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);
            h.SnapshotMismatch=true;
            Ensure(!l.TryRepair(h.RobotId,id,out var reason));Same(reason,"STALE_CROSS_PACKAGE_OWNERSHIP");
            Same(h.UnsafeRepairCalls,0);Same(h.Salvages,1);
        });
        Test("12_pending_transfer_blocked_even_when_inventory",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            var target=new SharedHost("robot-B");var qe=new RebuildQueueService(target);
            var guardedTarget=new GuardedLegendaryPort(target,target,qe,target);
            Ensure(!g.TransferInventoryAtomically(guardedTarget,id,out reason));
            Same(reason,"REBUILD_PENDING");Same(h.Parts.Count,1);Same(target.Parts.Count,0);
        });
        Test("13_unguarded_transfer_target_rejected",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);
            var target=new SharedHost("B");
            Ensure(!g.TransferInventoryAtomically(target,id,out var reason));Same(reason,"UNGUARDED_TRANSFER_TARGET");
            Same(h.Parts.Count,1);Same(target.Parts.Count,0);
        });
        Test("14_historical_free_epoch_persists_after_save",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            Ensure(e.TryFinishVictory("win1",true,out reason),reason);
            var a=Grant(h,l);Destroy(h,a);Ensure(e.TryQueueDestroyed(a,out reason),reason);
            var save=Save(e);
            var loaded=new RebuildQueueService(h);
            Ensure(loaded.TryRestore(save,out reason),reason);
            Ensure(!loaded.FreeSlotAvailable);
            var b=Grant(h,l);Destroy(h,b);
            Ensure(loaded.TryQueueDestroyed(b,out reason),reason);
            Same(h.Scrap,975);
        });
        Test("15_reject_broken_reservation_on_reload",()=>{
            var (h,e,l,g)=Start();var id=Grant(h,l);Destroy(h,id);
            Ensure(e.TryQueueDestroyed(id,out var reason),reason);
            var save=Save(e);
            h.Reservations.Clear();
            var restored=new RebuildQueueService(h);
            Ensure(!restored.TryRestore(save,out reason));Same(reason,"UNMATCHED_HOST_RESERVATION");
            Same(restored.PendingCount,0);
        });
        Test("16_multi_robot_different_legend_same_type_not_global_limit",()=>{
            var h1=new SharedHost("A");var h2=new SharedHost("B");
            var q1=new RebuildQueueService(h1);var q2=new RebuildQueueService(h2);
            var g1=new GuardedLegendaryPort(h1,h1,q1,h1);var g2=new GuardedLegendaryPort(h2,h2,q2,h2);
            var l=new LegendaryUnityService(new IAuthoritativeR3Port[]{g1,g2},LegendaryCatalog.CreatePilotDefinitions());
            string a=Grant(h1,l),b=Grant(h2,l);
            Ensure(l.TryEquip("A",a,new LegendaryPlacement(0,0,0,"weapon"),out var reason),reason);
            Ensure(l.TryEquip("B",b,new LegendaryPlacement(0,0,0,"weapon"),out reason),reason);
            Ensure(l.CheckInvariants(out reason),reason);
        });
        Test("17_queued_destroyed_mounted_keeps_legend_slot",()=>{
            var (h,e,l,g)=Start();string a=Grant(h,l),b=Grant(h,l);
            Ensure(l.TryEquip(h.RobotId,a,new LegendaryPlacement(0,0,0,"weapon"),out var reason),reason);
            Destroy(h,a);Ensure(e.TryQueueDestroyed(a,out reason),reason);
            Ensure(!l.TryEquip(h.RobotId,b,new LegendaryPlacement(0,0,0,"weapon"),out reason));
            Same(reason,"LEGEND_ALREADY_EQUIPPED");Same(h.Parts.Count,2);
        });
        Test("W14_same_guid_different_physical_owner_must_reject",()=>{
            var actualRobot=new SharedHost("A");
            var wrongEconomyStore=new SharedHost("A");
            var id=NewId();
            actualRobot.Parts.Add(new SharedHost.Item{Id=id,Kind="Gun",Family="Railgun",Health=0});
            wrongEconomyStore.Parts.Add(new SharedHost.Item{Id=id,Kind="Gun",Family="Railgun",Health=0});
            var guard=new GuardedLegendaryPort(actualRobot,wrongEconomyStore,
                new RebuildQueueService(wrongEconomyStore),wrongEconomyStore);
            Ensure(!guard.CheckSharedOwnership(out var reason),
                "SPLIT_BRAIN: identical GUID/kind/HP conceals DIFFERENT authoritative R3 inventories");
            Same(reason,"OWNER_IDENTITY_MISMATCH");
        });
        Test("W14_missing_shared_owner_proof_refuses_mutations",()=>{
            var host=new SharedHost("A");
            var id=NewId();
            host.Parts.Add(new SharedHost.Item{Id=id,Kind="Gun",Family="Railgun",Health=0});
            host.HideOwnershipIdentity=true;
            var gate=new GuardedLegendaryPort(host,host,new RebuildQueueService(host),host);
            Ensure(!gate.CheckSharedOwnership(out var reason));
            Same(reason,"OWNER_IDENTITY_UNAVAILABLE");
            Ensure(!gate.RepairExisting(id,out reason));
            Same(reason,"OWNER_IDENTITY_UNAVAILABLE");
            Same(host.UnsafeRepairCalls,0);
        });
        Test("W15_noncanonical_R3_physical_ID_rejected_before_any_repair",()=>{
            var (h,e,l,g)=Start();
            var id=Guid.NewGuid().ToString("N").ToUpperInvariant();
            h.Parts.Add(new SharedHost.Item{Id=id,Kind="Gun",Family="Railgun",Health=0.5f});
            Ensure(!g.CheckSharedOwnership(out var reason),
                "INVALID_PHYSICAL_INSTANCE: Uppercase GUID accepted by both live R3 host views");
            Same(reason,"INVALID_PHYSICAL_INSTANCE");
            Ensure(!g.RepairExisting(id,out reason),
                "Unsafe direct repair accepted noncanonical physical R3 ID");
            Same(h.UnsafeRepairCalls,0);
        });
        Test("W15_empty_and_out_of_range_R3_physical_ID_rejected",()=>{
            var (h,e,l,g)=Start();
            h.Parts.Add(new SharedHost.Item{Id=Guid.Empty.ToString("N"),Kind="Gun",Family="Railgun",Health=0.5f});
            Ensure(!g.CheckSharedOwnership(out var reason),"Guid.Empty accepted by shared owner");
            Same(reason,"INVALID_PHYSICAL_INSTANCE");
            h.Parts[0].Id=NewId();
            h.Parts[0].Health=1.5f;
            Ensure(!g.CheckSharedOwnership(out reason),"HP>1 accepted by shared owner");
            Same(reason,"INVALID_PHYSICAL_INSTANCE");
            Same(h.UnsafeRepairCalls,0);
        });
        Test("18_seeded_1800_damage_queue_win_restart_stress",()=>{
            var (h,e,l,g)=Start();
            var ids=new[]{Grant(h,l),Grant(h,l,"HoverEngine"),Grant(h,l)};
            var rnd=new Random(222513);
            for(var n=0;n<1800;n++)
            {
                var id=ids[rnd.Next(ids.Length)];
                var p=h.Parts.Single(x=>x.Id==id);
                if(p.Health>0)p.Health=0;
                if(!e.IsRebuilding(id))e.TryQueueDestroyed(id,out _);
                if(n%3==0)e.TryFinishVictory("failed-"+n,false,out _);
                if(n%7==0)Ensure(e.TryFinishVictory("victory-"+n,true,out var r),r);
                Ensure(!e.IsQuarantined);
                Ensure(g.CheckSharedOwnership(out var message),message);
                Ensure(l.CheckInvariants(out message),message);
                if(n%59==0){var s=Save(e);var copy=new RebuildQueueService(h);Ensure(copy.TryRestore(s,out message),message);}
            }
        });
    }
    static int Main(string[] args)
    {
        MainTests();
        var passed=Results.Count(x=>x.Pass);
        var report=new{schema="b01.w13.econlegendary.cross-source.v1",
            upstreamEcon="12572117a3dce369257feabc57021cde58023369",
            upstreamLegend="dffabbc6960304fdb33322b5a23c884feac804c6",
            nativeUnity="NOT_RUN",total=Results.Count,passed,failed=Results.Count-passed,
            cases=Results.Select(x=>new {id=x.Name,result=x.Pass?"PASS":"FAIL",error=x.Error})};
        var path=args.Length>0?args[0]:"crossqa-results.json";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path,JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"RESULT {passed}/{Results.Count} PASS, native Unity NOT_RUN");
        return passed==Results.Count?0:1;
    }
}
