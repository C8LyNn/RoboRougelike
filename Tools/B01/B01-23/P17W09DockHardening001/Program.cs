using System;
using B01.DockHardening001;
using B01.ReattachClearance001;
internal static class Program
{
    private static int pass, fail;
    private static void Test(string name,Func<bool> fn)
    {
        try {if(fn()) {pass++;Console.WriteLine("PASS "+name);}
             else {fail++;Console.WriteLine("FAIL "+name);}}
        catch(Exception e) {fail++;Console.WriteLine("FAIL "+name+" "+e.Message);}
    }
    private static HardenedDockVerdict Decision(bool armed=true,bool unsafeW07=false,bool unsafeW08=false,
        int layers= -1,DockVerdict all=DockVerdict.Allowed,DockVerdict previous=DockVerdict.Allowed) =>
        HardenedDockPolicy.Evaluate(armed,unsafeW07,unsafeW08,layers,all,previous);
    private static int Main()
    {
        Test("sole all-layer mask is -1",()=>HardenedDockPolicy.AllPhysicsLayers==-1);
        Test("allowed only fully armed/hidden/all-layer/double-clear",()=>Decision()==HardenedDockVerdict.Allowed);
        Test("not armed rejected",()=>Decision(armed:false)==HardenedDockVerdict.NotArmed);
        Test("legacy W07 GUI bypass rejected",()=>Decision(unsafeW07:true)==HardenedDockVerdict.UnsafeLegacyUi);
        Test("legacy W08 GUI bypass rejected",()=>Decision(unsafeW08:true)==HardenedDockVerdict.UnsafeLegacyUi);
        Test("both legacy GUIs enabled rejected",()=>Decision(unsafeW07:true,unsafeW08:true)==HardenedDockVerdict.UnsafeLegacyUi);
        Test("zero-layer original W08 mask cannot justify dock",()=>Decision(layers:0)==HardenedDockVerdict.InvalidFullLayerQuery);
        Test("default layer-only mask rejected",()=>Decision(layers:1)==HardenedDockVerdict.InvalidFullLayerQuery);
        Test("layer8-only mask rejected",()=>Decision(layers:256)==HardenedDockVerdict.InvalidFullLayerQuery);
        Test("mask minus2 excluding default rejected",()=>Decision(layers:-2)==HardenedDockVerdict.InvalidFullLayerQuery);
        Test("new all-layer obstacle rejects",()=>Decision(all:DockVerdict.Blocked)==HardenedDockVerdict.NewAuditRejected);
        Test("new saturated all-layer query rejects",()=>Decision(all:DockVerdict.QuerySaturated)==HardenedDockVerdict.NewAuditRejected);
        Test("out of reach rejects",()=>Decision(all:DockVerdict.OutOfReach)==HardenedDockVerdict.NewAuditRejected);
        Test("not detached rejects",()=>Decision(all:DockVerdict.NotDetached)==HardenedDockVerdict.NewAuditRejected);
        Test("invalid hit data rejects",()=>Decision(all:DockVerdict.InvalidInput)==HardenedDockVerdict.NewAuditRejected);
        Test("prior W08 blocker rejects",()=>Decision(previous:DockVerdict.Blocked)==HardenedDockVerdict.PreviousGateRejected);
        Test("prior W08 invalid rejects",()=>Decision(previous:DockVerdict.InvalidInput)==HardenedDockVerdict.PreviousGateRejected);
        Test("prior W08 out of reach rejects",()=>Decision(previous:DockVerdict.OutOfReach)==HardenedDockVerdict.PreviousGateRejected);
        Test("prior W08 saturated rejects",()=>Decision(previous:DockVerdict.QuerySaturated)==HardenedDockVerdict.PreviousGateRejected);
        Test("actual original W08 can falsely allow with omitted collider input",()=>{
            var empty=Array.Empty<ContactObservation>();
            return DockClearancePolicy.Evaluate(true,true,1,3,0,64,.005,empty)==DockVerdict.Allowed &&
                   Decision(layers:0)==HardenedDockVerdict.InvalidFullLayerQuery;
        });
        Test("actual original W08 detects blocker when collider included",()=>{
            var contacts=new[]{new ContactObservation(false,false,true,0.7)};
            return DockClearancePolicy.Evaluate(true,true,1,3,1,64,.005,contacts)==DockVerdict.Blocked &&
                   Decision(all:DockVerdict.Blocked)==HardenedDockVerdict.NewAuditRejected;
        });
        Test("original W08 ignores trigger",()=>{
            var contacts=new[]{new ContactObservation(false,true,true,0.7)};
            return DockClearancePolicy.Evaluate(true,true,1,3,1,64,.005,contacts)==DockVerdict.Allowed;
        });
        Test("actual original W08 saturated query denied",()=>{
            var contacts=new ContactObservation[64];
            for(int i=0;i<contacts.Length;i++)contacts[i]=new ContactObservation(true,false,true,0);
            return DockClearancePolicy.Evaluate(true,true,1,3,64,64,.005,contacts)==DockVerdict.QuerySaturated;
        });
        Test("all-layer masking rejects even prior W08 Allowed",()=>Decision(layers:0,all:DockVerdict.Allowed,previous:DockVerdict.Allowed)!=HardenedDockVerdict.Allowed);
        Test("blocker masks cannot be hidden by old W08",()=>Decision(all:DockVerdict.Blocked,previous:DockVerdict.Allowed)!=HardenedDockVerdict.Allowed);
        Test("legacy UI cannot be hidden by successful physics",()=>Decision(unsafeW07:true,all:DockVerdict.Allowed,previous:DockVerdict.Allowed)!=HardenedDockVerdict.Allowed);
        int variants=0;
        foreach(bool armed in new[]{false,true})
        foreach(bool w07 in new[]{false,true})
        foreach(bool w08 in new[]{false,true})
        foreach(int layers in new[]{-1,0})
        foreach(DockVerdict aud in new[]{DockVerdict.Allowed,DockVerdict.Blocked})
        foreach(DockVerdict old in new[]{DockVerdict.Allowed,DockVerdict.Blocked})
        {
            bool expected=armed&&!w07&&!w08&&layers==-1&&aud==DockVerdict.Allowed&&old==DockVerdict.Allowed;
            int index=variants++;
            Test("truth table variant "+index,()=>(Decision(armed,w07,w08,layers,aud,old)==HardenedDockVerdict.Allowed)==expected);
        }
        Test("truth table exactly 64 combinations",()=>variants==64);
        Console.WriteLine("TOTAL "+(pass+fail)+" PASS "+pass+" FAIL "+fail);
        return fail==0?0:1;
    }
}
