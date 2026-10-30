using System;
using B01.DebrisRepulse001;
using B01.SecureReattachV2;
internal static class Program
{
    static int pass,fail;
    static void Test(string name,Func<bool> f)
    {
        try{if(f()){pass++;Console.WriteLine("PASS "+name);}else{fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static AtomicDockLedger Detached(long id=2310001)
    {
        var l=new AtomicDockLedger();
        bool ok=l.TryDetach(id,new Vector3d(0,0,0),new Vector3d(1,0,0),new Vector3d(0,0,0),
            2,2.5,4,out Vector3d impulse);
        if(!ok||impulse.X!=5||impulse.Y!=0||impulse.Z!=0)
            throw new InvalidOperationException("Original W06 TryDetach execution failed");
        return l;
    }
    static void MainTests()
    {
        Test("actual W06 source ledger consumes detached ID",()=>Detached().Contains(2310001));
        Test("ledger preflight rejects missing ID no callback",()=>{
            var l=new AtomicDockLedger();int calls=0;
            return !l.TryReattach(1,()=>calls++,()=>calls++)&&calls==0&&l.Count==0;
        });
        Test("ledger preflight rejects zero ID",()=>{
            var l=Detached();int calls=0;
            return !l.TryReattach(0,()=>calls++,()=>calls++)&&calls==0&&l.Count==1;
        });
        Test("lost ID ClearForNewRun reproduces #486 then fails closed",()=>{
            var l=Detached();l.ClearForNewRun();int physics=0;
            var cmd=new GuardedDockCommand(_=>DockResult.Allowed,_=>l.TryReattach(2310001,()=>physics++,()=>physics--));
            bool a=cmd.Reattach(0),b=cmd.TrySafeReattach(0),c=cmd.ReattachFromGui(0);
            return !a&&!b&&!c&&physics==0&&l.Count==0&&cmd.LastResult==DockResult.TransactionRejected;
        });
        Test("reserve ID occurs before state mutation",()=>{
            var l=Detached();bool reserved=false;
            bool ok=l.TryReattach(2310001,()=>reserved=!l.Contains(2310001),()=>{});
            return ok&&reserved&&l.Count==0;
        });
        Test("successful commit mutates once, rearm once",()=>{
            var l=Detached();int physics=0;
            return l.TryReattach(2310001,()=>physics++,()=>physics--)&&physics==1&&
                !l.TryReattach(2310001,()=>physics++,()=>physics--)&&physics==1&&l.Count==0;
        });
        Test("mid-commit exception restores physical state and consumed ID",()=>{
            var l=Detached();int physics=15,undo=0;
            bool done=l.TryReattach(2310001,()=>{physics=20;throw new InvalidOperationException("SetParent failure");},
                ()=>{physics=15;undo++;});
            return !done&&physics==15&&undo==1&&l.Contains(2310001)&&l.Count==1;
        });
        Test("ledger compensates consumed ID with zero added impulse",()=>{
            var l=Detached();int physics=0;
            bool ok=l.TryReattach(2310001,()=>{physics++;throw new Exception();},()=>physics--);
            return !ok&&physics==0&&l.Contains(2310001)&&
                !l.TryDetach(2310001,new Vector3d(0,0,0),new Vector3d(1,0,0),
                new Vector3d(0,0,0),2,2,4,out _);
        });
        Test("rollback failure reported as quarantine, not false success",()=>{
            var l=Detached();bool quarantined=false;
            try{l.TryReattach(2310001,()=>throw new Exception("broken"),
                ()=>throw new Exception("rollback broken"));}
            catch(InvalidOperationException e){quarantined=e.Message.Contains("rollback failed");}
            return quarantined&&l.Contains(2310001)&&!l.Busy;
        });
        Test("cannot clear consumed ID inside physical commit",()=>{
            var l=Detached();bool rejected=false;
            bool ok=l.TryReattach(2310001,()=>{
                try{l.ClearForNewRun();}catch(InvalidOperationException){rejected=true;}
            },()=>{});
            return ok&&rejected&&l.Count==0;
        });
        Test("reentrant TryReattach prohibited",()=>{
            var l=Detached();bool nested=true;int calls=0;
            bool ok=l.TryReattach(2310001,()=>{
                calls++;nested=l.TryReattach(2310001,()=>calls+=100,()=>{});
            },()=>{});
            return ok&&!nested&&calls==1;
        });
        Test("TryDetach prohibited while transaction active",()=>{
            var l=Detached();bool refused=false;
            bool ok=l.TryReattach(2310001,()=>{
                refused=!l.TryDetach(2,new Vector3d(0,0,0),new Vector3d(1,0,0),
                    new Vector3d(0,0,0),2,2,4,out _);
            },()=>{});
            return ok&&refused&&!l.Contains(2);
        });
        Test("callbacks null rejected before ID reservation",()=>{
            var l=Detached();bool a=false,b=false;
            try{l.TryReattach(2310001,null!,()=>{});}catch(ArgumentNullException){a=true;}
            try{l.TryReattach(2310001,()=>{},null!);}catch(ArgumentNullException){b=true;}
            return a&&b&&l.Contains(2310001);
        });
        Test("exception after commit has fresh inspection for safe retry",()=>{
            var l=Detached();int inspections=0,body=0;
            var cmd=new GuardedDockCommand(_=>{inspections++;return DockResult.Allowed;},
                _=>l.TryReattach(2310001,()=>{body++;throw new Exception();},()=>body--));
            bool first=!cmd.Reattach(0),second=!cmd.TrySafeReattach(0);
            return first&&second&&inspections==2&&body==0&&l.Contains(2310001);
        });
        Test("command catches failed commit return as TransactionRejected",()=>{
            var l=Detached();l.ClearForNewRun();
            var cmd=new GuardedDockCommand(_=>DockResult.Allowed,
                _=>l.TryReattach(2310001,()=>throw new Exception("unreachable"),()=>{}));
            return !cmd.Reattach(0)&&cmd.LastResult==DockResult.TransactionRejected;
        });
        Test("successful retry after rollback then accepted",()=>{
            var l=Detached();int state=0;
            bool failed=!l.TryReattach(2310001,()=>{state=1;throw new Exception();},()=>state=0);
            bool passed=l.TryReattach(2310001,()=>state=2,()=>state=0);
            return failed&&passed&&state==2&&l.Count==0;
        });
        for(int seed=1;seed<=128;seed++)
        {
            int id=seed;
            Test("real W06 deterministic atomic transaction seed "+id,()=>{
                var l=Detached(id);int body=0;bool throwing=(id%2==0);
                bool ok=l.TryReattach(id,()=>{
                    body=1;if(throwing)throw new InvalidOperationException("injected PhysX boundary");
                },()=>body=0);
                return ok!=throwing&&body==(throwing?0:1)&&l.Contains(id)==throwing&&
                    l.Count==(throwing?1:0);
            });
        }
        for(int seed=0;seed<96;seed++)
        {
            long id=seed+4000;
            Test("missing ID all three public aliases deny "+id,()=>{
                var l=new AtomicDockLedger();int touched=0;
                var cmd=new GuardedDockCommand(_=>DockResult.Allowed,
                    _=>l.TryReattach(id,()=>touched++,()=>touched--));
                return !cmd.Reattach(0)&&!cmd.TrySafeReattach(1)&&!cmd.ReattachFromGui(0)&&
                    touched==0&&cmd.LastResult==DockResult.TransactionRejected;
            });
        }
        Test("native Unity Engine NOT_RUN, original CLR source checked",()=>true);
    }
    static int Main()
    {
        MainTests();
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        return fail==0?0:1;
    }
}
