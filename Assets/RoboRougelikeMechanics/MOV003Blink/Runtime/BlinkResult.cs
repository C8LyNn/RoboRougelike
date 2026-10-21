namespace RoboRougelike.Mechanics.MOV003Blink
{
    public enum BlinkResult
    {
        Activated,
        NotOperational,
        MissingRigidbody,
        InvalidRigidbody,
        MultiRigidbodyAssembly,
        DetachedFromVehicle,
        InvalidSettings,
        CoolingDown,
        MissingEnergySource,
        InsufficientEnergy,
        NoVehicleColliders,
        UnsupportedDestination,
        DestinationBlocked,
        PathBlocked,
        PhysicsQueryOverflow
    }
}
