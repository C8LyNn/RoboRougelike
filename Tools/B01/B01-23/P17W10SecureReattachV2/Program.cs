using System;
using System.Collections.Generic;
using B01.SecureReattachV2;

internal static class Program
{
    static int pass,fail;
    static void Test(string name,Func<bool> check)
    {
        try { if(check()){pass++;Console.WriteLine("PASS "+name);}
              else {fail++;Console.WriteLine("FAIL "+name);} }
        catch(Exception e){fail++;Console.WriteLine("FAIL "+name+" "+e);}
    }
    static DockResult Check(
        bool detached=true,bool dynamicBody=true,double distance=1,double reach=3,
        int layer=-1,int count=-1,int capacity=128,double tolerance=.005,
        IReadOnlyList<DockHit>? contacts=null)
    {
        var hits=contacts ?? Array.Empty<DockHit>();
        return SecureDockPolicy.Check(detached,dynamicBody,distance,reach,layer,
            count<0?hits.Count:count,capacity,tolerance,hits);
    }
    static void MainTests()
    {
        Test("all layer mask",()=>SecureDockPolicy.AllLayers == -1);
        Test("overlap buffer 128",()=>SecureDockPolicy.Capacity==128);
        Test("clear and detached",()=>Check()==DockResult.Allowed);
        Test("not detached",()=>Check(detached:false)==DockResult.NotDetached);
        Test("kinematic body",()=>Check(dynamicBody:false)==DockResult.InvalidInput);
        Test("zero layer exclusion",()=>Check(layer:0)==DockResult.InvalidInput);
        Test("default layer mask exclusion",()=>Check(layer:1)==DockResult.InvalidInput);
        Test("layer31-only exclusion",()=>Check(layer:unchecked((int)0x80000000))==DockResult.InvalidInput);
        Test("any partial layers invalid",()=>Check(layer:-2)==DockResult.InvalidInput);
        Test("NaN distance invalid",()=>Check(distance:double.NaN)==DockResult.InvalidInput);
        Test("Infinity distance invalid",()=>Check(distance:double.PositiveInfinity)==DockResult.InvalidInput);
        Test("negative distance invalid",()=>Check(distance:-1)==DockResult.InvalidInput);
        Test("distance too far",()=>Check(distance:3.001)==DockResult.OutOfReach);
        Test("distance exact boundary",()=>Check(distance:3)==DockResult.Allowed);
        Test("zero distance",()=>Check(distance:0)==DockResult.Allowed);
        Test("zero reach invalid",()=>Check(reach:0)==DockResult.InvalidInput);
        Test("reach beyond 50 invalid",()=>Check(reach:51)==DockResult.InvalidInput);
        Test("NaN reach invalid",()=>Check(reach:double.NaN)==DockResult.InvalidInput);
        Test("Negative tolerance invalid",()=>Check(tolerance:-.001)==DockResult.InvalidInput);
        Test("NaN tolerance invalid",()=>Check(tolerance:double.NaN)==DockResult.InvalidInput);
        Test("high tolerance invalid",()=>Check(tolerance:.051)==DockResult.InvalidInput);
        Test("count 999 invalid",()=>Check(count:999)==DockResult.InvalidInput);
        Test("count negative invalid",()=>SecureDockPolicy.Check(true,true,1,3,-1,-1,128,.005,Array.Empty<DockHit>())==DockResult.InvalidInput);
        Test("wrong count invalid",()=>Check(count:1)==DockResult.InvalidInput);
        Test("capacity less 4 invalid",()=>Check(capacity:3)==DockResult.InvalidInput);
        Test("capacity over 1024 invalid",()=>Check(capacity:1025)==DockResult.InvalidInput);
        Test("null hits invalid",()=>SecureDockPolicy.Check(true,true,1,3,-1,0,128,.005,null!)==DockResult.InvalidInput);
        Test("unsupported shape blocked",()=>Check(contacts:new[]{new DockHit(false,false,false,0)})==DockResult.Obstructed);
        Test("NaN depth blocked",()=>Check(contacts:new[]{new DockHit(false,false,true,double.NaN)})==DockResult.Obstructed);
        Test("negative depth blocked",()=>Check(contacts:new[]{new DockHit(false,false,true,-.1)})==DockResult.Obstructed);
        Test("positive infinity depth blocked",()=>Check(contacts:new[]{new DockHit(false,false,true,double.PositiveInfinity)})==DockResult.Obstructed);
        Test("positive overlap blocked",()=>Check(contacts:new[]{new DockHit(false,false,true,.7)})==DockResult.Obstructed);
        Test("touch exactly tolerance allowed",()=>Check(contacts:new[]{new DockHit(false,false,true,.005)})==DockResult.Allowed);
        Test("touch just over tolerance blocked",()=>Check(contacts:new[]{new DockHit(false,false,true,.0051)})==DockResult.Obstructed);
        Test("own body ignored",()=>Check(contacts:new[]{new DockHit(true,false,false,double.NaN)})==DockResult.Allowed);
        Test("trigger ignored",()=>Check(contacts:new[]{new DockHit(false,true,false,double.NaN)})==DockResult.Allowed);
        Test("second collider obstructs",()=>Check(contacts:new[]{new DockHit(true,false,true,0),new DockHit(false,false,true,.1)})==DockResult.Obstructed);
        Test("query saturation failclosed",()=>{
            var hits=new DockHit[128];
            for(int i=0;i<hits.Length;i++)hits[i]=new DockHit(true,false,true,0);
            return Check(contacts:hits)==DockResult.QuerySaturated;
        });
        Test("no saturation at 127 clear",()=>{
            var hits=new DockHit[127];
            for(int i=0;i<hits.Length;i++)hits[i]=new DockHit(true,false,true,0);
            return Check(contacts:hits)==DockResult.Allowed;
        });
        Test("two different demo rigs",()=>SecureRigCatalog.Light.PartMassKg==1.5 && SecureRigCatalog.Heavy.PartMassKg==12
            && SecureRigCatalog.Light.PartId!=SecureRigCatalog.Heavy.PartId);
        Test("separate positions/mounts",()=>SecureRigCatalog.Light.RootX<0 && SecureRigCatalog.Heavy.RootX>0
            && SecureRigCatalog.Light.PartOffsetX>0 && SecureRigCatalog.Heavy.PartOffsetX>0);
        Test("invalid rig index throws",()=>{
            try{SecureRigCatalog.At(2);return false;}catch(ArgumentOutOfRangeException){return true;}
        });
        Test("invalid rig rejects negative ID",()=>{
            try{new SecureRigSpec(-1,"invalid",0,1,1,.2,1,0);return false;}catch(ArgumentOutOfRangeException){return true;}
        });
        Test("invalid rig rejects zero mass",()=>{
            try{new SecureRigSpec(1,"invalid",0,1,1,.2,0,0);return false;}catch(ArgumentOutOfRangeException){return true;}
        });
        Test("invalid rig rejects NaN",()=>{
            try{new SecureRigSpec(1,"invalid",double.NaN,1,1,.2,1,0);return false;}catch(ArgumentOutOfRangeException){return true;}
        });

        foreach(string name in new[]{"Reattach","TrySafeReattach","ReattachFromGui"})
        {
            Test(name+" blocked never invokes original physical commit",()=>{
                int inspections=0,mutations=0;
                var router=new GuardedDockCommand(i=>{inspections++;return DockResult.Obstructed;},
                    i=>{mutations++;return true;});
                bool result=name=="Reattach"?router.Reattach(0):
                    name=="TrySafeReattach"?router.TrySafeReattach(0):router.ReattachFromGui(0);
                return !result&&inspections==1&&mutations==0&&router.LastResult==DockResult.Obstructed;
            });
            Test(name+" valid reattachment exactly once through safety inspector",()=>{
                int inspections=0,commits=0;
                var router=new GuardedDockCommand(i=>{inspections++;return DockResult.Allowed;},
                    i=>{commits++;return true;});
                bool allowed=name=="Reattach"?router.Reattach(1):
                    name=="TrySafeReattach"?router.TrySafeReattach(1):router.ReattachFromGui(1);
                return allowed&&inspections==1&&commits==1&&router.LastResult==DockResult.Allowed;
            });
            Test(name+" invalid index never inspects nor mutates",()=>{
                int inspections=0,commits=0;
                var router=new GuardedDockCommand(i=>{inspections++;return DockResult.Allowed;},
                    i=>{commits++;return true;});
                bool bad=name=="Reattach"?router.Reattach(-1):
                    name=="TrySafeReattach"?router.TrySafeReattach(2):router.ReattachFromGui(3);
                return !bad&&inspections==0&&commits==0&&router.LastResult==DockResult.InvalidInput;
            });
        }
        Test("reentrant legacy caller cannot mutate after nested callback",()=>{
            int calls=0;GuardedDockCommand? router=null;
            router=new GuardedDockCommand(i=>{
                if(router!.ReattachFromGui(i))throw new Exception("reentrancy bypassed");
                return DockResult.Obstructed;
            },i=>{calls++;return true;});
            return !router.Reattach(0)&&calls==0;
        });
        Test("exceptions release lock without unsafe commit",()=>{
            int calls=0,inspections=0;
            var router=new GuardedDockCommand(i=>{inspections++;if(inspections==1)throw new InvalidOperationException();return DockResult.Obstructed;},
                i=>{calls++;return true;});
            try{router.Reattach(0);}catch(InvalidOperationException){}
            return !router.Reattach(0)&&inspections==2&&calls==0;
        });
        Test("original command factory rejects missing inspector",()=>{
            try{new GuardedDockCommand(null!,_=>true);return false;}catch(ArgumentNullException){return true;}
        });
        Test("original command factory rejects missing committer",()=>{
            try{new GuardedDockCommand(_=>DockResult.Allowed,null!);return false;}catch(ArgumentNullException){return true;}
        });
        // 1024 actual public-call combinations; each rejected result must leave commit untouched.
        DockResult[] verdicts={DockResult.Allowed,DockResult.NotDetached,DockResult.InvalidInput,
            DockResult.OutOfReach,DockResult.QuerySaturated,DockResult.Obstructed};
        for(int seed=0;seed<128;seed++)
        {
            int s=seed;
            Test("three route combinations / seed "+seed,()=>{
                int inspected=0,committed=0;
                var router=new GuardedDockCommand(i=>{inspected++;return verdicts[(s+i)%verdicts.Length];},
                    i=>{committed++;return true;});
                bool[] got={router.Reattach(0),router.TrySafeReattach(1),router.ReattachFromGui(0)};
                DockResult[] expected={verdicts[s%6],verdicts[(s+1)%6],verdicts[s%6]};
                int expectedCommits=0;
                for(int i=0;i<3;i++){
                    bool allowed=expected[i]==DockResult.Allowed;
                    if(got[i]!=allowed)return false;
                    if(allowed)expectedCommits++;
                }
                return inspected==3 && committed==expectedCommits;
            });
        }
    }
    static int Main()
    {
        MainTests();
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        return fail==0?0:1;
    }
}
