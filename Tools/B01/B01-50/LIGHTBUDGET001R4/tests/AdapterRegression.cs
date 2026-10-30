using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using B01.Performance.LightBudget001.Unity;
using UnityEngine;

internal static class Program
{
    private static int pass, fail;
    private static readonly List<string> failed = new();
    private sealed class Fixture
    {
        public B0150OptionalCosmeticLightBudget Manager = null!;
        public B0150OptionalCosmeticLightBudget.LightSlot[] Slots = null!;
        public Light First = null!, Second = null!, Foreign = null!;
    }
    private static Fixture Setup(bool secondEnabled=true,int cadence=1,bool equalImportance=false)
    {
        var a = new Light { enabled=true };
        a.transform.position=new Vector3(1f,0,0);
        var b = new Light { enabled=secondEnabled };
        b.transform.position=new Vector3(6f,0,0);
        var foreign = new Light { enabled=false };
        foreign.transform.position=new Vector3(2f,0,0);
        var slots = new[] {
            new B0150OptionalCosmeticLightBudget.LightSlot { Light=a, Importance=equalImportance ? 50 : 70 },
            new B0150OptionalCosmeticLightBudget.LightSlot { Light=b, Importance=equalImportance ? 50 : 60 }
        };
        var owner=new B0150OptionalCosmeticLightBudget();
        if(!owner.Configure(new Camera(), slots, 1, 0f, cadence))
            throw new Exception("Configure failed");
        Call(owner,"Awake");
        return new Fixture { Manager=owner, Slots=slots, First=a, Second=b, Foreign=foreign };
    }
    private static void Call(object instance, string method)
    {
        var methodInfo=instance.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic);
        if(methodInfo==null)throw new Exception("Missing original adapter method: "+method);
        // Unwrap actual original method errors to report their true root.
        try { methodInfo.Invoke(instance,null); }
        catch(TargetInvocationException e) when(e.InnerException!=null) {
            throw new Exception("Original adapter "+method+" threw "+e.InnerException.GetType().Name, e.InnerException);
        }
    }
    private static void Assert(bool condition,string why)
    {
        if(!condition)throw new Exception(why);
    }
    private static void Test(string name,Action test)
    {
        try{test();pass++;Console.WriteLine("PASS "+name);}
        catch(Exception e){fail++;failed.Add(name+": "+e.Message);Console.WriteLine("FAIL "+name+": "+e);}
    }
    private static void Reapply(Fixture f)=>Call(f.Manager,"LateUpdate");
    private static void Restore(Fixture f)=>Call(f.Manager,"OnDisable");
    public static int Main(string[] args)
    {
        if(args.Length!=2||args[0]!="--receipt")return 3;
        Test("smoke capped lights and restore",()=>{
            var f=Setup();Reapply(f);
            Assert(f.First.enabled&&!f.Second.enabled,"cap broken");
            Restore(f);
            Assert(f.First.enabled&&f.Second.enabled,"baseline not restored");
        });
        Test("null slot after Awake cannot block original rollback",()=>{
            var f=Setup();Reapply(f);
            f.Slots[1]=null!;
            Restore(f);
            Assert(f.Second.enabled,"original disabled light not restored");
        });
        Test("slot changed to foreign light after Awake does not mutate foreign",()=>{
            var f=Setup();Reapply(f);
            f.Slots[1].Light=f.Foreign;
            Restore(f);
            Assert(f.Second.enabled,"original disabled light not restored");
            Assert(!f.Foreign.enabled,"foreign unowned light was enabled");
        });
        Test("serialized array shortened after Awake cannot truncate rollback",()=>{
            var f=Setup();Reapply(f);
            var field=typeof(B0150OptionalCosmeticLightBudget).GetField("cosmeticLights",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert(field!=null,"expected original serialized input field");
            field!.SetValue(f.Manager,new[]{f.Slots[0]});
            Restore(f);
            Assert(f.Second.enabled,"shortened input skipped restoring original second light");
        });
        Test("runtime slot importance mutation cannot hijack selection",()=>{
            var f=Setup();Reapply(f);
            f.Slots[0].Importance=0;
            f.Slots[1].Importance=100;
            Reapply(f);
            Assert(f.First.enabled&&!f.Second.enabled,"mutable slot importance changed trusted ranking");
            Restore(f);
        });
        Test("originally disabled lights remain disabled after restore",()=>{
            var f=Setup(secondEnabled:false);
            Reapply(f);Restore(f);
            Assert(f.First.enabled&&!f.Second.enabled,"originally disabled light activated");
        });
        Test("multiple disable/destroy calls are idempotent",()=>{
            var f=Setup();Reapply(f);Restore(f);
            Call(f.Manager,"OnDestroy");Restore(f);
            Assert(f.First.enabled&&f.Second.enabled,"double restore lost baseline");
        });
        Test("new Configure after Awake is rejected without mutation",()=>{
            var f=Setup();Reapply(f);
            Assert(!f.Manager.Configure(new Camera(),new[]{new B0150OptionalCosmeticLightBudget.LightSlot{Light=f.Foreign}},0,0,1),"unexpected config replacement");
            Restore(f);
            Assert(f.First.enabled&&f.Second.enabled&&!f.Foreign.enabled,"unauthorized post-Awake replacement");
        });
        Test("initial frame enforces cap despite six-frame steady cadence",()=>{
            var f=Setup(cadence:6);
            Reapply(f);
            Assert(f.First.enabled&&!f.Second.enabled,"startup frame exceeded 1-light cap");
            Restore(f);
        });
        Test("reenable first frame reestablishes cap after cleanup restoration",()=>{
            var f=Setup(cadence:6);
            for(int i=0;i<6;i++)Reapply(f);
            Restore(f);
            Assert(f.First.enabled&&f.Second.enabled,"restore precondition");
            f.Manager.enabled=true;
            Call(f.Manager,"OnEnable");
            Reapply(f);
            Assert(f.First.enabled&&!f.Second.enabled,"reenabled first frame exceeded cap");
            Restore(f);
        });
        Test("mutated serialized cadence zero cannot crash or bypass enabled cap",()=>{
            var f=Setup(cadence:6);
            var p=typeof(B0150OptionalCosmeticLightBudget).GetField("updateEveryNFrames",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert(p!=null,"missing serialized cadence");p!.SetValue(f.Manager,0);
            Reapply(f);
            Assert(f.First.enabled&&!f.Second.enabled,"runtime cadence drift broke cap");Restore(f);
        });
        Test("mutated serialized max cannot escalate original light cap",()=>{
            var f=Setup();
            var p=typeof(B0150OptionalCosmeticLightBudget).GetField("maximumEnabled",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert(p!=null,"missing serialized maximum");p!.SetValue(f.Manager,2);
            Reapply(f);
            Assert(f.First.enabled&&!f.Second.enabled,"runtime maximum escalation bypassed original cap");Restore(f);
        });
        Test("mutated serialized NaN hysteresis cannot freeze future selections",()=>{
            var f=Setup(equalImportance:true);Reapply(f);
            f.First.transform.position=new Vector3(20,0,0);
            f.Second.transform.position=new Vector3(0,0,0);
            var p=typeof(B0150OptionalCosmeticLightBudget).GetField("distanceHysteresisMeters",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert(p!=null,"missing serialized hysteresis");p!.SetValue(f.Manager,float.NaN);
            Reapply(f);
            Assert(!f.First.enabled&&f.Second.enabled,"invalid runtime hysteresis blocked changed light selection");Restore(f);
        });
        Test("invalid initial NaN hysteresis rejects entire ownership before mutation",()=>{
            var a=new Light{enabled=true};var b=new Light{enabled=true};
            var o=new B0150OptionalCosmeticLightBudget();
            var slots=new[]{new B0150OptionalCosmeticLightBudget.LightSlot{Light=a,Importance=50},new B0150OptionalCosmeticLightBudget.LightSlot{Light=b,Importance=50}};
            Assert(o.Configure(new Camera(),slots,1,float.NaN,1),"pre-Awake configure failure");
            Call(o,"Awake");
            Assert(!o.enabled,"invalid initial hysteresis incorrectly accepted");
            Assert(a.enabled&&b.enabled,"invalid initial state mutated lights");
        });
        string path=args[1];
        if(File.Exists(path))return 4;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var receipt=new{schema="B01-50-LIGHTBUDGET-R4-ADAPTER-SOURCE-REDGREEN",total=pass+fail,passed=pass,failed=fail,failures=failed,
            testsExecuteOriginalAdapterMethodBodies=true,engineType="SURROGATE_UNITY_API_NOT_NATIVE_UNITY",
            nativeUnity="NOT_RUN",actualGpuDrawCalls="NOT_MEASURED",actualFps="NOT_MEASURED"};
        File.WriteAllText(path,JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine($"ORIGINAL_ADAPTER_SOURCE_TESTS: {pass}/{pass+fail} PASS; {fail} FAIL; UNITY_NOT_RUN");
        return fail==0?0:1;
    }
}
