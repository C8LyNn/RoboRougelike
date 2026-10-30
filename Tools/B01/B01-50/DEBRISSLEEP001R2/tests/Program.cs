using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using B01.Performance.DebrisSleep001;
using B01.Performance.DebrisSleep001.Unity;
using UnityEngine;

internal static class Program
{
    private static int passed, failed;
    private static readonly List<string> failures = new List<string>();
    private static void Check(bool ok, string why="unexpected state")
    { if (!ok) throw new Exception(why); }

    private static void Test(string name, Action body)
    {
        try { body(); ++passed; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { ++failed; failures.Add(name+": "+ex); Console.WriteLine("FAIL "+name+": "+ex); }
    }

    private static bool Bad(Action body)
    {
        try { body(); return false; }
        catch (ArgumentOutOfRangeException) { return true; }
    }

    private static DebrisSleepPolicy Policy(int steps=8,float linear=0.1f,float angular=0.1f)
    { return new DebrisSleepPolicy(steps,linear,angular); }

    private static void Set(object o,string name,object value)
    {
        var field=o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
        Check(field!=null,"missing actual production field "+name);
        field.SetValue(o,value);
    }

    private static void Call(object o,string method,params object[] args)
    {
        var m=o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic);
        Check(m!=null,"missing actual production method "+method);
        try { m.Invoke(o,args); }
        catch(TargetInvocationException x) when(x.InnerException!=null)
        { throw new Exception("actual "+method+" threw "+x.InnerException.GetType().Name,x.InnerException); }
    }

    private static B0150OptionalDebrisSleepGovernor Probe(Rigidbody b=null,bool consent=true,
                    int steps=8,float linear=0.1f,float angular=0.1f)
    {
        var p=new B0150OptionalDebrisSleepGovernor { SimulatedBody=b??new Rigidbody() };
        Set(p,"allowCosmeticSleep",consent);
        Set(p,"requiredQuietFixedSteps",steps);
        Set(p,"maxLinearSpeed",linear);
        Set(p,"maxAngularSpeed",angular);
        Call(p,"Awake"); Call(p,"OnEnable");
        return p;
    }
    private static void Tick(B0150OptionalDebrisSleepGovernor p,int n=1)
    { for (int i=0;i<n;i++) Call(p,"FixedUpdate"); }
    private static void Impact(B0150OptionalDebrisSleepGovernor p,string eventName)
    { Call(p,eventName,new Collision()); }

    public static int Main(string[] args)
    {
        if(args.Length!=2 || args[0]!="--receipt" || File.Exists(args[1])) return 3;

        Test("policy rejects quiet steps below eight",()=>Check(Bad(()=>Policy(7))));
        Test("policy rejects quiet steps above 600",()=>Check(Bad(()=>Policy(601))));
        Test("policy accepts minimum 8 steps",()=>Check(Policy(8).RequiredSteps==8));
        Test("policy accepts maximum 600 steps",()=>Check(Policy(600).RequiredSteps==600));
        Test("policy rejects zero linear threshold",()=>Check(Bad(()=>Policy(linear:0))));
        Test("policy rejects negative angular threshold",()=>Check(Bad(()=>Policy(angular:-1))));
        Test("policy rejects NaN linear threshold",()=>Check(Bad(()=>Policy(linear:float.NaN))));
        Test("policy rejects infinity angular threshold",()=>Check(Bad(()=>Policy(angular:float.PositiveInfinity))));
        Test("policy rejects too-large linear threshold",()=>Check(Bad(()=>Policy(linear:10.1f))));
        Test("policy rejects too-large angular threshold",()=>Check(Bad(()=>Policy(angular:20.1f))));
        Test("policy accepts bounded maximum threshold",()=>Check(Policy(8,10,20).RequiredSteps==8));
        Test("policy seven quiet ticks never issue Sleep",()=>{
            var p=Policy();for(int i=0;i<7;i++)Check(!p.Step(0,0,false));
            Check(p.QuietSteps==7&&!p.Attempted);
        });
        Test("policy exactly eighth quiet tick issues once",()=>{
            var p=Policy();for(int i=0;i<7;i++)p.Step(0,0,false);
            Check(p.Step(0,0,false)&&p.Attempted&&p.QuietSteps==8);
        });
        Test("policy does not hammer Sleep when native Sleep failed",()=>{
            var p=Policy();for(int i=0;i<8;i++)p.Step(0,0,false);
            for(int i=0;i<200;i++)Check(!p.Step(0,0,false));
        });
        Test("policy observed high linear velocity resets quiet window",()=>{
            var p=Policy();for(int i=0;i<7;i++)p.Step(0,0,false);
            Check(!p.Step(1,0,false)&&p.QuietSteps==0);
            for(int i=0;i<7;i++)Check(!p.Step(0,0,false));
            Check(p.Step(0,0,false));
        });
        Test("policy high angular velocity resets quiet window",()=>{
            var p=Policy();for(int i=0;i<7;i++)p.Step(0,0,false);
            Check(!p.Step(0,1,false)&&p.QuietSteps==0);
        });
        Test("policy rejects negative speed squared",()=>{
            var p=Policy();p.Step(0,0,false);
            Check(!p.Step(-1,0,false)&&p.QuietSteps==0);
        });
        Test("policy rejects NaN speed squared",()=>{
            var p=Policy();p.Step(0,0,false);
            Check(!p.Step(float.NaN,0,false)&&p.QuietSteps==0);
        });
        Test("policy rejects infinity angular speed squared",()=>{
            var p=Policy();p.Step(0,0,false);
            Check(!p.Step(0,float.PositiveInfinity,false)&&p.QuietSteps==0);
        });
        Test("policy returns no request while already sleeping",()=>{
            var p=Policy();for(int i=0;i<7;i++)p.Step(0,0,false);
            Check(!p.Step(0,0,true)&&p.QuietSteps==0&&!p.Attempted);
        });
        Test("policy explicit Reset cancels outstanding quiet lease",()=>{
            var p=Policy();for(int i=0;i<7;i++)p.Step(0,0,false);
            p.Reset();Check(p.QuietSteps==0&&!p.Attempted&&!p.Step(0,0,false));
        });
        Test("policy Reset permits new attempt after denied native sleep",()=>{
            var p=Policy();for(int i=0;i<8;i++)p.Step(0,0,false);
            p.Reset();for(int i=0;i<7;i++)Check(!p.Step(0,0,false));
            Check(p.Step(0,0,false));
        });
        Test("policy zero speed is eligible only after quiet window",()=>{
            var p=Policy();Check(!p.Step(0,0,false));
        });
        Test("policy accepts exactly threshold squared",()=>{
            var p=Policy(8,1f,2f);for(int i=0;i<7;i++)p.Step(1f,4f,false);
            Check(p.Step(1f,4f,false));
        });
        Test("policy two distinct objects never share counters",()=>{
            var a=Policy();var b=Policy();for(int i=0;i<7;i++)a.Step(0,0,false);
            Check(a.QuietSteps==7&&b.QuietSteps==0);
        });

        Test("Unity adapter defaults opt-in OFF",()=>{
            var body=new Rigidbody();var p=Probe(body,false);
            Check(!p.enabled);Tick(p,20);Check(body.SleepCalls==0);
        });
        Test("Unity adapter cannot arm from late serialized consent mutation",()=>{
            var b=new Rigidbody();var p=Probe(b,false);
            Set(p,"allowCosmeticSleep",true);p.enabled=true;Call(p,"OnEnable");Tick(p,20);
            Check(b.SleepCalls==0);
        });
        Test("Unity adapter refuses missing Rigidbody",()=>{
            var p=new B0150OptionalDebrisSleepGovernor();Set(p,"allowCosmeticSleep",true);
            Call(p,"Awake");Call(p,"OnEnable");Tick(p,20);
            Check(!p.enabled&&p.SleepRequests==0);
        });
        Test("Unity adapter refuses kinematic Rigidbody",()=>{
            var b=new Rigidbody{isKinematic=true};var p=Probe(b);
            Tick(p,20);Check(!p.enabled&&b.SleepCalls==0);
        });
        Test("Unity adapter refuses jointed body at Awake",()=>{
            var b=new Rigidbody();var p=new B0150OptionalDebrisSleepGovernor{SimulatedBody=b,SimulatedJoint=new Joint()};
            Set(p,"allowCosmeticSleep",true);Set(p,"requiredQuietFixedSteps",8);
            Call(p,"Awake");Call(p,"OnEnable");Tick(p,20);
            Check(!p.enabled&&b.SleepCalls==0);
        });
        Test("Unity adapter refuses dynamically jointed body",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            p.SimulatedJoint=new Joint();Tick(p,8);Check(b.SleepCalls==0);
        });
        Test("Unity adapter refuses Rigidbody without collision detection",()=>{
            var b=new Rigidbody{detectCollisions=false};var p=Probe(b);
            Tick(p,20);Check(!p.enabled&&b.SleepCalls==0);
        });
        Test("Unity adapter rejects invalid quiet-step setting",()=>{
            var b=new Rigidbody();var p=Probe(b,steps:0);Tick(p,20);
            Check(!p.enabled&&b.SleepCalls==0);
        });
        Test("Unity adapter rejects NaN angular threshold",()=>{
            var b=new Rigidbody();var p=Probe(b,angular:float.NaN);Tick(p,20);
            Check(!p.enabled&&b.SleepCalls==0);
        });
        Test("Unity adapter waits seven fixed ticks before requesting Sleep",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Check(b.SleepCalls==0&&p.SleepRequests==0);
        });
        Test("Unity adapter requests Sleep after 8 quiet fixed ticks",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,8);
            Check(b.SleepCalls==1&&p.SleepRequests==1&&b.IsSleeping());
        });
        Test("Unity adapter never hammers failed Sleep requests",()=>{
            var b=new Rigidbody{acceptSleep=false};var p=Probe(b);Tick(p,200);
            Check(b.SleepCalls==1&&p.SleepRequests==1);
        });
        Test("Unity adapter high speed continuously rejects Sleep",()=>{
            var b=new Rigidbody{linearVelocity=new Vector3(1,0,0)};
            var p=Probe(b);Tick(p,25);Check(b.SleepCalls==0);
        });
        Test("Unity adapter impact Enter resets quiet window",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Impact(p,"OnCollisionEnter");Tick(p,7);Check(b.SleepCalls==0);
            Tick(p);Check(b.SleepCalls==1);
        });
        Test("Unity adapter impact Exit resets quiet window",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Impact(p,"OnCollisionExit");Tick(p,7);Check(b.SleepCalls==0);
            Tick(p);Check(b.SleepCalls==1);
        });
        Test("Unity adapter original OnCollisionStay revokes accumulated quiet period",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Impact(p,"OnCollisionStay");Tick(p,7);
            Check(b.SleepCalls==0,"quiet interval survived continuous contact");
            Tick(p);Check(b.SleepCalls==1,"new quiet window did not complete");
        });
        Test("Unity adapter sustained contact Stay never forces Sleep",()=>{
            var b=new Rigidbody();var p=Probe(b);
            for(int i=0;i<50;i++){Tick(p);Impact(p,"OnCollisionStay");}
            Check(b.SleepCalls==0&&p.SleepRequests==0,
                  "active collision maintained quiet window");
        });
        Test("Unity adapter null Stay does not revoke valid quiet period",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Call(p,"OnCollisionStay",(object)null);Tick(p);
            Check(b.SleepCalls==1,"null contact was counted as real contact");
        });
        Test("Unity adapter stale Stay while disabled cannot reset quiet lease",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            p.enabled=false;Impact(p,"OnCollisionStay");p.enabled=true;Tick(p);
            Check(b.SleepCalls==1,"disabled callback modified owned lease");
        });
        Test("Unity adapter revoked consent ignores stale Stay event",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Set(p,"allowCosmeticSleep",false);Impact(p,"OnCollisionStay");
            Set(p,"allowCosmeticSleep",true);Tick(p);
            Check(b.SleepCalls==1,"unconsented Stay changed owned lease");
        });
        Test("Unity adapter null impact does not falsely cancel quiet window",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Call(p,"OnCollisionEnter",(object)null);Tick(p);Check(b.SleepCalls==1);
        });
        Test("Unity adapter suppresses Sleep when disabled",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            p.enabled=false;Call(p,"OnDisable");Tick(p,20);
            Check(b.SleepCalls==0&&p.SleepRequests==0);
        });
        Test("Unity adapter disable does not forcibly WakeUp existing sleeping body",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,8);
            p.enabled=false;Call(p,"OnDisable");Check(b.sleeping&&b.SleepCalls==1);
        });
        Test("Unity adapter OnDestroy permanently blocks new requests",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Call(p,"OnDestroy");p.enabled=true;Call(p,"OnEnable");Tick(p,20);
            Check(b.SleepCalls==0);
        });
        Test("Unity adapter normal disable and reenable starts new quiet window",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            p.enabled=false;Call(p,"OnDisable");
            p.enabled=true;Call(p,"OnEnable");Tick(p,7);Check(b.SleepCalls==0);
            Tick(p);Check(b.SleepCalls==1);
        });
        Test("Unity adapter runtime consent cancellation blocks requests",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            Set(p,"allowCosmeticSleep",false);Tick(p,12);Check(b.SleepCalls==0);
        });
        Test("Unity adapter reenable ignores mutated quiet-step setting",()=>{
            var b=new Rigidbody();var p=Probe(b);Set(p,"requiredQuietFixedSteps",600);
            Tick(p,8);Check(b.SleepCalls==1);
        });
        Test("Unity adapter reenable ignores mutated velocity threshold",()=>{
            var b=new Rigidbody{linearVelocity=new Vector3(.15f,0,0)};var p=Probe(b);
            Set(p,"maxLinearSpeed",10f);Tick(p,20);Check(b.SleepCalls==0);
        });
        Test("Unity adapter sleep reattempt follows natural wake and quiet window",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,8);
            b.sleeping=false;Tick(p,7);Check(b.SleepCalls==1);
            Tick(p);Check(b.SleepCalls==2);
        });
        Test("Unity adapter off-threshold movement permits later quiet interval",()=>{
            var b=new Rigidbody{linearVelocity=new Vector3(1f,0,0)};var p=Probe(b);
            Tick(p,5);b.linearVelocity=new Vector3(0,0,0);
            Tick(p,7);Check(b.SleepCalls==0);Tick(p);Check(b.SleepCalls==1);
        });
        Test("Unity adapter independent debris bodies retain private windows",()=>{
            var a=new Rigidbody();var b=new Rigidbody();
            var x=Probe(a);var y=Probe(b);Tick(x,7);Tick(y,2);Tick(x);
            Check(a.SleepCalls==1&&b.SleepCalls==0);
        });
        Test("Unity adapter guarded by runtime kinematic switch",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            b.isKinematic=true;Tick(p,8);Check(b.SleepCalls==0);
        });
        Test("Unity adapter guarded by runtime detectCollisions switch",()=>{
            var b=new Rigidbody();var p=Probe(b);Tick(p,7);
            b.detectCollisions=false;Tick(p,8);Check(b.SleepCalls==0);
        });
        Test("Unity adapter invalid runtime speed NaN fails closed",()=>{
            var b=new Rigidbody{angularVelocity=new Vector3(float.NaN,0,0)};var p=Probe(b);
            Tick(p,10);Check(b.SleepCalls==0);
        });

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
        var receipt=new {
            schema="B01-50-DEBRISSLEEP-001-R2-SOURCE-RECEIPT",
            total=passed+failed,passed,failed,failures,
            genuineProductionCore=true,genuineProductionUnityFacingAdapter=true,
            unitySurface="SURROGATE_TEST_ONLY_NOT_UNITY",
            nativeUnity="NOT_RUN",nativePhysX="NOT_RUN",actualFrameTime="NOT_MEASURED",
            physicalSleepAcceptance="NOT_MEASURED",safeInstall="NOT_PERFORMED"
        };
        File.WriteAllText(args[1],JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
        Console.WriteLine("DEBRISSLEEP_EXACT_PRODUCTION_SOURCE "+passed+"/"+(passed+failed)+" PASS "+failed+
                          " FAIL; NATIVE_UNITY_NOT_RUN");
        return failed==0?0:1;
    }
}
