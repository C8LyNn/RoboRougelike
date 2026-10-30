using System;
using B01.P17WheelSupportArena001;
internal static class Program
{
    private static int n;
    static void Yes(bool okay,string title)
    { if(!okay) throw new InvalidOperationException("TEST_FAIL "+title);
      n++;Console.WriteLine("SOURCE_ASSERT_PASS "+title); }
    static bool Close(double a,double b) => Math.Abs(a-b)<1e-9;
    static void Fail(WheelRigRecipe p,string expected)
    { Yes(p.InvalidReason()==expected,"reject "+expected); }
    static WheelRigRecipe Custom(double kg=100,double width=1.2,double length=1.7,
        double wb=1.3,double track=1.1,double radius=.35,double suspension=.25,
        double spring=22000,double damper=3500)
    { return new WheelRigRecipe("CUSTOM",kg,width,length,wb,track,radius,suspension,spring,damper); }
    static void Main()
    {
        var light=WheelRigRecipe.Light();
        var heavy=WheelRigRecipe.Heavy();
        Yes(light.InvalidReason()==null,"original LIGHT physical fixture geometry valid");
        Yes(heavy.InvalidReason()==null,"original HEAVY physical fixture geometry valid");
        Yes(!Close(light.Kilograms,heavy.Kilograms),"two physically different mass rigs");
        Yes(!Close(light.WheelbaseMeters,heavy.WheelbaseMeters),"two different wheelbases");
        Yes(!Close(light.TrackMeters,heavy.TrackMeters),"two different support widths");
        Yes(!Close(light.WheelRadiusMeters,heavy.WheelRadiusMeters),"distinct wheel radii");
        Yes(!Close(light.Spring,heavy.Spring),"distinct suspension spring rates");
        Yes(Close(light.Kilograms,110),"authored light Rigidbody mass in kg");
        Yes(Close(heavy.Kilograms,620),"authored heavy Rigidbody mass in kg");
        foreach(var spec in new[]{light,heavy})
        {
            var a=spec.Anchors();
            Yes(a.Length==4,spec.Id+" actual 4 corner anchors");
            Yes(a[0].Id=="front-left" && a[1].Id=="front-right" &&
                a[2].Id=="rear-left" && a[3].Id=="rear-right",spec.Id+" stable wheel IDs");
            Yes(Close(a[0].X,-a[1].X),spec.Id+" front L/R symmetry");
            Yes(Close(a[2].X,-a[3].X),spec.Id+" rear L/R symmetry");
            Yes(Close(a[0].Z,-a[2].Z),spec.Id+" actual signed front/back");
            Yes(Close(a[1].Z,-a[3].Z),spec.Id+" actual signed left/right");
            Yes(Close(a[0].Z-a[2].Z,spec.WheelbaseMeters),spec.Id+" wheelbase metres");
            Yes(Close(a[1].X-a[0].X,spec.TrackMeters),spec.Id+" track metres");
            Yes(a[0].X<a[1].X && a[0].Z>a[2].Z,spec.Id+" oriented 2D wheel support rectangle");
            Yes(spec.WheelRadiusMeters>0 && spec.SuspensionTravelMeters>0,
                spec.Id+" actual physical suspension lengths");
        }
        Fail(new WheelRigRecipe("",100,1.2,1.7,1.3,1.1,.35,.25,22000,3500),"EMPTY_RIG_ID");
        Fail(Custom(kg:0),"INVALID_MASS");
        Fail(Custom(kg:-1),"INVALID_MASS");
        Fail(Custom(kg:double.NaN),"INVALID_MASS");
        Fail(Custom(kg:double.PositiveInfinity),"INVALID_MASS");
        Fail(Custom(width:0),"INVALID_BODY_SIZE");
        Fail(Custom(length:-1),"INVALID_BODY_SIZE");
        Fail(Custom(width:double.NaN),"INVALID_BODY_SIZE");
        Fail(Custom(wb:0),"INVALID_WHEEL_FOOTPRINT");
        Fail(Custom(track:0),"INVALID_WHEEL_FOOTPRINT");
        Fail(Custom(wb:9),"INVALID_WHEEL_FOOTPRINT");
        Fail(Custom(track:9),"INVALID_WHEEL_FOOTPRINT");
        Fail(Custom(track:.1),"INVALID_WHEEL_FOOTPRINT");
        Fail(Custom(track:double.NaN),"INVALID_WHEEL_FOOTPRINT");
        Fail(Custom(radius:0),"INVALID_WHEEL_ENVELOPE");
        Fail(Custom(radius:2),"INVALID_WHEEL_ENVELOPE");
        Fail(Custom(suspension:0),"INVALID_WHEEL_ENVELOPE");
        Fail(Custom(suspension:2),"INVALID_WHEEL_ENVELOPE");
        Fail(Custom(spring:0),"INVALID_SUSPENSION");
        Fail(Custom(damper:0),"INVALID_SUSPENSION");
        Fail(Custom(spring:double.NaN),"INVALID_SUSPENSION");
        Fail(Custom(damper:double.PositiveInfinity),"INVALID_SUSPENSION");
        var unsafeSpec=Custom(wb:-1);
        bool threw=false;
        try{unsafeSpec.Anchors();}catch(ArgumentException e){threw=e.Message.Contains("INVALID_WHEEL_FOOTPRINT");}
        Yes(threw,"invalid geometries cannot produce anchors");
        Console.WriteLine("ACTUAL_ORIGINAL_RECIPE_ASSERTIONS_PASS="+n+" NATIVE_PHYSX_NOT_RUN");
    }
}
