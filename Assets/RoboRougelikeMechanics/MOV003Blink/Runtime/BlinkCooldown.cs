namespace RoboRougelike.Mechanics.MOV003Blink
{
    public sealed class BlinkCooldown
    {
        private float readyAt;
        private bool started;
        public float ReadyAt => started ? readyAt : 0f;
        public bool IsReady(float time) => BlinkMath.IsFinite(time) && (!started || time >= readyAt);
        public float Remaining(float time) => BlinkMath.IsFinite(time) && started ? UnityEngine.Mathf.Max(0f, readyAt - time) : 0f;
        public bool TryStart(float now, float cooldownSeconds)
        {
            if (!BlinkMath.IsFinite(now) || !BlinkMath.IsFinite(cooldownSeconds) || cooldownSeconds < 0f ||
                !IsReady(now) || !BlinkMath.IsFinite(now + cooldownSeconds)) return false;
            started = true;
            readyAt = now + cooldownSeconds;
            return true;
        }
        public void Reset() { started = false; readyAt = 0f; }
    }
}
