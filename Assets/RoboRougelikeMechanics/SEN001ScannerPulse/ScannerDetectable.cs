using UnityEngine;

namespace RoboRougelike.Mechanics.SEN001
{
    public enum ScannerSignalKind { Unknown, Enemy, Salvage, Objective, Hazard }

    /// <summary>Opt-in tag: the scanner never writes to this component or its GameObject.</summary>
    [DisallowMultipleComponent]
    public sealed class ScannerDetectable : MonoBehaviour
    {
        [SerializeField] private ScannerSignalKind signalKind = ScannerSignalKind.Unknown;
        public ScannerSignalKind SignalKind => signalKind;
    }
}
