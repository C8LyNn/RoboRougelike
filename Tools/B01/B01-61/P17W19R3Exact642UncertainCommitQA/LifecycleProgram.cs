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
            // Unity disables only COMPONENT, not its GameObject, Rigidbody children.
            a.enabled=false;
            InvokeLifecycle(a,"OnDisable");
            Need(a.gameObject.activeInHierarchy&&ActiveRigidbodyCount(a)==2,
                 "original OnDisable releases lease while old physical child parts STAY ACTIVE");
            Need(b.TrySelectPersistentRunSlot(51,slot),
                 "B acquired same original FileShare.None lease after A OnDisable");
            Need(b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11),
                 "B publishes different Heavy consumed world at higher durable gen11");
            Need(a.HasWorld&&b.HasWorld&&a.Generation==10&&b.Generation==11&&
                 a.IsConsumed(0,2310001)&&b.IsConsumed(1,2310002),
                 "A and B original public hosts both hold different original W06 ledgers");
            Need(ActiveRigidbodyCount(a,b)==4,
                 "SOURCE RELEASE P0: two conflicting PHYSICAL worlds are still simultaneously active");
            Console.WriteLine("W19_R3_642_DISABLE_RELEASE_LEASE_TWO_LIVE_WORLDS_RED=REPRODUCED");
            Console.WriteLine("W19_R3_642_ONDISABLE_PHYSICS_STILL_ACTIVE_RED=REPRODUCED");
            // Even when A is reenabled, B keeps the lease; A cannot write stale.
            a.enabled=true;
            InvokeLifecycle(a,"OnEnable");
            Need(!a.TrySaveBytes(out byte[] oldSave)&&oldSave==null,
                 "new R3 original published generation/hash fence refuses old host export");
            Need(ActiveRigidbodyCount(a,b)==4,
                 "R3 reenable of stale A does NOT deactivate abandoned original world");
            Console.WriteLine("W19_R3_642_STALE_HOST_EXPORT_AFTER_REENABLE_GREEN=DENIED_BUT_PHYSICS_RED");
            // Actual B's PhysicalIdle blocks when enabled old A is visible, 
            // so in addition to double world one gets a physical liveness/deadlock.
            Need(!b.TrySaveBytes(out byte[] currentSave)&&currentSave==null,
                 "B active owner sees A enabled+HasWorld; cannot Save while stale A exists");
            Console.WriteLine("W19_R3_642_TWO_HOSTS_CURRENT_OWNER_SAVEBLOCK_RED=REPRODUCED");
            InvokeLifecycle(a,"OnDestroy");
            InvokeLifecycle(b,"OnDestroy");
            UnityEngine.Object.Destroy(a.gameObject);
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
            Console.WriteLine("W19_R3_642_PER_RUN_52_PUBLIC_HOST_GREEN=ISOLATED");
            Console.WriteLine("W19_R3_642_EXACT_ORIGINAL_MONOBEHAVIOUR_SOURCE_QA_PASS assertions="+assertions);
            Console.WriteLine("RELEASE_SECURITY=HOLD_LIFETIME_PHYSICS_WITHOUT_SLOT_LEASE");
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
