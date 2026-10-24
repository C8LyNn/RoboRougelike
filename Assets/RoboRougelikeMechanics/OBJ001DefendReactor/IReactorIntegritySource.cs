namespace RoboRougelike.Mechanics.OBJ001
{
    // Optional host authority. Standalone package users retain the original local HP behavior.
    public interface IReactorIntegritySource
    {
        float CurrentIntegrity { get; }
        float MaximumIntegrity { get; }
        void Damage(float amount);
        void Repair(float amount);
        void ResetIntegrity(float maximum);
    }
}
