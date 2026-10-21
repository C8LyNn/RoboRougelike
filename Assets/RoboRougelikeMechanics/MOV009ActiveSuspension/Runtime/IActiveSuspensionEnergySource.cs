namespace RoboRougelike.Mechanics.MOV009
{
    /// <summary>Optional adapter for powered ride-height adjustment, not for passive spring support.</summary>
    public interface IActiveSuspensionEnergySource
    {
        bool TryConsumeEnergy(float amount);
    }
}
