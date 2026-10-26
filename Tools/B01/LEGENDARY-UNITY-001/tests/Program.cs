using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using RoboRougelike.B01.Legendary;

sealed class MemoryPort : IAuthoritativeR3Port
{
    public string RobotId { get; private set; }
    public readonly List<PartObservation> M=new();
    public readonly List<PartObservation> I=new();
    public IReadOnlyList<PartObservation> Mounted => M;
    public IReadOnlyList<PartObservation> Inventory => I;
    public bool AllowWeaponSocket=true,AllowMobilitySocket=true,BlockMount=false,BlockRepair=false,BlockTransfer=false;
    public MemoryPort(string id){RobotId=id;}
    public bool HasCertifiedSocket(string kind,string token,LegendaryPlacement place) =>
        kind=="WeaponBase"&&token=="w1"&&AllowWeaponSocket ||
        kind=="MobilityHub"&&token=="m1"&&AllowMobilitySocket;
    public bool CanMountExisting(string id,LegendaryPlacement pos,out string e) {
        if(BlockMount){e="INTERRUPTED_MOUNT";return false;}
        if(!I.Any(p=>p.InstanceId==id)){e="NO_INVENTORY";return false;}
        e="OK";return true;
    }
    public bool MountExisting(string id,LegendaryPlacement pos,out string e) {
        if(!CanMountExisting(id,pos,out e))return false;
        var p=I.Single(x=>x.InstanceId==id);I.Remove(p);M.Add(p);return true;
    }
    public bool CanUnmountSafely(string id,out string e) {
        if(!M.Any(p=>p.InstanceId==id)){e="NOT_EQUIPPED";return false;}
        if(M.Single(p=>p.InstanceId==id).Health<=0){e="ZERO_HP_UNMOUNT_UNSAFE";return false;}
        e="OK";return true;
    }
    public bool UnmountExisting(string id,out string e) {
        if(!CanUnmountSafely(id,out e))return false;
        var p=M.Single(x=>x.InstanceId==id);M.Remove(p);I.Add(p);return true;
    }
    public bool RepairExisting(string id,out string e) {
        if(BlockRepair){e="NO_SCRAP_OR_CONNECTION";return false;}
        var p=M.Concat(I).FirstOrDefault(x=>x.InstanceId==id);
        if(p==null){e="NOT_OWNED";return false;}
        p.Health=1;e="OK";return true;
    }
    public bool ReceiveSalvage(PartObservation part,out string e) {
        if(M.Concat(I).Any(p=>p.InstanceId==part.InstanceId)){e="ID_COLLISION";return false;}
        I.Add(new PartObservation(part.InstanceId,part.BasePartKind,part.WeaponFamily,part.Health));
        e="OK";return true;
    }
    public bool TransferInventoryAtomically(IAuthoritativeR3Port dest,string id,out string e) {
        if(BlockTransfer||dest is not MemoryPort other){e="TRANSFER_HOST_API_PENDING";return false;}
        var p=I.FirstOrDefault(x=>x.InstanceId==id);
        if(p==null||other.I.Concat(other.M).Any(x=>x.InstanceId==id))
        {e="TRANSFER_REJECT";return false;}
        I.Remove(p);other.I.Add(p);e="OK";return true;
    }
}
static class Program
{
    static readonly List<(string,bool,string)> Cases=new();
    static int serial;
    static string NewId() => (++serial).ToString("x32");
    static LegendaryDefinition[] Defs() => LegendaryCatalog.CreatePilotDefinitions();
    static LegendaryUnityService Service(params MemoryPort[] ports)=>new LegendaryUnityService(ports,Defs());
    static PartObservation Part(string kind,string family=null,float hp=1,string id=null)=>
        new PartObservation(id??NewId(),kind,family,hp);
    static void Check(bool b,string msg="assertion failed") {if(!b)throw new Exception(msg);}
    static void Eq(string x,string y){Check(x==y,$"expected {y}, got {x}");}
    static void Test(string id,Action f) {
        try{f();Cases.Add((id,true,""));Console.WriteLine("PASS "+id);}
        catch(Exception e){Cases.Add((id,false,e.Message));Console.Error.WriteLine("FAIL "+id+" "+e.Message);}
    }
    static LegendaryPlacement Pos(string socket="w1")=>new LegendaryPlacement(0,0,0,socket);
    static string DefId(string kind)=>kind=="HoverEngine"?"LEG-MOV-001":"LEG-WPN-001";
    static string CertSocket(string kind)=>kind=="HoverEngine"?"m1":"w1";
    static PartObservation Grant(LegendaryUnityService s,MemoryPort port,string kind,string evt=null) {
        var p=Part(kind,kind=="Gun"?"Railgun":null);
        Check(s.TryGrant(port.RobotId,evt??"evt-"+p.InstanceId,p,DefId(kind),out var e),e);
        return p;
    }
    static void Mount(LegendaryUnityService s,MemoryPort port,PartObservation p) {
        Check(s.TryEquip(port.RobotId,p.InstanceId,Pos(CertSocket(p.BasePartKind)),out var e),e);
    }
    static string Save(LegendaryUnityService s) {
        Check(s.TrySaveMetadata(out var json,out var e),e);return json;
    }
    static string FixturePath=>Path.Combine(AppContext.BaseDirectory,"fixtures-leg11.json");
    static string Prop(JsonElement v,string k,string fallback=null)=>v.TryGetProperty(k,out var p)&&p.ValueKind!=JsonValueKind.Null?p.GetString():fallback;
    static float Hp(JsonElement v)=>v.TryGetProperty("hp",out var p)?p.GetSingle():1f;
    static void RunFixture(JsonElement row)
    {
        var input=row.GetProperty("input");
        var ev=Prop(input,"event");
        var a=new MemoryPort("A");var b=new MemoryPort("B");
        var host=new Dictionary<string,MemoryPort>{{"A",a},{"B",b}};
        var guidFor=new Dictionary<string,string>(StringComparer.Ordinal);
        string ConvertId(string id){if(!guidFor.TryGetValue(id,out var value))guidFor[id]=value=NewId();return value;}
        var bindings=new List<LegendaryBinding>();
        foreach(var old in input.GetProperty("owned").EnumerateArray())
        {
            var id=ConvertId(Prop(old,"id"));
            var kind=Prop(old,"kind",Prop(old,"definition")=="LEG-MOV-001"?"HoverEngine":"Gun");
            var fam=Prop(old,"family",kind=="Gun"?"Railgun":null);
            var p=Part(kind,fam,Hp(old),id);
            var robot=Prop(old,"robot","A");
            if(ev=="equip"&&old.TryGetProperty("robot",out _))host[robot].M.Add(p);else a.I.Add(p);
            var def=Prop(old,"definition");
            if(def!=null)bindings.Add(new LegendaryBinding(id,def));
        }
        var fresh=input.GetProperty("newPart");
        var freshKind=Prop(fresh,"kind",Prop(fresh,"definition")=="LEG-MOV-001"?"HoverEngine":"Gun");
        var freshDef=Prop(fresh,"definition");
        var freshFamily=Prop(fresh,"family",freshKind=="Gun"?"Railgun":null);
        var freshId=ConvertId(Prop(fresh,"id"));
        var robotNew=Prop(fresh,"robot","A");
        var newPart=Part(freshKind,freshFamily,Hp(fresh),freshId);
        var service=Service(a,b);
        string error;
        bool ok;
        if(ev=="acquire")
        {
            // Existing physical inventory metadata must be restored first.
            var state=new LegendaryPersistence{Bindings=bindings.ToArray(),RewardEventIds=Array.Empty<string>()};
            Check(service.TryRestoreMetadata(UnityEngine.JsonUtility.ToJson(state),out error),error);
            ok=service.TryGrant("A","fixture-"+Prop(row,"id"),newPart,freshDef,out error);
        }
        else
        {
            // For same GUID across different robots, build corrupted host state intentionally.
            host[robotNew].I.Add(newPart);
            var state=new LegendaryPersistence{Bindings=bindings.ToArray(),RewardEventIds=Array.Empty<string>()};
            if(!service.TryRestoreMetadata(UnityEngine.JsonUtility.ToJson(state),out error))
                ok=false;
            else
            {
                if(freshDef!=null)
                {
                    // In a real save, registration is restored together with the physical inventory.
                    var all=bindings.Concat(new[]{new LegendaryBinding(freshId,freshDef)}).ToArray();
                    var next=new LegendaryPersistence{Bindings=all,RewardEventIds=Array.Empty<string>()};
                    if(!service.TryRestoreMetadata(UnityEngine.JsonUtility.ToJson(next),out error))ok=false;
                    else ok=service.TryEquip(robotNew,freshId,Pos(CertSocket(freshKind)),out error);
                }
                else ok=service.TryEquip(robotNew,freshId,Pos(CertSocket(freshKind)),out error);
            }
        }
        var expected=row.GetProperty("expected");
        var want=Prop(expected,"status")=="PASS";
        var code=Prop(expected,"code");
        Check(ok==want,$"expected status={want} actual={ok}, error={error}");
        if(!ok)Eq(error,code);
        else Eq(error,"OK");
    }
    static int Main(string[] args)
    {
        var output=args.Length>0?args[0]:"test-results.json";
        Test("catalog_contract_and_calibration",()=>{
            var d=Defs();Check(LegendaryCatalog.Validate(d,out var e),e);
            Check(d.Length==2 && d[0].Mass.TestGridKg.SequenceEqual(new[]{220f,435f,650f}));
            Check(d[1].Mass.TestGridKg.SequenceEqual(new[]{150f,465f,780f}));
            Check(!d.Any(x=>x.Mass.FinalMassApproved||x.AbilityImplemented));
        });
        using var fixtures=JsonDocument.Parse(File.ReadAllText(FixturePath));
        var rows=fixtures.RootElement.GetProperty("fixtures").EnumerateArray().ToArray();
        Test("exact_upstream_fixture_count_and_ids",()=> {
            Eq(Prop(fixtures.RootElement,"upstreamSha"),"93f290acca26839792a5bec8ea42415ac83932f3");
            Check(rows.Length==11); Check(rows.Select(x=>Prop(x,"id")).Distinct().Count()==11);
        });
        foreach(var row in rows){var copy=row;Test(Prop(row,"id"),()=>RunFixture(copy));}
        Test("mount_interrupt_atomic_no_item_loss",()=>{
            var a=new MemoryPort("A"){BlockMount=true};var s=Service(a);var p=Grant(s,a,"Gun");
            var before=Save(s);
            Check(!s.TryEquip("A",p.InstanceId,Pos(),out var e));Eq(e,"INTERRUPTED_MOUNT");
            Check(a.I.Count==1 && a.M.Count==0 && Save(s)==before);
        });
        Test("duplicate_definition_mount_zero_hp_atomic",()=>{
            var a=new MemoryPort("A");var s=Service(a);
            var p=Grant(s,a,"Gun");var q=Grant(s,a,"Gun");Mount(s,a,p);
            a.M[0].Health=0;
            var before=Save(s);
            Check(!s.TryEquip("A",q.InstanceId,Pos(),out var e));Eq(e,"LEGEND_ALREADY_EQUIPPED");
            Check(a.M.Count==1&&a.I.Count==1&&Save(s)==before);
        });
        Test("zero_hp_repair_reload_same_instance_id",()=>{
            var a=new MemoryPort("A");var s=Service(a);var p=Grant(s,a,"Gun");Mount(s,a,p);
            a.M[0].Health=0;
            var saved=Save(s);
            var restored=Service(a);
            Check(restored.TryRestoreMetadata(saved,out var e),e);
            Check(restored.TryRepair("A",p.InstanceId,out e),e);
            Eq(restored.TryGetDefinition(p.InstanceId,out var d)?d.Id:"?","LEG-WPN-001");
            Check(a.M.Single().InstanceId==p.InstanceId&&a.M.Single().Health==1);
            var twice=Service(a);Check(twice.TryRestoreMetadata(Save(restored),out e),e);
        });
        Test("zero_hp_remove_refused_until_repair",()=>{
            var a=new MemoryPort("A");var s=Service(a);var p=Grant(s,a,"Gun");Mount(s,a,p);
            a.M[0].Health=0;Check(!s.TryUnequip("A",p.InstanceId,out var e));Eq(e,"ZERO_HP_UNMOUNT_UNSAFE");
            Check(a.M.Count==1);Check(s.TryRepair("A",p.InstanceId,out e),e);
            Check(s.TryUnequip("A",p.InstanceId,out e),e);Check(a.I.Count==1&&a.M.Count==0);
        });
        Test("mobilityhub_no_structural_fallback",()=>{
            var a=new MemoryPort("A"){AllowMobilitySocket=false};var s=Service(a);var p=Grant(s,a,"HoverEngine");
            Check(!s.TryEquip("A",p.InstanceId,Pos("structural"),out var e));
            Eq(e,"MOUNT_ADAPTER_PENDING_OR_SOCKET_MISMATCH");Check(a.I.Count==1);
            Check(!s.TryEquip("A",p.InstanceId,Pos("m1"),out e));Eq(e,"MOUNT_ADAPTER_PENDING_OR_SOCKET_MISMATCH");
        });
        Test("two_different_legends_on_same_robot",()=>{
            var a=new MemoryPort("A");var s=Service(a);var p=Grant(s,a,"Gun");var q=Grant(s,a,"HoverEngine");
            Mount(s,a,p);Mount(s,a,q);Check(a.M.Count==2&&s.CheckInvariants(out _));
        });
        Test("ordinary_railgun_shares_family",()=>{
            var a=new MemoryPort("A");a.M.Add(Part("Gun","Railgun"));
            var s=Service(a);var p=Grant(s,a,"Gun");Mount(s,a,p);Check(a.M.Count==2);
            a.M.Add(Part("Gun","Automatic"));var q=Grant(s,a,"Gun");
            Check(!s.TryEquip("A",q.InstanceId,Pos(),out var e));Eq(e,"LEGEND_ALREADY_EQUIPPED");
        });
        Test("weapon_third_family_rejected",()=>{
            var a=new MemoryPort("A");a.M.Add(Part("Gun","Shotgun"));a.M.Add(Part("Gun","Rocket"));
            var s=Service(a);var p=Grant(s,a,"Gun");Check(!s.TryEquip("A",p.InstanceId,Pos(),out var e));
            Eq(e,"WEAPON_FAMILY_3");Check(a.I.Count==1);
        });
        Test("reward_replay_and_cross_vehicle_instance",()=>{
            var a=new MemoryPort("A");var b=new MemoryPort("B");var s=Service(a,b);
            var p=Part("Gun","Railgun");Check(s.TryGrant("A","event-1",p,"LEG-WPN-001",out var e),e);
            Check(!s.TryGrant("B","event-1",Part("Gun","Railgun"),"LEG-WPN-001",out e));Eq(e,"REWARD_ALREADY_APPLIED");
            Check(!s.TryGrant("B","event-2",p,"LEG-WPN-001",out e));Eq(e,"DUPLICATE_INSTANCE");
            Check(a.I.Count==1&&b.I.Count==0);
        });
        Test("transfer_not_supported_is_lossless",()=>{
            var a=new MemoryPort("A"){BlockTransfer=true};var b=new MemoryPort("B");var s=Service(a,b);
            var p=Grant(s,a,"Gun");var before=Save(s);
            Check(!s.TryTransfer("A","B",p.InstanceId,out var e));Eq(e,"TRANSFER_HOST_API_PENDING");
            Check(a.I.Count==1&&b.I.Count==0&&Save(s)==before);
        });
        Test("atomic_transfer_when_host_supports",()=>{
            var a=new MemoryPort("A");var b=new MemoryPort("B");var s=Service(a,b);
            var p=Grant(s,a,"Gun");Check(s.TryTransfer("A","B",p.InstanceId,out var e),e);
            Check(a.I.Count==0&&b.I.Count==1);Mount(s,b,p);Check(s.CheckInvariants(out e),e);
        });
        Test("bad_save_rejected_without_partial_application",()=>{
            var a=new MemoryPort("A");var s=Service(a);var p=Grant(s,a,"Gun");
            var good=Save(s);
            var bad=good.Replace("LEG-WPN-001","LEG-UNKNOWN-999");
            Check(!s.TryRestoreMetadata(bad,out var e));Eq(e,"MISSING_OR_MISMATCHED_PHYSICAL_INSTANCE");
            Check(Save(s)==good);
            Check(!s.TryRestoreMetadata("{",out e));Eq(e,"CORRUPT_SAVE");
        });
        Test("corrupt_duplicate_binding_rejected",()=>{
            var a=new MemoryPort("A");var s=Service(a);var p=Grant(s,a,"Gun");
            var payload=new LegendaryPersistence{Bindings=new[]{
                new LegendaryBinding(p.InstanceId,"LEG-WPN-001"),new LegendaryBinding(p.InstanceId,"LEG-WPN-001")
            },RewardEventIds=Array.Empty<string>()};
            Check(!s.TryRestoreMetadata(UnityEngine.JsonUtility.ToJson(payload),out var e));Eq(e,"CORRUPT_OR_DUPLICATE_INSTANCE");
        });
        Test("save_replay_event_remains_idempotent",()=>{
            var a=new MemoryPort("A");var s=Service(a);var p=Grant(s,a,"Gun","once");
            var t=Service(a);Check(t.TryRestoreMetadata(Save(s),out var e),e);
            Check(!t.TryGrant("A","once",Part("Gun","Railgun"),"LEG-WPN-001",out e));
            Eq(e,"REWARD_ALREADY_APPLIED");Check(a.I.Single().InstanceId==p.InstanceId);
        });
        Test("different_robots_same_type_allowed",()=>{
            var a=new MemoryPort("A");var b=new MemoryPort("B");var s=Service(a,b);
            var p=Grant(s,a,"Gun");var q=Grant(s,b,"Gun");Mount(s,a,p);Mount(s,b,q);
            Check(s.CheckInvariants(out var e),e);
        });
        Test("cross_owner_duplicate_guid_disallowed",()=>{
            var a=new MemoryPort("A");var b=new MemoryPort("B");var same=NewId();
            a.I.Add(Part("Gun","Railgun",1,same));b.I.Add(Part("Gun","Railgun",1,same));
            var s=Service(a,b);Check(!s.CheckInvariants(out var e));Eq(e,"DUPLICATE_INSTANCE");
        });
        Test("noncanonical_guid_case_alias_refused",()=>{
            var a=new MemoryPort("A");var b=new MemoryPort("B");
            var id="deadbeef1234567890abcdef12345678";
            a.I.Add(Part("Gun","Railgun",1,id));
            b.I.Add(Part("Gun","Railgun",1,id.ToUpperInvariant()));
            var s=Service(a,b);
            Check(!s.CheckInvariants(out var e));
            Eq(e,"INVALID_R3_VIEW");
            var fresh=new MemoryPort("C");var clean=Service(fresh);
            Check(!clean.TryGrant("C","uppercase-event",
                Part("Gun","Railgun",1,id.ToUpperInvariant()),"LEG-WPN-001",out e));
            Eq(e,"DUPLICATE_INSTANCE");
            Check(fresh.I.Count==0);
        });
        Test("empty_guid_disallowed_as_physical_part",()=>{
            var a=new MemoryPort("A");var s=Service(a);
            Check(!s.TryGrant("A","empty-guid",
                Part("Gun","Railgun",1,Guid.Empty.ToString("N")),"LEG-WPN-001",out var e));
            Eq(e,"DUPLICATE_INSTANCE");
            Check(a.I.Count==0);
        });
        Test("seeded_2500_state_transitions",()=>{
            var a=new MemoryPort("A");var b=new MemoryPort("B");var s=Service(a,b);
            var rnd=new Random(224012);var parts=new List<(PartObservation,MemoryPort)>();
            for(int i=0;i<10;i++){var host=i%2==0?a:b;var kind=i%3==0?"HoverEngine":"Gun";
                parts.Add((Grant(s,host,kind),host));}
            for(int n=0;n<2500;n++){
                var entry=parts[rnd.Next(parts.Count)];var p=entry.Item1;var port=entry.Item2;
                if(port.I.Any(x=>x.InstanceId==p.InstanceId))
                    s.TryEquip(port.RobotId,p.InstanceId,Pos(CertSocket(p.BasePartKind)),out _);
                else {if(n%7==0)port.M.Single(x=>x.InstanceId==p.InstanceId).Health=0;
                    s.TryRepair(port.RobotId,p.InstanceId,out _);
                    s.TryUnequip(port.RobotId,p.InstanceId,out _);}
                Check(s.CheckInvariants(out var e),e);
                if(n%101==0){var t=Service(a,b);Check(t.TryRestoreMetadata(Save(s),out e),e);}
            }
        });
        int passed=Cases.Count(c=>c.Item2);
        var data=new {schema="b01.legendary-unity.surrogate-receipt.v1",
            sourceContractSha="93f290acca26839792a5bec8ea42415ac83932f3",
            nativeUnity="NOT_RUN",passed,failed=Cases.Count-passed,total=Cases.Count,
            cases=Cases.Select(c=>new{id=c.Item1,status=c.Item2?"PASS":"FAIL",error=c.Item3})};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output,JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"RESULT {passed}/{Cases.Count} PASS, failed={Cases.Count-passed}; UNITY=NOT_RUN");
        return passed==Cases.Count?0:1;
    }
}
