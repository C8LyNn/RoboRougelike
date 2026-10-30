// B01-62 W10: execute unchanged B01-23/W06 DebrisImpulseLedger and W10 GuardedDockCommand.
// "Physical" below is an explicit state model: this is NOT Unity Rigidbody execution.
using System;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;

internal static class Program
{
 static int pass, fail;
 static void Check(string title, Func<bool> test)
 {
     try { if(test()){pass++;Console.WriteLine("PASS "+title);}
           else {fail++;Console.WriteLine("FAIL "+title);} }
     catch(Exception ex){fail++;Console.WriteLine("FAIL "+title+": "+ex);}
 }
 static bool Consume(DebrisImpulseLedger ledger,long id)
 {
     return ledger.TryDetach(id, new Vector3d(0,0,0),
         new Vector3d(1,0,0),new Vector3d(0,0,0),
         2,2.5,4,out Vector3d _);
 }
 static int Main()
 {
     const long id=77001;
     Check("Original W06 ledger first detach consumes ID",()=>{
         var l=new DebrisImpulseLedger();
         return Consume(l,id) && l.Contains(id) && l.Count==1;
     });
     Check("Original W06 ledger duplicate detach rejected",()=>{
         var l=new DebrisImpulseLedger();
         return Consume(l,id) && !Consume(l,id) && l.Count==1;
     });
     Check("Original W06 ClearForNewRun erases consumed ID in place",()=>{
         var l=new DebrisImpulseLedger();
         if(!Consume(l,id))return false;
         l.ClearForNewRun();
         return !l.Contains(id) && l.Count==0;
     });
     Check("Original W06 rearm after state cleared returns false",()=>{
         var l=new DebrisImpulseLedger();
         if(!Consume(l,id))return false;
         l.ClearForNewRun();
         return !l.RearmAfterReattach(id);
     });
     Check("Original W10 router blocks all three aliases before commit",()=>{
         int inspected=0,committed=0;
         var r=new GuardedDockCommand(i=>{inspected++;return DockResult.Obstructed;},
             i=>{committed++;return true;});
         return !r.Reattach(0) && !r.TrySafeReattach(0) &&
                !r.ReattachFromGui(1) && inspected==3 && committed==0;
     });
     Check("Original W10 router invokes committer after allowed inspector",()=>{
         int ins=0,committed=0;
         var r=new GuardedDockCommand(i=>{ins++;return DockResult.Allowed;},
             i=>{committed++;return true;});
         return r.Reattach(0) && ins==1 && committed==1;
     });
     Check("Missing consumed ID + original V2 source order permits partial physical state MODEL",()=>{
         var l=new DebrisImpulseLedger();
         if(!Consume(l,id))return false;
         l.ClearForNewRun(); // legal public W06 API, misordered while fixture is still detached
         bool detached=true, kinematic=false, parented=false;
         int mutations=0;
         var r=new GuardedDockCommand(i=>DockResult.Allowed, i=>{
             // This exact commit ordering is verified separately against original W10 Git C#.
             kinematic=true;mutations++;
             parented=true;mutations++;
             detached=false;mutations++;
             if(!l.RearmAfterReattach(id))throw new InvalidOperationException("ID missing");
             return true;
         });
         bool threw=false;
         try {r.Reattach(0);}
         catch(InvalidOperationException){threw=true;}
         return threw && mutations==3 && !detached && kinematic && parented &&
                l.Count==0 && r.LastResult==DockResult.Allowed;
     });
     Check("Positive matching ledger rearm succeeds once",()=>{
         var l=new DebrisImpulseLedger();
         if(!Consume(l,id))return false;
         return l.RearmAfterReattach(id) && !l.Contains(id) &&
                !l.RearmAfterReattach(id);
     });
     Check("Fresh original W06 ledger does not restore consumed IDs",()=>{
         var l=new DebrisImpulseLedger();
         if(!Consume(l,id))return false;
         var fresh=new DebrisImpulseLedger();
         return !fresh.Contains(id) && fresh.Count==0 && Consume(fresh,id);
     });
     Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail+
         " NATIVE_UNITY_NOT_RUN SOURCE_ORDER_PRECONDITION_HOLD");
     return fail==0 ? 0 : 1;
 }
}
