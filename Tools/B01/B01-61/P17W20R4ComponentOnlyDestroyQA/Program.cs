// W20: independently run UNMODIFIED exact owner R4 #647 original Unity
// MonoBehaviour, original R4 core, frozen #556 and W06, REAL OS slot lease.
// Only UnityEngine API and its component-destroy callback are TEST shims.
// NOT native Unity/PhysX; this does not reach frozen #510 private fields.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
internal static class Program
{
    static int passed,failed;
    static void Check(string tag,bool result)
    {
        if(result){passed++;Console.WriteLine("PASS "+tag);}
        else{failed++;Console.WriteLine("FAIL "+tag);}
    }
    static IsolatedTwinRigSaveHost New(string id)
        =>new GameObject(id).AddComponent<IsolatedTwinRigSaveHost>();
    static int Live(params IsolatedTwinRigSaveHost[] hs)
        =>hs.SelectMany(x=>x.gameObject.GetComponentsInChildren<Rigidbody>(true))
              .Count(x=>x.gameObject.activeInHierarchy);
    static void ComponentOnlyScenario(string root)
    {
        var id=Guid.Parse("3ad8e627-647a-4ff8-8000-000000000051");
        var a=New("COMPONENT_ONLY_A");
        Check("A public original host acquired exact run51 GUID",
              a.TrySelectPersistentRunSlot(51,id));
        Check("A original Light gen10 loaded",
              a.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,10,true,false),51,10));
        Check("A original W06 Light ID consumed",
              a.IsConsumed(0,2310001)&&!a.IsConsumed(1,2310002)&&Live(a)==2);
        var b=New("COMPONENT_ONLY_B");
        Check("B same slot initially refused",
              !b.TrySelectPersistentRunSlot(51,id));
        Check("B cannot publish before owner retired",
              !b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11)&&
              !b.HasWorld&&Live(a,b)==2);
        // Equivalent of UnityEngine.Object.Destroy(a) (the MonoBehaviour
        // component), NOT Object.Destroy(a.gameObject). The original R4's
        // OnDisable+OnDestroy lifecycle runs, but its CHILD WORLD STAYS LIVE.
        UnityEngine.Object.Destroy((Component)a);
        Check("R4 component-only destruction does NOT remove hierarchy",
              a.gameObject.activeInHierarchy&&Live(a,b)==2&&a.HasWorld);
        Check("Destroyed original host no longer included in component scan",
              !UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                  FindObjectsInactive.Include,FindObjectsSortMode.None).Contains(a));
        Check("Other B obtains same durable owner slot after R4 component destroyed",
              b.TrySelectPersistentRunSlot(51,id));
        Check("Other B can publish Heavy world gen11 with original bytes",
              b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11));
        Check("Both conflicting original Light and Heavy W06 worlds active",
              a.HasWorld&&b.HasWorld&&a.Generation==10&&b.Generation==11&&
              a.IsConsumed(0,2310001)&&b.IsConsumed(1,2310002)&&
              Live(a,b)==4);
        Check("B current world can export while orphan Light Rigidbody lives",
              b.TrySaveBytes(out byte[] exported)&&
              SnapshotCodec.TryDecode(exported,out RunSnapshot value)&&
              value!=null&&value.RunId==51&&value.Generation==12&&
              value.Parts[1].Consumed);
        Console.WriteLine("W20_EXACT_R4_COMPONENT_ONLY_ONDESTROY_LEASE_LOST_WITH_LIVE_CHILDREN_RED=REPRODUCED");
        Console.WriteLine("W20_EXACT_R4_SECOND_HOST_DUPLICATE_PHYSICAL_WORLDS_RED=REPRODUCED");
        Console.WriteLine("W20_EXACT_R4_SECOND_HOST_SAVE_WITH_ORPHAN_LIGHT_RED=REPRODUCED");
        UnityEngine.Object.Destroy(a.gameObject);
        UnityEngine.Object.Destroy((Component)b);
        UnityEngine.Object.Destroy(b.gameObject);
    }
    static void WholeGameObjectControl()
    {
        var id=Guid.Parse("3ad8e627-647a-4ff8-8000-000000000052");
        var a=New("CONTROL_FULL_A");
        Check("Control A owner acquired",a.TrySelectPersistentRunSlot(52,id));
        Check("Control A Heavy loaded",
              a.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(52,1,false,true),52,1));
        Check("Control A two active bodies",Live(a)==2);
        // Physically remove ALL owned objects before giving up the slot.
        UnityEngine.Object.Destroy(a.gameObject);
        Check("Control full-root destruction removes physical bodies",Live(a)==0);
        // Explicit original OnDestroy callback for the root owner
        // after its physics was retired. No source alteration.
        typeof(IsolatedTwinRigSaveHost)
          .GetMethod("OnDestroy",System.Reflection.BindingFlags.NonPublic|
                                    System.Reflection.BindingFlags.Instance)!
          .Invoke(a,null);
        var b=New("CONTROL_FULL_B");
        Check("Control legitimate B same run+slot after world deletion",
              b.TrySelectPersistentRunSlot(52,id));
        Check("Control B original W06 Heavy persisted on cold restart",
              b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(52,1,false,true),52,1)&&
              b.IsConsumed(1,2310002)&&Live(b)==2);
        UnityEngine.Object.Destroy((Component)b);
        UnityEngine.Object.Destroy(b.gameObject);
    }
    public static int Main()
    {
        var root=Path.Combine(Path.GetTempPath(),"B0161W20-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Application.persistentDataPath=root;
        try
        {
            ComponentOnlyScenario(root);
            WholeGameObjectControl();
            Console.WriteLine("W20_R4_647_ORIGINAL_PUBLIC_HOST_SOURCE_ASSERTIONS PASS="+passed+" FAIL="+failed);
            Console.WriteLine("SOURCE_QA_WITNESS="+(failed==0?"PASS_EXPECTED_P0_RED":"FAIL"));
            Console.WriteLine("R4_SECURITY_RELEASE="+(failed==0?"HOLD_COMPONENT_ONLY_DESTROY_ORPHAN_PHYSICS":"UNRESOLVED"));
            Console.WriteLine("NATIVE_UNITY_PHYSX=NOT_RUN; CANONICAL_510_LEDGER=HOLD; G2=HOLD");
            return failed==0?0:1;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine("W20_R4_647_QA_UNEXPECTED_EXCEPTION "+ex);
            return 1;
        }
        finally {try{Directory.Delete(root,true);}catch(IOException){}}
    }
}
