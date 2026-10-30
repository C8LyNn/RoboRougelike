using System;

namespace B01.SecureReattachV2
{
    public readonly struct SecureRigSpec
    {
        public readonly long PartId;
        public readonly string Name;
        public readonly double RootX, ChassisWidth, PartWidth, Clearance, PartMassKg, InitialSpeed;
        public SecureRigSpec(long id, string name, double x, double chassis, double part,
            double clearance, double massKg, double initialSpeed)
        {
            PartId=id; Name=name; RootX=x; ChassisWidth=chassis; PartWidth=part;
            Clearance=clearance; PartMassKg=massKg; InitialSpeed=initialSpeed;
            if (id<=0 || string.IsNullOrWhiteSpace(name) ||
                !Finite(x,100) || !Positive(chassis,20) || !Positive(part,20) ||
                !Positive(clearance,5) || !Positive(massKg,1000) ||
                !Finite(initialSpeed,20))
                throw new ArgumentOutOfRangeException(nameof(id));
        }
        public double PartOffsetX => (ChassisWidth + PartWidth)/2.0+Clearance;
        private static bool Finite(double x,double limit) => !double.IsNaN(x) && !double.IsInfinity(x) && Math.Abs(x)<=limit;
        private static bool Positive(double x,double limit) => Finite(x,limit) && x>0;
    }
    public static class SecureRigCatalog
    {
        public static SecureRigSpec Light => new SecureRigSpec(2310001, "Safe Light", -3.5,1.4,.5,.25,1.5,0);
        public static SecureRigSpec Heavy => new SecureRigSpec(2310002,"Safe Heavy",3.5,2.4,.7,.25,12,-1);
        public static SecureRigSpec At(int index) =>
            index==0?Light:index==1?Heavy:throw new ArgumentOutOfRangeException(nameof(index));
    }
}
