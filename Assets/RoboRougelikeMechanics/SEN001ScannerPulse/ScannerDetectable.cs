using UnityEngine;

namespace RoboRougelike.Mechanics.SEN001
{
    public enum ScannerSignalKind { Unknown, Enemy, Salvage, Objective, Hazard }

    [System.Flags]
    public enum ScannerSignalMask
    {
        None = 0,
        Unknown = 1 << 0,
        Enemy = 1 << 1,
        Salvage = 1 << 2,
        Objective = 1 << 3,
        Hazard = 1 << 4,
        All = Unknown | Enemy | Salvage | Objective | Hazard
    }

    /// <summary>Opt-in tag: the scanner never writes to this component or its GameObject.</summary>
    [DisallowMultipleComponent]
    public sealed class ScannerDetectable : MonoBehaviour
    {
        [SerializeField] private ScannerSignalKind signalKind = ScannerSignalKind.Unknown;
        public ScannerSignalKind SignalKind => signalKind;

        /// <summary>Explicit configuration by the owner; the scanner itself never calls this.</summary>
        public void SetSignalKind(ScannerSignalKind kind) => signalKind = kind;
    }
}
