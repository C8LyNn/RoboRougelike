namespace B01.CHL003
{
    /// <summary>
    /// Distance gate for CHL's physical sphere-wheel ground probes.
    /// Ray length tracks the actual collider's vertical world radius;
    /// only a 0.03m near-contact allowance is permitted (not 0.95m).
    /// C#-only so exact production source runs in .NET source QA.
    /// </summary>
    public static class CHL003WheelSupportWindow
    {
        public const float OriginLiftMeters = 0.14f;
        public const float NearContactAllowanceMeters = 0.03f;

        public static float MaximumCastMeters(float wheelVerticalRadiusMeters)
        {
            if (float.IsNaN(wheelVerticalRadiusMeters) ||
                float.IsInfinity(wheelVerticalRadiusMeters) ||
                wheelVerticalRadiusMeters <= 0f)
                return 0f;
            return wheelVerticalRadiusMeters + OriginLiftMeters +
                   NearContactAllowanceMeters;
        }

        public static bool WithinWheelReach(float hitDistanceMeters,
            float wheelVerticalRadiusMeters)
        {
            float reach = MaximumCastMeters(wheelVerticalRadiusMeters);
            return reach > 0f &&
                   !float.IsNaN(hitDistanceMeters) &&
                   !float.IsInfinity(hitDistanceMeters) &&
                   hitDistanceMeters >= 0f &&
                   hitDistanceMeters <= reach;
        }
    }
}
