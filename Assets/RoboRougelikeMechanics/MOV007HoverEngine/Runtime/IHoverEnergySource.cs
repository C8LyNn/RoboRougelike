namespace RoboRougelike.Mechanics.MOV007
{
    /// <summary>Optional adapter to a real in-game energy resource.</summary>
    public interface IHoverEnergySource
    {
        bool CanSpend(float amount);
        bool TrySpend(float amount);
    }
}
