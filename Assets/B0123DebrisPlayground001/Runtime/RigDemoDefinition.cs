using System;

namespace B01.DebrisPlayground001
{
    /// <summary>Isolated demonstrator dimensions. NOT canonical robot mass, balance or placement data.</summary>
    public readonly struct RigDemoDefinition
    {
        public readonly long PartId;
        public readonly string Name;
        public readonly double RootX;
        public readonly double ChassisWidth;
        public readonly double PartWidth;
        public readonly double Clearance;
        public readonly double PartMassKg;
        public readonly double InitialRadialSpeed;

        public RigDemoDefinition(long partId, string name, double rootX,
            double chassisWidth, double partWidth, double clearance, double partMassKg,
            double initialRadialSpeed)
        {
            PartId = partId;
            Name = name;
            RootX = rootX;
            ChassisWidth = chassisWidth;
            PartWidth = partWidth;
            Clearance = clearance;
            PartMassKg = partMassKg;
            InitialRadialSpeed = initialRadialSpeed;
            Validate();
        }

        public double PartOffsetX => (ChassisWidth + PartWidth) / 2 + Clearance;

        public void Validate()
        {
            if (PartId <= 0 || string.IsNullOrWhiteSpace(Name) ||
                !Finite(RootX, 100) ||
                !Positive(ChassisWidth, 20) || !Positive(PartWidth, 20) ||
                !Positive(Clearance, 5) || !Positive(PartMassKg, 1000) ||
                !Finite(InitialRadialSpeed, 20))
                throw new ArgumentOutOfRangeException(nameof(PartId), "Invalid demo-rig definition");
        }

        private static bool Finite(double x, double limit) =>
            !double.IsNaN(x) && !double.IsInfinity(x) && Math.Abs(x) <= limit;
        private static bool Positive(double x, double max) =>
            !double.IsNaN(x) && !double.IsInfinity(x) && x > 0 && x <= max;
    }

    public static class DebrisRigDemoCatalog
    {
        public static RigDemoDefinition Light => new RigDemoDefinition(
            2307001, "Light rig", -3.5, 1.4, 0.5, 0.25, 1.5, 0.0);
        public static RigDemoDefinition Heavy => new RigDemoDefinition(
            2307002, "Heavy rig", 3.5, 2.4, 0.7, 0.25, 12.0, -1.0);

        public static RigDemoDefinition ByIndex(int index)
        {
            switch (index)
            {
                case 0: return Light;
                case 1: return Heavy;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
