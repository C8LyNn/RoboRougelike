namespace RoboRougelike.Mechanics.EMP
{
    public enum EmpPulseFailure
    {
        None, NotOperational, CoolingDown, MissingEnergySource, InsufficientEnergy,
        ColliderQueryOverflow, InvalidConfiguration
    }

    public struct EmpPulseReport
    {
        public EmpPulseFailure Failure;
        public int ReceiversDetected;
        public int ReceiversAffected;
        public int ReceiversOccluded;
        public float Radius;
        public bool Fired => Failure == EmpPulseFailure.None;
    }
}
