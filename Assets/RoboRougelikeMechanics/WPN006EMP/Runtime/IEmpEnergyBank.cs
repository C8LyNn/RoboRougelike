namespace RoboRougelike.Mechanics.EMP
{
    /// <summary>Optional adapter to R6 energy. A successful pulse consumes exactly one activation cost.</summary>
    public interface IEmpEnergyBank
    {
        bool TrySpend(float amount);
        void Refund(float amount);
    }
}
