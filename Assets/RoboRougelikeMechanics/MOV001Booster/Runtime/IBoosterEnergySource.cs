namespace RoboRougelike.Mechanics.Booster
{
    /// <summary>
    /// Optional energy provider. Integration adapters can implement this interface.
    /// Callers must explicitly assign the implementing MonoBehaviour to VehicleBooster.
    /// </summary>
    public interface IBoosterEnergySource
    {
        /// <summary>Atomically spend energy, returning false if insufficient.</summary>
        bool TryConsume(float amount);
    }
}
