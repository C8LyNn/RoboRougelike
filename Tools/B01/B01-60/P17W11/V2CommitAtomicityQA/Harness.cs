// B01-60 W11 - genuinely execute the original #488 private CommitDock method
// copied by SHA-pinned Git object byte extraction into Generated/OriginalCommit.g.cs.
// This is a QA-only simulated rigidbody boundary, NOT native Unity/PhysX.
using System;
using B01.DebrisRepulse001;

namespace B01.SecureReattachV2
{
    internal readonly struct Vector3
    {
        public readonly double X, Y, Z;
        public Vector3(double x,double y,double z) { X=x; Y=y; Z=z; }
        public static Vector3 zero => new Vector3(0,0,0);
        public override string ToString() => X+","+Y+","+Z;
    }
    internal sealed class TestBody
    {
        public Vector3 linearVelocity=new Vector3(2,3,4);
        public Vector3 angularVelocity=new Vector3(5,6,7);
        public bool isKinematic=false;
    }
    internal sealed class TestTransform
    {
        public TestTransform parent;
        public Vector3 localPosition=new Vector3(10,11,12);
        public void SetParent(TestTransform next,bool preserveWorld) { parent=next; }
    }
    internal sealed class TestPart { public readonly TestTransform transform=new TestTransform(); }
    internal sealed class TestRoot { public readonly TestTransform transform=new TestTransform(); }
    internal sealed class TestSpec { public long PartId; }
    internal sealed class TestEmitter
    {
        // Direct actual immutable W06 DebrisImpulseLedger C#; not a modeled set.
        public readonly DebrisImpulseLedger Ledger=new DebrisImpulseLedger();
        public bool RearmAfterReattach(long id)=>Ledger.RearmAfterReattach(id);
        public void ClearForNewRun()=>Ledger.ClearForNewRun();
    }
    internal sealed class Rig
    {
        public readonly TestSpec Spec=new TestSpec();
        public readonly TestBody Body=new TestBody();
        public readonly TestPart Part=new TestPart();
        public readonly TestRoot Root=new TestRoot();
        public readonly TestEmitter Emitter=new TestEmitter();
        public Vector3 LocalMount=new Vector3(1,2,3);
        public bool Detached=true;
    }
    internal sealed partial class TransactionHost
    {
        private readonly Rig[] rigs=new Rig[2];
        private bool built=true; // Original host BuildDemo() state boundary, QA-owned stub.
        private GuardedDockCommand gate;
        private DockResult inspector=DockResult.Allowed;
        public TransactionHost()
        {
            for(int i=0;i<rigs.Length;i++)
            {
                rigs[i]=new Rig();
                rigs[i].Spec.PartId=101+i;
            }
            gate=new GuardedDockCommand(i=>inspector,CommitDock);
        }
        public Rig RigAt(int i)=>rigs[i];
        public void SetInspection(DockResult next)=>inspector=next;
        public bool Call(int route,int id)
        {
            if(route==0) return gate.Reattach(id);
            if(route==1) return gate.TrySafeReattach(id);
            if(route==2) return gate.ReattachFromGui(id);
            throw new ArgumentOutOfRangeException(nameof(route));
        }
        public DockResult LastResult=>gate.LastResult;
        public bool ConsumeRealLedger(int index)
        {
            Rig rig=rigs[index];
            return rig.Emitter.Ledger.TryDetach(rig.Spec.PartId,
                new Vector3d(0,0,0),new Vector3d(1,0,0),new Vector3d(0,0,0),
                1,2,4,out Vector3d _);
        }
    }
    internal static class Program
    {
        private static int groups=0,checks=0,fail=0,invalidAtomicCases=0,positiveCases=0,blockedCases=0;
        private static void Assert(bool yes,string context)
        {
            checks++;
            if(!yes)throw new InvalidOperationException(context);
        }
        private static void Case(string name,Action test)
        {
            try { test();groups++;Console.WriteLine("PASS "+name); }
            catch(Exception e){fail++;Console.Error.WriteLine("FAIL "+name+" "+e);}
        }
        private static void BeforeDock(TransactionHost host,int i)
        {
            Rig rig=host.RigAt(i);
            Assert(rig.Detached,"part must begin detached");
            Assert(!rig.Body.isKinematic,"part must be dynamic");
            Assert(rig.Part.transform.parent==null,"part must have free parent");
            Assert(rig.Body.linearVelocity.X==2 && rig.Body.angularVelocity.Y==6,"initial real simulated physics state");
            Assert(rig.Part.transform.localPosition.X==10,"initial local position");
        }
        private static void AfterDock(TransactionHost host,int i)
        {
            Rig rig=host.RigAt(i);
            Assert(!rig.Detached,"part was not docked");
            Assert(rig.Body.isKinematic,"body did not become kinematic");
            Assert(rig.Body.linearVelocity.X==0 && rig.Body.angularVelocity.Y==0,
                   "velocity not cleared");
            Assert(rig.Part.transform.parent==rig.Root.transform,"parent not committed");
            Assert(rig.Part.transform.localPosition.X==1,"local mount not restored");
        }
        private static void Unchanged(TransactionHost host,int i)
        {
            Rig rig=host.RigAt(i);
            Assert(rig.Detached,"detached flag changed on denial");
            Assert(!rig.Body.isKinematic,"kinematic changed on denial");
            Assert(rig.Body.linearVelocity.X==2 && rig.Body.angularVelocity.Y==6,
                   "velocity changed on denial");
            Assert(rig.Part.transform.parent==null,"parent changed on denial");
            Assert(rig.Part.transform.localPosition.X==10,"mount position changed on denial");
        }
        public static int Main()
        {
            Case("actual source W06 ledger roundtrip",()=>{
                var h=new TransactionHost();
                Assert(h.ConsumeRealLedger(0),"actual ledger did not detach");
                Assert(h.RigAt(0).Emitter.Ledger.Contains(101),"ledger did not consume ID");
                Assert(h.RigAt(0).Emitter.RearmAfterReattach(101),"ledger did not rearm");
                Assert(!h.RigAt(0).Emitter.Ledger.Contains(101),"ledger still consumed");
            });
            for(int route=0;route<3;route++)
            {
                int alias=route;
                Case("real original CommitDock positive route="+alias,()=>{
                    var h=new TransactionHost();
                    BeforeDock(h,0);
                    Assert(h.ConsumeRealLedger(0),"real W06 detach refused");
                    Assert(h.Call(alias,0),"allowed alias refused commit");
                    AfterDock(h,0);
                    Assert(h.RigAt(0).Emitter.Ledger.Count==0,"rearm still consumed");
                    positiveCases++;
                });
                Case("original all aliases inspection deny atomic route="+alias,()=>{
                    var h=new TransactionHost();
                    Assert(h.ConsumeRealLedger(0),"real W06 detach refused");
                    h.SetInspection(DockResult.Obstructed);
                    Assert(!h.Call(alias,0),"blocked alias accepted");
                    Unchanged(h,0);
                    Assert(h.RigAt(0).Emitter.Ledger.Contains(101),"blocked route rearmed ID");
                    blockedCases++;
                });
                Case("original CommitDock real W06 reset causes non-atomic throw route="+alias,()=>{
                    var h=new TransactionHost();
                    BeforeDock(h,0);
                    Assert(h.ConsumeRealLedger(0),"real ledger detach refused");
                    h.RigAt(0).Emitter.ClearForNewRun(); // Original W06 API, genuinely invoked.
                    Assert(!h.RigAt(0).Emitter.Ledger.Contains(101),"stale ID still consumed");
                    bool threw=false;
                    try { h.Call(alias,0); }
                    catch(InvalidOperationException e)
                    {
                        threw=e.Message=="Detached ID missing at protected reattach";
                    }
                    Assert(threw,"original method did not throw on missing consumed ID");
                    // EXPECTED DEFECT WITNESS: real original CommitDock was actually invoked
                    // and has changed each simulated physical field before its late original
                    // ledger rearm attempt returned false.
                    AfterDock(h,0);
                    Assert(h.RigAt(0).Emitter.Ledger.Count==0,
                           "missing rearm unexpectedly consumed ID");
                    invalidAtomicCases++;
                });
                Case("invalid indices no commit and no ledger rearm route="+alias,()=>{
                    var h=new TransactionHost();
                    Assert(h.ConsumeRealLedger(0),"ledger failed");
                    Assert(!h.Call(alias,-1),"negative index accepted");
                    Assert(!h.Call(alias,2),"out-of-range index accepted");
                    Unchanged(h,0);
                    Assert(h.RigAt(0).Emitter.Ledger.Contains(101),"invalid alias changed ledger");
                });
            }
            Console.WriteLine("B01_60_W11_COMMIT_METHOD_ORIGINAL_EXECUTED=true");
            Console.WriteLine("B01_60_W11_CASES groups_pass="+groups+" groups_fail="+fail+
                " assertions="+checks+" original_positive="+positiveCases+
                " denied_atomic="+blockedCases+" missing_id_partial_commit="+invalidAtomicCases);
            Console.WriteLine("B01_60_W11_GATE=EXPECTED_SOURCE_TRANSACTION_RED_REPRODUCED");
            Console.WriteLine("B01_60_W11_BOUNDARY=qa_stub_physical_state_AND_REAL_W06_LEDGER;UNITY_NATIVE_NOT_RUN;G2_HOLD");
            return fail==0 && invalidAtomicCases==3 && positiveCases==3 && blockedCases==3 ? 0:1;
        }
    }
}
