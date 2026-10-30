using System;
using UnityEngine;

namespace Swarm.B01.Playtesting
{
    /// <summary>
    /// Optional Unity 6 technical-only, passive legacy-input inspector.
    /// Uses read-only polling; NEVER consumes or remaps input and does not write logs.
    /// Requires Active Input Handling: Input Manager (Old) or Both.
    /// </summary>
    public sealed class InputDiscoveryOverlay : MonoBehaviour
    {
        [SerializeField] private bool showAtStart = true;
        [SerializeField] private float panelWidth = 340f;
        [SerializeField] private float panelTop = 16f;

        private readonly InputDiscoveryLedger ledger = new InputDiscoveryLedger();
        private bool panelVisible;
        private bool legacyInputUnsupported;

        public int SeenCount { get { return ledger.SeenCount; } }
        public bool LegacyInputUnsupported { get { return legacyInputUnsupported; } }
        public bool PanelVisible { get { return panelVisible; } }
        public long? FirstPressMilliseconds(InputDiscoveryLedger.Control control)
        {
            return ledger.FirstPressMilliseconds(control);
        }

        private void OnEnable()
        {
            ledger.Begin(Time.realtimeSinceStartupAsDouble);
            panelVisible = showAtStart;
            legacyInputUnsupported = false;
        }

        private void Update()
        {
            if (legacyInputUnsupported) return;

            bool f8, w, a, s, d, space, left, right;
            try
            {
                f8 = Input.GetKeyDown(KeyCode.F8);
                w = Input.GetKeyDown(KeyCode.W);
                a = Input.GetKeyDown(KeyCode.A);
                s = Input.GetKeyDown(KeyCode.S);
                d = Input.GetKeyDown(KeyCode.D);
                space = Input.GetKeyDown(KeyCode.Space);
                left = Input.GetMouseButtonDown(0);
                right = Input.GetMouseButtonDown(1);
            }
            catch (InvalidOperationException)
            {
                // Unity project's Active Input Handling may be New Input System only.
                // Report incompatibility rather than pretend we observed controls.
                legacyInputUnsupported = true;
                return;
            }

            if (f8) panelVisible = !panelVisible;
            double now = Time.realtimeSinceStartupAsDouble;
            if (w) ledger.Observe(InputDiscoveryLedger.Control.KeyboardW, now);
            if (a) ledger.Observe(InputDiscoveryLedger.Control.KeyboardA, now);
            if (s) ledger.Observe(InputDiscoveryLedger.Control.KeyboardS, now);
            if (d) ledger.Observe(InputDiscoveryLedger.Control.KeyboardD, now);
            if (space) ledger.Observe(InputDiscoveryLedger.Control.KeyboardSpace, now);
            if (left) ledger.Observe(InputDiscoveryLedger.Control.MouseLeft, now);
            if (right) ledger.Observe(InputDiscoveryLedger.Control.MouseRight, now);
        }

        private void OnGUI()
        {
            if (!panelVisible) return;
            GUILayout.BeginArea(new Rect(12f, Mathf.Max(0f, panelTop),
                Mathf.Clamp(panelWidth, 290f, 500f), 325f), GUI.skin.box);
            GUILayout.Label("B01-13 — pasywna obserwacja klawiszy (F8)");
            GUILayout.Label("TEST TECHNICZNY — użycie wejścia ≠ skuteczna akcja gry");
            if (legacyInputUnsupported)
            {
                GUILayout.Label("NIEZGODNE: wymaga Input Manager (Old) lub Both.");
            }
            else
            {
                GUILayout.Label("Wykryte sygnały: " + ledger.SeenCount
                    + "/" + InputDiscoveryLedger.ControlCount);
                for (int i = 0; i < InputDiscoveryLedger.ControlCount; i++)
                {
                    InputDiscoveryLedger.Control key = (InputDiscoveryLedger.Control)i;
                    long? ms = ledger.FirstPressMilliseconds(key);
                    GUILayout.Label(InputDiscoveryLedger.DisplayName(key) + ": "
                        + (ms.HasValue ? ms.Value + " ms od startu" : "BRAK ODCZYTU"));
                }
            }

            if (GUILayout.Button("Resetuj tylko obserwator"))
            {
                ledger.Begin(Time.realtimeSinceStartupAsDouble);
                legacyInputUnsupported = false;
            }
            GUILayout.EndArea();
        }
    }
}
