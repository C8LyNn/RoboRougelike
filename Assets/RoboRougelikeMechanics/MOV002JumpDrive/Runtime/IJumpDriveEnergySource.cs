namespace RoboRougelike.Mechanics.MOV002JumpDrive
{
    /// <summary>Adapter interface: implementation may be a shared in-game power system.</summary>
    public interface IJumpDriveEnergySource
    {
        bool TrySpend(float amount);
        void Refund(float amount);
    }
}
