namespace RoboRougelike.Mechanics.MOV008
{
    /// <summary>Optional adapter: units are arbitrary but must match energyPerSecond.</summary>
    public interface ICrawlerEnergySource
    {
        bool TryConsume(float amount);
    }
}
