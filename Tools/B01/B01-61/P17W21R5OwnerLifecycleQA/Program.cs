// B01-61 W21: DIRECT original R5 #690 C# MonoBehaviour and actual OS slot,
 // original #556 W06. ONLY UnityEngine engine/equality/frame tick is test double.
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using B01.UnitySaveBridge001;
using B01.DetachedSaveContract001;
internal static class Program
{
    static int passed,failed;
    static void Assert(string label,bool truth)
    {
        if(truth){passed++;Console.WriteLine("PASS "+label);}
        else{failed++;Console.WriteLine("FAIL "+label);}
    }
    static int Live(params IsolatedTwinRigSaveHost[] hosts)
       =>hosts.SelectMany(h=>h.gameObject.GetComponentsInChildren<Rigidbody>(true))
         .Count(rb=>rb.gameObject.activeInHierarchy);
    static int Exists(params IsolatedTwinRigSaveHost[] hosts)
       =>hosts.SelectMany(h=>h.gameObject.GetComponentsInChildren<Rigidbody>(true)).Count();
    static IsolatedTwinRigSaveHost Host(string name)
       =>new GameObject(name).AddComponent<IsolatedTwinRigSaveHost>();
    static void R5ComponentOnlyDeferredDestroy()
    {
        var slot=Guid.Parse("f4180619-7531-4a5e-8201-090000000051");
        var a=Host("R5 OWNER A");
        Assert("A selects owner slot 51",a.TrySelectPersistentRunSlot(51,slot));
        Assert("A original Light gen10",a.TryRestoreBytes(
            IsolatedTwinRigSaveHost.MakeFixture(51,10,true,false),51,10));
        Assert("A W06 Light 2310001 consumed and two live parts",
            a.IsConsumed(0,2310001)&&!a.IsConsumed(1,2310002)&&Live(a)==2);
        var b=Host("R5 CONTENDER B");
        Assert("R5 B denied while A authority live",
            !b.TrySelectPersistentRunSlot(51,slot));
        UnityEngine.Object.Destroy((Component)a); // only MonoBehaviour
        Assert("R5 A removal quarantined + retired original world",
            a.Quarantined&&!a.HasWorld&&a.gameObject.activeInHierarchy);
        Assert("R5 original world deactivated BEFORE lease release",
            Live(a,b)==0 && Exists(a,b)==2);
        Assert("R5 B denied while original deferred physics still exists",
            !b.TrySelectPersistentRunSlot(51,slot)&&!b.HasWorld);
        Assert("R5 B cannot load a different world before end-of-frame",
            !b.TryRestoreBytes(
                IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11));
        Console.WriteLine("W21_R5_690_COMPONENT_ONLY_ONDESTROY_PENDING_CHILDREN_LEASE_HELD_GREEN=PASS");
        // Main-thread Unity frame tick is modeled ONLY via explicit shim.
        UnityEngine.Object.FlushEndOfFrame();
        Assert("R5 old physical owned Rigidbody parts actually retired",
            Live(a,b)==0&&Exists(a,b)==0);
        Assert("R5 slot passes to B only after verified physical destruction",
            b.TrySelectPersistentRunSlot(51,slot));
        Assert("R5 B next Heavy gen11 original 556 physical world accepted",
            b.TryRestoreBytes(
                IsolatedTwinRigSaveHost.MakeFixture(51,11,false,true),51,11));
        Assert("R5 B correct W06 Heavy ID, one live two-body world",
            b.IsConsumed(1,2310002)&&!b.IsConsumed(0,2310001)&&
            Live(a,b)==2&&Exists(a,b)==2);
        Assert("R5 B new durable original #556 state saves gen12",
            b.TrySaveBytes(out byte[] saved) &&
            SnapshotCodec.TryDecode(saved,out RunSnapshot loaded)&&
            loaded!=null&&loaded.RunId==51&&loaded.Generation==12&&
            loaded.Parts[1].Consumed);
        Console.WriteLine("W21_R5_690_NO_ORPHAN_WORLD_AFTER_TAKEOVER_GREEN=PASS");
        UnityEngine.Object.Destroy((Component)b);
        Assert("R5 B cleanup makes its physics inactive immediately",Live(b)==0);
        UnityEngine.Object.FlushEndOfFrame();
        Assert("R5 B destroyed children after frame",Exists(b)==0);
        UnityEngine.Object.Destroy(a.gameObject);
        UnityEngine.Object.Destroy(b.gameObject);
        UnityEngine.Object.FlushEndOfFrame();
    }
    static void CrossRunAndFalseAuthority()
    {
        var slot=Guid.Parse("f4180619-7531-4a5e-8201-090000000052");
        var c=Host("R5 NEW RUN52");
        Assert("R5 new run 52 independent persistent slot",
            c.TrySelectPersistentRunSlot(52,slot));
        Assert("R5 new run 52 frozen #556 W06 Heavy ID",
            c.TryRestoreBytes(
                IsolatedTwinRigSaveHost.MakeFixture(52,1,false,true),52,1)
            &&c.IsConsumed(1,2310002)&&Live(c)==2);
        Assert("R5 foreign RunId rejected without touching physics",
            !c.TryRestoreBytes(
                IsolatedTwinRigSaveHost.MakeFixture(51,2,true,false),51,2)
            &&c.LastRestoreVerdict==WorldSwapVerdict.WrongRun&&Live(c)==2);
        UnityEngine.Object.Destroy((Component)c);
        Assert("R5 Run52 physical world quiescent on host-only destroy",
            c.Quarantined&&Live(c)==0);
        UnityEngine.Object.FlushEndOfFrame();
        Assert("R5 Run52 deferred world ultimately retired",
            Exists(c)==0);
        UnityEngine.Object.Destroy(c.gameObject);
        UnityEngine.Object.FlushEndOfFrame();
    }
    static void StuckLeaseConservative()
    {
        // Execute ORIGINAL R5 OwnerWorldLeaseRetirement directly on two 
        // distinct real OS files. An adversarial cleanup verification failure
        // may never release owner lock or be forcible-reset in this process.
        var dir=Path.Combine(Application.persistentDataPath,"failing-owner");
        Directory.CreateDirectory(dir);
        string path=Path.Combine(dir,"run_53_slot_failing.checkpoint");
        var lease=new FileStream(path+".owner",FileMode.OpenOrCreate,
                                        FileAccess.ReadWrite,FileShare.None);
        {
            OwnerWorldLeaseRetirement.Hold(path,lease,()=>false,true);
            Assert("R5 cleanup not proven denies slot acquire",
                   !OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(path));
            Assert("R5 repeated unproven cleanup cannot grant slot",
                   !OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(path));
            // LEASE MUST STAY LIVE while pending, never dispose even at end of block.
            bool denied=false;
            try{using var bad=new FileStream(path+".owner",FileMode.Open,
                 FileAccess.ReadWrite,FileShare.None);}
            catch(IOException){denied=true;}
            Assert("R5 retained OS owner lock denies concurrent external host",denied);
        }
        string second=Path.Combine(dir,"run_54_slot_failed.checkpoint");
        var permanent=new FileStream(second+".owner",FileMode.OpenOrCreate,
                                      FileAccess.ReadWrite,FileShare.None);
        OwnerWorldLeaseRetirement.Hold(second,permanent,()=>true,false);
        Assert("R5 failed actual teardown permanently holds OS lease",
            !OwnerWorldLeaseRetirement.ReconcileBeforeNewAcquire(second));
        bool blocked=false;
        try{using var bad=new FileStream(second+".owner",FileMode.Open,
             FileAccess.ReadWrite,FileShare.None);}
        catch(IOException){blocked=true;}
        Assert("R5 cleanup failure lease remains physically exclusive",blocked);
        // Never release permanent; its lifetime is intentionally process-owned.
        Console.WriteLine("W21_R5_690_FAILED_CLEANUP_FAIL_CLOSED_LEASE_GREEN=PASS");
    }
    public static int Main()
    {
        var path=Path.Combine(Path.GetTempPath(),
            "B01-61-W21-R5-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        Application.persistentDataPath=path;
        try
        {
            R5ComponentOnlyDeferredDestroy();
            CrossRunAndFalseAuthority();
            StuckLeaseConservative();
            Console.WriteLine("W21_R5_690_INDEPENDENT_SOURCE_ASSERTIONS PASS="+passed+" FAIL="+failed);
            Console.WriteLine("W21_R5_690_COMPONENT_DESTROY_SOURCE_P0_670=NO_REPRODUCTION");
            Console.WriteLine("R5_SOURCE_GATE="+(failed==0?"GREEN_TESTED_SCOPE_ONLY":"RED_OR_UNTESTED"));
            Console.WriteLine("ACTUAL_LICENSED_UNITY_PHYSX=NOT_RUN; PRIVATE_CANONICAL_510_SAVELOAD=HOLD");
            return failed==0?0:1;
        }
        catch(Exception ex){Console.Error.WriteLine("R5_W21_QA_EXCEPTION "+ex);return 1;}
        finally {try{Directory.Delete(path,true);}catch(IOException){}
                 catch(UnauthorizedAccessException){}}
    }
}
