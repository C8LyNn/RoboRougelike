using System;
using System.Collections.Generic;
using B01.ReattachClearance001;

internal static class Program
{
    private static int passed,failed;
    private static readonly List<ContactObservation> Empty = new List<ContactObservation>();
    private static readonly ContactObservation Own = new ContactObservation(true,false,true,0);
    private static readonly ContactObservation Trigger = new ContactObservation(false,true,true,0);
    private static readonly ContactObservation Clear = new ContactObservation(false,false,true,0);
    private static readonly ContactObservation Touch = new ContactObservation(false,false,true,0.005);
    private static readonly ContactObservation Block = new ContactObservation(false,false,true,0.006);
    private static DockVerdict Check(bool detached=true,bool dynamic=true,double distance=1,double reach=3,
        int returned=-1,int capacity=64,double tolerance=0.005,IReadOnlyList<ContactObservation> hits=null)
    {
        var contacts=hits??Empty;
        return DockClearancePolicy.Evaluate(detached,dynamic,distance,reach,returned<0?contacts.Count:returned,capacity,tolerance,contacts);
    }
    private static void Test(string name,Func<bool> predicate)
    {
        try { if(predicate()){passed++; Console.WriteLine("PASS "+name);}
            else{failed++;Console.WriteLine("FAIL "+name);} }
        catch(Exception error){failed++;Console.WriteLine("FAIL "+name+" :: "+error.Message);}
    }
    private static void MainTests()
    {
        Test("empty clearance approves",()=>Check()==DockVerdict.Allowed);
        Test("not detached short circuits",()=>Check(detached:false)==DockVerdict.NotDetached);
        Test("kinematic rejects",()=>Check(dynamic:false)==DockVerdict.InvalidInput);
        Test("negative travel",()=>Check(distance:-1)==DockVerdict.InvalidInput);
        Test("NaN travel",()=>Check(distance:double.NaN)==DockVerdict.InvalidInput);
        Test("infinite travel",()=>Check(distance:double.PositiveInfinity)==DockVerdict.InvalidInput);
        Test("zero reach",()=>Check(reach:0)==DockVerdict.InvalidInput);
        Test("negative reach",()=>Check(reach:-1)==DockVerdict.InvalidInput);
        Test("NaN reach",()=>Check(reach:double.NaN)==DockVerdict.InvalidInput);
        Test("infinite reach",()=>Check(reach:double.PositiveInfinity)==DockVerdict.InvalidInput);
        Test("reach beyond 50",()=>Check(reach:51)==DockVerdict.InvalidInput);
        Test("reach exact boundary allowed",()=>Check(distance:3)==DockVerdict.Allowed);
        Test("reach epsilon 1e-8 allowed",()=>Check(distance:3+1e-8)==DockVerdict.Allowed);
        Test("reach exceeded",()=>Check(distance:3.001)==DockVerdict.OutOfReach);
        Test("query capacity 3 invalid",()=>Check(capacity:3)==DockVerdict.InvalidInput);
        Test("query capacity 513 invalid",()=>Check(capacity:513)==DockVerdict.InvalidInput);
        Test("negative hit count invalid",()=>DockClearancePolicy.Evaluate(true,true,1,3,-1,64,.005,Empty)==DockVerdict.InvalidInput);
        Test("hit mismatch invalid",()=>Check(returned:1)==DockVerdict.InvalidInput);
        Test("hit count > capacity invalid",()=>Check(returned:65,capacity:64)==DockVerdict.InvalidInput);
        Test("null contacts invalid",()=>DockClearancePolicy.Evaluate(true,true,1,3,0,64,.005,null)==DockVerdict.InvalidInput);
        Test("capacity full fails closed",()=>{
            var hits=new List<ContactObservation>();
            for(int i=0;i<64;i++)hits.Add(Own);
            return Check(hits:hits)==DockVerdict.QuerySaturated;
        });
        Test("not saturated at 63",()=>{
            var hits=new List<ContactObservation>();
            for(int i=0;i<63;i++)hits.Add(Own);
            return Check(hits:hits)==DockVerdict.Allowed;
        });
        Test("self part ignored",()=>Check(hits:new[]{Own})==DockVerdict.Allowed);
        Test("trigger ignored",()=>Check(hits:new[]{Trigger})==DockVerdict.Allowed);
        Test("owned and trigger ignored",()=>Check(hits:new[]{Own,Trigger})==DockVerdict.Allowed);
        Test("supported no-depth contact allowed",()=>Check(hits:new[]{Clear})==DockVerdict.Allowed);
        Test("at tolerance supported contact allowed",()=>Check(hits:new[]{Touch})==DockVerdict.Allowed);
        Test("greater than tolerance blocks",()=>Check(hits:new[]{Block})==DockVerdict.Blocked);
        Test("blocked when second collider penetrates",()=>Check(hits:new[]{Clear,Block})==DockVerdict.Blocked);
        Test("unsupported broadphase collider fails closed",()=>Check(hits:new[]{new ContactObservation(false,false,false,0)})==DockVerdict.Blocked);
        Test("nan depth blocks",()=>Check(hits:new[]{new ContactObservation(false,false,true,double.NaN)})==DockVerdict.Blocked);
        Test("positive infinity depth blocks",()=>Check(hits:new[]{new ContactObservation(false,false,true,double.PositiveInfinity)})==DockVerdict.Blocked);
        Test("negative depth blocks",()=>Check(hits:new[]{new ContactObservation(false,false,true,-0.1)})==DockVerdict.Blocked);
        Test("invalid negative tolerance",()=>Check(tolerance:-.01)==DockVerdict.InvalidInput);
        Test("invalid too large tolerance",()=>Check(tolerance:.051)==DockVerdict.InvalidInput);
        Test("NaN tolerance",()=>Check(tolerance:double.NaN)==DockVerdict.InvalidInput);
        Test("trigger ignores NaN depth",()=>Check(hits:new[]{new ContactObservation(false,true,false,double.NaN)})==DockVerdict.Allowed);
        Test("own detached body ignores NaN depth",()=>Check(hits:new[]{new ContactObservation(true,false,false,double.NaN)})==DockVerdict.Allowed);
        Test("saturation always wins before reach",()=>{
            var hits=new List<ContactObservation>();
            for(int i=0;i<64;i++)hits.Add(Own);
            return Check(distance:20,hits:hits)==DockVerdict.QuerySaturated;
        });
        Test("reach check before contact",()=>Check(distance:10,hits:new[]{Block})==DockVerdict.OutOfReach);
        Test("touch under small tolerance blocks",()=>Check(tolerance:.001,hits:new[]{Touch})==DockVerdict.Blocked);
        Test("tolerance zero allows exactly touching",()=>Check(tolerance:0,hits:new[]{Clear})==DockVerdict.Allowed);
        Test("zero actual travel accepted",()=>Check(distance:0)==DockVerdict.Allowed);
        Test("max bound 50 accepted",()=>Check(distance:49.0,reach:50)==DockVerdict.Allowed);
    }
    private static int Main()
    {
        MainTests();
        Console.WriteLine("TOTAL "+(passed+failed)+" PASS "+passed+" FAIL "+failed);
        return failed==0?0:1;
    }
}
