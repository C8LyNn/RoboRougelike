// B01-61 W17: executes exact original SELECTED producer #596 Unity MonoBehaviour.
// The only substitute is managed UnityEngine API/hierarchy/float Rigidbody surface.
// NOT NATIVE UNITY EDITOR, NOT PHYSX, NOT a genuine canonical #510 Rig[] restore.
using System;
using System.IO;
using UnityEngine;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;

internal sealed class SecureDebrisPlaygroundV2:MonoBehaviour {}

internal static class Program
{
    const long RUN=51;
    static int assertions,positives,sourceRed;
    static void Check(bool yes,string reason)
    {
        assertions++;
        if(!yes)throw new InvalidOperationException("B0161_EXACT_596_W17_QA_FAILED "+reason);
    }
    static byte[] Bytes(long gen,bool light,bool heavy)=>
        IsolatedTwinRigSaveHost.MakeFixture(RUN,gen,light,heavy);
    static IsolatedTwinRigSaveHost Host(string root,string label)
    {
        Application.persistentDataPath=Path.GetFullPath(root);
        var go=new GameObject(label);
        var host=go.AddComponent<IsolatedTwinRigSaveHost>(); // synthetic test-only Awake lifecycle
        Check(host!=null&&!host.HasWorld,"exact selected Unity source Awake with isolated FileCheckpointStore");
        return host;
    }
    static void Close(IsolatedTwinRigSaveHost host)=>UnityEngine.Object.Destroy(host.gameObject);
    static string Sidecar(string root)=>Path.Combine(Path.GetFullPath(root),"b0123_one_save_host_v2.checkpoint");
    static void State(IsolatedTwinRigSaveHost host,long gen,bool light,bool heavy,string label)
    {
        Check(host.HasWorld&&host.Generation==gen,label+" published original producer generation");
        Check(host.IsConsumed(0,2310001)==light&&host.IsConsumed(1,2310002)==heavy,
            label+" unchanged genuine original W06 stable consumed IDs");
        var bodies=host.gameObject.GetComponentsInChildren<Rigidbody>(true);
        Check(bodies.Length==2,label+" genuine original Unity host created exactly TWO Rigidbody parts");
        foreach(var body in bodies)
        {
            Check(body.gameObject.activeSelf&&body.gameObject.activeInHierarchy,
                label+" original staged Rigidbody was activated");
            bool isLight=body.gameObject.name.EndsWith("2310001",StringComparison.Ordinal);
            bool isHeavy=body.gameObject.name.EndsWith("2310002",StringComparison.Ordinal);
            Check(isLight||isHeavy,label+" precise frozen #510 numeric rig IDs");
            Check(body.isKinematic==(isLight?!light:!heavy),
                label+" original W06 consumed/detached agrees with physics kinematic bit");
        }
    }
    static void Physical(string root)
    {
        for(int mask=0;mask<4;mask++)
        {
            bool l=(mask&1)!=0,h=(mask&2)!=0;
            string local=Path.Combine(root,"rig_"+mask);
            var host=Host(local,"W17 selected #596 physical owned "+mask);
            Check(host.TryRestoreBytes(Bytes(1,l,h),RUN,1),
                "real selected #596 Unity MonoBehaviour TryRestoreBytes initial selected #556 state");
            State(host,1,l,h,"initial mask="+mask);
            Check(host.TrySaveBytes(out byte[] saved)&&saved!=null,
                "actual original host emitted original #556 binary with monotonic Save generation");
            RunSnapshot parsed=null!;
            Check(SnapshotCodec.TryDecode(saved,out parsed)&&parsed!=null&&parsed.Generation==2,
                "actual producer FileCheckpointStore + TrySave emits NEXT generation 2");
            Check(parsed.Parts[0].Id==2310001&&parsed.Parts[1].Id==2310002&&
                  parsed.Parts[0].Consumed==l&&parsed.Parts[1].Consumed==h,
                "original #510 ids and consumed bits persist in real Unity entrypoint bytes");
            var bad=Bytes(2,!l,!h);bad[bad.Length/2]^=0x10;
            Check(!host.TryRestoreBytes(bad,RUN,1)&&
                  host.LastRestoreVerdict==WorldSwapVerdict.InvalidSnapshot,
                "real selected host refuses corrupt checksum without partial commit");
            State(host,1,l,h,"after corrupt refusal mask="+mask);
            Check(!host.TryRestoreBytes(Bytes(1,!l,!h),RUN,1)&&
                  host.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                "same-generation different-world rollback denied with no W06 split");
            State(host,1,l,h,"equal generation refusal mask="+mask);
            Check(!host.TryRestoreBytes(Bytes(2,!l,!h),RUN+1,1)&&
                  host.LastRestoreVerdict==WorldSwapVerdict.WrongRun,
                "wrong authority run rejected on same physical world");
            State(host,1,l,h,"wrong run denial mask="+mask);
            Check(host.TryRestoreBytes(Bytes(2,!l,!h),RUN,1),
                "original new generation accepted after atomic replace/checkpoint commit");
            State(host,2,!l,!h,"next gen mask="+mask);
            Close(host);positives++;
        }
        string collisionRoot=Path.Combine(root,"w07_legacy");
        var legacyHost=Host(collisionRoot,"#596 source W07 block");
        var legacy=new GameObject("foreign controller");
        legacy.AddComponent<SecureDebrisPlaygroundV2>();
        Check(!legacyHost.TryRestoreBytes(Bytes(1,true,false),RUN,1)&&
              legacyHost.LastRestoreVerdict==WorldSwapVerdict.HostBusy,
              "real selected host refuses coinstall with SecureDebrisPlaygroundV2");
        UnityEngine.Object.Destroy(legacy);
        Check(legacyHost.TryRestoreBytes(Bytes(1,true,false),RUN,1),
              "after safely removing W07, selected host may load original selected Save");
        Close(legacyHost);positives++;
        Console.WriteLine("W17_EXACT_SELECTED_596_ORIGINAL_UNITY_MONO_PHYSICAL_SOURCE_PASS states="+
           positives+" assertions="+assertions);
    }
    static void ConcurrentHostAuthority(string root)
    {
        // The same selected Unity MonoBehaviour can be added to separate root
        // GameObjects. [DisallowMultipleComponent] only controls a SINGLE GameObject.
        // Original PhysicalIdle checks frozen W07 names but not another instance.
        string local=Path.Combine(root,"two_hosts_same_slot");
        var old=Host(local,"W17 concurrent old isolated Unity host");
        Check(old.TryRestoreBytes(Bytes(10,true,false),RUN,10),
            "host A loads Light detached gen10 with durable sidecar");
        State(old,10,true,false,"first published W06 state");
        var newer=Host(local,"W17 concurrent new isolated Unity host");
        Check(newer.TryRestoreBytes(Bytes(11,false,true),RUN,11),
            "host B on different GameObject shares SAME checkpoint path and publishes Heavy detached gen11");
        State(newer,11,false,true,"second published W06 state");
        Check(old.HasWorld&&old.Generation==10&&old.IsConsumed(0,2310001)&&
              newer.HasWorld&&newer.Generation==11&&newer.IsConsumed(1,2310002),
              "P0 SOURCE RED: TWO simultaneous active worlds use same durable save authority");
        sourceRed++;
        Console.WriteLine("W17_SELECTED_596_CONCURRENT_HOST_SOURCE_RED=TWO_SIMULTANEOUS_WORLDS_SAME_CAS_SLOT");
        Check(old.TrySaveBytes(out byte[] fromStale)&&fromStale!=null,
            "P0 SOURCE RED: stale old host can emit a HIGHER generation checkpoint from old world");
        RunSnapshot oldPayload=null!;
        Check(SnapshotCodec.TryDecode(fromStale,out oldPayload)&&
              oldPayload.Generation==12&&oldPayload.Parts[0].Consumed&&
              !oldPayload.Parts[1].Consumed,
            "old Light consumed world has been promoted to durable generation12 above newer Heavy world");
        var third=Host(local,"W17 concurrent third host from stale world");
        Check(third.TryRestoreBytes(fromStale,RUN,12),
            "P0 SOURCE RED: stale old world Save can commit and publish higher generation");
        State(third,12,true,false,"stale-world generation escalated");
        Check(newer.HasWorld&&newer.IsConsumed(1,2310002)&&old.HasWorld&&
              third.HasWorld&&third.IsConsumed(0,2310001),
            "source P0 demonstrates three divergent simultaneously active worlds");
        sourceRed++;
        Console.WriteLine("W17_SELECTED_596_STALE_HOST_ESCALATION_RED=OLD_WORLD_GENERATES_NEWER_GEN12_AND_RESTORES");
        Close(old);Close(newer);Close(third);
        Console.WriteLine("W17_EXACT_SELECTED_596_CONCURRENT_HOST_RELEASE_GATE_RED=REPRODUCED "+
             "assertions="+assertions);
    }

    // W18 NEW: original immutable #596 fixed global persistentDataPath sidecar
    // makes an independently authorized run unusable after previous run even
    // AFTER its old MonoBehaviour was retired. This is a source contract
    // regression witness, not evidence of native NUnit failure.
    static void CrossRunIdOwnership(string root)
    {
        string same=Path.Combine(root,"w18_cross_run_collision");
        var first=Host(same,"W18 first independent run");
        Check(first.TryRestoreBytes(Bytes(5,true,false),RUN,5),
              "first original selected Unity host stores run 51 generation5");
        State(first,5,true,false,"cross-run original");
        Check(File.Exists(Sidecar(same)),
              "original actual immutable #596 FileCheckpointStore created shared sidecar");
        Close(first);
        // Different run, new MonoBehaviour instance, no legacy controller:
        // no public API to set a distinct sidecar path, unlike ALT #604.
        const long differentRun=52;
        var newer=Host(same,"W18 independent second run");
        Check(!newer.TryRestoreBytes(
              IsolatedTwinRigSaveHost.MakeFixture(differentRun,1,false,true),
              differentRun,1),
              "selected original host cannot initialize a different run in same dataPath");
        Check(newer.LastRestoreVerdict==WorldSwapVerdict.RollbackQuarantine&&
              newer.Quarantined&&!newer.HasWorld,
              "second clean run is sticky quarantined due to previous run sidecar owner mismatch");
        Check(!newer.TrySaveBytes(out byte[] refused)&&refused==null,
              "quarantined independent run cannot save");
        Check(File.ReadAllText(Sidecar(same)).Contains("|51|"),
              "original first run sidecar remains unchanged and belongs to first authority");
        sourceRed++;
        Console.WriteLine("W18_SELECTED_596_NEW_RUN_STICKY_QUARANTINE_RED=GLOBAL_FIXED_SIDECAR_COLLISION");
        Close(newer);
        Console.WriteLine("W18_EXACT_SELECTED_596_CROSS_RUN_ID_QA_PASS assertions="+assertions+
           " source_release_red="+sourceRed);
    }
    static void Disk(string mode,string root)
    {
        var local=Path.Combine(root,"single_slot_restarts");
        var h=Host(local,"W17 selected #596 fresh process "+mode);
        var sidecar=Sidecar(local);
        var snapshot=Path.Combine(root,"w17-original-556-payload.bin");
        if(mode=="seal")
        {
            Check(!File.Exists(sidecar),"new owned disk sidecar starts absent");
            Check(h.TryRestoreBytes(Bytes(7,true,false),RUN,7),
                  "exact original Unity source initial binary #556 Save state accepted");
            State(h,7,true,false,"fresh durable seal");
            Check(File.Exists(sidecar),"original #596 file-backed sidecar created");
            Check(h.TrySaveBytes(out byte[] bytes)&&bytes!=null,
                  "actual Unity host TrySaveBytes emits generation8 pending checkpoint");
            RunSnapshot parsed=null!;
            Check(SnapshotCodec.TryDecode(bytes,out parsed)&&parsed.Generation==8&&
                  parsed.Parts[0].Id==2310001&&parsed.Parts[0].Consumed,
                  "real original #556 snapshot advances generation and W06 consumed ID");
            File.WriteAllBytes(snapshot,bytes); // scratch fixture only, NOT producer atomic Save writer
            Check(File.Exists(snapshot),"true OS Save binary written to owned disposable file");
            Console.WriteLine("W17_SELECTED_596_ORIGINAL_UNITY_HOST_REAL_SAVE_DISK_PASS");
        }
        else if(mode=="restart")
        {
            Check(File.Exists(snapshot)&&File.Exists(sidecar),
                  "independent fresh process sees original Save+sidecar files");
            Check(h.TryRestoreBytes(File.ReadAllBytes(snapshot),RUN,8),
                  "real original #596 Unity component fresh process loads original saved generation8 bytes");
            State(h,8,true,false,"original binary cold restarted");
            Console.WriteLine("W17_SELECTED_596_REAL_DISK_CROSS_PROCESS_W06_ID_GREEN");
        }
        else if(mode=="fork")
        {
            Check(!h.TryRestoreBytes(Bytes(8,false,true),RUN,8)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                  "different same generation binary refused using original durable SHA sidecar");
            Check(!h.HasWorld&&!h.Quarantined,
                  "fork denied before GameObjects created, without needless quarantine");
            Check(!h.TryRestoreBytes(Bytes(7,true,false),RUN,7)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                  "old gen7 refused across CLR processes");
            Console.WriteLine("W17_SELECTED_596_REAL_DISK_FORK_AND_STALE_REFUSED_GREEN");
        }
        else if(mode=="advance")
        {
            Check(h.TryRestoreBytes(Bytes(9,false,true),RUN,8),
                  "new generation9 original Unity host and genuine W06 ledger published");
            State(h,9,false,true,"generation9 heavy consumed");
            Console.WriteLine("W17_SELECTED_596_REAL_DISK_WATERMARK_GENERATION9_GREEN");
        }
        else if(mode=="stale")
        {
            Check(!h.TryRestoreBytes(File.ReadAllBytes(snapshot),RUN,8)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                  "new process refuses earlier generation8 snapshot after durable generation9");
            Check(!h.HasWorld,"old snapshot cannot stage original physical hierarchy");
            Console.WriteLine("W17_SELECTED_596_OLD_SAVE_FILE_REPLAY_REFUSED_GREEN");
        }
        else if(mode=="corrupt")
        {
            string data=File.ReadAllText(sidecar);
            Check(data.StartsWith("B0123CHK2|",StringComparison.Ordinal),
                  "actual original producer file sidecar header present");
            File.WriteAllText(sidecar,"CORRUPT|"+data.Substring(8));
            Check(!h.TryRestoreBytes(Bytes(10,true,false),RUN,9)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.RollbackQuarantine,
                  "unverifiable original sidecar gives FIRST-CALL quarantine");
            Check(h.Quarantined&&!h.HasWorld,"faulty durable sidecar protects world from install");
            Check(!h.TrySaveBytes(out byte[] denied)&&denied==null,
                  "quarantined host rejects further Save");
            Console.WriteLine("W17_SELECTED_596_CORRUPT_DURABLE_SIDECAR_QUARANTINE_GREEN");
        }
        else throw new ArgumentException("bad QA mode");
        Close(h);
        Console.WriteLine("W17_EXACT_SELECTED_596_PUBLIC_HOST_PROCESS_PASS mode="+mode+
          " assertions="+assertions+" native_unity=NOT_RUN");
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length==2 && args[0]=="physical")Physical(args[1]);
            else if(args.Length==2 && args[0]=="concurrent")ConcurrentHostAuthority(args[1]);
            else if(args.Length==2 && args[0]=="cross-run")CrossRunIdOwnership(args[1]);
            else if(args.Length==2)Disk(args[0],args[1]);
            else throw new ArgumentException("usage [physical|concurrent|seal|restart|fork|advance|stale|corrupt] scratch");
            Console.WriteLine("B0161_W17_SELECTED_596_EXACT_ORIGINAL_UNITY_HOST_EXECUTED=YES");
            Console.WriteLine("UNITY_ENGINE_BOUNDARY=SYNTHETIC; REAL_UNITY_G1_NOT_RUN; GAME_510_LIVE_SAVELOAD_HOLD");
            return 0; // Positive and source RED witnesses; RELEASE remains HOLD.
        }
        catch(Exception ex){Console.Error.WriteLine("W17_SELECTED_596_INDEPENDENT_QA_FAILURE "+ex);return 1;}
    }
}
