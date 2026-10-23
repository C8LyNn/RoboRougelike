namespace RoboRougelike.Defenses.DirectionalShield
{
    /// <summary>Integracja opcjonalnego zasobu energii. TryConsume musi być atomowe: pełna kwota albo zero.</summary>
    public interface IShieldEnergySource
    {
        bool TryConsume(float amount);
    }
}
