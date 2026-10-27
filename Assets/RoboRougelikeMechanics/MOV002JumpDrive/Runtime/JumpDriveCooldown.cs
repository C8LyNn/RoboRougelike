namespace RoboRougelike.Mechanics.MOV002JumpDrive
{
    /// <summary>Clock supplied by caller. Pure state, suitable for EditMode NUnit tests.</summary>
    public sealed class JumpDriveCooldown
    {
        public float ReadyAt { get; private set; } = float.NegativeInfinity;

        public bool IsReady(float timeSeconds) =>
            JumpDriveMath.Valid(timeSeconds) && timeSeconds >= ReadyAt;

        public bool TryStart(float timeSeconds, float durationSeconds)
        {
            if (!IsReady(timeSeconds) || !JumpDriveMath.Valid(durationSeconds) || durationSeconds < 0f)
                return false;
            float newReadyAt = timeSeconds + durationSeconds;
            if (!JumpDriveMath.Valid(newReadyAt))
                return false;
            ReadyAt = newReadyAt;
            return true;
        }

        public float Remaining(float timeSeconds) =>
            IsReady(timeSeconds) ? 0f :
            !JumpDriveMath.Valid(timeSeconds) ? float.PositiveInfinity : ReadyAt - timeSeconds;

        public void Reset() => ReadyAt = float.NegativeInfinity;
    }
}
