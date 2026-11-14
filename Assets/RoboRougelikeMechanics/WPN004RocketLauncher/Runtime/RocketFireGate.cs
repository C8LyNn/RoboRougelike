namespace RoboRougelike.Mechanics.Rockets
{
    public sealed class RocketFireGate
    {
        public float NextReadyTime { get; private set; }
        double nextReadyExact;
        public bool Ready(float now) => !float.IsNaN(now)&&!float.IsInfinity(now)&&now >= nextReadyExact;
        public void Commit(float now, float cooldown)
        {
            if(float.IsNaN(now)||float.IsInfinity(now))return;
            double tick=System.Math.Max(double.Epsilon,System.Math.Abs((double)now)*2.2204460492503131e-16);
            nextReadyExact=(double)now+System.Math.Max((double)RocketMath.NonNegative(cooldown),tick);
            NextReadyTime=(float)System.Math.Min(float.MaxValue,nextReadyExact);
        }
        public void Reset(){NextReadyTime=0f;nextReadyExact=0d;}
    }
}
