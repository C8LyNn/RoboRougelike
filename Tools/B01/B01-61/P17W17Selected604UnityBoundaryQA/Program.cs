// B01-61 P17 W17: DIRECT exact frozen #604 Unity MonoBehaviour compiled + executed.
// SyntheticUnityEngine models hierarchy/API ONLY; this is never genuine Unity/PhysX.
// In 7 independent CLR processes we test actual W16 Unity-host integration with
// the original W16 AtomicWorldSwap, FileCheckpointFence and frozen #556+W06.
using System;
using System.IO;
using UnityEngine;
using B01.DetachedSaveContract001;
using B01.UnitySaveBridge001;

internal sealed class SecureDebrisPlaygroundV2:MonoBehaviour {} // type-name collision guard only

internal static class Program
{
    private const long RUN=51;
    private static int assertions,positive,denials,sourceRed;
    static void Check(bool yes,string why)
    {
        assertions++;
        if(!yes)throw new Exception("INDEPENDENT_ORIGINAL_604_UNITY_BOUNDARY_QA_FAIL: "+why);
    }
    static IsolatedTwinRigSaveHost Make(string label)
    {
        var game=new GameObject(label);
        var host=game.AddComponent<IsolatedTwinRigSaveHost>(); // source Awake simulated
        Check(host!=null && !host.HasWorld && !host.PersistentCheckpointEnabled,
            "exact actual MonoBehaviour awake and volatile default");
        return host;
    }
    static void DestroyHost(IsolatedTwinRigSaveHost host)=>UnityEngine.Object.Destroy(host.gameObject);
    static byte[] Bytes(long gen,bool light,bool heavy)=>
        IsolatedTwinRigSaveHost.MakeFixture(RUN,gen,light,heavy);
    static void State(IsolatedTwinRigSaveHost h,long gen,bool light,bool heavy,string label)
    {
        Check(h.HasWorld && h.Generation==gen,label+" original MonoBehaviour committed");
        Check(h.IsConsumed(0,2310001)==light&&h.IsConsumed(1,2310002)==heavy,
              label+" original W06 Light and Heavy consumed stable ID");
        Rigidbody[] rb=h.gameObject.GetComponentsInChildren<Rigidbody>(true);
        Check(rb.Length==2,label+" exactly TWO original staged Unity Rigidbody objects");
        foreach(var body in rb)
        {
            Check(body.gameObject.activeInHierarchy,
                  label+" detached/attached Rigidbody actually active in fake scene hierarchy");
            Check(body.gameObject.activeSelf,
                  label+" staged child local active state restored on world publication");
            bool isLight=body.gameObject.name.EndsWith("2310001",StringComparison.Ordinal);
            bool isHeavy=body.gameObject.name.EndsWith("2310002",StringComparison.Ordinal);
            Check(isLight||isHeavy,label+" canonical numeric original game IDs on prefab parts");
            Check(body.isKinematic==(isLight?!light:!heavy),
                  label+" kin-state equals detached/attached original frozen #556");
        }
    }
    static void PhysicalFixture()
    {
        for(int mask=0;mask<4;mask++)
        {
            bool light=(mask&1)!=0,heavy=(mask&2)!=0;
            var h=Make("W17 exact #604 physical fixture "+mask);
            byte[] original=Bytes(1,light,heavy);
            Check(h.TryRestoreBytes(original,RUN,1),
                  "actual original #604 MonoBehaviour TryRestoreBytes valid 2-rig fixture");
            State(h,1,light,heavy,"physical mask="+mask);
            Check(h.TrySaveBytes(out byte[] fresh)&&fresh!=null,
                  "real actual unity-host .TrySaveBytes with VOLATILE demo must produce #556 data");
            RunSnapshot decoded=null!;
            Check(SnapshotCodec.TryDecode(fresh,out decoded)&&decoded!=null,
                  "exact original frozen #556 codec decodes Unity MonoBehaviour capture");
            Check(decoded.Parts[0].Consumed==light&&decoded.Parts[1].Consumed==heavy &&
                  decoded.Parts[0].Id==2310001&&decoded.Parts[1].Id==2310002,
                  "returned bytes contain correct exact canonical IDs and W06 coupling");
            byte[] invalid=Bytes(2,!light,!heavy);invalid[invalid.Length/2]^=0x04;
            Check(!h.TryRestoreBytes(invalid,RUN,1)&&h.LastRestoreVerdict==WorldSwapVerdict.InvalidSnapshot,
                 "tampered SHA256 rejects without physical publication");
            State(h,1,light,heavy,"after corrupt denial mask="+mask);
            Check(!h.TryRestoreBytes(Bytes(1,!light,!heavy),RUN,1)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                  "different same-gen world rejected without physical publication");
            State(h,1,light,heavy,"samegen denial mask="+mask);
            Check(!h.TryRestoreBytes(Bytes(2,!light,!heavy),RUN+1,1)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.WrongRun,
                  "foreign RunId rejected without world mutation");
            State(h,1,light,heavy,"foreign run denial mask="+mask);
            Check(h.TryRestoreBytes(Bytes(2,!light,!heavy),RUN,1),
                 "later generation atomically replaces exactly one demo active world");
            State(h,2,!light,!heavy,"newgeneration mask="+mask);
            Check(!h.UsePersistentCheckpointSlot("do_not_swap_after_commit"),
                 "real MonoBehaviour cannot switch checkpoint authority with live world");
            DestroyHost(h);
            positive++;
        }
        var old=Make("W17 legacy collision");
        var marker=new GameObject("foreign W07 controller");
        marker.AddComponent<SecureDebrisPlaygroundV2>();
        Check(!old.TryRestoreBytes(Bytes(1,true,false),RUN,1)&&
              old.LastRestoreVerdict==WorldSwapVerdict.HostBusy,
              "actual PhysicalIdle detects foreign SecureDebrisPlaygroundV2 component");
        UnityEngine.Object.Destroy(marker);
        Check(old.TryRestoreBytes(Bytes(1,true,false),RUN,1),
              "original host can continue only after legacy has been removed");
        DestroyHost(old);
        positive++;
        // An AUTHORED ORIGINAL native NUnit test calls UsePersistentCheckpointSlot
        // with "W16Native_" + GUID N (42 chars) but actual API allows <=32 chars.
        // No fake NUnit execution is claimed; direct public API witnesses contradiction.
        var malformed=Make("W17 native-authored-slot-contract");
        string authoredNativeSlot="W16Native_"+Guid.NewGuid().ToString("N");
        Check(authoredNativeSlot.Length==42,
              "original native test source constructs exactly 42-character slot");
        Check(!malformed.UsePersistentCheckpointSlot(authoredNativeSlot)&&
              !malformed.PersistentCheckpointEnabled,
              "original 604 Unity API rejects what its OWN authored native test expects accepted");
        sourceRed++;
        Console.WriteLine("W17_ORIGINAL_604_NATIVE_AUTHORED_PERSISTENT_SLOT_RED=42_CHAR_TEST_NAME_REJECTED_BY_32_CHAR_API");
        Check(!malformed.UsePersistentCheckpointSlot("../evil")&&
              !malformed.UsePersistentCheckpointSlot("x/y")&&
              !malformed.UsePersistentCheckpointSlot(" ")&&
              !malformed.UsePersistentCheckpointSlot(""),
              "exact selected host refuses path traversal/special/empty slot names");
        denials+=4;
        DestroyHost(malformed);
        Console.WriteLine("W17_ORIGINAL_604_ACTUAL_UNITY_HOST_MANAGED_SOURCE_QA_PASS physical_cases="+positive+
           " invalid_slot_denials="+denials+" native_test_contract_red="+sourceRed+
           " assertions="+assertions);
    }
    static void Durable(string mode,string root)
    {
        // Actual original Unity host uses Application.persistentDataPath and FIXED
        // subfolder. The own GitHub runner generates a disposable scratch directory.
        Application.persistentDataPath=Path.GetFullPath(root);
        const string SLOT="W17_Selected604_Fence";
        var path=Path.Combine(Application.persistentDataPath,
            "B0123UnitySaveBridge001","W16CheckpointFences",SLOT+".fence");
        var h=Make("W17 persistent host "+mode);
        Check(h.UsePersistentCheckpointSlot(SLOT)&&h.PersistentCheckpointEnabled,
            "exact original public Unity component opts into real original FileCheckpointFence");
        if(mode=="seal")
        {
            Check(!File.Exists(path),"own real watermark file absent on clean first host");
            Check(h.TryRestoreBytes(Bytes(7,true,false),RUN,7),
                  "real Unity-facing host accepts original #556 Light consumed");
            State(h,7,true,false,"real fence seal");
            Check(File.Exists(path)&&new FileInfo(path).Length==88,
                  "file fence written by ACTUAL host source and original SHA256 sealer");
            // Novel: exact original MonoBehaviour reads double state through Unity
            // float Vector3, then reencodes double. This can change equal-gen hash
            // even when no gameplay motion has occurred.
            bool exactSave=h.TrySaveBytes(out byte[] roundtrip);
            if(!exactSave)
            {
                Check(roundtrip==null,
                    "durable host refuses even unmodified world equal-generation due to float precision");
                sourceRed++;
                Console.WriteLine("W17_ORIGINAL_604_UNCHANGED_WORLD_DURABLE_SAVE_RED=FLOAT_VECTOR3_TO_DOUBLE_HASH_DRIFT");
            }
            else
            {
                Check(roundtrip!=null&&SnapshotCodec.TryDecode(roundtrip,out RunSnapshot s)&&s!=null,
                      "if canonical rounded bytes were identical, durable Save was allowed");
                Console.WriteLine("W17_ORIGINAL_604_UNCHANGED_WORLD_DURABLE_SAVE_GREEN=IDENTICAL_CANONICAL_HASH");
            }
            Console.WriteLine("W17_ORIGINAL_604_PUBLIC_HOST_DURABLE_FENCE_SEAL_PASS");
        }
        else if(mode=="restart")
        {
            Check(File.Exists(path),"NEW process observes actual 88byte disk watermark");
            Check(h.TryRestoreBytes(Bytes(7,true,false),RUN,7),
                  "NEW CLR process original Unity MonoBehaviour cold restores same hash");
            State(h,7,true,false,"restarted disk fence host");
            Console.WriteLine("W17_ORIGINAL_604_PUBLIC_HOST_FRESH_PROCESS_RESTORE_PASS");
        }
        else if(mode=="fork")
        {
            Check(!h.TryRestoreBytes(Bytes(7,false,true),RUN,7)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                  "different same-run+gen checkpoint refused on NEW Unity component");
            Check(!h.HasWorld&&!h.Quarantined,
                  "fork rejects before new Unity world staged and no false quarantine");
            Check(!h.TryRestoreBytes(Bytes(6,true,false),RUN,6)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                  "persistent original fence refuses stale generation on fresh host");
            Console.WriteLine("W17_ORIGINAL_604_PUBLIC_HOST_FRESH_PROCESS_FORK_REFUSAL_PASS");
        }
        else if(mode=="advance")
        {
            Check(h.TryRestoreBytes(Bytes(8,false,true),RUN,7),
                  "fresh Unity host publishes strictly later checkpoint generation");
            State(h,8,false,true,"real disk fence advanced");
            Check(new FileInfo(path).Length==88,"real disk watermark still 88 bytes");
            Console.WriteLine("W17_ORIGINAL_604_PUBLIC_HOST_GEN8_FENCE_ADVANCE_PASS");
        }
        else if(mode=="stale")
        {
            Check(!h.TryRestoreBytes(Bytes(7,true,false),RUN,7)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.StaleGeneration,
                  "cold restart refuses previous valid gen7 after gen8 was committed");
            Check(!h.HasWorld,"stale world did not get GameObjects");
            Console.WriteLine("W17_ORIGINAL_604_PUBLIC_HOST_OLD_GENERATION_REPLAY_DENIED_PASS");
        }
        else if(mode=="corrupt")
        {
            byte[] data=File.ReadAllBytes(path);
            Check(data.Length==88,"actual 88byte fence exists before scratch corruption");
            data[48]^=0x40;File.WriteAllBytes(path,data);
            Check(!h.TryRestoreBytes(Bytes(9,true,false),RUN,8)&&
                  h.LastRestoreVerdict==WorldSwapVerdict.RollbackQuarantine,
                  "actual Unity MonoBehaviour first call publishes quarantine verdict");
            Check(h.Quarantined&&!h.HasWorld,"unverifiable file prevents any Unity demo world");
            Check(!h.TrySaveBytes(out byte[] denied)&&denied==null,
                  "quarantined host cannot write new Save");
            Console.WriteLine("W17_ORIGINAL_604_PUBLIC_HOST_CORRUPTED_WATERMARK_QUARANTINE_PASS");
        }
        else throw new ArgumentException("unexpected mode");
        DestroyHost(h);
        Console.WriteLine("W17_ORIGINAL_604_MANAGED_UNITY_HOST_PROCESS_PASS mode="+mode+
           " assertions="+assertions+" source_red="+sourceRed);
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length==1 && args[0]=="physical")PhysicalFixture();
            else if(args.Length==2)Durable(args[0],args[1]);
            else throw new ArgumentException("usage physical OR [seal|restart|fork|advance|stale|corrupt] <disposable-directory>");
            Console.WriteLine("EXACT_ORIGINAL_604_UNITY_MONOBEHAVIOUR_COMPILED_EXECUTED=YES");
            Console.WriteLine("ACTUAL_UNITY_EDITOR_PHYSX=NOT_RUN; FAKE_ENGINE_BOUNDARY=YES; REAL_510_HOST=NOT_INTEGRATED");
            return 0; // Source-red witness == QA pass, NOT native Unity or Save release GO.
        }
        catch(Exception e){Console.Error.WriteLine("W17_INDEPENDENT_SOURCE_QA_FAILURE "+e);return 1;}
    }
}
