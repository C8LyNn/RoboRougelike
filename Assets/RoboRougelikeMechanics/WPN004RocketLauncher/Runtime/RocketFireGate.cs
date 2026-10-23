namespace RoboRougelike.Mechanics.Rockets
{
    public sealed class RocketFireGate
    {
        public float NextReadyTime { get; private set; }
        public bool Ready(float now) => now >= NextReadyTime;
        public void Commit(float now, float cooldown)
        {
            NextReadyTime = now + RocketMath.NonNegative(cooldown);
        }
        public void Reset() => NextReadyTime = 0f;
    }
}
