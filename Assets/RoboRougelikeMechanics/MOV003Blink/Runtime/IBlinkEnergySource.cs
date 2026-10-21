namespace RoboRougelike.Mechanics.MOV003Blink
{
    // External energy providers can implement this without referencing MOV-001 or MOV-002.
    public interface IBlinkEnergySource
    {
        bool CanSpend(float amount);
        bool TrySpend(float amount);
    }
}
