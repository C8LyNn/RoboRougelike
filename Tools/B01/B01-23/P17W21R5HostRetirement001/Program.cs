// B01-23 W21: actual NEW R5 MonoBehaviour/core and original #556/W06
// compiled under a SYNTHETIC UnityEngine only. Never call this native G1.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
internal static class Program
{
    static int pass,fail;
    static void Check(string label,bool ok)
    {if(ok){pass++;Console.WriteLine("PASS "+label);}
     else{fail++;Console.WriteLine("FAIL "+label);}}
    static IsolatedTwinRigSaveHost Host(string name)
        =>new GameObject(name).AddComponent<IsolatedTwinRigSaveHost>();
    static int Bodies(GameObject game)=>game.GetComponentsInChildren<Rigidbody>(true)
        .Count(x=>x.gameObject.activeInHierarchy);
    static void ComponentOnlyDestroyAndRecover()
    {
        Guid slot=Guid.NewGuid();
        var a=Host("R5 original Light owner");
        Check("owner acquired original slot",a.TrySelectPersistentRunSlot(51,slot));
        Check("old Light gen10 actual original 556 restore",
            a.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,10,true,false),51,10));
        Check("old Light W06 stable ID",a.IsConsumed(0,2310001)&&Bodies(a.gameObject)==2);
        var b=Host("R5 contender Heavy");
        Check("contender same slot denied while original world active",
            !b.TrySelectPersistentRunSlot(51,slot));
        UnityEngine.Object.Destroy((Component)a);
        Check("component-only destroy leaves host root but synchronously disables all owned physics",
            a.gameObject.activeInHierarchy&&Bodies(a.gameObject)==0);
        Check("contender cannot acquire during Unity Destroy deferred frame",
            !b.TrySelectPersistentRunSlot(51,slot)&&Bodies(b.gameObject)==0);
        UnityEngine.Object.FlushEndOfFrame();
        Check("all old owned physical Rigidbodies actually destroyed",
            a.gameObject.GetComponentsInChildren<Rigidbody>(true).Length==0);
        Check("contender reacquires only after real hierarchy retirement",
            b.TrySelectPersistentRunSlot(51,slot));
        Check("Heavy gen11 from original #556 now published",
            b.TryRestoreBytes(IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11) &&
            b.IsConsumed(1,2310002)&&!b.IsConsumed(0,2310001));
        Check("no conflicting Light body remains during Heavy world",
            Bodies(a.gameObject)==0&&Bodies(b.gameObject)==2);
        Check("Save gen12 original #556 only new Heavy consumed",
            b.TrySaveBytes(out byte[] data) &&
            SnapshotCodec.TryDecode(data,out RunSnapshot snap)&&
            snap!=null&&snap.Generation==12&&snap.Parts[1].Consumed&&!snap.Parts[0].Consumed);
        UnityEngine.Object.Destroy((Component)b);
        UnityEngine.Object.FlushEndOfFrame();
        UnityEngine.Object.Destroy(a.gameObject);
        UnityEngine.Object.Destroy(b.gameObject);
        UnityEngine.Object.FlushEndOfFrame();
    }
    static int Main()
    {
        string dir=Path.Combine(Path.GetTempPath(),"B0123R5_"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Application.persistentDataPath=dir;
        try
        {
            ComponentOnlyDestroyAndRecover();
            Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
            Console.WriteLine("R5_COMPONENT_DESTROY_ORPHAN_P0=SOURCE_GREEN_SYNTHETIC_ONLY");
            Console.WriteLine("NATIVE_UNITY_PHYSX=NOT_RUN; REAL_510_PRIVATE_LEDGER=HOLD");
            return fail==0?0:1;
        }
        catch(Exception e){Console.WriteLine("UNEXPECTED "+e);return 1;}
        finally{try{Directory.Delete(dir,true);}catch(IOException){}}
    }
}
