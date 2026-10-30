using B01.ReattachClearance001;

namespace B01.DockHardening001
{
    public enum HardenedDockVerdict
    {
        Allowed = 0,
        NotArmed = 1,
        UnsafeLegacyUi = 2,
        InvalidFullLayerQuery = 3,
        NewAuditRejected = 4,
        PreviousGateRejected = 5
    }

    /// <summary>
    /// W09 versioned, source-only aggregation of actual independent PhysX audits.
    /// Never permits docking if the W07/W08 bypass GUI is still enabled,
    /// if the independent W09 broadphase was filtered, or either gate rejects.
    /// </summary>
    public static class HardenedDockPolicy
    {
        public const int AllPhysicsLayers = -1;

        public static HardenedDockVerdict Evaluate(
            bool armed,
            bool originalDemoBehaviourEnabled,
            bool previousDockBehaviourEnabled,
            int queryLayerMask,
            DockVerdict fullLayerAudit,
            DockVerdict originalW08Audit)
        {
            if (!armed)
                return HardenedDockVerdict.NotArmed;
            if (originalDemoBehaviourEnabled || previousDockBehaviourEnabled)
                return HardenedDockVerdict.UnsafeLegacyUi;
            if (queryLayerMask != AllPhysicsLayers)
                return HardenedDockVerdict.InvalidFullLayerQuery;
            if (fullLayerAudit != DockVerdict.Allowed)
                return HardenedDockVerdict.NewAuditRejected;
            if (originalW08Audit != DockVerdict.Allowed)
                return HardenedDockVerdict.PreviousGateRejected;
            return HardenedDockVerdict.Allowed;
        }
    }
}
