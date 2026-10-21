namespace RoboRougelike.Mechanics.MOV004GyroscopicStabilizer
{
    /// <summary>
    /// Optional integration seam. Implement an adapter to the game's shared energy system.
    /// TrySpend must be atomic: return false without debiting anything if charge is insufficient.
    /// </summary>
    public interface IGyroEnergySource
    {
        bool TrySpend(float amount);
    }
}
