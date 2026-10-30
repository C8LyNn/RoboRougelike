// B01-23 / P17-W09-WHEEL-SUPPORT-ARENA-001 R1
// Actual dimensions for two physically different opt-in Unity Rigidbody+WheelCollider rigs.
// These are authored fixture dimensions, NOT values measured from a Codex vehicle.
using System;

namespace B01.P17WheelSupportArena001
{
    public sealed class WheelRigRecipe
    {
        public readonly string Id;
        public readonly double Kilograms;
        public readonly double WidthMeters;
        public readonly double LengthMeters;
        public readonly double WheelbaseMeters;
        public readonly double TrackMeters;
        public readonly double WheelRadiusMeters;
        public readonly double SuspensionTravelMeters;
        public readonly double Spring;
        public readonly double Damper;

        public WheelRigRecipe(string id, double kg, double width, double length,
            double wheelbase, double track, double radius, double suspension,
            double spring, double damper)
        {
            Id=id; Kilograms=kg; WidthMeters=width; LengthMeters=length;
            WheelbaseMeters=wheelbase; TrackMeters=track;
            WheelRadiusMeters=radius; SuspensionTravelMeters=suspension;
            Spring=spring; Damper=damper;
        }

        public string InvalidReason()
        {
            if(string.IsNullOrWhiteSpace(Id)) return "EMPTY_RIG_ID";
            if(!Valid(Kilograms) || Kilograms<=0) return "INVALID_MASS";
            if(!Valid(WidthMeters) || !Valid(LengthMeters) ||
               WidthMeters<=0 || LengthMeters<=0) return "INVALID_BODY_SIZE";
            if(!Valid(WheelbaseMeters) || !Valid(TrackMeters) ||
               WheelbaseMeters<=0 || TrackMeters<=0 ||
               WheelbaseMeters>LengthMeters+0.4 ||
               TrackMeters<WidthMeters*0.6 || TrackMeters>WidthMeters+0.6)
                return "INVALID_WHEEL_FOOTPRINT";
            if(!Valid(WheelRadiusMeters) || !Valid(SuspensionTravelMeters) ||
               WheelRadiusMeters<=0 || SuspensionTravelMeters<=0 ||
               WheelRadiusMeters>1.0 || SuspensionTravelMeters>1.0)
                return "INVALID_WHEEL_ENVELOPE";
            if(!Valid(Spring) || !Valid(Damper) || Spring<=0 || Damper<=0)
                return "INVALID_SUSPENSION";
            return null;
        }

        public WheelAnchor[] Anchors()
        {
            string error=InvalidReason();
            if(error!=null) throw new ArgumentException(error);
            double x=TrackMeters*.5;
            double z=WheelbaseMeters*.5;
            return new[] {
                new WheelAnchor("front-left",-x,z),
                new WheelAnchor("front-right",x,z),
                new WheelAnchor("rear-left",-x,-z),
                new WheelAnchor("rear-right",x,-z)
            };
        }

        private static bool Valid(double value)
        { return !double.IsNaN(value) && !double.IsInfinity(value); }

        public static WheelRigRecipe Light()
        { return new WheelRigRecipe("P17-W09-LIGHT",110,1.2,1.65,1.26,1.03,.35,.26,22000,3500); }
        public static WheelRigRecipe Heavy()
        { return new WheelRigRecipe("P17-W09-HEAVY",620,2.25,2.65,2.16,1.98,.46,.31,36000,6000); }
    }

    public sealed class WheelAnchor
    {
        public readonly string Id;
        public readonly double X, Z;
        public WheelAnchor(string id,double x,double z) {Id=id;X=x;Z=z;}
    }
}
