// Independent B01-61 W19: execute ACTUAL R3 #642 MonoBehaviour OnDisable/OnEnable,
// OS lease + original #556 save + original W06, test-only UnityEngine boundary.
// Unity's disabled MonoBehaviour does NOT deactivate its GameObject or Rigidbody.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;

internal static class LifecycleProgram
{
    static int assertions;
    static void Need(bool result,string message)
    {
        assertions++;
        if(!result)throw new Exception("R3_INDEPENDENT_LIFECYCLE_QA_FAIL "+message);
    }
    static void InvokeLifecycle(IsolatedTwinRigSaveHost host,string name)
    {
        var method=typeof(IsolatedTwinRigSaveHost).GetMethod(name,
              BindingFlags.NonPublic|BindingFlags.Instance);
        Need(method!=null,"exact original MonoBehaviour lifecycle callback exists "+name);
        method!.Invoke(host,null); // QA ONLY, emulates Unity OnDisable/OnEnable event
    }
    static IsolatedTwinRigSaveHost NewHost(string name)
    {
        var go=new GameObject(name);
        var h=go.AddComponent<IsolatedTwinRigSaveHost>();
        Need(!h.HasWorld,"source fixture begins without physical world");
        return h;
    }
    static int ActiveRigidbodyCount(params IsolatedTwinRigSaveHost[] hosts)
        =>hosts.SelectMany(h=>h.gameObject.GetComponentsInChildren<Rigidbody>(true))
              .Count(x=>x.gameObject.activeInHierarchy);
    static void Run()
    {
        var scratch=Path.Combine(Path.GetTempPath(),"b0161-w19-r3-642-unity-lease-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        Application.persistentDataPath=scratch;
        var slot=Guid.Parse("1256d7f0-25ca-47c3-97b1-0145fddca508");
        try
        {
            var a=NewHost("R3 old root A");
            Need(a.TrySelectPersistentRunSlot(51,slot),
                 "actual owner host A chooses explicit run and stable slot");
            Need(a.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,10,true,false),51,10),
                 "real R3 Unity MonoBehaviour publishes Light consumed gen10");
            Need(a.IsConsumed(0,2310001)&&!a.IsConsumed(1,2310002),
                 "original #556/W06 Light ID after initial committed public Restore");
            Need(ActiveRigidbodyCount(a)==2,"old world two active rigidbody parts");
            var b=NewHost("R3 contender root B");
            Need(!b.TrySelectPersistentRunSlot(51,slot),
                 "original FileShare.None owner lock refuses active same-slot contender");
            Need(!b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11)&&
                 b.LastRestoreVerdict==WorldSwapVerdict.HostBusy,
                 "no physical contender world with A current owning slot");
            Need(ActiveRigidbodyCount(a,b)==2,"leased host exclusion initially works");
            // Unity disables only MonoBehaviour, NOT attached Rigidbody GameObjects.
            a.enabled=false;
            InvokeLifecycle(a,"OnDisable");
            Need(a.gameObject.activeInHierarchy&&ActiveRigidbodyCount(a)==2,
                 "disabled component leaves original owned physical hierarchy active");
            Need(!b.TrySelectPersistentRunSlot(51,slot),
                 "R4 OnDisable retains original FileShare.None lease while physics survives");
            Need(!b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11)&&
                 b.LastRestoreVerdict==WorldSwapVerdict.HostBusy,
                 "same slot B blocked with live old world under disabled component");
            Need(ActiveRigidbodyCount(a,b)==2&&!b.HasWorld,
                 "R4 excludes second world after only MonoBehaviour disabled");
            Console.WriteLine("W19_R4_647_ONDISABLE_KEEPS_LIVE_WORLD_SLOT_LEASE_GREEN=EXCLUSIVE");
            a.enabled=true;
            InvokeLifecycle(a,"OnEnable");
            Need(a.TrySaveBytes(out byte[] saved)&&saved!=null,
                 "R4 original owner remains authorized after re-enable");
            Need(SnapshotCodec.TryDecode(saved,out RunSnapshot checkedSave)&&
                 checkedSave!=null&&checkedSave.Parts[0].Consumed,
                 "original #556 Light consumed ID persisted through script disable/enable");
            InvokeLifecycle(a,"OnDestroy");
            UnityEngine.Object.Destroy(a.gameObject);
            Need(ActiveRigidbodyCount(a,b)==0,
                 "old world no longer physically live after full owner destroy");
            Need(b.TrySelectPersistentRunSlot(51,slot),
                 "B can acquire real OS slot lock AFTER legitimate OnDestroy");
            Need(b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11),
                 "new R4 host restores different Heavy consumed gen11 after takeover");
            Need(b.HasWorld&&b.IsConsumed(1,2310002)&&!b.IsConsumed(0,2310001)&&
                 ActiveRigidbodyCount(b)==2,
                 "exact original W06 state one live world after takeover");
            Console.WriteLine("W19_R4_647_HOST_TAKEOVER_ONLY_AFTER_ONDESTROY_GREEN=PASS");
            InvokeLifecycle(b,"OnDestroy");
            UnityEngine.Object.Destroy(b.gameObject);
            // Independent RunId 52+distinct owned GUID must still be valid.
            var c=NewHost("R3 independent run C");
            Need(c.TrySelectPersistentRunSlot(52,slot),
                 "per-run original path does not intersect historical run51 slot");
            Need(c.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(52,1,false,true),52,1),
                 "new unrelated run52 genuine original #556 physical scene restores");
            Need(c.IsConsumed(1,2310002)&&ActiveRigidbodyCount(c)==2,
                 "new run durable slot keeps correct Heavy consumed ID");
            InvokeLifecycle(c,"OnDestroy");
            UnityEngine.Object.Destroy(c.gameObject);
            Console.WriteLine("W19_R4_647_PER_RUN_52_PUBLIC_HOST_GREEN=ISOLATED");
            Console.WriteLine("W19_R4_647_EXACT_ORIGINAL_MONOBEHAVIOUR_SOURCE_QA_PASS assertions="+assertions);
            Console.WriteLine("SOURCE_SECURITY=R4_OWNER_LIFETIME_AND_RUN_ISOLATION_PASS");
            Console.WriteLine("NATIVE_UNITY_ENGINE_PHYSX=NOT_RUN; SYNTHETIC_UNITY_API_ONLY");
        }
        finally
        {
            try{Directory.Delete(scratch,true);}catch(IOException){}
        }
    }
    public static int Main()
    {
        try{Run();return 0;}
        catch(Exception e){Console.Error.WriteLine("W19_ORIGINAL_R3_642_UNITY_LIFECYCLE_FAIL "+e);return 1;}
    }
}
