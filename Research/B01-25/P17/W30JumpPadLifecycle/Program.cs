using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using B01.MapJumpPad001;
using UnityEngine;

// Calls ACTUAL imported production MapJumpPad.cs (linked in .csproj).
// Fake UnityEngine is a narrowly scoped managed contract: real trigger/PhysX
// acceptance still requires the separately-authored Unity NUnit G1 tests.
internal static class Program
{
    private static readonly MethodInfo Enter =
        typeof(MapJumpPad).GetMethod("OnTriggerEnter", BindingFlags.Instance|BindingFlags.NonPublic)!;
    private static readonly MethodInfo Exit =
        typeof(MapJumpPad).GetMethod("OnTriggerExit", BindingFlags.Instance|BindingFlags.NonPublic)!;
    private static readonly MethodInfo Fixed =
        typeof(MapJumpPad).GetMethod("FixedUpdate", BindingFlags.Instance|BindingFlags.NonPublic)!;
    private static readonly MethodInfo Disable =
        typeof(MapJumpPad).GetMethod("OnDisable", BindingFlags.Instance|BindingFlags.NonPublic)!;
    private static int executed, rejected;
    private static readonly List<string> Receipts = new List<string>();

    private static void Check(bool condition,string reason)
    {
        if(!condition)throw new InvalidOperationException(reason);
    }
    private static void Test(string name,Action action)
    {
        Time.fixedTimeAsDouble=100;
        try
        {
            action();
            executed++;
            Receipts.Add(name+":PASS");
            Console.WriteLine(name+":PASS");
        }
        catch(Exception error)
        {
            rejected++;Receipts.Add(name+":FAIL "+error.Message);
            Console.WriteLine(name+":FAIL "+error);
        }
    }
    private static void Invoke(MethodInfo method,MapJumpPad pad,Collider collider=null)
    {
        try {method.Invoke(pad,collider==null?null:new object[]{collider});}
        catch(TargetInvocationException error)
        {throw error.InnerException??error;}
    }
    private static (Rigidbody body, Collider c1, Collider c2) Compound()
    {
        var body=new Rigidbody();
        var c1=new BoxCollider{attachedRigidbody=body};
        var c2=new BoxCollider{attachedRigidbody=body};
        return(body,c1,c2);
    }
    private static MapJumpPad Pad()=>new MapJumpPad();

    public static int Main(string[] args)
    {
        Test("W30_01_ACTUAL_ORIGINAL_VELOCITY_CHANGE_AND_DROP_CANCEL",()=>
        {
            var pad=Pad();var a=Compound();
            a.body.linearVelocity=new Vector3(2,-5,3);
            Invoke(Enter,pad,a.c1);
            Check(a.body.AppliedVelocityChangeCount==1,"no original force");
            Check(Math.Abs(a.body.linearVelocity.y-8.5f)<.0001f,
                "must cancel falling speed and apply original upward velocity");
            Check(Math.Abs(a.body.linearVelocity.z-5f)<.0001f,
                "original forward velocity change altered");
            Check(pad.TrackedContactBodies==1&&pad.CooldownHistoryBodies==1,
                "physically contacted body must be tracked exactly once");
        });
        Test("W30_02_COMPOUND_TWO_COLLIDERS_ONE_LAUNCH",()=>
        {
            var pad=Pad();var a=Compound();
            Invoke(Enter,pad,a.c1);Invoke(Enter,pad,a.c2);
            Check(a.body.AppliedVelocityChangeCount==1,"duplicate boost by second wheel");
            Check(pad.TrackedContactBodies==1&&pad.CooldownHistoryBodies==1,
                "one logical body must own one cooldown");
        });
        Test("W30_03_DISABLED_DEBRIS_COLLIDER_NO_STALE_CONTACT",()=>
        {
            var pad=Pad();var a=Compound();a.body.isKinematic=true;
            Invoke(Enter,pad,a.c1);
            Check(pad.TrackedContactBodies==1&&pad.CooldownHistoryBodies==0,
                "nonlaunching attached physics contact absent");
            a.c1.enabled=false;Invoke(Fixed,pad);
            Check(pad.TrackedContactBodies==0,
                "OnTriggerExit absent: disabled collider must be pruned");
            a.c1.enabled=true;a.body.isKinematic=false;
            Invoke(Enter,pad,a.c1);
            Check(a.body.AppliedVelocityChangeCount==1,
                "re-enabled body blocked by stale contact memory");
        });
        Test("W30_04_DETACHED_BODY_DESTROY_WITHOUT_TRIGGER_EXIT_PRUNED",()=>
        {
            var pad=Pad();var a=Compound();
            Invoke(Enter,pad,a.c1);
            a.body.Destroyed=true;Invoke(Fixed,pad);
            Check(pad.TrackedContactBodies==0&&pad.CooldownHistoryBodies==0,
                "destroyed Rigidbody retains stale contact or cooldown history");
        });
        Test("W30_05_LIVE_BODY_COOLDOWN_SURVIVES_GARBAGE_COLLECTOR",()=>
        {
            var pad=Pad();var a=Compound();
            Invoke(Enter,pad,a.c1);Invoke(Exit,pad,a.c1);
            Invoke(Fixed,pad);
            Check(pad.TrackedContactBodies==0&&pad.CooldownHistoryBodies==1,
                "live cooldown must not be pruned");
            Time.fixedTimeAsDouble=100.5;
            Invoke(Enter,pad,a.c1);
            Check(a.body.AppliedVelocityChangeCount==1,"cooldown bypassed");
            Invoke(Exit,pad,a.c1);
            Time.fixedTimeAsDouble=101.3;
            Invoke(Enter,pad,a.c1);
            Check(a.body.AppliedVelocityChangeCount==2,
                "legitimate second entry after original cooldown denied");
        });
        Test("W30_06_TWO_BODIES_ONE_DESTROY_NO_GLOBAL_COOLDOWN_RESET",()=>
        {
            var pad=Pad();var a=Compound();var b=Compound();
            Invoke(Enter,pad,a.c1);Invoke(Enter,pad,b.c1);
            Check(pad.CooldownHistoryBodies==2,"second independent source missing");
            a.body.Destroyed=true;Invoke(Fixed,pad);
            Check(pad.CooldownHistoryBodies==1&&pad.TrackedContactBodies==1,
                "pruning dead actor incorrectly erased live actor");
            Time.fixedTimeAsDouble=100.3;
            Invoke(Exit,pad,b.c1);Invoke(Enter,pad,b.c1);
            Check(b.body.AppliedVelocityChangeCount==1,
                "unrelated dead body caused premature relaunch");
        });
        Test("W30_07_ATTACHED_COLLIDER_REASSIGNED_OTHER_BODY_PRUNED",()=>
        {
            var pad=Pad();var a=Compound();a.body.isKinematic=true;
            Invoke(Enter,pad,a.c1);
            a.c1.attachedRigidbody=new Rigidbody();
            Invoke(Fixed,pad);
            Check(pad.TrackedContactBodies==0,"foreign reparented contact not removed");
        });
        Test("W30_08_LIVE_MAP_DISABLE_CLEARS_CONTACTS_AND_HISTORY",()=>
        {
            var pad=Pad();var a=Compound();
            Invoke(Enter,pad,a.c1);
            Invoke(Disable,pad);
            Check(pad.CooldownHistoryBodies==0&&pad.TrackedContactBodies==0,
                "map unload retained stale source");
        });
        Test("W30_09_NON_AUTHORIZED_KINEMATIC_CANNOT_CONSUME_COOLDOWN",()=>
        {
            var pad=Pad();var a=Compound();a.body.isKinematic=true;
            Invoke(Enter,pad,a.c1);
            Check(a.body.AppliedVelocityChangeCount==0&&pad.CooldownHistoryBodies==0,
                "kinematic actor in scene consumed physical launch");
        });
        Test("W30_10_STALE_SECOND_COLLIDER_PRUNED_KEEPS_VALID_FIRST",()=>
        {
            var pad=Pad();var a=Compound();
            Invoke(Enter,pad,a.c1);Invoke(Enter,pad,a.c2);
            a.c2.gameObject.activeInHierarchy=false;
            Invoke(Fixed,pad);
            Check(pad.TrackedContactBodies==1&&pad.CooldownHistoryBodies==1,
                "valid collider or cooldown lost on second wheel removal");
            Invoke(Exit,pad,a.c1);Invoke(Fixed,pad);
            Check(pad.TrackedContactBodies==0&&pad.CooldownHistoryBodies==1,
                "new contact must work after all physical children leave");
        });
        Console.WriteLine("B0125_W30_ORIGINAL_MAPJUMPPAD_MANAGED_SOURCE "+executed+"/"+
            (executed+rejected)+" FAIL="+rejected+" NATIVE_UNITY_G1_NOT_RUN");
        if(args.Length>0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(args[0])??".");
            File.WriteAllText(args[0],JsonSerializer.Serialize(new {
                package="B01-25-W30-R5-JUMPPAD-CONTACT-LIFECYCLE",
                checkedSource="Assets/B01Packages/MapJumpPad001/Runtime/MapJumpPad.cs",
                executed,failed=rejected,results=Receipts,
                method="ORIGINAL_RUNTIME_WITH_MANAGED_UNITY_SHIM",
                nativeUnity="NOT_RUN",windowsPlayer="NOT_RUN"},
                new JsonSerializerOptions{WriteIndented=true}));
        }
        return rejected==0?0:1;
    }
}
