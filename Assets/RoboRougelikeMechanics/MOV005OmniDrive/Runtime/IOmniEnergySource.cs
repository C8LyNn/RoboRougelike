namespace RoboRougelike.Mechanics.MOV005OmniDrive
{
    /// <summary>Adapter to the game's SHARED energy pool. TrySpend must be atomic and reject overdraft.</summary>
    public interface IOmniEnergySource
    {
        bool TrySpend(float units);
    }
}
