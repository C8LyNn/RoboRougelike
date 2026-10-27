namespace RoboRougelike.Mechanics.MOV006MagneticGrapple
{
    // Adapt the real game's energy system via this interface; do not add duplicate energy state.
    public interface IGrappleEnergySource
    {
        bool TrySpend(float amount);
    }
}
