using System;
using UnityEngine;

namespace Swarm.B01.Playtesting
{
    /// <summary>
    /// Optional test-scene-only observer. Samples a serialized target Transform and W/S.
    /// Does not write physics, consume input, alter target, or persist observations.
    /// </summary>
    public sealed class MotionStallOverlay : MonoBehaviour
    {
        [SerializeField] private Transform observedVehicle = null;
        [SerializeField] private bool visibleAtStart = true;
        [SerializeField] private float panelWidth = 380f;
        [SerializeField] private float panelTop = 12f;

        private readonly MotionStallLedger ledger = new MotionStallLedger();
        private bool visible;
        private bool inputUnsupported;

        public MotionStallLedger.State CurrentState { get { return ledger.CurrentState; } }
        public int StallCount { get { return ledger.SuspectedStallCount; } }
        public int RecoveryCount { get { return ledger.RecoveryCount; } }
        public int DiscontinuityCount { get { return ledger.DiscontinuityCount; } }
        public bool IsVisible { get { return visible; } }
        public bool InputUnsupported { get { return inputUnsupported; } }

        private void OnEnable()
        {
            visible = visibleAtStart;
            inputUnsupported = false;
            ResetObserver();
        }

        private void ResetObserver()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (observedVehicle == null)
                ledger.Begin(now, 0.0, 0.0);
            else
            {
                Vector3 p = observedVehicle.position;
                ledger.Begin(now, p.x, p.z);
            }
        }

        private void Update()
        {
            if (inputUnsupported) return;
            bool toggle, forward, back;
            try
            {
                toggle = Input.GetKeyDown(KeyCode.F7);
                forward = Input.GetKey(KeyCode.W);
                back = Input.GetKey(KeyCode.S);
            }
            catch (InvalidOperationException)
            {
                // Explicit failure when Old Input Manager is disabled.
                inputUnsupported = true;
                return;
            }

            if (toggle) visible = !visible;
            if (observedVehicle == null) return;

            Vector3 p = observedVehicle.position;
            // Inactive or stationary input does not imply the game character is stuck.
            ledger.Observe(Time.realtimeSinceStartupAsDouble, p.x, p.z, forward || back);
        }

        private void OnGUI()
        {
            if (!visible) return;
            GUILayout.BeginArea(new Rect(12f, Mathf.Max(0f, panelTop),
                Mathf.Clamp(panelWidth, 290f, 540f), 245f), GUI.skin.box);
            GUILayout.Label("B01-13 — OB-SUSPECT / F7: ukryj");
            GUILayout.Label("W/S + brak ruchu XZ ≥2s = podejrzenie, nie potwierdzony softlock.");
            if (observedVehicle == null)
                GUILayout.Label("BRAK Transform pojazdu — przypisz w Inspectorze.");
            else if (inputUnsupported)
                GUILayout.Label("INPUT NIEZGODNY — ustaw Old Input Manager lub Both.");
            else
            {
                GUILayout.Label("Stan: " + ledger.CurrentState);
                GUILayout.Label("Podejrzenia: " + ledger.SuspectedStallCount);
                GUILayout.Label("Wznowienia ruchu: " + ledger.RecoveryCount);
                GUILayout.Label("Przerwy / teleporty: " + ledger.DiscontinuityCount);
            }

            if (GUILayout.Button("Zresetuj wyłącznie obserwator"))
            {
                ResetObserver();
                inputUnsupported = false;
            }
            GUILayout.EndArea();
        }
    }
}
